using System.Collections.Generic;
using Labyrinth.Hero;
using Labyrinth.Maze;
using UnityEngine;

namespace Labyrinth.Core
{
    public sealed partial class GameBootstrap
    {
        private bool DropCarriedKey(HeroModel hero)
        {
            if (hero == null || hero.Inventory == null || currentMaze == null)
            {
                return false;
            }

            return DropCarriedKey(hero, HeroInventory.CentralRoomKeyItemName)
                || DropCarriedKey(hero, HeroInventory.DescentKeyItemName);
        }

        private bool DropCarriedKey(HeroModel hero, string itemName)
        {
            if (hero == null || hero.Inventory == null || !hero.Inventory.HasItem(itemName))
            {
                return false;
            }

            var inventorySlot = FormatCarryInventorySlot(hero.Inventory, itemName);
            hero.Inventory.TryRemoveItem(itemName);
            var dropPosition = FindCarryItemDropPosition(hero.Position);
            var key = currentMaze.GetOrCreateKeyPickup(dropPosition, itemName);
            mazeRenderer?.RenderKeyPickup(key);
            GameDebugLog.Info(
                "Hero",
                $"Hero {FormatCarryHero(hero)} dropped {itemName} from inventorySlot={inventorySlot} at {GameDebugLog.Position(dropPosition)} after death.");
            return true;
        }

        private Vector2Int FindCarryItemDropPosition(Vector2Int origin)
        {
            if (IsFreeCarryItemDropCell(origin))
            {
                return origin;
            }

            var queue = new Queue<Vector2Int>();
            var visited = new HashSet<Vector2Int> { origin };
            queue.Enqueue(origin);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var neighbor in currentMaze.Grid.WalkableNeighbors(current))
                {
                    if (!visited.Add(neighbor))
                    {
                        continue;
                    }

                    if (IsFreeCarryItemDropCell(neighbor))
                    {
                        return neighbor;
                    }

                    queue.Enqueue(neighbor);
                }
            }

            return origin;
        }

        private bool IsFreeCarryItemDropCell(Vector2Int position)
        {
            return currentMaze != null
                && currentMaze.Grid != null
                && currentMaze.Grid.InBounds(position)
                && currentMaze.Grid.Get(position).IsWalkable
                && !IsAvailableKeyAt(position);
        }

        private bool IsAvailableKeyAt(Vector2Int position)
        {
            if (currentMaze == null || currentMaze.KeyPickups == null)
            {
                return false;
            }

            for (var i = 0; i < currentMaze.KeyPickups.Count; i++)
            {
                var key = currentMaze.KeyPickups[i];
                if (key != null && key.IsAvailable && key.Position == position)
                {
                    return true;
                }
            }

            return false;
        }

        private static string FormatCarryHero(HeroModel hero)
        {
            if (hero == null)
            {
                return "unknown";
            }

            var name = string.IsNullOrWhiteSpace(hero.DisplayName) ? "unnamed" : hero.DisplayName;
            return hero.DisplayNumber > 0 ? $"#{hero.DisplayNumber} ({name})" : $"unassigned ({name})";
        }

        private static string FormatCarryInventorySlot(HeroInventory inventory, string itemName)
        {
            if (inventory == null)
            {
                return "none";
            }

            return inventory.TryFindItemSlot(itemName, out var slotIndex, out var slot)
                ? $"{slotIndex + 1}/{inventory.Slots.Count} {slot.Type} ({slot.Label})"
                : "missing";
        }
    }
}
