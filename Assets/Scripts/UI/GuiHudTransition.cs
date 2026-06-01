using UnityEngine;

namespace Labyrinth.UI
{
    public sealed class GuiHudTransition
    {
        private const float DurationSeconds = 0.13f;
        private bool targetVisible;
        private float startTime;
        private float startProgress;
        private float progress;

        public bool IsDrawing => targetVisible || CurrentProgress > 0.001f;

        public void Show()
        {
            SetTarget(true);
        }

        public void Hide()
        {
            SetTarget(false);
        }

        public Rect AnimateRect(Rect rect)
        {
            var eased = EaseOutCubic(CurrentProgress);
            var scale = Mathf.Lerp(0.965f, 1f, eased);
            var width = rect.width * scale;
            var height = rect.height * scale;
            return new Rect(
                rect.center.x - width * 0.5f,
                rect.center.y - height * 0.5f + (1f - eased) * 8f,
                width,
                height);
        }

        public Color ApplyGuiAlpha()
        {
            var previous = GUI.color;
            GUI.color = new Color(previous.r, previous.g, previous.b, previous.a * Mathf.Clamp01(CurrentProgress));
            return previous;
        }

        private float CurrentProgress
        {
            get
            {
                UpdateProgress();
                return progress;
            }
        }

        private void SetTarget(bool visible)
        {
            UpdateProgress();
            if (targetVisible == visible)
            {
                return;
            }

            targetVisible = visible;
            startProgress = progress;
            startTime = Time.unscaledTime;
        }

        private void UpdateProgress()
        {
            var target = targetVisible ? 1f : 0f;
            var elapsed = Mathf.Max(0f, Time.unscaledTime - startTime);
            var t = DurationSeconds > 0f ? Mathf.Clamp01(elapsed / DurationSeconds) : 1f;
            progress = Mathf.Lerp(startProgress, target, EaseOutCubic(t));
        }

        private static float EaseOutCubic(float t)
        {
            var inverse = 1f - Mathf.Clamp01(t);
            return 1f - inverse * inverse * inverse;
        }
    }
}
