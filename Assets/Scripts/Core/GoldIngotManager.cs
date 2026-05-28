using System.Collections.Generic;
using Labyrinth.Combat;
using Labyrinth.Hero;
using Labyrinth.Maze;
using UnityEngine;

namespace Labyrinth.Core
{
    public sealed class GoldIngotManager : MonoBehaviour
    {
        public const int TreasuryGoldReward = 20;
        public const int DeliveryExperienceReward = 5;

        private const int MinimumIngotCount = 5;
        private const int AreaPerIngot = 250;
        private const int MaximumIngotCount = 60;
        private const int MinimumDistanceFromEntrance = 4;
        private const int NearEntranceExtraMinimumCount = 2;
        private const int NearEntranceExtraAreaPerIngot = 700;
        private const int NearEntranceExtraMaximumCount = 24;
        private const int NearEntranceMinimumDistance = 2;
        private const int NearEntranceMaximumDistanceMinimum = 8;
        private const int NearEntranceMaximumDistanceMaximum = 22;

        private readonly List<GoldIngotModel> ingots = new List<GoldIngotModel>();
        private readonly Dictionary<HeroModel, GoldIngotModel> carriedIngots =
            new Dictionary<HeroModel, GoldIngotModel>();

        private ResourceWallet resources;
        private MazeGenerationResult result;
        private MazeRenderer mazeRenderer;
        private Transform root;
        private Material goldMaterial;
        private Material shadowMaterial;
        private int nextIngotId;

        public event System.Action<HeroModel> IngotDeliveredByHero;

