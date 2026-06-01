using Labyrinth.Base;
using Labyrinth.Hero;
using Labyrinth.Maze;

namespace Labyrinth.Core
{
    public sealed partial class GameBootstrap
    {
        private void BuildChapelFromBase()
        {
            TryStartBaseBuildingConstruction(BuildingType.Chapel, GetChapelCost(), "Chapel", out _);
        }

        private bool CanBuildChapel()
        {
            return currentMaze != null
                && !baseDevelopment.HasChapel
                && !HasPendingBuilding(BuildingType.Chapel)
                && IsBuildingUnlocked(BuildingType.Chapel)
                && resources.CanAfford(GetChapelCost());
        }

        private string GetChapelStatus()
        {
            var status = baseDevelopment.HasChapel
                ? $"построена ({baseDevelopment.ChapelPosition.x}, {baseDevelopment.ChapelPosition.y}), 1 благословение, {BuildBlessingPriceSummary()}"
                : $"не построена, постройка {GetChapelCost().Format()}, {BuildBlessingPriceSummary()}";
            if (baseDevelopment.LastBuildMessage.Contains("часовн"))
            {
                status += $", {baseDevelopment.LastBuildMessage}";
            }

            return AppendBuildingUnlockStatus(BuildingType.Chapel, status);
        }

        private static string BuildBlessingPriceSummary()
        {
            var min = int.MaxValue;
            var max = 0;
            foreach (var blessing in HeroBlessingCatalog.PurchaseOrder)
            {
                if (blessing.GoldCost < min)
                {
                    min = blessing.GoldCost;
                }

                if (blessing.GoldCost > max)
                {
                    max = blessing.GoldCost;
                }
            }

            return $"благословения {min}-{max} зол.";
        }
    }
}
