using System;
using Labyrinth.Core;
using UnityEngine;

namespace Labyrinth.UI
{
    public sealed class DungeonLevelHudView : MonoBehaviour
    {
        private Func<int> currentLevelProvider;
        private Func<int> unlockedLevelProvider;
        private Action<int> switchRequested;
        private GUIStyle buttonStyle;
        private GUIStyle activeButtonStyle;
        private GUIStyle labelStyle;

        public bool Visible { get; set; }

        public void Configure(Func<int> onCurrentLevelRequested, Func<int> onUnlockedLevelRequested, Action<int> onSwitchRequested)
        {
            currentLevelProvider = onCurrentLevelRequested;
            unlockedLevelProvider = onUnlockedLevelRequested;
            switchRequested = onSwitchRequested;
        }

        private void OnGUI()
        {
            if (!Visible)
            {
                return;
            }

            var unlocked = unlockedLevelProvider?.Invoke() ?? 1;
            if (unlocked <= 1)
            {
                return;
            }

            EnsureStyles();
            var current = currentLevelProvider?.Invoke() ?? 1;
            var width = 234f;
            var height = 92f;
            var rect = new Rect(Screen.width - width - 18f, 70f, width, height);
            FillRect(rect, new Color(0.08f, 0.075f, 0.07f, 0.88f));
            FillRect(new Rect(rect.x, rect.y, rect.width, 3f), new Color(0.87f, 0.72f, 0.34f, 0.95f));
            DrawOutline(rect, new Color(0f, 0f, 0f, 0.75f));
            GUI.Label(new Rect(rect.x + 12f, rect.y + 8f, rect.width - 24f, 22f), "Уровни подземелья", labelStyle);

            var buttonWidth = (rect.width - 34f) / 2f;
            DrawLevelButton(new Rect(rect.x + 12f, rect.y + 42f, buttonWidth, 34f), 1, current);
            DrawLevelButton(new Rect(rect.x + 22f + buttonWidth, rect.y + 42f, buttonWidth, 34f), 2, current);
        }

        private void DrawLevelButton(Rect rect, int level, int current)
        {
            if (GUI.Button(rect, $"Уровень {level}", current == level ? activeButtonStyle : buttonStyle))
            {
                GameAudioController.PlayUi(current == level ? GameSfx.HudClick : GameSfx.HudConfirm, 0.75f);
                switchRequested?.Invoke(level);
            }
        }

        private void EnsureStyles()
        {
            if (buttonStyle != null)
            {
                return;
            }

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };
            labelStyle.normal.textColor = new Color(0.96f, 0.93f, 0.86f);
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };
            buttonStyle.normal.textColor = new Color(0.9f, 0.9f, 0.86f);
            activeButtonStyle = new GUIStyle(buttonStyle);
            activeButtonStyle.normal.textColor = new Color(1f, 0.86f, 0.36f);
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
