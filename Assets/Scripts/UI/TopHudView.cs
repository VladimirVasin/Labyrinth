using System;
using Labyrinth.Core;
using UnityEngine;

namespace Labyrinth.UI
{
    public sealed class TopHudView : MonoBehaviour
    {
        private ResourceWallet resources;
        private Func<int> heroCountProvider;
        private Func<int> maxHeroCountProvider;
        private GUIStyle labelStyle;
        private GUIStyle valueStyle;
        private GUIStyle slashStyle;
        private Texture2D circleTexture;

        public bool Visible { get; set; }

        public void Configure(
            ResourceWallet resourceWallet,
            Func<int> onHeroCountRequested,
            Func<int> onMaxHeroCountRequested)
        {
            resources = resourceWallet;
            heroCountProvider = onHeroCountRequested;
            maxHeroCountProvider = onMaxHeroCountRequested;
        }

        private void OnGUI()
        {
            if (!Visible || resources == null)
            {
                return;
            }

            EnsureStyles();

            const float chipWidth = 146f;
            const float heroChipWidth = 154f;
            const float chipHeight = 44f;
            const float gap = 8f;
            var x = 18f;
            var y = 18f;

            DrawResourceItem(new Rect(x, y, chipWidth, chipHeight), ResourceIconType.Food, "Пища", resources.Food);
            x += chipWidth + gap;
            DrawResourceItem(new Rect(x, y, chipWidth, chipHeight), ResourceIconType.Gold, "Золото", resources.Gold);
            x += chipWidth + gap;
            DrawResourceItem(new Rect(x, y, chipWidth, chipHeight), ResourceIconType.Wood, "Дерево", resources.Wood);
            x += chipWidth + gap + 6f;
            DrawResourceItem(new Rect(x, y, chipWidth, chipHeight), ResourceIconType.Iron, "Железо", resources.Iron);
            x += chipWidth + gap + 6f;
            DrawTextItem(
                new Rect(x, y, heroChipWidth, chipHeight),
                ResourceIconType.Hero,
                "Герои",
                $"{GetHeroCount()} / {GetMaxHeroCount()}");
        }

        private void DrawResourceItem(Rect rect, ResourceIconType iconType, string label, int value)
        {
            DrawChipBackground(rect);
            DrawResourceIcon(new Rect(rect.x + 9f, rect.y + 9f, 26f, 26f), iconType);
            GUI.Label(new Rect(rect.x + 43f, rect.y + 5f, rect.width - 50f, 15f), label, labelStyle);
            GUI.Label(new Rect(rect.x + 43f, rect.y + 18f, rect.width - 52f, 22f), value.ToString(), valueStyle);
        }

        private void DrawTextItem(Rect rect, ResourceIconType iconType, string label, string value)
        {
            DrawChipBackground(rect);
            DrawResourceIcon(new Rect(rect.x + 9f, rect.y + 9f, 26f, 26f), iconType);
            GUI.Label(new Rect(rect.x + 43f, rect.y + 5f, rect.width - 50f, 15f), label, labelStyle);
            GUI.Label(new Rect(rect.x + 43f, rect.y + 18f, rect.width - 52f, 22f), value, slashStyle);
        }

