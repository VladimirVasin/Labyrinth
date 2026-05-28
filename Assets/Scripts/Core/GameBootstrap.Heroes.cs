using System.Collections.Generic;
using Labyrinth.Base;
using Labyrinth.Combat;
using Labyrinth.Hero;
using UnityEngine;

namespace Labyrinth.Core
{
    public sealed partial class GameBootstrap
    {
        private const float HeroHouseFundContributionRate = 0.1f;
        private const int HeroHouseFundMinimumEligibleGold = 10;
        private const int HeroHouseFundPersonalGoldReserve = 20;
        private const int HeroHouseFundMaxContributionPerReturn = 10;

        private readonly Dictionary<int, BuildingView> heroHouseViewsByHeroNumber = new Dictionary<int, BuildingView>();
        private readonly Dictionary<int, HeroLineageState> heroLineagesByHeroNumber = new Dictionary<int, HeroLineageState>();
        private readonly SortedSet<int> defeatedHeroSlots = new SortedSet<int>();
        private int heroNameRollCounter;

        private void CreateHeroFromBase()
        {
            if (currentMaze == null)
            {
                return;
            }

            if (TryGetDefeatedHeroSlot(out var rebirthHeroNumber))
            {
                RebirthHeroFromHouse(rebirthHeroNumber);
                return;
            }

            if (!CanCreateHero())
            {
                var cost = GetHeroCost();
                if (baseDevelopment.HeroHouseCount >= baseDevelopment.MaxHeroCount)
                {
                    baseDevelopment.ReportBuildBlocked($"дом героя: лимит домов {baseDevelopment.HeroHouseCount} / {baseDevelopment.MaxHeroCount}");
                    GameDebugLog.Warning(
                        "Hero",
                        $"Hero creation blocked: house slots are full, houses={baseDevelopment.HeroHouseCount}, max={baseDevelopment.MaxHeroCount}.");
                    return;
                }

                if (currentMaze != null
                    && heroes.Count < baseDevelopment.MaxHeroCount
                    && !resources.CanAfford(cost))
                {
                    baseDevelopment.ReportBuildBlocked($"дом героя: нужно {cost.Format()}");
                    GameDebugLog.Warning("Hero", $"Hero creation blocked: food={resources.Food}, gold={resources.Gold}, wood={resources.Wood}, required={cost.Format()}");
                }

                return;
            }

            var heroCost = GetHeroCost();
            var heroNumber = nextHeroNumber;
            if (!baseDevelopment.TryBuildHeroHouse(currentMaze, out var housePosition))
            {
                var blockMessage = baseDevelopment.LastBuildMessage;
                baseDevelopment.ReportBuildBlocked($"дом героя: {blockMessage}");
                GameDebugLog.Warning("Hero", $"Hero {heroNumber} creation blocked: {blockMessage}");
                return;
            }

            if (!resources.TrySpend(heroCost))
            {
                baseDevelopment.RemoveHeroHouse(housePosition);
                baseDevelopment.ReportBuildBlocked($"дом героя: нужно {heroCost.Format()}");
                GameDebugLog.Warning("Hero", $"Hero {heroNumber} creation payment failed: food={resources.Food}, gold={resources.Gold}, wood={resources.Wood}, required={heroCost.Format()}");
                return;
            }

            var lineage = CreateHeroLineage(heroNumber);
            var heroName = lineage.CurrentDisplayName;
            ClearTerrainDecorationsAround(housePosition, BaseDevelopment.HeroHouseFootprintRadiusCells);
            var houseView = mazeRenderer.RenderHeroHouse(housePosition, heroNumber);
            if (houseView != null)
            {
                heroHouseViewsByHeroNumber[heroNumber] = houseView;
                houseView.SetEffectText(BuildHeroHouseEffectText(heroName, lineage));
            }

            baseAmbience.RegisterBuilding(BuildingType.HeroHouse, housePosition);
            cityAmbience.RegisterBuilding(BuildingType.HeroHouse, housePosition);
            SyncPeasantHuts();

            if (cartographerMemory == null)
            {
                cartographerMemory = new HeroMemory(currentMaze.Grid);
                cartographerMemory.Remember(currentMaze.EntrancePosition);
            }

            if (sharedHeroMemoryView == null)
            {
                sharedHeroMemoryView = HeroMemoryView.Create(mazeRenderer);
                sharedHeroMemoryView.transform.SetParent(transform, true);
            }

            if (selectedHeroVisibilityView == null)
            {
                selectedHeroVisibilityView = HeroVisibilityView.Create(mazeRenderer);
                selectedHeroVisibilityView.transform.SetParent(transform, true);
            }

            selectedHeroVisibilityView.SetMode(visibilityDisplayMode);

            var personalMemory = new HeroMemory(currentMaze.Grid);
            if (baseDevelopment.HasCartographerHouse)
            {
                personalMemory.MergeFrom(cartographerMemory);
            }

            var hero = HeroController.Create(
                currentMaze,
                currentMaze.EntrancePosition,
                heroNumber,
                heroName,
                mazeRenderer,
                personalMemory,
                null,
                goldIngotManager,
                deathTokenManager,
                SyncHeroKnowledgeAtEntrance,
                HandleDownStairsOpened,
                BuildHeroStatSeed(heroNumber, lineage.Generation));
            var trainingBonus = ApplyHeroLineageTraits(hero, lineage);
            hero.SetFortifiedCellProvider(IsHeroMovementFortifiedCell);
            heroes.Add(hero);
            nextHeroNumber++;
            SelectHero(hero);
            GameAudioController.Play(GameSfx.HeroCreated, mazeRenderer.GridToWorld(currentMaze.EntrancePosition));
            StartAdventureMusicIfNeeded();
            GameDebugLog.Info(
                "Hero",
                $"Created hero #{heroNumber} ({heroName}): cost={heroCost.Format()}, foodLeft={resources.Food}, goldLeft={resources.Gold}, woodLeft={resources.Wood}, spawn={GameDebugLog.Position(currentMaze.EntrancePosition)}, house={GameDebugLog.Position(housePosition)}, training={trainingBonus.ToDisplayText()}, vengeance={hero.Model.VengeanceText}, hp={hero.Model.HitPoints}/{hero.Model.MaxHitPoints}, atk={hero.Model.AttackPoints}, armor={hero.Model.ArmorPoints}, stamina={hero.Model.Stamina}/{hero.Model.MaxStamina}");
        }

