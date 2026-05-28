using Labyrinth.Hero;
using UnityEngine;

namespace Labyrinth.UI
{
    public sealed partial class HeroHudView
    {
        private void DrawProgressIcon(Rect rect, string label)
        {
            if (label == "HP")
            {
                DrawHeartIcon(rect);
                return;
            }

            DrawStaminaIcon(rect);
        }

        private void DrawInfoChipIcon(Rect rect, string label)
        {
            if (label == "Золото")
            {
                DrawCoinIcon(rect);
                return;
            }

            if (label == "Уровень")
            {
                DrawLevelIcon(rect);
                return;
            }

            DrawTrainingIcon(rect);
        }

        private void DrawTraitIcon(Rect rect, string title)
        {
            if (title == "Клятва мести")
            {
                DrawVengeanceIcon(rect);
                return;
            }

            DrawBlessingIcon(rect);
        }

        private void DrawCombatIcon(Rect rect, string label)
        {
            if (label == "Armor Points")
            {
                DrawShieldIcon(rect, new Color(0.55f, 0.78f, 1f));
                return;
            }

            DrawSwordIcon(rect, new Color(0.98f, 0.76f, 0.34f));
        }

        private void DrawInventorySlotTypeIcon(Rect rect, HeroInventorySlotType type)
        {
            switch (type)
            {
                case HeroInventorySlotType.Weapon:
                    DrawSwordIcon(rect, new Color(0.96f, 0.78f, 0.38f));
                    break;
                case HeroInventorySlotType.Armor:
                    DrawShieldIcon(rect, new Color(0.58f, 0.75f, 0.96f));
                    break;
                case HeroInventorySlotType.Footwear:
                    DrawBootIcon(rect);
                    break;
                case HeroInventorySlotType.Potion:
                    DrawPotionIcon(rect);
                    break;
                case HeroInventorySlotType.Ration:
                    DrawRationIcon(rect);
                    break;
                case HeroInventorySlotType.Artifact:
                    DrawArtifactIcon(rect);
                    break;
                default:
                    DrawEmptySlotIcon(rect);
                    break;
            }
        }

        private void DrawHeartIcon(Rect rect)
        {
            var red = new Color(0.96f, 0.22f, 0.24f);
            DrawCircle(new Rect(rect.x, rect.y + rect.height * 0.12f, rect.width * 0.55f, rect.height * 0.55f), red);
            DrawCircle(new Rect(rect.x + rect.width * 0.45f, rect.y + rect.height * 0.12f, rect.width * 0.55f, rect.height * 0.55f), red);
            FillRect(new Rect(rect.x + rect.width * 0.2f, rect.y + rect.height * 0.42f, rect.width * 0.6f, rect.height * 0.46f), red);
            FillRect(new Rect(rect.x + rect.width * 0.32f, rect.y + rect.height * 0.76f, rect.width * 0.36f, rect.height * 0.16f), new Color(0.62f, 0.04f, 0.08f));
        }

        private static void DrawStaminaIcon(Rect rect)
        {
            var blue = new Color(0.34f, 0.72f, 1f);
            FillRect(new Rect(rect.x + rect.width * 0.42f, rect.y, rect.width * 0.22f, rect.height * 0.46f), blue);
            FillRect(new Rect(rect.x + rect.width * 0.24f, rect.y + rect.height * 0.38f, rect.width * 0.4f, rect.height * 0.2f), blue);
            FillRect(new Rect(rect.x + rect.width * 0.36f, rect.y + rect.height * 0.54f, rect.width * 0.22f, rect.height * 0.46f), new Color(0.1f, 0.52f, 0.92f));
            FillRect(new Rect(rect.x + rect.width * 0.52f, rect.y + rect.height * 0.44f, rect.width * 0.24f, rect.height * 0.2f), new Color(0.8f, 0.94f, 1f));
        }

        private void DrawCoinIcon(Rect rect)
        {
            DrawCircle(rect, new Color(1f, 0.74f, 0.18f));
            DrawCircle(new Rect(rect.x + rect.width * 0.18f, rect.y + rect.height * 0.18f, rect.width * 0.64f, rect.height * 0.64f), new Color(1f, 0.9f, 0.36f));
            FillRect(new Rect(rect.x + rect.width * 0.46f, rect.y + rect.height * 0.22f, rect.width * 0.08f, rect.height * 0.56f), new Color(0.6f, 0.34f, 0.04f));
        }

        private static void DrawLevelIcon(Rect rect)
        {
            var green = new Color(0.62f, 0.94f, 0.38f);
            FillRect(new Rect(rect.x + rect.width * 0.44f, rect.y + rect.height * 0.1f, rect.width * 0.12f, rect.height * 0.78f), green);
            FillRect(new Rect(rect.x + rect.width * 0.24f, rect.y + rect.height * 0.18f, rect.width * 0.52f, rect.height * 0.14f), green);
            FillRect(new Rect(rect.x + rect.width * 0.32f, rect.y + rect.height * 0.3f, rect.width * 0.36f, rect.height * 0.14f), green);
        }

