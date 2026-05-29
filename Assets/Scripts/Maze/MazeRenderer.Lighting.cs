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
        private static readonly Color VoxelHeroTorchLight = new Color(1f, 0.56f, 0.18f, 1f);
        private static readonly Color VoxelArcaneLight = new Color(0.3f, 0.86f, 1f, 1f);
        private static readonly Color VoxelIronLight = new Color(0.62f, 0.72f, 0.84f, 1f);
        private const float HeroTorchVoxelRadiusBonus = 1.65f;
        private const float HeroTorchVoxelStrength = 0.4f;
        private const float HeroTorchCornerSpillStrength = 0.14f;

        private readonly Dictionary<Vector2Int, Color> staticVoxelLightByCell = new Dictionary<Vector2Int, Color>();
        private readonly HashSet<Vector2Int> heroTintedCells = new HashSet<Vector2Int>();
        private readonly HashSet<Vector2Int> nextHeroTintedCells = new HashSet<Vector2Int>();
        private readonly HashSet<Vector2Int> heroTintCandidates = new HashSet<Vector2Int>();
        private int appliedHeroTintSignature = int.MinValue;

        private void ApplyStaticVoxelLightGrid(MazeGenerationResult result)
        {
            if (!VoxelVisuals.Enabled || result == null || result.Grid == null)
            {
                return;
            }

            staticVoxelLightByCell.Clear();
            heroTintedCells.Clear();
            nextHeroTintedCells.Clear();
            heroTintCandidates.Clear();
            appliedHeroTintSignature = int.MinValue;
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

        public void ApplyHeroLightTints(
            IReadOnlyList<HeroController> heroes,
            MazeGrid grid,
            HashSet<Vector2Int> visibleCells)
        {
            if (!VoxelVisuals.Enabled || heroes == null || grid == null || visibleCells == null || visibleCells.Count == 0)
            {
                ClearHeroLightTints();
                return;
            }

            var signature = CalculateHeroTintSignature(heroes, visibleCells);
            if (appliedHeroTintSignature == signature)
            {
                return;
            }

            heroTintCandidates.Clear();
            nextHeroTintedCells.Clear();
            foreach (var cell in visibleCells)
            {
                AddHeroTintCandidate(grid, cell);
                foreach (var neighbor in EightNeighbors(cell))
                {
                    AddHeroTintCandidate(grid, neighbor);
                }
            }

            foreach (var cell in heroTintCandidates)
            {
                if (!cellRenderers.ContainsKey(cell))
                {
                    continue;
                }

                var strength = CalculateHeroTorchVoxelStrength(heroes, grid, visibleCells, cell);
                if (strength <= 0.015f)
                {
                    continue;
                }

                ApplyCellVoxelTint(cell, BlendHeroTorchTint(GetBaseVoxelTint(cell), strength));
                nextHeroTintedCells.Add(cell);
            }

            foreach (var cell in heroTintedCells)
            {
                if (!nextHeroTintedCells.Contains(cell))
                {
                    ApplyCellVoxelTint(cell, GetBaseVoxelTint(cell));
                }
            }

            heroTintedCells.Clear();
            foreach (var cell in nextHeroTintedCells)
            {
                heroTintedCells.Add(cell);
            }

            appliedHeroTintSignature = signature;
        }

        public void ClearHeroLightTints()
        {
            if (heroTintedCells.Count == 0)
            {
                return;
            }

            foreach (var cell in heroTintedCells)
            {
                ApplyCellVoxelTint(cell, GetBaseVoxelTint(cell));
            }

            heroTintedCells.Clear();
            nextHeroTintedCells.Clear();
            heroTintCandidates.Clear();
            appliedHeroTintSignature = int.MinValue;
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

        private void AddHeroTintCandidate(MazeGrid grid, Vector2Int cell)
        {
            if (!grid.InBounds(cell) || !cellRenderers.ContainsKey(cell))
            {
                return;
            }

            var type = grid.Get(cell).Type;
            if (grid.Get(cell).IsStructurallyPassable
                || type == MazeCellType.Wall
                || type == MazeCellType.ClosedDoor
                || type == MazeCellType.LockedDownStairs)
            {
                heroTintCandidates.Add(cell);
            }
        }

        private float CalculateHeroTorchVoxelStrength(
            IReadOnlyList<HeroController> heroes,
            MazeGrid grid,
            HashSet<Vector2Int> visibleCells,
            Vector2Int cell)
        {
            var strongest = 0f;
            for (var i = 0; i < heroes.Count; i++)
            {
                var hero = heroes[i];
                if (hero == null || hero.Model == null || !hero.ProvidesVisibility)
                {
                    continue;
                }

                var origin = hero.Model.Position;
                var distance = Vector2Int.Distance(origin, cell);
                var radius = Mathf.Max(HeroVisibility.SightRange + HeroTorchVoxelRadiusBonus, hero.Model.SightRange + HeroTorchVoxelRadiusBonus);
                if (distance > radius)
                {
                    continue;
                }

                var normalized = Mathf.Clamp01(1f - distance / radius);
                var falloff = normalized * normalized * (3f - 2f * normalized);
                var lineWeight = HasTorchLine(grid, origin, cell)
                    ? 1f
                    : (HasVisibleNeighbor(visibleCells, cell) ? HeroTorchCornerSpillStrength : 0f);
                var wallBoost = IsTallLightCell(grid, cell) ? 1.02f : 0.82f;
                strongest = Mathf.Max(strongest, falloff * lineWeight * wallBoost);
            }

            return Mathf.Clamp01(strongest);
        }

        private void ApplyCellVoxelTint(Vector2Int cell, Color tint)
        {
            if (!cellRenderers.TryGetValue(cell, out var renderers))
            {
                return;
            }

            for (var i = 0; i < renderers.Count; i++)
            {
                VoxelVisuals.ApplyVoxelLightTint(renderers[i], tint);
            }
        }

        private Color GetBaseVoxelTint(Vector2Int cell)
        {
            return staticVoxelLightByCell.TryGetValue(cell, out var tint) ? tint : VoxelAmbientLight;
        }

        private static Color BlendHeroTorchTint(Color baseTint, float strength)
        {
            var warmth = HeroTorchVoxelStrength * Mathf.Clamp01(strength);
            return ClampLight(new Color(
                baseTint.r + VoxelHeroTorchLight.r * warmth,
                baseTint.g + VoxelHeroTorchLight.g * warmth,
                baseTint.b + VoxelHeroTorchLight.b * warmth * 0.55f,
                1f));
        }

        private static bool HasTorchLine(MazeGrid grid, Vector2Int origin, Vector2Int target)
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

                if (current == target)
                {
                    return true;
                }

                if (IsTallLightCell(grid, current))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool HasVisibleNeighbor(HashSet<Vector2Int> visibleCells, Vector2Int cell)
        {
            foreach (var neighbor in EightNeighbors(cell))
            {
                if (visibleCells.Contains(neighbor))
                {
                    return true;
                }
            }

            return false;
        }

        private static IEnumerable<Vector2Int> EightNeighbors(Vector2Int cell)
        {
            for (var x = -1; x <= 1; x++)
            {
                for (var y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0)
                    {
                        continue;
                    }

                    yield return new Vector2Int(cell.x + x, cell.y + y);
                }
            }
        }

        private static bool IsTallLightCell(MazeGrid grid, Vector2Int cell)
        {
            if (grid == null || !grid.InBounds(cell))
            {
                return false;
            }

            var type = grid.Get(cell).Type;
            return type == MazeCellType.Wall
                || type == MazeCellType.ClosedDoor
                || type == MazeCellType.LockedDownStairs;
        }

        private static int CalculateHeroTintSignature(IReadOnlyList<HeroController> heroes, HashSet<Vector2Int> visibleCells)
        {
            unchecked
            {
                var hash = 23 + visibleCells.Count * 397;
                for (var i = 0; i < heroes.Count; i++)
                {
                    var hero = heroes[i];
                    if (hero == null || hero.Model == null || !hero.ProvidesVisibility)
                    {
                        continue;
                    }

                    hash = hash * 31 + hero.Model.Position.x;
                    hash = hash * 31 + hero.Model.Position.y;
                    hash = hash * 31 + hero.Model.SightRange;
                }

                return hash;
            }
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
    }
}
