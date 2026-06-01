using System.Collections.Generic;
using Labyrinth.Maze;
using UnityEngine;

namespace Labyrinth.Core
{
    public sealed partial class MineConstructionController
    {
        private enum MineZoneState
        {
            BuildingRoute,
            BuildingMine,
            Completed
        }

        private enum MineWorkerState
        {
            WalkingToCastle,
            WaitingAtCastle,
            WalkingToTarget,
            Building,
            ReturningToCastle
        }

        private enum MineCartArrival
        {
            None,
            Delivered,
            Returned
        }

        private sealed class MineZone
        {
            public MineZone(CaveInfo cave, List<Vector2Int> route, OreDepositType oreType)
            {
                Cave = cave;
                Route = route;
                OreType = oreType;
                State = MineZoneState.BuildingRoute;
            }

            public CaveInfo Cave { get; }

            public List<Vector2Int> Route { get; }

            public OreDepositType OreType { get; }

            public int RouteIndex { get; set; }

            public MineZoneState State { get; set; }

            public int MineBuildDeliveredWood { get; set; }

            public int StoredAmount { get; set; }

            public int ActiveCartCount { get; set; }

            public int Level { get; set; } = 1;

            public HashSet<Vector2Int> AssignedRouteCells { get; } = new HashSet<Vector2Int>();
        }

        private sealed class MineWorker
        {
            private readonly List<Vector3> path = new List<Vector3>();
            private int pathIndex;

            public MineWorker(
                int id,
                Transform root,
                MineZone zone,
                List<Vector3> worldPath)
            {
                Id = id;
                Root = root;
                Zone = zone;
                State = MineWorkerState.WalkingToCastle;
                SetPath(worldPath);
            }

            public int Id { get; }

            public Transform Root { get; }

            public MineZone Zone { get; private set; }

            public Vector2Int TargetCell { get; private set; }

            public int TargetIndex { get; private set; } = -1;

            public bool BuildsMine { get; private set; }

            public MineWorkerState State { get; private set; }

            public bool IsBuilding => State == MineWorkerState.Building;

            public bool IsMoving => State == MineWorkerState.WalkingToCastle
                || State == MineWorkerState.WalkingToTarget
                || State == MineWorkerState.ReturningToCastle;

            public bool IsWaitingAtCastle => State == MineWorkerState.WaitingAtCastle;

            public bool IsWorkingOnRoute => Zone != null
                && !BuildsMine
                && (State == MineWorkerState.WalkingToTarget || State == MineWorkerState.Building);

            public bool IsWorkingOnMine => Zone != null
                && BuildsMine
                && (State == MineWorkerState.WalkingToTarget || State == MineWorkerState.Building);

            public bool CarryingWood { get; private set; }

            public float BuildSeconds { get; private set; }

            public float BuildRemaining { get; set; }

            public Vector3 CurrentWorldPosition => Root != null ? Root.position : Vector3.zero;

            public Vector3 DestinationWorld => path.Count > 0 ? path[path.Count - 1] : CurrentWorldPosition;

            public Vector3 NextWaypointWorld => pathIndex >= 0 && pathIndex < path.Count
                ? path[pathIndex]
                : DestinationWorld;

            public int PathLength => path.Count;

            public int RemainingWaypoints => Mathf.Max(0, path.Count - pathIndex);

            public void WaitAtCastle()
            {
                State = MineWorkerState.WaitingAtCastle;
                CarryingWood = false;
                BuildsMine = false;
                TargetCell = default;
                TargetIndex = -1;
                path.Clear();
                pathIndex = 0;
            }

            public void AssignTarget(
                MineZone zone,
                Vector2Int targetCell,
                int targetIndex,
                List<Vector3> worldPath,
                float buildSeconds,
                bool buildsMine)
            {
                Zone = zone;
                TargetCell = targetCell;
                TargetIndex = targetIndex;
                BuildSeconds = Mathf.Max(0.1f, buildSeconds);
                BuildsMine = buildsMine;
                CarryingWood = true;
                State = MineWorkerState.WalkingToTarget;
                SetPath(worldPath);
            }

            public void ReturnToCastle(List<Vector3> worldPath)
            {
                CarryingWood = false;
                BuildsMine = false;
                TargetIndex = -1;
                State = MineWorkerState.ReturningToCastle;
                SetPath(worldPath);
            }

            public void BeginBuild()
            {
                State = MineWorkerState.Building;
                BuildRemaining = BuildSeconds;
            }