        private void RebirthHeroFromHouse(int heroNumber)
        {
            var heroCost = GetHeroCost();
            if (!resources.TrySpend(heroCost))
            {
                baseDevelopment.ReportBuildBlocked($"возрождение Рыцаря {heroNumber}: нужно {heroCost.Format()}");
                GameDebugLog.Warning(
                    "Hero",
                    $"Hero #{heroNumber} rebirth payment failed: food={resources.Food}, gold={resources.Gold}, wood={resources.Wood}, required={heroCost.Format()}.");
                return;
            }

            RemoveFallenHeroController(heroNumber);
            defeatedHeroSlots.Remove(heroNumber);

            var lineage = GetOrCreateHeroLineage(heroNumber);
            var heroName = lineage.AdvanceToNextGeneration();
            var houseView = GetHeroHouseView(heroNumber);
            if (houseView != null)
            {
                houseView.SetEffectText(BuildHeroHouseEffectText(heroName, lineage));
            }

            if (cartographerMemory == null)
            {
                cartographerMemory = new HeroMemory(currentMaze.Grid);
                cartographerMemory.Remember(currentMaze.EntrancePosition);
            }

            var personalMemory = new HeroMemory(currentMaze.Grid);
            if (baseDevelopment.HasCartographerHouse)
            {
                personalMemory.MergeFrom(cartographerMemory);
            }

            var hero = HeroController.Create(
                currentMaze,
                currentMaze.EntrancePosition,
                heroNumber,
                heroName,
                mazeRenderer,
                personalMemory,
                null,
                goldIngotManager,
                deathTokenManager,
                SyncHeroKnowledgeAtEntrance,
                HandleDownStairsOpened,
                BuildHeroStatSeed(heroNumber, lineage.Generation));
            var trainingBonus = ApplyHeroLineageTraits(hero, lineage);
            hero.SetFortifiedCellProvider(IsHeroMovementFortifiedCell);
            var legacyExperience = lineage.ConsumePendingLegacyExperience();
            var gainedLevels = hero.Model.AddExperience(legacyExperience);
            heroes.Add(hero);
            SelectHero(hero);
            GameAudioController.Play(GameSfx.HeroCreated, mazeRenderer.GridToWorld(currentMaze.EntrancePosition));
            if (gainedLevels > 0)
            {
                GameAudioController.Play(GameSfx.LevelUp, mazeRenderer.GridToWorld(currentMaze.EntrancePosition), 0.85f);
            }

            StartAdventureMusicIfNeeded();
            GameDebugLog.Info(
                "Hero",
                $"Reborn hero #{heroNumber} ({heroName}): generation={lineage.Generation}, legacyXp={legacyExperience}, gainedLevels={gainedLevels}, cost={heroCost.Format()}, foodLeft={resources.Food}, goldLeft={resources.Gold}, woodLeft={resources.Wood}, spawn={GameDebugLog.Position(currentMaze.EntrancePosition)}, house={GameDebugLog.Position(houseView != null ? houseView.GridPosition : currentMaze.BasePosition)}, training={trainingBonus.ToDisplayText()}, vengeance={hero.Model.VengeanceText}, hp={hero.Model.HitPoints}/{hero.Model.MaxHitPoints}, atk={hero.Model.AttackPoints}, armor={hero.Model.ArmorPoints}, stamina={hero.Model.Stamina}/{hero.Model.MaxStamina}.");
        }

