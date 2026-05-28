using System;
using System.Globalization;
using Labyrinth.Core;
using UnityEngine;

namespace Labyrinth.UI
{
    public sealed class MainMenuUI : MonoBehaviour
    {
        private const string EditorMenuArtFolder = "Assets/Art/Menu";
        private const string ResourcesMenuArtFolder = "Menu";

        private string seedText;
        private string customWidthText = "100";
        private string customHeightText = "25";
        private string seedError;
        private string sizeError;
        private bool pauseMode;
        private bool visible;
        private Action<MazeGenerationSettings> startRequested;
        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle kickerStyle;
        private GUIStyle sectionStyle;
        private GUIStyle bodyStyle;
        private GUIStyle mutedStyle;
        private GUIStyle statusStyle;
        private GUIStyle summaryStyle;
        private GUIStyle errorStyle;
        private GUIStyle buttonStyle;
        private GUIStyle primaryButtonStyle;
        private GUIStyle textFieldStyle;
        private Texture2D menuBackground;
        private bool menuBackgroundLoaded;

        public bool IsVisible => visible;

        public void Show(Action<MazeGenerationSettings> onStartRequested, bool isPauseMenu = false)
        {
            startRequested = onStartRequested;
            pauseMode = isPauseMenu;
            EnsureSeedText();
            visible = true;
        }

        public void Hide()
        {
            visible = false;
        }

        private void OnGUI()
        {
            if (!visible)
            {
                return;
            }

            EnsureStyles();
            DrawBackground();

            var margin = Mathf.Clamp(Screen.width * 0.025f, 24f, 44f);
            var panelWidth = Mathf.Min(pauseMode ? 560f : 650f, Screen.width - margin * 2f);
            var panelHeight = Mathf.Min(pauseMode ? 650f : 690f, Screen.height - margin * 2f);
            var rect = new Rect(
                margin,
                margin,
                panelWidth,
                panelHeight);

            DrawMenuPanel(rect);

            GUILayout.BeginArea(new Rect(rect.x + 34f, rect.y + 28f, rect.width - 68f, rect.height - 56f));
            DrawHeader();
            GUILayout.Space(20f);

            DrawCustomSizeControls();
            GUILayout.Space(16f);
            DrawSeedControls();
            GUILayout.Space(16f);
            DrawSummaryAndStart();

            GUILayout.EndArea();
        }

        private void DrawBackground()
        {
            EnsureMenuBackground();
            var screenRect = new Rect(0f, 0f, Screen.width, Screen.height);
            if (menuBackground != null)
            {
                GUI.DrawTexture(screenRect, menuBackground, ScaleMode.ScaleAndCrop);
            }
            else
            {
                FillRect(screenRect, new Color(0.18f, 0.2f, 0.22f));
            }

            FillRect(screenRect, new Color(0f, 0f, 0f, 0.18f));
        }

        private void EnsureMenuBackground()
        {
            if (menuBackgroundLoaded)
            {
                return;
            }

            menuBackgroundLoaded = true;
            menuBackground = Resources.Load<Texture2D>(ResourcesMenuArtFolder);
            if (menuBackground == null)
            {
                LoadMenuBackgroundFromEditorAssets();
            }
        }

        private void LoadMenuBackgroundFromEditorAssets()
        {
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
            Texture2D fallback = null;
            var guids = UnityEditor.AssetDatabase.FindAssets("t:Texture2D", new[] { EditorMenuArtFolder });
            foreach (var guid in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var texture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture == null)
                {
                    continue;
                }

                if (fallback == null)
                {
                    fallback = texture;
                }

                if (texture.name == "Menu")
                {
                    menuBackground = texture;
                    return;
                }
            }

            menuBackground = fallback;
#endif
        }

        private void DrawCustomSizeControls()
        {
            var rect = DrawCard(172f);
            GUI.Label(new Rect(rect.x, rect.y, rect.width, 26f), "Размер подземелья", sectionStyle);
            GUI.Label(
                new Rect(rect.x, rect.y + 29f, rect.width, 38f),
                $"Ширина {MazeGenerationSettings.MinWidth}-{MazeGenerationSettings.MaxWidth}, высота {MazeGenerationSettings.MinHeight}-{MazeGenerationSettings.MaxHeight}; нечетность подгоняется автоматически.",
                mutedStyle);

            var fieldY = rect.y + 78f;
            var fieldWidth = Mathf.Min(160f, (rect.width - 36f) * 0.5f);
            var nextWidth = DrawNumberField(new Rect(rect.x, fieldY, fieldWidth, 66f), "Ширина", customWidthText);
            var nextHeight = DrawNumberField(
                new Rect(rect.x + fieldWidth + 24f, fieldY, fieldWidth, 66f),
                "Высота",
                customHeightText);

            if (nextWidth != customWidthText || nextHeight != customHeightText)
            {
                customWidthText = nextWidth;
                customHeightText = nextHeight;
            }

            DrawSizeStatus(new Rect(rect.x, rect.y + 144f, rect.width, 22f));
        }

