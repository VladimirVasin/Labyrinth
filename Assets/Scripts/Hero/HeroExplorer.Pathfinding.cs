using System;
using System.Collections.Generic;
using Labyrinth.Maze;
using UnityEngine;

namespace Labyrinth.Hero
{
    public sealed partial class HeroExplorer
    {
        private bool TryFindUnrememberedNeighbor(Vector2Int position, out Vector2Int next)
        {
            if (explorationCoordinator != null
                && explorationCoordinator.TryGetReservedTarget(heroNumber, out var reservedTarget)
                && GridDistance(position, reservedTarget) == 1
                && grid.InBounds(reservedTarget)
                && grid.Get(reservedTarget).IsWalkable
                && !model.Memory.IsRemembered(reservedTarget)
                && IsWithinAllowedExplorationDepth(reservedTarget))
            {
                next = reservedTarget;
                return true;
            }

            var candidates = new List<HeroExplorationCandidate>();
            foreach (var neighbor in grid.WalkableNeighbors(position))
            {
                if (!model.Memory.IsRemembered(neighbor) && IsWithinAllowedExplorationDepth(neighbor))
                {
                    candidates.Add(new HeroExplorationCandidate(
                        position,
                        neighbor,
                        new Queue<Vector2Int>(new[] { neighbor }),
                        1,
                        CountUnknownWalkableNeighbors(position) + CountUnknownWalkableNeighbors(neighbor),
                        CalculateStrategicWeight(neighbor)));
                }
            }

            if (candidates.Count > 0
                && explorationCoordinator != null
                && explorationCoordinator.TryChooseTarget(heroNumber, position, candidates, out var selected))
            {
                next = selected.TargetCell;
                return true;
            }

            if (candidates.Count > 0)
            {
                next = candidates[0].TargetCell;
                return true;
            }

            next = default;
            return false;
        }

        private bool TryBuildPathToNearestFrontier(out Queue<Vector2Int> path)
        {
            path = new Queue<Vector2Int>();
            var candidates = BuildFrontierCandidates();
            if (candidates.Count == 0)
            {
                explorationCoordinator?.Release(heroNumber, "frontier exhausted");
                return false;
            }

            if (explorationCoordinator != null
                && explorationCoordinator.TryChooseTarget(heroNumber, model.Position, candidates, out var selected))
            {
                path = new Queue<Vector2Int>(selected.Path);
                return path.Count > 0;
            }

            path = new Queue<Vector2Int>(candidates[0].Path);
            return path.Count > 0;
        }

        private bool TryBuildRememberedPath(Vector2Int start, Vector2Int target, out Queue<Vector2Int> path)
        {
            return TryBuildRememberedPathToGoal(start, current => current == target, out path);
        }

        private bool TryBuildRememberedPathToDoor(CentralDoorModel door, out Queue<Vector2Int> path)
        {
            return TryBuildRememberedPathToGoal(
                model.Position,
                current => GridDistance(current, door.Position) <= 1,
                out path);
        }

        private bool TryBuildRememberedPathToStairs(DungeonStairsModel stairs, out Queue<Vector2Int> path)
        {
            return TryBuildRememberedPathToGoal(
                model.Position,
                current => GridDistance(current, stairs.Position) <= 1,
                out path);
        }

        private bool TryBuildRememberedPathToGoal(
            Vector2Int start,
            Func<Vector2Int, bool> isGoal,
            out Queue<Vector2Int> path)
        {
            path = new Queue<Vector2Int>();
            if (isGoal == null)
            {
                return false;
            }

            var queue = new Queue<Vector2Int>();
            var cameFrom = new Dictionary<Vector2Int, Vector2Int>();

            queue.Enqueue(start);
            cameFrom[start] = start;

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (isGoal(current))
                {
                    path = BuildPath(cameFrom, start, current);
                    return true;
                }

                EnqueueRememberedNeighbors(current, queue, cameFrom);
            }

            return false;
        }

        private bool TryBuildPathToFarthestRememberedCell(out Queue<Vector2Int> path)
        {
            path = new Queue<Vector2Int>();
            var queue = new Queue<Vector2Int>();
            var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            var farthest = model.Position;
            var farthestDistance = 0;

            queue.Enqueue(model.Position);
            cameFrom[model.Position] = model.Position;

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var currentDistance = GridDistance(model.Position, current);
                if (currentDistance > farthestDistance && IsWithinAllowedPatrolDepth(current))
                {
                    farthestDistance = currentDistance;
                    farthest = current;
                }

                EnqueueRememberedNeighbors(current, queue, cameFrom);
            }

            if (farthest == model.Position)
            {
                return false;
            }

