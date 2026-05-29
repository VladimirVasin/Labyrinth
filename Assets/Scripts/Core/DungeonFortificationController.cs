using System.Collections.Generic;
using Labyrinth.Hero;
using Labyrinth.Maze;
using UnityEngine;

namespace Labyrinth.Core
{
    public sealed class DungeonFortificationController : MonoBehaviour
    {
        public const int FloorWoodCost = 1;
        public const int TorchWoodCost = 5;
        public const int TorchLightRange = 2;
        private const int TorchSpacingCells = 5;
        private const float WorkerSpeedCellsPerSecond = 2.1f;
        private const float NextTaskDelay = 0.45f;

        private readonly HashSet<Vector2Int> fortifiedCells = new HashSet<Vector2Int>();
        private readonly Queue<Vector2Int> queuedCells = new Queue<Vector2Int>();
        private readonly HashSet<Vector2Int> queuedCellSet = new HashSet<Vector2Int>();
        private readonly HashSet<Vector2Int> torchPositions = new HashSet<Vector2Int>();
        private readonly HashSet<Vector2Int> torchLitCells = new HashSet<Vector2Int>();

        private ResourceWallet resources;
        private MazeRenderer mazeRenderer;
        private MazeGenerationResult result;
        private HeroMemory memory;
        private DungeonFortificationRenderer fortificationRenderer;
        private FortificationWorker activeWorker;
        private float taskCooldown;
        private bool selectionModeActive;
        private string lastStatus = "ожидает выбора";

        public bool SelectionModeActive => selectionModeActive;

        public bool IsCellFortified(Vector2Int cell)
        {
            return fortifiedCells.Contains(cell);
        }

        public string StatusText
        {
            get
            {
                var mode = selectionModeActive ? "разметка" : "готово";
                return $"{mode}: очередь {queuedCellSet.Count}, укреплено {fortifiedCells.Count}, факелов {torchPositions.Count}, {lastStatus}";
            }
        }

        public void Configure(ResourceWallet wallet, MazeRenderer renderer)
        {
            resources = wallet;
            mazeRenderer = renderer;
        }

        public void Initialize(MazeGenerationResult generationResult, HeroMemory sharedMemory)
        {
            Clear();
            result = generationResult;
            memory = sharedMemory;
            if (result == null || result.Grid == null || mazeRenderer == null)
            {
                return;
            }

            fortificationRenderer = DungeonFortificationRenderer.Create(transform, mazeRenderer);
            lastStatus = "выберите клетку";
        }

        public void Clear()
        {
            fortifiedCells.Clear();
            queuedCells.Clear();
            queuedCellSet.Clear();
            torchPositions.Clear();
            torchLitCells.Clear();
            selectionModeActive = false;
            taskCooldown = 0f;
            lastStatus = "ожидает выбора";
            activeWorker = null;
            if (fortificationRenderer != null)
            {
                fortificationRenderer.Clear();
                fortificationRenderer = null;
            }

            result = null;
            memory = null;
        }

        public void BeginSelectionMode()
        {
            selectionModeActive = true;
            lastStatus = "кликните разведанную клетку";
        }

        public void CancelSelectionMode()
        {
            selectionModeActive = false;
            fortificationRenderer?.HideHoverMarker();
            lastStatus = queuedCellSet.Count > 0 ? "очередь выполняется" : "ожидает выбора";
            GameDebugLog.Info("Base", "Dungeon fortification selection mode cancelled.");
        }

        public void UpdateHoverCell(Vector2Int cell)
        {
            if (!selectionModeActive || result == null || fortificationRenderer == null)
            {
                return;
            }

            if (!result.Grid.InBounds(cell))
            {
                fortificationRenderer.HideHoverMarker();
                return;
            }

            fortificationRenderer.ShowHoverMarker(cell, CanQueueCell(cell));
        }

        public void ClearHoverCell()
        {
            fortificationRenderer?.HideHoverMarker();
        }

