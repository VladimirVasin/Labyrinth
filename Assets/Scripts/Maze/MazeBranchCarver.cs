using System.Collections.Generic;
using Labyrinth.Core;
using UnityEngine;

namespace Labyrinth.Maze
{
    public static partial class MazeBranchCarver
    {
        private const int ExtraConnectionAreaDivisor = 180;
        private const int MinimumExtraConnections = 4;
        private const int MaximumExtraConnections = 90;
        private const int EntranceBranchSearchDistance = 6;
        private const int EntranceBranchFallbackDistance = 3;
        private const int AlternativeRoutePathRadius = 5;
        private const int MaxAlternativeCarvesPerGoal = 18;
        private const int MaxAlternativeRouteStabilizationPasses = 8;

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

        public static void EnsureAlternativeRoutes(
            MazeGrid grid,
            Vector2Int entrance,
            CentralRoomInfo centralRoom,
            KeyPickupModel centralRoomKey,
            DungeonStairsModel downStairs,
            IReadOnlyList<CaveInfo> caves,
            System.Random random)
        {
            var goals = BuildRouteGoals(grid, entrance, centralRoom, centralRoomKey, downStairs, caves);
            if (goals.Count == 0)
            {
                return;
            }

            var initialCandidateCount = CollectExtraConnectionCandidates(grid, centralRoom).Count;
            var satisfiedBefore = 0;
            var satisfiedAfter = 0;
            var carved = 0;
            var failed = 0;
            var carvedDetails = string.Empty;

            for (var pass = 0; pass < MaxAlternativeRouteStabilizationPasses; pass++)
            {
                goals = BuildRouteGoals(grid, entrance, centralRoom, centralRoomKey, downStairs, caves);
                if (goals.Count == 0)
                {
                    break;
                }

                var carvedThisPass = 0;
                foreach (var goal in goals)
                {
                    if (IsRouteGoalSatisfied(grid, goal, out _))
                    {
                        if (pass == 0)
                        {
                            satisfiedBefore++;
                        }

                        continue;
                    }

                    var carvedForGoal = 0;
                    while (carvedForGoal < MaxAlternativeCarvesPerGoal
                        && !IsRouteGoalSatisfied(grid, goal, out _))
                    {
                        var candidates = CollectExtraConnectionCandidates(grid, centralRoom);
                        Shuffle(candidates, random);
                        if (!TryCarveAlternativeRouteConnection(
                            grid,
                            centralRoom,
                            goal,
                            candidates,
                            random,
                            carvedForGoal < 2,
                            out var carvedPosition))
                        {
                            break;
                        }

                        carvedForGoal++;
                        carvedThisPass++;
                        carved++;
                        AppendCarvedDetail(ref carvedDetails, goal.Name, carvedPosition);
                    }

                    if (carvedForGoal == 0 && pass == MaxAlternativeRouteStabilizationPasses - 1)
                    {
                        IsRouteGoalSatisfied(grid, goal, out var failedDetails);
                        GameDebugLog.Warning(
                            "Maze",
                            $"Early alternative route still limited: goal={goal.Name}, start={GameDebugLog.Position(goal.Start)}, target={GameDebugLog.Position(goal.Target)}, pass={pass + 1}, details={failedDetails}.");
                    }
                }

                goals = BuildRouteGoals(grid, entrance, centralRoom, centralRoomKey, downStairs, caves);
                if (AreAllRouteGoalsSatisfied(grid, goals))
                {
                    break;
                }

                if (carvedThisPass == 0)
                {
                    break;
                }
            }

            goals = BuildRouteGoals(grid, entrance, centralRoom, centralRoomKey, downStairs, caves);
            foreach (var goal in goals)
            {
                if (IsRouteGoalSatisfied(grid, goal, out _))
                {
                    satisfiedAfter++;
                    continue;
                }

                IsRouteGoalSatisfied(grid, goal, out var failedDetails);
                failed++;
                GameDebugLog.Warning(
                    "Maze",
                    $"Early alternative route still limited after stabilization: goal={goal.Name}, start={GameDebugLog.Position(goal.Start)}, target={GameDebugLog.Position(goal.Target)}, details={failedDetails}.");
            }

            GameDebugLog.Info(
                "Maze",
                $"Early alternative routes: goals={goals.Count}, initialCandidates={initialCandidateCount}, satisfiedBefore={satisfiedBefore}, satisfiedAfter={satisfiedAfter}, carved={carved}, failed={failed}{(string.IsNullOrEmpty(carvedDetails) ? string.Empty : $", carvedAt={carvedDetails}")}");
        }

