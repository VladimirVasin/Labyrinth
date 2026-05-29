using System.Collections.Generic;
using Labyrinth.Core;
using UnityEngine;

namespace Labyrinth.Maze
{
    public static partial class MazeBranchCarver
    {
        private const int SpaciousPocketAreaDivisor = 360;
        private const int MinimumSpaciousPockets = 3;
        private const int MaximumSpaciousPockets = 36;
        private const int MinimumSpaciousPocketCarves = 2;

        public static void AddSpaciousAreas(
            MazeGrid grid,
            Vector2Int entrance,
            CentralRoomInfo centralRoom,
            System.Random random)
        {
            if (grid == null || random == null)
            {
                return;
            }

            var desiredCount = CalculateSpaciousPocketCount(grid.Width, grid.Height);
            var origins = CollectSpaciousPocketOrigins(grid, entrance, centralRoom);
            Shuffle(origins, random);

            var reservedCells = new HashSet<Vector2Int>();
            var carvedCells = new List<Vector2Int>(4);
            var placed = 0;
            var carved = 0;

            foreach (var origin in origins)
            {
                if (placed >= desiredCount)
                {
                    break;
                }

                if (reservedCells.Contains(origin))
                {
                    continue;
                }

                carvedCells.Clear();
                if (!TryCarveSpaciousPocket(grid, centralRoom, origin, random, reservedCells, carvedCells))
                {
                    continue;
                }

                placed++;
                carved += carvedCells.Count;
                ReserveSpaciousPocketArea(reservedCells, origin, carvedCells);
            }

            GameDebugLog.Info(
                "Maze",
                $"Spacious pockets: desired={desiredCount}, origins={origins.Count}, placed={placed}, carvedCells={carved}.");
        }

        private static int CalculateSpaciousPocketCount(int width, int height)
        {
            var desiredCount = Mathf.RoundToInt(width * height / (float)SpaciousPocketAreaDivisor);
            return Mathf.Clamp(desiredCount, MinimumSpaciousPockets, MaximumSpaciousPockets);
        }

        private static List<Vector2Int> CollectSpaciousPocketOrigins(
            MazeGrid grid,
            Vector2Int entrance,
            CentralRoomInfo centralRoom)
        {
            var origins = new List<Vector2Int>();
            for (var x = 1; x < grid.Width - 1; x++)
            {
                for (var y = 1; y < grid.Height - 1; y++)
                {
                    var position = new Vector2Int(x, y);
                    var cell = grid.Get(position);
                    if (!cell.IsWalkable
                        || cell.Type == MazeCellType.Entrance
                        || position == entrance
                        || centralRoom.Contains(position)
                        || !IsExtraConnectionPositionAllowed(position, centralRoom)
                        || IsOnEdge(grid, position))
                    {
                        continue;
                    }

                    var neighborCount = WalkableNeighborCount(grid, position);
                    if (neighborCount < 1 || neighborCount > 3)
                    {
                        continue;
                    }

                    origins.Add(position);
                }
            }

            return origins;
        }

        private static bool TryCarveSpaciousPocket(
            MazeGrid grid,
            CentralRoomInfo centralRoom,
            Vector2Int origin,
            System.Random random,
            HashSet<Vector2Int> reservedCells,
            List<Vector2Int> carvedCells)
        {
            var primaryDirections = new List<Vector2Int>(MazeDirections.Cardinal);
            var secondaryDirections = new List<Vector2Int>(MazeDirections.Cardinal);
            Shuffle(primaryDirections, random);
            Shuffle(secondaryDirections, random);

            foreach (var primary in primaryDirections)
            {
                foreach (var secondary in secondaryDirections)
                {
                    if (primary == secondary || primary + secondary == Vector2Int.zero)
                    {
                        continue;
                    }

                    var first = origin + primary;
                    var second = origin + secondary;
                    var corner = first + secondary;
                    if (!CanUseSpaciousPocketCell(grid, centralRoom, origin, first, reservedCells)
                        || !CanUseSpaciousPocketCell(grid, centralRoom, origin, second, reservedCells)
                        || !CanUseSpaciousPocketCell(grid, centralRoom, origin, corner, reservedCells))
                    {
                        continue;
                    }

                    var openableCount = CountOpenableSpaciousCells(grid, first, second, corner);
                    if (openableCount < MinimumSpaciousPocketCarves)
                    {
                        continue;
                    }

                    CarveSpaciousCell(grid, first, carvedCells);
                    CarveSpaciousCell(grid, second, carvedCells);
                    CarveSpaciousCell(grid, corner, carvedCells);
                    return carvedCells.Count >= MinimumSpaciousPocketCarves;
                }
            }

            return false;
        }

        private static int CountOpenableSpaciousCells(MazeGrid grid, Vector2Int first, Vector2Int second, Vector2Int corner)
        {
            var count = 0;
            if (grid.Get(first).Type == MazeCellType.Wall)
            {
                count++;
            }

            if (grid.Get(second).Type == MazeCellType.Wall)
            {
                count++;
            }

            if (grid.Get(corner).Type == MazeCellType.Wall)
            {
                count++;
            }

            return count;
        }

        private static bool CanUseSpaciousPocketCell(
            MazeGrid grid,
            CentralRoomInfo centralRoom,
            Vector2Int origin,
            Vector2Int position,
            HashSet<Vector2Int> reservedCells)
        {
            if (!grid.InBounds(position)
                || IsOnEdge(grid, position)
                || reservedCells.Contains(position)
                || centralRoom.Contains(position)
                || !IsExtraConnectionPositionAllowed(position, centralRoom)
                || !AreInSameMazeSection(origin, position, centralRoom))
            {
                return false;
            }

            var cell = grid.Get(position);
            return cell.Type == MazeCellType.Wall || cell.IsWalkable;
        }

        private static void CarveSpaciousCell(MazeGrid grid, Vector2Int position, List<Vector2Int> carvedCells)
        {
            if (grid.Get(position).Type != MazeCellType.Wall)
            {
                return;
            }

            grid.SetType(position, MazeCellType.Path);
            carvedCells.Add(position);
        }

        private static void ReserveSpaciousPocketArea(
            HashSet<Vector2Int> reservedCells,
            Vector2Int origin,
            IReadOnlyList<Vector2Int> carvedCells)
        {
            reservedCells.Add(origin);
            foreach (var direction in MazeDirections.Cardinal)
            {
                reservedCells.Add(origin + direction);
            }

            for (var i = 0; i < carvedCells.Count; i++)
            {
                var carved = carvedCells[i];
                reservedCells.Add(carved);
                foreach (var direction in MazeDirections.Cardinal)
                {
                    reservedCells.Add(carved + direction);
                }
            }
        }
    }
}
