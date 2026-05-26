namespace Labyrinth.Maze
{
    public sealed class MazeCell
    {
        public MazeCell(int x, int y, MazeCellType type)
        {
            X = x;
            Y = y;
            Type = type;
        }

        public int X { get; }

        public int Y { get; }

        public MazeCellType Type { get; set; }

        public bool IsRemembered { get; set; }

        public bool IsWalkable => Type != MazeCellType.Wall
            && Type != MazeCellType.ClosedDoor
            && Type != MazeCellType.LockedDownStairs;

        public bool IsStructurallyPassable => Type != MazeCellType.Wall;
    }
}
