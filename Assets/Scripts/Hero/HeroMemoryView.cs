using System.Collections.Generic;
using Labyrinth.Maze;
using UnityEngine;

namespace Labyrinth.Hero
{
    public sealed class HeroMemoryView : MonoBehaviour
    {
        private readonly HashSet<Vector2Int> shownCells = new HashSet<Vector2Int>();
        private readonly Dictionary<Vector2Int, GameObject> markers = new Dictionary<Vector2Int, GameObject>();

        private MazeRenderer mazeRenderer;
        private Material rememberedMaterial;
        private HeroMemory sourceMemory;
        private int sourceRememberedCount = -1;

        public static HeroMemoryView Create(MazeRenderer mazeRenderer)
        {
            var root = new GameObject("HeroMemoryView");
            var view = root.AddComponent<HeroMemoryView>();
            view.Initialize(mazeRenderer);
            return view;
        }

        public void ShowRemembered(Vector2Int position)
        {
            if (!shownCells.Add(position))
            {
                return;
            }

            var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = "Remembered Cell";
            marker.transform.SetParent(transform, false);
            var cellSize = mazeRenderer.CellSize;
            marker.transform.position = mazeRenderer.GridToWorld(position) + new Vector3(0f, cellSize * 0.035f, 0f);
            marker.transform.localScale = new Vector3(cellSize * 0.38f, cellSize * 0.035f, cellSize * 0.38f);
            marker.GetComponent<Renderer>().sharedMaterial = rememberedMaterial;
            markers[position] = marker;

            var collider = marker.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }
        }

        public void ShowMemory(HeroMemory memory)
        {
            if (memory == null)
            {
                ClearMarkers();
                sourceMemory = null;
                sourceRememberedCount = -1;
                return;
            }

            if (sourceMemory == memory && sourceRememberedCount == memory.RememberedCount)
            {
                return;
            }

            ClearMarkers();
            sourceMemory = memory;
            sourceRememberedCount = memory.RememberedCount;
            foreach (var position in memory.RememberedCells)
            {
                ShowRemembered(position);
            }
        }

        public void ShowAllRemembered()
        {
            foreach (var marker in markers.Values)
            {
                SetMarkerVisible(marker, true);
            }
        }

        public void ApplyVisibility(HashSet<Vector2Int> visibleCells)
        {
            if (visibleCells == null)
            {
                ShowAllRemembered();
                return;
            }

            foreach (var pair in markers)
            {
                SetMarkerVisible(pair.Value, visibleCells.Contains(pair.Key));
            }
        }

        private void Initialize(MazeRenderer renderer)
        {
            mazeRenderer = renderer;
            rememberedMaterial = CreateMaterial();
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

        private static Material CreateMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            var material = new Material(shader)
            {
                name = "Hero Remembered Cell",
                color = new Color(0.2f, 0.55f, 1f)
            };

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", material.color);
            }

            return material;
        }

        private static void SetMarkerVisible(GameObject marker, bool visible)
        {
            if (marker == null)
            {
                return;
            }

            foreach (var renderer in marker.GetComponentsInChildren<Renderer>())
            {
                renderer.enabled = visible;
            }
        }
    }
}
