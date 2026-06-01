using System.Collections.Generic;
using Labyrinth.Base;
using UnityEngine;

namespace Labyrinth.Core
{
    public sealed partial class BaseAmbienceController
    {
        private enum CartArrival
        {
            None,
            Delivered,
            Returned
        }

        private sealed class RoadConnection
        {
            public RoadConnection(BuildingType type, Vector2Int buildingPosition, List<Vector2Int> path)
            {
                Type = type;
                BuildingPosition = buildingPosition;
                Path = path;
            }

            public BuildingType Type { get; }

            public Vector2Int BuildingPosition { get; }

            public List<Vector2Int> Path { get; }

            public List<GameObject> Segments { get; } = new List<GameObject>();

            public int BuiltSegments { get; set; }

            public RoadWorkerRuntime Worker { get; set; }

            public bool IsComplete => BuiltSegments >= Path.Count - 1;

            public void DestroySegments()
            {
                foreach (var segment in Segments)
                {
                    if (segment != null)
                    {
                        Object.Destroy(segment);
                    }
                }

                Segments.Clear();
                if (Worker != null)
                {
                    Worker.Destroy();
                    Worker = null;
                }
            }
        }

        private sealed class RoadWorkerRuntime
        {
            private const float ArrivalSqrDistance = 0.0025f;

            private readonly Transform root;
            private readonly Transform tool;
            private readonly Quaternion toolBaseRotation;
            private int activeSegmentIndex = -1;
            private bool building;
            private float buildProgress;

            public RoadWorkerRuntime(Transform root, Transform tool)
            {
                this.root = root;
                this.tool = tool;
                toolBaseRotation = tool != null ? tool.localRotation : Quaternion.identity;
            }

            public bool Update(int segmentIndex, Vector3 target, float moveDistance, float deltaTime, float buildSeconds)
            {
                if (root == null)
                {
                    return true;
                }

                if (activeSegmentIndex != segmentIndex)
                {
                    activeSegmentIndex = segmentIndex;
                    building = false;
                    buildProgress = 0f;
                    FaceTarget(target);
                }

                if (!building)
                {
                    MoveTo(target, moveDistance);
                    if ((target - root.position).sqrMagnitude > ArrivalSqrDistance)
                    {
                        return false;
                    }

                    root.position = target;
                    building = true;
                    buildProgress = 0f;
                }

                buildProgress = Mathf.Clamp01(buildProgress + deltaTime / Mathf.Max(0.1f, buildSeconds));
                AnimateBuild(buildProgress);
                if (buildProgress < 1f)
                {
                    return false;
                }

                activeSegmentIndex = -1;
                building = false;
                buildProgress = 0f;
                AnimateBuild(0f);
                return true;
            }

            public void Destroy()
            {
                if (root != null)
                {
                    Object.Destroy(root.gameObject);
                }
            }

            private void MoveTo(Vector3 target, float moveDistance)
            {
                var offset = target - root.position;
                var distance = offset.magnitude;
                if (distance <= Mathf.Max(moveDistance, 0.001f))
                {
                    root.position = target;
                    return;
                }

                var direction = offset / distance;
                root.position += direction * moveDistance;
                root.rotation = Quaternion.Lerp(root.rotation, Quaternion.LookRotation(direction, Vector3.up), 0.25f);
            }

            private void FaceTarget(Vector3 target)
            {
                if (root == null)
                {
                    return;
                }

                var direction = target - root.position;
                if (direction.sqrMagnitude > 0.0001f)
                {
                    root.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
                }
            }

            private void AnimateBuild(float progress)
            {
                if (tool == null)
                {
                    return;
                }

                var swing = Mathf.Sin(progress * Mathf.PI * 6f) * 34f;
                tool.localRotation = toolBaseRotation * Quaternion.Euler(swing, 0f, -Mathf.Abs(swing) * 0.45f);
            }
        }

        private readonly struct AmbientBuilding
        {
            public AmbientBuilding(BuildingType type, Vector2Int position, int footprintRadius)
            {
                Type = type;
                Position = position;
                FootprintRadius = footprintRadius;
            }

            public BuildingType Type { get; }

            public Vector2Int Position { get; }

            public int FootprintRadius { get; }

            public bool Contains(Vector2Int position)
            {
                return Mathf.Abs(position.x - Position.x) <= FootprintRadius
                    && Mathf.Abs(position.y - Position.y) <= FootprintRadius;
            }
        }

        private sealed class CartRuntime
        {
            private const float ArrivalSqrDistance = 0.0025f;
            private const float WheelRollSpeed = 240f;
            private const float ShakeDistanceFrequency = 9.5f;

