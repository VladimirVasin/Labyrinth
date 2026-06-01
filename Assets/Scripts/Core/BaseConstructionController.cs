using System;
using System.Collections.Generic;
using Labyrinth.Base;
using Labyrinth.Maze;
using UnityEngine;
using UnityEngine.Rendering;

namespace Labyrinth.Core
{
    public sealed class BaseConstructionController : MonoBehaviour
    {
        private const float WorkerYOffset = 0.07f;
        private const float WorkerSpeedCellsPerSecond = 1.75f;
        private const float BaseBuildSeconds = 7.5f;
        private const float BuildSecondsPerFootprint = 1.15f;

        private readonly List<ConstructionSiteRuntime> sites = new List<ConstructionSiteRuntime>();
        private readonly Vector2Int[] directions =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        private MazeRenderer mazeRenderer;
        private TerrainDecorationController terrainDecorations;
        private Func<MazeGenerationResult> mazeProvider;
        private Action<BuildingType, Vector2Int, int> completedHandler;
        private Transform root;
        private Material foundationMaterial;
        private Material scaffoldMaterial;
        private Material plankMaterial;
        private Material workerBodyMaterial;
        private Material workerHeadMaterial;
        private Material workerToolMaterial;
        private Material labelBackgroundMaterial;

        public void Configure(
            MazeRenderer renderer,
            TerrainDecorationController decorations,
            Func<MazeGenerationResult> currentMazeProvider,
            Action<BuildingType, Vector2Int, int> onCompleted)
        {
            mazeRenderer = renderer;
            terrainDecorations = decorations;
            mazeProvider = currentMazeProvider;
            completedHandler = onCompleted;
        }

        public void BeginConstruction(BuildingType type, Vector2Int position, int footprintRadius, int payload = 0)
        {
            PlaceConstructionSite(type, position, footprintRadius, payload);
            BeginConstructionWork(type, position);
        }

        public void PlaceConstructionSite(BuildingType type, Vector2Int position, int footprintRadius, int payload = 0)
        {
            if (mazeRenderer == null || mazeProvider == null || mazeProvider.Invoke() == null)
            {
                return;
            }

            if (HasSite(type, position))
            {
                return;
            }

            EnsureRoot();
            EnsureMaterials();
            var siteRoot = new GameObject($"Construction Site {type} {position.x},{position.y}").transform;
            siteRoot.SetParent(root, false);
            siteRoot.position = mazeRenderer.GridToWorld(position);
            var siteVisual = BuildSiteVisual(siteRoot, type, footprintRadius);
            var buildSeconds = BaseBuildSeconds + Mathf.Max(0, footprintRadius) * BuildSecondsPerFootprint;
            var site = new ConstructionSiteRuntime(
                type,
                position,
                footprintRadius,
                payload,
                siteRoot,
                siteVisual,
                buildSeconds);
            sites.Add(site);
            GameDebugLog.Info(
                "Base",
                $"Construction site placed: type={type}, position={GameDebugLog.Position(position)}, footprint={footprintRadius}, buildSeconds={buildSeconds:0.0}.");
        }

        public bool BeginConstructionWork(BuildingType type, Vector2Int position)
        {
            var site = FindSite(type, position);
            if (site == null || site.WorkStarted)
            {
                return false;
            }

            var workerPath = BuildWorkerWorldPath(position);
            var worker = BuildWorker(workerPath.Count > 0 ? workerPath[0] : mazeRenderer.GridToWorld(position));
            site.StartWorker(worker, workerPath);
            GameDebugLog.Info(
                "Base",
                $"Construction worker dispatched: type={type}, position={GameDebugLog.Position(position)}, workerPath={workerPath.Count}.");
            return true;
        }

        public void Clear()
        {
            for (var i = sites.Count - 1; i >= 0; i--)
            {
                sites[i].Destroy();
            }

            sites.Clear();
            if (root != null)
            {
                Destroy(root.gameObject);
                root = null;
            }
        }

        private void Update()
        {
            if (mazeRenderer == null || sites.Count == 0)
            {
                return;
            }

            var moveDistance = mazeRenderer.CellSize * WorkerSpeedCellsPerSecond * Time.deltaTime;
            for (var i = sites.Count - 1; i >= 0; i--)
            {
                var site = sites[i];
                if (site == null)
                {
                    sites.RemoveAt(i);
                    continue;
                }

                if (!site.WorkStarted)
                {
                    site.RefreshLabelBillboard();
                    continue;
                }

                if (!site.WorkerArrived)
                {
                    site.MoveWorker(moveDistance);
                    site.RefreshLabelBillboard();
                    continue;
                }

                site.Build(Time.deltaTime);
                if (!site.IsComplete)
                {
                    continue;
                }

                var type = site.Type;
                var position = site.Position;
                var payload = site.Payload;
                site.Destroy();
                sites.RemoveAt(i);
                completedHandler?.Invoke(type, position, payload);
            }
        }