        public bool TryQueueCell(Vector2Int cell)
        {
            if (!selectionModeActive || result == null || fortificationRenderer == null)
            {
                return false;
            }

            if (!result.Grid.InBounds(cell))
            {
                lastStatus = "клетка вне подземелья";
                return false;
            }

            if (!IsKnownCell(cell))
            {
                lastStatus = "клетка еще не разведана";
                return false;
            }

            if (!result.Grid.Get(cell).IsWalkable)
            {
                lastStatus = "можно укреплять только проход";
                return false;
            }

            if (fortifiedCells.Contains(cell))
            {
                lastStatus = "клетка уже укреплена";
                return false;
            }

            if (!queuedCellSet.Add(cell))
            {
                lastStatus = "клетка уже в очереди";
                return false;
            }

            queuedCells.Enqueue(cell);
            fortificationRenderer.RenderQueuedMarker(cell);
            mazeRenderer.TrackExternalCellRenderer(cell, fortificationRenderer.GetCellRoot(cell));
            lastStatus = $"добавлено {GameDebugLog.Position(cell)}";
            GameDebugLog.Info("Base", $"Dungeon fortification cell queued: {GameDebugLog.Position(cell)}, queue={queuedCellSet.Count}.");
            return true;
        }

        private bool CanQueueCell(Vector2Int cell)
        {
            return result.Grid.InBounds(cell)
                && IsKnownCell(cell)
                && result.Grid.Get(cell).IsWalkable
                && !fortifiedCells.Contains(cell)
                && !queuedCellSet.Contains(cell);
        }

        public void UpdateFortification()
        {
            if (result == null || fortificationRenderer == null)
            {
                return;
            }

            UpdateWorker();
            if (activeWorker != null)
            {
                return;
            }

            if (taskCooldown > 0f)
            {
                taskCooldown -= Time.deltaTime;
                return;
            }

            TryDispatchWorker();
        }

        public void AddTorchLitCells(HashSet<Vector2Int> visibleCells)
        {
            if (visibleCells == null)
            {
                return;
            }

            foreach (var position in torchLitCells)
            {
                visibleCells.Add(position);
            }
        }

        private void UpdateWorker()
        {
            if (activeWorker == null)
            {
                return;
            }

            var speed = mazeRenderer.CellSize * WorkerSpeedCellsPerSecond * Time.deltaTime;
            if (!activeWorker.Move(speed))
            {
                return;
            }

            if (!activeWorker.Returning)
            {
                CompleteOutboundWork();
                return;
            }

            fortificationRenderer.DestroyWorker(activeWorker.Root);
            activeWorker = null;
            taskCooldown = NextTaskDelay;
        }

        private void TryDispatchWorker()
        {
            if (!TryGetNextQueuedTarget(out var target))
            {
                if (!selectionModeActive)
                {
                    lastStatus = "очередь пуста";
                }

                return;
            }

            if (resources == null || resources.Wood < FloorWoodCost)
            {
                lastStatus = $"пауза: нужно {FloorWoodCost} дер.";
                return;
            }

            if (!TryBuildKnownPathToTarget(target, out var path))
            {
                lastStatus = $"нет маршрута к {GameDebugLog.Position(target)}";
                return;
            }

            queuedCells.Dequeue();
            queuedCellSet.Remove(target);
            fortificationRenderer.ClearQueuedMarker(target);

            var worldPath = BuildWorkerWorldPath(path);
            var workerRoot = fortificationRenderer.CreateWorker(worldPath[0]);
            activeWorker = new FortificationWorker(workerRoot, target, worldPath);
            lastStatus = $"рабочий идет к {GameDebugLog.Position(target)}";
            GameDebugLog.Info("Base", $"Dungeon fortification worker sent to {GameDebugLog.Position(target)}.");
        }

