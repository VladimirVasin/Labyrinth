using System.Collections.Generic;
using Labyrinth.Maze;
using UnityEngine;

namespace Labyrinth.Hero
{
    public enum HeroVisibilityDisplayMode
    {
        Schematic,
        Lighting
    }

    public sealed class HeroVisibilityView : MonoBehaviour
    {
        private readonly Dictionary<Vector2Int, GameObject> markers = new Dictionary<Vector2Int, GameObject>();
        private readonly HashSet<Vector2Int> shownCells = new HashSet<Vector2Int>();

        private MazeRenderer mazeRenderer;
        private Material pathVisibilityMaterial;
        private Material wallVisibilityMaterial;
        private HeroVisibilityDisplayMode mode = HeroVisibilityDisplayMode.Schematic;
        private HeroVisibilityDisplayMode renderedMode = HeroVisibilityDisplayMode.Schematic;

        public static HeroVisibilityView Create(MazeRenderer renderer)
        {
            var root = new GameObject("HeroVisibilityView");
            var view = root.AddComponent<HeroVisibilityView>();
            view.Initialize(renderer);
            return view;
        }

        public HeroVisibilityDisplayMode Mode => mode;

        public void SetMode(HeroVisibilityDisplayMode displayMode)
        {
            if (mode == displayMode)
            {
                return;
            }

            mode = displayMode;
            ClearMarkers();
            if (mode == HeroVisibilityDisplayMode.Schematic)
            {
                DisableHeroLights();
            }
        }

        public void Show(HeroController hero, MazeGrid grid)
        {
            if (hero == null || hero.Model == null || grid == null || !hero.ProvidesVisibility)
            {
                Hide();
                return;
            }

            UpdateHeroLight(hero);
            var visibility = hero.Model.Visibility;

            if (Matches(visibility, grid))
            {
                return;
            }

            ClearMarkers();

            if (mode == HeroVisibilityDisplayMode.Schematic)
            {
                RenderSchematicVisibility(visibility, grid);
            }
            else
            {
                RenderLightingVisibility(visibility, grid);
            }

            renderedMode = mode;
        }

        public void ShowLighting(IReadOnlyList<HeroController> heroes, MazeGrid grid, HashSet<Vector2Int> visibleCells = null)
        {
            if (grid == null)
            {
                Hide();
                return;
            }

            UpdateHeroLights(heroes);
            if (mode != HeroVisibilityDisplayMode.Lighting)
            {
                ClearMarkers();
                renderedMode = mode;
                return;
            }

            var displayedCells = visibleCells ?? CollectVisibleCells(heroes, grid);
            var sameVisibility = Matches(displayedCells);

            if (sameVisibility)
            {
                return;
            }

            ClearMarkers();
            foreach (var position in displayedCells)
            {
                shownCells.Add(position);
            }

            renderedMode = mode;
        }

        public void ShowSchematic(IReadOnlyList<HeroController> heroes, MazeGrid grid)
        {
            if (grid == null)
            {
                Hide();
                return;
            }

            DisableHeroLights();
            if (mode != HeroVisibilityDisplayMode.Schematic)
            {
                ClearMarkers();
                renderedMode = mode;
                return;
            }

            var visibleCells = CollectVisibleCells(heroes, grid);
            if (Matches(visibleCells))
            {
                return;
            }

            ClearMarkers();
            RenderSchematicVisibility(visibleCells, grid);
            renderedMode = mode;
        }

        public void ShowSchematic(HashSet<Vector2Int> visibleCells, MazeGrid grid)
        {
            if (grid == null || visibleCells == null)
            {
                Hide();
                return;
            }

            DisableHeroLights();
            if (mode != HeroVisibilityDisplayMode.Schematic)
            {
                ClearMarkers();
                renderedMode = mode;
                return;
            }

            if (Matches(visibleCells))
            {
                return;
            }

            ClearMarkers();
            RenderSchematicVisibility(visibleCells, grid);
            renderedMode = mode;
        }

        public void Hide()
        {
            ClearMarkers();
            DisableHeroLights();
        }

        private void Initialize(MazeRenderer renderer)
        {
            mazeRenderer = renderer;
            pathVisibilityMaterial = CreateMaterial("Hero Visible Path", new Color(0.26f, 0.78f, 1f));
            wallVisibilityMaterial = CreateMaterial("Hero Visible Wall", new Color(1f, 0.86f, 0.28f));
        }

