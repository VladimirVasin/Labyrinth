using System.Collections.Generic;
using Labyrinth.Maze;
using UnityEngine;
using UnityEngine.Rendering;

namespace Labyrinth.Core
{
    public sealed class MineConstructionRenderer
    {
        private readonly Dictionary<Vector2Int, GameObject> cellRoots = new Dictionary<Vector2Int, GameObject>();
        private readonly List<GameObject> selectionMarkers = new List<GameObject>();
        private readonly Transform root;
        private readonly MazeRenderer mazeRenderer;
        private readonly Material selectionMaterial;
        private readonly Material hoverValidMaterial;
        private readonly Material hoverInvalidMaterial;
        private readonly Material zoneMaterial;
        private readonly Material plankMaterial;
        private readonly Material reinforcedFloorMaterial;
        private readonly Material reinforcedWallMaterial;
        private readonly Material beamMaterial;
        private readonly Material stoneMaterial;
        private readonly Material metalMaterial;
        private readonly Material lampMaterial;
        private readonly Material workerBodyMaterial;
        private readonly Material workerHeadMaterial;

        private GameObject hoverMarker;

        private MineConstructionRenderer(Transform parent, MazeRenderer renderer)
        {
            mazeRenderer = renderer;
            root = new GameObject("MineConstructionRoot").transform;
            root.SetParent(parent, false);
            selectionMaterial = CreateMaterial("Mine Cave Selection", new Color(0.16f, 0.78f, 1f, 0.42f));
            hoverValidMaterial = CreateMaterial("Mine Cave Hover Valid", new Color(0.26f, 1f, 0.42f, 0.66f));
            hoverInvalidMaterial = CreateMaterial("Mine Cave Hover Invalid", new Color(1f, 0.25f, 0.14f, 0.66f));
            zoneMaterial = CreateMaterial("Mine Build Zone", new Color(0.92f, 0.68f, 0.2f, 0.72f));
            plankMaterial = CreateMaterial("Mine Route Planks", new Color(0.46f, 0.27f, 0.11f));
            reinforcedFloorMaterial = CreateTexturedMaterial("Mine Reinforced Floor Texture", "Textures/Fortifications/fortified_wood_floor", new Color(0.48f, 0.3f, 0.15f));
            reinforcedWallMaterial = CreateTexturedMaterial("Mine Reinforced Wall Texture", "Textures/Fortifications/fortified_wood_wall", new Color(0.42f, 0.24f, 0.12f));
            beamMaterial = CreateMaterial("Mine Route Beams", new Color(0.22f, 0.12f, 0.05f));
            stoneMaterial = CreateMaterial("Mine Stone", new Color(0.28f, 0.29f, 0.31f));
            metalMaterial = CreateMaterial("Mine Metal", new Color(0.62f, 0.64f, 0.66f));
            lampMaterial = VoxelVisuals.CreateEmissiveMaterial("Mine Lamp", new Color(1f, 0.62f, 0.14f), 2f);
            workerBodyMaterial = CreateMaterial("Mine Worker Body", new Color(0.34f, 0.25f, 0.17f));
            workerHeadMaterial = CreateMaterial("Mine Worker Head", new Color(0.76f, 0.62f, 0.42f));
        }

        public static MineConstructionRenderer Create(Transform parent, MazeRenderer renderer)
        {
            return new MineConstructionRenderer(parent, renderer);
        }

        public void Clear()
        {
            selectionMarkers.Clear();
            cellRoots.Clear();
            if (root != null)
            {
                Object.Destroy(root.gameObject);
            }
        }

        public GameObject GetCellRoot(Vector2Int cell)
        {
            return EnsureCellRoot(cell);
        }

        public void RenderCaveSelection(IEnumerable<CaveInfo> caves)
        {
            ClearSelection();
            if (caves == null)
            {
                return;
            }

            foreach (var cave in caves)
            {
                var marker = CreateCaveMarker("Selectable Mine Cave", cave.Center, selectionMaterial, 0.09f);
                selectionMarkers.Add(marker);
            }
        }

