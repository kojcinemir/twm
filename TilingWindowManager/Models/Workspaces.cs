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
using System.Linq;

namespace TilingWindowManager
{
    public class Workspaces
    {
        private List<Workspace> workspaceList;
        private int currentWorkspaceId;
        private int monitorIndex;

        public const int MAX_WORKSPACES = 8;

        public int CurrentWorkspaceId
        {
            get => currentWorkspaceId;
            private set => currentWorkspaceId = Math.Max(1, Math.Min(MAX_WORKSPACES, value));
        }

        public int MonitorIndex => monitorIndex;

        public Workspaces(int monitorIndex)
        {
            this.monitorIndex = monitorIndex;
            this.currentWorkspaceId = 1;

            workspaceList = new List<Workspace>();
            for (int i = 1; i <= MAX_WORKSPACES; i++)
            {
                workspaceList.Add(new Workspace(i, monitorIndex));
            }
        }

        public Workspace GetCurrentWorkspace()
        {
            return GetWorkspace(currentWorkspaceId);
        }

        public Workspace GetWorkspace(int workspaceId)
        {
            if (workspaceId >= 1 && workspaceId <= MAX_WORKSPACES)
            {
                return workspaceList[workspaceId - 1];
            }
            return workspaceList[0]; // fallback to workspace 1
        }

        public List<Workspace> GetAllWorkspaces()
        {
            return new List<Workspace>(workspaceList);
        }

        public bool SwitchToWorkspace(int workspaceId)
        {
            if (workspaceId >= 1 && workspaceId <= MAX_WORKSPACES && workspaceId != currentWorkspaceId)
            {
                var previousWorkspace = GetCurrentWorkspace();
                var newWorkspace = GetWorkspace(workspaceId);

                if (!newWorkspace.IsStackedMode)
                {
                    newWorkspace.ShowAllWindows();
                }

                previousWorkspace.HideAllWindows();

                currentWorkspaceId = workspaceId;
                return true;
            }
            return false;
        }

        public void AddWindowToCurrentWorkspace(nint window)
        {
            var currentWorkspace = GetCurrentWorkspace();

            RemoveWindowFromAllWorkspaces(window);

            currentWorkspace.AddWindow(window);
        }

        public void RemoveWindowFromAllWorkspaces(nint window)
        {
            foreach (var workspace in workspaceList)
            {
                workspace.RemoveWindow(window);
            }
        }

        public void MoveWindowToWorkspace(nint window, int targetWorkspaceId)
        {
            if (targetWorkspaceId < 1 || targetWorkspaceId > MAX_WORKSPACES)
                return;

            RemoveWindowFromAllWorkspaces(window);

            var targetWorkspace = GetWorkspace(targetWorkspaceId);
            targetWorkspace.AddWindow(window);
        }

        public Workspace? FindWorkspaceContaining(nint window)
        {
            return workspaceList.FirstOrDefault(w => w.ContainsWindow(window));
        }

        public bool IsWindowInCurrentWorkspace(nint window)
        {
            return GetCurrentWorkspace().ContainsWindow(window);
        }

        public int GetTotalWindowCount()
        {
            return workspaceList.Sum(w => w.WindowCount);
        }

        public int GetCurrentWorkspaceWindowCount()
        {
            return GetCurrentWorkspace().WindowCount;
        }

        public List<nint> GetAllWindowsInCurrentWorkspace()
        {
            return GetCurrentWorkspace().GetAllWindows();
        }

        public List<nint> GetTileableWindowsInCurrentWorkspace()
        {
            return GetCurrentWorkspace().GetTileableWindows();
        }

        public void ClearAllWorkspaces()
        {
            foreach (var workspace in workspaceList)
            {
                workspace.ClearWindows();
            }
        }

        public void ClearCurrentWorkspace()
        {
            GetCurrentWorkspace().ClearWindows();
        }

        public override string ToString()
        {
            var current = GetCurrentWorkspace();
            return $"Workspaces: {MAX_WORKSPACES} total, Current: {currentWorkspaceId} ({current.WindowCount} windows), Total Windows: {GetTotalWindowCount()}";
        }
    }
}