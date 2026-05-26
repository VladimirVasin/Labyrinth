using System.Collections.Generic;
using UnityEngine;

namespace Labyrinth.Maze
{
    public sealed class MazeGrid
    {
        private readonly MazeCell[,] cells;

        public MazeGrid(int width, int height)
        {
            Width = width;
            Height = height;
            cells = new MazeCell[width, height];

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    cells[x, y] = new MazeCell(x, y, MazeCellType.Wall);
                }
            }
        }

        public int Width { get; }

        public int Height { get; }

        public MazeCell Get(int x, int y)
        {
            return cells[x, y];
        }

        public MazeCell Get(Vector2Int position)
        {
            return cells[position.x, position.y];
        }

        public void SetType(int x, int y, MazeCellType type)
        {
            cells[x, y].Type = type;
        }

        public void SetType(Vector2Int position, MazeCellType type)
        {
            cells[position.x, position.y].Type = type;
        }

        public bool InBounds(int x, int y)
        {
            return x >= 0 && y >= 0 && x < Width && y < Height;
        }

        public bool InBounds(Vector2Int position)
        {
            return InBounds(position.x, position.y);
        }

        public IEnumerable<MazeCell> Cells()
        {
            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    yield return cells[x, y];
                }
            }
        }

        public IEnumerable<Vector2Int> WalkableNeighbors(Vector2Int position)
        {
            return WalkableNeighbors(position, false);
        }

        public IEnumerable<Vector2Int> WalkableNeighbors(Vector2Int position, bool includeClosedDoors)
        {
            foreach (var direction in MazeDirections.Cardinal)
            {
                var next = position + direction;
                if (!InBounds(next))
                {
                    continue;
                }

                var cell = Get(next);
                if (includeClosedDoors ? cell.IsStructurallyPassable : cell.IsWalkable)
                {
                    yield return next;
                }
            }
        }
    }
}
