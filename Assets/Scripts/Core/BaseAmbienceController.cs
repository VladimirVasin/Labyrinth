using System.Collections.Generic;
using Labyrinth.Base;
using Labyrinth.Maze;
using UnityEngine;

namespace Labyrinth.Core
{
    public sealed partial class BaseAmbienceController : MonoBehaviour
    {
        private const float RoadWorkerSpeedCellsPerSecond = 1.9f;
        private const float RoadSegmentBuildSeconds = 0.55f;
        private const float RoadWorkerYOffset = 0.06f;
        private const float RoadHeight = 0.028f;
        private const float RoadWidthRatio = 0.3f;
        private const float RoadYOffset = 0.018f;
        private const float CartYOffset = 0.052f;
        private const float CartSpeedCellsPerSecond = 2.35f;
        private const int MaxActiveCarts = 18;

        private enum MazeSide
        {
            Left,
            Right,
            Bottom,
            Top
        }

        private readonly List<RoadConnection> roads = new List<RoadConnection>();
        private readonly List<CartRuntime> carts = new List<CartRuntime>();
        private readonly List<AmbientBuilding> buildings = new List<AmbientBuilding>();

        private MazeGenerationResult result;
        private MazeRenderer mazeRenderer;
        private TerrainDecorationController terrainDecorations;
        private Transform root;
        private Material roadMaterial;
        private Material cartWoodMaterial;
        private Material cartWheelMaterial;
        private Material cartCargoMaterial;
        private Material roadWorkerBodyMaterial;
        private Material roadWorkerHeadMaterial;
        private Material roadWorkerToolMaterial;

        public event System.Action<Vector2Int, Vector2Int, int> FarmCartDelivered;
        public event System.Action<BuildingType, Vector2Int> RoadCompleted;

        public void Configure(TerrainDecorationController decorations)
        {
            terrainDecorations = decorations;
        }

        public void Initialize(MazeGenerationResult generationResult, MazeRenderer renderer)
        {
            Clear();
            result = generationResult;
            mazeRenderer = renderer;
            if (result == null || mazeRenderer == null)
            {
                return;
            }

            EnsureMaterials();
            root = new GameObject("BaseAmbienceRoot").transform;
            root.SetParent(transform, false);
            buildings.Add(new AmbientBuilding(
                BuildingType.Castle,
                result.BasePosition,
                BaseDevelopment.CastleFootprintRadiusCells));
            AddEntranceRoad();
        }

        public void Clear()
        {
            ClearRoadRuntime();
            buildings.Clear();

            if (root != null)
            {
                Destroy(root.gameObject);
                root = null;
            }

            result = null;
            mazeRenderer = null;
        }

        public void RegisterBuilding(BuildingType type, Vector2Int buildingPosition)
        {
            if (type == BuildingType.Castle || result == null || mazeRenderer == null)
            {
                return;
            }

            if (IsBuildingRegistered(type, buildingPosition))
            {
                return;
            }

            if (root == null)
            {
                root = new GameObject("BaseAmbienceRoot").transform;
                root.SetParent(transform, false);
            }

            EnsureMaterials();
            var building = new AmbientBuilding(type, buildingPosition, GetFootprintRadius(type));
            buildings.Add(building);
            AddRoad(building, BuildRoadPath(building));
        }

        public bool TrySendFarmCart(Vector2Int farmPosition, int foodAmount)
        {
            return TrySendProductionCart(BuildingType.Farm, farmPosition, foodAmount);
        }

        public bool TrySendLumberCart(Vector2Int campPosition, int woodAmount)
        {
            return TrySendProductionCart(BuildingType.LumberjackCamp, campPosition, woodAmount);
        }

        private bool TrySendProductionCart(BuildingType type, Vector2Int buildingPosition, int amount)
        {
            if (amount <= 0 || carts.Count >= MaxActiveCarts || HasActiveCartFor(buildingPosition))
            {
                return false;
            }

            foreach (var road in roads)
            {
                if (road.Type == type
                    && road.BuildingPosition == buildingPosition
                    && road.IsComplete)
                {
                    SpawnCart(road, amount);
                    return true;
                }
            }

            return false;
        }

