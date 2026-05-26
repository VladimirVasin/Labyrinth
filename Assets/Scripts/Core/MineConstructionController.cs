using System.Collections.Generic;
using System.Globalization;
using Labyrinth.Combat;
using Labyrinth.Hero;
using Labyrinth.Maze;
using UnityEngine;

namespace Labyrinth.Core
{
    public sealed partial class MineConstructionController : MonoBehaviour
    {
        public const int RouteWoodCost = 1;
        public const int MineWoodCost = 10;

        public const int BaseMineBatchCapacity = 10;
        private const int TorchLightRange = 2;
        private const float WorkerSpeedCellsPerSecond = 2.25f;
        private const float WorkerYOffset = 0.08f;
        private const float CartYOffset = 0.06f;
        private const float CellBuildSeconds = 2f;
        private const float MineBuildSeconds = 5f;
        private const float CartSpeedCellsPerSecond = 2.25f;
        private const float WorkerSpawnIntervalSeconds = 0.38f;
        private const float MineRuntimeTraceIntervalSeconds = 2f;
        private const int UpgradedMineBatchCapacity = 15;
        private const int UpgradedMineUnitsPerTick = 2;
        private const int MaxActiveMineWorkers = 5;

        private readonly List<CaveInfo> selectableCaves = new List<CaveInfo>();
        private readonly List<MineZone> zones = new List<MineZone>();
        private readonly List<MineCartRuntime> mineCarts = new List<MineCartRuntime>();
        private readonly HashSet<Vector2Int> fortifiedCells = new HashSet<Vector2Int>();
        private readonly HashSet<Vector2Int> torchPositions = new HashSet<Vector2Int>();
        private readonly HashSet<Vector2Int> torchLitCells = new HashSet<Vector2Int>();
        private readonly HashSet<string> reinforcedWallFaces = new HashSet<string>();

        private ResourceWallet resources;
        private BaseDevelopment baseDevelopment;
        private BaseAmbienceController baseAmbience;
        private MazeRenderer mazeRenderer;
        private MazeGenerationResult result;
        private HeroMemory knowledge;
        private MineConstructionRenderer constructionRenderer;
        private readonly List<MineWorker> activeWorkers = new List<MineWorker>();
        private float productionProgress;
        private float workerSpawnCooldown;
        private float workerTraceTimer;
        private float cartTraceTimer;
        private int mineWorkerSerial;
        private int mineCartSerial;
        private bool selectionModeActive;
        private string lastStatus = "ожидает гильдию";

        public bool SelectionModeActive => selectionModeActive;

        public bool IsCellFortified(Vector2Int cell)
        {
            return fortifiedCells.Contains(cell);
        }

        public string StatusText
        {
            get
            {
                var completed = 0;
                for (var i = 0; i < zones.Count; i++)
                {
                    if (zones[i].State == MineZoneState.Completed)
                    {
                        completed++;
                    }
                }

                return $"зон {zones.Count}, готово {completed}, укреплено {fortifiedCells.Count}, {lastStatus}";
            }
        }

        public void Configure(ResourceWallet wallet, BaseDevelopment development, MazeRenderer activeMazeRenderer, BaseAmbienceController ambience = null)
        {
            resources = wallet;
            baseDevelopment = development;
            mazeRenderer = activeMazeRenderer;
            baseAmbience = ambience;
        }

        public void Initialize(MazeGenerationResult generationResult)
        {
            Clear();
            result = generationResult;
            if (result == null || result.Grid == null || mazeRenderer == null)
            {
                return;
            }

            constructionRenderer = MineConstructionRenderer.Create(transform, mazeRenderer);
            lastStatus = "готово к выбору пещеры";
        }

        public void Clear()
        {
            selectableCaves.Clear();
            zones.Clear();
            for (var i = 0; i < mineCarts.Count; i++)
            {
                mineCarts[i].Destroy();
            }

            mineCarts.Clear();
            fortifiedCells.Clear();
            torchPositions.Clear();
            torchLitCells.Clear();
            reinforcedWallFaces.Clear();
            selectionModeActive = false;
            for (var i = 0; i < activeWorkers.Count; i++)
            {
                constructionRenderer?.DestroyWorker(activeWorkers[i].Root);
            }

            activeWorkers.Clear();
            knowledge = null;
            productionProgress = 0f;
            workerSpawnCooldown = 0f;
            workerTraceTimer = 0f;
            cartTraceTimer = 0f;
            mineWorkerSerial = 0;
            mineCartSerial = 0;
            lastStatus = "ожидает гильдию";
            if (constructionRenderer != null)
            {
                constructionRenderer.Clear();
                constructionRenderer = null;
            }

            result = null;
        }

