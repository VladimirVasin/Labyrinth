using Labyrinth.Core;
using UnityEngine;

namespace Labyrinth.UI
{
    public sealed class ObjectMicroHudView : MonoBehaviour
    {
        private ObjectMicroHudTarget selectedObject;
        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle labelStyle;
        private GUIStyle valueStyle;
        private GUIStyle effectStyle;
        private GUIStyle closeButtonStyle;
        private readonly GuiHudTransition transition = new GuiHudTransition();
        private bool visible;

        public bool IsVisible => visible;

        public void Show(ObjectMicroHudTarget target)
        {
            if (!visible || selectedObject == null || selectedObject != target)
            {
                GameAudioController.PlayUi(GameSfx.HudOpen);
            }

            selectedObject = target;
            visible = selectedObject != null;
            if (visible)
            {
                transition.Show();
            }
        }

        public void Hide()
        {
            if (visible && selectedObject != null)
            {
                GameAudioController.PlayUi(GameSfx.HudClose);
            }

            visible = false;
            transition.Hide();
        }

        public bool ContainsScreenPoint(Vector2 screenPosition)
        {
            return visible && selectedObject != null && CalculatePanelRect().Contains(ToGuiPoint(screenPosition));
        }

        private void OnGUI()
        {
            if (selectedObject == null)
            {
                return;
            }

            if (!transition.IsDrawing)
            {
                selectedObject = null;
                return;
            }

            EnsureStyles();
            var previousColor = transition.ApplyGuiAlpha();
            var rect = transition.AnimateRect(CalculatePanelRect());
            DrawPanel(rect, selectedObject.AccentColor);

            var contentX = rect.x + 14f;
            var contentWidth = rect.width - 28f;
            var y = rect.y + 12f;
            GUI.Label(new Rect(contentX, y, contentWidth, 25f), selectedObject.DisplayName, titleStyle);
            y += 25f;
            GUI.Label(new Rect(contentX, y, contentWidth, 18f), selectedObject.Subtitle, subtitleStyle);
            y += 28f;
            DrawRow(new Rect(contentX, y, contentWidth, 26f), "Тип", selectedObject.TypeName);
            y += 32f;
            DrawRow(new Rect(contentX, y, contentWidth, 26f), "Статус", selectedObject.StatusText);
            y += 34f;
            var effectHeight = CalculateEffectHeight(contentWidth);
            DrawEffectBox(new Rect(contentX, y, contentWidth, effectHeight), selectedObject.EffectText);
            y += effectHeight + 10f;

            if (selectedObject.HasAction)
            {
                GUI.enabled = selectedObject.CanInvokeAction;
                if (GUI.Button(new Rect(contentX, y, contentWidth, 28f), selectedObject.ActionLabel, closeButtonStyle))
                {
                    selectedObject.InvokeAction();
                    GameAudioController.PlayUi(GameSfx.HudConfirm);
                }

                GUI.enabled = true;
            }

            if (GUI.Button(new Rect(contentX, rect.yMax - 38f, contentWidth, 28f), "Закрыть", closeButtonStyle))
            {
                Hide();
            }

            GUI.color = previousColor;
        }

        private Rect CalculatePanelRect()
        {
            var width = Mathf.Min(340f, Screen.width - 80f);
            var contentWidth = width - 28f;
            var actionHeight = selectedObject != null && selectedObject.HasAction ? 38f : 0f;
            var height = Mathf.Min(178f + CalculateEffectHeight(contentWidth) + actionHeight, Screen.height - 96f);
            return new Rect(Screen.width - width - 18f, Screen.height - height - 18f, width, height);
        }

        private float CalculateEffectHeight(float contentWidth)
        {
            if (effectStyle == null)
            {
                return 58f;
            }

            var textHeight = effectStyle.CalcHeight(new GUIContent(selectedObject != null ? selectedObject.EffectText : string.Empty), contentWidth - 20f);
            return Mathf.Clamp(textHeight + 32f, 58f, 150f);
        }

        private static Vector2 ToGuiPoint(Vector2 screenPosition)
        {
            return new Vector2(screenPosition.x, Screen.height - screenPosition.y);
        }

        private static void DrawPanel(Rect rect, Color accent)
        {
            FillRect(rect, new Color(0.11f, 0.105f, 0.1f, 0.94f));
            FillRect(new Rect(rect.x, rect.y, rect.width, 3f), new Color(accent.r, accent.g, accent.b, 0.95f));
            DrawOutline(rect, new Color(0f, 0f, 0f, 0.75f));
        }

        private void DrawRow(Rect rect, string label, string value)
        {
            FillRect(rect, new Color(1f, 1f, 1f, 0.045f));
            GUI.Label(new Rect(rect.x + 8f, rect.y, rect.width * 0.36f, rect.height), label, labelStyle);
            GUI.Label(new Rect(rect.x + rect.width * 0.36f, rect.y, rect.width * 0.61f - 8f, rect.height), value, valueStyle);
        }

        private void DrawEffectBox(Rect rect, string text)
        {
            FillRect(rect, new Color(1f, 1f, 1f, 0.045f));
            DrawOutline(rect, new Color(1f, 1f, 1f, 0.08f));
            GUI.Label(new Rect(rect.x + 10f, rect.y + 5f, rect.width - 20f, 16f), "Описание", labelStyle);
            GUI.Label(new Rect(rect.x + 10f, rect.y + 24f, rect.width - 20f, rect.height - 28f), text, effectStyle);
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
                fontSize = 20,
                fontStyle = FontStyle.Bold
            };
            titleStyle.normal.textColor = new Color(0.96f, 0.93f, 0.86f);
            subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 12,
                fontStyle = FontStyle.Italic
            };
            subtitleStyle.normal.textColor = new Color(0.72f, 0.7f, 0.64f);
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };
            labelStyle.normal.textColor = new Color(0.73f, 0.72f, 0.68f);
            valueStyle = new GUIStyle(labelStyle) { alignment = TextAnchor.MiddleRight };
            valueStyle.normal.textColor = Color.white;
            effectStyle = new GUIStyle(labelStyle)
            {
                alignment = TextAnchor.UpperLeft,
                wordWrap = true
            };
            effectStyle.normal.textColor = new Color(0.92f, 0.91f, 0.86f);
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