        public void ClearSelection()
        {
            for (var i = 0; i < selectionMarkers.Count; i++)
            {
                if (selectionMarkers[i] != null)
                {
                    Object.Destroy(selectionMarkers[i]);
                }
            }

            selectionMarkers.Clear();
            HideHoverMarker();
        }

        public void ShowHoverMarker(CaveInfo cave, bool valid)
        {
            if (hoverMarker == null)
            {
                hoverMarker = CreateCaveMarker("Mine Cave Hover", cave.Center, valid ? hoverValidMaterial : hoverInvalidMaterial, 0.12f);
            }

            hoverMarker.transform.position = mazeRenderer.GridToWorld(cave.Center) + new Vector3(0f, mazeRenderer.CellSize * 0.12f, 0f);
            hoverMarker.GetComponent<Renderer>().sharedMaterial = valid ? hoverValidMaterial : hoverInvalidMaterial;
            hoverMarker.SetActive(true);
        }

        public void HideHoverMarker()
        {
            if (hoverMarker != null)
            {
                hoverMarker.SetActive(false);
            }
        }

        public GameObject RenderMineZone(CaveInfo cave, OreDepositType oreType)
        {
            var caveRootObject = EnsureCellRoot(cave.Center);
            var caveRoot = caveRootObject.transform;
            ClearChildren(caveRoot);
            var center = mazeRenderer.GridToWorld(cave.Center);
            var unit = mazeRenderer.CellSize;
            var accent = GetOreAccentMaterial(oreType);
            CreatePart(
                "Mine Construction Zone",
                PrimitiveType.Cube,
                caveRoot,
                center + new Vector3(0f, unit * 0.08f, 0f),
                new Vector3(unit * 2.45f, unit * 0.05f, unit * 2.45f),
                zoneMaterial);
            CreatePart(
                "Mine Zone Sign",
                PrimitiveType.Cube,
                caveRoot,
                center + new Vector3(unit * -0.82f, unit * 0.42f, unit * -0.82f),
                new Vector3(unit * 0.12f, unit * 0.72f, unit * 0.12f),
                beamMaterial);
            CreatePart(
                "Mine Zone Board",
                PrimitiveType.Cube,
                caveRoot,
                center + new Vector3(unit * -0.82f, unit * 0.76f, unit * -0.82f),
                new Vector3(unit * 0.72f, unit * 0.34f, unit * 0.08f),
                plankMaterial);
            CreatePart(
                oreType == OreDepositType.Iron ? "Iron Mine Marker" : "Gold Mine Marker",
                PrimitiveType.Sphere,
                caveRoot,
                center + new Vector3(unit * -0.82f, unit * 0.78f, unit * -0.72f),
                Vector3.one * unit * 0.14f,
                accent);
            AddCaveCollider(caveRootObject, cave.Center);
            return caveRootObject;
        }

        public void RenderFortifiedCell(Vector2Int cell)
        {
            var cellRoot = EnsureCellRoot(cell).transform;
            var center = mazeRenderer.GridToWorld(cell);
            var unit = mazeRenderer.CellSize;
            CreatePart(
                "Mine Reinforced Floor Texture",
                PrimitiveType.Cube,
                cellRoot,
                center + new Vector3(0f, unit * 0.035f, 0f),
                new Vector3(unit * 0.92f, unit * 0.025f, unit * 0.92f),
                reinforcedFloorMaterial);
            CreatePart(
                "Mine Floor Board A",
                PrimitiveType.Cube,
                cellRoot,
                center + new Vector3(unit * -0.18f, unit * 0.04f, 0f),
                new Vector3(unit * 0.18f, unit * 0.035f, unit * 0.78f),
                plankMaterial);
            CreatePart(
                "Mine Floor Board B",
                PrimitiveType.Cube,
                cellRoot,
                center + new Vector3(unit * 0.18f, unit * 0.04f, 0f),
                new Vector3(unit * 0.18f, unit * 0.035f, unit * 0.78f),
                plankMaterial);
            CreatePart(
                "Mine Route Brace",
                PrimitiveType.Cube,
                cellRoot,
                center + new Vector3(0f, unit * 0.075f, 0f),
                new Vector3(unit * 0.72f, unit * 0.035f, unit * 0.1f),
                beamMaterial);
        }