        private bool HasActiveCartFor(Vector2Int buildingPosition)
        {
            for (var i = 0; i < carts.Count; i++)
            {
                if (carts[i].FarmPosition == buildingPosition)
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryGetRoadPath(Vector2Int start, Vector2Int end, out List<Vector2Int> path)
        {
            for (var i = 0; i < roads.Count; i++)
            {
                var roadPath = roads[i].Path;
                if (roadPath.Count < 2)
                {
                    continue;
                }

                if (!roads[i].IsComplete)
                {
                    continue;
                }

                if (roadPath[0] == start && roadPath[roadPath.Count - 1] == end)
                {
                    path = new List<Vector2Int>(roadPath);
                    return true;
                }

                if (roadPath[0] == end && roadPath[roadPath.Count - 1] == start)
                {
                    path = new List<Vector2Int>(roadPath);
                    path.Reverse();
                    return true;
                }
            }

            path = null;
            return false;
        }

        private void AddRoad(AmbientBuilding building, List<Vector2Int> path)
        {
            if (path.Count < 2)
            {
                GameDebugLog.Warning(
                    "Base",
                    $"Ambient road skipped for {building.Type} at {GameDebugLog.Position(building.Position)} -> castle {GameDebugLog.Position(result.BasePosition)}: no safe outside-maze route.");
                return;
            }

            roads.Add(new RoadConnection(building.Type, building.Position, path));
            GameDebugLog.Info(
                "Base",
                $"Ambient road started: {building.Type} at {GameDebugLog.Position(building.Position)} -> castle {GameDebugLog.Position(result.BasePosition)}, start={GameDebugLog.Position(path[0])}, end={GameDebugLog.Position(path[path.Count - 1])}, segments={path.Count - 1}.");
        }

        private void AddEntranceRoad()
        {
            if (result == null || result.Grid == null || buildings.Count == 0)
            {
                return;
            }

            var castle = buildings[0];
            var start = result.BasePosition;
            var path = BuildValidDirectPath(start, result.EntrancePosition, castle);
            if (path.Count == 0)
            {
                path = BuildRoadPathWithSearch(start, result.EntrancePosition, castle);
            }

            if (path.Count < 2)
            {
                GameDebugLog.Warning("Base", "Entrance road skipped: no safe route from castle to labyrinth entrance.");
                return;
            }

            var road = new RoadConnection(BuildingType.Castle, result.BasePosition, path);
            roads.Add(road);

            GameDebugLog.Info("Base", $"Entrance road queued: castle={GameDebugLog.Position(result.BasePosition)}, entrance={GameDebugLog.Position(result.EntrancePosition)}, segments={path.Count - 1}.");
        }

        private void ClearRoadRuntime()
        {
            foreach (var cart in carts)
            {
                cart.Destroy();
            }

            carts.Clear();

            foreach (var road in roads)
            {
                road.DestroySegments();
            }

            roads.Clear();
        }

        private void Update()
        {
            if (root == null || mazeRenderer == null)
            {
                return;
            }

            BuildRoads();
            MoveCarts();
        }

        private void BuildRoads()
        {
            var moveDistance = mazeRenderer.CellSize * RoadWorkerSpeedCellsPerSecond * Time.deltaTime;
            foreach (var road in roads)
            {
                if (road.IsComplete)
                {
                    continue;
                }

                EnsureRoadWorker(road);
                if (road.Worker == null)
                {
                    continue;
                }

                var segmentIndex = GetRoadBuildSegmentIndex(road);
                var target = GetRoadWorkerTarget(road);
                if (!road.Worker.Update(segmentIndex, target, moveDistance, Time.deltaTime, RoadSegmentBuildSeconds))
                {
                    continue;
                }

                CreateRoadSegment(road, segmentIndex);
                road.BuiltSegments++;

                if (road.IsComplete)
                {
                    road.Worker.Destroy();
                    road.Worker = null;
                    GameDebugLog.Info(
                        "Base",
                        $"Ambient road completed for {road.Type} at {GameDebugLog.Position(road.BuildingPosition)}.");
                    RoadCompleted?.Invoke(road.Type, road.BuildingPosition);
                }
            }
        }

        private void MoveCarts()
        {
            var speed = mazeRenderer.CellSize * CartSpeedCellsPerSecond * Time.deltaTime;
            for (var i = carts.Count - 1; i >= 0; i--)
            {
                var arrival = carts[i].Move(speed);
                if (arrival == CartArrival.None)
                {
                    continue;
                }

                if (arrival == CartArrival.Delivered)
                {
                    FarmCartDelivered?.Invoke(carts[i].FarmPosition, result.BasePosition, carts[i].FoodAmount);
                    carts[i].BeginReturn();
                    continue;
                }

                if (arrival == CartArrival.Returned)
                {
                    carts[i].Destroy();
                    carts.RemoveAt(i);
                }
            }
        }

        private bool IsBuildingRegistered(BuildingType type, Vector2Int buildingPosition)
        {
            foreach (var building in buildings)
            {
                if (building.Type == type && building.Position == buildingPosition)
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasCompletedRoad(BuildingType type, Vector2Int buildingPosition)
        {
            for (var i = 0; i < roads.Count; i++)
            {
                var road = roads[i];
                if (road.Type == type && road.BuildingPosition == buildingPosition && road.IsComplete)
                {
                    return true;
                }
            }

            return false;
        }

        private List<Vector2Int> BuildRoadPath(AmbientBuilding building)
        {
            var start = building.Position;
            var end = result.BasePosition;
            var direct = BuildValidDirectPath(start, end, building);
            if (direct.Count > 0)
            {
                return direct;
            }

            var path = BuildPerimeterPath(start, end);
            if (IsValidRoadPath(path, building))
            {
                return path;
            }

            return BuildRoadPathWithSearch(start, end, building);
        }

        private void CreateRoadSegment(RoadConnection road, int segmentIndex)
        {
            if (segmentIndex < 0 || segmentIndex >= road.Path.Count - 1)
            {
                return;
            }

            var from = road.Path[segmentIndex];
            var to = road.Path[segmentIndex + 1];
            var fromWorld = mazeRenderer.GridToWorld(from);
            var toWorld = mazeRenderer.GridToWorld(to);
            var center = Vector3.Lerp(fromWorld, toWorld, 0.5f) + new Vector3(0f, RoadYOffset, 0f);
            var horizontal = from.x != to.x;
            var cellSize = mazeRenderer.CellSize;
            var scale = horizontal
                ? new Vector3(cellSize * 1.08f, RoadHeight, cellSize * RoadWidthRatio)
                : new Vector3(cellSize * RoadWidthRatio, RoadHeight, cellSize * 1.08f);

            var segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
            segment.name = "Ambient Road Segment";
            segment.transform.SetParent(root, false);
            segment.transform.position = center;
            segment.transform.localScale = scale;
            segment.GetComponent<Renderer>().sharedMaterial = roadMaterial;
            RemoveCollider(segment);
            VoxelVisuals.ApplyBlockStyle(segment, PrimitiveType.Cube, roadMaterial, false);
            road.Segments.Add(segment);
            terrainDecorations?.RegisterRoadSegment(from, to);
        }

        private void EnsureRoadWorker(RoadConnection road)
        {
            if (road.Worker != null || road.Path.Count == 0)
            {
                return;
            }

            var startIndex = GetRoadWorkerStartIndex(road);
            var start = mazeRenderer.GridToWorld(road.Path[startIndex]) + Vector3.up * mazeRenderer.CellSize * RoadWorkerYOffset;
            var worker = BuildRoadWorker(start, road);
            road.Worker = worker;
            GameDebugLog.Info(
                "Base",
                $"Road worker started: type={road.Type}, building={GameDebugLog.Position(road.BuildingPosition)}, from={GameDebugLog.Position(road.Path[startIndex])}, nextSegment={road.BuiltSegments + 1}/{road.Path.Count - 1}.");
        }

        private int GetRoadBuildSegmentIndex(RoadConnection road)
        {
            return ShouldBuildRoadForward(road)
                ? road.BuiltSegments
                : road.Path.Count - 2 - road.BuiltSegments;
        }

        private int GetRoadWorkerStartIndex(RoadConnection road)
        {
            return ShouldBuildRoadForward(road)
                ? Mathf.Clamp(road.BuiltSegments, 0, road.Path.Count - 1)
                : Mathf.Clamp(road.Path.Count - 1 - road.BuiltSegments, 0, road.Path.Count - 1);
        }

        private Vector3 GetRoadWorkerTarget(RoadConnection road)
        {
            var nextIndex = ShouldBuildRoadForward(road)
                ? Mathf.Clamp(road.BuiltSegments + 1, 0, road.Path.Count - 1)
                : Mathf.Clamp(road.Path.Count - 2 - road.BuiltSegments, 0, road.Path.Count - 1);
            return mazeRenderer.GridToWorld(road.Path[nextIndex]) + Vector3.up * mazeRenderer.CellSize * RoadWorkerYOffset;
        }

        private bool ShouldBuildRoadForward(RoadConnection road)
        {
            if (result != null && road.Path.Count > 0)
            {
                if (road.Path[0] == result.BasePosition)
                {
                    return true;
                }

                if (road.Path[road.Path.Count - 1] == result.BasePosition)
                {
                    return false;
                }
            }

            return road.Type == BuildingType.Castle;
        }

        private RoadWorkerRuntime BuildRoadWorker(Vector3 position, RoadConnection road)
        {
            var worker = new GameObject("Road Construction Worker").transform;
            worker.SetParent(root, false);
            worker.position = position;
            var unit = mazeRenderer.ModelUnitSize * 1.08f;
            VoxelVisuals.CreateContactShadow(
                "Road Worker Contact Shadow",
                worker,
                new Vector3(0f, 0.006f, 0f),
                new Vector3(unit * 0.34f, 0.004f, unit * 0.27f),
                0.3f);
            CreateCartPart("Road Worker Body", worker, PrimitiveType.Capsule, new Vector3(0f, unit * 0.32f, 0f), new Vector3(unit * 0.2f, unit * 0.34f, unit * 0.2f), Quaternion.identity, roadWorkerBodyMaterial);
            CreateCartPart("Road Worker Head", worker, PrimitiveType.Sphere, new Vector3(0f, unit * 0.74f, 0f), Vector3.one * unit * 0.16f, Quaternion.identity, roadWorkerHeadMaterial);
            CreateCartPart("Road Worker Left Foot", worker, PrimitiveType.Cube, new Vector3(-unit * 0.09f, unit * 0.09f, unit * 0.05f), new Vector3(unit * 0.1f, unit * 0.08f, unit * 0.16f), Quaternion.identity, roadWorkerBodyMaterial);
            CreateCartPart("Road Worker Right Foot", worker, PrimitiveType.Cube, new Vector3(unit * 0.09f, unit * 0.09f, unit * 0.05f), new Vector3(unit * 0.1f, unit * 0.08f, unit * 0.16f), Quaternion.identity, roadWorkerBodyMaterial);
            AmbientWalkerMoveAnimator.Attach(worker, unit, BuildRoadWorkerAnimationSeed(road));
            var shovel = CreateCartPart("Road Worker Shovel", worker, PrimitiveType.Cube, new Vector3(unit * 0.18f, unit * 0.48f, unit * 0.08f), new Vector3(unit * 0.055f, unit * 0.52f, unit * 0.055f), Quaternion.Euler(0f, 0f, 34f), roadWorkerToolMaterial);
            CreateCartPart("Road Worker Shovel Blade", worker, PrimitiveType.Cube, new Vector3(unit * 0.28f, unit * 0.22f, unit * 0.08f), new Vector3(unit * 0.16f, unit * 0.06f, unit * 0.18f), Quaternion.Euler(0f, 0f, 34f), roadWorkerToolMaterial);
            return new RoadWorkerRuntime(worker, shovel);
        }

        private void SpawnCart(RoadConnection road, int foodAmount)
        {
            var waypoints = BuildCartWaypoints(road.Path);
            if (waypoints.Count < 2)
            {
                return;
            }

            var isLumber = road.Type == BuildingType.LumberjackCamp;
            var cartRoot = new GameObject(isLumber ? "Lumber Cart" : "Farm Cart");
            cartRoot.transform.SetParent(root, false);
            cartRoot.transform.position = waypoints[0];
            var visuals = BuildCartModel(cartRoot.transform);
            carts.Add(new CartRuntime(cartRoot, waypoints, visuals, road.BuildingPosition, foodAmount));
            if (isLumber)
            {
                GameDebugLog.Info(
                    "Base",
                    $"Lumber cart sent: camp={GameDebugLog.Position(road.BuildingPosition)}, wood={foodAmount}.");
                return;
            }

            GameDebugLog.Info(
                "Base",
                $"Farm cart sent: farm={GameDebugLog.Position(road.BuildingPosition)}, food={foodAmount}.");
        }

        private List<Vector3> BuildCartWaypoints(IReadOnlyList<Vector2Int> path)
        {
            return SubCellPathBuilder.Build(
                mazeRenderer,
                path,
                CartYOffset,
                SubCellPathBuilder.BuildSeed(path, 0x2f49),
                SubCellPathProfile.Cart);
        }

        private CartVisuals BuildCartModel(Transform parent)
        {
            var unit = mazeRenderer.ModelUnitSize;
            VoxelVisuals.CreateContactShadow(
                "Farm Cart Contact Shadow",
                parent,
                new Vector3(0f, 0.006f, 0f),
                new Vector3(unit * 0.72f, 0.004f, unit * 0.5f),
                0.26f);
            var visualRoot = new GameObject("Farm Cart Visual").transform;
            visualRoot.SetParent(parent, false);
            CreateCartPart(
                "Cart Bed",
                visualRoot,
                PrimitiveType.Cube,
                new Vector3(0f, unit * 0.2f, 0f),
                new Vector3(unit * 0.52f, unit * 0.18f, unit * 0.42f),
                Quaternion.identity,
                cartWoodMaterial);
            var cargo = CreateCartPart(
                "Cart Cargo",
                visualRoot,
                PrimitiveType.Cube,
                new Vector3(0f, unit * 0.37f, unit * -0.02f),
                new Vector3(unit * 0.42f, unit * 0.18f, unit * 0.32f),
                Quaternion.identity,
                cartCargoMaterial);
            CreateCartPart(
                "Cart Handle",
                visualRoot,
                PrimitiveType.Cube,
                new Vector3(0f, unit * 0.2f, unit * 0.36f),
                new Vector3(unit * 0.12f, unit * 0.07f, unit * 0.38f),
                Quaternion.identity,
                cartWoodMaterial);

            var wheels = new Transform[4];
            var rotation = Quaternion.Euler(0f, 0f, 90f);
            wheels[0] = CreateWheel(visualRoot, new Vector3(unit * -0.32f, unit * 0.1f, unit * -0.18f), rotation);
            wheels[1] = CreateWheel(visualRoot, new Vector3(unit * 0.32f, unit * 0.1f, unit * -0.18f), rotation);
            wheels[2] = CreateWheel(visualRoot, new Vector3(unit * -0.32f, unit * 0.1f, unit * 0.18f), rotation);
            wheels[3] = CreateWheel(visualRoot, new Vector3(unit * 0.32f, unit * 0.1f, unit * 0.18f), rotation);
            return new CartVisuals(visualRoot, cargo, wheels);
        }

        private Transform CreateWheel(Transform parent, Vector3 localPosition, Quaternion localRotation)
        {
            var unit = mazeRenderer.ModelUnitSize;
            return CreateCartPart(
                "Cart Wheel",
                parent,
                PrimitiveType.Cylinder,
                localPosition,
                new Vector3(unit * 0.16f, unit * 0.055f, unit * 0.16f),
                localRotation,
                cartWheelMaterial);
        }

        private Transform CreateCartPart(
            string partName,
            Transform parent,
            PrimitiveType primitiveType,
            Vector3 localPosition,
            Vector3 localScale,
            Quaternion localRotation,
            Material material)
        {
            var part = GameObject.CreatePrimitive(VoxelVisuals.ResolvePrimitive(primitiveType, partName));
            part.name = partName;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = localRotation;
            part.transform.localScale = localScale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            RemoveCollider(part);
            VoxelVisuals.ApplyBlockStyle(part, primitiveType, material, false);
            return part.transform;
        }

        private void EnsureMaterials()
        {
            if (roadMaterial != null)
            {
                return;
            }

            roadMaterial = CreateMaterial("Ambient Dirt Road", new Color(0.44f, 0.34f, 0.21f));
            cartWoodMaterial = CreateMaterial("Ambient Cart Wood", new Color(0.38f, 0.2f, 0.08f));
            cartWheelMaterial = CreateMaterial("Ambient Cart Wheels", new Color(0.09f, 0.07f, 0.05f));
            cartCargoMaterial = CreateMaterial("Ambient Cart Cargo", new Color(0.7f, 0.56f, 0.22f));
            roadWorkerBodyMaterial = CreateMaterial("Road Worker Body", new Color(0.31f, 0.25f, 0.18f));
            roadWorkerHeadMaterial = CreateMaterial("Road Worker Head", new Color(0.73f, 0.58f, 0.4f));
            roadWorkerToolMaterial = CreateMaterial("Road Worker Tool", new Color(0.42f, 0.37f, 0.29f));
        }

        private List<Vector2Int> BuildValidDirectPath(Vector2Int start, Vector2Int end, AmbientBuilding building)
        {
            var horizontalFirst = (Hash(building.Position) & 1) == 0;
            var first = BuildManhattanPath(start, end, horizontalFirst);
            if (IsValidRoadPath(first, building))
            {
                return first;
            }

            var second = BuildManhattanPath(start, end, !horizontalFirst);
            return IsValidRoadPath(second, building) ? second : new List<Vector2Int>();
        }

        private List<Vector2Int> BuildPerimeterPath(Vector2Int start, Vector2Int end)
        {
            var startSide = GetOutsideSide(start);
            var endSide = GetOutsideSide(end);
            var startLane = ProjectToSideLane(start, startSide);
            var endLane = ProjectToSideLane(end, endSide);
            var waypoints = new List<Vector2Int> { start, startLane };
            AddPerimeterCorners(waypoints, startSide, endSide, startLane, endLane);
            waypoints.Add(endLane);
            waypoints.Add(end);
            return BuildPathThroughWaypoints(waypoints);
        }

        private void AddPerimeterCorners(
            List<Vector2Int> waypoints,
            MazeSide startSide,
            MazeSide endSide,
            Vector2Int startLane,
            Vector2Int endLane)
        {
            if (startSide == endSide)
            {
                return;
            }

            if (AreAdjacentSides(startSide, endSide))
            {
                waypoints.Add(GetSharedCorner(startSide, endSide));
                return;
            }

            if ((startSide == MazeSide.Left && endSide == MazeSide.Right)
                || (startSide == MazeSide.Right && endSide == MazeSide.Left))
            {
                var topCost = Mathf.Abs(startLane.y - result.Grid.Height) + Mathf.Abs(endLane.y - result.Grid.Height);
                var bottomCost = Mathf.Abs(startLane.y + 1) + Mathf.Abs(endLane.y + 1);
                var routeTop = topCost <= bottomCost;
                waypoints.Add(GetCorner(startSide, routeTop ? MazeSide.Top : MazeSide.Bottom));
                waypoints.Add(GetCorner(endSide, routeTop ? MazeSide.Top : MazeSide.Bottom));
                return;
            }

            var rightCost = Mathf.Abs(startLane.x - result.Grid.Width) + Mathf.Abs(endLane.x - result.Grid.Width);
            var leftCost = Mathf.Abs(startLane.x + 1) + Mathf.Abs(endLane.x + 1);
            var routeRight = rightCost <= leftCost;
            waypoints.Add(GetCorner(startSide, routeRight ? MazeSide.Right : MazeSide.Left));
            waypoints.Add(GetCorner(endSide, routeRight ? MazeSide.Right : MazeSide.Left));
        }

        private List<Vector2Int> BuildPathThroughWaypoints(IReadOnlyList<Vector2Int> waypoints)
        {
            var path = new List<Vector2Int>();
            if (waypoints == null || waypoints.Count == 0)
            {
                return path;
            }

            path.Add(waypoints[0]);
            var current = waypoints[0];
            for (var i = 1; i < waypoints.Count; i++)
            {
                AddAxisPath(path, ref current, waypoints[i].x, true);
                AddAxisPath(path, ref current, waypoints[i].y, false);
            }

            RemoveConsecutiveDuplicates(path);
            return path;
        }

        private List<Vector2Int> BuildRoadPathWithSearch(Vector2Int start, Vector2Int end, AmbientBuilding building)
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
                    return RestoreSearchPath(cameFrom, start, end);
                }

                foreach (var direction in MazeDirections.Cardinal)
                {
                    var next = current + direction;
                    if (cameFrom.ContainsKey(next)
                        || !IsAllowedRoadPosition(next, building, next == end))
                    {
                        continue;
                    }

                    cameFrom[next] = current;
                    frontier.Enqueue(next);
                }
            }

            return new List<Vector2Int>();
        }

        private static List<Vector2Int> RestoreSearchPath(
            Dictionary<Vector2Int, Vector2Int> cameFrom,
            Vector2Int start,
            Vector2Int end)
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

        private bool IsValidRoadPath(IReadOnlyList<Vector2Int> path, AmbientBuilding building)
        {
            if (path == null || path.Count < 2 || result == null || result.Grid == null)
            {
                return false;
            }

            for (var i = 0; i < path.Count; i++)
            {
                if (!IsAllowedRoadPosition(path[i], building, i == path.Count - 1, i == 0))
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsAllowedRoadPosition(
            Vector2Int position,
            AmbientBuilding roadOwner,
            bool isEnd,
            bool isStart = false)
        {
            if (roadOwner.Type == BuildingType.Castle && isEnd && position == result.EntrancePosition)
            {
                return true;
            }

            if (result.Grid.InBounds(position) || !IsInsideTerrain(position))
            {
                return false;
            }

            foreach (var building in buildings)
            {
                if (!building.Contains(position))
                {
                    continue;
                }

                if (building.Type == BuildingType.Castle || building.Position == roadOwner.Position)
                {
                    return true;
                }

                return false;
            }

            return true;
        }

        private bool IsInsideTerrain(Vector2Int position)
        {
            var minX = -MazeTerrain.PaddingCells;
            var minY = -MazeTerrain.PaddingCells;
            var maxX = result.Grid.Width - 1 + MazeTerrain.PaddingCells;
            var maxY = result.Grid.Height - 1 + MazeTerrain.PaddingCells;
            return position.x >= minX
                && position.y >= minY
                && position.x <= maxX
                && position.y <= maxY;
        }

        private MazeSide GetOutsideSide(Vector2Int position)
        {
            if (position.x < 0)
            {
                return MazeSide.Left;
            }

            if (position.x >= result.Grid.Width)
            {
                return MazeSide.Right;
            }

            if (position.y < 0)
            {
                return MazeSide.Bottom;
            }

            return MazeSide.Top;
        }

        private Vector2Int ProjectToSideLane(Vector2Int position, MazeSide side)
        {
            switch (side)
            {
                case MazeSide.Left:
                    return new Vector2Int(-1, Mathf.Clamp(position.y, -1, result.Grid.Height));
                case MazeSide.Right:
                    return new Vector2Int(result.Grid.Width, Mathf.Clamp(position.y, -1, result.Grid.Height));
                case MazeSide.Bottom:
                    return new Vector2Int(Mathf.Clamp(position.x, -1, result.Grid.Width), -1);
                case MazeSide.Top:
                default:
                    return new Vector2Int(Mathf.Clamp(position.x, -1, result.Grid.Width), result.Grid.Height);
            }
        }

        private Vector2Int GetCorner(MazeSide sideA, MazeSide sideB)
        {
            if ((sideA == MazeSide.Left && sideB == MazeSide.Top)
                || (sideA == MazeSide.Top && sideB == MazeSide.Left))
            {
                return new Vector2Int(-1, result.Grid.Height);
            }

            if ((sideA == MazeSide.Right && sideB == MazeSide.Top)
                || (sideA == MazeSide.Top && sideB == MazeSide.Right))
            {
                return new Vector2Int(result.Grid.Width, result.Grid.Height);
            }

            if ((sideA == MazeSide.Right && sideB == MazeSide.Bottom)
                || (sideA == MazeSide.Bottom && sideB == MazeSide.Right))
            {
                return new Vector2Int(result.Grid.Width, -1);
            }

            return new Vector2Int(-1, -1);
        }

        private Vector2Int GetSharedCorner(MazeSide sideA, MazeSide sideB)
        {
            return GetCorner(sideA, sideB);
        }

        private static bool AreAdjacentSides(MazeSide sideA, MazeSide sideB)
        {
            return sideA != sideB
                && !((sideA == MazeSide.Left && sideB == MazeSide.Right)
                    || (sideA == MazeSide.Right && sideB == MazeSide.Left)
                    || (sideA == MazeSide.Top && sideB == MazeSide.Bottom)
                    || (sideA == MazeSide.Bottom && sideB == MazeSide.Top));
        }

        private static void RemoveConsecutiveDuplicates(List<Vector2Int> path)
        {
            for (var i = path.Count - 1; i > 0; i--)
            {
                if (path[i] == path[i - 1])
                {
                    path.RemoveAt(i);
                }
            }
        }

        private static List<Vector2Int> BuildManhattanPath(Vector2Int start, Vector2Int end, bool horizontalFirst)
        {
            var path = new List<Vector2Int> { start };
            var current = start;
            if (horizontalFirst)
            {
                AddAxisPath(path, ref current, end.x, true);
                AddAxisPath(path, ref current, end.y, false);
            }
            else
            {
                AddAxisPath(path, ref current, end.y, false);
                AddAxisPath(path, ref current, end.x, true);
            }

            return path;
        }

        private static void AddAxisPath(List<Vector2Int> path, ref Vector2Int current, int target, bool xAxis)
        {
            while ((xAxis ? current.x : current.y) != target)
            {
                if (xAxis)
                {
                    current.x += current.x < target ? 1 : -1;
                }
                else
                {
                    current.y += current.y < target ? 1 : -1;
                }

                path.Add(current);
            }
        }

        private static int GetFootprintRadius(BuildingType type)
        {
            switch (type)
            {
                case BuildingType.Farm:
                    return BaseDevelopment.FarmFootprintRadiusCells;
                case BuildingType.LumberjackCamp:
                    return BaseDevelopment.LumberjackCampFootprintRadiusCells;
                case BuildingType.HeroHouse:
                    return BaseDevelopment.HeroHouseFootprintRadiusCells;
                case BuildingType.PeasantHut:
                    return BaseDevelopment.PeasantHutFootprintRadiusCells;
                case BuildingType.AlchemistShop:
                    return BaseDevelopment.AlchemistShopFootprintRadiusCells;
                case BuildingType.Tavern:
                    return BaseDevelopment.TavernFootprintRadiusCells;
                case BuildingType.Forge:
                    return BaseDevelopment.ForgeFootprintRadiusCells;
                case BuildingType.Infirmary:
                    return BaseDevelopment.InfirmaryFootprintRadiusCells;
                case BuildingType.CartographerHouse:
                    return BaseDevelopment.CartographerHouseFootprintRadiusCells;
                case BuildingType.Chapel:
                    return BaseDevelopment.ChapelFootprintRadiusCells;
                case BuildingType.MinersGuild:
                    return BaseDevelopment.MinersGuildFootprintRadiusCells;
                case BuildingType.Market:
                    return BaseDevelopment.MarketFootprintRadiusCells;
                case BuildingType.Antiquary:
                    return BaseDevelopment.AntiquaryFootprintRadiusCells;
                case BuildingType.HeroesGuild:
                    return BaseDevelopment.HeroesGuildFootprintRadiusCells;
                case BuildingType.Castle:
                default:
                    return BaseDevelopment.CastleFootprintRadiusCells;
            }
        }

        private static int Hash(Vector2Int position)
        {
            unchecked
            {
                var hash = position.x * 73856093 ^ position.y * 19349663 ^ 0x64b1a5d;
                hash ^= hash >> 13;
                hash *= 1274126177;
                hash ^= hash >> 16;
                return hash & 0x7fffffff;
            }
        }

        private static int BuildRoadWorkerAnimationSeed(RoadConnection road)
        {
            unchecked
            {
                return Hash(road.BuildingPosition)
                    ^ ((int)road.Type * 83492791)
                    ^ (road.Path.Count * 19349663);
            }
        }

        private static Material CreateMaterial(string materialName, Color color)
        {
            return VoxelVisuals.CreateLitMaterial(materialName, color);
        }

        private static void RemoveCollider(GameObject target)
        {
            var collider = target.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }
        }

    }
}