            private readonly GameObject root;
            private readonly List<Vector3> waypoints;
            private readonly List<Vector3> returnWaypoints;
            private readonly Transform visualRoot;
            private readonly Transform cargo;
            private readonly Transform[] wheels;
            private readonly Vector3 visualBaseLocalPosition;
            private readonly Quaternion visualBaseLocalRotation;
            private int nextWaypoint = 1;
            private float shakePhase;
            private bool returning;

            public CartRuntime(
                GameObject root,
                List<Vector3> waypoints,
                CartVisuals visuals,
                Vector2Int farmPosition,
                int foodAmount)
            {
                this.root = root;
                this.waypoints = waypoints;
                returnWaypoints = new List<Vector3>(waypoints);
                returnWaypoints.Reverse();
                visualRoot = visuals.Root;
                cargo = visuals.Cargo;
                wheels = visuals.Wheels;
                visualBaseLocalPosition = visualRoot != null ? visualRoot.localPosition : Vector3.zero;
                visualBaseLocalRotation = visualRoot != null ? visualRoot.localRotation : Quaternion.identity;
                FarmPosition = farmPosition;
                FoodAmount = foodAmount;
                FaceNextWaypoint();
            }

            public Vector2Int FarmPosition { get; }

            public int FoodAmount { get; }

            public bool IsReturning => returning;

            public CartArrival Move(float distance)
            {
                if (root == null || nextWaypoint >= waypoints.Count)
                {
                    return returning ? CartArrival.Returned : CartArrival.Delivered;
                }

                var remaining = distance;
                var traveled = 0f;
                while (remaining > 0f && nextWaypoint < waypoints.Count)
                {
                    var target = waypoints[nextWaypoint];
                    var offset = target - root.transform.position;
                    var stepDistance = offset.magnitude;
                    if (stepDistance <= Mathf.Max(remaining, 0.001f))
                    {
                        root.transform.position = target;
                        remaining -= stepDistance;
                        traveled += stepDistance;
                        RotateWheels(stepDistance);
                        nextWaypoint++;
                        FaceNextWaypoint();
                        continue;
                    }

                    var direction = offset / stepDistance;
                    root.transform.position += direction * remaining;
                    root.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
                    RotateWheels(remaining);
                    traveled += remaining;
                    remaining = 0f;
                }

                ApplyRideShake(traveled);
                if (nextWaypoint < waypoints.Count
                    && (waypoints[waypoints.Count - 1] - root.transform.position).sqrMagnitude > ArrivalSqrDistance)
                {
                    return CartArrival.None;
                }

                return returning ? CartArrival.Returned : CartArrival.Delivered;
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
                    Object.Destroy(root);
                }
            }

            private void FaceNextWaypoint()
            {
                if (root == null || nextWaypoint >= waypoints.Count)
                {
                    return;
                }

                var direction = waypoints[nextWaypoint] - root.transform.position;
                if (direction.sqrMagnitude > 0.0001f)
                {
                    root.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
                }
            }

            private void ApplyRideShake(float distance)
            {
                if (visualRoot == null)
                {
                    return;
                }

                if (distance <= 0.0001f)
                {
                    visualRoot.localPosition = Vector3.Lerp(visualRoot.localPosition, visualBaseLocalPosition, Time.deltaTime * 8f);
                    visualRoot.localRotation = Quaternion.Slerp(visualRoot.localRotation, visualBaseLocalRotation, Time.deltaTime * 8f);
                    return;
                }

                shakePhase += distance * ShakeDistanceFrequency;
                var hop = Mathf.Abs(Mathf.Sin(shakePhase * 1.7f)) * 0.026f;
                var side = Mathf.Sin(shakePhase * 0.82f) * 0.018f;
                var pitch = Mathf.Sin(shakePhase * 1.35f) * 2.2f;
                var roll = Mathf.Sin(shakePhase * 0.95f) * 3.6f;
                visualRoot.localPosition = visualBaseLocalPosition + new Vector3(side, hop, 0f);
                visualRoot.localRotation = visualBaseLocalRotation * Quaternion.Euler(pitch, 0f, roll);
            }

            private void RotateWheels(float distance)
            {
                var angle = distance * WheelRollSpeed;
                foreach (var wheel in wheels)
                {
                    if (wheel != null)
                    {
                        wheel.Rotate(Vector3.up, angle, Space.Self);
                    }
                }
            }
        }

        private readonly struct CartVisuals
        {
            public CartVisuals(Transform root, Transform cargo, Transform[] wheels)
            {
                Root = root;
                Cargo = cargo;
                Wheels = wheels;
            }

            public Transform Root { get; }
            public Transform Cargo { get; }
            public Transform[] Wheels { get; }
        }
    }
}
