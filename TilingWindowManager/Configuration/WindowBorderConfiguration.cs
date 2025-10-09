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
    public class WindowBorderConfiguration
    {
        private const string CONFIG_FILE_NAME = "config.toml";

        public int BorderWidth { get; private set; } = 4;
        public uint BorderColor { get; private set; } = 0xF4DB91;
        public bool RoundedBorders { get; private set; } = false;
        public int CornerRadius { get; private set; } = 8;
        public byte Opacity { get; private set; } = 255;

        public bool LoadConfiguration()
        {
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CONFIG_FILE_NAME);

                if (!File.Exists(configPath))
                {
                    Logger.Info("Config file not found, using default window border settings");
                    return false;
                }

                string tomlContent = File.ReadAllText(configPath);
                var model = Toml.ToModel(tomlContent);

                if (model.TryGetValue("window_border", out var borderObj) && borderObj is TomlTable borderTable)
                {
                    BorderWidth = GetIntValue(borderTable, "border_width", BorderWidth);
                    BorderColor = GetUintValue(borderTable, "border_color", BorderColor);
                    RoundedBorders = GetBoolValue(borderTable, "rounded_borders", RoundedBorders);
                    CornerRadius = GetIntValue(borderTable, "corner_radius", CornerRadius);
                    Opacity = GetByteValue(borderTable, "opacity", Opacity);

                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error loading window border configuration");
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

        private byte GetByteValue(TomlTable table, string key, byte defaultValue)
        {
            if (table.TryGetValue(key, out var value))
            {
                if (value is long longValue && longValue >= 0 && longValue <= 255)
                    return (byte)longValue;
                if (value is int intValue && intValue >= 0 && intValue <= 255)
                    return (byte)intValue;
                if (byte.TryParse(value.ToString(), out var parsedValue))
                    return parsedValue;
            }
            return defaultValue;
        }
    }
}