        private void DrawResourceIcon(Rect rect, ResourceIconType iconType)
        {
            if (iconType == ResourceIconType.Food)
            {
                DrawCircle(new Rect(rect.x + 3f, rect.y + 7f, 18f, 16f), new Color(0.78f, 0.43f, 0.2f));
                FillRect(new Rect(rect.x + 7f, rect.y + 5f, 15f, 7f), new Color(0.98f, 0.72f, 0.35f));
                FillRect(new Rect(rect.x + 11f, rect.y + 3f, 3f, 5f), new Color(0.44f, 0.72f, 0.24f));
                FillRect(new Rect(rect.x + 15f, rect.y + 2f, 7f, 3f), new Color(0.35f, 0.66f, 0.22f));
                return;
            }

            if (iconType == ResourceIconType.Gold)
            {
                DrawCircle(new Rect(rect.x + 3f, rect.y + 3f, 20f, 20f), new Color(0.96f, 0.68f, 0.18f));
                DrawCircle(new Rect(rect.x + 7f, rect.y + 7f, 12f, 12f), new Color(1f, 0.86f, 0.33f));
                FillRect(new Rect(rect.x + 10f, rect.y + 8f, 6f, 2f), new Color(0.64f, 0.38f, 0.08f));
                FillRect(new Rect(rect.x + 10f, rect.y + 13f, 6f, 2f), new Color(0.64f, 0.38f, 0.08f));
                return;
            }

            if (iconType == ResourceIconType.Wood)
            {
                FillRect(new Rect(rect.x + 5f, rect.y + 15f, 18f, 7f), new Color(0.5f, 0.28f, 0.1f));
                DrawCircle(new Rect(rect.x + 3f, rect.y + 12f, 10f, 10f), new Color(0.66f, 0.42f, 0.18f));
                DrawCircle(new Rect(rect.x + 15f, rect.y + 12f, 10f, 10f), new Color(0.66f, 0.42f, 0.18f));
                FillRect(new Rect(rect.x + 10f, rect.y + 17f, 7f, 2f), new Color(0.24f, 0.12f, 0.05f));
                return;
            }

            if (iconType == ResourceIconType.Iron)
            {
                FillRect(new Rect(rect.x + 6f, rect.y + 14f, 18f, 9f), new Color(0.42f, 0.48f, 0.54f));
                FillRect(new Rect(rect.x + 9f, rect.y + 10f, 16f, 7f), new Color(0.62f, 0.68f, 0.74f));
                FillRect(new Rect(rect.x + 6f, rect.y + 23f, 15f, 4f), new Color(0.25f, 0.29f, 0.33f));
                FillRect(new Rect(rect.x + 12f, rect.y + 12f, 8f, 2f), new Color(0.86f, 0.9f, 0.92f));
                return;
            }

            DrawCircle(new Rect(rect.x + 5f, rect.y + 2f, 17f, 17f), new Color(0.72f, 0.74f, 0.78f));
            FillRect(new Rect(rect.x + 7f, rect.y + 10f, 13f, 4f), new Color(0.16f, 0.18f, 0.2f));
            FillRect(new Rect(rect.x + 9f, rect.y + 19f, 14f, 5f), new Color(0.18f, 0.28f, 0.7f));
        }

        private int GetHeroCount()
        {
            return heroCountProvider != null ? heroCountProvider.Invoke() : 0;
        }

        private int GetMaxHeroCount()
        {
            return maxHeroCountProvider != null ? maxHeroCountProvider.Invoke() : 0;
        }

        private void EnsureStyles()
        {
            if (labelStyle != null)
            {
                return;
            }

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
            labelStyle.normal.textColor = new Color(0.82f, 0.8f, 0.74f);
            valueStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleRight,
                fontSize = 19,
                fontStyle = FontStyle.Bold
            };
            valueStyle.normal.textColor = Color.white;
            slashStyle = new GUIStyle(valueStyle)
            {
                fontSize = 18
            };
            circleTexture = CreateCircleTexture();
        }

        private static void DrawChipBackground(Rect rect)
        {
            FillRect(rect, new Color(0.09f, 0.095f, 0.09f, 0.9f));
            FillRect(new Rect(rect.x, rect.y, rect.width, 2f), new Color(0.87f, 0.72f, 0.34f, 0.65f));
            DrawOutline(rect, new Color(0f, 0f, 0f, 0.7f));
        }

        private void DrawCircle(Rect rect, Color color)
        {
            var previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, circleTexture);
            GUI.color = previousColor;
        }

        private static void FillRect(Rect rect, Color color)
        {
            var previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previousColor;
        }

        private static void DrawOutline(Rect rect, Color color)
        {
            FillRect(new Rect(rect.x, rect.y, rect.width, 1f), color);
            FillRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), color);
            FillRect(new Rect(rect.x, rect.y, 1f, rect.height), color);
            FillRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), color);
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

        private enum ResourceIconType
        {
            Food,
            Gold,
            Wood,
            Iron,
            Hero
        }
    }
}
