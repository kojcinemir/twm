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
using System.IO;
using Tomlyn;
using Tomlyn.Model;

namespace TilingWindowManager
{
    public class WorkspaceConfiguration
    {
        private const string CONFIG_FILE_NAME = "config.toml";

        public bool StackedOnStartup { get; private set; } = false;
        public bool PausedOnStartup { get; private set; } = true;

        public bool LoadConfiguration()
        {
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CONFIG_FILE_NAME);

                if (!File.Exists(configPath))
                {
                    Logger.Info("Config file not found, using default workspace settings");
                    return false;
                }

                string tomlContent = File.ReadAllText(configPath);
                var model = Toml.ToModel(tomlContent);

                if (model.TryGetValue("workspace", out var workspaceObj) && workspaceObj is TomlTable workspaceTable)
                {
                    StackedOnStartup = GetBoolValue(workspaceTable, "stacked_on_startup", StackedOnStartup);
                    PausedOnStartup = GetBoolValue(workspaceTable, "paused_on_startup", PausedOnStartup);
                    return true;
                }
                else
                {
                    // No workspace section found, use defaults
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error loading workspace configuration");
                return false;
            }
        }

        private bool GetBoolValue(TomlTable table, string key, bool defaultValue)
        {
            if (table.TryGetValue(key, out var value))
            {
                if (value is bool boolValue)
                    return boolValue;
                if (bool.TryParse(value.ToString(), out var parsedValue))
                    return parsedValue;
            }
            return defaultValue;
        }
    }
}
