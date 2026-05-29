using System.Collections.Generic;
using Labyrinth.Hero;
using Labyrinth.Maze;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Labyrinth.Core
{
    public sealed partial class GameBootstrap
    {
        private const int SilentStepTrailBlockRadius = 2;
        private const int RespawnHeroSafetyRadius = 5;

        private void HandleVisibilityModeHotkeys()
        {
            if (Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current.f1Key.wasPressedThisFrame)
            {
                SetVisibilityMode(HeroVisibilityDisplayMode.Schematic);
            }
            else if (Keyboard.current.f2Key.wasPressedThisFrame)
            {
                SetVisibilityMode(HeroVisibilityDisplayMode.Lighting);
            }
        }

        private void SetVisibilityMode(HeroVisibilityDisplayMode displayMode)
        {
            visibilityDisplayMode = displayMode;
            if (selectedHeroVisibilityView != null)
            {
                selectedHeroVisibilityView.SetMode(displayMode);
            }

            RefreshSelectedHeroVisibility();
        }

        private void SelectHero(HeroController hero)
        {
            if (selectedHero == hero)
            {
                return;
            }

            if (selectedHero != null)
            {
                selectedHero.SetSelected(false);
            }

            selectedHero = hero;

            if (selectedHero != null)
            {
                ClearSelectedMob();
                selectedHero.SetSelected(true);
            }

            RefreshSelectedHeroVisibility();
            RefreshMapSelectionMarker();
        }

        private void RefreshSelectedHeroVisibility()
        {
            if (selectedHeroVisibilityView == null)
            {
                return;
            }

            ApplyVisibilityEnvironment(visibilityDisplayMode);

            if (currentMaze == null)
            {
                selectedHeroVisibilityView.Hide();
                mazeRenderer.ShowAllCells();
                RefreshMemoryOverlay();
                fogOfWarView.Hide();
                mobManager.ShowAllMobs();
                return;
            }

            if (visibilityDisplayMode == HeroVisibilityDisplayMode.Lighting)
            {
                var visibleHeroes = BuildVisibilityHeroes();
                selectedHeroVisibilityView.ShowLighting(visibleHeroes, currentMaze.Grid);
                var visibleCells = BuildLightingVisibleCells(visibleHeroes);
                var exploredCells = BuildDisplayedExploredCells();
                mazeRenderer.ApplyCellVisibility(BuildKnownCells(visibleCells, exploredCells), currentMaze.Grid);
                mazeRenderer.ApplyHeroLightTints(visibleHeroes, currentMaze.Grid, visibleCells);
                fogOfWarView.Show(currentMaze.Grid, exploredCells, visibleCells);
                RefreshMemoryOverlay(visibleCells);
                mobManager.ApplyVisibility(visibleCells);
                return;
            }

            selectedHeroVisibilityView.ShowSchematic(BuildSchematicVisibleCells(BuildVisibilityHeroes()), currentMaze.Grid);
            mazeRenderer.ClearHeroLightTints();
            mazeRenderer.ShowAllCells();
            fogOfWarView.Hide();
            RefreshMemoryOverlay();
            mobManager.ShowAllMobs();
        }

        private HashSet<Vector2Int> BuildSchematicVisibleCells(IReadOnlyList<HeroController> heroesWithVisibility)
        {
            var visibleCells = new HashSet<Vector2Int>();
            foreach (var hero in heroesWithVisibility)
            {
                if (hero == null || hero.Model == null || !hero.ProvidesVisibility)
                {
                    continue;
                }

                foreach (var position in hero.Model.Visibility.VisibleCells)
                {
                    if (currentMaze.Grid.InBounds(position))
                    {
                        visibleCells.Add(position);
                    }
                }
            }

            dungeonFortificationController?.AddTorchLitCells(visibleCells);
            mineConstructionController?.AddTorchLitCells(visibleCells);
            return visibleCells;
        }

        private HashSet<Vector2Int> BuildLightingVisibleCells(IReadOnlyList<HeroController> heroesWithVisibility)
        {
            var visibleCells = new HashSet<Vector2Int>();
            foreach (var hero in heroesWithVisibility)
            {
                if (hero == null || hero.Model == null || !hero.ProvidesVisibility)
                {
                    continue;
                }

                foreach (var position in hero.Model.Visibility.VisibleCells)
                {
                    if (currentMaze.Grid.InBounds(position))
                    {
                        visibleCells.Add(position);
                    }
                }
            }

            visibleCells.Add(currentMaze.EntrancePosition);
            AddBuildingVisibility(visibleCells, currentMaze.BasePosition, BaseDevelopment.CastleFootprintRadiusCells + BaseDevelopment.BuildingVisibilityPaddingCells);

            foreach (var farmPosition in baseDevelopment.FarmPositions)
            {
                AddBuildingVisibility(visibleCells, farmPosition, BaseDevelopment.FarmFootprintRadiusCells + BaseDevelopment.BuildingVisibilityPaddingCells);
            }

            foreach (var campPosition in baseDevelopment.LumberjackCampPositions)
            {
                AddBuildingVisibility(visibleCells, campPosition, BaseDevelopment.LumberjackCampFootprintRadiusCells + BaseDevelopment.BuildingVisibilityPaddingCells);
            }

            foreach (var housePosition in baseDevelopment.HeroHousePositions)
            {
                AddBuildingVisibility(visibleCells, housePosition, BaseDevelopment.HeroHouseFootprintRadiusCells + BaseDevelopment.BuildingVisibilityPaddingCells);
            }

            foreach (var hutPosition in baseDevelopment.PeasantHutPositions)
            {
                AddBuildingVisibility(visibleCells, hutPosition, BaseDevelopment.PeasantHutFootprintRadiusCells + BaseDevelopment.BuildingVisibilityPaddingCells);
            }

            if (baseDevelopment.HasAlchemistShop)
            {
                AddBuildingVisibility(visibleCells, baseDevelopment.AlchemistShopPosition, BaseDevelopment.AlchemistShopFootprintRadiusCells + BaseDevelopment.BuildingVisibilityPaddingCells);
            }

            if (baseDevelopment.HasTavern)
            {
                AddBuildingVisibility(visibleCells, baseDevelopment.TavernPosition, BaseDevelopment.TavernFootprintRadiusCells + BaseDevelopment.BuildingVisibilityPaddingCells);
            }

            if (baseDevelopment.HasForge)
            {
                AddBuildingVisibility(visibleCells, baseDevelopment.ForgePosition, BaseDevelopment.ForgeFootprintRadiusCells + BaseDevelopment.BuildingVisibilityPaddingCells);
            }

            if (baseDevelopment.HasInfirmary)
            {
                AddBuildingVisibility(visibleCells, baseDevelopment.InfirmaryPosition, BaseDevelopment.InfirmaryFootprintRadiusCells + BaseDevelopment.BuildingVisibilityPaddingCells);
            }

            if (baseDevelopment.HasCartographerHouse)
            {
                AddBuildingVisibility(visibleCells, baseDevelopment.CartographerHousePosition, BaseDevelopment.CartographerHouseFootprintRadiusCells + BaseDevelopment.BuildingVisibilityPaddingCells);
            }

            if (baseDevelopment.HasChapel)
            {
                AddBuildingVisibility(visibleCells, baseDevelopment.ChapelPosition, BaseDevelopment.ChapelFootprintRadiusCells + BaseDevelopment.BuildingVisibilityPaddingCells);
            }

            if (baseDevelopment.HasMinersGuild)
            {
                AddBuildingVisibility(visibleCells, baseDevelopment.MinersGuildPosition, BaseDevelopment.MinersGuildFootprintRadiusCells + BaseDevelopment.BuildingVisibilityPaddingCells);
            }

            if (baseDevelopment.HasMarket)
            {
                AddBuildingVisibility(visibleCells, baseDevelopment.MarketPosition, BaseDevelopment.MarketFootprintRadiusCells + BaseDevelopment.BuildingVisibilityPaddingCells);
            }

            if (baseDevelopment.HasAntiquary)
            {
                AddBuildingVisibility(visibleCells, baseDevelopment.AntiquaryPosition, BaseDevelopment.AntiquaryFootprintRadiusCells + BaseDevelopment.BuildingVisibilityPaddingCells);
            }

            dungeonFortificationController?.AddTorchLitCells(visibleCells);
            mineConstructionController?.AddTorchLitCells(visibleCells);
            return visibleCells;
        }

        private HashSet<Vector2Int> BuildRespawnBlockedCells(IReadOnlyList<HeroController> heroesWithVisibility)
        {
            var blockedCells = new HashSet<Vector2Int>();
            if (currentMaze == null || currentMaze.Grid == null)
            {
                return blockedCells;
            }

            blockedCells.Add(currentMaze.EntrancePosition);
            dungeonFortificationController?.AddTorchLitCells(blockedCells);
            mineConstructionController?.AddTorchLitCells(blockedCells);

            foreach (var hero in heroesWithVisibility)
            {
                if (hero == null || hero.Model == null || !hero.Model.IsAlive)
                {
                    continue;
                }

                AddRespawnSafetyCells(
                    blockedCells,
                    hero.Model.Position,
                    Mathf.Max(RespawnHeroSafetyRadius, hero.Model.SightRange + 1));

                if (!hero.Model.HasBlessing(HeroBlessingType.SilentStep))
                {
                    continue;
                }

                foreach (var remembered in hero.Model.Memory.RememberedCells)
                {
                    AddBlessedTrailCells(blockedCells, remembered);
                }
            }

            return blockedCells;
        }

        private void AddRespawnSafetyCells(HashSet<Vector2Int> cells, Vector2Int center, int radius)
        {
            for (var x = center.x - radius; x <= center.x + radius; x++)
            {
                for (var y = center.y - radius; y <= center.y + radius; y++)
                {
                    var position = new Vector2Int(x, y);
                    if (currentMaze.Grid.InBounds(position))
                    {
                        cells.Add(position);
                    }
                }
            }
        }

        private void AddBlessedTrailCells(HashSet<Vector2Int> cells, Vector2Int center)
        {
            for (var x = center.x - SilentStepTrailBlockRadius; x <= center.x + SilentStepTrailBlockRadius; x++)
            {
                for (var y = center.y - SilentStepTrailBlockRadius; y <= center.y + SilentStepTrailBlockRadius; y++)
                {
                    var position = new Vector2Int(x, y);
                    if (currentMaze.Grid.InBounds(position))
                    {
                        cells.Add(position);
                    }
                }
            }
        }

        private void AddBuildingVisibility(HashSet<Vector2Int> visibleCells, Vector2Int buildingPosition, int radius)
        {
            for (var x = buildingPosition.x - radius; x <= buildingPosition.x + radius; x++)
            {
                for (var y = buildingPosition.y - radius; y <= buildingPosition.y + radius; y++)
                {
                    visibleCells.Add(new Vector2Int(x, y));
                }
            }
        }

        private void ApplyVisibilityEnvironment(HeroVisibilityDisplayMode displayMode)
        {
            if (mainCamera == null)
            {
                return;
            }

            if (displayMode == HeroVisibilityDisplayMode.Lighting)
            {
                RenderSettings.ambientLight = new Color(0.08f, 0.09f, 0.115f);
                mainCamera.clearFlags = CameraClearFlags.SolidColor;
                mainCamera.backgroundColor = new Color(0.005f, 0.006f, 0.008f);
                mazeTerrain.SetVisualVisible(true);
                return;
            }

            RenderSettings.ambientLight = normalAmbientLight;
            mainCamera.clearFlags = normalCameraClearFlags;
            mainCamera.backgroundColor = normalCameraBackgroundColor;
            mazeTerrain.SetVisualVisible(true);
        }
    }
}