        private void DrawSeedControls()
        {
            var rect = DrawCard(122f);
            GUI.Label(new Rect(rect.x, rect.y, rect.width, 26f), "Seed мира", sectionStyle);
            GUI.Label(new Rect(rect.x, rect.y + 29f, rect.width, 22f), "Одинаковый seed повторяет ту же карту.", mutedStyle);

            var fieldY = rect.y + 62f;
            var buttonWidth = 170f;
            seedText = GUI.TextField(
                new Rect(rect.x, fieldY, rect.width - buttonWidth - 12f, 44f),
                seedText,
                textFieldStyle);

            if (GUI.Button(new Rect(rect.xMax - buttonWidth, fieldY, buttonWidth, 44f), "Случайный", buttonStyle))
            {
                GameAudioController.PlayUi(GameSfx.HudClick);
                seedText = GenerateSeedText();
                seedError = null;
            }

            if (!TryParseSeed(out _))
            {
                GUI.Label(new Rect(rect.x, rect.y + 106f, rect.width, 22f), seedError, errorStyle);
            }
        }

        private void DrawSummaryAndStart()
        {
            var canStart = TryBuildSettings(out var selected);
            var rect = DrawCard(pauseMode ? 132f : 154f);
            GUI.Label(new Rect(rect.x, rect.y, rect.width, 26f), pauseMode ? "Новая попытка" : "Готово к экспедиции", sectionStyle);

            var summaryText = selected == null
                ? "Проверьте параметры генерации."
                : $"Карта {selected.Width} x {selected.Height}    Seed {selected.Seed}";
            GUI.Label(new Rect(rect.x, rect.y + 38f, rect.width, 28f), summaryText, selected == null ? errorStyle : summaryStyle);

            GUI.enabled = canStart;
            var startButtonText = pauseMode ? "Начать заново" : "Начать экспедицию";
            if (GUI.Button(new Rect(rect.x, rect.y + 82f, rect.width, 56f), startButtonText, primaryButtonStyle))
            {
                GameAudioController.PlayUi(GameSfx.HudConfirm);
                visible = false;
                startRequested?.Invoke(selected);
            }

            GUI.enabled = true;
        }

        private void DrawHeader()
        {
            GUILayout.Label(pauseMode ? "Меню паузы" : "Labyrinth", titleStyle);
            GUILayout.Space(4f);
            GUILayout.Label(pauseMode ? "Игра остановлена" : "Подготовка вылазки", kickerStyle);
            GUILayout.Space(8f);
            GUILayout.Label(
                pauseMode
                    ? "Можно закрыть меню Escape или создать новый лабиринт."
                    : "Настрой размер подземелья и seed перед стартом.",
                subtitleStyle);
        }

        private string DrawNumberField(Rect rect, string label, string value)
        {
            GUI.Label(new Rect(rect.x, rect.y, rect.width, 20f), label, bodyStyle);
            return GUI.TextField(new Rect(rect.x, rect.y + 22f, rect.width, 44f), value, textFieldStyle);
        }

        private void DrawSizeStatus(Rect rect)
        {
            if (TryParseCustomSize(out var width, out var height))
            {
                var normalizedWidth = MazeGenerationSettings.NormalizeWidth(width);
                var normalizedHeight = MazeGenerationSettings.NormalizeHeight(height);
                var hint = normalizedWidth == width && normalizedHeight == height
                    ? $"Будет создано: {normalizedWidth} x {normalizedHeight}"
                    : $"Будет создано: {normalizedWidth} x {normalizedHeight}, размер приведен к нечетному";
                GUI.Label(rect, hint, statusStyle);
                return;
            }

            GUI.Label(rect, sizeError, errorStyle);
        }

