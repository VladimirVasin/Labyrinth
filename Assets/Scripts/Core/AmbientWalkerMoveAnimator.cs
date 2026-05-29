using System.Collections.Generic;
using UnityEngine;

namespace Labyrinth.Core
{
    public sealed class AmbientWalkerMoveAnimator : MonoBehaviour
    {
        private const float MovementThresholdSqr = 0.0000006f;
        private const float FramesPerSecond = 8f;

        private readonly List<PartState> parts = new List<PartState>();
        private Vector3 previousPosition;
        private float unit = 1f;
        private float phase;
        private float moveBlend;
        private bool initialized;
        private bool appliedAnimation;

        public static AmbientWalkerMoveAnimator Attach(Transform root, float visualUnit, int seed)
        {
            if (root == null)
            {
                return null;
            }

            var animator = root.GetComponent<AmbientWalkerMoveAnimator>();
            if (animator == null)
            {
                animator = root.gameObject.AddComponent<AmbientWalkerMoveAnimator>();
            }

            animator.Initialize(visualUnit, seed);
            return animator;
        }

        public void Initialize(float visualUnit, int seed)
        {
            unit = Mathf.Max(0.01f, visualUnit);
            phase = ((seed & 0xffff) / 65535f) * Mathf.PI * 2f;
            previousPosition = transform.position;
            CaptureParts();
        }

        private void Awake()
        {
            previousPosition = transform.position;
        }

        private void LateUpdate()
        {
            if (!initialized)
            {
                CaptureParts();
            }

            var delta = transform.position - previousPosition;
            delta.y = 0f;
            previousPosition = transform.position;

            var moving = delta.sqrMagnitude > MovementThresholdSqr;
            var targetBlend = moving ? 1f : 0f;
            moveBlend = Mathf.MoveTowards(moveBlend, targetBlend, Time.deltaTime * (moving ? 8f : 5f));
            if (moveBlend <= 0.001f)
            {
                if (appliedAnimation)
                {
                    RestoreParts();
                    appliedAnimation = false;
                }

                return;
            }

            var distance = Mathf.Sqrt(delta.sqrMagnitude);
            phase += Mathf.Max(distance / Mathf.Max(0.01f, unit) * 1.35f, Time.deltaTime * 0.28f);
            var steppedPhase = Mathf.Floor(phase * FramesPerSecond) / FramesPerSecond;
            var wave = Mathf.Sin(steppedPhase * Mathf.PI * 2f);
            var impact = Mathf.Abs(Mathf.Cos(steppedPhase * Mathf.PI * 2f));
            var side = Mathf.Sign(Mathf.Abs(wave) < 0.001f ? 1f : wave);

            for (var i = 0; i < parts.Count; i++)
            {
                ApplyPart(parts[i], wave, impact, side);
            }

            appliedAnimation = true;
        }

        private void CaptureParts()
        {
            parts.Clear();
            for (var i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (ShouldSkip(child.name))
                {
                    continue;
                }

                parts.Add(new PartState(child, Classify(child.name)));
            }

            initialized = true;
        }

        private void RestoreParts()
        {
            for (var i = 0; i < parts.Count; i++)
            {
                var part = parts[i];
                if (part.Transform == null)
                {
                    continue;
                }

                part.Transform.localPosition = part.BasePosition;
                part.Transform.localRotation = part.BaseRotation;
                part.Transform.localScale = part.BaseScale;
            }
        }

