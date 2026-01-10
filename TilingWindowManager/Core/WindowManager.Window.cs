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
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace TilingWindowManager
{
    public partial class WindowManager
    {
        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
        private static extern int DwmGetWindowAttribute(nint hwnd, int dwAttribute, out RECT pvAttribute, int cbAttribute);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool GetClientRect(nint hWnd, out RECT lpRect);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ClientToScreen(nint hWnd, ref POINT lpPoint);

        private const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;

        public void SwitchFocus(BSPTiling.FocusDirection direction)
        {
            nint currentWindow = GetForegroundWindow();

            if (currentWindow == nint.Zero)
            {
                return;
            }

            Monitor targetMonitor = null;
            Workspace targetWorkspace = null;
            foreach (var monitor in monitors)
            {
                var workspace = monitor.FindWorkspaceContaining(currentWindow);
                if (workspace != null)
                {
                    targetMonitor = monitor;
                    targetWorkspace = workspace;
                    break;
                }
            }

            if (targetMonitor == null || targetWorkspace == null)
            {
                return;
            }

            if (targetWorkspace.IsStackedMode)
            {
                if (direction == BSPTiling.FocusDirection.Left)
                {
                    CycleStackedWindow(-1); // cycle backward
                }
                else if (direction == BSPTiling.FocusDirection.Right)
                {
                    CycleStackedWindow(1); // cycle forward
                }
                return;
            }

            windowBorder.HideAllBorders();

            nint targetWindow = targetWorkspace.GetWindowInDirection(currentWindow, direction);

            if (targetWindow != nint.Zero && targetWindow != currentWindow)
            {
                SetForegroundWindow(targetWindow);
                BringWindowToTop(targetWindow);
                targetWorkspace.SetLastActiveWindow(targetWindow);
            }
        }

        private void FocusMostRecentWindow(nint monitorHandle, List<nint> availableWindows)
        {
            if (availableWindows == null || availableWindows.Count == 0)
                return;

            var monitor = GetMonitorByHandle(monitorHandle);
            var currentWorkspace = monitor?.GetCurrentWorkspace();

            if(currentWorkspace.WindowCount > 0)
            {
                FocusWindow(currentWorkspace.WindowCount - 1);
            }
        }

        private void FocusLastActiveOrRootWindow(Workspace workspace)
        {
            if (workspace == null || workspace.WindowCount == 0)
                return;

            nint windowToFocus = nint.Zero;

            nint lastActive = workspace.GetLastActiveWindow();
            // in paused mode, skip minimized windows
            if (lastActive != nint.Zero && workspace.ContainsWindow(lastActive) && IsWindowVisible(lastActive) &&
                (!workspace.IsPaused || !IsIconic(lastActive)))
            {
                windowToFocus = lastActive;
            }
            else
            {
                // fall back to root window -> first window in BPS tree
                nint rootWindow = workspace.GetRootWindow();
                // in paused mode, skip minimized windows
                if (rootWindow != nint.Zero && workspace.ContainsWindow(rootWindow) && IsWindowVisible(rootWindow) &&
                    (!workspace.IsPaused || !IsIconic(rootWindow)))
                {
                    windowToFocus = rootWindow;
                }
                else
                {
                    // focus first available tilable window if nothing else can be selected
                    var tileableWindows = workspace.GetTileableWindows();
                    // in paused mode, filter out minimized windows
                    if (workspace.IsPaused)
                    {
                        tileableWindows = tileableWindows.Where(w => !IsIconic(w)).ToList();
                    }
                    if (tileableWindows.Count > 0)
                    {
                        windowToFocus = tileableWindows[0];
                    }
                }
            }

            if (windowToFocus != nint.Zero)
            {
                FocusWindow(windowToFocus, workspace);
            }
        }

        private void FocusWindow(nint window, Workspace? workspace = null)
        {
            try
            {
                // find workspace if not provided
                if (workspace == null)
                {
                    foreach (var monitor in monitors)
                    {
                        workspace = monitor.FindWorkspaceContaining(window);
                        if (workspace != null)
                            break;
                    }
                }

                // in paused mode don't restore minimised windows
                if (workspace == null || !workspace.IsPaused || !IsIconic(window))
                {
                    ShowWindow(window, SW_RESTORE);
                }

                SetForegroundWindow(window);
                BringWindowToTop(window);

                if (workspace != null)
                {
                    workspace.SetLastActiveWindow(window);
                }
            }
            catch (Exception)
            {
            }
        }

        private void SwapActiveWindowInDirection(BSPTiling.FocusDirection direction)
        {
            nint activeWindow = GetForegroundWindow();

            if (activeWindow == nint.Zero)
            {
                return;
            }

            Monitor sourceMonitor = null;
            Workspace currentWorkspace = null;
            foreach (var monitor in monitors)
            {
                var workspace = monitor.FindWorkspaceContaining(activeWindow);
                if (workspace != null)
                {
                    sourceMonitor = monitor;
                    currentWorkspace = workspace;
                    break;
                }
            }

            if (sourceMonitor == null || currentWorkspace == null)
            {
                return;
            }

            var tiledWindows = currentWorkspace.GetTileableWindows();

            bool isAtEdge = IsWindowAtMonitorEdge(activeWindow, sourceMonitor, direction);

            if (isAtEdge)
            {
                MonitorDirection monitorDirection = ConvertToMonitorDirection(direction);
                var adjacentMonitor = FindMonitorInDirection(sourceMonitor, monitorDirection);

                if (adjacentMonitor != null && adjacentMonitor.Index != sourceMonitor.Index)
                {
                    MoveWindowToAdjacentMonitor(activeWindow, sourceMonitor, direction);
                    return;
                }
            }

            if (tiledWindows.Count < 2)
            {
                MoveWindowToAdjacentMonitor(activeWindow, sourceMonitor, direction);
                return;
            }

            try
            {
                isTilingInProgress = true;

                foreach (var window in tiledWindows)
                {
                    SuspendBorder(window);
                }

                bool swapSuccessful = currentWorkspace.SwapWindowInDirection(activeWindow, direction);

                if (swapSuccessful)
                {
                    foreach (var window in tiledWindows)
                    {
                        if (GetWindowRect(window, out RECT rect))
                        {
                            sourceMonitor.TrackWindowPositions(window, rect);
                        }
                    }

                    foreach (var window in tiledWindows)
                    {
                        RefreshBorder(window);
                    }
                }
                else
                {
                    MoveWindowToAdjacentMonitor(activeWindow, sourceMonitor, direction);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error during window swap: {ex.Message}");
            }
            finally
            {
                isTilingInProgress = false;
            }
        }
        private MonitorDirection ConvertToMonitorDirection(BSPTiling.FocusDirection direction)
        {
            switch (direction)
            {
                case BSPTiling.FocusDirection.Left:
                    return MonitorDirection.Left;
                case BSPTiling.FocusDirection.Right:
                    return MonitorDirection.Right;
                case BSPTiling.FocusDirection.Up:
                    return MonitorDirection.Up;
                case BSPTiling.FocusDirection.Down:
                    return MonitorDirection.Down;
                default:
                    return MonitorDirection.Right;
            }
        }
        private bool IsValidApplicationWindow(nint hWnd)
        {

            if (!IsWindowVisible(hWnd)) return false;

            uint exStyle = (uint)GetWindowLong(hWnd, GWL_EXSTYLE);

            if ((exStyle & WS_EX_TOOLWINDOW) != 0)
            {
                return false;
            }

            if ((exStyle & WS_EX_APPWINDOW) != 0)
            {
                string title = GetWindowTitle(hWnd);
                return !string.IsNullOrWhiteSpace(title) && title.Length >= 2;
            }

            if (GetWindow(hWnd, GW_OWNER) != nint.Zero)
            {
                return false;
            }

            string windowTitle = GetWindowTitle(hWnd);
            if (string.IsNullOrWhiteSpace(windowTitle) || windowTitle.Length < 2)
            {
                return false;
            }

            return true;
        }
        private string GetWindowTitle(nint hwnd)
        {
            try
            {
                int length = GetWindowTextLength(hwnd);
                if (length == 0) return string.Empty;

                StringBuilder sb = new StringBuilder(length + 1);
                GetWindowText(hwnd, sb, sb.Capacity);
                return sb.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }

        private bool IsWindowAtMonitorEdge(nint window, Monitor monitor, BSPTiling.FocusDirection direction)
        {
            if (!GetWindowRect(window, out RECT windowRect))
                return false;

            const int EDGE_TOLERANCE = 50; 

            switch (direction)
            {
                case BSPTiling.FocusDirection.Left:
                    return Math.Abs(windowRect.Left - monitor.WorkArea.Left) <= EDGE_TOLERANCE;

                case BSPTiling.FocusDirection.Right:
                    return Math.Abs(windowRect.Right - monitor.WorkArea.Right) <= EDGE_TOLERANCE;

                case BSPTiling.FocusDirection.Up:
                    return Math.Abs(windowRect.Top - monitor.WorkArea.Top) <= EDGE_TOLERANCE;

                case BSPTiling.FocusDirection.Down:
                    return Math.Abs(windowRect.Bottom - monitor.WorkArea.Bottom) <= EDGE_TOLERANCE;

                default:
                    return false;
            }
        }

        public bool IsWindowTiled(nint window)
        {
            if (isTilingInProgress) return false;

            foreach (var monitor in monitors)
            {
                if (monitor.IsWindowInCurrentWorkspace(window))
                {
                    if (IsWindowVisible(window))
                    {
                        return isTilingEnabled;
                    }
                }
            }
            return false;
        }
        private void MoveActiveWindowToWorkspace(int targetWorkspaceId, Monitor monitor)
        {
            nint activeWindow = GetForegroundWindow();

            if (activeWindow == nint.Zero)
            {
                return;
            }

            string executableName = GetExecutableNameFromWindow(activeWindow);
            if (pinnedApplicationsConfig.IsApplicationPinned(executableName))
            {
                return;
            }
            var currentWorkspace = monitor.GetCurrentWorkspace();
            var targetWorkspace = monitor.GetWorkspace(targetWorkspaceId);
            if (!currentWorkspace.ContainsWindow(activeWindow))
            {
                return;
            }

            if (targetWorkspaceId == monitor.CurrentWorkspaceId)
            {
                return;
            }

            SuspendBorder(activeWindow);

            monitor.MoveWindowToWorkspace(activeWindow, targetWorkspaceId);
			targetWorkspace.SetLastActiveWindow(activeWindow);
            ApplyTilingToWorkspace(currentWorkspace);

            SwitchToWorkspace(targetWorkspaceId, monitor.Index, activateBorderLastWindow: false);
            ApplyTilingToWorkspace(targetWorkspace);

            RefreshBorder(activeWindow);

            CleanupBackupWorkspaceIfEmpty(monitor, currentWorkspace.Id);
        }

        private void MoveWindowToAdjacentMonitor(nint window, Monitor sourceMonitor, BSPTiling.FocusDirection direction)
        {
            MonitorDirection monitorDirection;
            switch (direction)
            {
                case BSPTiling.FocusDirection.Left:
                    monitorDirection = MonitorDirection.Left;
                    break;
                case BSPTiling.FocusDirection.Right:
                    monitorDirection = MonitorDirection.Right;
                    break;
                case BSPTiling.FocusDirection.Up:
                    monitorDirection = MonitorDirection.Up;
                    break;
                case BSPTiling.FocusDirection.Down:
                    monitorDirection = MonitorDirection.Down;
                    break;
                default:
                    return;
            }

            var targetMonitor = FindMonitorInDirection(sourceMonitor, monitorDirection);

            if (targetMonitor == null)
            {
                return;
            }

            if (targetMonitor.Index == sourceMonitor.Index)
            {
                return;
            }

            try
            {
                SuspendBorder(window);

                var sourceWorkspace = sourceMonitor.FindWorkspaceContaining(window);
                sourceMonitor.RemoveWindowFromAllWorkspaces(window);
                targetMonitor.AddWindowToCurrentWorkspace(window);
                targetMonitor.GetCurrentWorkspace().SetLastActiveWindow(window);

                MoveWindowToMonitor(window, targetMonitor);

                globalActiveMonitorIndex = targetMonitor.Index;
                lastActiveMonitorIndex = targetMonitor.Index;
                ApplyTilingToCurrentWorkspace(sourceMonitor);
                ApplyTilingToCurrentWorkspace(targetMonitor);

                RefreshBorder(window);

                UpdateWorkspaceIndicator();
            }
            catch (Exception ex)
            {
                if (!sourceMonitor.IsWindowInCurrentWorkspace(window))
                {
                    sourceMonitor.AddWindowToCurrentWorkspace(window);
                }
            }
        }

        //TODO!!!: refactor this!
        public void UpdateAllWorkspaces(Monitor monitor)
        {
            if (monitor == null)
            {
                return;
            }

            for (int workspaceId = 8; workspaceId > 0; workspaceId--)
            {
                var currentWorkspace = monitor.GetCurrentWorkspace();

                if (autoTilingEnabled && currentWorkspace.WindowCount > 0)
                {
                    StoreMonitorWorkspaceWindows(monitor);
                }

                HideWorkspaceWindows(monitor, monitor.CurrentWorkspaceId);

                monitor.SwitchToWorkspace(workspaceId);

                var targetWorkspace = monitor.GetCurrentWorkspace();

                bool shouldActivateLastWindow = activeWindowFollowsMouse && targetWorkspace.WindowCount > 0;
                windowBorder.SetCurrentWorkspace(workspaceId, monitor.Handle, shouldActivateLastWindow);

                ShowWorkspaceWindows(monitor, workspaceId);

                if (targetWorkspace.WindowCount == 0)
                {
                    windowBorder.HideAllBorders();
                }
                else
                {
                    nint currentFocusedWindow = GetForegroundWindow();
                    bool hasFocusedWindow = currentFocusedWindow != nint.Zero &&
                                           targetWorkspace.ContainsWindow(currentFocusedWindow) &&
                                           IsWindowVisible(currentFocusedWindow);

                    if (!hasFocusedWindow)
                    {
                        var availableWindows = targetWorkspace.GetTileableWindows();
                        if (availableWindows.Count > 0)
                        {
                            FocusMostRecentWindow(monitor.Handle, availableWindows);
                        }
                    }
                }

            }

            UpdateWorkspaceIndicator();
        }
        
        public void SwitchToWorkspace(int workspaceId, int monitorIndex)
        {
            SwitchToWorkspace(workspaceId, monitorIndex, activateBorderLastWindow: true);
        }

        public void SwitchToWorkspace(int workspaceId, int monitorIndex, bool activateBorderLastWindow)
        {
            SwitchToWorkspace(workspaceId, monitorIndex, activateBorderLastWindow, autoFocusWindow: true);
        }

        public void SwitchToWorkspace(int workspaceId, int monitorIndex, bool activateBorderLastWindow, bool autoFocusWindow)
        {

            var monitor = GetMonitorByIndex(monitorIndex);
            if (monitor == null)
            {
                return;
            }

            if (workspaceId < 1 || workspaceId > 8)
            {
                return;
            }

            if (workspaceId == monitor.CurrentWorkspaceId)
            {
                return;
            }

            lastActiveMonitorIndex = monitorIndex;

            var currentWorkspace = monitor.GetCurrentWorkspace();
            var targetWorkspace = monitor.GetWorkspace(workspaceId);

            windowBorder.HideAllBorders();

            // for stacked mode, show target window FIRST to avoid desktop flash
            if (targetWorkspace.IsStackedMode && autoFocusWindow && targetWorkspace.WindowCount > 0)
            {
                var stackableWindows = targetWorkspace.GetStackableWindows();
                int currentIndex = targetWorkspace.GetCurrentStackedWindowIndex();
                if (currentIndex >= 0 && currentIndex < stackableWindows.Count)
                {
                    // show the target window before switching to prevent desktop flash
                    ShowWindow(stackableWindows[currentIndex], SW_RESTORE);
                }
            }

            monitor.SwitchToWorkspace(workspaceId);

            bool shouldActivateLastWindow = activateBorderLastWindow && activeWindowFollowsMouse && targetWorkspace.WindowCount > 0;
            windowBorder.SetCurrentWorkspace(workspaceId, monitor.Handle, shouldActivateLastWindow);

            if (targetWorkspace.WindowCount == 0)
            {
                windowBorder.HideAllBorders();
            }
            else if (autoFocusWindow)
            {
                if (targetWorkspace.IsStackedMode)
                {
                    ApplyStackedLayout(monitor, targetWorkspace);
                }
                else
                {
                    FocusLastActiveOrRootWindow(targetWorkspace);
                }
            }

            RefreshBorderForForeground();
            UpdateWorkspaceIndicator();
        }

        public void SwitchToStackedWindow(int stackedWindowIndex, int monitorIndex)
        {
            var monitor = GetMonitorByIndex(monitorIndex);
            if (monitor == null)
            {
                return;
            }

            var currentWorkspace = monitor.GetCurrentWorkspace();
            if (currentWorkspace == null || !currentWorkspace.IsStackedMode)
            {
                return;
            }

            currentWorkspace.SetCurrentStackedWindowIndex(stackedWindowIndex);

            ApplyTilingToCurrentWorkspace(monitor);

            UpdateWorkspaceIndicator();

            var stackableWindows = currentWorkspace.GetStackableWindows();
            if (stackedWindowIndex >= 0 && stackedWindowIndex < stackableWindows.Count)
            {
                var windowToFocus = stackableWindows[stackedWindowIndex];
                FocusWindow(windowToFocus);
            }
        }

        private void ResizeWindowForNewMonitor(nint window, Monitor sourceMonitor, Monitor targetMonitor)
        {
            try
            {
                if (!GetWindowRect(window, out RECT currentRect))
                    return;

                float relativeX = (float)(currentRect.Left - sourceMonitor.WorkArea.Left) / sourceMonitor.WorkArea.Width;
                float relativeY = (float)(currentRect.Top - sourceMonitor.WorkArea.Top) / sourceMonitor.WorkArea.Height;
                float relativeWidth = (float)currentRect.Width / sourceMonitor.WorkArea.Width;
                float relativeHeight = (float)currentRect.Height / sourceMonitor.WorkArea.Height;

                int newX = targetMonitor.WorkArea.Left + (int)(relativeX * targetMonitor.WorkArea.Width);
                int newY = targetMonitor.WorkArea.Top + (int)(relativeY * targetMonitor.WorkArea.Height);
                int newWidth = (int)(relativeWidth * targetMonitor.WorkArea.Width);
                int newHeight = (int)(relativeHeight * targetMonitor.WorkArea.Height);

                newWidth = Math.Max(newWidth, 300);
                newHeight = Math.Max(newHeight, 200);

                if (newX + newWidth > targetMonitor.WorkArea.Right)
                {
                    newX = targetMonitor.WorkArea.Right - newWidth;
                }
                if (newY + newHeight > targetMonitor.WorkArea.Bottom)
                {
                    newY = targetMonitor.WorkArea.Bottom - newHeight;
                }
                if (newX < targetMonitor.WorkArea.Left)
                {
                    newX = targetMonitor.WorkArea.Left;
                }
                if (newY < targetMonitor.WorkArea.Top)
                {
                    newY = targetMonitor.WorkArea.Top;
                }

                SuspendBorder(window);
                SetWindowPos(window, nint.Zero, newX, newY, newWidth, newHeight,
                    SWP_NOZORDER | SWP_NOACTIVATE);
                RefreshBorder(window);

                var newRect = new RECT
                {
                    Left = newX,
                    Top = newY,
                    Right = newX + newWidth,
                    Bottom = newY + newHeight
                };
                targetMonitor.TrackWindowPositions(window, newRect);

            }
            catch (Exception ex)
            {
                Logger.Error($"Error resizing window for new monitor: {ex.Message}");
            }
        }
        private void MoveWindowToMonitor(nint window, Monitor targetMonitor)
        {
            try
            {
                if (!GetWindowRect(window, out RECT currentRect))
                    return;

                int windowWidth = currentRect.Width;
                int windowHeight = currentRect.Height;

                int newX = targetMonitor.WorkArea.Left + (targetMonitor.WorkArea.Width - windowWidth) / 2;
                int newY = targetMonitor.WorkArea.Top + (targetMonitor.WorkArea.Height - windowHeight) / 2;

                if (newX + windowWidth > targetMonitor.WorkArea.Right)
                    newX = targetMonitor.WorkArea.Right - windowWidth;
                if (newY + windowHeight > targetMonitor.WorkArea.Bottom)
                    newY = targetMonitor.WorkArea.Bottom - windowHeight;

                if (newX < targetMonitor.WorkArea.Left)
                    newX = targetMonitor.WorkArea.Left;
                if (newY < targetMonitor.WorkArea.Top)
                    newY = targetMonitor.WorkArea.Top;

                SuspendBorder(window);
                SetWindowPos(window, nint.Zero, newX, newY, 0, 0,
                    SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
                RefreshBorder(window);
            }
            catch (Exception ex)
            {
                Logger.Info($"Error moving window to monitor: {ex.Message}");
            }
        }

        private void SwapAllWindowsBetweenWorkspaces(int targetWorkspaceId, Monitor monitor)
        {
            if (targetWorkspaceId < 1 || targetWorkspaceId > 8)
            {
                return;
            }

            int currentWorkspaceId = monitor.CurrentWorkspaceId;
            if (targetWorkspaceId == currentWorkspaceId)
            {
                return;
            }

            try
            {
                var currentWorkspace = monitor.GetWorkspace(currentWorkspaceId);
                var targetWorkspace = monitor.GetWorkspace(targetWorkspaceId);

                var currentWindows = new List<nint>(currentWorkspace.GetAllWindows());
                var targetWindows = new List<nint>(targetWorkspace.GetAllWindows());

                foreach (var window in currentWindows)
                {
                    SuspendBorder(window);
                }
                foreach (var window in targetWindows)
                {
                    SuspendBorder(window);
                }

                foreach (var window in currentWindows)
                {
                    currentWorkspace.RemoveWindow(window);
                }

                foreach (var window in targetWindows)
                {
                    targetWorkspace.RemoveWindow(window);
                }

                foreach (var window in targetWindows)
                {
                    currentWorkspace.AddWindow(window);
                }

                foreach (var window in currentWindows)
                {
                    targetWorkspace.AddWindow(window);
                }

                targetWorkspace.ShowAllWindows();
                currentWorkspace.HideAllWindows();

                bool shouldActivateLastWindow = activeWindowFollowsMouse && currentWorkspace.WindowCount > 0;
                windowBorder.SetCurrentWorkspace(currentWorkspaceId, monitor.Handle, shouldActivateLastWindow);

                if (currentWorkspace.WindowCount == 0)
                {
                    windowBorder.HideAllBorders();
                }
                else
                {
                    foreach (var window in targetWindows)
                    {
                        RefreshBorder(window);
                    }
                }

                UpdateWorkspaceIndicator();

            }
            catch (Exception ex)
            {
                Logger.Info($"Error during workspace swap: {ex.Message}");
            }
        }

        private void RemoveFocusedWindowFromTiling()
        {
            nint focusedWindow = GetForegroundWindow();

            if (focusedWindow == nint.Zero)
            {
                return;
            }

            if (excludedFromTilingWindows.Contains(focusedWindow))
            {
                excludedFromTilingWindows.Remove(focusedWindow);

                nint monitorHandle = MonitorFromWindow(focusedWindow, MONITOR_DEFAULTTONEAREST);
                var monitor = GetMonitorByHandle(monitorHandle);

                if (monitor != null)
                {
                    monitor.AddWindowToCurrentWorkspace(focusedWindow);
                    ApplyTilingToCurrentWorkspace(monitor);
                }
            }
            else
            {
                foreach (var monitor in monitors)
                {
                    var workspace = monitor.FindWorkspaceContaining(focusedWindow);
                    if (workspace != null)
                    {
                        int workspaceId = workspace.Id;
                        workspace.RemoveWindow(focusedWindow);
                        monitor.RemoveWindowTrackingPositions(focusedWindow);
                        excludedFromTilingWindows.Add(focusedWindow);

                        if (monitor.CurrentWorkspaceId == workspace.Id)
                        {
                            FocusMostRecentWindow(monitor.Handle, workspace.GetAllWindows());
                        }

                        ApplyTilingToCurrentWorkspace(monitor);

                        CleanupBackupWorkspaceIfEmpty(monitor, workspaceId);
                        break;
                    }
                }
            }
        }

        private void ToggleStackedMode()
        {
            var activeMonitor = GetActiveMonitor();
            if (activeMonitor == null)
            {
                return;
            }

            var currentWorkspace = activeMonitor.GetCurrentWorkspace();
            currentWorkspace.ToggleStackedMode();

            if (currentWorkspace.IsStackedMode)
            {
                ApplyStackedLayout(activeMonitor, currentWorkspace);
            }
            else
            {
                var stackableWindows = currentWorkspace.GetStackableWindows();
                foreach (var window in stackableWindows)
                {
                    ShowWindow(window, SW_RESTORE);
                }

                ApplyTilingToCurrentWorkspace(activeMonitor);
            }

            if (_backupWorkspacesPerMonitor.TryGetValue(activeMonitor.Index, out var backupWorkspaces))
            {
                activeMonitor.UpdateWorkspaceIndicator(backupWorkspaces);
            }
            else
            {
                activeMonitor.UpdateWorkspaceIndicator();
            }
        }

        private void TogglePausedMode()
        {
            var activeMonitor = GetActiveMonitor();
            if (activeMonitor == null)
            {
                return;
            }

            var currentWorkspace = activeMonitor.GetCurrentWorkspace();
            bool wasStackedMode = currentWorkspace.IsStackedMode;

            if (!currentWorkspace.IsPaused)
            {
                if (wasStackedMode)
                {
                    // tile the windows before entering the pause mode
                    currentWorkspace.DisableStackedMode();
                    ApplyTilingToCurrentWorkspace(activeMonitor);
                }
            }

            currentWorkspace.TogglePausedMode();

            if (!currentWorkspace.IsPaused)
            {
                // unpause - retile the windows
                ApplyTilingToCurrentWorkspace(activeMonitor);
            }

            UpdateWorkspaceIndicator();
        }

        private void ApplyStackedLayout(Monitor monitor, Workspace workspace)
        {
            var windows = workspace.GetStackableWindows();

            if (windows.Count == 0)
            {
                return;
            }

            int currentIndex = workspace.GetCurrentStackedWindowIndex();

            for (int i = 0; i < windows.Count; i++)
            {
                var title = GetWindowTitle(windows[i]);
                if (i == currentIndex)
                {
                    ShowWindow(windows[i], SW_RESTORE);
                    SuspendBorder(windows[i]);
                    var borderCfg = new WindowBorderConfiguration();
                    borderCfg.LoadConfiguration();
                    int bw = borderCfg.BorderWidth;

                    int desiredLeft = monitor.WorkArea.Left + bw;
                    int desiredTop = monitor.WorkArea.Top + bw;
                    int desiredRight = monitor.WorkArea.Right - bw;
                    int desiredBottom = monitor.WorkArea.Bottom - bw;

                    var corrected = GetCorrectedWindowRectForStacked(windows[i], desiredLeft, desiredTop, desiredRight, desiredBottom);

                    SetWindowPos(windows[i], nint.Zero,
                        corrected.Left,
                        corrected.Top,
                        Math.Max(0, corrected.Right - corrected.Left),
                        Math.Max(0, corrected.Bottom - corrected.Top),
                        SWP_NOZORDER | SWP_NOACTIVATE);
                    RefreshBorder(windows[i]);
                    SetForegroundWindow(windows[i]);
                    BringWindowToTop(windows[i]);
                }
                else
                {
                    var borderCfg = new WindowBorderConfiguration();
                    borderCfg.LoadConfiguration();
                    int bw = borderCfg.BorderWidth;

                    int desiredLeft = monitor.WorkArea.Left + bw;
                    int desiredTop = monitor.WorkArea.Top + bw;
                    int desiredRight = monitor.WorkArea.Right - bw;
                    int desiredBottom = monitor.WorkArea.Bottom - bw;

                    var corrected = GetCorrectedWindowRectForStacked(windows[i], desiredLeft, desiredTop, desiredRight, desiredBottom);

                    SetWindowPos(windows[i], nint.Zero,
                        corrected.Left,
                        corrected.Top,
                        Math.Max(0, corrected.Right - corrected.Left),
                        Math.Max(0, corrected.Bottom - corrected.Top),
                        SWP_NOZORDER | SWP_NOACTIVATE);
                }
            }
            UpdateWorkspaceIndicator();
        }



        private RECT GetCorrectedWindowRectForStacked(nint window, int desiredLeft, int desiredTop, int desiredRight, int desiredBottom)
        {
            RECT correction = CalculateFrameCorrectionForWindow(window);
            return new RECT
            {
                Left = desiredLeft - correction.Left,
                Top = desiredTop - correction.Top,
                Right = desiredRight + correction.Right,
                Bottom = desiredBottom + correction.Bottom
            };
        }

        private RECT CalculateFrameCorrectionForWindow(nint window)
        {
            RECT correction = new RECT { Left = 0, Top = 0, Right = 0, Bottom = 0 };
            try
            {
                if (GetWindowRect(window, out RECT winRect))
                {
                    if (DwmGetWindowAttribute(window, DWMWA_EXTENDED_FRAME_BOUNDS, out RECT dwmRect, System.Runtime.InteropServices.Marshal.SizeOf(typeof(RECT))) == 0)
                    {
                        correction.Left = dwmRect.Left - winRect.Left;
                        correction.Top = dwmRect.Top - winRect.Top;
                        correction.Right = winRect.Right - dwmRect.Right;
                        correction.Bottom = winRect.Bottom - dwmRect.Bottom;
                        return correction;
                    }

                    if (GetClientRect(window, out RECT clientRect))
                    {
                        POINT tl = new POINT { x = 0, y = 0 };
                        POINT br = new POINT { x = clientRect.Right, y = clientRect.Bottom };
                        ClientToScreen(window, ref tl);
                        ClientToScreen(window, ref br);

                        RECT actualClient = new RECT
                        {
                            Left = tl.x,
                            Top = tl.y,
                            Right = br.x,
                            Bottom = br.y
                        };

                        correction.Left = actualClient.Left - winRect.Left;
                        correction.Top = actualClient.Top - winRect.Top;
                        correction.Right = winRect.Right - actualClient.Right;
                        correction.Bottom = winRect.Bottom - actualClient.Bottom;
                    }
                }
            }
            catch { }
            return correction;
        }

        private void CycleStackedWindow()
        {
            var activeMonitor = GetActiveMonitor();
            if (activeMonitor == null)
            {
                return;
            }

            var currentWorkspace = activeMonitor.GetCurrentWorkspace();

            if (!currentWorkspace.IsStackedMode)
            {
                return;
            }

            if (currentWorkspace.WindowCount == 0)
            {
                return;
            }

            currentWorkspace.CycleStackedWindow();

            ApplyStackedLayout(activeMonitor, currentWorkspace);

            var windows = currentWorkspace.GetStackableWindows();
            int currentIndex = currentWorkspace.GetCurrentStackedWindowIndex();
        }

        private void CycleStackedWindow(int direction)
        {
            var activeMonitor = GetActiveMonitor();
            if (activeMonitor == null)
            {
                return;
            }

            var currentWorkspace = activeMonitor.GetCurrentWorkspace();

            if (!currentWorkspace.IsStackedMode)
            {
                return;
            }

            if (currentWorkspace.WindowCount == 0)
            {
                return;
            }

            currentWorkspace.CycleStackedWindow(direction);

            ApplyStackedLayout(activeMonitor, currentWorkspace);

            var windows = currentWorkspace.GetStackableWindows();
            int currentIndex = currentWorkspace.GetCurrentStackedWindowIndex();
        }

        private void JumpToStackedWindow(int windowIndex)
        {
            var activeMonitor = GetActiveMonitor();
            if (activeMonitor == null)
            {
                return;
            }

            var currentWorkspace = activeMonitor.GetCurrentWorkspace();

            if (!currentWorkspace.IsStackedMode)
            {
                return;
            }

            if (currentWorkspace.WindowCount == 0)
            {
                return;
            }

            var stackableWindows = currentWorkspace.GetStackableWindows();
            if (stackableWindows.Count == 0)
            {
                return;
            }

            // convert from 1-based to 0-based index
            int targetIndex = windowIndex - 1;

            if (targetIndex < 0 || targetIndex >= stackableWindows.Count)
            {
                return;
            }

            currentWorkspace.SetCurrentStackedWindowIndex(targetIndex);
            ApplyStackedLayout(activeMonitor, currentWorkspace);
        }

        private void MoveStackedWindowLeft()
        {
            var activeMonitor = GetActiveMonitor();
            if (activeMonitor == null)
            {
                return;
            }

            var currentWorkspace = activeMonitor.GetCurrentWorkspace();

            if (!currentWorkspace.IsStackedMode)
            {
                return;
            }

            if (currentWorkspace.WindowCount == 0)
            {
                return;
            }

            currentWorkspace.MoveStackedWindowLeft();
            ApplyStackedLayout(activeMonitor, currentWorkspace);
        }

        private void MoveStackedWindowRight()
        {
            var activeMonitor = GetActiveMonitor();
            if (activeMonitor == null)
            {
                return;
            }

            var currentWorkspace = activeMonitor.GetCurrentWorkspace();

            if (!currentWorkspace.IsStackedMode)
            {
                return;
            }

            if (currentWorkspace.WindowCount == 0)
            {
                return;
            }

            currentWorkspace.MoveStackedWindowRight();
            ApplyStackedLayout(activeMonitor, currentWorkspace);
        }
    }
}
