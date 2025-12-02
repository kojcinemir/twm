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
    public static class FuzzyMatcher
    {
        public static int CalculateScore(string searchTerm, string target)
        {
            if (string.IsNullOrEmpty(searchTerm))
                return 1000; // empty search matches all with high score

            if (string.IsNullOrEmpty(target))
                return 0;

            searchTerm = searchTerm.ToLowerInvariant();
            target = target.ToLowerInvariant();

            int score = 0;
            int searchIndex = 0;
            int consecutiveMatches = 0;

            for (int i = 0; i < target.Length && searchIndex < searchTerm.Length; i++)
            {
                if (target[i] == searchTerm[searchIndex])
                {
                    score += 10;

                    consecutiveMatches++;
                    score += consecutiveMatches * 5;

                    if (i == 0)
                        score += 50;

                    if (i > 0 && (target[i - 1] == ' ' || target[i - 1] == '-' || target[i - 1] == '_'))
                        score += 30;

                    searchIndex++;
                }
                else
                {
                    consecutiveMatches = 0;
                }
            }

            return searchIndex == searchTerm.Length ? score : 0;
        }

        public static List<WindowSearchEntry> FilterAndSort(
            List<WindowSearchEntry> entries,
            string searchTerm,
            int maxResults)
        {
            if (string.IsNullOrEmpty(searchTerm))
            {
                return entries.Take(maxResults).ToList();
            }

            foreach (var entry in entries)
            {
                entry.FuzzyScore = CalculateScore(searchTerm, entry.Title);
            }

            return entries
                .Where(e => e.FuzzyScore > 0)
                .OrderByDescending(e => e.FuzzyScore)
                .Take(maxResults)
                .ToList();
        }
    }
}
