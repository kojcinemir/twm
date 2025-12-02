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
    public class AppSwitcherConfiguration
    {
        private const string CONFIG_FILE_NAME = "config.toml";

        public int Width { get; private set; } = 600;
        public int Height { get; private set; } = 400;
        public int MaxResults { get; private set; } = 8;
        public int ItemHeight { get; private set; } = 50;
        public int SearchBoxHeight { get; private set; } = 40;
        public int CornerRadius { get; private set; } = 8;

        public int GetCalculatedHeight()
        {
            int searchBoxArea = 10 + SearchBoxHeight + 10; 
            int itemsArea = MaxResults * ItemHeight;
            int bottomPadding = 10;
            return searchBoxArea + itemsArea + bottomPadding;
        }

        public uint BackgroundColor { get; private set; } = 0x1c1c1c;
        public uint SelectedColor { get; private set; } = 0x2a4a7c;
        public uint TextColor { get; private set; } = 0xFFFFFF;
        public uint SubtitleColor { get; private set; } = 0x888888;

        public bool LoadConfiguration()
        {
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CONFIG_FILE_NAME);

                if (!File.Exists(configPath))
                {
                    Logger.Info("Config file not found, using default app switcher settings");
                    return false;
                }

                string tomlContent = File.ReadAllText(configPath);
                var model = Toml.ToModel(tomlContent);

                // Load color scheme defaults first
                string? colorSchemeName = null;
                if (model.TryGetValue("color_scheme", out var schemeValue))
                {
                    colorSchemeName = schemeValue?.ToString();
                    if (!string.IsNullOrWhiteSpace(colorSchemeName))
                    {
                        var scheme = ColorScheme.GetScheme(colorSchemeName);
                        if (scheme != null)
                        {
                            BackgroundColor = scheme.BackgroundColor;
                            SelectedColor = scheme.StackedAppActiveColor;
                            TextColor = scheme.InactiveWorkspaceTextColor;
                            SubtitleColor = 0x888888; 
                        }
                    }
                }

                // Load app_switcher specific configuration
                if (model.TryGetValue("app_switcher", out var switcherObj) && switcherObj is TomlTable switcherTable)
                {
                    Width = GetIntValue(switcherTable, "width", Width);
                    Height = GetIntValue(switcherTable, "height", Height);
                    MaxResults = GetIntValue(switcherTable, "max_results", MaxResults);
                    ItemHeight = GetIntValue(switcherTable, "item_height", ItemHeight);
                    SearchBoxHeight = GetIntValue(switcherTable, "search_box_height", SearchBoxHeight);
                    CornerRadius = GetIntValue(switcherTable, "corner_radius", CornerRadius);

                    // Override colors if specified
                    if (switcherTable.ContainsKey("background_color"))
                        BackgroundColor = GetUintValue(switcherTable, "background_color", BackgroundColor);

                    if (switcherTable.ContainsKey("selected_color"))
                        SelectedColor = GetUintValue(switcherTable, "selected_color", SelectedColor);

                    if (switcherTable.ContainsKey("text_color"))
                        TextColor = GetUintValue(switcherTable, "text_color", TextColor);

                    if (switcherTable.ContainsKey("subtitle_color"))
                        SubtitleColor = GetUintValue(switcherTable, "subtitle_color", SubtitleColor);

                    return true;
                }
                else
                {
                    Logger.Info("app_switcher configuration not found, using defaults");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error loading app switcher configuration");
                return false;
            }
        }

        private int GetIntValue(TomlTable table, string key, int defaultValue)
        {
            if (table.TryGetValue(key, out var value))
            {
                if (value is long longValue)
                    return (int)longValue;
                if (value is int intValue)
                    return intValue;
                if (int.TryParse(value.ToString(), out var parsedValue))
                    return parsedValue;
            }
            return defaultValue;
        }

        private uint GetUintValue(TomlTable table, string key, uint defaultValue)
        {
            if (table.TryGetValue(key, out var value))
            {
                if (value is long longValue && longValue >= 0)
                    return (uint)longValue;
                if (value is int intValue && intValue >= 0)
                    return (uint)intValue;
                if (uint.TryParse(value.ToString(), out var parsedValue))
                    return parsedValue;
            }
            return defaultValue;
        }
    }
}