        public int AvailableCount
        {
            get
            {
                var count = 0;
                foreach (var ingot in ingots)
                {
                    if (ingot != null && ingot.IsAvailable)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public void Configure(ResourceWallet wallet)
        {
            resources = wallet;
        }

        public void Spawn(
            MazeGenerationResult generationResult,
            MazeRenderer renderer,
            HashSet<Vector2Int> blockedPositions)
        {
            Clear();
            result = generationResult;
            mazeRenderer = renderer;

            if (result == null || mazeRenderer == null || mazeRenderer.ContentRoot == null)
            {
                return;
            }

            EnsureMaterials();
            root = new GameObject("GoldIngotsRoot").transform;
            root.SetParent(mazeRenderer.ContentRoot, false);

            var random = new System.Random(result.Settings.Seed ^ 0x4d2b61f);
            var candidates = CollectCandidates(blockedPositions ?? new HashSet<Vector2Int>());
            var desiredCount = CalculateIngotCount(result.Grid.Width, result.Grid.Height);
            var positions = SelectIngotPositions(candidates, desiredCount, random);
            var nearEntranceDesiredCount = CalculateNearEntranceExtraIngotCount(result.Grid.Width, result.Grid.Height);
            var nearEntranceCandidates = CollectNearEntranceCandidates(
                blockedPositions ?? new HashSet<Vector2Int>(),
                new HashSet<Vector2Int>(positions));
            var nearEntrancePositions = SelectNearEntranceExtraPositions(
                nearEntranceCandidates,
                nearEntranceDesiredCount,
                random);
            positions.AddRange(nearEntrancePositions);

            foreach (var position in positions)
            {
                var ingot = new GoldIngotModel(++nextIngotId, position);
                ingots.Add(ingot);
                RenderIngot(ingot);
            }

            GameDebugLog.Info(
                "Maze",
                $"Gold ingots spawned: desired={desiredCount}, nearEntranceExtra={nearEntranceDesiredCount}, placed={positions.Count}, nearEntrancePlaced={nearEntrancePositions.Count}, candidates={candidates.Count}, nearEntranceCandidates={nearEntranceCandidates.Count}, reward={TreasuryGoldReward} gold/{DeliveryExperienceReward} XP.");
        }

        public void Clear()
        {
            ingots.Clear();
            carriedIngots.Clear();
            result = null;
            mazeRenderer = null;
            nextIngotId = 0;

            if (root != null)
            {
                Destroy(root.gameObject);
                root = null;
            }
        }

        public bool TryPickUp(HeroModel hero)
        {
            if (hero == null || hero.Inventory == null || hero.Inventory.HasGoldIngot)
            {
                return false;
            }

            var ingot = FindAvailableAt(hero.Position);
            if (ingot == null)
            {
                return false;
            }

            if (!hero.Inventory.TryPlaceInEmptySlot(
                HeroInventory.GoldIngotItemName,
                HeroInventory.GoldIngotHoverInfo))
            {
                GameDebugLog.Warning(
                    "Hero",
                    $"Hero {FormatHero(hero)} reached gold ingot at {GameDebugLog.Position(hero.Position)} but has no empty inventory slot.");
                return false;
            }

            ingot.PickUp();
            carriedIngots[hero] = ingot;
            DamageNumberView.CreateText(
                mazeRenderer,
                hero.Position,
                HeroInventory.GoldIngotItemName,
                new Color(1f, 0.82f, 0.25f),
                1.55f);
            GameAudioController.Play(GameSfx.IngotPickup, mazeRenderer.GridToWorld(hero.Position));
            GameDebugLog.Info(
                "Hero",
                $"Hero {FormatHero(hero)} picked up gold ingot #{ingot.Id} at {GameDebugLog.Position(hero.Position)}, inventorySlot={FormatInventorySlot(hero.Inventory, HeroInventory.GoldIngotItemName)}.");
            return true;
        }

        public bool TryDeliver(HeroModel hero)
        {
            if (hero == null
                || result == null
                || resources == null
                || hero.Position != result.EntrancePosition
                || !hero.Inventory.HasGoldIngot)
            {
                return false;
            }

            var inventorySlot = FormatInventorySlot(hero.Inventory, HeroInventory.GoldIngotItemName);
            if (!hero.Inventory.TryRemoveItem(HeroInventory.GoldIngotItemName))
            {
                return false;
            }

            resources.AddGold(TreasuryGoldReward);
            var gainedLevels = hero.AddExperience(DeliveryExperienceReward);
            var vengeanceProgress = hero.ApplyGoldIngotDeliveryVengeance();
            gainedLevels += vengeanceProgress.GainedLevels;
            if (carriedIngots.TryGetValue(hero, out var ingot))
            {
                ingot.Deliver();
                carriedIngots.Remove(hero);
            }

            DamageNumberView.CreateText(
                mazeRenderer,
                result.EntrancePosition,
                $"+{TreasuryGoldReward} зол.",
                new Color(1f, 0.84f, 0.26f),
                1.75f);
            DamageNumberView.CreateText(
                mazeRenderer,
                result.EntrancePosition,
                $"+{DeliveryExperienceReward} XP",
                new Color(0.55f, 0.86f, 1f),
                2.05f);
            ShowVengeanceProgress(hero, vengeanceProgress, 2.35f);
            GameAudioController.Play(GameSfx.IngotDeposit, mazeRenderer.GridToWorld(result.EntrancePosition));
            if (gainedLevels > 0)
            {
                GameAudioController.Play(GameSfx.LevelUp, mazeRenderer.GridToWorld(result.EntrancePosition));
            }

            GameDebugLog.Info(
                "Hero",
                $"Hero {FormatHero(hero)} delivered gold ingot: inventorySlot={inventorySlot}, treasuryGold={resources.Gold}, vengeanceGold={vengeanceProgress.BonusGold}, vengeanceXP={vengeanceProgress.BonusExperience}, heroGold={hero.Gold}, heroXP={hero.Experience}/{hero.ExperienceForNextLevel}, heroLevel={hero.Level}, gainedLevels={gainedLevels}.");
            IngotDeliveredByHero?.Invoke(hero);
            return true;
        }

        public bool DropCarriedIngot(HeroModel hero)
        {
            if (hero == null || hero.Inventory == null || !hero.Inventory.HasGoldIngot)
            {
                return false;
            }

            var inventorySlot = FormatInventorySlot(hero.Inventory, HeroInventory.GoldIngotItemName);
            hero.Inventory.TryRemoveItem(HeroInventory.GoldIngotItemName);
            if (!carriedIngots.TryGetValue(hero, out var ingot) || ingot == null)
            {
                ingot = new GoldIngotModel(++nextIngotId, hero.Position);
                ingots.Add(ingot);
            }

            carriedIngots.Remove(hero);
            var dropPosition = FindDropPosition(hero.Position);
            ingot.Drop(dropPosition);
            RenderIngot(ingot);
            GameDebugLog.Info(
                "Hero",
                $"Hero {FormatHero(hero)} dropped gold ingot #{ingot.Id} from inventorySlot={inventorySlot} at {GameDebugLog.Position(dropPosition)} after death.");
            return true;
        }

        private static string FormatHero(HeroModel hero)
        {
            if (hero == null)
            {
                return "unknown";
            }

            var name = string.IsNullOrWhiteSpace(hero.DisplayName) ? "unnamed" : hero.DisplayName;
            return hero.DisplayNumber > 0 ? $"#{hero.DisplayNumber} ({name})" : $"unassigned ({name})";
        }

        private static string FormatInventorySlot(HeroInventory inventory, string itemName)
        {
            if (inventory == null)
            {
                return "none";
            }

            return inventory.TryFindItemSlot(itemName, out var slotIndex, out var slot)
                ? $"{slotIndex + 1}/{inventory.Slots.Count} {slot.Type} ({slot.Label})"
                : "missing";
        }

        private List<Vector2Int> CollectCandidates(HashSet<Vector2Int> blockedPositions)
        {
            var candidates = new List<Vector2Int>();
            var distances = MazeValidation.GetReachableDistances(result.Grid, result.EntrancePosition, true);

            foreach (var cell in result.Grid.Cells())
            {
                var position = new Vector2Int(cell.X, cell.Y);
                if (!cell.IsWalkable
                    || cell.Type == MazeCellType.Entrance
                    || result.CentralRoom.Contains(position)
                    || blockedPositions.Contains(position)
                    || IsReservedByStaticObject(position)
                    || GridDistance(position, result.EntrancePosition) <= MinimumDistanceFromEntrance
                    || !distances.ContainsKey(position))
                {
                    continue;
                }

                candidates.Add(position);
            }

            return candidates;
        }

        private List<Vector2Int> CollectNearEntranceCandidates(
            HashSet<Vector2Int> blockedPositions,
            HashSet<Vector2Int> excludedPositions)
        {
            var candidates = new List<Vector2Int>();
            var distances = MazeValidation.GetReachableDistances(result.Grid, result.EntrancePosition, true);
            var maxDistance = CalculateNearEntranceMaximumDistance(result.Grid.Width, result.Grid.Height);

            foreach (var cell in result.Grid.Cells())
            {
                var position = new Vector2Int(cell.X, cell.Y);
                if (!cell.IsWalkable
                    || cell.Type == MazeCellType.Entrance
                    || result.CentralRoom.Contains(position)
                    || blockedPositions.Contains(position)
                    || excludedPositions.Contains(position)
                    || IsReservedByStaticObject(position)
                    || !distances.TryGetValue(position, out var distance)
                    || distance < NearEntranceMinimumDistance
                    || distance > maxDistance)
                {
                    continue;
                }

                candidates.Add(position);
            }

            return candidates;
        }

        private List<Vector2Int> SelectIngotPositions(
            List<Vector2Int> candidates,
            int desiredCount,
            System.Random random)
        {
            var selected = new List<Vector2Int>();
            var firstHalf = new List<Vector2Int>();
            var secondHalf = new List<Vector2Int>();

            foreach (var candidate in candidates)
            {
                if (candidate.x < result.CentralRoom.Min.x)
                {
                    firstHalf.Add(candidate);
                }
                else if (candidate.x > result.CentralRoom.Max.x)
                {
                    secondHalf.Add(candidate);
                }
            }

            AddSpreadPositions(selected, firstHalf, (desiredCount + 1) / 2, random);
            AddSpreadPositions(selected, secondHalf, desiredCount - selected.Count, random);
            AddSpreadPositions(selected, candidates, desiredCount - selected.Count, random);
            return selected;
        }

        private static void AddSpreadPositions(
            List<Vector2Int> selected,
            List<Vector2Int> candidates,
            int count,
            System.Random random)
        {
            AddSpreadPositions(selected, candidates, count, random, 6);
        }

        private static void AddSpreadPositions(
            List<Vector2Int> selected,
            List<Vector2Int> candidates,
            int count,
            System.Random random,
            int startingMinimumDistance)
        {
            if (count <= 0 || candidates.Count == 0)
            {
                return;
            }

            var targetCount = selected.Count + count;
            Shuffle(candidates, random);
            var minimumDistance = startingMinimumDistance;
            while (selected.Count < targetCount && minimumDistance >= 0)
            {
                var addedThisPass = false;
                foreach (var candidate in candidates)
                {
                    if (selected.Count >= targetCount)
                    {
                        return;
                    }

                    if (selected.Contains(candidate) || IsNearSelected(candidate, selected, minimumDistance))
                    {
                        continue;
                    }

                    selected.Add(candidate);
                    addedThisPass = true;
                }

                if (!addedThisPass)
                {
                    minimumDistance--;
                }
            }
        }

        private static List<Vector2Int> SelectNearEntranceExtraPositions(
            List<Vector2Int> candidates,
            int desiredCount,
            System.Random random)
        {
            var selected = new List<Vector2Int>();
            AddSpreadPositions(selected, candidates, desiredCount, random, 4);
            return selected;
        }

        private Vector2Int FindDropPosition(Vector2Int origin)
        {
            if (IsFreeDropCell(origin))
            {
                return origin;
            }

            var queue = new Queue<Vector2Int>();
            var visited = new HashSet<Vector2Int> { origin };
            queue.Enqueue(origin);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var neighbor in result.Grid.WalkableNeighbors(current))
                {
                    if (!visited.Add(neighbor))
                    {
                        continue;
                    }

                    if (IsFreeDropCell(neighbor))
                    {
                        return neighbor;
                    }

                    queue.Enqueue(neighbor);
                }
            }

            return origin;
        }

        private bool IsFreeDropCell(Vector2Int position)
        {
            return result != null
                && result.Grid.InBounds(position)
                && result.Grid.Get(position).IsWalkable
                && FindAvailableAt(position) == null;
        }

        private GoldIngotModel FindAvailableAt(Vector2Int position)
        {
            foreach (var ingot in ingots)
            {
                if (ingot != null && ingot.IsAvailable && ingot.Position == position)
                {
                    return ingot;
                }
            }

            return null;
        }

        private bool IsReservedByStaticObject(Vector2Int position)
        {
            if (result.CentralRoomKey != null && result.CentralRoomKey.Position == position)
            {
                return true;
            }

            foreach (var door in result.CentralDoors)
            {
                if (door != null && door.Position == position)
                {
                    return true;
                }
            }

            foreach (var chest in result.Chests)
            {
                if (chest != null && chest.Position == position)
                {
                    return true;
                }
            }

            if ((result.DownStairs != null && result.DownStairs.Position == position)
                || (result.UpStairs != null && result.UpStairs.Position == position))
            {
                return true;
            }

            return false;
        }

        private void RenderIngot(GoldIngotModel ingot)
        {
            if (ingot == null || root == null || mazeRenderer == null)
            {
                return;
            }

            var ingotRoot = new GameObject($"Gold Ingot {ingot.Id}");
            ingotRoot.transform.SetParent(root, false);
            var position = mazeRenderer.GridToWorld(ingot.Position);
            ingotRoot.transform.position = position;

            var shadow = CreateCube(
                "Ingot Shadow",
                position + new Vector3(0f, mazeRenderer.CellSize * 0.015f, 0f),
                new Vector3(mazeRenderer.CellSize * 0.42f, mazeRenderer.CellSize * 0.03f, mazeRenderer.CellSize * 0.28f),
                shadowMaterial,
                ingotRoot.transform);
            mazeRenderer.TrackExternalCellRenderer(ingot.Position, shadow);

            var body = CreateCube(
                "Gold Ingot Body",
                position + new Vector3(0f, mazeRenderer.CellSize * 0.12f, 0f),
                new Vector3(mazeRenderer.CellSize * 0.36f, mazeRenderer.CellSize * 0.14f, mazeRenderer.CellSize * 0.22f),
                goldMaterial,
                ingotRoot.transform);
            body.transform.rotation = Quaternion.Euler(0f, 35f, 0f);
            mazeRenderer.TrackExternalCellRenderer(ingot.Position, body);
            var hudTarget = ingotRoot.AddComponent<ObjectMicroHudTarget>();
            hudTarget.Configure(
                "Золотой слиток",
                "ценный ресурс",
                "Ресурс",
                ingot.Position,
                new Color(1f, 0.74f, 0.18f),
                () => ingot.IsAvailable ? "лежит в лабиринте" : ingot.State == GoldIngotState.Carried ? "у рыцаря" : "сдан в казну",
                () => $"+{TreasuryGoldReward} зол. в казну и +{DeliveryExperienceReward} XP, если рыцарь донесет слиток до входа.");
            var collider = ingotRoot.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, mazeRenderer.CellSize * 0.14f, 0f);
            collider.size = new Vector3(mazeRenderer.CellSize * 0.52f, mazeRenderer.CellSize * 0.28f, mazeRenderer.CellSize * 0.38f);
            ingot.AttachVisual(ingotRoot);
        }

