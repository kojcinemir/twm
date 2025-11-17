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
    public class WorkspaceIndicatorConfiguration
    {
        private const string CONFIG_FILE_NAME = "config.toml";

        public int WorkspaceWidth { get; private set; } = 75;
        public int WorkspaceHeight { get; private set; } = 40;
        public int WorkspaceMargin { get; private set; } = 2;
        public int IconSize { get; private set; } = 16;
        public uint ActiveWorkspaceBorderColor { get; private set; } = 0xF4DB91;
        public uint BackgroundColor { get; private set; } = 0x1c1c1c;
        public uint ActiveWorkspaceColor { get; private set; } = 0x0f0f0f;
        public uint HoveredWorkspaceColor { get; private set; } = 0x383838;
        public uint InactiveWorkspaceColor { get; private set; } = 0x1c1c1c;
        public uint ActiveWorkspaceTextColor { get; private set; } = 0x00FFFF;
        public uint InactiveWorkspaceTextColor { get; private set; } = 0xFFFFFF;
        public uint StackedModeWorkspaceColor { get; private set; } = 0x2a4a7c;
        public uint StackedModeBorderColor { get; private set; } = 0x6495ED;
        public uint BackupWorkspaceColor { get; private set; } = 0x7c4a2a;
        public uint BackupWorkspaceBorderColor { get; private set; } = 0xED9564;
        public uint BackupAndStackedWorkspaceColor { get; private set; } = 0x5a3a5a;
        public uint BackupAndStackedBorderColor { get; private set; } = 0xC77DC7;
        public uint PausedWorkspaceColor { get; private set; } = 0x90EE90;
        public uint PausedWorkspaceBorderColor { get; private set; } = 0x90EE90;
        public int OffsetFromTaskbarLeftEdge { get; private set; } = 0;
        public byte ActiveWorkspaceBorderOpacity { get; private set; } = 255;

        public int Windows10OffsetFromTaskbarRightEdge { get; private set; } = 200;
        public bool UseWindows10Positioning { get; private set; } = false;

        // Stacked app display configuration
        public int StackedAppIconSize { get; private set; } = 24;
        public int StackedAppItemWidth { get; private set; } = 150;
        public int StackedAppItemWidthIconOnly { get; private set; } = 40;
        public int StackedAppTitleMaxLength { get; private set; } = 15;
        public bool ShowStackedAppTitle { get; private set; } = true;
        public uint StackedAppBackgroundColor { get; private set; } = 0x2a2a2a;
        public uint StackedAppHoverColor { get; private set; } = 0x3a3a3a;
        public uint StackedAppActiveColor { get; private set; } = 0x4a4a7c;
        public uint StackedAppTextColor { get; private set; } = 0xFFFFFF;
        public uint StackedAppActiveTextColor { get; private set; } = 0xF4DB91;
        public int StackedAppMargin { get; private set; } = 2;

        public bool LoadConfiguration()
        {
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CONFIG_FILE_NAME);

                if (!File.Exists(configPath))
                {
                    Logger.Info("Config file not found, using default workspace indicator settings");
                    ApplyWindowsVersionDefaults();
                    return false;
                }

                string tomlContent = File.ReadAllText(configPath);
                var model = Toml.ToModel(tomlContent);

                if (model.TryGetValue("workspace_indicator", out var indicatorObj) && indicatorObj is TomlTable indicatorTable)
                {
                    WorkspaceWidth = GetIntValue(indicatorTable, "workspace_width", WorkspaceWidth);
                    WorkspaceHeight = GetIntValue(indicatorTable, "workspace_height", WorkspaceHeight);
                    WorkspaceMargin = GetIntValue(indicatorTable, "workspace_margin", WorkspaceMargin);
                    IconSize = GetIntValue(indicatorTable, "icon_size", IconSize);

                    ActiveWorkspaceBorderColor = GetUintValue(indicatorTable, "active_workspace_border_color", ActiveWorkspaceBorderColor);
                    BackgroundColor = GetUintValue(indicatorTable, "background_color", BackgroundColor);
                    ActiveWorkspaceColor = GetUintValue(indicatorTable, "active_workspace_color", ActiveWorkspaceColor);
                    HoveredWorkspaceColor = GetUintValue(indicatorTable, "hovered_workspace_color", HoveredWorkspaceColor);
                    InactiveWorkspaceColor = GetUintValue(indicatorTable, "inactive_workspace_color", InactiveWorkspaceColor);
                    ActiveWorkspaceTextColor = GetUintValue(indicatorTable, "active_workspace_text_color", ActiveWorkspaceTextColor);
                    InactiveWorkspaceTextColor = GetUintValue(indicatorTable, "inactive_workspace_text_color", InactiveWorkspaceTextColor);
                    StackedModeWorkspaceColor = GetUintValue(indicatorTable, "stacked_mode_workspace_color", StackedModeWorkspaceColor);
                    StackedModeBorderColor = GetUintValue(indicatorTable, "stacked_mode_border_color", StackedModeBorderColor);
                    BackupWorkspaceColor = GetUintValue(indicatorTable, "backup_workspace_color", BackupWorkspaceColor);
                    BackupWorkspaceBorderColor = GetUintValue(indicatorTable, "backup_workspace_border_color", BackupWorkspaceBorderColor);
                    BackupAndStackedWorkspaceColor = GetUintValue(indicatorTable, "backup_and_stacked_workspace_color", BackupAndStackedWorkspaceColor);
                    BackupAndStackedBorderColor = GetUintValue(indicatorTable, "backup_and_stacked_border_color", BackupAndStackedBorderColor);
                    PausedWorkspaceColor = GetUintValue(indicatorTable, "paused_workspace_color", PausedWorkspaceColor);
                    PausedWorkspaceBorderColor = GetUintValue(indicatorTable, "paused_workspace_border_color", PausedWorkspaceBorderColor);

                    OffsetFromTaskbarLeftEdge = GetIntValue(indicatorTable, "offset_from_taskbar_left_edge", OffsetFromTaskbarLeftEdge);
                    ActiveWorkspaceBorderOpacity = GetByteValue(indicatorTable, "active_workspace_border_opacity", ActiveWorkspaceBorderOpacity);

                    Windows10OffsetFromTaskbarRightEdge = GetIntValue(indicatorTable, "windows10_offset_from_taskbar_right_edge", Windows10OffsetFromTaskbarRightEdge);
                    UseWindows10Positioning = GetBoolValue(indicatorTable, "use_windows10_positioning", UseWindows10Positioning);

                    // Load stacked app display configuration
                    StackedAppIconSize = GetIntValue(indicatorTable, "stacked_app_icon_size", StackedAppIconSize);
                    StackedAppItemWidth = GetIntValue(indicatorTable, "stacked_app_item_width", StackedAppItemWidth);
                    StackedAppItemWidthIconOnly = GetIntValue(indicatorTable, "stacked_app_item_width_icon_only", StackedAppItemWidthIconOnly);
                    StackedAppTitleMaxLength = GetIntValue(indicatorTable, "stacked_app_title_max_length", StackedAppTitleMaxLength);
                    ShowStackedAppTitle = GetBoolValue(indicatorTable, "show_stacked_app_title", ShowStackedAppTitle);
                    StackedAppBackgroundColor = GetUintValue(indicatorTable, "stacked_app_background_color", StackedAppBackgroundColor);
                    StackedAppHoverColor = GetUintValue(indicatorTable, "stacked_app_hover_color", StackedAppHoverColor);
                    StackedAppActiveColor = GetUintValue(indicatorTable, "stacked_app_active_color", StackedAppActiveColor);
                    StackedAppTextColor = GetUintValue(indicatorTable, "stacked_app_text_color", StackedAppTextColor);
                    StackedAppActiveTextColor = GetUintValue(indicatorTable, "stacked_app_active_text_color", StackedAppActiveTextColor);
                    StackedAppMargin = GetIntValue(indicatorTable, "stacked_app_margin", StackedAppMargin);

                    if (!UseWindows10Positioning)
                    {
                        ApplyWindowsVersionDefaults();
                    }

                    return true;
                }
                else
                { // fallback to use default values
                    ApplyWindowsVersionDefaults();
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error loading workspace indicator configuration");
                ApplyWindowsVersionDefaults();
                return false;
            }
        }

        private void ApplyWindowsVersionDefaults()
        {
            if (OSUtilities.IsWindows10())
            {
                UseWindows10Positioning = true;
            }
            else
            {
                UseWindows10Positioning = false;
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