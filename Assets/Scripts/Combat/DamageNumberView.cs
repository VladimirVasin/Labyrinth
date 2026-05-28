using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Labyrinth.Maze;
using UnityEngine;

namespace Labyrinth.Combat
{
    public sealed class DamageNumberView : MonoBehaviour
    {
        private const float DefaultLifetime = 1f;
        private const float DefaultRiseSpeed = 0.62f;
        private const float DefaultScaleGrowth = 0.18f;
        private const float DefaultCharacterSize = 0.135f;
        private const int DefaultFontSize = 40;
        private const float DefaultStaggerInterval = 0.32f;
        private const float StaggerLanePruneAge = 2f;
        private const float CombatLifetime = 1.05f;
        private const float CombatRiseSpeed = 0.42f;
        private const float CombatScaleGrowth = 0.12f;
        private const float CombatCharacterSize = 0.105f;
        private const int CombatFontSize = 34;

        private static readonly Dictionary<string, float> nextTextTimesByLane = new Dictionary<string, float>();
        private static readonly List<string> staleLaneKeys = new List<string>();

        private TextMesh textMesh;
        private Color startColor;
        private float age;
        private float delayRemaining;
        private float lifetime = DefaultLifetime;
        private float riseSpeed = DefaultRiseSpeed;
        private float scaleGrowth = DefaultScaleGrowth;
        private float worldScale = 1f;

        public static void Create(MazeRenderer renderer, Vector2Int gridPosition, int damage, Color color)
        {
            CreateText(renderer, gridPosition, $"-{damage}", color, 1.45f);
        }

        public static void CreateText(MazeRenderer renderer, Vector2Int gridPosition, string text, Color color, float height, float delay = 0f)
        {
            CreateText(
                renderer,
                gridPosition,
                text,
                color,
                height,
                DefaultCharacterSize,
                DefaultFontSize,
                DefaultLifetime,
                DefaultRiseSpeed,
                DefaultScaleGrowth,
                delay,
                true);
        }

        public static void CreateCombatText(
            MazeRenderer renderer,
            Vector2Int gridPosition,
            string text,
            Color color,
            float height,
            float delay = 0f)
        {
            CreateText(
                renderer,
                gridPosition,
                text,
                color,
                height,
                CombatCharacterSize,
                CombatFontSize,
                CombatLifetime,
                CombatRiseSpeed,
                CombatScaleGrowth,
                delay,
                false);
        }

        private static void CreateText(
            MazeRenderer renderer,
            Vector2Int gridPosition,
            string text,
            Color color,
            float height,
            float characterSize,
            int fontSize,
            float lifetime,
            float riseSpeed,
            float scaleGrowth,
            float delay,
            bool autoStagger)
        {
            if (renderer == null)
            {
                return;
            }

            var effectiveDelay = autoStagger
                ? ReserveStaggerDelay(renderer, gridPosition, delay)
                : Mathf.Max(0f, delay);
            var numberObject = new GameObject("Damage Number");
            numberObject.transform.position = renderer.GridToWorld(gridPosition) + new Vector3(0f, height * renderer.ModelUnitSize, 0f);
            numberObject.transform.rotation = Quaternion.Euler(62f, 45f, 0f);

            var view = numberObject.AddComponent<DamageNumberView>();
            view.Initialize(text, color, renderer.ModelUnitSize, characterSize, fontSize, lifetime, riseSpeed, scaleGrowth, effectiveDelay);
        }

        private static float ReserveStaggerDelay(MazeRenderer renderer, Vector2Int gridPosition, float requestedDelay)
        {
            var now = Time.time;
            PruneStaggerLanes(now);

            var requestedTime = now + Mathf.Max(0f, requestedDelay);
            var key = BuildStaggerLaneKey(renderer, gridPosition);
            if (!nextTextTimesByLane.TryGetValue(key, out var nextTime) || nextTime < requestedTime)
            {
                nextTime = requestedTime;
            }

            nextTextTimesByLane[key] = nextTime + DefaultStaggerInterval;
            return Mathf.Max(0f, nextTime - now);
        }

        private static void PruneStaggerLanes(float now)
        {
            staleLaneKeys.Clear();
            foreach (var pair in nextTextTimesByLane)
            {
                if (pair.Value < now - StaggerLanePruneAge)
                {
                    staleLaneKeys.Add(pair.Key);
                }
            }

            for (var i = 0; i < staleLaneKeys.Count; i++)
            {
                nextTextTimesByLane.Remove(staleLaneKeys[i]);
            }

            staleLaneKeys.Clear();
        }

        private static string BuildStaggerLaneKey(MazeRenderer renderer, Vector2Int gridPosition)
        {
            return $"{RuntimeHelpers.GetHashCode(renderer)}:{gridPosition.x}:{gridPosition.y}";
        }

        private void Update()
        {
            if (delayRemaining > 0f)
            {
                delayRemaining -= Time.deltaTime;
                if (delayRemaining > 0f)
                {
                    return;
                }

                age = 0f;
                textMesh.color = startColor;
            }

            age += Time.deltaTime;
            transform.position += Vector3.up * (Time.deltaTime * riseSpeed);

            var progress = Mathf.Clamp01(age / lifetime);
            var color = startColor;
            color.a = 1f - progress;
            textMesh.color = color;
            transform.localScale = Vector3.one * worldScale * (1f + progress * scaleGrowth);

            if (age >= lifetime)
            {
                Destroy(gameObject);
            }
        }

        private void Initialize(
            string text,
            Color color,
            float scale,
            float characterSize,
            int fontSize,
            float lifetime,
            float riseSpeed,
            float scaleGrowth,
            float delay)
        {
            startColor = color;
            worldScale = scale;
            this.lifetime = Mathf.Max(0.1f, lifetime);
            this.riseSpeed = Mathf.Max(0f, riseSpeed);
            this.scaleGrowth = Mathf.Max(0f, scaleGrowth);
            delayRemaining = Mathf.Max(0f, delay);
            textMesh = gameObject.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.characterSize = characterSize;
            textMesh.fontSize = fontSize;
            textMesh.color = delayRemaining > 0f ? new Color(color.r, color.g, color.b, 0f) : color;
        }
    }
}
