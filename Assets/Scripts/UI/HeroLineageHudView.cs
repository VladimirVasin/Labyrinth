using System;
using Labyrinth.Core;
using Labyrinth.Hero;
using UnityEngine;

namespace Labyrinth.UI
{
    public sealed class HeroLineageHudView : MonoBehaviour
    {
        private HeroLineageState selectedLineage;
        private Func<int, HeroController> activeHeroProvider;
        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle labelStyle;
        private GUIStyle valueStyle;
        private GUIStyle bodyStyle;
        private GUIStyle closeButtonStyle;
        private Vector2 scrollPosition;

        public bool IsVisible => selectedLineage != null;

        public void Configure(Func<int, HeroController> getActiveHero)
        {
            activeHeroProvider = getActiveHero;
        }

        public void Show(HeroLineageState lineage)
        {
            if (lineage == null)
            {
                return;
            }

            if (selectedLineage != lineage)
            {
                scrollPosition = Vector2.zero;
                GameAudioController.PlayUi(GameSfx.HudOpen);
            }

            selectedLineage = lineage;
        }

        public void Hide()
        {
            if (selectedLineage != null)
            {
                GameAudioController.PlayUi(GameSfx.HudClose);
            }

            selectedLineage = null;
            scrollPosition = Vector2.zero;
        }

        public bool ContainsScreenPoint(Vector2 screenPosition)
        {
            return selectedLineage != null && CalculatePanelRect().Contains(ToGuiPoint(screenPosition));
        }

        private void OnGUI()
        {
            if (selectedLineage == null)
            {
                return;
            }

            EnsureStyles();
            var rect = CalculatePanelRect();
            DrawPanel(rect);

            var contentX = rect.x + 20f;
            var contentWidth = rect.width - 40f;
            var y = rect.y + 18f;
            GUI.Label(new Rect(contentX, y, contentWidth, 34f), "Родословная", titleStyle);
            y += 34f;
            GUI.Label(new Rect(contentX, y, contentWidth, 22f), selectedLineage.BaseName, subtitleStyle);
            y += 34f;

            DrawSummary(new Rect(contentX, y, contentWidth, 84f));
            y += 98f;

            var viewRect = new Rect(contentX, y, contentWidth, rect.yMax - y - 56f);
            var members = selectedLineage.Members;
            const float memberCardHeight = 136f;
            const float memberStep = 150f;
            var contentHeight = Mathf.Max(viewRect.height - 4f, members.Count * memberStep + 18f);
            scrollPosition = GUI.BeginScrollView(viewRect, scrollPosition, new Rect(0f, 0f, viewRect.width - 18f, contentHeight));
            var localY = 8f;
            for (var i = 0; i < members.Count; i++)
            {
                DrawMemberCard(new Rect(8f, localY, viewRect.width - 34f, memberCardHeight), members[i]);
                localY += memberStep;
                if (i < members.Count - 1)
                {
                    DrawConnector(new Rect(viewRect.width * 0.5f - 1f, localY - 10f, 2f, 20f));
                }
            }

            GUI.EndScrollView();

            if (GUI.Button(new Rect(contentX, rect.yMax - 40f, contentWidth, 30f), "Закрыть", closeButtonStyle))
            {
                Hide();
            }
        }

        private void DrawSummary(Rect rect)
        {
            FillRect(rect, new Color(1f, 1f, 1f, 0.055f));
            DrawOutline(rect, new Color(1f, 1f, 1f, 0.1f));
            DrawStat(new Rect(rect.x + 12f, rect.y + 6f, rect.width * 0.32f, 20f), "Дом", $"#{selectedLineage.HeroNumber}");
            DrawStat(new Rect(rect.x + rect.width * 0.36f, rect.y + 6f, rect.width * 0.28f, 20f), "Поколение", selectedLineage.Generation.ToString());
            DrawStat(new Rect(rect.x + rect.width * 0.68f, rect.y + 6f, rect.width * 0.28f, 20f), "Потери", selectedLineage.DeathsCount.ToString());
            DrawStat(new Rect(rect.x + 12f, rect.y + 30f, rect.width * 0.42f, 20f), "Фонд", $"{selectedLineage.HouseFundGold} зол.");
            DrawStat(new Rect(rect.x + rect.width * 0.48f, rect.y + 30f, rect.width * 0.48f, 20f), "Всего внесено", $"{selectedLineage.TotalContributedGold} зол.");
            DrawStat(new Rect(rect.x + 12f, rect.y + 54f, rect.width * 0.32f, 20f), "Выучка", $"{selectedLineage.TrainingScore}/{HeroLineageState.MaxTrainingScore}");
            DrawStat(new Rect(rect.x + rect.width * 0.36f, rect.y + 54f, rect.width * 0.6f, 20f), "Бонус наследника", selectedLineage.TrainingBonus.ToCompactText());
        }