        private bool TryGetNextQueuedTarget(out Vector2Int target)
        {
            while (queuedCells.Count > 0)
            {
                target = queuedCells.Peek();
                if (queuedCellSet.Contains(target) && !fortifiedCells.Contains(target))
                {
                    return true;
                }

                queuedCells.Dequeue();
                queuedCellSet.Remove(target);
                fortificationRenderer?.ClearQueuedMarker(target);
            }

            target = default;
            return false;
        }

        private void CompleteOutboundWork()
        {
            var target = activeWorker.TargetCell;
            if (!resources.TrySpendWood(FloorWoodCost))
            {
                RequeueTarget(target);
                lastStatus = $"пауза: нужно {FloorWoodCost} дер.";
                StartWorkerReturn();
                return;
            }

            fortifiedCells.Add(target);
            fortificationRenderer.RenderFortifiedFloor(target);
            mazeRenderer.TrackExternalCellRenderer(target, fortificationRenderer.GetCellRoot(target));
            var placedTorch = TryPlaceTorch(target);
            RefreshTorchLight();
            if (lastStatus.StartsWith("факел"))
            {
                lastStatus += $", укреплена {GameDebugLog.Position(target)}";
            }
            else
            {
                lastStatus = placedTorch
                    ? $"укреплена клетка, факел {GameDebugLog.Position(target)}"
                    : $"укреплена клетка {GameDebugLog.Position(target)}";
            }

            GameAudioController.Play(placedTorch ? GameSfx.TorchPlaced : GameSfx.Fortify, mazeRenderer.GridToWorld(target), 0.85f);
            GameDebugLog.Info(
                "Base",
                $"Dungeon cell fortified at {GameDebugLog.Position(target)}. torch={placedTorch}, woodLeft={resources.Wood}, fortified={fortifiedCells.Count}, torches={torchPositions.Count}.");
            StartWorkerReturn();
        }

        private void RequeueTarget(Vector2Int target)
        {
            if (fortifiedCells.Contains(target) || queuedCellSet.Contains(target))
            {
                return;
            }

            queuedCellSet.Add(target);
            queuedCells.Enqueue(target);
            fortificationRenderer.RenderQueuedMarker(target);
        }

        private bool TryPlaceTorch(Vector2Int cell)
        {
            if (!ShouldPlaceTorch(cell, out var wallDirection))
            {
                return false;
            }

            if (!resources.TrySpendWood(TorchWoodCost))
            {
                lastStatus = $"факел отложен: нужно {TorchWoodCost} дер.";
                return false;
            }

            torchPositions.Add(cell);
            fortificationRenderer.RenderTorch(cell, wallDirection, TorchLightRange);
            mazeRenderer.TrackExternalCellRenderer(cell, fortificationRenderer.GetCellRoot(cell));
            return true;
        }

        private bool ShouldPlaceTorch(Vector2Int cell, out Vector2Int wallDirection)
        {
            wallDirection = Vector2Int.zero;
            if (!TryFindAdjacentWall(cell, out wallDirection))
            {
                return false;
            }

            var minimumDistance = torchPositions.Count == 0 ? 3 : TorchSpacingCells;
            foreach (var torch in torchPositions)
            {
                if (GridDistance(torch, cell) < minimumDistance)
                {
                    return false;
                }
            }

            return torchPositions.Count > 0 || GridDistance(result.EntrancePosition, cell) >= minimumDistance;
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

        private void StartWorkerReturn()
        {
            activeWorker.StartReturn();
        }

        private bool TryBuildKnownPathToTarget(Vector2Int target, out List<Vector2Int> path)
        {
            path = null;
            if (memory == null || result == null || !IsKnownCell(target))
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
                if (current == target)
                {
                    path = BuildPath(cameFrom, target);
                    return true;
                }

                foreach (var neighbor in result.Grid.WalkableNeighbors(current))
                {
                    if (cameFrom.ContainsKey(neighbor) || !IsKnownCell(neighbor))
                    {
                        continue;
                    }

                    cameFrom[neighbor] = current;
                    queue.Enqueue(neighbor);
                }
            }

            return false;
        }

