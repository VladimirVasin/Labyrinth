using System.Collections.Generic;
using Labyrinth.Maze;
using UnityEngine;

namespace Labyrinth.Core
{
    internal static class DungeonLampProfile
    {
        public const float BaseIntensity = 9.2f;
        public const float RangeCells = 6.8f;
        public const float EmissiveIntensity = 1.55f;
        public static readonly Color LightColor = new Color(1f, 0.68f, 0.34f);
        public static readonly Color EmissiveColor = new Color(1f, 0.58f, 0.16f);

        public static Material CreateEmissiveMaterial(string materialName)
        {
            return VoxelVisuals.CreateEmissiveMaterial(materialName, EmissiveColor, EmissiveIntensity);
        }

        public static Light ConfigurePointLight(Light light, float cellSize)
        {
            light.type = LightType.Point;
            light.color = LightColor;
            light.range = cellSize * RangeCells;
            light.intensity = BaseIntensity;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.34f;
            light.shadowBias = 0.04f;
            light.shadowNormalBias = 0.24f;
            light.bounceIntensity = 0.55f;
            light.renderMode = LightRenderMode.ForcePixel;
            return light;
        }

        public static float CalculatePulse(float time)
        {
            return 1f + Mathf.Sin(time * 7.3f) * 0.08f + Mathf.Sin(time * 11.7f) * 0.035f;
        }

        public static float CalculateIntensity(float pulse)
        {
            return BaseIntensity * Mathf.Clamp(pulse, 0.88f, 1.1f);
        }
    }

    public sealed class DungeonFortificationRenderer
    {
        private readonly Dictionary<Vector2Int, GameObject> cellRoots = new Dictionary<Vector2Int, GameObject>();
        private readonly Dictionary<Vector2Int, GameObject> queuedMarkers = new Dictionary<Vector2Int, GameObject>();
        private readonly Transform root;
        private readonly MazeRenderer mazeRenderer;
        private readonly Material hoverValidMaterial;
        private readonly Material hoverInvalidMaterial;
        private readonly Material queuedMaterial;
        private readonly Material plankMaterial;
        private readonly Material beamMaterial;
        private readonly Material flameMaterial;
        private readonly Material workerBodyMaterial;
        private readonly Material workerHeadMaterial;

        private DungeonFortificationRenderer(Transform parent, MazeRenderer renderer)
        {
            mazeRenderer = renderer;
            root = new GameObject("DungeonFortificationRoot").transform;
            root.SetParent(parent, false);
            hoverValidMaterial = CreateMaterial("Fortification Hover Valid", new Color(0.24f, 0.95f, 1f, 0.78f));
            hoverInvalidMaterial = CreateMaterial("Fortification Hover Invalid", new Color(1f, 0.24f, 0.16f, 0.72f));
            queuedMaterial = CreateMaterial("Fortification Queued Cell", new Color(0.25f, 0.72f, 1f));
            plankMaterial = CreateMaterial("Fortification Planks", new Color(0.48f, 0.29f, 0.12f));
            beamMaterial = CreateMaterial("Fortification Beams", new Color(0.25f, 0.13f, 0.05f));
            flameMaterial = DungeonLampProfile.CreateEmissiveMaterial("Fortification Flame");
            workerBodyMaterial = CreateMaterial("Fortification Worker Body", new Color(0.42f, 0.34f, 0.22f));
            workerHeadMaterial = CreateMaterial("Fortification Worker Head", new Color(0.76f, 0.62f, 0.42f));
        }

        public static DungeonFortificationRenderer Create(Transform parent, MazeRenderer renderer)
        {
            return new DungeonFortificationRenderer(parent, renderer);
        }

        public void Clear()
        {
            cellRoots.Clear();
            queuedMarkers.Clear();
            if (root != null)
            {
                Object.Destroy(root.gameObject);
            }
        }

        public GameObject GetCellRoot(Vector2Int cell)
        {
            return EnsureCellRoot(cell);
        }

