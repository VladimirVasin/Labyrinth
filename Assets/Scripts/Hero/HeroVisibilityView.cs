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
        private const int LightOverlaySubdivisions = 6;
        private const float LightOverlayHeightCells = 0.118f;
        private const float LightOverlayRadiusBonusCells = 2.15f;
        private const float LightOverlayMaxAlpha = 0.24f;
        private const float WallLightOverlayMaxAlpha = 0.16f;
        private const float LightOverlaySpillWeight = 0.26f;
        private const float LightOverlayDeepSpillWeight = 0.07f;
        private const float HeroLightBaseIntensity = 3.15f;

        private readonly Dictionary<Vector2Int, GameObject> markers = new Dictionary<Vector2Int, GameObject>();
        private readonly HashSet<Vector2Int> shownCells = new HashSet<Vector2Int>();
        private readonly Dictionary<Vector2Int, float> lightOverlayWeights = new Dictionary<Vector2Int, float>();
        private readonly HashSet<Vector2Int> lightOverlayCells = new HashSet<Vector2Int>();
        private readonly List<Vector2Int> lightOverlayBuffer = new List<Vector2Int>();
        private readonly List<SoftLightSource> lightSources = new List<SoftLightSource>();
        private readonly List<Vector3> lightOverlayVertices = new List<Vector3>();
        private readonly List<Color32> lightOverlayColors = new List<Color32>();
        private readonly List<int> lightOverlayTriangles = new List<int>();
        private readonly List<HeroController> singleHeroBuffer = new List<HeroController>(1);

        private MazeRenderer mazeRenderer;
        private Material pathVisibilityMaterial;
        private Material wallVisibilityMaterial;
        private Material lightOverlayMaterial;
        private Light heroLight;
        private readonly List<Light> heroLights = new List<Light>();
        private GameObject lightOverlayObject;
        private Mesh lightOverlayMesh;
        private HeroVisibilityDisplayMode mode = HeroVisibilityDisplayMode.Schematic;
        private HeroVisibilityDisplayMode renderedMode = HeroVisibilityDisplayMode.Schematic;
        private int renderedLightSignature = int.MinValue;
        private float lightFlickerPhase;

        private readonly struct SoftLightSource
        {
            public SoftLightSource(Vector2 worldPosition, float radius)
            {
                WorldPosition = worldPosition;
                Radius = radius;
            }

            public Vector2 WorldPosition { get; }

            public float Radius { get; }
        }

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
                HideSoftLightingOverlay();
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

            if (mode == HeroVisibilityDisplayMode.Lighting)
            {
                singleHeroBuffer.Clear();
                singleHeroBuffer.Add(hero);
                var visibleCells = CollectVisibleCells(singleHeroBuffer, grid);
                var lightSignature = CalculateLightSourceSignature(singleHeroBuffer);
                if (!Matches(visibility, grid)
                    || renderedLightSignature != lightSignature
                    || lightOverlayObject == null
                    || !lightOverlayObject.activeSelf)
                {
                    RenderSoftLightingOverlay(singleHeroBuffer, grid, visibleCells, lightSignature);
                }
            }
            else
            {
                HideSoftLightingOverlay();
            }

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

        public void ShowLighting(IReadOnlyList<HeroController> heroes, MazeGrid grid)
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
                HideSoftLightingOverlay();
                renderedMode = mode;
                return;
            }

            var visibleCells = CollectVisibleCells(heroes, grid);
            var lightSignature = CalculateLightSourceSignature(heroes);
            var sameVisibility = Matches(visibleCells);
            if (!sameVisibility
                || renderedLightSignature != lightSignature
                || lightOverlayObject == null
                || !lightOverlayObject.activeSelf)
            {
                RenderSoftLightingOverlay(heroes, grid, visibleCells, lightSignature);
            }

            if (sameVisibility)
            {
                return;
            }

            ClearMarkers();
            foreach (var position in visibleCells)
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
                HideSoftLightingOverlay();
                renderedMode = mode;
                return;
            }

            HideSoftLightingOverlay();
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
                HideSoftLightingOverlay();
                renderedMode = mode;
                return;
            }

            HideSoftLightingOverlay();
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
            HideSoftLightingOverlay();
        }

        private void Initialize(MazeRenderer renderer)
        {
            mazeRenderer = renderer;
            pathVisibilityMaterial = CreateMaterial("Hero Visible Path", new Color(0.26f, 0.78f, 1f));
            wallVisibilityMaterial = CreateMaterial("Hero Visible Wall", new Color(1f, 0.86f, 0.28f));
            heroLight = CreateHeroLight();
            lightOverlayMaterial = CreateSoftLightingMaterial();
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

        private void RenderSoftLightingOverlay(
            IReadOnlyList<HeroController> heroes,
            MazeGrid grid,
            HashSet<Vector2Int> visibleCells,
            int lightSignature)
        {
            if (mode != HeroVisibilityDisplayMode.Lighting
                || heroes == null
                || grid == null
                || visibleCells == null
                || visibleCells.Count == 0)
            {
                HideSoftLightingOverlay();
                return;
            }

            BuildLightSources(heroes);
            if (lightSources.Count == 0)
            {
                HideSoftLightingOverlay();
                return;
            }

            BuildLightOverlayCells(visibleCells, grid);
            if (lightOverlayCells.Count == 0)
            {
                HideSoftLightingOverlay();
                return;
            }

            EnsureSoftLightingOverlay();
            lightOverlayVertices.Clear();
            lightOverlayColors.Clear();
            lightOverlayTriangles.Clear();

            var cellSize = mazeRenderer.CellSize;

            foreach (var cell in lightOverlayCells)
            {
                if (!CanRenderSoftLight(grid, cell))
                {
                    continue;
                }

                var isWallGlow = IsLightableWall(grid, cell);
                var center = mazeRenderer.GridToWorld(cell);
                var coverage = isWallGlow ? 0.7f : 1.02f;
                var halfCell = cellSize * 0.5f * coverage;
                var step = (halfCell * 2f) / LightOverlaySubdivisions;
                var y = isWallGlow
                    ? mazeRenderer.WallHeight + cellSize * 0.022f
                    : cellSize * LightOverlayHeightCells;
                var startX = center.x - halfCell;
                var startZ = center.z - halfCell;

                for (var x = 0; x < LightOverlaySubdivisions; x++)
                {
                    for (var z = 0; z < LightOverlaySubdivisions; z++)
                    {
                        var p0 = new Vector3(startX + x * step, y, startZ + z * step);
                        var p1 = new Vector3(startX + (x + 1) * step, y, startZ + z * step);
                        var p2 = new Vector3(startX + (x + 1) * step, y, startZ + (z + 1) * step);
                        var p3 = new Vector3(startX + x * step, y, startZ + (z + 1) * step);

                        AddLightOverlayQuad(p0, p1, p2, p3, isWallGlow);
                    }
                }
            }

            if (lightOverlayVertices.Count == 0)
            {
                HideSoftLightingOverlay();
                return;
            }

            lightOverlayMesh.Clear();
            lightOverlayMesh.SetVertices(lightOverlayVertices);
            lightOverlayMesh.SetColors(lightOverlayColors);
            lightOverlayMesh.SetTriangles(lightOverlayTriangles, 0);
            lightOverlayMesh.RecalculateBounds();
            lightOverlayObject.SetActive(true);
            renderedLightSignature = lightSignature;
        }

        private void BuildLightSources(IReadOnlyList<HeroController> heroes)
        {
            lightSources.Clear();
            foreach (var hero in heroes)
            {
                if (hero == null || hero.Model == null || !hero.ProvidesVisibility)
                {
                    continue;
                }

                var world = mazeRenderer.GridToWorld(hero.Model.Position);
                var radiusCells = Mathf.Max(HeroVisibility.SightRange + LightOverlayRadiusBonusCells, hero.Model.SightRange + LightOverlayRadiusBonusCells);
                lightSources.Add(new SoftLightSource(new Vector2(world.x, world.z), radiusCells * mazeRenderer.CellSize));
            }
        }

        private void BuildLightOverlayCells(HashSet<Vector2Int> visibleCells, MazeGrid grid)
        {
            lightOverlayWeights.Clear();
            lightOverlayCells.Clear();
            lightOverlayBuffer.Clear();

            foreach (var position in visibleCells)
            {
                if (!CanRenderSoftLight(grid, position))
                {
                    continue;
                }

                if (IsLightableWall(grid, position))
                {
                    SetLightOverlayWeight(position, 0.5f);
                    continue;
                }

                SetLightOverlayWeight(position, 0.82f);
                if (grid.Get(position).IsStructurallyPassable)
                {
                    lightOverlayBuffer.Add(position);
                }
            }

            var originalCount = lightOverlayBuffer.Count;
            for (var i = 0; i < originalCount; i++)
            {
                foreach (var neighbor in LightNeighbors(lightOverlayBuffer[i]))
                {
                    if (!CanRenderSoftLight(grid, neighbor))
                    {
                        continue;
                    }

                    SetLightOverlayWeight(neighbor, IsLightableWall(grid, neighbor) ? LightOverlaySpillWeight * 0.82f : LightOverlaySpillWeight);
                    if (grid.Get(neighbor).IsStructurallyPassable)
                    {
                        lightOverlayBuffer.Add(neighbor);
                    }
                }
            }

            var spillCount = lightOverlayBuffer.Count;
            for (var i = originalCount; i < spillCount; i++)
            {
                foreach (var neighbor in LightNeighbors(lightOverlayBuffer[i]))
                {
                    if (CanRenderSoftLight(grid, neighbor))
                    {
                        SetLightOverlayWeight(neighbor, IsLightableWall(grid, neighbor) ? LightOverlayDeepSpillWeight * 0.65f : LightOverlayDeepSpillWeight);
                    }
                }
            }

            lightOverlayBuffer.Clear();
            foreach (var pair in lightOverlayWeights)
            {
                if (!CanRenderSoftLight(grid, pair.Key))
                {
                    continue;
                }

                lightOverlayCells.Add(pair.Key);
                foreach (var neighbor in LightNeighbors(pair.Key))
                {
                    if (CanRenderSoftLight(grid, neighbor))
                    {
                        lightOverlayCells.Add(neighbor);
                    }
                }
            }
        }

        private static IEnumerable<Vector2Int> LightNeighbors(Vector2Int position)
        {
            for (var x = -1; x <= 1; x++)
            {
                for (var y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0)
                    {
                        continue;
                    }

                    yield return new Vector2Int(position.x + x, position.y + y);
                }
            }
        }

        private static bool CanRenderSoftLight(MazeGrid grid, Vector2Int position)
        {
            return grid.InBounds(position)
                && (grid.Get(position).IsStructurallyPassable || IsLightableWall(grid, position));
        }

        private static bool IsLightableWall(MazeGrid grid, Vector2Int position)
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

        private void SetLightOverlayWeight(Vector2Int position, float weight)
        {
            if (lightOverlayWeights.TryGetValue(position, out var current) && current >= weight)
            {
                return;
            }

            lightOverlayWeights[position] = weight;
        }

        private void AddLightOverlayQuad(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, bool wallGlow)
        {
            var c0 = CalculateSoftLightColor(p0, wallGlow);
            var c1 = CalculateSoftLightColor(p1, wallGlow);
            var c2 = CalculateSoftLightColor(p2, wallGlow);
            var c3 = CalculateSoftLightColor(p3, wallGlow);

            if (c0.a == 0 && c1.a == 0 && c2.a == 0 && c3.a == 0)
            {
                return;
            }

            var index = lightOverlayVertices.Count;
            lightOverlayVertices.Add(p0);
            lightOverlayVertices.Add(p1);
            lightOverlayVertices.Add(p2);
            lightOverlayVertices.Add(p3);
            lightOverlayColors.Add(c0);
            lightOverlayColors.Add(c1);
            lightOverlayColors.Add(c2);
            lightOverlayColors.Add(c3);
            lightOverlayTriangles.Add(index);
            lightOverlayTriangles.Add(index + 1);
            lightOverlayTriangles.Add(index + 2);
            lightOverlayTriangles.Add(index);
            lightOverlayTriangles.Add(index + 2);
            lightOverlayTriangles.Add(index + 3);
        }

        private Color32 CalculateSoftLightColor(Vector3 worldPosition, bool wallGlow)
        {
            var pathWeight = CalculateSoftLightPathWeight(worldPosition);
            if (pathWeight <= 0.01f)
            {
                return new Color32(255, 178, 72, 0);
            }

            var point = new Vector2(worldPosition.x, worldPosition.z);
            var strongest = 0f;
            foreach (var source in lightSources)
            {
                var distance = Vector2.Distance(point, source.WorldPosition);
                var normalized = Mathf.Clamp01(1f - distance / source.Radius);
                var falloff = normalized * normalized * (3f - 2f * normalized);
                strongest = Mathf.Max(strongest, falloff);
            }

            var alpha = Mathf.Clamp01(strongest * pathWeight * (wallGlow ? WallLightOverlayMaxAlpha : LightOverlayMaxAlpha));
            var color = wallGlow
                ? new Color32(255, 158, 66, (byte)Mathf.RoundToInt(alpha * 255f))
                : new Color32(255, 184, 86, (byte)Mathf.RoundToInt(alpha * 255f));
            return color;
        }

        private float CalculateSoftLightPathWeight(Vector3 worldPosition)
        {
            var cellSize = mazeRenderer.CellSize;
            var gridPoint = new Vector2(worldPosition.x / cellSize, worldPosition.z / cellSize);
            var baseCell = new Vector2Int(Mathf.RoundToInt(gridPoint.x), Mathf.RoundToInt(gridPoint.y));
            var strongest = 0f;

            for (var x = -1; x <= 1; x++)
            {
                for (var y = -1; y <= 1; y++)
                {
                    var cell = new Vector2Int(baseCell.x + x, baseCell.y + y);
                    if (!lightOverlayWeights.TryGetValue(cell, out var weight))
                    {
                        continue;
                    }

                    var distance = Vector2.Distance(gridPoint, new Vector2(cell.x, cell.y));
                    var normalized = Mathf.Clamp01(1f - distance / 1.42f);
                    var falloff = normalized * normalized * (3f - 2f * normalized);
                    strongest = Mathf.Max(strongest, weight * falloff);
                }
            }

            return strongest;
        }

        private void EnsureSoftLightingOverlay()
        {
            if (lightOverlayObject != null && lightOverlayMesh != null)
            {
                return;
            }

            lightOverlayObject = new GameObject("Hero Soft Light Overlay");
            lightOverlayObject.transform.SetParent(transform, false);
            var meshFilter = lightOverlayObject.AddComponent<MeshFilter>();
            var meshRenderer = lightOverlayObject.AddComponent<MeshRenderer>();
            lightOverlayMesh = new Mesh { name = "Hero Soft Light Overlay Mesh" };
            lightOverlayMesh.MarkDynamic();
            meshFilter.sharedMesh = lightOverlayMesh;
            meshRenderer.sharedMaterial = lightOverlayMaterial != null ? lightOverlayMaterial : CreateSoftLightingMaterial();
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.sortingOrder = 12;
        }

        private void HideSoftLightingOverlay()
        {
            if (lightOverlayObject != null)
            {
                lightOverlayObject.SetActive(false);
            }

            renderedLightSignature = int.MinValue;
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
            if (heroLight == null)
            {
                return;
            }

            heroLight.enabled = mode == HeroVisibilityDisplayMode.Lighting;
            if (!heroLight.enabled)
            {
                SetExtraHeroLightsEnabled(false);
                return;
            }

            heroLight.transform.position = mazeRenderer.GridToWorld(hero.Model.Position) + new Vector3(0f, mazeRenderer.CellSize * 1.1f, 0f);
            SetExtraHeroLightsEnabled(false);
        }

        private void UpdateHeroLights(IReadOnlyList<HeroController> heroes)
        {
            var activeHeroCount = 0;
            if (heroes != null)
            {
                foreach (var hero in heroes)
                {
                    if (hero != null && hero.Model != null && hero.ProvidesVisibility)
                    {
                        activeHeroCount++;
                    }
                }
            }

            EnsureHeroLightCount(activeHeroCount);
            var lightIndex = 0;
            if (heroes != null)
            {
                foreach (var hero in heroes)
                {
                    if (hero == null || hero.Model == null || !hero.ProvidesVisibility)
                    {
                        continue;
                    }

                    var light = heroLights[lightIndex];
                    light.enabled = mode == HeroVisibilityDisplayMode.Lighting;
                    light.transform.position = mazeRenderer.GridToWorld(hero.Model.Position) + new Vector3(0f, mazeRenderer.CellSize * 1.1f, 0f);
                    lightIndex++;
                }
            }

            for (var i = lightIndex; i < heroLights.Count; i++)
            {
                heroLights[i].enabled = false;
            }
        }

        private void EnsureHeroLightCount(int count)
        {
            while (heroLights.Count < count)
            {
                heroLights.Add(CreateHeroLight($"Hero Light {heroLights.Count + 1}"));
            }
        }

        private void SetExtraHeroLightsEnabled(bool enabled)
        {
            foreach (var light in heroLights)
            {
                if (light != null)
                {
                    light.enabled = enabled;
                }
            }
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
            if (heroLight != null)
            {
                heroLight.enabled = false;
            }

            SetExtraHeroLightsEnabled(false);
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

        private static Material CreateSoftLightingMaterial()
        {
            var shader = Shader.Find("Labyrinth/Voxel Soft Light Overlay");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            if (shader == null)
            {
                shader = Shader.Find("Unlit/Transparent");
            }

            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            }

            var material = new Material(shader)
            {
                name = "Hero Soft Light Overlay",
                color = Color.white
            };

            material.renderQueue = 3100;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", Color.white);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", Color.white);
            }

            return material;
        }

        private Light CreateHeroLight()
        {
            return CreateHeroLight("Selected Hero Light");
        }

        private Light CreateHeroLight(string lightName)
        {
            var lightObject = new GameObject(lightName);
            lightObject.transform.SetParent(transform, false);

            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.68f, 0.34f);
            light.range = mazeRenderer.CellSize * 3.25f;
            light.intensity = HeroLightBaseIntensity;
            light.shadows = LightShadows.None;
            light.bounceIntensity = 0.38f;
            light.enabled = false;
            return light;
        }

        private void Update()
        {
            if (mode != HeroVisibilityDisplayMode.Lighting)
            {
                return;
            }

            lightFlickerPhase += Time.deltaTime * 4.8f;
            var flicker = 0.965f
                + Mathf.Sin(lightFlickerPhase) * 0.035f
                + Mathf.Sin(lightFlickerPhase * 2.17f + 1.3f) * 0.018f;
            var tint = new Color(1f, 0.96f, 0.84f, Mathf.Clamp(flicker, 0.9f, 1.02f));
            if (lightOverlayMaterial != null)
            {
                if (lightOverlayMaterial.HasProperty("_Color"))
                {
                    lightOverlayMaterial.SetColor("_Color", tint);
                }

                if (lightOverlayMaterial.HasProperty("_BaseColor"))
                {
                    lightOverlayMaterial.SetColor("_BaseColor", tint);
                }
            }

            ApplyLightFlicker(heroLight, flicker);
            for (var i = 0; i < heroLights.Count; i++)
            {
                ApplyLightFlicker(heroLights[i], flicker);
            }
        }

        private static void ApplyLightFlicker(Light light, float flicker)
        {
            if (light != null)
            {
                light.intensity = HeroLightBaseIntensity * Mathf.Clamp(flicker, 0.88f, 1.08f);
            }
        }

        private static int CalculateLightSourceSignature(IReadOnlyList<HeroController> heroes)
        {
            unchecked
            {
                var hash = 17;
                if (heroes == null)
                {
                    return hash;
                }

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
    }
}