        public bool CanBeginSelection(HeroMemory knownMap)
        {
            if (result == null || constructionRenderer == null || knownMap == null)
            {
                return false;
            }

            return CountSelectableCaves(knownMap) > 0;
        }

        public void BeginSelectionMode(HeroMemory knownMap)
        {
            if (result == null || constructionRenderer == null)
            {
                return;
            }

            knowledge = knownMap;
            selectionModeActive = true;
            RebuildSelectableCaves();
            constructionRenderer.RenderCaveSelection(selectableCaves);
            lastStatus = selectableCaves.Count > 0
                ? $"выберите пещеру, доступно {selectableCaves.Count}"
                : "нет изученных свободных минипещер";
            GameDebugLog.Info("Mine", $"Mine selection mode started: selectableCaves={selectableCaves.Count}, zones={zones.Count}.");
        }

        public void CancelSelectionMode()
        {
            selectionModeActive = false;
            constructionRenderer?.ClearSelection();
            lastStatus = zones.Count > 0 ? "строительство выполняется" : "ожидает выбор";
            GameDebugLog.Info("Mine", "Mine selection mode cancelled.");
        }

        public void UpdateHoverCell(Vector2Int cell)
        {
            if (!selectionModeActive || constructionRenderer == null)
            {
                return;
            }

            if (TryGetSelectableCave(cell, out var cave))
            {
                constructionRenderer.ShowHoverMarker(cave, true);
                return;
            }

            if (TryGetAnyCave(cell, out cave))
            {
                constructionRenderer.ShowHoverMarker(cave, false);
                return;
            }

            constructionRenderer.HideHoverMarker();
        }

        public void ClearHoverCell()
        {
            constructionRenderer?.HideHoverMarker();
        }

        public bool TrySelectCave(Vector2Int cell)
        {
            if (!selectionModeActive || result == null || constructionRenderer == null)
            {
                return false;
            }

            if (!TryGetSelectableCave(cell, out var cave))
            {
                lastStatus = "выберите подсвеченную изученную минипещеру";
                return false;
            }

            if (!TryBuildKnownPathToCave(cave, out var path))
            {
                lastStatus = $"нет изученного маршрута к {GameDebugLog.Position(cave.Center)}";
                return false;
            }

            if (!TryGetCaveOreType(cave, out var oreType))
            {
                lastStatus = $"в пещере {GameDebugLog.Position(cave.Center)} нет залежей";
                return false;
            }

            path = BuildMineRouteWithCaveFootprint(cave, path);
            var zone = new MineZone(cave, path, oreType);
            zones.Add(zone);
            var mineRoot = constructionRenderer.RenderMineZone(cave, oreType);
            ConfigureMineHud(zone, mineRoot);
            mazeRenderer.TrackExternalCellRenderer(cave.Center, constructionRenderer.GetCellRoot(cave.Center));
            selectionModeActive = false;
            constructionRenderer.ClearSelection();
            lastStatus = $"зона шахты поставлена {GameDebugLog.Position(cave.Center)}, маршрут {path.Count} клеток";
            GameAudioController.Play(GameSfx.HudConfirm, mazeRenderer.GridToWorld(cave.Center), 0.9f);
            GameDebugLog.Info("Mine", $"Mine zone placed at {GameDebugLog.Position(cave.Center)}. routeLength={path.Count}, caveFootprint=3x3, zones={zones.Count}.");
            return true;
        }

        public void UpdateConstruction()
        {
            if (result == null || constructionRenderer == null)
            {
                return;
            }

            MoveMineCarts();
            ProduceMines();
            UpdateMineWorkers();

            var zone = GetNextActiveZone();
            if (zone == null)
            {
                return;
            }

            if (zone.State == MineZoneState.BuildingRoute)
            {
                UpdateRouteConstruction(zone);
                return;
            }

            UpdateMineBuild(zone);
        }

        public void AddTorchLitCells(HashSet<Vector2Int> visibleCells)
        {
            if (visibleCells == null)
            {
                return;
            }

            foreach (var position in fortifiedCells)
            {
                visibleCells.Add(position);
            }

            foreach (var position in torchLitCells)
            {
                visibleCells.Add(position);
            }
        }

