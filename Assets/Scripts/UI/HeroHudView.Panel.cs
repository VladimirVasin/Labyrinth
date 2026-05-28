using Labyrinth.Hero;
using UnityEngine;

namespace Labyrinth.UI
{
    public sealed partial class HeroHudView
    {
        private void DrawHeroPanel(HeroController hero, int heroNumber)
        {
            var rect = CalculatePanelRect();

            DrawPanel(rect);
            var title = BuildHeroTitle(hero, heroNumber);
            var contentX = rect.x + 18f;
            var contentWidth = rect.width - 36f;

            string hoveredTitle = null;
            string hoveredInfo = null;
            var hoveredRect = Rect.zero;

            var y = rect.y + 16f;
            var headerRect = new Rect(contentX, y, contentWidth, 92f);
            DrawHeroHeader(headerRect, title, hero);
            CaptureHover(headerRect, title, BuildHeroHeaderTooltip(hero, title), ref hoveredTitle, ref hoveredInfo, ref hoveredRect);
            y += 106f;

            DrawSection(new Rect(contentX, y, contentWidth, 18f), "Состояние");
            y += 24f;
            var hpRect = new Rect(contentX, y, contentWidth, 34f);
            DrawProgressBar(
                hpRect,
                "HP",
                hero.Model.HitPoints,
                hero.Model.MaxHitPoints,
                new Color(0.92f, 0.34f, 0.28f));
            CaptureHover(hpRect, "HP", BuildHealthTooltip(hero.Model), ref hoveredTitle, ref hoveredInfo, ref hoveredRect);
            y += 42f;

            var staminaRect = new Rect(contentX, y, contentWidth, 34f);
            DrawProgressBar(
                staminaRect,
                "Выносливость",
                hero.Model.Stamina,
                hero.Model.MaxStamina,
                new Color(0.34f, 0.72f, 1f));
            CaptureHover(staminaRect, "Выносливость", BuildStaminaTooltip(hero.Model), ref hoveredTitle, ref hoveredInfo, ref hoveredRect);
            y += 46f;

            var chipWidth = (contentWidth - 24f) / 3f;
            var goldRect = new Rect(contentX, y, chipWidth, 38f);
            var levelRect = new Rect(contentX + chipWidth + 12f, y, chipWidth, 38f);
            var trainingRect = new Rect(contentX + (chipWidth + 12f) * 2f, y, chipWidth, 38f);
            DrawInfoChip(goldRect, "Золото", hero.Model.Gold.ToString(), new Color(1f, 0.84f, 0.26f));
            DrawInfoChip(levelRect, "Уровень", hero.Model.Level.ToString(), new Color(0.72f, 1f, 0.42f));
            DrawInfoChip(trainingRect, "Выучка", $"{hero.Model.LineageTrainingScore}/{HeroLineageState.MaxTrainingScore}", new Color(0.96f, 0.74f, 0.28f));
            CaptureHover(goldRect, "Личное золото", BuildGoldTooltip(hero.Model), ref hoveredTitle, ref hoveredInfo, ref hoveredRect);
            CaptureHover(levelRect, "Уровень", BuildLevelTooltip(hero.Model), ref hoveredTitle, ref hoveredInfo, ref hoveredRect);
            CaptureHover(trainingRect, "Выучка", BuildTrainingTooltip(hero.Model), ref hoveredTitle, ref hoveredInfo, ref hoveredRect);
            y += 48f;

            var traitWidth = (contentWidth - 8f) * 0.5f;
            var blessingRect = new Rect(contentX, y, traitWidth, 50f);
            var vengeanceRect = new Rect(contentX + traitWidth + 8f, y, traitWidth, 50f);
            DrawTraitCard(blessingRect, "Благословение", hero.Model.BlessingText);
            DrawTraitCard(vengeanceRect, "Клятва мести", hero.Model.VengeanceText);
            BuildBlessingTooltip(hero.Model, out var blessingTitle, out var blessingInfo);
            BuildVengeanceTooltip(hero.Model, out var vengeanceTitle, out var vengeanceInfo);
            CaptureHover(blessingRect, blessingTitle, blessingInfo, ref hoveredTitle, ref hoveredInfo, ref hoveredRect);
            CaptureHover(vengeanceRect, vengeanceTitle, vengeanceInfo, ref hoveredTitle, ref hoveredInfo, ref hoveredRect);
            y += 62f;

            DrawSection(new Rect(contentX, y, contentWidth, 18f), "Боевые параметры");
            y += 24f;
            var attackRect = new Rect(contentX, y, chipWidth, 52f);
            var armorRect = new Rect(contentX + chipWidth + 12f, y, chipWidth, 52f);
            DrawCombatCard(attackRect, "Attack Points", hero.Model.AttackPoints.ToString(), new Color(0.98f, 0.76f, 0.34f));
            DrawCombatCard(armorRect, "Armor Points", hero.Model.ArmorPoints.ToString(), new Color(0.55f, 0.78f, 1f));
            CaptureHover(attackRect, "Attack Points", BuildAttackTooltip(hero.Model), ref hoveredTitle, ref hoveredInfo, ref hoveredRect);
            CaptureHover(armorRect, "Armor Points", BuildArmorTooltip(hero.Model), ref hoveredTitle, ref hoveredInfo, ref hoveredRect);
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

            var closeRect = new Rect(contentX, rect.yMax - 42f, contentWidth, 31f);
            if (GUI.Button(closeRect, "Закрыть", closeButtonStyle))
            {
                Hide();
            }
        }
    }
}
