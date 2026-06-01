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
        private readonly Material outpostSelectionMaterial;
        private readonly Material hoverInvalidMaterial;
        private readonly Material zoneMaterial;
        private readonly Material plankMaterial;
        private readonly Material reinforcedFloorMaterial;
        private readonly Material reinforcedWallMaterial;
        private readonly Material beamMaterial;
        private readonly Material stoneMaterial;
        private readonly Material metalMaterial;
        private readonly Material lampMaterial;
        private readonly Material labelBackgroundMaterial;
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
            outpostSelectionMaterial = CreateMaterial("Outpost Cave Selection", new Color(0.32f, 0.92f, 0.58f, 0.46f));
            hoverInvalidMaterial = CreateMaterial("Mine Cave Hover Invalid", new Color(1f, 0.25f, 0.14f, 0.66f));
            zoneMaterial = CreateMaterial("Mine Build Zone", new Color(0.92f, 0.68f, 0.2f, 0.72f));
            plankMaterial = CreateMaterial("Mine Route Planks", new Color(0.46f, 0.27f, 0.11f));
            reinforcedFloorMaterial = CreateTexturedMaterial("Mine Reinforced Floor Texture", "Textures/Fortifications/fortified_wood_floor", new Color(0.48f, 0.3f, 0.15f));
            reinforcedWallMaterial = CreateTexturedMaterial("Mine Reinforced Wall Texture", "Textures/Fortifications/fortified_wood_wall", new Color(0.42f, 0.24f, 0.12f));
            beamMaterial = CreateMaterial("Mine Route Beams", new Color(0.22f, 0.12f, 0.05f));
            stoneMaterial = CreateMaterial("Mine Stone", new Color(0.28f, 0.29f, 0.31f));
            metalMaterial = CreateMaterial("Mine Metal", new Color(0.62f, 0.64f, 0.66f));
            lampMaterial = DungeonLampProfile.CreateEmissiveMaterial("Mine Lamp");
            labelBackgroundMaterial = CreateTransparentMaterial("Mine Label Background", new Color(0.05f, 0.06f, 0.05f, 0.78f));
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

        public void RenderCaveSelection(IEnumerable<CaveInfo> caves, bool outpostMode = false)
        {
            ClearSelection();
            if (caves == null)
            {
                return;
            }

            foreach (var cave in caves)
            {
                var marker = CreateCaveMarker(outpostMode ? "Selectable Outpost Cave" : "Selectable Mine Cave", cave.Center, outpostMode ? outpostSelectionMaterial : selectionMaterial, 0.09f);
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

        public GameObject RenderOutpostZone(CaveInfo cave)
        {
            var caveRootObject = EnsureCellRoot(cave.Center);
            var caveRoot = caveRootObject.transform;
            ClearChildren(caveRoot);
            var center = mazeRenderer.GridToWorld(cave.Center);
            var unit = mazeRenderer.CellSize;
            CreatePart(
                "Outpost Construction Zone",
                PrimitiveType.Cube,
                caveRoot,
                center + new Vector3(0f, unit * 0.08f, 0f),
                new Vector3(unit * 2.45f, unit * 0.05f, unit * 2.45f),
                zoneMaterial);
            CreatePart(
                "Outpost Zone Post",
                PrimitiveType.Cube,
                caveRoot,
                center + new Vector3(unit * -0.78f, unit * 0.48f, unit * -0.78f),
                new Vector3(unit * 0.12f, unit * 0.86f, unit * 0.12f),
                beamMaterial);
            CreatePart(
                "Outpost Zone Board",
                PrimitiveType.Cube,
                caveRoot,
                center + new Vector3(unit * -0.72f, unit * 0.82f, unit * -0.78f),
                new Vector3(unit * 0.78f, unit * 0.32f, unit * 0.08f),
                plankMaterial);
            CreatePart(
                "Outpost Zone Lamp Preview",
                PrimitiveType.Sphere,
                caveRoot,
                center + new Vector3(unit * -0.46f, unit * 0.88f, unit * -0.7f),
                Vector3.one * unit * 0.1f,
                lampMaterial);
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
            CreatePointLight("Mine Shaft Light", caveRoot, center + new Vector3(0f, unit * 1.02f, unit * -0.42f));
            RenderMineUpgradeDetails(caveRoot, center, unit, oreType, level);
            CreateMineWorldLabel(caveRoot, center, unit, oreType);
            AddCaveCollider(caveRootObject, cave.Center);
            return caveRootObject;
        }

        public void RenderMineBuildProgress(CaveInfo cave, OreDepositType oreType, float progress)
        {
            var caveRootObject = EnsureCellRoot(cave.Center);
            var caveRoot = caveRootObject.transform;
            ClearChildren(caveRoot);
            var center = mazeRenderer.GridToWorld(cave.Center);
            var unit = mazeRenderer.CellSize;
            var accent = GetOreAccentMaterial(oreType);
            var normalized = Mathf.Clamp01(progress);

            CreatePart("Mine Construction Foundation", PrimitiveType.Cube, caveRoot, center + new Vector3(0f, unit * 0.08f, 0f), new Vector3(unit * 1.65f, unit * 0.08f, unit * 1.3f), zoneMaterial);
            if (normalized >= 0.12f)
            {
                CreatePart("Mine Crate Stack", PrimitiveType.Cube, caveRoot, center + new Vector3(unit * -0.52f, unit * 0.24f, unit * 0.38f), new Vector3(unit * 0.34f, unit * 0.28f, unit * 0.34f), plankMaterial);
            }

            if (normalized >= 0.28f)
            {
                CreatePart("Mine Left Partial Support", PrimitiveType.Cube, caveRoot, center + new Vector3(unit * -0.48f, unit * 0.52f, unit * -0.22f), new Vector3(unit * 0.13f, unit * 0.82f, unit * 0.15f), beamMaterial);
            }

            if (normalized >= 0.44f)
            {
                CreatePart("Mine Right Partial Support", PrimitiveType.Cube, caveRoot, center + new Vector3(unit * 0.48f, unit * 0.52f, unit * -0.22f), new Vector3(unit * 0.13f, unit * 0.82f, unit * 0.15f), beamMaterial);
            }

            if (normalized >= 0.6f)
            {
                CreatePart("Mine Partial Top Beam", PrimitiveType.Cube, caveRoot, center + new Vector3(0f, unit * 1.02f, unit * -0.22f), new Vector3(unit * 1.05f, unit * 0.14f, unit * 0.16f), beamMaterial);
            }

            if (normalized >= 0.76f)
            {
                CreatePart("Mine Track Preview A", PrimitiveType.Cube, caveRoot, center + new Vector3(unit * -0.2f, unit * 0.15f, unit * 0.56f), new Vector3(unit * 0.07f, unit * 0.06f, unit * 0.6f), metalMaterial);
                CreatePart("Mine Track Preview B", PrimitiveType.Cube, caveRoot, center + new Vector3(unit * 0.2f, unit * 0.15f, unit * 0.56f), new Vector3(unit * 0.07f, unit * 0.06f, unit * 0.6f), metalMaterial);
            }

            if (normalized >= 0.9f)
            {
                CreatePart(oreType == OreDepositType.Iron ? "Iron Mine Build Ore" : "Gold Mine Build Ore", PrimitiveType.Sphere, caveRoot, center + new Vector3(unit * 0.46f, unit * 0.34f, unit * 0.38f), new Vector3(unit * 0.28f, unit * 0.16f, unit * 0.24f), accent);
                CreatePart("Mine Build Lamp", PrimitiveType.Sphere, caveRoot, center + new Vector3(0f, unit * 0.86f, unit * -0.42f), Vector3.one * unit * 0.11f, lampMaterial);
                CreatePointLight("Mine Build Light", caveRoot, center + new Vector3(0f, unit * 0.86f, unit * -0.42f));
            }

            AddCaveCollider(caveRootObject, cave.Center);
        }

        public void RenderOutpostBuildProgress(CaveInfo cave, float progress)
        {
            var caveRootObject = EnsureCellRoot(cave.Center);
            var caveRoot = caveRootObject.transform;
            ClearChildren(caveRoot);
            var center = mazeRenderer.GridToWorld(cave.Center);
            var unit = mazeRenderer.CellSize;
            var normalized = Mathf.Clamp01(progress);

            CreatePart("Outpost Foundation", PrimitiveType.Cube, caveRoot, center + new Vector3(0f, unit * 0.08f, 0f), new Vector3(unit * 1.72f, unit * 0.08f, unit * 1.42f), zoneMaterial);
            if (normalized >= 0.16f)
            {
                CreatePart("Outpost Supply Crates", PrimitiveType.Cube, caveRoot, center + new Vector3(unit * -0.48f, unit * 0.22f, unit * 0.42f), new Vector3(unit * 0.36f, unit * 0.26f, unit * 0.34f), plankMaterial);
            }

            if (normalized >= 0.32f)
            {
                CreatePart("Outpost Left Post", PrimitiveType.Cube, caveRoot, center + new Vector3(unit * -0.56f, unit * 0.54f, unit * -0.24f), new Vector3(unit * 0.12f, unit * 0.92f, unit * 0.12f), beamMaterial);
            }

            if (normalized >= 0.48f)
            {
                CreatePart("Outpost Right Post", PrimitiveType.Cube, caveRoot, center + new Vector3(unit * 0.56f, unit * 0.54f, unit * -0.24f), new Vector3(unit * 0.12f, unit * 0.92f, unit * 0.12f), beamMaterial);
            }

            if (normalized >= 0.64f)
            {
                CreatePart("Outpost Back Wall", PrimitiveType.Cube, caveRoot, center + new Vector3(0f, unit * 0.54f, unit * -0.42f), new Vector3(unit * 1.28f, unit * 0.72f, unit * 0.12f), stoneMaterial);
            }

            if (normalized >= 0.78f)
            {
                CreatePart("Outpost Roof Beam", PrimitiveType.Cube, caveRoot, center + new Vector3(0f, unit * 1.04f, unit * -0.24f), new Vector3(unit * 1.45f, unit * 0.14f, unit * 0.2f), beamMaterial);
            }

            if (normalized >= 0.92f)
            {
                CreatePart("Outpost Build Lamp", PrimitiveType.Sphere, caveRoot, center + new Vector3(unit * 0.42f, unit * 0.96f, unit * -0.38f), Vector3.one * unit * 0.11f, lampMaterial);
                CreatePointLight("Outpost Build Light", caveRoot, center + new Vector3(unit * 0.42f, unit * 0.98f, unit * -0.38f));
            }

            AddCaveCollider(caveRootObject, cave.Center);
        }

        public GameObject RenderOutpost(CaveInfo cave, int level)
        {
            var caveRootObject = EnsureCellRoot(cave.Center);
            var caveRoot = caveRootObject.transform;
            ClearChildren(caveRoot);
            var center = mazeRenderer.GridToWorld(cave.Center);
            var unit = mazeRenderer.CellSize;

            CreatePart("Outpost Stone Base", PrimitiveType.Cube, caveRoot, center + new Vector3(0f, unit * 0.12f, 0f), new Vector3(unit * 1.5f, unit * 0.12f, unit * 1.25f), stoneMaterial);
            CreatePart("Outpost Rear Wall", PrimitiveType.Cube, caveRoot, center + new Vector3(0f, unit * 0.62f, unit * -0.42f), new Vector3(unit * 1.36f, unit * 0.9f, unit * 0.16f), stoneMaterial);
            CreatePart("Outpost Left Brace", PrimitiveType.Cube, caveRoot, center + new Vector3(unit * -0.62f, unit * 0.7f, unit * -0.18f), new Vector3(unit * 0.13f, unit * 1.05f, unit * 0.14f), beamMaterial);
            CreatePart("Outpost Right Brace", PrimitiveType.Cube, caveRoot, center + new Vector3(unit * 0.62f, unit * 0.7f, unit * -0.18f), new Vector3(unit * 0.13f, unit * 1.05f, unit * 0.14f), beamMaterial);
            CreatePart("Outpost Roof", PrimitiveType.Cube, caveRoot, center + new Vector3(0f, unit * 1.22f, unit * -0.18f), new Vector3(unit * 1.62f, unit * 0.16f, unit * 0.72f), beamMaterial);
            CreatePart("Outpost Supply Chest", PrimitiveType.Cube, caveRoot, center + new Vector3(unit * -0.34f, unit * 0.32f, unit * 0.34f), new Vector3(unit * 0.44f, unit * 0.28f, unit * 0.34f), plankMaterial);
            CreatePart("Outpost Banner Pole", PrimitiveType.Cube, caveRoot, center + new Vector3(unit * 0.52f, unit * 1.04f, unit * 0.34f), new Vector3(unit * 0.07f, unit * 0.96f, unit * 0.07f), metalMaterial);
            CreatePart("Outpost Banner", PrimitiveType.Cube, caveRoot, center + new Vector3(unit * 0.33f, unit * 1.28f, unit * 0.34f), new Vector3(unit * 0.38f, unit * 0.22f, unit * 0.035f), zoneMaterial);
            CreatePart("Outpost Lamp", PrimitiveType.Sphere, caveRoot, center + new Vector3(unit * 0.36f, unit * 0.98f, unit * -0.38f), Vector3.one * unit * 0.13f, lampMaterial);
            CreatePointLight("Outpost Light", caveRoot, center + new Vector3(unit * 0.36f, unit * 1.0f, unit * -0.38f));
            CreateWorldLabel(caveRoot, center, unit, "Аванпост", new Color(0.84f, 1f, 0.76f, 1f));
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
            CreateLocalPart("Mine Cart Lamp", PrimitiveType.Sphere, cart, new Vector3(0f, unit * 0.34f, -unit * 0.28f), Vector3.one * unit * 0.095f, lampMaterial);
            CreateLocalPointLight("Mine Cart Light", cart, new Vector3(0f, unit * 0.36f, -unit * 0.3f));
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
            var light = DungeonLampProfile.ConfigurePointLight(lightObject.AddComponent<Light>(), mazeRenderer.CellSize);
            TorchLightFlicker.Attach(light, flame.transform, BuildTorchFlickerSeed(cell));
        }

        private static int BuildTorchFlickerSeed(Vector2Int cell)
        {
            return cell.x * 73856093 ^ cell.y * 19349663 ^ 0x27d4eb2d;
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
            CreateLocalPart("Worker Helmet Lamp", PrimitiveType.Sphere, worker, new Vector3(0f, unit * 0.8f, unit * 0.16f), Vector3.one * unit * 0.055f, lampMaterial);
            CreateLocalPointLight("Worker Headlamp", worker, new Vector3(0f, unit * 0.82f, unit * 0.18f));
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

        private void CreateMineWorldLabel(Transform caveRoot, Vector3 center, float unit, OreDepositType oreType)
        {
            CreateWorldLabel(
                caveRoot,
                center,
                unit,
                GetMineDisplayName(oreType),
                oreType == OreDepositType.Iron ? new Color(0.88f, 0.93f, 0.98f, 1f) : new Color(1f, 0.92f, 0.55f, 1f));
        }

        private void CreateWorldLabel(Transform caveRoot, Vector3 center, float unit, string labelText, Color textColor)
        {
            var labelRoot = new GameObject("Dungeon Building Label").transform;
            labelRoot.SetParent(caveRoot, false);
            labelRoot.position = center + Vector3.up * unit * 2.35f;
            labelRoot.gameObject.AddComponent<MineWorldLabel>();

            var background = GameObject.CreatePrimitive(PrimitiveType.Quad);
            background.name = "Dungeon Building Label Background";
            background.transform.SetParent(labelRoot, false);
            background.transform.localPosition = Vector3.zero;
            background.transform.localScale = new Vector3(Mathf.Clamp(1.55f + labelText.Length * 0.18f, 2.25f, 5.2f), 0.56f, 1f);
            background.GetComponent<Renderer>().sharedMaterial = labelBackgroundMaterial;
            var collider = background.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }

            CreateWorldLabelText(
                labelRoot,
                "Dungeon Building Label Shadow",
                labelText,
                new Vector3(0.045f, -0.045f, -0.034f),
                new Color(0f, 0f, 0f, 0.92f));
            CreateWorldLabelText(
                labelRoot,
                "Dungeon Building Label Text",
                labelText,
                new Vector3(0f, 0f, -0.045f),
                textColor);
        }

        private static string GetMineDisplayName(OreDepositType oreType)
        {
            return oreType == OreDepositType.Iron ? "Железная шахта" : "Золотая шахта";
        }

        private static void CreateWorldLabelText(Transform parent, string objectName, string text, Vector3 localPosition, Color color)
        {
            var textObject = new GameObject(objectName);
            textObject.transform.SetParent(parent, false);
            textObject.transform.localPosition = localPosition;
            var textMesh = textObject.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.fontSize = 72;
            textMesh.characterSize = 0.085f;
            textMesh.fontStyle = FontStyle.Bold;
            textMesh.color = color;

            var meshRenderer = textObject.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.sortingOrder = 44;
            }
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

        private Light CreatePointLight(string name, Transform parent, Vector3 position)
        {
            var lightObject = new GameObject(name);
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.position = position;
            return DungeonLampProfile.ConfigurePointLight(lightObject.AddComponent<Light>(), mazeRenderer.CellSize);
        }

        private Light CreateLocalPointLight(string name, Transform parent, Vector3 localPosition)
        {
            var lightObject = new GameObject(name);
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.localPosition = localPosition;
            return DungeonLampProfile.ConfigurePointLight(lightObject.AddComponent<Light>(), mazeRenderer.CellSize);
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

        private static Material CreateTransparentMaterial(string materialName, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            var material = new Material(shader) { name = materialName, color = color };
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.renderQueue = (int)RenderQueue.Transparent;
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            return material;
        }
    }

    internal sealed class MineWorldLabel : MonoBehaviour
    {
        private void LateUpdate()
        {
            var camera = Camera.main;
            if (camera != null)
            {
                transform.rotation = camera.transform.rotation;
            }
        }
    }
}
