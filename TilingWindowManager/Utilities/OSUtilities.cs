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
using System.Runtime.InteropServices;

namespace TilingWindowManager
{
    public static class OSUtilities
    {
        [DllImport("ntdll.dll", SetLastError = true)]
        private static extern int RtlGetVersion(out OSVERSIONINFOEX versionInfo);

        [StructLayout(LayoutKind.Sequential)]
        private struct OSVERSIONINFOEX
        {
            public int dwOSVersionInfoSize;
            public int dwMajorVersion;
            public int dwMinorVersion;
            public int dwBuildNumber;
            public int dwPlatformId;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szCSDVersion;
            public ushort wServicePackMajor;
            public ushort wServicePackMinor;
            public ushort wSuiteMask;
            public byte wProductType;
            public byte wReserved;
        }

        public enum WindowsVersion
        {
            Unknown,
            Windows10,
            Windows11
        }

        public static WindowsVersion GetWindowsVersion()
        {
            try
            {
                var versionInfo = new OSVERSIONINFOEX();
                versionInfo.dwOSVersionInfoSize = Marshal.SizeOf(versionInfo);

                if (RtlGetVersion(out versionInfo) == 0)
                {
                    if (versionInfo.dwMajorVersion == 10)
                    {
                        if (versionInfo.dwBuildNumber >= 22000)
                        {
                            return WindowsVersion.Windows11;
                        }
                        else
                        {
                            return WindowsVersion.Windows10;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to detect Windows version");
            }

            return WindowsVersion.Unknown;
        }

        public static bool IsWindows10()
        {
            return GetWindowsVersion() == WindowsVersion.Windows10;
        }

        public static bool IsWindows11()
        {
            return GetWindowsVersion() == WindowsVersion.Windows11;
        }
    }
}