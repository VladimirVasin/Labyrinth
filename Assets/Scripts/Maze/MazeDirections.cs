using UnityEngine;

namespace Labyrinth.Maze
{
    public static class MazeDirections
    {
        public static readonly Vector2Int[] Cardinal =
        {
            new Vector2Int(0, 1),
            new Vector2Int(1, 0),
            new Vector2Int(0, -1),
            new Vector2Int(-1, 0)
        };

        public static readonly Vector2Int[] CarveSteps =
        {
            new Vector2Int(0, 2),
            new Vector2Int(2, 0),
            new Vector2Int(0, -2),
            new Vector2Int(-2, 0)
        };
    }
}
