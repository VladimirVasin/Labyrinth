using Labyrinth.Base;
using Labyrinth.Maze;

namespace Labyrinth.Core
{
    public sealed partial class GameBootstrap
    {
        private void BuildLumberjackCampFromBase()
        {
            if (currentMaze == null)
            {
                return;
            }

            var cost = GetLumberjackCampCost();
            if (!resources.CanAfford(cost))
            {
                baseDevelopment.ReportBuildBlocked($"лагерь лесорубов: нужно {cost.Format()}");
                GameDebugLog.Warning(
                    "Base",
                    $"Lumberjack camp build blocked: gold={resources.Gold}, wood={resources.Wood}, required={cost.Format()}");
                return;
            }

            if (!baseDevelopment.TryBuildLumberjackCamp(currentMaze, out var campPosition))
            {
                var blockMessage = baseDevelopment.LastBuildMessage;
                baseDevelopment.ReportBuildBlocked($"лагерь лесорубов: {blockMessage}");
                GameDebugLog.Warning("Base", $"Lumberjack camp build blocked: {blockMessage}");
                return;
            }

            if (resources.TrySpend(cost))
            {
                LumberjackCampRenderer.Render(mazeRenderer, campPosition);
                RefreshAllBuildingUpgradeVisuals();
                baseAmbience.RegisterBuilding(BuildingType.LumberjackCamp, campPosition);
                cityAmbience.RegisterBuilding(BuildingType.LumberjackCamp, campPosition);
                SyncPeasantHuts();
                RefreshSelectedHeroVisibility();
                GameAudioController.Play(GameSfx.Build, mazeRenderer.GridToWorld(campPosition));
                GameDebugLog.Info(
                    "Base",
                    $"Lumberjack camp built at {GameDebugLog.Position(campPosition)}. buildCost={cost.Format()}, goldLeft={resources.Gold}, woodLeft={resources.Wood}.");
            }
        }

        private bool CanBuildLumberjackCamp()
        {
            return currentMaze != null && resources.CanAfford(GetLumberjackCampCost());
        }

        private string GetLumberjackCampStatus()
        {
            var status = $"{baseDevelopment.LumberjackCampCount} (ур. {baseDevelopment.LumberjackCampLevel}, +{baseDevelopment.LumberjackUnitsPerTick}/{ResourceProductionController.LumberjackCampProductionIntervalSeconds:0.#} сек, караван {baseDevelopment.LumberjackBatchCapacity}, постройка {GetLumberjackCampCost().Format()})";
            if (baseDevelopment.LastBuildMessage.Contains("лесоруб")
                || baseDevelopment.LastBuildMessage.StartsWith("нужно"))
            {
                status += $", {baseDevelopment.LastBuildMessage}";
            }

            return status;
        }
    }
}
