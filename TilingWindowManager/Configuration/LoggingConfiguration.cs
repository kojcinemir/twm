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
    public class LoggingConfiguration
    {
        public bool Enabled { get; set; } = true;
        public string Level { get; set; } = "info";

        public static LoggingConfiguration LoadFromFile(string configPath)
        {
            var config = new LoggingConfiguration();

            try
            {
                if (!File.Exists(configPath))
                {
                    return config; // return default configuration
                }

                var tomlContent = File.ReadAllText(configPath);
                var model = Toml.ToModel(tomlContent);

                if (model.TryGetValue("logging", out var loggingObj) && loggingObj is TomlTable loggingTable)
                {
                    if (loggingTable.TryGetValue("enabled", out var enabledObj) && enabledObj is bool enabled)
                    {
                        config.Enabled = enabled;
                    }

                    if (loggingTable.TryGetValue("level", out var levelObj) && levelObj is string level)
                    {
                        config.Level = level.ToLowerInvariant();
                    }
                }
            }
            catch (Exception ex)
            {
                // fallback to default configuration
                Logger.Error($"Failed to load logging configuration: {ex.Message}");
            }

            return config;
        }
    }
}