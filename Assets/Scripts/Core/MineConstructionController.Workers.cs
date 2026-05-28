using UnityEngine;

namespace Labyrinth.Core
{
    public sealed partial class MineConstructionController
    {
        private void UpdateMineWorkers()
        {
            if (workerSpawnCooldown > 0f)
            {
                workerSpawnCooldown -= Time.deltaTime;
            }

            var speed = mazeRenderer.CellSize * WorkerSpeedCellsPerSecond * Time.deltaTime;
            var i = activeWorkers.Count - 1;
            while (i >= 0)
            {
                if (i >= activeWorkers.Count)
                {
                    i = activeWorkers.Count - 1;
                    continue;
                }

                var worker = activeWorkers[i];
                if (worker == null || worker.Root == null)
                {
                    ReleaseRouteAssignment(worker);
                    activeWorkers.RemoveAt(i);
                    i = Mathf.Min(i - 1, activeWorkers.Count - 1);
                    continue;
                }

                if (worker.IsBuilding)
                {
                    UpdateWorkerBuild(worker, i);
                    i = Mathf.Min(i - 1, activeWorkers.Count - 1);
                    continue;
                }

                if (!worker.IsMoving || !worker.Move(speed))
                {
                    i--;
                    continue;
                }

                CompleteWorkerMove(worker);
                i = Mathf.Min(i - 1, activeWorkers.Count - 1);
            }

            TraceMineWorkers();
        }

        private void UpdateRouteConstruction(MineZone zone)
        {
            if (zone == null)
            {
                return;
            }

            SkipAlreadyFortifiedRouteCells(zone);
            if (zone.RouteIndex >= zone.Route.Count)
            {
                zone.State = MineZoneState.BuildingMine;
                lastStatus = "маршрут укреплён, шахта ждёт строительство";
                GameDebugLog.Info("Mine", $"Mine route completed. cave={GameDebugLog.Position(zone.Cave.Center)}, routeLength={zone.Route.Count}.");
                return;
            }

            TrySpawnMineWorker(zone);
            AssignWaitingWorkersToRoute(zone);
        }

        private void UpdateMineBuild(MineZone zone)
        {
            if (zone == null || zone.State != MineZoneState.BuildingMine)
            {
                return;
            }

            TrySpawnMineWorker(zone);
            if (!HasActiveMineBuildWorker(zone))
            {
                TryAssignWaitingWorkerToMine(zone);
            }
        }

        private void UpdateWorkerBuild(MineWorker worker, int workerIndex)
        {
            worker.BuildRemaining = Mathf.Max(0f, worker.BuildRemaining - Time.deltaTime);
            constructionRenderer.AnimateWorkerBuild(
                worker.Root,
                1f - Mathf.Clamp01(worker.BuildRemaining / worker.BuildSeconds));
            if (worker.BuildRemaining > 0f)
            {
                return;
            }

            var zone = worker.Zone;
            if (worker.BuildsMine)
            {
                GameDebugLog.Info(
                    "Mine",
                    $"Mine worker #{worker.Id} finished mine build: cave={GameDebugLog.Position(zone.Cave.Center)}, position={FormatWorldPosition(worker.CurrentWorldPosition)}, buildSeconds={FormatSeconds(worker.BuildSeconds)}.");
                CompleteMine(zone);
                DismissWorkersForZone(zone);
                return;
            }

            var targetIndex = worker.TargetIndex;
            GameDebugLog.Info(
                "Mine",
                $"Mine worker #{worker.Id} finished route cell: target={GameDebugLog.Position(worker.TargetCell)}, index={targetIndex}/{zone.Route.Count}, position={FormatWorldPosition(worker.CurrentWorldPosition)}, buildSeconds={FormatSeconds(worker.BuildSeconds)}.");
            CompleteRouteCell(zone, worker.TargetCell);
            if (TryBuildRouteTargetToCastleWorldPath(zone.Route, targetIndex, out var returnPath))
            {
                worker.ReturnToCastle(returnPath);
                constructionRenderer.SetWorkerCarryingWood(worker.Root, false);
                GameDebugLog.Info(
                    "Mine",
                    $"Mine worker #{worker.Id} returning to castle: from={FormatWorldPosition(worker.CurrentWorldPosition)}, target={GameDebugLog.Position(worker.TargetCell)}, returnWaypoints={returnPath.Count}, destination={FormatWorldPosition(worker.DestinationWorld)}.");
                return;
            }

            GameDebugLog.Warning(
                "Mine",
                $"Mine worker #{worker.Id} removed after fortifying {GameDebugLog.Position(worker.TargetCell)}: no valid return path to castle, position={FormatWorldPosition(worker.CurrentWorldPosition)}.");
            ReleaseRouteAssignment(worker);
            constructionRenderer.DestroyWorker(worker.Root);
            activeWorkers.RemoveAt(workerIndex);
        }

