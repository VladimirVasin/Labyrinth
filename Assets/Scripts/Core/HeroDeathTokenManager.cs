using System;
using System.Collections.Generic;
using Labyrinth.Base;
using Labyrinth.Combat;
using Labyrinth.Hero;
using Labyrinth.Maze;
using UnityEngine;

namespace Labyrinth.Core
{
    public sealed class HeroDeathTokenManager : MonoBehaviour
    {
        public const int DeliveryExperienceReward = 10;

        private readonly List<HeroDeathTokenModel> tokens = new List<HeroDeathTokenModel>();
        private readonly Dictionary<HeroModel, HeroDeathTokenModel> carriedTokens =
            new Dictionary<HeroModel, HeroDeathTokenModel>();
        private readonly Dictionary<int, GameObject> houseTokenVisuals = new Dictionary<int, GameObject>();

        private MazeGenerationResult result;
        private MazeRenderer mazeRenderer;
        private Func<int, BuildingView> heroHouseProvider;
        private Transform root;
        private Material tokenMaterial;
        private Material strapMaterial;
        private Material shadowMaterial;
        private int nextTokenId;

        public event Action<HeroDeathTokenModel> TokenDelivered;
        public event Action<HeroModel> TokenDeliveredByHero;

        public void Initialize(
            MazeGenerationResult generationResult,
            MazeRenderer renderer,
            Func<int, BuildingView> getHeroHouse)
        {
            result = generationResult;
            mazeRenderer = renderer;
            heroHouseProvider = getHeroHouse;
            DestroyRootVisuals();
            houseTokenVisuals.Clear();

            if (result == null || mazeRenderer == null || mazeRenderer.ContentRoot == null)
            {
                return;
            }

            EnsureMaterials();
            root = new GameObject("HeroDeathTokensRoot").transform;
            root.SetParent(mazeRenderer.ContentRoot, false);
            RefreshTokenVisuals();
            RefreshHouseMemorials();
        }

        public void Clear()
        {
            tokens.Clear();
            carriedTokens.Clear();
            houseTokenVisuals.Clear();
            result = null;
            mazeRenderer = null;
            heroHouseProvider = null;
            nextTokenId = 0;
            DestroyRootVisuals();
        }

        public bool HasCarriedToken(HeroModel hero)
        {
            return TryGetCarriedToken(hero, out _);
        }

        public void ClearTokensForHero(int heroNumber)
        {
            for (var i = tokens.Count - 1; i >= 0; i--)
            {
                var token = tokens[i];
                if (token == null || token.HeroNumber != heroNumber)
                {
                    continue;
                }

                RemoveCarriedReferences(token);
                token.DestroyVisual();
                if (houseTokenVisuals.TryGetValue(token.Id, out var tokenVisual) && tokenVisual != null)
                {
                    Destroy(tokenVisual);
                }

                houseTokenVisuals.Remove(token.Id);
                tokens.RemoveAt(i);
            }
        }

        public HeroDeathTokenModel CreateTokenForDefeatedHero(HeroController hero, Vector2Int housePosition)
        {
            if (hero == null || hero.Model == null || result == null)
            {
                return null;
            }

            var dropPosition = FindDropPosition(hero.Model.Position);
            var token = new HeroDeathTokenModel(
                ++nextTokenId,
                hero.DisplayNumber,
                hero.DisplayName,
                result.LevelNumber,
                dropPosition,
                housePosition);
            tokens.Add(token);
            RenderToken(token);
            DamageNumberView.CreateText(
                mazeRenderer,
                dropPosition,
                "Жетон",
                new Color(0.9f, 0.88f, 0.72f),
                1.75f);
            GameDebugLog.Info(
                "Hero",
                $"Created death token #{token.Id} for hero #{hero.DisplayNumber} ({token.FallenHeroName}): level={token.LevelNumber}, drop={GameDebugLog.Position(dropPosition)}, house={GameDebugLog.Position(housePosition)}.");
            return token;
        }