        private bool IsKnownCell(Vector2Int position)
        {
            return position == result.EntrancePosition || memory.IsRemembered(position);
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

        private List<Vector3> BuildWorkerWorldPath(IReadOnlyList<Vector2Int> cellPath)
        {
            var cells = new List<Vector2Int>();
            cells.Add(result.BasePosition);
            if (cellPath != null)
            {
                for (var i = 0; i < cellPath.Count; i++)
                {
                    cells.Add(cellPath[i]);
                }
            }

            return SubCellPathBuilder.Build(
                mazeRenderer,
                cells,
                mazeRenderer.CellSize * 0.08f,
                SubCellPathBuilder.BuildSeed(cells, 0x37dd),
                SubCellPathProfile.Worker);
        }

        private void RefreshTorchLight()
        {
            torchLitCells.Clear();
            foreach (var torch in torchPositions)
            {
                AddTorchLight(torch);
            }
        }

        private void AddTorchLight(Vector2Int origin)
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

        private static bool CanSee(MazeGrid grid, Vector2Int origin, Vector2Int target)
        {
            if (origin == target)
            {
                return true;
            }

            var current = origin;
            var dx = Mathf.Abs(target.x - origin.x);
            var dy = Mathf.Abs(target.y - origin.y);
            var stepX = origin.x < target.x ? 1 : -1;
            var stepY = origin.y < target.y ? 1 : -1;
            var error = dx - dy;

            while (current != target)
            {
                var previous = current;
                var doubledError = error * 2;
                if (doubledError > -dy)
                {
                    error -= dy;
                    current.x += stepX;
                }

                if (doubledError < dx)
                {
                    error += dx;
                    current.y += stepY;
                }

                if (IsBlockedByCorner(grid, previous, current, target))
                {
                    return false;
                }

                if (current == target)
                {
                    return true;
                }

                if (IsBlockingWall(grid, current))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsBlockedByCorner(MazeGrid grid, Vector2Int previous, Vector2Int current, Vector2Int target)
        {
            if (previous.x == current.x || previous.y == current.y)
            {
                return false;
            }

            if (current == target && IsBlockingWall(grid, target))
            {
                return false;
            }

            return IsBlockingWall(grid, new Vector2Int(current.x, previous.y))
                && IsBlockingWall(grid, new Vector2Int(previous.x, current.y));
        }

        private static bool IsBlockingWall(MazeGrid grid, Vector2Int position)
        {
            return grid.InBounds(position) && grid.Get(position).Type == MazeCellType.Wall;
        }

        private static int ChebyshevDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
        }

        private static int GridDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        private sealed class FortificationWorker
        {
            private readonly List<Vector3> path;
            private int pathIndex;

            public FortificationWorker(Transform root, Vector2Int targetCell, List<Vector3> worldPath)
            {
                Root = root;
                TargetCell = targetCell;
                path = worldPath;
                pathIndex = Mathf.Min(1, path.Count - 1);
            }

            public Transform Root { get; }

            public Vector2Int TargetCell { get; }

            public bool Returning { get; private set; }

            public bool Move(float speed)
            {
                if (Root == null || path.Count == 0 || pathIndex >= path.Count)
                {
                    return true;
                }

                var target = path[pathIndex];
                Root.position = Vector3.MoveTowards(Root.position, target, speed);
                var direction = target - Root.position;
                if (direction.sqrMagnitude > 0.001f)
                {
                    Root.rotation = Quaternion.Lerp(Root.rotation, Quaternion.LookRotation(direction.normalized, Vector3.up), 0.2f);
                }

                if ((Root.position - target).sqrMagnitude > 0.001f)
                {
                    return false;
                }

                pathIndex++;
                return pathIndex >= path.Count;
            }

            public void StartReturn()
            {
                path.Reverse();
                pathIndex = Mathf.Min(1, path.Count - 1);
                Returning = true;
            }
        }
    }
}
