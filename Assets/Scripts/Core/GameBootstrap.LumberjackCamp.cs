using Labyrinth.Base;
using Labyrinth.Maze;

namespace Labyrinth.Core
{
    public sealed partial class GameBootstrap
    {
        private void BuildLumberjackCampFromBase()
        {
            TryStartBaseBuildingConstruction(BuildingType.LumberjackCamp, GetLumberjackCampCost(), "Lumberjack camp", out _);
        }

        private bool CanBuildLumberjackCamp()
        {
            return currentMaze != null
                && IsBuildingUnlocked(BuildingType.LumberjackCamp)
                && resources.CanAfford(GetLumberjackCampCost());
        }

        private string GetLumberjackCampStatus()
        {
            var status = $"{baseDevelopment.LumberjackCampCount} (ур. {baseDevelopment.LumberjackCampLevel}, +{baseDevelopment.LumberjackUnitsPerTick}/{ResourceProductionController.LumberjackCampProductionIntervalSeconds:0.#} сек, караван {baseDevelopment.LumberjackBatchCapacity}, постройка {GetLumberjackCampCost().Format()})";
            if (baseDevelopment.LastBuildMessage.Contains("лесоруб")
                || baseDevelopment.LastBuildMessage.StartsWith("нужно"))
            {
                status += $", {baseDevelopment.LastBuildMessage}";
            }

            return AppendBuildingUnlockStatus(BuildingType.LumberjackCamp, status);
        }
    }
}
