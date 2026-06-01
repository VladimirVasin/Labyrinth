using Labyrinth.Core;
using Labyrinth.Hero;
using UnityEngine;

namespace Labyrinth.UI
{
    public sealed partial class HeroHudView
    {
        private void DrawHeroPanel(HeroController hero, int heroNumber)
        {
            var rect = panelTransition.AnimateRect(CalculatePanelRect());

            DrawPanel(rect);
            var title = BuildHeroTitle(hero, heroNumber);
            var contentX = rect.x + 18f;
            var contentWidth = rect.width - 36f;

            string hoveredTitle = null;
            string hoveredInfo = null;
            var hoveredRect = Rect.zero;

            var y = rect.y + 12f;
            var headerRect = new Rect(contentX, y, contentWidth, 92f);
            DrawHeroHeader(headerRect, title, hero);
            CaptureHover(headerRect, title, BuildHeroHeaderTooltip(hero, title), ref hoveredTitle, ref hoveredInfo, ref hoveredRect);
            y += 98f;

            DrawSection(new Rect(contentX, y, contentWidth, 18f), "Состояние");
            y += 20f;
            var hpRect = new Rect(contentX, y, contentWidth, 32f);
            DrawProgressBar(
                hpRect,
                "HP",
                hero.Model.HitPoints,
                hero.Model.MaxHitPoints,
                new Color(0.92f, 0.34f, 0.28f));
            CaptureHover(hpRect, "HP", BuildHealthTooltip(hero.Model), ref hoveredTitle, ref hoveredInfo, ref hoveredRect);
            y += 36f;

            var staminaRect = new Rect(contentX, y, contentWidth, 32f);
            DrawProgressBar(
                staminaRect,
                "Выносливость",
                hero.Model.Stamina,
                hero.Model.MaxStamina,
                new Color(0.34f, 0.72f, 1f));
            CaptureHover(staminaRect, "Выносливость", BuildStaminaTooltip(hero.Model), ref hoveredTitle, ref hoveredInfo, ref hoveredRect);
            y += 36f;

            var conditionRect = new Rect(contentX, y, contentWidth, 30f);
            DrawConditionLine(conditionRect, hero.Model);
            var conditionThird = contentWidth / 3f;
            var woundsRect = new Rect(contentX, y, conditionThird, 30f);
            var severeRect = new Rect(contentX + conditionThird, y, conditionThird, 30f);
            var scarRect = new Rect(contentX + conditionThird * 2f, y, conditionThird, 30f);
            CaptureHover(woundsRect, "Боевые раны", BuildWoundsTooltip(hero.Model), ref hoveredTitle, ref hoveredInfo, ref hoveredRect);
            CaptureHover(severeRect, "Тяжелая травма", BuildSevereInjuryTooltip(hero.Model), ref hoveredTitle, ref hoveredInfo, ref hoveredRect);
            CaptureHover(scarRect, "Личный шрам", BuildScarTooltip(hero.Model), ref hoveredTitle, ref hoveredInfo, ref hoveredRect);
            y += 38f;

            var chipWidth = (contentWidth - 12f) * 0.5f;
            var goldRect = new Rect(contentX, y, chipWidth, 38f);
            var trainingRect = new Rect(contentX + chipWidth + 12f, y, chipWidth, 38f);
            DrawInfoChip(goldRect, "Золото", hero.Model.Gold.ToString(), new Color(1f, 0.84f, 0.26f));
            DrawInfoChip(trainingRect, "Выучка", $"{hero.Model.LineageTrainingScore}/{HeroLineageState.MaxTrainingScore}", new Color(0.96f, 0.74f, 0.28f));
            CaptureHover(goldRect, "Личное золото", BuildGoldTooltip(hero.Model), ref hoveredTitle, ref hoveredInfo, ref hoveredRect);
            CaptureHover(trainingRect, "Выучка", BuildTrainingTooltip(hero.Model), ref hoveredTitle, ref hoveredInfo, ref hoveredRect);
            y += 44f;

            DrawSection(new Rect(contentX, y, contentWidth, 18f), "Наследие");
            y += 20f;
            var legacyRect = new Rect(contentX, y, contentWidth, 72f);
            DrawLegacySummary(legacyRect, hero.Model);
            var legacyLineHeight = legacyRect.height / 3f;
            var blessingRect = new Rect(contentX, y, contentWidth, legacyLineHeight);
            var vengeanceRect = new Rect(contentX, y + legacyLineHeight, contentWidth, legacyLineHeight);
            var characterRect = new Rect(contentX, y + legacyLineHeight * 2f, contentWidth, legacyLineHeight);
            BuildBlessingTooltip(hero.Model, out var blessingTitle, out var blessingInfo);
            BuildVengeanceTooltip(hero.Model, out var vengeanceTitle, out var vengeanceInfo);
            CaptureHover(blessingRect, blessingTitle, blessingInfo, ref hoveredTitle, ref hoveredInfo, ref hoveredRect);
            CaptureHover(vengeanceRect, vengeanceTitle, vengeanceInfo, ref hoveredTitle, ref hoveredInfo, ref hoveredRect);
            CaptureHover(characterRect, "Характер", BuildCharacterTraitTooltip(hero.Model), ref hoveredTitle, ref hoveredInfo, ref hoveredRect);
            y += 78f;

            var guildQuest = GetHeroGuildQuestInfo(hero);
            if (guildQuest.HasQuest)
            {
                DrawSection(new Rect(contentX, y, contentWidth, 18f), "Квест гильдии");
                y += 20f;
                var questRect = new Rect(contentX, y, contentWidth, 52f);
                DrawGuildQuestSummary(questRect, guildQuest);
                CaptureHover(questRect, "Квест гильдии", guildQuest.Tooltip, ref hoveredTitle, ref hoveredInfo, ref hoveredRect);
                y += 60f;
            }

            DrawSection(new Rect(contentX, y, contentWidth, 18f), "Боевые параметры");
            y += 20f;
            var combatRect = new Rect(contentX, y, contentWidth, 42f);
            DrawCombatSummary(combatRect, hero.Model);
            var attackRect = new Rect(contentX, y, contentWidth * 0.5f, 42f);
            var armorRect = new Rect(contentX + contentWidth * 0.5f, y, contentWidth * 0.5f, 42f);
            CaptureHover(attackRect, "Attack Points", BuildAttackTooltip(hero.Model), ref hoveredTitle, ref hoveredInfo, ref hoveredRect);
            CaptureHover(armorRect, "Armor Points", BuildArmorTooltip(hero.Model), ref hoveredTitle, ref hoveredInfo, ref hoveredRect);
            y += 50f;

            DrawSection(new Rect(contentX, y, contentWidth, 18f), "Инвентарь");
            y += 20f;

            var closeRect = new Rect(contentX, rect.yMax - 42f, contentWidth, 31f);
            var slots = hero.Model.Inventory.Slots;
            const float inventoryGap = 4f;
            const float slotHeight = 35f;
            var rowCount = Mathf.CeilToInt(slots.Count / 2f);
            var slotContentHeight = Mathf.Max(slotHeight, rowCount * (slotHeight + inventoryGap) - inventoryGap);
            var inventoryViewRect = new Rect(contentX, y, contentWidth, slotContentHeight);
            var slotWidth = (contentWidth - inventoryGap) * 0.5f;
            FillRect(inventoryViewRect, new Color(0f, 0f, 0f, 0.08f));
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
                CaptureHover(
                    slotRect,
                    BuildInventorySlotTooltipTitle(slot),
                    BuildInventorySlotTooltip(slot),
                    ref hoveredTitle,
                    ref hoveredInfo,
                    ref hoveredRect);
            }

