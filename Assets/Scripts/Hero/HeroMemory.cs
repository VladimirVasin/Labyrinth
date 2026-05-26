using System.Collections.Generic;
using Labyrinth.Maze;
using UnityEngine;

namespace Labyrinth.Hero
{
    public sealed class HeroMemory
    {
        private MazeGrid grid;
        private readonly HashSet<Vector2Int> rememberedCells = new HashSet<Vector2Int>();
        private readonly HashSet<Vector2Int> rememberedWalls = new HashSet<Vector2Int>();
        private readonly HashSet<Vector2Int> knownClosedDoors = new HashSet<Vector2Int>();

        public HeroMemory(MazeGrid grid)
        {
            this.grid = grid;
        }

        public int RememberedCount => rememberedCells.Count;

        public int KnownCellCount => rememberedCells.Count + rememberedWalls.Count + knownClosedDoors.Count;

        public IEnumerable<Vector2Int> RememberedCells => rememberedCells;

        public IEnumerable<Vector2Int> RememberedWalls => rememberedWalls;

        public int RememberedWallCount => rememberedWalls.Count;

        public int KnownClosedDoorCount => knownClosedDoors.Count;

        public IEnumerable<Vector2Int> KnownClosedDoors => knownClosedDoors;

        public bool IsRemembered(Vector2Int position)
        {
            return rememberedCells.Contains(position);
        }

        public bool IsWallRemembered(Vector2Int position)
        {
            return rememberedWalls.Contains(position);
        }

        public bool IsKnown(Vector2Int position)
        {
            return IsRemembered(position)
                || IsWallRemembered(position)
                || IsClosedDoorKnown(position);
        }

        public bool Remember(Vector2Int position)
        {
            if (!grid.InBounds(position) || !grid.Get(position).IsWalkable)
            {
                return false;
            }

            return rememberedCells.Add(position);
        }

        public bool RememberWall(Vector2Int position)
        {
            if (!grid.InBounds(position) || grid.Get(position).Type != MazeCellType.Wall)
            {
                return false;
            }

            return rememberedWalls.Add(position);
        }

        public int MergeFrom(HeroMemory source)
        {
            if (source == null || source == this)
            {
                return 0;
            }

            var added = 0;
            foreach (var position in source.RememberedCells)
            {
                if (Remember(position))
                {
                    added++;
                }
            }

            foreach (var position in source.RememberedWalls)
            {
                if (RememberWall(position))
                {
                    added++;
                }
            }

            foreach (var position in source.KnownClosedDoors)
            {
                if (RememberClosedDoor(position))
                {
                    added++;
                }
            }

            return added;
        }

        public void Clear()
        {
            rememberedCells.Clear();
            rememberedWalls.Clear();
            knownClosedDoors.Clear();
        }

        public void Reset(MazeGrid nextGrid)
        {
            grid = nextGrid;
            Clear();
        }

        public bool IsClosedDoorKnown(Vector2Int position)
        {
            return knownClosedDoors.Contains(position);
        }

        public bool RememberClosedDoor(Vector2Int position)
        {
            if (!grid.InBounds(position)
                || (grid.Get(position).Type != MazeCellType.ClosedDoor
                    && grid.Get(position).Type != MazeCellType.LockedDownStairs))
            {
                return false;
            }

            return knownClosedDoors.Add(position);
        }

        public void ForgetClosedDoor(Vector2Int position)
        {
            knownClosedDoors.Remove(position);
        }
    }
}
