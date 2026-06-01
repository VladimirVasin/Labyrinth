using Labyrinth.Base;
using Labyrinth.Maze;
using UnityEngine;

namespace Labyrinth.Core
{
    public sealed partial class GameBootstrap
    {
        private void SyncPeasantHuts()
        {
            if (currentMaze == null || taxCollectorController == null)
            {
                return;
            }

            while (baseDevelopment.PeasantHutCount + GetPendingBuildingCount(BuildingType.PeasantHut) < baseDevelopment.RequiredPeasantHutCount)
            {
                if (!TryStartBaseBuildingConstruction(BuildingType.PeasantHut, new BuildingCost(0, 0), "Peasant hut", out var hutPosition))
                {
                    GameDebugLog.Warning("Base", $"Peasant hut construction skipped: {baseDevelopment.LastBuildMessage}");
                    return;
                }

                GameDebugLog.Info(
                    "Base",
                    $"Peasant hut construction queued at {GameDebugLog.Position(hutPosition)}. activeBuildings={baseDevelopment.ActivePlayerBuildingCount}, huts={baseDevelopment.PeasantHutCount}, pending={GetPendingBuildingCount(BuildingType.PeasantHut)}/{baseDevelopment.RequiredPeasantHutCount}.");
            }

            RefreshSelectedHeroVisibility();
        }

        private void RebuildBaseAmbienceFromDevelopment()
        {
            if (currentMaze == null || mazeRenderer == null)
            {
                return;
            }

            baseAmbience.Initialize(currentMaze, mazeRenderer);
            cityAmbience.Initialize(currentMaze, mazeRenderer);
            RegisterExistingBuildingsForAmbience();
        }

        private void RegisterExistingBuildingsForAmbience()
        {
            foreach (var farmPosition in baseDevelopment.FarmPositions)
            {
                RegisterExistingBuilding(BuildingType.Farm, farmPosition);
            }

            foreach (var campPosition in baseDevelopment.LumberjackCampPositions)
            {
                RegisterExistingBuilding(BuildingType.LumberjackCamp, campPosition);
            }

            foreach (var housePosition in baseDevelopment.HeroHousePositions)
            {
                RegisterExistingBuilding(BuildingType.HeroHouse, housePosition);
            }

            foreach (var hutPosition in baseDevelopment.PeasantHutPositions)
            {
                RegisterExistingBuilding(BuildingType.PeasantHut, hutPosition);
            }

            if (baseDevelopment.HasAlchemistShop)
            {
                RegisterExistingBuilding(BuildingType.AlchemistShop, baseDevelopment.AlchemistShopPosition);
            }

            if (baseDevelopment.HasTavern)
            {
                RegisterExistingBuilding(BuildingType.Tavern, baseDevelopment.TavernPosition);
            }

            if (baseDevelopment.HasForge)
            {
                RegisterExistingBuilding(BuildingType.Forge, baseDevelopment.ForgePosition);
            }

            if (baseDevelopment.HasInfirmary)
            {
                RegisterExistingBuilding(BuildingType.Infirmary, baseDevelopment.InfirmaryPosition);
            }

            if (baseDevelopment.HasCartographerHouse)
            {
                RegisterExistingBuilding(BuildingType.CartographerHouse, baseDevelopment.CartographerHousePosition);
            }

            if (baseDevelopment.HasChapel)
            {
                RegisterExistingBuilding(BuildingType.Chapel, baseDevelopment.ChapelPosition);
            }

            if (baseDevelopment.HasMinersGuild)
            {
                RegisterExistingBuilding(BuildingType.MinersGuild, baseDevelopment.MinersGuildPosition);
            }

            if (baseDevelopment.HasMarket)
            {
                RegisterExistingBuilding(BuildingType.Market, baseDevelopment.MarketPosition);
            }

            if (baseDevelopment.HasAntiquary)
            {
                RegisterExistingBuilding(BuildingType.Antiquary, baseDevelopment.AntiquaryPosition);
            }

            if (baseDevelopment.HasHeroesGuild)
            {
                RegisterExistingBuilding(BuildingType.HeroesGuild, baseDevelopment.HeroesGuildPosition);
            }
        }

        private void RegisterExistingBuilding(BuildingType type, Vector2Int position)
        {
            baseAmbience.RegisterBuilding(type, position);
            cityAmbience.RegisterBuilding(type, position);
        }
    }
}
