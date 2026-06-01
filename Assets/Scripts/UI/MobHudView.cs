using Labyrinth.Core;
using Labyrinth.Mobs;
using UnityEngine;

namespace Labyrinth.UI
{
    public sealed class MobHudView : MonoBehaviour
    {
        private MobController selectedMob;
        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle sectionStyle;
        private GUIStyle statLabelStyle;
        private GUIStyle statValueStyle;
        private GUIStyle statusBadgeStyle;
        private GUIStyle barLabelStyle;
        private GUIStyle barValueStyle;
        private GUIStyle combatValueStyle;
        private GUIStyle combatLabelStyle;
        private GUIStyle closeButtonStyle;
        private Texture2D circleTexture;
        private readonly GuiHudTransition transition = new GuiHudTransition();
        private bool visible;

        public bool IsVisible => visible;

        public bool ContainsScreenPoint(Vector2 screenPosition)
        {
            return visible && selectedMob != null && CalculatePanelRect().Contains(ToGuiPoint(screenPosition));
        }

        public void Show(MobController mob)
        {
            if (!visible || selectedMob == null || selectedMob != mob)
            {
                GameAudioController.PlayUi(GameSfx.HudOpen);
            }

            selectedMob = mob;
            visible = selectedMob != null;
            if (visible)
            {
                transition.Show();
            }
        }

        public void Hide()
        {
            if (visible && selectedMob != null)
            {
                GameAudioController.PlayUi(GameSfx.HudClose);
            }

            visible = false;
            transition.Hide();
        }

        private void OnGUI()
        {
            if (selectedMob == null || selectedMob.Model == null)
            {
                selectedMob = null;
                visible = false;
                return;
            }

            if (!transition.IsDrawing)
            {
                selectedMob = null;
                return;
            }

            EnsureStyles();

            var previousColor = transition.ApplyGuiAlpha();
            var rect = transition.AnimateRect(CalculatePanelRect());

            DrawPanel(rect, selectedMob.Model.Rank);

            var contentX = rect.x + 18f;
            var contentWidth = rect.width - 36f;
            var y = rect.y + 16f;

            DrawMobHeader(new Rect(contentX, y, contentWidth, 78f), selectedMob.Model);
            y += 92f;
            DrawSection(new Rect(contentX, y, contentWidth, 18f), "Состояние");
            y += 24f;
            DrawStatRow(new Rect(contentX, y, contentWidth, 26f), "Тип", BuildSpeciesName(selectedMob.Model.Species));
            y += 30f;
            DrawStatRow(new Rect(contentX, y, contentWidth, 26f), "Lvl", selectedMob.Model.Level.ToString());
            y += 30f;
            DrawStatRow(new Rect(contentX, y, contentWidth, 26f), "Награда", BuildRewardText(selectedMob.Model));
            y += 36f;
            DrawProgressBar(
                new Rect(contentX, y, contentWidth, 38f),
                "HP",
                selectedMob.Model.HitPoints,
                selectedMob.Model.MaxHitPoints,
                GetRankColor(selectedMob.Model.Rank));

            y += 52f;
            DrawSection(new Rect(contentX, y, contentWidth, 18f), "Боевые параметры");
            y += 24f;
            var cardWidth = (contentWidth - 10f) * 0.5f;
            DrawCombatCard(new Rect(contentX, y, cardWidth, 54f), "Attack Points", selectedMob.Model.AttackPoints.ToString(), new Color(0.98f, 0.76f, 0.34f));
            DrawCombatCard(new Rect(contentX + cardWidth + 10f, y, cardWidth, 54f), "Armor Points", selectedMob.Model.ArmorPoints.ToString(), new Color(0.55f, 0.78f, 1f));

            if (GUI.Button(new Rect(contentX, rect.yMax - 42f, contentWidth, 31f), "Закрыть", closeButtonStyle))
            {
                Hide();
            }

            GUI.color = previousColor;
        }

        private static string BuildTitle(MobModel model)
        {
            if (model.Rank == MobRank.Boss)
            {
                return "Босс";
            }

            return model.Rank == MobRank.MiniBoss ? "Мини-босс" : BuildSpeciesName(model.Species);
        }

