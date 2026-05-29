using System.Collections.Generic;
using Labyrinth.Maze;
using UnityEngine;

namespace Labyrinth.Core
{
    public enum SubCellPathProfile
    {
        Hero,
        Mob,
        Cart,
        Worker,
        Civilian
    }

    public readonly struct SubCellPathNode
    {
        public SubCellPathNode(Vector3 position, Vector2Int cell)
        {
            Position = position;
            Cell = cell;
        }

        public Vector3 Position { get; }

        public Vector2Int Cell { get; }
    }

    public static class SubCellPathBuilder
    {
        private const float MinimumPointDistanceSqr = 0.0001f;

        public static int BuildSeed(IReadOnlyList<Vector2Int> cells, int salt = 0)
        {
            var hash = salt ^ 0x58d42f;
            if (cells == null)
            {
                return hash;
            }

            for (var i = 0; i < cells.Count; i++)
            {
                hash = unchecked(hash * 397) ^ cells[i].x;
                hash = unchecked(hash * 397) ^ (cells[i].y << 1);
            }

            return hash;
        }

        public static List<Vector3> Build(
            MazeRenderer renderer,
            IReadOnlyList<Vector2Int> cells,
            float yOffset,
            int laneSeed,
            SubCellPathProfile profile)
        {
            var nodes = BuildNodes(renderer, cells, yOffset, laneSeed, profile);
            var points = new List<Vector3>(nodes.Count);
            for (var i = 0; i < nodes.Count; i++)
            {
                points.Add(nodes[i].Position);
            }

            return points;
        }

        public static List<Vector3> BuildStep(
            MazeRenderer renderer,
            Vector2Int from,
            Vector2Int to,
            float yOffset,
            int laneSeed,
            SubCellPathProfile profile,
            Vector3 currentWorldPosition)
        {
            var cells = new[] { from, to };
            var points = Build(renderer, cells, yOffset, laneSeed, profile);
            if (points.Count == 0)
            {
                return points;
            }

            currentWorldPosition.y = points[0].y;
            if ((points[0] - currentWorldPosition).sqrMagnitude > MinimumPointDistanceSqr)
            {
                points.Insert(0, currentWorldPosition);
            }
            else
            {
                points[0] = currentWorldPosition;
            }

            return points;
        }

        public static List<SubCellPathNode> BuildNodes(
            MazeRenderer renderer,
            IReadOnlyList<Vector2Int> cells,
            float yOffset,
            int laneSeed,
            SubCellPathProfile profile)
        {
            var nodes = new List<SubCellPathNode>();
            if (renderer == null || cells == null || cells.Count == 0)
            {
                return nodes;
            }

            if (cells.Count == 1)
            {
                AppendNode(nodes, BuildStationaryPoint(renderer, cells[0], yOffset, laneSeed, profile), cells[0]);
                return nodes;
            }

            for (var i = 0; i < cells.Count - 1; i++)
            {
                AppendSegment(nodes, renderer, cells[i], cells[i + 1], yOffset, laneSeed, profile, i == 0);
            }

            if (nodes.Count == 0)
            {
                AppendNode(nodes, BuildStationaryPoint(renderer, cells[cells.Count - 1], yOffset, laneSeed, profile), cells[cells.Count - 1]);
            }

            return nodes;
        }

        public static float CalculateLength(IReadOnlyList<Vector3> points)
        {
            if (points == null || points.Count < 2)
            {
                return 0f;
            }

            var length = 0f;
            for (var i = 1; i < points.Count; i++)
            {
                length += Vector3.Distance(points[i - 1], points[i]);
            }

            return length;
        }

