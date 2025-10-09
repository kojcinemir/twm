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
using System.Text;
using System.Threading.Tasks;

namespace TilingWindowManager
{
    public partial class WindowManager 
    {
       private Dictionary<nint, int> windowMinimumSizes = new Dictionary<nint, int>();
       private void OnUserStartsResizing(nint window)
        {
            if (GetWindowRect(window, out RECT rect))
            {
                preMoveWindowPositions[window] = rect;
            }
        }

        private bool OnUserFinishesResizing(nint window)
        {
            bool hasChanged = false;
            if (!GetWindowRect(window, out RECT newRect))
                return hasChanged;

            if (!preMoveWindowPositions.TryGetValue(window, out RECT oldRect))
                return hasChanged;

             hasChanged = Math.Abs(newRect.Left - oldRect.Left) > 5 ||
                            Math.Abs(newRect.Top - oldRect.Top) > 5 ||
                            Math.Abs(newRect.Width - oldRect.Width) > 10 ||
                            Math.Abs(newRect.Height - oldRect.Height) > 10;

            if (hasChanged)
            {

                nint monitorHandle = MonitorFromWindow(window, MONITOR_DEFAULTTONEAREST);
                UpdateBSPAndRetileFromUserResize(window, newRect, oldRect, monitorHandle);
            }

            preMoveWindowPositions.Remove(window);
            return hasChanged;
        }
        private void UpdateBSPAndRetileFromUserResize(nint window, RECT newRect, RECT oldRect, nint monitorHandle)
        {
            try
            {
                RECT actualOldRect;
                var monitor = GetMonitorByHandle(monitorHandle);
                var trackedPosition = monitor?.GetWindowPosition(window);
                if (trackedPosition.HasValue)
                {
                    actualOldRect = trackedPosition.Value;
                }
                else
                {
                    if (!GetWindowRect(window, out actualOldRect))
                    {
                        actualOldRect = oldRect;
                    }
                }

                int widthDiff = newRect.Width - actualOldRect.Width;
                int heightDiff = newRect.Height - actualOldRect.Height;

                int leftDiff = newRect.Left - actualOldRect.Left;
                int topDiff = newRect.Top - actualOldRect.Top;


                if (Math.Abs(widthDiff) < 5 && Math.Abs(heightDiff) < 5)
                {
                    return;
                }

                if (monitor == null)
                {
                    return;
                }

                bool draggedLeftEdge = false;
                bool draggedRightEdge = false;
                bool draggedTopEdge = false;
                bool draggedBottomEdge = false;

                if (Math.Abs(leftDiff) > 2)
                {
                    draggedLeftEdge = true;
                }
                else if (Math.Abs(widthDiff) > 5)
                {
                    draggedRightEdge = true;
                }

                if (Math.Abs(topDiff) > 2)
                {
                    draggedTopEdge = true;
                }
                else if (Math.Abs(heightDiff) > 5)
                {
                    draggedBottomEdge = true;
                }

                int widthPixelChange = Math.Abs(widthDiff);
                int heightPixelChange = Math.Abs(heightDiff);

                bool isPrimaryWidthChange = widthPixelChange > heightPixelChange;
                BSPTiling.ResizeDirection resizeDirection = isPrimaryWidthChange ?
                    BSPTiling.ResizeDirection.Horizontal : BSPTiling.ResizeDirection.Vertical;

                if (widthPixelChange < 5 && heightPixelChange < 5)
                {
                    return;
                }

                isTilingInProgress = true;

                var currentWorkspace = monitor.GetCurrentWorkspace();
                var tiling = currentWorkspace.GetTiling();
                tiling?.ResizeSplitWithEdgeInfo(window, widthDiff, heightDiff, resizeDirection, draggedLeftEdge, draggedRightEdge, draggedTopEdge, draggedBottomEdge);

                if (monitor != null)
                {
                    var validWindows = currentWorkspace.GetTileableWindows();

                    foreach (var w in validWindows)
                    {
                        if (GetWindowRect(w, out RECT rect))
                        {
                            monitor.TrackWindowPositions(w, rect);
                        }
                    }
                }

                isTilingInProgress = false;
                UpdateAllWindowPositions(monitorHandle);
            }
            catch (Exception)
            {
                isTilingInProgress = false;
            }
        }
        private void UpdateAllWindowPositions(nint monitorHandle)
        {
            try
            {
                var monitor = GetMonitorByHandle(monitorHandle);
                if (monitor != null)
                {
                    var currentWorkspace = monitor.GetCurrentWorkspace();
                    var validWindows = currentWorkspace.GetTileableWindows();

                    foreach (var w in validWindows)
                    {
                        if (GetWindowRect(w, out RECT rect))
                        {
                            monitor.TrackWindowPositions(w, rect);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }

        private void AdjustSplitRatio(float delta)
        {
            nint activeWindow = GetForegroundWindow();
            if (activeWindow == nint.Zero)
            {
                return;
            }

            foreach (var monitor in monitors)
            {
                var currentWorkspace = monitor.GetCurrentWorkspace();
                if (currentWorkspace.ContainsWindow(activeWindow))
                {
                    if (!GetWindowRect(activeWindow, out RECT beforeRect))
                    {
                        return;
                    }


                    if (delta < 0 && beforeRect.Width <= 480)
                    {
                        return;
                    }

                    try
                    {
                        isTilingInProgress = true;

                        var validWindows = currentWorkspace.GetTileableWindows();
                        foreach (var w in validWindows)
                        {
                            SuspendBorder(w);
                        }

                        currentWorkspace.ResizeSplit(activeWindow, delta);

                        if (GetWindowRect(activeWindow, out RECT afterRect))
                        {

                            int actualWidthChange = afterRect.Width - beforeRect.Width;
                            int actualHeightChange = afterRect.Height - beforeRect.Height;

                            if (Math.Abs(actualWidthChange) < 20 && Math.Abs(actualHeightChange) < 20 && Math.Abs(delta) > 0.05f)
                            {
                                if (delta < 0) 
                                {
                                    windowMinimumSizes[activeWindow] = afterRect.Width;
                                }

                                foreach (var w in validWindows)
                                {
                                    RefreshBorder(w);
                                }

                                return;
                            }
                        }

                        foreach (var w in validWindows)
                        {
                            if (GetWindowRect(w, out RECT rect))
                            {
                                monitor.TrackWindowPositions(w, rect);
                            }
                        }
                        currentWorkspace.GetTiling()?.ClearWindowSizeOverrides();

                        foreach (var w in validWindows)
                        {
                            RefreshBorder(w);
                        }

                    }
                    catch (Exception ex)
                    {
                    }
                    finally
                    {
                        isTilingInProgress = false;
                    }

                    break;
                }
            }
        }
    }
}
