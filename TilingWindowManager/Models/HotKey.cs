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
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static TilingWindowManager.HotKey;

namespace TilingWindowManager
{
    public class HotKey
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(nint hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(nint hWnd, int id);


        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool PeekMessage(out MSG lpMsg, nint hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern ushort RegisterClass(ref WNDCLASS lpWndClass);

        public delegate nint WndProc(nint hWnd, uint msg, nint wParam, nint lParam);

        [DllImport("kernel32.dll")]
        private static extern nint GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern nint CreateWindowEx(
            uint dwExStyle, string lpClassName, string lpWindowName, uint dwStyle,
            int x, int y, int nWidth, int nHeight, nint hWndParent,
            nint hMenu, nint hInstance, nint lpParam);

        [DllImport("user32.dll")]
        private static extern nint DefWindowProc(nint hWnd, uint uMsg, nint wParam, nint lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyWindow(nint hWnd);

        [DllImport("user32.dll", SetLastError = false)]
        private static extern bool WaitMessage();

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

        [StructLayout(LayoutKind.Sequential)]
        public struct MSG
        {
            public nint hwnd;
            public uint message;
            public nint wParam;
            public nint lParam;
            public uint time;
            public POINT pt;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        public const uint WM_HOTKEY = 0x0312;
        public const uint WM_QUIT = 0x0012;
        public const uint PM_REMOVE = 0x0001;

        public nint messageWindow;
        private const string WINDOW_CLASS_NAME = "TilingWMHotKeyWindow";
        private WndProc wndProc = null!; // strong reference to prevent GC
        private GCHandle wndProcHandle; // GCHandle to pin delegate
        private bool isDisposed = false;
        private bool isProcessingMessage = false; 
        private int consecutiveMessageCount = 0;
        private const int MAX_CONSECUTIVE_MESSAGES = 100;
        private HotKeyConfiguration _config;
        private ApplicationHotkeysConfiguration _appHotkeysConfig;

        public HotKeyConfiguration Configuration => _config;
        public ApplicationHotkeysConfiguration ApplicationHotkeysConfiguration => _appHotkeysConfig;

        public HotKey()
        {
            _config = new HotKeyConfiguration();
            _config.LoadConfiguration();

            _appHotkeysConfig = new ApplicationHotkeysConfiguration();
            _appHotkeysConfig.LoadConfiguration();

            InitializeMessageWindow();
            InitializeHotKeys();
            InitializeApplicationHotKeys();
        }

        private void InitializeMessageWindow()
        {
            wndProc = new WndProc(WindowProc);
            wndProcHandle = GCHandle.Alloc(wndProc);

            WNDCLASS wndClass = new WNDCLASS
            {
                lpfnWndProc = wndProc,
                hInstance = GetModuleHandle(null!),
                lpszClassName = WINDOW_CLASS_NAME
            };

            ushort classAtom = RegisterClass(ref wndClass);
            if (classAtom == 0)
            {
                throw new InvalidOperationException("Failed to register window class");
            }

            messageWindow = CreateWindowEx(
                0, WINDOW_CLASS_NAME, "TilingWM Message Window", 0,
                0, 0, 0, 0, nint.Zero, nint.Zero,
                GetModuleHandle(null!), nint.Zero);

            if (messageWindow == nint.Zero)
            {
                throw new InvalidOperationException("Failed to create message window");
            }
        }

        private void InitializeHotKeys()
        {
            foreach (var hotkey in _config.AllHotKeys)
            {
                uint modifiers = _config.GetModifiers(hotkey);
                bool success = RegisterHotKey(messageWindow, (int)hotkey.HotkeyId, modifiers, hotkey.KeyCode);

                if (!success)
                {
                    int error = Marshal.GetLastWin32Error();
                    if (error == 1409)
                    {
                        Logger.Warning($"Hotkey {hotkey.Name} already registered by another application");
                    }
                    else
                    {
                        Logger.Error($"Failed to register hotkey {hotkey.Name}. Error: {error}");
                    }
                }
                else
                {
                    Logger.Info($"Registered hotkey: {hotkey.Name}");
                }
            }
        }

        private void InitializeApplicationHotKeys()
        {
            if (!_appHotkeysConfig.HasHotkeys)
            {
                return;
            }

            foreach (var hotkey in _appHotkeysConfig.AllApplicationHotkeys)
            {
                uint modifiers = _appHotkeysConfig.GetModifiers(hotkey);
                bool success = RegisterHotKey(messageWindow, (int)hotkey.HotkeyId, modifiers, hotkey.KeyCode);

                if (!success)
                {
                    int error = Marshal.GetLastWin32Error();
                    if (error == 1409)
                    {
                        Logger.Warning($"Application hotkey {hotkey.KeyCombination} already registered by another application");
                    }
                    else
                    {
                        Logger.Error($"Failed to register application hotkey {hotkey.KeyCombination}. Error: {error}");
                    }
                }
                else
                {
                    Logger.Info($"Registered application hotkey: {hotkey.KeyCombination} -> {hotkey.ExecutableName}");
                }
            }
        }

        public string GetHotKeyName(int hotkeyId)
        {
            var hotkey = _config.GetHotKeyById((uint)hotkeyId);
            return hotkey?.Name ?? "Unknown";
        }

        private nint WindowProc(nint hWnd, uint msg, nint wParam, nint lParam)
        {
            if (msg == WM_HOTKEY)
            {
                int hotkeyId = wParam.ToInt32();
                return nint.Zero;
            }

            return DefWindowProc(hWnd, msg, wParam, lParam);
        }

        public MSG? CheckReceivedKey()
        {
            if (isDisposed || isProcessingMessage)
                return null;

            // prevent message overflow by limiting consecutive processing
            if (consecutiveMessageCount >= MAX_CONSECUTIVE_MESSAGES)
            {
                consecutiveMessageCount = 0;
                Thread.Sleep(1);
                return null;
            }

            try
            {
                isProcessingMessage = true;
                MSG msg;
                bool messageAvailable = PeekMessage(out msg, messageWindow, 0, 0, PM_REMOVE);

                if (!messageAvailable)
                {
                    consecutiveMessageCount = 0; // reset counter when no messages
                    return null;
                }

                consecutiveMessageCount++;
                return msg;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error in CheckReceivedKey");
                consecutiveMessageCount = 0;
                return null;
            }
            finally
            {
                isProcessingMessage = false;
            }
        }

        public void WaitForNextMessage()
        {
            try
            {
                WaitMessage();
            }
            catch
            {
            }
        }

        public void CleanUp()
        {
            if (isDisposed) return;

            foreach (var hotkey in _config.AllHotKeys)
            {
                UnregisterHotKey(messageWindow, (int)hotkey.HotkeyId);
            }

            foreach (var appHotkey in _appHotkeysConfig.AllApplicationHotkeys)
            {
                UnregisterHotKey(messageWindow, (int)appHotkey.HotkeyId);
            }

            if (messageWindow != nint.Zero)
            {
                DestroyWindow(messageWindow);
                messageWindow = nint.Zero;
            }

            if (wndProcHandle.IsAllocated)
            {
                wndProcHandle.Free();
            }
            wndProc = null!;
            isDisposed = true;
        }
    }
}