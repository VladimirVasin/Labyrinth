using Labyrinth.Base;
using Labyrinth.Hero;
using Labyrinth.Maze;

namespace Labyrinth.Core
{
    public sealed partial class GameBootstrap
    {
        private void BuildAntiquaryFromBase()
        {
            TryStartBaseBuildingConstruction(BuildingType.Antiquary, GetAntiquaryCost(), "Antiquary", out _);
        }

        private bool CanBuildAntiquary()
        {
            return currentMaze != null
                && !baseDevelopment.HasAntiquary
                && !HasPendingBuilding(BuildingType.Antiquary)
                && IsBuildingUnlocked(BuildingType.Antiquary)
                && resources.CanAfford(GetAntiquaryCost());
        }

        private string GetAntiquaryStatus()
        {
            var status = baseDevelopment.HasAntiquary
                ? $"построен ({baseDevelopment.AntiquaryPosition.x}, {baseDevelopment.AntiquaryPosition.y}), {HeroInventory.ReturnStoneItemName} {BaseDevelopment.ReturnStoneGoldCost} зол."
                : $"не построен, постройка {GetAntiquaryCost().Format()}, {HeroInventory.ReturnStoneItemName} {BaseDevelopment.ReturnStoneGoldCost} зол.";
            if (baseDevelopment.LastBuildMessage.Contains("антиквариат"))
            {
                status += $", {baseDevelopment.LastBuildMessage}";
            }

            return AppendBuildingUnlockStatus(BuildingType.Antiquary, status);
        }
    }
}
