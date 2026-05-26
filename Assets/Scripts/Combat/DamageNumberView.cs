using Labyrinth.Maze;
using UnityEngine;

namespace Labyrinth.Combat
{
    public sealed class DamageNumberView : MonoBehaviour
    {
        private const float Lifetime = 0.85f;

        private TextMesh textMesh;
        private Color startColor;
        private float age;
        private float worldScale = 1f;

        public static void Create(MazeRenderer renderer, Vector2Int gridPosition, int damage, Color color)
        {
            CreateText(renderer, gridPosition, $"-{damage}", color, 1.45f);
        }

        public static void CreateText(MazeRenderer renderer, Vector2Int gridPosition, string text, Color color, float height)
        {
            if (renderer == null)
            {
                return;
            }

            var numberObject = new GameObject("Damage Number");
            numberObject.transform.position = renderer.GridToWorld(gridPosition) + new Vector3(0f, height * renderer.ModelUnitSize, 0f);
            numberObject.transform.rotation = Quaternion.Euler(62f, 45f, 0f);

            var view = numberObject.AddComponent<DamageNumberView>();
            view.Initialize(text, color, renderer.ModelUnitSize);
        }

        private void Update()
        {
            age += Time.deltaTime;
            transform.position += Vector3.up * (Time.deltaTime * 0.9f);

            var progress = Mathf.Clamp01(age / Lifetime);
            var color = startColor;
            color.a = 1f - progress;
            textMesh.color = color;
            transform.localScale = Vector3.one * worldScale * (1f + progress * 0.35f);

            if (age >= Lifetime)
            {
                Destroy(gameObject);
            }
        }

        private void Initialize(string text, Color color, float scale)
        {
            startColor = color;
            worldScale = scale;
            textMesh = gameObject.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.characterSize = 0.16f;
            textMesh.fontSize = 48;
            textMesh.color = color;
        }
    }
}