        private void CompleteRouteCell(MineZone zone, Vector2Int target)
        {
            fortifiedCells.Add(target);
            zone.AssignedRouteCells.Remove(target);
            constructionRenderer.RenderFortifiedCell(target);
            mazeRenderer.TrackExternalCellRenderer(target, constructionRenderer.GetCellRoot(target));
            RenderAdjacentWallReinforcements(target);
            var placedTorch = TryPlaceTorch(target);
            RefreshTorchLight();
            AdvanceRouteIndex(zone);
            lastStatus = placedTorch
                ? $"укреплена клетка и поставлен факел {GameDebugLog.Position(target)}"
                : $"укреплена и освещена клетка {GameDebugLog.Position(target)}";
            GameAudioController.Play(placedTorch ? GameSfx.TorchPlaced : GameSfx.Fortify, mazeRenderer.GridToWorld(target), 0.84f);
            GameDebugLog.Info(
                "Mine",
                $"Mine route cell fortified: {GameDebugLog.Position(target)}, torch={placedTorch}, woodLeft={resources.Wood}, routeIndex={zone.RouteIndex}/{zone.Route.Count}, litCells={torchLitCells.Count}.");
        }

        private void CompleteMine(MineZone zone)
        {
            if (zone == null)
            {
                return;
            }

            zone.State = MineZoneState.Completed;
            var mineRoot = constructionRenderer.RenderMine(zone.Cave, zone.OreType, zone.Level);
            ConfigureMineHud(zone, mineRoot);
            mazeRenderer.TrackExternalCellRenderer(zone.Cave.Center, constructionRenderer.GetCellRoot(zone.Cave.Center));
            lastStatus = $"{GetOreTypeName(zone.OreType)} шахта готова {GameDebugLog.Position(zone.Cave.Center)}";
            GameAudioController.Play(GameSfx.Build, mazeRenderer.GridToWorld(zone.Cave.Center), 1f);
            GameDebugLog.Info("Mine", $"Mine completed at {GameDebugLog.Position(zone.Cave.Center)}. ore={zone.OreType}, completedZones={CountCompletedZones()}.");
        }

        private void ConfigureMineHud(MineZone zone, GameObject mineRoot)
        {
            if (zone == null || mineRoot == null)
            {
                return;
            }

            var hudTarget = mineRoot.GetComponent<ObjectMicroHudTarget>();
            if (hudTarget == null)
            {
                hudTarget = mineRoot.AddComponent<ObjectMicroHudTarget>();
            }

            var completed = zone.State == MineZoneState.Completed;
            hudTarget.Configure(
                completed ? "Шахта" : "Стройзона шахты",
                completed ? $"{GetOreTypeName(zone.OreType)} залежь" : $"{GetOreTypeName(zone.OreType)} маршрут укрепляется",
                "Шахта",
                zone.Cave.Center,
                GetOreAccentColor(zone.OreType),
                () => GetZoneHudStatus(zone),
                () => zone.State == MineZoneState.Completed
                    ? $"Шахта ур. {zone.Level} добывает {GetOreResourceName(zone.OreType)} до склада {GetMineBatchCapacity(zone)}, затем отправляет караван в замок. Ресурс попадает в казну только после доставки."
                    : "Шахтёры идут из гильдии в замок за деревом, затем несут его в подземелье и укрепляют маршрут по одной клетке.");
            if (completed)
            {
                hudTarget.ConfigureAction(
                    () => GetMineActionLabel(zone),
                    () => CanUpgradeMine(zone),
                    () => TryUpgradeMine(zone));
            }
        }

        private string GetZoneHudStatus(MineZone zone)
        {
            if (zone == null)
            {
                return "нет данных";
            }

            return zone.State == MineZoneState.Completed
                ? $"{GetOreTypeName(zone.OreType)}, ур. {zone.Level}, склад {zone.StoredAmount}/{GetMineBatchCapacity(zone)}, караваны {zone.ActiveCartCount}"
                : $"строится, укреплено {Mathf.Min(zone.RouteIndex, zone.Route.Count)}/{zone.Route.Count}";
        }

        private string GetMineActionLabel(MineZone zone)
        {
            if (zone == null || zone.Level >= 3)
            {
                return "Шахта улучшена";
            }

            return $"Улучшить до ур. {zone.Level + 1} ({GetMineUpgradeCost(zone).Format()})";
        }

        private bool CanUpgradeMine(MineZone zone)
        {
            return zone != null
                && zone.State == MineZoneState.Completed
                && zone.Level < 3
                && resources != null
                && resources.CanAfford(GetMineUpgradeCost(zone));
        }

