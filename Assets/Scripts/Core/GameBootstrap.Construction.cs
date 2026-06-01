using Labyrinth.Base;
using Labyrinth.Maze;
using System.Collections.Generic;
using UnityEngine;

namespace Labyrinth.Core
{
    public sealed partial class GameBootstrap
    {
        private readonly Dictionary<Vector2Int, int> pendingConstructionPayloads = new Dictionary<Vector2Int, int>();

        private bool TryStartBaseBuildingConstruction(
            BuildingType type,
            BuildingCost cost,
            string logName,
            out Vector2Int position,
            int payload = 0)
        {
            position = Vector2Int.zero;
            if (currentMaze == null || baseConstructionController == null)
            {
                return false;
            }

            if (baseDevelopment != null && !baseDevelopment.IsBuildingUnlocked(type))
            {
                var unlockHint = baseDevelopment.GetBuildingUnlockHint(type);
                baseDevelopment.ReportBuildBlocked($"{logName}: {unlockHint}");
                GameDebugLog.Warning("Base", $"{logName} construction blocked: locked, hint={unlockHint}.");
                return false;
            }

            if (!resources.CanAfford(cost))
            {
                baseDevelopment.ReportBuildBlocked($"{logName}: need {cost.Format()}");
                GameDebugLog.Warning(
                    "Base",
                    $"{logName} construction blocked: gold={resources.Gold}, wood={resources.Wood}, food={resources.Food}, iron={resources.Iron}, required={cost.Format()}.");
                return false;
            }

            if (!baseDevelopment.TryReserveBuildingSite(currentMaze, type, out position))
            {
                var blockMessage = baseDevelopment.LastBuildMessage;
                baseDevelopment.ReportBuildBlocked($"{logName}: {blockMessage}");
                GameDebugLog.Warning("Base", $"{logName} construction blocked: {blockMessage}");
                return false;
            }

            if (!resources.TrySpend(cost))
            {
                baseDevelopment.CancelReservedBuilding(type, position);
                baseDevelopment.ReportBuildBlocked($"{logName}: need {cost.Format()}");
                return false;
            }

            var footprint = BaseDevelopment.GetFootprintRadius(type);
            ClearTerrainDecorationsAround(position, footprint);
            pendingConstructionPayloads[position] = payload;
            baseConstructionController.PlaceConstructionSite(type, position, footprint, payload);
            baseAmbience.RegisterBuilding(type, position);
            if (baseAmbience.HasCompletedRoad(type, position))
            {
                baseConstructionController.BeginConstructionWork(type, position);
            }

            RefreshSelectedHeroVisibility();
            GameAudioController.Play(GameSfx.Build, mazeRenderer.GridToWorld(position), 0.72f);
            GameDebugLog.Info(
                "Base",
                $"{logName} construction site placed at {GameDebugLog.Position(position)}. Road must complete before building work starts. type={type}, cost={cost.Format()}, goldLeft={resources.Gold}, woodLeft={resources.Wood}, foodLeft={resources.Food}, ironLeft={resources.Iron}.");
            return true;
        }

        private void HandleBaseRoadCompleted(BuildingType type, Vector2Int position)
        {
            if (baseConstructionController == null || !HasPendingBuildingSite(type, position))
            {
                return;
            }

            if (baseConstructionController.BeginConstructionWork(type, position))
            {
                GameDebugLog.Info(
                    "Base",
                    $"Construction road ready: type={type}, position={GameDebugLog.Position(position)}. Building worker dispatched.");
            }
        }

        private void HandleBaseConstructionCompleted(BuildingType type, Vector2Int position, int payload)
        {
            if (currentMaze == null || mazeRenderer == null)
            {
                return;
            }

            if (!baseDevelopment.TryCompleteReservedBuilding(type, position))
            {
                GameDebugLog.Warning(
                    "Base",
                    $"Construction completion ignored: type={type}, position={GameDebugLog.Position(position)}, message={baseDevelopment.LastBuildMessage}.");
                return;
            }

            pendingConstructionPayloads.Remove(position);
            if (type == BuildingType.HeroHouse)
            {
                CompleteHeroHouseConstruction(payload, position);
                return;
            }

            var view = RenderCompletedBaseBuilding(type, position);
            if (type == BuildingType.PeasantHut && view != null)
            {
                taxCollectorController.RegisterHut(position, view);
            }

            RegisterCompletedBaseBuilding(type, position);
            RefreshAllBuildingUpgradeVisuals();
            SyncPeasantHuts();
            RefreshSelectedHeroVisibility();
            GameAudioController.Play(GameSfx.Build, mazeRenderer.GridToWorld(position));
            GameDebugLog.Info(
                "Base",
                $"Construction completed: type={type}, position={GameDebugLog.Position(position)}, activeBuildings={baseDevelopment.ActivePlayerBuildingCount}.");
        }

