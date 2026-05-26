using System.Collections.Generic;
using Labyrinth.Maze;
using UnityEngine;

namespace Labyrinth.Core
{
    public sealed partial class MineConstructionController
    {
        private bool TryBuildGuildToCastleWorldPath(out List<Vector3> worldPath)
        {
            worldPath = new List<Vector3>();
            if (baseDevelopment == null || !baseDevelopment.HasMinersGuild)
            {
                return false;
            }

            if (!TryBuildCompletedRoadPath(baseDevelopment.MinersGuildPosition, result.BasePosition, out var roadCells))
            {
                GameDebugLog.Warning(
                    "Mine",
                    $"Mine worker dispatch blocked: no completed road from miners guild {GameDebugLog.Position(baseDevelopment.MinersGuildPosition)} to castle {GameDebugLog.Position(result.BasePosition)}.");
                return false;
            }

            var offset = new Vector3(0f, mazeRenderer.CellSize * WorkerYOffset, 0f);
            AddWorldPathCells(worldPath, roadCells, offset);
            EnsureWorldPathStartsAt(worldPath, baseDevelopment.MinersGuildPosition, offset);
            GameDebugLog.Info(
                "Mine",
                $"Mine worker guild-to-castle road confirmed: cells={roadCells.Count}, path={FormatCellPathPreview(roadCells)}.");
            return true;
        }

        private bool TryBuildCastleToRouteTargetWorldPath(
            IReadOnlyList<Vector2Int> route,
            int targetIndex,
            bool requireFortifiedPrefix,
            out List<Vector3> worldPath)
        {
            worldPath = new List<Vector3>();
            if (!TryValidateWorkerMazeRoute(route, targetIndex, requireFortifiedPrefix))
            {
                return false;
            }

            if (!TryBuildCompletedRoadPath(result.BasePosition, result.EntrancePosition, out var roadCells))
            {
                GameDebugLog.Warning("Mine", "Mine worker target path blocked: completed castle-to-entrance road is missing.");
                return false;
            }

            var offset = new Vector3(0f, mazeRenderer.CellSize * WorkerYOffset, 0f);
            AddWorldPathCells(worldPath, roadCells, offset);
            for (var i = 1; i <= targetIndex; i++)
            {
                AddWorldPoint(worldPath, route[i], offset);
            }

            return true;
        }

        private bool TryBuildRouteTargetToCastleWorldPath(IReadOnlyList<Vector2Int> route, int targetIndex, out List<Vector3> worldPath)
        {
            worldPath = new List<Vector3>();
            if (!TryValidateWorkerMazeRoute(route, targetIndex, false))
            {
                return false;
            }

            if (!TryBuildCompletedRoadPath(result.EntrancePosition, result.BasePosition, out var roadCells))
            {
                GameDebugLog.Warning("Mine", "Mine worker return path blocked: completed entrance-to-castle road is missing.");
                return false;
            }

            var offset = new Vector3(0f, mazeRenderer.CellSize * WorkerYOffset, 0f);
            for (var i = targetIndex; i >= 0; i--)
            {
                AddWorldPoint(worldPath, route[i], offset);
            }

            AddWorldPathCells(worldPath, roadCells, offset);
            return worldPath.Count > 1;
        }

        private bool TryBuildCastleToMineWorldPath(IReadOnlyList<Vector2Int> route, out List<Vector3> worldPath)
        {
            worldPath = new List<Vector3>();
            if (route == null || route.Count == 0)
            {
                return false;
            }

            return TryBuildCastleToRouteTargetWorldPath(route, route.Count - 1, true, out worldPath);
        }

        private bool TryBuildCompletedRoadPath(Vector2Int start, Vector2Int end, out List<Vector2Int> path)
        {
            path = null;
            if (baseAmbience == null || !baseAmbience.TryGetRoadPath(start, end, out var roadPath))
            {
                return false;
            }

            if (!ValidateAdjacentPath(roadPath, start, end, "road"))
            {
                return false;
            }

            path = roadPath;
            return true;
        }