        private void TryUpgradeMine(MineZone zone)
        {
            if (zone == null || zone.State != MineZoneState.Completed || zone.Level >= 3)
            {
                return;
            }

            var cost = GetMineUpgradeCost(zone);
            if (resources == null || !resources.TrySpend(cost))
            {
                lastStatus = $"шахта: нужно {cost.Format()}";
                GameDebugLog.Warning("Mine", $"Mine upgrade blocked: cave={GameDebugLog.Position(zone.Cave.Center)}, level={zone.Level}, required={cost.Format()}, gold={resources?.Gold ?? 0}, wood={resources?.Wood ?? 0}, iron={resources?.Iron ?? 0}.");
                return;
            }

            zone.Level++;
            var mineRoot = constructionRenderer.RenderMine(zone.Cave, zone.OreType, zone.Level);
            ConfigureMineHud(zone, mineRoot);
            mazeRenderer.TrackExternalCellRenderer(zone.Cave.Center, constructionRenderer.GetCellRoot(zone.Cave.Center));
            lastStatus = $"{GetOreTypeName(zone.OreType)} шахта улучшена до ур. {zone.Level}";
            ShowFloatingText(zone.Cave.Center, $"Шахта ур. {zone.Level}", GetOreAccentColor(zone.OreType), 3.1f);
            GameAudioController.Play(GameSfx.Build, mazeRenderer.GridToWorld(zone.Cave.Center), 0.9f);
            GameDebugLog.Info("Mine", $"Mine upgraded: cave={GameDebugLog.Position(zone.Cave.Center)}, ore={zone.OreType}, level={zone.Level}, cost={cost.Format()}, goldLeft={resources.Gold}, woodLeft={resources.Wood}, ironLeft={resources.Iron}.");
        }

        private void ProduceMines()
        {
            if (zones.Count == 0)
            {
                return;
            }

            productionProgress += Time.deltaTime;
            var wholeTicks = Mathf.FloorToInt(productionProgress / ResourceProductionController.FarmProductionIntervalSeconds);
            if (wholeTicks <= 0)
            {
                DispatchReadyMineCarts();
                return;
            }

            for (var tick = 0; tick < wholeTicks; tick++)
            {
                for (var i = 0; i < zones.Count; i++)
                {
                    var zone = zones[i];
                    var capacity = GetMineBatchCapacity(zone);
                    if (zone.State != MineZoneState.Completed || zone.StoredAmount >= capacity)
                    {
                        continue;
                    }

                    zone.StoredAmount = Mathf.Min(capacity, zone.StoredAmount + GetMineUnitsPerTick(zone));
                    if (zone.StoredAmount >= capacity)
                    {
                        TryDispatchMineCart(zone);
                    }
                }
            }

            productionProgress -= wholeTicks * ResourceProductionController.FarmProductionIntervalSeconds;
            DispatchReadyMineCarts();
        }

        private void DispatchReadyMineCarts()
        {
            for (var i = 0; i < zones.Count; i++)
            {
                if (zones[i].State == MineZoneState.Completed && zones[i].StoredAmount >= GetMineBatchCapacity(zones[i]))
                {
                    TryDispatchMineCart(zones[i]);
                }
            }
        }

        private bool TryDispatchMineCart(MineZone zone)
        {
            var capacity = GetMineBatchCapacity(zone);
            if (zone.StoredAmount < capacity || constructionRenderer == null)
            {
                return false;
            }

            if (!TryBuildMineCartWorldPath(zone, out var waypoints))
            {
                return false;
            }

            zone.StoredAmount -= capacity;
            zone.ActiveCartCount++;
            var root = constructionRenderer.CreateMineCart(waypoints[0], zone.OreType);
            var cart = new MineCartRuntime(++mineCartSerial, root, waypoints, zone, capacity);
            mineCarts.Add(cart);
            lastStatus = $"{GetOreTypeName(zone.OreType)} караван отправлен из {GameDebugLog.Position(zone.Cave.Center)}";
            GameDebugLog.Info(
                "Mine",
                $"Mine cart #{cart.Id} dispatched: cave={GameDebugLog.Position(zone.Cave.Center)}, ore={zone.OreType}, amount={capacity}, level={zone.Level}, pathWaypoints={waypoints.Count}, fortifiedRouteCells={zone.Route.Count}, from={FormatWorldPosition(waypoints[0])}, to={FormatWorldPosition(waypoints[waypoints.Count - 1])}, activeCarts={zone.ActiveCartCount}.");
            return true;
        }

        private static int GetMineBatchCapacity(MineZone zone)
        {
            return zone != null && zone.Level >= 2 ? UpgradedMineBatchCapacity : BaseMineBatchCapacity;
        }

        private static int GetMineUnitsPerTick(MineZone zone)
        {
            return zone != null && zone.Level >= 3 ? UpgradedMineUnitsPerTick : 1;
        }

        private static BuildingCost GetMineUpgradeCost(MineZone zone)
        {
            if (zone == null || zone.Level >= 3)
            {
                return new BuildingCost(0, 0);
            }

            return zone.Level == 1
                ? new BuildingCost(75, 25, 0, 20)
                : new BuildingCost(130, 45, 0, 45);
        }