        private void DrawStat(Rect rect, string label, string value)
        {
            GUI.Label(new Rect(rect.x, rect.y, rect.width * 0.55f, rect.height), label, labelStyle);
            GUI.Label(new Rect(rect.x + rect.width * 0.55f, rect.y, rect.width * 0.45f, rect.height), value, valueStyle);
        }

        private void DrawMemberCard(Rect rect, HeroLineageMember member)
        {
            var activeHero = activeHeroProvider != null ? activeHeroProvider.Invoke(member.HeroNumber) : null;
            var isCurrentActive = activeHero != null
                && member.Generation == selectedLineage.Generation
                && member.Status == HeroLineageMemberStatus.Alive;
            var background = isCurrentActive
                ? new Color(0.22f, 0.2f, 0.12f, 0.95f)
                : member.Status == HeroLineageMemberStatus.Dead
                    ? new Color(0.11f, 0.1f, 0.095f, 0.95f)
                    : new Color(0.14f, 0.14f, 0.12f, 0.95f);
            FillRect(rect, background);
            FillRect(new Rect(rect.x, rect.y, 4f, rect.height), isCurrentActive ? new Color(0.95f, 0.77f, 0.28f) : new Color(0.45f, 0.4f, 0.3f));
            DrawOutline(rect, isCurrentActive ? new Color(0.95f, 0.77f, 0.28f, 0.62f) : new Color(1f, 1f, 1f, 0.11f));

            GUI.Label(new Rect(rect.x + 14f, rect.y + 6f, rect.width - 28f, 24f), member.DisplayName, bodyStyle);
            GUI.Label(new Rect(rect.x + 14f, rect.y + 31f, rect.width * 0.36f, 20f), $"Поколение {member.Generation}", labelStyle);
            GUI.Label(new Rect(rect.x + rect.width * 0.4f, rect.y + 31f, rect.width * 0.56f, 20f), BuildStatusText(member, isCurrentActive), valueStyle);

            if (member.Status == HeroLineageMemberStatus.Dead)
            {
                GUI.Label(new Rect(rect.x + 14f, rect.y + 55f, rect.width - 28f, 18f), $"Пал: {BuildDeathCauseText(member)}, ур. {member.LevelAtDeath}, XP {member.ExperienceAtDeath}", labelStyle);
                GUI.Label(new Rect(rect.x + 14f, rect.y + 74f, rect.width - 28f, 18f), $"{BuildTokenText(member)}; вклад {member.ContributedGold} зол.", labelStyle);
                GUI.Label(new Rect(rect.x + 14f, rect.y + 93f, rect.width - 28f, 18f), BuildLegacyMarksText(member), labelStyle);
                GUI.Label(new Rect(rect.x + 14f, rect.y + 112f, rect.width - 28f, 18f), BuildVengeanceText(member), labelStyle);
                return;
            }

            if (isCurrentActive && activeHero.Model != null)
            {
                GUI.Label(new Rect(rect.x + 14f, rect.y + 55f, rect.width - 28f, 18f), $"Ур. {activeHero.Model.Level}, XP {activeHero.Model.Experience}/{activeHero.Model.ExperienceForNextLevel}", labelStyle);
                GUI.Label(new Rect(rect.x + 14f, rect.y + 74f, rect.width - 28f, 18f), $"HP {activeHero.Model.HitPoints}/{activeHero.Model.MaxHitPoints}, выносл. {activeHero.Model.Stamina}/{activeHero.Model.MaxStamina}, вклад {member.ContributedGold} зол.", labelStyle);
                GUI.Label(new Rect(rect.x + 14f, rect.y + 93f, rect.width - 28f, 18f), BuildActiveMarksText(activeHero.Model), labelStyle);
                GUI.Label(new Rect(rect.x + 14f, rect.y + 112f, rect.width - 28f, 18f), BuildVengeanceText(member), labelStyle);
            }
            else
            {
                GUI.Label(new Rect(rect.x + 14f, rect.y + 62f, rect.width - 28f, 20f), "Наследник ещё не призван", labelStyle);
                GUI.Label(new Rect(rect.x + 14f, rect.y + 86f, rect.width - 28f, 20f), BuildLegacyMarksText(member), labelStyle);
                GUI.Label(new Rect(rect.x + 14f, rect.y + 110f, rect.width - 28f, 20f), BuildVengeanceText(member), labelStyle);
            }
        }

