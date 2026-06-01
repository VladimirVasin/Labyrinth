using System.Collections.Generic;
using Labyrinth.Core;
using Labyrinth.Hero;
using UnityEngine;

namespace Labyrinth.Maze
{
    public sealed partial class MazeRenderer
    {
        private static readonly Color VoxelAmbientLight = new Color(0.92f, 0.93f, 0.96f, 1f);
        private static readonly Color VoxelEntranceLight = new Color(0.42f, 0.95f, 1f, 1f);
        private static readonly Color VoxelGoldLight = new Color(1f, 0.74f, 0.24f, 1f);
        private static readonly Color VoxelTorchLight = new Color(1f, 0.5f, 0.14f, 1f);
        private static readonly Color VoxelArcaneLight = new Color(0.3f, 0.86f, 1f, 1f);
        private static readonly Color VoxelIronLight = new Color(0.62f, 0.72f, 0.84f, 1f);
        private static readonly Color DungeonBlackTint = new Color(0.008f, 0.006f, 0.004f, 1f);
        private static readonly Color DungeonBoundaryTint = new Color(1.55f, 1.45f, 1.22f, 1f);
        private const float BuiltTorchLightRadius = DungeonLampProfile.RangeCells;

        private readonly Dictionary<Vector2Int, Color> staticVoxelLightByCell = new Dictionary<Vector2Int, Color>();

        public void ApplyDungeonLightMask(
            IReadOnlyList<HeroController> heroes,
            MazeGrid grid,
            HashSet<Vector2Int> visibleCells = null,
            IReadOnlyCollection<Vector2Int> builtTorchOrigins = null)
        {
            if (!VoxelVisuals.Enabled || grid == null)
            {
                RestoreStaticVoxelLightGrid();
                ApplyExternalDungeonObjectVisibility(visibleCells, grid);
                return;
            }

            foreach (var pair in cellRenderers)
            {
                var staticTint = staticVoxelLightByCell.TryGetValue(pair.Key, out var tint) ? tint : Color.white;
                var lightTint = IsAlwaysVisibleBoundaryCell(grid, pair.Key)
                    ? MaxLight(staticTint, DungeonBoundaryTint)
                    : Color.Lerp(DungeonBlackTint, staticTint, CalculateDungeonLightBrightness(grid, pair.Key, heroes, builtTorchOrigins, visibleCells));
                lightTint.a = 1f;
                for (var i = 0; i < pair.Value.Count; i++)
                {
                    VoxelVisuals.ApplyVoxelLightTint(pair.Value[i], lightTint);
                }
            }

            ApplyExternalDungeonObjectVisibility(visibleCells, grid);
        }

        private void ApplyExternalDungeonObjectVisibility(HashSet<Vector2Int> visibleCells, MazeGrid grid)
        {
            externalVisibilityMaskActive = true;
            currentExternalVisibleCells = visibleCells;
            currentExternalVisibilityGrid = grid;

            foreach (var pair in externalCellRenderers)
            {
                SetRenderersEnabled(pair.Value, IsExternalDungeonObjectVisible(pair.Key, visibleCells, grid));
            }
        }

        private static bool IsExternalDungeonObjectVisible(
            Vector2Int cell,
            HashSet<Vector2Int> visibleCells,
            MazeGrid grid)
        {
            if (visibleCells != null && visibleCells.Contains(cell))
            {
                return true;
            }

            return grid != null && IsAlwaysVisibleBoundaryCell(grid, cell);
        }

        private void RestoreStaticVoxelLightGrid()
        {
            if (!VoxelVisuals.Enabled || staticVoxelLightByCell.Count == 0)
            {
                return;
            }

            foreach (var pair in cellRenderers)
            {
                if (!staticVoxelLightByCell.TryGetValue(pair.Key, out var tint))
                {
                    tint = Color.white;
                }

                for (var i = 0; i < pair.Value.Count; i++)
                {
                    VoxelVisuals.ApplyVoxelLightTint(pair.Value[i], tint);
                }
            }
        }

        private void ApplyStaticVoxelLightGrid(MazeGenerationResult result)
        {
            if (!VoxelVisuals.Enabled || result == null || result.Grid == null)
            {
                return;
            }

            staticVoxelLightByCell.Clear();
            foreach (var pair in cellRenderers)
            {
                var tint = CalculateStaticVoxelLight(result, pair.Key);
                staticVoxelLightByCell[pair.Key] = tint;
                for (var i = 0; i < pair.Value.Count; i++)
                {
                    VoxelVisuals.ApplyVoxelLightTint(pair.Value[i], tint);
                }
            }
        }

        private static Color CalculateStaticVoxelLight(MazeGenerationResult result, Vector2Int cell)
        {
            var light = VoxelAmbientLight;
            AddLight(ref light, cell, result.EntrancePosition, 6, 0.42f, VoxelEntranceLight);

            if (result.CentralDoors != null)
            {
                for (var i = 0; i < result.CentralDoors.Count; i++)
                {
                    AddLight(ref light, cell, result.CentralDoors[i].Position, 4, 0.24f, VoxelTorchLight);
                }
            }

            if (result.KeyPickups != null)
            {
                for (var i = 0; i < result.KeyPickups.Count; i++)
                {
                    var key = result.KeyPickups[i];
                    if (key != null && key.IsAvailable)
                    {
                        AddLight(ref light, cell, key.Position, 5, 0.4f, VoxelGoldLight);
                    }
                }
            }

            if (result.Chests != null)
            {
                for (var i = 0; i < result.Chests.Count; i++)
                {
                    AddLight(ref light, cell, result.Chests[i].Position, 3, 0.18f, VoxelGoldLight);
                }
            }

            if (result.DownStairs != null)
            {
                AddLight(ref light, cell, result.DownStairs.Position, 6, 0.36f, VoxelArcaneLight);
            }

            if (result.UpStairs != null)
            {
                AddLight(ref light, cell, result.UpStairs.Position, 4, 0.24f, VoxelArcaneLight);
            }

            if (result.OreDeposits != null)
            {
                for (var i = 0; i < result.OreDeposits.Count; i++)
                {
                    var deposit = result.OreDeposits[i];
                    var color = deposit.Type == OreDepositType.Gold ? VoxelGoldLight : VoxelIronLight;
                    for (var j = 0; j < deposit.Cells.Count; j++)
                    {
                        AddLight(ref light, cell, deposit.Cells[j], 2, 0.12f, color);
                    }
                }
            }

            return ClampLight(light);
        }

