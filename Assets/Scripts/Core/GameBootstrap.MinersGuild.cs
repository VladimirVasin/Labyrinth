using Labyrinth.Base;
using Labyrinth.Maze;

namespace Labyrinth.Core
{
    public sealed partial class GameBootstrap
    {
        private void BuildMinersGuildFromBase()
        {
            TryStartBaseBuildingConstruction(BuildingType.MinersGuild, GetMinersGuildCost(), "Miners guild", out _);
        }

        private bool CanBuildMinersGuild()
        {
            return currentMaze != null
                && !baseDevelopment.HasMinersGuild
                && !HasPendingBuilding(BuildingType.MinersGuild)
                && IsBuildingUnlocked(BuildingType.MinersGuild)
                && resources.CanAfford(GetMinersGuildCost());
        }

        private string GetMinersGuildStatus()
        {
            var status = baseDevelopment.HasMinersGuild
                ? $"построена ({baseDevelopment.MinersGuildPosition.x}, {baseDevelopment.MinersGuildPosition.y}), доступны шахты"
                : $"не построена, постройка {GetMinersGuildCost().Format()}, открывает шахты";
            if (baseDevelopment.LastBuildMessage.Contains("гильдия шахтёров"))
            {
                status += $", {baseDevelopment.LastBuildMessage}";
            }

            return AppendBuildingUnlockStatus(BuildingType.MinersGuild, status);
        }
    }
}
