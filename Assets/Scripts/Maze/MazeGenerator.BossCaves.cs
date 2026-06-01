using System.Collections.Generic;
using Labyrinth.Core;
using UnityEngine;

namespace Labyrinth.Maze
{
    public sealed partial class MazeGenerator
    {
        private static CaveInfo EnsureSecondHalfBossCave(
            MazeGrid grid,
            Vector2Int entrance,
            CentralRoomInfo centralRoom,
            List<CaveInfo> caves)
        {
            var bossCave = SelectFarthestSecondHalfCave(grid, entrance, centralRoom, caves, default);
            if (bossCave.IsValid)
            {
                return bossCave;
            }

            if (TryPlaceForcedSecondHalfCave(grid, entrance, centralRoom, caves, default, out bossCave))
            {
                caves.Add(bossCave);
                GameDebugLog.Warning("Maze", $"Boss cave fallback placed at {GameDebugLog.Position(bossCave.Center)}.");
                return bossCave;
            }

            bossCave = CarveFallbackSecondHalfCave(grid, entrance, centralRoom, caves, default, "Boss cave");
            if (bossCave.IsValid)
            {
                caves.Add(bossCave);
            }

            return bossCave;
        }

        private static void EnsureSecondHalfStairsCave(
            MazeGrid grid,
            Vector2Int entrance,
            CentralRoomInfo centralRoom,
            List<CaveInfo> caves,
            CaveInfo bossCave)
        {
            foreach (var cave in caves)
            {
                if (!IsSameCave(cave, bossCave) && centralRoom.IsBeyondExitSide(cave.Center))
                {
                    return;
                }
            }

            if (TryPlaceForcedSecondHalfCave(grid, entrance, centralRoom, caves, bossCave, out var stairsCave))
            {
                caves.Add(stairsCave);
                GameDebugLog.Warning("Maze", $"Second-half stairs cave fallback placed at {GameDebugLog.Position(stairsCave.Center)}.");
                return;
            }

            stairsCave = CarveFallbackSecondHalfCave(grid, entrance, centralRoom, caves, bossCave, "Second-half stairs cave");
            if (stairsCave.IsValid)
            {
                caves.Add(stairsCave);
            }
        }

        private static CaveInfo SelectFarthestSecondHalfCave(
            MazeGrid grid,
            Vector2Int entrance,
            CentralRoomInfo centralRoom,
            IReadOnlyList<CaveInfo> caves,
            CaveInfo excludedCave)
        {
            var distances = MazeValidation.GetReachableDistances(grid, entrance, true);
            var bestDistance = -1;
            var best = default(CaveInfo);
            foreach (var cave in caves)
            {
                if (IsSameCave(cave, excludedCave)
                    || !centralRoom.IsBeyondExitSide(cave.Center)
                    || !distances.TryGetValue(cave.Center, out var distance))
                {
                    continue;
                }

                if (distance > bestDistance)
                {
                    bestDistance = distance;
                    best = cave;
                }
            }

            return best;
        }

        private static bool TryPlaceForcedSecondHalfCave(
            MazeGrid grid,
            Vector2Int entrance,
            CentralRoomInfo centralRoom,
            IReadOnlyList<CaveInfo> caves,
            CaveInfo excludedCave,
            out CaveInfo cave)
        {
            cave = default;
            var candidates = CollectCaveCandidates(grid);
            var distances = MazeValidation.GetReachableDistances(grid, entrance, true);

            while (candidates.Count > 0)
            {
                var bestIndex = -1;
                var bestDistance = -1;
                for (var i = 0; i < candidates.Count; i++)
                {
                    var center = candidates[i];
                    if (!centralRoom.IsBeyondExitSide(center)
                        || ContainsCaveCell(excludedCave, center)
                        || IsCaveBlockedByCentralPassage(center, centralRoom))
                    {
                        continue;
                    }

                    var distance = distances.TryGetValue(center, out var reachableDistance)
                        ? reachableDistance
                        : GridDistance(center, entrance);
                    if (distance > bestDistance)
                    {
                        bestDistance = distance;
                        bestIndex = i;
                    }
                }

                if (bestIndex < 0)
                {
                    return false;
                }

                var selected = candidates[bestIndex];
                candidates.RemoveAt(bestIndex);
                if (TryPlaceCave(grid, selected, entrance, centralRoom, caves, out cave) == CavePlacementStatus.Placed)
                {
                    return true;
                }
            }

            return false;
        }