        public void ShowHoverMarker(Vector2Int cell, bool valid)
        {
            if (!cellRoots.TryGetValue(cell, out var cellRoot) || cellRoot == null)
            {
                cellRoot = EnsureCellRoot(cell);
            }

            var marker = GetOrCreateHoverMarker();
            marker.transform.position = mazeRenderer.GridToWorld(cell) + new Vector3(0f, mazeRenderer.CellSize * 0.12f, 0f);
            marker.transform.localScale = new Vector3(mazeRenderer.CellSize * 0.76f, mazeRenderer.CellSize * 0.04f, mazeRenderer.CellSize * 0.76f);
            marker.GetComponent<Renderer>().sharedMaterial = valid ? hoverValidMaterial : hoverInvalidMaterial;
            marker.SetActive(true);
        }

        public void HideHoverMarker()
        {
            var marker = root.Find("Fortification Hover Marker");
            if (marker != null)
            {
                marker.gameObject.SetActive(false);
            }
        }

        public void RenderQueuedMarker(Vector2Int cell)
        {
            if (queuedMarkers.ContainsKey(cell))
            {
                return;
            }

            var cellRoot = EnsureCellRoot(cell).transform;
            var center = mazeRenderer.GridToWorld(cell);
            var unit = mazeRenderer.CellSize;
            var marker = CreatePart(
                "Queued Fortification Marker",
                PrimitiveType.Cube,
                cellRoot,
                center + new Vector3(0f, unit * 0.075f, 0f),
                new Vector3(unit * 0.54f, unit * 0.035f, unit * 0.54f),
                queuedMaterial);
            queuedMarkers[cell] = marker;
        }

        public void ClearQueuedMarker(Vector2Int cell)
        {
            if (!queuedMarkers.TryGetValue(cell, out var marker))
            {
                return;
            }

            queuedMarkers.Remove(cell);
            if (marker != null)
            {
                Object.Destroy(marker);
            }
        }

        public void RenderFortifiedFloor(Vector2Int cell)
        {
            ClearQueuedMarker(cell);
            var cellRoot = EnsureCellRoot(cell).transform;
            var center = mazeRenderer.GridToWorld(cell);
            var unit = mazeRenderer.CellSize;
            for (var i = -1; i <= 1; i++)
            {
                CreatePart(
                    $"Floor Plank {i + 2}",
                    PrimitiveType.Cube,
                    cellRoot,
                    center + new Vector3(unit * i * 0.24f, unit * 0.018f, 0f),
                    new Vector3(unit * 0.18f, unit * 0.035f, unit * 0.78f),
                    plankMaterial);
            }

            CreatePart(
                "Cross Brace",
                PrimitiveType.Cube,
                cellRoot,
                center + new Vector3(0f, unit * 0.045f, 0f),
                new Vector3(unit * 0.72f, unit * 0.035f, unit * 0.11f),
                beamMaterial);
        }

        public void RenderTorch(Vector2Int cell, Vector2Int wallDirection, int lightRange)
        {
            var cellRoot = EnsureCellRoot(cell).transform;
            var center = mazeRenderer.GridToWorld(cell);
            var unit = mazeRenderer.CellSize;
            var normal = new Vector3(wallDirection.x, 0f, wallDirection.y);
            var side = center + normal * unit * 0.43f;
            var holderPosition = side + new Vector3(0f, mazeRenderer.WallHeight * 0.45f, 0f);
            var flamePosition = side + new Vector3(0f, mazeRenderer.WallHeight * 0.62f, 0f);

            var holder = CreatePart(
                "Wall Torch Holder",
                PrimitiveType.Cube,
                cellRoot,
                holderPosition,
                new Vector3(unit * 0.08f, unit * 0.34f, unit * 0.08f),
                beamMaterial);
            holder.transform.rotation = Quaternion.LookRotation(normal == Vector3.zero ? Vector3.forward : normal, Vector3.up);

            var flame = CreatePart(
                "Wall Torch Flame",
                PrimitiveType.Sphere,
                cellRoot,
                flamePosition,
                Vector3.one * unit * 0.16f,
                flameMaterial);
            flame.transform.localScale = new Vector3(unit * 0.14f, unit * 0.22f, unit * 0.14f);

            var lightObject = new GameObject("Wall Torch Light");
            lightObject.transform.SetParent(cellRoot, false);
            lightObject.transform.position = flamePosition;
            var light = DungeonLampProfile.ConfigurePointLight(lightObject.AddComponent<Light>(), mazeRenderer.CellSize);
            TorchLightFlicker.Attach(light, flame.transform, BuildTorchFlickerSeed(cell));
        }

