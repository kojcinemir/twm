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

using System.Collections.Generic;

namespace TilingWindowManager
{
    public class ColorSchemeColors
    {
        // Workspace Indicator Colors
        public uint ActiveWorkspaceBorderColor { get; set; }
        public uint BackgroundColor { get; set; }
        public uint ActiveWorkspaceColor { get; set; }
        public uint HoveredWorkspaceColor { get; set; }
        public uint InactiveWorkspaceColor { get; set; }
        public uint ActiveWorkspaceTextColor { get; set; }
        public uint InactiveWorkspaceTextColor { get; set; }
        public uint StackedModeWorkspaceColor { get; set; }
        public uint StackedModeBorderColor { get; set; }
        public uint PausedWorkspaceColor { get; set; }
        public uint PausedWorkspaceBorderColor { get; set; }

        // Stacked App Display Colors
        public uint StackedAppBackgroundColor { get; set; }
        public uint StackedAppHoverColor { get; set; }
        public uint StackedAppActiveColor { get; set; }
        public uint StackedAppTextColor { get; set; }
        public uint StackedAppActiveTextColor { get; set; }

        // Window Border Colors
        public uint BorderColor { get; set; }
    }

    public static class ColorScheme
    {
        private static readonly Dictionary<string, ColorSchemeColors> _schemes = new()
        {
            { "gruvbox", new ColorSchemeColors
                {
                    ActiveWorkspaceBorderColor = 0xfe8019,
                    BackgroundColor = 0x1d2021,                
                    ActiveWorkspaceColor = 0x282828,            
                    HoveredWorkspaceColor = 0x3c3836,           
                    InactiveWorkspaceColor = 0x1d2021,          
                    ActiveWorkspaceTextColor = 0xfabd2f,        
                    InactiveWorkspaceTextColor = 0xebdbb2,      
                    StackedModeWorkspaceColor = 0x1c1c1c,       
                    StackedModeBorderColor = 0x504945,
                    PausedWorkspaceColor = 0xb8bb26,            
                    PausedWorkspaceBorderColor = 0xb8bb26,      
                    StackedAppBackgroundColor = 0x1c1c1c,       
                    StackedAppHoverColor = 0x2f2f2f,            
                    StackedAppActiveColor = 0x383838,
                    StackedAppTextColor = 0xebdbb2,             
                    StackedAppActiveTextColor = 0xfabd2f,       
                    BorderColor = 0xfe8019                      
                }
            },
            { "tokyo-night", new ColorSchemeColors
                {
                    ActiveWorkspaceBorderColor = 0xc0a0ff,      
                    BackgroundColor = 0x16161e,                 
                    ActiveWorkspaceColor = 0xbb9af7,            
                    HoveredWorkspaceColor = 0xc9b3ff,           
                    InactiveWorkspaceColor = 0x9d7cd8,          
                    ActiveWorkspaceTextColor = 0x7dcfff,        
                    InactiveWorkspaceTextColor = 0xc0caf5,      
                    StackedModeWorkspaceColor = 0x24283b,       
                    StackedModeBorderColor = 0x292e42,          
                    PausedWorkspaceColor = 0x9ece6a,            
                    PausedWorkspaceBorderColor = 0x9ece6a,      
                    StackedAppBackgroundColor = 0x1a1b26,       
                    StackedAppHoverColor = 0x24283b,            
                    StackedAppActiveColor = 0x3d59a1,           
                    StackedAppTextColor = 0xc0caf5,             
                    StackedAppActiveTextColor = 0x7aa2f7,       
                    BorderColor = 0x7aa2f7                      
                }
            },
            { "nordic", new ColorSchemeColors
                {
                    ActiveWorkspaceBorderColor = 0xbf9fc0,      
                    BackgroundColor = 0x2e3440,                 
                    ActiveWorkspaceColor = 0xb48ead,            
                    HoveredWorkspaceColor = 0xc39fc0,           
                    InactiveWorkspaceColor = 0x9d7a95,          
                    ActiveWorkspaceTextColor = 0x88c0d0,        
                    InactiveWorkspaceTextColor = 0xeceff4,      
                    StackedModeWorkspaceColor = 0x434c5e,       
                    StackedModeBorderColor = 0x4c566a,          
                    PausedWorkspaceColor = 0xa3be8c,            
                    PausedWorkspaceBorderColor = 0xa3be8c,      
                    StackedAppBackgroundColor = 0x3b4252,       
                    StackedAppHoverColor = 0x434c5e,            
                    StackedAppActiveColor = 0x5e81ac,           
                    StackedAppTextColor = 0xd8dee9,             
                    StackedAppActiveTextColor = 0x88c0d0,       
                    BorderColor = 0x88c0d0                      
                }
            },
            { "vscode", new ColorSchemeColors
                {
                    ActiveWorkspaceBorderColor = 0x8b3eb8,      
                    BackgroundColor = 0x1e1e1e,                 
                    ActiveWorkspaceColor = 0x68217a,            
                    HoveredWorkspaceColor = 0x7a2e8f,           
                    InactiveWorkspaceColor = 0x581a66,          
                    ActiveWorkspaceTextColor = 0xffffff,        
                    InactiveWorkspaceTextColor = 0xcccccc,      
                    StackedModeWorkspaceColor = 0x37373d,       
                    StackedModeBorderColor = 0x3e3e42,          
                    PausedWorkspaceColor = 0x4ec9b0,            
                    PausedWorkspaceBorderColor = 0x4ec9b0,      
                    StackedAppBackgroundColor = 0x252526,       
                    StackedAppHoverColor = 0x2a2d2e,            
                    StackedAppActiveColor = 0x094771,           
                    StackedAppTextColor = 0xcccccc,             
                    StackedAppActiveTextColor = 0xffffff,       
                    BorderColor = 0x007acc                      
                }
            },
            { "everforest", new ColorSchemeColors
                {
                    ActiveWorkspaceBorderColor = 0xe3a8c7,      
                    BackgroundColor = 0x2b3339,                 
                    ActiveWorkspaceColor = 0xd699b6,            
                    HoveredWorkspaceColor = 0xe0aac3,           
                    InactiveWorkspaceColor = 0xbe85a0,          
                    ActiveWorkspaceTextColor = 0xdbbc7f,        
                    InactiveWorkspaceTextColor = 0xd3c6aa,      
                    StackedModeWorkspaceColor = 0x3a464c,       
                    StackedModeBorderColor = 0x414b50,          
                    PausedWorkspaceColor = 0xa7c080,            
                    PausedWorkspaceBorderColor = 0xa7c080,      
                    StackedAppBackgroundColor = 0x323d43,       
                    StackedAppHoverColor = 0x3a464c,            
                    StackedAppActiveColor = 0x7fbbb3,           
                    StackedAppTextColor = 0xd3c6aa,             
                    StackedAppActiveTextColor = 0xa7c080,       
                    BorderColor = 0xa7c080                      
                }
            },
            { "windows10", new ColorSchemeColors
                {
                    ActiveWorkspaceBorderColor = 0x8b3eb8,      
                    BackgroundColor = 0x1f1f1f,                 
                    ActiveWorkspaceColor = 0x6b2e8f,            
                    HoveredWorkspaceColor = 0x7d3fa3,           
                    InactiveWorkspaceColor = 0x5a2577,          
                    ActiveWorkspaceTextColor = 0x0078d4,        
                    InactiveWorkspaceTextColor = 0xe1e1e1,      
                    StackedModeWorkspaceColor = 0x1e3a5f,       
                    StackedModeBorderColor = 0x3e3e42,          
                    PausedWorkspaceColor = 0x10893e,            
                    PausedWorkspaceBorderColor = 0x10893e,      
                    StackedAppBackgroundColor = 0x2b2b2b,       
                    StackedAppHoverColor = 0x3f3f3f,            
                    StackedAppActiveColor = 0x005a9e,           
                    StackedAppTextColor = 0xe1e1e1,             
                    StackedAppActiveTextColor = 0x0078d4,       
                    BorderColor = 0x0078d4                      
                }
            }
        };

        public static ColorSchemeColors? GetScheme(string schemeName)
        {
            var normalizedName = schemeName.ToLowerInvariant().Trim();
            return _schemes.TryGetValue(normalizedName, out var scheme) ? scheme : null;
        }

        public static bool IsValidScheme(string schemeName)
        {
            return _schemes.ContainsKey(schemeName.ToLowerInvariant().Trim());
        }

        public static IEnumerable<string> GetAvailableSchemes()
        {
            return _schemes.Keys;
        }
    }
}
