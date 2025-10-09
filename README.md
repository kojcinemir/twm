# Tiling Window Manager (TWM) for Widnows 11/10

I've always been drawn to tiling window managers on linux and the productivity they bring to the table after getting used to them. However, my day job requires working on a Windows machine, which led me on a journey to create a Tiling Window Manager for Windows. What started as a simple project with just a few features quickly evolved as I found myself adding more and more functionality. Now it has matured enough that I'm excited to share it with the world.

Note: Though technically still in alpha, the project has reached a level of stability where I comfortably use it as my daily driver.

## Features

- **8 Virtual Workspaces** - Each monitor have its own 8 virtual workspaces (1-8)
- **BSP Tiling** - Automatic window tiling
- **Multi-Monitor Support** - Multi-monitor support with backup and restore functionality.
- **Stacked Mode** - Stacked mode for stacking full screen applications on same workspace
- **Workspace Indicator** - Workspace indicator located added to the taskbar itself. For me it is crutial feature for fast navigation across workspaces.
- **Active window border** - Highlight focused windows with colored border
- **Hotkey system** -  Hotkey system for managing windows with keyboard
- **Application Pinning** - Ability to pin application to specific workspace
- **Application hotkey shortcuts** - Ability to assign hotkey to application, and get quick way to locate application on workspaces
- **Drag & Swap** - Mouse actions to resize, swap windows, or rearange 

## Installation

### From Release

Requires .NET Desktop Runtime 8 installed on the machine

1. Download the latest release from the releases page
2. Extract the archive to your desired location
3. Run `TilingWindowManager.exe`
4. Configure your hotkeys and preferences in `config.toml`

## Configuration

All configuration is done through the `config.toml` file located in the application directory.

## IMPORTANT
Don't kill application in the processes. Use default (ALT+CTRL+O) keybinding to exit cleanly and properly restore hidden windows.

## Hotkeys

### Workspace Navigation
- `ALT+1-8` - Switch to workspace 1-8 on active monitor
- `ALT+SHIFT+1-8` - Move active window to workspace 1-8 on active monitor
- `ALT+CTRL+1-8` - Swap current workspace with workspace 1-8 on active monitor

### Window Focus - VIM based shortcuts
- `ALT+H` - Focus window to the left
- `ALT+J` - Focus window below
- `ALT+K` - Focus window above
- `ALT+L` - Focus window to the right

### Window Movement
- `ALT+SHIFT+H` - Swap window left
- `ALT+SHIFT+J` - Swap window down
- `ALT+SHIFT+K` - Swap window up
- `ALT+SHIFT+L` - Swap window right

### Monitor Management
- `ALT+CTRL+H` - Switch to left monitor
- `ALT+CTRL+J` - Switch to bottom monitor
- `ALT+CTRL+K` - Switch to top monitor
- `ALT+CTRL+L` - Switch to right monitor

### Window Management
- `ALT+SHIFT+C` - Close active window
- `ALT+T` - Remove window from tiling or introduce tiling to window that was previously tiled
- `ALT+[` - Decrease split ratio
- `ALT+]` - Increase split ratio

### Layout Management
- `ALT+SHIFT+S` - Toggle stacked mode
- `ALT+S` - Cycle through stacked windows

### Utility
- `ALT+R` - Refresh active workspace tiling - Usefull to try and "retile" workspace if there is a issue.
- `ALT+CTRL+O` - Clean Exit application 

## Requirements

- Windows 10/11
- .NET 8.0 Runtime

## Contributing

Contributions are welcome! Please feel free to submit issues and pull requests.

## License

Copyright (C) 2025 Kojčin Emir

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program. If not, see <https://www.gnu.org/licenses/>.

## Author

Kojčin Emir - 2025

## Acknowledgments

Inspired by BSP, Hyperland and other tiling window managers in the Linux ecosystem.
