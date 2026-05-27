using Labyrinth.Base;
using Labyrinth.Maze;

namespace Labyrinth.Core
{
    public sealed partial class GameBootstrap
    {
        private void BuildMinersGuildFromBase()
        {
            if (currentMaze == null)
            {
                return;
            }

            var cost = GetMinersGuildCost();
            if (!resources.CanAfford(cost))
            {
                baseDevelopment.ReportBuildBlocked($"гильдия шахтёров: нужно {cost.Format()}");
                GameDebugLog.Warning("Base", $"Miners guild build blocked: gold={resources.Gold}, wood={resources.Wood}, required={cost.Format()}");
                return;
            }

            if (!baseDevelopment.TryBuildMinersGuild(currentMaze, out var guildPosition))
            {
                var blockMessage = baseDevelopment.LastBuildMessage;
                baseDevelopment.ReportBuildBlocked($"гильдия шахтёров: {blockMessage}");
                GameDebugLog.Warning("Base", $"Miners guild build blocked: {blockMessage}");
                return;
            }

            if (!resources.TrySpend(cost))
            {
                baseDevelopment.ReportBuildBlocked($"гильдия шахтёров: нужно {cost.Format()}");
                return;
            }

            ClearTerrainDecorationsAround(guildPosition, BaseDevelopment.MinersGuildFootprintRadiusCells);
            MinersGuildRenderer.Render(mazeRenderer, guildPosition);
            baseAmbience.RegisterBuilding(BuildingType.MinersGuild, guildPosition);
            cityAmbience.RegisterBuilding(BuildingType.MinersGuild, guildPosition);
            SyncPeasantHuts();
            RefreshSelectedHeroVisibility();
            GameAudioController.Play(GameSfx.Build, mazeRenderer.GridToWorld(guildPosition));
            GameDebugLog.Info(
                "Base",
                $"Miners guild built at {GameDebugLog.Position(guildPosition)}. buildCost={cost.Format()}, goldLeft={resources.Gold}, woodLeft={resources.Wood}.");
        }

        private bool CanBuildMinersGuild()
        {
            return currentMaze != null
                && !baseDevelopment.HasMinersGuild
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

            return status;
        }
    }
}
