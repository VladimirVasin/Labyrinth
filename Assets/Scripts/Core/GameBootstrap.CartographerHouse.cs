using Labyrinth.Base;
using Labyrinth.Maze;

namespace Labyrinth.Core
{
    public sealed partial class GameBootstrap
    {
        private void BuildCartographerHouseFromBase()
        {
            TryStartBaseBuildingConstruction(BuildingType.CartographerHouse, GetCartographerHouseCost(), "Cartographer house", out _);
        }

        private bool CanBuildCartographerHouse()
        {
            return currentMaze != null
                && !baseDevelopment.HasCartographerHouse
                && !HasPendingBuilding(BuildingType.CartographerHouse)
                && IsBuildingUnlocked(BuildingType.CartographerHouse)
                && resources.CanAfford(GetCartographerHouseCost());
        }

        private string GetCartographerHouseStatus()
        {
            var status = baseDevelopment.HasCartographerHouse
                ? $"построен ({baseDevelopment.CartographerHousePosition.x}, {baseDevelopment.CartographerHousePosition.y}), общая карта {cartographerMemory?.KnownCellCount ?? 0} клеток"
                : $"не построен, постройка {GetCartographerHouseCost().Format()}, обмен знаниями у входа";
            if (baseDevelopment.LastBuildMessage.Contains("картограф"))
            {
                status += $", {baseDevelopment.LastBuildMessage}";
            }

            return AppendBuildingUnlockStatus(BuildingType.CartographerHouse, status);
        }
    }
}
