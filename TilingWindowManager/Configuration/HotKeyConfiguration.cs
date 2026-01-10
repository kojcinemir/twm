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
using System.Linq;
using Tomlyn;
using Tomlyn.Model;

namespace TilingWindowManager
{
    public enum MonitorDirection
    {
        Left,
        Down,
        Up,
        Right
    }

    public static class ModifierKeys
    {
        public const uint MOD_ALT = 0x0001;
        public const uint MOD_CTRL = 0x0002;
        public const uint MOD_SHIFT = 0x0004;
        public const uint MOD_WIN = 0x0008;
    }

    public class HotKeyEntry
    {
        public string Name { get; set; } = "";
        public string KeyCombination { get; set; } = "";
        public string Key { get; set; } = "";
        public string[] Modifiers { get; set; } = Array.Empty<string>();
        public uint KeyCode { get; set; }
        public uint HotkeyId { get; set; }
        public string Action { get; set; } = "";
        
        // Alternative hotkey support
        public string? AltKeyCombination { get; set; }
        public string? AltKey { get; set; }
        public string[]? AltModifiers { get; set; }
        public uint? AltKeyCode { get; set; }
        public uint? AltHotkeyId { get; set; }
    }

    public class HotKeyConfiguration
    {
        private const string CONFIG_FILE_NAME = "config.toml";
        private readonly Dictionary<uint, HotKeyEntry> _hotkeys = new();
        private readonly Dictionary<string, uint> _modifierMap = new()
        {
            { "alt", ModifierKeys.MOD_ALT },
            { "ctrl", ModifierKeys.MOD_CTRL },
            { "shift", ModifierKeys.MOD_SHIFT },
            { "win", ModifierKeys.MOD_WIN }
        };

        private readonly Dictionary<string, uint> _keyMap = new()
        {
            // Numbers
            { "0", 0x30 }, { "1", 0x31 }, { "2", 0x32 }, { "3", 0x33 }, { "4", 0x34 },
            { "5", 0x35 }, { "6", 0x36 }, { "7", 0x37 }, { "8", 0x38 }, { "9", 0x39 },

            // Letters A-Z
            { "A", 0x41 }, { "B", 0x42 }, { "C", 0x43 }, { "D", 0x44 }, { "E", 0x45 },
            { "F", 0x46 }, { "G", 0x47 }, { "H", 0x48 }, { "I", 0x49 }, { "J", 0x4A },
            { "K", 0x4B }, { "L", 0x4C }, { "M", 0x4D }, { "N", 0x4E }, { "O", 0x4F },
            { "P", 0x50 }, { "Q", 0x51 }, { "R", 0x52 }, { "S", 0x53 }, { "T", 0x54 },
            { "U", 0x55 }, { "V", 0x56 }, { "W", 0x57 }, { "X", 0x58 }, { "Y", 0x59 },
            { "Z", 0x5A },

            // Function keys
            { "F1", 0x70 }, { "F2", 0x71 }, { "F3", 0x72 }, { "F4", 0x73 },
            { "F5", 0x74 }, { "F6", 0x75 }, { "F7", 0x76 }, { "F8", 0x77 },
            { "F9", 0x78 }, { "F10", 0x79 }, { "F11", 0x7A }, { "F12", 0x7B },

            // Special keys
            { "SPACE", 0x20 }, { "TAB", 0x09 }, { "ENTER", 0x0D }, { "ESC", 0x1B },
            { "BACKSPACE", 0x08 }, { "DELETE", 0x2E }, { "INSERT", 0x2D },
            { "HOME", 0x24 }, { "END", 0x23 }, { "PAGEUP", 0x21 }, { "PAGEDOWN", 0x22 },
            { "UP", 0x26 }, { "DOWN", 0x28 }, { "LEFT", 0x25 }, { "RIGHT", 0x27 },

            // Punctuation and symbols
            { ";", 0xBA }, { "=", 0xBB }, { ",", 0xBC }, { "-", 0xBD },
            { ".", 0xBE }, { "/", 0xBF }, { "`", 0xC0 }, { "[", 0xDB },
            { "\\", 0xDC }, { "]", 0xDD }, { "'", 0xDE },

            // Numpad
            { "NUMPAD0", 0x60 }, { "NUMPAD1", 0x61 }, { "NUMPAD2", 0x62 }, { "NUMPAD3", 0x63 },
            { "NUMPAD4", 0x64 }, { "NUMPAD5", 0x65 }, { "NUMPAD6", 0x66 }, { "NUMPAD7", 0x67 },
            { "NUMPAD8", 0x68 }, { "NUMPAD9", 0x69 }, { "MULTIPLY", 0x6A }, { "ADD", 0x6B },
            { "SUBTRACT", 0x6D }, { "DECIMAL", 0x6E }, { "DIVIDE", 0x6F }
        };

