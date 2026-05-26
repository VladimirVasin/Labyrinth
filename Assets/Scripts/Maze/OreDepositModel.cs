using System.Collections.Generic;
using UnityEngine;

namespace Labyrinth.Maze
{
    public enum OreDepositType
    {
        Iron,
        Gold
    }

    public sealed class OreDepositModel
    {
        private readonly List<Vector2Int> cells;

        public OreDepositModel(OreDepositType type, CaveInfo cave, IReadOnlyList<Vector2Int> depositCells)
        {
            Type = type;
            Cave = cave;
            cells = depositCells == null ? new List<Vector2Int>() : new List<Vector2Int>(depositCells);
        }

        public OreDepositType Type { get; }

        public CaveInfo Cave { get; }

        public IReadOnlyList<Vector2Int> Cells => cells;

        public bool IsDepleted { get; private set; }

        public void Deplete()
        {
            IsDepleted = true;
        }
    }
}