        public bool TryPickUp(HeroModel hero)
        {
            if (hero == null || hero.Inventory == null || hero.Inventory.HasDeathToken)
            {
                return false;
            }

            var token = FindAvailableAt(hero.Position);
            if (token == null)
            {
                return false;
            }

            var hoverInfo = $"{token.FallenHeroName}: вернуть к входу, жетон прикрепится к дому, +{DeliveryExperienceReward} XP";
            if (!hero.Inventory.TryPlaceInEmptySlot(token.ItemName, hoverInfo))
            {
                GameDebugLog.Warning(
                    "Hero",
                    $"Hero {FormatHero(hero)} reached death token #{token.Id} at {GameDebugLog.Position(hero.Position)} but has no empty inventory slot.");
                return false;
            }

            token.PickUp();
            carriedTokens[hero] = token;
            DamageNumberView.CreateText(
                mazeRenderer,
                hero.Position,
                token.ItemName,
                new Color(0.92f, 0.88f, 0.72f),
                1.75f);
            GameAudioController.Play(GameSfx.KeyPickup, mazeRenderer.GridToWorld(hero.Position), 0.7f);
            GameDebugLog.Info(
                "Hero",
                $"Hero {FormatHero(hero)} picked up death token #{token.Id} for hero #{token.HeroNumber} ({token.FallenHeroName}) at {GameDebugLog.Position(hero.Position)}, inventorySlot={FormatInventorySlot(hero.Inventory, token.ItemName)}.");
            return true;
        }

        public bool TryDeliver(HeroModel hero)
        {
            return result != null && TryDeliver(hero, result.EntrancePosition);
        }

        public bool TryDeliver(HeroModel hero, Vector2Int deliveryPosition)
        {
            if (hero == null
                || hero.Inventory == null
                || result == null
                || hero.Position != deliveryPosition
                || !TryGetCarriedToken(hero, out var token))
            {
                return false;
            }

            var inventorySlot = FormatInventorySlot(hero.Inventory, token.ItemName);
            if (!hero.Inventory.TryRemoveItem(token.ItemName))
            {
                return false;
            }

            token.Deliver();
            carriedTokens.Remove(hero);
            var gainedLevels = hero.AddExperience(DeliveryExperienceReward);
            var vengeanceProgress = hero.ApplyDeathTokenDeliveryVengeance();
            gainedLevels += vengeanceProgress.GainedLevels;
            AttachTokenToHouse(token);
            TokenDelivered?.Invoke(token);
            DamageNumberView.CreateText(
                mazeRenderer,
                deliveryPosition,
                "Жетон возвращен",
                new Color(0.92f, 0.88f, 0.72f),
                1.9f);
            DamageNumberView.CreateText(
                mazeRenderer,
                deliveryPosition,
                $"+{DeliveryExperienceReward} XP",
                new Color(0.55f, 0.86f, 1f),
                2.2f);
            ShowVengeanceProgress(hero, deliveryPosition, vengeanceProgress, 2.5f);
            GameAudioController.Play(GameSfx.IngotDeposit, mazeRenderer.GridToWorld(deliveryPosition), 0.78f);
            if (gainedLevels > 0)
            {
                GameAudioController.Play(GameSfx.LevelUp, mazeRenderer.GridToWorld(deliveryPosition));
            }

            GameDebugLog.Info(
                "Hero",
                $"Hero {FormatHero(hero)} delivered death token #{token.Id} for hero #{token.HeroNumber} ({token.FallenHeroName}): inventorySlot={inventorySlot}, deliveryPosition={GameDebugLog.Position(deliveryPosition)}, vengeanceXP={vengeanceProgress.BonusExperience}, xp={hero.Experience}/{hero.ExperienceForNextLevel}, level={hero.Level}, gainedLevels={gainedLevels}.");
            TokenDeliveredByHero?.Invoke(hero);
            return true;
        }

