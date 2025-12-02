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
using System.Runtime.InteropServices;
using System.Threading;

namespace TilingWindowManager
{
    public class AppSwitcher
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

        [DllImport("user32.dll", SetLastError = true, EntryPoint = "CreateWindowExW")]
        private static extern IntPtr CreateWindowExAtom(
            int dwExStyle, ushort lpClassName, string lpWindowName,
            uint dwStyle, int x, int y, int nWidth, int nHeight,
            IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool DestroyWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

        [DllImport("user32.dll")]
        private static extern bool TranslateMessage(ref MSG lpMsg);

        [DllImport("user32.dll")]
        private static extern IntPtr DispatchMessage(ref MSG lpMsg);

        [DllImport("user32.dll")]
        private static extern IntPtr BeginPaint(IntPtr hwnd, ref PAINTSTRUCT lpPaint);

        [DllImport("user32.dll")]
        private static extern bool EndPaint(IntPtr hWnd, ref PAINTSTRUCT lpPaint);

        [DllImport("user32.dll")]
        private static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, bool bErase);

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int DrawText(IntPtr hdc, string lpchText, int cchText, ref RECT lprc, uint format);

        [DllImport("user32.dll")]
        private static extern bool FillRect(IntPtr hDC, ref RECT lprc, IntPtr hbr);