        private static int BuildTorchFlickerSeed(Vector2Int cell)
        {
            return cell.x * 73856093 ^ cell.y * 19349663 ^ 0x45d9f3b;
        }

        public Transform CreateWorker(Vector3 position)
        {
            var worker = new GameObject("Dungeon Fortification Worker").transform;
            worker.SetParent(root, false);
            worker.position = position;
            var unit = mazeRenderer.ModelUnitSize * 1.18f;
            CreatePart(
                "Worker Body",
                PrimitiveType.Capsule,
                worker,
                new Vector3(0f, unit * 0.32f, 0f),
                new Vector3(unit * 0.2f, unit * 0.32f, unit * 0.2f),
                workerBodyMaterial);
            CreatePart(
                "Worker Head",
                PrimitiveType.Sphere,
                worker,
                new Vector3(0f, unit * 0.74f, 0f),
                Vector3.one * unit * 0.17f,
                workerHeadMaterial);
            CreatePart(
                "Worker Wood Pack",
                PrimitiveType.Cube,
                worker,
                new Vector3(0f, unit * 0.42f, -unit * 0.18f),
                new Vector3(unit * 0.2f, unit * 0.2f, unit * 0.1f),
                plankMaterial);
            return worker;
        }

        public void DestroyWorker(Transform worker)
        {
            if (worker != null)
            {
                Object.Destroy(worker.gameObject);
            }
        }

        private GameObject EnsureCellRoot(Vector2Int cell)
        {
            if (cellRoots.TryGetValue(cell, out var cellRoot) && cellRoot != null)
            {
                return cellRoot;
            }

            cellRoot = new GameObject($"Fortification Cell {cell.x},{cell.y}");
            cellRoot.transform.SetParent(root, false);
            cellRoots[cell] = cellRoot;
            return cellRoot;
        }

        private GameObject GetOrCreateHoverMarker()
        {
            var existing = root.Find("Fortification Hover Marker");
            if (existing != null)
            {
                return existing.gameObject;
            }

            return CreatePart(
                "Fortification Hover Marker",
                PrimitiveType.Cube,
                root,
                Vector3.zero,
                Vector3.one,
                hoverValidMaterial);
        }

        private static GameObject CreatePart(
            string name,
            PrimitiveType primitive,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Material material)
        {
            var part = GameObject.CreatePrimitive(VoxelVisuals.ResolvePrimitive(primitive, name));
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.position = position;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            var collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }

            VoxelVisuals.ApplyBlockStyle(part, primitive, material, false);
            return part;
        }

        private static Material CreateMaterial(string materialName, Color color)
        {
            return VoxelVisuals.CreateLitMaterial(materialName, color);
        }
    }

    internal sealed class TorchLightFlicker : MonoBehaviour
    {
        private Light torchLight;
        private Transform flame;
        private Vector3 baseLocalPosition;
        private Vector3 baseFlameScale;
        private float baseRange;
        private float seed;

        public static void Attach(Light light, Transform flameTransform, int seedValue)
        {
            if (light == null)
            {
                return;
            }

            var flicker = light.gameObject.AddComponent<TorchLightFlicker>();
            flicker.Initialize(light, flameTransform, seedValue);
        }

        private void Initialize(Light light, Transform flameTransform, int seedValue)
        {
            torchLight = light;
            flame = flameTransform;
            baseLocalPosition = transform.localPosition;
            baseFlameScale = flame != null ? flame.localScale : Vector3.one;
            baseRange = torchLight.range;
            seed = Mathf.Abs(seedValue % 10000) * 0.017f;
        }

        private void Update()
        {
            if (torchLight == null)
            {
                return;
            }

            var t = Time.time + seed;
            var pulse = DungeonLampProfile.CalculatePulse(t);
            torchLight.intensity = DungeonLampProfile.CalculateIntensity(pulse);
            torchLight.range = baseRange;
            torchLight.color = DungeonLampProfile.LightColor;
            transform.localPosition = baseLocalPosition;

            if (flame != null)
            {
                flame.localScale = new Vector3(baseFlameScale.x, baseFlameScale.y * pulse, baseFlameScale.z);
            }
        }
    }
}
