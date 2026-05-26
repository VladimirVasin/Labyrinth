using Labyrinth.Base;
using Labyrinth.Maze;
using UnityEngine;

namespace Labyrinth.Core
{
    public sealed partial class GameBootstrap
    {
        private BuildingCost GetBuildingUpgradeCost(BuildingUpgradeType type)
        {
            return baseDevelopment.GetUpgradeCost(type);
        }

        private bool CanUpgradeBuilding(BuildingUpgradeType type)
        {
            return currentMaze != null
                && baseDevelopment.CanUpgrade(type)
                && resources.CanAfford(GetBuildingUpgradeCost(type));
        }

        private void UpgradeBuildingFromBase(BuildingUpgradeType type)
        {
            if (currentMaze == null)
            {
                return;
            }

            var cost = GetBuildingUpgradeCost(type);
            if (!baseDevelopment.HasUpgradeTarget(type))
            {
                baseDevelopment.ReportBuildBlocked($"{BaseDevelopment.GetUpgradeName(type)}: нужно построить здание");
                GameDebugLog.Warning("Base", $"Building upgrade blocked: type={type}, reason=missing-target.");
                return;
            }

            if (!resources.CanAfford(cost))
            {
                baseDevelopment.ReportBuildBlocked($"{BaseDevelopment.GetUpgradeName(type)}: нужно {cost.Format()}");
                GameDebugLog.Warning(
                    "Base",
                    $"Building upgrade blocked: type={type}, level={baseDevelopment.GetUpgradeLevel(type)}, required={cost.Format()}, gold={resources.Gold}, wood={resources.Wood}, iron={resources.Iron}.");
                return;
            }

            var previousLevel = baseDevelopment.GetUpgradeLevel(type);
            if (!baseDevelopment.TryUpgrade(type, resources))
            {
                GameDebugLog.Warning("Base", $"Building upgrade failed: type={type}, message={baseDevelopment.LastBuildMessage}.");
                return;
            }

            RefreshAllBuildingUpgradeVisuals();
            RefreshSelectedHeroVisibility();
            GameAudioController.Play(GameSfx.Build, mazeRenderer.GridToWorld(currentMaze.BasePosition), 0.9f);
            GameDebugLog.Info(
                "Base",
                $"Building upgraded: type={type}, {previousLevel}->{baseDevelopment.GetUpgradeLevel(type)}, cost={cost.Format()}, goldLeft={resources.Gold}, woodLeft={resources.Wood}, ironLeft={resources.Iron}.");
        }

        private string GetBuildingUpgradeStatus(BuildingUpgradeType type)
        {
            var level = baseDevelopment.GetUpgradeLevel(type);
            if (!baseDevelopment.HasUpgradeTarget(type))
            {
                return $"ур. {level}/3, нужно построить: {GetUpgradeEffect(type, level)}";
            }

            if (level >= 3)
            {
                return $"ур. {level}/3, максимум: {GetUpgradeEffect(type, level)}";
            }

            return $"ур. {level}/3: {GetUpgradeEffect(type, level)} -> ур. {level + 1}: {GetUpgradeEffect(type, level + 1)}";
        }

        private string GetUpgradeEffect(BuildingUpgradeType type, int level)
        {
            switch (type)
            {
                case BuildingUpgradeType.Castle:
                    return level >= 3 ? "лимит героев 8" : level >= 2 ? "лимит героев 6" : "лимит героев 5";
                case BuildingUpgradeType.Farm:
                    return level >= 3 ? "склад еды 25" : level >= 2 ? "склад еды 15" : "склад еды 10";
                case BuildingUpgradeType.LumberjackCamp:
                    return level >= 3 ? "склад дерева 15, добыча x2" : level >= 2 ? "склад дерева 15" : "склад дерева 10";
                case BuildingUpgradeType.AlchemistShop:
                    return level >= 3 ? "зелья 7 HP, запас 4" : level >= 2 ? "зелья 7 HP" : "зелья 5 HP, запас 3";
                case BuildingUpgradeType.Tavern:
                    return level >= 3 ? "пайки 12 выносл., запас 4" : level >= 2 ? "пайки 12 выносл." : "пайки 10 выносл., запас 3";
                case BuildingUpgradeType.Forge:
                    return level >= 3 ? "мастерский клинок, латный доспех" : level >= 2 ? "рыцарский меч, бригантина" : "стальной меч, кольчуга";
                default:
                    return "нет эффекта";
            }
        }

        private int GetBuildingMicroHudLevel(BuildingType buildingType)
        {
            if (baseDevelopment != null && TryGetUpgradeType(buildingType, out var upgradeType))
            {
                return baseDevelopment.GetUpgradeLevel(upgradeType);
            }

            return 1;
        }

        private void RefreshAllBuildingUpgradeVisuals()
        {
            var buildings = FindObjectsByType<BuildingView>(FindObjectsInactive.Exclude);
            for (var i = 0; i < buildings.Length; i++)
            {
                RefreshBuildingUpgradeVisual(buildings[i]);
            }
        }

        private void RefreshBuildingUpgradeVisual(BuildingView building)
        {
            if (building == null)
            {
                return;
            }

            if (!TryGetUpgradeType(building.Type, out var upgradeType))
            {
                return;
            }

            var level = baseDevelopment.GetUpgradeLevel(upgradeType);
            building.SetEffectText($"Ур. {level}: {GetUpgradeEffect(upgradeType, level)}");
            BuildingUpgradeVisuals.Apply(building, level, mazeRenderer.ModelUnitSize * 2f);
        }

        private static bool TryGetUpgradeType(BuildingType buildingType, out BuildingUpgradeType upgradeType)
        {
            switch (buildingType)
            {
                case BuildingType.Castle:
                    upgradeType = BuildingUpgradeType.Castle;
                    return true;
                case BuildingType.Farm:
                    upgradeType = BuildingUpgradeType.Farm;
                    return true;
                case BuildingType.LumberjackCamp:
                    upgradeType = BuildingUpgradeType.LumberjackCamp;
                    return true;
                case BuildingType.AlchemistShop:
                    upgradeType = BuildingUpgradeType.AlchemistShop;
                    return true;
                case BuildingType.Tavern:
                    upgradeType = BuildingUpgradeType.Tavern;
                    return true;
                case BuildingType.Forge:
                    upgradeType = BuildingUpgradeType.Forge;
                    return true;
                default:
                    upgradeType = default;
                    return false;
            }
        }
    }
}