        public void RenderWallReinforcement(Vector2Int wallCell, Vector2Int wallDirection)
        {
            var cellRoot = EnsureCellRoot(wallCell).transform;
            var wallCenter = mazeRenderer.GridToWorld(wallCell);
            var unit = mazeRenderer.CellSize;
            var normal = new Vector3(wallDirection.x, 0f, wallDirection.y);
            if (normal.sqrMagnitude < 0.001f)
            {
                normal = Vector3.forward;
            }

            var faceCenter = wallCenter - normal.normalized * unit * 0.515f;
            var horizontalFace = wallDirection.x != 0;
            var panelScale = horizontalFace
                ? new Vector3(unit * 0.055f, mazeRenderer.WallHeight * 0.78f, unit * 0.9f)
                : new Vector3(unit * 0.9f, mazeRenderer.WallHeight * 0.78f, unit * 0.055f);
            CreatePart(
                "Mine Reinforced Wall Texture",
                PrimitiveType.Cube,
                cellRoot,
                faceCenter + new Vector3(0f, mazeRenderer.WallHeight * 0.48f, 0f),
                panelScale,
                reinforcedWallMaterial);

            var braceScale = horizontalFace
                ? new Vector3(unit * 0.07f, unit * 0.08f, unit * 0.96f)
                : new Vector3(unit * 0.96f, unit * 0.08f, unit * 0.07f);
            CreatePart(
                "Mine Wall Top Brace",
                PrimitiveType.Cube,
                cellRoot,
                faceCenter + new Vector3(0f, mazeRenderer.WallHeight * 0.84f, 0f),
                braceScale,
                beamMaterial);
            CreatePart(
                "Mine Wall Bottom Brace",
                PrimitiveType.Cube,
                cellRoot,
                faceCenter + new Vector3(0f, mazeRenderer.WallHeight * 0.2f, 0f),
                braceScale,
                beamMaterial);
        }

        public GameObject RenderMine(CaveInfo cave, OreDepositType oreType, int level)
        {
            var caveRootObject = EnsureCellRoot(cave.Center);
            var caveRoot = caveRootObject.transform;
            ClearChildren(caveRoot);
            var center = mazeRenderer.GridToWorld(cave.Center);
            var unit = mazeRenderer.CellSize;
            var accent = GetOreAccentMaterial(oreType);
            CreatePart("Mine Entrance Shadow", PrimitiveType.Cube, caveRoot, center + new Vector3(0f, unit * 0.12f, 0f), new Vector3(unit * 1.35f, unit * 0.1f, unit * 1.18f), stoneMaterial);
            CreatePart("Mine Left Support", PrimitiveType.Cube, caveRoot, center + new Vector3(unit * -0.48f, unit * 0.68f, unit * -0.22f), new Vector3(unit * 0.14f, unit * 1.18f, unit * 0.16f), beamMaterial);
            CreatePart("Mine Right Support", PrimitiveType.Cube, caveRoot, center + new Vector3(unit * 0.48f, unit * 0.68f, unit * -0.22f), new Vector3(unit * 0.14f, unit * 1.18f, unit * 0.16f), beamMaterial);
            CreatePart("Mine Top Beam", PrimitiveType.Cube, caveRoot, center + new Vector3(0f, unit * 1.28f, unit * -0.22f), new Vector3(unit * 1.2f, unit * 0.16f, unit * 0.18f), beamMaterial);
            CreatePart("Mine Cart", PrimitiveType.Cube, caveRoot, center + new Vector3(unit * 0.06f, unit * 0.26f, unit * 0.48f), new Vector3(unit * 0.62f, unit * 0.26f, unit * 0.42f), metalMaterial);
            CreatePart("Mine Ore Cargo", PrimitiveType.Sphere, caveRoot, center + new Vector3(unit * 0.06f, unit * 0.48f, unit * 0.48f), new Vector3(unit * 0.3f, unit * 0.16f, unit * 0.26f), accent);
            CreatePart("Mine Lamp", PrimitiveType.Sphere, caveRoot, center + new Vector3(0f, unit * 1.05f, unit * -0.42f), Vector3.one * unit * 0.13f, lampMaterial);
            RenderMineUpgradeDetails(caveRoot, center, unit, oreType, level);
            AddCaveCollider(caveRootObject, cave.Center);
            return caveRootObject;
        }

