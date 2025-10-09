/*
 * Tiling Window Manager
 * Copyright (C) 2025 Kojčin Emir
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */


﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace TilingWindowManager
{
    public class BSPTiling
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(nint hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetClassName(nint hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(nint hWnd, out Monitor.RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool ClientToScreen(nint hWnd, ref POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern uint GetWindowLong(nint hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(nint hWnd, out Monitor.RECT lpRect);

        [DllImport("dwmapi.dll")]
        private static extern int DwmGetWindowAttribute(nint hwnd, int dwAttribute, out Monitor.RECT pvAttribute, int cbAttribute);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }

        private const int GWL_STYLE = -16;
        private const int GWL_EXSTYLE = -20;
        private const uint WS_BORDER = 0x00800000;
        private const uint WS_THICKFRAME = 0x00040000;
        private const uint WS_EX_CLIENTEDGE = 0x00000200;
        private const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;

        private Windows windows;
        private Monitor.RECT workingArea;

        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_FRAMECHANGED = 0x0020;
        private const int SW_RESTORE = 9;

        private const float DEFAULT_SPLIT_RATIO = 0.5f;
        private const int WINDOW_MARGIN = -4; 
        public enum ResizeDirection
        {
            Horizontal, 
            Vertical    
        }
        public enum FocusDirection
        {
            Left,
            Right,
            Up,
            Down
        }
        public class BSPNode
        {
            public Monitor.RECT Bounds { get; set; }
            public nint Window { get; set; }
            public BSPNode? LeftChild { get; set; }
            public BSPNode? RightChild { get; set; }
            public bool IsVerticalSplit { get; set; }
            public float SplitRatio { get; set; } = DEFAULT_SPLIT_RATIO;
            public BSPNode? Parent { get; set; }

            public bool IsLeaf => LeftChild == null && RightChild == null;
            public bool HasWindow => Window != nint.Zero;
        }

        private class TilingData
        {
            public BSPNode RootNode { get; set; } = null!;
            public Dictionary<nint, BSPNode> WindowToNodeMap { get; set; } = new Dictionary<nint, BSPNode>();
            public List<nint> WindowInsertionOrder { get; set; } = new List<nint>();

        }

        private TilingData tilingData;
        private Dictionary<nint, Monitor.RECT> windowSizeOverrides = new Dictionary<nint, Monitor.RECT>();
        private WindowBorderConfiguration borderConfig = new WindowBorderConfiguration();
        private readonly Dictionary<nint, (Monitor.RECT Corr, long Ticks)> _corrCache = new();

        public BSPTiling(Windows windowsInstance, Monitor.RECT workingArea)
        {
            windows = windowsInstance ?? throw new ArgumentNullException(nameof(windowsInstance));
            this.workingArea = workingArea;
            tilingData = new TilingData();
            borderConfig.LoadConfiguration();
        }

        public void UpdateWorkingArea(Monitor.RECT newWorkingArea)
        {
            workingArea = newWorkingArea;
        }

        private bool IsElectronApp(nint window)
        {
            try
            {
                uint processId;
                GetWindowThreadProcessId(window, out processId);

                var process = System.Diagnostics.Process.GetProcessById((int)processId);
                string processName = process.ProcessName.ToLower();

                string[] electronIndicators = {
                    "code", "discord", "slack", "whatsapp", "spotify",
                    "atom", "figma", "notion", "obsidian"
                };

                return electronIndicators.Any(indicator => processName.Contains(indicator));
            }
            catch
            {
                return false;
            }
        }

        private string GetWindowClassName(nint window)
        {
            try
            {
                var className = new System.Text.StringBuilder(256);
                GetClassName(window, className, className.Capacity);
                return className.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }

        private bool IsBorderlessWindow(nint window)
        {
            try
            {
                uint style = GetWindowLong(window, GWL_STYLE);
                uint exStyle = GetWindowLong(window, GWL_EXSTYLE);

                bool hasThickFrame = (style & WS_THICKFRAME) != 0;
                bool hasBorder = (style & WS_BORDER) != 0;
                bool hasClientEdge = (exStyle & WS_EX_CLIENTEDGE) != 0;

                string className = GetWindowClassName(window);
                string[] borderlessClasses = {
                    "Chrome_WidgetWin_1",
                    "ApplicationFrameWindow",
                    "CASCADIA_HOSTING_WINDOW_CLASS",
                    "Qt5QWindowIcon",
                };

                bool isKnownBorderless = borderlessClasses.Any(cls =>
                    className.Contains(cls, StringComparison.OrdinalIgnoreCase));

                return !hasThickFrame || !hasBorder || isKnownBorderless;
            }
            catch
            {
                return false;
            }
        }

        private Monitor.RECT CalculateFrameCorrection(nint window, Monitor.RECT windowRect)
        {
            var correction = new Monitor.RECT
            {
                Left = 0,
                Top = 0,
                Right = 0,
                Bottom = 0
            };

            try
            {
                Monitor.RECT dwmRect;
                int dwmResult = DwmGetWindowAttribute(window, DWMWA_EXTENDED_FRAME_BOUNDS, out dwmRect, Marshal.SizeOf(typeof(Monitor.RECT)));

                if (dwmResult == 0)
                {
                    correction.Left = dwmRect.Left - windowRect.Left;
                    correction.Top = dwmRect.Top - windowRect.Top;
                    correction.Right = windowRect.Right - dwmRect.Right;
                    correction.Bottom = windowRect.Bottom - dwmRect.Bottom;

                    return correction;
                }

                if (!GetClientRect(window, out Monitor.RECT clientRect))
                {
                    return correction;
                }

                var clientTopLeft = new POINT { x = 0, y = 0 };
                var clientBottomRight = new POINT { x = clientRect.Right, y = clientRect.Bottom };

                ClientToScreen(window, ref clientTopLeft);
                ClientToScreen(window, ref clientBottomRight);

                var actualClientRect = new Monitor.RECT
                {
                    Left = clientTopLeft.x,
                    Top = clientTopLeft.y,
                    Right = clientBottomRight.x,
                    Bottom = clientBottomRight.y
                };

                int leftOffset = actualClientRect.Left - windowRect.Left;
                int topOffset = actualClientRect.Top - windowRect.Top;
                int rightOffset = windowRect.Right - actualClientRect.Right;
                int bottomOffset = windowRect.Bottom - actualClientRect.Bottom;

                if (IsBorderlessWindow(window))
                {
                    string className = GetWindowClassName(window);
                    if (className.Contains("Chrome_WidgetWin_1") || IsElectronApp(window))
                    {
                        leftOffset = Math.Max(0, leftOffset - 1);
                        topOffset = Math.Max(0, topOffset - 1);
                        rightOffset = Math.Max(0, rightOffset - 1);
                        bottomOffset = Math.Max(0, bottomOffset - 1);
                    }
                }

                correction.Left = leftOffset;
                correction.Top = topOffset;
                correction.Right = rightOffset;
                correction.Bottom = bottomOffset;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error calculating frame correction in BSPTiling: {ex.Message}");
            }

            return correction;
        }

        public void TileWindows()
        {
            var windowsToTile = windows.GetTileableWindows();
            if (windowsToTile.Count == 0)
            {
                return;
            }

            tilingData.RootNode = null!;
            tilingData.WindowToNodeMap.Clear();

            var orderedWindows = new List<nint>();

            foreach (var existingWindow in tilingData.WindowInsertionOrder)
            {
                if (windowsToTile.Contains(existingWindow))
                {
                    orderedWindows.Add(existingWindow);
                }
            }

            foreach (var window in windowsToTile)
            {
                if (!tilingData.WindowInsertionOrder.Contains(window))
                {
                    orderedWindows.Add(window);
                    tilingData.WindowInsertionOrder.Add(window);
                }
            }

            tilingData.RootNode = BuildBSPTree(orderedWindows, workingArea, 0);
            if (tilingData.RootNode != null)
            {
                PositionWindows(tilingData.RootNode);
            }
        }


        private BSPNode? BuildBSPTree(List<nint> windows, Monitor.RECT bounds, int depth)
        {
            if (windows.Count == 0)
                return null;

            BSPNode node = new BSPNode
            {
                Bounds = bounds
            };

            if (windows.Count == 1)
            {
                node.Window = windows[0];
                tilingData.WindowToNodeMap[windows[0]] = node;
                return node;
            }

            bool isVerticalSplit = depth % 2 == 0;
            node.IsVerticalSplit = isVerticalSplit;

            var firstWindow = new List<nint> { windows[0] };
            var remainingWindows = windows.Skip(1).ToList();

            Monitor.RECT leftBounds, rightBounds;
            CalculateSplitBounds(bounds, isVerticalSplit, 0.5f, out leftBounds, out rightBounds);

            node.LeftChild = BuildBSPTree(firstWindow, leftBounds, depth + 1);
            node.RightChild = BuildBSPTree(remainingWindows, rightBounds, depth + 1);

            if (node.LeftChild != null)
                node.LeftChild.Parent = node;
            if (node.RightChild != null)
                node.RightChild.Parent = node;

            return node;
        }

        private void CalculateSplitBounds(Monitor.RECT parentBounds, bool isVerticalSplit, float splitRatio, out Monitor.RECT leftBounds, out Monitor.RECT rightBounds)
        {
            if (isVerticalSplit)
            {
                // for vertical split 
                int totalWidth = parentBounds.Width;
                int splitX = (int)Math.Round(parentBounds.Left + totalWidth * splitRatio);

                leftBounds = new Monitor.RECT
                {
                    Left = parentBounds.Left,
                    Top = parentBounds.Top,
                    Right = splitX - WINDOW_MARGIN / 2,
                    Bottom = parentBounds.Bottom
                };

                rightBounds = new Monitor.RECT
                {
                    Left = splitX + WINDOW_MARGIN / 2,
                    Top = parentBounds.Top,
                    Right = parentBounds.Right,
                    Bottom = parentBounds.Bottom
                };
            }
            else
            {
                // for horizontal split 
                int totalHeight = parentBounds.Height;
                int splitY = parentBounds.Top + (int)(totalHeight * splitRatio);

                leftBounds = new Monitor.RECT
                {
                    Left = parentBounds.Left,
                    Top = parentBounds.Top,
                    Right = parentBounds.Right,
                    Bottom = splitY - WINDOW_MARGIN / 2
                };

                rightBounds = new Monitor.RECT
                {
                    Left = parentBounds.Left,
                    Top = splitY + WINDOW_MARGIN / 2,
                    Right = parentBounds.Right,
                    Bottom = parentBounds.Bottom
                };
            }
        }

        private Monitor.RECT GetWindowBoundsWithBorderSpace(Monitor.RECT bounds)
        {
            int borderWidth = borderConfig.BorderWidth;

            return new Monitor.RECT
            {
                Left = bounds.Left + borderWidth,
                Top = bounds.Top + borderWidth,
                Right = bounds.Right - borderWidth,
                Bottom = bounds.Bottom - borderWidth
            };
        }

        private bool TryGetCorrection(nint hwnd, out Monitor.RECT corr)
        {
            if (_corrCache.TryGetValue(hwnd, out var e) &&
                (Environment.TickCount64 - e.Ticks) < 500)
            {
                corr = e.Corr;
                return true;
            }
            corr = default;
            return false;
        }

        private void SetCorrection(nint hwnd, Monitor.RECT corr)
        {
            _corrCache[hwnd] = (corr, Environment.TickCount64);
        }

        private Monitor.RECT GetCorrectedBounds(nint window, Monitor.RECT desiredBounds)
        {
            Monitor.RECT corrected = desiredBounds;
            if (GetWindowRect(window, out Monitor.RECT currentWindowRect))
            {
                if (!TryGetCorrection(window, out Monitor.RECT correction))
                {
                    correction = CalculateFrameCorrection(window, currentWindowRect);
                    SetCorrection(window, correction);
                }

                corrected = new Monitor.RECT
                {
                    Left = desiredBounds.Left - correction.Left,
                    Top = desiredBounds.Top - correction.Top,
                    Right = desiredBounds.Right + correction.Right,
                    Bottom = desiredBounds.Bottom + correction.Bottom
                };
            }
            return corrected;
        }

        private void PositionWindow(nint window, Monitor.RECT bounds)
        {
            var desired = GetWindowBoundsWithBorderSpace(bounds);
            var corrected = GetCorrectedBounds(window, desired);
            windows.PositionWindow(window, corrected);
        }

        private void HandleWindowSizeOverrides(BSPNode node)
        {
            if (node == null) return;

            if (node.IsLeaf && node.HasWindow)
            {
                if (windowSizeOverrides.TryGetValue(node.Window, out Monitor.RECT actualRect))
                {
                    node.Bounds = actualRect;
                }
            }
            else
            {
                if (node.LeftChild != null) HandleWindowSizeOverrides(node.LeftChild);
                if (node.RightChild != null) HandleWindowSizeOverrides(node.RightChild);
            }
        }

        private void PositionWindows(BSPNode node)
        {
            if (node == null)
                return;

            var batch = new List<(nint, Monitor.RECT)>();
            CollectPositions(node, batch);
            windows.PositionWindowsBatch(batch);
        }

        private void CollectPositions(BSPNode node, List<(nint, Monitor.RECT)> batch)
        {
            if (node == null) return;

            if (node.IsLeaf && node.HasWindow)
            {
                var desired = GetWindowBoundsWithBorderSpace(node.Bounds);
                var corrected = GetCorrectedBounds(node.Window, desired);
                batch.Add((node.Window, corrected));
            }
            else
            {
                if (node.LeftChild != null) CollectPositions(node.LeftChild, batch);
                if (node.RightChild != null) CollectPositions(node.RightChild, batch);
            }
        }

        private void PositionWindowsFirstPass(BSPNode node)
        {
            if (node == null)
                return;

            if (node.IsLeaf && node.HasWindow)
            {
                PositionWindow(node.Window, node.Bounds);
            }
            else
            {
                if (node.LeftChild != null) PositionWindowsFirstPass(node.LeftChild);
                if (node.RightChild != null) PositionWindowsFirstPass(node.RightChild);
            }
        }

        private bool DetectAndHandleWindowSizeOverrides(BSPNode node)
        {
            if (node == null)
                return false;

            bool hasOverrides = false;

            if (node.IsLeaf && node.HasWindow)
            {
                if (windowSizeOverrides.ContainsKey(node.Window))
                {
                    hasOverrides = true;
                    Monitor.RECT actualRect = windowSizeOverrides[node.Window];
                    node.Bounds = actualRect;
                }
            }
            else
            {
                bool leftHasOverrides = node.LeftChild != null && DetectAndHandleWindowSizeOverrides(node.LeftChild);
                bool rightHasOverrides = node.RightChild != null && DetectAndHandleWindowSizeOverrides(node.RightChild);
                hasOverrides = leftHasOverrides || rightHasOverrides;
            }

            return hasOverrides;
        }

        private void RecalculateTreeFromActualSizes(BSPNode node)
        {
            if (node == null || node.IsLeaf)
                return;

            if (node.LeftChild != null) RecalculateTreeFromActualSizes(node.LeftChild);
            if (node.RightChild != null) RecalculateTreeFromActualSizes(node.RightChild);

            if (node.LeftChild != null && node.RightChild != null)
            {
                if (node.IsVerticalSplit)
                {
                    int leftWidth = node.LeftChild.Bounds.Width;
                    int rightWidth = node.RightChild.Bounds.Width;
                    int totalWidth = leftWidth + rightWidth + Math.Abs(WINDOW_MARGIN); 

                    if (totalWidth > 0)
                    {
                        float newRatio = (float)leftWidth / totalWidth;
                        newRatio = Math.Max(0.05f, Math.Min(0.95f, newRatio)); 
                        node.SplitRatio = newRatio;
                        Monitor.RECT leftBounds, rightBounds;
                        if (node.LeftChild.IsLeaf && windowSizeOverrides.ContainsKey(node.LeftChild.Window))
                        {
                            Monitor.RECT overriddenRect = windowSizeOverrides[node.LeftChild.Window];
                            leftBounds = overriddenRect;
                            rightBounds = new Monitor.RECT
                            {
                                Left = overriddenRect.Right + WINDOW_MARGIN / 2,
                                Top = node.Bounds.Top,
                                Right = node.Bounds.Right,
                                Bottom = node.Bounds.Bottom
                            };

                        }
                        else if (node.RightChild.IsLeaf && windowSizeOverrides.ContainsKey(node.RightChild.Window))
                        {
                            Monitor.RECT overriddenRect = windowSizeOverrides[node.RightChild.Window];
                            rightBounds = overriddenRect;

                            leftBounds = new Monitor.RECT
                            {
                                Left = node.Bounds.Left,
                                Top = node.Bounds.Top,
                                Right = overriddenRect.Left - WINDOW_MARGIN / 2,
                                Bottom = node.Bounds.Bottom
                            };

                        }
                        else
                        {
                            CalculateSplitBounds(node.Bounds, true, newRatio, out leftBounds, out rightBounds);
                        }

                        node.LeftChild.Bounds = leftBounds;
                        node.RightChild.Bounds = rightBounds;
                    }
                }
                else
                {
                    int topHeight = node.LeftChild.Bounds.Height;
                    int bottomHeight = node.RightChild.Bounds.Height;
                    int totalHeight = topHeight + bottomHeight + Math.Abs(WINDOW_MARGIN);

                    if (totalHeight > 0)
                    {
                        float newRatio = (float)topHeight / totalHeight;
                        newRatio = Math.Max(0.05f, Math.Min(0.95f, newRatio));


                        node.SplitRatio = newRatio;

                        Monitor.RECT leftBounds, rightBounds;

                        if (node.LeftChild.IsLeaf && windowSizeOverrides.ContainsKey(node.LeftChild.Window))
                        {
                            Monitor.RECT overriddenRect = windowSizeOverrides[node.LeftChild.Window];
                            leftBounds = overriddenRect;

                            rightBounds = new Monitor.RECT
                            {
                                Left = node.Bounds.Left,
                                Top = overriddenRect.Bottom + WINDOW_MARGIN / 2,
                                Right = node.Bounds.Right,
                                Bottom = node.Bounds.Bottom
                            };

                        }
                        else if (node.RightChild.IsLeaf && windowSizeOverrides.ContainsKey(node.RightChild.Window))
                        {
                            Monitor.RECT overriddenRect = windowSizeOverrides[node.RightChild.Window];
                            rightBounds = overriddenRect;

                            leftBounds = new Monitor.RECT
                            {
                                Left = node.Bounds.Left,
                                Top = node.Bounds.Top,
                                Right = node.Bounds.Right,
                                Bottom = overriddenRect.Top - WINDOW_MARGIN / 2
                            };

                        }
                        else
                        {
                            CalculateSplitBounds(node.Bounds, false, newRatio, out leftBounds, out rightBounds);
                        }

                        node.LeftChild.Bounds = leftBounds;
                        node.RightChild.Bounds = rightBounds;
                    }
                }
            }
        }

        public void ClearWindowSizeOverrides()
        {
            windowSizeOverrides.Clear();
        }

        public void AddWindow(nint window)
        {
            if (tilingData.WindowToNodeMap.ContainsKey(window))
            {
                return;
            }

            windows.AddWindow(window);

            if (!tilingData.WindowInsertionOrder.Contains(window))
            {
                tilingData.WindowInsertionOrder.Add(window);
            }

            if (tilingData.RootNode == null)
            {
                tilingData.RootNode = new BSPNode
                {
                    Bounds = workingArea,
                    Window = window
                };
                tilingData.WindowToNodeMap[window] = tilingData.RootNode;
                PositionWindow(window, workingArea);
                return;
            }

            TileWindows();
        }

        public void RemoveWindow(nint window)
        {
            if (!tilingData.WindowToNodeMap.ContainsKey(window))
            {
                return;
            }

            windows.RemoveWindow(window);
            tilingData.WindowInsertionOrder.Remove(window);

            tilingData.RootNode = null!;
            tilingData.WindowToNodeMap.Clear();

            if (tilingData.WindowInsertionOrder.Count > 0)
            {
                TileWindows();
            }
        }

        private bool WouldViolateMinimumSizes(BSPNode node, float newRatio)
        {
            if (node == null || node.IsLeaf) return false;

            Monitor.RECT leftBounds, rightBounds;
            CalculateSplitBounds(node.Bounds, node.IsVerticalSplit, newRatio, out leftBounds, out rightBounds);

            return (node.LeftChild != null && CheckSubtreeMinimumViolation(node.LeftChild, leftBounds)) ||
                   (node.RightChild != null && CheckSubtreeMinimumViolation(node.RightChild, rightBounds));
        }

        private bool CheckSubtreeMinimumViolation(BSPNode node, Monitor.RECT bounds)
        {
            if (node == null) return false;

            if (node.IsLeaf)
            {
                const int PRACTICAL_MIN_WIDTH = 470; 
                const int PRACTICAL_MIN_HEIGHT = 150;

                return bounds.Width < PRACTICAL_MIN_WIDTH || bounds.Height < PRACTICAL_MIN_HEIGHT;
            }

            Monitor.RECT leftBounds, rightBounds;
            CalculateSplitBounds(bounds, node.IsVerticalSplit, node.SplitRatio, out leftBounds, out rightBounds);

            return (node.LeftChild != null && CheckSubtreeMinimumViolation(node.LeftChild, leftBounds)) ||
                   (node.RightChild != null && CheckSubtreeMinimumViolation(node.RightChild, rightBounds));
        }

        public void ResizeSplit(nint window, float deltaRatio)
        {
            if (!tilingData.WindowToNodeMap.ContainsKey(window))
            {
                return;
            }

            BSPNode windowNode = tilingData.WindowToNodeMap[window];
            BSPNode? parent = windowNode.Parent;
            if (parent == null)
            {
                return;
            }

            float oldRatio = parent.SplitRatio;
            float newRatio = Math.Max(0.05f, Math.Min(0.95f, parent.SplitRatio + deltaRatio));

            if (WouldViolateMinimumSizes(parent, newRatio))
            {
                return;
            }

            parent.SplitRatio = newRatio;

            RecalculateBounds(parent);
            PositionWindows(parent);
        }

        public void ResizeSplitWithEdgeInfo(nint window, int widthDiff, int heightDiff, ResizeDirection direction,
            bool draggedLeftEdge, bool draggedRightEdge, bool draggedTopEdge, bool draggedBottomEdge)
        {
            if (!tilingData.WindowToNodeMap.ContainsKey(window))
            {
                return;
            }

            BSPNode windowNode = tilingData.WindowToNodeMap[window];
            BSPNode? targetNode = null;

            if (direction == ResizeDirection.Horizontal)
            {
                if (draggedLeftEdge)
                {
                    targetNode = FindSplitForLeftwardExpansion(windowNode);
                }
                else if (draggedRightEdge)
                {
                    targetNode = FindResizableParent(windowNode, direction);
                }
            }
            else if (direction == ResizeDirection.Vertical)
            {
                if (draggedTopEdge)
                {
                    targetNode = FindSplitForUpwardExpansion(windowNode);
                }
                else if (draggedBottomEdge)
                {
                    targetNode = FindResizableParent(windowNode, direction);
                }
            }

            if (targetNode == null)
            {
                targetNode = FindResizableParent(windowNode, direction);
            }

            if (targetNode == null)
            {
                return;
            }

            float deltaRatio;
            if (direction == ResizeDirection.Horizontal)
            {
                int availableWidth = targetNode.Bounds.Width;
                deltaRatio = (float)widthDiff / availableWidth;
            }
            else
            {
                int availableHeight = targetNode.Bounds.Height;
                deltaRatio = (float)heightDiff / availableHeight;
            }

            if (Math.Abs(deltaRatio) < 0.005f)
            {
                return;
            }

            float oldRatio = targetNode.SplitRatio;

            float adjustedDelta = GetAdjustedDeltaWithEdge(windowNode, targetNode, deltaRatio, direction,
                draggedLeftEdge, draggedRightEdge, draggedTopEdge, draggedBottomEdge);

            float newRatio = Math.Max(0.1f, Math.Min(0.9f, oldRatio + adjustedDelta));

            targetNode.SplitRatio = newRatio;


            RecalculateBounds(targetNode);

            PositionWindows(targetNode);
        }

        private BSPNode FindSplitForLeftwardExpansion(BSPNode windowNode)
        {
            BSPNode current = windowNode;

            while (current.Parent != null)
            {
                var parent = current.Parent;
                if (parent.IsVerticalSplit && parent.RightChild != null)
                {
                    if (ContainsWindow(parent.RightChild, windowNode.Window))
                    {
                        return parent;
                    }
                }
                current = parent;
            }

            return null;
        }

        private BSPNode FindSplitForUpwardExpansion(BSPNode windowNode)
        {
            BSPNode current = windowNode;

            while (current.Parent != null)
            {
                var parent = current.Parent;
                if (!parent.IsVerticalSplit && parent.RightChild != null) 
                {
                    if (ContainsWindow(parent.RightChild, windowNode.Window))
                    {
                        return parent;
                    }
                }
                current = parent;
            }

            return null;
        }

        private float GetAdjustedDeltaWithEdge(BSPNode windowNode, BSPNode targetSplitNode, float originalDelta, ResizeDirection direction,
            bool draggedLeftEdge, bool draggedRightEdge, bool draggedTopEdge, bool draggedBottomEdge)
        {

            BSPNode childOfTarget = FindChildContainingWindow(targetSplitNode, windowNode.Window);
            bool isOnLeftOrTopSide = targetSplitNode.LeftChild == childOfTarget;


            if (direction == ResizeDirection.Horizontal && targetSplitNode.IsVerticalSplit)
            {
                if (draggedLeftEdge)
                {
                    if (!isOnLeftOrTopSide)
                    {
                        return -originalDelta;
                    }
                }
                else if (draggedRightEdge)
                {
                    if (!isOnLeftOrTopSide)
                    {
                        return -originalDelta;
                    }
                }
            }

            if (!isOnLeftOrTopSide)
            {
                return -originalDelta;
            }

            return originalDelta;
        }

        private BSPNode FindResizableParent(BSPNode node, ResizeDirection direction)
        {
            if (node == null || node.Parent == null)
                return null;

            BSPNode current = node;
            while (current.Parent != null)
            {
                var parent = current.Parent;

                bool isMatchingDirection = direction == ResizeDirection.Horizontal && parent.IsVerticalSplit ||
                                          direction == ResizeDirection.Vertical && !parent.IsVerticalSplit;

                if (isMatchingDirection)
                {
                    if (direction == ResizeDirection.Horizontal && parent.IsVerticalSplit)
                    {
                        bool isRootSplit = parent.Bounds.Left == 0 && parent.Bounds.Width >= 2000; 
                        if (isRootSplit)
                        {
                            return parent;
                        }
                    }

                    if (HasDirectSibling(current, parent))
                    {
                        return parent;
                    }
                }

                current = parent;
            }

            return null;
        }
        
        private bool HasDirectSibling(BSPNode childNode, BSPNode parentNode)
        {
            if (parentNode.LeftChild == null || parentNode.RightChild == null)
                return false;

            bool leftHasContent = parentNode.LeftChild.IsLeaf || HasAnyWindows(parentNode.LeftChild);
            bool rightHasContent = parentNode.RightChild.IsLeaf || HasAnyWindows(parentNode.RightChild);

            return leftHasContent && rightHasContent;
        }

        private bool HasAnyWindows(BSPNode node)
        {
            if (node == null)
                return false;

            if (node.IsLeaf)
                return node.HasWindow;

            return HasAnyWindows(node.LeftChild) || HasAnyWindows(node.RightChild);
        }

        
        private BSPNode FindChildContainingWindow(BSPNode splitNode, nint window)
        {
            if (splitNode == null || splitNode.IsLeaf)
                return null;

            if (splitNode.LeftChild != null && ContainsWindow(splitNode.LeftChild, window))
            {
                return splitNode.LeftChild;
            }

            if (splitNode.RightChild != null && ContainsWindow(splitNode.RightChild, window))
            {
                return splitNode.RightChild;
            }

            return null;
        }

        private bool ContainsWindow(BSPNode node, nint window)
        {
            if (node == null)
                return false;

            if (node.IsLeaf)
                return node.Window == window;

            return ContainsWindow(node.LeftChild, window) || ContainsWindow(node.RightChild, window);
        }

        
        private void RecalculateBounds(BSPNode node)
        {
            if (node == null || node.IsLeaf)
                return;

            Monitor.RECT leftBounds, rightBounds;
            CalculateSplitBounds(node.Bounds, node.IsVerticalSplit, node.SplitRatio, out leftBounds, out rightBounds);


            if (node.LeftChild != null)
            {
                node.LeftChild.Bounds = leftBounds;
                RecalculateBounds(node.LeftChild);
            }
            if (node.RightChild != null)
            {
                node.RightChild.Bounds = rightBounds;
                RecalculateBounds(node.RightChild);
            }
        }

        public nint GetWindowInDirection(nint currentWindow, FocusDirection direction)
        {
            if (!tilingData.WindowToNodeMap.ContainsKey(currentWindow))
                return nint.Zero;

            BSPNode currentNode = tilingData.WindowToNodeMap[currentWindow];
            BSPNode targetNode = FindWindowInDirection(currentNode, direction);

            if (targetNode == null)
            {
                targetNode = FindWrappingWindow(currentNode, direction);
            }

            return targetNode?.Window ?? nint.Zero;
        }

        private BSPNode FindWrappingWindow(BSPNode currentNode, FocusDirection direction)
        {
            if (tilingData.RootNode == null) return null;

            var allLeaves = new List<BSPNode>();
            CollectAllLeaves(tilingData.RootNode, allLeaves);

            allLeaves.RemoveAll(leaf => leaf == currentNode);

            if (allLeaves.Count == 0) return null;

            var currentX = currentNode.Bounds.Left + currentNode.Bounds.Width / 2;
            var currentY = currentNode.Bounds.Top + currentNode.Bounds.Height / 2;

            switch (direction)
            {
                case FocusDirection.Left:
                    return allLeaves
                        .OrderBy(leaf => Math.Abs(leaf.Bounds.Top + leaf.Bounds.Height / 2 - currentY))
                        .ThenByDescending(leaf => leaf.Bounds.Left) 
                        .FirstOrDefault();

                case FocusDirection.Right:
                    return allLeaves
                        .OrderBy(leaf => Math.Abs(leaf.Bounds.Top + leaf.Bounds.Height / 2 - currentY))
                        .ThenBy(leaf => leaf.Bounds.Left) 
                        .FirstOrDefault();

                case FocusDirection.Up:
                    return allLeaves
                        .OrderBy(leaf => Math.Abs(leaf.Bounds.Left + leaf.Bounds.Width / 2 - currentX))
                        .ThenBy(leaf => leaf.Bounds.Top) 
                        .FirstOrDefault();

                case FocusDirection.Down:
                    return allLeaves
                        .OrderBy(leaf => Math.Abs(leaf.Bounds.Left + leaf.Bounds.Width / 2 - currentX))
                        .ThenByDescending(leaf => leaf.Bounds.Top)
                        .FirstOrDefault();

                default:
                    return allLeaves.FirstOrDefault();
            }
        }
        private void CollectAllLeaves(BSPNode node, List<BSPNode> leaves)
        {
            if (node == null) return;

            if (node.IsLeaf && node.HasWindow)
            {
                leaves.Add(node);
                return;
            }

            CollectAllLeaves(node.LeftChild, leaves);
            CollectAllLeaves(node.RightChild, leaves);
        }

        private BSPNode FindWindowInDirection(BSPNode currentNode, FocusDirection direction)
        {
            if (currentNode?.Parent == null)
                return null;

            BSPNode parent = currentNode.Parent;
            bool isCurrentNodeLeft = parent.LeftChild == currentNode;

            if (CanMoveInDirection(parent, direction, isCurrentNodeLeft))
            {
                BSPNode sibling = isCurrentNodeLeft ? parent.RightChild : parent.LeftChild;
                return FindClosestLeafInDirection(sibling, direction);
            }
            else
            {
                return FindWindowInDirection(parent, direction);
            }
        }

        private bool CanMoveInDirection(BSPNode splitNode, FocusDirection direction, bool isCurrentNodeLeft)
        {
            bool isVerticalSplit = splitNode.IsVerticalSplit;

            switch (direction)
            {
                case FocusDirection.Left:
                    return isVerticalSplit && !isCurrentNodeLeft;
                case FocusDirection.Right:
                    return isVerticalSplit && isCurrentNodeLeft;
                case FocusDirection.Up:
                    return !isVerticalSplit && !isCurrentNodeLeft;
                case FocusDirection.Down:
                    return !isVerticalSplit && isCurrentNodeLeft;
                default:
                    return false;
            }
        }

        private BSPNode FindClosestLeafInDirection(BSPNode node, FocusDirection direction)
        {
            if (node == null)
                return null;

            if (node.IsLeaf)
                return node;

            BSPNode preferred, fallback;
            if (node.IsVerticalSplit)
            {
                if (direction == FocusDirection.Left)
                {
                    preferred = node.LeftChild;   
                    fallback = node.RightChild;
                }
                else if (direction == FocusDirection.Right)
                {
                    preferred = node.RightChild;
                    fallback = node.LeftChild;
                }
                else 
                {
                    preferred = node.LeftChild;
                    fallback = node.RightChild;
                }
            }
            else
            {
                if (direction == FocusDirection.Up)
                {
                    preferred = node.LeftChild; 
                    fallback = node.RightChild;
                }
                else if (direction == FocusDirection.Down)
                {
                    preferred = node.RightChild; 
                    fallback = node.LeftChild;
                }
                else 
                {
                    preferred = node.LeftChild;
                    fallback = node.RightChild;
                }
            }

            BSPNode result = FindClosestLeafInDirection(preferred, direction);
            return result ?? FindClosestLeafInDirection(fallback, direction);
        }

        public int GetWindowCount()
        {
            return tilingData.WindowToNodeMap.Count;
        }

        public bool HasWindows()
        {
            return tilingData.WindowToNodeMap.Count > 0;
        }

        public void Clear()
        {
            tilingData.RootNode = null;
            tilingData.WindowToNodeMap.Clear();
            tilingData.WindowInsertionOrder.Clear();
            windows.ClearWindows();
        }

        public List<nint> GetWindowOrder()
        {
            return new List<nint>(tilingData.WindowInsertionOrder);
        }

        public void SetWindowOrder(List<nint> windowHandles)
        {
            tilingData.WindowInsertionOrder.Clear();
            tilingData.WindowInsertionOrder.AddRange(windowHandles);

            foreach (var window in windowHandles)
            {
                windows.AddWindow(window);
            }

            if (windowHandles.Count > 0)
            {
                TileWindows();
            }
        }
        
        private void RecalculateAllSplitRatios(BSPNode node)
        {
            if (node == null || node.IsLeaf)
                return;

            RecalculateAllSplitRatios(node.LeftChild);
            RecalculateAllSplitRatios(node.RightChild);

            if (node.LeftChild != null && node.RightChild != null)
            {
                if (node.IsVerticalSplit)
                {
                    // for vertical split
                    int leftWidth = GetSubtreeDesiredWidth(node.LeftChild);
                    int rightWidth = GetSubtreeDesiredWidth(node.RightChild);
                    int totalWidth = leftWidth + rightWidth;

                    if (totalWidth > 0)
                    {
                        float newRatio = (float)leftWidth / totalWidth;
                        newRatio = Math.Max(0.1f, Math.Min(0.9f, newRatio));

                        node.SplitRatio = newRatio;
                    }
                }
                else
                {
                    int topHeight = GetSubtreeDesiredHeight(node.LeftChild);
                    int bottomHeight = GetSubtreeDesiredHeight(node.RightChild);
                    int totalHeight = topHeight + bottomHeight;

                    if (totalHeight > 0)
                    {
                        float newRatio = (float)topHeight / totalHeight;
                        newRatio = Math.Max(0.1f, Math.Min(0.9f, newRatio));

                        node.SplitRatio = newRatio;
                    }
                }
            }
        }



        private int GetSubtreeDesiredWidth(BSPNode node)
        {
            if (node == null)
                return 0;

            if (node.IsLeaf)
                return node.Bounds.Width;

            if (node.IsVerticalSplit)
            {
                return GetSubtreeDesiredWidth(node.LeftChild) + GetSubtreeDesiredWidth(node.RightChild);
            }
            else
            {
                return Math.Max(GetSubtreeDesiredWidth(node.LeftChild), GetSubtreeDesiredWidth(node.RightChild));
            }
        }

        private int GetSubtreeDesiredHeight(BSPNode node)
        {
            if (node == null)
                return 0;

            if (node.IsLeaf)
                return node.Bounds.Height;

            if (node.IsVerticalSplit)
            {
                return Math.Max(GetSubtreeDesiredHeight(node.LeftChild), GetSubtreeDesiredHeight(node.RightChild));
            }
            else
            {
                return GetSubtreeDesiredHeight(node.LeftChild) + GetSubtreeDesiredHeight(node.RightChild);
            }
        }

        private void RecalculateEntireSubtree(BSPNode node)
        {
            if (node == null)
                return;

            if (node.IsLeaf)
                return;

            Monitor.RECT leftBounds, rightBounds;
            CalculateSplitBounds(node.Bounds, node.IsVerticalSplit, node.SplitRatio, out leftBounds, out rightBounds);

            if (node.LeftChild != null)
            {
                node.LeftChild.Bounds = leftBounds;
                RecalculateEntireSubtree(node.LeftChild);
            }

            if (node.RightChild != null)
            {
                node.RightChild.Bounds = rightBounds;
                RecalculateEntireSubtree(node.RightChild);
            }
        }


        private void RepositionAllWindows(BSPNode node)
        {
            if (node == null)
                return;

            if (node.IsLeaf && node.HasWindow)
            {
                PositionWindow(node.Window, node.Bounds);
            }
            else
            {
                RepositionAllWindows(node.LeftChild);
                RepositionAllWindows(node.RightChild);
            }
        }

        public bool SwapWindowInDirection(nint currentWindow, FocusDirection direction)
        {
            if (!tilingData.WindowToNodeMap.ContainsKey(currentWindow))
                return false;

            nint targetWindow = GetWindowInDirection(currentWindow, direction);

            if (targetWindow == nint.Zero || targetWindow == currentWindow)
                return false;

            BSPNode currentNode = tilingData.WindowToNodeMap[currentWindow];
            BSPNode targetNode = tilingData.WindowToNodeMap[targetWindow];

            currentNode.Window = targetWindow;
            targetNode.Window = currentWindow;

            tilingData.WindowToNodeMap[currentWindow] = targetNode;
            tilingData.WindowToNodeMap[targetWindow] = currentNode;

            int currentIndex = tilingData.WindowInsertionOrder.IndexOf(currentWindow);
            int targetIndex = tilingData.WindowInsertionOrder.IndexOf(targetWindow);

            if (currentIndex >= 0 && targetIndex >= 0)
            {
                tilingData.WindowInsertionOrder[currentIndex] = targetWindow;
                tilingData.WindowInsertionOrder[targetIndex] = currentWindow;
            }

            PositionWindow(currentWindow, targetNode.Bounds);
            PositionWindow(targetWindow, currentNode.Bounds);

            return true;
        }

        public bool SwapWindows(nint currentWindow, nint targetWindow)
        {
            if (!tilingData.WindowToNodeMap.ContainsKey(currentWindow))
                return false;

            if (!tilingData.WindowToNodeMap.ContainsKey(targetWindow))
                return false;

            BSPNode currentNode = tilingData.WindowToNodeMap[currentWindow];
            BSPNode targetNode = tilingData.WindowToNodeMap[targetWindow];

            currentNode.Window = targetWindow;
            targetNode.Window = currentWindow;

            tilingData.WindowToNodeMap[currentWindow] = targetNode;
            tilingData.WindowToNodeMap[targetWindow] = currentNode;

            int currentIndex = tilingData.WindowInsertionOrder.IndexOf(currentWindow);
            int targetIndex = tilingData.WindowInsertionOrder.IndexOf(targetWindow);

            if (currentIndex >= 0 && targetIndex >= 0)
            {
                tilingData.WindowInsertionOrder[currentIndex] = targetWindow;
                tilingData.WindowInsertionOrder[targetIndex] = currentWindow;
            }

            PositionWindow(currentWindow, targetNode.Bounds);
            PositionWindow(targetWindow, currentNode.Bounds);

            return true;
        }

        public bool RepositionWindowToOriginalState(nint currentWindow)
        {
            if (!tilingData.WindowToNodeMap.ContainsKey(currentWindow))
                return false;

            BSPNode currentNode = tilingData.WindowToNodeMap[currentWindow];
            PositionWindow(currentWindow, currentNode.Bounds);
            return true;
        }

    }
}