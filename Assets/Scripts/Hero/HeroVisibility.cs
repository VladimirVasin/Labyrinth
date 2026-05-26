using System.Collections.Generic;
using Labyrinth.Maze;
using UnityEngine;

namespace Labyrinth.Hero
{
    public sealed class HeroVisibility
    {
        public const int SightRange = 2;

        private readonly HashSet<Vector2Int> visibleCells = new HashSet<Vector2Int>();

        public IReadOnlyCollection<Vector2Int> VisibleCells => visibleCells;

        public int VisibleCount => visibleCells.Count;

        public void Clear()
        {
            visibleCells.Clear();
        }

        public bool IsVisible(Vector2Int position)
        {
            return visibleCells.Contains(position);
        }

        public void Refresh(MazeGrid grid, Vector2Int origin)
        {
            Refresh(grid, origin, SightRange);
        }

        public void Refresh(MazeGrid grid, Vector2Int origin, int sightRange)
        {
            visibleCells.Clear();
            if (grid == null || !grid.InBounds(origin))
            {
                return;
            }

            var normalizedSightRange = Mathf.Max(0, sightRange);
            for (var x = origin.x - normalizedSightRange; x <= origin.x + normalizedSightRange; x++)
            {
                for (var y = origin.y - normalizedSightRange; y <= origin.y + normalizedSightRange; y++)
                {
                    var target = new Vector2Int(x, y);
                    if (!grid.InBounds(target) || ChebyshevDistance(origin, target) > normalizedSightRange)
                    {
                        continue;
                    }

                    if (CanSee(grid, origin, target))
                    {
                        visibleCells.Add(target);
                    }
                }
            }
        }

        private static bool CanSee(MazeGrid grid, Vector2Int origin, Vector2Int target)
        {
            if (origin == target)
            {
                return true;
            }

            var current = origin;
            var dx = Mathf.Abs(target.x - origin.x);
            var dy = Mathf.Abs(target.y - origin.y);
            var stepX = origin.x < target.x ? 1 : -1;
            var stepY = origin.y < target.y ? 1 : -1;
            var error = dx - dy;

            while (current != target)
            {
                var previous = current;
                var doubledError = error * 2;

                if (doubledError > -dy)
                {
                    error -= dy;
                    current.x += stepX;
                }

                if (doubledError < dx)
                {
                    error += dx;
                    current.y += stepY;
                }

                if (IsBlockedByCorner(grid, previous, current, target))
                {
                    return false;
                }

                if (current == target)
                {
                    return true;
                }

                if (IsBlockingWall(grid, current))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsBlockedByCorner(MazeGrid grid, Vector2Int previous, Vector2Int current, Vector2Int target)
        {
            if (previous.x == current.x || previous.y == current.y)
            {
                return false;
            }

            if (current == target && IsBlockingWall(grid, target))
            {
                return false;
            }

            var sideA = new Vector2Int(current.x, previous.y);
            var sideB = new Vector2Int(previous.x, current.y);
            return IsBlockingWall(grid, sideA) && IsBlockingWall(grid, sideB);
        }

        private static bool IsBlockingWall(MazeGrid grid, Vector2Int position)
        {
            return grid.InBounds(position) && grid.Get(position).Type == MazeCellType.Wall;
        }

        private static int ChebyshevDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
        }
    }
}
