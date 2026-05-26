using System.Collections.Generic;
using UnityEngine;

namespace Labyrinth.Maze
{
    public static class MazeValidation
    {
        private const int CentralRoomSize = 6;

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
