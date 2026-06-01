using Labyrinth.Base;
using Labyrinth.Maze;

namespace Labyrinth.Core
{
    public sealed partial class GameBootstrap
    {
        private void BuildInfirmaryFromBase()
        {
            TryStartBaseBuildingConstruction(BuildingType.Infirmary, GetInfirmaryCost(), "Infirmary", out _);
        }

        private bool CanBuildInfirmary()
        {
            return currentMaze != null
                && !baseDevelopment.HasInfirmary
                && !HasPendingBuilding(BuildingType.Infirmary)
                && IsBuildingUnlocked(BuildingType.Infirmary)
                && resources.CanAfford(GetInfirmaryCost());
        }

        private string GetInfirmaryStatus()
        {
            var status = baseDevelopment.HasInfirmary
                ? $"построен ({baseDevelopment.InfirmaryPosition.x}, {baseDevelopment.InfirmaryPosition.y}), лечение 1 HP за {BaseDevelopment.InfirmaryFoodPerHitPoint} пищи + {BaseDevelopment.InfirmaryGoldPerHitPoint} зол., 1 рана за {BaseDevelopment.InfirmaryFoodPerHitPoint * 2}"
                : $"не построен, постройка {GetInfirmaryCost().Format()}, лечение 1 HP за {BaseDevelopment.InfirmaryFoodPerHitPoint} пищи + {BaseDevelopment.InfirmaryGoldPerHitPoint} зол., 1 рана за {BaseDevelopment.InfirmaryFoodPerHitPoint * 2}";
            if (baseDevelopment.LastBuildMessage.Contains("лазарет"))
            {
                status += $", {baseDevelopment.LastBuildMessage}";
            }

            return AppendBuildingUnlockStatus(BuildingType.Infirmary, status);
        }
    }
}