        [DllImport("user32.dll")]
        private static extern bool DrawIconEx(IntPtr hdc, int xLeft, int yTop, IntPtr hIcon, int cxWidth, int cyHeight,
            uint istepIfAniCur, IntPtr hbrFlickerFreeDraw, uint diFlags);

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

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight,
            IntPtr hdcSrc, int nXSrc, int nYSrc, uint dwRop);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateFont(int nHeight, int nWidth, int nEscapement, int nOrientation,
            int fnWeight, uint fdwItalic, uint fdwUnderline, uint fdwStrikeOut, uint fdwCharSet,
            uint fdwOutputPrecision, uint fdwClipPrecision, uint fdwQuality, uint fdwPitchAndFamily, string lpszFace);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern void PostQuitMessage(int nExitCode);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private const string WINDOW_CLASS_NAME = "TilingWMAppSwitcher";

        private const uint WM_PAINT = 0x000F;
        private const uint WM_DESTROY = 0x0002;
        private const uint WM_KEYDOWN = 0x0100;
        private const uint WM_CHAR = 0x0102;
        private const uint WM_ACTIVATE = 0x0006;
        private const uint WM_KILLFOCUS = 0x0008;

        private const uint WS_POPUP = 0x80000000;
        private const uint WS_VISIBLE = 0x10000000;
        private const int WS_EX_LAYERED = 0x80000;
        private const int WS_EX_TOPMOST = 0x8;
        private const int WS_EX_TOOLWINDOW = 0x80;
        private const int GWL_EXSTYLE = -20;

        private const int SW_SHOW = 5;
        private const int SW_HIDE = 0;

        private const int VK_ESCAPE = 0x1B;
        private const int VK_RETURN = 0x0D;
        private const int VK_UP = 0x26;
        private const int VK_DOWN = 0x28;
        private const int VK_BACK = 0x08;

        private const int TRANSPARENT = 1;
        private const uint DT_LEFT = 0x0000;
        private const uint DT_VCENTER = 0x0004;
        private const uint DT_SINGLELINE = 0x0020;
        private const uint DT_END_ELLIPSIS = 0x8000;
        private const uint SRCCOPY = 0x00CC0020;
        private const uint DI_NORMAL = 0x0003;
        private const uint LWA_COLORKEY = 0x1;

        private const int FW_NORMAL = 400;
        private const int FW_BOLD = 700;
        private const uint DEFAULT_CHARSET = 1;
        private const uint OUT_DEFAULT_PRECIS = 0;
        private const uint CLIP_DEFAULT_PRECIS = 0;
        private const uint ANTIALIASED_QUALITY = 4;
        private const uint DEFAULT_PITCH = 0;
        private const uint FF_DONTCARE = 0;

        private const uint WM_SHOW_SWITCHER = 0x8001;
        private const uint WM_HIDE_SWITCHER = 0x8002;

        [StructLayout(LayoutKind.Sequential)]
        private struct WNDCLASSEX
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
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpszMenuName;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpszClassName;
            public IntPtr hIconSm;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSG
        {
            public IntPtr hwnd;
            public uint message;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public POINT pt;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
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
        private struct PAINTSTRUCT
        {
            public IntPtr hdc;
            public bool fErase;
            public RECT rcPaint;
            public bool fRestore;
            public bool fIncUpdate;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] rgbReserved;
        }

        private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        private AppSwitcherConfiguration config;
        private IntPtr windowHandle = IntPtr.Zero;
        private Thread uiThread;
        private volatile bool isRunning = false;
        private volatile bool isVisible = false;
        private readonly object lockObject = new object();
        private WndProcDelegate wndProcDelegate;
        private bool isClassRegistered = false;
        private IntPtr moduleHandle;
        private readonly ManualResetEventSlim windowReadyEvent = new ManualResetEventSlim(false);
        private ushort classAtom = 0;

        private List<WindowSearchEntry> allWindows = new List<WindowSearchEntry>();
        private List<WindowSearchEntry> filteredWindows = new List<WindowSearchEntry>();
        private string searchText = "";
        private int selectedIndex = 0;
        private Monitor activeMonitor;

        private IntPtr normalFont;
        private IntPtr boldFont;

        public event Action<nint> WindowSelected;

        public AppSwitcher()
        {
            config = new AppSwitcherConfiguration();
            config.LoadConfiguration();

            moduleHandle = GetModuleHandle(null);

            // keep delegate alive to prevent GC
            wndProcDelegate = WndProc;

            isRunning = true;
            uiThread = new Thread(RunUIThread)
            {
                IsBackground = true,
                Name = "AppSwitcherUI"
            };
            uiThread.SetApartmentState(ApartmentState.STA);
            uiThread.Start();

        }



        public void Show(List<WindowSearchEntry> windows, Monitor monitor)
        {
            lock (lockObject)
            {
                allWindows = windows ?? new List<WindowSearchEntry>();
                activeMonitor = monitor;
                searchText = "";
                selectedIndex = 0;
                UpdateFiltered();
            }

            // wait for the window to be created (with timeout)
            if (!windowReadyEvent.Wait(2000))
            {
                Logger.Error("AppSwitcher window not ready after 2 seconds");
                return;
            }

            if (windowHandle != IntPtr.Zero)
            {
                Logger.Info($"Posting WM_SHOW_SWITCHER to windowHandle {windowHandle}");
                PostMessage(windowHandle, WM_SHOW_SWITCHER, IntPtr.Zero, IntPtr.Zero);
            }
            else
            {
                Logger.Error("AppSwitcher windowHandle is Zero even after ready event");
            }
        }

        public void Hide()
        {
            if (windowHandle != IntPtr.Zero)
            {
                PostMessage(windowHandle, WM_HIDE_SWITCHER, IntPtr.Zero, IntPtr.Zero);
            }
        }

        public void Cleanup()
        {
            isRunning = false;

            if (windowHandle != IntPtr.Zero)
            {
                PostMessage(windowHandle, WM_DESTROY, IntPtr.Zero, IntPtr.Zero);
            }

            uiThread?.Join(1000);

            Logger.Info("AppSwitcher cleaned up");
        }

        private void RunUIThread()
        {
            try
            {
                // Register window class
                if (!isClassRegistered)
                {
                    var wndClass = new WNDCLASSEX
                    {
                        cbSize = (uint)Marshal.SizeOf(typeof(WNDCLASSEX)),
                        style = 0,
                        lpfnWndProc = Marshal.GetFunctionPointerForDelegate(wndProcDelegate),
                        cbClsExtra = 0,
                        cbWndExtra = 0,
                        hInstance = moduleHandle,
                        hIcon = IntPtr.Zero,
                        hCursor = IntPtr.Zero,
                        hbrBackground = CreateSolidBrush(RgbToBgr(config.BackgroundColor)),
                        lpszMenuName = null,
                        lpszClassName = WINDOW_CLASS_NAME,
                        hIconSm = IntPtr.Zero
                    };

                    classAtom = RegisterClassEx(ref wndClass);
                    if (classAtom == 0)
                    {
                        int error = Marshal.GetLastWin32Error();
                        Logger.Error($"Failed to register AppSwitcher window class, error: {error}");
                        windowReadyEvent.Set();
                        return;
                    }

                    isClassRegistered = true;
                    Thread.Sleep(50);
                }

                // Create fonts
                normalFont = CreateFont(-14, 0, 0, 0, FW_NORMAL, 0, 0, 0, DEFAULT_CHARSET,
                    OUT_DEFAULT_PRECIS, CLIP_DEFAULT_PRECIS, ANTIALIASED_QUALITY,
                    DEFAULT_PITCH | FF_DONTCARE, "Segoe UI");

                boldFont = CreateFont(-16, 0, 0, 0, FW_BOLD, 0, 0, 0, DEFAULT_CHARSET,
                    OUT_DEFAULT_PRECIS, CLIP_DEFAULT_PRECIS, ANTIALIASED_QUALITY,
                    DEFAULT_PITCH | FF_DONTCARE, "Segoe UI");

                // Create an initial hidden window to receive messages, offscreen!!!
                Logger.Info($"Creating window with atom: {classAtom}, hInstance: {moduleHandle}");

                windowHandle = CreateWindowExAtom(
                    WS_EX_TOOLWINDOW, // tool window-> so it doesn't appear on taskbar 
                    classAtom,
                    "App Switcher",
                    WS_POPUP,
                    -10000, -10000, 1, 1, // off-screen with minimal size
                    IntPtr.Zero, IntPtr.Zero, moduleHandle, IntPtr.Zero
                );

                if (windowHandle == IntPtr.Zero)
                {
                    int error = Marshal.GetLastWin32Error();
                    Logger.Error($"Failed to create AppSwitcher initial window, error: {error}");
                    Logger.Error($"Class registered: {isClassRegistered}, hInstance: {moduleHandle}");
                    windowReadyEvent.Set(); 
                    return;
                }

                Logger.Info($"AppSwitcher initial window created successfully, handle: {windowHandle}");
                windowReadyEvent.Set(); 

                // Message loop
                while (isRunning)
                {
                    if (GetMessage(out MSG msg, IntPtr.Zero, 0, 0))
                    {
                        TranslateMessage(ref msg);
                        DispatchMessage(ref msg);
                    }
                    else
                    {
                        break;
                    }
                }

                // Cleanup
                if (windowHandle != IntPtr.Zero)
                {
                    DestroyWindow(windowHandle);
                    windowHandle = IntPtr.Zero;
                }

                if (normalFont != IntPtr.Zero)
                    DeleteObject(normalFont);
                if (boldFont != IntPtr.Zero)
                    DeleteObject(boldFont);

                if (isClassRegistered)
                {
                    UnregisterClass(WINDOW_CLASS_NAME, moduleHandle);
                    isClassRegistered = false;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error in AppSwitcher UI thread");
            }
        }

        private void CreateWindowIfNeeded()
        {

            if (activeMonitor == null)
            {
                return;
            }

            if (windowHandle == IntPtr.Zero)
            {
                return;
            }

            int width = config.Width;
            int height = config.Height;
            int x = activeMonitor.WorkArea.Left + (activeMonitor.WorkArea.Width - width) / 2;
            int y = activeMonitor.WorkArea.Top + (activeMonitor.WorkArea.Height - height) / 2;


            const uint SWP_NOZORDER = 0x0004;
            const uint SWP_NOACTIVATE = 0x0010;
            SetWindowPos(windowHandle, IntPtr.Zero, x, y, width, height, SWP_NOZORDER | SWP_NOACTIVATE);

            SetWindowLong(windowHandle, GWL_EXSTYLE, WS_EX_TOPMOST | WS_EX_LAYERED | WS_EX_TOOLWINDOW);

            SetLayeredWindowAttributes(windowHandle, 0x000000, 0, LWA_COLORKEY);

        }

        private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                switch (msg)
                {
                    case WM_SHOW_SWITCHER:
                        CreateWindowIfNeeded();
                        if (windowHandle != IntPtr.Zero)
                        {
                            ShowWindow(windowHandle, SW_SHOW);
                            SetForegroundWindow(windowHandle);
                            isVisible = true;
                            InvalidateRect(windowHandle, IntPtr.Zero, false);
                        }
                        else
                        {
                            Logger.Error("windowHandle is Zero in WM_SHOW_SWITCHER");
                        }
                        return IntPtr.Zero;

                    case WM_HIDE_SWITCHER:
                        if (windowHandle != IntPtr.Zero)
                        {
                            ShowWindow(windowHandle, SW_HIDE);
                            isVisible = false;
                        }
                        return IntPtr.Zero;

                    case WM_PAINT:
                        if (isVisible)
                            HandlePaint(hWnd);
                        return IntPtr.Zero;

                    case WM_KEYDOWN:
                        HandleKeyDown((int)wParam);
                        return IntPtr.Zero;

                    case WM_CHAR:
                        HandleChar((char)wParam);
                        return IntPtr.Zero;

                    case WM_KILLFOCUS:
                        Hide();
                        return IntPtr.Zero;

                    case WM_DESTROY:
                        PostQuitMessage(0);
                        return IntPtr.Zero;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error in AppSwitcher WndProc");
            }

            return DefWindowProc(hWnd, msg, wParam, lParam);
        }

        private void HandleKeyDown(int virtualKey)
        {
            lock (lockObject)
            {
                switch (virtualKey)
                {
                    case VK_ESCAPE:
                        Hide();
                        break;

                    case VK_RETURN:
                        if (filteredWindows.Count > 0 && selectedIndex < filteredWindows.Count)
                        {
                            var selected = filteredWindows[selectedIndex];
                            Hide();
                            WindowSelected?.Invoke(selected.WindowHandle);
                        }
                        break;

                    case VK_UP:
                        if (selectedIndex > 0)
                        {
                            selectedIndex--;
                            InvalidateRect(windowHandle, IntPtr.Zero, false);
                        }
                        break;

                    case VK_DOWN:
                        if (selectedIndex < filteredWindows.Count - 1)
                        {
                            selectedIndex++;
                            InvalidateRect(windowHandle, IntPtr.Zero, false);
                        }
                        break;

                    case VK_BACK:
                        if (searchText.Length > 0)
                        {
                            searchText = searchText.Substring(0, searchText.Length - 1);
                            UpdateFiltered();
                            InvalidateRect(windowHandle, IntPtr.Zero, false);
                        }
                        break;
                }
            }
        }

        private void HandleChar(char c)
        {
            if (c < 32) return;

            lock (lockObject)
            {
                searchText += c;
                UpdateFiltered();
                InvalidateRect(windowHandle, IntPtr.Zero, false);
            }
        }

        private void UpdateFiltered()
        {
            filteredWindows = FuzzyMatcher.FilterAndSort(allWindows, searchText, config.MaxResults);
            selectedIndex = 0;
        }
        private void HandlePaint(IntPtr hWnd)
        {
            var ps = new PAINTSTRUCT();
            IntPtr hdc = BeginPaint(hWnd, ref ps);

            try
            {
                GetClientRect(hWnd, out RECT clientRect);

                // create off-screen buffer
                IntPtr memDC = CreateCompatibleDC(hdc);
                IntPtr memBitmap = CreateCompatibleBitmap(hdc, clientRect.Width, clientRect.Height);
                IntPtr oldBitmap = SelectObject(memDC, memBitmap);

                // clear background
                IntPtr bgBrush = CreateSolidBrush(RgbToBgr(config.BackgroundColor));
                FillRect(memDC, ref clientRect, bgBrush);
                DeleteObject(bgBrush);

                lock (lockObject)
                {
                    // draw search box
                    DrawSearchBox(memDC, clientRect);

                    // Draw results
                    DrawResults(memDC, clientRect);
                }

                // blit to screen
                BitBlt(hdc, 0, 0, clientRect.Width, clientRect.Height, memDC, 0, 0, SRCCOPY);

                // cleanup
                SelectObject(memDC, oldBitmap);
                DeleteObject(memBitmap);
                DeleteDC(memDC);
            }
            finally
            {
                EndPaint(hWnd, ref ps);
            }
        }

        private void DrawSearchBox(IntPtr hdc, RECT clientRect)
        {
            var searchRect = new RECT
            {
                Left = 10,
                Top = 10,
                Right = clientRect.Width - 10,
                Bottom = 10 + config.SearchBoxHeight
            };

            // draw search box background
            IntPtr searchBgBrush = CreateSolidBrush(RgbToBgr(DarkenColor(config.BackgroundColor, 0.9f)));
            FillRect(hdc, ref searchRect, searchBgBrush);
            DeleteObject(searchBgBrush);

            // draw search text
            SetBkMode(hdc, TRANSPARENT);
            SetTextColor(hdc, RgbToBgr(config.TextColor));
            IntPtr oldFont = SelectObject(hdc, normalFont);

            string displayText = "Search: " + searchText + "_";
            var textRect = new RECT
            {
                Left = searchRect.Left + 10,
                Top = searchRect.Top,
                Right = searchRect.Right - 10,
                Bottom = searchRect.Bottom
            };
            DrawText(hdc, displayText, -1, ref textRect, DT_LEFT | DT_VCENTER | DT_SINGLELINE);

            SelectObject(hdc, oldFont);
        }

        private void DrawResults(IntPtr hdc, RECT clientRect)
        {
            int yOffset = 10 + config.SearchBoxHeight + 10;
            int itemsDrawn = 0;
            int maxVisible = config.MaxResults;

            for (int i = 0; i < filteredWindows.Count && itemsDrawn < maxVisible; i++)
            {
                bool isSelected = (i == selectedIndex);
                DrawResultItem(hdc, filteredWindows[i], yOffset, isSelected, clientRect.Width);
                yOffset += config.ItemHeight;
                itemsDrawn++;
            }

            // draw "no results" message if empty
            if (filteredWindows.Count == 0)
            {
                SetBkMode(hdc, TRANSPARENT);
                SetTextColor(hdc, RgbToBgr(config.SubtitleColor));
                IntPtr oldFont = SelectObject(hdc, normalFont);

                var textRect = new RECT
                {
                    Left = 10,
                    Top = yOffset,
                    Right = clientRect.Width - 10,
                    Bottom = yOffset + config.ItemHeight
                };
                DrawText(hdc, "No matching windows", -1, ref textRect, DT_LEFT | DT_VCENTER | DT_SINGLELINE);

                SelectObject(hdc, oldFont);
            }
        }

        private void DrawResultItem(IntPtr hdc, WindowSearchEntry entry, int yOffset, bool isSelected, int totalWidth)
        {
            var itemRect = new RECT
            {
                Left = 10,
                Top = yOffset,
                Right = totalWidth - 10,
                Bottom = yOffset + config.ItemHeight
            };

            // background
            uint bgColor = isSelected ? config.SelectedColor : config.BackgroundColor;
            IntPtr bgBrush = CreateSolidBrush(RgbToBgr(bgColor));
            FillRect(hdc, ref itemRect, bgBrush);
            DeleteObject(bgBrush);

            // icon
            if (entry.Icon != IntPtr.Zero)
            {
                DrawIconEx(hdc, itemRect.Left + 8, yOffset + 9, entry.Icon, 32, 32, 0, IntPtr.Zero, DI_NORMAL);
            }

            // title
            SetBkMode(hdc, TRANSPARENT);
            SetTextColor(hdc, RgbToBgr(config.TextColor));
            IntPtr oldFont = SelectObject(hdc, boldFont);

            var titleRect = new RECT
            {
                Left = itemRect.Left + 48,
                Top = yOffset + 8,
                Right = itemRect.Right - 10,
                Bottom = yOffset + 28
            };
            DrawText(hdc, entry.Title, -1, ref titleRect, DT_LEFT | DT_VCENTER | DT_SINGLELINE | DT_END_ELLIPSIS);

            // subtitle (workspace/monitor info)
            SelectObject(hdc, normalFont);
            SetTextColor(hdc, RgbToBgr(config.SubtitleColor));

            var subtitleRect = new RECT
            {
                Left = itemRect.Left + 48,
                Top = yOffset + 28,
                Right = itemRect.Right - 10,
                Bottom = yOffset + 45
            };
            string subtitle = $"Workspace {entry.WorkspaceId} - Monitor {entry.MonitorIndex + 1}";
            DrawText(hdc, subtitle, -1, ref subtitleRect, DT_LEFT | DT_VCENTER | DT_SINGLELINE);

            SelectObject(hdc, oldFont);
        }

        private uint RgbToBgr(uint rgb)
        {
            return ((rgb & 0xFF) << 16) | (rgb & 0xFF00) | ((rgb >> 16) & 0xFF);
        }

        private uint DarkenColor(uint color, float factor)
        {
            byte r = (byte)((color >> 16) & 0xFF);
            byte g = (byte)((color >> 8) & 0xFF);
            byte b = (byte)(color & 0xFF);

            r = (byte)(r * factor);
            g = (byte)(g * factor);
            b = (byte)(b * factor);

            return ((uint)r << 16) | ((uint)g << 8) | b;
        }

        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    }
}