        private static void DrawTrainingIcon(Rect rect)
        {
            var cover = new Color(0.84f, 0.56f, 0.28f);
            FillRect(new Rect(rect.x + rect.width * 0.08f, rect.y + rect.height * 0.16f, rect.width * 0.38f, rect.height * 0.72f), cover);
            FillRect(new Rect(rect.x + rect.width * 0.54f, rect.y + rect.height * 0.16f, rect.width * 0.38f, rect.height * 0.72f), cover);
            FillRect(new Rect(rect.x + rect.width * 0.46f, rect.y + rect.height * 0.14f, rect.width * 0.08f, rect.height * 0.76f), new Color(0.22f, 0.18f, 0.12f));
            FillRect(new Rect(rect.x + rect.width * 0.16f, rect.y + rect.height * 0.36f, rect.width * 0.24f, rect.height * 0.08f), new Color(1f, 0.88f, 0.48f));
        }

        private void DrawBlessingIcon(Rect rect)
        {
            DrawCircle(rect, new Color(0.98f, 0.78f, 0.28f));
            FillRect(new Rect(rect.x + rect.width * 0.45f, rect.y + rect.height * 0.18f, rect.width * 0.1f, rect.height * 0.64f), new Color(1f, 0.96f, 0.7f));
            FillRect(new Rect(rect.x + rect.width * 0.27f, rect.y + rect.height * 0.44f, rect.width * 0.46f, rect.height * 0.1f), new Color(1f, 0.96f, 0.7f));
        }

        private void DrawVengeanceIcon(Rect rect)
        {
            DrawCircle(rect, new Color(0.55f, 0.12f, 0.1f));
            DrawSwordIcon(new Rect(rect.x + rect.width * 0.14f, rect.y + rect.height * 0.12f, rect.width * 0.72f, rect.height * 0.76f), new Color(1f, 0.72f, 0.42f));
        }

        private static void DrawSwordIcon(Rect rect, Color bladeColor)
        {
            FillRect(new Rect(rect.x + rect.width * 0.46f, rect.y + rect.height * 0.04f, rect.width * 0.12f, rect.height * 0.62f), bladeColor);
            FillRect(new Rect(rect.x + rect.width * 0.36f, rect.y + rect.height * 0.6f, rect.width * 0.34f, rect.height * 0.12f), new Color(0.55f, 0.35f, 0.16f));
            FillRect(new Rect(rect.x + rect.width * 0.48f, rect.y + rect.height * 0.66f, rect.width * 0.08f, rect.height * 0.28f), new Color(0.28f, 0.16f, 0.08f));
            FillRect(new Rect(rect.x + rect.width * 0.42f, rect.y + rect.height * 0.9f, rect.width * 0.2f, rect.height * 0.08f), new Color(0.84f, 0.64f, 0.28f));
        }

        private void DrawShieldIcon(Rect rect, Color color)
        {
            DrawCircle(new Rect(rect.x + rect.width * 0.08f, rect.y + rect.height * 0.04f, rect.width * 0.84f, rect.height * 0.84f), color);
            FillRect(new Rect(rect.x + rect.width * 0.18f, rect.y + rect.height * 0.08f, rect.width * 0.64f, rect.height * 0.48f), color);
            FillRect(new Rect(rect.x + rect.width * 0.42f, rect.y + rect.height * 0.15f, rect.width * 0.14f, rect.height * 0.64f), new Color(0.88f, 0.94f, 1f, 0.82f));
            FillRect(new Rect(rect.x + rect.width * 0.24f, rect.y + rect.height * 0.34f, rect.width * 0.52f, rect.height * 0.12f), new Color(0.88f, 0.94f, 1f, 0.82f));
        }

        private void DrawArtifactIcon(Rect rect)
        {
            DrawCircle(new Rect(rect.x + rect.width * 0.08f, rect.y + rect.height * 0.08f, rect.width * 0.84f, rect.height * 0.84f), new Color(0.42f, 0.62f, 1f));
            DrawCircle(new Rect(rect.x + rect.width * 0.26f, rect.y + rect.height * 0.26f, rect.width * 0.48f, rect.height * 0.48f), new Color(0.88f, 0.96f, 1f));
        }

        private static void DrawEmptySlotIcon(Rect rect)
        {
            FillRect(new Rect(rect.x + rect.width * 0.2f, rect.y + rect.height * 0.2f, rect.width * 0.6f, rect.height * 0.1f), new Color(1f, 1f, 1f, 0.32f));
            FillRect(new Rect(rect.x + rect.width * 0.2f, rect.y + rect.height * 0.7f, rect.width * 0.6f, rect.height * 0.1f), new Color(1f, 1f, 1f, 0.32f));
            FillRect(new Rect(rect.x + rect.width * 0.2f, rect.y + rect.height * 0.2f, rect.width * 0.1f, rect.height * 0.6f), new Color(1f, 1f, 1f, 0.32f));
            FillRect(new Rect(rect.x + rect.width * 0.7f, rect.y + rect.height * 0.2f, rect.width * 0.1f, rect.height * 0.6f), new Color(1f, 1f, 1f, 0.32f));
        }

        private static bool IsWeaponItem(string itemName)
        {
            return itemName == HeroInventory.RustySwordItemName
                || itemName == HeroInventory.SteelSwordItemName
                || itemName == HeroInventory.KnightSwordItemName
                || itemName == HeroInventory.MasterBladeItemName;
        }

        private static bool IsArmorItem(string itemName)
        {
            return itemName == HeroInventory.CommonClothesItemName
                || itemName == HeroInventory.ChainmailItemName
                || itemName == HeroInventory.BrigandineItemName
                || itemName == HeroInventory.PlateHarnessItemName;
        }
    }
}