            path = BuildPath(cameFrom, model.Position, farthest);
            return path.Count > 0;
        }

        private void EnqueueRememberedNeighbors(
            Vector2Int current,
            Queue<Vector2Int> queue,
            IDictionary<Vector2Int, Vector2Int> cameFrom)
        {
            foreach (var neighbor in grid.WalkableNeighbors(current))
            {
                if (!model.Memory.IsRemembered(neighbor) || cameFrom.ContainsKey(neighbor))
                {
                    continue;
                }

                cameFrom[neighbor] = current;
                queue.Enqueue(neighbor);
            }
        }

        private bool HasUnrememberedNeighbor(Vector2Int position)
        {
            foreach (var neighbor in grid.WalkableNeighbors(position))
            {
                if (!model.Memory.IsRemembered(neighbor))
                {
                    return true;
                }
            }

            return false;
        }

        private List<HeroExplorationCandidate> BuildFrontierCandidates()
        {
            var candidates = new List<HeroExplorationCandidate>();
            var queue = new Queue<Vector2Int>();
            var cameFrom = new Dictionary<Vector2Int, Vector2Int>();

            queue.Enqueue(model.Position);
            cameFrom[model.Position] = model.Position;

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current != model.Position)
                {
                    AddFrontierCandidates(current, cameFrom, candidates);
                }

                EnqueueRememberedNeighbors(current, queue, cameFrom);
            }

            return candidates;
        }

        private void AddFrontierCandidates(
            Vector2Int approachCell,
            IReadOnlyDictionary<Vector2Int, Vector2Int> cameFrom,
            ICollection<HeroExplorationCandidate> candidates)
        {
            var pathToApproach = BuildPath(cameFrom, model.Position, approachCell);
            if (pathToApproach.Count == 0)
            {
                return;
            }

            foreach (var neighbor in grid.WalkableNeighbors(approachCell))
            {
                if (model.Memory.IsRemembered(neighbor))
                {
                    continue;
                }

                if (!IsWithinAllowedExplorationDepth(neighbor))
                {
                    continue;
                }

                candidates.Add(new HeroExplorationCandidate(
                    approachCell,
                    neighbor,
                    new Queue<Vector2Int>(pathToApproach),
                    pathToApproach.Count + 1,
                    CountUnknownWalkableNeighbors(approachCell) + CountUnknownWalkableNeighbors(neighbor),
                    CalculateStrategicWeight(neighbor)));
            }
        }

        private int CountUnknownWalkableNeighbors(Vector2Int position)
        {
            var count = 0;
            foreach (var neighbor in grid.WalkableNeighbors(position))
            {
                if (!model.Memory.IsRemembered(neighbor))
                {
                    count++;
                }
            }

            return count;
        }

        private int CalculateStrategicWeight(Vector2Int position)
        {
            var weight = 0;
            if (result.DownStairs != null && result.DownStairs.Position == position)
            {
                weight += 28;
            }

            if (result.UpStairs != null && result.UpStairs.Position == position)
            {
                weight += 18;
            }

            if (result.KeyPickups != null)
            {
                for (var i = 0; i < result.KeyPickups.Count; i++)
                {
                    var key = result.KeyPickups[i];
                    if (key != null && key.IsAvailable && key.Position == position)
                    {
                        weight += 26;
                    }
                }
            }

            if (result.Chests != null)
            {
                for (var i = 0; i < result.Chests.Count; i++)
                {
                    var chest = result.Chests[i];
                    if (chest != null && !chest.IsOpened && GridDistance(chest.Position, position) <= 1)
                    {
                        weight += 16;
                    }
                }
            }

            return weight;
        }

        private bool IsWithinAllowedExplorationDepth(Vector2Int position)
        {
            if (maxDistanceFromEntrance <= 0 || distancesFromEntrance == null)
            {
                return true;
            }

            if (!distancesFromEntrance.TryGetValue(position, out var distance))
            {
                return false;
            }

            return distance <= GetAllowedExplorationDistance();
        }

        private bool IsWithinAllowedPatrolDepth(Vector2Int position)
        {
            if (maxDistanceFromEntrance <= 0 || distancesFromEntrance == null)
            {
                return true;
            }

            if (!distancesFromEntrance.TryGetValue(position, out var distance))
            {
                return false;
            }

            var patrolSlack = Mathf.Max(4, Mathf.RoundToInt(maxDistanceFromEntrance * 0.08f));
            return distance <= Mathf.Min(maxDistanceFromEntrance, GetAllowedExplorationDistance() + patrolSlack);
        }

        private int GetAllowedExplorationDistance()
        {
            if (maxDistanceFromEntrance <= 0)
            {
                return int.MaxValue;
            }

            var level = model != null ? model.Level : 1;
            var gearBonus = model?.Inventory != null
                ? (model.Inventory.AttackBonus + model.Inventory.ArmorBonus) * 2
                : 0;
            var ratio = level <= 2
                ? 0.32f
                : level <= 4
                    ? 0.45f
                    : level <= 7
                        ? 0.62f
                        : level <= 10
                            ? 0.82f
                            : 1f;
            var ratioDistance = Mathf.RoundToInt(maxDistanceFromEntrance * ratio);
            var minimumDistance = 24 + level * 4 + gearBonus;
            return Mathf.Clamp(Mathf.Max(ratioDistance, minimumDistance), 6, maxDistanceFromEntrance);
        }

        private static Queue<Vector2Int> BuildPath(
            IReadOnlyDictionary<Vector2Int, Vector2Int> cameFrom,
            Vector2Int start,
            Vector2Int target)
        {
            var reversed = new List<Vector2Int>();
            var current = target;

            while (current != start)
            {
                reversed.Add(current);
                current = cameFrom[current];
            }

            reversed.Reverse();
            return new Queue<Vector2Int>(reversed);
        }

        private static int GridDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        private static int CalculateMaxEntranceDistance(Dictionary<Vector2Int, int> distances)
        {
            var maxDistance = 0;
            if (distances == null)
            {
                return maxDistance;
            }

            foreach (var distance in distances.Values)
            {
                maxDistance = Mathf.Max(maxDistance, distance);
            }

            return maxDistance;
        }
    }
}
