using System.Collections.Generic;
using Labyrinth.Base;
using Labyrinth.Hero;

namespace Labyrinth.Core
{
    public sealed partial class GameBootstrap
    {
        private readonly Dictionary<int, BuildingView> heroHouseViewsByHeroNumber = new Dictionary<int, BuildingView>();

        private void CreateHeroFromBase()
        {
            if (!CanCreateHero())
            {
                var cost = GetHeroCost();
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

            ClearTerrainDecorationsAround(housePosition, BaseDevelopment.HeroHouseFootprintRadiusCells);
            var houseView = mazeRenderer.RenderHeroHouse(housePosition, heroNumber);
            if (houseView != null)
            {
                heroHouseViewsByHeroNumber[heroNumber] = houseView;
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
                mazeRenderer,
                personalMemory,
                null,
                goldIngotManager,
                deathTokenManager,
                SyncHeroKnowledgeAtEntrance,
                HandleDownStairsOpened);
            hero.SetFortifiedCellProvider(IsHeroMovementFortifiedCell);
            heroes.Add(hero);
            nextHeroNumber++;
            SelectHero(hero);
            GameAudioController.Play(GameSfx.HeroCreated, mazeRenderer.GridToWorld(currentMaze.EntrancePosition));
            StartAdventureMusicIfNeeded();
            GameDebugLog.Info(
                "Hero",
                $"Created hero #{heroNumber}: cost={heroCost.Format()}, foodLeft={resources.Food}, goldLeft={resources.Gold}, woodLeft={resources.Wood}, spawn={GameDebugLog.Position(currentMaze.EntrancePosition)}, house={GameDebugLog.Position(housePosition)}, hp={hero.Model.HitPoints}/{hero.Model.MaxHitPoints}, atk={hero.Model.AttackPoints}, armor={hero.Model.ArmorPoints}, stamina={hero.Model.Stamina}/{hero.Model.MaxStamina}");
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
            return currentMaze != null
                && heroes.Count < baseDevelopment.MaxHeroCount
                && resources.CanAfford(GetHeroCost());
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
                goldIngotManager?.DropCarriedIngot(hero.Model);
                deathTokenManager?.DropCarriedToken(hero.Model);
                MarkHeroHouseForDefeatedHero(hero);
                deathTokenManager?.CreateTokenForDefeatedHero(hero, housePosition);
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

        private BuildingView GetHeroHouseView(int heroNumber)
        {
            return heroHouseViewsByHeroNumber.TryGetValue(heroNumber, out var houseView) ? houseView : null;
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

            houseView.SetEffectText($"Рыцарь {hero.DisplayNumber} погиб: жетон не возвращен");
            RefreshSelectedHeroVisibility();
            GameDebugLog.Info("Hero", $"Marked hero house for defeated hero #{hero.DisplayNumber} at {GameDebugLog.Position(houseView.GridPosition)}.");
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
