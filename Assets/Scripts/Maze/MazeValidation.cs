using System.Collections.Generic;
using UnityEngine;

namespace Labyrinth.Maze
{
    public static class MazeValidation
    {
        private const int CentralRoomSize = 6;
        public const int EarlyAlternativeProtectedCells = 2;
        private const float EarlyAlternativeWindowFraction = 0.25f;
        private const int EarlyAlternativeMinimumWindowEnd = 5;
        private const int EarlyAlternativeMaximumWindowEnd = 14;
        private const int EarlyAlternativeMinimumDetourSpan = 2;
        private const int EarlyAlternativeMaximumDetourSpan = 10;
        private const int EarlyAlternativeDetourSpanDivisor = 12;

        public static bool ValidateGeneratedMaze(MazeGenerationResult result, out string error)
        {
            if (result == null)
            {
                error = "Результат генерации отсутствует.";
                return false;
            }

            var grid = result.Grid;
            var entranceCount = 0;

            foreach (var cell in grid.Cells())
            {
                if (cell.Type == MazeCellType.Entrance)
                {
                    entranceCount++;
                }
            }

            if (entranceCount != 1)
            {
                error = "На карте должен быть ровно один вход.";
                return false;
            }

            if (!grid.InBounds(result.EntrancePosition) || !grid.Get(result.EntrancePosition).IsWalkable)
            {
                error = "Вход находится вне карты или заблокирован.";
                return false;
            }

            if (!IsOnEdge(grid, result.EntrancePosition))
            {
                error = "Вход должен находиться на внешней границе карты.";
                return false;
            }

            if (!ValidateCentralRoom(result, out error))
            {
                return false;
            }

            if (!ValidateCentralDoorsAndKey(result, out error))
            {
                return false;
            }

            if (!ValidateChests(result, out error))
            {
                return false;
            }

            var distances = GetReachableDistances(grid, result.EntrancePosition, true);
            if (!AllStructurallyPassableCellsReachable(grid, distances))
            {
                error = "Не все открываемые клетки лабиринта достижимы из входа.";
                return false;
            }

            if (!ValidateAlternativeRoutes(result, out error))
            {
                return false;
            }

            error = string.Empty;
            return true;
        }