        private static HeroLineageTrainingBonus ApplyHeroLineageTraits(HeroController hero, HeroLineageState lineage)
        {
            var bonus = lineage != null ? lineage.TrainingBonus : default;
            hero?.Model?.ApplyLineageBonus(bonus);
            hero?.Model?.AssignVengeanceQuest(lineage?.CurrentMember?.VengeanceQuest);
            return bonus;
        }

        private void StartAdventureMusicIfNeeded()
        {
            if (adventureMusicStarted)
            {
                return;
            }

            adventureMusicStarted = true;
            GameAudioController.StartWorldMusic();
            GameDebugLog.Info("Audio", "Adventure music started after first hero creation.");
        }

        private void TryStartHeroEncounter()
        {
            if (currentMaze == null || combatController.IsActive || victoryAchieved)
            {
                return;
            }

            foreach (var hero in heroes)
            {
                if (hero == null || hero.Model == null || !hero.Model.IsAlive)
                {
                    continue;
                }

                if (mobManager.TryGetEncounter(hero, out var encounteredMob))
                {
                    combatController.StartCombat(hero, encounteredMob, currentMaze.Grid, mazeRenderer);
                    return;
                }
            }
        }

        private bool CanCreateHero()
        {
            if (currentMaze == null || !resources.CanAfford(GetHeroCost()))
            {
                return false;
            }

            return TryGetDefeatedHeroSlot(out _)
                || baseDevelopment.HeroHouseCount < baseDevelopment.MaxHeroCount;
        }

        private bool IsHeroMovementFortifiedCell(UnityEngine.Vector2Int cell)
        {
            return (dungeonFortificationController != null && dungeonFortificationController.IsCellFortified(cell))
                || (mineConstructionController != null && mineConstructionController.IsCellFortified(cell));
        }

        private string GetHeroHouseStatus()
        {
            var status = $"активных: {heroes.Count} / {baseDevelopment.MaxHeroCount}, домов: {baseDevelopment.HeroHouseCount}, постройка {GetHeroCost().Format()}, замок ур. {baseDevelopment.CastleLevel}";
            if (baseDevelopment.LastBuildMessage.Contains("дом героя")
                || baseDevelopment.LastBuildMessage == "нет свободной клетки рядом")
            {
                status += $", {baseDevelopment.LastBuildMessage}";
            }

            if (defeatedHeroSlots.Count > 0)
            {
                status += $", возрождение: {BuildDefeatedHeroSlotsText()}";
            }

            return status;
        }