        private static void AddLight(
            ref Color target,
            Vector2Int cell,
            Vector2Int origin,
            int radius,
            float strength,
            Color color)
        {
            var distance = Mathf.Abs(cell.x - origin.x) + Mathf.Abs(cell.y - origin.y);
            if (distance > radius)
            {
                return;
            }

            var t = 1f - distance / Mathf.Max(1f, radius);
            var falloff = t * t;
            target.r += color.r * strength * falloff;
            target.g += color.g * strength * falloff;
            target.b += color.b * strength * falloff;
        }

        private static Color ClampLight(Color color)
        {
            return new Color(
                Mathf.Clamp(color.r, 0.72f, 1.35f),
                Mathf.Clamp(color.g, 0.72f, 1.35f),
                Mathf.Clamp(color.b, 0.76f, 1.38f),
                1f);
        }

        private static Color MaxLight(Color left, Color right)
        {
            return new Color(
                Mathf.Max(left.r, right.r),
                Mathf.Max(left.g, right.g),
                Mathf.Max(left.b, right.b),
                1f);
        }

        private static float CalculateDungeonLightBrightness(
            MazeGrid grid,
            Vector2Int cell,
            IReadOnlyList<HeroController> heroes,
            IReadOnlyCollection<Vector2Int> builtTorchOrigins,
            HashSet<Vector2Int> visibleCells)
        {
            var brightness = 0f;
            if (heroes != null)
            {
                for (var i = 0; i < heroes.Count; i++)
                {
                    var hero = heroes[i];
                    if (hero == null || hero.Model == null || !hero.ProvidesVisibility)
                    {
                        continue;
                    }

                    var origin = hero.Model.Position;
                    var radius = Mathf.Max(4.5f, hero.Model.SightRange + 5.5f);
                    var dx = cell.x - origin.x;
                    var dy = cell.y - origin.y;
                    var distance = Mathf.Sqrt(dx * dx + dy * dy);
                    if (distance > radius || !CanDungeonLightReach(grid, origin, cell))
                    {
                        continue;
                    }

                    var t = Mathf.Clamp01(1f - distance / radius);
                    var falloff = t * t * (3f - 2f * t);
                    brightness = Mathf.Max(brightness, falloff);
                }
            }

            if (builtTorchOrigins == null || builtTorchOrigins.Count == 0)
            {
                return Mathf.Clamp01(brightness);
            }

            foreach (var origin in builtTorchOrigins)
            {
                var dx = cell.x - origin.x;
                var dy = cell.y - origin.y;
                var distance = Mathf.Sqrt(dx * dx + dy * dy);
                if (distance > BuiltTorchLightRadius || !CanDungeonLightReach(grid, origin, cell))
                {
                    continue;
                }

                var t = Mathf.Clamp01(1f - distance / BuiltTorchLightRadius);
                var falloff = t * t * (3f - 2f * t);
                brightness = Mathf.Max(brightness, falloff);
            }

            return Mathf.Clamp01(brightness);
        }

        private static bool CanDungeonLightReach(MazeGrid grid, Vector2Int origin, Vector2Int target)
        {
            if (origin == target)
            {
                return true;
            }

            var current = origin;
            var dx = Mathf.Abs(target.x - origin.x);
            var dy = Mathf.Abs(target.y - origin.y);
            var stepX = origin.x < target.x ? 1 : -1;
            var stepY = origin.y < target.y ? 1 : -1;
            var error = dx - dy;

            while (current != target)
            {
                var previous = current;
                var doubledError = error * 2;

                if (doubledError > -dy)
                {
                    error -= dy;
                    current.x += stepX;
                }

                if (doubledError < dx)
                {
                    error += dx;
                    current.y += stepY;
                }

                if (IsDungeonLightBlockedByCorner(grid, previous, current, target))
                {
                    return false;
                }

                if (current == target)
                {
                    return true;
                }

                if (IsDungeonLightBlockingWall(grid, current))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsDungeonLightBlockedByCorner(
            MazeGrid grid,
            Vector2Int previous,
            Vector2Int current,
            Vector2Int target)
        {
            if (previous.x == current.x || previous.y == current.y)
            {
                return false;
            }

            if (current == target && IsDungeonLightBlockingWall(grid, target))
            {
                return false;
            }

            var sideA = new Vector2Int(current.x, previous.y);
            var sideB = new Vector2Int(previous.x, current.y);
            return IsDungeonLightBlockingWall(grid, sideA) && IsDungeonLightBlockingWall(grid, sideB);
        }

        private static bool IsDungeonLightBlockingWall(MazeGrid grid, Vector2Int position)
        {
            return grid.InBounds(position) && grid.Get(position).Type == MazeCellType.Wall;
        }

        private static bool IsAlwaysVisibleBoundaryCell(MazeGrid grid, Vector2Int position)
        {
            if (!grid.InBounds(position))
            {
                return false;
            }

            return position.x == 0
                || position.y == 0
                || position.x == grid.Width - 1
                || position.y == grid.Height - 1;
        }
    }
}