        private bool TryBuildMineCartWorldPath(MineZone zone, out List<Vector3> waypoints)
        {
            waypoints = new List<Vector3>();
            if (zone == null || zone.Route == null || zone.Route.Count < 2)
            {
                GameDebugLog.Warning("Mine", "Mine cart dispatch blocked: mine route is empty.");
                return false;
            }

            if (zone.Route[zone.Route.Count - 1] != zone.Cave.Center)
            {
                GameDebugLog.Warning(
                    "Mine",
                    $"Mine cart dispatch blocked: route ends at {GameDebugLog.Position(zone.Route[zone.Route.Count - 1])}, expected mine center {GameDebugLog.Position(zone.Cave.Center)}.");
                return false;
            }

            if (!TryValidateMineCartFortifiedRoute(zone.Route))
            {
                lastStatus = "караван шахты: маршрут не укреплен";
                return false;
            }

            if (!TryBuildCompletedRoadPath(result.EntrancePosition, result.BasePosition, out var roadPath))
            {
                lastStatus = "караван шахты: нет дороги от входа к замку";
                GameDebugLog.Warning(
                    "Mine",
                    $"Mine cart dispatch blocked: no completed road from entrance {GameDebugLog.Position(result.EntrancePosition)} to castle {GameDebugLog.Position(result.BasePosition)}.");
                return false;
            }

            var offset = new Vector3(0f, mazeRenderer.CellSize * CartYOffset, 0f);
            AddWorldPoint(waypoints, zone.Cave.Center, offset);
            for (var i = zone.Route.Count - 2; i >= 0; i--)
            {
                AddWorldPoint(waypoints, zone.Route[i], offset);
            }

            for (var i = 1; i < roadPath.Count; i++)
            {
                waypoints.Add(mazeRenderer.GridToWorld(roadPath[i]) + offset);
            }

            GameDebugLog.Info(
                "Mine",
                $"Mine cart path confirmed: cave={GameDebugLog.Position(zone.Cave.Center)}, mineToEntrance={FormatCellPathPreviewReverse(zone.Route)}, road={FormatCellPathPreview(roadPath)}.");
            return waypoints.Count >= 2;
        }

        private bool TryValidateMineCartFortifiedRoute(IReadOnlyList<Vector2Int> route)
        {
            if (route == null || route.Count < 2 || route[0] != result.EntrancePosition)
            {
                GameDebugLog.Warning("Mine", "Mine cart route rejected: route must start at the labyrinth entrance and contain at least two cells.");
                return false;
            }

            for (var i = 0; i < route.Count; i++)
            {
                var cell = route[i];
                if (!result.Grid.InBounds(cell) || !result.Grid.Get(cell).IsWalkable)
                {
                    GameDebugLog.Warning("Mine", $"Mine cart route rejected: cell {GameDebugLog.Position(cell)} at index {i} is not walkable.");
                    return false;
                }

                if (!fortifiedCells.Contains(cell))
                {
                    GameDebugLog.Warning("Mine", $"Mine cart route rejected: cell {GameDebugLog.Position(cell)} at index {i} is not fortified.");
                    return false;
                }

                if (i > 0 && ManhattanDistance(route[i - 1], cell) != 1)
                {
                    GameDebugLog.Warning("Mine", $"Mine cart route rejected: non-adjacent fortified step {GameDebugLog.Position(route[i - 1])} -> {GameDebugLog.Position(cell)}.");
                    return false;
                }
            }

            return true;
        }

        private void MoveMineCarts()
        {
            if (mineCarts.Count == 0)
            {
                return;
            }

            var speed = mazeRenderer.CellSize * CartSpeedCellsPerSecond * Time.deltaTime;
            for (var i = mineCarts.Count - 1; i >= 0; i--)
            {
                if (!mineCarts[i].Move(speed))
                {
                    continue;
                }

                CompleteMineCartDelivery(mineCarts[i]);
                mineCarts[i].Destroy();
                mineCarts.RemoveAt(i);
            }

            TraceMineCarts();
        }

        private void CompleteMineCartDelivery(MineCartRuntime cart)
        {
            if (cart.Zone.OreType == OreDepositType.Iron)
            {
                resources.AddIron(cart.Amount);
                ShowFloatingText(result.BasePosition, $"+{cart.Amount} железо", GetOreAccentColor(cart.Zone.OreType), 4.2f);
            }
            else
            {
                resources.AddGold(cart.Amount);
                ShowFloatingText(result.BasePosition, $"+{cart.Amount} золото", GetOreAccentColor(cart.Zone.OreType), 4.2f);
            }

            cart.Zone.ActiveCartCount = Mathf.Max(0, cart.Zone.ActiveCartCount - 1);
            GameAudioController.Play(GameSfx.Deposit, mazeRenderer.GridToWorld(result.BasePosition), 0.9f);
            GameDebugLog.Info(
                "Mine",
                $"Mine cart #{cart.Id} delivered: cave={GameDebugLog.Position(cart.Zone.Cave.Center)}, ore={cart.Zone.OreType}, amount={cart.Amount}, position={FormatWorldPosition(cart.CurrentWorldPosition)}, totalIron={resources.Iron}, totalGold={resources.Gold}, activeCarts={cart.Zone.ActiveCartCount}.");
        }

