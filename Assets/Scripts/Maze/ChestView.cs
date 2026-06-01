using Labyrinth.Core;
using UnityEngine;

namespace Labyrinth.Maze
{
    public sealed class ChestView : MonoBehaviour
    {
        private const float OpenDuration = 2f;
        private Transform lidPivot;
        private Quaternion closedRotation;
        private Quaternion openRotation;
        private float animationTime;
        private bool isOpening;
        private bool isOpened;
        private float worldScale = 1f;

        public void Initialize(Transform lidPivotTransform, float scale)
        {
            lidPivot = lidPivotTransform;
            worldScale = scale;
            closedRotation = lidPivot.localRotation;
            openRotation = Quaternion.Euler(-72f, 0f, 0f);
        }

        public void PlayOpen()
        {
            if (isOpened || lidPivot == null)
            {
                return;
            }

            isOpening = true;
            isOpened = true;
            animationTime = 0f;
            GameAudioController.Play(GameSfx.ChestOpen, transform.position);
        }

        public void ShowOpenedImmediate()
        {
            isOpened = true;
            isOpening = false;
            if (lidPivot != null)
            {
                lidPivot.localRotation = openRotation;
            }
        }

        private void Update()
        {
            if (!isOpening || lidPivot == null)
            {
                return;
            }

            animationTime += Time.deltaTime;
            var progress = Mathf.Clamp01(animationTime / OpenDuration);
            var eased = 1f - Mathf.Pow(1f - progress, 3f);
            lidPivot.localRotation = Quaternion.Slerp(closedRotation, openRotation, eased);

            if (progress >= 1f)
            {
                isOpening = false;
            }
        }

        private sealed class ChestRewardTextView : MonoBehaviour
        {
            private const float Lifetime = 1f;

            private TextMesh textMesh;
            private Color startColor;
            private float age;
            private float scale;

            public static void Create(Vector3 position, float worldScale, string text)
            {
                var textObject = new GameObject("Chest Reward Text");
                textObject.transform.position = position + new Vector3(0f, worldScale * 0.72f, 0f);
                textObject.transform.rotation = Quaternion.Euler(62f, 45f, 0f);

                var view = textObject.AddComponent<ChestRewardTextView>();
                view.Initialize(text, worldScale);
            }

            private void Initialize(string text, float worldScale)
            {
                scale = worldScale;
                startColor = new Color(1f, 0.82f, 0.2f);
                textMesh = gameObject.AddComponent<TextMesh>();
                textMesh.text = text;
                textMesh.anchor = TextAnchor.MiddleCenter;
                textMesh.alignment = TextAlignment.Center;
                textMesh.characterSize = 0.065f;
                textMesh.fontSize = 30;
                textMesh.color = startColor;
            }

            private void Update()
            {
                age += Time.deltaTime;
                transform.position += Vector3.up * (Time.deltaTime * 0.48f);

                var progress = Mathf.Clamp01(age / Lifetime);
                var color = startColor;
                color.a = 1f - progress;
                textMesh.color = color;
                transform.localScale = Vector3.one * scale * (1f + progress * 0.08f);

                if (age >= Lifetime)
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}
