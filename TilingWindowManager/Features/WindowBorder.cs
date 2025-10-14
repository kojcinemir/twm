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
using System.Drawing;
using System.Collections.Generic;
using System.Linq;

namespace TilingWindowManager
{
    public class WindowBorder
    {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(nint hWnd);

        [DllImport("user32.dll")]
        private static extern bool BringWindowToTop(nint hWnd);

        [DllImport("user32.dll")]
        private static extern nint GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(nint hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern nint MonitorFromWindow(nint hwnd, uint dwFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern nint CreateWindowEx(
            uint dwExStyle, string lpClassName, string lpWindowName, uint dwStyle,
            int x, int y, int nWidth, int nHeight, nint hWndParent,
            nint hMenu, nint hInstance, nint lpParam);

        [DllImport("user32.dll")]
        private static extern bool DestroyWindow(nint hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(nint hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(nint hWnd, nint hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern ushort RegisterClass(ref WNDCLASS lpWndClass);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterClass(string lpClassName, nint hInstance);

        [DllImport("kernel32.dll")]
        private static extern nint GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern nint DefWindowProc(nint hWnd, uint uMsg, nint wParam, nint lParam);

        [DllImport("gdi32.dll")]
        private static extern nint CreateSolidBrush(uint crColor);

        [DllImport("user32.dll")]
        private static extern bool SetLayeredWindowAttributes(nint hwnd, uint crKey, byte bAlpha, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(nint hWnd);

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(nint hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool ClientToScreen(nint hWnd, ref POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern uint GetWindowLong(nint hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetClassName(nint hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

        [DllImport("dwmapi.dll")]
        private static extern int DwmGetWindowAttribute(nint hwnd, int dwAttribute, out RECT pvAttribute, int cbAttribute);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(nint hWnd, out uint lpdwProcessId);

        [DllImport("shcore.dll")]
        public static extern int GetDpiForMonitor(nint hmonitor, MONITOR_DPI_TYPE dpiType, out uint dpiX, out uint dpiY);

        [DllImport("gdi32.dll")]
        private static extern nint CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);

        [DllImport("user32.dll")]
        private static extern int SetWindowRgn(nint hWnd, nint hRgn, bool bRedraw);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(nint hObject);

        [DllImport("gdi32.dll")]
        private static extern nint CreateRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);

        [DllImport("gdi32.dll")]
        private static extern int CombineRgn(nint hrgnDest, nint hrgnSrc1, nint hrgnSrc2, int fnCombineMode);

        [DllImport("user32.dll")]
        private static extern nint MonitorFromRect([In] ref RECT lprc, uint dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool GetMonitorInfo(nint hMonitor, ref Monitor.MONITORINFO lpmi);

        public delegate nint WndProc(nint hWnd, uint msg, nint wParam, nint lParam);


        private const int GWL_STYLE = -16;
        private const int GWL_EXSTYLE = -20;
        private const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;
        private const uint WS_BORDER = 0x00800000;
        private const uint WS_DLGFRAME = 0x00400000;
        private const uint WS_THICKFRAME = 0x00040000;
        private const uint WS_CAPTION = 0x00C00000;
        private const uint WS_EX_CLIENTEDGE = 0x00000200;
        private const uint WS_POPUP = 0x80000000;
        private const uint WS_VISIBLE = 0x10000000;
        private const uint WS_EX_LAYERED = 0x00080000;
        private const uint WS_EX_TRANSPARENT = 0x00000020;
        private const uint WS_EX_TOPMOST = 0x00000008;
        private const uint WS_EX_TOOLWINDOW = 0x00000080;
        private const int SW_SHOW = 5;
        private const int SW_HIDE = 0;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOCOPYBITS = 0x0100;
        private static readonly nint HWND_TOPMOST = new nint(-1);
        private const uint LWA_COLORKEY = 0x00000001;
        private const uint LWA_ALPHA = 0x00000002;
        private const int SW_SHOWNOACTIVATE = 4;
        private const uint SWP_NOOWNERZORDER = 0x0200;
        private const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

        private const int RGN_DIFF = 4; 

        public enum MONITOR_DPI_TYPE
        {
            MDT_EFFECTIVE_DPI = 0,
            MDT_ANGULAR_DPI = 1,
            MDT_RAW_DPI = 2,
            MDT_DEFAULT = MDT_EFFECTIVE_DPI
        }
        private struct FrameCorrection
        {
            public int LeftOffset;
            public int TopOffset;
            public int RightOffset;
            public int BottomOffset;
            public DateTime LastCalculated;
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

            public int Width => Right - Left;
            public int Height => Bottom - Top;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WNDCLASS
        {
            public uint style;
            public WndProc lpfnWndProc;
            public int cbClsExtra;
            public int cbWndExtra;
            public nint hInstance;
            public nint hIcon;
            public nint hCursor;
            public nint hbrBackground;
            public string lpszMenuName;
            public string lpszClassName;
        }

        private Dictionary<nint, FrameCorrection> frameCorrections = new Dictionary<nint, FrameCorrection>();
        private const string BORDER_WINDOW_CLASS = "TilingWMBorderWindow";

        private WindowBorderConfiguration config = new WindowBorderConfiguration();

        private static readonly (string ClassNameFragment, FrameCorrection Correction)[] KnownFrameCorrections = new[]
        {
            ("Chrome_WidgetWin_1", new FrameCorrection
            {
                LeftOffset = 1,
                TopOffset = 1,
                RightOffset = 1,
                BottomOffset = 1,
                LastCalculated = DateTime.MinValue
            }),
            ("ApplicationFrameWindow", new FrameCorrection
            {
                LeftOffset = 0,
                TopOffset = 0,
                RightOffset = 0,
                BottomOffset = 0,
                LastCalculated = DateTime.MinValue
            })
        };

        private int BORDER_WIDTH => config.BorderWidth;
        private uint BORDER_COLOR => config.BorderColor;
        private bool ROUNDED_BORDERS => config.RoundedBorders;
        private int CORNER_RADIUS => config.CornerRadius;
        private byte OPACITY => config.Opacity;
       

        private class MonitorBorderData
        {
            public nint BorderWindow { get; set; } = nint.Zero; 
            public nint LastActiveWindow { get; set; } = nint.Zero;
            public Dictionary<int, nint> LastActiveWindowPerWorkspace { get; set; } = new Dictionary<int, nint>();
            public int CurrentWorkspaceId { get; set; } = 1;
            public RECT LastOuterRect { get; set; }
            public RECT LastWindowRect { get; set; }
            public bool HasLastWindowRect { get; set; }
            public bool HasAppliedRegion { get; set; }
        }

        private Dictionary<nint, MonitorBorderData> monitorBorders = new Dictionary<nint, MonitorBorderData>();
        private WndProc wndProc = null!;
        private bool isInitialized = false;
        private bool isClassRegistered = false;
        private Func<nint, bool>? isWindowTiledCallback;

        public WindowBorder()
        {
            Initialize();
        }

        private void Initialize()
        {
            try
            {
                config.LoadConfiguration();

                // keeping reference to prevent GC
                wndProc = new WndProc(BorderWindowProc);

                WNDCLASS wndClass = new WNDCLASS
                {
                    lpfnWndProc = wndProc,
                    hInstance = GetModuleHandle(null!),
                    lpszClassName = BORDER_WINDOW_CLASS,
                    hbrBackground = CreateSolidBrush(RgbToBgr(BORDER_COLOR))
                };

                ushort classAtom = RegisterClass(ref wndClass);
                if (classAtom == 0)
                {
                    int error = Marshal.GetLastWin32Error();
                    if (error != 1410)
                    {
                        Logger.Error($"Failed to register window border class. Error: {error}");
                        return;
                    }
                }
                else
                {
                    isClassRegistered = true;
                }

                InitializeAllMonitorBorders();
                isInitialized = true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error initializing WindowBorder");
            }
        }

        private void InitializeAllMonitorBorders()
        {
            var monitors = Monitor.GetAllMonitors();
            foreach (var monitor in monitors)
            {
                if (!monitorBorders.ContainsKey(monitor.Handle))
                {
                    var borderData = new MonitorBorderData();
                    CreateBorderWindowsForMonitor(borderData);
                    monitorBorders[monitor.Handle] = borderData;
                }
            }
        }

        public static uint RgbToBgr(uint rgb)
        {
            uint r = (rgb & 0xFF0000) >> 16;
            uint g = rgb & 0x00FF00;
            uint b = (rgb & 0x0000FF) << 16;

            return b | g | r;
        }
        private void CreateBorderWindowsForMonitor(MonitorBorderData borderData)
        {
            nint hInstance = GetModuleHandle(null!);

            borderData.BorderWindow = CreateWindowEx(
                WS_EX_LAYERED | WS_EX_TOPMOST | WS_EX_TOOLWINDOW | WS_EX_TRANSPARENT,
                BORDER_WINDOW_CLASS,
                "BorderWindow",
                WS_POPUP,
                0, 0, 0, 0,
                nint.Zero,
                nint.Zero,
                hInstance,
                nint.Zero);

            if (borderData.BorderWindow == nint.Zero)
            {
                throw new InvalidOperationException("Failed to create border window");
            }

            SetLayeredWindowAttributes(borderData.BorderWindow, 0, OPACITY, LWA_ALPHA);
        }

        private void CreateHollowBorderRegion(nint hWnd, RECT outerRect, RECT innerRect)
        {
            try
            {
                nint outerRegion, innerRegion, borderRegion;

                if (ROUNDED_BORDERS && CORNER_RADIUS > 0)
                {
                    outerRegion = CreateRoundRectRgn(
                        0, 0,
                        outerRect.Width, outerRect.Height,
                        CORNER_RADIUS * 2, CORNER_RADIUS * 2);

                    innerRegion = CreateRoundRectRgn(
                        innerRect.Left - outerRect.Left,
                        innerRect.Top - outerRect.Top,
                        innerRect.Right - outerRect.Left,
                        innerRect.Bottom - outerRect.Top,
                        Math.Max(0, CORNER_RADIUS * 2 - BORDER_WIDTH * 2),
                        Math.Max(0, CORNER_RADIUS * 2 - BORDER_WIDTH * 2));
                }
                else
                {
                    outerRegion = CreateRectRgn(0, 0, outerRect.Width, outerRect.Height);
                    innerRegion = CreateRectRgn(
                        innerRect.Left - outerRect.Left,
                        innerRect.Top - outerRect.Top,
                        innerRect.Right - outerRect.Left,
                        innerRect.Bottom - outerRect.Top);
                }

                if (outerRegion == nint.Zero || innerRegion == nint.Zero)
                {
                    if (outerRegion != nint.Zero) DeleteObject(outerRegion);
                    if (innerRegion != nint.Zero) DeleteObject(innerRegion);
                    return;
                }

                borderRegion = CreateRectRgn(0, 0, 0, 0); 
                if (borderRegion != nint.Zero)
                {
                    int result = CombineRgn(borderRegion, outerRegion, innerRegion, RGN_DIFF);
                    if (result != 0) 
                    {
                        SetWindowRgn(hWnd, borderRegion, true);
                    }
                    else
                    {
                        DeleteObject(borderRegion);
                    }
                }

                DeleteObject(outerRegion);
                DeleteObject(innerRegion);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating hollow border region: {ex.Message}");
            }
        }

        public void SetTileCheckCallback(Func<nint, bool> callback)
        {
            isWindowTiledCallback = callback;
        }

        private nint BorderWindowProc(nint hWnd, uint msg, nint wParam, nint lParam)
        {
            return DefWindowProc(hWnd, msg, wParam, lParam);
        }

        public void UpdateBorderImproved(nint window)
        {
            UpdateBorderForWindow(window);
        }

        public void HideBorderForWindow(nint window)
        {
            if (!isInitialized || window == nint.Zero)
                return;

            try
            {
                nint monitorHandle = MonitorFromWindow(window, MONITOR_DEFAULTTONEAREST);

                if (!monitorBorders.ContainsKey(monitorHandle))
                {
                    InitializeAllMonitorBorders();
                }

                if (monitorBorders.TryGetValue(monitorHandle, out var borderData))
                {
                    HideBorderForMonitor(borderData);
                    borderData.LastActiveWindow = nint.Zero;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in HideBorderForWindow: {ex.Message}");
            }
        }

        public void UpdateBorderForWindow(nint targetWindow)
        {
            if (!isInitialized) return;

            try
            {
                if (targetWindow == nint.Zero || IsBorderWindow(targetWindow))
                {
                    return;
                }

                if (!IsWindowVisible(targetWindow))
                {
                    return;
                }

                nint monitorHandle = MonitorFromWindow(targetWindow, MONITOR_DEFAULTTONEAREST);

                if (!monitorBorders.ContainsKey(monitorHandle))
                {
                    InitializeAllMonitorBorders();
                }

                var borderData = monitorBorders[monitorHandle];

                if (isWindowTiledCallback != null && !isWindowTiledCallback(targetWindow))
                {
                    HideBorderForMonitor(borderData);
                    borderData.LastActiveWindow = nint.Zero;
                    return;
                }

                HideAllBordersExcept(monitorHandle);

                PositionBorderWindowsImproved(borderData, targetWindow);
                ShowBorderForMonitor(borderData);
                borderData.LastActiveWindow = targetWindow;
            }
            catch (Exception ex)
            {
                HideAllBorders();
                System.Diagnostics.Debug.WriteLine($"Error in UpdateBorderForWindow: {ex.Message}");
            }
        }

        private bool IsBorderWindow(nint hWnd)
        {
            foreach (var borderData in monitorBorders.Values)
            {
                if (borderData.BorderWindow == hWnd)
                    return true;
            }
            return false;
        }

        private static bool RectEquals(in RECT left, in RECT right)
        {
            return left.Left == right.Left && left.Top == right.Top &&
                   left.Right == right.Right && left.Bottom == right.Bottom;
        }

        private static bool RectSizeEquals(in RECT left, in RECT right)
        {
            return (left.Right - left.Left) == (right.Right - right.Left) &&
                   (left.Bottom - left.Top) == (right.Bottom - right.Top);
        }

        private static bool RectPositionEquals(in RECT left, in RECT right)
        {
            return left.Left == right.Left && left.Top == right.Top;
        }

        private void PositionBorderWindow(MonitorBorderData borderData, RECT targetRect)
        {
            RECT outerRect = new RECT
            {
                Left = targetRect.Left - BORDER_WIDTH,
                Top = targetRect.Top - BORDER_WIDTH,
                Right = targetRect.Right + BORDER_WIDTH,
                Bottom = targetRect.Bottom + BORDER_WIDTH
            };

            RECT innerRect = new RECT
            {
                Left = BORDER_WIDTH,
                Top = BORDER_WIDTH,
                Right = outerRect.Width - BORDER_WIDTH,
                Bottom = outerRect.Height - BORDER_WIDTH
            };

            bool hadRegion = borderData.HasAppliedRegion;
            RECT previousOuter = borderData.LastOuterRect;

            bool sizeChanged = !hadRegion || !RectSizeEquals(previousOuter, outerRect);
            bool positionChanged = !hadRegion || !RectPositionEquals(previousOuter, outerRect);
            bool needsMoveOrResize = sizeChanged || positionChanged;

            if (needsMoveOrResize)
            {
                uint flags = SWP_NOACTIVATE | SWP_NOOWNERZORDER | SWP_NOZORDER | SWP_NOCOPYBITS;
                if (!sizeChanged)
                {
                    flags |= SWP_NOSIZE;
                }

                if (!positionChanged)
                {
                    flags |= SWP_NOMOVE;
                }

                SetWindowPos(borderData.BorderWindow, HWND_TOPMOST,
                    outerRect.Left, outerRect.Top,
                    outerRect.Width, outerRect.Height,
                    flags);
            }

            if (sizeChanged || !hadRegion)
            {
                CreateHollowBorderRegion(borderData.BorderWindow,
                    new RECT { Left = 0, Top = 0, Right = outerRect.Width, Bottom = outerRect.Height },
                    innerRect);
                borderData.HasAppliedRegion = true;
            }

            borderData.LastOuterRect = outerRect;
            borderData.LastWindowRect = targetRect;
            borderData.HasLastWindowRect = true;
        }

        private void PositionBorderWindowsImproved(MonitorBorderData borderData, nint targetWindow)
        {
            RECT actualBounds = GetActualWindowBounds(targetWindow);

            if (actualBounds.Width <= 0 || actualBounds.Height <= 0)
            {
                HideBorderForMonitor(borderData);
                borderData.HasLastWindowRect = false;
                return;
            }

            bool windowChanged = borderData.LastActiveWindow != targetWindow;
            if (!windowChanged && borderData.HasLastWindowRect && RectEquals(actualBounds, borderData.LastWindowRect))
            {
                return;
            }

            PositionBorderWindow(borderData, actualBounds);
        }

        private RECT GetActualWindowBounds(nint window)
        {
            if (TryGetDwmFrameBounds(window, out RECT dwmBounds))
            {
                return dwmBounds;
            }

            return CalculateImprovedWindowBounds(window);
        }

        private bool TryGetDwmFrameBounds(nint window, out RECT dwmBounds)
        {
            dwmBounds = new RECT();

            try
            {
                int result = DwmGetWindowAttribute(window, DWMWA_EXTENDED_FRAME_BOUNDS,
                    out dwmBounds, Marshal.SizeOf(typeof(RECT)));
                return result == 0; // S_OK
            }
            catch
            {
                return false;
            }
        }

        private RECT CalculateImprovedWindowBounds(nint window)
        {
            if (!GetWindowRect(window, out RECT windowRect))
            {
                return new RECT();
            }

            string className = GetWindowClassName(window);
            if (HasKnownFrameCorrection(className, out FrameCorrection correction))
            {
                return ApplyFrameCorrection(windowRect, correction);
            }

            var calculatedCorrection = CalculateFrameCorrection(window, windowRect);

            frameCorrections[window] = calculatedCorrection;

            return ApplyFrameCorrection(windowRect, calculatedCorrection);
        }

        private FrameCorrection CalculateFrameCorrection(nint window, RECT windowRect)
        {
            var correction = new FrameCorrection
            {
                LastCalculated = DateTime.Now
            };

            try
            {
                if (!GetClientRect(window, out RECT clientRect))
                {
                    return correction;
                }

                var clientTopLeft = new POINT { x = 0, y = 0 };
                var clientBottomRight = new POINT { x = clientRect.Right, y = clientRect.Bottom };

                ClientToScreen(window, ref clientTopLeft);
                ClientToScreen(window, ref clientBottomRight);

                var actualClientRect = new RECT
                {
                    Left = clientTopLeft.x,
                    Top = clientTopLeft.y,
                    Right = clientBottomRight.x,
                    Bottom = clientBottomRight.y
                };

                correction.LeftOffset = actualClientRect.Left - windowRect.Left;
                correction.TopOffset = actualClientRect.Top - windowRect.Top;
                correction.RightOffset = windowRect.Right - actualClientRect.Right;
                correction.BottomOffset = windowRect.Bottom - actualClientRect.Bottom;

                if (IsBorderlessWindow(window)) 
                {
                    correction = AdjustForBorderlessWindow(window, correction);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error calculating frame correction: {ex.Message}");
            }

            return correction;
        }

       private bool IsBorderlessWindow(nint window)
        {
            try
            {
                uint style = GetWindowLong(window, GWL_STYLE);
                uint exStyle = GetWindowLong(window, GWL_EXSTYLE);

                bool hasThickFrame = (style & WS_THICKFRAME) != 0;
                bool hasBorder = (style & WS_BORDER) != 0;
                bool hasClientEdge = (exStyle & WS_EX_CLIENTEDGE) != 0;

                string className = GetWindowClassName(window);
                string[] borderlessClasses = {
            "Chrome_WidgetWin_1",  
            "ApplicationFrameWindow", 
            "CASCADIA_HOSTING_WINDOW_CLASS", 
            "Qt5QWindowIcon", 
        };

                bool isKnownBorderless = borderlessClasses.Any(cls =>
                    className.Contains(cls, StringComparison.OrdinalIgnoreCase));

                return !hasThickFrame || !hasBorder || isKnownBorderless;
            }
            catch
            {
                return false;
            }
        }
        private FrameCorrection AdjustForBorderlessWindow(nint window, FrameCorrection correction)
        {
            string className = GetWindowClassName(window);

            if (className.Contains("Chrome_WidgetWin_1") || IsElectronApp(window))
            {
                correction.LeftOffset = Math.Max(0, correction.LeftOffset - 1);
                correction.TopOffset = Math.Max(0, correction.TopOffset - 1);
                correction.RightOffset = Math.Max(0, correction.RightOffset - 1);
                correction.BottomOffset = Math.Max(0, correction.BottomOffset - 1);
            }

            return correction;
        }
        private bool IsElectronApp(nint window)
        {
            try
            {
                uint processId;
                GetWindowThreadProcessId(window, out processId);

                var process = System.Diagnostics.Process.GetProcessById((int)processId);
                string processName = process.ProcessName.ToLower();

                string[] electronIndicators = {
            "code", "discord", "slack", "whatsapp", "spotify",
            "atom", "figma", "notion", "obsidian"
        };

                return electronIndicators.Any(indicator => processName.Contains(indicator));
            }
            catch
            {
                return false;
            }
        }
        private string GetWindowClassName(nint window)
        {
            try
            {
                var className = new System.Text.StringBuilder(256);
                GetClassName(window, className, className.Capacity);
                return className.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }

        private bool HasKnownFrameCorrection(string className, out FrameCorrection correction)
        {
            if (!string.IsNullOrEmpty(className))
            {
                foreach (var entry in KnownFrameCorrections)
                {
                    if (className.Contains(entry.ClassNameFragment, StringComparison.OrdinalIgnoreCase))
                    {
                        correction = entry.Correction;
                        return true;
                    }
                }
            }

            correction = default;
            return false;
        }
        private RECT ApplyFrameCorrection(RECT windowRect, FrameCorrection correction)
        {
            return new RECT
            {
                Left = windowRect.Left + correction.LeftOffset,
                Top = windowRect.Top + correction.TopOffset,
                Right = windowRect.Right - correction.RightOffset,
                Bottom = windowRect.Bottom - correction.BottomOffset
            };
        }

               public void ClearFrameCorrections()
        {
            frameCorrections.Clear();
        }

        private void ShowBorderForMonitor(MonitorBorderData borderData)
        {
            if (borderData.BorderWindow != nint.Zero)
            {
                ShowWindow(borderData.BorderWindow, SW_SHOWNOACTIVATE);
            }
        }

        private void HideBorderForMonitor(MonitorBorderData borderData)
        {
            if (borderData.BorderWindow != nint.Zero)
            {
                ShowWindow(borderData.BorderWindow, SW_HIDE);
            }

            borderData.HasLastWindowRect = false;
        }

        public void HideAllBorders()
        {
            foreach (var borderData in monitorBorders.Values)
            {
                HideBorderForMonitor(borderData);
            }
        }

        private void HideAllBordersExcept(nint monitorHandle)
        {
            foreach (var kvp in monitorBorders)
            {
                if (kvp.Key != monitorHandle)
                {
                    HideBorderForMonitor(kvp.Value);
                }
            }
        }

        public void SetCurrentWorkspace(int workspaceId, nint monitorHandle, bool activateLastWindow = false)
        {
            if (!monitorBorders.ContainsKey(monitorHandle))
            {
                InitializeAllMonitorBorders();
            }

            var borderData = monitorBorders[monitorHandle];

            if (borderData.LastActiveWindow != nint.Zero)
            {
                borderData.LastActiveWindowPerWorkspace[borderData.CurrentWorkspaceId] = borderData.LastActiveWindow;
            }

            borderData.CurrentWorkspaceId = workspaceId;

            if (borderData.LastActiveWindowPerWorkspace.ContainsKey(workspaceId))
            {
                nint lastWindowInWorkspace = borderData.LastActiveWindowPerWorkspace[workspaceId];

                if (isWindowTiledCallback != null && isWindowTiledCallback(lastWindowInWorkspace))
                {
                    RECT windowRect;
                    if (GetWindowRect(lastWindowInWorkspace, out windowRect))
                    {
                        if (activateLastWindow)
                        {
                            ActivateWindow(lastWindowInWorkspace);
                        }

                        borderData.LastActiveWindow = lastWindowInWorkspace;
                        return;
                    }
                }
                else
                {
                    borderData.LastActiveWindowPerWorkspace.Remove(workspaceId);
                }
            }
            borderData.LastActiveWindow = nint.Zero;
        }

        private void ActivateWindow(nint window)
        {
            try
            {
                SetForegroundWindow(window);
                BringWindowToTop(window);
            }
            catch (Exception)
            {
            }
        }

        public void Cleanup()
        {
            try
            {
                HideAllBorders();

                frameCorrections.Clear();

                foreach (var borderData in monitorBorders.Values)
                {
                    if (borderData.BorderWindow != nint.Zero)
                    {
                        DestroyWindow(borderData.BorderWindow);
                        borderData.BorderWindow = nint.Zero;
                    }
                }

                monitorBorders.Clear();

                if (isClassRegistered)
                {
                    nint hInstance = GetModuleHandle(null!);
                    if (UnregisterClass(BORDER_WINDOW_CLASS, hInstance))
                    {
                        isClassRegistered = false;
                    }
                    else
                    {
                        int error = Marshal.GetLastWin32Error();
                        Logger.Warning($"Failed to unregister window border class. Error: {error}");
                    }
                }

                isInitialized = false;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error in WindowBorder cleanup");
            }
        }

        public bool IsEnabled => isInitialized;
    }
}