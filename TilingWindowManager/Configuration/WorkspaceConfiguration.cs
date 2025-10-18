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
using Tomlyn;
using Tomlyn.Model;

namespace TilingWindowManager
{
    public enum WorkspaceMode
    {
        Tiled,
        Stacked,
        Paused
    }

    public class WorkspaceConfiguration
    {
        private const string CONFIG_FILE_NAME = "config.toml";

        private Dictionary<int, WorkspaceMode> workspaceModes = new Dictionary<int, WorkspaceMode>();

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
                    LoadWorkspaceModes(workspaceTable);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error loading workspace configuration");
                return false;
            }
        }

        private void LoadWorkspaceModes(TomlTable workspaceTable)
        {
            workspaceModes.Clear();

            for (int i = 1; i <= 8; i++)
            {
                string key = $"workspace_{i}";
                if (workspaceTable.TryGetValue(key, out var modeValue))
                {
                    string modeStr = modeValue.ToString()?.ToLower() ?? "";
                    WorkspaceMode mode = ParseWorkspaceMode(modeStr);
                    workspaceModes[i] = mode;
                }
            }
        }

        private WorkspaceMode ParseWorkspaceMode(string modeStr)
        {
            return modeStr switch
            {
                "tiled" => WorkspaceMode.Tiled,
                "stacked" => WorkspaceMode.Stacked,
                "paused" => WorkspaceMode.Paused,
                _ => WorkspaceMode.Tiled
            };
        }

        public WorkspaceMode GetWorkspaceMode(int workspaceId)
        {
            if (workspaceModes.TryGetValue(workspaceId, out var mode))
            {
                return mode;
            }

            return WorkspaceMode.Tiled;
        }
    }
}
