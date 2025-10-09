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
using System.Threading.Tasks;

namespace TilingWindowManager
{
    class Program
    {
        private static WindowManager? staticWindowManager = null;

#if !DEBUG
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;
#endif

        [STAThread]
        static void Main(string[] args)
        {
            bool debugMode = args.Length > 0 && (args[0].Equals("--debug", StringComparison.OrdinalIgnoreCase) ||
                                                  args[0].Equals("-d", StringComparison.OrdinalIgnoreCase));

#if !DEBUG
            if (!debugMode)
            {
                var handle = GetConsoleWindow();
                ShowWindow(handle, SW_HIDE);
            }
#endif

            if (debugMode)
            {
                Logger.EnableLogging(true);
            }

            Logger.Info("=== TWM starting ===");

            WindowManager? windowManager = null;

            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true; // prevent immediate termination
                staticWindowManager?.Cleanup();
                Environment.Exit(0);
            };

            AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
            {
                staticWindowManager?.Cleanup();
            };

            try
            {
                HotKey hotKey = new HotKey();
                windowManager = new WindowManager();
                staticWindowManager = windowManager;
                windowManager.Run(hotKey);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error occurred");
            }
            finally
            {
                windowManager?.Cleanup();
                Logger.Info("Shutdown complete. Press any key to exit.");
                Logger.CloseAndFlush();
                Console.ReadKey();
            }
        }
    }
}