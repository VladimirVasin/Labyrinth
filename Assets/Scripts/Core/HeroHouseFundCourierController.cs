using System;
using System.Collections.Generic;
using Labyrinth.Maze;
using UnityEngine;

namespace Labyrinth.Core
{
    public sealed class HeroHouseFundCourierController : MonoBehaviour
    {
        private const float CourierYOffset = 0.065f;
        private const float CourierSpeedCellsPerSecond = 1.85f;
        private const int MaxActiveCouriers = 12;

        private readonly List<PendingDelivery> pendingDeliveries = new List<PendingDelivery>();
        private readonly List<CourierRuntime> activeCouriers = new List<CourierRuntime>();

        private MazeRenderer mazeRenderer;
        private BaseAmbienceController baseAmbience;
        private Transform root;
        private Material bodyMaterial;
        private Material headMaterial;
        private Material goldMaterial;
        private Material packMaterial;
        private int courierSerial;

        public void Configure(MazeRenderer renderer, BaseAmbienceController ambience)
        {
            mazeRenderer = renderer;
            baseAmbience = ambience;
        }

        public void Clear()
        {
            pendingDeliveries.Clear();
            for (var i = 0; i < activeCouriers.Count; i++)
            {
                activeCouriers[i].Destroy();
            }

            activeCouriers.Clear();
            courierSerial = 0;
            if (root != null)
            {
                Destroy(root.gameObject);
                root = null;
            }
        }

        public void QueueGoldTransfer(
            int heroNumber,
            int generation,
            int amount,
            Vector2Int entrancePosition,
            Vector2Int castlePosition,
            Vector2Int housePosition,
            Action<int, int> onDelivered)
        {
            if (amount <= 0)
            {
                return;
            }

            pendingDeliveries.Add(new PendingDelivery(
                heroNumber,
                generation,
                amount,
                entrancePosition,
                castlePosition,
                housePosition,
                onDelivered));
            GameDebugLog.Info(
                "Base",
                $"Hero house fund delivery queued: hero=#{heroNumber}, generation={generation}, amount={amount}, entrance={GameDebugLog.Position(entrancePosition)}, house={GameDebugLog.Position(housePosition)}.");
        }

        private void Update()
        {
            if (mazeRenderer == null || baseAmbience == null)
            {
                return;
            }

            TryStartPendingDeliveries();
            MoveCouriers();
        }

        private void TryStartPendingDeliveries()
        {
            if (pendingDeliveries.Count == 0 || activeCouriers.Count >= MaxActiveCouriers)
            {
                return;
            }

            EnsureRoot();
            EnsureMaterials();
            for (var i = pendingDeliveries.Count - 1; i >= 0 && activeCouriers.Count < MaxActiveCouriers; i--)
            {
                var delivery = pendingDeliveries[i];
                if (!TryBuildDeliveryPath(delivery, out var waypoints))
                {
                    continue;
                }

                pendingDeliveries.RemoveAt(i);
                var courierRoot = CreateCourier(waypoints[0]);
                activeCouriers.Add(new CourierRuntime(
                    ++courierSerial,
                    courierRoot,
                    waypoints,
                    delivery));
                GameDebugLog.Info(
                    "Base",
                    $"Hero house fund courier #{courierSerial} sent: hero=#{delivery.HeroNumber}, amount={delivery.Amount}, waypoints={waypoints.Count}.");
            }
        }

        private bool TryBuildDeliveryPath(PendingDelivery delivery, out List<Vector3> waypoints)
        {
            waypoints = null;
            if (!baseAmbience.TryGetRoadPath(delivery.EntrancePosition, delivery.CastlePosition, out var entranceToCastle)
                || !baseAmbience.TryGetRoadPath(delivery.CastlePosition, delivery.HousePosition, out var castleToHouse))
            {
                return false;
            }

            var cells = new List<Vector2Int>(entranceToCastle.Count + castleToHouse.Count);
            cells.AddRange(entranceToCastle);
            for (var i = 1; i < castleToHouse.Count; i++)
            {
                cells.Add(castleToHouse[i]);
            }

            waypoints = new List<Vector3>(cells.Count);
            var offset = new Vector3(0f, mazeRenderer.CellSize * CourierYOffset, 0f);
            for (var i = 0; i < cells.Count; i++)
            {
                waypoints.Add(mazeRenderer.GridToWorld(cells[i]) + offset);
            }

            return waypoints.Count >= 2;
        }