        private BuildingView RenderCompletedBaseBuilding(BuildingType type, Vector2Int position)
        {
            switch (type)
            {
                case BuildingType.Farm:
                    return mazeRenderer.RenderFarm(position);
                case BuildingType.LumberjackCamp:
                    return LumberjackCampRenderer.Render(mazeRenderer, position);
                case BuildingType.PeasantHut:
                    return PeasantHutRenderer.Render(mazeRenderer, position);
                case BuildingType.AlchemistShop:
                    return mazeRenderer.RenderAlchemistShop(position);
                case BuildingType.Tavern:
                    return mazeRenderer.RenderTavern(position);
                case BuildingType.Forge:
                    return ForgeRenderer.Render(mazeRenderer, position);
                case BuildingType.Infirmary:
                    return InfirmaryRenderer.Render(mazeRenderer, position);
                case BuildingType.CartographerHouse:
                    return CartographerHouseRenderer.Render(mazeRenderer, position);
                case BuildingType.Chapel:
                    return ChapelRenderer.Render(mazeRenderer, position);
                case BuildingType.MinersGuild:
                    return MinersGuildRenderer.Render(mazeRenderer, position);
                case BuildingType.Market:
                    return MarketRenderer.Render(mazeRenderer, position);
                case BuildingType.Antiquary:
                    return AntiquaryRenderer.Render(mazeRenderer, position);
                case BuildingType.HeroesGuild:
                    heroesGuildView = HeroesGuildRenderer.Render(mazeRenderer, position);
                    heroGuildQuestController.SetGuildView(heroesGuildView);
                    return heroesGuildView;
                default:
                    return null;
            }
        }

        private void RegisterCompletedBaseBuilding(BuildingType type, Vector2Int position)
        {
            if (type == BuildingType.Castle)
            {
                return;
            }

            baseAmbience.RegisterBuilding(type, position);
            cityAmbience.RegisterBuilding(type, position);
        }

        private bool HasPendingBuilding(BuildingType type)
        {
            return baseDevelopment != null && baseDevelopment.HasPendingBuilding(type);
        }

        private bool IsBuildingUnlocked(BuildingType type)
        {
            return baseDevelopment == null || baseDevelopment.IsBuildingUnlocked(type);
        }

        private string AppendBuildingUnlockStatus(BuildingType type, string status)
        {
            if (baseDevelopment == null || baseDevelopment.IsBuildingUnlocked(type))
            {
                return status;
            }

            return $"{status}, заблокировано: {baseDevelopment.GetBuildingUnlockHint(type)}";
        }

        private int GetPendingBuildingCount(BuildingType type)
        {
            return baseDevelopment != null ? baseDevelopment.GetPendingBuildingCount(type) : 0;
        }

        private void RestorePendingBaseConstructionSites()
        {
            if (baseDevelopment == null || baseConstructionController == null)
            {
                return;
            }

            foreach (var site in baseDevelopment.PendingBuildingSites)
            {
                pendingConstructionPayloads.TryGetValue(site.Position, out var payload);
                baseConstructionController.PlaceConstructionSite(site.Type, site.Position, site.FootprintRadius, payload);
                baseAmbience.RegisterBuilding(site.Type, site.Position);
                if (baseAmbience.HasCompletedRoad(site.Type, site.Position))
                {
                    baseConstructionController.BeginConstructionWork(site.Type, site.Position);
                }
            }
        }

        private bool HasPendingBuildingSite(BuildingType type, Vector2Int position)
        {
            if (baseDevelopment == null)
            {
                return false;
            }

            foreach (var site in baseDevelopment.PendingBuildingSites)
            {
                if (site.Type == type && site.Position == position)
                {
                    return true;
                }
            }

            return false;
        }

        private void ClearBaseConstructionPayloads()
        {
            pendingConstructionPayloads.Clear();
        }
    }
}