        private bool TryValidateWorkerMazeRoute(
            IReadOnlyList<Vector2Int> route,
            int targetIndex,
            bool requireConstructedPrefix)
        {
            if (route == null || route.Count == 0 || targetIndex < 0 || targetIndex >= route.Count)
            {
                return false;
            }

            if (route[0] != result.EntrancePosition)
            {
                GameDebugLog.Warning(
                    "Mine",
                    $"Mine worker maze path rejected: route starts at {GameDebugLog.Position(route[0])}, expected entrance {GameDebugLog.Position(result.EntrancePosition)}.");
                return false;
            }

            for (var i = 0; i <= targetIndex; i++)
            {
                var cell = route[i];
                if (!result.Grid.InBounds(cell) || !result.Grid.Get(cell).IsWalkable)
                {
                    GameDebugLog.Warning(
                        "Mine",
                        $"Mine worker maze path rejected: cell {GameDebugLog.Position(cell)} is not walkable at route index {i}/{targetIndex}.");
                    return false;
                }

                if (i > 0 && ManhattanDistance(route[i - 1], cell) != 1)
                {
                    GameDebugLog.Warning(
                        "Mine",
                        $"Mine worker maze path rejected: non-adjacent step {GameDebugLog.Position(route[i - 1])} -> {GameDebugLog.Position(cell)}.");
                    return false;
                }

                if (requireConstructedPrefix
                    && i < targetIndex
                    && !fortifiedCells.Contains(cell)
                    && !IsRouteCellAssigned(route, cell, i))
                {
                    GameDebugLog.Warning(
                        "Mine",
                        $"Mine worker maze path rejected: route cell {GameDebugLog.Position(cell)} at index {i} is not fortified or reserved yet.");
                    return false;
                }
            }

            return true;
        }

        private bool IsRouteCellAssigned(IReadOnlyList<Vector2Int> route, Vector2Int cell, int routeIndex)
        {
            for (var z = 0; z < zones.Count; z++)
            {
                var zone = zones[z];
                if (zone == null || zone.Route != route)
                {
                    continue;
                }

                return routeIndex >= zone.RouteIndex && zone.AssignedRouteCells.Contains(cell);
            }

            return false;
        }

        private bool ValidateAdjacentPath(IReadOnlyList<Vector2Int> path, Vector2Int start, Vector2Int end, string label)
        {
            if (path == null || path.Count < 2 || path[0] != start || path[path.Count - 1] != end)
            {
                GameDebugLog.Warning(
                    "Mine",
                    $"Mine worker {label} path rejected: expected {GameDebugLog.Position(start)} -> {GameDebugLog.Position(end)}.");
                return false;
            }

            for (var i = 1; i < path.Count; i++)
            {
                if (ManhattanDistance(path[i - 1], path[i]) != 1)
                {
                    GameDebugLog.Warning(
                        "Mine",
                        $"Mine worker {label} path rejected: non-adjacent step {GameDebugLog.Position(path[i - 1])} -> {GameDebugLog.Position(path[i])}.");
                    return false;
                }
            }

            for (var i = 0; i < path.Count; i++)
            {
                var cell = path[i];
                if (result.Grid.InBounds(cell) && cell != result.EntrancePosition)
                {
                    GameDebugLog.Warning(
                        "Mine",
                        $"Mine worker {label} path rejected: road cell {GameDebugLog.Position(cell)} is inside the labyrinth.");
                    return false;
                }
            }

            return true;
        }

        private void AddWorldPathCells(List<Vector3> worldPath, IReadOnlyList<Vector2Int> cells, Vector3 offset)
        {
            for (var i = 0; i < cells.Count; i++)
            {
                var point = mazeRenderer.GridToWorld(cells[i]) + offset;
                if (worldPath.Count == 0 || (worldPath[worldPath.Count - 1] - point).sqrMagnitude > 0.0001f)
                {
                    worldPath.Add(point);
                }
            }
        }

        private void AddWorldPoint(List<Vector3> worldPath, Vector2Int cell, Vector3 offset)
        {
            var point = mazeRenderer.GridToWorld(cell) + offset;
            if (worldPath.Count == 0 || (worldPath[worldPath.Count - 1] - point).sqrMagnitude > 0.0001f)
            {
                worldPath.Add(point);
            }
        }

        private void EnsureWorldPathStartsAt(List<Vector3> worldPath, Vector2Int cell, Vector3 offset)
        {
            if (worldPath == null)
            {
                return;
            }

            var expected = mazeRenderer.GridToWorld(cell) + offset;
            if (worldPath.Count == 0)
            {
                worldPath.Add(expected);
                return;
            }

            if ((worldPath[0] - expected).sqrMagnitude > 0.0001f)
            {
                worldPath.Insert(0, expected);
            }
        }

        private static int ManhattanDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }
    }
}
