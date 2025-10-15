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
using System.Runtime.InteropServices;

namespace TilingWindowManager
{
    public partial class WindowManager 
    {

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        [DllImport("shcore.dll", SetLastError = true)]
        private static extern int GetDpiForMonitor(IntPtr hmonitor, DpiType dpiType, out uint dpiX, out uint dpiY);

        private enum DpiType
        {
            Effective = 0,
            Angular = 1,
            Raw = 2,
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }
        private void InitializeMonitors()
        {
            monitors = Monitor.GetAllMonitors();
        }

        private Monitor GetActiveMonitor()
        {

            if (GetCursorPos(out POINT cursorPos))
            {
                nint cursorMonitorHandle = MonitorFromPoint(cursorPos, MONITOR_DEFAULTTONEAREST);
                var cursorMonitor = GetMonitorByHandle(cursorMonitorHandle);
                if (cursorMonitor != null)
                {
                    globalActiveMonitorIndex = cursorMonitor.Index;
                    return cursorMonitor;
                }
            }

            var primaryMonitor = GetPrimaryMonitor();
            if (primaryMonitor != null)
            {
                globalActiveMonitorIndex = primaryMonitor.Index;
            }
            return primaryMonitor;
        }
        private void StoreMonitorWorkspaceWindows(Monitor monitor)
        {
            var currentWorkspace = monitor.GetCurrentWorkspace();

            var existingWindows = currentWorkspace.GetAllWindows();
            var newWindowList = new List<nint>();

            foreach (var existingWindow in existingWindows)
            {
                if (windowMonitor.IsValidApplicationWindow(existingWindow))
                {
                    nint windowMonitor = MonitorFromWindow(existingWindow, MONITOR_DEFAULTTONEAREST);
                    if (windowMonitor == monitor.Handle)
                    {
                        newWindowList.Add(existingWindow);

                        if (GetWindowRect(existingWindow, out RECT rect))
                        {
                            monitor.TrackWindowPositions(existingWindow, rect);
                        }
                    }
                }
            }

            currentEnumCallback = (hWnd, lParam) =>
            {
                if (windowMonitor.IsValidApplicationWindow(hWnd) && !newWindowList.Contains(hWnd))
                {
                    nint windowMonitor = MonitorFromWindow(hWnd, MONITOR_DEFAULTTONEAREST);
                    if (windowMonitor == monitor.Handle)
                    {
                        bool isTrackedElsewhere = false;
                        foreach (var m in monitors)
                        {
                            foreach (var ws in m.GetAllWorkspaces())
                            {
                                if (ws != currentWorkspace && ws.ContainsWindow(hWnd))
                                {
                                    isTrackedElsewhere = true;
                                    break;
                                }
                            }
                            if (isTrackedElsewhere) break;
                        }

                        if (!isTrackedElsewhere)
                        {
                            newWindowList.Add(hWnd);

                            if (GetWindowRect(hWnd, out RECT rect))
                            {
                                monitor.TrackWindowPositions(hWnd, rect);
                            }
                        }
                    }
                }
                return true;
            };
            EnumWindows(enumWindowsProc, nint.Zero);

            currentWorkspace.ClearWindows();
            foreach (var window in newWindowList)
            {
                currentWorkspace.AddWindow(window);
            }

        }

        private Monitor GetMonitorByHandle(nint handle)
        {
            return monitors.FirstOrDefault(m => m.Handle == handle);
        }

        private Monitor GetMonitorByIndex(int index)
        {
            return monitors.FirstOrDefault(m => m.Index == index);
        }

        private Monitor GetPrimaryMonitor()
        {
            return monitors.FirstOrDefault(m => m.IsPrimary) ?? monitors.FirstOrDefault();
        }

        private Monitor GetMonitorForWindow(nint window)
        {
            nint monitorHandle = MonitorFromWindow(window, MONITOR_DEFAULTTONEAREST);
            return GetMonitorByHandle(monitorHandle);
        }
        private void SwitchToMonitorInDirection(MonitorDirection direction)
        {
            var currentMonitor = GetMonitorByIndex(globalActiveMonitorIndex) ?? GetActiveMonitor();
            var targetMonitor = FindMonitorInDirection(currentMonitor, direction);

            if (targetMonitor != null && targetMonitor.Index != currentMonitor.Index)
            {
                EnsureMonitorIsActive(targetMonitor);

                SwitchToMonitor(targetMonitor);

            }
        }
        private void EnsureMonitorIsActive(Monitor monitor)
        {
            int centerX = monitor.WorkArea.Left + monitor.WorkArea.Width / 2;
            int centerY = monitor.WorkArea.Top + monitor.WorkArea.Height / 2;

            SetCursorPos(centerX, centerY);

            globalActiveMonitorIndex = monitor.Index;
        }

