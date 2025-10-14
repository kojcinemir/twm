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
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace TilingWindowManager
{
    public class Monitor
    {
        public const int NO_OF_WORKSPACES = 8;
        [DllImport("user32.dll")]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref WindowManager.RECT lprcMonitor, IntPtr dwData);

        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public int Width => Right - Left;
            public int Height => Bottom - Top;

            public static implicit operator RECT(WindowManager.RECT rect)
            {
                return new RECT { Left = rect.Left, Top = rect.Top, Right = rect.Right, Bottom = rect.Bottom };
            }

            public static implicit operator WindowManager.RECT(RECT rect)
            {
                return new WindowManager.RECT { Left = rect.Left, Top = rect.Top, Right = rect.Right, Bottom = rect.Bottom };
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct MONITORINFO
        {
            public uint cbSize;
            public WindowManager.RECT rcMonitor;
            public WindowManager.RECT rcWork;
            public uint dwFlags;
        }

        public IntPtr Handle { get; set; }
        public RECT Bounds { get; set; }
        public RECT WorkArea { get; set; }
        public bool IsPrimary { get; set; }
        public int Index { get; set; }

        private Workspaces workspaces;

        public int CurrentWorkspaceId => workspaces.CurrentWorkspaceId;

        private Dictionary<nint, WindowManager.RECT> windowPositions = new Dictionary<nint, WindowManager.RECT>();
        private static WorkspaceIndicator? sharedWorkspaceIndicator;

        public Monitor(IntPtr handle, RECT bounds, RECT workArea, bool isPrimary, int index)
        {
            Handle = handle;
            Bounds = bounds;
            WorkArea = workArea;
            IsPrimary = isPrimary;
            Index = index;

            workspaces = new Workspaces(index);
            InitializeAllWorkspaces();
        }

        private void InitializeAllWorkspaces()
        {
            var workspaceConfig = new WorkspaceConfiguration();
            workspaceConfig.LoadConfiguration();

            foreach (var workspace in workspaces.GetAllWorkspaces())
            {
                workspace.InitializeTiling(WorkArea);

                if (workspaceConfig.StackedOnStartup)
                {
                    workspace.EnableStackedMode();
                }

                if (workspaceConfig.PausedOnStartup && workspace.Id == 1)
                {
                    workspace.EnablePausedMode();
                }
            }
        }

        public static List<Monitor> GetAllMonitors()
        {
            var monitors = new List<Monitor>();
            int index = 0;

            MonitorEnumProc callback = (IntPtr hMonitor, IntPtr hdcMonitor, ref WindowManager.RECT lprcMonitor, IntPtr dwData) =>
            {
                MONITORINFO mi = new MONITORINFO();
                mi.cbSize = (uint)Marshal.SizeOf(mi);

                if (GetMonitorInfo(hMonitor, ref mi))
                {
                    bool isPrimary = (mi.dwFlags & 0x00000001) != 0; //if it is primary monitor?
                    var monitor = new Monitor(hMonitor, mi.rcMonitor, mi.rcWork, isPrimary, index++);
                    monitors.Add(monitor);
                }
                return true;
            };

            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, callback, IntPtr.Zero);
            return monitors;
        }

        public Workspaces GetWorkspaces()
        {
            return workspaces;
        }

        public Workspace GetCurrentWorkspace()
        {
            return workspaces.GetCurrentWorkspace();
        }

        public Workspace GetWorkspace(int workspaceId)
        {
            return workspaces.GetWorkspace(workspaceId);
        }

        public List<Workspace> GetAllWorkspaces()
        {
            return workspaces.GetAllWorkspaces();
        }
        public Workspace? GetWorkspaceAtIndex(int index)
        {
            if(index < 1 || index > NO_OF_WORKSPACES)
            {
                return null;
            }
            foreach (var workspace in workspaces.GetAllWorkspaces())
            {
                if (workspace.Id == index)
                {
                    return workspace;
                }
            }
            return null;
        }

        public bool SwitchToWorkspace(int workspaceId)
        {
            return workspaces.SwitchToWorkspace(workspaceId);
        }

        public void AddWindowToCurrentWorkspace(nint window)
        {
            workspaces.AddWindowToCurrentWorkspace(window);
        }

        public void AddWindowToWorkspace(nint window, int workspaceId)
        {
            workspaces.MoveWindowToWorkspace(window, workspaceId);
        }

        public void RemoveWindowFromAllWorkspaces(nint window)
        {
            workspaces.RemoveWindowFromAllWorkspaces(window);
        }

        public void MoveWindowToWorkspace(nint window, int targetWorkspaceId)
        {
            workspaces.MoveWindowToWorkspace(window, targetWorkspaceId);
        }

        public bool IsWindowInCurrentWorkspace(nint window)
        {
            return workspaces.IsWindowInCurrentWorkspace(window);
        }

        public Workspace FindWorkspaceContaining(nint window)
        {
            return workspaces.FindWorkspaceContaining(window);
        }

        public void TrackWindowPositions(nint window, RECT position)
        {
            windowPositions[window] = position; 
        }

        public RECT? GetWindowPosition(nint window)
        {
            return windowPositions.TryGetValue(window, out var pos) ? (RECT?)pos : null;
        }


        public void RemoveWindowTrackingPositions(nint window)
        {
            windowPositions.Remove(window);
        }

        public static void InitializeWorkspaceIndicator(List<Monitor> monitors, Action<int, int> onWorkspaceClicked)
        {
            if (sharedWorkspaceIndicator != null)
            {
                sharedWorkspaceIndicator.Cleanup();
                sharedWorkspaceIndicator = null;
            }

            sharedWorkspaceIndicator = new WorkspaceIndicator();
            sharedWorkspaceIndicator.WorkspaceClicked += onWorkspaceClicked;
            sharedWorkspaceIndicator.Initialize(monitors);
        }

        public void UpdateWorkspaceIndicator()
        {
            sharedWorkspaceIndicator?.UpdateMonitor(Index, CurrentWorkspaceId, GetAllWorkspaces());
        }

        public void UpdateWorkspaceIndicator(HashSet<int> backupWorkspaces)
        {
            sharedWorkspaceIndicator?.UpdateMonitor(Index, CurrentWorkspaceId, GetAllWorkspaces(), backupWorkspaces);
        }

        public static void UpdateWorkspaceIndicatorMonitors(List<Monitor> monitors)
        {
            sharedWorkspaceIndicator?.UpdateMonitors(monitors);
        }

        public static void CleanupWorkspaceIndicator()
        {
            sharedWorkspaceIndicator?.Cleanup();
            sharedWorkspaceIndicator = null;
        }

        public override string ToString()
        {
            return $"Monitor {Index} ({(IsPrimary ? "Primary" : "Secondary")}) - {Bounds.Width}x{Bounds.Height} at ({Bounds.Left},{Bounds.Top})";
        }
    }
}