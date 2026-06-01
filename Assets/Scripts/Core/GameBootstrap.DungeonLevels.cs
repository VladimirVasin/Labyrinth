using System.Collections.Generic;
using Labyrinth.Base;
using Labyrinth.Hero;
using Labyrinth.Maze;
using UnityEngine;

namespace Labyrinth.Core
{
    public sealed partial class GameBootstrap
    {
        private HeroMemory levelOneCartographerMemory;
        private HeroMemory levelTwoCartographerMemory;
        private Vector2Int? levelTwoEntryAnchor;

        private void HandleDownStairsOpened(HeroModel heroModel, int heroNumber, DungeonStairsModel stairs)
        {
            if (stairs == null || stairs.Direction != DungeonStairsDirection.Down)
            {
                return;
            }

            if (stairs.TargetLevel > 2)
            {
                victoryHud.Show("Спуск открыт. Следующий уровень зарезервирован для следующей итерации MVP.");
                GameDebugLog.Info("Dungeon", $"Hero #{heroNumber} opened stairs to unsupported MVP level {stairs.TargetLevel}.");
                return;
            }

            levelTwoEntryAnchor = stairs.Position;
            EnsureDungeonLevel(stairs.TargetLevel);
            unlockedDungeonLevel = Mathf.Max(unlockedDungeonLevel, stairs.TargetLevel);
            RefreshHeroHouseEffect(heroNumber);
            victoryHud.Show($"Спуск открыт. Доступен уровень {stairs.TargetLevel}.");
            GameAudioController.Play(GameSfx.LevelSwitch, mazeRenderer.GridToWorld(stairs.Position), 0.9f);
            GameDebugLog.Info(
                "Dungeon",
                $"Hero #{heroNumber} opened stairs from level {currentDungeonLevel} to level {stairs.TargetLevel} at {GameDebugLog.Position(stairs.Position)}.");
        }

        private void SwitchDungeonLevel(int levelNumber)
        {
            if (levelNumber == currentDungeonLevel || levelNumber < 1 || levelNumber > unlockedDungeonLevel)
            {
                return;
            }

            var target = EnsureDungeonLevel(levelNumber);
            if (target == null)
            {
                return;
            }

            baseHud.Hide();
            heroHud.Hide();
            mobHud.Hide();
            buildingMicroHud.Hide();
            objectMicroHud.Hide();
            combatController.CancelCombat();
            ClearFallenHeroes();
            RenderDungeonLevel(target, levelNumber == 1);
            TransferHeroesToCurrentLevel();
            cameraController.Focus(mainCamera, currentMaze, mazeRenderer.CellSize);
            RefreshSelectedHeroVisibility();
            GameAudioController.Play(GameSfx.LevelSwitch, mazeRenderer.GridToWorld(currentMaze.EntrancePosition), 0.75f);
            GameDebugLog.Info("Dungeon", $"Switched active dungeon view to level {currentDungeonLevel}.");
        }

