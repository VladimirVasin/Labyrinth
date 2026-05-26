using Labyrinth.Base;
using Labyrinth.Maze;

namespace Labyrinth.Core
{
    public sealed partial class GameBootstrap
    {
        private void BuildCartographerHouseFromBase()
        {
            if (currentMaze == null)
            {
                return;
            }

            var cost = GetCartographerHouseCost();
            if (!resources.CanAfford(cost))
            {
                baseDevelopment.ReportBuildBlocked($"дом картографа: нужно {cost.Format()}");
                GameDebugLog.Warning("Base", $"Cartographer house build blocked: gold={resources.Gold}, wood={resources.Wood}, required={cost.Format()}");
                return;
            }

            if (!baseDevelopment.TryBuildCartographerHouse(currentMaze, out var housePosition))
            {
                var blockMessage = baseDevelopment.LastBuildMessage;
                baseDevelopment.ReportBuildBlocked($"дом картографа: {blockMessage}");
                GameDebugLog.Warning("Base", $"Cartographer house build blocked: {blockMessage}");
                return;
            }

            if (!resources.TrySpend(cost))
            {
                baseDevelopment.ReportBuildBlocked($"дом картографа: нужно {cost.Format()}");
                return;
            }

            CartographerHouseRenderer.Render(mazeRenderer, housePosition);
            baseAmbience.RegisterBuilding(BuildingType.CartographerHouse, housePosition);
            cityAmbience.RegisterBuilding(BuildingType.CartographerHouse, housePosition);
            SyncPeasantHuts();
            RefreshSelectedHeroVisibility();
            GameAudioController.Play(GameSfx.Build, mazeRenderer.GridToWorld(housePosition));
            GameDebugLog.Info(
                "Cartographer",
                $"Cartographer house built at {GameDebugLog.Position(housePosition)}. buildCost={cost.Format()}, goldLeft={resources.Gold}, woodLeft={resources.Wood}.");
        }

        private bool CanBuildCartographerHouse()
        {
            return currentMaze != null
                && !baseDevelopment.HasCartographerHouse
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

            return status;
        }
    }
}