        private void CompleteWorkerMove(MineWorker worker)
        {
            if (worker.State == MineWorkerState.WalkingToTarget)
            {
                constructionRenderer.SetWorkerCarryingWood(worker.Root, false);
                worker.BeginBuild();
                lastStatus = worker.BuildsMine
                    ? $"шахтёр строит шахту {GameDebugLog.Position(worker.TargetCell)}"
                    : $"шахтёр укрепляет {GameDebugLog.Position(worker.TargetCell)}";
                GameDebugLog.Info(
                    "Mine",
                    $"Mine worker #{worker.Id} reached target and started build: target={GameDebugLog.Position(worker.TargetCell)}, buildsMine={worker.BuildsMine}, position={FormatWorldPosition(worker.CurrentWorldPosition)}, buildSeconds={FormatSeconds(worker.BuildSeconds)}.");
                return;
            }

            worker.WaitAtCastle();
            constructionRenderer.SetWorkerCarryingWood(worker.Root, false);
            lastStatus = "шахтёр ждёт дерево в замке";
            GameDebugLog.Info(
                "Mine",
                $"Mine worker #{worker.Id} reached castle and is waiting: position={FormatWorldPosition(worker.CurrentWorldPosition)}, active={activeWorkers.Count}/{MaxActiveMineWorkers}, wood={resources?.Wood ?? 0}.");
        }

        private void TrySpawnMineWorker(MineZone zone)
        {
            if (zone == null
                || resources == null
                || resources.Wood <= 0
                || activeWorkers.Count >= MaxActiveMineWorkers
                || workerSpawnCooldown > 0f)
            {
                return;
            }

            if (!TryBuildGuildToCastleWorldPath(out var path))
            {
                lastStatus = "шахтёрам нужна готовая дорога от гильдии до замка";
                return;
            }

            var spawnOffset = new Vector3(0f, mazeRenderer.CellSize * WorkerYOffset, 0f);
            var spawnPosition = mazeRenderer.GridToWorld(baseDevelopment.MinersGuildPosition) + spawnOffset;
            var root = constructionRenderer.CreateWorker(spawnPosition, false);
            var worker = new MineWorker(++mineWorkerSerial, root, zone, path);
            activeWorkers.Add(worker);
            workerSpawnCooldown = WorkerSpawnIntervalSeconds;
            lastStatus = $"шахтёр вышел из гильдии ({activeWorkers.Count}/{MaxActiveMineWorkers})";
            GameDebugLog.Info(
                "Mine",
                $"Mine worker #{worker.Id} spawned: guild={GameDebugLog.Position(baseDevelopment.MinersGuildPosition)}, castle={GameDebugLog.Position(result.BasePosition)}, start={FormatWorldPosition(worker.CurrentWorldPosition)}, destination={FormatWorldPosition(worker.DestinationWorld)}, pathWaypoints={worker.PathLength}, active={activeWorkers.Count}/{MaxActiveMineWorkers}, wood={resources.Wood}.");
        }

        private bool TryAssignWaitingWorkerToRoute(MineZone zone)
        {
            var worker = GetWaitingWorker(zone);
            if (worker == null)
            {
                return false;
            }

            if (!TryGetNextRouteTarget(zone, out var targetIndex, out var target))
            {
                return false;
            }

            if (!TryBuildCastleToRouteTargetWorldPath(zone.Route, targetIndex, true, out var path))
            {
                lastStatus = "маршрут шахтёра заблокирован";
                return false;
            }

            if (!resources.TrySpendWood(RouteWoodCost))
            {
                lastStatus = $"нужно {RouteWoodCost} дерева на клетку укрепления";
                return false;
            }

            zone.AssignedRouteCells.Add(target);
            worker.AssignTarget(zone, target, targetIndex, path, CellBuildSeconds, false);
            constructionRenderer.SetWorkerCarryingWood(worker.Root, true);
            lastStatus = $"шахтёр взял дерево и идёт к {GameDebugLog.Position(target)}";
            GameDebugLog.Info(
                "Mine",
                $"Mine worker #{worker.Id} assigned route cell: target={GameDebugLog.Position(target)}, index={targetIndex}/{zone.Route.Count}, from={FormatWorldPosition(worker.CurrentWorldPosition)}, destination={FormatWorldPosition(worker.DestinationWorld)}, pathWaypoints={worker.PathLength}, woodLeft={resources.Wood}.");
            return true;
        }

        private void AssignWaitingWorkersToRoute(MineZone zone)
        {
            var assigned = 0;
            while (TryAssignWaitingWorkerToRoute(zone))
            {
                assigned++;
            }

            if (assigned <= 0)
            {
                return;
            }

            GameDebugLog.Info(
                "Mine",
                $"Mine route parallel assignment pass: assigned={assigned}, activeWorkers={activeWorkers.Count}/{MaxActiveMineWorkers}, routeIndex={zone.RouteIndex}/{zone.Route.Count}, inProgress={zone.AssignedRouteCells.Count}.");
        }

