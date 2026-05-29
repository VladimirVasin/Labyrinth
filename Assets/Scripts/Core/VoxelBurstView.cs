using UnityEngine;

namespace Labyrinth.Core
{
    public sealed class VoxelBurstView : MonoBehaviour
    {
        private const float Lifetime = 0.72f;
        private const float Gravity = 1.8f;

        private readonly ParticleState[] particles = new ParticleState[14];
        private Material material;
        private Color startColor;
        private float age;
        private float delay;
        private int particleCount;

        public void Initialize(Color color, float unit, int count, float spread, float startDelay, bool impact)
        {
            startColor = new Color(color.r, color.g, color.b, 0.9f);
            delay = startDelay;
            particleCount = Mathf.Clamp(count, 1, particles.Length);
            material = VoxelVisuals.CreateLitMaterial(impact ? "Voxel Impact Particle" : "Voxel Pickup Particle", startColor);

            for (var i = 0; i < particleCount; i++)
            {
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = "Voxel Particle";
                cube.transform.SetParent(transform, false);
                cube.transform.localScale = Vector3.one * unit * (impact ? 0.08f : 0.065f);
                cube.GetComponent<Renderer>().sharedMaterial = material;
                var collider = cube.GetComponent<Collider>();
                if (collider != null)
                {
                    Destroy(collider);
                }

                var angle = (i / (float)particleCount) * Mathf.PI * 2f;
                var radius = unit * spread * (0.4f + Hash01(i, 5) * 0.7f);
                var vertical = unit * (impact ? 0.36f : 0.48f) * (0.65f + Hash01(i, 11) * 0.7f);
                var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                particles[i] = new ParticleState(
                    cube.transform,
                    direction * radius + Vector3.up * vertical,
                    new Vector3(
                        110f + Hash01(i, 17) * 180f,
                        80f + Hash01(i, 23) * 160f,
                        120f + Hash01(i, 31) * 200f));
                cube.SetActive(delay <= 0f);
            }
        }

        private void Update()
        {
            if (delay > 0f)
            {
                delay -= Time.deltaTime;
                if (delay > 0f)
                {
                    return;
                }

                for (var i = 0; i < particleCount; i++)
                {
                    particles[i].Transform.gameObject.SetActive(true);
                }
            }

            age += Time.deltaTime;
            var progress = Mathf.Clamp01(age / Lifetime);
            var fadeColor = startColor;
            fadeColor.a = 0.9f * (1f - progress);
            VoxelVisuals.ApplyMaterialProfile(material, fadeColor);

            for (var i = 0; i < particleCount; i++)
            {
                var particle = particles[i];
                var gravityOffset = Vector3.down * (Gravity * progress * progress * Time.deltaTime);
                particle.Transform.localPosition += (particle.Velocity * Time.deltaTime) + gravityOffset;
                particle.Transform.Rotate(particle.Spin * Time.deltaTime, Space.Self);
                particle.Transform.localScale *= 1f - Time.deltaTime * 0.45f;
            }

            if (age >= Lifetime)
            {
                Destroy(gameObject);
            }
        }

        private static float Hash01(int index, int salt)
        {
            unchecked
            {
                var hash = index * 73856093 ^ salt * 19349663;
                hash ^= hash >> 13;
                hash *= 1274126177;
                hash ^= hash >> 16;
                return (hash & 0x7fffffff) / (float)int.MaxValue;
            }
        }

        private readonly struct ParticleState
        {
            public ParticleState(Transform transform, Vector3 velocity, Vector3 spin)
            {
                Transform = transform;
                Velocity = velocity;
                Spin = spin;
            }

            public Transform Transform { get; }

            public Vector3 Velocity { get; }

            public Vector3 Spin { get; }
        }
    }
}
