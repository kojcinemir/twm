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
using System.Text;

namespace TilingWindowManager
{
    public class Windows
    {
        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(nint hWnd, nint hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(nint hWnd, out Monitor.RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(nint hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(nint hWnd);

        [DllImport("user32.dll")]
        private static extern int GetWindowText(nint hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern nint GetParent(nint hWnd);

        [DllImport("user32.dll")]
        private static extern uint GetWindowLong(nint hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetClassName(nint hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(nint hWnd);

        [DllImport("user32.dll")]
        private static extern nint BeginDeferWindowPos(int nNumWindows);

        [DllImport("user32.dll")]
        private static extern nint DeferWindowPos(nint hWinPosInfo, nint hWnd, nint hWndInsertAfter,
            int x, int y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool EndDeferWindowPos(nint hWinPosInfo);

        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_FRAMECHANGED = 0x0020;
        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;
        private const int SW_RESTORE = 9;
        private const uint GW_OWNER = 4;
        private const int GWL_EXSTYLE = -20;
        private const int GWL_STYLE = -16;
        private const uint WS_EX_TOOLWINDOW = 0x00000080;

        private List<nint> windowList;
        private Dictionary<nint, WindowInfo> windowInfoCache;

        public class WindowInfo
        {
            public string Title { get; set; } = "";
            public string ClassName { get; set; } = "";
            public Monitor.RECT Bounds { get; set; }
            public bool IsVisible { get; set; }
            public bool IsMinimized { get; set; }
            public DateTime LastUpdated { get; set; }
        }

        public Windows()
        {
            windowList = new List<nint>();
            windowInfoCache = new Dictionary<nint, WindowInfo>();
        }

        public List<nint> GetAllWindows()
        {
            return new List<nint>(windowList);
        }

        public int Count => windowList.Count;

        public bool Contains(nint window)
        {
            return windowList.Contains(window);
        }

        public void AddWindow(nint window)
        {
            if (!windowList.Contains(window))
            {
                windowList.Add(window);
                RefreshWindowInfo(window);
            }
        }

        public bool RemoveWindow(nint window)
        {
            if (windowList.Remove(window))
            {
                windowInfoCache.Remove(window);
                return true;
            }
            return false;
        }

        public void ClearWindows()
        {
            windowList.Clear();
            windowInfoCache.Clear();
        }

        public WindowInfo? GetWindowInfo(nint window)
        {
            if (windowInfoCache.TryGetValue(window, out WindowInfo? info))
            {
                if (DateTime.Now - info.LastUpdated > TimeSpan.FromSeconds(1))
                {
                    RefreshWindowInfo(window);
                    return windowInfoCache[window];
                }
                return info;
            }

            RefreshWindowInfo(window);
            return windowInfoCache.GetValueOrDefault(window);
        }

        public void RefreshWindowInfo(nint window)
        {
            var info = new WindowInfo
            {
                Title = GetWindowTitle(window),
                ClassName = GetWindowClassName(window),
                IsVisible = IsWindowVisible(window),
                IsMinimized = IsIconic(window),
                LastUpdated = DateTime.Now
            };

            if (GetWindowRect(window, out Monitor.RECT rect))
            {
                info.Bounds = rect;
            }

            windowInfoCache[window] = info;
        }

        public void PositionWindow(nint window, Monitor.RECT bounds)
        {
            try
            {
                ShowWindow(window, SW_RESTORE);

                if (GetWindowRect(window, out Monitor.RECT cur))
                {
                    int tx = bounds.Left;
                    int ty = bounds.Top;
                    int tw = Math.Max(bounds.Width, 200);
                    int th = Math.Max(bounds.Height, 150);

                    bool same = Math.Abs(cur.Left - tx) <= 1 &&
                                Math.Abs(cur.Top - ty) <= 1 &&
                                Math.Abs((cur.Right - cur.Left) - tw) <= 1 &&
                                Math.Abs((cur.Bottom - cur.Top) - th) <= 1;

                    if (same)
                    {
                        return;
                    }
                }

                SetWindowPos(window, nint.Zero, bounds.Left, bounds.Top,
                    Math.Max(bounds.Width, 200), Math.Max(bounds.Height, 150),
                    SWP_NOZORDER | SWP_NOACTIVATE);

            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error positioning window {window}");
            }
        }

        public void PositionWindowsBatch(IEnumerable<(nint hwnd, Monitor.RECT rect)> items)
        {
            var list = items.ToList();
            if (list.Count == 0) return;

            var hInfo = BeginDeferWindowPos(list.Count);
            foreach (var (hwnd, r) in list)
            {
                int x = r.Left;
                int y = r.Top;
                int w = Math.Max(r.Width, 200);
                int h = Math.Max(r.Height, 150);
                hInfo = DeferWindowPos(hInfo, hwnd, nint.Zero, x, y, w, h, SWP_NOZORDER | SWP_NOACTIVATE);
            }
            EndDeferWindowPos(hInfo);
        }

        public void ShowWindow(nint window)
        {
            if (window != nint.Zero && windowList.Contains(window))
            {
                ShowWindow(window, SW_SHOW);
            }
        }

        public void HideWindow(nint window)
        {
            if (window != nint.Zero && windowList.Contains(window))
            {
                ShowWindow(window, SW_HIDE);
            }
        }

        public void ShowAllWindows()
        {
            foreach (var window in windowList.ToList())
            {
                ShowWindow(window);
            }
        }

        public void HideAllWindows()
        {
            foreach (var window in windowList.ToList())
            {
                HideWindow(window);
            }
        }

        public List<nint> GetVisibleWindows()
        {
            return windowList.Where(w => IsValidWindow(w) && IsWindowVisible(w)).ToList();
        }

        public List<nint> GetTileableWindows()
        {
            return windowList.Where(w => IsValidTileableWindow(w) && !IsIconic(w) && IsWindowVisible(w)).ToList();
        }

        public List<nint> GetStackableWindows()
        {
            return windowList.Where(w => IsValidTileableWindow(w) && !IsIconic(w)).ToList();
        }

        public bool MoveWindowInList(nint window, int offset)
        {
            int currentIndex = windowList.IndexOf(window);
            if (currentIndex < 0)
                return false;

            int newIndex = currentIndex + offset;
            if (newIndex < 0 || newIndex >= windowList.Count)
                return false;

            windowList.RemoveAt(currentIndex);
            windowList.Insert(newIndex, window);
            return true;
        }

        private string GetWindowTitle(nint window)
        {
            var title = new StringBuilder(256);
            GetWindowText(window, title, title.Capacity);
            return title.ToString();
        }

        private string GetWindowClassName(nint window)
        {
            var className = new StringBuilder(256);
            GetClassName(window, className, className.Capacity);
            return className.ToString();
        }

        private bool IsValidWindow(nint hWnd)
        {
            if (hWnd == nint.Zero)
                return false;

            if (!IsWindowVisible(hWnd))
                return false;

            if (GetParent(hWnd) != nint.Zero)
                return false;

            uint exStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
            if ((exStyle & WS_EX_TOOLWINDOW) != 0)
                return false;

            return true;
        }

        private bool IsValidTileableWindow(nint hWnd)
        {
            if (GetWindowRect(hWnd, out Monitor.RECT windowRect))
            {
                int width = windowRect.Width;
                int height = windowRect.Height;

                if (width < 200 || height < 100)
                {
                    return false;
                }
            }

            return true;
        }

        public nint GetMostRecentWindow()
        {
            return windowList.LastOrDefault();
        }

        public void MoveWindowToFront(nint window)
        {
            if (windowList.Remove(window))
            {
                windowList.Add(window);
            }
        }

        public override string ToString()
        {
            return $"Windows: {windowList.Count} total, {GetVisibleWindows().Count} visible";
        }
    }
}