        private void RetireDefeatedHeroes()
        {
            for (var i = heroes.Count - 1; i >= 0; i--)
            {
                var hero = heroes[i];
                if (hero == null || hero.Model == null)
                {
                    heroes.RemoveAt(i);
                    continue;
                }

                if (hero.Model.IsAlive)
                {
                    continue;
                }

                heroes.RemoveAt(i);
                var housePosition = GetHeroHousePositionOrFallback(hero);
                defeatedHeroSlots.Add(hero.DisplayNumber);
                var lineage = GetOrCreateHeroLineage(hero.DisplayNumber);
                var deathContext = BuildHeroDeathContext(hero);
                goldIngotManager?.DropCarriedIngot(hero.Model);
                deathTokenManager?.DropCarriedToken(hero.Model);
                DropCarriedKey(hero.Model);
                var deathToken = deathTokenManager?.CreateTokenForDefeatedHero(hero, housePosition);
                lineage.RecordDeath(hero.Model, deathToken != null ? deathToken.Id : 0, hero.Model.Position, deathContext);
                MarkHeroHouseForDefeatedHero(hero);
                fallenHeroes.Add(hero);
                hero.SetSelected(false);
                if (selectedHero == hero)
                {
                    SelectHero(null);
                }

                GameDebugLog.Info(
                    "Hero",
                    $"Removed defeated hero #{hero.DisplayNumber} from active hero list. Corpse visibility remains for {hero.CorpseVisibilityRemaining:0.0} seconds.");
            }
        }

        private void DestroyExpiredFallenHeroes()
        {
            for (var i = fallenHeroes.Count - 1; i >= 0; i--)
            {
                var hero = fallenHeroes[i];
                if (hero == null)
                {
                    fallenHeroes.RemoveAt(i);
                    continue;
                }

                if (!hero.IsExpiredCorpse)
                {
                    continue;
                }

                fallenHeroes.RemoveAt(i);
                Destroy(hero.gameObject);
                GameDebugLog.Info("Hero", $"Destroyed expired corpse controller for hero #{hero.DisplayNumber}.");
            }
        }

        private IReadOnlyList<HeroController> BuildVisibilityHeroes()
        {
            visibilityHeroes.Clear();
            AddVisibleHeroes(heroes);
            AddVisibleHeroes(fallenHeroes);
            return visibilityHeroes;
        }

        private void AddVisibleHeroes(IReadOnlyList<HeroController> source)
        {
            for (var i = 0; i < source.Count; i++)
            {
                var hero = source[i];
                if (hero != null && hero.Model != null && hero.ProvidesVisibility)
                {
                    visibilityHeroes.Add(hero);
                }
            }
        }

        private bool TryGetDefeatedHeroSlot(out int heroNumber)
        {
            foreach (var candidate in defeatedHeroSlots)
            {
                if (IsHeroActive(candidate) || GetHeroHouseView(candidate) == null)
                {
                    continue;
                }

                heroNumber = candidate;
                return true;
            }

            heroNumber = 0;
            return false;
        }

        private HeroLineageState CreateHeroLineage(int heroNumber)
        {
            var seed = currentMaze != null ? currentMaze.Settings.Seed : 0;
            var baseName = HeroKnightNameCatalog.Pick(seed, heroNumber, heroNameRollCounter++, BuildUsedHeroNames());
            var lineage = new HeroLineageState(heroNumber, baseName);
            heroLineagesByHeroNumber[heroNumber] = lineage;
            return lineage;
        }

        private HeroLineageState GetOrCreateHeroLineage(int heroNumber)
        {
            if (heroLineagesByHeroNumber.TryGetValue(heroNumber, out var lineage) && lineage != null)
            {
                return lineage;
            }

            return CreateHeroLineage(heroNumber);
        }

        private int BuildHeroStatSeed(int heroNumber, int generation)
        {
            unchecked
            {
                var mapSeed = currentMaze != null ? currentMaze.Settings.Seed : 0;
                return mapSeed
                    ^ (heroNumber * 73856093)
                    ^ (Mathf.Max(1, generation) * 19349663)
                    ^ 0x2d2b79f5;
            }
        }

        private HashSet<string> BuildUsedHeroNames()
        {
            var names = new HashSet<string>();
            foreach (var pair in heroLineagesByHeroNumber)
            {
                if (pair.Value != null && !string.IsNullOrEmpty(pair.Value.BaseName))
                {
                    names.Add(pair.Value.BaseName);
                }
            }

            for (var i = 0; i < heroes.Count; i++)
            {
                var name = heroes[i] != null ? heroes[i].DisplayName : null;
                if (!string.IsNullOrEmpty(name))
                {
                    names.Add(name);
                }
            }

            return names;
        }

