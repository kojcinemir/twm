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
using System.Runtime.InteropServices;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

namespace TilingWindowManager
{
    public class WorkspaceIndicator
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern ushort RegisterClassEx(ref WNDCLASSEX lpwcx);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterClass(string lpClassName, IntPtr hInstance);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CreateWindowEx(
            int dwExStyle, string lpClassName, string lpWindowName,
            uint dwStyle, int x, int y, int nWidth, int nHeight,
            IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool UpdateWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

        [DllImport("user32.dll")]
        private static extern bool TranslateMessage(ref MSG lpMsg);

        [DllImport("user32.dll")]
        private static extern IntPtr DispatchMessage(ref MSG lpMsg);

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("user32.dll")]
        private static extern uint SetTimer(IntPtr hWnd, uint nIDEvent, uint uElapse, IntPtr lpTimerFunc);

        [DllImport("user32.dll")]
        private static extern bool KillTimer(IntPtr hWnd, uint uIDEvent);

        [DllImport("user32.dll")]
        private static extern bool TrackMouseEvent(ref TRACKMOUSEEVENT lpEventTrack);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateSolidBrush(uint crColor);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("gdi32.dll")]
        private static extern uint SetBkColor(IntPtr hdc, uint crColor);

        [DllImport("gdi32.dll")]
        private static extern uint SetTextColor(IntPtr hdc, uint crColor);

        [DllImport("gdi32.dll")]
        private static extern int SetBkMode(IntPtr hdc, int iBkMode);

        [DllImport("user32.dll")]
        private static extern bool FillRect(IntPtr hDC, ref RECT lprc, IntPtr hbr);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int DrawText(IntPtr hdc, string lpchText, int cchText, ref RECT lprc, uint format);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr LoadCursor(IntPtr hInstance, int lpCursorName);

        [DllImport("user32.dll")]
        private static extern IntPtr SetCursor(IntPtr hCursor);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern bool DrawIconEx(IntPtr hdc, int xLeft, int yTop, IntPtr hIcon, int cxWidth, int cyHeight, uint istepIfAniCur, IntPtr hbrFlickerFreeDraw, uint diFlags);

        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, uint processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("psapi.dll", SetLastError = true)]
        private static extern uint GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule, System.Text.StringBuilder lpBaseName, uint nSize);

        [DllImport("shell32.dll")]
        private static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

        [DllImport("user32.dll")]
        private static extern IntPtr BeginPaint(IntPtr hwnd, ref PAINTSTRUCT lpPaint);

        [DllImport("user32.dll")]
        private static extern bool EndPaint(IntPtr hWnd, ref PAINTSTRUCT lpPaint);

        [DllImport("user32.dll")]
        private static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, bool bErase);

        [DllImport("user32.dll")]
        private static extern bool InvalidateRect(IntPtr hWnd, ref RECT lpRect, bool bErase);

        [DllImport("msimg32.dll")]
        private static extern bool AlphaBlend(IntPtr hdcDest, int nXOriginDest, int nYOriginDest,
            int nWidthDest, int nHeightDest, IntPtr hdcSrc, int nXOriginSrc, int nYOriginSrc,
            int nWidthSrc, int nHeightSrc, BLENDFUNCTION blendFunction);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr hdc);

        [DllImport("user32.dll")]
        private static extern void PostQuitMessage(int nExitCode);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll")]
        private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
        private static extern bool GetTextExtentPoint32(IntPtr hdc, string lpString, int c, out SIZE lpSize);

        [DllImport("gdi32.dll")]
        private static extern bool RoundRect(IntPtr hdc, int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidth, int nHeight);

        [DllImport("gdi32.dll")]
        private static extern bool Ellipse(IntPtr hdc, int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreatePen(int fnPenStyle, int nWidth, uint crColor);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        // windows api
        private const uint WS_VISIBLE = 0x10000000;
        private const uint WS_POPUP = 0x80000000;
        private const uint WS_CLIPSIBLINGS = 0x04000000;
        private const int WS_EX_TOOLWINDOW = 0x80;
        private const int WS_EX_TOPMOST = 0x8;
        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const int WS_EX_LAYERED = 0x00080000;
        private const int LWA_COLORKEY = 0x00000001;
        private const uint SWP_NOMOVE = 0x2;
        private const uint SWP_NOSIZE = 0x1;
        private const uint SWP_SHOWWINDOW = 0x40;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const int WM_PAINT = 0xF;
        private const int WM_DESTROY = 0x2;
        private const int WM_TIMER = 0x113;
        private const int WM_MOUSEMOVE = 0x200;
        private const int WM_MOUSELEAVE = 0x2A3;
        private const int WM_LBUTTONDOWN = 0x201;
        private const int WM_LBUTTONUP = 0x202;
        private const int WM_ACTIVATE = 0x0006;
        private const int WM_ACTIVATEAPP = 0x001C;
        private const int WM_KILLFOCUS = 0x0008;
        private const int WM_SHOWWINDOW = 0x0018;
        private const int TME_LEAVE = 0x2;
        private const int WM_USER = 0x0400;
        private const int WM_UPDATE_WORKSPACE = WM_USER + 1;
        private const int WM_SETCURSOR = 0x0020;
        private const int WM_GETICON = 0x007F;
        private const int WM_SETTINGCHANGE = 0x001A;
        private const int WM_ERASEBKGND = 0x0014;
        private const int ICON_SMALL = 0;
        private const int ICON_BIG = 1;
        private const int IDC_ARROW = 32512;

        private const uint DI_NORMAL = 0x0003;
        private const uint PROCESS_QUERY_INFORMATION = 0x0400;
        private const uint PROCESS_VM_READ = 0x0010;

        private const byte AC_SRC_OVER = 0x00;
        private const byte AC_SRC_ALPHA = 0x01;
        private class MonitorIndicatorData
        {
            public IntPtr WindowHandle { get; set; }
            public bool IsHovered { get; set; } = false;
            public bool IsPressed { get; set; } = false;
            public int HoveredWorkspace { get; set; } = -1;
            public int CurrentWorkspace { get; set; } = 1;
            public Dictionary<int, List<WorkspaceWindow>> WorkspaceWindows { get; set; } = new Dictionary<int, List<WorkspaceWindow>>();
            public HashSet<int> StackedModeWorkspaces { get; set; } = new HashSet<int>();
            public HashSet<int> BackupWorkspaces { get; set; } = new HashSet<int>();
            public HashSet<int> PausedWorkspaces { get; set; } = new HashSet<int>();
            public int WindowX { get; set; } = 0;
            public int WindowY { get; set; } = 0;
            public int WindowWidth { get; set; } = 620;
            public int WindowHeight { get; set; } = 90;
            public int CurrentStackedWindowIndex { get; set; } = 0;
            public int HoveredStackedAppIndex { get; set; } = -1;
        }
        public class WorkspaceWindow
        {
            public IntPtr Handle { get; set; }
            public string Title { get; set; } = "";
            public IntPtr Icon { get; set; }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WNDCLASSEX
        {
            public uint cbSize;
            public uint style;
            public IntPtr lpfnWndProc;
            public int cbClsExtra;
            public int cbWndExtra;
            public IntPtr hInstance;
            public IntPtr hIcon;
            public IntPtr hCursor;
            public IntPtr hbrBackground;
            public string lpszMenuName;
            public string lpszClassName;
            public IntPtr hIconSm;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MSG
        {
            public IntPtr hwnd;
            public uint message;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public POINT pt;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SIZE
        {
            public int cx;
            public int cy;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TRACKMOUSEEVENT
        {
            public uint cbSize;
            public uint dwFlags;
            public IntPtr hwndTrack;
            public uint dwHoverTime;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PAINTSTRUCT
        {
            public IntPtr hdc;
            public bool fErase;
            public RECT rcPaint;
            public bool fRestore;
            public bool fIncUpdate;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] rgbReserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct BLENDFUNCTION
        {
            public byte BlendOp;
            public byte BlendFlags;
            public byte SourceConstantAlpha;
            public byte AlphaFormat;
        }

        // Configuration instance
        private WorkspaceIndicatorConfiguration config = new WorkspaceIndicatorConfiguration();

        // Properties that access configuration values
        private int WORKSPACE_WIDTH => config.WorkspaceWidth;
        private int WORKSPACE_HEIGHT => config.WorkspaceHeight;
        private int WORKSPACE_MARGIN => config.WorkspaceMargin;
        private int ICON_SIZE => config.IconSize;
        private bool SHOW_ONLY_OCCUPIED_WORKSPACES => config.ShowOnlyOccupiedWorkspaces;
        private int STACKED_APP_ICON_SIZE => config.StackedAppIconSize;
        private int STACKED_APP_ITEM_WIDTH => config.ShowStackedAppTitle ? config.StackedAppItemWidth : config.StackedAppItemWidthIconOnly;
        private int STACKED_APP_TITLE_MAX_LENGTH => config.StackedAppTitleMaxLength;
        private int STACKED_APP_MARGIN => config.StackedAppMargin;
        private bool SHOW_STACKED_APP_TITLE => config.ShowStackedAppTitle;
        private uint ACTIVE_WORKSPACE_BORDER_COLOR => config.ActiveWorkspaceBorderColor;
        private uint BACKGROUND_COLOR => config.BackgroundColor;
        private uint ACTIVE_WORKSPACE_COLOR => config.ActiveWorkspaceColor;
        private uint HOVERED_WORKSPACE_COLOR => config.HoveredWorkspaceColor;
        private uint INACTIVE_WORKSPACE_COLOR => config.InactiveWorkspaceColor;
        private uint ACTIVE_WORKSPACE_TEXT_COLOR => config.ActiveWorkspaceTextColor;
        private uint INACTIVE_WORKSPACE_TEXT_COLOR => config.InactiveWorkspaceTextColor;
        private uint STACKED_MODE_WORKSPACE_COLOR => config.StackedModeWorkspaceColor;
        private uint STACKED_MODE_BORDER_COLOR => config.StackedModeBorderColor;
        private uint BACKUP_WORKSPACE_COLOR => config.BackupWorkspaceColor;
        private uint BACKUP_WORKSPACE_BORDER_COLOR => config.BackupWorkspaceBorderColor;
        private uint BACKUP_AND_STACKED_WORKSPACE_COLOR => config.BackupAndStackedWorkspaceColor;
        private uint BACKUP_AND_STACKED_BORDER_COLOR => config.BackupAndStackedBorderColor;
        private uint PAUSED_WORKSPACE_COLOR => config.PausedWorkspaceColor;
        private uint PAUSED_WORKSPACE_BORDER_COLOR => config.PausedWorkspaceBorderColor;
        private int OFFSET_FROM_TASKBAR_LEFT_EDGE => config.OffsetFromTaskbarLeftEdge;
        private byte ACTIVE_WORKSPACE_BORDER_OPACITY => config.ActiveWorkspaceBorderOpacity;
        private int WINDOWS10_OFFSET_FROM_TASKBAR_RIGHT_EDGE => config.Windows10OffsetFromTaskbarRightEdge;
        private bool USE_WINDOWS10_POSITIONING => config.UseWindows10Positioning;
        private uint STACKED_APP_BACKGROUND_COLOR => config.StackedAppBackgroundColor;
        private uint STACKED_APP_HOVER_COLOR => config.StackedAppHoverColor;
        private uint STACKED_APP_ACTIVE_COLOR => config.StackedAppActiveColor;
        private uint STACKED_APP_TEXT_COLOR => config.StackedAppTextColor;
        private uint STACKED_APP_ACTIVE_TEXT_COLOR => config.StackedAppActiveTextColor;
        private bool SHOW_STACKED_APP_NUMBERS => config.ShowStackedAppNumbers;
        private int STACKED_APP_NUMBER_BADGE_SIZE => config.StackedAppNumberBadgeSize;
        private uint STACKED_APP_NUMBER_BADGE_BG_COLOR => config.StackedAppNumberBadgeBackgroundColor;
        private uint STACKED_APP_NUMBER_BADGE_TEXT_COLOR => config.StackedAppNumberBadgeTextColor;
        private List<string> STACKED_WINDOW_SHORTCUT_LABELS => config.StackedWindowShortcutLabels;

        private Dictionary<int, MonitorIndicatorData> monitorIndicators = new Dictionary<int, MonitorIndicatorData>();
        private Thread uiThread = null!;
        private volatile bool isRunning = false;
        private readonly object lockObject = new object();
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private string windowClassName = "";
        private IntPtr moduleHandle = IntPtr.Zero;


        public event Action<int, int>? WorkspaceClicked; // (monitorIndex, workspaceId)
        public event Action<int, int>? StackedAppClicked; // (monitorIndex, stackedAppIndex)
        private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
        private WndProcDelegate wndProcDelegate = null!;

        public void Initialize(List<Monitor> monitors)
        {
            if (isRunning)
            {
                Cleanup();
            }

            config.LoadConfiguration();

            monitorIndicators.Clear();

            foreach (var monitor in monitors)
            {
                var indicatorData = new MonitorIndicatorData();
                for (int i = 1; i <= Monitor.NO_OF_WORKSPACES; i++)
                {
                    indicatorData.WorkspaceWindows[i] = new List<WorkspaceWindow>();
                }
                monitorIndicators[monitor.Index] = indicatorData;
            }

            isRunning = true;
            uiThread = new Thread(() => RunUIThread(monitors))
            {
                IsBackground = true,
                Name = "WorkspaceIndicatorUI"
            };
            uiThread.SetApartmentState(ApartmentState.STA);
            uiThread.Start();
        }

        public static uint RgbToBgr(uint rgb)
        {
            uint r = (rgb & 0xFF0000) >> 16;
            uint g = (rgb & 0x00FF00);
            uint b = (rgb & 0x0000FF) << 16;

            return (b | g | r);
        }

        private void DrawAlphaBlendedRect(IntPtr hdc, RECT rect, uint color, byte alpha)
        {
            if (alpha == 255)
            {
                // full opacity, normal drawing
                IntPtr brush = CreateSolidBrush(RgbToBgr(color));
                FillRect(hdc, ref rect, brush);
                DeleteObject(brush);
                return;
            }

            IntPtr memDC = CreateCompatibleDC(hdc);
            IntPtr bitmap = CreateCompatibleBitmap(hdc, rect.Right - rect.Left, rect.Bottom - rect.Top);
            IntPtr oldBitmap = SelectObject(memDC, bitmap);

            IntPtr memBrush = CreateSolidBrush(RgbToBgr(color));
            var memRect = new RECT { Left = 0, Top = 0, Right = rect.Right - rect.Left, Bottom = rect.Bottom - rect.Top };
            FillRect(memDC, ref memRect, memBrush);
            DeleteObject(memBrush);

            BLENDFUNCTION blendFunc = new BLENDFUNCTION
            {
                BlendOp = AC_SRC_OVER,
                BlendFlags = 0,
                SourceConstantAlpha = alpha,
                AlphaFormat = 0
            };

            AlphaBlend(hdc, rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top,
                      memDC, 0, 0, rect.Right - rect.Left, rect.Bottom - rect.Top, blendFunc);

            // clean up
            SelectObject(memDC, oldBitmap);
            DeleteObject(bitmap);
            DeleteDC(memDC);
        }
        private void RunUIThread(List<Monitor> monitors)
        {
            try
            {
                wndProcDelegate = WndProc;
                moduleHandle = GetModuleHandle(null!);
                windowClassName = $"WorkspaceIndicatorClass_{Environment.TickCount}_{Thread.CurrentThread.ManagedThreadId}";

                var wndClass = new WNDCLASSEX
                {
                    cbSize = (uint)Marshal.SizeOf<WNDCLASSEX>(),
                    style = 0,
                    lpfnWndProc = Marshal.GetFunctionPointerForDelegate(wndProcDelegate),
                    cbClsExtra = 0,
                    cbWndExtra = 0,
                    hInstance = moduleHandle,
                    hIcon = IntPtr.Zero,
                    hCursor = LoadCursor(IntPtr.Zero, IDC_ARROW),
                    hbrBackground = IntPtr.Zero, // no automatic background erase - we handle all painting
                    lpszMenuName = null!,
                    lpszClassName = windowClassName,
                    hIconSm = IntPtr.Zero
                };

                ushort classAtom = RegisterClassEx(ref wndClass);
                if (classAtom == 0)
                {
                    int error = Marshal.GetLastWin32Error();
                    if (error == 1410) // class already exists
                    {
                        UnregisterClass(windowClassName, moduleHandle);
                        classAtom = RegisterClassEx(ref wndClass);
                    }

                    if (classAtom == 0)
                    {
                        error = Marshal.GetLastWin32Error();
                        Logger.Error($"Failed to register workspace indicator window class. Error: {error}");
                        return;
                    }
                }

                CreateIndicatorsForMonitors(monitors);

                MSG msg;
                while (isRunning && GetMessage(out msg, IntPtr.Zero, 0, 0))
                {
                    TranslateMessage(ref msg);
                    DispatchMessage(ref msg);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in workspace indicator UI thread: {ex.Message}");
            }
        }

        private void CreateIndicatorsForMonitors(List<Monitor> monitors)
        {
            SetProcessDPIAware();

            for (int i = 0; i < monitors.Count; i++)
            {
                var monitor = monitors[i];
                var indicatorData = monitorIndicators[monitor.Index];

                var taskbar = FindTaskbarForMonitor(monitor);
                if (taskbar == IntPtr.Zero)
                {
                    Logger.Error($"taskbar not found for monitor {monitor.Index}");
                    continue;
                }

                RECT taskbarRect;
                GetClientRect(taskbar, out taskbarRect);

                int windowWidth = CalculateIndicatorWidth(indicatorData);
                indicatorData.WindowWidth = windowWidth;
                indicatorData.WindowHeight = taskbarRect.Bottom - taskbarRect.Top;

                int xPos;
                if (USE_WINDOWS10_POSITIONING)
                {
                    // windows 10: center position
                    xPos = (taskbarRect.Right - windowWidth) / 2;
                }
                else
                {
                    // windows 11: position from left edge
                    xPos = OFFSET_FROM_TASKBAR_LEFT_EDGE;
                }

                indicatorData.WindowHandle = CreateWindowEx(
                    WS_EX_NOACTIVATE | WS_EX_LAYERED | WS_EX_TOOLWINDOW | WS_EX_TOPMOST,
                    windowClassName,
                    $"Workspace Indicator M{monitor.Index}",
                    (uint)(WS_VISIBLE | WS_CLIPSIBLINGS | WS_POPUP),
                    xPos, 0,
                    windowWidth,
                    taskbarRect.Bottom - taskbarRect.Top,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    moduleHandle,
                    IntPtr.Zero
                );

                if (indicatorData.WindowHandle == IntPtr.Zero)
                {
                    int error = Marshal.GetLastWin32Error();
                    Logger.Error($"Failed to create workspace indicator window for monitor {monitor.Index}. Error: {error}");
                    continue;
                }

                SetParent(indicatorData.WindowHandle, taskbar);
                SetLayeredWindowAttributes(indicatorData.WindowHandle, GetTransparencyColorKey(), 0, LWA_COLORKEY);

                ShowWindow(indicatorData.WindowHandle, 1);
                UpdateWindow(indicatorData.WindowHandle);
            }
        }

        private IntPtr FindTaskbarForMonitor(Monitor monitor)
        {
            IntPtr taskbarHandle = IntPtr.Zero;

            EnumWindows((hWnd, lParam) =>
            {
                var className = new System.Text.StringBuilder(256);
                GetClassName(hWnd, className, className.Capacity);

                if (className.ToString() == "Shell_TrayWnd" || className.ToString() == "Shell_SecondaryTrayWnd")
                {
                    GetWindowRect(hWnd, out RECT rect);

                    int centerX = rect.Left + (rect.Right - rect.Left) / 2;
                    int centerY = rect.Top + (rect.Bottom - rect.Top) / 2;

                    if (centerX >= monitor.Bounds.Left && centerX < monitor.Bounds.Right &&
                        centerY >= monitor.Bounds.Top && centerY < monitor.Bounds.Bottom)
                    {
                        taskbarHandle = hWnd;
                        return false; 
                    }
                }
                return true; // continue enumeration
            }, IntPtr.Zero);

            return taskbarHandle;
        }

        private uint GetTransparencyColorKey()
        {
            return 0x000000; 
        }

        private static bool AreWindowListsEqual(List<WorkspaceWindow>? existing, List<IntPtr> handles)
        {
            if ((existing == null || existing.Count == 0) && (handles == null || handles.Count == 0))
                return true;

            if (existing == null || handles == null)
                return false;

            if (existing.Count != handles.Count)
                return false;

            for (int i = 0; i < handles.Count; i++)
            {
                if (existing[i].Handle != handles[i])
                    return false;
            }

            return true;
        }

        public void UpdateMonitor(int monitorIndex, int currentWorkspaceId, List<Workspace> workspaces)
        {
            var workspaceHandles = new Dictionary<int, List<IntPtr>>();
            var stackedModeWorkspaces = new HashSet<int>();
            var pausedWorkspaces = new HashSet<int>();

            foreach (var workspace in workspaces)
            {
                workspaceHandles[workspace.Id] = workspace.GetAllWindows();
                if (workspace.IsStackedMode)
                {
                    stackedModeWorkspaces.Add(workspace.Id);
                }
                if (workspace.IsPaused)
                {
                    pausedWorkspaces.Add(workspace.Id);
                }
            }

            UpdateMonitorInternal(monitorIndex, currentWorkspaceId, workspaces, workspaceHandles, stackedModeWorkspaces, null, pausedWorkspaces);
        }

        public void UpdateMonitor(int monitorIndex, int currentWorkspaceId, List<Workspace> workspaces, HashSet<int> backupWorkspaces)
        {
            var workspaceHandles = new Dictionary<int, List<IntPtr>>();
            var stackedModeWorkspaces = new HashSet<int>();
            var pausedWorkspaces = new HashSet<int>();

            foreach (var workspace in workspaces)
            {
                workspaceHandles[workspace.Id] = workspace.GetAllWindows();
                if (workspace.IsStackedMode)
                {
                    stackedModeWorkspaces.Add(workspace.Id);
                }
                if (workspace.IsPaused)
                {
                    pausedWorkspaces.Add(workspace.Id);
                }
            }

            UpdateMonitorInternal(monitorIndex, currentWorkspaceId, workspaces, workspaceHandles, stackedModeWorkspaces, backupWorkspaces, pausedWorkspaces);
        }

        private void UpdateMonitorInternal(int monitorIndex, int currentWorkspaceId, List<Workspace> workspaces,
            Dictionary<int, List<IntPtr>> workspaceHandles, HashSet<int> stackedModeWorkspaces, HashSet<int>? backupWorkspaces, HashSet<int> pausedWorkspaces)
        {
            bool stateChanged = false;
            IntPtr indicatorHandle = IntPtr.Zero;
            var changedWorkspaces = new List<int>();
            bool needsResize = false;
            int newWidth = 0;
            int currentHeight = 0;

            lock (lockObject)
            {
                if (monitorIndicators.TryGetValue(monitorIndex, out var indicatorData))
                {
                    indicatorHandle = indicatorData.WindowHandle;

                    if (indicatorData.CurrentWorkspace != currentWorkspaceId)
                    {
                        stateChanged = true;
                    }

                    // check if current stacked window index changed
                    var currentWS = workspaces.FirstOrDefault(w => w.Id == currentWorkspaceId);
                    if (currentWS != null && currentWS.IsStackedMode)
                    {
                        int newStackedIndex = currentWS.GetCurrentStackedWindowIndex();
                        if (indicatorData.CurrentStackedWindowIndex != newStackedIndex)
                        {
                            stateChanged = true;
                        }
                    }

                    // check if stacked mode changed for any workspace
                    if (!indicatorData.StackedModeWorkspaces.SetEquals(stackedModeWorkspaces))
                    {
                        stateChanged = true;
                        indicatorData.StackedModeWorkspaces = stackedModeWorkspaces;
                    }

                    // check if backup workspaces changed
                    var newBackupWorkspaces = backupWorkspaces ?? new HashSet<int>();
                    if (!indicatorData.BackupWorkspaces.SetEquals(newBackupWorkspaces))
                    {
                        stateChanged = true;
                        indicatorData.BackupWorkspaces = newBackupWorkspaces;
                    }

                    // check if paused workspaces changed
                    if (!indicatorData.PausedWorkspaces.SetEquals(pausedWorkspaces))
                    {
                        stateChanged = true;
                        indicatorData.PausedWorkspaces = pausedWorkspaces;
                    }

                    foreach (var kvp in workspaceHandles)
                    {
                        indicatorData.WorkspaceWindows.TryGetValue(kvp.Key, out var existingList);
                        if (!AreWindowListsEqual(existingList, kvp.Value))
                        {
                            stateChanged = true;
                            changedWorkspaces.Add(kvp.Key);
                        }
                    }

                    if (stateChanged)
                    {
                        indicatorData.CurrentWorkspace = currentWorkspaceId;

                        // Update current stacked window index if current workspace is in stacked mode
                        var currentWorkspace = workspaces.FirstOrDefault(w => w.Id == currentWorkspaceId);
                        if (currentWorkspace != null && currentWorkspace.IsStackedMode)
                        {
                            indicatorData.CurrentStackedWindowIndex = currentWorkspace.GetCurrentStackedWindowIndex();
                        }
                        else
                        {
                            indicatorData.CurrentStackedWindowIndex = 0;
                        }

                        foreach (var workspaceId in changedWorkspaces)
                        {
                            if (indicatorData.WorkspaceWindows.TryGetValue(workspaceId, out var oldList) && oldList != null)
                            {
                                foreach (var window in oldList)
                                {
                                    if (window.Icon != IntPtr.Zero)
                                    {
                                        DestroyIcon(window.Icon);
                                    }
                                }
                            }

                            var newWindows = new List<WorkspaceWindow>();
                            foreach (var handle in workspaceHandles[workspaceId])
                            {
                                var window = CreateWorkspaceWindow(handle);
                                if (window != null)
                                {
                                    newWindows.Add(window);
                                }
                            }

                            indicatorData.WorkspaceWindows[workspaceId] = newWindows;
                        }

                        foreach (var kvp in indicatorData.WorkspaceWindows)
                        {
                            if (!workspaceHandles.ContainsKey(kvp.Key) && kvp.Value.Count > 0)
                            {
                                foreach (var window in kvp.Value)
                                {
                                    if (window.Icon != IntPtr.Zero)
                                    {
                                        DestroyIcon(window.Icon);
                                    }
                                }
                                kvp.Value.Clear();
                            }
                        }

                        // check if window width needs to change due to stacked mode
                        newWidth = CalculateIndicatorWidth(indicatorData);
                        if (newWidth != indicatorData.WindowWidth)
                        {
                            Logger.Info($"Workspace indicator resizing: {indicatorData.WindowWidth} -> {newWidth} (workspace {currentWorkspaceId}, stacked: {indicatorData.StackedModeWorkspaces.Contains(currentWorkspaceId)})");
                            needsResize = true;
                            currentHeight = indicatorData.WindowHeight;
                            indicatorData.WindowWidth = newWidth;
                        }
                    }
                }
            }

            // perform window resize outside the lock to avoid holding lock during Windows API call
            if (needsResize && indicatorHandle != IntPtr.Zero)
            {
                const uint SWP_NOZORDER = 0x0004;
                const uint SWP_NOMOVE = 0x0002;
                const uint SWP_NOACTIVATE = 0x0010;
                const uint SWP_SHOWWINDOW = 0x0040;

                SetWindowPos(indicatorHandle, IntPtr.Zero,
                    0, 0, newWidth, currentHeight,
                    SWP_SHOWWINDOW | SWP_NOACTIVATE | SWP_NOZORDER | SWP_NOMOVE);
            }

            if (stateChanged && indicatorHandle != IntPtr.Zero)
            {
                PostMessage(indicatorHandle, WM_UPDATE_WORKSPACE, IntPtr.Zero, IntPtr.Zero);
            }
        }

        private WorkspaceWindow? CreateWorkspaceWindow(IntPtr handle)
        {
            try
            {
                var title = new System.Text.StringBuilder(256);
                GetWindowText(handle, title, title.Capacity);

                IntPtr icon = GetWindowIcon(handle);

                return new WorkspaceWindow
                {
                    Handle = handle,
                    Title = title.ToString(),
                    Icon = icon
                };
            }
            catch
            {
                return null;
            }
        }

        private IntPtr GetWindowIcon(IntPtr hwnd)
        {
            IntPtr icon = SendMessage(hwnd, WM_GETICON, new IntPtr(ICON_SMALL), IntPtr.Zero);
            if (icon != IntPtr.Zero)
                return icon;

            icon = SendMessage(hwnd, WM_GETICON, new IntPtr(ICON_BIG), IntPtr.Zero);
            if (icon != IntPtr.Zero)
                return icon;

            try
            {
                GetWindowThreadProcessId(hwnd, out uint processId);
                IntPtr processHandle = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, false, processId);

                if (processHandle != IntPtr.Zero)
                {
                    var exePath = new System.Text.StringBuilder(260);
                    if (GetModuleFileNameEx(processHandle, IntPtr.Zero, exePath, 260) > 0)
                    {
                        icon = ExtractIcon(GetModuleHandle(null!), exePath.ToString(), 0);
                    }
                    CloseHandle(processHandle);
                }
            }
            catch
            {
            }

            return icon;
        }

        private bool ShouldShowWorkspace(int workspaceId, int currentWorkspace, Dictionary<int, List<WorkspaceWindow>> workspaceWindows)
        {
            if (!SHOW_ONLY_OCCUPIED_WORKSPACES)
                return true;

            // Always show current workspace
            if (workspaceId == currentWorkspace)
                return true;

            // Show if workspace has windows
            if (workspaceWindows.TryGetValue(workspaceId, out var windows) && windows != null && windows.Count > 0)
                return true;

            return false;
        }

        private List<int> GetVisibleWorkspaces(int currentWorkspace, Dictionary<int, List<WorkspaceWindow>> workspaceWindows)
        {
            var visibleWorkspaces = new List<int>();
            for (int i = 1; i <= Monitor.NO_OF_WORKSPACES; i++)
            {
                if (ShouldShowWorkspace(i, currentWorkspace, workspaceWindows))
                {
                    visibleWorkspaces.Add(i);
                }
            }
            return visibleWorkspaces;
        }

        private int CalculateIndicatorWidth(MonitorIndicatorData indicatorData)
        {
            var visibleWorkspaces = GetVisibleWorkspaces(indicatorData.CurrentWorkspace, indicatorData.WorkspaceWindows);
            int baseWidth = (WORKSPACE_WIDTH + WORKSPACE_MARGIN) * visibleWorkspaces.Count + WORKSPACE_MARGIN;

            // check if current workspace is in stacked mode and has windows
            if (indicatorData.StackedModeWorkspaces.Contains(indicatorData.CurrentWorkspace))
            {
                if (indicatorData.WorkspaceWindows.TryGetValue(indicatorData.CurrentWorkspace, out var windows) && windows != null && windows.Count > 0)
                {
                    int stackedAppWidth = windows.Count * (STACKED_APP_ITEM_WIDTH + STACKED_APP_MARGIN) + STACKED_APP_MARGIN;
                    return baseWidth + stackedAppWidth;
                }
            }

            return baseWidth;
        }

        private RECT GetWorkspaceRect(IntPtr windowHandle, int workspaceId, MonitorIndicatorData indicatorData)
        {
            if (workspaceId < 1 || workspaceId > Monitor.NO_OF_WORKSPACES)
                return new RECT();

            RECT clientRect;
            GetClientRect(windowHandle, out clientRect);

            var visibleWorkspaces = GetVisibleWorkspaces(indicatorData.CurrentWorkspace, indicatorData.WorkspaceWindows);
            int visibleIndex = visibleWorkspaces.IndexOf(workspaceId);

            if (visibleIndex < 0)
                return new RECT(); // Workspace is not visible

            int x = WORKSPACE_MARGIN + (visibleIndex * (WORKSPACE_WIDTH + WORKSPACE_MARGIN));
            int y = 5;

            return new RECT
            {
                Left = x,
                Top = y,
                Right = x + WORKSPACE_WIDTH,
                Bottom = clientRect.Bottom - 5
            };
        }

        private RECT GetStackedAppRect(IntPtr windowHandle, int appIndex, int totalApps, MonitorIndicatorData indicatorData)
        {
            if (appIndex < 0 || appIndex >= totalApps)
                return new RECT();

            RECT clientRect;
            GetClientRect(windowHandle, out clientRect);

            var visibleWorkspaces = GetVisibleWorkspaces(indicatorData.CurrentWorkspace, indicatorData.WorkspaceWindows);
            int baseX = WORKSPACE_MARGIN + (visibleWorkspaces.Count * (WORKSPACE_WIDTH + WORKSPACE_MARGIN)) + STACKED_APP_MARGIN;
            int y = 5;
            int itemX = baseX + (appIndex * (STACKED_APP_ITEM_WIDTH + STACKED_APP_MARGIN));

            int leftPadding = (appIndex > 0) ? STACKED_APP_MARGIN : 0;
            int rightPadding = (appIndex < totalApps - 1) ? STACKED_APP_MARGIN : 0;

            return new RECT
            {
                Left = itemX - leftPadding,
                Top = y,
                Right = itemX + STACKED_APP_ITEM_WIDTH + rightPadding,
                Bottom = clientRect.Bottom - 5
            };
        }

        private (int monitorIndex, int workspaceId) GetWorkspaceAtPosition(IntPtr windowHandle, int x, int y)
        {
            foreach (var kvp in monitorIndicators)
            {
                if (kvp.Value.WindowHandle == windowHandle)
                {
                    int monitorIndex = kvp.Key;
                    var indicatorData = kvp.Value;

                    var visibleWorkspaces = GetVisibleWorkspaces(indicatorData.CurrentWorkspace, indicatorData.WorkspaceWindows);

                    for (int i = 0; i < visibleWorkspaces.Count; i++)
                    {
                        int workspaceX = WORKSPACE_MARGIN + (i * (WORKSPACE_WIDTH + WORKSPACE_MARGIN));
                        int workspaceY = 5;

                        if (x >= workspaceX && x < workspaceX + WORKSPACE_WIDTH &&
                            y >= workspaceY)
                        {
                            return (monitorIndex, visibleWorkspaces[i]);
                        }
                    }
                    break;
                }
            }
            return (-1, -1);
        }

        private (int monitorIndex, int stackedAppIndex) GetStackedAppAtPosition(IntPtr windowHandle, int x, int y)
        {
            foreach (var kvp in monitorIndicators)
            {
                if (kvp.Value.WindowHandle == windowHandle)
                {
                    int monitorIndex = kvp.Key;
                    var indicatorData = kvp.Value;

                    // check if current workspace is in stacked mode
                    if (!indicatorData.StackedModeWorkspaces.Contains(indicatorData.CurrentWorkspace))
                        return (-1, -1);

                    // get stacked windows for current workspace
                    if (!indicatorData.WorkspaceWindows.TryGetValue(indicatorData.CurrentWorkspace, out var windows) || windows == null || windows.Count == 0)
                        return (-1, -1);

                    var visibleWorkspaces = GetVisibleWorkspaces(indicatorData.CurrentWorkspace, indicatorData.WorkspaceWindows);
                    int baseX = WORKSPACE_MARGIN + (visibleWorkspaces.Count * (WORKSPACE_WIDTH + WORKSPACE_MARGIN)) + STACKED_APP_MARGIN;
                    int itemY = 5;

                    for (int i = 0; i < windows.Count; i++)
                    {
                        int itemX = baseX + (i * (STACKED_APP_ITEM_WIDTH + STACKED_APP_MARGIN));

                        if (x >= itemX && x < itemX + STACKED_APP_ITEM_WIDTH &&
                            y >= itemY)
                        {
                            return (monitorIndex, i);
                        }
                    }
                    break;
                }
            }
            return (-1, -1);
        }

        private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            int currentMonitorIndex = -1;
            MonitorIndicatorData currentIndicatorData = null;

            foreach (var kvp in monitorIndicators)
            {
                if (kvp.Value.WindowHandle == hWnd)
                {
                    currentMonitorIndex = kvp.Key;
                    currentIndicatorData = kvp.Value;
                    break;
                }
            }

            if (currentIndicatorData == null)
                return DefWindowProc(hWnd, msg, wParam, lParam);

            switch (msg)
            {
                case WM_ERASEBKGND:
                    // prevent automatic background erase to avoid flickering
                    return new IntPtr(1);

                case WM_PAINT:
                    HandlePaint(hWnd, currentMonitorIndex, currentIndicatorData);
                    break;

                case WM_UPDATE_WORKSPACE:
                    InvalidateRect(hWnd, IntPtr.Zero, false);
                    break;

                case WM_SETTINGCHANGE:
                    // reload configuration and redraw when system settings change
                    config.LoadConfiguration();
                    SetLayeredWindowAttributes(hWnd, GetTransparencyColorKey(), 0, LWA_COLORKEY);
                    InvalidateRect(hWnd, IntPtr.Zero, false);
                    return IntPtr.Zero;

                case WM_ACTIVATE:
                case WM_ACTIVATEAPP:
                case WM_KILLFOCUS:
                    return IntPtr.Zero;

                case WM_SHOWWINDOW:
                    if (wParam == IntPtr.Zero)
                    {
                        ShowWindow(hWnd, 1);
                        return IntPtr.Zero;
                    }
                    break;

                case WM_SETCURSOR:
                    SetCursor(LoadCursor(IntPtr.Zero, IDC_ARROW));
                    return new IntPtr(1);

                case WM_MOUSEMOVE:
                    int x = (int)(lParam.ToInt64() & 0xFFFF);
                    int y = (int)((lParam.ToInt64() >> 16) & 0xFFFF);

                    var (_, newHoveredStackedApp) = GetStackedAppAtPosition(hWnd, x, y);
                    bool stackedAppHoverChanged = newHoveredStackedApp != currentIndicatorData.HoveredStackedAppIndex;

                    var (_, newHoveredWorkspace) = GetWorkspaceAtPosition(hWnd, x, y);
                    bool workspaceHoverChanged = newHoveredWorkspace != currentIndicatorData.HoveredWorkspace;

                    if (stackedAppHoverChanged)
                    {
                        int stackedAppCount = 0;
                        if (currentIndicatorData.StackedModeWorkspaces.Contains(currentIndicatorData.CurrentWorkspace))
                        {
                            if (currentIndicatorData.WorkspaceWindows.TryGetValue(currentIndicatorData.CurrentWorkspace, out var windows))
                            {
                                stackedAppCount = windows?.Count ?? 0;
                            }
                        }

                        if (stackedAppCount > 0)
                        {
                            bool hasOld = currentIndicatorData.HoveredStackedAppIndex >= 0;
                            bool hasNew = newHoveredStackedApp >= 0;

                            if (hasOld && hasNew)
                            {
                                var oldRect = GetStackedAppRect(hWnd, currentIndicatorData.HoveredStackedAppIndex, stackedAppCount, currentIndicatorData);
                                var newRect = GetStackedAppRect(hWnd, newHoveredStackedApp, stackedAppCount, currentIndicatorData);

                                var combinedRect = new RECT
                                {
                                    Left = Math.Min(oldRect.Left, newRect.Left),
                                    Top = Math.Min(oldRect.Top, newRect.Top),
                                    Right = Math.Max(oldRect.Right, newRect.Right),
                                    Bottom = Math.Max(oldRect.Bottom, newRect.Bottom)
                                };
                                InvalidateRect(hWnd, ref combinedRect, false);
                            }
                            else if (hasOld)
                            {
                                var oldRect = GetStackedAppRect(hWnd, currentIndicatorData.HoveredStackedAppIndex, stackedAppCount, currentIndicatorData);
                                InvalidateRect(hWnd, ref oldRect, false);
                            }
                            else if (hasNew)
                            {
                                var newRect = GetStackedAppRect(hWnd, newHoveredStackedApp, stackedAppCount, currentIndicatorData);
                                InvalidateRect(hWnd, ref newRect, false);
                            }
                        }

                        currentIndicatorData.HoveredStackedAppIndex = newHoveredStackedApp;
                    }

                    if (workspaceHoverChanged)
                    {
                        if (currentIndicatorData.HoveredWorkspace > 0)
                        {
                            var oldRect = GetWorkspaceRect(hWnd, currentIndicatorData.HoveredWorkspace, currentIndicatorData);
                            InvalidateRect(hWnd, ref oldRect, false);
                        }

                        if (newHoveredWorkspace > 0)
                        {
                            var newRect = GetWorkspaceRect(hWnd, newHoveredWorkspace, currentIndicatorData);
                            InvalidateRect(hWnd, ref newRect, false);
                        }

                        currentIndicatorData.HoveredWorkspace = newHoveredWorkspace;
                    }

                    if (!currentIndicatorData.IsHovered)
                    {
                        currentIndicatorData.IsHovered = true;
                        SetCursor(LoadCursor(IntPtr.Zero, IDC_ARROW));

                        var tme = new TRACKMOUSEEVENT
                        {
                            cbSize = (uint)Marshal.SizeOf<TRACKMOUSEEVENT>(),
                            dwFlags = TME_LEAVE,
                            hwndTrack = hWnd,
                            dwHoverTime = 0
                        };
                        TrackMouseEvent(ref tme);
                    }
                    return IntPtr.Zero;

                case WM_MOUSELEAVE:
                    currentIndicatorData.IsHovered = false;
                    currentIndicatorData.IsPressed = false;

                    if (currentIndicatorData.HoveredWorkspace > 0)
                    {
                        var rect = GetWorkspaceRect(hWnd, currentIndicatorData.HoveredWorkspace, currentIndicatorData);
                        InvalidateRect(hWnd, ref rect, false);
                        currentIndicatorData.HoveredWorkspace = -1;
                    }

                    if (currentIndicatorData.HoveredStackedAppIndex >= 0)
                    {
                        int stackedAppCount = 0;
                        if (currentIndicatorData.StackedModeWorkspaces.Contains(currentIndicatorData.CurrentWorkspace))
                        {
                            if (currentIndicatorData.WorkspaceWindows.TryGetValue(currentIndicatorData.CurrentWorkspace, out var windows))
                            {
                                stackedAppCount = windows?.Count ?? 0;
                            }
                        }

                        if (stackedAppCount > 0)
                        {
                            var rect = GetStackedAppRect(hWnd, currentIndicatorData.HoveredStackedAppIndex, stackedAppCount, currentIndicatorData);
                            InvalidateRect(hWnd, ref rect, false);
                        }
                        currentIndicatorData.HoveredStackedAppIndex = -1;
                    }
                    break;

                case WM_LBUTTONDOWN:
                    currentIndicatorData.IsPressed = true;

                    // only invalidate the element being clicked, not the entire window
                    GetCursorPos(out POINT downPos);
                    ScreenToClient(hWnd, ref downPos);

                    var (_, clickedStackedApp) = GetStackedAppAtPosition(hWnd, downPos.x, downPos.y);
                    if (clickedStackedApp >= 0)
                    {
                        // clicking on stacked app
                        int count = 0;
                        if (currentIndicatorData.WorkspaceWindows.TryGetValue(currentIndicatorData.CurrentWorkspace, out var wins))
                            count = wins?.Count ?? 0;

                        if (count > 0)
                        {
                            var rect = GetStackedAppRect(hWnd, clickedStackedApp, count, currentIndicatorData);
                            InvalidateRect(hWnd, ref rect, false);
                        }
                    }
                    else
                    {
                        // clicking on workspace
                        var (_, clickedWS) = GetWorkspaceAtPosition(hWnd, downPos.x, downPos.y);
                        if (clickedWS > 0)
                        {
                            var rect = GetWorkspaceRect(hWnd, clickedWS, currentIndicatorData);
                            InvalidateRect(hWnd, ref rect, false);
                        }
                    }
                    break;

                case WM_LBUTTONUP:
                    if (currentIndicatorData.IsPressed)
                    {
                        currentIndicatorData.IsPressed = false;

                        GetCursorPos(out POINT cursorPos);
                        ScreenToClient(hWnd, ref cursorPos);

                        var (stackedMonitorIndex, stackedAppIndex) = GetStackedAppAtPosition(hWnd, cursorPos.x, cursorPos.y);
                        if (stackedAppIndex >= 0)
                        {
                            ThreadPool.QueueUserWorkItem(_ => StackedAppClicked?.Invoke(stackedMonitorIndex, stackedAppIndex));

                            int count = 0;
                            if (currentIndicatorData.WorkspaceWindows.TryGetValue(currentIndicatorData.CurrentWorkspace, out var wins))
                                count = wins?.Count ?? 0;

                            if (count > 0)
                            {
                                var rect = GetStackedAppRect(hWnd, stackedAppIndex, count, currentIndicatorData);
                                InvalidateRect(hWnd, ref rect, false);
                            }
                        }
                        else
                        {
                            var (monitorIndex, clickedWorkspace) = GetWorkspaceAtPosition(hWnd, cursorPos.x, cursorPos.y);
                            if (clickedWorkspace > 0)
                            {
                                ThreadPool.QueueUserWorkItem(_ => WorkspaceClicked?.Invoke(monitorIndex, clickedWorkspace));

                                var rect = GetWorkspaceRect(hWnd, clickedWorkspace, currentIndicatorData);
                                InvalidateRect(hWnd, ref rect, false);
                            }
                        }
                    }
                    break;

                case WM_DESTROY:
                    KillTimer(hWnd, 1);
                    if (monitorIndicators.Values.All(d => d.WindowHandle == IntPtr.Zero || d.WindowHandle == hWnd))
                    {
                        isRunning = false;
                        PostQuitMessage(0);
                    }
                    break;

                default:
                    return DefWindowProc(hWnd, msg, wParam, lParam);
            }

            return IntPtr.Zero;
        }

        private void HandlePaint(IntPtr hWnd, int monitorIndex, MonitorIndicatorData indicatorData)
        {
            var ps = new PAINTSTRUCT();
            IntPtr hdc = BeginPaint(hWnd, ref ps);

            try
            {
                int currentWS;
                Dictionary<int, List<WorkspaceWindow>> currentWorkspaceWindows;

                lock (lockObject)
                {
                    currentWS = indicatorData.CurrentWorkspace;
                    currentWorkspaceWindows = new Dictionary<int, List<WorkspaceWindow>>();
                    foreach (var kvp in indicatorData.WorkspaceWindows)
                    {
                        currentWorkspaceWindows[kvp.Key] = new List<WorkspaceWindow>(kvp.Value);
                    }
                }

                RECT rect;
                GetClientRect(hWnd, out rect);
                RECT paintRect = ps.rcPaint;

                // create off-screen buffer for double buffering to eliminate flickering
                IntPtr memDC = CreateCompatibleDC(hdc);
                IntPtr memBitmap = CreateCompatibleBitmap(hdc, rect.Right - rect.Left, rect.Bottom - rect.Top);
                IntPtr oldBitmap = SelectObject(memDC, memBitmap);

                uint transparencyKey = GetTransparencyColorKey();
                IntPtr bgBrush = CreateSolidBrush(transparencyKey);
                FillRect(memDC, ref rect, bgBrush);
                DeleteObject(bgBrush);

                SetBkMode(memDC, 1); // transparent

                // get stacked mode, backup workspaces, and paused workspaces
                HashSet<int> stackedModeWorkspaces;
                HashSet<int> backupWorkspaces;
                HashSet<int> pausedWorkspaces;
                lock (lockObject)
                {
                    stackedModeWorkspaces = new HashSet<int>(indicatorData.StackedModeWorkspaces);
                    backupWorkspaces = new HashSet<int>(indicatorData.BackupWorkspaces);
                    pausedWorkspaces = new HashSet<int>(indicatorData.PausedWorkspaces);
                }

                // draw all visible workspaces to off-screen buffer
                var visibleWorkspaces = GetVisibleWorkspaces(currentWS, currentWorkspaceWindows);
                for (int i = 0; i < visibleWorkspaces.Count; i++)
                {
                    int workspaceId = visibleWorkspaces[i];
                    DrawWorkspaceWithIcons(memDC, workspaceId, i, currentWS, currentWorkspaceWindows, stackedModeWorkspaces, backupWorkspaces, pausedWorkspaces, rect);
                }

                // draw stacked apps if current workspace is in stacked mode
                if (stackedModeWorkspaces.Contains(currentWS))
                {
                    if (currentWorkspaceWindows.TryGetValue(currentWS, out var stackedWindows) && stackedWindows != null && stackedWindows.Count > 0)
                    {
                        int currentStackedIndex;
                        int hoveredStackedIndex;
                        lock (lockObject)
                        {
                            currentStackedIndex = indicatorData.CurrentStackedWindowIndex;
                            hoveredStackedIndex = indicatorData.HoveredStackedAppIndex;
                        }
                        DrawStackedApps(memDC, stackedWindows, currentStackedIndex, hoveredStackedIndex, rect, indicatorData);
                    }
                }

                // copy off-screren buffer in one operation to eliminate flickering
                BLENDFUNCTION blendFunc = new BLENDFUNCTION
                {
                    BlendOp = AC_SRC_OVER,
                    BlendFlags = 0,
                    SourceConstantAlpha = 255,
                    AlphaFormat = 0
                };

                AlphaBlend(hdc, 0, 0, rect.Right - rect.Left, rect.Bottom - rect.Top,
                          memDC, 0, 0, rect.Right - rect.Left, rect.Bottom - rect.Top, blendFunc);

                // clean up double buffering resources
                SelectObject(memDC, oldBitmap);
                DeleteObject(memBitmap);
                DeleteDC(memDC);
            }
            finally
            {
                EndPaint(hWnd, ref ps);
            }
        }

        private void DrawWorkspaceWithIcons(IntPtr hdc, int workspaceId, int visualIndex, int currentWS, Dictionary<int, List<WorkspaceWindow>> allWorkspaces, HashSet<int> stackedModeWorkspaces, HashSet<int> backupWorkspaces, HashSet<int> pausedWorkspaces, RECT clientRect)
        {
            int x = WORKSPACE_MARGIN + (visualIndex * (WORKSPACE_WIDTH + WORKSPACE_MARGIN));
            int y = 5;

            bool isStackedMode = stackedModeWorkspaces.Contains(workspaceId);
            bool isBackupWorkspace = backupWorkspaces.Contains(workspaceId);
            bool isPaused = pausedWorkspaces.Contains(workspaceId);

            bool isHovered = false;
            foreach (var kvp in monitorIndicators)
            {
                if (kvp.Value.HoveredWorkspace == workspaceId)
                {
                    isHovered = true;
                    break;
                }
            }

            // determine workspace background color
            uint workspaceBgColor;
            if (isPaused)
                workspaceBgColor = PAUSED_WORKSPACE_COLOR;
            else if (isBackupWorkspace && isStackedMode)
                workspaceBgColor = BACKUP_AND_STACKED_WORKSPACE_COLOR;
            else if (isBackupWorkspace)
                workspaceBgColor = BACKUP_WORKSPACE_COLOR;
            else if (isStackedMode)
                workspaceBgColor = STACKED_MODE_WORKSPACE_COLOR;
            else if (workspaceId == currentWS)
                workspaceBgColor = ACTIVE_WORKSPACE_COLOR;
            else if (isHovered)
                workspaceBgColor = HOVERED_WORKSPACE_COLOR;
            else
                workspaceBgColor = INACTIVE_WORKSPACE_COLOR;

            IntPtr workspaceBrush = CreateSolidBrush(RgbToBgr(workspaceBgColor));
            var workspaceRect = new RECT { Left = x, Top = y, Right = x + WORKSPACE_WIDTH, Bottom = clientRect.Bottom - 5 };
            FillRect(hdc, ref workspaceRect, workspaceBrush);
            DeleteObject(workspaceBrush);

            // draw workspace content
            if (allWorkspaces.ContainsKey(workspaceId) && allWorkspaces[workspaceId].Count > 0)
            {
                DrawIntegratedWorkspaceContent(hdc, x, y, workspaceId, currentWS, allWorkspaces[workspaceId], clientRect.Bottom - y - 5);
            }
            else
            {
                DrawWorkspaceNumber(hdc, x, y, workspaceId, currentWS, clientRect.Bottom - y - 5);
            }

            // draw border for active workspace, stacked mode, backup workspace, or paused workspace
            if (workspaceId == currentWS || isStackedMode || isBackupWorkspace || isPaused)
            {
                // determine border color based on workspace state
                uint borderColor;
                if (workspaceId == currentWS)
                    borderColor = ACTIVE_WORKSPACE_BORDER_COLOR;
                else if (isPaused)
                    borderColor = PAUSED_WORKSPACE_BORDER_COLOR;
                else if (isBackupWorkspace && isStackedMode)
                    borderColor = BACKUP_AND_STACKED_BORDER_COLOR;
                else if (isBackupWorkspace)
                    borderColor = BACKUP_WORKSPACE_BORDER_COLOR;
                else
                    borderColor = STACKED_MODE_BORDER_COLOR;

                var topBorder = new RECT { Left = x, Top = y, Right = x + WORKSPACE_WIDTH, Bottom = y + 2 };
                DrawAlphaBlendedRect(hdc, topBorder, borderColor, ACTIVE_WORKSPACE_BORDER_OPACITY);

                var bottomBorder = new RECT { Left = x, Top = clientRect.Bottom - 7, Right = x + WORKSPACE_WIDTH, Bottom = clientRect.Bottom - 5 };
                DrawAlphaBlendedRect(hdc, bottomBorder, borderColor, ACTIVE_WORKSPACE_BORDER_OPACITY);

                var leftBorder = new RECT { Left = x, Top = y, Right = x + 2, Bottom = clientRect.Bottom - 5 };
                DrawAlphaBlendedRect(hdc, leftBorder, borderColor, ACTIVE_WORKSPACE_BORDER_OPACITY);

                var rightBorder = new RECT { Left = x + WORKSPACE_WIDTH - 2, Top = y, Right = x + WORKSPACE_WIDTH, Bottom = clientRect.Bottom - 5 };
                DrawAlphaBlendedRect(hdc, rightBorder, borderColor, ACTIVE_WORKSPACE_BORDER_OPACITY);
            }
        }

        private void DrawIntegratedWorkspaceContent(IntPtr hdc, int workspaceX, int workspaceY, int workspaceId, int currentWS, List<WorkspaceWindow> windows, int workspaceHeight)
        {
            SetBkMode(hdc, 1);
            if (workspaceId == currentWS)
                SetTextColor(hdc, ACTIVE_WORKSPACE_TEXT_COLOR);
            else
                SetTextColor(hdc, INACTIVE_WORKSPACE_TEXT_COLOR);

            int iconsPerRow = 4;
            int totalRows = 4;
            int gridStartX = workspaceX + 8;
            int gridStartY = workspaceY;
            int cellWidth = 16;
            int cellHeight = 18;

            // workspace number in first position
            string workspaceText = workspaceId.ToString();
            var numberRect = new RECT
            {
                Left = gridStartX,
                Top = gridStartY,
                Right = gridStartX + cellWidth,
                Bottom = gridStartY + cellHeight
            };
            DrawText(hdc, workspaceText, -1, ref numberRect, 0x25); // DT_CENTER | DT_VCENTER

            // draw application icons
            int maxIconsToShow = (iconsPerRow * totalRows) - 1;
            int iconIndex = 0;

            for (int row = 0; row < totalRows && iconIndex < windows.Count && iconIndex < maxIconsToShow; row++)
            {
                for (int col = 0; col < iconsPerRow && iconIndex < windows.Count && iconIndex < maxIconsToShow; col++)
                {
                    if (row == 0 && col == 0) continue;

                    var window = windows[iconIndex];
                    if (window.Icon != IntPtr.Zero)
                    {
                        int iconX = gridStartX + (col * cellWidth);
                        int iconY = gridStartY + (row * cellHeight);

                        DrawIconEx(hdc, iconX, iconY + 1, window.Icon, ICON_SIZE, ICON_SIZE, 0, IntPtr.Zero, DI_NORMAL);
                    }
                    iconIndex++;
                }
            }

            // show overflow indicator if needed
            if (windows.Count > maxIconsToShow)
            {
                int remainingCount = windows.Count - maxIconsToShow;
                string moreText = $"+{remainingCount}";

                SetTextColor(hdc, 0xCCCCCC);
                var moreRect = new RECT
                {
                    Left = workspaceX + 2,
                    Top = workspaceY + workspaceHeight - 15,
                    Right = workspaceX + WORKSPACE_WIDTH - 2,
                    Bottom = workspaceY + workspaceHeight - 2
                };
                DrawText(hdc, moreText, -1, ref moreRect, 0x01); // DT_CENTER
            }
        }

        private bool RectIntersects(RECT rect1, RECT rect2)
        {
            return !(rect1.Right <= rect2.Left || rect1.Left >= rect2.Right ||
                     rect1.Bottom <= rect2.Top || rect1.Top >= rect2.Bottom);
        }

        private void DrawNumberBadge(IntPtr hdc, int x, int y, string label)
        {
            if (!SHOW_STACKED_APP_NUMBERS || string.IsNullOrWhiteSpace(label))
                return;

            int badgeSize = STACKED_APP_NUMBER_BADGE_SIZE;

            // draw badge circle
            IntPtr bgBrush = CreateSolidBrush(RgbToBgr(STACKED_APP_NUMBER_BADGE_BG_COLOR));
            IntPtr bgPen = CreatePen(0, 1, RgbToBgr(STACKED_APP_NUMBER_BADGE_BG_COLOR));
            IntPtr oldBrush = SelectObject(hdc, bgBrush);
            IntPtr oldPen = SelectObject(hdc, bgPen);

            Ellipse(hdc, x, y, x + badgeSize, y + badgeSize);

            SelectObject(hdc, oldBrush);
            SelectObject(hdc, oldPen);
            DeleteObject(bgBrush);
            DeleteObject(bgPen);

            // draw label text
            SetBkMode(hdc, 1); // transparent
            SetTextColor(hdc, STACKED_APP_NUMBER_BADGE_TEXT_COLOR);

            var textRect = new RECT
            {
                Left = x,
                Top = y,
                Right = x + badgeSize,
                Bottom = y + badgeSize
            };
            // DT_CENTER | DT_VCENTER | DT_SINGLELINE
            DrawText(hdc, label, -1, ref textRect, 0x01 | 0x04 | 0x20);
        }

        private void DrawStackedApps(IntPtr hdc, List<WorkspaceWindow> windows, int currentStackedIndex, int hoveredStackedIndex, RECT clientRect, MonitorIndicatorData indicatorData)
        {
            var visibleWorkspaces = GetVisibleWorkspaces(indicatorData.CurrentWorkspace, indicatorData.WorkspaceWindows);
            int baseX = WORKSPACE_MARGIN + (visibleWorkspaces.Count * (WORKSPACE_WIDTH + WORKSPACE_MARGIN)) + STACKED_APP_MARGIN;
            int y = 5;
            int itemHeight = clientRect.Bottom - 10;

            for (int i = 0; i < windows.Count; i++)
            {
                var window = windows[i];
                int itemX = baseX + (i * (STACKED_APP_ITEM_WIDTH + STACKED_APP_MARGIN));

                // determine background color
                uint bgColor;
                uint textColor;
                if (i == currentStackedIndex)
                {
                    bgColor = STACKED_APP_ACTIVE_COLOR;
                    textColor = STACKED_APP_ACTIVE_TEXT_COLOR;
                }
                else if (i == hoveredStackedIndex)
                {
                    bgColor = STACKED_APP_HOVER_COLOR;
                    textColor = STACKED_APP_TEXT_COLOR;
                }
                else
                {
                    bgColor = STACKED_APP_BACKGROUND_COLOR;
                    textColor = STACKED_APP_TEXT_COLOR;
                }

                // draw background
                var itemRect = new RECT
                {
                    Left = itemX,
                    Top = y,
                    Right = itemX + STACKED_APP_ITEM_WIDTH,
                    Bottom = y + itemHeight
                };
                IntPtr bgBrush = CreateSolidBrush(RgbToBgr(bgColor));
                FillRect(hdc, ref itemRect, bgBrush);
                DeleteObject(bgBrush);

                // draw icon
                if (window.Icon != IntPtr.Zero)
                {
                    int iconX;
                    if (SHOW_STACKED_APP_TITLE)
                    {
                        // icon on left side when title is shown
                        iconX = itemX + 8;
                    }
                    else
                    {
                        // center icon when title is hidden
                        iconX = itemX + (STACKED_APP_ITEM_WIDTH - STACKED_APP_ICON_SIZE) / 2;
                    }
                    int iconY = y + (itemHeight - STACKED_APP_ICON_SIZE) / 2;
                    DrawIconEx(hdc, iconX, iconY, window.Icon, STACKED_APP_ICON_SIZE, STACKED_APP_ICON_SIZE, 0, IntPtr.Zero, 0x0003);

                    // draw shortcut label badge on top-right corner of icon
                    if (i < STACKED_WINDOW_SHORTCUT_LABELS.Count)
                    {
                        int badgeX = iconX + STACKED_APP_ICON_SIZE - STACKED_APP_NUMBER_BADGE_SIZE;
                        int badgeY = iconY - 2; // slight offset upward
                        DrawNumberBadge(hdc, badgeX, badgeY, STACKED_WINDOW_SHORTCUT_LABELS[i]);
                    }
                }

                // draw title (only if enabled)
                if (SHOW_STACKED_APP_TITLE)
                {
                    string title = window.Title;
                    if (title.Length > STACKED_APP_TITLE_MAX_LENGTH)
                    {
                        title = title.Substring(0, STACKED_APP_TITLE_MAX_LENGTH - 3) + "...";
                    }

                    SetBkMode(hdc, 1); // transparent
                    SetTextColor(hdc, textColor);

                    var textRect = new RECT
                    {
                        Left = itemX + 8 + STACKED_APP_ICON_SIZE + 5,
                        Top = y,
                        Right = itemX + STACKED_APP_ITEM_WIDTH - 5,
                        Bottom = y + itemHeight
                    };
                    // DT_VCENTER | DT_SINGLELINE | DT_NOPREFIX | DT_END_ELLIPSIS
                    DrawText(hdc, title, -1, ref textRect, 0x20 | 0x04 | 0x800 | 0x8000);
                }
            }
        }

        private void DrawWorkspaceNumber(IntPtr hdc, int workspaceX, int workspaceY, int workspaceId, int currentWS, int workspaceHeight)
        {
            SetBkMode(hdc, 1); // transparent
            if (workspaceId == currentWS)
                SetTextColor(hdc, ACTIVE_WORKSPACE_TEXT_COLOR);
            else
                SetTextColor(hdc, INACTIVE_WORKSPACE_TEXT_COLOR);

            string workspaceText = workspaceId.ToString();
            int gridStartX = workspaceX + 8;
            int gridStartY = workspaceY;
            int cellWidth = 16;
            int cellHeight = 18;

            var numberRect = new RECT
            {
                Left = gridStartX,
                Top = gridStartY,
                Right = gridStartX + cellWidth,
                Bottom = gridStartY + cellHeight
            };
            DrawText(hdc, workspaceText, -1, ref numberRect, 0x25); // DT_CENTER | DT_VCENTER
        }

        public void UpdateMonitors(List<Monitor> monitors)
        {
            lock (lockObject)
            {
                // find monitors that were removed
                var currentMonitorIndices = new HashSet<int>(monitorIndicators.Keys);
                var newMonitorIndices = new HashSet<int>(monitors.Select(m => m.Index));

                // remove indicators for monitors that no longer exist
                var removedMonitors = currentMonitorIndices.Except(newMonitorIndices).ToList();
                foreach (var monitorIndex in removedMonitors)
                {
                    Logger.Info($"Removing indicator for monitor {monitorIndex}");
                    if (monitorIndicators.TryGetValue(monitorIndex, out var indicatorData))
                    {
                        foreach (var workspace in indicatorData.WorkspaceWindows.Values)
                        {
                            foreach (var window in workspace)
                            {
                                if (window.Icon != IntPtr.Zero)
                                {
                                    DestroyIcon(window.Icon);
                                }
                            }
                        }
                        indicatorData.WorkspaceWindows.Clear();

                        // destroy window
                        if (indicatorData.WindowHandle != IntPtr.Zero)
                        {
                            PostMessage(indicatorData.WindowHandle, WM_DESTROY, IntPtr.Zero, IntPtr.Zero);
                        }

                        monitorIndicators.Remove(monitorIndex);
                    }
                }

                // add indicators for new monitors
                var addedMonitors = monitors.Where(m => !currentMonitorIndices.Contains(m.Index)).ToList();
                if (addedMonitors.Count > 0)
                {
                    foreach (var monitor in addedMonitors)
                    {
                        var indicatorData = new MonitorIndicatorData();
                        for (int i = 1; i <= Monitor.NO_OF_WORKSPACES; i++)
                        {
                            indicatorData.WorkspaceWindows[i] = new List<WorkspaceWindow>();
                        }
                        monitorIndicators[monitor.Index] = indicatorData;
                    }

                    // create windows for new monitors (must be done on UI thread)
                    if (uiThread != null && uiThread.IsAlive)
                    {
                        ThreadPool.QueueUserWorkItem(_ =>
                        {
                            foreach (var monitor in addedMonitors)
                            {
                                CreateIndicatorForMonitor(monitor, monitors);
                            }
                        });
                    }
                }

                foreach (var monitor in monitors)
                {
                    if (monitorIndicators.ContainsKey(monitor.Index))
                    {
                        var indicatorData = monitorIndicators[monitor.Index];
                        if (indicatorData.WindowHandle != IntPtr.Zero)
                        {
                            var taskbar = FindTaskbarForMonitor(monitor);
                            if (taskbar != IntPtr.Zero)
                            {
                                RECT taskbarRect;
                                GetClientRect(taskbar, out taskbarRect);

                                int windowWidth = CalculateIndicatorWidth(indicatorData);
                                bool sizeChanged = indicatorData.WindowWidth != windowWidth ||
                                                   indicatorData.WindowHeight != (taskbarRect.Bottom - taskbarRect.Top);

                                if (sizeChanged)
                                {
                                    const uint SWP_NOZORDER = 0x0004;
                                    SetWindowPos(indicatorData.WindowHandle, IntPtr.Zero,
                                        0, 0, windowWidth, taskbarRect.Bottom - taskbarRect.Top,
                                        SWP_SHOWWINDOW | SWP_NOACTIVATE | SWP_NOZORDER | SWP_NOMOVE);

                                    indicatorData.WindowWidth = windowWidth;
                                    indicatorData.WindowHeight = taskbarRect.Bottom - taskbarRect.Top;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void CreateIndicatorForMonitor(Monitor monitor, List<Monitor> allMonitors)
        {
            lock (lockObject)
            {
                if (!monitorIndicators.ContainsKey(monitor.Index))
                    return;

                var indicatorData = monitorIndicators[monitor.Index];

                var taskbar = FindTaskbarForMonitor(monitor);
                if (taskbar == IntPtr.Zero)
                {
                    Logger.Error($"Taskbar not found for monitor {monitor.Index}");
                    return;
                }

                RECT taskbarRect;
                GetClientRect(taskbar, out taskbarRect);

                int windowWidth = CalculateIndicatorWidth(indicatorData);
                indicatorData.WindowWidth = windowWidth;
                indicatorData.WindowHeight = taskbarRect.Bottom - taskbarRect.Top;

                int xPos;
                if (USE_WINDOWS10_POSITIONING)
                {
                    xPos = (taskbarRect.Right - windowWidth) / 2;
                }
                else
                {
                    xPos = OFFSET_FROM_TASKBAR_LEFT_EDGE;
                }

                indicatorData.WindowHandle = CreateWindowEx(
                    WS_EX_NOACTIVATE | WS_EX_LAYERED | WS_EX_TOOLWINDOW | WS_EX_TOPMOST,
                    windowClassName,
                    $"Workspace Indicator M{monitor.Index}",
                    (uint)(WS_VISIBLE | WS_CLIPSIBLINGS | WS_POPUP),
                    xPos, 0,
                    windowWidth,
                    taskbarRect.Bottom - taskbarRect.Top,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    moduleHandle,
                    IntPtr.Zero
                );

                if (indicatorData.WindowHandle == IntPtr.Zero)
                {
                    int error = Marshal.GetLastWin32Error();
                    Logger.Error($"Failed to create workspace indicator window for monitor {monitor.Index}. Error: {error}");
                    return;
                }

                SetParent(indicatorData.WindowHandle, taskbar);
                SetLayeredWindowAttributes(indicatorData.WindowHandle, GetTransparencyColorKey(), 0, LWA_COLORKEY);

                ShowWindow(indicatorData.WindowHandle, 1);
                UpdateWindow(indicatorData.WindowHandle);

                Logger.Info($"Created taskbar-integrated indicator window for monitor {monitor.Index}");
            }
        }

        public void Cleanup()
        {
            isRunning = false;

            lock (lockObject)
            {
                foreach (var indicatorData in monitorIndicators.Values)
                {
                    foreach (var workspace in indicatorData.WorkspaceWindows.Values)
                    {
                        foreach (var window in workspace)
                        {
                            if (window.Icon != IntPtr.Zero)
                            {
                                DestroyIcon(window.Icon);
                            }
                        }
                    }
                    indicatorData.WorkspaceWindows.Clear();

                    if (indicatorData.WindowHandle != IntPtr.Zero)
                    {
                        PostMessage(indicatorData.WindowHandle, WM_DESTROY, IntPtr.Zero, IntPtr.Zero);
                    }
                }
                monitorIndicators.Clear();
            }

            // wait for ui thread to finish
            if (uiThread != null && uiThread.IsAlive)
            {
                // increasing the limit to 5s, just to wait for thread to finish
                if (!uiThread.Join(5000))
                {
                    Logger.Error("Warning: Workspace indicator thread did not exit cleanly after 5 seconds");
                    try
                    {
                        // force abort as last resort
                        uiThread.Interrupt();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Error interrupting thread: {ex.Message}");
                    }
                }
                uiThread = null;
            }

            // unregister window class
            if (!string.IsNullOrEmpty(windowClassName) && moduleHandle != IntPtr.Zero)
            {
                if (!UnregisterClass(windowClassName, moduleHandle)) { 
                    int error = Marshal.GetLastWin32Error();
                    Logger.Error($"Failed to unregister window class. Error: {error}");
                }
                windowClassName = "";
                moduleHandle = IntPtr.Zero;
            }
        }
    }
}