using Labyrinth.Base;
using Labyrinth.Maze;

namespace Labyrinth.Core
{
    public sealed partial class GameBootstrap
    {
        private void BuildForgeFromBase()
        {
            TryStartBaseBuildingConstruction(BuildingType.Forge, GetForgeCost(), "Forge", out _);
        }

        private bool CanBuildForge()
        {
            return currentMaze != null
                && !baseDevelopment.HasForge
                && !HasPendingBuilding(BuildingType.Forge)
                && IsBuildingUnlocked(BuildingType.Forge)
                && resources.CanAfford(GetForgeCost());
        }

        private string GetForgeStatus()
        {
            var status = baseDevelopment.HasForge
                ? $"построена ({baseDevelopment.ForgePosition.x}, {baseDevelopment.ForgePosition.y}), ур. {baseDevelopment.ForgeLevel}, {GetForgeLevelText()}"
                : $"не построена, постройка {GetForgeCost().Format()}, снаряжение от {BaseDevelopment.LeatherBootsGoldCost} зол.";
            if (baseDevelopment.LastBuildMessage.Contains("кузниц"))
            {
                status += $", {baseDevelopment.LastBuildMessage}";
            }

            return AppendBuildingUnlockStatus(BuildingType.Forge, status);
        }

        private string GetForgeLevelText()
        {
            if (baseDevelopment.ForgeLevel >= 3)
            {
                return $"ур. 3: клинок {BaseDevelopment.MasterBladeGoldCost}, доспех {BaseDevelopment.PlateHarnessGoldCost}, сапоги {BaseDevelopment.SwiftwalkerBootsGoldCost} зол.";
            }

            if (baseDevelopment.ForgeLevel >= 2)
            {
                return $"ур. 2: меч {BaseDevelopment.KnightSwordGoldCost}, броня {BaseDevelopment.BrigandineGoldCost}, сапоги {BaseDevelopment.PathfinderBootsGoldCost} зол.";
            }

            return $"ур. 1: меч {BaseDevelopment.SteelSwordGoldCost}, броня {BaseDevelopment.ChainmailGoldCost}, сапоги {BaseDevelopment.LeatherBootsGoldCost} зол.";
        }
    }
}
