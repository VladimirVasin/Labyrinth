using System;
using System.Collections.Generic;
using Labyrinth.Core;
using Labyrinth.Hero;
using UnityEngine;

namespace Labyrinth.UI
{
    public sealed class HeroHudView : MonoBehaviour
    {
        private const float HeroCardX = 16f;
        private const float HeroCardY = 76f;
        private const float HeroCardWidth = 164f;
        private const float HeroCardHeight = 104f;
        private const float HeroCardSpacing = 124f;
        private const float HeroPanelX = 202f;

        private Func<IReadOnlyList<HeroController>> heroesProvider;
        private Func<HeroController> selectedHeroProvider;
        private Action<HeroController> heroSelected;
        private bool panelVisible;
        private GUIStyle iconStyle;
        private GUIStyle iconCaptionStyle;
        private GUIStyle iconNameStyle;
        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle sectionStyle;
        private GUIStyle statLabelStyle;
        private GUIStyle statValueStyle;
        private GUIStyle headerNameStyle;
        private GUIStyle statusBadgeStyle;
        private GUIStyle barLabelStyle;
        private GUIStyle barValueStyle;
        private GUIStyle chipLabelStyle;
        private GUIStyle chipValueStyle;
        private GUIStyle blessingValueStyle;
        private GUIStyle combatValueStyle;
        private GUIStyle combatLabelStyle;
        private GUIStyle slotLabelStyle;
        private GUIStyle slotItemStyle;
        private GUIStyle emptySlotStyle;
        private GUIStyle tooltipTitleStyle;
        private GUIStyle tooltipBodyStyle;
        private GUIStyle closeButtonStyle;
        private Texture2D circleTexture;

        public bool IsVisible => panelVisible;

        public void Configure(
            Func<IReadOnlyList<HeroController>> onHeroesRequested,
            Func<HeroController> onSelectedHeroRequested,
            Action<HeroController> onHeroSelected)
        {
            heroesProvider = onHeroesRequested;
            selectedHeroProvider = onSelectedHeroRequested;
            heroSelected = onHeroSelected;
        }

        public void Hide()
        {
            if (panelVisible)
            {
                GameAudioController.PlayUi(GameSfx.HudClose);
            }

            panelVisible = false;
        }

        public void ShowSelectedPanel()
        {
            panelVisible = true;
        }

        public bool ContainsScreenPoint(Vector2 screenPosition)
        {
            var guiPoint = ToGuiPoint(screenPosition);
            if (ContainsHeroIconPoint(guiPoint))
            {
                return true;
            }

            return panelVisible
                && selectedHeroProvider != null
                && selectedHeroProvider.Invoke() != null
                && CalculatePanelRect().Contains(guiPoint);
        }

        private void OnGUI()
        {
            var heroes = heroesProvider != null ? heroesProvider.Invoke() : null;
            if (heroes == null || heroes.Count == 0)
            {
                panelVisible = false;
                return;
            }

            EnsureStyles();

            var selectedHero = selectedHeroProvider != null ? selectedHeroProvider.Invoke() : null;
            var drawnIcons = 0;

            for (var i = 0; i < heroes.Count; i++)
            {
                var hero = heroes[i];
                if (hero == null || hero.Model == null)
                {
                    continue;
                }

                var isSelected = selectedHero == hero;
                var iconRect = new Rect(
                    HeroCardX,
                    HeroCardY + drawnIcons * HeroCardSpacing,
                    HeroCardWidth,
                    HeroCardHeight);

                DrawSelectionFrame(iconRect, isSelected);
                if (GUI.Button(iconRect, GUIContent.none, iconStyle))
                {
                    if (isSelected && panelVisible)
                    {
                        GameAudioController.PlayUi(GameSfx.HudClose);
                        panelVisible = false;
                    }
                    else
                    {
                        GameAudioController.PlayUi(GameSfx.HudOpen);
                        heroSelected?.Invoke(hero);
                        panelVisible = true;
                    }
                }

                DrawHeroIconCard(iconRect, GetHeroDisplayNumber(hero, i + 1), hero, isSelected);
                drawnIcons++;
            }

            if (panelVisible && selectedHero != null && selectedHero.Model != null)
            {
                DrawHeroPanel(selectedHero, GetHeroDisplayNumber(selectedHero, GetHeroNumber(heroes, selectedHero)));
            }
        }

