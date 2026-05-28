using System.Collections.Generic;
using Labyrinth.Core;
using Labyrinth.Hero;
using UnityEngine;

namespace Labyrinth.Maze
{
    public sealed partial class MazeGenerator
    {
        private const int EmptyCellsBetweenCastleAndEntrance = 1;
        private const int CastleYOffsetFromEntrance = 3;
        private const int CentralRoomSize = 6;
        private const int CaveSize = 3;
        private const int MinimumCaveMapSize = 15;
        private const int CaveAreaPerRoom = 220;
        private const int MaximumCaveCount = 48;
        private const int MinimumCaveDistanceFromEntrance = 4;
        private const int MinimumCaveDistanceFromOtherCaves = 5;
        private const int MinimumChestGoldReward = 10;
        private const int MaximumChestGoldReward = 20;
        private const int GoldChestChancePercent = 70;
        private const int WeaponChestChancePercent = 15;
        private const int IronOreChancePercent = 70;
        private const int MinimumOreCells = 2;
        private const int MaximumOreCells = 4;

        public MazeGenerationResult Generate(MazeGenerationSettings settings, int levelNumber = 1)
        {
            var width = MazeGenerationSettings.NormalizeWidth(settings.Width);
            var height = MazeGenerationSettings.NormalizeHeight(settings.Height);
            var levelSeed = levelNumber <= 1 ? settings.Seed : settings.Seed ^ (levelNumber * 104729);
            var normalizedSettings = new MazeGenerationSettings(width, height, levelSeed, settings.Preset);
            var random = new System.Random(levelSeed);
            var grid = new MazeGrid(width, height);
            var entranceY = MakeOdd(height / 2);
            var entrance = new Vector2Int(0, entranceY);
            var start = new Vector2Int(1, entranceY);
            var baseX = -(BaseDevelopment.CastleFootprintRadiusCells + EmptyCellsBetweenCastleAndEntrance + 1);
            var basePosition = new Vector2Int(baseX, entranceY + CastleYOffsetFromEntrance);
            var centralRoom = BuildCentralRoom(width, height);
            var secondSectionStart = FindSecondSectionStart(centralRoom, width);

            CarveMaze(grid, start, random, position => IsInFirstSection(position, centralRoom));
            CarveMaze(grid, secondSectionStart, random, position => IsInSecondSection(position, centralRoom));
            OpenCentralRoom(grid, centralRoom);
            grid.SetType(entrance, MazeCellType.Entrance);
            MazeBranchCarver.AddExtraConnections(grid, entrance, centralRoom, random);
            var caves = PlaceCaves(grid, entrance, centralRoom, random);
            EnsureSecondHalfStairsCave(grid, entrance, centralRoom, caves);
            var centralRoomKey = PlaceCentralRoomKey(grid, entrance, centralRoom, caves);
            var downStairs = PlaceDownStairs(grid, entrance, centralRoom, caves, levelNumber + 1);
            MazeBranchCarver.EnsureAlternativeRoutes(grid, entrance, centralRoom, centralRoomKey, downStairs, caves, random);
            var centralDoors = CreateCentralDoors(grid, centralRoom);
            var chests = CreateChests(caves, centralRoomKey, downStairs.Position, random);
            var oreDeposits = CreateOreDeposits(grid, caves, entrance, centralRoomKey, downStairs.Position, random);
            var upStairs = levelNumber > 1
                ? new DungeonStairsModel(entrance, DungeonStairsDirection.Up, levelNumber - 1, true)
                : null;

            GameDebugLog.Info(
                "Maze",
                $"Level {levelNumber} central room: min={GameDebugLog.Position(centralRoom.Min)}, max={GameDebugLog.Position(centralRoom.Max)}, entranceDoor={GameDebugLog.Position(centralRoom.EntrancePosition)} via {GameDebugLog.Position(centralRoom.EntranceExternalPosition)}, exitDoor={GameDebugLog.Position(centralRoom.ExitPosition)} via {GameDebugLog.Position(centralRoom.ExitExternalPosition)}, key={GameDebugLog.Position(centralRoomKey.Position)}, downStairs={GameDebugLog.Position(downStairs.Position)}, chests={chests.Count}, oreDeposits={oreDeposits.Count}");
            return new MazeGenerationResult(grid, normalizedSettings, basePosition, entrance, centralRoom, centralDoors, centralRoomKey, chests, caves, oreDeposits, downStairs, upStairs, levelNumber);
        }