        private bool HasSite(BuildingType type, Vector2Int position)
        {
            return FindSite(type, position) != null;
        }

        private ConstructionSiteRuntime FindSite(BuildingType type, Vector2Int position)
        {
            for (var i = 0; i < sites.Count; i++)
            {
                if (sites[i].Type == type && sites[i].Position == position)
                {
                    return sites[i];
                }
            }

            return null;
        }

        private ConstructionSiteVisual BuildSiteVisual(Transform parent, BuildingType type, int footprintRadius)
        {
            var unit = mazeRenderer.CellSize;
            var radius = Mathf.Max(1, footprintRadius);
            var width = unit * (radius * 1.16f + 1.05f);
            CreatePart(
                "Construction Foundation",
                parent,
                new Vector3(0f, unit * 0.025f, 0f),
                new Vector3(width, unit * 0.05f, width),
                foundationMaterial);
            var label = BuildSiteLabel(parent, type, unit, radius);

            var pieces = new List<Transform>();
            AddScaffoldPost(parent, pieces, -width * 0.42f, -width * 0.42f, unit);
            AddScaffoldPost(parent, pieces, -width * 0.42f, width * 0.42f, unit);
            AddScaffoldPost(parent, pieces, width * 0.42f, -width * 0.42f, unit);
            AddScaffoldPost(parent, pieces, width * 0.42f, width * 0.42f, unit);

            for (var i = 0; i < 6; i++)
            {
                var y = unit * (0.12f + i * 0.13f);
                var horizontal = (i & 1) == 0;
                var offset = (i - 2.5f) * unit * 0.11f;
                var scale = horizontal
                    ? new Vector3(width * 0.82f, unit * 0.07f, unit * 0.12f)
                    : new Vector3(unit * 0.12f, unit * 0.07f, width * 0.82f);
                var localPosition = horizontal
                    ? new Vector3(0f, y, offset)
                    : new Vector3(offset, y, 0f);
                var piece = CreatePart($"Construction {type} Stage {i + 1}", parent, localPosition, scale, plankMaterial);
                piece.gameObject.SetActive(false);
                pieces.Add(piece);
            }

            return new ConstructionSiteVisual(pieces, label);
        }

        private Transform BuildSiteLabel(Transform parent, BuildingType type, float unit, int footprintRadius)
        {
            var labelRoot = new GameObject("Construction Site Label").transform;
            labelRoot.SetParent(parent, false);
            labelRoot.localPosition = new Vector3(0f, unit * (1.8f + footprintRadius * 0.42f), 0f);

            var background = GameObject.CreatePrimitive(PrimitiveType.Quad);
            background.name = "Construction Site Label Background";
            background.transform.SetParent(labelRoot, false);
            background.transform.localPosition = Vector3.zero;
            background.transform.localScale = new Vector3(unit * 2.9f, unit * 0.46f, 1f);
            background.GetComponent<Renderer>().sharedMaterial = labelBackgroundMaterial;
            RemoveCollider(background);

            var text = $"Стройка: {GetConstructionDisplayName(type)}";
            CreateLabelText(labelRoot, "Construction Site Label Shadow", text, new Vector3(unit * 0.022f, -unit * 0.022f, -0.034f), new Color(0f, 0f, 0f, 0.9f));
            CreateLabelText(labelRoot, "Construction Site Label Text", text, new Vector3(0f, 0f, -0.045f), new Color(1f, 0.9f, 0.58f, 1f));
            return labelRoot;
        }

        private static void CreateLabelText(Transform parent, string objectName, string text, Vector3 localPosition, Color color)
        {
            var textObject = new GameObject(objectName);
            textObject.transform.SetParent(parent, false);
            textObject.transform.localPosition = localPosition;
            var textMesh = textObject.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.fontSize = 64;
            textMesh.characterSize = 0.075f;
            textMesh.fontStyle = FontStyle.Bold;
            textMesh.color = color;
            var meshRenderer = textObject.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.sortingOrder = 42;
            }
        }