        private string GetHeroHouseEffectText(int heroNumber)
        {
            for (var i = 0; i < heroes.Count; i++)
            {
                var hero = heroes[i];
                if (hero != null
                    && hero.DisplayNumber == heroNumber
                    && hero.Model != null
                    && hero.Model.IsAlive)
                {
                    var activeLineage = GetOrCreateHeroLineage(heroNumber);
                    return BuildHeroHouseEffectText(hero.DisplayName, activeLineage);
                }
            }

            var lineage = GetOrCreateHeroLineage(heroNumber);
            return defeatedHeroSlots.Contains(heroNumber)
                ? BuildHeroHouseEffectText($"{lineage.CurrentDisplayName} погиб: ждёт наследника", lineage)
                : BuildHeroHouseEffectText(lineage.CurrentDisplayName, lineage);
        }

        private static string BuildHeroHouseEffectText(string title, HeroLineageState lineage)
        {
            if (lineage == null)
            {
                return title;
            }

            return $"{title}\nФонд: {lineage.HouseFundGold} зол.; {lineage.TrainingCompactText}\n{lineage.CurrentVengeanceSummaryText}";
        }

        private bool IsHeroActive(int heroNumber)
        {
            for (var i = 0; i < heroes.Count; i++)
            {
                var hero = heroes[i];
                if (hero != null
                    && hero.DisplayNumber == heroNumber
                    && hero.Model != null
                    && hero.Model.IsAlive)
                {
                    return true;
                }
            }

            return false;
        }

        private string BuildDefeatedHeroSlotsText()
        {
            if (defeatedHeroSlots.Count == 0)
            {
                return "нет";
            }

            var text = string.Empty;
            foreach (var heroNumber in defeatedHeroSlots)
            {
                if (text.Length > 0)
                {
                    text += ", ";
                }

                text += $"#{heroNumber}";
            }

            return text;
        }

        private BuildingView GetHeroHouseView(int heroNumber)
        {
            return heroHouseViewsByHeroNumber.TryGetValue(heroNumber, out var houseView) ? houseView : null;
        }

        private bool TryGetHeroNumberByHouse(BuildingView house, out int heroNumber)
        {
            foreach (var pair in heroHouseViewsByHeroNumber)
            {
                if (pair.Value == house)
                {
                    heroNumber = pair.Key;
                    return true;
                }
            }

            heroNumber = 0;
            return false;
        }

        private HeroController GetActiveHeroByNumber(int heroNumber)
        {
            for (var i = 0; i < heroes.Count; i++)
            {
                var hero = heroes[i];
                if (hero != null
                    && hero.DisplayNumber == heroNumber
                    && hero.Model != null
                    && hero.Model.IsAlive)
                {
                    return hero;
                }
            }

            return null;
        }

        private void HandleHeroCarryObjectiveDelivered(HeroModel heroModel)
        {
            if (TryGetHeroNumberByModel(heroModel, out var heroNumber))
            {
                RefreshHeroHouseEffect(heroNumber);
            }
        }

        private bool TryGetHeroNumberByModel(HeroModel heroModel, out int heroNumber)
        {
            for (var i = 0; i < heroes.Count; i++)
            {
                var hero = heroes[i];
                if (hero != null && hero.Model == heroModel)
                {
                    heroNumber = hero.DisplayNumber;
                    return true;
                }
            }

            heroNumber = 0;
            return false;
        }

        private void RefreshHeroHouseEffect(int heroNumber)
        {
            if (heroNumber <= 0)
            {
                return;
            }

            var houseView = GetHeroHouseView(heroNumber);
            if (houseView != null)
            {
                houseView.SetEffectText(GetHeroHouseEffectText(heroNumber));
            }
        }

        private HeroDeathContext BuildHeroDeathContext(HeroController hero)
        {
            if (hero == null || hero.Model == null)
            {
                return default;
            }

            var carriedGoldIngot = hero.Model.Inventory != null && hero.Model.Inventory.HasGoldIngot;
            var carriedDeathToken = deathTokenManager != null && deathTokenManager.HasCarriedToken(hero.Model);
            var diedInDarkness = IsDeathPositionDark(hero.Model.Position);
            var nearBarrier = TryFindNearbyClosedBarrier(hero.Model.Position, out var barrierPosition, out var barrierName);
            var context = hero.Model.BuildDeathContext(
                carriedGoldIngot,
                carriedDeathToken,
                diedInDarkness,
                nearBarrier,
                barrierPosition,
                barrierName);
            GameDebugLog.Info(
                "Hero",
                $"Death context for hero #{hero.DisplayNumber}: cause={context.CauseText}, level={context.DungeonLevel}, dark={context.DiedInDarkness}, carriedGold={context.CarriedGoldIngot}, carriedToken={context.CarriedDeathToken}, nearBarrier={context.NearBarrier} {context.BarrierName}.");
            return context;
        }