        private void MoveCouriers()
        {
            if (activeCouriers.Count == 0)
            {
                return;
            }

            var speed = mazeRenderer.CellSize * CourierSpeedCellsPerSecond * Time.deltaTime;
            for (var i = activeCouriers.Count - 1; i >= 0; i--)
            {
                var courier = activeCouriers[i];
                if (!courier.Move(speed))
                {
                    continue;
                }

                courier.Delivery.OnDelivered?.Invoke(courier.Delivery.Generation, courier.Delivery.Amount);
                GameAudioController.Play(GameSfx.GoldFound, courier.Root.position, 0.55f);
                GameDebugLog.Info(
                    "Base",
                    $"Hero house fund courier #{courier.Id} delivered: hero=#{courier.Delivery.HeroNumber}, generation={courier.Delivery.Generation}, amount={courier.Delivery.Amount}.");
                courier.Destroy();
                activeCouriers.RemoveAt(i);
            }
        }

        private Transform CreateCourier(Vector3 position)
        {
            var unit = mazeRenderer.ModelUnitSize * 0.86f;
            var courierRoot = new GameObject("Hero House Fund Courier").transform;
            courierRoot.SetParent(root, false);
            courierRoot.position = position;
            CreatePart(courierRoot, "Body", PrimitiveType.Capsule, new Vector3(0f, unit * 0.35f, 0f), new Vector3(unit * 0.2f, unit * 0.34f, unit * 0.2f), bodyMaterial);
            CreatePart(courierRoot, "Head", PrimitiveType.Sphere, new Vector3(0f, unit * 0.74f, 0f), Vector3.one * unit * 0.18f, headMaterial);
            CreatePart(courierRoot, "Gold Pack", PrimitiveType.Cube, new Vector3(unit * 0.18f, unit * 0.5f, unit * -0.06f), new Vector3(unit * 0.14f, unit * 0.18f, unit * 0.18f), packMaterial);
            CreatePart(courierRoot, "Gold Coin", PrimitiveType.Sphere, new Vector3(unit * 0.23f, unit * 0.68f, unit * -0.02f), Vector3.one * unit * 0.09f, goldMaterial);
            return courierRoot;
        }

        private static void CreatePart(Transform parent, string name, PrimitiveType type, Vector3 localPosition, Vector3 localScale, Material material)
        {
            var part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            var collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }
        }

        private void EnsureRoot()
        {
            if (root != null)
            {
                return;
            }

            root = new GameObject("HeroHouseFundCouriersRoot").transform;
            root.SetParent(transform, false);
        }

        private void EnsureMaterials()
        {
            if (bodyMaterial != null)
            {
                return;
            }

            bodyMaterial = CreateMaterial("Fund Courier Body", new Color(0.42f, 0.25f, 0.54f));
            headMaterial = CreateMaterial("Fund Courier Head", new Color(0.82f, 0.62f, 0.42f));
            goldMaterial = CreateMaterial("Fund Courier Gold", new Color(1f, 0.75f, 0.16f));
            packMaterial = CreateMaterial("Fund Courier Pack", new Color(0.36f, 0.18f, 0.08f));
        }

        private static Material CreateMaterial(string materialName, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            var material = new Material(shader) { name = materialName, color = color };
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            return material;
        }

        private sealed class PendingDelivery
        {
            public PendingDelivery(
                int heroNumber,
                int generation,
                int amount,
                Vector2Int entrancePosition,
                Vector2Int castlePosition,
                Vector2Int housePosition,
                Action<int, int> onDelivered)
            {
                HeroNumber = heroNumber;
                Generation = generation;
                Amount = amount;
                EntrancePosition = entrancePosition;
                CastlePosition = castlePosition;
                HousePosition = housePosition;
                OnDelivered = onDelivered;
            }

            public int HeroNumber { get; }
            public int Generation { get; }
            public int Amount { get; }
            public Vector2Int EntrancePosition { get; }
            public Vector2Int CastlePosition { get; }
            public Vector2Int HousePosition { get; }
            public Action<int, int> OnDelivered { get; }
        }

        private sealed class CourierRuntime
        {
            private readonly List<Vector3> waypoints;
            private int waypointIndex = 1;

            public CourierRuntime(int id, Transform root, List<Vector3> waypoints, PendingDelivery delivery)
            {
                Id = id;
                Root = root;
                this.waypoints = waypoints;
                Delivery = delivery;
            }

            public int Id { get; }
            public Transform Root { get; }
            public PendingDelivery Delivery { get; }

            public bool Move(float distance)
            {
                if (Root == null || waypointIndex >= waypoints.Count)
                {
                    return true;
                }

                var target = waypoints[waypointIndex];
                var direction = target - Root.position;
                if (direction.sqrMagnitude > 0.0001f)
                {
                    Root.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
                }

                Root.position = Vector3.MoveTowards(Root.position, target, distance);
                if (Vector3.Distance(Root.position, target) <= 0.01f)
                {
                    waypointIndex++;
                }

                return waypointIndex >= waypoints.Count;
            }

            public void Destroy()
            {
                if (Root != null)
                {
                    UnityEngine.Object.Destroy(Root.gameObject);
                }
            }
        }
    }
}