        private static string GetConstructionDisplayName(BuildingType type)
        {
            switch (type)
            {
                case BuildingType.Farm:
                    return "ферма";
                case BuildingType.LumberjackCamp:
                    return "лесорубы";
                case BuildingType.HeroHouse:
                    return "дом героя";
                case BuildingType.PeasantHut:
                    return "лачужка";
                case BuildingType.AlchemistShop:
                    return "алхимик";
                case BuildingType.Tavern:
                    return "харчевня";
                case BuildingType.Forge:
                    return "кузница";
                case BuildingType.Infirmary:
                    return "лазарет";
                case BuildingType.CartographerHouse:
                    return "картограф";
                case BuildingType.Chapel:
                    return "часовня";
                case BuildingType.MinersGuild:
                    return "шахтеры";
                case BuildingType.Market:
                    return "рынок";
                case BuildingType.Antiquary:
                    return "антиквариат";
                case BuildingType.HeroesGuild:
                    return "гильдия";
                default:
                    return "здание";
            }
        }

        private void AddScaffoldPost(Transform parent, List<Transform> pieces, float x, float z, float unit)
        {
            var post = CreatePart(
                "Construction Scaffold Post",
                parent,
                new Vector3(x, unit * 0.44f, z),
                new Vector3(unit * 0.11f, unit * 0.88f, unit * 0.11f),
                scaffoldMaterial);
            post.gameObject.SetActive(false);
            pieces.Add(post);
        }

        private Transform BuildWorker(Vector3 position)
        {
            var worker = new GameObject("Base Construction Worker").transform;
            worker.SetParent(root, false);
            worker.position = position + Vector3.up * mazeRenderer.CellSize * WorkerYOffset;
            var unit = mazeRenderer.ModelUnitSize * 1.16f;
            VoxelVisuals.CreateContactShadow(
                "Base Builder Contact Shadow",
                worker,
                new Vector3(0f, 0.006f, 0f),
                new Vector3(unit * 0.34f, 0.004f, unit * 0.27f),
                0.3f);
            CreateLocalPart("Builder Body", worker, new Vector3(0f, unit * 0.32f, 0f), new Vector3(unit * 0.2f, unit * 0.34f, unit * 0.2f), workerBodyMaterial, PrimitiveType.Capsule);
            CreateLocalPart("Builder Head", worker, new Vector3(0f, unit * 0.74f, 0f), Vector3.one * unit * 0.17f, workerHeadMaterial, PrimitiveType.Sphere);
            CreateLocalPart("Builder Left Foot", worker, new Vector3(-unit * 0.09f, unit * 0.09f, unit * 0.05f), new Vector3(unit * 0.1f, unit * 0.08f, unit * 0.16f), workerBodyMaterial, PrimitiveType.Cube);
            CreateLocalPart("Builder Right Foot", worker, new Vector3(unit * 0.09f, unit * 0.09f, unit * 0.05f), new Vector3(unit * 0.1f, unit * 0.08f, unit * 0.16f), workerBodyMaterial, PrimitiveType.Cube);
            var hammer = CreateLocalPart("Builder Hammer", worker, new Vector3(unit * 0.18f, unit * 0.5f, unit * 0.06f), new Vector3(unit * 0.055f, unit * 0.46f, unit * 0.055f), workerToolMaterial, PrimitiveType.Cube);
            hammer.localRotation = Quaternion.Euler(0f, 0f, 35f);
            AmbientWalkerMoveAnimator.Attach(worker, unit, Mathf.RoundToInt(position.x * 73f) ^ Mathf.RoundToInt(position.z * 151f));
            return worker;
        }

        private List<Vector3> BuildWorkerWorldPath(Vector2Int target)
        {
            var maze = mazeProvider?.Invoke();
            if (maze == null || maze.Grid == null)
            {
                return new List<Vector3>();
            }

            var cells = BuildWorkerCellPath(maze, maze.BasePosition, target);
            if (cells.Count < 2)
            {
                cells = BuildManhattanPath(maze.BasePosition, target);
            }

            return SubCellPathBuilder.Build(
                mazeRenderer,
                cells,
                mazeRenderer.CellSize * WorkerYOffset,
                SubCellPathBuilder.BuildSeed(cells, 0x4157),
                SubCellPathProfile.Worker);
        }