        private MazeGenerationResult EnsureDungeonLevel(int levelNumber)
        {
            if (levelNumber <= 1)
            {
                return levelOneMaze;
            }

            if (levelNumber > 2)
            {
                GameDebugLog.Warning("Dungeon", $"Level {levelNumber} requested, but MVP supports levels 1-2.");
                return null;
            }

            if (levelTwoMaze != null)
            {
                return levelTwoMaze;
            }

            if (rootGenerationSettings == null)
            {
                return null;
            }

            var settings = new MazeGenerationSettings(
                rootGenerationSettings.Width,
                rootGenerationSettings.Height,
                rootGenerationSettings.Seed,
                rootGenerationSettings.Preset);
            var generated = generator.Generate(settings, 2);
            if (levelTwoEntryAnchor.HasValue)
            {
                generated = AnchorLowerLevelUpStairs(generated, levelTwoEntryAnchor.Value);
            }

            if (!MazeValidation.ValidateGeneratedMaze(generated, out var error))
            {
                GameDebugLog.Error("Maze", $"Lower level generation failed: {error}");
                return null;
            }

            levelTwoMaze = generated;
            levelTwoCartographerMemory = new HeroMemory(levelTwoMaze.Grid);
            levelTwoCartographerMemory.Remember(levelTwoMaze.UpStairs != null ? levelTwoMaze.UpStairs.Position : levelTwoMaze.EntrancePosition);
            GameDebugLog.Info(
                "Dungeon",
                $"Generated lower dungeon level 2: size={levelTwoMaze.Grid.Width}x{levelTwoMaze.Grid.Height}, seed={levelTwoMaze.Settings.Seed}, up={GameDebugLog.Position(levelTwoMaze.UpStairs?.Position ?? levelTwoMaze.EntrancePosition)}, anchor={GameDebugLog.Position(levelTwoEntryAnchor ?? Vector2Int.zero)}, down={GameDebugLog.Position(levelTwoMaze.DownStairs.Position)}.");
            return levelTwoMaze;
        }

        private MazeGenerationResult AnchorLowerLevelUpStairs(MazeGenerationResult generated, Vector2Int anchor)
        {
            if (generated == null || generated.Grid == null)
            {
                return generated;
            }

            if (!generated.Grid.InBounds(anchor))
            {
                GameDebugLog.Warning("Dungeon", $"Lower level up stairs anchor {GameDebugLog.Position(anchor)} is out of bounds; using generated entrance.");
                return generated;
            }

            ConnectLowerLevelAnchor(generated, anchor);
            generated.Grid.SetType(anchor, MazeCellType.UpStairs);
            var downStairs = generated.DownStairs;
            if (downStairs != null && downStairs.Position == anchor)
            {
                downStairs = RepositionLowerLevelDownStairs(generated, anchor);
            }

            var bossCave = generated.BossCave;
            if (ContainsCaveCell(bossCave, anchor))
            {
                var replacement = FindReplacementBossCave(generated, anchor, downStairs?.Position ?? anchor);
                if (replacement.IsValid)
                {
                    GameDebugLog.Warning("Dungeon", $"Lower level boss cave moved from up-stairs anchor cave {GameDebugLog.Position(bossCave.Center)} to {GameDebugLog.Position(replacement.Center)}.");
                    bossCave = replacement;
                }
                else
                {
                    GameDebugLog.Warning("Dungeon", $"Lower level up stairs anchor overlaps boss cave at {GameDebugLog.Position(bossCave.Center)} and no replacement boss cave was found.");
                }
            }

            var chests = FilterChestsAtPosition(FilterChestsAtPosition(generated.Chests, anchor), downStairs?.Position ?? anchor);
            var ores = FilterOreDepositsAtPosition(FilterOreDepositsAtPosition(generated.OreDeposits, anchor), downStairs?.Position ?? anchor);
            chests = FilterChestsInCave(chests, bossCave);
            ores = FilterOreDepositsInCave(ores, bossCave);
            var upStairs = new DungeonStairsModel(anchor, DungeonStairsDirection.Up, generated.LevelNumber - 1, true);
            GameDebugLog.Info("Dungeon", $"Lower level up stairs anchored at {GameDebugLog.Position(anchor)}.");
            return new MazeGenerationResult(
                generated.Grid,
                generated.Settings,
                generated.BasePosition,
                generated.EntrancePosition,
                generated.CentralRoom,
                generated.CentralDoors,
                generated.CentralRoomKey,
                chests,
                generated.Caves,
                ores,
                downStairs,
                upStairs,
                generated.LevelNumber,
                bossCave);
        }

