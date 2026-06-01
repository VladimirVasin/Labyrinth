using System.Collections.Generic;
using Labyrinth.Hero;
using UnityEngine;

namespace Labyrinth.Core
{
    public sealed partial class GameBootstrap
    {
        private const float HeroEntranceCommuteStepSeconds = 0.31f;

        private readonly List<HeroEntranceCommuteRuntime> heroEntranceCommutes = new List<HeroEntranceCommuteRuntime>();

        private HeroController CreateHeroControllerFromHouse(
            int heroNumber,
            string heroName,
            HeroLineageState lineage,
            Vector2Int housePosition,
            out HeroLineageTrainingBonus trainingBonus)
        {
            trainingBonus = default;
            if (currentMaze == null)
            {
                return null;
            }

            EnsureHeroExpeditionViews();
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
                TryGetNearbyHeroMobInteractionCell,
                explorationCoordinator,
                BuildHeroStatSeed(heroNumber, lineage.Generation));
            trainingBonus = ApplyHeroLineageTraits(hero, lineage);
            hero.SetFortifiedCellProvider(IsHeroMovementFortifiedCell);
            QueueHeroEntranceCommute(hero, housePosition);
            heroes.Add(hero);
            SelectHero(hero);
            return hero;
        }

        private void EnsureHeroExpeditionViews()
        {
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
        }

        private void QueueHeroEntranceCommute(HeroController hero, Vector2Int housePosition)
        {
            if (hero == null || currentMaze == null)
            {
                return;
            }

            RemoveHeroEntranceCommute(hero.DisplayNumber);
            hero.BeginEntranceCommute(housePosition);
            heroEntranceCommutes.Add(new HeroEntranceCommuteRuntime(
                hero,
                housePosition,
                currentMaze.BasePosition,
                currentMaze.EntrancePosition));
            GameDebugLog.Info(
                "Hero",
                $"Hero #{hero.DisplayNumber} left house for entrance: house={GameDebugLog.Position(housePosition)}, castle={GameDebugLog.Position(currentMaze.BasePosition)}, entrance={GameDebugLog.Position(currentMaze.EntrancePosition)}.");
        }

        private void UpdateHeroEntranceCommutes()
        {
            if (currentMaze == null || baseAmbience == null || heroEntranceCommutes.Count == 0)
            {
                return;
            }

            for (var i = heroEntranceCommutes.Count - 1; i >= 0; i--)
            {
                var commute = heroEntranceCommutes[i];
                if (commute.Hero == null || commute.Hero.Model == null || !commute.Hero.Model.IsAlive)
                {
                    heroEntranceCommutes.RemoveAt(i);
                    continue;
                }

                if (!commute.HasPath)
                {
                    if (!TryBuildHeroEntranceCommutePath(commute, out var path))
                    {
                        commute.LogWaitingForRoadOnce();
                        continue;
                    }

                    commute.SetPath(path);
                    GameDebugLog.Info(
                        "Hero",
                        $"Hero #{commute.Hero.DisplayNumber} entrance route ready: cells={path.Count}, house={GameDebugLog.Position(commute.HousePosition)}, entrance={GameDebugLog.Position(commute.EntrancePosition)}.");
                }

                if (!commute.TryAdvance(Time.deltaTime, out var nextCell, out var arrived))
                {
                    continue;
                }

                if (arrived)
                {
                    commute.Hero.CompleteEntranceCommute();
                    heroEntranceCommutes.RemoveAt(i);
                    GameAudioController.Play(GameSfx.HeroCreated, mazeRenderer.GridToWorld(commute.EntrancePosition), 0.82f);
                    RefreshSelectedHeroVisibility();
                    GameDebugLog.Info(
                        "Hero",
                        $"Hero #{commute.Hero.DisplayNumber} reached the labyrinth entrance from house {GameDebugLog.Position(commute.HousePosition)}.");
                    continue;
                }

                commute.Hero.MoveEntranceCommuteTo(nextCell);
            }
        }

        private bool TryBuildHeroEntranceCommutePath(HeroEntranceCommuteRuntime commute, out List<Vector2Int> path)
        {
            path = null;
            if (commute == null
                || !baseAmbience.TryGetRoadPath(commute.HousePosition, commute.CastlePosition, out var houseToCastle)
                || !baseAmbience.TryGetRoadPath(commute.CastlePosition, commute.EntrancePosition, out var castleToEntrance))
            {
                return false;
            }

            path = new List<Vector2Int>(houseToCastle.Count + castleToEntrance.Count);
            path.AddRange(houseToCastle);
            for (var i = 1; i < castleToEntrance.Count; i++)
            {
                path.Add(castleToEntrance[i]);
            }

            return path.Count >= 2;
        }

        private void RemoveHeroEntranceCommute(int heroNumber)
        {
            for (var i = heroEntranceCommutes.Count - 1; i >= 0; i--)
            {
                var hero = heroEntranceCommutes[i].Hero;
                if (hero != null && hero.DisplayNumber == heroNumber)
                {
                    heroEntranceCommutes.RemoveAt(i);
                }
            }
        }

        private void ClearHeroEntranceCommutes()
        {
            heroEntranceCommutes.Clear();
        }

        private sealed class HeroEntranceCommuteRuntime
        {
            private List<Vector2Int> path;
            private int nextCellIndex = 1;
            private float stepTimer;
            private bool roadWaitLogged;

            public HeroEntranceCommuteRuntime(
                HeroController hero,
                Vector2Int housePosition,
                Vector2Int castlePosition,
                Vector2Int entrancePosition)
            {
                Hero = hero;
                HousePosition = housePosition;
                CastlePosition = castlePosition;
                EntrancePosition = entrancePosition;
            }

            public HeroController Hero { get; }
            public Vector2Int HousePosition { get; }
            public Vector2Int CastlePosition { get; }
            public Vector2Int EntrancePosition { get; }
            public bool HasPath => path != null && path.Count >= 2;

            public void SetPath(List<Vector2Int> route)
            {
                path = route;
                nextCellIndex = 1;
                stepTimer = 0f;
                roadWaitLogged = false;
            }

            public bool TryAdvance(float deltaTime, out Vector2Int nextCell, out bool arrived)
            {
                nextCell = default;
                arrived = false;
                if (!HasPath || nextCellIndex >= path.Count)
                {
                    arrived = true;
                    return HasPath;
                }

                stepTimer -= deltaTime;
                if (stepTimer > 0f)
                {
                    return false;
                }

                nextCell = path[nextCellIndex++];
                arrived = nextCellIndex >= path.Count;
                stepTimer = HeroEntranceCommuteStepSeconds;
                return true;
            }

            public void LogWaitingForRoadOnce()
            {
                if (roadWaitLogged || Hero == null)
                {
                    return;
                }

                roadWaitLogged = true;
                GameDebugLog.Info(
                    "Hero",
                    $"Hero #{Hero.DisplayNumber} waits for completed roads before entering the labyrinth: house={GameDebugLog.Position(HousePosition)}, castle={GameDebugLog.Position(CastlePosition)}, entrance={GameDebugLog.Position(EntrancePosition)}.");
            }
        }
    }
}