        public Transform CreateMineCart(Vector3 position, OreDepositType oreType)
        {
            var cart = new GameObject(oreType == OreDepositType.Iron ? "Iron Mine Cart" : "Gold Mine Cart").transform;
            cart.SetParent(root, false);
            cart.position = position;
            var unit = mazeRenderer.CellSize;
            var cargo = GetOreAccentMaterial(oreType);

            CreateLocalPart("Mine Cart Bed", PrimitiveType.Cube, cart, new Vector3(0f, unit * 0.12f, 0f), new Vector3(unit * 0.58f, unit * 0.18f, unit * 0.42f), beamMaterial);
            CreateLocalPart("Mine Cart Cargo", PrimitiveType.Sphere, cart, new Vector3(0f, unit * 0.28f, 0f), new Vector3(unit * 0.42f, unit * 0.2f, unit * 0.32f), cargo);
            CreateLocalPart("Mine Cart Handle", PrimitiveType.Cube, cart, new Vector3(0f, unit * 0.16f, -unit * 0.34f), new Vector3(unit * 0.08f, unit * 0.08f, unit * 0.36f), metalMaterial);
            CreateLocalPart("Mine Cart Wheel L", PrimitiveType.Sphere, cart, new Vector3(-unit * 0.24f, unit * 0.04f, -unit * 0.16f), Vector3.one * unit * 0.13f, metalMaterial);
            CreateLocalPart("Mine Cart Wheel R", PrimitiveType.Sphere, cart, new Vector3(unit * 0.24f, unit * 0.04f, -unit * 0.16f), Vector3.one * unit * 0.13f, metalMaterial);
            return cart;
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
                "Mine Wall Torch Holder",
                PrimitiveType.Cube,
                cellRoot,
                holderPosition,
                new Vector3(unit * 0.08f, unit * 0.34f, unit * 0.08f),
                beamMaterial);
            holder.transform.rotation = Quaternion.LookRotation(normal == Vector3.zero ? Vector3.forward : normal, Vector3.up);

            var flame = CreatePart(
                "Mine Wall Torch Flame",
                PrimitiveType.Sphere,
                cellRoot,
                flamePosition,
                Vector3.one * unit * 0.16f,
                lampMaterial);
            flame.transform.localScale = new Vector3(unit * 0.14f, unit * 0.22f, unit * 0.14f);

