using UnityEngine;
using UnityEngine.InputSystem;

namespace Labyrinth.Core
{
    public sealed class TimeScaleController : MonoBehaviour
    {
        private const float NormalScale = 1f;
        private const float FastScale = 2f;
        private const float FasterScale = 3f;

        private GUIStyle indicatorStyle;
        private GUIStyle labelStyle;
        private bool paused;
        private float scaleBeforePause = NormalScale;

        public bool Visible { get; set; }

        public float CurrentScale { get; private set; } = NormalScale;

        private void Awake()
        {
            SetScale(NormalScale);
        }

        private void Update()
        {
            if (!Visible || paused || Keyboard.current == null || GUIUtility.keyboardControl != 0)
            {
                return;
            }

            if (Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame)
            {
                SetScale(NormalScale);
            }
            else if (Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame)
            {
                SetScale(FastScale);
            }
            else if (Keyboard.current.digit3Key.wasPressedThisFrame || Keyboard.current.numpad3Key.wasPressedThisFrame)
            {
                SetScale(FasterScale);
            }
        }

        private void OnGUI()
        {
            if (!Visible)
            {
                return;
            }

            EnsureStyle();

            const float width = 132f;
            const float height = 42f;
            var rect = new Rect(Screen.width - width - 18f, 18f, width, height);
            DrawIndicator(rect);
        }

        private void SetScale(float scale)
        {
            CurrentScale = scale;
            Time.timeScale = scale;
        }

        public void Pause()
        {
            if (paused)
            {
                return;
            }

            scaleBeforePause = CurrentScale;
            paused = true;
            Time.timeScale = 0f;
        }

        public void ResumePaused()
        {
            if (!paused)
            {
                return;
            }

            paused = false;
            SetScale(scaleBeforePause);
        }

        public void ResetToNormal()
        {
            paused = false;
            scaleBeforePause = NormalScale;
            SetScale(NormalScale);
        }

        private void EnsureStyle()
        {
            if (indicatorStyle != null)
            {
                return;
            }

            indicatorStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleRight,
                fontSize = 20,
                fontStyle = FontStyle.Bold
            };
            indicatorStyle.normal.textColor = Color.white;
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
            labelStyle.normal.textColor = new Color(0.82f, 0.8f, 0.74f);
        }

        private void DrawIndicator(Rect rect)
        {
            var accent = paused
                ? new Color(0.95f, 0.42f, 0.36f, 0.82f)
                : CurrentScale >= FasterScale
                    ? new Color(1f, 0.62f, 0.22f, 0.82f)
                    : CurrentScale >= FastScale
                        ? new Color(1f, 0.84f, 0.26f, 0.82f)
                        : new Color(0.48f, 0.82f, 1f, 0.78f);

            FillRect(rect, new Color(0.09f, 0.095f, 0.09f, 0.9f));
            FillRect(new Rect(rect.x, rect.y, rect.width, 2f), accent);
            DrawOutline(rect, new Color(0f, 0f, 0f, 0.7f));
            GUI.Label(new Rect(rect.x + 12f, rect.y + 5f, rect.width - 24f, 15f), paused ? "Пауза" : "Время", labelStyle);
            GUI.Label(new Rect(rect.x + 12f, rect.y + 17f, rect.width - 24f, 22f), paused ? "II" : $"x{CurrentScale:0}", indicatorStyle);
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