        private void ApplyPart(PartState part, float wave, float impact, float side)
        {
            if (part.Transform == null)
            {
                return;
            }

            var blend = moveBlend;
            var bob = impact * unit * 0.055f * blend;
            var sway = wave * unit * 0.025f * blend;
            var tilt = wave * 7f * blend;
            var squash = impact * 0.035f * blend;

            switch (part.Role)
            {
                case PartRole.Body:
                    part.Transform.localPosition = part.BasePosition + new Vector3(sway * 0.35f, bob, 0f);
                    part.Transform.localRotation = part.BaseRotation * Quaternion.Euler(0f, 0f, -tilt);
                    part.Transform.localScale = Vector3.Scale(part.BaseScale, new Vector3(1f + squash * 0.55f, 1f - squash, 1f + squash * 0.35f));
                    return;
                case PartRole.Head:
                    part.Transform.localPosition = part.BasePosition + new Vector3(sway * 0.55f, bob * 1.15f, 0f);
                    part.Transform.localRotation = part.BaseRotation * Quaternion.Euler(tilt * 0.25f, -tilt * 0.18f, tilt * 0.45f);
                    return;
                case PartRole.LeftFoot:
                    ApplyFoot(part, wave, 1f, blend);
                    return;
                case PartRole.RightFoot:
                    ApplyFoot(part, -wave, -1f, blend);
                    return;
                case PartRole.Tool:
                    part.Transform.localPosition = part.BasePosition + new Vector3(-sway * 0.7f, bob * 0.45f, 0f);
                    part.Transform.localRotation = part.BaseRotation * Quaternion.Euler(tilt * 1.1f, 0f, -tilt * 1.3f);
                    return;
                case PartRole.Carried:
                    part.Transform.localPosition = part.BasePosition + new Vector3(-sway * 0.45f, bob * 0.75f, 0f);
                    part.Transform.localRotation = part.BaseRotation * Quaternion.Euler(0f, tilt * 0.18f, -tilt * 0.75f);
                    return;
                default:
                    part.Transform.localPosition = part.BasePosition + new Vector3(sway * 0.2f, bob * 0.55f, 0f);
                    part.Transform.localRotation = part.BaseRotation * Quaternion.Euler(0f, 0f, -tilt * 0.35f);
                    return;
            }
        }

        private void ApplyFoot(PartState part, float wave, float side, float blend)
        {
            var step = Mathf.Max(0f, wave);
            var slide = wave * unit * 0.055f * blend;
            var lift = step * unit * 0.09f * blend;
            part.Transform.localPosition = part.BasePosition + new Vector3(side * unit * 0.012f * blend, lift, slide);
            part.Transform.localRotation = part.BaseRotation * Quaternion.Euler(-wave * 22f * blend, 0f, side * step * 8f * blend);
        }

        private static bool ShouldSkip(string name)
        {
            return Contains(name, "Shadow")
                || Contains(name, "Light")
                || Contains(name, "Voxel")
                || Contains(name, "Label")
                || Contains(name, "Marker")
                || Contains(name, "Glow");
        }

        private static PartRole Classify(string name)
        {
            if ((Contains(name, "Left") || Contains(name, " L")) && (Contains(name, "Foot") || Contains(name, "Leg")))
            {
                return PartRole.LeftFoot;
            }

            if ((Contains(name, "Right") || Contains(name, " R")) && (Contains(name, "Foot") || Contains(name, "Leg")))
            {
                return PartRole.RightFoot;
            }

            if (Contains(name, "Body"))
            {
                return PartRole.Body;
            }

            if (Contains(name, "Head") || Contains(name, "Face"))
            {
                return PartRole.Head;
            }

            if (Contains(name, "Pack")
                || Contains(name, "Bag")
                || Contains(name, "Basket")
                || Contains(name, "Cargo")
                || Contains(name, "Coin")
                || Contains(name, "Timber"))
            {
                return PartRole.Carried;
            }

            if (Contains(name, "Pick")
                || Contains(name, "Axe")
                || Contains(name, "Hammer")
                || Contains(name, "Spear")
                || Contains(name, "Shield")
                || Contains(name, "Bottle")
                || Contains(name, "Mug")
                || Contains(name, "Scroll")
                || Contains(name, "Cross"))
            {
                return PartRole.Tool;
            }

            return PartRole.Accessory;
        }

        private static bool Contains(string source, string value)
        {
            return !string.IsNullOrEmpty(source) && source.IndexOf(value, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private enum PartRole
        {
            Accessory,
            Body,
            Head,
            LeftFoot,
            RightFoot,
            Tool,
            Carried
        }

        private readonly struct PartState
        {
            public PartState(Transform transform, PartRole role)
            {
                Transform = transform;
                Role = role;
                BasePosition = transform.localPosition;
                BaseRotation = transform.localRotation;
                BaseScale = transform.localScale;
            }

            public Transform Transform { get; }
            public PartRole Role { get; }
            public Vector3 BasePosition { get; }
            public Quaternion BaseRotation { get; }
            public Vector3 BaseScale { get; }
        }
    }
}