        private static void CarveMaze(
            MazeGrid grid,
            Vector2Int start,
            System.Random random,
            System.Predicate<Vector2Int> isAllowed)
        {
            grid.SetType(start, MazeCellType.Path);

            var stack = new Stack<Vector2Int>();
            stack.Push(start);

            while (stack.Count > 0)
            {
                var current = stack.Peek();
                var availableDirections = GetAvailableDirections(grid, current, random, isAllowed);

                if (availableDirections.Count == 0)
                {
                    stack.Pop();
                    continue;
                }

                var direction = availableDirections[0];
                var between = current + new Vector2Int(direction.x / 2, direction.y / 2);
                var next = current + direction;

                grid.SetType(between, MazeCellType.Path);
                grid.SetType(next, MazeCellType.Path);
                stack.Push(next);
            }
        }

        private static List<Vector2Int> GetAvailableDirections(
            MazeGrid grid,
            Vector2Int current,
            System.Random random,
            System.Predicate<Vector2Int> isAllowed)
        {
            var directions = new List<Vector2Int>(MazeDirections.CarveSteps);

            for (var i = directions.Count - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                var temp = directions[i];
                directions[i] = directions[j];
                directions[j] = temp;
            }

            for (var i = directions.Count - 1; i >= 0; i--)
            {
                if (!IsCarvable(grid, current, directions[i], isAllowed))
                {
                    directions.RemoveAt(i);
                }
            }

            return directions;
        }

        private static bool IsCarvable(
            MazeGrid grid,
            Vector2Int current,
            Vector2Int direction,
            System.Predicate<Vector2Int> isAllowed)
        {
            var between = current + new Vector2Int(direction.x / 2, direction.y / 2);
            var next = current + direction;
            if (next.x <= 0 || next.y <= 0 || next.x >= grid.Width - 1 || next.y >= grid.Height - 1)
            {
                return false;
            }

            return isAllowed(between)
                && isAllowed(next)
                && grid.Get(next).Type == MazeCellType.Wall;
        }

        private static CentralRoomInfo BuildCentralRoom(int width, int height)
        {
            var minX = Mathf.Clamp(
                width / 2 - CentralRoomSize / 2 + 1,
                2,
                width - CentralRoomSize - 3);
            var minY = Mathf.Clamp(
                height / 2 - CentralRoomSize / 2 + 1,
                2,
                height - CentralRoomSize - 2);
            var max = new Vector2Int(minX + CentralRoomSize - 1, minY + CentralRoomSize - 1);
            var doorY = MakeOdd((minY + max.y) / 2);
            if (doorY < minY)
            {
                doorY = minY + 1;
            }
            else if (doorY > max.y)
            {
                doorY = max.y - 1;
            }

            var min = new Vector2Int(minX, minY);
            var entrancePosition = new Vector2Int(min.x, doorY);
            var entranceExternal = new Vector2Int(min.x - 1, doorY);
            var exitPosition = new Vector2Int(max.x, doorY);
            var exitExternal = new Vector2Int(max.x + 1, doorY);
            return new CentralRoomInfo(min, max, entrancePosition, entranceExternal, exitPosition, exitExternal);
        }

