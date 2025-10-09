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
using System.ComponentModel.Design;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace TilingWindowManager
{
    public partial class WindowManager
    {
        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern nint WindowFromPoint(POINT Point);

        private Dictionary<nint, DragState> dragStates = new Dictionary<nint, DragState>();
        private Dictionary<nint, DateTime> lastDragOperationTime = new Dictionary<nint, DateTime>();
        private const int DRAG_THRESHOLD = 30; // pixels to distinguish between move and resize
        private const int SWAP_PROXIMITY_THRESHOLD = 30; // pixels around window edges to detect proximity
        private const int SWAP_DISTANCE_THRESHOLD = 1000; // maximum distance to window center for swapping
        private const int MIN_DRAG_DURATION_MS = 30; // minimum drag time to prevent accidental swaps
        nint monitorHandleOnDragStart;

        private struct DragState
        {
            public RECT InitialRect;
            public POINT InitialCursorPos;
            public bool IsMoving; // true if moving, false if resizing
            public bool HasMoved;
            public DateTime StartTime;
            public nint TargetWindow; 
        }

        private nint locationChangeHook = nint.Zero;

        private void InitializeDragAndSwap()
        {
            locationEventProc = new WinEventDelegate(LocationChangeEventProc);
            locationEventProcHandle = GCHandle.Alloc(locationEventProc);

            locationChangeHook = SetWinEventHook(
                EVENT_OBJECT_LOCATIONCHANGE, EVENT_OBJECT_LOCATIONCHANGE,
                nint.Zero, locationEventProc, 0, 0, WINEVENT_OUTOFCONTEXT);

            if (locationChangeHook == nint.Zero)
            {
                Logger.Error("Warning: Failed to install location change hook for drag detection");
            }
        }

        private void LocationChangeEventProc(nint hWinEventHook, uint eventType, nint hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            const int OBJID_WINDOW = 0;
            const int CHILDID_SELF = 0;

            if (idObject != OBJID_WINDOW || idChild != CHILDID_SELF || hwnd == nint.Zero)
                return;

            if (isTilingInProgress) return;

            try
            {
                if (eventType == EVENT_OBJECT_LOCATIONCHANGE && IsWindowInCurrentWorkspace(hwnd))
                {
                    MonitorDragMovement(hwnd);

                    if (windowBorder != null && windowBorder.IsEnabled && GetForegroundWindow() == hwnd)
                    {
                        windowBorder.UpdateBorderForWindow(hwnd);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in LocationChangeEventProc: {ex.Message}");
            }
        }

        private void CleanupDragAndSwap()
        {
            if (locationChangeHook != nint.Zero)
            {
                UnhookWinEvent(locationChangeHook);
                locationChangeHook = nint.Zero;
            }

            dragStates.Clear();
            lastDragOperationTime.Clear();
        }

        private void OnUserStartsDragOrResize(nint window)
        {
            try
            {
                if (!GetWindowRect(window, out RECT windowRect))
                    return;

                if (!GetCursorPos(out POINT cursorPos))
                    return;

                // determine if this is a move or resize operation based on cursor position relative to window
                bool isMoving = IsCursorInMoveArea(cursorPos, windowRect);

                var dragState = new DragState
                {
                    InitialRect = windowRect,
                    InitialCursorPos = cursorPos,
                    IsMoving = isMoving,
                    HasMoved = false,
                    StartTime = DateTime.Now,
                    TargetWindow = nint.Zero
                };

                dragStates[window] = dragState;

                if (windowBorder != null && windowBorder.IsEnabled)
                {
                    windowBorder.UpdateBorderForWindow(window);
                }

                // still store for resize functionality
                preMoveWindowPositions[window] = windowRect;
                if (monitorHandleOnDragStart == 0)
                {
                     monitorHandleOnDragStart = MonitorFromWindow(window, MONITOR_DEFAULTTONEAREST);

                }

            }
            catch (Exception ex)
            {
                Logger.Error($"Error in OnUserStartsDragOrResize: {ex.Message}");
            }
        }

        private void OnUserFinishesDragOrResize(nint window)
        {
            try
            {
                if (!dragStates.TryGetValue(window, out DragState dragState))
                {
                    // no drag state found
                    OnUserFinishesResizing(window);
                    return;
                }

                if (!GetWindowRect(window, out RECT finalRect))
                {
                    // failed to get final window rect
                    dragStates.Remove(window);
                    return;
                }

                var dragDuration = DateTime.Now - dragState.StartTime;

                if (dragState.IsMoving && dragState.HasMoved && dragDuration.TotalMilliseconds > MIN_DRAG_DURATION_MS)
                {
                    // this was a move operation - try to perform swap
                    if (dragState.TargetWindow != nint.Zero && dragState.TargetWindow != window)
                    {
                        var status = PerformWindowSwap(window, dragState.TargetWindow);
                        if (!status)
                        {
                            foreach (var monitor in monitors)
                            {
                                var workspace = monitor.FindWorkspaceContaining(window);
                                if (workspace != null)
                                {
                                    workspace.GetTiling()?.RepositionWindowToOriginalState(window);
                                    break;
                                }
                            }
                        }
                        else
                        {
                            UpdateWorkspaceIndicator();
                        }

                    }
                    else
                    {
                        // no valid swap target found -> check if we can move to an empty workspace
                        if (!GetCursorPos(out POINT cursorPos))
                        {
                            //cursor position not identified, restoring back
                            RestoreWindowToOriginalPosition(window);
                            return;
                        }

                        nint targetMonitorHandle = MonitorFromPoint(cursorPos, MONITOR_DEFAULTTONEAREST);
                        var targetMonitor = GetMonitorByHandle(targetMonitorHandle);

                        if (targetMonitor != null)
                        {
                            var targetWorkspace = targetMonitor.GetCurrentWorkspace();

                            if (targetWorkspace.WindowCount == 0)
                            {
                                var sourceMonitor = GetMonitorByHandle(monitorHandleOnDragStart);

                                foreach (var monitor in monitors)
                                {
                                    monitor.RemoveWindowFromAllWorkspaces(window);
                                    monitor.RemoveWindowTrackingPositions(window);
                                }

                                targetMonitor.AddWindowToCurrentWorkspace(window);

								ApplyTilingToCurrentWorkspace(sourceMonitor);
								ApplyTilingToCurrentWorkspace(targetMonitor);

                                monitorHandleOnDragStart = 0;
                                return;
                            }
                        }
                        // if got to this line something is wrong , restore to original
                        RestoreWindowToOriginalPosition(window);
                    }
                }
                else if (!dragState.IsMoving)
                {
                    if(!OnUserFinishesResizing(window))
                    {
                        foreach (var monitor in monitors)
                        {
                            var workspace = monitor.FindWorkspaceContaining(window);
                            if (workspace != null)
                            {
                                workspace.GetTiling()?.RepositionWindowToOriginalState(window);
                                break;
                            }
                        }
                    }
                }
                else
                {
                    foreach (var monitor in monitors)
                    {
                        var workspace = monitor.FindWorkspaceContaining(window);
                        if (workspace != null)
                        {
                            workspace.GetTiling()?.RepositionWindowToOriginalState(window);
                            break;
                        }
                    }
                }

                dragStates.Remove(window);
                lastDragOperationTime[window] = DateTime.Now;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in OnUserFinishesDragOrResize: {ex.Message}");
                dragStates.Remove(window);
            }
        }

        private bool IsCursorInMoveArea(POINT cursorPos, RECT windowRect)
        {
            const int RESIZE_BORDER_SIZE = 8; 
            const int TITLE_BAR_HEIGHT = 40; 

            if (cursorPos.x < windowRect.Left || cursorPos.x > windowRect.Right ||
                cursorPos.y < windowRect.Top || cursorPos.y > windowRect.Bottom)
            {
                return true;
            }

            bool inLeftBorder = (cursorPos.x - windowRect.Left) <= RESIZE_BORDER_SIZE;
            bool inRightBorder = (windowRect.Right - cursorPos.x) <= RESIZE_BORDER_SIZE;
            bool inTopBorder = (cursorPos.y - windowRect.Top) <= RESIZE_BORDER_SIZE;
            bool inBottomBorder = (windowRect.Bottom - cursorPos.y) <= RESIZE_BORDER_SIZE;

            if ((inLeftBorder && inTopBorder) || (inRightBorder && inTopBorder) ||
                (inLeftBorder && inBottomBorder) || (inRightBorder && inBottomBorder))
            {
                return false; 
            }
            if (cursorPos.y > windowRect.Top + TITLE_BAR_HEIGHT)
            {
                if (inLeftBorder || inRightBorder || inBottomBorder)
                {
                    return false;
                }
            }

            if (cursorPos.y >= windowRect.Top && cursorPos.y <= windowRect.Top + TITLE_BAR_HEIGHT)
            {
                return true; 
            }

            return true;
        }

        private void MonitorDragMovement(nint window)
        {
            if (!dragStates.TryGetValue(window, out DragState dragState) || !dragState.IsMoving)
                return;

            try
            {
                if (!GetCursorPos(out POINT currentPos))
                    return;

                if (!GetWindowRect(window, out RECT currentRect))
                    return;

                int deltaX = currentPos.x - dragState.InitialCursorPos.x;
                int deltaY = currentPos.y - dragState.InitialCursorPos.y;
                int totalMovement = (int)Math.Sqrt(deltaX * deltaX + deltaY * deltaY);

                bool wasMoving = dragState.HasMoved;
                dragState.HasMoved = totalMovement > DRAG_THRESHOLD;
                dragStates[window] = dragState;

                if (dragState.HasMoved)
                {
                    nint targetWindow = FindSwapTarget(window, currentPos);

                    if (targetWindow != dragState.TargetWindow)
                    {
                        dragState.TargetWindow = targetWindow;
                        dragStates[window] = dragState;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error monitoring drag movement: {ex.Message}");
            }
        }

        private nint FindSwapTarget(nint draggedWindow, POINT cursorPos)
        {
            try
            {
                nint monitorHandle = MonitorFromWindow(draggedWindow, MONITOR_DEFAULTTONEAREST);
                var monitor = GetMonitorByHandle(monitorHandle);
                if (monitor == null)
                {
                    return nint.Zero;
                }

                var currentWorkspace = monitor.GetCurrentWorkspace();
                var tiledWindows = currentWorkspace.GetTileableWindows().Where(w =>
                    w != draggedWindow &&
                    IsWindowVisible(w)).ToList();


                nint closestWindow = nint.Zero;
                double closestDistance = double.MaxValue;

                foreach (var window in tiledWindows)
                {
                    if (!GetWindowRect(window, out RECT windowRect))
                        continue;

                    int windowCenterX = windowRect.Left + windowRect.Width / 2;
                    int windowCenterY = windowRect.Top + windowRect.Height / 2;

                    double distance = Math.Sqrt(
                        Math.Pow(cursorPos.x - windowCenterX, 2) +
                        Math.Pow(cursorPos.y - windowCenterY, 2));

                    bool isOverWindow = cursorPos.x >= windowRect.Left - SWAP_PROXIMITY_THRESHOLD &&
                                       cursorPos.x <= windowRect.Right + SWAP_PROXIMITY_THRESHOLD &&
                                       cursorPos.y >= windowRect.Top - SWAP_PROXIMITY_THRESHOLD &&
                                       cursorPos.y <= windowRect.Bottom + SWAP_PROXIMITY_THRESHOLD;


                    if (isOverWindow && distance < closestDistance && distance <= SWAP_DISTANCE_THRESHOLD)
                    {
                        closestWindow = window;
                        closestDistance = distance;
                    }
                }

                bool finalResult = closestWindow != nint.Zero;

                return finalResult ? closestWindow : nint.Zero;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error finding swap target: {ex.Message}");
                return nint.Zero;
            }
        }

        private bool PerformWindowSwap(nint sourceWindow, nint targetWindow)
        {
            bool status = false;
            try
            {
                if (lastDragOperationTime.ContainsKey(sourceWindow) &&
                    (DateTime.Now - lastDragOperationTime[sourceWindow]).TotalMilliseconds < 1000)
                {
                    return status;
                }

                if (lastDragOperationTime.ContainsKey(targetWindow) &&
                    (DateTime.Now - lastDragOperationTime[targetWindow]).TotalMilliseconds < 1000)
                {
                    return status;
                }

                nint monitorHandle = MonitorFromWindow(sourceWindow, MONITOR_DEFAULTTONEAREST);
                var monitor = GetMonitorByHandle(monitorHandle);
                if (monitor == null) return status;

                var currentWorkspace = monitor.GetCurrentWorkspace();
                if (!currentWorkspace.ContainsWindow(sourceWindow) || !currentWorkspace.ContainsWindow(targetWindow))
                {
                    var targetMonitorHandle = MonitorFromWindow(targetWindow, MONITOR_DEFAULTTONEAREST);
                    var sourceMonitor = GetMonitorByHandle(monitorHandleOnDragStart);
                    var targetMonitor = GetMonitorByHandle(targetMonitorHandle);

                    if (sourceMonitor != null && targetMonitor != null)
                    {
                        sourceMonitor.RemoveWindowFromAllWorkspaces(sourceWindow);
                        sourceMonitor.RemoveWindowTrackingPositions(sourceWindow);
                        targetMonitor.RemoveWindowFromAllWorkspaces(targetWindow);
                        targetMonitor.RemoveWindowTrackingPositions(targetWindow);

                        sourceMonitor.AddWindowToCurrentWorkspace(targetWindow);
                        targetMonitor.AddWindowToCurrentWorkspace(sourceWindow);

						ApplyTilingToCurrentWorkspace(sourceMonitor);
						ApplyTilingToCurrentWorkspace(targetMonitor);
                    }

                    monitorHandleOnDragStart = 0;
                    status = true;

                    return status;
                }

                isTilingInProgress = true;

                bool sourceRectValid = GetWindowRect(sourceWindow, out RECT sourceRect);
                bool targetRectValid = GetWindowRect(targetWindow, out RECT targetRect);

                bool swapSuccessful = currentWorkspace.SwapWindows(sourceWindow, targetWindow);

                if (swapSuccessful)
                {
                    var tiledWindows = currentWorkspace.GetTileableWindows();
                    foreach (var window in tiledWindows)
                    {
                        if (GetWindowRect(window, out RECT rect))
                        {
                            monitor.TrackWindowPositions(window, rect);
                        }
                    }

                    SetForegroundWindow(sourceWindow);
                    BringWindowToTop(sourceWindow);

                    status = true;
                    return status;
                }
                else
                {
                    if (sourceRectValid)
                    {
                        SuspendBorder(sourceWindow);
                        SetWindowPos(sourceWindow, nint.Zero, sourceRect.Left, sourceRect.Top,
                            sourceRect.Width, sourceRect.Height, SWP_NOZORDER | SWP_NOACTIVATE);
                        RefreshBorder(sourceWindow);
                    }
                    if (targetRectValid)
                    {
                        SuspendBorder(targetWindow);
                        SetWindowPos(targetWindow, nint.Zero, targetRect.Left, targetRect.Top,
                            targetRect.Width, targetRect.Height, SWP_NOZORDER | SWP_NOACTIVATE);
                        RefreshBorder(targetWindow);
                    }
                    return status;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error performing window swap: {ex.Message}");
                return status;
            }
            finally
            {
                isTilingInProgress = false;
            }
        }

        private BSPTiling.FocusDirection GetDirectionToTarget(nint sourceWindow, nint targetWindow)
        {
            if (!GetWindowRect(sourceWindow, out RECT sourceRect) ||
                !GetWindowRect(targetWindow, out RECT targetRect))
            {
                return BSPTiling.FocusDirection.Right; 
            }

            int sourceCenterX = sourceRect.Left + sourceRect.Width / 2;
            int sourceCenterY = sourceRect.Top + sourceRect.Height / 2;
            int targetCenterX = targetRect.Left + targetRect.Width / 2;
            int targetCenterY = targetRect.Top + targetRect.Height / 2;

            int deltaX = targetCenterX - sourceCenterX;
            int deltaY = targetCenterY - sourceCenterY;

            if (Math.Abs(deltaX) > Math.Abs(deltaY))
            {
                return deltaX > 0 ? BSPTiling.FocusDirection.Right : BSPTiling.FocusDirection.Left;
            }
            else
            {
                return deltaY > 0 ? BSPTiling.FocusDirection.Down : BSPTiling.FocusDirection.Up;
            }
        }

        private void RestoreWindowToOriginalPosition(nint window)
        {
            foreach (var monitor in monitors)
            {
                var workspace = monitor.FindWorkspaceContaining(window);
                if (workspace != null)
                {
                    workspace.GetTiling()?.RepositionWindowToOriginalState(window);
                    break;
                }
            }
        }
    }
}