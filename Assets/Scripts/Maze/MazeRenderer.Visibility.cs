using System.Collections.Generic;
using Labyrinth.Hero;
using UnityEngine;

namespace Labyrinth.Maze
{
    public sealed partial class MazeRenderer
    {
        public void ShowAllCells()
        {
            foreach (var pair in cellRenderers)
            {
                SetCellRenderersVisible(pair.Key, pair.Value, true);
            }

            RestoreStaticVoxelLightGrid();
            externalVisibilityMaskActive = false;
            currentExternalVisibleCells = null;
            currentExternalVisibilityGrid = null;
            SetLightingFogVisible(false);
        }

        public void TrackExternalCellRenderer(Vector2Int cellPosition, GameObject target)
        {
            TrackCellRenderer(cellPosition, target, true);
        }

        public void ApplyCellVisibility(HeroVisibility visibility, MazeGrid grid)
        {
            if (visibility == null || grid == null)
            {
                ShowAllCells();
                return;
            }

            foreach (var pair in cellRenderers)
            {
                SetCellRenderersVisible(pair.Key, pair.Value, ShouldShowInLightingMode(grid, pair.Key, visibility.IsVisible(pair.Key)));
            }

            SetLightingFogVisible(true);
        }

        public void ApplyCellVisibility(HashSet<Vector2Int> visibleCells, MazeGrid grid)
        {
            if (visibleCells == null || grid == null)
            {
                ShowAllCells();
                return;
            }

            foreach (var pair in cellRenderers)
            {
                SetCellRenderersVisible(pair.Key, pair.Value, ShouldShowInLightingMode(grid, pair.Key, visibleCells.Contains(pair.Key)));
            }

            SetLightingFogVisible(true);
        }

        private void TrackCellRenderer(Vector2Int cellPosition, GameObject target)
        {
            TrackCellRenderer(cellPosition, target, false);
        }

        private void TrackCellRenderer(Vector2Int cellPosition, GameObject target, bool externalObject)
        {
            if (target == null)
            {
                return;
            }

            if (!cellRenderers.TryGetValue(cellPosition, out var renderers))
            {
                renderers = new List<Renderer>();
                cellRenderers[cellPosition] = renderers;
            }

            var trackedRenderers = target.GetComponentsInChildren<Renderer>();
            renderers.AddRange(trackedRenderers);
            TrackProjectedShadowSibling(target, renderers, out var projectedShadowRenderer);
            if (externalObject)
            {
                TrackExternalRenderers(cellPosition, trackedRenderers, projectedShadowRenderer);
            }

            if (cellVisibilityStates.TryGetValue(cellPosition, out var visible))
            {
                SetRenderersEnabled(trackedRenderers, visible);
                if (projectedShadowRenderer != null)
                {
                    projectedShadowRenderer.enabled = visible;
                }
            }
        }

        private void TrackExternalRenderers(
            Vector2Int cellPosition,
            Renderer[] trackedRenderers,
            Renderer projectedShadowRenderer)
        {
            if (!externalCellRenderers.TryGetValue(cellPosition, out var renderers))
            {
                renderers = new List<Renderer>();
                externalCellRenderers[cellPosition] = renderers;
            }

            renderers.AddRange(trackedRenderers);
            if (projectedShadowRenderer != null && !renderers.Contains(projectedShadowRenderer))
            {
                renderers.Add(projectedShadowRenderer);
            }

            if (externalVisibilityMaskActive)
            {
                var visible = IsExternalDungeonObjectVisible(
                    cellPosition,
                    currentExternalVisibleCells,
                    currentExternalVisibilityGrid);
                SetRenderersEnabled(trackedRenderers, visible);
                if (projectedShadowRenderer != null)
                {
                    projectedShadowRenderer.enabled = visible;
                }
            }
        }

        private static void TrackProjectedShadowSibling(
            GameObject target,
            List<Renderer> renderers,
            out Renderer projectedShadowRenderer)
        {
            projectedShadowRenderer = null;
            var parent = target.transform.parent;
            if (parent == null)
            {
                return;
            }

            var projectedShadow = parent.Find($"{target.name} Projected Shadow");
            if (projectedShadow == null)
            {
                return;
            }

            projectedShadowRenderer = projectedShadow.GetComponent<Renderer>();
            if (projectedShadowRenderer == null || renderers.Contains(projectedShadowRenderer))
            {
                return;
            }

            renderers.Add(projectedShadowRenderer);
        }

        private void TrackCellRenderer(Vector2Int cellPosition, Renderer renderer)
        {
            if (renderer == null)
            {
                return;
            }

            if (!cellRenderers.TryGetValue(cellPosition, out var renderers))
            {
                renderers = new List<Renderer>(1);
                cellRenderers[cellPosition] = renderers;
            }

            renderers.Add(renderer);
            if (cellVisibilityStates.TryGetValue(cellPosition, out var visible))
            {
                renderer.enabled = visible;
            }
        }

        private static bool ShouldShowInLightingMode(MazeGrid grid, Vector2Int position, bool isVisible)
        {
            if (!grid.InBounds(position) || isVisible)
            {
                return true;
            }

            return IsOuterBoundaryCell(grid, position);
        }

        private static bool IsOuterBoundaryCell(MazeGrid grid, Vector2Int position)
        {
            return position.x == 0
                || position.y == 0
                || position.x == grid.Width - 1
                || position.y == grid.Height - 1;
        }

        private void CreateLightingFogCover(MazeGrid grid)
        {
            if (grid == null)
            {
                return;
            }

            lightingFogCover = CreateCube(
                "Maze Lighting Fog Cover",
                new Vector3((grid.Width - 1) * cellSize * 0.5f, Scale(-0.035f), (grid.Height - 1) * cellSize * 0.5f),
                new Vector3(grid.Width * cellSize, Scale(0.012f), grid.Height * cellSize),
                lightingFogMaterial,
                root,
                false);
            lightingFogCover.SetActive(false);
        }

        private void SetLightingFogVisible(bool visible)
        {
            if (lightingFogCover != null)
            {
                lightingFogCover.SetActive(visible);
            }
        }

        private void SetCellRenderersVisible(Vector2Int cellPosition, List<Renderer> renderers, bool visible)
        {
            if (cellVisibilityStates.TryGetValue(cellPosition, out var current) && current == visible)
            {
                return;
            }

            SetRenderersEnabled(renderers, visible);
            cellVisibilityStates[cellPosition] = visible;
        }

        private static void SetRenderersEnabled(IEnumerable<Renderer> renderers, bool enabled)
        {
            foreach (var renderer in renderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = enabled;
                }
            }
        }
    }
}
