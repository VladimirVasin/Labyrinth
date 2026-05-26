using Labyrinth.Maze;
using UnityEngine;

namespace Labyrinth.Core
{
    public sealed partial class MineConstructionController
    {
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

            return IsBlockingWall(grid, new Vector2Int(current.x, previous.y))
                && IsBlockingWall(grid, new Vector2Int(previous.x, current.y));
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
