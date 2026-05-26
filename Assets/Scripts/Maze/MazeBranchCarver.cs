using System.Collections.Generic;
using Labyrinth.Core;
using UnityEngine;

namespace Labyrinth.Maze
{
    public static class MazeBranchCarver
    {
        private const int ExtraConnectionAreaDivisor = 180;
        private const int MinimumExtraConnections = 4;
        private const int MaximumExtraConnections = 90;
        private const int EntranceBranchSearchDistance = 6;
        private const int EntranceBranchFallbackDistance = 3;

        public static void AddExtraConnections(
            MazeGrid grid,
            Vector2Int entrance,
            CentralRoomInfo centralRoom,
            System.Random random)
        {
            var desiredCount = CalculateExtraConnectionCount(grid.Width, grid.Height);
            if (desiredCount <= 0)
            {
                return;
            }

            var candidates = CollectExtraConnectionCandidates(grid, centralRoom);
            Shuffle(candidates, random);
            var placed = 0;
            var junctions = 0;
            var firstBranch = TryCreateEntranceBranch(grid, entrance, centralRoom, candidates, random, out var firstBranchPosition);
            if (firstBranch)
            {
                placed++;
                junctions++;
            }

            placed += PlaceExtraConnections(grid, centralRoom, candidates, desiredCount - placed, true, ref junctions);
            placed += PlaceExtraConnections(grid, centralRoom, candidates, desiredCount - placed, false, ref junctions);

            GameDebugLog.Info(
                "Maze",
                $"Extra connections: desired={desiredCount}, candidates={candidates.Count}, placed={placed}, junctions={junctions}, entranceBranch={(firstBranch ? GameDebugLog.Position(firstBranchPosition) : "none")}");
        }

        private static int PlaceExtraConnections(
            MazeGrid grid,
            CentralRoomInfo centralRoom,
            IReadOnlyList<Vector2Int> candidates,
            int desiredCount,
            bool requireJunction,
            ref int junctions)
        {
            var placed = 0;
            if (desiredCount <= 0)
            {
                return placed;
            }

            foreach (var candidate in candidates)
            {
                if (placed >= desiredCount)
                {
                    break;
                }

                if (grid.Get(candidate).Type != MazeCellType.Wall
                    || !WouldConnectWalkableCorridors(grid, candidate, centralRoom))
                {
                    continue;
                }

                var createsJunction = WouldCreateJunction(grid, candidate);
                if (requireJunction && !createsJunction)
                {
                    continue;
                }

                grid.SetType(candidate, MazeCellType.Path);
                placed++;
                if (createsJunction)
                {
                    junctions++;
                }
            }

            return placed;
        }

        private static bool TryCreateEntranceBranch(
            MazeGrid grid,
            Vector2Int entrance,
            CentralRoomInfo centralRoom,
            IReadOnlyList<Vector2Int> candidates,
            System.Random random,
            out Vector2Int branchPosition)
        {
            var distances = MazeValidation.GetReachableDistances(grid, entrance, true);
            if (TryFindEntranceLoopBranch(grid, candidates, distances, EntranceBranchFallbackDistance, out branchPosition))
            {
                grid.SetType(branchPosition, MazeCellType.Path);
                return true;
            }

            if (TryCreateEntranceSideSpur(grid, centralRoom, distances, random, out branchPosition))
            {
                return true;
            }

            if (TryFindEntranceLoopBranch(grid, candidates, distances, EntranceBranchSearchDistance, out branchPosition))
            {
                grid.SetType(branchPosition, MazeCellType.Path);
                return true;
            }

            GameDebugLog.Warning("Maze", $"Entrance branch not created near {GameDebugLog.Position(entrance)}: no valid loop or side spur candidate.");
            return false;
        }