        private List<Vector2Int> BuildWorkerCellPath(MazeGenerationResult maze, Vector2Int start, Vector2Int end)
        {
            var frontier = new Queue<Vector2Int>();
            var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            frontier.Enqueue(start);
            cameFrom[start] = start;

            while (frontier.Count > 0)
            {
                var current = frontier.Dequeue();
                if (current == end)
                {
                    return RestorePath(cameFrom, start, end);
                }

                for (var i = 0; i < directions.Length; i++)
                {
                    var next = current + directions[i];
                    if (cameFrom.ContainsKey(next) || !IsAllowedWorkerCell(maze, next, start, end))
                    {
                        continue;
                    }

                    cameFrom[next] = current;
                    frontier.Enqueue(next);
                }
            }

            return new List<Vector2Int>();
        }

        private bool IsAllowedWorkerCell(MazeGenerationResult maze, Vector2Int cell, Vector2Int start, Vector2Int end)
        {
            if (cell == start || cell == end)
            {
                return true;
            }

            if (maze.Grid.InBounds(cell) || !IsInsideTerrain(maze, cell))
            {
                return false;
            }

            return terrainDecorations == null || !terrainDecorations.BlocksCityWalker(cell);
        }

        private static bool IsInsideTerrain(MazeGenerationResult maze, Vector2Int cell)
        {
            var minX = -MazeTerrain.PaddingCells;
            var minY = -MazeTerrain.PaddingCells;
            var maxX = maze.Grid.Width - 1 + MazeTerrain.PaddingCells;
            var maxY = maze.Grid.Height - 1 + MazeTerrain.PaddingCells;
            return cell.x >= minX && cell.y >= minY && cell.x <= maxX && cell.y <= maxY;
        }