        private static Vector2Int FindSecondSectionStart(CentralRoomInfo centralRoom, int width)
        {
            var x = centralRoom.ExitExternalPosition.x;
            if (x % 2 == 0)
            {
                x++;
            }

            if (x >= width - 1)
            {
                x = width - 2;
                if (x % 2 == 0)
                {
                    x--;
                }
            }

            return new Vector2Int(x, centralRoom.ExitExternalPosition.y);
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

        private static void OpenCentralRoom(MazeGrid grid, CentralRoomInfo centralRoom)
        {
            for (var x = centralRoom.Min.x; x <= centralRoom.Max.x; x++)
            {
                for (var y = centralRoom.Min.y; y <= centralRoom.Max.y; y++)
                {
                    grid.SetType(x, y, MazeCellType.Path);
                }
            }

            grid.SetType(centralRoom.EntranceExternalPosition, MazeCellType.Path);
            grid.SetType(centralRoom.EntrancePosition, MazeCellType.Path);
            grid.SetType(centralRoom.ExitPosition, MazeCellType.Path);
            grid.SetType(centralRoom.ExitExternalPosition, MazeCellType.Path);
        }

        private static List<CentralDoorModel> CreateCentralDoors(MazeGrid grid, CentralRoomInfo centralRoom)
        {
            grid.SetType(centralRoom.EntrancePosition, MazeCellType.ClosedDoor);
            grid.SetType(centralRoom.ExitPosition, MazeCellType.ClosedDoor);

            return new List<CentralDoorModel>
            {
                new CentralDoorModel("Входная дверь", centralRoom.EntrancePosition, centralRoom.EntranceExternalPosition),
                new CentralDoorModel("Выходная дверь", centralRoom.ExitPosition, centralRoom.ExitExternalPosition)
            };
        }

        private static KeyPickupModel PlaceCentralRoomKey(
            MazeGrid grid,
            Vector2Int entrance,
            CentralRoomInfo centralRoom,
            IReadOnlyList<CaveInfo> caves)
        {
            var distances = MazeValidation.GetReachableDistances(grid, entrance);
            var bestCaveDistance = -1;
            var bestCavePosition = default(Vector2Int);

            foreach (var cave in caves)
            {
                if (cave.Center.x >= centralRoom.Min.x || !distances.TryGetValue(cave.Center, out var distance))
                {
                    continue;
                }

                if (distance > bestCaveDistance)
                {
                    bestCaveDistance = distance;
                    bestCavePosition = cave.Center;
                }
            }

            if (bestCaveDistance >= 0)
            {
                return new KeyPickupModel(bestCavePosition, HeroInventory.CentralRoomKeyItemName);
            }

            var bestFallbackDistance = -1;
            var fallback = entrance;
            foreach (var cell in grid.Cells())
            {
                var position = new Vector2Int(cell.X, cell.Y);
                if (position.x >= centralRoom.Min.x
                    || cell.Type == MazeCellType.Entrance
                    || !cell.IsWalkable
                    || GridDistance(position, entrance) <= MinimumCaveDistanceFromEntrance
                    || !distances.TryGetValue(position, out var distance))
                {
                    continue;
                }

                if (distance > bestFallbackDistance)
                {
                    bestFallbackDistance = distance;
                    fallback = position;
                }
            }

            GameDebugLog.Warning("Maze", $"Central room key fallback used at {GameDebugLog.Position(fallback)} because no first-half cave was available.");
            return new KeyPickupModel(fallback, HeroInventory.CentralRoomKeyItemName);
        }

        private static List<ChestModel> CreateChests(
            IReadOnlyList<CaveInfo> caves,
            KeyPickupModel centralRoomKey,
            Vector2Int stairsPosition,
            System.Random random)
        {
            var chests = new List<ChestModel>();
            foreach (var cave in caves)
            {
                if ((centralRoomKey != null && cave.Center == centralRoomKey.Position)
                    || cave.Center == stairsPosition)
                {
                    continue;
                }

                chests.Add(CreateChest(cave, random));
            }

            return chests;
        }

        private static ChestModel CreateChest(CaveInfo cave, System.Random random)
        {
            var roll = random.Next(100);
            if (roll < GoldChestChancePercent)
            {
                var rewardGold = random.Next(MinimumChestGoldReward, MaximumChestGoldReward + 1);
                return new ChestModel(cave, ChestRewardType.Gold, rewardGold);
            }

            if (roll < GoldChestChancePercent + WeaponChestChancePercent)
            {
                return new ChestModel(cave, ChestRewardType.WeaponTier2, 0);
            }

            return new ChestModel(cave, ChestRewardType.ArmorTier2, 0);
        }

        private static List<OreDepositModel> CreateOreDeposits(
            MazeGrid grid,
            IReadOnlyList<CaveInfo> caves,
            Vector2Int entrance,
            KeyPickupModel centralRoomKey,
            Vector2Int stairsPosition,
            System.Random random)
        {
            var deposits = new List<OreDepositModel>();
            var skipped = 0;
            var ironCount = 0;
            var goldCount = 0;

            foreach (var cave in caves)
            {
                var cells = CollectOreCells(grid, cave, centralRoomKey, stairsPosition);
                if (cells.Count < MinimumOreCells)
                {
                    skipped++;
                    GameDebugLog.Warning("Maze", $"Ore deposit skipped in cave {GameDebugLog.Position(cave.Center)}: not enough free cells.");
                    continue;
                }

                Shuffle(cells, random);
                var cellCount = Mathf.Min(cells.Count, random.Next(MinimumOreCells, MaximumOreCells + 1));
                var selectedCells = new List<Vector2Int>(cellCount);
                for (var i = 0; i < cellCount; i++)
                {
                    selectedCells.Add(cells[i]);
                }

                var type = random.Next(100) < IronOreChancePercent
                    ? OreDepositType.Iron
                    : OreDepositType.Gold;
                if (type == OreDepositType.Iron)
                {
                    ironCount++;
                }
                else
                {
                    goldCount++;
                }

                deposits.Add(new OreDepositModel(type, cave, selectedCells));
            }

            var protectedIronIndex = EnsureNearestEntranceOreIsIron(grid, entrance, deposits, ref ironCount, ref goldCount);
            EnsureOreDepositDiversity(deposits, protectedIronIndex, ref ironCount, ref goldCount);
            GameDebugLog.Info(
                "Maze",
                $"Ore deposits placed: total={deposits.Count}, iron={ironCount}, gold={goldCount}, skipped={skipped}.");
            return deposits;
        }

        private static int EnsureNearestEntranceOreIsIron(
            MazeGrid grid,
            Vector2Int entrance,
            List<OreDepositModel> deposits,
            ref int ironCount,
            ref int goldCount)
        {
            var index = FindNearestEntranceOreDepositIndex(grid, entrance, deposits);
            if (index < 0)
            {
                return -1;
            }

            var deposit = deposits[index];
            if (deposit.Type == OreDepositType.Iron)
            {
                GameDebugLog.Info("Maze", $"Nearest entrance ore cave is already iron at {GameDebugLog.Position(deposit.Cave.Center)}.");
                return index;
            }

            deposits[index] = CopyDepositWithType(deposit, OreDepositType.Iron);
            ironCount++;
            goldCount--;
            GameDebugLog.Info("Maze", $"Nearest entrance ore cave forced to iron at {GameDebugLog.Position(deposit.Cave.Center)}.");
            return index;
        }

        private static int FindNearestEntranceOreDepositIndex(
            MazeGrid grid,
            Vector2Int entrance,
            IReadOnlyList<OreDepositModel> deposits)
        {
            if (deposits == null || deposits.Count == 0)
            {
                return -1;
            }

            var distances = MazeValidation.GetReachableDistances(grid, entrance);
            var index = FindNearestOreDepositIndexByDistances(deposits, distances);
            if (index >= 0)
            {
                return index;
            }

            distances = MazeValidation.GetReachableDistances(grid, entrance, true);
            index = FindNearestOreDepositIndexByDistances(deposits, distances);
            return index >= 0 ? index : FindNearestOreDepositIndexByGridDistance(deposits, entrance);
        }

        private static int FindNearestOreDepositIndexByDistances(
            IReadOnlyList<OreDepositModel> deposits,
            IReadOnlyDictionary<Vector2Int, int> distances)
        {
            var bestIndex = -1;
            var bestDistance = int.MaxValue;
            for (var i = 0; i < deposits.Count; i++)
            {
                if (deposits[i] == null || !distances.TryGetValue(deposits[i].Cave.Center, out var distance))
                {
                    continue;
                }

                if (distance < bestDistance
                    || (distance == bestDistance && IsEarlierPosition(deposits[i].Cave.Center, deposits[bestIndex].Cave.Center)))
                {
                    bestIndex = i;
                    bestDistance = distance;
                }
            }

            return bestIndex;
        }

        private static int FindNearestOreDepositIndexByGridDistance(
            IReadOnlyList<OreDepositModel> deposits,
            Vector2Int entrance)
        {
            var bestIndex = -1;
            var bestDistance = int.MaxValue;
            for (var i = 0; i < deposits.Count; i++)
            {
                if (deposits[i] == null)
                {
                    continue;
                }

                var distance = GridDistance(deposits[i].Cave.Center, entrance);
                if (distance < bestDistance
                    || (distance == bestDistance && IsEarlierPosition(deposits[i].Cave.Center, deposits[bestIndex].Cave.Center)))
                {
                    bestIndex = i;
                    bestDistance = distance;
                }
            }

            return bestIndex;
        }

        private static void EnsureOreDepositDiversity(
            List<OreDepositModel> deposits,
            int protectedIronIndex,
            ref int ironCount,
            ref int goldCount)
        {
            if (deposits.Count < 2)
            {
                GameDebugLog.Warning("Maze", $"Ore diversity limited: deposits={deposits.Count}, need at least 2 deposits to guarantee both iron and gold caves.");
                return;
            }

            if (ironCount > 0 && goldCount > 0)
            {
                return;
            }

            if (ironCount <= 0)
            {
                var index = FindDepositToRetype(deposits, OreDepositType.Gold, -1);
                if (index >= 0)
                {
                    deposits[index] = CopyDepositWithType(deposits[index], OreDepositType.Iron);
                    ironCount++;
                    goldCount--;
                    GameDebugLog.Info("Maze", $"Ore diversity forced iron cave at {GameDebugLog.Position(deposits[index].Cave.Center)}.");
                }
            }

            if (goldCount <= 0)
            {
                var index = FindDepositToRetype(deposits, OreDepositType.Iron, protectedIronIndex);
                if (index >= 0)
                {
                    deposits[index] = CopyDepositWithType(deposits[index], OreDepositType.Gold);
                    goldCount++;
                    ironCount--;
                    GameDebugLog.Info("Maze", $"Ore diversity forced gold cave at {GameDebugLog.Position(deposits[index].Cave.Center)}.");
                }
            }
        }

        private static int FindDepositToRetype(
            IReadOnlyList<OreDepositModel> deposits,
            OreDepositType currentType,
            int excludedIndex)
        {
            for (var i = deposits.Count - 1; i >= 0; i--)
            {
                if (i != excludedIndex && deposits[i] != null && deposits[i].Type == currentType)
                {
                    return i;
                }
            }

            return -1;
        }

        private static OreDepositModel CopyDepositWithType(OreDepositModel source, OreDepositType type)
        {
            return new OreDepositModel(type, source.Cave, source.Cells);
        }

        private static List<Vector2Int> CollectOreCells(
            MazeGrid grid,
            CaveInfo cave,
            KeyPickupModel centralRoomKey,
            Vector2Int stairsPosition)
        {
            var cells = new List<Vector2Int>();
            var radius = CaveSize / 2;
            for (var x = cave.Center.x - radius; x <= cave.Center.x + radius; x++)
            {
                for (var y = cave.Center.y - radius; y <= cave.Center.y + radius; y++)
                {
                    var position = new Vector2Int(x, y);
                    if (position == cave.Center
                        || position == cave.EntrancePosition
                        || position == stairsPosition
                        || (centralRoomKey != null && position == centralRoomKey.Position)
                        || !grid.InBounds(position)
                        || !grid.Get(position).IsWalkable)
                    {
                        continue;
                    }

                    cells.Add(position);
                }
            }

            return cells;
        }

        private static DungeonStairsModel PlaceDownStairs(
            MazeGrid grid,
            Vector2Int entrance,
            CentralRoomInfo centralRoom,
            IReadOnlyList<CaveInfo> caves,
            int targetLevel)
        {
            var distances = MazeValidation.GetReachableDistances(grid, entrance, true);
            var bestDistance = -1;
            var stairsPosition = default(Vector2Int);
            foreach (var cave in caves)
            {
                if (!centralRoom.IsBeyondExitSide(cave.Center)
                    || !distances.TryGetValue(cave.Center, out var distance))
                {
                    continue;
                }

                if (distance > bestDistance)
                {
                    bestDistance = distance;
                    stairsPosition = cave.Center;
                }
            }

            if (bestDistance < 0)
            {
                stairsPosition = FindFallbackSecondHalfStairsPosition(grid, entrance, centralRoom);
                GameDebugLog.Warning("Maze", $"Down stairs fallback used at {GameDebugLog.Position(stairsPosition)} because no second-half cave was available.");
            }

            grid.SetType(stairsPosition, MazeCellType.LockedDownStairs);
            return new DungeonStairsModel(stairsPosition, DungeonStairsDirection.Down, targetLevel, false);
        }

        private static Vector2Int FindFallbackSecondHalfStairsPosition(
            MazeGrid grid,
            Vector2Int entrance,
            CentralRoomInfo centralRoom)
        {
            var distances = MazeValidation.GetReachableDistances(grid, entrance, true);
            var bestDistance = -1;
            var best = centralRoom.ExitExternalPosition;
            foreach (var cell in grid.Cells())
            {
                var position = new Vector2Int(cell.X, cell.Y);
                if (!cell.IsStructurallyPassable
                    || centralRoom.Contains(position)
                    || !centralRoom.IsBeyondExitSide(position)
                    || !distances.TryGetValue(position, out var distance))
                {
                    continue;
                }

                if (distance > bestDistance)
                {
                    bestDistance = distance;
                    best = position;
                }
            }

            return best;
        }

        private static void EnsureSecondHalfStairsCave(
            MazeGrid grid,
            Vector2Int entrance,
            CentralRoomInfo centralRoom,
            List<CaveInfo> caves)
        {
            foreach (var cave in caves)
            {
                if (centralRoom.IsBeyondExitSide(cave.Center))
                {
                    return;
                }
            }

            var center = FindFallbackSecondHalfCaveCenter(grid, entrance, centralRoom);
            if (center == default)
            {
                return;
            }

            var radius = CaveSize / 2;
            for (var x = center.x - radius; x <= center.x + radius; x++)
            {
                for (var y = center.y - radius; y <= center.y + radius; y++)
                {
                    grid.SetType(x, y, MazeCellType.Path);
                }
            }

            caves.Add(new CaveInfo(center, center));
            GameDebugLog.Warning("Maze", $"Second-half stairs cave fallback carved at {GameDebugLog.Position(center)}.");
        }

        private static Vector2Int FindFallbackSecondHalfCaveCenter(
            MazeGrid grid,
            Vector2Int entrance,
            CentralRoomInfo centralRoom)
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

        private static List<CaveInfo> PlaceCaves(
            MazeGrid grid,
            Vector2Int entrance,
            CentralRoomInfo centralRoom,
            System.Random random)
        {
            var caves = new List<CaveInfo>();
            var caveCount = CalculateCaveCount(grid.Width, grid.Height);
            if (caveCount <= 0)
            {
                return caves;
            }

            var candidates = CollectCaveCandidates(grid);
            Shuffle(candidates, random);

            var rejectedNearEntrance = 0;
            var rejectedSpacing = 0;
            var rejectedNoContact = 0;
            var rejectedDisconnected = 0;
            var rejectedCentralPassage = 0;

            foreach (var center in candidates)
            {
                var status = TryPlaceCave(grid, center, entrance, centralRoom, caves, out var cave);
                if (status != CavePlacementStatus.Placed)
                {
                    switch (status)
                    {
                        case CavePlacementStatus.TooCloseToEntrance:
                            rejectedNearEntrance++;
                            break;
                        case CavePlacementStatus.TooCloseToOtherCave:
                            rejectedSpacing++;
                            break;
                        case CavePlacementStatus.NoExternalContact:
                            rejectedNoContact++;
                            break;
                        case CavePlacementStatus.DisconnectsMaze:
                            rejectedDisconnected++;
                            break;
                        case CavePlacementStatus.BlocksCentralPassage:
                            rejectedCentralPassage++;
                            break;
                    }

                    continue;
                }

                caves.Add(cave);

                if (caves.Count >= caveCount)
                {
                    break;
                }
            }

            GameDebugLog.Info(
                "Maze",
                $"Cave placement: desired={caveCount}, candidates={candidates.Count}, placed={caves.Count}, rejectedNearEntrance={rejectedNearEntrance}, rejectedSpacing={rejectedSpacing}, rejectedNoContact={rejectedNoContact}, rejectedDisconnected={rejectedDisconnected}, rejectedCentralPassage={rejectedCentralPassage}");
            return caves;
        }

        private static int CalculateCaveCount(int width, int height)
        {
            if (Mathf.Min(width, height) < MinimumCaveMapSize)
            {
                return 0;
            }

            var desiredCount = Mathf.RoundToInt(width * height / (float)CaveAreaPerRoom);
            return Mathf.Clamp(desiredCount, 1, MaximumCaveCount);
        }

        private static List<Vector2Int> CollectCaveCandidates(MazeGrid grid)
        {
            var candidates = new List<Vector2Int>();
            for (var x = CaveSize; x <= grid.Width - CaveSize - 1; x++)
            {
                for (var y = CaveSize; y <= grid.Height - CaveSize - 1; y++)
                {
                    candidates.Add(new Vector2Int(x, y));
                }
            }

            return candidates;
        }

        private static CavePlacementStatus TryPlaceCave(
            MazeGrid grid,
            Vector2Int center,
            Vector2Int entrance,
            CentralRoomInfo centralRoom,
            IReadOnlyList<CaveInfo> caves,
            out CaveInfo cave)
        {
            cave = default;
            if (GridDistance(center, entrance) <= MinimumCaveDistanceFromEntrance)
            {
                return CavePlacementStatus.TooCloseToEntrance;
            }

            foreach (var existingCave in caves)
            {
                if (GridDistance(center, existingCave.Center) < MinimumCaveDistanceFromOtherCaves)
                {
                    return CavePlacementStatus.TooCloseToOtherCave;
                }
            }

            if (IsCaveBlockedByCentralPassage(center, centralRoom))
            {
                return CavePlacementStatus.BlocksCentralPassage;
            }

            var contacts = CollectExternalPathContacts(grid, center);
            if (contacts.Count == 0)
            {
                return CavePlacementStatus.NoExternalContact;
            }

            var selectedContacts = SelectCaveEntranceContacts(contacts, entrance);
            var snapshots = new List<CellSnapshot>();
            ApplyCaveCandidate(grid, center, contacts, selectedContacts, snapshots);

            if (!AllWalkableCellsReachable(grid, entrance))
            {
                RestoreSnapshots(grid, snapshots);
                return CavePlacementStatus.DisconnectsMaze;
            }

            cave = new CaveInfo(center, selectedContacts[0].EntrancePosition);
            return CavePlacementStatus.Placed;
        }

    }
}
