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
    public class ApplicationHotkeyEntry
    {
        public string KeyCombination { get; set; } = "";
        public string ExecutableName { get; set; } = "";
        public string Key { get; set; } = "";
        public string[] Modifiers { get; set; } = Array.Empty<string>();
        public uint KeyCode { get; set; }
        public uint HotkeyId { get; set; }
    }

    public class ApplicationHotkeysConfiguration
    {
        private const string CONFIG_FILE_NAME = "config.toml";
        private readonly Dictionary<uint, ApplicationHotkeyEntry> _applicationHotkeys = new();
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

        public IEnumerable<ApplicationHotkeyEntry> AllApplicationHotkeys => _applicationHotkeys.Values;

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

                _applicationHotkeys.Clear();
                uint currentId = 1000; // starting with higher ID to avoid possible conflicts

                if (model.TryGetValue("application_hotkeys", out var appHotkeysObj) && appHotkeysObj is TomlTable appHotkeysTable)
                {
                    foreach (var kvp in appHotkeysTable)
                    {
                        string keyCombination = kvp.Key;
                        if (kvp.Value is string executableName)
                        {
                            var entry = ParseApplicationHotkeyEntry(keyCombination, executableName, currentId++);
                            if (entry != null)
                            {
                                _applicationHotkeys[entry.HotkeyId] = entry;
                            }
                        }
                        else
                        {
                            Logger.Warning($"Invalid executable name for hotkey {keyCombination}: {kvp.Value}");
                        }
                    }

                    return true;
                }
                else
                {
                    Logger.Info("No [application_hotkeys] section found in config");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error loading application hotkeys configuration");
                return false;
            }
        }

        private ApplicationHotkeyEntry? ParseApplicationHotkeyEntry(string keyCombination, string executableName, uint id)
        {
            try
            {
                var entry = new ApplicationHotkeyEntry
                {
                    KeyCombination = keyCombination,
                    ExecutableName = executableName.ToLowerInvariant(),
                    HotkeyId = id
                };

                if (!ParseKeyCombination(keyCombination, entry))
                {
                    Logger.Error($"Invalid key combination: {keyCombination}");
                    return null;
                }

                return entry;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error parsing application hotkey {keyCombination}");
                return null;
            }
        }

        private bool ParseKeyCombination(string keyCombination, ApplicationHotkeyEntry entry)
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

        public uint GetModifiers(ApplicationHotkeyEntry entry)
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

        public ApplicationHotkeyEntry? GetHotkeyById(uint hotkeyId)
        {
            return _applicationHotkeys.TryGetValue(hotkeyId, out var entry) ? entry : null;
        }

        public ApplicationHotkeyEntry? GetHotkeyByExecutable(string executableName)
        {
            string normalizedName = executableName.ToLowerInvariant();
            return _applicationHotkeys.Values.FirstOrDefault(h => h.ExecutableName == normalizedName);
        }

        public bool HasHotkeys => _applicationHotkeys.Count > 0;
    }
}