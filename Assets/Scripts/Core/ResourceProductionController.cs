using Labyrinth.Combat;
using Labyrinth.Maze;
using UnityEngine;

namespace Labyrinth.Core
{
    public sealed class ResourceProductionController : MonoBehaviour
    {
        public const float FarmProductionIntervalSeconds = 2f;
        public const float LumberjackCampProductionIntervalSeconds = FarmProductionIntervalSeconds;

        private readonly System.Collections.Generic.Dictionary<Vector2Int, int> storedFood =
            new System.Collections.Generic.Dictionary<Vector2Int, int>();
        private readonly System.Collections.Generic.Dictionary<Vector2Int, int> storedWood =
            new System.Collections.Generic.Dictionary<Vector2Int, int>();

        private ResourceWallet resources;
        private BaseDevelopment baseDevelopment;
        private BaseAmbienceController baseAmbience;
        private MazeRenderer mazeRenderer;
        private float foodProgress;

        public void Configure(
            ResourceWallet resourceWallet,
            BaseDevelopment development,
            BaseAmbienceController ambience,
            MazeRenderer renderer)
        {
            if (baseAmbience != null)
            {
                baseAmbience.FarmCartDelivered -= HandleFarmCartDelivered;
            }

            resources = resourceWallet;
            baseDevelopment = development;
            baseAmbience = ambience;
            mazeRenderer = renderer;
            foodProgress = 0f;

            if (baseAmbience != null)
            {
                baseAmbience.FarmCartDelivered += HandleFarmCartDelivered;
            }
        }

        public void ResetProgress()
        {
            foodProgress = 0f;
            storedFood.Clear();
            storedWood.Clear();
        }

        private void Update()
        {
            if (resources == null
                || baseDevelopment == null
                || (baseDevelopment.FoodPerTimeUnit <= 0 && baseDevelopment.WoodPerTimeUnit <= 0))
            {
                return;
            }

            TryDispatchReadyFarmCarts();
            TryDispatchReadyLumberCarts();

            foodProgress += Time.deltaTime;
            var wholeTicks = Mathf.FloorToInt(foodProgress / FarmProductionIntervalSeconds);
            if (wholeTicks <= 0)
            {
                return;
            }

            for (var i = 0; i < wholeTicks; i++)
            {
                ProduceFoodOnFarms();
                ProduceWoodOnLumberjackCamps();
            }

            foodProgress -= wholeTicks * FarmProductionIntervalSeconds;
        }

        private void ProduceFoodOnFarms()
        {
            foreach (var farmPosition in baseDevelopment.FarmPositions)
            {
                var currentAmount = GetStoredFood(farmPosition);
                var capacity = baseDevelopment.FarmBatchCapacity;
                if (currentAmount >= capacity)
                {
                    TryDispatchFarmCart(farmPosition);
                    continue;
                }

                storedFood[farmPosition] = Mathf.Min(capacity, currentAmount + baseDevelopment.FarmUnitsPerTick);

                if (storedFood[farmPosition] >= capacity)
                {
                    TryDispatchFarmCart(farmPosition);
                }
            }
        }

        private void TryDispatchReadyFarmCarts()
        {
            foreach (var farmPosition in baseDevelopment.FarmPositions)
            {
                if (GetStoredFood(farmPosition) >= baseDevelopment.FarmBatchCapacity)
                {
                    TryDispatchFarmCart(farmPosition);
                }
            }
        }

        private void ProduceWoodOnLumberjackCamps()
        {
            foreach (var campPosition in baseDevelopment.LumberjackCampPositions)
            {
                var currentAmount = GetStoredWood(campPosition);
                var capacity = baseDevelopment.LumberjackBatchCapacity;
                if (currentAmount >= capacity)
                {
                    TryDispatchLumberCart(campPosition);
                    continue;
                }

                storedWood[campPosition] = Mathf.Min(capacity, currentAmount + baseDevelopment.LumberjackUnitsPerTick);

                if (storedWood[campPosition] >= capacity)
                {
                    TryDispatchLumberCart(campPosition);
                }
            }
        }

