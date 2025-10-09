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
using System.IO;
using Tomlyn;
using Tomlyn.Model;

namespace TilingWindowManager
{
    public class AllowedOwnedWindowsConfiguration
    {
        private const string CONFIG_FILE_NAME = "config.toml";
        private readonly HashSet<string> _allowedOwnedWindows = new(StringComparer.OrdinalIgnoreCase);

        public IReadOnlySet<string> AllowedOwnedWindows => _allowedOwnedWindows;

        public bool LoadConfiguration()
        {
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CONFIG_FILE_NAME);

                if (!File.Exists(configPath))
                {
                    return false;
                }

                string tomlContent = File.ReadAllText(configPath);
                var model = Toml.ToModel(tomlContent);

                _allowedOwnedWindows.Clear();

                if (model.TryGetValue("allowed_owned_windows", out var allowedObj) && allowedObj is TomlArray allowedArray)
                {
                    foreach (var item in allowedArray)
                    {
                        if (item is string appName && !string.IsNullOrWhiteSpace(appName))
                        {
                            _allowedOwnedWindows.Add(appName);
                        }
                    }

                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error loading allowed owned windows configuration");
                return false;
            }
        }

        public bool IsAllowed(string processName)
        {
            if (string.IsNullOrEmpty(processName))
                return false;

            return _allowedOwnedWindows.Contains(processName);
        }
    }
}
