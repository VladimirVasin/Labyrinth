using Labyrinth.Core;
using UnityEngine;

namespace Labyrinth.UI
{
    public sealed class VictoryHudView : MonoBehaviour
    {
        private bool visible;
        private string message = string.Empty;
        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle badgeStyle;
        private GUIStyle closeButtonStyle;

        public bool IsVisible => visible;

        public bool ContainsScreenPoint(Vector2 screenPosition)
        {
            return visible && CalculatePanelRect().Contains(ToGuiPoint(screenPosition));
        }

        public void Show(string victoryMessage)
        {
            message = victoryMessage;
            if (!visible)
            {
                GameAudioController.PlayUi(GameSfx.HudOpen, 0.8f);
            }

            visible = true;
        }

        public void Hide()
        {
            if (visible)
            {
                GameAudioController.PlayUi(GameSfx.HudClose, 0.7f);
            }

            visible = false;
        }

        private void OnGUI()
        {
            if (!visible)
            {
                return;
            }

            EnsureStyles();

            var rect = CalculatePanelRect();

            FillRect(rect, new Color(0.12f, 0.08f, 0.07f, 0.95f));
            FillRect(new Rect(rect.x, rect.y, rect.width, 4f), new Color(1f, 0.72f, 0.22f, 0.95f));
            DrawOutline(rect, new Color(0f, 0f, 0f, 0.75f));
            DrawTrophy(new Rect(rect.x + 28f, rect.y + 34f, 58f, 64f));
            GUI.Label(new Rect(rect.x + 104f, rect.y + 24f, rect.width - 132f, 42f), "Победа", titleStyle);
            GUI.Label(new Rect(rect.x + 104f, rect.y + 70f, rect.width - 132f, 42f), message, subtitleStyle);
            DrawBadge(new Rect(rect.x + 104f, rect.y + 126f, rect.width - 132f, 34f), "Босс подземелья повержен");
            if (GUI.Button(new Rect(rect.x + 104f, rect.y + 178f, rect.width - 132f, 34f), "Закрыть", closeButtonStyle))
            {
                Hide();
            }
        }

        private static Rect CalculatePanelRect()
        {
            var width = Mathf.Min(560f, Screen.width - 48f);
            const float height = 238f;
            return new Rect((Screen.width - width) * 0.5f, 74f, width, height);
        }

        private static Vector2 ToGuiPoint(Vector2 screenPosition)
        {
            return new Vector2(screenPosition.x, Screen.height - screenPosition.y);
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
                fontSize = 34,
                fontStyle = FontStyle.Bold
            };
            titleStyle.normal.textColor = new Color(1f, 0.86f, 0.44f);
            subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 17,
                fontStyle = FontStyle.Bold
            };
            subtitleStyle.normal.textColor = new Color(0.96f, 0.93f, 0.86f);
            badgeStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 15,
                fontStyle = FontStyle.Bold
            };
            badgeStyle.normal.textColor = new Color(1f, 0.9f, 0.58f);
            closeButtonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 15,
                fontStyle = FontStyle.Bold
            };
            closeButtonStyle.normal.textColor = new Color(0.96f, 0.92f, 0.82f);
            closeButtonStyle.hover.textColor = Color.white;
            closeButtonStyle.active.textColor = new Color(1f, 0.86f, 0.44f);
        }

        private void DrawBadge(Rect rect, string text)
        {
            FillRect(rect, new Color(1f, 0.72f, 0.22f, 0.14f));
            DrawOutline(rect, new Color(1f, 0.72f, 0.22f, 0.42f));
            GUI.Label(rect, text, badgeStyle);
        }

        private static void DrawTrophy(Rect rect)
        {
            FillRect(new Rect(rect.x + 17f, rect.y + 12f, 24f, 28f), new Color(1f, 0.74f, 0.18f));
            FillRect(new Rect(rect.x + 22f, rect.y + 40f, 14f, 12f), new Color(0.86f, 0.54f, 0.12f));
            FillRect(new Rect(rect.x + 13f, rect.y + 52f, 32f, 8f), new Color(0.66f, 0.36f, 0.08f));
            FillRect(new Rect(rect.x + 6f, rect.y + 16f, 12f, 8f), new Color(1f, 0.78f, 0.22f));
            FillRect(new Rect(rect.x + 40f, rect.y + 16f, 12f, 8f), new Color(1f, 0.78f, 0.22f));
            FillRect(new Rect(rect.x + 24f, rect.y + 3f, 10f, 10f), new Color(1f, 0.9f, 0.36f));
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
    }
}
