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
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace TilingWindowManager
{
    public class WindowEventArgs : EventArgs
    {
        public nint WindowHandle { get; set; }
        public string WindowTitle { get; set; } = "";
        public string ProcessName { get; set; } = "";
        public uint ProcessId { get; set; }
        public DateTime Timestamp { get; set; }

        public override string ToString()
        {
            return $"[{Timestamp:HH:mm:ss}] {ProcessName} - {WindowTitle} (PID: {ProcessId}, Handle: 0x{WindowHandle.ToInt64():X})";
        }
    }
    public partial class WindowMonitor : IDisposable
    {

        [DllImport("user32.dll")]
        private static extern nint SetWinEventHook(uint eventMin, uint eventMax, nint hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool UnhookWinEvent(nint hWinEventHook);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetWindowText(nint hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern int GetWindowTextLength(nint hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(nint hWnd);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(nint hWnd, out uint processId);

        [DllImport("user32.dll")]
        private static extern nint GetWindow(nint hWnd, uint uCmd);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(nint hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLong", SetLastError = true)]
        private static extern IntPtr GetWindowLong32(IntPtr hWnd, int nIndex);

        private static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
        {
            return IntPtr.Size == 8 ? GetWindowLongPtr64(hWnd, nIndex) : GetWindowLong32(hWnd, nIndex);
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out int pvAttribute, int cbAttribute);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        private delegate void WinEventDelegate(nint hWinEventHook, uint eventType, nint hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int Left, Top, Right, Bottom; }

        // windows api
        private const uint EVENT_OBJECT_CREATE = 0x8000;
        private const uint EVENT_OBJECT_DESTROY = 0x8001;
        private const uint EVENT_OBJECT_SHOW = 0x8002;
        private const uint WINEVENT_OUTOFCONTEXT = 0x0000;
        private const uint GW_OWNER = 4;
        private const int GWL_EXSTYLE = -20;
        private const int GWL_STYLE = -16;
        private const uint GA_ROOT = 2;
        private const int DWMWA_CLOAKED = 14;
        private const long WS_EX_NOACTIVATE = 0x08000000L;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_APPWINDOW = 0x00040000;
        private const int WS_EX_ACCEPTFILES = 0x00000010;
        private const int WS_EX_NOREDIRECTIONBITMAP = 0x00200000;
        private const int WS_EX_TOPMOST = 0x00000008;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_CAPTION = 0x00C00000;
        private const int WS_MINIMIZEBOX = 0x00020000;
        private const int WS_OVERLAPPED = 0x00000000;
        private const int WS_DISABLED = 0x08000000;
        private static readonly int[] WS_EX_STYLES_TO_IGNORE = {
        };
        private const int OBJID_WINDOW = 0;
        private const int CHILDID_SELF = 0;

        private nint _createHook = nint.Zero;
        private nint _showHook = nint.Zero;
        private nint _destroyHook = nint.Zero;

        private WinEventDelegate _winEventProc;
        private GCHandle _winEventProcHandle; // GCHandle to pin delegate
        private HashSet<nint> trackedWindows = new HashSet<nint>();  // for tracking windows that probably have GUI
        private AllowedOwnedWindowsConfiguration _allowedOwnedWindowsConfig;

#if !DEBUG
        private static readonly IntPtr _currentProcessMainWindow;
#endif

        static WindowMonitor()
        {
#if !DEBUG
            _currentProcessMainWindow = Process.GetCurrentProcess().MainWindowHandle;
#endif
        }

        public event EventHandler<WindowEventArgs>? WindowCreated;
        public event EventHandler<WindowEventArgs>? WindowDestroyed;
        public event EventHandler<WindowEventArgs>? WindowShown;

        public WindowMonitor()
        {
            _winEventProc = WinEventProc;
            _winEventProcHandle = GCHandle.Alloc(_winEventProc);

            _allowedOwnedWindowsConfig = new AllowedOwnedWindowsConfiguration();
            _allowedOwnedWindowsConfig.LoadConfiguration();
        }

        public void StartMonitoring()
        {
            if (_createHook != nint.Zero)
            {
                throw new InvalidOperationException("Monitor is already running.");
            }

            // hook for window creation
            _createHook = SetWinEventHook(
                EVENT_OBJECT_CREATE, EVENT_OBJECT_CREATE,
                nint.Zero, _winEventProc, 0, 0, WINEVENT_OUTOFCONTEXT);

            // gook for window show events
            _showHook = SetWinEventHook(
                EVENT_OBJECT_SHOW, EVENT_OBJECT_SHOW,
                nint.Zero, _winEventProc, 0, 0, WINEVENT_OUTOFCONTEXT);

            // hook for window destruction
            _destroyHook = SetWinEventHook(
                EVENT_OBJECT_DESTROY, EVENT_OBJECT_DESTROY,
                nint.Zero, _winEventProc, 0, 0, WINEVENT_OUTOFCONTEXT);

            if (_createHook == nint.Zero || _showHook == nint.Zero || _destroyHook == nint.Zero)
            {
                StopMonitoring();
                int error = Marshal.GetLastWin32Error();
                throw new InvalidOperationException($"Failed to install hook. Error code: {error}");
            }
        }

        public void StopMonitoring()
        {
            if (_createHook != nint.Zero)
            {
                UnhookWinEvent(_createHook);
                _createHook = nint.Zero;
            }

            if (_showHook != nint.Zero)
            {
                UnhookWinEvent(_showHook);
                _showHook = nint.Zero;
            }

            if (_destroyHook != nint.Zero)
            {
                UnhookWinEvent(_destroyHook);
                _destroyHook = nint.Zero;
            }

            if (_winEventProcHandle.IsAllocated)
            {
                _winEventProcHandle.Free();
            }
            _winEventProc = null!;
        }

        public void TrackExistingWindow(nint windowHandle)
        {
            if (windowHandle != nint.Zero && !trackedWindows.Contains(windowHandle))
            {
                trackedWindows.Add(windowHandle);
            }
        }
        private void WinEventProc(nint hWinEventHook, uint eventType, nint hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (idObject != OBJID_WINDOW || idChild != CHILDID_SELF || hwnd == nint.Zero)
            {
                return;
            }
            try
            {
                switch (eventType)
                {
                    case EVENT_OBJECT_CREATE:
                        if (!trackedWindows.Contains(hwnd) && IsValidApplicationWindow(hwnd))
                        {
                            trackedWindows.Add(hwnd);
                            WindowEventArgs args = CreateWindowEventArgs(hwnd);
                            WindowShown?.Invoke(this, args);
                        }
                        break;

                    case EVENT_OBJECT_SHOW:
                        if (!trackedWindows.Contains(hwnd) && IsValidApplicationWindow(hwnd))
                        {
                            if (!trackedWindows.Contains(hwnd))
                            {
                                trackedWindows.Add(hwnd);

                            }
                            WindowEventArgs args = CreateWindowEventArgs(hwnd);
                            WindowShown?.Invoke(this, args);
                        }
                        break;

                    case EVENT_OBJECT_DESTROY:
                        // only fire event if this widnow is tracked 
                        if (trackedWindows.Contains(hwnd))
                        {
                            trackedWindows.Remove(hwnd);
                            WindowEventArgs args = CreateWindowEventArgs(hwnd);
                            WindowDestroyed?.Invoke(this, args);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in WinEventProc: {ex.Message}");
            }
        }

        public bool IsValidApplicationWindow(IntPtr hWnd, bool allowUntitled = false, int minWidth = 250, int minHeight = 250, double requireAreaShare = 0.5)
        {

            if (hWnd == IntPtr.Zero) return false;
            if (!IsWindow(hWnd)) return false;
            if (!IsWindowVisible(hWnd)) return false;

#if !DEBUG
            if (hWnd == _currentProcessMainWindow && _currentProcessMainWindow != IntPtr.Zero) return false;
#endif

            if (GetAncestor(hWnd, GA_ROOT) != hWnd) return false;

            long exStyle = GetWindowLongPtr(hWnd, GWL_EXSTYLE).ToInt64();

            if ((exStyle & WS_EX_TOOLWINDOW) != 0) return false;

            if ((exStyle & WS_EX_NOACTIVATE) != 0) return false;

            try
            {
                if (DwmGetWindowAttribute(hWnd, DWMWA_CLOAKED, out int cloaked, Marshal.SizeOf(typeof(int))) == 0 && cloaked != 0)
                    return false;
            }
            catch
            {
            }

            var classNameSb = new StringBuilder(256);
            GetClassName(hWnd, classNameSb, classNameSb.Capacity);
            string cls = classNameSb.ToString();
            if (!string.IsNullOrEmpty(cls))
            {
                string lc = cls.ToLowerInvariant();
                if (lc == "shell_traywnd" || lc == "traynotifywnd" || lc == "progman" || lc == "workerw")
                    return false;
            }

            if ((exStyle & WS_EX_APPWINDOW) != 0)
            {
                int len = GetWindowTextLength(hWnd);
                if (len == 0 && !allowUntitled) return false;
                if (len > 0)
                {
                    var sb = new StringBuilder(len + 1);
                    GetWindowText(hWnd, sb, sb.Capacity);
                    if (string.IsNullOrWhiteSpace(sb.ToString()) && !allowUntitled) return false;
                }

                if (!GetWindowRect(hWnd, out RECT r)) return false;
                int width = Math.Max(0, r.Right - r.Left);
                int height = Math.Max(0, r.Bottom - r.Top);
                if (width < minWidth || height < minHeight) return false;

                return true;
            }

            if (GetWindow(hWnd, GW_OWNER) != IntPtr.Zero)
            {
                GetWindowThreadProcessId(hWnd, out uint pidTemp);
                try
                {
                    Process procc = Process.GetProcessById((int)pidTemp);
                    string processName = procc.ProcessName;
                    if (!_allowedOwnedWindowsConfig.IsAllowed(processName))
                    {
                        return false;
                    }
                }
                catch
                {
                    return false;
                }
            }

            int titleLen = GetWindowTextLength(hWnd);
            if (titleLen == 0 && !allowUntitled) return false;
            if (titleLen > 0)
            {
                var sb = new StringBuilder(titleLen + 1);
                GetWindowText(hWnd, sb, sb.Capacity);
                if (string.IsNullOrWhiteSpace(sb.ToString()) && !allowUntitled) return false;
            }

            if (!GetWindowRect(hWnd, out RECT rect)) return false;
            int windowWidth = Math.Max(0, rect.Right - rect.Left);
            int windowHeight = Math.Max(0, rect.Bottom - rect.Top);
            if (windowWidth < minWidth || windowHeight < minHeight) return false;

            GetWindowThreadProcessId(hWnd, out uint pidU);
            int pid = (int)pidU;
            Process? proc = null;
            try { proc = Process.GetProcessById(pid); } catch { proc = null; }

            if (proc != null)
            {
                try
                {
                    if (proc.MainWindowHandle == hWnd && proc.MainWindowHandle != IntPtr.Zero)
                        return true;
                }
                catch {

                }

                var windows = GetTopLevelWindowsForProcess(pid);
                if (windows.Count == 0)
                {
                    return true;
                }

                long thisArea = (long)windowWidth * windowHeight;
                long maxArea = 0;
                foreach (var w in windows)
                {
                    if (GetWindowRect(w, out RECT rr))
                    {
                        long a = Math.Max(0, rr.Right - rr.Left) * Math.Max(0, rr.Bottom - rr.Top);
                        if (a > maxArea) maxArea = a;
                    }
                }

                if (maxArea == 0) return true; 
                if (thisArea >= maxArea * requireAreaShare) return true;

                return false;
            }
            return true;
        }

        private static List<IntPtr> GetTopLevelWindowsForProcess(int pid)
        {
            var list = new List<IntPtr>();
            EnumWindows((hWnd, lParam) =>
            {
                GetWindowThreadProcessId(hWnd, out uint p);
                if ((int)p != pid) return true;

                if (!IsWindowVisible(hWnd)) return true;
                if (GetAncestor(hWnd, GA_ROOT) != hWnd) return true;

                long exStyle = GetWindowLongPtr(hWnd, GWL_EXSTYLE).ToInt64();

                if ((exStyle & WS_EX_TOOLWINDOW) != 0) return true;

                bool hasAppWindow = (exStyle & WS_EX_APPWINDOW) != 0;

                if (GetWindow(hWnd, GW_OWNER) != IntPtr.Zero && !hasAppWindow) return true;

                try
                {
                    if (DwmGetWindowAttribute(hWnd, DWMWA_CLOAKED, out int cloaked, Marshal.SizeOf(typeof(int))) == 0 && cloaked != 0)
                        return true;
                }
                catch {

                }

                list.Add(hWnd);
                return true;
            }, IntPtr.Zero);

            return list;
        }
        public bool IsValidApplicationWindowForDestorying(nint hwnd)
        {
            if (GetWindow(hwnd, GW_OWNER) != nint.Zero) return false;

            int exStyle = GetWindowLong(hwnd, GWL_STYLE);
            foreach(int value in WS_EX_STYLES_TO_IGNORE)
            {
                if (value == exStyle)
                {
                    return false;
                }
            }
            string title = GetWindowTitle(hwnd);
            if (string.IsNullOrWhiteSpace(title) || title.Length < 2) return false;

            return true;
        }

        private WindowEventArgs CreateWindowEventArgs(nint hwnd)
        {
            string windowTitle = GetWindowTitle(hwnd);
            string processName = GetProcessName(hwnd);
            uint processId = GetProcessId(hwnd);

            return new WindowEventArgs
            {
                WindowHandle = hwnd,
                WindowTitle = windowTitle,
                ProcessName = processName,
                ProcessId = processId,
                Timestamp = DateTime.Now
            };
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

        private string GetProcessName(nint hwnd)
        {
            try
            {
                uint processId = GetProcessId(hwnd);
                if (processId == 0) return "Unknown";

                Process process = Process.GetProcessById((int)processId);
                return process.ProcessName;
            }
            catch
            {
                return "Unknown";
            }
        }

        private uint GetProcessId(nint hwnd)
        {
            try
            {
                GetWindowThreadProcessId(hwnd, out uint processId);
                return processId;
            }
            catch
            {
                return 0;
            }
        }

        public void ReloadAllowedOwnedWindowsConfiguration()
        {
            _allowedOwnedWindowsConfig.LoadConfiguration();
        }

        public void Dispose()
        {
            StopMonitoring();
        }
    } 
}