            public bool Move(float speed)
            {
                if (Root == null || path.Count == 0 || pathIndex >= path.Count)
                {
                    return true;
                }

                var target = path[pathIndex];
                var offset = target - Root.position;
                var stepDistance = offset.magnitude;
                if (stepDistance <= Mathf.Max(speed, 0.001f))
                {
                    Root.position = target;
                    pathIndex++;
                    FaceNextWaypoint();
                    return pathIndex >= path.Count;
                }

                var direction = offset / stepDistance;
                Root.position += direction * speed;
                Root.rotation = Quaternion.Lerp(Root.rotation, Quaternion.LookRotation(direction, Vector3.up), 0.22f);
                return false;
            }

            private void SetPath(List<Vector3> worldPath)
            {
                path.Clear();
                if (worldPath != null)
                {
                    path.AddRange(worldPath);
                }

                pathIndex = path.Count > 1 ? 1 : 0;
                FaceNextWaypoint();
            }

            private void FaceNextWaypoint()
            {
                if (Root == null || pathIndex >= path.Count)
                {
                    return;
                }

                var direction = path[pathIndex] - Root.position;
                if (direction.sqrMagnitude > 0.0001f)
                {
                    Root.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
                }
            }
        }

        private sealed class MineCartRuntime
        {
            private const float ArrivalSqrDistance = 0.0025f;

            private readonly Transform root;
            private readonly List<Vector3> waypoints;
            private readonly List<Vector3> returnWaypoints;
            private readonly Transform cargo;
            private int nextWaypoint = 1;
            private bool returning;

            public MineCartRuntime(int id, Transform cartRoot, List<Vector3> cartWaypoints, MineZone zone, int amount)
            {
                Id = id;
                root = cartRoot;
                waypoints = cartWaypoints;
                returnWaypoints = new List<Vector3>(cartWaypoints);
                returnWaypoints.Reverse();
                cargo = cartRoot != null ? cartRoot.Find("Mine Cart Cargo") : null;
                Zone = zone;
                Amount = amount;
                FaceNextWaypoint();
            }

            public int Id { get; }

            public MineZone Zone { get; }

            public int Amount { get; }

            public bool IsReturning => returning;

            public Vector3 CurrentWorldPosition => root != null ? root.position : Vector3.zero;

            public Vector3 DestinationWorld => waypoints.Count > 0 ? waypoints[waypoints.Count - 1] : CurrentWorldPosition;

            public Vector3 NextWaypointWorld => nextWaypoint < waypoints.Count ? waypoints[nextWaypoint] : DestinationWorld;

            public int RemainingWaypoints => Mathf.Max(0, waypoints.Count - nextWaypoint);

            public MineCartArrival Move(float distance)
            {
                if (root == null || nextWaypoint >= waypoints.Count)
                {
                    return returning ? MineCartArrival.Returned : MineCartArrival.Delivered;
                }

                var remaining = distance;
                while (remaining > 0f && nextWaypoint < waypoints.Count)
                {
                    var target = waypoints[nextWaypoint];
                    var offset = target - root.position;
                    var stepDistance = offset.magnitude;
                    if (stepDistance <= Mathf.Max(remaining, 0.001f))
                    {
                        root.position = target;
                        remaining -= stepDistance;
                        nextWaypoint++;
                        FaceNextWaypoint();
                        continue;
                    }

                    var direction = offset / stepDistance;
                    root.position += direction * remaining;
                    root.rotation = Quaternion.LookRotation(direction, Vector3.up);
                    remaining = 0f;
                }

                if (nextWaypoint < waypoints.Count
                    && (waypoints[waypoints.Count - 1] - root.position).sqrMagnitude > ArrivalSqrDistance)
                {
                    return MineCartArrival.None;
                }

                return returning ? MineCartArrival.Returned : MineCartArrival.Delivered;
            }

            public void BeginReturn()
            {
                if (returning)
                {
                    return;
                }

                returning = true;
                waypoints.Clear();
                waypoints.AddRange(returnWaypoints);
                nextWaypoint = waypoints.Count > 1 ? 1 : 0;
                if (cargo != null)
                {
                    cargo.gameObject.SetActive(false);
                }

                FaceNextWaypoint();
            }

            public void Destroy()
            {
                if (root != null)
                {
                    Object.Destroy(root.gameObject);
                }
            }

            private void FaceNextWaypoint()
            {
                if (root == null || nextWaypoint >= waypoints.Count)
                {
                    return;
                }

                var direction = waypoints[nextWaypoint] - root.position;
                if (direction.sqrMagnitude > 0.0001f)
                {
                    root.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
                }
            }
        }
    }
}
