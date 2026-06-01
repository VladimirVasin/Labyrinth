using System.Collections.Generic;
using Labyrinth.Hero;
using Labyrinth.Maze;
using UnityEngine;

namespace Labyrinth.Core
{
    public sealed partial class MineConstructionController
    {
        private void RebuildSelectableCaves()
        {
            selectableCaves.Clear();
            if (knowledge == null || result == null)
            {
                return;
            }

            for (var i = 0; i < result.Caves.Count; i++)
            {
                var cave = result.Caves[i];
                if (IsCaveAlreadyUsed(cave) || !TryGetCaveOreType(cave, out _) || !IsCaveKnown(cave, knowledge))
                {
                    continue;
                }

                selectableCaves.Add(cave);
            }
        }

        private int CountSelectableCaves(HeroMemory knownMap)
        {
            var count = 0;
            for (var i = 0; i < result.Caves.Count; i++)
            {
                var cave = result.Caves[i];
                if (!IsCaveAlreadyUsed(cave) && TryGetCaveOreType(cave, out _) && IsCaveKnown(cave, knownMap))
                {
                    count++;
                }
            }

            return count;
        }

        private bool TryGetSelectableCave(Vector2Int cell, out CaveInfo cave)
        {
            for (var i = 0; i < selectableCaves.Count; i++)
            {
                if (ContainsCaveCell(selectableCaves[i], cell))
                {
                    cave = selectableCaves[i];
                    return true;
                }
            }

            cave = default;
            return false;
        }

        private bool TryGetAnyCave(Vector2Int cell, out CaveInfo cave)
        {
            if (result == null)
            {
                cave = default;
                return false;
            }

            for (var i = 0; i < result.Caves.Count; i++)
            {
                if (ContainsCaveCell(result.Caves[i], cell))
                {
                    cave = result.Caves[i];
                    return true;
                }
            }

            cave = default;
            return false;
        }

        private bool TryBuildKnownPathToCave(CaveInfo cave, out List<Vector2Int> path)
        {
            path = null;
            if (knowledge == null || result == null || !IsKnownWalkable(cave.Center))
            {
                return false;
            }

            var queue = new Queue<Vector2Int>();
            var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            queue.Enqueue(result.EntrancePosition);
            cameFrom[result.EntrancePosition] = result.EntrancePosition;

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current == cave.Center)
                {
                    path = BuildPath(cameFrom, cave.Center);
                    return true;
                }

                foreach (var neighbor in result.Grid.WalkableNeighbors(current))
                {
                    if (cameFrom.ContainsKey(neighbor) || !IsKnownWalkable(neighbor))
                    {
                        continue;
                    }

                    cameFrom[neighbor] = current;
                    queue.Enqueue(neighbor);
                }
            }

            return false;
        }

        private List<Vector2Int> BuildPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int target)
        {
            var path = new List<Vector2Int>();
            var current = target;
            while (current != result.EntrancePosition)
            {
                path.Add(current);
                current = cameFrom[current];
            }

            path.Add(result.EntrancePosition);
            path.Reverse();
            return path;
        }

        private List<Vector2Int> BuildMineRouteWithCaveFootprint(CaveInfo cave, List<Vector2Int> pathToCenter)
        {
            var route = new List<Vector2Int>(pathToCenter);
            GameDebugLog.Info(
                "Mine",
                $"Mine center route prepared: cave={GameDebugLog.Position(cave.Center)}, routeLength={route.Count}.");
            return route;
        }

        private static void AddRouteStep(List<Vector2Int> route, Vector2Int cell)
        {
            if (route.Count == 0 || route[route.Count - 1] != cell)
            {
                route.Add(cell);
            }
        }

        private bool IsCaveAlreadyUsed(CaveInfo cave)
        {
            for (var i = 0; i < zones.Count; i++)
            {
                if (zones[i].Cave.Center == cave.Center)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsCaveKnown(CaveInfo cave, HeroMemory knownMap)
        {
            for (var x = cave.Center.x - 1; x <= cave.Center.x + 1; x++)
            {
                for (var y = cave.Center.y - 1; y <= cave.Center.y + 1; y++)
                {
                    var cell = new Vector2Int(x, y);
                    if (!result.Grid.InBounds(cell) || !result.Grid.Get(cell).IsWalkable || !knownMap.IsRemembered(cell))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool IsKnownWalkable(Vector2Int position)
        {
            if (!result.Grid.InBounds(position) || !result.Grid.Get(position).IsWalkable)
            {
                return false;
            }

            return position == result.EntrancePosition || knowledge.IsRemembered(position);
        }

        private bool TryGetCaveOreType(CaveInfo cave, out OreDepositType oreType)
        {
            if (result?.OreDeposits != null)
            {
                for (var i = 0; i < result.OreDeposits.Count; i++)
                {
                    var deposit = result.OreDeposits[i];
                    if (deposit != null && !deposit.IsDepleted && deposit.Cave.Center == cave.Center)
                    {
                        oreType = deposit.Type;
                        return true;
                    }
                }
            }

            oreType = default;
            return false;
        }

        private static string GetOreTypeName(OreDepositType oreType)
        {
            return oreType == OreDepositType.Iron ? "железная" : "золотая";
        }

        private static string GetOreResourceName(OreDepositType oreType)
        {
            return oreType == OreDepositType.Iron ? "железо" : "золото";
        }

        private static Color GetOreAccentColor(OreDepositType oreType)
        {
            return oreType == OreDepositType.Iron
                ? new Color(0.62f, 0.68f, 0.74f)
                : new Color(1f, 0.72f, 0.14f);
        }

        private static bool ContainsCaveCell(CaveInfo cave, Vector2Int cell)
        {
            return Mathf.Abs(cell.x - cave.Center.x) <= 1
                && Mathf.Abs(cell.y - cave.Center.y) <= 1;
        }
    }
}
