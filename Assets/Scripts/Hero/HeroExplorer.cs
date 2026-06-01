using System;
using System.Collections.Generic;
using Labyrinth.Combat;
using Labyrinth.Core;
using Labyrinth.Maze;
using UnityEngine;

namespace Labyrinth.Hero
{
    public sealed partial class HeroExplorer
    {
        private delegate bool EquipmentEquipHandler(out string previousItem);
        public delegate bool NearbyMobInteractionCellProvider(
            Vector2Int heroPosition,
            Vector2Int interactionCell,
            int radius);
        public delegate bool PriorityDungeonTargetProvider(
            HeroModel hero,
            out Vector2Int targetCell,
            out string label);

        private const int StaminaPerNewCell = 1;
        private const int DuplicateEquipmentGoldCompensation = 5;
        private const int ExplorationProgressLogStep = 12;

        private readonly MazeGenerationResult result;
        private readonly MazeGrid grid;
        private readonly MazeRenderer mazeRenderer;
        private readonly GoldIngotManager goldIngotManager;
        private readonly HeroDeathTokenManager deathTokenManager;
        private readonly HeroModel model;
        private readonly Vector2Int entrancePosition;
        private readonly Dictionary<Vector2Int, int> distancesFromEntrance;
        private readonly int maxDistanceFromEntrance;
        private readonly int heroNumber;
        private readonly Action<HeroModel, int> entranceKnowledgeSync;
        private readonly Action<HeroModel, int, DungeonStairsModel> downStairsOpened;
        private readonly NearbyMobInteractionCellProvider nearbyMobInteractionCellProvider;
        private readonly PriorityDungeonTargetProvider priorityDungeonTargetProvider;
        private readonly HeroExplorationCoordinator explorationCoordinator;
        private readonly HashSet<Vector2Int> doorPathWarningPositions = new HashSet<Vector2Int>();
        private Queue<Vector2Int> returnPath = new Queue<Vector2Int>();
        private Queue<Vector2Int> doorPath = new Queue<Vector2Int>();
        private Queue<Vector2Int> patrolPath = new Queue<Vector2Int>();
        private Queue<Vector2Int> priorityTargetPath = new Queue<Vector2Int>();
        private Vector2Int priorityTargetCell;
        private string priorityTargetLabel = string.Empty;
        private CentralDoorModel targetDoor;
        private DungeonStairsModel targetStairs;
        private int nextExplorationProgressLog = ExplorationProgressLogStep;

        public HeroExplorer(
            MazeGenerationResult result,
            HeroModel model,
            Vector2Int entrancePosition,
            int heroNumber,
            MazeRenderer mazeRenderer,
            GoldIngotManager goldIngotManager,
            HeroDeathTokenManager deathTokenManager,
            Action<HeroModel, int> entranceKnowledgeSync,
            Action<HeroModel, int, DungeonStairsModel> downStairsOpened,
            NearbyMobInteractionCellProvider nearbyMobInteractionCellProvider,
            PriorityDungeonTargetProvider priorityDungeonTargetProvider,
            HeroExplorationCoordinator explorationCoordinator)
        {
            this.result = result;
            grid = result.Grid;
            this.mazeRenderer = mazeRenderer;
            this.goldIngotManager = goldIngotManager;
            this.deathTokenManager = deathTokenManager;
            this.model = model;
            this.entrancePosition = entrancePosition;
            distancesFromEntrance = MazeValidation.GetReachableDistances(grid, entrancePosition, true);
            maxDistanceFromEntrance = CalculateMaxEntranceDistance(distancesFromEntrance);
            this.heroNumber = heroNumber;
            this.entranceKnowledgeSync = entranceKnowledgeSync;
            this.downStairsOpened = downStairsOpened;
            this.nearbyMobInteractionCellProvider = nearbyMobInteractionCellProvider;
            this.priorityDungeonTargetProvider = priorityDungeonTargetProvider;
            this.explorationCoordinator = explorationCoordinator;
        }

        public void ReleaseExplorationTarget(string reason)
        {
            explorationCoordinator?.Release(heroNumber, reason);
        }

