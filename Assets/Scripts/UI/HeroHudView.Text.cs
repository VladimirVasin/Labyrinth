using UnityEngine;

namespace Labyrinth.UI
{
    public sealed partial class HeroHudView
    {
        private static void DrawFittedLabel(
            Rect rect,
            string text,
            GUIStyle baseStyle,
            int minimumFontSize,
            bool wordWrap)
        {
            var contentText = string.IsNullOrEmpty(text) ? string.Empty : text;
            var style = new GUIStyle(baseStyle)
            {
                wordWrap = wordWrap
            };
            var startSize = style.fontSize > 0 ? style.fontSize : 12;
            var minSize = Mathf.Clamp(minimumFontSize, 6, startSize);
            var content = new GUIContent(contentText);

            for (var size = startSize; size >= minSize; size--)
            {
                style.fontSize = size;
                if (TextFits(rect, content, style, wordWrap))
                {
                    GUI.Label(rect, content, style);
                    return;
                }
            }

            style.fontSize = minSize;
            GUI.Label(rect, FitText(rect, contentText, style, wordWrap), style);
        }

        private static GUIContent FitText(Rect rect, string text, GUIStyle style, bool wordWrap)
        {
            if (string.IsNullOrEmpty(text))
            {
                return GUIContent.none;
            }

            const string suffix = "...";
            if (TextFits(rect, new GUIContent(suffix), style, wordWrap))
            {
                var low = 0;
                var high = text.Length;
                var best = suffix;
                while (low <= high)
                {
                    var mid = (low + high) / 2;
                    var candidate = text.Substring(0, mid).TrimEnd() + suffix;
                    var content = new GUIContent(candidate);
                    if (TextFits(rect, content, style, wordWrap))
                    {
                        best = candidate;
                        low = mid + 1;
                    }
                    else
                    {
                        high = mid - 1;
                    }
                }

                return new GUIContent(best);
            }

            return new GUIContent(string.Empty);
        }

        private static bool TextFits(Rect rect, GUIContent content, GUIStyle style, bool wordWrap)
        {
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return false;
            }

            if (!wordWrap)
            {
                var size = style.CalcSize(content);
                return size.x <= rect.width + 0.5f && size.y <= rect.height + 0.5f;
            }

            var height = style.CalcHeight(content, rect.width);
            return height <= rect.height + 0.5f && LongestWordFits(content.text, style, rect.width);
        }

        private static bool LongestWordFits(string text, GUIStyle style, float width)
        {
            var wordStart = -1;
            for (var i = 0; i <= text.Length; i++)
            {
                var isBoundary = i == text.Length || char.IsWhiteSpace(text[i]);
                if (!isBoundary && wordStart < 0)
                {
                    wordStart = i;
                }

                if (!isBoundary || wordStart < 0)
                {
                    continue;
                }

                var word = text.Substring(wordStart, i - wordStart);
                if (style.CalcSize(new GUIContent(word)).x > width + 0.5f)
                {
                    return false;
                }

                wordStart = -1;
            }

            return true;
        }
    }
}
