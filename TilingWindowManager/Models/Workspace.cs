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
    public class Workspace
    {
        public int Id { get; private set; }
        public int MonitorIndex { get; private set; }
        public string Name { get; private set; }

        private Windows windows;
        private BSPTiling bspTiling = null!;
        private nint lastActiveWindow = nint.Zero;
        private bool isStackedMode = false;
        private int currentStackedWindowIndex = 0;
        private bool isPaused = false;

        public int WindowCount => windows.Count;
        public bool IsStackedMode => isStackedMode;
        public bool IsPaused => isPaused;

        public Workspace(int id, int monitorIndex)
        {
            Id = id;
            MonitorIndex = monitorIndex;
            Name = $"Monitor {monitorIndex} - Workspace {id}";

            windows = new Windows();
        }

        public void InitializeTiling(Monitor.RECT workingArea)
        {
            bspTiling = new BSPTiling(windows, workingArea);
        }

        public void UpdateWorkingArea(Monitor.RECT workingArea)
        {
            bspTiling?.UpdateWorkingArea(workingArea);
        }

        public BSPTiling GetTiling()
        {
            return bspTiling;
        }

        public Windows GetWindows()
        {
            return windows;
        }

        public void AddWindow(nint window)
        {
            windows.AddWindow(window);
            if (!isPaused && !isStackedMode)
            {
                bspTiling?.AddWindow(window);
            }
        }

        public bool RemoveWindow(nint window)
        {
            bool removed = windows.RemoveWindow(window);
            if (removed && !isPaused && !isStackedMode)
            {
                bspTiling?.RemoveWindow(window);
            }
            return removed;
        }

        public bool ContainsWindow(nint window)
        {
            return windows.Contains(window);
        }

        public List<nint> GetAllWindows()
        {
            return windows.GetAllWindows();
        }

        public List<nint> GetTileableWindows()
        {
            return windows.GetTileableWindows();
        }

        public List<nint> GetVisibleWindows()
        {
            return windows.GetVisibleWindows();
        }

        public List<nint> GetStackableWindows()
        {
            return windows.GetStackableWindows();
        }

        public void ShowAllWindows()
        {
            windows.ShowAllWindows();
        }

        public void HideAllWindows()
        {
            windows.HideAllWindows();
        }

        public void ShowWindow(nint window)
        {
            windows.ShowWindow(window);
        }

        public void HideWindow(nint window)
        {
            windows.HideWindow(window);
        }

        public void ClearWindows()
        {
            windows.ClearWindows();
            bspTiling?.Clear();
        }

        public void ApplyTiling()
        {
            if (!isPaused)
            {
                bspTiling?.TileWindows();
            }
        }

        public nint GetWindowInDirection(nint currentWindow, BSPTiling.FocusDirection direction)
        {
            return bspTiling?.GetWindowInDirection(currentWindow, direction) ?? nint.Zero;
        }

        public bool SwapWindowInDirection(nint currentWindow, BSPTiling.FocusDirection direction)
        {
            return bspTiling?.SwapWindowInDirection(currentWindow, direction) ?? false;
        }

        public bool SwapWindows(nint window1, nint window2)
        {
            return bspTiling?.SwapWindows(window1, window2) ?? false;
        }

        public void ResizeSplit(nint window, float deltaRatio)
        {
            bspTiling?.ResizeSplit(window, deltaRatio);
        }

        public void SetLastActiveWindow(nint window)
        {
            if (ContainsWindow(window))
            {
                lastActiveWindow = window;
            }
        }

        public nint GetLastActiveWindow()
        {
            return lastActiveWindow;
        }

        public nint GetRootWindow()
        {
            var windowOrder = bspTiling?.GetWindowOrder();
            if (windowOrder != null && windowOrder.Count > 0)
            {
                return windowOrder[0];
            }
            return nint.Zero;
        }

        public void EnableStackedMode()
        {
            isStackedMode = true;
            currentStackedWindowIndex = 0;
        }

        public void DisableStackedMode()
        {
            isStackedMode = false;
            currentStackedWindowIndex = 0;
            // rebuild BSP tree when exiting stacked mode
            if (!isPaused)
            {
                bspTiling?.TileWindows();
            }
        }

        public void FocusNewestWindowInStack()
        {
            var stackableWindows = GetStackableWindows();
            if (stackableWindows.Count > 0)
            {
                currentStackedWindowIndex = stackableWindows.Count - 1;
            }
        }

        public void ToggleStackedMode()
        {
            if (isStackedMode)
            {
                DisableStackedMode();
            }
            else
            {
                EnableStackedMode();
            }
        }

        public void EnablePausedMode()
        {
            isPaused = true;
        }

        public void DisablePausedMode()
        {
            isPaused = false;
        }

        public void TogglePausedMode()
        {
            if (isPaused)
            {
                DisablePausedMode();
            }
            else
            {
                EnablePausedMode();
            }
        }

        public void CycleStackedWindow()
        {
            var stackableWindows = GetStackableWindows();
            if (!isStackedMode || stackableWindows.Count == 0)
                return;


            currentStackedWindowIndex = (currentStackedWindowIndex + 1) % stackableWindows.Count;

        }

        public void CycleStackedWindow(int direction)
        {
            var stackableWindows = GetStackableWindows();
            if (!isStackedMode || stackableWindows.Count == 0)
                return;


            if (direction > 0)
            {
                // cycle forward (right)
                currentStackedWindowIndex = (currentStackedWindowIndex + 1) % stackableWindows.Count;
            }
            else
            {
                // cycle backward (left)
                currentStackedWindowIndex = (currentStackedWindowIndex - 1 + stackableWindows.Count) % stackableWindows.Count;
            }

        }

        public int GetCurrentStackedWindowIndex()
        {
            return currentStackedWindowIndex;
        }

        public void SetCurrentStackedWindowIndex(int index)
        {
            var stackableWindows = GetStackableWindows();
            if (isStackedMode && stackableWindows.Count > 0)
            {
                currentStackedWindowIndex = Math.Max(0, Math.Min(index, stackableWindows.Count - 1));
            }
        }

        public override string ToString()
        {
            return $"{Name} ({WindowCount} windows)";
        }
    }
}