        public void Step()
        {
            if (model.State == HeroState.OpeningDoor)
            {
                model.SetState(HeroState.Exploring);
                return;
            }

            if (model.State == HeroState.ReturningToDoor)
            {
                if (TryPursueNearbyInteraction())
                {
                    doorPath.Clear();
                    patrolPath.Clear();
                    priorityTargetPath.Clear();
                    return;
                }

                StepReturnToDoor();
                return;
            }

            if (model.State == HeroState.ReturningToCastle)
            {
                if (model.Position != entrancePosition && TryPursueNearbyInteraction())
                {
                    returnPath.Clear();
                    patrolPath.Clear();
                    priorityTargetPath.Clear();
                    return;
                }

                StepReturnToCastle();
                return;
            }

            if (model.State != HeroState.Exploring && model.State != HeroState.SearchingKey)
            {
                return;
            }

            model.Memory.Remember(model.Position);
            if (TryCollectKey())
            {
                return;
            }

            if (TryHandleDeathTokenOnCurrentCell())
            {
                return;
            }

            if (TryHandleGoldIngotOnCurrentCell())
            {
                return;
            }

            if (TryOpenChestInCurrentCave())
            {
                return;
            }

            if (TryOpenAdjacentKnownDoor())
            {
                return;
            }

            if (TryOpenAdjacentKnownStairs())
            {
                return;
            }

            if (TryPursueNearbyInteraction())
            {
                ReleaseExplorationTarget("nearby interaction");
                patrolPath.Clear();
                priorityTargetPath.Clear();
                return;
            }

            if (HasDescentKey() && TryBeginReturnToKnownStairs())
            {
                return;
            }

            if (HasCentralRoomKey() && TryBeginReturnToKnownDoor())
            {
                return;
            }

            if (TryRememberAdjacentClosedDoor(out var adjacentDoor))
            {
                if (HasCentralRoomKey())
                {
                    OpenDoor(adjacentDoor);
                    return;
                }

                model.SetState(HeroState.SearchingKey);
                return;
            }

            if (TryRememberAdjacentLockedStairs(out var adjacentStairs))
            {
                if (HasDescentKey())
                {
                    OpenStairs(adjacentStairs);
                    return;
                }

                model.SetState(HeroState.SearchingKey);
                return;
            }

            if (model.Stamina <= 0)
            {
                BeginReturnToCastle();
                StepReturnToCastle();
                return;
            }

            if (TryFindUnrememberedNeighbor(model.Position, out var next))
            {
                patrolPath.Clear();
                priorityTargetPath.Clear();
                MoveToNewCell(next);
                return;
            }

            if (TryBuildPathToNearestFrontier(out var frontierPath) && frontierPath.Count > 0)
            {
                patrolPath.Clear();
                priorityTargetPath.Clear();
                MoveAlongRememberedPath(frontierPath.Dequeue());
                return;
            }

            if (TryContinuePriorityDungeonTarget() || TryBeginPriorityDungeonTargetFallback())
            {
                return;
            }

            if (TryContinuePatrolFallback())
            {
                return;
            }

            HandleNoFrontierFallback();
        }

        public bool TryUseReturnStoneToEntrance()
        {
            if (model == null
                || !model.IsAlive
                || (model.State != HeroState.ReturningToCastle && model.State != HeroState.Stuck)
                || model.Position == entrancePosition
                || model.Inventory == null
                || !model.Inventory.TryConsumeReturnStone())
            {
                return false;
            }

            var from = model.Position;
            model.SetPosition(entrancePosition);
            model.Memory.Remember(entrancePosition);
            RestoreAtCastle();
            GameDebugLog.Info(
                "Hero",
                $"{HeroLogName} used {HeroInventory.ReturnStoneItemName}: from={GameDebugLog.Position(from)} to={GameDebugLog.Position(entrancePosition)}, stamina={model.Stamina}/{model.MaxStamina}, gold={model.Gold}.");
            return true;
        }

        private void BeginReturnToCastle()
        {
            ReleaseExplorationTarget("return to entrance");
            patrolPath.Clear();
            priorityTargetPath.Clear();
            if (model.Position == entrancePosition)
            {
                RestoreAtCastle();
                return;
            }

            if (!TryBuildRememberedPath(model.Position, entrancePosition, out returnPath) || returnPath.Count == 0)
            {
                model.SetState(HeroState.Stuck);
                GameDebugLog.Warning(
                    "Hero",
                    $"{HeroLogName} cannot return to entrance from {GameDebugLog.Position(model.Position)}: remembered path missing, memory={model.Memory.RememberedCount}, stamina={model.Stamina}/{model.MaxStamina}.");
                return;
            }

            model.SetState(HeroState.ReturningToCastle);
            GameDebugLog.Info(
                "Hero",
                $"{HeroLogName} returning to entrance: from={GameDebugLog.Position(model.Position)}, pathSteps={returnPath.Count}, stamina={model.Stamina}/{model.MaxStamina}, memory={model.Memory.RememberedCount}.");
        }

