using Labyrinth.Base;
using Labyrinth.Hero;
using Labyrinth.Maze;

namespace Labyrinth.Core
{
    public sealed partial class GameBootstrap
    {
        private void BuildAntiquaryFromBase()
        {
            if (currentMaze == null)
            {
                return;
            }

            var cost = GetAntiquaryCost();
            if (!resources.CanAfford(cost))
            {
                baseDevelopment.ReportBuildBlocked($"антиквариат: нужно {cost.Format()}");
                GameDebugLog.Warning("Base", $"Antiquary build blocked: gold={resources.Gold}, wood={resources.Wood}, required={cost.Format()}");
                return;
            }

            if (!baseDevelopment.TryBuildAntiquary(currentMaze, out var antiquaryPosition))
            {
                var blockMessage = baseDevelopment.LastBuildMessage;
                baseDevelopment.ReportBuildBlocked($"антиквариат: {blockMessage}");
                GameDebugLog.Warning("Base", $"Antiquary build blocked: {blockMessage}");
                return;
            }

            if (!resources.TrySpend(cost))
            {
                baseDevelopment.ReportBuildBlocked($"антиквариат: нужно {cost.Format()}");
                return;
            }

            ClearTerrainDecorationsAround(antiquaryPosition, BaseDevelopment.AntiquaryFootprintRadiusCells);
            AntiquaryRenderer.Render(mazeRenderer, antiquaryPosition);
            baseAmbience.RegisterBuilding(BuildingType.Antiquary, antiquaryPosition);
            cityAmbience.RegisterBuilding(BuildingType.Antiquary, antiquaryPosition);
            SyncPeasantHuts();
            RefreshSelectedHeroVisibility();
            GameAudioController.Play(GameSfx.Build, mazeRenderer.GridToWorld(antiquaryPosition));
            GameDebugLog.Info(
                "Base",
                $"Antiquary built at {GameDebugLog.Position(antiquaryPosition)}. buildCost={cost.Format()}, goldLeft={resources.Gold}, woodLeft={resources.Wood}, returnStoneCost={BaseDevelopment.ReturnStoneGoldCost}.");
        }

        private bool CanBuildAntiquary()
        {
            return currentMaze != null
                && !baseDevelopment.HasAntiquary
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

            return status;
        }
    }
}