        private static bool ValidateChests(MazeGenerationResult result, out string error)
        {
            if (result.Chests == null)
            {
                error = "Список сундуков отсутствует.";
                return false;
            }

            foreach (var chest in result.Chests)
            {
                if (chest == null)
                {
                    error = "Сундук не должен быть null.";
                    return false;
                }

                if (chest.Position != chest.Cave.Center
                    || !result.Grid.InBounds(chest.Position)
                    || !result.Grid.Get(chest.Position).IsWalkable)
                {
                    error = "Сундук должен стоять на центральной проходимой клетке обычной пещеры.";
                    return false;
                }

                if (result.CentralRoomKey != null && chest.Position == result.CentralRoomKey.Position)
                {
                    error = "Пещера с ключом не должна содержать сундук.";
                    return false;
                }

                if (chest.RewardType == ChestRewardType.Gold
                    && (chest.RewardGold < 10 || chest.RewardGold > 20))
                {
                    error = "Награда сундука должна быть в диапазоне 10-20 золота.";
                    return false;
                }

                if (chest.RewardType != ChestRewardType.Gold
                    && chest.RewardType != ChestRewardType.WeaponTier2
                    && chest.RewardType != ChestRewardType.ArmorTier2)
                {
                    error = "Тип награды сундука неизвестен.";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }

        private static bool ValidateCentralRoom(MazeGenerationResult result, out string error)
        {
            var grid = result.Grid;
            var room = result.CentralRoom;
            if (!room.IsValid || room.Width != CentralRoomSize || room.Height != CentralRoomSize)
            {
                error = "Центральная комната должна иметь размер 6 x 6.";
                return false;
            }

            if (!grid.InBounds(room.Min) || !grid.InBounds(room.Max))
            {
                error = "Центральная комната выходит за границы лабиринта.";
                return false;
            }

            if (!room.Contains(room.EntrancePosition)
                || !room.Contains(room.ExitPosition)
                || room.EntrancePosition.x != room.Min.x
                || room.ExitPosition.x != room.Max.x)
            {
                error = "Вход и выход центральной комнаты должны находиться на ее противоположных сторонах.";
                return false;
            }

            if (!grid.InBounds(room.EntranceExternalPosition)
                || !grid.InBounds(room.ExitExternalPosition)
                || !grid.Get(room.EntranceExternalPosition).IsWalkable
                || !grid.Get(room.ExitExternalPosition).IsWalkable)
            {
                error = "Внешние клетки входа и выхода центральной комнаты должны быть проходимыми.";
                return false;
            }

            for (var x = room.Min.x; x <= room.Max.x; x++)
            {
                for (var y = room.Min.y; y <= room.Max.y; y++)
                {
                    if (!grid.Get(x, y).IsStructurallyPassable)
                    {
                        error = "Все клетки центральной комнаты должны быть проходимыми после открытия дверей.";
                        return false;
                    }
                }
            }

            var contacts = CollectCentralRoomExternalContacts(grid, room);
            if (contacts.Count != 2
                || !ContainsPosition(contacts, room.EntranceExternalPosition)
                || !ContainsPosition(contacts, room.ExitExternalPosition))
            {
                error = $"У центральной комнаты должен быть ровно один вход и один выход, найдено контактов: {contacts.Count}.";
                return false;
            }

            if (!HasPathWithoutCentralRoom(grid, result.EntrancePosition, room.EntranceExternalPosition, room))
            {
                error = "До входа центральной комнаты должен существовать путь из входа лабиринта.";
                return false;
            }

            if (HasPathWithoutCentralRoom(grid, result.EntrancePosition, room.ExitExternalPosition, room))
            {
                error = "Вторая часть лабиринта должна быть доступна только через центральную комнату.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private static bool ValidateCentralDoorsAndKey(MazeGenerationResult result, out string error)
        {
            if (result.CentralDoors == null || result.CentralDoors.Count != 2)
            {
                error = "У центральной комнаты должны быть две двери.";
                return false;
            }

            if (result.Grid.Get(result.CentralRoom.EntrancePosition).Type != MazeCellType.ClosedDoor
                || result.Grid.Get(result.CentralRoom.ExitPosition).Type != MazeCellType.ClosedDoor)
            {
                error = "Двери центральной комнаты должны быть закрыты на старте.";
                return false;
            }

            if (result.CentralRoomKey == null
                || result.CentralRoomKey.IsCollected
                || !result.Grid.InBounds(result.CentralRoomKey.Position)
                || !result.Grid.Get(result.CentralRoomKey.Position).IsWalkable
                || result.CentralRoomKey.Position.x >= result.CentralRoom.Min.x)
            {
                error = "Ключ центральной комнаты должен лежать на проходимой клетке первой половины лабиринта.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        public static bool HasPath(MazeGrid grid, Vector2Int start, Vector2Int target)
        {
            var distances = GetReachableDistances(grid, start);
            return distances.ContainsKey(target);
        }

        public static bool HasAlternativeRoute(
            MazeGrid grid,
            Vector2Int start,
            Vector2Int target,
            bool includeClosedDoors)
        {
            if (!TryFindRoutePath(grid, start, target, includeClosedDoors, out var primaryPath))
            {
                return false;
            }

            const int protectedEndpointCells = 2;
            for (var i = protectedEndpointCells; i < primaryPath.Count - protectedEndpointCells; i++)
            {
                var blocked = primaryPath[i];
                if (TryFindRoutePath(grid, start, target, includeClosedDoors, blocked, out _))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool HasEarlyAlternativeRoute(
            MazeGrid grid,
            Vector2Int start,
            Vector2Int target,
            bool includeClosedDoors)
        {
            return HasEarlyAlternativeRoute(grid, start, target, includeClosedDoors, out _);
        }

        public static bool HasEarlyAlternativeRoute(
            MazeGrid grid,
            Vector2Int start,
            Vector2Int target,
            bool includeClosedDoors,
            out string details)
        {
            if (!TryFindRoutePath(grid, start, target, includeClosedDoors, out var primaryPath))
            {
                details = "primaryPath=missing";
                return false;
            }

            var earlyStartIndex = EarlyAlternativeProtectedCells;
            var earlyEndIndex = GetEarlyAlternativeWindowEndIndex(primaryPath.Count);
            var minimumDetourSpan = GetMinimumEarlyAlternativeDetourSpan(primaryPath.Count);
            if (earlyEndIndex < earlyStartIndex)
            {
                details = $"primary={primaryPath.Count}, earlyWindow=none, minDetourSpan={minimumDetourSpan}";
                return false;
            }

            for (var i = earlyStartIndex; i <= earlyEndIndex; i++)
            {
                var blocked = primaryPath[i];
                if (!TryFindRoutePath(grid, start, target, includeClosedDoors, blocked, out var alternativePath))
                {
                    continue;
                }

                var detourSpan = CalculatePrimaryDetourSpan(primaryPath, alternativePath, i);
                if (detourSpan < minimumDetourSpan)
                {
                    continue;
                }

                details = $"primary={primaryPath.Count}, earlyWindow={earlyStartIndex}-{earlyEndIndex}, splitIndex={i}, detourSpan={detourSpan}, alternative={alternativePath.Count}, minDetourSpan={minimumDetourSpan}";
                return true;
            }

            details = $"primary={primaryPath.Count}, earlyWindow={earlyStartIndex}-{earlyEndIndex}, minDetourSpan={minimumDetourSpan}";
            return false;
        }

        public static int GetEarlyAlternativeWindowEndIndex(int pathLength)
        {
            var lastAllowedIndex = pathLength - EarlyAlternativeProtectedCells - 1;
            if (lastAllowedIndex < EarlyAlternativeProtectedCells)
            {
                return lastAllowedIndex;
            }

            var fractionalEnd = Mathf.CeilToInt(pathLength * EarlyAlternativeWindowFraction);
            var clampedEnd = Mathf.Clamp(
                fractionalEnd,
                EarlyAlternativeMinimumWindowEnd,
                EarlyAlternativeMaximumWindowEnd);
            return Mathf.Min(clampedEnd, lastAllowedIndex);
        }

        public static int GetMinimumEarlyAlternativeDetourSpan(int pathLength)
        {
            return Mathf.Clamp(
                pathLength / EarlyAlternativeDetourSpanDivisor,
                EarlyAlternativeMinimumDetourSpan,
                EarlyAlternativeMaximumDetourSpan);
        }

        public static bool TryFindRoutePath(
            MazeGrid grid,
            Vector2Int start,
            Vector2Int target,
            bool includeClosedDoors,
            out List<Vector2Int> path)
        {
            return TryFindRoutePath(grid, start, target, includeClosedDoors, default, false, out path);
        }

        public static Dictionary<Vector2Int, int> GetReachableDistances(MazeGrid grid, Vector2Int start)
        {
            return GetReachableDistances(grid, start, false);
        }

        public static Dictionary<Vector2Int, int> GetReachableDistances(
            MazeGrid grid,
            Vector2Int start,
            bool includeClosedDoors)
        {
            var distances = new Dictionary<Vector2Int, int>();

            if (!grid.InBounds(start)
                || (includeClosedDoors ? !grid.Get(start).IsStructurallyPassable : !grid.Get(start).IsWalkable))
            {
                return distances;
            }

            var queue = new Queue<Vector2Int>();
            distances[start] = 0;
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var nextDistance = distances[current] + 1;

                foreach (var neighbor in grid.WalkableNeighbors(current, includeClosedDoors))
                {
                    if (distances.ContainsKey(neighbor))
                    {
                        continue;
                    }

                    distances[neighbor] = nextDistance;
                    queue.Enqueue(neighbor);
                }
            }

            return distances;
        }

        public static Vector2Int FindFarthestReachable(MazeGrid grid, Vector2Int start)
        {
            var distances = GetReachableDistances(grid, start);
            var bestPosition = start;
            var bestDistance = -1;

            foreach (var pair in distances)
            {
                if (pair.Key == start)
                {
                    continue;
                }

                if (pair.Value > bestDistance)
                {
                    bestDistance = pair.Value;
                    bestPosition = pair.Key;
                }
            }

            return bestPosition;
        }

        private static bool AllStructurallyPassableCellsReachable(MazeGrid grid, Dictionary<Vector2Int, int> distances)
        {
            foreach (var cell in grid.Cells())
            {
                if (!cell.IsStructurallyPassable)
                {
                    continue;
                }

                if (!distances.ContainsKey(new Vector2Int(cell.X, cell.Y)))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ValidateAlternativeRoutes(MazeGenerationResult result, out string error)
        {
            var grid = result.Grid;
            var room = result.CentralRoom;
            if (!HasEarlyAlternativeRoute(grid, result.EntrancePosition, room.EntranceExternalPosition, false, out var centralEntryDetails))
            {
                error = $"До входа центральной комнаты должна существовать ранняя развилка из области входа внутри первой половины лабиринта. {centralEntryDetails}.";
                return false;
            }

            if (result.CentralRoomKey != null
                && !HasEarlyAlternativeRoute(grid, result.EntrancePosition, result.CentralRoomKey.Position, false, out var keyDetails))
            {
                error = $"До ключа центральной комнаты должна существовать ранняя развилка из области входа. {keyDetails}.";
                return false;
            }

            if (result.DownStairs != null
                && !HasEarlyAlternativeRoute(grid, room.ExitExternalPosition, result.DownStairs.Position, true, out var downStairsDetails))
            {
                error = $"До спуска во второй половине должна существовать ранняя развилка от выхода центральной комнаты. {downStairsDetails}.";
                return false;
            }

            var farthestSecondHalf = FindFarthestSecondHalfRouteGoal(grid, room);
            if (farthestSecondHalf != room.ExitExternalPosition
                && !HasEarlyAlternativeRoute(grid, room.ExitExternalPosition, farthestSecondHalf, true, out var farthestDetails))
            {
                error = $"До дальней цели второй половины должна существовать ранняя развилка от выхода центральной комнаты. {farthestDetails}.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private static Vector2Int FindFarthestSecondHalfRouteGoal(MazeGrid grid, CentralRoomInfo room)
        {
            var distances = GetReachableDistances(grid, room.ExitExternalPosition, true);
            var best = room.ExitExternalPosition;
            var bestDistance = -1;
            foreach (var pair in distances)
            {
                if (!room.IsBeyondExitSide(pair.Key) || room.Contains(pair.Key))
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

        private static bool TryFindRoutePath(
            MazeGrid grid,
            Vector2Int start,
            Vector2Int target,
            bool includeClosedDoors,
            Vector2Int blocked,
            out List<Vector2Int> path)
        {
            return TryFindRoutePath(grid, start, target, includeClosedDoors, blocked, true, out path);
        }

        private static bool TryFindRoutePath(
            MazeGrid grid,
            Vector2Int start,
            Vector2Int target,
            bool includeClosedDoors,
            Vector2Int blocked,
            bool hasBlocked,
            out List<Vector2Int> path)
        {
            path = new List<Vector2Int>();
            if (!IsRouteCellPassable(grid, start, includeClosedDoors, blocked, hasBlocked)
                || !IsRouteCellPassable(grid, target, includeClosedDoors, blocked, hasBlocked))
            {
                return false;
            }

            var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            var queue = new Queue<Vector2Int>();
            cameFrom[start] = start;
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current == target)
                {
                    path = RestorePath(cameFrom, start, target);
                    return true;
                }

                foreach (var direction in MazeDirections.Cardinal)
                {
                    var next = current + direction;
                    if (cameFrom.ContainsKey(next)
                        || !IsRouteCellPassable(grid, next, includeClosedDoors, blocked, hasBlocked))
                    {
                        continue;
                    }

                    cameFrom[next] = current;
                    queue.Enqueue(next);
                }
            }

            return false;
        }

        private static int CalculatePrimaryDetourSpan(
            IReadOnlyList<Vector2Int> primaryPath,
            IReadOnlyList<Vector2Int> alternativePath,
            int fromPrimaryIndex)
        {
            var alternativeCells = new HashSet<Vector2Int>(alternativePath);
            var firstMissing = -1;
            var lastMissing = -1;
            var lastCheckedIndex = primaryPath.Count - EarlyAlternativeProtectedCells - 1;
            for (var i = fromPrimaryIndex; i <= lastCheckedIndex; i++)
            {
                if (alternativeCells.Contains(primaryPath[i]))
                {
                    continue;
                }

                if (firstMissing < 0)
                {
                    firstMissing = i;
                }

                lastMissing = i;
            }

            return firstMissing < 0 ? 0 : lastMissing - firstMissing + 1;
        }

        private static bool IsRouteCellPassable(
            MazeGrid grid,
            Vector2Int position,
            bool includeClosedDoors,
            Vector2Int blocked,
            bool hasBlocked)
        {
            if (!grid.InBounds(position) || (hasBlocked && position == blocked))
            {
                return false;
            }

            var cell = grid.Get(position);
            return includeClosedDoors ? cell.IsStructurallyPassable : cell.IsWalkable;
        }

        private static List<Vector2Int> RestorePath(
            IReadOnlyDictionary<Vector2Int, Vector2Int> cameFrom,
            Vector2Int start,
            Vector2Int target)
        {
            var path = new List<Vector2Int>();
            var current = target;
            path.Add(current);
            while (current != start)
            {
                current = cameFrom[current];
                path.Add(current);
            }

            path.Reverse();
            return path;
        }

        private static List<Vector2Int> CollectCentralRoomExternalContacts(MazeGrid grid, CentralRoomInfo room)
        {
            var contacts = new List<Vector2Int>();
            for (var x = room.Min.x; x <= room.Max.x; x++)
            {
                for (var y = room.Min.y; y <= room.Max.y; y++)
                {
                    var position = new Vector2Int(x, y);
                    foreach (var direction in MazeDirections.Cardinal)
                    {
                        var external = position + direction;
                        if (room.Contains(external)
                            || !grid.InBounds(external)
                            || !grid.Get(external).IsWalkable
                            || ContainsPosition(contacts, external))
                        {
                            continue;
                        }

                        contacts.Add(external);
                    }
                }
            }

            return contacts;
        }

        private static bool HasPathWithoutCentralRoom(
            MazeGrid grid,
            Vector2Int start,
            Vector2Int target,
            CentralRoomInfo room)
        {
            if (!grid.InBounds(start)
                || !grid.InBounds(target)
                || !grid.Get(start).IsWalkable
                || !grid.Get(target).IsWalkable
                || room.Contains(start)
                || room.Contains(target))
            {
                return false;
            }

            var visited = new HashSet<Vector2Int>();
            var queue = new Queue<Vector2Int>();
            visited.Add(start);
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current == target)
                {
                    return true;
                }

                foreach (var neighbor in grid.WalkableNeighbors(current))
                {
                    if (room.Contains(neighbor) || visited.Contains(neighbor))
                    {
                        continue;
                    }

                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }

            return false;
        }

        private static bool ContainsPosition(IReadOnlyList<Vector2Int> positions, Vector2Int target)
        {
            for (var i = 0; i < positions.Count; i++)
            {
                if (positions[i] == target)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsOnEdge(MazeGrid grid, Vector2Int position)
        {
            return position.x == 0
                || position.y == 0
                || position.x == grid.Width - 1
                || position.y == grid.Height - 1;
        }
    }
}