        private static string BuildSubtitle(MobModel model)
        {
            if (model.Rank == MobRank.Boss)
            {
                return $"{BuildSpeciesName(model.Species)}, цель победы";
            }

            return model.Rank == MobRank.MiniBoss ? $"{BuildSpeciesName(model.Species)}, страж пещеры" : "нейтральный моб";
        }

        private static string BuildSpeciesName(MobSpecies species)
        {
            switch (species)
            {
                case MobSpecies.Orc:
                    return "Орк";
                case MobSpecies.Goblin:
                    return "Гоблин";
                case MobSpecies.Rat:
                    return "Крыса";
                default:
                    return "Моб";
            }
        }

        private static string BuildStateText(MobState state)
        {
            switch (state)
            {
                case MobState.Wandering:
                    return "бродит";
                case MobState.Fighting:
                    return "сражается";
                case MobState.Defeated:
                    return "повержен";
                default:
                    return "неизвестно";
            }
        }

        private static string BuildRewardText(MobModel model)
        {
            if (model == null)
            {
                return "-";
            }

            if (model.IsBoss)
            {
                return "110-170 зол., 125 XP, ключ";
            }

            if (model.IsMiniBoss)
            {
                return "36-64 зол., 48 XP";
            }

            switch (model.Species)
            {
                case MobSpecies.Rat:
                    return "3-6 зол., 2 XP";
                case MobSpecies.Goblin:
                    return "7-13 зол., 7 XP";
                case MobSpecies.Orc:
                    return "14-24 зол., 14 XP";
                default:
                    return "награда неизвестна";
            }
        }

        private static void DrawPanel(Rect rect, MobRank rank)
        {
            FillRect(rect, new Color(0.11f, 0.105f, 0.1f, 0.94f));
            FillRect(
                new Rect(rect.x, rect.y, rect.width, 3f),
                GetRankColor(rank));
            DrawOutline(rect, new Color(0f, 0f, 0f, 0.75f));
        }

        private static Rect CalculatePanelRect()
        {
            var width = Mathf.Min(370f, Screen.width - 120f);
            var height = Mathf.Min(412f, Screen.height - 96f);
            return new Rect(Screen.width - width - 18f, 74f, width, height);
        }

        private static Vector2 ToGuiPoint(Vector2 screenPosition)
        {
            return new Vector2(screenPosition.x, Screen.height - screenPosition.y);
        }