        public bool DropCarriedToken(HeroModel hero)
        {
            if (hero == null
                || hero.Inventory == null
                || result == null
                || !TryGetCarriedToken(hero, out var token))
            {
                return false;
            }

            var inventorySlot = FormatInventorySlot(hero.Inventory, token.ItemName);
            hero.Inventory.TryRemoveItem(token.ItemName);
            carriedTokens.Remove(hero);
            var dropPosition = FindDropPosition(hero.Position);
            token.Drop(dropPosition, result.LevelNumber);
            RenderToken(token);
            GameDebugLog.Info(
                "Hero",
                $"Hero {FormatHero(hero)} dropped carried death token #{token.Id} for hero #{token.HeroNumber} ({token.FallenHeroName}) from inventorySlot={inventorySlot} at {GameDebugLog.Position(dropPosition)} after death.");
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

        private void RefreshTokenVisuals()
        {
            for (var i = 0; i < tokens.Count; i++)
            {
                RenderToken(tokens[i]);
            }
        }

        private void RefreshHouseMemorials()
        {
            for (var i = 0; i < tokens.Count; i++)
            {
                if (tokens[i].IsDelivered)
                {
                    AttachTokenToHouse(tokens[i]);
                }
            }
        }

        private void RenderToken(HeroDeathTokenModel token)
        {
            if (token == null
                || !token.IsAvailable
                || result == null
                || token.LevelNumber != result.LevelNumber
                || root == null
                || mazeRenderer == null)
            {
                return;
            }

            var tokenRoot = new GameObject($"Hero Death Token {token.Id}");
            tokenRoot.transform.SetParent(root, false);
            var position = mazeRenderer.GridToWorld(token.Position);
            tokenRoot.transform.position = position;

            CreateCube(
                "Token Shadow",
                position + new Vector3(0f, mazeRenderer.CellSize * 0.012f, 0f),
                new Vector3(mazeRenderer.CellSize * 0.34f, mazeRenderer.CellSize * 0.025f, mazeRenderer.CellSize * 0.26f),
                shadowMaterial,
                tokenRoot.transform,
                token.Position);

            var body = CreatePrimitive(
                "Token Body",
                PrimitiveType.Cylinder,
                position + new Vector3(0f, mazeRenderer.CellSize * 0.08f, 0f),
                new Vector3(mazeRenderer.CellSize * 0.14f, mazeRenderer.CellSize * 0.04f, mazeRenderer.CellSize * 0.14f),
                tokenMaterial,
                tokenRoot.transform,
                token.Position);
            body.transform.rotation = Quaternion.Euler(0f, 30f, 0f);

            CreateCube(
                "Token Strap",
                position + new Vector3(0f, mazeRenderer.CellSize * 0.12f, 0f),
                new Vector3(mazeRenderer.CellSize * 0.08f, mazeRenderer.CellSize * 0.03f, mazeRenderer.CellSize * 0.3f),
                strapMaterial,
                tokenRoot.transform,
                token.Position);

            var hudTarget = tokenRoot.AddComponent<ObjectMicroHudTarget>();
            hudTarget.Configure(
                $"Жетон {token.FallenHeroName}",
                "память героя",
                "Жетон",
                token.Position,
                new Color(0.92f, 0.88f, 0.72f),
                () => token.IsAvailable ? "лежит в лабиринте" : token.State == HeroDeathTokenState.Carried ? "у рыцаря" : "прикреплен к дому",
                () => $"Вернуть к входу: жетон прикрепится к дому {token.FallenHeroName}, рыцарь-подборщик получит +{DeliveryExperienceReward} XP.");
            var collider = tokenRoot.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, mazeRenderer.CellSize * 0.12f, 0f);
            collider.size = new Vector3(mazeRenderer.CellSize * 0.5f, mazeRenderer.CellSize * 0.26f, mazeRenderer.CellSize * 0.42f);
            token.AttachVisual(tokenRoot);
        }

        private void AttachTokenToHouse(HeroDeathTokenModel token)
        {
            var house = GetHouse(token);
            if (house == null)
            {
                return;
            }

            if (houseTokenVisuals.TryGetValue(token.Id, out var existing) && existing != null)
            {
                Destroy(existing);
            }

            var unit = mazeRenderer != null ? mazeRenderer.CellSize : 1.65f;
            var plaqueIndex = CountDeliveredTokensForHeroUpTo(token);
            var plaqueColumn = plaqueIndex % 3;
            var plaqueRow = plaqueIndex / 3;
            var plaqueRoot = new GameObject($"Returned Token {token.Id}");
            plaqueRoot.transform.SetParent(house.transform, false);
            plaqueRoot.transform.localPosition = new Vector3(
                unit * (0.48f + plaqueColumn * 0.18f),
                unit * (1.08f + plaqueRow * 0.16f),
                unit * -0.32f);

            CreateCube(
                "House Token Plaque",
                plaqueRoot.transform.position,
                new Vector3(unit * 0.2f, unit * 0.16f, unit * 0.06f),
                tokenMaterial,
                plaqueRoot.transform,
                null);
            CreateCube(
                "House Token Strap",
                plaqueRoot.transform.position + new Vector3(0f, unit * 0.11f, 0f),
                new Vector3(unit * 0.05f, unit * 0.16f, unit * 0.07f),
                strapMaterial,
                plaqueRoot.transform,
                null);
            houseTokenVisuals[token.Id] = plaqueRoot;
        }

