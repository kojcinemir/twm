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
    public class PinnedApplicationsConfiguration
    {
        private const string CONFIG_FILE_NAME = "config.toml";
        private readonly Dictionary<string, int> _pinnedApplications = new();

        public IReadOnlyDictionary<string, int> PinnedApplications => _pinnedApplications;

        public bool LoadConfiguration()
        {
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CONFIG_FILE_NAME);

                if (!File.Exists(configPath))
                {
                    Logger.Info("Config file not found, no pinned applications configured");
                    return false;
                }

                string tomlContent = File.ReadAllText(configPath);
                var model = Toml.ToModel(tomlContent);

                _pinnedApplications.Clear();

                if (model.TryGetValue("pinned_applications", out var pinnedObj) && pinnedObj is TomlTable pinnedTable)
                {
                    foreach (var kvp in pinnedTable)
                    {
                        string executableName = kvp.Key.ToLowerInvariant();
                        if (kvp.Value is long workspaceNumber && workspaceNumber >= 1 && workspaceNumber <= 8)
                        {
                            _pinnedApplications[executableName] = (int)workspaceNumber;
                            Logger.Info($"Pinned application configured: {executableName} -> workspace {workspaceNumber}");
                        }
                        else if (kvp.Value is int workspaceInt && workspaceInt >= 1 && workspaceInt <= 8)
                        {
                            _pinnedApplications[executableName] = workspaceInt;
                            Logger.Info($"Pinned application configured: {executableName} -> workspace {workspaceInt}");
                        }
                        else
                        {
                            Logger.Warning($"Invalid workspace number for {executableName}: {kvp.Value}. Must be between 1-8.");
                        }
                    }

                    Logger.Info($"Loaded {_pinnedApplications.Count} pinned application(s)");
                    return true;
                }
                else
                {
                    Logger.Info("No [pinned_applications] section found in config");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error loading pinned applications configuration");
                return false;
            }
        }

        public int? GetPinnedWorkspace(string executableName)
        {
            if (string.IsNullOrEmpty(executableName))
                return null;

            string normalizedName = executableName.ToLowerInvariant();
            return _pinnedApplications.TryGetValue(normalizedName, out int workspace) ? workspace : null;
        }

        public bool IsApplicationPinned(string executableName)
        {
            if (string.IsNullOrEmpty(executableName))
                return false;

            string normalizedName = executableName.ToLowerInvariant();
            return _pinnedApplications.ContainsKey(normalizedName);
        }
    }
}