        private void StepReturnToCastle()
        {
            if (model.State == HeroState.Stuck)
            {
                return;
            }

            if (model.Position == entrancePosition)
            {
                RestoreAtCastle();
                return;
            }

            if (returnPath.Count == 0 && !TryBuildRememberedPath(model.Position, entrancePosition, out returnPath))
            {
                model.SetState(HeroState.Stuck);
                GameDebugLog.Warning(
                    "Hero",
                    $"{HeroLogName} lost return path to entrance from {GameDebugLog.Position(model.Position)} while returning, memory={model.Memory.RememberedCount}.");
                return;
            }

            if (returnPath.Count == 0)
            {
                RestoreAtCastle();
                return;
            }

            MoveAlongRememberedPath(returnPath.Dequeue());
            if (model.Position == entrancePosition)
            {
                RestoreAtCastle();
            }
        }

        private void StepReturnToDoor()
        {
            if (targetStairs != null)
            {
                StepReturnToStairs();
                return;
            }

            if (targetDoor == null || targetDoor.IsOpen)
            {
                SetExplorationState();
                return;
            }

            if (GridDistance(model.Position, targetDoor.Position) <= 1)
            {
                OpenDoor(targetDoor);
                return;
            }

            if (doorPath.Count == 0 && !TryBuildRememberedPathToDoor(targetDoor, out doorPath))
            {
                SetExplorationState();
                return;
            }

            if (doorPath.Count == 0)
            {
                OpenDoor(targetDoor);
                return;
            }

            MoveAlongRememberedPath(doorPath.Dequeue());
        }

        private void RestoreAtCastle()
        {
            deathTokenManager?.TryDeliver(model, entrancePosition);
            goldIngotManager?.TryDeliver(model);
            entranceKnowledgeSync?.Invoke(model, heroNumber);
            var staminaBefore = model.Stamina;
            model.RestoreStamina();
            SetExplorationState();
            returnPath.Clear();
            patrolPath.Clear();
            priorityTargetPath.Clear();
            GameDebugLog.Info(
                "Hero",
                $"{HeroLogName} restored at entrance: stamina {staminaBefore}->{model.Stamina}/{model.MaxStamina}, state={model.State}, memory={model.Memory.RememberedCount}, gold={model.Gold}, xp={model.Experience}/{model.ExperienceForNextLevel}.");
        }

        private void MoveToNewCell(Vector2Int position)
        {
            if (!model.TrySpendStamina(StaminaPerNewCell))
            {
                GameDebugLog.Info(
                    "Hero",
                    $"{HeroLogName} has no stamina for new cell {GameDebugLog.Position(position)}; starting return from {GameDebugLog.Position(model.Position)}.");
                BeginReturnToCastle();
                return;
            }

            MoveAndRemember(position);
        }

        private void MoveAndRemember(Vector2Int position)
        {
            model.MoveTo(position);
            explorationCoordinator?.CompleteTarget(heroNumber, position);
            if (model.Memory.Remember(position))
            {
                var gainedLevels = model.RewardNewCellExploration(out var vengeanceProgress);
                LogExplorationProgress(position, gainedLevels);
                ShowVengeanceProgress(vengeanceProgress, position, 2.1f);
            }

            TryOpenChestInCurrentCave();
            if (TryCollectKey())
            {
                return;
            }

            if (TryHandleDeathTokenOnCurrentCell())
            {
                return;
            }

            TryHandleGoldIngotOnCurrentCell();
        }

        private void MoveAlongRememberedPath(Vector2Int position)
        {
            model.MoveTo(position);
            model.Memory.Remember(position);
            TryOpenChestInCurrentCave();
            if (TryCollectKey())
            {
                return;
            }

            if (TryHandleDeathTokenOnCurrentCell())
            {
                return;
            }

            TryHandleGoldIngotOnCurrentCell();
        }

        private void SetExplorationState()
        {
            patrolPath.Clear();
            model.SetState(HasCentralRoomKey() || model.Memory.KnownClosedDoorCount == 0
                ? HeroState.Exploring
                : HeroState.SearchingKey);
        }

        private bool HasCentralRoomKey()
        {
            return model.Inventory.HasItem(HeroInventory.CentralRoomKeyItemName);
        }

        private bool HasDescentKey()
        {
            return model.Inventory.HasItem(HeroInventory.DescentKeyItemName);
        }