        private void DrawHeroPanel(HeroController hero, int heroNumber)
        {
            var rect = CalculatePanelRect();

            DrawPanel(rect);
            var title = heroNumber > 0 ? $"Рыцарь {heroNumber}" : "Рыцарь";
            var contentX = rect.x + 18f;
            var contentWidth = rect.width - 36f;

            var y = rect.y + 16f;
            DrawHeroHeader(new Rect(contentX, y, contentWidth, 92f), title, hero);
            y += 106f;

            DrawSection(new Rect(contentX, y, contentWidth, 18f), "Состояние");
            y += 24f;
            DrawProgressBar(
                new Rect(contentX, y, contentWidth, 34f),
                "HP",
                hero.Model.HitPoints,
                hero.Model.MaxHitPoints,
                new Color(0.92f, 0.34f, 0.28f));
            y += 42f;
            DrawProgressBar(
                new Rect(contentX, y, contentWidth, 34f),
                "Выносливость",
                hero.Model.Stamina,
                hero.Model.MaxStamina,
                new Color(0.34f, 0.72f, 1f));
            y += 46f;

            var chipWidth = (contentWidth - 12f) / 2f;
            DrawInfoChip(new Rect(contentX, y, chipWidth, 38f), "Золото", hero.Model.Gold.ToString(), new Color(1f, 0.84f, 0.26f));
            DrawInfoChip(new Rect(contentX + chipWidth + 12f, y, chipWidth, 38f), "Уровень", hero.Model.Level.ToString(), new Color(0.72f, 1f, 0.42f));
            y += 48f;

            string hoveredItemName = null;
            string hoveredInfo = null;
            var hoveredRect = Rect.zero;
            var blessingRect = new Rect(contentX, y, contentWidth, 50f);
            DrawBlessingCard(blessingRect, hero.Model.BlessingText);
            if (blessingRect.Contains(Event.current.mousePosition)
                && TryGetActiveBlessing(hero.Model, out var activeBlessing))
            {
                hoveredItemName = activeBlessing.DisplayName;
                hoveredInfo = activeBlessing.Description;
                hoveredRect = blessingRect;
            }

            y += 62f;

            DrawSection(new Rect(contentX, y, contentWidth, 18f), "Боевые параметры");
            y += 24f;
            DrawCombatCard(new Rect(contentX, y, chipWidth, 52f), "Attack Points", hero.Model.AttackPoints.ToString(), new Color(0.98f, 0.76f, 0.34f));
            DrawCombatCard(new Rect(contentX + chipWidth + 12f, y, chipWidth, 52f), "Armor Points", hero.Model.ArmorPoints.ToString(), new Color(0.55f, 0.78f, 1f));
            y += 66f;

            DrawSection(new Rect(contentX, y, contentWidth, 18f), "Инвентарь");
            y += 24f;

            var slots = hero.Model.Inventory.Slots;
            const float inventoryGap = 8f;
            var slotWidth = (contentWidth - inventoryGap) * 0.5f;
            const float slotHeight = 40f;
            for (var i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                var column = i % 2;
                var row = i / 2;
                var slotRect = new Rect(
                    contentX + column * (slotWidth + inventoryGap),
                    y + row * (slotHeight + inventoryGap),
                    slotWidth,
                    slotHeight);
                DrawInventorySlot(slotRect, slot);
                if (slotRect.Contains(Event.current.mousePosition) && !string.IsNullOrEmpty(slot.HoverInfo))
                {
                    hoveredItemName = slot.DisplayItem;
                    hoveredInfo = slot.HoverInfo;
                    hoveredRect = slotRect;
                }
            }

            if (!string.IsNullOrEmpty(hoveredInfo))
            {
                DrawInventoryTooltip(hoveredRect, rect, hoveredItemName, hoveredInfo);
            }

            var closeRect = new Rect(contentX, rect.yMax - 42f, contentWidth, 31f);
            if (GUI.Button(closeRect, "Закрыть", closeButtonStyle))
            {
                Hide();
            }
        }

        private Rect CalculatePanelRect()
        {
            var availableWidth = Mathf.Max(330f, Screen.width - HeroPanelX - 18f);
            var width = Mathf.Min(480f, availableWidth);
            var height = Mathf.Min(660f, Screen.height - 78f);
            return new Rect(HeroPanelX, 66f, width, height);
        }