        private static DungeonStairsModel RepositionLowerLevelDownStairs(MazeGenerationResult generated, Vector2Int blockedPosition)
        {
            var distances = MazeValidation.GetReachableDistances(generated.Grid, generated.EntrancePosition, true);
            var bestDistance = -1;
            var best = default(Vector2Int);
            foreach (var cell in generated.Grid.Cells())
            {
                var position = new Vector2Int(cell.X, cell.Y);
                if (position == blockedPosition
                    || !cell.IsStructurallyPassable
                    || generated.CentralRoom.Contains(position)
                    || !generated.CentralRoom.IsBeyondExitSide(position)
                    || ContainsCaveCell(generated.BossCave, position)
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

            if (bestDistance < 0)
            {
                GameDebugLog.Warning("Dungeon", $"Lower level down stairs conflict at {GameDebugLog.Position(blockedPosition)} could not be resolved.");
                return generated.DownStairs;
            }

            generated.Grid.SetType(best, MazeCellType.LockedDownStairs);
            GameDebugLog.Warning("Dungeon", $"Lower level down stairs moved from up-stairs anchor {GameDebugLog.Position(blockedPosition)} to {GameDebugLog.Position(best)}.");
            return new DungeonStairsModel(best, DungeonStairsDirection.Down, generated.DownStairs.TargetLevel, false);
        }

        private static bool ContainsCaveCell(CaveInfo cave, Vector2Int cell)
        {
            return cave.IsValid
                && Mathf.Abs(cell.x - cave.Center.x) <= 1
                && Mathf.Abs(cell.y - cave.Center.y) <= 1;
        }

        private static CaveInfo FindReplacementBossCave(MazeGenerationResult generated, Vector2Int anchor, Vector2Int stairsPosition)
        {
            var distances = MazeValidation.GetReachableDistances(generated.Grid, generated.EntrancePosition, true);
            var bestDistance = -1;
            var best = default(CaveInfo);
            foreach (var cave in generated.Caves)
            {
                if (!cave.IsValid
                    || !generated.CentralRoom.IsBeyondExitSide(cave.Center)
                    || ContainsCaveCell(cave, anchor)
                    || ContainsCaveCell(cave, stairsPosition)
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

        private static List<ChestModel> FilterChestsAtPosition(IReadOnlyList<ChestModel> chests, Vector2Int blockedPosition)
        {
            var filtered = new List<ChestModel>();
            if (chests == null)
            {
                return filtered;
            }

            for (var i = 0; i < chests.Count; i++)
            {
                if (chests[i] != null && chests[i].Position != blockedPosition)
                {
                    filtered.Add(chests[i]);
                }
            }

            return filtered;
        }

        private static List<OreDepositModel> FilterOreDepositsAtPosition(IReadOnlyList<OreDepositModel> deposits, Vector2Int blockedPosition)
        {
            var filtered = new List<OreDepositModel>();
            if (deposits == null)
            {
                return filtered;
            }

            for (var i = 0; i < deposits.Count; i++)
            {
                var deposit = deposits[i];
                if (deposit == null || ContainsCell(deposit.Cells, blockedPosition))
                {
                    continue;
                }

                filtered.Add(deposit);
            }

            return filtered;
        }

        private static List<ChestModel> FilterChestsInCave(IReadOnlyList<ChestModel> chests, CaveInfo cave)
        {
            var filtered = new List<ChestModel>();
            if (chests == null)
            {
                return filtered;
            }

            for (var i = 0; i < chests.Count; i++)
            {
                if (chests[i] != null && !ContainsCaveCell(cave, chests[i].Position))
                {
                    filtered.Add(chests[i]);
                }
            }

            return filtered;
        }

        private static List<OreDepositModel> FilterOreDepositsInCave(IReadOnlyList<OreDepositModel> deposits, CaveInfo cave)
        {
            var filtered = new List<OreDepositModel>();
            if (deposits == null)
            {
                return filtered;
            }

            for (var i = 0; i < deposits.Count; i++)
            {
                var deposit = deposits[i];
                if (deposit == null || deposit.Cave.Center == cave.Center || ContainsAnyCaveCell(cave, deposit.Cells))
                {
                    continue;
                }

                filtered.Add(deposit);
            }

            return filtered;
        }

        private static bool ContainsCell(IReadOnlyList<Vector2Int> cells, Vector2Int position)
        {
            if (cells == null)
            {
                return false;
            }

            for (var i = 0; i < cells.Count; i++)
            {
                if (cells[i] == position)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsAnyCaveCell(CaveInfo cave, IReadOnlyList<Vector2Int> cells)
        {
            if (cells == null)
            {
                return false;
            }

            for (var i = 0; i < cells.Count; i++)
            {
                if (ContainsCaveCell(cave, cells[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static void ConnectLowerLevelAnchor(MazeGenerationResult generated, Vector2Int anchor)
        {
            generated.Grid.SetType(anchor, MazeCellType.Path);
            var reachable = MazeValidation.GetReachableDistances(generated.Grid, generated.EntrancePosition, true);
            if (reachable.ContainsKey(anchor))
            {
                return;
            }

            var queue = new Queue<Vector2Int>();
            var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            queue.Enqueue(anchor);
            cameFrom[anchor] = anchor;

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current != anchor && reachable.ContainsKey(current))
                {
                    CarveLowerLevelConnection(generated.Grid, cameFrom, current, anchor);
                    GameDebugLog.Info("Dungeon", $"Lower level up stairs anchor connected: anchor={GameDebugLog.Position(anchor)}, target={GameDebugLog.Position(current)}.");
                    return;
                }

                foreach (var direction in MazeDirections.Cardinal)
                {
                    var next = current + direction;
                    if (cameFrom.ContainsKey(next)
                        || !generated.Grid.InBounds(next)
                        || !IsAllowedLowerLevelConnectionCell(generated.CentralRoom, anchor, next))
                    {
                        continue;
                    }

                    cameFrom[next] = current;
                    queue.Enqueue(next);
                }
            }

            GameDebugLog.Warning("Dungeon", $"Lower level up stairs anchor {GameDebugLog.Position(anchor)} could not be connected to generated maze.");
        }

        private static void CarveLowerLevelConnection(
            MazeGrid grid,
            Dictionary<Vector2Int, Vector2Int> cameFrom,
            Vector2Int target,
            Vector2Int anchor)
        {
            var current = target;
            while (current != anchor)
            {
                if (grid.Get(current).Type == MazeCellType.Wall)
                {
                    grid.SetType(current, MazeCellType.Path);
                }

                current = cameFrom[current];
            }

            grid.SetType(anchor, MazeCellType.Path);
        }

        private static bool IsAllowedLowerLevelConnectionCell(CentralRoomInfo room, Vector2Int anchor, Vector2Int cell)
        {
            if (!room.IsValid)
            {
                return true;
            }

            if (room.Contains(cell))
            {
                return false;
            }

            return room.IsBeyondExitSide(anchor)
                ? room.IsBeyondExitSide(cell)
                : cell.x < room.Min.x;
        }

        private void RenderDungeonLevel(MazeGenerationResult target, bool includeBase)
        {
            currentMaze = target;
            currentDungeonLevel = target.LevelNumber;
            currentBase = null;
            cartographerMemory = GetCartographerMemoryForLevel(currentDungeonLevel, target.Grid);
            explorationCoordinator.Reset(target.Grid, GetExplorationCoordinatorEntrance(target), target.LevelNumber);

            mazeTerrain.Clear();
            terrainDecorations.Clear();
            mazeRenderer.Clear();
            fogOfWarView.Clear();
            baseConstructionController.Clear();
            heroesGuildView = null;
            heroGuildQuestController.SetGuildView(null);
            mobManager.Clear();
            goldIngotManager.Clear();
            taxCollectorController.Clear();
            dungeonFortificationController.Clear();
            mineConstructionController.Clear();
            baseAmbience.Clear();
            cityAmbience.Clear();

            if (includeBase)
            {
                mazeTerrain.Render(target, mazeRenderer.CellSize);
                mazeTerrain.SetVisualVisible(true);
            }

            currentBase = mazeRenderer.Render(target);
            if (!includeBase && currentBase != null)
            {
                Destroy(currentBase.gameObject);
                currentBase = null;
            }

            if (includeBase)
            {
                terrainDecorations.Render(target, mazeRenderer, baseDevelopment);
                baseAmbience.Initialize(target, mazeRenderer);
                cityAmbience.Initialize(target, mazeRenderer);
                taxCollectorController.Initialize(target);
                dungeonFortificationController.Initialize(target, cartographerMemory);
                mineConstructionController.Initialize(target);
                RestoreBaseBuildingsForLevelOne();
                RestorePendingBaseConstructionSites();
            }

            mobManager.Spawn(target, mazeRenderer);
            RefreshCentralExitSeal();
            var mobPositions = new HashSet<Vector2Int>();
            mobManager.CollectOccupiedPositions(mobPositions);
            goldIngotManager.Spawn(target, mazeRenderer, mobPositions);
            deathTokenManager.Initialize(target, mazeRenderer, GetHeroHouseView);
        }

        private HeroMemory GetCartographerMemoryForLevel(int levelNumber, MazeGrid grid)
        {
            if (levelNumber <= 1)
            {
                if (levelOneCartographerMemory == null)
                {
                    levelOneCartographerMemory = new HeroMemory(grid);
                    levelOneCartographerMemory.Remember(levelOneMaze != null ? levelOneMaze.EntrancePosition : currentMaze.EntrancePosition);
                }

                return levelOneCartographerMemory;
            }

            if (levelTwoCartographerMemory == null)
            {
                levelTwoCartographerMemory = new HeroMemory(grid);
                var knownStart = levelTwoMaze != null && levelTwoMaze.UpStairs != null
                    ? levelTwoMaze.UpStairs.Position
                    : currentMaze.EntrancePosition;
                levelTwoCartographerMemory.Remember(knownStart);
            }

            return levelTwoCartographerMemory;
        }

        private void TransferHeroesToCurrentLevel()
        {
            var start = currentDungeonLevel <= 1
                ? currentMaze.DownStairs != null && currentMaze.DownStairs.IsOpen
                    ? currentMaze.DownStairs.Position
                    : currentMaze.EntrancePosition
                : currentMaze.UpStairs != null
                    ? currentMaze.UpStairs.Position
                    : currentMaze.EntrancePosition;

            foreach (var hero in heroes)
            {
                if (hero == null || hero.Model == null || !hero.Model.IsAlive)
                {
                    continue;
                }

                hero.TransferToLevel(currentMaze, start, mazeRenderer, goldIngotManager, deathTokenManager, SyncHeroKnowledgeAtEntrance, HandleDownStairsOpened, TryGetNearbyHeroMobInteractionCell, TryGetPriorityDungeonHeroTarget, explorationCoordinator);
                hero.SetFortifiedCellProvider(IsHeroMovementFortifiedCell);
            }
        }

        private static Vector2Int GetExplorationCoordinatorEntrance(MazeGenerationResult target)
        {
            if (target == null)
            {
                return Vector2Int.zero;
            }

            if (target.LevelNumber > 1 && target.UpStairs != null)
            {
                return target.UpStairs.Position;
            }

            return target.EntrancePosition;
        }

        private void RestoreBaseBuildingsForLevelOne()
        {
            heroHouseViewsByHeroNumber.Clear();
            foreach (var farmPosition in baseDevelopment.FarmPositions)
            {
                mazeRenderer.RenderFarm(farmPosition);
                RegisterExistingBuilding(BuildingType.Farm, farmPosition);
            }

            foreach (var campPosition in baseDevelopment.LumberjackCampPositions)
            {
                LumberjackCampRenderer.Render(mazeRenderer, campPosition);
                RegisterExistingBuilding(BuildingType.LumberjackCamp, campPosition);
            }

            var heroNumber = 1;
            foreach (var housePosition in baseDevelopment.HeroHousePositions)
            {
                var house = mazeRenderer.RenderHeroHouse(housePosition, heroNumber);
                if (house != null)
                {
                    heroHouseViewsByHeroNumber[heroNumber] = house;
                    house.SetEffectText(GetHeroHouseEffectText(heroNumber));
                }

                RegisterExistingBuilding(BuildingType.HeroHouse, housePosition);
                heroNumber++;
            }

            foreach (var hutPosition in baseDevelopment.PeasantHutPositions)
            {
                var hut = PeasantHutRenderer.Render(mazeRenderer, hutPosition);
                taxCollectorController.RegisterHut(hutPosition, hut);
                RegisterExistingBuilding(BuildingType.PeasantHut, hutPosition);
            }

            if (baseDevelopment.HasAlchemistShop)
            {
                mazeRenderer.RenderAlchemistShop(baseDevelopment.AlchemistShopPosition);
                RegisterExistingBuilding(BuildingType.AlchemistShop, baseDevelopment.AlchemistShopPosition);
            }

            if (baseDevelopment.HasTavern)
            {
                mazeRenderer.RenderTavern(baseDevelopment.TavernPosition);
                RegisterExistingBuilding(BuildingType.Tavern, baseDevelopment.TavernPosition);
            }

            if (baseDevelopment.HasForge)
            {
                ForgeRenderer.Render(mazeRenderer, baseDevelopment.ForgePosition);
                RegisterExistingBuilding(BuildingType.Forge, baseDevelopment.ForgePosition);
            }

            if (baseDevelopment.HasInfirmary)
            {
                InfirmaryRenderer.Render(mazeRenderer, baseDevelopment.InfirmaryPosition);
                RegisterExistingBuilding(BuildingType.Infirmary, baseDevelopment.InfirmaryPosition);
            }

            if (baseDevelopment.HasCartographerHouse)
            {
                CartographerHouseRenderer.Render(mazeRenderer, baseDevelopment.CartographerHousePosition);
                RegisterExistingBuilding(BuildingType.CartographerHouse, baseDevelopment.CartographerHousePosition);
            }

            if (baseDevelopment.HasChapel)
            {
                ChapelRenderer.Render(mazeRenderer, baseDevelopment.ChapelPosition);
                RegisterExistingBuilding(BuildingType.Chapel, baseDevelopment.ChapelPosition);
            }

            if (baseDevelopment.HasMinersGuild)
            {
                MinersGuildRenderer.Render(mazeRenderer, baseDevelopment.MinersGuildPosition);
                RegisterExistingBuilding(BuildingType.MinersGuild, baseDevelopment.MinersGuildPosition);
            }

            if (baseDevelopment.HasMarket)
            {
                MarketRenderer.Render(mazeRenderer, baseDevelopment.MarketPosition);
                RegisterExistingBuilding(BuildingType.Market, baseDevelopment.MarketPosition);
            }

            if (baseDevelopment.HasAntiquary)
            {
                AntiquaryRenderer.Render(mazeRenderer, baseDevelopment.AntiquaryPosition);
                RegisterExistingBuilding(BuildingType.Antiquary, baseDevelopment.AntiquaryPosition);
            }

            if (baseDevelopment.HasHeroesGuild)
            {
                heroesGuildView = HeroesGuildRenderer.Render(mazeRenderer, baseDevelopment.HeroesGuildPosition);
                heroGuildQuestController.SetGuildView(heroesGuildView);
                RegisterExistingBuilding(BuildingType.HeroesGuild, baseDevelopment.HeroesGuildPosition);
            }

            RefreshAllBuildingUpgradeVisuals();
        }

        private void ClearFallenHeroes()
        {
            for (var i = 0; i < fallenHeroes.Count; i++)
            {
                if (fallenHeroes[i] != null)
                {
                    Destroy(fallenHeroes[i].gameObject);
                }
            }

            fallenHeroes.Clear();
        }
    }
}