        private void DrawSection(Rect rect, string text)
        {
            GUI.Label(rect, text, sectionStyle);
            FillRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), new Color(1f, 1f, 1f, 0.1f));
        }

        private void DrawMobHeader(Rect rect, MobModel model)
        {
            FillRect(rect, new Color(1f, 1f, 1f, 0.055f));
            DrawOutline(rect, new Color(1f, 1f, 1f, 0.11f));
            DrawMobIcon(new Rect(rect.x + 12f, rect.y + 14f, 44f, 46f), model);
            var textX = rect.x + 68f;
            GUI.Label(new Rect(textX, rect.y + 9f, rect.width - 204f, 26f), BuildTitle(model), titleStyle);
            GUI.Label(new Rect(textX, rect.y + 36f, rect.width - 92f, 18f), BuildSubtitle(model), subtitleStyle);
            DrawStatusBadge(
                new Rect(rect.xMax - 118f, rect.y + 14f, 104f, 26f),
                BuildStateText(model.State),
                GetRankColor(model.Rank));
        }

        private void DrawMobIcon(Rect rect, MobModel model)
        {
            var main = model.Rank == MobRank.Boss
                ? new Color(0.68f, 0.1f, 0.08f)
                : model.Rank == MobRank.MiniBoss
                    ? new Color(0.72f, 0.26f, 0.08f)
                    : GetMobIconColor(model.Species);
            DrawCircle(new Rect(rect.x + 7f, rect.y + 3f, 28f, 28f), main);
            FillRect(new Rect(rect.x + 12f, rect.y + 11f, 5f, 4f), Color.black);
            FillRect(new Rect(rect.x + 25f, rect.y + 11f, 5f, 4f), Color.black);
            FillRect(new Rect(rect.x + 15f, rect.y + 22f, 13f, 3f), new Color(0.12f, 0.06f, 0.04f));
            FillRect(new Rect(rect.x + 10f, rect.y + 31f, 24f, 11f), main * 0.85f);
        }

        private static Color GetMobIconColor(MobSpecies species)
        {
            switch (species)
            {
                case MobSpecies.Rat:
                    return new Color(0.34f, 0.32f, 0.3f);
                case MobSpecies.Goblin:
                    return new Color(0.24f, 0.68f, 0.2f);
                default:
                    return new Color(0.34f, 0.58f, 0.26f);
            }
        }

        private static Color GetRankColor(MobRank rank)
        {
            switch (rank)
            {
                case MobRank.Boss:
                    return new Color(1f, 0.24f, 0.18f, 0.95f);
                case MobRank.MiniBoss:
                    return new Color(1f, 0.54f, 0.16f, 0.95f);
                default:
                    return new Color(0.36f, 0.78f, 0.31f, 0.95f);
            }
        }

        private void DrawStatusBadge(Rect rect, string text, Color color)
        {
            FillRect(rect, new Color(color.r, color.g, color.b, 0.18f));
            DrawOutline(rect, new Color(color.r, color.g, color.b, 0.55f));
            var previousColor = GUI.color;
            GUI.color = new Color(color.r, color.g, color.b, color.a * previousColor.a);
            GUI.Label(rect, text, statusBadgeStyle);
            GUI.color = previousColor;
        }

        private void DrawProgressBar(Rect rect, string label, int value, int maxValue, Color color)
        {
            FillRect(rect, new Color(1f, 1f, 1f, 0.045f));
            DrawOutline(rect, new Color(1f, 1f, 1f, 0.08f));
            GUI.Label(new Rect(rect.x + 10f, rect.y + 3f, rect.width - 20f, 16f), label, barLabelStyle);
            GUI.Label(new Rect(rect.x + 10f, rect.y + 3f, rect.width - 20f, 16f), $"{value} / {maxValue}", barValueStyle);
            var barRect = new Rect(rect.x + 10f, rect.y + 24f, rect.width - 20f, 8f);
            FillRect(barRect, new Color(0f, 0f, 0f, 0.45f));
            var normalized = maxValue > 0 ? Mathf.Clamp01((float)value / maxValue) : 0f;
            FillRect(new Rect(barRect.x, barRect.y, barRect.width * normalized, barRect.height), color);
        }

        private void DrawCombatCard(Rect rect, string label, string value, Color valueColor)
        {
            FillRect(rect, new Color(1f, 1f, 1f, 0.055f));
            DrawOutline(rect, new Color(1f, 1f, 1f, 0.1f));
            var previousColor = GUI.color;
            GUI.color = new Color(valueColor.r, valueColor.g, valueColor.b, valueColor.a * previousColor.a);
            GUI.Label(new Rect(rect.x + 10f, rect.y + 8f, rect.width - 20f, 24f), value, combatValueStyle);
            GUI.color = previousColor;
            GUI.Label(new Rect(rect.x + 10f, rect.y + 31f, rect.width - 20f, 18f), label, combatLabelStyle);
        }

        private void DrawStatRow(Rect rect, string label, string value)
        {
            FillRect(rect, new Color(1f, 1f, 1f, 0.045f));
            GUI.Label(new Rect(rect.x + 8f, rect.y, rect.width * 0.47f, rect.height), label, statLabelStyle);
            GUI.Label(new Rect(rect.x + rect.width * 0.47f, rect.y, rect.width * 0.5f - 8f, rect.height), value, statValueStyle);
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
            GUI.color = new Color(color.r, color.g, color.b, color.a * previousColor.a);
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
                alignment = TextAnchor.MiddleLeft,
                fontSize = 23,
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
            sectionStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };
            sectionStyle.normal.textColor = new Color(0.52f, 0.9f, 0.45f);
            statLabelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };
            statLabelStyle.normal.textColor = new Color(0.73f, 0.72f, 0.68f);
            statValueStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleRight,
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };
            statValueStyle.normal.textColor = Color.white;
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
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };
            barValueStyle.normal.textColor = new Color(0.95f, 0.95f, 0.9f);
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
            closeButtonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };
            closeButtonStyle.normal.textColor = new Color(0.92f, 0.92f, 0.88f);
            circleTexture = CreateCircleTexture();
        }

        private void DrawCircle(Rect rect, Color color)
        {
            var previousColor = GUI.color;
            GUI.color = new Color(color.r, color.g, color.b, color.a * previousColor.a);
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