        private void TraceMineCarts()
        {
            if (mineCarts.Count == 0)
            {
                cartTraceTimer = 0f;
                return;
            }

            cartTraceTimer -= Time.deltaTime;
            if (cartTraceTimer > 0f)
            {
                return;
            }

            cartTraceTimer = MineRuntimeTraceIntervalSeconds;
            for (var i = 0; i < mineCarts.Count; i++)
            {
                var cart = mineCarts[i];
                if (cart == null)
                {
                    continue;
                }

                GameDebugLog.Info(
                    "Mine",
                    $"Mine cart #{cart.Id} trace: ore={cart.Zone.OreType}, cave={GameDebugLog.Position(cart.Zone.Cave.Center)}, amount={cart.Amount}, position={FormatWorldPosition(cart.CurrentWorldPosition)}, next={FormatWorldPosition(cart.NextWaypointWorld)}, remainingWaypoints={cart.RemainingWaypoints}, activeCarts={cart.Zone.ActiveCartCount}.");
            }
        }

        private void ShowFloatingText(Vector2Int position, string text, Color color, float height)
        {
            DamageNumberView.CreateText(mazeRenderer, position, text, color, height);
        }

        private string FormatWorldPosition(Vector3 worldPosition)
        {
            var x = worldPosition.x.ToString("0.00", CultureInfo.InvariantCulture);
            var z = worldPosition.z.ToString("0.00", CultureInfo.InvariantCulture);
            return $"{GameDebugLog.Position(WorldToApproxGridCell(worldPosition))}/world({x}, {z})";
        }

        private static string FormatCellPathPreview(IReadOnlyList<Vector2Int> cells)
        {
            if (cells == null || cells.Count == 0)
            {
                return "empty";
            }

            const int edgeCount = 6;
            if (cells.Count <= edgeCount * 2)
            {
                return FormatCellRange(cells, 0, cells.Count);
            }

            return $"{FormatCellRange(cells, 0, edgeCount)} -> ... -> {FormatCellRange(cells, cells.Count - edgeCount, cells.Count)}";
        }

        private static string FormatCellPathPreviewReverse(IReadOnlyList<Vector2Int> cells)
        {
            if (cells == null || cells.Count == 0)
            {
                return "empty";
            }

            const int edgeCount = 6;
            if (cells.Count <= edgeCount * 2)
            {
                return FormatCellRangeReverse(cells, cells.Count - 1, -1);
            }

            return $"{FormatCellRangeReverse(cells, cells.Count - 1, cells.Count - edgeCount - 1)} -> ... -> {FormatCellRangeReverse(cells, edgeCount - 1, -1)}";
        }

        private static string FormatCellRange(IReadOnlyList<Vector2Int> cells, int startInclusive, int endExclusive)
        {
            var text = string.Empty;
            for (var i = startInclusive; i < endExclusive; i++)
            {
                if (i > startInclusive)
                {
                    text += " -> ";
                }

                text += GameDebugLog.Position(cells[i]);
            }

            return text;
        }

        private static string FormatCellRangeReverse(IReadOnlyList<Vector2Int> cells, int startInclusive, int endExclusive)
        {
            var text = string.Empty;
            for (var i = startInclusive; i > endExclusive; i--)
            {
                if (i < startInclusive)
                {
                    text += " -> ";
                }

                text += GameDebugLog.Position(cells[i]);
            }

            return text;
        }

        private Vector2Int WorldToApproxGridCell(Vector3 worldPosition)
        {
            var size = mazeRenderer != null ? mazeRenderer.CellSize : 1f;
            if (size <= 0.001f)
            {
                size = 1f;
            }

            return new Vector2Int(
                Mathf.RoundToInt(worldPosition.x / size),
                Mathf.RoundToInt(worldPosition.z / size));
        }

        private MineZone GetNextActiveZone()
        {
            for (var i = 0; i < zones.Count; i++)
            {
                var zone = zones[i];
                if (zone.State != MineZoneState.Completed)
                {
                    return zone;
                }
            }

            return null;
        }

        private int CountCompletedZones()
        {
            var completed = 0;
            for (var i = 0; i < zones.Count; i++)
            {
                if (zones[i].State == MineZoneState.Completed)
                {
                    completed++;
                }
            }

            return completed;
        }