            var lightObject = new GameObject("Mine Wall Torch Light");
            lightObject.transform.SetParent(cellRoot, false);
            lightObject.transform.position = flamePosition;
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.55f, 0.18f);
            light.range = mazeRenderer.CellSize * (lightRange + 1.15f);
            light.intensity = 2.3f;
            light.shadows = LightShadows.None;
            light.bounceIntensity = 0.18f;
        }

        public Transform CreateWorker(Vector3 position, bool carryingWood)
        {
            var worker = new GameObject("Mine Construction Worker").transform;
            worker.SetParent(root, false);
            worker.position = position;
            var unit = mazeRenderer.ModelUnitSize * 1.2f;
            VoxelVisuals.CreateContactShadow(
                "Mine Worker Contact Shadow",
                worker,
                new Vector3(0f, 0.006f, 0f),
                new Vector3(unit * 0.34f, 0.004f, unit * 0.27f),
                0.32f);
            CreateLocalPart("Worker Body", PrimitiveType.Capsule, worker, new Vector3(0f, unit * 0.32f, 0f), new Vector3(unit * 0.2f, unit * 0.34f, unit * 0.2f), workerBodyMaterial);
            CreateLocalPart("Worker Left Foot", PrimitiveType.Cube, worker, new Vector3(unit * -0.09f, unit * 0.09f, unit * 0.05f), new Vector3(unit * 0.1f, unit * 0.08f, unit * 0.17f), workerBodyMaterial);
            CreateLocalPart("Worker Right Foot", PrimitiveType.Cube, worker, new Vector3(unit * 0.09f, unit * 0.09f, unit * 0.05f), new Vector3(unit * 0.1f, unit * 0.08f, unit * 0.17f), workerBodyMaterial);
            CreateLocalPart("Worker Head", PrimitiveType.Sphere, worker, new Vector3(0f, unit * 0.76f, 0f), Vector3.one * unit * 0.17f, workerHeadMaterial);
            var timber = CreateLocalPart("Worker Timber", PrimitiveType.Cube, worker, new Vector3(0f, unit * 0.46f, -unit * 0.18f), new Vector3(unit * 0.42f, unit * 0.13f, unit * 0.12f), plankMaterial);
            timber.SetActive(carryingWood);
            CreateLocalPart("Worker Pick", PrimitiveType.Cube, worker, new Vector3(unit * 0.2f, unit * 0.5f, unit * 0.08f), new Vector3(unit * 0.05f, unit * 0.52f, unit * 0.05f), beamMaterial).transform.localRotation = Quaternion.Euler(0f, 0f, 32f);
            AmbientWalkerMoveAnimator.Attach(worker, unit, BuildWorkerAnimationSeed(worker));
            return worker;
        }

        public void SetWorkerCarryingWood(Transform worker, bool carryingWood)
        {
            if (worker == null)
            {
                return;
            }

            var timber = worker.Find("Worker Timber");
            if (timber != null)
            {
                ResetWorkerTimberTransform(timber);
                timber.gameObject.SetActive(carryingWood);
            }
        }

        public void AnimateWorkerBuild(Transform worker, float progress)
        {
            if (worker == null)
            {
                return;
            }

            var unit = mazeRenderer.ModelUnitSize * 1.2f;
            var swing = Mathf.Sin(progress * Mathf.PI * 8f);
            var bob = Mathf.Abs(swing) * unit * 0.035f;
            var body = worker.Find("Worker Body");
            var head = worker.Find("Worker Head");
            var pick = worker.Find("Worker Pick");
            if (body != null)
            {
                body.localPosition = new Vector3(0f, unit * 0.32f - bob, 0f);
            }

            if (head != null)
            {
                head.localPosition = new Vector3(0f, unit * 0.76f - bob, 0f);
            }

            if (pick != null)
            {
                pick.localRotation = Quaternion.Euler(0f, 0f, 32f + swing * 48f);
            }
        }

        public void DestroyWorker(Transform worker)
        {
            if (worker != null)
            {
                Object.Destroy(worker.gameObject);
            }
        }

        private GameObject CreateCaveMarker(string name, Vector2Int center, Material material, float height)
        {
            var unit = mazeRenderer.CellSize;
            return CreatePart(
                name,
                PrimitiveType.Cube,
                root,
                mazeRenderer.GridToWorld(center) + new Vector3(0f, unit * height, 0f),
                new Vector3(unit * 2.82f, unit * 0.04f, unit * 2.82f),
                material);
        }

        private GameObject EnsureCellRoot(Vector2Int cell)
        {
            if (cellRoots.TryGetValue(cell, out var cellRoot) && cellRoot != null)
            {
                return cellRoot;
            }

            cellRoot = new GameObject($"Mine Cell {cell.x},{cell.y}");
            cellRoot.transform.SetParent(root, false);
            cellRoots[cell] = cellRoot;
            return cellRoot;
        }

        private static void ClearChildren(Transform parent)
        {
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(parent.GetChild(i).gameObject);
            }
        }

        private Material GetOreAccentMaterial(OreDepositType oreType)
        {
            return oreType == OreDepositType.Iron
                ? metalMaterial
                : CreateMaterial("Mine Gold Accent", new Color(1f, 0.72f, 0.14f));
        }

        private static int BuildWorkerAnimationSeed(Transform worker)
        {
            var position = worker != null ? worker.position : Vector3.zero;
            return Mathf.RoundToInt(position.x * 79f)
                ^ Mathf.RoundToInt(position.y * 43f)
                ^ Mathf.RoundToInt(position.z * 167f)
                ^ 0x4c91;
        }

        private void RenderMineUpgradeDetails(Transform caveRoot, Vector3 center, float unit, OreDepositType oreType, int level)
        {
            if (level < 2)
            {
                return;
            }

            var accent = GetOreAccentMaterial(oreType);
            CreatePart("Mine Reinforced Rail A", PrimitiveType.Cube, caveRoot, center + new Vector3(unit * -0.24f, unit * 0.16f, unit * 0.76f), new Vector3(unit * 0.08f, unit * 0.08f, unit * 0.78f), metalMaterial);
            CreatePart("Mine Reinforced Rail B", PrimitiveType.Cube, caveRoot, center + new Vector3(unit * 0.24f, unit * 0.16f, unit * 0.76f), new Vector3(unit * 0.08f, unit * 0.08f, unit * 0.78f), metalMaterial);
            CreatePart("Mine Resource Crate", PrimitiveType.Cube, caveRoot, center + new Vector3(unit * -0.48f, unit * 0.3f, unit * 0.5f), new Vector3(unit * 0.34f, unit * 0.28f, unit * 0.34f), accent);

            if (level < 3)
            {
                return;
            }

            CreatePart("Mine Deep Shaft Frame", PrimitiveType.Cube, caveRoot, center + new Vector3(0f, unit * 1.5f, unit * -0.24f), new Vector3(unit * 1.35f, unit * 0.12f, unit * 0.18f), metalMaterial);
            CreatePart("Mine Guard Post", PrimitiveType.Cube, caveRoot, center + new Vector3(unit * 0.62f, unit * 0.56f, unit * 0.52f), new Vector3(unit * 0.12f, unit * 0.92f, unit * 0.12f), beamMaterial);
            CreatePart(
                oreType == OreDepositType.Iron ? "Iron Mine Signal" : "Gold Mine Signal",
                PrimitiveType.Sphere,
                caveRoot,
                center + new Vector3(unit * 0.62f, unit * 1.08f, unit * 0.52f),
                Vector3.one * unit * 0.16f,
                accent);
        }

        private void AddCaveCollider(GameObject caveRoot, Vector2Int centerCell)
        {
            if (caveRoot == null)
            {
                return;
            }

            var unit = mazeRenderer.CellSize;
            var center = mazeRenderer.GridToWorld(centerCell);
            var collider = caveRoot.GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = caveRoot.AddComponent<BoxCollider>();
            }

            collider.center = center + new Vector3(0f, unit * 0.55f, 0f);
            collider.size = new Vector3(unit * 2.8f, unit * 1.4f, unit * 2.8f);
        }

        private static GameObject CreatePart(string name, PrimitiveType primitive, Transform parent, Vector3 position, Vector3 scale, Material material)
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

        private static GameObject CreateLocalPart(string name, PrimitiveType primitive, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
        {
            var part = GameObject.CreatePrimitive(VoxelVisuals.ResolvePrimitive(primitive, name));
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = Quaternion.identity;
            part.transform.localScale = localScale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            var collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }

            VoxelVisuals.ApplyBlockStyle(part, primitive, material, false);
            return part;
        }

        private void ResetWorkerTimberTransform(Transform timber)
        {
            var unit = mazeRenderer.ModelUnitSize * 1.2f;
            timber.localPosition = new Vector3(0f, unit * 0.46f, -unit * 0.18f);
            timber.localRotation = Quaternion.identity;
            timber.localScale = new Vector3(unit * 0.42f, unit * 0.13f, unit * 0.12f);
        }

        private static Material CreateMaterial(string materialName, Color color)
        {
            var material = VoxelVisuals.CreateLitMaterial(materialName, color);

            if (color.a < 0.99f)
            {
                material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.renderQueue = (int)RenderQueue.Transparent;
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }

            return material;
        }

        private static Material CreateTexturedMaterial(string materialName, string resourcePath, Color fallbackColor)
        {
            var material = CreateMaterial(materialName, fallbackColor);
            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
            {
                return material;
            }

            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Bilinear;
            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
            }

            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", texture);
            }

            return material;
        }
    }
}
