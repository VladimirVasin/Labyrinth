using Labyrinth.Core;

namespace Labyrinth.Hero
{
    public sealed partial class HeroExplorer
    {
        private void RecordCurrentVisit()
        {
            explorationCoordinator?.RecordVisit(heroNumber, model.Position);
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

            if (TryBeginStalePatrolFallback())
            {
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

        private bool TryBeginStalePatrolFallback()
        {
            if (!TryBuildPathToStalePatrolTarget(out var newPatrolPath, out var target) || newPatrolPath.Count == 0)
            {
                return false;
            }

            patrolPath = newPatrolPath;
            priorityTargetPath.Clear();
            GameDebugLog.Info(
                "Hero",
                $"{HeroLogName} has no frontier; patrolling stale route: target={GameDebugLog.Position(target.TargetCell)}, pathSteps={patrolPath.Count}, staleness={target.TargetStaleness}, visits={target.TargetVisitCount}, staleCells={target.StaleRouteCells}, memory={model.Memory.RememberedCount}.");
            MoveAlongRememberedPath(patrolPath.Dequeue());
            return true;
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
    }
}
