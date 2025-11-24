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


using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace TilingWindowManager
{
    public partial class WindowManager
    {
        // ===== Win32 constants =====
        private const int WM_DEVICECHANGE = 0x0219;
        private const int WM_DISPLAYCHANGE = 0x007E;
        private const int DBT_DEVICEARRIVAL = 0x8000;
        private const int DBT_DEVICEREMOVECOMPLETE = 0x8004;
        private const int DBT_DEVNODES_CHANGED = 0x0007;
        private const int DBT_DEVTYP_DEVICEINTERFACE = 0x00000005;
        private const int DEVICE_NOTIFY_WINDOW_HANDLE = 0x00000000;

        private readonly HashSet<string> _lastKnownMonitors = new HashSet<string>();
        private bool _monitorChangeInitialized = false;
        private nint _deviceNotificationHandle = nint.Zero;
        private nint _monitorMessageWindow = nint.Zero;
        private readonly Guid GUID_DEVINTERFACE_MONITOR = new Guid("e6f07b5f-ee97-4a90-b076-33f57bf4eaa7");
        private const string MONITOR_WINDOW_CLASS = "TilingWMMonitorWatch";
        private WndProcDelegate _monitorWndProc;

        private delegate nint WndProcDelegate(nint hWnd, uint msg, nint wParam, nint lParam);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern ushort RegisterClassW(ref WNDCLASS lpWndClass);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern nint CreateWindowExW(
            int dwExStyle,
            string lpClassName,
            string lpWindowName,
            int dwStyle,
            int x, int y, int nWidth, int nHeight,
            nint hWndParent,
            nint hMenu,
            nint hInstance,
            nint lpParam);

        [DllImport("user32.dll")]
        private static extern nint DefWindowProcW(nint hWnd, uint uMsg, nint wParam, nint lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyWindow(nint hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterClassW(string lpClassName, nint hInstance);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern nint RegisterDeviceNotificationW(
            nint hRecipient,
            nint NotificationFilter,
            int Flags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterDeviceNotification(nint Handle);

        [DllImport("user32.dll")]
        private static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WNDCLASS
        {
            public uint style;
            public WndProcDelegate lpfnWndProc;
            public int cbClsExtra;
            public int cbWndExtra;
            public nint hInstance;
            public nint hIcon;
            public nint hCursor;
            public nint hbrBackground;
            public string lpszMenuName;
            public string lpszClassName;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DEV_BROADCAST_HDR
        {
            public int dbch_size;
            public int dbch_devicetype;
            public int dbch_reserved;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct DEV_BROADCAST_DEVICEINTERFACE_W_FIXED
        {
            public int dbcc_size;
            public int dbcc_devicetype;
            public int dbcc_reserved;
            public Guid dbcc_classguid;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct DISPLAY_DEVICE
        {
            [MarshalAs(UnmanagedType.U4)]
            public int cb;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceString;
            [MarshalAs(UnmanagedType.U4)]
            public DisplayDeviceStateFlags StateFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceKey;
        }

        [Flags]
        public enum DisplayDeviceStateFlags : int
        {
            AttachedToDesktop = 0x1,
            MultiDriver = 0x2,
            PrimaryDevice = 0x4,
            MirroringDriver = 0x8,
            VGACompatible = 0x10,
            Removable = 0x20,
            ModesPruned = 0x8000000,
            Remote = 0x4000000,
            Disconnect = 0x2000000
        }


        private Dictionary<int, Dictionary<int, MonitorBackup>> _monitorBackupsByCount = new Dictionary<int, Dictionary<int, MonitorBackup>>();
        private Dictionary<int, HashSet<int>> _backupWorkspacesPerMonitor = new Dictionary<int, HashSet<int>>();
        private int _lastMonitorCount = 0;
        private int _reconnectionAttempts = 0;
        private const int BASE_STABILIZATION_DELAY_MS = 1000;
        private const int MAX_STABILIZATION_DELAY_MS = 8000;
        private const int COALESCE_DELAY_MS = 1200;
        private readonly object _monitorChangeDebounceLock = new object();
        private System.Threading.Timer? _monitorChangeDebounceTimer = null;
        private bool _useCoalescedMonitorReconciliation = true;

        private class MonitorBackup
        {
            public int MonitorIndex { get; set; }
            public bool IsPrimary { get; set; }
            public int CurrentWorkspaceId { get; set; }
            public Monitor MonitorReference { get; set; } = null!;
            public RECT Bounds { get; set; }
            public DateTime BackedUpAt { get; set; }
            public int MonitorCount { get; set; } // total monitor count when this was backed up
            public Dictionary<int, List<nint>> WorkspaceWindows { get; set; } = new Dictionary<int, List<nint>>();
            public Dictionary<int, bool> WorkspaceStackedMode { get; set; } = new Dictionary<int, bool>();
        }
        public void InitializeMonitorChangeDetection()
        {
            if (_monitorChangeInitialized) return;

            UpdateMonitorList();
            _lastMonitorCount = monitors?.Count ?? 0;

            try
            {
                CreateMonitorMessageWindow();

                if (_monitorMessageWindow != nint.Zero)
                {
                    _deviceNotificationHandle = RegisterForMonitorInterfaceNotifications(_monitorMessageWindow);

                }

                _monitorChangeInitialized = true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error initializing monitor change detection: {ex.Message}");
            }
        }

        public void CleanupMonitorChangeDetection()
        {
            if (_deviceNotificationHandle != nint.Zero)
            {
                UnregisterDeviceNotification(_deviceNotificationHandle);
                _deviceNotificationHandle = nint.Zero;
            }

            if (_monitorMessageWindow != nint.Zero)
            {
                DestroyWindow(_monitorMessageWindow);
                _monitorMessageWindow = nint.Zero;
            }

            try
            {
                UnregisterClassW(MONITOR_WINDOW_CLASS, GetModuleHandle(null!));
            }
            catch { }

            _monitorChangeInitialized = false;
        }

        private void CreateMonitorMessageWindow()
        {
            _monitorWndProc = MonitorWindowProc;

            var wc = new WNDCLASS
            {
                lpfnWndProc = _monitorWndProc,
                hInstance = GetModuleHandle(null!),
                lpszClassName = MONITOR_WINDOW_CLASS,
                lpszMenuName = string.Empty
            };

            ushort atom = RegisterClassW(ref wc);
            if (atom == 0)
            {
                throw new InvalidOperationException("Failed to register monitor window class");
            }

            _monitorMessageWindow = CreateWindowExW(
                0,
                MONITOR_WINDOW_CLASS,
                "TilingWM Monitor Watch Window",
                0,
                0, 0, 0, 0,
                nint.Zero,
                nint.Zero,
                GetModuleHandle(null!),
                nint.Zero);

            if (_monitorMessageWindow == nint.Zero)
            {
                throw new InvalidOperationException("Failed to create monitor message window");
            }
        }

        private nint MonitorWindowProc(nint hWnd, uint msg, nint wParam, nint lParam)
        {
            switch (msg)
            {
                case WM_DEVICECHANGE:
                {
                    int eventType = wParam.ToInt32();
                    if (eventType == DBT_DEVICEARRIVAL || eventType == DBT_DEVICEREMOVECOMPLETE)
                    {
                        if (_useCoalescedMonitorReconciliation)
                        {
                            bool isArrival = (eventType == DBT_DEVICEARRIVAL);
                            ScheduleMonitorReconciliation(isArrival);
                        }
                        else
                        {
                            Task.Run(() => CheckForMonitorChanges());
                        }
                    }
                    else if (eventType == DBT_DEVNODES_CHANGED)
                    {
                        if (_useCoalescedMonitorReconciliation)
                        {
                            ScheduleMonitorReconciliation(false);
                        }
                        else
                        {
                            Task.Run(() => CheckForMonitorChanges());
                        }
                    }
                    break;
                }

                case WM_DISPLAYCHANGE:
                {
                    int bitsPerPixel = (int)wParam & 0xFFFF;
                    int width = (int)(lParam.ToInt64() & 0xFFFF);
                    int height = (int)((lParam.ToInt64() >> 16) & 0xFFFF);

                    if (_useCoalescedMonitorReconciliation)
                    {
                        ScheduleMonitorReconciliation(false);
                    }
                    else
                    {
                        Task.Run(() => CheckForMonitorChanges());
                    }
                    break;
                }
            }

            return DefWindowProcW(hWnd, msg, wParam, lParam);
        }

        private void BackupMonitorsByRole(int monitorCount)
        {
            if (monitors == null || monitors.Count == 0)
            {
                return;
            }


            var sortedMonitors = monitors.OrderByDescending(m => m.IsPrimary).ThenBy(m => m.Index).ToList();

            if (!_monitorBackupsByCount.ContainsKey(monitorCount))
            {
                _monitorBackupsByCount[monitorCount] = new Dictionary<int, MonitorBackup>();
            }

            var backupsForCount = _monitorBackupsByCount[monitorCount];

            for (int position = 0; position < sortedMonitors.Count; position++)
            {
                var monitor = sortedMonitors[position];

                var backup = new MonitorBackup
                {
                    MonitorIndex = position,
                    IsPrimary = monitor.IsPrimary,
                    CurrentWorkspaceId = monitor.CurrentWorkspaceId,
                    MonitorReference = monitor,
                    Bounds = monitor.Bounds,
                    BackedUpAt = DateTime.Now,
                    MonitorCount = monitorCount
                };

                int totalWindows = 0;
                for (var i = 1; i <= Monitor.NO_OF_WORKSPACES; i++)
                {
                    var workspace = monitor.GetWorkspaceAtIndex(i);
                    if (workspace != null && workspace.WindowCount > 0)
                    {
                        backup.WorkspaceWindows[i] = workspace.GetAllWindows().ToList();
                        backup.WorkspaceStackedMode[i] = workspace.IsStackedMode;
                        totalWindows += workspace.WindowCount;
                    }
                }


                if (totalWindows == 0 && backupsForCount.ContainsKey(position))
                {
                    var existingBackup = backupsForCount[position];
                    int existingWindowCount = existingBackup.WorkspaceWindows.Sum(kvp => kvp.Value.Count);
                    if (existingWindowCount > 0)
                    {
                        continue; // skip this backup keep existing one
                    }
                }

                backupsForCount[position] = backup;
            }
        }

        private void RedistributeWindowsByMonitorRoles(int newMonitorCount)
        {

            int oldMonitorCount = _lastMonitorCount;


            var sortedMonitors = monitors.OrderByDescending(m => m.IsPrimary).ThenBy(m => m.Index).ToList();


            _backupWorkspacesPerMonitor.Clear();

            if (oldMonitorCount > newMonitorCount && _monitorBackupsByCount.ContainsKey(oldMonitorCount))
            {
                var backups = _monitorBackupsByCount[oldMonitorCount];

                // restore windows to positions that still exist
                for (int position = 0; position < sortedMonitors.Count; position++)
                {
                    var monitor = sortedMonitors[position];

                    if (backups.TryGetValue(position, out var backup) && backup.MonitorReference != null)
                    {
                        RestoreWindowsFromBackup(monitor, backup);
                    }
                    else
                    {
                        Logger.Info($"[REDISTRIBUTE]   No backup found for position {position}");
                    }
                }

                // consolidate windows from positions that no longer exist
                // E.g., 3→2: position 2 goes to position 1 (last monitor)
                // E.g., 4→2: positions 2,3 go to position 1
                // E.g., 3→1: positions 1,2 go to position 0
                if (oldMonitorCount > newMonitorCount)
                {
                    var lastMonitor = sortedMonitors[sortedMonitors.Count - 1];

                    for (int oldPosition = newMonitorCount; oldPosition < oldMonitorCount; oldPosition++)
                    {
                        if (backups.TryGetValue(oldPosition, out var backup) && backup.MonitorReference != null)
                        {
                            int windowCount = backup.WorkspaceWindows.Sum(w => w.Value.Count);
                            if (windowCount > 0)
                            {
                                ConsolidateWindowsToMonitor(lastMonitor, backup);
                            }
                        }
                    }
                }
            }
            FinalizeMonitorLayout();
        }
        private void RestoreWindowsByMonitorRoles(int newMonitorCount)
        {
            if (monitors == null || monitors.Count == 0)
            {
                return;
            }

            int oldMonitorCount = _lastMonitorCount;

            var sortedMonitors = monitors.OrderByDescending(m => m.IsPrimary).ThenBy(m => m.Index).ToList();

            foreach (var kvp in _monitorBackupsByCount)
            {
                foreach (var posBackup in kvp.Value)
                {
                    int windowCount = posBackup.Value.WorkspaceWindows.Sum(w => w.Value.Count);
                }
            }

            _backupWorkspacesPerMonitor.Clear();

            int backupCountToUse = newMonitorCount;

            if (_monitorBackupsByCount.ContainsKey(backupCountToUse))
            {
                var backups = _monitorBackupsByCount[backupCountToUse];

                for (int position = 0; position < sortedMonitors.Count; position++)
                {
                    var monitor = sortedMonitors[position];

                    if (backups.TryGetValue(position, out var backup) && backup.MonitorReference != null)
                    {
                        RestoreWindowsFromBackup(monitor, backup);
                    }
                }
            }
            else
            {
                if (oldMonitorCount == 0)
                {
                    if (_monitorBackupsByCount.ContainsKey(newMonitorCount))
                    {
                        var backups = _monitorBackupsByCount[newMonitorCount];
                        for (int position = 0; position < sortedMonitors.Count; position++)
                        {
                            var monitor = sortedMonitors[position];

                            if (backups.TryGetValue(position, out var backup) && backup.MonitorReference != null)
                            {
                                RestoreWindowsFromBackup(monitor, backup);
                            }
                        }
                    }
                    else
                    {
                        var availableBackup = _monitorBackupsByCount.OrderByDescending(kvp => kvp.Key).FirstOrDefault();
                        if (availableBackup.Value != null)
                        {
                            var backups = availableBackup.Value;

                            // restore as many positions as we have
                            for (int position = 0; position < sortedMonitors.Count && position < availableBackup.Key; position++)
                            {
                                var monitor = sortedMonitors[position];

                                if (backups.TryGetValue(position, out var backup))
                                {
                                    RestoreWindowsFromBackup(monitor, backup);
                                }
                            }
                        }
                    }
                }
                else if (oldMonitorCount < newMonitorCount && oldMonitorCount > 0)
                {
                    if (_monitorBackupsByCount.ContainsKey(oldMonitorCount))
                    {
                        var oldBackups = _monitorBackupsByCount[oldMonitorCount];

                        // restore to positions that existed in old configuration
                        for (int position = 0; position < oldMonitorCount && position < sortedMonitors.Count; position++)
                        {
                            var monitor = sortedMonitors[position];

                            if (oldBackups.TryGetValue(position, out var backup))
                            {
                                RestoreWindowsFromBackup(monitor, backup);
                            }
                        }
                    }
                }
            }
            FinalizeMonitorLayout();
        }

        private void RestoreWindowsFromBackup(Monitor targetMonitor, MonitorBackup backup)
        {
            if (backup.WorkspaceWindows == null || backup.WorkspaceWindows.Count == 0)
            {
                if (backup.MonitorReference == null)
                {
                    return;
                }

                for (var i = 1; i <= Monitor.NO_OF_WORKSPACES; i++)
                {
                    var workspace = backup.MonitorReference.GetWorkspaceAtIndex(i);
                    if (workspace != null && workspace.WindowCount > 0)
                    {
                        var windows = workspace.GetAllWindows().ToList();
                        foreach (var window in windows)
                        {
                            try
                            {
                                GetWindowRect(window, out RECT rect);

                                ShowWindow(window, SW_SHOW);

                                targetMonitor.AddWindowToWorkspace(window, i);
                            }
                            catch (Exception ex)
                            {
                                Logger.Error($"[RESTORE-WINDOWS] ERROR: Could not restore window 0x{window.ToInt64():X}: {ex.Message}");
                            }
                        }

                        // restore stacked mode state
                        if (workspace.IsStackedMode)
                        {
                            var targetWorkspace = targetMonitor.GetWorkspaceAtIndex(i);
                            if (targetWorkspace != null)
                            {
                                targetWorkspace.EnableStackedMode();
                            }
                        }
                    }
                }
            }
            else
            {
                int restoredCount = 0;

                foreach (var kvp in backup.WorkspaceWindows)
                {
                    int workspaceId = kvp.Key;
                    var windows = kvp.Value;

                    foreach (var window in windows)
                    {
                        try
                        {
                            GetWindowRect(window, out RECT rect);

                            ShowWindow(window, SW_SHOW);

                            targetMonitor.AddWindowToWorkspace(window, workspaceId);
                            restoredCount++;
                        }
                        catch (Exception ex)
                        {
                        }
                    }

                    // restore stacked mode state from backup
                    if (backup.WorkspaceStackedMode.TryGetValue(workspaceId, out bool isStackedMode) && isStackedMode)
                    {
                        var targetWorkspace = targetMonitor.GetWorkspaceAtIndex(workspaceId);
                        if (targetWorkspace != null)
                        {
                            targetWorkspace.EnableStackedMode();
                        }
                    }
                }
            }

            if (backup.CurrentWorkspaceId > 0 && backup.CurrentWorkspaceId != targetMonitor.CurrentWorkspaceId)
            {
                targetMonitor.SwitchToWorkspace(backup.CurrentWorkspaceId);
            }
        }

        private void ConsolidateWindowsToMonitor(Monitor targetMonitor, MonitorBackup backup)
        {
            if (backup.WorkspaceWindows == null || backup.WorkspaceWindows.Count == 0)
            {
                if (backup.MonitorReference == null) return;

                int targetWorkspaceId = 1;
                for (var i = 1; i <= Monitor.NO_OF_WORKSPACES; i++)
                {
                    var ws = targetMonitor.GetWorkspaceAtIndex(i);
                    if (ws != null && ws.WindowCount == 0)
                    {
                        targetWorkspaceId = i;
                        break;
                    }
                }

                if (!_backupWorkspacesPerMonitor.ContainsKey(targetMonitor.Index))
                {
                    _backupWorkspacesPerMonitor[targetMonitor.Index] = new HashSet<int>();
                }
                _backupWorkspacesPerMonitor[targetMonitor.Index].Add(targetWorkspaceId);

                foreach (var workspace in backup.MonitorReference.GetAllWorkspaces())
                {
                    if (workspace.WindowCount > 0)
                    {
                        var windows = workspace.GetAllWindows().ToList();
                        foreach (var window in windows)
                        {
                            try
                            {
                                GetWindowRect(window, out RECT rect);
                                ShowWindow(window, SW_SHOW);
                                targetMonitor.AddWindowToWorkspace(window, targetWorkspaceId);
                            }
                            catch (Exception ex)
                            {
                                Logger.Error($"ERROR: Window: {window}: {ex.Message}");
                            }
                        }
                    }
                }
            }
            else
            {
                int targetWorkspaceId = 1;
                for (var i = 1; i <= Monitor.NO_OF_WORKSPACES; i++)
                {
                    var ws = targetMonitor.GetWorkspaceAtIndex(i);
                    if (ws != null && ws.WindowCount == 0)
                    {
                        targetWorkspaceId = i;
                        break;
                    }
                }

                if (!_backupWorkspacesPerMonitor.ContainsKey(targetMonitor.Index))
                {
                    _backupWorkspacesPerMonitor[targetMonitor.Index] = new HashSet<int>();
                }
                _backupWorkspacesPerMonitor[targetMonitor.Index].Add(targetWorkspaceId);

                foreach (var kvp in backup.WorkspaceWindows)
                {
                    var windows = kvp.Value;
                    foreach (var window in windows)
                    {
                        try
                        {
                            GetWindowRect(window, out RECT rect);
                            ShowWindow(window, SW_SHOW);
                            targetMonitor.AddWindowToWorkspace(window, targetWorkspaceId);
                        }
                        catch (Exception ex)
                        {
                            Logger.Error($"ERROR: window: {window}: {ex.Message}");
                        }
                    }
                }
            }
        }

        private void FinalizeMonitorLayout()
        {
            if (monitors == null)
            {
                return;
            }

            foreach (var monitor in monitors)
            {
                ApplyTilingToAllMonitorWorkspaces(monitor);

                try
                {
                    for (var i = 1; i <= Monitor.NO_OF_WORKSPACES; i++)
                    {
                        var workspace = monitor.GetWorkspaceAtIndex(i);
                        int windowCount = workspace?.WindowCount ?? 0;

                        if (i == monitor.CurrentWorkspaceId)
                        {
                            ShowWorkspaceWindows(monitor, i);
                        }
                        else
                        {
                            HideWorkspaceWindows(monitor, i);
                        }
                    }

                    if (_backupWorkspacesPerMonitor.TryGetValue(monitor.Index, out var backupWorkspaces))
                    {
                        monitor.UpdateWorkspaceIndicator(backupWorkspaces);
                    }
                    else
                    {
                        monitor.UpdateWorkspaceIndicator();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"ERROR finalizing Monitor {monitor.Index}: {ex.Message}");
                }
            }
        }

        private string PtrToDeviceInterfaceName(nint lParam, int totalSize)
        {
            int offset = Marshal.SizeOf<DEV_BROADCAST_DEVICEINTERFACE_W_FIXED>();
            int chars = (totalSize - offset) / 2; 
            if (chars <= 1) return string.Empty;

            nint pName = nint.Add(lParam, offset);
            string raw = Marshal.PtrToStringUni(pName, chars) ?? string.Empty;
            return raw.TrimEnd('\0');
        }

        private nint RegisterForMonitorInterfaceNotifications(nint hwnd)
        {
            int fixedSize = Marshal.SizeOf<DEV_BROADCAST_DEVICEINTERFACE_W_FIXED>();
            nint buffer = Marshal.AllocHGlobal(fixedSize);
            try
            {
                var filter = new DEV_BROADCAST_DEVICEINTERFACE_W_FIXED
                {
                    dbcc_size = fixedSize,
                    dbcc_devicetype = DBT_DEVTYP_DEVICEINTERFACE,
                    dbcc_reserved = 0,
                    dbcc_classguid = GUID_DEVINTERFACE_MONITOR
                };
                Marshal.StructureToPtr(filter, buffer, false);

                return RegisterDeviceNotificationW(hwnd, buffer, DEVICE_NOTIFY_WINDOW_HANDLE);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        private void ScheduleMonitorReconciliation(bool isArrival)
        {
            int delay = isArrival
                ? Math.Min(BASE_STABILIZATION_DELAY_MS * (int)Math.Pow(2, _reconnectionAttempts), MAX_STABILIZATION_DELAY_MS)
                : COALESCE_DELAY_MS;

            lock (_monitorChangeDebounceLock)
            {
                if (_monitorChangeDebounceTimer == null)
                {
                    _monitorChangeDebounceTimer = new System.Threading.Timer(_ =>
                    {
                        try
                        {
                            PerformMonitorReconciliation();
                        }
                        catch (Exception ex)
                        {
                            Logger.Error($"Monitor reconciliation error: {ex.Message}");
                        }
                        finally
                        {
                            lock (_monitorChangeDebounceLock)
                            {
                                _monitorChangeDebounceTimer?.Dispose();
                                _monitorChangeDebounceTimer = null;
                            }
                        }
                    }, null, delay, System.Threading.Timeout.Infinite);
                }
                else
                {
                    _monitorChangeDebounceTimer.Change(delay, System.Threading.Timeout.Infinite);
                }
            }
        }

        private void PerformMonitorReconciliation()
        {
            int currentMonitorCount = monitors?.Count ?? 0;
            if (currentMonitorCount > 0)
            {
                BackupMonitorsByRole(currentMonitorCount);
            }

            if (monitors != null)
            {
                foreach (var monitor in monitors)
                {
                    foreach (var workspace in monitor.GetAllWorkspaces())
                    {
                        var windows = workspace.GetAllWindows().ToList();
                        foreach (var window in windows)
                        {
                            monitor.RemoveWindowTrackingPositions(window);
                            monitor.RemoveWindowFromAllWorkspaces(window);
                        }
                    }
                }
            }

            monitors = null;
            Thread.Sleep(300);
            InitializeMonitors();

            int newMonitorCount = monitors?.Count ?? 0;

            CleanupWorkspaceIndicators();
            Thread.Sleep(200);
            InitializeWorkspaceIndicators();

            if (newMonitorCount >= currentMonitorCount)
            {
                RestoreWindowsByMonitorRoles(newMonitorCount);
                _reconnectionAttempts = 0;
            }
            else
            {
                RedistributeWindowsByMonitorRoles(newMonitorCount);
                _reconnectionAttempts++;
            }

            _lastMonitorCount = newMonitorCount;
            Task.Run(() => CheckForMonitorChanges());
        }

        private void CheckForMonitorChanges()
        {
            try
            {
                var currentMonitors = new HashSet<string>();

                uint deviceNum = 0;
                DISPLAY_DEVICE device = new DISPLAY_DEVICE();
                device.cb = Marshal.SizeOf(device);

                while (EnumDisplayDevices(null, deviceNum, ref device, 0))
                {
                    if ((device.StateFlags & DisplayDeviceStateFlags.AttachedToDesktop) != 0)
                    {
                        currentMonitors.Add(device.DeviceName);
                    }
                    deviceNum++;
                }
                _lastKnownMonitors.Clear();
                foreach (var monitor in currentMonitors)
                {
                    _lastKnownMonitors.Add(monitor);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error checking monitor changes: {ex.Message}");
            }
        }

        private void UpdateMonitorList()
        {
            _lastKnownMonitors.Clear();

            uint deviceNum = 0;
            DISPLAY_DEVICE device = new DISPLAY_DEVICE();
            device.cb = Marshal.SizeOf(device);

            while (EnumDisplayDevices(null, deviceNum, ref device, 0))
            {
                if ((device.StateFlags & DisplayDeviceStateFlags.AttachedToDesktop) != 0)
                {
                    _lastKnownMonitors.Add(device.DeviceName);
                }
                deviceNum++;
            }
        }

        private void CleanupBackupWorkspaceIfEmpty(Monitor monitor, int workspaceId)
        {
            if (_backupWorkspacesPerMonitor.TryGetValue(monitor.Index, out var backupWorkspaces))
            {
                if (backupWorkspaces.Contains(workspaceId))
                {
                    backupWorkspaces.Remove(workspaceId);

                    if (backupWorkspaces.Count == 0)
                    {
                        _backupWorkspacesPerMonitor.Remove(monitor.Index);
                    }

                    if (_backupWorkspacesPerMonitor.TryGetValue(monitor.Index, out var remainingBackups))
                    {
                        monitor.UpdateWorkspaceIndicator(remainingBackups);
                    }
                    else
                    {
                        monitor.UpdateWorkspaceIndicator();
                    }
                }
            }
        }
    }
}