        private void RebuildSelectableCaves()
        {
            selectableCaves.Clear();
            if (knowledge == null || result == null)
            {
                return;
            }

            for (var i = 0; i < result.Caves.Count; i++)
            {
                var cave = result.Caves[i];
                if (IsCaveAlreadyUsed(cave) || !TryGetCaveOreType(cave, out _) || !IsCaveKnown(cave, knowledge))
                {
                    continue;
                }

                selectableCaves.Add(cave);
            }
        }

        private int CountSelectableCaves(HeroMemory knownMap)
        {
            var count = 0;
            for (var i = 0; i < result.Caves.Count; i++)
            {
                var cave = result.Caves[i];
                if (!IsCaveAlreadyUsed(cave) && TryGetCaveOreType(cave, out _) && IsCaveKnown(cave, knownMap))
                {
                    count++;
                }
            }

            return count;
        }

        private bool TryGetSelectableCave(Vector2Int cell, out CaveInfo cave)
        {
            for (var i = 0; i < selectableCaves.Count; i++)
            {
                if (ContainsCaveCell(selectableCaves[i], cell))
                {
                    cave = selectableCaves[i];
                    return true;
                }
            }

            cave = default;
            return false;
        }

        private bool TryGetAnyCave(Vector2Int cell, out CaveInfo cave)
        {
            if (result == null)
            {
                cave = default;
                return false;
            }

            for (var i = 0; i < result.Caves.Count; i++)
            {
                if (ContainsCaveCell(result.Caves[i], cell))
                {
                    cave = result.Caves[i];
                    return true;
                }
            }

            cave = default;
            return false;
        }

        private bool TryBuildKnownPathToCave(CaveInfo cave, out List<Vector2Int> path)
        {
            path = null;
            if (knowledge == null || result == null || !IsKnownWalkable(cave.Center))
            {
                return false;
            }

            var queue = new Queue<Vector2Int>();
            var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            queue.Enqueue(result.EntrancePosition);
            cameFrom[result.EntrancePosition] = result.EntrancePosition;

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current == cave.Center)
                {
                    path = BuildPath(cameFrom, cave.Center);
                    return true;
                }

                foreach (var neighbor in result.Grid.WalkableNeighbors(current))
                {
                    if (cameFrom.ContainsKey(neighbor) || !IsKnownWalkable(neighbor))
                    {
                        continue;
                    }

                    cameFrom[neighbor] = current;
                    queue.Enqueue(neighbor);
                }
            }

