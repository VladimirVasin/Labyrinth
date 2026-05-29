using UnityEngine;

namespace Labyrinth.Core
{
    public sealed class VoxelFigurePartAnimator : MonoBehaviour
    {
        private const float IdleFramesPerSecond = 7f;
        private const float AccessoryFramesPerSecond = 9f;

        private Vector3 baseLocalPosition;
        private Quaternion baseLocalRotation;
        private Vector3 baseLocalScale;
        private float phase;
        private float amplitude;
        private bool accessory;
        private bool initialized;

        public void Initialize(int seed, bool accessoryPart)
        {
            accessory = accessoryPart;
            phase = (seed & 0xffff) / 65535f * Mathf.PI * 2f;
            amplitude = accessory ? 0.012f : 0.008f;
            CaptureBaseTransform();
        }

        private void Awake()
        {
            CaptureBaseTransform();
        }

        private void Update()
        {
            if (!initialized)
            {
                CaptureBaseTransform();
            }

            var fps = accessory ? AccessoryFramesPerSecond : IdleFramesPerSecond;
            var stepped = Mathf.Floor((Time.time + phase) * fps) / fps;
            var wave = Mathf.Sin((stepped * Mathf.PI * 2f) + phase);
            var secondary = Mathf.Cos((stepped * Mathf.PI * 3.1f) + phase * 0.7f);

            transform.localPosition = baseLocalPosition + new Vector3(secondary * amplitude * 0.35f, wave * amplitude, 0f);
            transform.localRotation = baseLocalRotation * Quaternion.Euler(
                accessory ? wave * 1.6f : wave * 0.65f,
                accessory ? secondary * 1.2f : secondary * 0.45f,
                accessory ? secondary * 1.8f : 0f);
            var scalePulse = 1f + wave * (accessory ? 0.012f : 0.006f);
            transform.localScale = baseLocalScale * scalePulse;
        }

        private void CaptureBaseTransform()
        {
            if (initialized)
            {
                return;
            }

            baseLocalPosition = transform.localPosition;
            baseLocalRotation = transform.localRotation;
            baseLocalScale = transform.localScale;
            initialized = true;
        }
    }
}
