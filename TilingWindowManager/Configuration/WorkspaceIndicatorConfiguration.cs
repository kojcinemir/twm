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
        public bool ShowOnlyOccupiedWorkspaces { get; private set; } = false;
        public bool WorkspaceRoundedCorners { get; private set; } = false;
        public int WorkspaceCornerRadius { get; private set; } = 8;
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

        // Stacked app number badge configuration
        public bool ShowStackedAppNumbers { get; private set; } = true;
        public int StackedAppNumberBadgeSize { get; private set; } = 14;
        public uint StackedAppNumberBadgeBackgroundColor { get; private set; } = 0x4a4a7c;
        public uint StackedAppNumberBadgeTextColor { get; private set; } = 0xFFFFFF;
        public List<string> StackedWindowShortcutLabels { get; private set; } = new List<string> { "1", "2", "3", "4", "5", "6", "7", "8", "9" };

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

                // Check for color scheme first
                string? colorSchemeName = null;
                if (model.TryGetValue("color_scheme", out var schemeValue))
                {
                    colorSchemeName = schemeValue?.ToString();
                    if (!string.IsNullOrWhiteSpace(colorSchemeName))
                    {
                        var scheme = ColorScheme.GetScheme(colorSchemeName);
                        if (scheme != null)
                        {
                            Logger.Info($"Applying color scheme: {colorSchemeName}");
                            ApplyColorScheme(scheme);
                        }
                        else
                        {
                            Logger.Warning($"Unknown color scheme: {colorSchemeName}. Available schemes: {string.Join(", ", ColorScheme.GetAvailableSchemes())}");
                        }
                    }
                }

                if (model.TryGetValue("workspace_indicator", out var indicatorObj) && indicatorObj is TomlTable indicatorTable)
                {
                    WorkspaceWidth = GetIntValue(indicatorTable, "workspace_width", WorkspaceWidth);
                    WorkspaceHeight = GetIntValue(indicatorTable, "workspace_height", WorkspaceHeight);
                    WorkspaceMargin = GetIntValue(indicatorTable, "workspace_margin", WorkspaceMargin);
                    IconSize = GetIntValue(indicatorTable, "icon_size", IconSize);
                    ShowOnlyOccupiedWorkspaces = GetBoolValue(indicatorTable, "show_only_occupied_workspaces", ShowOnlyOccupiedWorkspaces);
                    WorkspaceRoundedCorners = GetBoolValue(indicatorTable, "workspace_rounded_corners", WorkspaceRoundedCorners);
                    WorkspaceCornerRadius = GetIntValue(indicatorTable, "workspace_corner_radius", WorkspaceCornerRadius);

                    // only override color scheme colors if explicitly set in config
                    // this allows color scheme to be the base, with manual overrides
                    if (indicatorTable.ContainsKey("active_workspace_border_color"))
                        ActiveWorkspaceBorderColor = GetUintValue(indicatorTable, "active_workspace_border_color", ActiveWorkspaceBorderColor);
                    if (indicatorTable.ContainsKey("background_color"))
                        BackgroundColor = GetUintValue(indicatorTable, "background_color", BackgroundColor);
                    if (indicatorTable.ContainsKey("active_workspace_color"))
                        ActiveWorkspaceColor = GetUintValue(indicatorTable, "active_workspace_color", ActiveWorkspaceColor);
                    if (indicatorTable.ContainsKey("hovered_workspace_color"))
                        HoveredWorkspaceColor = GetUintValue(indicatorTable, "hovered_workspace_color", HoveredWorkspaceColor);
                    if (indicatorTable.ContainsKey("inactive_workspace_color"))
                        InactiveWorkspaceColor = GetUintValue(indicatorTable, "inactive_workspace_color", InactiveWorkspaceColor);
                    if (indicatorTable.ContainsKey("active_workspace_text_color"))
                        ActiveWorkspaceTextColor = GetUintValue(indicatorTable, "active_workspace_text_color", ActiveWorkspaceTextColor);
                    if (indicatorTable.ContainsKey("inactive_workspace_text_color"))
                        InactiveWorkspaceTextColor = GetUintValue(indicatorTable, "inactive_workspace_text_color", InactiveWorkspaceTextColor);
                    if (indicatorTable.ContainsKey("stacked_mode_workspace_color"))
                        StackedModeWorkspaceColor = GetUintValue(indicatorTable, "stacked_mode_workspace_color", StackedModeWorkspaceColor);
                    if (indicatorTable.ContainsKey("stacked_mode_border_color"))
                        StackedModeBorderColor = GetUintValue(indicatorTable, "stacked_mode_border_color", StackedModeBorderColor);
                    if (indicatorTable.ContainsKey("backup_workspace_color"))
                        BackupWorkspaceColor = GetUintValue(indicatorTable, "backup_workspace_color", BackupWorkspaceColor);
                    if (indicatorTable.ContainsKey("backup_workspace_border_color"))
                        BackupWorkspaceBorderColor = GetUintValue(indicatorTable, "backup_workspace_border_color", BackupWorkspaceBorderColor);
                    if (indicatorTable.ContainsKey("backup_and_stacked_workspace_color"))
                        BackupAndStackedWorkspaceColor = GetUintValue(indicatorTable, "backup_and_stacked_workspace_color", BackupAndStackedWorkspaceColor);
                    if (indicatorTable.ContainsKey("backup_and_stacked_border_color"))
                        BackupAndStackedBorderColor = GetUintValue(indicatorTable, "backup_and_stacked_border_color", BackupAndStackedBorderColor);
                    if (indicatorTable.ContainsKey("paused_workspace_color"))
                        PausedWorkspaceColor = GetUintValue(indicatorTable, "paused_workspace_color", PausedWorkspaceColor);
                    if (indicatorTable.ContainsKey("paused_workspace_border_color"))
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

                    if (indicatorTable.ContainsKey("stacked_app_background_color"))
                        StackedAppBackgroundColor = GetUintValue(indicatorTable, "stacked_app_background_color", StackedAppBackgroundColor);
                    if (indicatorTable.ContainsKey("stacked_app_hover_color"))
                        StackedAppHoverColor = GetUintValue(indicatorTable, "stacked_app_hover_color", StackedAppHoverColor);
                    if (indicatorTable.ContainsKey("stacked_app_active_color"))
                        StackedAppActiveColor = GetUintValue(indicatorTable, "stacked_app_active_color", StackedAppActiveColor);
                    if (indicatorTable.ContainsKey("stacked_app_text_color"))
                        StackedAppTextColor = GetUintValue(indicatorTable, "stacked_app_text_color", StackedAppTextColor);
                    if (indicatorTable.ContainsKey("stacked_app_active_text_color"))
                        StackedAppActiveTextColor = GetUintValue(indicatorTable, "stacked_app_active_text_color", StackedAppActiveTextColor);

                    StackedAppMargin = GetIntValue(indicatorTable, "stacked_app_margin", StackedAppMargin);

                    // Load stacked app number badge configuration
                    ShowStackedAppNumbers = GetBoolValue(indicatorTable, "show_stacked_app_numbers", ShowStackedAppNumbers);
                    StackedAppNumberBadgeSize = GetIntValue(indicatorTable, "stacked_app_number_badge_size", StackedAppNumberBadgeSize);
                    if (indicatorTable.ContainsKey("stacked_app_number_badge_background_color"))
                        StackedAppNumberBadgeBackgroundColor = GetUintValue(indicatorTable, "stacked_app_number_badge_background_color", StackedAppNumberBadgeBackgroundColor);
                    if (indicatorTable.ContainsKey("stacked_app_number_badge_text_color"))
                        StackedAppNumberBadgeTextColor = GetUintValue(indicatorTable, "stacked_app_number_badge_text_color", StackedAppNumberBadgeTextColor);

                    if (!UseWindows10Positioning)
                    {
                        ApplyWindowsVersionDefaults();
                    }
                }
                else
                { // fallback to use default values
                    ApplyWindowsVersionDefaults();
                }

                // Load stacked window shortcut labels from hotkeys section
                if (model.TryGetValue("hotkeys", out var hotkeysObj) && hotkeysObj is TomlTable hotkeysTable)
                {
                    var labels = new List<string>();
                    for (int i = 1; i <= 9; i++)
                    {
                        string hotkeyName = $"jump_to_stacked_window_{i}";
                        if (hotkeysTable.TryGetValue(hotkeyName, out var hotkeyObj) && hotkeyObj is TomlTable hotkeyTable)
                        {
                            if (hotkeyTable.TryGetValue("key", out var keyValue) && keyValue != null)
                            {
                                string keyString = keyValue.ToString() ?? "";
                                string label = ExtractLabelFromKey(keyString);
                                labels.Add(label);
                            }
                            else
                            {
                                labels.Add(i.ToString());
                            }
                        }
                        else
                        {
                            labels.Add(i.ToString());
                        }
                    }
                    if (labels.Count > 0)
                    {
                        StackedWindowShortcutLabels = labels;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error loading workspace indicator configuration");
                ApplyWindowsVersionDefaults();
                return false;
            }
        }

        private void ApplyColorScheme(ColorSchemeColors scheme)
        {
            ActiveWorkspaceBorderColor = scheme.ActiveWorkspaceBorderColor;
            BackgroundColor = scheme.BackgroundColor;
            ActiveWorkspaceColor = scheme.ActiveWorkspaceColor;
            HoveredWorkspaceColor = scheme.HoveredWorkspaceColor;
            InactiveWorkspaceColor = scheme.InactiveWorkspaceColor;
            ActiveWorkspaceTextColor = scheme.ActiveWorkspaceTextColor;
            InactiveWorkspaceTextColor = scheme.InactiveWorkspaceTextColor;
            StackedModeWorkspaceColor = scheme.StackedModeWorkspaceColor;
            StackedModeBorderColor = scheme.StackedModeBorderColor;
            PausedWorkspaceColor = scheme.PausedWorkspaceColor;
            PausedWorkspaceBorderColor = scheme.PausedWorkspaceBorderColor;
            StackedAppBackgroundColor = scheme.StackedAppBackgroundColor;
            StackedAppHoverColor = scheme.StackedAppHoverColor;
            StackedAppActiveColor = scheme.StackedAppActiveColor;
            StackedAppTextColor = scheme.StackedAppTextColor;
            StackedAppActiveTextColor = scheme.StackedAppActiveTextColor;
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

        private string ExtractLabelFromKey(string keyString)
        {
            if (string.IsNullOrWhiteSpace(keyString))
                return "";

            // Extract the last part after the last "+"
            int lastPlusIndex = keyString.LastIndexOf('+');
            if (lastPlusIndex >= 0 && lastPlusIndex < keyString.Length - 1)
            {
                return keyString.Substring(lastPlusIndex + 1).Trim().ToUpper();
            }

            return keyString.Trim().ToUpper();
        }
    }
}