        private void ShowVengeanceProgress(HeroModel hero, HeroVengeanceProgressResult progress, float delay)
        {
            if (!progress.HasAnyFeedback || hero == null)
            {
                return;
            }

            if (progress.Completed)
            {
                DamageNumberView.CreateText(
                    mazeRenderer,
                    result.EntrancePosition,
                    progress.Message,
                    new Color(1f, 0.72f, 0.28f),
                    delay);
                delay += 0.3f;
            }

            if (progress.BonusGold > 0)
            {
                DamageNumberView.CreateText(
                    mazeRenderer,
                    result.EntrancePosition,
                    $"+{progress.BonusGold} личн. зол.",
                    new Color(1f, 0.84f, 0.26f),
                    delay);
                delay += 0.3f;
            }

            if (progress.BonusExperience > 0)
            {
                DamageNumberView.CreateText(
                    mazeRenderer,
                    result.EntrancePosition,
                    $"+{progress.BonusExperience} XP клятвы",
                    new Color(0.55f, 0.86f, 1f),
                    delay);
            }
        }

        private static GameObject CreateCube(
            string name,
            Vector3 position,
            Vector3 scale,
            Material material,
            Transform parent)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.position = position;
            cube.transform.localScale = scale;
            cube.GetComponent<Renderer>().sharedMaterial = material;
            var collider = cube.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            return cube;
        }