        private bool TryAssignWaitingWorkerToMine(MineZone zone)
        {
            var worker = GetWaitingWorker(zone);
            if (worker == null)
            {
                return false;
            }

            if (!TryBuildCastleToMineWorldPath(zone.Route, out var path))
            {
                lastStatus = "шахта ждёт укреплённый маршрут";
                return false;
            }

            if (!zone.MineBuildPaid)
            {
                if (!resources.TrySpendWood(MineWoodCost))
                {
                    lastStatus = $"нужно {MineWoodCost} дерева на шахту";
                    return false;
                }

                zone.MineBuildPaid = true;
            }

            var targetIndex = Mathf.Max(0, zone.Route.Count - 1);
            worker.AssignTarget(zone, zone.Cave.Center, targetIndex, path, MineBuildSeconds, true);
            constructionRenderer.SetWorkerCarryingWood(worker.Root, true);
            lastStatus = $"шахтёр несёт материалы к шахте {GameDebugLog.Position(zone.Cave.Center)}";
            GameDebugLog.Info(
                "Mine",
                $"Mine worker #{worker.Id} assigned mine build: cave={GameDebugLog.Position(zone.Cave.Center)}, from={FormatWorldPosition(worker.CurrentWorldPosition)}, destination={FormatWorldPosition(worker.DestinationWorld)}, pathWaypoints={worker.PathLength}, costWood={MineWoodCost}, woodLeft={resources.Wood}.");
            return true;
        }

        private MineWorker GetWaitingWorker(MineZone zone)
        {
            for (var i = 0; i < activeWorkers.Count; i++)
            {
                var worker = activeWorkers[i];
                if (worker != null && worker.Zone == zone && worker.IsWaitingAtCastle)
                {
                    return worker;
                }
            }

            return null;
        }

        private bool HasActiveRouteWorker(MineZone zone)
        {
            for (var i = 0; i < activeWorkers.Count; i++)
            {
                if (activeWorkers[i] != null && activeWorkers[i].Zone == zone && activeWorkers[i].IsWorkingOnRoute)
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryGetNextRouteTarget(MineZone zone, out int targetIndex, out Vector2Int target)
        {
            AdvanceRouteIndex(zone);
            for (var i = zone.RouteIndex; i < zone.Route.Count; i++)
            {
                var cell = zone.Route[i];
                if (fortifiedCells.Contains(cell) || zone.AssignedRouteCells.Contains(cell))
                {
                    continue;
                }

                targetIndex = i;
                target = cell;
                return true;
            }

            targetIndex = -1;
            target = default;
            return false;
        }

        private void ReleaseRouteAssignment(MineWorker worker)
        {
            if (worker == null || worker.Zone == null || worker.BuildsMine)
            {
                return;
            }

            worker.Zone.AssignedRouteCells.Remove(worker.TargetCell);
        }

        private bool HasActiveMineBuildWorker(MineZone zone)
        {
            for (var i = 0; i < activeWorkers.Count; i++)
            {
                if (activeWorkers[i] != null && activeWorkers[i].Zone == zone && activeWorkers[i].IsWorkingOnMine)
                {
                    return true;
                }
            }

            return false;
        }

        private void DismissWorkersForZone(MineZone zone)
        {
            for (var i = activeWorkers.Count - 1; i >= 0; i--)
            {
                if (activeWorkers[i] == null || activeWorkers[i].Zone != zone)
                {
                    continue;
                }

                GameDebugLog.Info(
                    "Mine",
                    $"Mine worker #{activeWorkers[i].Id} dismissed for completed zone: cave={GameDebugLog.Position(zone.Cave.Center)}, state={activeWorkers[i].State}, position={FormatWorldPosition(activeWorkers[i].CurrentWorldPosition)}.");
                constructionRenderer.DestroyWorker(activeWorkers[i].Root);
                activeWorkers.RemoveAt(i);
            }
        }

        private void TraceMineWorkers()
        {
            if (activeWorkers.Count == 0)
            {
                workerTraceTimer = 0f;
                return;
            }

            workerTraceTimer -= Time.deltaTime;
            if (workerTraceTimer > 0f)
            {
                return;
            }

            workerTraceTimer = MineRuntimeTraceIntervalSeconds;
            for (var i = 0; i < activeWorkers.Count; i++)
            {
                var worker = activeWorkers[i];
                if (worker == null)
                {
                    continue;
                }

                GameDebugLog.Info(
                    "Mine",
                    $"Mine worker #{worker.Id} trace: state={worker.State}, carryingWood={worker.CarryingWood}, buildsMine={worker.BuildsMine}, cave={GameDebugLog.Position(worker.Zone.Cave.Center)}, target={GameDebugLog.Position(worker.TargetCell)}, targetIndex={worker.TargetIndex}, position={FormatWorldPosition(worker.CurrentWorldPosition)}, next={FormatWorldPosition(worker.NextWaypointWorld)}, remainingWaypoints={worker.RemainingWaypoints}, buildRemaining={FormatSeconds(worker.BuildRemaining)}.");
            }
        }
    }
}