        private void SwitchToWorkspaceOnOtherMonitor(int targetWorkspaceId, Monitor sourceMonitor)
        {
            Monitor targetMonitor = null;
            foreach (var monitor in monitors)
            {
                if (monitor.Index != sourceMonitor.Index)
                {
                    targetMonitor = monitor;
                    break;
                }
            }

            if (targetMonitor == null)
            {
                return;
            }

            if (targetWorkspaceId == targetMonitor.CurrentWorkspaceId && GetActiveMonitor() == targetMonitor)
            {
                return;
            }

            SwitchToMonitor(targetMonitor);
            SwitchToWorkspace(targetWorkspaceId, targetMonitor.Index);
        }

        private Monitor FindMonitorInDirection(Monitor currentMonitor, MonitorDirection direction)
        {
            if (currentMonitor == null) return null;

            var currentCenter = new POINT
            {
                x = currentMonitor.Bounds.Left + currentMonitor.Bounds.Width / 2,
                y = currentMonitor.Bounds.Top + currentMonitor.Bounds.Height / 2
            };

            Monitor bestMatch = null;
            double bestDistance = double.MaxValue;

            foreach (var monitor in monitors)
            {
                if (monitor.Index == currentMonitor.Index) continue; 

                var monitorCenter = new POINT
                {
                    x = monitor.Bounds.Left + monitor.Bounds.Width / 2,
                    y = monitor.Bounds.Top + monitor.Bounds.Height / 2
                };

                bool isInCorrectDirection = false;
                double distance = 0;

                switch (direction)
                {
                    case MonitorDirection.Left:
                        isInCorrectDirection = monitorCenter.x < currentCenter.x;
                        if (isInCorrectDirection)
                        {
                            double horizontalDistance = currentCenter.x - monitorCenter.x;
                            double verticalDistance = Math.Abs(currentCenter.y - monitorCenter.y);
                        }
                        break;

                    case MonitorDirection.Right:
                        isInCorrectDirection = monitorCenter.x > currentCenter.x;
                        if (isInCorrectDirection)
                        {
                            double horizontalDistance = monitorCenter.x - currentCenter.x;
                            double verticalDistance = Math.Abs(currentCenter.y - monitorCenter.y);
                            distance = horizontalDistance + verticalDistance * 0.1;
                        }
                        break;

                    case MonitorDirection.Up:
                        isInCorrectDirection = monitorCenter.y < currentCenter.y;
                        if (isInCorrectDirection)
                        {
                            double verticalDistance = currentCenter.y - monitorCenter.y;
                            double horizontalDistance = Math.Abs(currentCenter.x - monitorCenter.x);
                            distance = verticalDistance + horizontalDistance * 0.1;
                        }
                        break;

                    case MonitorDirection.Down:
                        isInCorrectDirection = monitorCenter.y > currentCenter.y;
                        if (isInCorrectDirection)
                        {
                            double verticalDistance = monitorCenter.y - currentCenter.y;
                            double horizontalDistance = Math.Abs(currentCenter.x - monitorCenter.x);
                            distance = verticalDistance + horizontalDistance * 0.1;
                        }
                        break;
                }

                if (isInCorrectDirection && distance < bestDistance)
                {
                    bestMatch = monitor;
                    bestDistance = distance;
                }
            }

            return bestMatch;
        }


        private void SwitchToMonitor(Monitor targetMonitor)
        {
            globalActiveMonitorIndex = targetMonitor.Index;
            lastActiveMonitorIndex = targetMonitor.Index;

            int centerX = targetMonitor.WorkArea.Left + targetMonitor.WorkArea.Width / 2;
            int centerY = targetMonitor.WorkArea.Top + targetMonitor.WorkArea.Height / 2;

            SetCursorPos(centerX, centerY);

            var currentWorkspace = targetMonitor.GetCurrentWorkspace();
            if (currentWorkspace.WindowCount > 0)
            {
                nint lastActiveWindow = nint.Zero;
                var windows = currentWorkspace.GetAllWindows();

                for (int i = windows.Count - 1; i >= 0; i--)
                {
                    var window = windows[i];
                    if (IsWindowVisible(window))
                    {
                        lastActiveWindow = window;
                        break;
                    }
                }

                if (lastActiveWindow != nint.Zero)
                {
                    try
                    {
                        SetForegroundWindow(lastActiveWindow);
                        BringWindowToTop(lastActiveWindow);
                    }
                    catch
                    {
                    }
                }
            }
        }
    }
}