        private bool TryCollectKey()
        {
            if (result.KeyPickups == null)
            {
                return false;
            }

            for (var i = 0; i < result.KeyPickups.Count; i++)
            {
                if (TryCollectKey(result.KeyPickups[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryCollectKey(KeyPickupModel key)
        {
            if (key == null || !key.IsAvailable || model.Position != key.Position)
            {
                return false;
            }

            if (!model.Inventory.TryPlaceInEmptySlot(key.ItemName))
            {
                GameDebugLog.Warning("Hero", $"{HeroLogName} reached key at {GameDebugLog.Position(key.Position)} but has no empty inventory slot.");
                return false;
            }

            key.Collect();
            GameAudioController.Play(GameSfx.KeyPickup, mazeRenderer.GridToWorld(key.Position));
            GameDebugLog.Info(
                "Hero",
                $"{HeroLogName} picked up {key.ItemName} at {GameDebugLog.Position(key.Position)}, knownClosedDoors={model.Memory.KnownClosedDoorCount}.");
            if (key.ItemName == HeroInventory.DescentKeyItemName)
            {
                TryBeginReturnToKnownStairs();
            }
            else
            {
                TryBeginReturnToKnownDoor();
            }

            return true;
        }

        private bool TryHandleGoldIngotOnCurrentCell()
        {
            if (goldIngotManager == null)
            {
                return false;
            }

            if (goldIngotManager.TryDeliver(model))
            {
                returnPath.Clear();
                SetExplorationState();
                return true;
            }

            if (model.Inventory.HasGoldIngot)
            {
                if (model.Position != entrancePosition && model.State != HeroState.ReturningToCastle)
                {
                    BeginReturnToCastle();
                    return true;
                }

                return false;
            }

            if (!goldIngotManager.TryPickUp(model))
            {
                return false;
            }

            if (model.Position == entrancePosition)
            {
                goldIngotManager.TryDeliver(model);
            }
            else
            {
                BeginReturnToCastle();
            }

            return true;
        }

        private bool TryHandleDeathTokenOnCurrentCell()
        {
            if (deathTokenManager == null)
            {
                return false;
            }

            if (deathTokenManager.TryDeliver(model, entrancePosition))
            {
                returnPath.Clear();
                SetExplorationState();
                return true;
            }

            if (deathTokenManager.HasCarriedToken(model))
            {
                if (model.Position != entrancePosition && model.State != HeroState.ReturningToCastle)
                {
                    BeginReturnToCastle();
                    return true;
                }

                return false;
            }

            if (!deathTokenManager.TryPickUp(model))
            {
                return false;
            }

            if (model.Position == entrancePosition)
            {
                deathTokenManager.TryDeliver(model, entrancePosition);
            }
            else
            {
                BeginReturnToCastle();
            }

            return true;
        }

        private bool TryOpenAdjacentKnownDoor()
        {
            if (!HasCentralRoomKey())
            {
                return false;
            }

            return TryRememberAdjacentClosedDoor(out var door) && OpenDoor(door);
        }

        private bool TryOpenAdjacentKnownStairs()
        {
            if (!HasDescentKey())
            {
                return false;
            }

            return TryRememberAdjacentLockedStairs(out var stairs) && OpenStairs(stairs);
        }

        private bool TryRememberAdjacentClosedDoor(out CentralDoorModel door)
        {
            door = null;
            foreach (var candidate in result.CentralDoors)
            {
                if (candidate == null || !candidate.IsClosed || GridDistance(model.Position, candidate.Position) > 1)
                {
                    continue;
                }

                var discoveredNow = model.Memory.RememberClosedDoor(candidate.Position);
                if (discoveredNow)
                {
                    GameDebugLog.Info(
                        "Hero",
                        $"{HeroLogName} remembered closed {candidate.Name} at {GameDebugLog.Position(candidate.Position)} from {GameDebugLog.Position(model.Position)}.");
                }

                door = candidate;
                return discoveredNow || HasCentralRoomKey();
            }

            return false;
        }

        private bool TryRememberAdjacentLockedStairs(out DungeonStairsModel stairs)
        {
            stairs = result.DownStairs;
            if (stairs == null || stairs.IsOpen || GridDistance(model.Position, stairs.Position) > 1)
            {
                stairs = null;
                return false;
            }

            var discoveredNow = model.Memory.RememberClosedDoor(stairs.Position);
            if (discoveredNow)
            {
                GameDebugLog.Info(
                    "Hero",
                    $"{HeroLogName} remembered locked {stairs.DisplayName} at {GameDebugLog.Position(stairs.Position)} from {GameDebugLog.Position(model.Position)}.");
            }

            return discoveredNow || HasDescentKey();
        }

        private bool TryBeginReturnToKnownDoor()
        {
            foreach (var door in result.CentralDoors)
            {
                if (door == null || !door.IsClosed || !model.Memory.IsClosedDoorKnown(door.Position))
                {
                    continue;
                }

                if (!TryBuildRememberedPathToDoor(door, out doorPath))
                {
                    LogMissingDoorPathOnce(door);
                    continue;
                }

                ReleaseExplorationTarget("return to known door");
                patrolPath.Clear();
                priorityTargetPath.Clear();
                doorPathWarningPositions.Remove(door.Position);
                targetDoor = door;
                model.SetState(HeroState.ReturningToDoor);
                GameDebugLog.Info(
                    "Hero",
                    $"{HeroLogName} returning to {door.Name} at {GameDebugLog.Position(door.Position)}, pathSteps={doorPath.Count}, from={GameDebugLog.Position(model.Position)}.");
                StepReturnToDoor();
                return true;
            }

            return false;
        }

        private bool TryBeginReturnToKnownStairs()
        {
            var stairs = result.DownStairs;
            if (stairs == null || stairs.IsOpen || !model.Memory.IsClosedDoorKnown(stairs.Position))
            {
                return false;
            }

            if (!TryBuildRememberedPathToStairs(stairs, out doorPath))
            {
                LogMissingStairsPathOnce(stairs);
                return false;
            }

            ReleaseExplorationTarget("return to known stairs");
            patrolPath.Clear();
            priorityTargetPath.Clear();
            doorPathWarningPositions.Remove(stairs.Position);
            targetDoor = null;
            targetStairs = stairs;
            model.SetState(HeroState.ReturningToDoor);
            GameDebugLog.Info(
                "Hero",
                $"{HeroLogName} returning to {stairs.DisplayName} at {GameDebugLog.Position(stairs.Position)}, pathSteps={doorPath.Count}, from={GameDebugLog.Position(model.Position)}.");
            StepReturnToStairs();
            return true;
        }

        private bool TryBeginReturnToReachableClosedDoor()
        {
            foreach (var door in result.CentralDoors)
            {
                if (door == null || !door.IsClosed)
                {
                    continue;
                }

                if (!TryBuildRememberedPathToDoor(door, out doorPath))
                {
                    continue;
                }

                ReleaseExplorationTarget("reachable door fallback");
                patrolPath.Clear();
                priorityTargetPath.Clear();
                model.Memory.RememberClosedDoor(door.Position);
                doorPathWarningPositions.Remove(door.Position);
                targetDoor = door;
                model.SetState(HeroState.ReturningToDoor);
                GameDebugLog.Info(
                    "Hero",
                    $"{HeroLogName} found reachable {door.Name} after frontier exhaustion, pathSteps={doorPath.Count}, from={GameDebugLog.Position(model.Position)}.");
                StepReturnToDoor();
                return true;
            }

            return false;
        }

        private bool TryBeginReturnToReachableLockedStairs()
        {
            var stairs = result.DownStairs;
            if (stairs == null || stairs.IsOpen || !TryBuildRememberedPathToStairs(stairs, out doorPath))
            {
                return false;
            }

            ReleaseExplorationTarget("reachable stairs fallback");
            patrolPath.Clear();
            priorityTargetPath.Clear();
            model.Memory.RememberClosedDoor(stairs.Position);
            doorPathWarningPositions.Remove(stairs.Position);
            targetDoor = null;
            targetStairs = stairs;
            model.SetState(HeroState.ReturningToDoor);
            GameDebugLog.Info(
                "Hero",
                $"{HeroLogName} found reachable {stairs.DisplayName} after frontier exhaustion, pathSteps={doorPath.Count}, from={GameDebugLog.Position(model.Position)}.");
            StepReturnToStairs();
            return true;
        }

        private void HandleNoFrontierFallback()
        {
            if (HasDescentKey()
                && (TryBeginReturnToKnownStairs() || TryBeginReturnToReachableLockedStairs()))
            {
                return;
            }

            if (HasCentralRoomKey()
                && (TryBeginReturnToKnownDoor() || TryBeginReturnToReachableClosedDoor()))
            {
                return;
            }

            if (model.Position != entrancePosition)
            {
                ReleaseExplorationTarget("no frontier");
                BeginReturnToCastle();
                return;
            }

            if (TryBuildPathToFarthestRememberedCell(out var newPatrolPath) && newPatrolPath.Count > 0)
            {
                patrolPath = newPatrolPath;
                ReleaseExplorationTarget("patrol fallback");
                GameDebugLog.Info(
                    "Hero",
                    $"{HeroLogName} has no frontier at entrance; patrolling farthest remembered cell, pathSteps={patrolPath.Count}, memory={model.Memory.RememberedCount}.");
                MoveAlongRememberedPath(patrolPath.Dequeue());
                return;
            }

            GameDebugLog.Info(
                "Hero",
                $"{HeroLogName} has no frontier and no patrol target: state={model.State}, memory={model.Memory.RememberedCount}, knownDoors={model.Memory.KnownClosedDoorCount}.");
            SetExplorationState();
        }

        private bool TryContinuePatrolFallback()
        {
            while (patrolPath.Count > 0)
            {
                var next = patrolPath.Dequeue();
                if (next == model.Position)
                {
                    continue;
                }

                if (!grid.InBounds(next)
                    || !grid.Get(next).IsWalkable
                    || !model.Memory.IsRemembered(next)
                    || GridDistance(model.Position, next) != 1)
                {
                    GameDebugLog.Warning(
                        "Hero",
                        $"{HeroLogName} canceled no-frontier patrol: next={GameDebugLog.Position(next)}, from={GameDebugLog.Position(model.Position)}, remaining={patrolPath.Count}, memory={model.Memory.RememberedCount}.");
                    patrolPath.Clear();
                    return false;
                }

                MoveAlongRememberedPath(next);
                if (patrolPath.Count == 0)
                {
                    GameDebugLog.Info(
                        "Hero",
                        $"{HeroLogName} completed no-frontier patrol at {GameDebugLog.Position(model.Position)}, memory={model.Memory.RememberedCount}.");
                }

                return true;
            }

            return false;
        }

        private bool TryBeginPriorityDungeonTargetFallback()
        {
            if (priorityDungeonTargetProvider == null
                || !priorityDungeonTargetProvider.Invoke(model, out var target, out var label)
                || !TryBuildRememberedPath(model.Position, target, out var path)
                || path.Count == 0)
            {
                priorityTargetPath.Clear();
                return false;
            }

            priorityTargetPath = path;
            priorityTargetCell = target;
            priorityTargetLabel = string.IsNullOrWhiteSpace(label) ? "priority dungeon target" : label;
            ReleaseExplorationTarget("priority dungeon target");
            patrolPath.Clear();
            GameDebugLog.Info(
                "Hero",
                $"{HeroLogName} targets {priorityTargetLabel} after frontier exhaustion: target={GameDebugLog.Position(priorityTargetCell)}, pathSteps={priorityTargetPath.Count}, from={GameDebugLog.Position(model.Position)}, memory={model.Memory.RememberedCount}.");
            return TryContinuePriorityDungeonTarget();
        }

        private bool TryContinuePriorityDungeonTarget()
        {
            if (priorityTargetPath.Count == 0)
            {
                return false;
            }

            if (priorityDungeonTargetProvider == null
                || !priorityDungeonTargetProvider.Invoke(model, out var target, out var label)
                || target != priorityTargetCell)
            {
                priorityTargetPath.Clear();
                return false;
            }

            priorityTargetLabel = string.IsNullOrWhiteSpace(label) ? priorityTargetLabel : label;
            while (priorityTargetPath.Count > 0)
            {
                var next = priorityTargetPath.Dequeue();
                if (next == model.Position)
                {
                    continue;
                }

                if (!grid.InBounds(next)
                    || !grid.Get(next).IsWalkable
                    || !model.Memory.IsRemembered(next)
                    || GridDistance(model.Position, next) != 1)
                {
                    GameDebugLog.Warning(
                        "Hero",
                        $"{HeroLogName} canceled priority dungeon target: label={priorityTargetLabel}, target={GameDebugLog.Position(priorityTargetCell)}, next={GameDebugLog.Position(next)}, from={GameDebugLog.Position(model.Position)}, remaining={priorityTargetPath.Count}, memory={model.Memory.RememberedCount}.");
                    priorityTargetPath.Clear();
                    return false;
                }

                MoveAlongRememberedPath(next);
                if (priorityTargetPath.Count == 0)
                {
                    GameDebugLog.Info(
                        "Hero",
                        $"{HeroLogName} reached priority dungeon target approach: label={priorityTargetLabel}, target={GameDebugLog.Position(priorityTargetCell)}, position={GameDebugLog.Position(model.Position)}, memory={model.Memory.RememberedCount}.");
                }

                return true;
            }

            return false;
        }

        private void LogExplorationProgress(Vector2Int position, int gainedLevels)
        {
            if (model.Memory.RememberedCount < nextExplorationProgressLog && gainedLevels <= 0)
            {
                return;
            }

            while (nextExplorationProgressLog <= model.Memory.RememberedCount)
            {
                nextExplorationProgressLog += ExplorationProgressLogStep;
            }

            GameDebugLog.Info(
                "Hero",
                $"{HeroLogName} exploration progress: newCell={GameDebugLog.Position(position)}, memory={model.Memory.RememberedCount}, walls={model.Memory.RememberedWallCount}, stamina={model.Stamina}/{model.MaxStamina}, gold={model.Gold}, xp={model.Experience}/{model.ExperienceForNextLevel}, level={model.Level}, gainedLevels={gainedLevels}.");
        }

        private void ShowVengeanceProgress(HeroVengeanceProgressResult result, Vector2Int position, float delay)
        {
            if (!result.HasAnyFeedback)
            {
                return;
            }

            if (result.Completed)
            {
                DamageNumberView.CreateText(
                    mazeRenderer,
                    position,
                    result.Message,
                    new Color(1f, 0.72f, 0.28f),
                    delay);
                GameAudioController.Play(GameSfx.LevelUp, mazeRenderer.GridToWorld(position), 0.62f);
            }

            GameDebugLog.Info(
                "Hero",
                $"{HeroLogName} vengeance progress: {result.Message}, goldBonus={result.BonusGold}, xpBonus={result.BonusExperience}, hpBonus={result.MaxHitPointBonus}, staminaBonus={result.MaxStaminaBonus}.");
        }

        private void LogMissingDoorPathOnce(CentralDoorModel door)
        {
            if (!doorPathWarningPositions.Add(door.Position))
            {
                return;
            }

            GameDebugLog.Warning(
                "Hero",
                $"{HeroLogName} knows {door.Name} at {GameDebugLog.Position(door.Position)} and has key, but no remembered path from {GameDebugLog.Position(model.Position)}.");
        }

        private void LogMissingStairsPathOnce(DungeonStairsModel stairs)
        {
            if (!doorPathWarningPositions.Add(stairs.Position))
            {
                return;
            }

            GameDebugLog.Warning(
                "Hero",
                $"{HeroLogName} knows {stairs.DisplayName} at {GameDebugLog.Position(stairs.Position)} and has descent key, but no remembered path from {GameDebugLog.Position(model.Position)}.");
        }

        private bool OpenDoor(CentralDoorModel door)
        {
            if (door == null || door.IsOpen || !HasCentralRoomKey() || GridDistance(model.Position, door.Position) > 1)
            {
                return false;
            }

            if (!door.Open(grid))
            {
                doorPath.Clear();
                targetDoor = null;
                model.Memory.RememberClosedDoor(door.Position);
                SetExplorationState();
                GameDebugLog.Info("Hero", $"{HeroLogName} cannot open sealed {door.Name} at {GameDebugLog.Position(door.Position)}: {door.SealedReason}");
                return false;
            }

            model.Memory.ForgetClosedDoor(door.Position);
            model.Inventory.TryRemoveItem(HeroInventory.CentralRoomKeyItemName);
            doorPath.Clear();
            targetDoor = null;
            model.SetState(HeroState.OpeningDoor);
            GameAudioController.Play(GameSfx.DoorOpen, mazeRenderer.GridToWorld(door.Position));
            ShowVengeanceProgress(model.RegisterVengeanceBarrierOpened(door.Position, false), door.Position, 1.8f);
            GameDebugLog.Info("Hero", $"{HeroLogName} opened {door.Name} at {GameDebugLog.Position(door.Position)} using {HeroInventory.CentralRoomKeyItemName}.");
            return true;
        }

        private void StepReturnToStairs()
        {
            if (targetStairs == null || targetStairs.IsOpen)
            {
                targetStairs = null;
                SetExplorationState();
                return;
            }

            if (GridDistance(model.Position, targetStairs.Position) <= 1)
            {
                OpenStairs(targetStairs);
                return;
            }

            if (doorPath.Count == 0 && !TryBuildRememberedPathToStairs(targetStairs, out doorPath))
            {
                targetStairs = null;
                SetExplorationState();
                return;
            }

            if (doorPath.Count == 0)
            {
                OpenStairs(targetStairs);
                return;
            }

            MoveAlongRememberedPath(doorPath.Dequeue());
        }

        private bool OpenStairs(DungeonStairsModel stairs)
        {
            if (stairs == null || stairs.IsOpen || !HasDescentKey() || GridDistance(model.Position, stairs.Position) > 1)
            {
                return false;
            }

            stairs.Open(grid);
            model.Memory.ForgetClosedDoor(stairs.Position);
            model.Inventory.TryRemoveItem(HeroInventory.DescentKeyItemName);
            doorPath.Clear();
            targetStairs = null;
            targetDoor = null;
            model.SetState(HeroState.OpeningDoor);
            GameAudioController.Play(GameSfx.StairsOpen, mazeRenderer.GridToWorld(stairs.Position));
            ShowVengeanceProgress(model.RegisterVengeanceBarrierOpened(stairs.Position, true), stairs.Position, 1.8f);
            GameDebugLog.Info("Hero", $"{HeroLogName} opened {stairs.DisplayName} at {GameDebugLog.Position(stairs.Position)} using {HeroInventory.DescentKeyItemName}.");
            downStairsOpened?.Invoke(model, heroNumber, stairs);
            return true;
        }

        private bool TryOpenChestInCurrentCave()
        {
            foreach (var chest in result.Chests)
            {
                if (chest == null || chest.IsOpened || !chest.Contains(model.Position))
                {
                    continue;
                }

                if (!chest.Open())
                {
                    continue;
                }

                var rewardText = ApplyChestReward(chest, out var rewardColor);
                DamageNumberView.CreateText(mazeRenderer, chest.Position, rewardText, rewardColor, 1.65f);
                var rememberedCells = RememberChestCave(chest);
                GameDebugLog.Info(
                    "Hero",
                    $"Hero opened chest at {GameDebugLog.Position(chest.Position)}, cave={GameDebugLog.Position(chest.Cave.Center)}, reward={chest.RewardType}, text={rewardText}, heroGold={model.Gold}, attack={model.AttackPoints}, armor={model.ArmorPoints}, rememberedCells={rememberedCells}");
                return true;
            }

            return false;
        }

        private string ApplyChestReward(ChestModel chest, out Color rewardColor)
        {
            switch (chest.RewardType)
            {
                case ChestRewardType.WeaponTier2:
                    rewardColor = new Color(0.98f, 0.76f, 0.34f);
                    GameAudioController.Play(GameSfx.EquipmentFound, mazeRenderer.GridToWorld(chest.Position));
                    return ApplyEquipmentReward(
                        chest,
                        model.Inventory.TryEquipSteelSword,
                        "weapon");
                case ChestRewardType.ArmorTier2:
                    rewardColor = new Color(0.55f, 0.78f, 1f);
                    GameAudioController.Play(GameSfx.EquipmentFound, mazeRenderer.GridToWorld(chest.Position));
                    return ApplyEquipmentReward(
                        chest,
                        model.Inventory.TryEquipChainmail,
                        "armor");
                case ChestRewardType.Gold:
                default:
                    model.AddGold(chest.RewardGold);
                    rewardColor = new Color(1f, 0.84f, 0.26f);
                    GameAudioController.Play(GameSfx.GoldFound, mazeRenderer.GridToWorld(chest.Position));
                    return $"+{chest.RewardGold} зол.";
            }
        }

        private string ApplyEquipmentReward(
            ChestModel chest,
            EquipmentEquipHandler equip,
            string equipmentSlot)
        {
            if (equip(out var previousItem))
            {
                GameDebugLog.Info(
                    "Hero",
                    $"Hero equipped {equipmentSlot}: {previousItem} -> {chest.RewardItemName}.");
                return chest.RewardItemName;
            }

            model.AddGold(DuplicateEquipmentGoldCompensation);
            GameDebugLog.Info(
                "Hero",
                $"Hero found duplicate {equipmentSlot}: {chest.RewardItemName}, compensationGold={DuplicateEquipmentGoldCompensation}.");
            return $"{chest.RewardItemName} (+{DuplicateEquipmentGoldCompensation} зол.)";
        }

        private int RememberChestCave(ChestModel chest)
        {
            var rememberedCells = 0;
            foreach (var cellPosition in chest.CaveCells())
            {
                if (!grid.InBounds(cellPosition) || !grid.Get(cellPosition).IsWalkable)
                {
                    continue;
                }

                if (model.Memory.Remember(cellPosition))
                {
                    var gainedLevels = model.RewardNewCellExploration(out var vengeanceProgress);
                    LogExplorationProgress(cellPosition, gainedLevels);
                    ShowVengeanceProgress(vengeanceProgress, cellPosition, 2.1f);
                    rememberedCells++;
                }
            }

            return rememberedCells;
        }

        private string HeroLogName => $"Hero #{heroNumber}";
    }
}
