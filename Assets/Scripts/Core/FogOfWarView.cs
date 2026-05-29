using System.Collections.Generic;
using Labyrinth.Maze;
using UnityEngine;

namespace Labyrinth.Core
{
    public sealed class FogOfWarView : MonoBehaviour
    {
        private readonly Dictionary<Vector2Int, GameObject> overlays = new Dictionary<Vector2Int, GameObject>();

        private MazeRenderer mazeRenderer;
        private Material floorFogMaterial;
        private Material wallFogMaterial;
        private Material wallTopFogMaterial;

        public void Configure(MazeRenderer renderer)
        {
            mazeRenderer = renderer;
            floorFogMaterial = CreateMaterial("Known Floor Fog", new Color(0.125f, 0.14f, 0.165f, 1f));
            wallFogMaterial = CreateMaterial("Known Wall Fog Body", new Color(0.07f, 0.078f, 0.095f, 1f));
            wallTopFogMaterial = CreateMaterial("Known Wall Fog Top", new Color(0.18f, 0.19f, 0.215f, 1f));
        }

        public void Show(MazeGrid grid, HashSet<Vector2Int> exploredCells, HashSet<Vector2Int> visibleCells)
        {
            if (grid == null || mazeRenderer == null || exploredCells == null)
            {
                Hide();
                return;
            }

            foreach (var position in exploredCells)
            {
                if (!grid.InBounds(position) || (visibleCells != null && visibleCells.Contains(position)))
                {
                    continue;
                }

                SetOverlayActive(EnsureOverlay(grid, position), true);
            }

            foreach (var pair in overlays)
            {
                var shouldShow = exploredCells.Contains(pair.Key)
                    && (visibleCells == null || !visibleCells.Contains(pair.Key));
                SetOverlayActive(pair.Value, shouldShow);
            }
        }

        public void Hide()
        {
            foreach (var overlay in overlays.Values)
            {
                if (overlay != null)
                {
                    SetOverlayActive(overlay, false);
                }
            }
        }

        public void Clear()
        {
            foreach (var overlay in overlays.Values)
            {
                if (overlay != null)
                {
                    Destroy(overlay);
                }
            }

            overlays.Clear();
        }

        private GameObject EnsureOverlay(MazeGrid grid, Vector2Int position)
        {
            if (overlays.TryGetValue(position, out var overlay) && overlay != null)
            {
                return overlay;
            }

            overlay = new GameObject($"Fog Of War {position.x},{position.y}");
            overlay.name = $"Fog Of War {position.x},{position.y}";
            overlay.transform.SetParent(transform, false);
            var cellSize = mazeRenderer.CellSize;
            if (IsTallFogCell(grid, position))
            {
                CreateWallOverlay(overlay.transform, position, cellSize);
            }
            else
            {
                CreateCube(
                    "Known Floor Fog",
                    overlay.transform,
                    mazeRenderer.GridToWorld(position) + new Vector3(0f, cellSize * 0.055f, 0f),
                    new Vector3(cellSize * 0.96f, cellSize * 0.025f, cellSize * 0.96f),
                    floorFogMaterial);
            }

            overlays[position] = overlay;
            return overlay;
        }

        private static void SetOverlayActive(GameObject overlay, bool active)
        {
            if (overlay != null && overlay.activeSelf != active)
            {
                overlay.SetActive(active);
            }
        }

        private void CreateWallOverlay(Transform parent, Vector2Int position, float cellSize)
        {
            var world = mazeRenderer.GridToWorld(position);
            var wallHeight = mazeRenderer.WallHeight;
            CreateCube(
                "Known Wall Fog Body",
                parent,
                world + new Vector3(0f, wallHeight * 0.48f, 0f),
                new Vector3(cellSize * 0.99f, wallHeight * 0.96f, cellSize * 0.99f),
                wallFogMaterial);
            CreateCube(
                "Known Wall Fog Top",
                parent,
                world + new Vector3(0f, wallHeight * 0.985f, 0f),
                new Vector3(cellSize * 0.86f, cellSize * 0.045f, cellSize * 0.86f),
                wallTopFogMaterial);
        }

        private static void CreateCube(
            string name,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Material material)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.position = position;
            cube.transform.localScale = scale;
            cube.GetComponent<Renderer>().sharedMaterial = material;

            var collider = cube.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }
        }

        private static bool IsTallFogCell(MazeGrid grid, Vector2Int position)
        {
            if (grid == null || !grid.InBounds(position))
            {
                return false;
            }

            var type = grid.Get(position).Type;
            return type == MazeCellType.Wall
                || type == MazeCellType.ClosedDoor
                || type == MazeCellType.LockedDownStairs;
        }

        private static Material CreateMaterial(string materialName, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            var material = new Material(shader)
            {
                name = materialName,
                color = color
            };

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", material.color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", material.color);
            }

            return material;
        }
    }
}