        private static bool TryFindEntranceLoopBranch(
            MazeGrid grid,
            IReadOnlyList<Vector2Int> candidates,
            IReadOnlyDictionary<Vector2Int, int> distances,
            int maxDistance,
            out Vector2Int branchPosition)
        {
            branchPosition = default;
            var bestDistance = int.MaxValue;
            foreach (var candidate in candidates)
            {
                if (grid.Get(candidate).Type != MazeCellType.Wall
                    || !WouldCreateJunction(grid, candidate)
                    || !TryGetNearestWalkableDistance(candidate, distances, out var distance)
                    || distance > maxDistance
                    || distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                branchPosition = candidate;
            }

            return bestDistance < int.MaxValue;
        }

        private static bool TryCreateEntranceSideSpur(
            MazeGrid grid,
            CentralRoomInfo centralRoom,
            Dictionary<Vector2Int, int> distances,
            System.Random random,
            out Vector2Int branchPosition)
        {
            branchPosition = default;
            for (var distance = 1; distance <= EntranceBranchFallbackDistance; distance++)
            {
                var origins = CollectWalkableCellsAtDistance(distances, distance);
                Shuffle(origins, random);
                foreach (var origin in origins)
                {
                    if (!IsInFirstSection(origin, centralRoom) || WalkableNeighborCount(grid, origin) < 2)
                    {
                        continue;
                    }

                    var directions = new List<Vector2Int>(MazeDirections.Cardinal);
                    Shuffle(directions, random);
                    foreach (var direction in directions)
                    {
                        var side = origin + direction;
                        var end = side + direction;
                        if (!grid.InBounds(side)
                            || !grid.InBounds(end)
                            || !IsInFirstSection(side, centralRoom)
                            || !IsInFirstSection(end, centralRoom)
                            || grid.Get(side).Type != MazeCellType.Wall
                            || grid.Get(end).Type != MazeCellType.Wall)
                        {
                            continue;
                        }

                        grid.SetType(side, MazeCellType.Path);
                        grid.SetType(end, MazeCellType.Path);
                        branchPosition = side;
                        return true;
                    }
                }
            }

            return false;
        }

        private static int CalculateExtraConnectionCount(int width, int height)
        {
            var desiredCount = Mathf.RoundToInt(width * height / (float)ExtraConnectionAreaDivisor);
            return Mathf.Clamp(desiredCount, MinimumExtraConnections, MaximumExtraConnections);
        }

        private static List<Vector2Int> CollectExtraConnectionCandidates(
            MazeGrid grid,
            CentralRoomInfo centralRoom)
        {
            var candidates = new List<Vector2Int>();
            for (var x = 1; x < grid.Width - 1; x++)
            {
                for (var y = 1; y < grid.Height - 1; y++)
                {
                    var position = new Vector2Int(x, y);
                    if (grid.Get(position).Type != MazeCellType.Wall
                        || !IsExtraConnectionPositionAllowed(position, centralRoom)
                        || !WouldConnectWalkableCorridors(grid, position, centralRoom))
                    {
                        continue;
                    }

                    candidates.Add(position);
                }
            }

            return candidates;
        }

        private static bool WouldConnectWalkableCorridors(
            MazeGrid grid,
            Vector2Int position,
            CentralRoomInfo centralRoom)
        {
            var left = position + Vector2Int.left;
            var right = position + Vector2Int.right;
            var down = position + Vector2Int.down;
            var up = position + Vector2Int.up;
            return AreExtraConnectionNeighbors(grid, left, right, centralRoom)
                || AreExtraConnectionNeighbors(grid, down, up, centralRoom);
        }

        private static bool WouldCreateJunction(MazeGrid grid, Vector2Int position)
        {
            if (grid.Get(position).Type != MazeCellType.Wall)
            {
                return false;
            }

            if (WalkableNeighborCountAfterCarve(grid, position, position) >= 3)
            {
                return true;
            }

            foreach (var direction in MazeDirections.Cardinal)
            {
                var neighbor = position + direction;
                if (grid.InBounds(neighbor)
                    && grid.Get(neighbor).IsWalkable
                    && WalkableNeighborCountAfterCarve(grid, neighbor, position) >= 3)
                {
                    return true;
                }
            }

            return false;
        }

        private static int WalkableNeighborCountAfterCarve(
            MazeGrid grid,
            Vector2Int position,
            Vector2Int carvedPosition)
        {
            var count = 0;
            foreach (var direction in MazeDirections.Cardinal)
            {
                var neighbor = position + direction;
                if (!grid.InBounds(neighbor))
                {
                    continue;
                }

                if (neighbor == carvedPosition || grid.Get(neighbor).IsWalkable)
                {
                    count++;
                }
            }

            return count;
        }

        private static int WalkableNeighborCount(MazeGrid grid, Vector2Int position)
        {
            var count = 0;
            foreach (var direction in MazeDirections.Cardinal)
            {
                var neighbor = position + direction;
                if (grid.InBounds(neighbor) && grid.Get(neighbor).IsWalkable)
                {
                    count++;
                }
            }

            return count;
        }

        private static bool TryGetNearestWalkableDistance(
            Vector2Int position,
            IReadOnlyDictionary<Vector2Int, int> distances,
            out int nearestDistance)
        {
            nearestDistance = int.MaxValue;
            foreach (var direction in MazeDirections.Cardinal)
            {
                var neighbor = position + direction;
                if (distances.TryGetValue(neighbor, out var distance) && distance < nearestDistance)
                {
                    nearestDistance = distance;
                }
            }

            return nearestDistance < int.MaxValue;
        }

        private static List<Vector2Int> CollectWalkableCellsAtDistance(
            IReadOnlyDictionary<Vector2Int, int> distances,
            int targetDistance)
        {
            var cells = new List<Vector2Int>();
            foreach (var pair in distances)
            {
                if (pair.Value == targetDistance)
                {
                    cells.Add(pair.Key);
                }
            }

            return cells;
        }

        private static bool AreExtraConnectionNeighbors(
            MazeGrid grid,
            Vector2Int first,
            Vector2Int second,
            CentralRoomInfo centralRoom)
        {
            return grid.InBounds(first)
                && grid.InBounds(second)
                && grid.Get(first).IsWalkable
                && grid.Get(second).IsWalkable
                && IsExtraConnectionPositionAllowed(first, centralRoom)
                && IsExtraConnectionPositionAllowed(second, centralRoom)
                && AreInSameMazeSection(first, second, centralRoom);
        }

        private static bool IsExtraConnectionPositionAllowed(
            Vector2Int position,
            CentralRoomInfo centralRoom)
        {
            return IsInFirstSection(position, centralRoom)
                || IsInSecondSection(position, centralRoom);
        }

        private static bool AreInSameMazeSection(
            Vector2Int first,
            Vector2Int second,
            CentralRoomInfo centralRoom)
        {
            return (IsInFirstSection(first, centralRoom) && IsInFirstSection(second, centralRoom))
                || (IsInSecondSection(first, centralRoom) && IsInSecondSection(second, centralRoom));
        }

        private static bool IsInFirstSection(Vector2Int position, CentralRoomInfo centralRoom)
        {
            return position.x < centralRoom.Min.x
                && !IsBlockedRoomSideContact(position, centralRoom, centralRoom.EntranceExternalPosition);
        }

        private static bool IsInSecondSection(Vector2Int position, CentralRoomInfo centralRoom)
        {
            return position.x > centralRoom.Max.x
                && !IsBlockedRoomSideContact(position, centralRoom, centralRoom.ExitExternalPosition);
        }

        private static bool IsBlockedRoomSideContact(
            Vector2Int position,
            CentralRoomInfo centralRoom,
            Vector2Int openContact)
        {
            var isSideContact = (position.x == centralRoom.Min.x - 1 || position.x == centralRoom.Max.x + 1)
                && position.y >= centralRoom.Min.y
                && position.y <= centralRoom.Max.y;
            return isSideContact && position != openContact;
        }

        private static void Shuffle(List<Vector2Int> positions, System.Random random)
        {
            for (var i = positions.Count - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                var temp = positions[i];
                positions[i] = positions[j];
                positions[j] = temp;
            }
        }
    }
}
