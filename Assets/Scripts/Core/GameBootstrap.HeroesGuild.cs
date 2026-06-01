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
            TryStartBaseBuildingConstruction(BuildingType.HeroesGuild, GetHeroesGuildCost(), "Heroes guild", out _);
        }

        private bool CanBuildHeroesGuild()
        {
            return currentMaze != null
                && !baseDevelopment.HasHeroesGuild
                && !HasPendingBuilding(BuildingType.HeroesGuild)
                && IsBuildingUnlocked(BuildingType.HeroesGuild)
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

            return AppendBuildingUnlockStatus(BuildingType.HeroesGuild, status);
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