        private static List<Vector2Int> RestorePath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int start, Vector2Int end)
        {
            var path = new List<Vector2Int>();
            if (!cameFrom.ContainsKey(end))
            {
                return path;
            }

            var current = end;
            path.Add(current);
            while (current != start)
            {
                current = cameFrom[current];
                path.Add(current);
            }

            path.Reverse();
            return path;
        }

        private static List<Vector2Int> BuildManhattanPath(Vector2Int start, Vector2Int end)
        {
            var path = new List<Vector2Int> { start };
            var current = start;
            while (current.x != end.x)
            {
                current.x += current.x < end.x ? 1 : -1;
                path.Add(current);
            }

            while (current.y != end.y)
            {
                current.y += current.y < end.y ? 1 : -1;
                path.Add(current);
            }

            return path;
        }

        private void EnsureRoot()
        {
            if (root != null)
            {
                return;
            }

            root = new GameObject("BaseConstructionRoot").transform;
            root.SetParent(transform, false);
        }

        private void EnsureMaterials()
        {
            if (foundationMaterial != null)
            {
                return;
            }

            foundationMaterial = VoxelVisuals.CreateLitMaterial("Construction Foundation", new Color(0.33f, 0.29f, 0.22f));
            scaffoldMaterial = VoxelVisuals.CreateLitMaterial("Construction Scaffold", new Color(0.48f, 0.3f, 0.12f));
            plankMaterial = VoxelVisuals.CreateLitMaterial("Construction Planks", new Color(0.58f, 0.37f, 0.16f));
            workerBodyMaterial = VoxelVisuals.CreateLitMaterial("Base Builder Body", new Color(0.31f, 0.27f, 0.2f));
            workerHeadMaterial = VoxelVisuals.CreateLitMaterial("Base Builder Head", new Color(0.74f, 0.6f, 0.42f));
            workerToolMaterial = VoxelVisuals.CreateLitMaterial("Base Builder Tool", new Color(0.5f, 0.48f, 0.44f));
            labelBackgroundMaterial = CreateTransparentMaterial("Construction Site Label Background", new Color(0.055f, 0.047f, 0.035f, 0.86f));
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

        private static Transform CreatePart(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            RemoveCollider(part);
            VoxelVisuals.ApplyBlockStyle(part, PrimitiveType.Cube, material, false);
            return part.transform;
        }

        private static Transform CreateLocalPart(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material, PrimitiveType primitive)
        {
            var part = GameObject.CreatePrimitive(VoxelVisuals.ResolvePrimitive(primitive, name));
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            RemoveCollider(part);
            VoxelVisuals.ApplyBlockStyle(part, primitive, material, false);
            return part.transform;
        }

        private static void RemoveCollider(GameObject target)
        {
            var collider = target.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.Destroy(collider);
            }
        }

        private sealed class ConstructionSiteRuntime
        {
            private readonly Transform siteRoot;
            private readonly ConstructionSiteVisual visual;
            private readonly float buildSeconds;
            private Transform worker;
            private List<Vector3> workerPath = new List<Vector3>();
            private int pathIndex = 1;
            private float buildProgress;

            public ConstructionSiteRuntime(
                BuildingType type,
                Vector2Int position,
                int footprintRadius,
                int payload,
                Transform siteRoot,
                ConstructionSiteVisual visual,
                float buildSeconds)
            {
                Type = type;
                Position = position;
                FootprintRadius = footprintRadius;
                Payload = payload;
                this.siteRoot = siteRoot;
                this.visual = visual;
                this.buildSeconds = Mathf.Max(1f, buildSeconds);
                visual.SetProgress(0f);
                RefreshLabelBillboard();
            }

            public BuildingType Type { get; }

            public Vector2Int Position { get; }

            public int FootprintRadius { get; }

            public int Payload { get; }

            public bool WorkerArrived { get; private set; }

            public bool WorkStarted => worker != null;

            public bool IsComplete => buildProgress >= 1f;

            public void StartWorker(Transform constructionWorker, List<Vector3> path)
            {
                worker = constructionWorker;
                workerPath = path ?? new List<Vector3>();
                pathIndex = workerPath.Count > 1 ? 1 : 0;
                WorkerArrived = false;
                FaceNextWaypoint();
            }

            public void MoveWorker(float distance)
            {
                if (worker == null || workerPath.Count == 0 || pathIndex >= workerPath.Count)
                {
                    WorkerArrived = true;
                    return;
                }

                var remaining = distance;
                while (remaining > 0f && pathIndex < workerPath.Count)
                {
                    var target = workerPath[pathIndex];
                    var offset = target - worker.position;
                    var stepDistance = offset.magnitude;
                    if (stepDistance <= Mathf.Max(remaining, 0.001f))
                    {
                        worker.position = target;
                        remaining -= stepDistance;
                        pathIndex++;
                        FaceNextWaypoint();
                        continue;
                    }

                    var direction = offset / stepDistance;
                    worker.position += direction * remaining;
                    worker.rotation = Quaternion.Lerp(worker.rotation, Quaternion.LookRotation(direction, Vector3.up), 0.24f);
                    remaining = 0f;
                }

                WorkerArrived = pathIndex >= workerPath.Count;
            }

            public void Build(float deltaTime)
            {
                buildProgress = Mathf.Clamp01(buildProgress + deltaTime / buildSeconds);
                visual.SetProgress(buildProgress);
                AnimateWorker(buildProgress);
                RefreshLabelBillboard();
            }

            public void RefreshLabelBillboard()
            {
                visual.RefreshLabelBillboard();
            }

            public void Destroy()
            {
                if (siteRoot != null)
                {
                    UnityEngine.Object.Destroy(siteRoot.gameObject);
                }

                if (worker != null)
                {
                    UnityEngine.Object.Destroy(worker.gameObject);
                }
            }

            private void FaceNextWaypoint()
            {
                if (worker == null || pathIndex >= workerPath.Count)
                {
                    return;
                }

                var direction = workerPath[pathIndex] - worker.position;
                if (direction.sqrMagnitude > 0.0001f)
                {
                    worker.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
                }
            }

            private void AnimateWorker(float progress)
            {
                if (worker == null)
                {
                    return;
                }

                var hammer = worker.Find("Builder Hammer");
                if (hammer != null)
                {
                    var swing = Mathf.Sin(progress * Mathf.PI * 22f);
                    hammer.localRotation = Quaternion.Euler(0f, 0f, 35f + swing * 52f);
                }
            }
        }

        private sealed class ConstructionSiteVisual
        {
            private readonly List<Transform> pieces;
            private readonly Transform labelRoot;

            public ConstructionSiteVisual(List<Transform> pieces, Transform labelRoot)
            {
                this.pieces = pieces;
                this.labelRoot = labelRoot;
            }

            public void SetProgress(float progress)
            {
                var visibleCount = Mathf.FloorToInt(Mathf.Clamp01(progress) * pieces.Count + 0.0001f);
                for (var i = 0; i < pieces.Count; i++)
                {
                    if (pieces[i] != null)
                    {
                        pieces[i].gameObject.SetActive(i < visibleCount);
                    }
                }
            }

            public void RefreshLabelBillboard()
            {
                if (labelRoot == null)
                {
                    return;
                }

                var camera = Camera.main;
                if (camera != null)
                {
                    labelRoot.rotation = camera.transform.rotation;
                }
            }
        }
    }
}
