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
        private void HideWorkspaceWindows(Monitor monitor, int workspaceId)
        {
            var workspace = monitor.GetWorkspace(workspaceId);
            workspace.HideAllWindows();
        }

        private void ShowWorkspaceWindows(Monitor monitor, int workspaceId)
        {
            var workspace = monitor.GetWorkspace(workspaceId);
            workspace.ShowAllWindows();
        }
        
        private void UpdateWorkspaceIndicator()
        {
            foreach (var monitor in monitors)
            {
                // preserve backup workspace info when updating
                if (_backupWorkspacesPerMonitor.TryGetValue(monitor.Index, out var backupWorkspaces))
                {
                    monitor.UpdateWorkspaceIndicator(backupWorkspaces);
                }
                else
                {
                    monitor.UpdateWorkspaceIndicator();
                }
            }
        }
        private void ApplyTilingToWorkspace(Workspace workspace)
        {
            var windows = workspace.GetTileableWindows();

            if (windows.Count > 0)
            {
                Monitor? workspaceMonitor = null;
                foreach (var monitor in monitors)
                {
                    if (monitor.GetWorkspace(workspace.Id).Equals(workspace))
                    {
                        workspaceMonitor = monitor;
                        break;
                    }
                }

                if (workspaceMonitor != null)
                {
                    if (workspace.IsStackedMode)
                    {
                        // Focus the newest window when in stacked mode
                        workspace.FocusNewestWindowInStack();
                        ApplyStackedLayout(workspaceMonitor, workspace);
                    }
                    else
                    {
                        workspace.ApplyTiling();
                    }
                }
                else
                {
                    // Fallback if monitor not found
                    workspace.ApplyTiling();
                }
            }

            UpdateWorkspaceIndicator();
        }


        private void ApplyTilingToCurrentWorkspace(Monitor monitor)
        {
            var currentWorkspace = monitor.GetCurrentWorkspace();
            var validWindows = currentWorkspace.GetTileableWindows();

            if (validWindows.Count > 0)
            {
                SuspendBorderForForeground();

                if (currentWorkspace.IsStackedMode)
                {
                    ApplyStackedLayout(monitor, currentWorkspace);
                }
                else
                {
                    currentWorkspace.ApplyTiling();

                    foreach (var w in validWindows)
                    {
                        if (GetWindowRect(w, out RECT rect))
                        {
                            monitor.TrackWindowPositions(w, rect);
                        }
                    }
                }

                RefreshBorderForForeground();
            }
            else
            {
                if (windowBorder != null && windowBorder.IsEnabled)
                {
                    windowBorder.HideAllBorders();
                }
            }

            UpdateWorkspaceIndicator();
        }

        private void ApplyTilingToAllMonitorWorkspaces(Monitor monitor)
        {
            foreach(var workspace in monitor.GetAllWorkspaces())
            {
                var validWindows = workspace.GetTileableWindows();

                if (validWindows.Count > 0)
                {

                    SuspendBorderForForeground();

                    if (workspace.IsStackedMode)
                    {
                        ApplyStackedLayout(monitor, workspace);
                    }
                    else
                    {
                        workspace.ApplyTiling();

                        foreach (var w in validWindows)
                        {
                            if (GetWindowRect(w, out RECT rect))
                            {
                                monitor.TrackWindowPositions(w, rect);
                            }
                        }
                    }

                    RefreshBorderForForeground();
                }
                else if (monitor.GetCurrentWorkspace() == workspace)
                {
                    if (windowBorder != null && windowBorder.IsEnabled)
                    {
                        windowBorder.HideAllBorders();
                    }
                }
                UpdateWorkspaceIndicator();
            }
        }
    }
}
