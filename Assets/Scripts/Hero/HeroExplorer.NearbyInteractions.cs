using System.Collections.Generic;
using Labyrinth.Core;
using UnityEngine;

namespace Labyrinth.Hero
{
    public sealed partial class HeroExplorer
    {
        private const int NearbyInteractionRadius = 2;

        private bool TryPursueNearbyInteraction()
        {
            if (!TryBuildNearbyInteractionPath(out var path, out var target, out var label))
            {
                return false;
            }

            if (path.Count == 0)
            {
                return true;
            }

            var stepCount = path.Count;
            var next = path.Dequeue();
            GameDebugLog.Info(
                "Hero",
                $"{HeroLogName} prioritizes nearby {label}: from={GameDebugLog.Position(model.Position)}, next={GameDebugLog.Position(next)}, target={GameDebugLog.Position(target)}, steps={stepCount}.");

            if (model.Memory.IsRemembered(next))
            {
                MoveAlongRememberedPath(next);
            }
            else
            {
                MoveToNewCell(next);
            }

            return true;
        }

        private bool TryBuildNearbyInteractionPath(
            out Queue<Vector2Int> path,
            out Vector2Int target,
            out string label)
        {
            path = new Queue<Vector2Int>();
            target = default;
            label = string.Empty;

            var queue = new Queue<Vector2Int>();
            var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            var distances = new Dictionary<Vector2Int, int>();
            var bestMobTarget = default(Vector2Int);
            var bestGoldTarget = default(Vector2Int);
            var bestMobDistance = int.MaxValue;
            var bestGoldDistance = int.MaxValue;
            var canTargetGold = CanTargetNearbyGoldIngot();

            queue.Enqueue(model.Position);
            cameFrom[model.Position] = model.Position;
            distances[model.Position] = 0;

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var distance = distances[current];

                if (IsNearbyMobInteractionCell(current)
                    && distance < bestMobDistance
                    && CanReachNearbyInteractionTarget(cameFrom, current))
                {
                    bestMobDistance = distance;
                    bestMobTarget = current;
                }

                if (canTargetGold
                    && goldIngotManager.HasAvailableIngotAt(current)
                    && distance < bestGoldDistance
                    && CanReachNearbyInteractionTarget(cameFrom, current))
                {
                    bestGoldDistance = distance;
                    bestGoldTarget = current;
                }

                if (distance >= NearbyInteractionRadius)
                {
                    continue;
                }

                foreach (var neighbor in grid.WalkableNeighbors(current))
                {
                    if (cameFrom.ContainsKey(neighbor))
                    {
                        continue;
                    }

                    cameFrom[neighbor] = current;
                    distances[neighbor] = distance + 1;
                    queue.Enqueue(neighbor);
                }
            }

            if (bestMobDistance != int.MaxValue)
            {
                target = bestMobTarget;
                label = "mob";
                path = BuildPath(cameFrom, model.Position, target);
                return true;
            }

            if (bestGoldDistance == int.MaxValue)
            {
                return false;
            }

            target = bestGoldTarget;
            label = "gold ingot";
            path = BuildPath(cameFrom, model.Position, target);
            return true;
        }

        private bool CanReachNearbyInteractionTarget(
            IReadOnlyDictionary<Vector2Int, Vector2Int> cameFrom,
            Vector2Int target)
        {
            if (target == model.Position)
            {
                return true;
            }

            var requiredStamina = 0;
            var current = target;
            while (current != model.Position)
            {
                if (!cameFrom.TryGetValue(current, out var previous))
                {
                    return false;
                }

                if (model.Memory == null || !model.Memory.IsRemembered(current))
                {
                    requiredStamina += StaminaPerNewCell;
                    if (requiredStamina > model.Stamina)
                    {
                        return false;
                    }
                }

                current = previous;
            }

            return true;
        }

        private bool IsNearbyMobInteractionCell(Vector2Int interactionCell)
        {
            return nearbyMobInteractionCellProvider != null
                && nearbyMobInteractionCellProvider.Invoke(
                    model.Position,
                    interactionCell,
                    NearbyInteractionRadius);
        }

        private bool CanTargetNearbyGoldIngot()
        {
            if (goldIngotManager == null
                || model.Inventory == null
                || model.Inventory.HasGoldIngot)
            {
                return false;
            }

            foreach (var slot in model.Inventory.Slots)
            {
                if (slot.Type == HeroInventorySlotType.Empty && !slot.HasItem)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