        private void TryDispatchReadyLumberCarts()
        {
            foreach (var campPosition in baseDevelopment.LumberjackCampPositions)
            {
                if (GetStoredWood(campPosition) >= baseDevelopment.LumberjackBatchCapacity)
                {
                    TryDispatchLumberCart(campPosition);
                }
            }
        }

        private bool TryDispatchFarmCart(Vector2Int farmPosition)
        {
            var capacity = baseDevelopment.FarmBatchCapacity;
            if (baseAmbience == null || GetStoredFood(farmPosition) < capacity)
            {
                return false;
            }

            if (!baseAmbience.TrySendFarmCart(farmPosition, capacity))
            {
                return false;
            }

            storedFood[farmPosition] = 0;
            GameDebugLog.Info(
                "Base",
                $"Farm storage dispatched: farm={GameDebugLog.Position(farmPosition)}, food={capacity}, farmLevel={baseDevelopment.FarmLevel}.");
            return true;
        }

        private bool TryDispatchLumberCart(Vector2Int campPosition)
        {
            var capacity = baseDevelopment.LumberjackBatchCapacity;
            if (baseAmbience == null || GetStoredWood(campPosition) < capacity)
            {
                return false;
            }

            if (!baseAmbience.TrySendFarmCart(campPosition, capacity))
            {
                return false;
            }

            storedWood[campPosition] = 0;
            GameDebugLog.Info(
                "Base",
                $"Lumber camp storage dispatched: camp={GameDebugLog.Position(campPosition)}, wood={capacity}, lumberLevel={baseDevelopment.LumberjackCampLevel}.");
            return true;
        }

        private void HandleFarmCartDelivered(Vector2Int farmPosition, Vector2Int deliveryPosition, int foodAmount)
        {
            if (resources == null || foodAmount <= 0)
            {
                return;
            }

            if (IsLumberjackCamp(farmPosition))
            {
                resources.AddWood(foodAmount);
                ShowFloatingText(deliveryPosition, $"+{foodAmount} дерево", new Color(0.66f, 0.42f, 0.18f), 3.2f);
                if (mazeRenderer != null)
                {
                    GameAudioController.Play(GameSfx.LumberDelivery, mazeRenderer.GridToWorld(deliveryPosition));
                }

                GameDebugLog.Info(
                    "Base",
                    $"Lumber cart delivered: camp={GameDebugLog.Position(farmPosition)}, wood={foodAmount}, totalWood={resources.Wood}.");
                return;
            }

            resources.AddFood(foodAmount);
            ShowFloatingText(deliveryPosition, $"+{foodAmount}", new Color(0.58f, 1f, 0.32f), 3.2f);
            if (mazeRenderer != null)
            {
                GameAudioController.Play(GameSfx.FarmDelivery, mazeRenderer.GridToWorld(deliveryPosition));
            }

            GameDebugLog.Info(
                "Base",
                $"Farm cart delivered: farm={GameDebugLog.Position(farmPosition)}, food={foodAmount}, totalFood={resources.Food}.");
        }

        private int GetStoredFood(Vector2Int farmPosition)
        {
            return storedFood.TryGetValue(farmPosition, out var amount) ? amount : 0;
        }

        private int GetStoredWood(Vector2Int campPosition)
        {
            return storedWood.TryGetValue(campPosition, out var amount) ? amount : 0;
        }

        private bool IsLumberjackCamp(Vector2Int position)
        {
            foreach (var campPosition in baseDevelopment.LumberjackCampPositions)
            {
                if (campPosition == position)
                {
                    return true;
                }
            }

            return false;
        }

        private void ShowFloatingText(Vector2Int position, string text, Color color, float height)
        {
            if (mazeRenderer == null)
            {
                return;
            }

            DamageNumberView.CreateText(mazeRenderer, position, text, color, height);
        }
    }
}