        private bool ContainsHeroIconPoint(Vector2 guiPoint)
        {
            var heroes = heroesProvider != null ? heroesProvider.Invoke() : null;
            if (heroes == null)
            {
                return false;
            }

            var drawnIcons = 0;
            for (var i = 0; i < heroes.Count; i++)
            {
                var hero = heroes[i];
                if (hero == null || hero.Model == null)
                {
                    continue;
                }

                var iconRect = new Rect(
                    HeroCardX,
                    HeroCardY + drawnIcons * HeroCardSpacing,
                    HeroCardWidth,
                    HeroCardHeight);
                if (iconRect.Contains(guiPoint))
                {
                    return true;
                }

                drawnIcons++;
            }

            return false;
        }

        private static Vector2 ToGuiPoint(Vector2 screenPosition)
        {
            return new Vector2(screenPosition.x, Screen.height - screenPosition.y);
        }

        private static int GetHeroNumber(IReadOnlyList<HeroController> heroes, HeroController selectedHero)
        {
            for (var i = 0; i < heroes.Count; i++)
            {
                if (heroes[i] == selectedHero)
                {
                    return i + 1;
                }
            }

            return 0;
        }

        private static bool TryGetActiveBlessing(HeroModel model, out HeroBlessingDefinition definition)
        {
            if (model != null && model.Blessings != null)
            {
                foreach (var blessing in model.Blessings.Active)
                {
                    definition = HeroBlessingCatalog.Get(blessing);
                    return true;
                }
            }

            definition = default;
            return false;
        }

        private static int GetHeroDisplayNumber(HeroController hero, int fallbackNumber)
        {
            return hero != null && hero.DisplayNumber > 0 ? hero.DisplayNumber : fallbackNumber;
        }

        private void DrawSelectionFrame(Rect iconRect, bool isSelected)
        {
            if (!isSelected)
            {
                return;
            }

            var previousColor = GUI.color;
            GUI.color = new Color(1f, 0.86f, 0.24f);
            GUI.Box(new Rect(iconRect.x - 4f, iconRect.y - 4f, iconRect.width + 8f, iconRect.height + 8f), GUIContent.none);
            GUI.color = previousColor;
        }

        private void DrawHeroIconCard(Rect rect, int heroNumber, HeroController hero, bool isSelected)
        {
            FillRect(rect, isSelected ? new Color(0.18f, 0.16f, 0.12f, 0.96f) : new Color(0.1f, 0.095f, 0.09f, 0.92f));
            FillRect(new Rect(rect.x, rect.y, rect.width, 2f), isSelected ? new Color(0.96f, 0.74f, 0.28f) : new Color(1f, 1f, 1f, 0.12f));
            DrawOutline(rect, isSelected ? new Color(0.95f, 0.76f, 0.26f, 0.8f) : new Color(0f, 0f, 0f, 0.75f));

            DrawKnightIcon(new Rect(rect.x + 10f, rect.y + 14f, 40f, 44f), isSelected);
            var textX = rect.x + 58f;
            var textWidth = rect.width - 68f;
            GUI.Label(new Rect(textX, rect.y + 12f, textWidth, 22f), $"Рыцарь {heroNumber}", iconNameStyle);
            GUI.Label(new Rect(textX, rect.y + 36f, textWidth, 20f), GetStateShortText(hero), iconCaptionStyle);

            DrawIconStatBar(
                new Rect(rect.x + 10f, rect.y + rect.height - 26f, rect.width - 20f, 7f),
                hero.Model.Stamina,
                hero.Model.MaxStamina,
                new Color(0.34f, 0.72f, 1f));
            DrawIconStatBar(
                new Rect(rect.x + 10f, rect.y + rect.height - 15f, rect.width - 20f, 7f),
                hero.Model.HitPoints,
                hero.Model.MaxHitPoints,
                new Color(0.92f, 0.34f, 0.28f));
        }

