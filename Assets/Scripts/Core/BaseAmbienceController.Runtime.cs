using System.Collections.Generic;
using Labyrinth.Base;
using UnityEngine;

namespace Labyrinth.Core
{
    public sealed partial class BaseAmbienceController
    {
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

            public float BuildTimer { get; set; }

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

            private readonly GameObject root;
            private readonly List<Vector3> waypoints;
            private readonly Transform[] wheels;
            private int nextWaypoint = 1;

            public CartRuntime(
                GameObject root,
                List<Vector3> waypoints,
                Transform[] wheels,
                Vector2Int farmPosition,
                int foodAmount)
            {
                this.root = root;
                this.waypoints = waypoints;
                this.wheels = wheels;
                FarmPosition = farmPosition;
                FoodAmount = foodAmount;
                FaceNextWaypoint();
            }

            public Vector2Int FarmPosition { get; }

            public int FoodAmount { get; }

            public bool Move(float distance)
            {
                if (root == null || nextWaypoint >= waypoints.Count)
                {
                    return true;
                }

                var remaining = distance;
                while (remaining > 0f && nextWaypoint < waypoints.Count)
                {
                    var target = waypoints[nextWaypoint];
                    var offset = target - root.transform.position;
                    var stepDistance = offset.magnitude;
                    if (stepDistance <= Mathf.Max(remaining, 0.001f))
                    {
                        root.transform.position = target;
                        remaining -= stepDistance;
                        nextWaypoint++;
                        FaceNextWaypoint();
                        continue;
                    }

                    var direction = offset / stepDistance;
                    root.transform.position += direction * remaining;
                    root.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
                    RotateWheels(remaining);
                    remaining = 0f;
                }

                return nextWaypoint >= waypoints.Count
                    || (waypoints[waypoints.Count - 1] - root.transform.position).sqrMagnitude <= ArrivalSqrDistance;
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

            private void RotateWheels(float distance)
            {
                var angle = distance * WheelRollSpeed;
                foreach (var wheel in wheels)
                {
                    if (wheel != null)
                    {
                        wheel.Rotate(Vector3.right, angle, Space.Self);
                    }
                }
            }
        }
    }
}