        private static Rect DrawCard(float height)
        {
            var rect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(height), GUILayout.ExpandWidth(true));
            DrawCardBackground(rect);
            return new Rect(rect.x + 18f, rect.y + 14f, rect.width - 36f, rect.height - 28f);
        }

        private bool TryBuildSettings(out MazeGenerationSettings settings)
        {
            settings = null;
            if (!TryParseSeed(out var seed))
            {
                return false;
            }

            if (!TryParseCustomSize(out var width, out var height))
            {
                return false;
            }

            settings = MazeGenerationSettings.CreateCustom(width, height, seed);
            return true;
        }

        private void EnsureSeedText()
        {
            if (string.IsNullOrWhiteSpace(seedText))
            {
                seedText = GenerateSeedText();
            }
        }

        private bool TryParseSeed(out int seed)
        {
            if (int.TryParse(seedText, NumberStyles.Integer, CultureInfo.InvariantCulture, out seed))
            {
                seedError = null;
                return true;
            }

            seedError = "Seed должен быть целым числом.";
            return false;
        }

        private bool TryParseCustomSize(out int width, out int height)
        {
            width = 0;
            height = 0;

            if (!int.TryParse(customWidthText, NumberStyles.Integer, CultureInfo.InvariantCulture, out width))
            {
                sizeError = "Ширина должна быть целым числом.";
                return false;
            }

            if (!int.TryParse(customHeightText, NumberStyles.Integer, CultureInfo.InvariantCulture, out height))
            {
                sizeError = "Высота должна быть целым числом.";
                return false;
            }

            if (width < MazeGenerationSettings.MinWidth || width > MazeGenerationSettings.MaxWidth)
            {
                sizeError = $"Ширина должна быть от {MazeGenerationSettings.MinWidth} до {MazeGenerationSettings.MaxWidth}.";
                return false;
            }

            if (height < MazeGenerationSettings.MinHeight || height > MazeGenerationSettings.MaxHeight)
            {
                sizeError = $"Высота должна быть от {MazeGenerationSettings.MinHeight} до {MazeGenerationSettings.MaxHeight}.";
                return false;
            }

            sizeError = null;
            return true;
        }

        private static string GenerateSeedText()
        {
            return (Environment.TickCount & int.MaxValue).ToString(CultureInfo.InvariantCulture);
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 50,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            titleStyle.normal.textColor = new Color(0.96f, 0.9f, 0.78f);
            subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true
            };
            subtitleStyle.normal.textColor = new Color(0.82f, 0.82f, 0.76f);
            kickerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                fontStyle = FontStyle.Italic,
                alignment = TextAnchor.MiddleLeft
            };
            kickerStyle.normal.textColor = new Color(0.82f, 0.7f, 0.42f);
            sectionStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 21,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            sectionStyle.normal.textColor = new Color(0.9f, 0.76f, 0.38f);
            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleLeft
            };
            bodyStyle.normal.textColor = new Color(0.8f, 0.79f, 0.72f);
            mutedStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true
            };
            mutedStyle.normal.textColor = new Color(0.62f, 0.62f, 0.58f);
            statusStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true
            };
            statusStyle.normal.textColor = new Color(0.72f, 0.92f, 0.7f);
            summaryStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 19,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true
            };
            summaryStyle.normal.textColor = new Color(0.9f, 0.9f, 0.84f);
            errorStyle = new GUIStyle(summaryStyle)
            {
                normal = { textColor = new Color(1f, 0.48f, 0.36f) }
            };
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            primaryButtonStyle = new GUIStyle(buttonStyle)
            {
                fontSize = 23
            };
            textFieldStyle = new GUIStyle(GUI.skin.textField)
            {
                fontSize = 22,
                alignment = TextAnchor.MiddleLeft
            };
        }

        private static void DrawMenuPanel(Rect rect)
        {
            FillRect(rect, new Color(0.075f, 0.082f, 0.078f, 0.94f));
            FillRect(new Rect(rect.x, rect.y, rect.width, 3f), new Color(0.87f, 0.72f, 0.34f, 0.95f));
            DrawOutline(rect, new Color(0f, 0f, 0f, 0.75f));
        }

        private static void DrawCardBackground(Rect rect)
        {
            FillRect(rect, new Color(0.035f, 0.04f, 0.038f, 0.86f));
            FillRect(new Rect(rect.x, rect.y, 3f, rect.height), new Color(0.7f, 0.55f, 0.2f, 0.78f));
            FillRect(new Rect(rect.x + 3f, rect.y, rect.width - 3f, 1f), new Color(1f, 1f, 1f, 0.08f));
            DrawOutline(rect, new Color(0f, 0f, 0f, 0.62f));
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
    }
}