        private void DrawKnightIcon(Rect rect, bool isSelected)
        {
            var metal = isSelected ? new Color(0.83f, 0.84f, 0.8f) : new Color(0.64f, 0.66f, 0.68f);
            var shadow = new Color(0.18f, 0.2f, 0.22f);
            var accent = isSelected ? new Color(0.96f, 0.74f, 0.28f) : new Color(0.45f, 0.58f, 0.68f);

            DrawCircle(new Rect(rect.x + 7f, rect.y + 1f, 18f, 18f), metal);
            FillRect(new Rect(rect.x + 9f, rect.y + 10f, 14f, 4f), shadow);
            FillRect(new Rect(rect.x + 13f, rect.y + 2f, 6f, 6f), new Color(0.94f, 0.94f, 0.9f));
            FillRect(new Rect(rect.x + 5f, rect.y + 20f, 22f, 12f), metal);
            FillRect(new Rect(rect.x + 9f, rect.y + 23f, 14f, 8f), accent);
            FillRect(new Rect(rect.x + 4f, rect.y + 18f, 4f, 13f), shadow);
            FillRect(new Rect(rect.x + 24f, rect.y + 18f, 4f, 13f), shadow);
        }

        private void DrawIconStatBar(Rect rect, int value, int maxValue, Color color)
        {
            FillRect(rect, new Color(0f, 0f, 0f, 0.45f));
            var normalized = maxValue > 0 ? Mathf.Clamp01((float)value / maxValue) : 0f;
            FillRect(new Rect(rect.x, rect.y, rect.width * normalized, rect.height), color);
        }

        private static string GetStateShortText(HeroController hero)
        {
            switch (hero.Model.State)
            {
                case HeroState.Exploring:
                case HeroState.SearchingKey:
                case HeroState.ReturningToDoor:
                case HeroState.OpeningDoor:
                    return "идет";
                case HeroState.ReturningToCastle:
                    return "домой";
                case HeroState.Stuck:
                    return "ждет";
                case HeroState.Defeated:
                    return "погиб";
                default:
                    return "герой";
            }
        }

        private static string BuildStateText(HeroState state)
        {
            switch (state)
            {
                case HeroState.Exploring:
                    return "исследует";
                case HeroState.SearchingKey:
                    return "ищет ключ";
                case HeroState.ReturningToDoor:
                    return "идет к двери";
                case HeroState.OpeningDoor:
                    return "открывает дверь";
                case HeroState.ReturningToCastle:
                    return "возвращается";
                case HeroState.Fighting:
                    return "сражается";
                case HeroState.Stuck:
                    return "ждет цель";
                case HeroState.Defeated:
                    return "побежден";
                default:
                    return "неизвестно";
            }
        }

        private static Color BuildStateColor(HeroState state)
        {
            switch (state)
            {
                case HeroState.Exploring:
                case HeroState.SearchingKey:
                    return new Color(0.62f, 0.88f, 0.58f);
                case HeroState.ReturningToDoor:
                case HeroState.OpeningDoor:
                    return new Color(0.98f, 0.86f, 0.24f);
                case HeroState.ReturningToCastle:
                    return new Color(0.45f, 0.78f, 1f);
                case HeroState.Fighting:
                    return new Color(1f, 0.6f, 0.3f);
                case HeroState.Stuck:
                    return new Color(0.98f, 0.86f, 0.24f);
                case HeroState.Defeated:
                    return new Color(0.8f, 0.28f, 0.24f);
                default:
                    return Color.white;
            }
        }

        private static void DrawPanel(Rect rect)
        {
            FillRect(rect, new Color(0.11f, 0.105f, 0.1f, 0.94f));
            FillRect(new Rect(rect.x, rect.y, rect.width, 3f), new Color(0.87f, 0.72f, 0.34f, 0.95f));
            DrawOutline(rect, new Color(0f, 0f, 0f, 0.75f));
        }

