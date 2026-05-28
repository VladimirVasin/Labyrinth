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
            foreach (var neighbor in grid.WalkableNeighbors(position))
            {
                if (!model.Memory.IsRemembered(neighbor))
                {
                    next = neighbor;
                    return true;
                }
            }

            next = default;
            return false;
        }

        private bool TryBuildPathToNearestFrontier(out Queue<Vector2Int> path)
        {
            return TryBuildRememberedPathToGoal(
                model.Position,
                current => current != model.Position && HasUnrememberedNeighbor(current),
                out path);
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
                if (currentDistance > farthestDistance)
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
    }
}