            if (!string.IsNullOrEmpty(hoveredInfo))
            {
                DrawInventoryTooltip(hoveredRect, rect, hoveredTitle, hoveredInfo);
            }

            if (GUI.Button(closeRect, "Закрыть", closeButtonStyle))
            {
                Hide();
            }
        }

        private void DrawGuildQuestSummary(Rect rect, HeroGuildQuestHudInfo quest)
        {
            FillRect(rect, new Color(0.87f, 0.72f, 0.34f, 0.075f));
            DrawOutline(rect, new Color(0.87f, 0.72f, 0.34f, 0.22f));

            var innerX = rect.x + 10f;
            var innerWidth = rect.width - 20f;
            DrawFittedLabel(new Rect(innerX, rect.y + 4f, innerWidth, 18f), $"Зачистка: {quest.Target}", slotLabelStyle, 10, false);
            FillRect(new Rect(innerX, rect.y + 24f, innerWidth, 1f), new Color(0.87f, 0.72f, 0.34f, 0.18f));

            var gap = 8f;
            var cellWidth = (innerWidth - gap * 2f) / 3f;
            var rowY = rect.y + 28f;
            DrawQuestSummaryCell(new Rect(innerX, rowY, cellWidth, 19f), "Прогресс", quest.Progress, new Color(0.66f, 1f, 0.42f));
            DrawQuestSummaryCell(new Rect(innerX + cellWidth + gap, rowY, cellWidth, 19f), "Награда", quest.Reward, new Color(1f, 0.84f, 0.26f));
            DrawQuestSummaryCell(new Rect(innerX + (cellWidth + gap) * 2f, rowY, cellWidth, 19f), "Статус", quest.State, new Color(0.52f, 0.82f, 1f));
        }

        private void DrawQuestSummaryCell(Rect rect, string label, string value, Color valueColor)
        {
            FillRect(rect, new Color(0f, 0f, 0f, 0.12f));
            DrawOutline(rect, new Color(1f, 1f, 1f, 0.08f));

            var labelWidth = Mathf.Min(58f, rect.width * 0.56f);
            DrawFittedLabel(new Rect(rect.x + 5f, rect.y, labelWidth, rect.height), $"{label}:", chipLabelStyle, 10, false);
            var previousColor = GUI.color;
            GUI.color = new Color(valueColor.r, valueColor.g, valueColor.b, valueColor.a * previousColor.a);
            DrawFittedLabel(new Rect(rect.x + labelWidth + 7f, rect.y, rect.width - labelWidth - 12f, rect.height), value, slotItemStyle, 10, false);
            GUI.color = previousColor;
        }
    }
}