        private void DrawSection(Rect rect, string text)
        {
            GUI.Label(rect, text, sectionStyle);
            FillRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), new Color(1f, 1f, 1f, 0.1f));
        }

        private void DrawStatRow(Rect rect, string label, string value, Color valueColor)
        {
            FillRect(rect, new Color(1f, 1f, 1f, 0.045f));
            GUI.Label(new Rect(rect.x + 8f, rect.y, rect.width * 0.48f, rect.height), label, statLabelStyle);

            var previousColor = GUI.color;
            GUI.color = valueColor;
            GUI.Label(new Rect(rect.x + rect.width * 0.48f, rect.y, rect.width * 0.48f - 8f, rect.height), value, statValueStyle);
            GUI.color = previousColor;
        }

        private void DrawHeroHeader(Rect rect, string title, HeroController hero)
        {
            FillRect(rect, new Color(1f, 1f, 1f, 0.055f));
            DrawOutline(rect, new Color(1f, 1f, 1f, 0.11f));

            DrawKnightIcon(new Rect(rect.x + 12f, rect.y + 16f, 48f, 52f), true);
            var textX = rect.x + 74f;
            var statusWidth = Mathf.Min(132f, rect.width * 0.32f);
            GUI.Label(new Rect(textX, rect.y + 10f, rect.width - statusWidth - 90f, 28f), title, headerNameStyle);
            GUI.Label(new Rect(textX, rect.y + 37f, rect.width - 96f, 18f), "герой-исследователь", subtitleStyle);
            DrawStatusBadge(
                new Rect(rect.xMax - statusWidth - 12f, rect.y + 14f, statusWidth, 26f),
                BuildStateText(hero.Model.State),
                BuildStateColor(hero.Model.State));
            DrawHeaderXpBar(new Rect(textX, rect.y + 63f, rect.width - 92f, 15f), hero.Model.Experience, hero.Model.ExperienceForNextLevel);
        }

        private void DrawBlessingCard(Rect rect, string blessingText)
        {
            FillRect(rect, new Color(0.87f, 0.72f, 0.34f, 0.07f));
            DrawOutline(rect, new Color(0.87f, 0.72f, 0.34f, 0.18f));
            GUI.Label(new Rect(rect.x + 10f, rect.y + 4f, rect.width - 20f, 14f), "Благословение", chipLabelStyle);
            GUI.Label(new Rect(rect.x + 10f, rect.y + 18f, rect.width - 20f, 28f), string.IsNullOrEmpty(blessingText) ? "нет" : blessingText, blessingValueStyle);
        }

        private void DrawStatusBadge(Rect rect, string text, Color color)
        {
            FillRect(rect, new Color(color.r, color.g, color.b, 0.18f));
            DrawOutline(rect, new Color(color.r, color.g, color.b, 0.55f));
            var previousColor = GUI.color;
            GUI.color = color;
            GUI.Label(rect, text, statusBadgeStyle);
            GUI.color = previousColor;
        }

        private void DrawHeaderXpBar(Rect rect, int value, int maxValue)
        {
            FillRect(rect, new Color(0f, 0f, 0f, 0.34f));
            var normalized = maxValue > 0 ? Mathf.Clamp01((float)value / maxValue) : 0f;
            FillRect(new Rect(rect.x, rect.y, rect.width * normalized, rect.height), new Color(0.18f, 0.72f, 0.96f, 0.82f));
            GUI.Label(rect, $"XP {value} / {maxValue}", barValueStyle);
        }

        private void DrawProgressBar(Rect rect, string label, int value, int maxValue, Color color)
        {
            FillRect(rect, new Color(1f, 1f, 1f, 0.045f));
            DrawOutline(rect, new Color(1f, 1f, 1f, 0.08f));
            GUI.Label(new Rect(rect.x + 10f, rect.y + 2f, rect.width - 20f, 16f), label, barLabelStyle);

            var barRect = new Rect(rect.x + 10f, rect.y + 21f, rect.width - 20f, 8f);
            FillRect(barRect, new Color(0f, 0f, 0f, 0.45f));
            var normalized = maxValue > 0 ? Mathf.Clamp01((float)value / maxValue) : 0f;
            FillRect(new Rect(barRect.x, barRect.y, barRect.width * normalized, barRect.height), color);
            GUI.Label(new Rect(rect.x, rect.y + 1f, rect.width - 12f, 18f), $"{value} / {maxValue}", barValueStyle);
        }

        private void DrawInfoChip(Rect rect, string label, string value, Color valueColor)
        {
            FillRect(rect, new Color(1f, 1f, 1f, 0.055f));
            DrawOutline(rect, new Color(1f, 1f, 1f, 0.1f));
            GUI.Label(new Rect(rect.x + 10f, rect.y + 4f, rect.width - 20f, 14f), label, chipLabelStyle);
            var previousColor = GUI.color;
            GUI.color = valueColor;
            GUI.Label(new Rect(rect.x + 10f, rect.y + 17f, rect.width - 20f, 18f), value, chipValueStyle);
            GUI.color = previousColor;
        }

        private void DrawCombatCard(Rect rect, string label, string value, Color valueColor)
        {
            FillRect(rect, new Color(1f, 1f, 1f, 0.055f));
            DrawOutline(rect, new Color(1f, 1f, 1f, 0.1f));
            var previousColor = GUI.color;
            GUI.color = valueColor;
            GUI.Label(new Rect(rect.x + 10f, rect.y + 7f, rect.width - 20f, 24f), value, combatValueStyle);
            GUI.color = previousColor;
            GUI.Label(new Rect(rect.x + 10f, rect.y + 29f, rect.width - 20f, 18f), label, combatLabelStyle);
        }

        private void DrawInventorySlot(Rect rect, HeroInventorySlot slot)
        {
            FillRect(rect, slot.HasItem ? new Color(0.18f, 0.17f, 0.15f, 0.95f) : new Color(0.08f, 0.08f, 0.08f, 0.7f));
            DrawOutline(rect, slot.HasItem ? new Color(0.75f, 0.63f, 0.36f, 0.45f) : new Color(1f, 1f, 1f, 0.16f));

            var labelWidth = rect.width * 0.42f;
            GUI.Label(new Rect(rect.x + 9f, rect.y, labelWidth - 9f, rect.height), slot.Label, slotLabelStyle);

            var itemX = rect.x + labelWidth;
            if (slot.HasItem && DrawInventoryItemIcon(new Rect(itemX + 4f, rect.y + 4f, 19f, 19f), slot.ItemName))
            {
                itemX += 28f;
            }

            GUI.Label(
                new Rect(itemX, rect.y, rect.xMax - itemX - 8f, rect.height),
                slot.DisplayItem,
                slot.HasItem ? slotItemStyle : emptySlotStyle);
        }

        private bool DrawInventoryItemIcon(Rect rect, string itemName)
        {
            if (itemName == HeroInventory.HealthPotionItemName)
            {
                DrawPotionIcon(rect);
                return true;
            }

            if (itemName == HeroInventory.RationItemName)
            {
                DrawRationIcon(rect);
                return true;
            }

            if (itemName == HeroInventory.GoldIngotItemName)
            {
                DrawGoldIngotIcon(rect);
                return true;
            }

            if (IsFootwearItem(itemName))
            {
                DrawBootIcon(rect);
                return true;
            }

            return false;
        }

        private static bool IsFootwearItem(string itemName)
        {
            return itemName == HeroInventory.SandalsItemName
                || itemName == HeroInventory.LeatherBootsItemName
                || itemName == HeroInventory.PathfinderBootsItemName
                || itemName == HeroInventory.SwiftwalkerBootsItemName;
        }

        private void DrawPotionIcon(Rect rect)
        {
            FillRect(new Rect(rect.x + rect.width * 0.38f, rect.y, rect.width * 0.24f, rect.height * 0.32f), new Color(0.84f, 0.92f, 1f));
            DrawCircle(new Rect(rect.x + rect.width * 0.18f, rect.y + rect.height * 0.26f, rect.width * 0.64f, rect.height * 0.64f), new Color(0.18f, 0.86f, 0.94f));
            FillRect(new Rect(rect.x + rect.width * 0.28f, rect.y + rect.height * 0.55f, rect.width * 0.44f, rect.height * 0.12f), new Color(0.95f, 0.22f, 0.28f));
        }

        private void DrawRationIcon(Rect rect)
        {
            DrawCircle(new Rect(rect.x + rect.width * 0.1f, rect.y + rect.height * 0.16f, rect.width * 0.8f, rect.height * 0.68f), new Color(0.86f, 0.58f, 0.25f));
            FillRect(new Rect(rect.x + rect.width * 0.2f, rect.y + rect.height * 0.58f, rect.width * 0.6f, rect.height * 0.14f), new Color(0.58f, 0.34f, 0.12f));
            FillRect(new Rect(rect.x + rect.width * 0.36f, rect.y + rect.height * 0.28f, rect.width * 0.1f, rect.height * 0.24f), new Color(1f, 0.78f, 0.38f));
        }

        private static void DrawGoldIngotIcon(Rect rect)
        {
            FillRect(new Rect(rect.x + rect.width * 0.14f, rect.y + rect.height * 0.34f, rect.width * 0.72f, rect.height * 0.42f), new Color(1f, 0.68f, 0.15f));
            FillRect(new Rect(rect.x + rect.width * 0.24f, rect.y + rect.height * 0.22f, rect.width * 0.52f, rect.height * 0.18f), new Color(1f, 0.84f, 0.3f));
            FillRect(new Rect(rect.x + rect.width * 0.18f, rect.y + rect.height * 0.66f, rect.width * 0.64f, rect.height * 0.08f), new Color(0.62f, 0.34f, 0.05f));
        }

        private static void DrawBootIcon(Rect rect)
        {
            var leather = new Color(0.58f, 0.34f, 0.16f);
            var sole = new Color(0.18f, 0.12f, 0.08f);
            FillRect(new Rect(rect.x + rect.width * 0.25f, rect.y + rect.height * 0.12f, rect.width * 0.36f, rect.height * 0.58f), leather);
            FillRect(new Rect(rect.x + rect.width * 0.45f, rect.y + rect.height * 0.52f, rect.width * 0.42f, rect.height * 0.24f), leather);
            FillRect(new Rect(rect.x + rect.width * 0.2f, rect.y + rect.height * 0.74f, rect.width * 0.7f, rect.height * 0.12f), sole);
            FillRect(new Rect(rect.x + rect.width * 0.32f, rect.y + rect.height * 0.22f, rect.width * 0.22f, rect.height * 0.08f), new Color(0.86f, 0.64f, 0.32f));
        }

        private void DrawInventoryTooltip(Rect sourceRect, Rect panelRect, string itemName, string info)
        {
            const float tooltipWidth = 260f;
            const float gap = 18f;
            var bodyHeight = Mathf.Clamp(tooltipBodyStyle.CalcHeight(new GUIContent(info), tooltipWidth - 20f), 22f, 110f);
            var tooltipHeight = Mathf.Clamp(bodyHeight + 43f, 66f, 150f);
            var showOnRight = true;
            var x = sourceRect.xMax + gap;
            if (x + tooltipWidth > Screen.width - 10f)
            {
                x = Mathf.Max(10f, sourceRect.x - tooltipWidth - gap);
                showOnRight = false;
            }

            var y = Mathf.Clamp(sourceRect.center.y - tooltipHeight * 0.5f, 10f, Screen.height - tooltipHeight - 10f);
            var rect = new Rect(x, y, tooltipWidth, tooltipHeight);
            DrawInventoryTooltipConnector(sourceRect, rect, showOnRight);
            FillRect(rect, new Color(0.07f, 0.065f, 0.055f, 0.98f));
            FillRect(new Rect(rect.x, rect.y, rect.width, 2f), new Color(0.92f, 0.72f, 0.28f, 0.95f));
            DrawOutline(rect, new Color(0f, 0f, 0f, 0.85f));
            GUI.Label(new Rect(rect.x + 10f, rect.y + 7f, rect.width - 20f, 22f), itemName, tooltipTitleStyle);
            GUI.Label(new Rect(rect.x + 10f, rect.y + 32f, rect.width - 20f, bodyHeight), info, tooltipBodyStyle);
        }

        private static void DrawInventoryTooltipConnector(Rect sourceRect, Rect tooltipRect, bool showOnRight)
        {
            var color = new Color(0.92f, 0.72f, 0.28f, 0.92f);
            var y = sourceRect.center.y;
            if (showOnRight)
            {
                var startX = sourceRect.xMax + 2f;
                var endX = tooltipRect.x - 2f;
                FillRect(new Rect(startX, y - 1f, Mathf.Max(1f, endX - startX), 2f), color);
                FillRect(new Rect(tooltipRect.x - 7f, y - 4f, 5f, 2f), color);
                FillRect(new Rect(tooltipRect.x - 9f, y - 1f, 7f, 2f), color);
                FillRect(new Rect(tooltipRect.x - 7f, y + 2f, 5f, 2f), color);
                return;
            }

            var leftStartX = tooltipRect.xMax + 2f;
            var leftEndX = sourceRect.x - 2f;
            FillRect(new Rect(leftStartX, y - 1f, Mathf.Max(1f, leftEndX - leftStartX), 2f), color);
            FillRect(new Rect(tooltipRect.xMax + 2f, y - 4f, 5f, 2f), color);
            FillRect(new Rect(tooltipRect.xMax + 2f, y - 1f, 7f, 2f), color);
            FillRect(new Rect(tooltipRect.xMax + 2f, y + 2f, 5f, 2f), color);
        }

        private static void DrawOutline(Rect rect, Color color)
        {
            FillRect(new Rect(rect.x, rect.y, rect.width, 1f), color);
            FillRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), color);
            FillRect(new Rect(rect.x, rect.y, 1f, rect.height), color);
            FillRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), color);
        }

        private static void FillRect(Rect rect, Color color)
        {
            var previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previousColor;
        }

        private void EnsureStyles()
        {
            if (iconStyle != null)
            {
                return;
            }

            iconStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 1,
                fontStyle = FontStyle.Normal
            };
            iconStyle.normal.textColor = Color.clear;
            iconStyle.hover.textColor = Color.clear;
            iconStyle.active.textColor = Color.clear;
            iconCaptionStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };
            iconCaptionStyle.normal.textColor = new Color(0.86f, 0.86f, 0.82f);
            iconNameStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 15,
                fontStyle = FontStyle.Bold
            };
            iconNameStyle.normal.textColor = new Color(0.96f, 0.92f, 0.82f);
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 25,
                fontStyle = FontStyle.Bold
            };
            titleStyle.normal.textColor = new Color(0.96f, 0.93f, 0.86f);
            subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 13,
                fontStyle = FontStyle.Italic
            };
            subtitleStyle.normal.textColor = new Color(0.72f, 0.7f, 0.64f);
            headerNameStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 23,
                fontStyle = FontStyle.Bold
            };
            headerNameStyle.normal.textColor = new Color(0.96f, 0.93f, 0.86f);
            statusBadgeStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };
            statusBadgeStyle.normal.textColor = Color.white;
            barLabelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };
            barLabelStyle.normal.textColor = new Color(0.84f, 0.82f, 0.76f);
            barValueStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleRight,
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
            barValueStyle.normal.textColor = new Color(0.95f, 0.95f, 0.9f);
            chipLabelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
            chipLabelStyle.normal.textColor = new Color(0.72f, 0.7f, 0.64f);
            chipValueStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleRight,
                fontSize = 17,
                fontStyle = FontStyle.Bold
            };
            chipValueStyle.normal.textColor = Color.white;
            blessingValueStyle = new GUIStyle(chipValueStyle)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = 13,
                wordWrap = true
            };
            blessingValueStyle.normal.textColor = new Color(0.96f, 0.93f, 0.84f);
            combatValueStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 22,
                fontStyle = FontStyle.Bold
            };
            combatValueStyle.normal.textColor = Color.white;
            combatLabelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
            combatLabelStyle.normal.textColor = new Color(0.76f, 0.75f, 0.7f);
            sectionStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };
            sectionStyle.normal.textColor = new Color(0.88f, 0.76f, 0.47f);
            statLabelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };
            statLabelStyle.normal.textColor = new Color(0.73f, 0.72f, 0.68f);
            statValueStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleRight,
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };
            statValueStyle.normal.textColor = Color.white;
            slotLabelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };
            slotLabelStyle.normal.textColor = new Color(0.95f, 0.89f, 0.72f);
            slotItemStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleRight,
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };
            slotItemStyle.normal.textColor = Color.white;
            emptySlotStyle = new GUIStyle(slotItemStyle);
            emptySlotStyle.normal.textColor = new Color(0.65f, 0.65f, 0.62f);
            tooltipTitleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };
            tooltipTitleStyle.normal.textColor = new Color(1f, 0.9f, 0.64f);
            tooltipBodyStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };
            tooltipBodyStyle.normal.textColor = new Color(0.86f, 0.86f, 0.8f);
            closeButtonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };
            circleTexture = CreateCircleTexture();
        }

        private void DrawCircle(Rect rect, Color color)
        {
            var previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, circleTexture);
            GUI.color = previousColor;
        }

        private static Texture2D CreateCircleTexture()
        {
            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear
            };

            var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            var radius = size * 0.47f;
            var pixels = new Color[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var distance = Vector2.Distance(new Vector2(x, y), center);
                    pixels[y * size + x] = distance <= radius ? Color.white : Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }
    }
}
