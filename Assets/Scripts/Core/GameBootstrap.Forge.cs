using Labyrinth.Base;
using Labyrinth.Maze;

namespace Labyrinth.Core
{
    public sealed partial class GameBootstrap
    {
        private void BuildForgeFromBase()
        {
            if (currentMaze == null)
            {
                return;
            }

            var cost = GetForgeCost();
            if (!resources.CanAfford(cost))
            {
                baseDevelopment.ReportBuildBlocked($"кузница: нужно {cost.Format()}");
                GameDebugLog.Warning("Base", $"Forge build blocked: gold={resources.Gold}, wood={resources.Wood}, required={cost.Format()}");
                return;
            }

            if (!baseDevelopment.TryBuildForge(currentMaze, out var forgePosition))
            {
                var blockMessage = baseDevelopment.LastBuildMessage;
                baseDevelopment.ReportBuildBlocked($"кузница: {blockMessage}");
                GameDebugLog.Warning("Base", $"Forge build blocked: {blockMessage}");
                return;
            }

            if (resources.TrySpend(cost))
            {
                ClearTerrainDecorationsAround(forgePosition, BaseDevelopment.ForgeFootprintRadiusCells);
                ForgeRenderer.Render(mazeRenderer, forgePosition);
                RefreshAllBuildingUpgradeVisuals();
                baseAmbience.RegisterBuilding(BuildingType.Forge, forgePosition);
                cityAmbience.RegisterBuilding(BuildingType.Forge, forgePosition);
                SyncPeasantHuts();
                RefreshSelectedHeroVisibility();
                GameAudioController.Play(GameSfx.Build, mazeRenderer.GridToWorld(forgePosition));
                GameDebugLog.Info("Base", $"Forge built at {GameDebugLog.Position(forgePosition)}. buildCost={cost.Format()}, goldLeft={resources.Gold}, woodLeft={resources.Wood}.");
            }
        }

        private bool CanBuildForge()
        {
            return currentMaze != null
                && !baseDevelopment.HasForge
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

            return status;
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