        private static void AppendSegment(
            List<SubCellPathNode> nodes,
            MazeRenderer renderer,
            Vector2Int from,
            Vector2Int to,
            float yOffset,
            int laneSeed,
            SubCellPathProfile profile,
            bool includeStart)
        {
            if (from == to)
            {
                if (includeStart)
                {
                    AppendNode(nodes, BuildStationaryPoint(renderer, from, yOffset, laneSeed, profile), from);
                }

                return;
            }

            var gridDelta = to - from;
            if (Mathf.Abs(gridDelta.x) + Mathf.Abs(gridDelta.y) != 1)
            {
                if (includeStart)
                {
                    AppendNode(nodes, BuildStationaryPoint(renderer, from, yOffset, laneSeed, profile), from);
                }

                AppendNode(nodes, BuildStationaryPoint(renderer, to, yOffset, laneSeed, profile), to);
                return;
            }

            var cellSize = Mathf.Max(0.01f, renderer.CellSize);
            var direction = new Vector3(gridDelta.x, 0f, gridDelta.y).normalized;
            var side = new Vector3(-direction.z, 0f, direction.x);
            var offset = new Vector3(0f, yOffset, 0f) + side * BuildSideOffset(cellSize, laneSeed, profile);
            var fromPoint = renderer.GridToWorld(from) + offset;
            var toPoint = renderer.GridToWorld(to) + offset;
            var inset = cellSize * GetInset(profile);

            if (includeStart)
            {
                AppendNode(nodes, fromPoint, from);
            }

            AppendNode(nodes, fromPoint + direction * inset, from);
            AppendNode(nodes, toPoint - direction * inset, to);
            AppendNode(nodes, toPoint, to);
        }

        private static Vector3 BuildStationaryPoint(
            MazeRenderer renderer,
            Vector2Int cell,
            float yOffset,
            int laneSeed,
            SubCellPathProfile profile)
        {
            var cellSize = Mathf.Max(0.01f, renderer.CellSize);
            var lane = BuildLaneScalar(laneSeed);
            var alt = BuildLaneScalar(laneSeed ^ 0x31c9);
            var maxOffset = GetMaxSideOffset(profile) * cellSize;
            return renderer.GridToWorld(cell) + new Vector3(lane * maxOffset, yOffset, alt * maxOffset * 0.55f);
        }

        private static float BuildSideOffset(float cellSize, int laneSeed, SubCellPathProfile profile)
        {
            return BuildLaneScalar(laneSeed) * GetMaxSideOffset(profile) * cellSize;
        }

        private static float BuildLaneScalar(int seed)
        {
            switch (PositiveMod(seed, 4))
            {
                case 0:
                    return -1f;
                case 1:
                    return -0.45f;
                case 2:
                    return 0.45f;
                default:
                    return 1f;
            }
        }

        private static float GetMaxSideOffset(SubCellPathProfile profile)
        {
            switch (profile)
            {
                case SubCellPathProfile.Cart:
                    return 0.075f;
                case SubCellPathProfile.Worker:
                    return 0.16f;
                case SubCellPathProfile.Civilian:
                    return 0.2f;
                case SubCellPathProfile.Mob:
                    return 0.15f;
                case SubCellPathProfile.Hero:
                default:
                    return 0.13f;
            }
        }

        private static float GetInset(SubCellPathProfile profile)
        {
            switch (profile)
            {
                case SubCellPathProfile.Cart:
                    return 0.36f;
                case SubCellPathProfile.Worker:
                case SubCellPathProfile.Civilian:
                    return 0.3f;
                case SubCellPathProfile.Mob:
                    return 0.28f;
                case SubCellPathProfile.Hero:
                default:
                    return 0.32f;
            }
        }

        private static int PositiveMod(int value, int divisor)
        {
            var result = value % divisor;
            return result < 0 ? result + divisor : result;
        }

        private static void AppendNode(List<SubCellPathNode> nodes, Vector3 point, Vector2Int cell)
        {
            if (nodes.Count > 0 && (nodes[nodes.Count - 1].Position - point).sqrMagnitude <= MinimumPointDistanceSqr)
            {
                return;
            }

            nodes.Add(new SubCellPathNode(point, cell));
        }
    }
}
