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


﻿using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static TilingWindowManager.BSPTiling;

namespace TilingWindowManager
{
    public partial class WindowManager
    {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(nint hWnd);

        [DllImport("user32.dll")]
        private static extern bool BringWindowToTop(nint hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(nint hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, nint lParam);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(nint hWnd);

        [DllImport("user32.dll")]
        private static extern int GetWindowText(nint hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern int GetWindowTextLength(nint hWnd);

        [DllImport("user32.dll")]
        private static extern nint GetWindow(nint hWnd, uint uCmd);

        [DllImport("user32.dll")]
        private static extern nint GetParent(nint hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsWindowEnabled(nint hWnd);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(nint hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(nint hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(nint hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern nint MonitorFromWindow(nint hwnd, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern nint GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern nint SetWinEventHook(uint eventMin, uint eventMax, nint hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool UnhookWinEvent(nint hWinEventHook);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(nint hWnd, nint hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern nint SetWindowsHookEx(int idHook, MouseHookProc lpfn, nint hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(nint hhk);

        [DllImport("user32.dll")]
        private static extern nint CallNextHookEx(nint hhk, int nCode, nint wParam, nint lParam);

        [DllImport("kernel32.dll")]
        private static extern nint GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern nint MonitorFromPoint(POINT pt, uint dwFlags);
        
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetClassName(nint hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(nint hWnd);

        [DllImport("user32.dll")]
        private static extern bool PostMessage(nint hWnd, uint Msg, nint wParam, nint lParam);


        private delegate void WinEventDelegate(nint hWinEventHook, uint eventType, nint hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        private delegate nint MouseHookProc(int nCode, nint wParam, nint lParam);

        private delegate bool EnumWindowsProc(nint hWnd, nint lParam);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public int Width => Right - Left;
            public int Height => Bottom - Top;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public nint dwExtraInfo;
        }

        // windows api constants
        private const uint EVENT_OBJECT_CREATE = 0x8000;
        private const uint EVENT_OBJECT_SHOW = 0x8002;
        private const uint EVENT_OBJECT_HIDE = 0x8003;
        private const uint EVENT_OBJECT_LOCATIONCHANGE = 0x800B;
        private const uint EVENT_OBJECT_SIZECHANGE = 0x800A;
        private const uint EVENT_SYSTEM_MOVESIZESTART = 0x000A;
        private const uint EVENT_SYSTEM_MOVESIZEEND = 0x000B;
        private const uint EVENT_SYSTEM_MINIMIZESTART = 0x0016;
        private const uint EVENT_SYSTEM_MINIMIZEEND = 0x0017;
        private const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
        private const uint WINEVENT_OUTOFCONTEXT = 0x0000;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const int WH_MOUSE_LL = 14;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const uint WM_CLOSE = 0x0010;

        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;
        private const int SW_RESTORE = 9;
        private const uint GW_OWNER = 4;
        private const int GWL_EXSTYLE = -20;
        private const int GWL_STYLE = -16;
        private const uint WS_EX_TOOLWINDOW = 0x00000080;
        private const uint WS_EX_APPWINDOW = 0x00040000;
        private const uint PM_NOREMOVE = 0x0000;
        private const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

        private WinEventDelegate winEventProc = null!;
        private WinEventDelegate newWindowEventProc = null!;
        private GCHandle winEventProcHandle;
        private GCHandle newWindowEventProcHandle;
        private EnumWindowsProc enumWindowsProc = null!;
        private GCHandle enumWindowsProcHandle;
        private WinEventDelegate locationEventProc = null!;
        private GCHandle locationEventProcHandle; 
        private nint moveStartHook = nint.Zero;
        private nint moveEndHook = nint.Zero;
        private nint newWindowHook = nint.Zero;
        private nint minimizeStartHook = nint.Zero;
        private nint minimizeEndHook = nint.Zero;
        private nint foregroundHook = nint.Zero;
        private Dictionary<nint, RECT> preMoveWindowPositions = new Dictionary<nint, RECT>(); 
        private readonly Dictionary<nint, System.Threading.Timer> moveEndDebounceTimers = new();

        private List<Monitor> monitors = null!;
        private bool isRunning = true;
        private WindowBorder windowBorder;
        private WindowMonitor windowMonitor;
        private bool isTilingEnabled = true;
        private bool autoTilingEnabled = true;
        private bool isTilingInProgress = false;
        private bool activeWindowFollowsMouse = true;
        private int globalActiveMonitorIndex = 0;
        private int lastActiveMonitorIndex = 0;
        private bool isInEventCallback = false; 
        private readonly object eventCallbackLock = new object();
        private Func<nint, nint, bool> currentEnumCallback = null!; 
        private PinnedApplicationsConfiguration pinnedApplicationsConfig;
        private ApplicationHotkeysConfiguration applicationHotkeysConfig;
        private nint lastTrackedFocusedWindow = nint.Zero;
        private HashSet<nint> excludedFromTilingWindows = new HashSet<nint>(); 

        public bool ActiveWindowFollowsMouse
        {
            get => activeWindowFollowsMouse;
            set => activeWindowFollowsMouse = value;
        }


        public WindowManager()
        {
            enumWindowsProc = new EnumWindowsProc(EnumWindowsCallback);
            enumWindowsProcHandle = GCHandle.Alloc(enumWindowsProc);

            pinnedApplicationsConfig = new PinnedApplicationsConfiguration();
            pinnedApplicationsConfig.LoadConfiguration();

            applicationHotkeysConfig = new ApplicationHotkeysConfiguration();
            applicationHotkeysConfig.LoadConfiguration();

            InitializeMonitors();
            InitializeDragAndSwap();
            InitializeMonitorChangeDetection();

            InitializeWorkspaceIndicators();

            windowBorder = new WindowBorder();
            windowMonitor = new WindowMonitor();
            windowBorder.SetTileCheckCallback(IsWindowTiled);

            InitializeWindowEventHooks();

            windowMonitor.StartMonitoring();

            foreach (var monitor in monitors)
            {
                ApplyTilingToAllMonitorWorkspaces(monitor);
            }

            ScanAndMovePinnedApplications();

            windowMonitor.WindowShown += (sender, e) =>
            {
                if (IsWindowVisible(e.WindowHandle))
                {

                    var originalMonitorHandle = MonitorFromWindow(e.WindowHandle, MONITOR_DEFAULTTONEAREST);
                    var targetMonitor = GetMonitorByIndex(lastActiveMonitorIndex) ?? GetActiveMonitor();
                    if (targetMonitor.GetCurrentWorkspace().ContainsWindow(e.WindowHandle)) return;
                    if (targetMonitor.Handle == nint.Zero) return;
                    if (originalMonitorHandle != targetMonitor.Handle)
                    {
                        MoveWindowToMonitor(e.WindowHandle, targetMonitor);
                    }
                    
                    string executableName = GetExecutableNameFromWindow(e.WindowHandle);
                    int? pinnedWorkspace = pinnedApplicationsConfig.GetPinnedWorkspace(executableName);

                    if (pinnedWorkspace.HasValue)
                    {
                        targetMonitor.AddWindowToWorkspace(e.WindowHandle, pinnedWorkspace.Value);

                        if (targetMonitor.CurrentWorkspaceId == pinnedWorkspace.Value)
                        {
                            var currentWorkspace = targetMonitor.GetCurrentWorkspace();
                            if (currentWorkspace.IsStackedMode)
                            {
                                currentWorkspace.FocusNewestWindowInStack();
                                ApplyStackedLayout(targetMonitor, currentWorkspace);
                            }
                            else
                            {
                                ApplyTilingToCurrentWorkspace(targetMonitor);
                            }
                        }
                    }
                    else
                    {
                        targetMonitor.AddWindowToCurrentWorkspace(e.WindowHandle);

                        var currentWorkspace = targetMonitor.GetCurrentWorkspace();
                        if (currentWorkspace.IsStackedMode)
                        {
                            currentWorkspace.FocusNewestWindowInStack();
                            ApplyStackedLayout(targetMonitor, currentWorkspace);
                        }
                        else
                        {
                            ApplyTilingToCurrentWorkspace(targetMonitor);
                        }
                    }
                }
            };

            windowMonitor.WindowDestroyed += (sender, e) =>
            {
                excludedFromTilingWindows.Remove(e.WindowHandle);

                foreach (var m in monitors)
                {
                    var workspace = m.FindWorkspaceContaining(e.WindowHandle);
                    if (workspace != null)
                    {
                        int workspaceId = workspace.Id;
                        bool willBeEmpty = workspace.WindowCount <= 1;
                        bool isStackedMode = workspace.IsStackedMode;

                        workspace.RemoveWindow(e.WindowHandle);
                        m.RemoveWindowTrackingPositions(e.WindowHandle);

                        if (m.CurrentWorkspaceId == workspace.Id)
                        {
                            if (isStackedMode)
                            {
                                var stackableWindows = workspace.GetStackableWindows();
                                if (stackableWindows.Count > 0)
                                {
                                    int currentIndex = workspace.GetCurrentStackedWindowIndex();
                                    if (currentIndex >= stackableWindows.Count)
                                    {
                                        workspace.EnableStackedMode();
                                    }
                                    ApplyStackedLayout(m, workspace);
                                }
                                else
                                {
                                    UpdateWorkspaceIndicator();
                                }
                            }
                            else
                            {
                                FocusMostRecentWindow(m.Handle, workspace.GetAllWindows());
                                ApplyTilingToCurrentWorkspace(m);
                            }
                        }

                        if (willBeEmpty)
                        {
                            CleanupBackupWorkspaceIfEmpty(m, workspaceId);
                        }
                        break;
                    }
                }
            };
        }


        public void Run(HotKey hotKey)
        {
            const int MAX_CONSECUTIVE_ERRORS = 10;
            int consecutiveErrors = 0;

            InitializeHotkeyConfig(hotKey.Configuration);

            while (isRunning)
            {
                try
                {
                    var msg = hotKey.CheckReceivedKey();
                    if (msg.HasValue && msg.Value.message == HotKey.WM_HOTKEY)
                    {
                        int hotkeyId = msg.Value.wParam.ToInt32();
                        HandleHotkey(hotKey, hotkeyId);
                    }

                    consecutiveErrors = 0;
                }
                catch (System.ExecutionEngineException)
                {
                    consecutiveErrors++;

                    if (consecutiveErrors >= MAX_CONSECUTIVE_ERRORS)
                    {
                        isRunning = false;
                        break;
                    }

                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    Thread.Sleep(100);
                }
                catch (Exception)
                {
                    consecutiveErrors++;

                    if (consecutiveErrors >= MAX_CONSECUTIVE_ERRORS)
                    {
                        isRunning = false;
                        break;
                    }
                }

                hotKey.WaitForNextMessage();
            }
        }

        private void CheckAndMoveNewWindow(nint hwnd)
        {
            try
            {

                nint windowMonitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
                var currentMonitor = GetMonitorByHandle(windowMonitor);

                var activeMonitor = GetMonitorByIndex(globalActiveMonitorIndex);

                if (activeMonitor == null || currentMonitor == null)
                    return;

                if (currentMonitor.Index != activeMonitor.Index)
                {
                    MoveWindowToActiveMonitor(hwnd, activeMonitor);
                }

                MoveWindowToEmptyWorkspace(hwnd);
            }
            catch (Exception)
            {
            }
        }
        private void MoveWindowToActiveMonitor(nint window, Monitor targetMonitor)
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

                // move window
                SuspendBorder(window);
                SetWindowPos(window, nint.Zero, newX, newY, 0, 0,
                    SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
                RefreshBorder(window);

            }
            catch (Exception ex)
            {
                Logger.Error($"Error moving window to active monitor: {ex.Message}");
            }
        }

        public void MoveWindowToEmptyWorkspace(nint window)
        {

            nint monitorHandle = MonitorFromWindow(window, MONITOR_DEFAULTTONEAREST);
            var monitor = GetMonitorByHandle(monitorHandle);
            if (monitor == null) return;

            string executableName = GetExecutableNameFromWindow(window);
            int? pinnedWorkspace = pinnedApplicationsConfig.GetPinnedWorkspace(executableName);

            if (pinnedWorkspace.HasValue)
            {
                monitor.AddWindowToWorkspace(window, pinnedWorkspace.Value);

                if (monitor.CurrentWorkspaceId == pinnedWorkspace.Value)
                {
                    ApplyTilingToCurrentWorkspace(monitor);
                }
            }
            else
            {
                monitor.AddWindowToCurrentWorkspace(window);
                ApplyTilingToCurrentWorkspace(monitor);
            }
        }
        private void InitializeWorkspaceIndicators()
        {

            Monitor.InitializeWorkspaceIndicator(monitors, (monitorIndex, workspaceId) => SwitchToWorkspace(workspaceId, monitorIndex));

            foreach (var monitor in monitors)
            {
                try
                {
                    monitor.UpdateWorkspaceIndicator();
                }
                catch (Exception ex)
                {
                    Logger.Error($"Exception details: {ex}");
                }
            }
        }
        private void CleanupWorkspaceIndicators()
        {
            Monitor.CleanupWorkspaceIndicator();
        }

        private void ReloadConfiguration(HotKey hotKey)
        {

            try
            {
                Logger.ReloadConfiguration();

                hotKey.ReloadHotKeys();
                InitializeHotkeyConfig(hotKey.Configuration);

                windowBorder?.Cleanup();
                Thread.Sleep(100); 
                windowBorder = new WindowBorder();
                windowBorder.SetTileCheckCallback(IsWindowTiled);

                CleanupWorkspaceIndicators();
                Thread.Sleep(100);
                InitializeWorkspaceIndicators();

                pinnedApplicationsConfig = new PinnedApplicationsConfiguration();
                pinnedApplicationsConfig.LoadConfiguration();

                applicationHotkeysConfig = new ApplicationHotkeysConfiguration();
                applicationHotkeysConfig.LoadConfiguration();

                var workspaceConfig = new WorkspaceConfiguration();
                workspaceConfig.LoadConfiguration();

                if (workspaceConfig.StackedOnStartup)
                {
                    foreach (var monitor in monitors)
                    {
                        foreach (var workspace in monitor.GetAllWorkspaces())
                        {
                            if (!workspace.IsStackedMode)
                            {
                                workspace.EnableStackedMode();
                            }
                        }
                    }
                }


                foreach (var monitor in monitors)
                {
                    var currentWorkspace = monitor.GetCurrentWorkspace();
                    if (currentWorkspace.IsStackedMode)
                    {
                        currentWorkspace.FocusNewestWindowInStack();
                        ApplyStackedLayout(monitor, currentWorkspace);
                    }
                    else
                    {
                        ApplyTilingToCurrentWorkspace(monitor);
                    }
                }

                UpdateWorkspaceIndicator();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error reloading configuration");
            }
        }

        private void InitializeWindowEventHooks()
        {
            winEventProc = new WinEventDelegate(WinEventProc);
            winEventProcHandle = GCHandle.Alloc(winEventProc);

            moveStartHook = SetWinEventHook(
                EVENT_SYSTEM_MOVESIZESTART, EVENT_SYSTEM_MOVESIZESTART,
                nint.Zero, winEventProc, 0, 0, WINEVENT_OUTOFCONTEXT);

            moveEndHook = SetWinEventHook(
                EVENT_SYSTEM_MOVESIZEEND, EVENT_SYSTEM_MOVESIZEEND,
                nint.Zero, winEventProc, 0, 0, WINEVENT_OUTOFCONTEXT);

            minimizeStartHook = SetWinEventHook(
                EVENT_SYSTEM_MINIMIZESTART, EVENT_SYSTEM_MINIMIZESTART,
                nint.Zero, winEventProc, 0, 0, WINEVENT_OUTOFCONTEXT);

            minimizeEndHook = SetWinEventHook(
                EVENT_SYSTEM_MINIMIZEEND, EVENT_SYSTEM_MINIMIZEEND,
                nint.Zero, winEventProc, 0, 0, WINEVENT_OUTOFCONTEXT);

            foregroundHook = SetWinEventHook(
                EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND,
                nint.Zero, winEventProc, 0, 0, WINEVENT_OUTOFCONTEXT);

        }

        private void WinEventProc(nint hWinEventHook, uint eventType, nint hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            const int OBJID_WINDOW = 0;
            const int CHILDID_SELF = 0;

            if (idObject != OBJID_WINDOW || idChild != CHILDID_SELF || hwnd == nint.Zero)
                return;

            lock (eventCallbackLock)
            {
                if (isInEventCallback)
                    return;
                isInEventCallback = true;
            }

            try
            {
                if (eventType == EVENT_SYSTEM_MINIMIZESTART || eventType == EVENT_SYSTEM_MINIMIZEEND)
                {
                    if (eventType == EVENT_SYSTEM_MINIMIZESTART)
                    {
                        OnWindowMinimized(hwnd);
                    }
                    else if (eventType == EVENT_SYSTEM_MINIMIZEEND)
                    {
                        OnWindowRestored(hwnd);
                    }
                }
                else if (eventType == EVENT_SYSTEM_FOREGROUND)
                {
                    windowBorder.UpdateBorderImproved(hwnd);
                    TrackFocusedWindowChanges(hwnd);

                    // if tray click activates a window switch to workspace where window lives
					try
					{
						if (windowMonitor != null && windowMonitor.IsValidApplicationWindow(hwnd))
						{
							foreach (var m in monitors)
							{
								var ws = m.FindWorkspaceContaining(hwnd);
								if (ws != null)
								{
									if (m.CurrentWorkspaceId != ws.Id)
									{
										SwitchToWorkspace(ws.Id, m.Index);
									}
									SetForegroundWindow(hwnd);
									BringWindowToTop(hwnd);
									globalActiveMonitorIndex = m.Index;
									lastActiveMonitorIndex = m.Index;
									break;
								}
							}
						}
					}
					catch { }
                }
                else if (IsWindowInCurrentWorkspace(hwnd))
                {
                    if (eventType == EVENT_SYSTEM_MOVESIZESTART)
                    {
                        OnUserStartsDragOrResize(hwnd);
                    }
                    else if (eventType == EVENT_SYSTEM_MOVESIZEEND)
                    {
                        if (moveEndDebounceTimers.TryGetValue(hwnd, out var t))
                        {
                            t.Change(75, System.Threading.Timeout.Infinite);
                        }
                        else
                        {
                            var timer = new System.Threading.Timer(_ =>
                            {
                                try { OnUserFinishesDragOrResize(hwnd); }
                                catch { }
                                finally
                                {
                                    lock (moveEndDebounceTimers) { moveEndDebounceTimers.Remove(hwnd); }
                                }
                            }, null, 75, System.Threading.Timeout.Infinite);
                            moveEndDebounceTimers[hwnd] = timer;
                        }
                    }
                }
                else if (eventType == EVENT_OBJECT_CREATE)
                {
                   CheckAndMoveNewWindow(hwnd); 
                }
            }
            catch (Exception ex)
            {
                Logger.Info($"Error in WinEventProc: {ex.Message}");
            }
            finally
            {
                lock (eventCallbackLock)
                {
                    isInEventCallback = false;
                }
            }
        }
        private bool IsWindowInCurrentWorkspace(nint window)
        {
            if (monitors == null) return false;
            foreach (var monitor in monitors)
            {
                if (monitor.IsWindowInCurrentWorkspace(window))
                {
                    return true;
                }
            }
            return false;
        }

        private void OnWindowMinimized(nint window)
        {
            foreach (var monitor in monitors)
            {
                var workspace = monitor.FindWorkspaceContaining(window);
                if (workspace != null)
                {
                    workspace.GetTiling()?.RemoveWindow(window);
                    break;
                }
            }
        }

        private void OnWindowRestored(nint window)
        {
            var monitor = GetMonitorForWindow(window);
            if (monitor == null) return;
            monitor.AddWindowToCurrentWorkspace(window);
            ApplyTilingToCurrentWorkspace(monitor);
        }

        private bool EnumWindowsCallback(nint hWnd, nint lParam)
        {
            return currentEnumCallback?.Invoke(hWnd, lParam) ?? true;
        }

        private string GetExecutableNameFromWindow(nint window)
        {
            try
            {
                GetWindowThreadProcessId(window, out uint processId);
                using var process = System.Diagnostics.Process.GetProcessById((int)processId);
                return Path.GetFileName(process.ProcessName + ".exe");
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get executable name for window {window}: {ex.Message}");
                return string.Empty;
            }
        }

        private void ScanAndMovePinnedApplications()
        {

            var allWindows = new List<nint>();

            currentEnumCallback = (hWnd, lParam) =>
            {
                if (windowMonitor.IsValidApplicationWindow(hWnd))
                {
                    allWindows.Add(hWnd);
                }
                return true;
            };

            EnumWindows(enumWindowsProc, nint.Zero);

            int movedCount = 0;
            foreach (var window in allWindows)
            {
                try
                {

                    nint monitorHandle = MonitorFromWindow(window, MONITOR_DEFAULTTONEAREST);
                    var monitor = GetMonitorByHandle(monitorHandle);

                    if (monitor != null)
                    {
                        string executableName = GetExecutableNameFromWindow(window);
                        int? pinnedWorkspace = pinnedApplicationsConfig.GetPinnedWorkspace(executableName);

                        if (pinnedWorkspace.HasValue)
                        {
                            var currentWorkspace = monitor.FindWorkspaceContaining(window);

                            if (currentWorkspace == null || currentWorkspace.Id != pinnedWorkspace.Value)
                            {
                                if (currentWorkspace != null)
                                {
                                    currentWorkspace.RemoveWindow(window);
                                }

                                monitor.AddWindowToWorkspace(window, pinnedWorkspace.Value);

                                if (monitor.CurrentWorkspaceId != pinnedWorkspace.Value)
                                {
                                    ShowWindow(window, SW_HIDE);
                                }

                                movedCount++;
                            }
                        }
                        else
                        {
                            var currentWorkspace = monitor.FindWorkspaceContaining(window);
                            if (currentWorkspace == null)
                            {
                                monitor.AddWindowToCurrentWorkspace(window);
                                movedCount++;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error processing window {window}: {ex.Message}");
                }
            }

            if (movedCount > 0)
            {
                foreach (var monitor in monitors)
                {
                    ApplyTilingToCurrentWorkspace(monitor);
                }
            }
        }

        private void SwitchToApplicationWorkspace(string executableName)
        {
            foreach (var monitor in monitors)
            {
                for (int workspaceId = 1; workspaceId <= 8; workspaceId++)
                {
                    var workspace = monitor.GetWorkspace(workspaceId);
                    var windows = workspace.GetAllWindows();

                    foreach (var window in windows)
                    {
                        try
                        {
                            string windowExecutable = GetExecutableNameFromWindow(window);
                            if (windowExecutable.Equals(executableName, StringComparison.OrdinalIgnoreCase))
                            {

                                SwitchToWorkspace(workspaceId, monitor.Index);

                                SetForegroundWindow(window);
                                BringWindowToTop(window);

                                globalActiveMonitorIndex = monitor.Index;
                                lastActiveMonitorIndex = monitor.Index;

                                return;
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Error($"Error checking window {window}: {ex.Message}");
                        }
                    }
                }
            }

        }


        private void RefreshBorder(nint window)
        {
            if (window == nint.Zero || windowBorder == null || !windowBorder.IsEnabled)
            {
                return;
            }

            windowBorder.UpdateBorderForWindow(window);
        }

        private void SuspendBorder(nint window)
        {
            if (window == nint.Zero || windowBorder == null || !windowBorder.IsEnabled)
            {
                return;
            }

            windowBorder.HideBorderForWindow(window);
        }

        private void RefreshBorderForForeground()
        {
            if (windowBorder == null || !windowBorder.IsEnabled)
            {
                return;
            }

            nint foreground = GetForegroundWindow();
            if (foreground != nint.Zero)
            {
                windowBorder.UpdateBorderForWindow(foreground);
            }
        }

        private void SuspendBorderForForeground()
        {
            if (windowBorder == null || !windowBorder.IsEnabled)
            {
                return;
            }

            nint foreground = GetForegroundWindow();
            if (foreground != nint.Zero)
            {
                windowBorder.HideBorderForWindow(foreground);
            }
        }

        private void TrackFocusedWindowChanges(nint currentFocusedWindow)
        {
            try
            {

                if (currentFocusedWindow == nint.Zero || currentFocusedWindow == lastTrackedFocusedWindow)
                {
                    return;
                }

                lastTrackedFocusedWindow = currentFocusedWindow;

                foreach (var monitor in monitors)
                {
                    var workspace = monitor.FindWorkspaceContaining(currentFocusedWindow);
                    if (workspace != null)
                    {
                        workspace.SetLastActiveWindow(currentFocusedWindow);

                        if (monitor.Index != globalActiveMonitorIndex)
                        {
                            globalActiveMonitorIndex = monitor.Index;
                            lastActiveMonitorIndex = monitor.Index;
                        }

                        break;
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        private void UnstackAllWorkspaces()
        {
            foreach (var monitor in monitors)
            {
                foreach (var workspace in monitor.GetAllWorkspaces())
                {
                    if (workspace.IsStackedMode)
                    {
                        workspace.DisableStackedMode();
                        var stackableWindows = workspace.GetStackableWindows();
                        foreach (var window in stackableWindows)
                        {
                            ShowWindow(window, SW_RESTORE);
                        }
                    }
                }
            }
        }

        public void Cleanup()
        {


            if (minimizeEndHook != nint.Zero)
            {
                UnhookWinEvent(minimizeEndHook);
                minimizeEndHook = nint.Zero;
            }

            if (minimizeStartHook != nint.Zero)
            {
                UnhookWinEvent(minimizeStartHook);
                minimizeStartHook = nint.Zero;
            }

            if (newWindowHook != nint.Zero)
            {
                UnhookWinEvent(newWindowHook);
                newWindowHook = nint.Zero;
            }

            if (moveEndHook != nint.Zero)
            {
                UnhookWinEvent(moveEndHook);
                moveEndHook = nint.Zero;
            }

            if (moveStartHook != nint.Zero)
            {
                UnhookWinEvent(moveStartHook);
                moveStartHook = nint.Zero;
            }

            if (foregroundHook != nint.Zero)
            {
                UnhookWinEvent(foregroundHook);
                foregroundHook = nint.Zero;
            }

            lock (moveEndDebounceTimers)
            {
                foreach (var kv in moveEndDebounceTimers)
                {
                    kv.Value.Dispose();
                }
                moveEndDebounceTimers.Clear();
            }

            UnstackAllWorkspaces();

            ShowAllWindowsInAllMonitors();
            CleanupMonitorChangeDetection();
            CleanupDragAndSwap();
            windowBorder?.Cleanup();
            windowMonitor?.StopMonitoring();
            CleanupWorkspaceIndicators();

            foreach (var monitor in monitors)
            {
                monitor.GetWorkspaces().ClearAllWorkspaces();
                foreach (var workspace in monitor.GetAllWorkspaces())
                {
                    foreach (var window in workspace.GetAllWindows())
                    {
                        monitor.RemoveWindowTrackingPositions(window);
                    }
                }
            }

            preMoveWindowPositions.Clear();

            if (winEventProcHandle.IsAllocated)
            {
                winEventProcHandle.Free();
            }
            if (newWindowEventProcHandle.IsAllocated)
            {
                newWindowEventProcHandle.Free();
            }
            if (enumWindowsProcHandle.IsAllocated)
            {
                enumWindowsProcHandle.Free();
            }
            if (locationEventProcHandle.IsAllocated)
            {
                locationEventProcHandle.Free();
            }

            winEventProc = null!;
            newWindowEventProc = null!;
            enumWindowsProc = null!;
            locationEventProc = null!;
            currentEnumCallback = null!;
        }

    }
}