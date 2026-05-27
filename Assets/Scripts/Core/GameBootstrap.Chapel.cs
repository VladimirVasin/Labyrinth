using Labyrinth.Base;
using Labyrinth.Hero;
using Labyrinth.Maze;

namespace Labyrinth.Core
{
    public sealed partial class GameBootstrap
    {
        private void BuildChapelFromBase()
        {
            if (currentMaze == null)
            {
                return;
            }

            var cost = GetChapelCost();
            if (!resources.CanAfford(cost))
            {
                baseDevelopment.ReportBuildBlocked($"часовня: нужно {cost.Format()}");
                GameDebugLog.Warning("Base", $"Chapel build blocked: gold={resources.Gold}, wood={resources.Wood}, required={cost.Format()}");
                return;
            }

            if (!baseDevelopment.TryBuildChapel(currentMaze, out var chapelPosition))
            {
                var blockMessage = baseDevelopment.LastBuildMessage;
                baseDevelopment.ReportBuildBlocked($"часовня: {blockMessage}");
                GameDebugLog.Warning("Base", $"Chapel build blocked: {blockMessage}");
                return;
            }

            if (!resources.TrySpend(cost))
            {
                baseDevelopment.ReportBuildBlocked($"часовня: нужно {cost.Format()}");
                return;
            }

            ClearTerrainDecorationsAround(chapelPosition, BaseDevelopment.ChapelFootprintRadiusCells);
            ChapelRenderer.Render(mazeRenderer, chapelPosition);
            baseAmbience.RegisterBuilding(BuildingType.Chapel, chapelPosition);
            cityAmbience.RegisterBuilding(BuildingType.Chapel, chapelPosition);
            SyncPeasantHuts();
            RefreshSelectedHeroVisibility();
            GameAudioController.Play(GameSfx.Build, mazeRenderer.GridToWorld(chapelPosition));
            GameDebugLog.Info(
                "Base",
                $"Chapel built at {GameDebugLog.Position(chapelPosition)}. buildCost={cost.Format()}, goldLeft={resources.Gold}, woodLeft={resources.Wood}, blessingPrices={BuildBlessingPriceSummary()}.");
        }

        private bool CanBuildChapel()
        {
            return currentMaze != null
                && !baseDevelopment.HasChapel
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

            return status;
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