            return false;
        }

        private List<Vector2Int> BuildPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int target)
        {
            var path = new List<Vector2Int>();
            var current = target;
            while (current != result.EntrancePosition)
            {
                path.Add(current);
                current = cameFrom[current];
            }

            path.Add(result.EntrancePosition);
            path.Reverse();
            return path;
        }

        private List<Vector2Int> BuildMineRouteWithCaveFootprint(CaveInfo cave, List<Vector2Int> pathToCenter)
        {
            var route = new List<Vector2Int>(pathToCenter);
            var coverage = new[]
            {
                Vector2Int.zero,
                Vector2Int.left,
                Vector2Int.left + Vector2Int.up,
                Vector2Int.up,
                Vector2Int.right + Vector2Int.up,
                Vector2Int.right,
                Vector2Int.right + Vector2Int.down,
                Vector2Int.down,
                Vector2Int.left + Vector2Int.down,
                Vector2Int.left,
                Vector2Int.zero
            };

            var uniqueCells = new HashSet<Vector2Int>();
            for (var i = 0; i < coverage.Length; i++)
            {
                var cell = cave.Center + coverage[i];
                if (!result.Grid.InBounds(cell) || !result.Grid.Get(cell).IsWalkable)
                {
                    GameDebugLog.Warning("Mine", $"Mine cave footprint cell skipped: {GameDebugLog.Position(cell)} is not walkable.");
                    continue;
                }

                uniqueCells.Add(cell);
                AddRouteStep(route, cell);
            }

            GameDebugLog.Info(
                "Mine",
                $"Mine cave footprint route prepared: cave={GameDebugLog.Position(cave.Center)}, uniqueFootprintCells={uniqueCells.Count}/9, routeLength={route.Count}.");
            return route;
        }

        private static void AddRouteStep(List<Vector2Int> route, Vector2Int cell)
        {
            if (route.Count == 0 || route[route.Count - 1] != cell)
            {
                route.Add(cell);
            }
        }

        private void SkipAlreadyFortifiedRouteCells(MineZone zone)
        {
            AdvanceRouteIndex(zone);
        }

        private void AdvanceRouteIndex(MineZone zone)
        {
            while (zone.RouteIndex < zone.Route.Count && fortifiedCells.Contains(zone.Route[zone.RouteIndex]))
            {
                zone.RouteIndex++;
            }
        }

        private bool IsCaveAlreadyUsed(CaveInfo cave)
        {
            for (var i = 0; i < zones.Count; i++)
            {
                if (zones[i].Cave.Center == cave.Center)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsCaveKnown(CaveInfo cave, HeroMemory knownMap)
        {
            for (var x = cave.Center.x - 1; x <= cave.Center.x + 1; x++)
            {
                for (var y = cave.Center.y - 1; y <= cave.Center.y + 1; y++)
                {
                    var cell = new Vector2Int(x, y);
                    if (!result.Grid.InBounds(cell) || !result.Grid.Get(cell).IsWalkable || !knownMap.IsRemembered(cell))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool IsKnownWalkable(Vector2Int position)
        {
            if (!result.Grid.InBounds(position) || !result.Grid.Get(position).IsWalkable)
            {
                return false;
            }

            return position == result.EntrancePosition || knowledge.IsRemembered(position);
        }

        private bool TryGetCaveOreType(CaveInfo cave, out OreDepositType oreType)
        {
            if (result?.OreDeposits != null)
            {
                for (var i = 0; i < result.OreDeposits.Count; i++)
                {
                    var deposit = result.OreDeposits[i];
                    if (deposit != null && !deposit.IsDepleted && deposit.Cave.Center == cave.Center)
                    {
                        oreType = deposit.Type;
                        return true;
                    }
                }
            }

            oreType = default;
            return false;
        }

        private static string GetOreTypeName(OreDepositType oreType)
        {
            return oreType == OreDepositType.Iron ? "железная" : "золотая";
        }

        private static string GetOreResourceName(OreDepositType oreType)
        {
            return oreType == OreDepositType.Iron ? "железо" : "золото";
        }

        private static Color GetOreAccentColor(OreDepositType oreType)
        {
            return oreType == OreDepositType.Iron
                ? new Color(0.62f, 0.68f, 0.74f)
                : new Color(1f, 0.72f, 0.14f);
        }

        private static bool ContainsCaveCell(CaveInfo cave, Vector2Int cell)
        {
            return Mathf.Abs(cell.x - cave.Center.x) <= 1
                && Mathf.Abs(cell.y - cave.Center.y) <= 1;
        }

        private bool TryPlaceTorch(Vector2Int cell)
        {
            if (torchPositions.Contains(cell) || torchLitCells.Contains(cell) || !TryFindAdjacentWall(cell, out var wallDirection))
            {
                return false;
            }

            torchPositions.Add(cell);
            constructionRenderer.RenderTorch(cell, wallDirection, TorchLightRange);
            mazeRenderer.TrackExternalCellRenderer(cell, constructionRenderer.GetCellRoot(cell));
            return true;
        }

        private void RenderAdjacentWallReinforcements(Vector2Int cell)
        {
            foreach (var direction in MazeDirections.Cardinal)
            {
                var wall = cell + direction;
                if (!result.Grid.InBounds(wall) || result.Grid.Get(wall).Type != MazeCellType.Wall)
                {
                    continue;
                }

                var key = $"{wall.x}:{wall.y}:{direction.x}:{direction.y}";
                if (!reinforcedWallFaces.Add(key))
                {
                    continue;
                }

                constructionRenderer.RenderWallReinforcement(wall, direction);
                mazeRenderer.TrackExternalCellRenderer(wall, constructionRenderer.GetCellRoot(wall));
            }
        }

        private bool TryFindAdjacentWall(Vector2Int cell, out Vector2Int wallDirection)
        {
            foreach (var direction in MazeDirections.Cardinal)
            {
                var neighbor = cell + direction;
                if (result.Grid.InBounds(neighbor) && result.Grid.Get(neighbor).Type == MazeCellType.Wall)
                {
                    wallDirection = direction;
                    return true;
                }
            }

            wallDirection = Vector2Int.zero;
            return false;
        }

        private void RefreshTorchLight()
        {
            torchLitCells.Clear();
            foreach (var cell in torchPositions)
            {
                AddLightFrom(cell);
            }
        }

        private void AddLightFrom(Vector2Int origin)
        {
            for (var x = origin.x - TorchLightRange; x <= origin.x + TorchLightRange; x++)
            {
                for (var y = origin.y - TorchLightRange; y <= origin.y + TorchLightRange; y++)
                {
                    var target = new Vector2Int(x, y);
                    if (!result.Grid.InBounds(target) || ChebyshevDistance(origin, target) > TorchLightRange)
                    {
                        continue;
                    }

                    if (CanSee(result.Grid, origin, target))
                    {
                        torchLitCells.Add(target);
                    }
                }
            }
        }

    }
}
