using Labyrinth.Base;
using Labyrinth.Maze;

namespace Labyrinth.Core
{
    public sealed partial class GameBootstrap
    {
        private void BuildInfirmaryFromBase()
        {
            if (currentMaze == null)
            {
                return;
            }

            var cost = GetInfirmaryCost();
            if (!resources.CanAfford(cost))
            {
                baseDevelopment.ReportBuildBlocked($"лазарет: нужно {cost.Format()}");
                GameDebugLog.Warning("Base", $"Infirmary build blocked: gold={resources.Gold}, wood={resources.Wood}, required={cost.Format()}");
                return;
            }

            if (!baseDevelopment.TryBuildInfirmary(currentMaze, out var infirmaryPosition))
            {
                var blockMessage = baseDevelopment.LastBuildMessage;
                baseDevelopment.ReportBuildBlocked($"лазарет: {blockMessage}");
                GameDebugLog.Warning("Base", $"Infirmary build blocked: {blockMessage}");
                return;
            }

            if (!resources.TrySpend(cost))
            {
                baseDevelopment.ReportBuildBlocked($"лазарет: нужно {cost.Format()}");
                return;
            }

            ClearTerrainDecorationsAround(infirmaryPosition, BaseDevelopment.InfirmaryFootprintRadiusCells);
            InfirmaryRenderer.Render(mazeRenderer, infirmaryPosition);
            baseAmbience.RegisterBuilding(BuildingType.Infirmary, infirmaryPosition);
            cityAmbience.RegisterBuilding(BuildingType.Infirmary, infirmaryPosition);
            SyncPeasantHuts();
            RefreshSelectedHeroVisibility();
            GameAudioController.Play(GameSfx.Build, mazeRenderer.GridToWorld(infirmaryPosition));
            GameDebugLog.Info(
                "Base",
                $"Infirmary built at {GameDebugLog.Position(infirmaryPosition)}. buildCost={cost.Format()}, goldLeft={resources.Gold}, woodLeft={resources.Wood}, healFoodPerHp={BaseDevelopment.InfirmaryFoodPerHitPoint}, healGoldPerHp={BaseDevelopment.InfirmaryGoldPerHitPoint}.");
        }

        private bool CanBuildInfirmary()
        {
            return currentMaze != null
                && !baseDevelopment.HasInfirmary
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

            return status;
        }
    }
}