        private void EnsureMaterials()
        {
            if (goldMaterial != null)
            {
                return;
            }

            goldMaterial = CreateMaterial("Gold Ingot", new Color(1f, 0.73f, 0.18f));
            shadowMaterial = CreateMaterial("Gold Ingot Shadow", new Color(0.25f, 0.16f, 0.04f, 0.55f));
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

        private static int CalculateIngotCount(int width, int height)
        {
            var desired = Mathf.RoundToInt(width * height / (float)AreaPerIngot);
            return Mathf.Clamp(desired, MinimumIngotCount, MaximumIngotCount);
        }

        private static int CalculateNearEntranceExtraIngotCount(int width, int height)
        {
            var desired = Mathf.RoundToInt(width * height / (float)NearEntranceExtraAreaPerIngot);
            return Mathf.Clamp(desired, NearEntranceExtraMinimumCount, NearEntranceExtraMaximumCount);
        }

        private static int CalculateNearEntranceMaximumDistance(int width, int height)
        {
            var desired = Mathf.RoundToInt((width + height) / 6f);
            return Mathf.Clamp(
                desired,
                NearEntranceMaximumDistanceMinimum,
                NearEntranceMaximumDistanceMaximum);
        }

        private static bool IsNearSelected(Vector2Int position, IReadOnlyList<Vector2Int> selected, int distance)
        {
            foreach (var selectedPosition in selected)
            {
                if (GridDistance(position, selectedPosition) < distance)
                {
                    return true;
                }
            }

            return false;
        }

        private static void Shuffle(List<Vector2Int> values, System.Random random)
        {
            for (var i = values.Count - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                var temp = values[i];
                values[i] = values[j];
                values[j] = temp;
            }
        }

        private static int GridDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }
    }
}