        private bool IsDeathPositionDark(Vector2Int position)
        {
            if (currentMaze == null || currentMaze.Grid == null)
            {
                return false;
            }

            var visibleCells = BuildLightingVisibleCells(BuildVisibilityHeroes());
            return !visibleCells.Contains(position);
        }

        private bool TryFindNearbyClosedBarrier(Vector2Int position, out Vector2Int barrierPosition, out string barrierName)
        {
            barrierPosition = default;
            barrierName = string.Empty;
            if (currentMaze == null)
            {
                return false;
            }

            foreach (var door in currentMaze.CentralDoors)
            {
                if (door == null || !door.IsClosed || GridDistance(position, door.Position) > 1)
                {
                    continue;
                }

                barrierPosition = door.Position;
                barrierName = door.Name;
                return true;
            }

            var stairs = currentMaze.DownStairs;
            if (stairs != null && !stairs.IsOpen && GridDistance(position, stairs.Position) <= 1)
            {
                barrierPosition = stairs.Position;
                barrierName = stairs.DisplayName;
                return true;
            }

            return false;
        }

        private static int GridDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        private void TryContributeHeroHouseFundAtEntrance(HeroModel heroModel, int heroNumber)
        {
            if (heroModel == null
                || currentMaze == null
                || houseFundCouriers == null
                || !heroLineagesByHeroNumber.TryGetValue(heroNumber, out var lineage)
                || lineage == null)
            {
                return;
            }

            var eligibleGold = heroModel.ConsumeHouseFundEligibleGold();
            if (eligibleGold < HeroHouseFundMinimumEligibleGold)
            {
                if (eligibleGold > 0)
                {
                    GameDebugLog.Info(
                        "Hero",
                        $"Hero #{heroNumber} skipped house fund contribution at entrance: eligibleGold={eligibleGold}, minimum={HeroHouseFundMinimumEligibleGold}.");
                }

                return;
            }

            var rawContribution = Mathf.Max(1, Mathf.FloorToInt(eligibleGold * HeroHouseFundContributionRate));
            var contribution = Mathf.Min(rawContribution, HeroHouseFundMaxContributionPerReturn);
            var affordableContribution = Mathf.Max(0, heroModel.Gold - HeroHouseFundPersonalGoldReserve);
            contribution = Mathf.Min(contribution, affordableContribution);
            if (contribution <= 0)
            {
                GameDebugLog.Info(
                    "Hero",
                    $"Hero #{heroNumber} skipped house fund contribution at entrance: eligibleGold={eligibleGold}, heroGold={heroModel.Gold}, reserve={HeroHouseFundPersonalGoldReserve}.");
                return;
            }

            if (!heroModel.TrySpendGold(contribution))
            {
                GameDebugLog.Warning(
                    "Hero",
                    $"Hero #{heroNumber} could not reserve house fund contribution: contribution={contribution}, heroGold={heroModel.Gold}.");
                return;
            }

            var houseView = GetHeroHouseView(heroNumber);
            var housePosition = houseView != null ? houseView.GridPosition : currentMaze.BasePosition;
            houseFundCouriers.QueueGoldTransfer(
                heroNumber,
                lineage.Generation,
                contribution,
                currentMaze.EntrancePosition,
                currentMaze.BasePosition,
                housePosition,
                (deliveredGeneration, deliveredAmount) => CompleteHeroHouseFundContribution(heroNumber, deliveredGeneration, deliveredAmount));
            GameDebugLog.Info(
                "Hero",
                $"Hero #{heroNumber} reserved house fund contribution at entrance: eligibleGold={eligibleGold}, rawContribution={rawContribution}, contribution={contribution}, reserve={HeroHouseFundPersonalGoldReserve}, heroGold={heroModel.Gold}.");
        }