        private static bool AreAllRouteGoalsSatisfied(MazeGrid grid, IReadOnlyList<RouteGoal> goals)
        {
            foreach (var goal in goals)
            {
                if (!IsRouteGoalSatisfied(grid, goal, out _))
                {
                    return false;
                }
            }

            return true;
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

        private static List<RouteGoal> BuildRouteGoals(
            MazeGrid grid,
            Vector2Int entrance,
            CentralRoomInfo centralRoom,
            KeyPickupModel centralRoomKey,
            DungeonStairsModel downStairs,
            IReadOnlyList<CaveInfo> caves)
        {
            var goals = new List<RouteGoal>();
            AddRouteGoal(goals, "central-entry", entrance, centralRoom.EntranceExternalPosition, false);
            if (centralRoomKey != null)
            {
                AddRouteGoal(goals, "central-key", entrance, centralRoomKey.Position, false);
            }

            if (downStairs != null)
            {
                AddRouteGoal(goals, "down-stairs", centralRoom.ExitExternalPosition, downStairs.Position, true);
            }

            if (TryFindFarthestCaveGoal(grid, entrance, centralRoom, caves, false, out var firstHalfCave))
            {
                AddRouteGoal(goals, "first-cave", entrance, firstHalfCave, false);
            }

            if (TryFindFarthestCaveGoal(grid, centralRoom.ExitExternalPosition, centralRoom, caves, true, out var secondHalfCave))
            {
                AddRouteGoal(goals, "second-cave", centralRoom.ExitExternalPosition, secondHalfCave, true);
            }

            var farthestSecondHalf = FindFarthestSecondHalfGoal(grid, centralRoom);
            if (farthestSecondHalf != centralRoom.ExitExternalPosition)
            {
                AddRouteGoal(goals, "second-far", centralRoom.ExitExternalPosition, farthestSecondHalf, true);
            }

            return goals;
        }

        private static void AddRouteGoal(
            List<RouteGoal> goals,
            string name,
            Vector2Int start,
            Vector2Int target,
            bool includeClosedDoors)
        {
            if (start == target)
            {
                return;
            }

            foreach (var goal in goals)
            {
                if (goal.Start == start && goal.Target == target)
                {
                    return;
                }
            }

            goals.Add(new RouteGoal(name, start, target, includeClosedDoors));
        }

        private static bool TryFindFarthestCaveGoal(
            MazeGrid grid,
            Vector2Int start,
            CentralRoomInfo centralRoom,
            IReadOnlyList<CaveInfo> caves,
            bool secondHalf,
            out Vector2Int target)
        {
            target = default;
            if (caves == null || caves.Count == 0)
            {
                return false;
            }

            var distances = MazeValidation.GetReachableDistances(grid, start, true);
            var bestDistance = -1;
            for (var i = 0; i < caves.Count; i++)
            {
                var cave = caves[i];
                var inRequestedHalf = secondHalf
                    ? centralRoom.IsBeyondExitSide(cave.Center)
                    : cave.Center.x < centralRoom.Min.x;
                if (!inRequestedHalf || !distances.TryGetValue(cave.Center, out var distance))
                {
                    continue;
                }

                if (distance > bestDistance)
                {
                    bestDistance = distance;
                    target = cave.Center;
                }
            }

            return bestDistance >= 0;
        }

        private static Vector2Int FindFarthestSecondHalfGoal(MazeGrid grid, CentralRoomInfo centralRoom)
        {
            var distances = MazeValidation.GetReachableDistances(grid, centralRoom.ExitExternalPosition, true);
            var best = centralRoom.ExitExternalPosition;
            var bestDistance = -1;
            foreach (var pair in distances)
            {
                if (!centralRoom.IsBeyondExitSide(pair.Key) || centralRoom.Contains(pair.Key))
                {
                    continue;
                }

                if (pair.Value > bestDistance)
                {
                    best = pair.Key;
                    bestDistance = pair.Value;
                }
            }

            return best;
        }

        private static bool TryCarveAlternativeRouteConnection(
            MazeGrid grid,
            CentralRoomInfo centralRoom,
            RouteGoal goal,
            IReadOnlyList<Vector2Int> candidates,
            System.Random random,
            bool allowTwoCellConnector,
            out Vector2Int carvedPosition)
        {
            carvedPosition = default;
            if (!MazeValidation.TryFindRoutePath(grid, goal.Start, goal.Target, goal.IncludeClosedDoors, out var primaryPath))
            {
                return false;
            }

            var earlyStartIndex = MazeValidation.EarlyAlternativeProtectedCells;
            var earlyEndIndex = MazeValidation.GetEarlyAlternativeWindowEndIndex(primaryPath.Count);
            if (TryFindAlternativeConnectionCandidate(
                grid,
                centralRoom,
                goal,
                candidates,
                primaryPath,
                true,
                earlyStartIndex,
                earlyEndIndex,
                out carvedPosition)
                || (allowTwoCellConnector
                    && TryCreateEarlyRouteConnector(
                        grid,
                        centralRoom,
                        goal,
                        primaryPath,
                        random,
                        earlyStartIndex,
                        earlyEndIndex,
                        out carvedPosition))
                || TryFindAlternativeConnectionCandidate(
                    grid,
                    centralRoom,
                    goal,
                    candidates,
                    primaryPath,
                    false,
                    0,
                    primaryPath.Count - 1,
                    out carvedPosition))
            {
                if (grid.Get(carvedPosition).Type == MazeCellType.Wall)
                {
                    grid.SetType(carvedPosition, MazeCellType.Path);
                }

                return true;
            }

            return false;
        }

        private static bool TryFindAlternativeConnectionCandidate(
            MazeGrid grid,
            CentralRoomInfo centralRoom,
            RouteGoal goal,
            IReadOnlyList<Vector2Int> candidates,
            IReadOnlyList<Vector2Int> primaryPath,
            bool requireNearPath,
            int pathStartIndex,
            int pathEndIndex,
            out Vector2Int candidatePosition)
        {
            candidatePosition = default;
            foreach (var candidate in candidates)
            {
                if (grid.Get(candidate).Type != MazeCellType.Wall
                    || !IsCandidateInGoalSection(candidate, goal, centralRoom)
                    || !WouldConnectWalkableCorridors(grid, candidate, centralRoom)
                    || (requireNearPath && !IsNearPath(candidate, primaryPath, AlternativeRoutePathRadius, pathStartIndex, pathEndIndex)))
                {
                    continue;
                }

                candidatePosition = candidate;
                return true;
            }

            return false;
        }

        private static bool TryCreateEarlyRouteConnector(
            MazeGrid grid,
            CentralRoomInfo centralRoom,
            RouteGoal goal,
            IReadOnlyList<Vector2Int> primaryPath,
            System.Random random,
            int pathStartIndex,
            int pathEndIndex,
            out Vector2Int branchPosition)
        {
            branchPosition = default;
            if (pathEndIndex < pathStartIndex)
            {
                return false;
            }

            var origins = CollectPathWindow(primaryPath, pathStartIndex, pathEndIndex);
            Shuffle(origins, random);
            foreach (var origin in origins)
            {
                if (!IsCandidateInGoalSection(origin, goal, centralRoom))
                {
                    continue;
                }

                var directions = new List<Vector2Int>(MazeDirections.Cardinal);
                Shuffle(directions, random);
                foreach (var direction in directions)
                {
                    var side = origin + direction;
                    var end = side + direction;
                    if (!CanCreateTwoCellConnector(grid, centralRoom, goal, origin, side, end))
                    {
                        continue;
                    }

                    grid.SetType(side, MazeCellType.Path);
                    grid.SetType(end, MazeCellType.Path);
                    branchPosition = side;
                    return true;
                }
            }

            return false;
        }

        private static bool IsCandidateInGoalSection(
            Vector2Int candidate,
            RouteGoal goal,
            CentralRoomInfo centralRoom)
        {
            if (IsInFirstSection(goal.Target, centralRoom))
            {
                return IsInFirstSection(candidate, centralRoom);
            }

            if (IsInSecondSection(goal.Target, centralRoom))
            {
                return IsInSecondSection(candidate, centralRoom);
            }

            return IsInFirstSection(candidate, centralRoom) || IsInSecondSection(candidate, centralRoom);
        }

        private static bool IsNearPath(
            Vector2Int candidate,
            IReadOnlyList<Vector2Int> path,
            int maxDistance)
        {
            return IsNearPath(candidate, path, maxDistance, 0, path.Count - 1);
        }

        private static bool IsNearPath(
            Vector2Int candidate,
            IReadOnlyList<Vector2Int> path,
            int maxDistance,
            int startIndex,
            int endIndex)
        {
            var clampedStart = Mathf.Max(0, startIndex);
            var clampedEnd = Mathf.Min(path.Count - 1, endIndex);
            for (var i = clampedStart; i <= clampedEnd; i++)
            {
                if (GridDistance(candidate, path[i]) <= maxDistance)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool CanCreateTwoCellConnector(
            MazeGrid grid,
            CentralRoomInfo centralRoom,
            RouteGoal goal,
            Vector2Int origin,
            Vector2Int side,
            Vector2Int end)
        {
            if (!grid.InBounds(side)
                || !grid.InBounds(end)
                || IsOnEdge(grid, side)
                || IsOnEdge(grid, end)
                || grid.Get(side).Type != MazeCellType.Wall
                || grid.Get(end).Type != MazeCellType.Wall
                || !IsCandidateInGoalSection(side, goal, centralRoom)
                || !IsCandidateInGoalSection(end, goal, centralRoom)
                || !IsExtraConnectionPositionAllowed(side, centralRoom)
                || !IsExtraConnectionPositionAllowed(end, centralRoom))
            {
                return false;
            }

            foreach (var direction in MazeDirections.Cardinal)
            {
                var neighbor = end + direction;
                if (neighbor == side
                    || neighbor == origin
                    || !grid.InBounds(neighbor)
                    || !grid.Get(neighbor).IsWalkable
                    || !IsCandidateInGoalSection(neighbor, goal, centralRoom)
                    || !AreInSameMazeSection(origin, neighbor, centralRoom))
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        private static List<Vector2Int> CollectPathWindow(
            IReadOnlyList<Vector2Int> path,
            int startIndex,
            int endIndex)
        {
            var cells = new List<Vector2Int>();
            var clampedStart = Mathf.Max(0, startIndex);
            var clampedEnd = Mathf.Min(path.Count - 1, endIndex);
            for (var i = clampedStart; i <= clampedEnd; i++)
            {
                cells.Add(path[i]);
            }

            return cells;
        }

        private static int GridDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        private static void AppendCarvedDetail(ref string text, string goalName, Vector2Int position)
        {
            if (text.Length > 0)
            {
                text += "; ";
            }

            text += $"{goalName}:{GameDebugLog.Position(position)}";
        }

        private static bool IsRouteGoalSatisfied(MazeGrid grid, RouteGoal goal, out string details)
        {
            return MazeValidation.HasEarlyAlternativeRoute(
                grid,
                goal.Start,
                goal.Target,
                goal.IncludeClosedDoors,
                out details);
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

        private static bool IsOnEdge(MazeGrid grid, Vector2Int position)
        {
            return position.x == 0
                || position.y == 0
                || position.x == grid.Width - 1
                || position.y == grid.Height - 1;
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

        private readonly struct RouteGoal
        {
            public RouteGoal(string name, Vector2Int start, Vector2Int target, bool includeClosedDoors)
            {
                Name = name;
                Start = start;
                Target = target;
                IncludeClosedDoors = includeClosedDoors;
            }

            public string Name { get; }

            public Vector2Int Start { get; }

            public Vector2Int Target { get; }

            public bool IncludeClosedDoors { get; }
        }
    }
}