        private static CaveInfo CarveFallbackSecondHalfCave(
            MazeGrid grid,
            Vector2Int entrance,
            CentralRoomInfo centralRoom,
            IReadOnlyList<CaveInfo> caves,
            CaveInfo excludedCave,
            string label)
        {
            var center = FindFallbackSecondHalfCaveCenter(grid, entrance, centralRoom, caves, CaveSize);
            if (center == default)
            {
                center = FindFallbackSecondHalfCaveCenter(grid, entrance, centralRoom, caves, 1);
            }

            if (center == default || ContainsCaveCell(excludedCave, center))
            {
                GameDebugLog.Warning("Maze", $"{label} fallback failed: no second-half cave center was available.");
                return default;
            }

            var contacts = CollectExternalPathContacts(grid, center);
            if (contacts.Count == 0)
            {
                GameDebugLog.Warning("Maze", $"{label} fallback failed at {GameDebugLog.Position(center)}: no external contact.");
                return default;
            }

            var selectedContacts = SelectCaveEntranceContacts(contacts, entrance);
            var snapshots = new List<CellSnapshot>();
            ApplyCaveCandidate(grid, center, contacts, selectedContacts, snapshots);
            if (!AllWalkableCellsReachable(grid, entrance))
            {
                RestoreSnapshots(grid, snapshots);
                GameDebugLog.Warning("Maze", $"{label} fallback failed at {GameDebugLog.Position(center)}: disconnected maze.");
                return default;
            }

            GameDebugLog.Warning("Maze", $"{label} fallback carved at {GameDebugLog.Position(center)}.");
            return new CaveInfo(center, selectedContacts[0].EntrancePosition);
        }

        private static Vector2Int FindFallbackSecondHalfCaveCenter(
            MazeGrid grid,
            Vector2Int entrance,
            CentralRoomInfo centralRoom,
            IReadOnlyList<CaveInfo> caves,
            int minimumDistanceFromCaves)
        {
            var distances = MazeValidation.GetReachableDistances(grid, entrance, true);
            var bestDistance = -1;
            var best = default(Vector2Int);
            for (var x = centralRoom.Max.x + CaveSize; x <= grid.Width - CaveSize - 1; x++)
            {
                for (var y = CaveSize; y <= grid.Height - CaveSize - 1; y++)
                {
                    var center = new Vector2Int(x, y);
                    if (IsCaveBlockedByCentralPassage(center, centralRoom)
                        || IsTooCloseToExistingCave(center, caves, minimumDistanceFromCaves)
                        || !distances.TryGetValue(center, out var distance))
                    {
                        continue;
                    }

                    if (distance > bestDistance)
                    {
                        bestDistance = distance;
                        best = center;
                    }
                }
            }

            return best;
        }

        private static bool IsTooCloseToExistingCave(
            Vector2Int center,
            IReadOnlyList<CaveInfo> caves,
            int minimumDistance)
        {
            foreach (var cave in caves)
            {
                if (Mathf.Abs(center.x - cave.Center.x) <= CaveSize - 1
                    && Mathf.Abs(center.y - cave.Center.y) <= CaveSize - 1)
                {
                    return true;
                }

                if (GridDistance(center, cave.Center) < minimumDistance)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsSameCave(CaveInfo left, CaveInfo right)
        {
            return left.IsValid && right.IsValid && left.Center == right.Center;
        }

        private static bool ContainsCaveCell(CaveInfo cave, Vector2Int cell)
        {
            return cave.IsValid
                && Mathf.Abs(cell.x - cave.Center.x) <= CaveSize / 2
                && Mathf.Abs(cell.y - cave.Center.y) <= CaveSize / 2;
        }
    }
}