        private void CompleteHeroHouseFundContribution(int heroNumber, int generation, int amount)
        {
            if (!heroLineagesByHeroNumber.TryGetValue(heroNumber, out var lineage) || lineage == null)
            {
                return;
            }

            lineage.ContributeGold(amount, generation);
            var houseView = GetHeroHouseView(heroNumber);
            if (houseView != null)
            {
                houseView.SetEffectText(GetHeroHouseEffectText(heroNumber));
                DamageNumberView.CreateText(
                    mazeRenderer,
                    houseView.GridPosition,
                    $"+{amount} фонд",
                    new Color(1f, 0.78f, 0.22f),
                    3.8f);
            }

            GameDebugLog.Info(
                "Hero",
                $"Hero house fund updated: heroSlot={heroNumber}, generation={generation}, delivered={amount}, fund={lineage.HouseFundGold}, total={lineage.TotalContributedGold}.");
        }

        private void ShowHeroHouseLineage(BuildingView house)
        {
            if (house == null
                || house.Type != BuildingType.HeroHouse
                || !TryGetHeroNumberByHouse(house, out var heroNumber))
            {
                return;
            }

            var lineage = GetOrCreateHeroLineage(heroNumber);
            heroLineageHud.Show(lineage);
        }

        private void HandleHeroDeathTokenDelivered(HeroDeathTokenModel token)
        {
            if (token == null
                || !heroLineagesByHeroNumber.TryGetValue(token.HeroNumber, out var lineage)
                || lineage == null)
            {
                return;
            }

            if (lineage.MarkDeathTokenReturned(token.Id))
            {
                RefreshHeroHouseEffect(token.HeroNumber);
                GameDebugLog.Info("Hero", $"Lineage token returned: heroSlot={token.HeroNumber}, token={token.Id}, fallen={token.FallenHeroName}.");
            }
        }

        private UnityEngine.Vector2Int GetHeroHousePositionOrFallback(HeroController hero)
        {
            if (hero != null
                && heroHouseViewsByHeroNumber.TryGetValue(hero.DisplayNumber, out var houseView)
                && houseView != null)
            {
                return houseView.GridPosition;
            }

            return currentMaze != null ? currentMaze.BasePosition : UnityEngine.Vector2Int.zero;
        }

        private void DestroyHeroes()
        {
            selectedHero = null;
            heroHouseViewsByHeroNumber.Clear();
            heroLineagesByHeroNumber.Clear();
            defeatedHeroSlots.Clear();
            heroNameRollCounter = 0;
            DestroyHeroList(heroes);
            DestroyHeroList(fallenHeroes);
            heroes.Clear();
            fallenHeroes.Clear();
            visibilityHeroes.Clear();
            nextHeroNumber = 1;
        }

        private void MarkHeroHouseForDefeatedHero(HeroController hero)
        {
            if (hero == null || !heroHouseViewsByHeroNumber.TryGetValue(hero.DisplayNumber, out var houseView) || houseView == null)
            {
                return;
            }

            var lineage = GetOrCreateHeroLineage(hero.DisplayNumber);
            houseView.SetEffectText(BuildHeroHouseEffectText($"{lineage.CurrentDisplayName} погиб: ждёт наследника", lineage));
            RefreshSelectedHeroVisibility();
            GameDebugLog.Info("Hero", $"Marked hero house for defeated hero #{hero.DisplayNumber} ({lineage.CurrentDisplayName}) at {GameDebugLog.Position(houseView.GridPosition)}.");
        }

        private void RemoveFallenHeroController(int heroNumber)
        {
            for (var i = fallenHeroes.Count - 1; i >= 0; i--)
            {
                var hero = fallenHeroes[i];
                if (hero == null)
                {
                    fallenHeroes.RemoveAt(i);
                    continue;
                }

                if (hero.DisplayNumber != heroNumber)
                {
                    continue;
                }

                fallenHeroes.RemoveAt(i);
                Destroy(hero.gameObject);
                GameDebugLog.Info("Hero", $"Removed old corpse controller before rebirth for hero #{heroNumber}.");
            }
        }

        private void DestroyHeroList(IReadOnlyList<HeroController> source)
        {
            for (var i = 0; i < source.Count; i++)
            {
                var hero = source[i];
                if (hero != null)
                {
                    Destroy(hero.gameObject);
                }
            }
        }
    }
}