        private void ShowVengeanceProgress(HeroModel hero, Vector2Int position, HeroVengeanceProgressResult progress, float delay)
        {
            if (!progress.HasAnyFeedback || hero == null)
            {
                return;
            }

            if (progress.Completed)
            {
                DamageNumberView.CreateText(
                    mazeRenderer,
                    position,
                    progress.Message,
                    new Color(1f, 0.72f, 0.28f),
                    delay);
                delay += 0.3f;
            }

            if (progress.BonusExperience > 0)
            {
                DamageNumberView.CreateText(
                    mazeRenderer,
                    position,
                    $"+{progress.BonusExperience} XP клятвы",
                    new Color(0.55f, 0.86f, 1f),
                    delay);
            }
        }

        private bool TryGetCarriedToken(HeroModel hero, out HeroDeathTokenModel token)
        {
            token = null;
            if (hero == null || hero.Inventory == null)
            {
                return false;
            }

            if (carriedTokens.TryGetValue(hero, out token) && token != null)
            {
                return true;
            }

            if (!hero.Inventory.TryGetDeathTokenItemName(out var itemName))
            {
                return false;
            }

            token = FindTokenByItemName(itemName);
            if (token == null)
            {
                return false;
            }

            carriedTokens[hero] = token;
            return true;
        }

        private void RemoveCarriedReferences(HeroDeathTokenModel token)
        {
            var heroesToClear = new List<HeroModel>();
            foreach (var pair in carriedTokens)
            {
                if (pair.Value == token)
                {
                    heroesToClear.Add(pair.Key);
                }
            }

            for (var i = 0; i < heroesToClear.Count; i++)
            {
                var hero = heroesToClear[i];
                hero?.Inventory?.TryRemoveItem(token.ItemName);
                carriedTokens.Remove(hero);
            }
        }

        private HeroDeathTokenModel FindTokenByItemName(string itemName)
        {
            for (var i = 0; i < tokens.Count; i++)
            {
                if (tokens[i] != null && tokens[i].ItemName == itemName)
                {
                    return tokens[i];
                }
            }

            return null;
        }

        private int CountDeliveredTokensForHeroUpTo(HeroDeathTokenModel token)
        {
            var count = 0;
            for (var i = 0; i < tokens.Count; i++)
            {
                var candidate = tokens[i];
                if (candidate == null
                    || !candidate.IsDelivered
                    || candidate.HeroNumber != token.HeroNumber
                    || candidate.Id > token.Id)
                {
                    continue;
                }

                count++;
            }

            return Mathf.Max(0, count - 1);
        }

        private HeroDeathTokenModel FindAvailableAt(Vector2Int position)
        {
            for (var i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                if (token != null
                    && token.IsAvailable
                    && token.LevelNumber == result.LevelNumber
                    && token.Position == position)
                {
                    return token;
                }
            }

            return null;
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

        private BuildingView GetHouse(HeroDeathTokenModel token)
        {
            return token == null || heroHouseProvider == null
                ? null
                : heroHouseProvider.Invoke(token.HeroNumber);
        }

        private GameObject CreateCube(
            string objectName,
            Vector3 position,
            Vector3 scale,
            Material material,
            Transform parent,
            Vector2Int? trackedCell)
        {
            return CreatePrimitive(objectName, PrimitiveType.Cube, position, scale, material, parent, trackedCell);
        }

        private GameObject CreatePrimitive(
            string objectName,
            PrimitiveType primitiveType,
            Vector3 position,
            Vector3 scale,
            Material material,
            Transform parent,
            Vector2Int? trackedCell)
        {
            var primitive = GameObject.CreatePrimitive(primitiveType);
            primitive.name = objectName;
            primitive.transform.SetParent(parent, true);
            primitive.transform.position = position;
            primitive.transform.localScale = scale;
            primitive.GetComponent<Renderer>().sharedMaterial = material;
            var collider = primitive.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            if (trackedCell.HasValue)
            {
                mazeRenderer.TrackExternalCellRenderer(trackedCell.Value, primitive);
            }

            return primitive;
        }

        private void DestroyRootVisuals()
        {
            if (root != null)
            {
                Destroy(root.gameObject);
                root = null;
            }
        }

        private void EnsureMaterials()
        {
            if (tokenMaterial != null)
            {
                return;
            }

            tokenMaterial = CreateMaterial("Hero Death Token", new Color(0.86f, 0.8f, 0.58f));
            strapMaterial = CreateMaterial("Hero Death Token Strap", new Color(0.32f, 0.18f, 0.12f));
            shadowMaterial = CreateMaterial("Hero Death Token Shadow", new Color(0.09f, 0.06f, 0.04f, 0.55f));
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
    }
}