        private bool Matches(HeroVisibility visibility, MazeGrid grid)
        {
            if (renderedMode != mode)
            {
                return false;
            }

            if (mode == HeroVisibilityDisplayMode.Schematic)
            {
                if (shownCells.Count != visibility.VisibleCount)
                {
                    return false;
                }

                foreach (var position in visibility.VisibleCells)
                {
                    if (!shownCells.Contains(position))
                    {
                        return false;
                    }
                }

                return true;
            }

            if (shownCells.Count != visibility.VisibleCount)
            {
                return false;
            }

            foreach (var position in visibility.VisibleCells)
            {
                if (!shownCells.Contains(position))
                {
                    return false;
                }
            }

            return true;
        }

        private bool Matches(HashSet<Vector2Int> visibleCells)
        {
            if (renderedMode != mode || shownCells.Count != visibleCells.Count)
            {
                return false;
            }

            foreach (var position in visibleCells)
            {
                if (!shownCells.Contains(position))
                {
                    return false;
                }
            }

            return true;
        }

        private void RenderSchematicVisibility(HeroVisibility visibility, MazeGrid grid)
        {
            foreach (var position in visibility.VisibleCells)
            {
                if (!grid.InBounds(position))
                {
                    continue;
                }

                CreateMarker(position, grid.Get(position).Type == MazeCellType.Wall);
                shownCells.Add(position);
            }
        }

        private void RenderSchematicVisibility(HashSet<Vector2Int> visibleCells, MazeGrid grid)
        {
            foreach (var position in visibleCells)
            {
                if (!grid.InBounds(position))
                {
                    continue;
                }

                CreateMarker(position, grid.Get(position).Type == MazeCellType.Wall);
                shownCells.Add(position);
            }
        }

        private void RenderLightingVisibility(HeroVisibility visibility, MazeGrid grid)
        {
            foreach (var position in visibility.VisibleCells)
            {
                shownCells.Add(position);
            }
        }

        private void CreateMarker(Vector2Int gridPosition, bool isWall)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = isWall ? "Visible Wall Cell" : "Visible Path Cell";
            marker.transform.SetParent(transform, false);

            var cellSize = mazeRenderer.CellSize;
            var y = isWall ? mazeRenderer.WallHeight + cellSize * 0.055f : cellSize * 0.082f;
            marker.transform.position = mazeRenderer.GridToWorld(gridPosition) + new Vector3(0f, y, 0f);
            marker.transform.localScale = isWall
                ? new Vector3(cellSize * 0.62f, cellSize * 0.08f, cellSize * 0.62f)
                : new Vector3(cellSize * 0.68f, cellSize * 0.045f, cellSize * 0.68f);
            marker.GetComponent<Renderer>().sharedMaterial = isWall ? wallVisibilityMaterial : pathVisibilityMaterial;
            markers[gridPosition] = marker;

            var collider = marker.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }
        }

        private void UpdateHeroLight(HeroController hero)
        {
            // The visible knight model owns the real lantern light. The visibility view now only
            // decides which cells are shown, so we do not stack invisible extra point lights.
            DisableHeroLights();
        }

        private void UpdateHeroLights(IReadOnlyList<HeroController> heroes)
        {
            DisableHeroLights();
        }

        private static HashSet<Vector2Int> CollectVisibleCells(IReadOnlyList<HeroController> heroes, MazeGrid grid)
        {
            var visibleCells = new HashSet<Vector2Int>();
            if (heroes == null)
            {
                return visibleCells;
            }

            foreach (var hero in heroes)
            {
                if (hero == null || hero.Model == null || !hero.ProvidesVisibility)
                {
                    continue;
                }

                foreach (var position in hero.Model.Visibility.VisibleCells)
                {
                    if (grid.InBounds(position))
                    {
                        visibleCells.Add(position);
                    }
                }
            }

            return visibleCells;
        }

        private void DisableHeroLights()
        {
        }

        private void ClearMarkers()
        {
            foreach (var marker in markers.Values)
            {
                if (marker != null)
                {
                    Destroy(marker);
                }
            }

            markers.Clear();
            shownCells.Clear();
        }

        private static Material CreateMaterial(string materialName, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            var material = new Material(shader)
            {
                name = materialName,
                color = color
            };

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            return material;
        }

    }
}
