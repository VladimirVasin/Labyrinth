using System;
using Labyrinth.Base;
using Labyrinth.Hero;
using Labyrinth.Maze;
using Labyrinth.UI;

namespace Labyrinth.Core
{
    public sealed partial class GameBootstrap
    {
        private void BuildHeroesGuildFromBase()
        {
            if (currentMaze == null)
            {
                return;
            }

            var cost = GetHeroesGuildCost();
            if (!resources.CanAfford(cost))
            {
                baseDevelopment.ReportBuildBlocked($"гильдия героев: нужно {cost.Format()}");
                GameDebugLog.Warning("Base", $"Heroes guild build blocked: gold={resources.Gold}, wood={resources.Wood}, required={cost.Format()}");
                return;
            }

            if (!baseDevelopment.TryBuildHeroesGuild(currentMaze, out var guildPosition))
            {
                var blockMessage = baseDevelopment.LastBuildMessage;
                baseDevelopment.ReportBuildBlocked($"гильдия героев: {blockMessage}");
                GameDebugLog.Warning("Base", $"Heroes guild build blocked: {blockMessage}");
                return;
            }

            if (!resources.TrySpend(cost))
            {
                baseDevelopment.ReportBuildBlocked($"гильдия героев: нужно {cost.Format()}");
                return;
            }

            ClearTerrainDecorationsAround(guildPosition, BaseDevelopment.HeroesGuildFootprintRadiusCells);
            heroesGuildView = HeroesGuildRenderer.Render(mazeRenderer, guildPosition);
            heroGuildQuestController.SetGuildView(heroesGuildView);
            baseAmbience.RegisterBuilding(BuildingType.HeroesGuild, guildPosition);
            cityAmbience.RegisterBuilding(BuildingType.HeroesGuild, guildPosition);
            SyncPeasantHuts();
            RefreshSelectedHeroVisibility();
            GameAudioController.Play(GameSfx.Build, mazeRenderer.GridToWorld(guildPosition));
            GameDebugLog.Info(
                "Base",
                $"Heroes guild built at {GameDebugLog.Position(guildPosition)}. buildCost={cost.Format()}, goldLeft={resources.Gold}, woodLeft={resources.Wood}.");
        }

        private bool CanBuildHeroesGuild()
        {
            return currentMaze != null
                && !baseDevelopment.HasHeroesGuild
                && resources.CanAfford(GetHeroesGuildCost());
        }

        private string GetHeroesGuildStatus()
        {
            var status = baseDevelopment.HasHeroesGuild
                ? heroGuildQuestController.GetStatusText()
                : $"не построена, постройка {GetHeroesGuildCost().Format()}, открывает контракты зачистки";
            if (baseDevelopment.LastBuildMessage.Contains("гильдия героев"))
            {
                status += $", {baseDevelopment.LastBuildMessage}";
            }

            return status;
        }

        private BuildingServiceEntry[] GetHeroesGuildServiceEntries()
        {
            return heroGuildQuestController != null
                ? heroGuildQuestController.BuildServiceEntries(selectedHero)
                : Array.Empty<BuildingServiceEntry>();
        }

        private HeroGuildQuestHudInfo GetHeroGuildQuestHudInfo(HeroController hero)
        {
            return heroGuildQuestController != null
                ? heroGuildQuestController.GetHeroQuestHudInfo(hero)
                : HeroGuildQuestHudInfo.None;
        }

        private void TryAssignHeroesGuildQuest(int serviceIndex)
        {
            heroGuildQuestController?.TryAssignQuest(serviceIndex, selectedHero);
        }

        private bool GetBuildingQuestGenerationToggle(BuildingType buildingType)
        {
            return buildingType != BuildingType.HeroesGuild
                || heroGuildQuestController == null
                || heroGuildQuestController.AutoGenerateQuests;
        }

        private void SetBuildingQuestGenerationToggle(BuildingType buildingType, bool enabled)
        {
            if (buildingType != BuildingType.HeroesGuild)
            {
                return;
            }

            heroGuildQuestController?.SetAutoGenerateQuests(enabled);
        }
    }
}