        public IEnumerable<HotKeyEntry> AllHotKeys => _hotkeys.Values;

        public bool LoadConfiguration()
        {
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CONFIG_FILE_NAME);

                if (!File.Exists(configPath))
                {
                    CreateDefaultConfiguration(configPath);
                }

                string tomlContent = File.ReadAllText(configPath);
                var model = Toml.ToModel(tomlContent);

                _hotkeys.Clear();
                uint currentId = 1;

                if (model.TryGetValue("hotkeys", out var hotkeysObj) && hotkeysObj is TomlTable hotkeysTable)
                {
                    foreach (var hotkeyName in hotkeysTable.Keys)
                    {
                        if (hotkeysTable[hotkeyName] is TomlTable hotkeyTable)
                        {
                            var entry = ParseSimpleHotKeyEntry(hotkeyName, hotkeyTable, ref currentId);
                            if (entry != null)
                            {
                                _hotkeys[entry.HotkeyId] = entry;
                                // if there is an alternative hotkey, register it with a separate ID
                                if (entry.AltHotkeyId.HasValue)
                                {
                                    _hotkeys[entry.AltHotkeyId.Value] = entry;
                                }
                            }
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error loading hotkey configuration");
                LoadDefaultConfiguration();
                return false;
            }
        }

        private HotKeyEntry? ParseSimpleHotKeyEntry(string name, TomlTable table, ref uint id)
        {
            try
            {
                var entry = new HotKeyEntry
                {
                    Name = name,
                    HotkeyId = id++
                };

                // parse key combination (e.g., "ALT+CTRL+5")
                if (table.TryGetValue("key", out var keyObj) && keyObj is string keyCombination)
                {
                    entry.KeyCombination = keyCombination;
                    if (!ParseKeyCombination(keyCombination, entry))
                    {
                        Logger.Error($"Invalid key combination: {keyCombination}");
                        return null;
                    }
                }

                if (table.TryGetValue("alt_key", out var altKeyObj) && altKeyObj is string altKeyCombination)
                {
                    entry.AltKeyCombination = altKeyCombination;
                    entry.AltHotkeyId = id++;
                    if (!ParseAltKeyCombination(altKeyCombination, entry))
                    {
                        Logger.Error($"Invalid alternative key combination: {altKeyCombination}");
                        entry.AltKeyCombination = null;
                        entry.AltHotkeyId = null;
                        id--; // Return the ID since its not used
                    }
                }

                // parse action
                if (table.TryGetValue("action", out var actionObj) && actionObj is string action)
                {
                    entry.Action = action;
                }

                return entry;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error parsing hotkey entry {name}");
                return null;
            }
        }

        private bool ParseKeyCombination(string keyCombination, HotKeyEntry entry)
        {
            var parts = keyCombination.Split('+').Select(p => p.Trim().ToUpper()).ToArray();
            if (parts.Length == 0) return false;

            entry.Key = parts[parts.Length - 1];
            entry.Modifiers = parts.Take(parts.Length - 1).Select(m => m.ToLower()).ToArray();

            if (!_keyMap.TryGetValue(entry.Key, out var keyCode))
            {
                Logger.Error($"Unknown key: {entry.Key}");
                return false;
            }
            entry.KeyCode = keyCode;

            return true;
        }

        private bool ParseAltKeyCombination(string keyCombination, HotKeyEntry entry)
        {
            var parts = keyCombination.Split('+').Select(p => p.Trim().ToUpper()).ToArray();
            if (parts.Length == 0) return false;

            entry.AltKey = parts[parts.Length - 1];
            entry.AltModifiers = parts.Take(parts.Length - 1).Select(m => m.ToLower()).ToArray();

            if (!_keyMap.TryGetValue(entry.AltKey, out var keyCode))
            {
                Logger.Error($"Unknown alternative key: {entry.AltKey}");
                return false;
            }
            entry.AltKeyCode = keyCode;

            return true;
        }

        public uint GetModifiers(HotKeyEntry entry)
        {
            uint modifiers = 0;
            foreach (var modifier in entry.Modifiers)
            {
                if (_modifierMap.TryGetValue(modifier, out var modifierValue))
                {
                    modifiers |= modifierValue;
                }
            }
            return modifiers;
        }

        public uint GetAltModifiers(HotKeyEntry entry)
        {
            if (entry.AltModifiers == null) return 0;
            
            uint modifiers = 0;
            foreach (var modifier in entry.AltModifiers)
            {
                if (_modifierMap.TryGetValue(modifier, out var modifierValue))
                {
                    modifiers |= modifierValue;
                }
            }
            return modifiers;
        }

        public HotKeyEntry? GetHotKeyById(uint hotkeyId)
        {
            return _hotkeys.TryGetValue(hotkeyId, out var entry) ? entry : null;
        }

        public HotKeyEntry? GetHotKeyByName(string name)
        {
            return _hotkeys.Values.FirstOrDefault(h => h.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public IEnumerable<HotKeyEntry> GetHotKeysByAction(string action)
        {
            return _hotkeys.Values.Where(h => h.Action.Equals(action, StringComparison.OrdinalIgnoreCase));
        }

        private void LoadDefaultConfiguration()
        {

            for (uint i = 1; i <= 8; i++)
            {
                var entry = new HotKeyEntry
                {
                    Name = $"workspace_{i}",
                    HotkeyId = i,
                    KeyCode = 0x30 + i, // virtual key codes for 1-8
                    KeyCombination = $"ALT+{i}",
                    Key = i.ToString(),
                    Modifiers = new[] { "alt" },
                    Action = "switch_to_workspace"
                };
                _hotkeys[entry.HotkeyId] = entry;
            }
        }

        private void CreateDefaultConfiguration(string configPath)
        {
            string defaultConfig = GenerateDefaultTomlConfiguration();
            File.WriteAllText(configPath, defaultConfig);
            Logger.Info($"Created default hotkey configuration at: {configPath}");
        }

        private string GenerateDefaultTomlConfiguration()
        {
            return @"
# Tiling Window Manager Hotkey Configuration

# Don't force (kill process) of the application.
# USE exit_application hotkey. Default ALT+CTRL+O instead to force close

#####CONFIG#####

# Color Scheme Selection
# Available options: ""gruvbox"", ""tokyo-night"", ""nordic"", ""vscode"", ""everforest"", ""windows10""
color_scheme = ""gruvbox""

# If application is not recognized for tile
# Add process names (without .exe) here to include them
allowed_owned_windows = [""frontrunner""]

# Workspace Settings
# Options: ""tiled"", ""stacked"", ""paused""
# If not specified, workspace defaults to ""tiled""
[workspace]
    workspace_1 = ""stacked""
    workspace_2 = ""stacked""
    workspace_3 = ""stacked""
    workspace_4 = ""stacked""
    workspace_5 = ""stacked""
    workspace_6 = ""stacked""
    workspace_7 = ""stacked""
    workspace_8 = ""stacked""

# Logging Settings
[logging]
    enabled = false  # Set to false to disable logging in normal mode (saves resources)
    level = ""info""   # options: debug, info, warning, error

# Workspace Indicator Visual Settings
[workspace_indicator]
    workspace_width = 75
    workspace_height = 40
    workspace_margin = 2
    icon_size = 16
    show_only_occupied_workspaces = true    # Only show current workspace and workspaces with applications
    workspace_rounded_corners = true       # Enable rounded corners for workspace boxes
    workspace_corner_radius = 4             # Corner radius in pixels (only applies if rounded corners enabled)

    # Color settings - uncomment to override the color scheme
    # active_workspace_border_color = 0xFB923C
    # background_color = 0x1c1c1c
    # active_workspace_color = 0x0f0f0f
    # hovered_workspace_color = 0x383838
    # inactive_workspace_color = 0x1c1c1c
    # active_workspace_text_color = 0x00FFFF
    # inactive_workspace_text_color = 0xFFFFFF
    # stacked_mode_workspace_color = 0x2a4a7c
    # stacked_mode_border_color = 0x2a4a7c
    # paused_workspace_color = 0x90EE90
    # paused_workspace_border_color = 0x90EE90

    offset_from_taskbar_left_edge = 0
    active_workspace_border_opacity = 255

    # to force windows 10 positioning of workspace indicator, since start menu on windows 10 is on the left
    # use_windows10_positioning = true
    windows10_offset_from_taskbar_right_edge = 120

    # Stacked App Display Settings (shown when workspace is in stacked mode)
    show_stacked_app_title = false               # Show title text next to icon (true) or just icon (false)
    stacked_app_title_max_length = 30            # Maximum characters for title before truncation
    stacked_app_icon_size = 24                   # Size of application icons in pixels
    stacked_app_item_width = 150                 # Width of each app item when showing icon + title
    stacked_app_item_width_icon_only = 40        # Width of each app item when showing icon only
    stacked_app_margin = 2                       # Margin between stacked app items

    # Stacked App Shortcut Badge Settings (displays keyboard shortcut letters on stacked app icons)
    show_stacked_app_numbers = true              # Show shortcut key badges on stacked app icons
    stacked_app_number_badge_size = 14           # Size of the circular badge in pixels
    # stacked_app_number_badge_background_color = 0x4a4a7c  # Badge background color (uncomment to override color scheme)
    # stacked_app_number_badge_text_color = 0xFFFFFF        # Badge text color (uncomment to override color scheme)

    # Stacked app colors - uncomment to override the color scheme
    # stacked_app_background_color = 0x2a2a2a      # Background color for inactive stacked apps
    # stacked_app_hover_color = 0x3a3a3a           # Background color when hovering over a stacked app
    # stacked_app_active_color = 0x4a4a7c          # Background color for currently visible stacked app
    # stacked_app_text_color = 0xFFFFFF            # Text color for inactive stacked apps
    # stacked_app_active_text_color = 0xFB923C     # Text color for currently visible stacked app

# Window Border Settings
[window_border]
    border_width = 2
    rounded_borders = true
    corner_radius = 12
    opacity = 255
	# border_color = 0xFB923C # uncomment to override default color palette

# App Switcher Settings
[app_switcher]
    width = 600
    height = 400
    max_results = 8
    item_height = 50
    search_box_height = 40
    corner_radius = 8
    # Color settings - uncomment to override the color scheme
    # background_color = 0x1c1c1c
    # selected_color = 0x2a4a7c
    # text_color = 0xFFFFFF
    # subtitle_color = 0x888888

# Pinned Applications - Applications that should always appear on specific workspaces
[pinned_applications]
    ""slack.exe"" = 8
    #""chrome.exe"" = 1

# Application Hotkeys - Hotkeys to switch to workspace containing specific applications
[application_hotkeys]
    ""ALT+SHIFT+Q"" = ""slack.exe""
    ""ALT+SHIFT+W"" = ""TcXaeShell.exe""
    ""ALT+SHIFT+E"" = ""devenv.exe""
    ""ALT+SHIFT+A"" = ""Arc.exe""

# HotKey Key-Action Mappings
[hotkeys]
# Exit application properly. Will ensure that all the windows tiled by the MELD have proper visiblity restored
    [hotkeys.exit_application]
        key = ""ALT+CTRL+O""
        action = ""exit_application""

# Open application switcher
    [hotkeys.open_app_switcher]
        key = ""CTRL+ALT+SPACE""
        action = ""open_app_switcher""

# Find empty workspace on active monitor and switch to it
    [hotkeys.switch_to_free_workspace]
        key = ""ALT+F""
        action = ""switch_to_free_workspace""

# Move active window to free workspace on active monitor and move to it
    [hotkeys.move_to_free_workspace]
        key = ""ALT+SHIFT+F""
        action = ""move_to_free_workspace""

# Find free workspace on other monitor and move to it. Fallback to workspace 1 if all occupied. Works correctly in 2 monitor setup
    [hotkeys.switch_to_free_workspace_other_monitor]
        key = ""ALT+CTRL+F""
        action = ""switch_to_free_workspace_other_monitor""

# Move active window from one monitor to other monitor at first free workspace. If all occupied fallback to 1. Works correcly in 2 monitor setup
    [hotkeys.move_to_free_workspace_other_monitor]
        key = ""CTRL+SHIFT+F""
        action = ""move_to_free_workspace_other_monitor""

# Will refresh (retile) the workspace if there is some and issue with tiling
    [hotkeys.refresh_monitor]
        key = ""ALT+SHIFT+R""
        action = ""refresh_active_monitor""

# Reload configuration without restarting the application
    [hotkeys.reload_configuration]
        key = ""ALT+CTRL+R""
        action = ""reload_configuration""

# Will untile the untile or retile the window that was previously tiled
    [hotkeys.remove_from_tiling]
        key = ""ALT+T""
        action = ""remove_from_tiling""

# Toogle stacked mode
    [hotkeys.toggle_stacked_mode]
        key = ""ALT+SHIFT+S""
        action = ""toggle_stacked_mode""

# Cycle between stacked windows in a workspace
    [hotkeys.cycle_stacked_window]
        key = ""ALT+S""
        action = ""cycle_stacked_window""
  # Move stacked window position left (earlier in Z-order)
      [hotkeys.move_stacked_window_left]
          key = ""ALT+SHIFT+,""
          action = ""move_stacked_window_left""

  # Move stacked window position right (later in Z-order)
      [hotkeys.move_stacked_window_right]
          key = ""ALT+SHIFT+.""
          action = ""move_stacked_window_right""

# Jump to specific stacked window by position 
    [hotkeys.jump_to_stacked_window_1]
        key = ""ALT+Q""
        action = ""jump_to_stacked_window""

    [hotkeys.jump_to_stacked_window_2]
        key = ""ALT+W""
        action = ""jump_to_stacked_window""

    [hotkeys.jump_to_stacked_window_3]
        key = ""ALT+E""
        action = ""jump_to_stacked_window""

    [hotkeys.jump_to_stacked_window_4]
        key = ""ALT+R""
        action = ""jump_to_stacked_window""

    [hotkeys.jump_to_stacked_window_5]
        key = ""WIN+5""
        action = ""jump_to_stacked_window""

    [hotkeys.jump_to_stacked_window_6]
        key = ""WIN+6""
        action = ""jump_to_stacked_window""

    [hotkeys.jump_to_stacked_window_7]
        key = ""WIN+7""
        action = ""jump_to_stacked_window""

    [hotkeys.jump_to_stacked_window_8]
        key = ""WIN+8""
        action = ""jump_to_stacked_window""

    [hotkeys.jump_to_stacked_window_9]
        key = ""WIN+9""
        action = ""jump_to_stacked_window""

# Toggle paused mode
    [hotkeys.toggle_paused_mode]
        key = ""ALT+P""
        action = ""toggle_paused_mode""

# Will try to reload if some of the windows got lost in the tiling process. Still in the testing phase 
    [hotkeys.reload_allowed_owned_windows]
        key = ""ALT+SHIFT+R""
        action = ""reload_allowed_owned_windows""

# Resize windows
    [hotkeys.increase_split]
        key = ""ALT+]""
        action = ""increase_split_ratio""

    [hotkeys.decrease_split]
        key = ""ALT+[""
        action = ""decrease_split_ratio""

# Close active window
    [hotkeys.close_window]
        key = ""ALT+SHIFT+C""
        action = ""close_window""

# Focus window on active workspace
    [hotkeys.focus_left]
        key = ""ALT+H""
        alt_key = ""ALT+LEFT""
        action = ""focus_left""

    [hotkeys.focus_down]
        key = ""ALT+J""
        alt_key = ""ALT+DOWN""
        action = ""focus_down""

    [hotkeys.focus_up]
        key = ""ALT+K""
        alt_key = ""ALT+UP""
        action = ""focus_up""

    [hotkeys.focus_right]
        key = ""ALT+L""
        alt_key = ""ALT+RIGHT""
        action = ""focus_right""

# Move window position on active workspace
    [hotkeys.swap_left]
        key = ""ALT+SHIFT+H""
        alt_key = ""ALT+SHIFT+LEFT""
        action = ""swap_left""

    [hotkeys.swap_down]
        key = ""ALT+SHIFT+J""
        alt_key = ""ALT+SHIFT+DOWN""
        action = ""swap_down""

    [hotkeys.swap_up]
        key = ""ALT+SHIFT+K""
        alt_key = ""ALT+SHIFT+UP""
        action = ""swap_up""

    [hotkeys.swap_right]
        key = ""ALT+SHIFT+L""
        alt_key = ""ALT+SHIFT+RIGHT""
        action = ""swap_right""

# Switch to workspace on current active monitor
    [hotkeys.workspace_1]
        key = ""ALT+1""
        action = ""switch_to_workspace""

    [hotkeys.workspace_2]
        key = ""ALT+2""
        action = ""switch_to_workspace""

    [hotkeys.workspace_3]
        key = ""ALT+3""
        action = ""switch_to_workspace""

    [hotkeys.workspace_4]
        key = ""ALT+4""
        action = ""switch_to_workspace""

    [hotkeys.workspace_5]
        key = ""ALT+5""
        action = ""switch_to_workspace""

    [hotkeys.workspace_6]
        key = ""ALT+6""
        action = ""switch_to_workspace""

    [hotkeys.workspace_7]
        key = ""ALT+7""
        action = ""switch_to_workspace""

    [hotkeys.workspace_8]
        key = ""ALT+8""
        action = ""switch_to_workspace""

# Move window to workspace on active monitor
    [hotkeys.move_to_workspace_1]
        key = ""ALT+SHIFT+1""
        action = ""move_to_workspace""

    [hotkeys.move_to_workspace_2]
        key = ""ALT+SHIFT+2""
        action = ""move_to_workspace""

    [hotkeys.move_to_workspace_3]

        key = ""ALT+SHIFT+3""
        action = ""move_to_workspace""

    [hotkeys.move_to_workspace_4]
        key = ""ALT+SHIFT+4""
        action = ""move_to_workspace""

    [hotkeys.move_to_workspace_5]
        key = ""ALT+SHIFT+5""
        action = ""move_to_workspace""

    [hotkeys.move_to_workspace_6]
        key = ""ALT+SHIFT+6""
        action = ""move_to_workspace""

    [hotkeys.move_to_workspace_7]
        key = ""ALT+SHIFT+7""
        action = ""move_to_workspace""

    [hotkeys.move_to_workspace_8]
        key = ""ALT+SHIFT+8""
        action = ""move_to_workspace""


# Switch to monitor positioned left/right/top/bottom
    [hotkeys.switch_monitor_left]
        key = ""ALT+CTRL+H""
        alt_key = ""ALT+CTRL+LEFT""
        action = ""switch_monitor_left""

    [hotkeys.switch_monitor_down]
        key = ""ALT+CTRL+J""
        alt_key = ""ALT+CTRL+DOWN""
        action = ""switch_monitor_down""

    [hotkeys.switch_monitor_up]
        key = ""ALT+CTRL+K""
        alt_key = ""ALT+CTRL+UP""
        action = ""switch_monitor_up""

    [hotkeys.switch_monitor_right]
        key = ""ALT+CTRL+L""
        alt_key = ""ALT+CTRL+RIGHT""
        action = ""switch_monitor_right""

# Swapping active workspace windows with workspace at N on the active monitor
    [hotkeys.swap_workspace_1]
        key = ""ALT+CTRL+1""
        action = ""swap_workspace""

    [hotkeys.swap_workspace_2]
        key = ""ALT+CTRL+2""
        action = ""swap_workspace""

    [hotkeys.swap_workspace_3]
        key = ""ALT+CTRL+3""
        action = ""swap_workspace""

    [hotkeys.swap_workspace_4]
        key = ""ALT+CTRL+4""
        action = ""swap_workspace""

    [hotkeys.swap_workspace_5]
        key = ""ALT+CTRL+5""
        action = ""swap_workspace""

    [hotkeys.swap_workspace_6]
        key = ""ALT+CTRL+6""
        action = ""swap_workspace""

    [hotkeys.swap_workspace_7]
        key = ""ALT+CTRL+7""
        action = ""swap_workspace""

    [hotkeys.swap_workspace_8]
        key = ""ALT+CTRL+8""
        action = ""swap_workspace""

""";
        }
    }
}