        private static string BuildStatusText(HeroLineageMember member, bool isCurrentActive)
        {
            if (isCurrentActive)
            {
                return "текущий наследник";
            }

            return member.Status == HeroLineageMemberStatus.Dead ? "погиб" : "ожидает призыва";
        }

        private static string BuildTokenText(HeroLineageMember member)
        {
            if (!member.HasDeathToken)
            {
                return "Жетон: не создан";
            }

            return member.IsDeathTokenReturned
                ? $"Жетон #{member.DeathTokenId}: возвращён домой"
                : $"Жетон #{member.DeathTokenId}: не возвращён";
        }

        private static string BuildDeathCauseText(HeroLineageMember member)
        {
            return member.HasDeathContext ? member.DeathContext.CauseText : "причина неизвестна";
        }

        private static string BuildVengeanceText(HeroLineageMember member)
        {
            var quest = member.VengeanceQuest;
            return quest == null || !quest.IsActive ? "Клятва: нет" : quest.SummaryText;
        }

        private static string BuildLegacyMarksText(HeroLineageMember member)
        {
            var scar = member.ScarAtDeath != HeroScarType.None
                ? HeroInjuryCatalog.GetScarShortName(member.ScarAtDeath)
                : "нет";
            var trait = member.CharacterTrait != HeroCharacterTraitType.None
                ? HeroInjuryCatalog.GetCharacterTraitShortName(member.CharacterTrait)
                : "нет";
            return $"Шрам: {scar}; характер: {trait}";
        }

        private static string BuildActiveMarksText(HeroModel model)
        {
            var scar = model.PersonalScar != HeroScarType.None ? model.PersonalScarCompactText : "нет";
            var trait = model.CharacterTrait != HeroCharacterTraitType.None ? model.CharacterTraitCompactText : "нет";
            return $"Шрам: {scar}; характер: {trait}";
        }

        private static void DrawPanel(Rect rect)
        {
            FillRect(rect, new Color(0.08f, 0.075f, 0.065f, 0.96f));
            FillRect(new Rect(rect.x, rect.y, rect.width, 3f), new Color(0.87f, 0.72f, 0.34f, 0.95f));
            DrawOutline(rect, new Color(0f, 0f, 0f, 0.85f));
        }

        private static void DrawConnector(Rect rect)
        {
            FillRect(rect, new Color(0.87f, 0.72f, 0.34f, 0.65f));
        }

        private static Rect CalculatePanelRect()
        {
            var width = Mathf.Min(620f, Screen.width - 80f);
            var height = Mathf.Min(620f, Screen.height - 90f);
            return new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
        }

        private static Vector2 ToGuiPoint(Vector2 screenPosition)
        {
            return new Vector2(screenPosition.x, Screen.height - screenPosition.y);
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
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 25,
                fontStyle = FontStyle.Bold
            };
            titleStyle.normal.textColor = new Color(0.96f, 0.93f, 0.84f);
            subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                fontStyle = FontStyle.Italic,
                wordWrap = true
            };
            subtitleStyle.normal.textColor = new Color(0.75f, 0.72f, 0.62f);
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };
            labelStyle.normal.textColor = new Color(0.72f, 0.71f, 0.66f);
            valueStyle = new GUIStyle(labelStyle)
            {
                alignment = TextAnchor.MiddleRight
            };
            valueStyle.normal.textColor = new Color(0.98f, 0.88f, 0.5f);
            bodyStyle = new GUIStyle(labelStyle)
            {
                fontSize = 14
            };
            bodyStyle.normal.textColor = new Color(0.96f, 0.94f, 0.88f);
            closeButtonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };
            closeButtonStyle.normal.textColor = new Color(0.92f, 0.92f, 0.88f);
        }
    }
}
