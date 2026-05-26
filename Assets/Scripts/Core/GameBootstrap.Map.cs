using System.Collections.Generic;
using Labyrinth.Hero;
using Labyrinth.Maze;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Labyrinth.Core
{
    public sealed partial class GameBootstrap
    {
        private void HandleMapHotkey()
        {
            if (mapHud == null || !mapHud.Visible || !IsCommonMapUnlocked() || Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current.mKey.wasPressedThisFrame)
            {
                mapHud.ToggleExpanded();
            }
        }

        private MazeGenerationResult GetCurrentMaze()
        {
            return currentMaze;
        }

        private HeroMemory GetDisplayedKnowledgeMemory()
        {
            if (baseDevelopment != null && baseDevelopment.HasCartographerHouse)
            {
                return cartographerMemory;
            }

            return null;
        }

        private bool IsCommonMapUnlocked()
        {
            return baseDevelopment != null && baseDevelopment.HasCartographerHouse;
        }

        private HashSet<Vector2Int> BuildMapVisibleCells()
        {
            return currentMaze == null
                ? new HashSet<Vector2Int>()
                : BuildLightingVisibleCells(BuildVisibilityHeroes());
        }

        private HashSet<Vector2Int> BuildDisplayedExploredCells()
        {
            var exploredCells = new HashSet<Vector2Int>();
            if (currentMaze == null)
            {
                return exploredCells;
            }

            if (!IsCommonMapUnlocked())
            {
                return exploredCells;
            }

            exploredCells.Add(currentMaze.EntrancePosition);
            var memory = GetDisplayedKnowledgeMemory();
            if (memory == null)
            {
                return exploredCells;
            }

            foreach (var position in memory.RememberedCells)
            {
                exploredCells.Add(position);
            }

            foreach (var position in memory.RememberedWalls)
            {
                exploredCells.Add(position);
            }

            foreach (var position in memory.KnownClosedDoors)
            {
                exploredCells.Add(position);
            }

            return exploredCells;
        }

        private void RefreshMemoryOverlay(HashSet<Vector2Int> visibleCells = null)
        {
            if (sharedHeroMemoryView == null)
            {
                return;
            }

            sharedHeroMemoryView.ShowMemory(null);
        }

        private HashSet<Vector2Int> BuildKnownCells(HashSet<Vector2Int> visibleCells, HashSet<Vector2Int> exploredCells)
        {
            var knownCells = new HashSet<Vector2Int>();
            if (visibleCells != null)
            {
                foreach (var position in visibleCells)
                {
                    knownCells.Add(position);
                }
            }

            if (exploredCells != null)
            {
                foreach (var position in exploredCells)
                {
                    knownCells.Add(position);
                }
            }

            return knownCells;
        }
    }
}
