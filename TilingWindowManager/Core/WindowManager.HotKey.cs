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
        private HotKeyConfiguration? _hotkeyConfig;

        private void InitializeHotkeyConfig(HotKeyConfiguration config)
        {
            _hotkeyConfig = config;
        }

        private void HandleHotkey(HotKey hotKey, int hotkeyId)
        {
            if (_hotkeyConfig == null) return;

            var hotkeyEntry = _hotkeyConfig.GetHotKeyById((uint)hotkeyId);
            if (hotkeyEntry == null)
            {
                var appHotkeyEntry = hotKey.ApplicationHotkeysConfiguration.GetHotkeyById((uint)hotkeyId);
                if (appHotkeyEntry != null)
                {
                    SwitchToApplicationWorkspace(appHotkeyEntry.ExecutableName);
                }
                return;
            }

            var activeMonitor = GetActiveMonitor();

            switch (hotkeyEntry.Action)
            {
                case "switch_to_workspace":
                    if (activeMonitor != null)
                    {
                        int targetWorkspaceId = ExtractWorkspaceNumberFromHotkey(hotkeyEntry);
                        if (targetWorkspaceId >= 1 && targetWorkspaceId <= 8)
                        {
                            nint focusedWindow = GetForegroundWindow();
                            Monitor targetMonitor = activeMonitor;

                            if (focusedWindow != nint.Zero)
                            {
                                foreach (var monitor in monitors)
                                {
                                    var workspace = monitor.FindWorkspaceContaining(focusedWindow);
                                    if (workspace != null)
                                    {
                                        targetMonitor = monitor;
                                        break;
                                    }
                                }
                            }

                            lastActiveMonitorIndex = targetMonitor.Index;
                            SwitchToWorkspace(targetWorkspaceId, targetMonitor.Index);
                        }
                    }
                    break;

                case "refresh_active_monitor":
                    if (activeMonitor != null)
                    {
                        if (isTilingEnabled)
                        {
                            ApplyTilingToCurrentWorkspace(activeMonitor);
                        }
                    }
                    break;

                case "increase_split_ratio":
                    if (isTilingEnabled)
                    {
                        AdjustSplitRatio(0.1f);
                    }
                    break;

                case "decrease_split_ratio":
                    if (isTilingEnabled)
                    {
                        AdjustSplitRatio(-0.1f);
                    }
                    break;

                case "focus_left":
                    SwitchFocus(BSPTiling.FocusDirection.Left);
                    break;

                case "focus_right":
                    SwitchFocus(BSPTiling.FocusDirection.Right);
                    break;

                case "focus_down":
                    SwitchFocus(BSPTiling.FocusDirection.Down);
                    break;

                case "focus_up":
                    SwitchFocus(BSPTiling.FocusDirection.Up);
                    break;

                case "move_to_workspace":
                    if (activeMonitor != null)
                    {
                        int targetWorkspaceId = ExtractWorkspaceNumberFromHotkey(hotkeyEntry);
                        if (targetWorkspaceId > 0)
                        {
                            MoveActiveWindowToWorkspace(targetWorkspaceId, activeMonitor);
                        }
                    }
                    break;

                case "swap_left":
                    if (activeMonitor != null && activeMonitor.GetCurrentWorkspace().IsStackedMode)
                    {
                        MoveStackedWindowLeft();
                    }
                    else
                    {
                        SwapActiveWindowInDirection(BSPTiling.FocusDirection.Left);
                    }
                    break;

                case "swap_right":
                    if (activeMonitor != null && activeMonitor.GetCurrentWorkspace().IsStackedMode)
                    {
                        MoveStackedWindowRight();
                    }
                    else
                    {
                        SwapActiveWindowInDirection(BSPTiling.FocusDirection.Right);
                    }
                    break;

                case "swap_down":
                    if (activeMonitor != null && activeMonitor.GetCurrentWorkspace().IsStackedMode)
                    {
                        MoveStackedWindowRight();
                    }
                    else
                    {
                        SwapActiveWindowInDirection(BSPTiling.FocusDirection.Down);
                    }
                    break;

                case "swap_up":
                    if (activeMonitor != null && activeMonitor.GetCurrentWorkspace().IsStackedMode)
                    {
                        MoveStackedWindowLeft();
                    }
                    else
                    {
                        SwapActiveWindowInDirection(BSPTiling.FocusDirection.Up);
                    }
                    break;

                case "switch_monitor_left":
                    SwitchToMonitorInDirection(MonitorDirection.Left);
                    break;

                case "switch_monitor_right":
                    SwitchToMonitorInDirection(MonitorDirection.Right);
                    break;

                case "switch_monitor_down":
                    SwitchToMonitorInDirection(MonitorDirection.Down);
                    break;

                case "switch_monitor_up":
                    SwitchToMonitorInDirection(MonitorDirection.Up);
                    break;

                case "swap_workspace":
                    if (activeMonitor != null)
                    {
                        int targetWorkspaceId = ExtractWorkspaceNumberFromHotkey(hotkeyEntry);
                        if (targetWorkspaceId > 0)
                        {
                            SwapAllWindowsBetweenWorkspaces(targetWorkspaceId, activeMonitor);
                        }
                    }
                    break;

                case "switch_other_monitor_workspace":
                    if (activeMonitor != null)
                    {
                        int targetWorkspaceId = ExtractWorkspaceNumberFromHotkey(hotkeyEntry);
                        if (targetWorkspaceId > 0)
                        {
                            SwitchToWorkspaceOnOtherMonitor(targetWorkspaceId, activeMonitor);
                        }
                    }
                    break;

                case "move_to_other_monitor_workspace":
                    if (activeMonitor != null)
                    {
                        int targetWorkspaceId = ExtractWorkspaceNumberFromHotkey(hotkeyEntry);
                        if (targetWorkspaceId > 0)
                        {
                            MoveActiveWindowToWorkspaceOnOtherMonitor(targetWorkspaceId, activeMonitor);
                        }
                    }
                    break;

                case "remove_from_tiling":
                    RemoveFocusedWindowFromTiling();
                    break;

                case "toggle_stacked_mode":
                    ToggleStackedMode();
                    break;

                case "cycle_stacked_window":
                    CycleStackedWindow();
                    break;

                case "move_stacked_window_left":
                    MoveStackedWindowLeft();
                    break;

                case "move_stacked_window_right":
                    MoveStackedWindowRight();
                    break;

                case "jump_to_stacked_window":
                    int windowIndex = ExtractWorkspaceNumberFromHotkey(hotkeyEntry);
                    if (windowIndex >= 1 && windowIndex <= 9)
                    {
                        JumpToStackedWindow(windowIndex);
                    }
                    break;

                case "toggle_paused_mode":
                    TogglePausedMode();
                    break;

                case "exit_application":
                    Logger.Info("Exit hotkey pressed - cleaning up and terminating...");
                    Cleanup();
                    Environment.Exit(0);
                    break;

                case "open_app_switcher":
                    if (activeMonitor != null)
                    {
                        var allWindows = CollectAllWindows();
                        appSwitcher.Show(allWindows, activeMonitor);
                    }
                    break;

                case "reload_allowed_owned_windows":
                    ReloadAllowedOwnedWindows();
                    break;

                case "close_window":
                    CloseActiveWindow();
                    break;

                case "reload_configuration":
                    ReloadConfiguration(hotKey);
                    break;

                case "switch_to_free_workspace":
                    if (activeMonitor != null)
                    {
                        SwitchToFirstFreeWorkspace(activeMonitor);
                    }
                    break;

                case "switch_to_free_workspace_other_monitor":
                    if (activeMonitor != null)
                    {
                        var primary = GetPrimaryMonitor();
                        Monitor target = null;

                        if (activeMonitor.IsPrimary)
                        {
                            foreach (var m in monitors)
                            {
                                if (!m.IsPrimary)
                                {
                                    target = m;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            target = primary;
                        }

                        if (target != null)
                        {
                            var workspaces = target.GetAllWorkspaces();
                            int chosenId = 0;
                            foreach (var ws in workspaces)
                            {
                                if (ws.WindowCount == 0)
                                {
                                    chosenId = ws.Id;
                                    break;
                                }
                            }

                            if (chosenId == 0)
                            {
                                chosenId = 1; // fallback to workspace 1 if none empty
                            }

                            lastActiveMonitorIndex = target.Index;
                            SwitchToMonitor(target);
                            SwitchToWorkspace(chosenId, target.Index);
                            EnsureMonitorIsActive(target);
                        }
                    }
                    break;

                case "move_to_free_workspace":
                    if (activeMonitor != null)
                    {
                        MoveActiveWindowToFirstFreeWorkspace(activeMonitor);
                    }
                    break;

                case "move_to_free_workspace_other_monitor":
                    if (activeMonitor != null)
                    {
                        var primary = GetPrimaryMonitor();
                        Monitor target = null;

                        if (activeMonitor.IsPrimary)
                        {
                            foreach (var m in monitors)
                            {
                                if (!m.IsPrimary)
                                {
                                    target = m;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            target = primary;
                        }

                        if (target != null)
                        {
                            var workspaces = target.GetAllWorkspaces();
                            int chosenId = 0;
                            foreach (var ws in workspaces)
                            {
                                if (ws.WindowCount == 0)
                                {
                                    chosenId = ws.Id;
                                    break;
                                }
                            }

                            if (chosenId == 0)
                            {
                                chosenId = 1; // fallback to workspace 1 if none empty
                            }

                            MoveActiveWindowToWorkspaceOnOtherMonitor(chosenId, activeMonitor);
                            EnsureMonitorIsActive(target);
                        }
                    }
                    break;

                default:
                    Logger.Error($"Unknown action: {hotkeyEntry.Action}");
                    break;
            }
        }

        private int ExtractWorkspaceNumberFromHotkey(HotKeyEntry hotkeyEntry)
        {

            // extract from name ("move_to_workspace_3" -> 3)
            if (hotkeyEntry.Name.Contains("_"))
            {
                var parts = hotkeyEntry.Name.Split('_');
                var lastPart = parts[parts.Length - 1];
                if (int.TryParse(lastPart, out int workspaceFromName))
                {
                    return workspaceFromName;
                }
            }

            // extract from key combination ("ALT+SHIFT+3" -> 3)
            var keyParts = hotkeyEntry.KeyCombination.Split('+');
            var lastKey = keyParts[keyParts.Length - 1];
            if (int.TryParse(lastKey, out int workspaceFromKey))
            {
                return workspaceFromKey;
            }

            // extract from the individual key property
            if (int.TryParse(hotkeyEntry.Key, out int workspaceFromKeyProp))
            {
                return workspaceFromKeyProp;
            }

            Logger.Error($"Could not extract workspace number from hotkey: {hotkeyEntry.Name} ({hotkeyEntry.KeyCombination})");
            return 0;
        }

        private void ShowAllWindowsInAllMonitors()
        {
            foreach (var monitor in monitors)
            {
                ShowAllWindowsInMonitor(monitor);
            }
        }
        private void ShowAllWindowsInMonitor(Monitor monitor)
        {
            var allWindows = new List<nint>();
            foreach (var workspace in monitor.GetAllWorkspaces())
            {
                allWindows.AddRange(workspace.GetAllWindows());
            }

            currentEnumCallback = (hWnd, lParam) =>
            {
                if (IsValidApplicationWindow(hWnd) && !allWindows.Contains(hWnd))
                {
                    nint windowMonitor = MonitorFromWindow(hWnd, MONITOR_DEFAULTTONEAREST);
                    if (windowMonitor == monitor.Handle)
                    {
                        allWindows.Add(hWnd);
                    }
                }
                return true;
            };
            EnumWindows(enumWindowsProc, nint.Zero);


            foreach (var window in allWindows)
            {
                ShowWindow(window, SW_RESTORE);
            }

            var currentWorkspace = monitor.GetCurrentWorkspace();
            currentWorkspace.ClearWindows();
            foreach (var window in allWindows)
            {
                currentWorkspace.AddWindow(window);
            }

            foreach (var workspace in monitor.GetAllWorkspaces())
            {
                if (workspace.Id != monitor.CurrentWorkspaceId)
                {
                    workspace.ClearWindows();
                }
            }

            UpdateWorkspaceIndicator();

            if (isTilingEnabled && autoTilingEnabled)
            {
                ApplyTilingToCurrentWorkspace(monitor);
            }
        }

        private void ReloadAllowedOwnedWindows()
        {
            windowMonitor?.ReloadAllowedOwnedWindowsConfiguration();
        }

        private void CloseActiveWindow()
        {
            nint activeWindow = GetForegroundWindow();
            if (activeWindow == nint.Zero)
            {
                return;
            }

            PostMessage(activeWindow, WM_CLOSE, nint.Zero, nint.Zero);
        }

        private void SwitchToFirstFreeWorkspace(Monitor activeMonitor)
        {
            var workspaces = activeMonitor.GetAllWorkspaces();

            foreach (var workspace in workspaces)
            {
                if (workspace.WindowCount == 0)
                {
                    lastActiveMonitorIndex = activeMonitor.Index;
                    SwitchToWorkspace(workspace.Id, activeMonitor.Index);
                    return;
                }
            }
        }

        private void MoveActiveWindowToFirstFreeWorkspace(Monitor activeMonitor)
        {
            nint activeWindow = GetForegroundWindow();
            if (activeWindow == nint.Zero)
            {
                return;
            }

            var workspaces = activeMonitor.GetAllWorkspaces();

            foreach (var workspace in workspaces)
            {
                if (workspace.WindowCount == 0)
                {
                    MoveActiveWindowToWorkspace(workspace.Id, activeMonitor);
                    return;
                }
            }
        }
    }
}
