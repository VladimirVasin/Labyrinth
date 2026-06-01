using System.Collections.Generic;
using Labyrinth.Core;
using Labyrinth.Maze;
using UnityEngine;

namespace Labyrinth.Hero
{
    public sealed class HeroView : MonoBehaviour
    {
        private const float MoveDuration = 0.26f;
        private const float RotationSpeed = 720f;
        private const float WalkAnimationSpeed = 10f;
        private const float MapClickColliderHeight = 1.42f;
        private const float LanternLightBaseIntensity = 9.2f;
        private const float LanternLightRangeCells = 6.8f;
        private static readonly Vector3 VisualFootprintScale = new Vector3(0.78f, 0.92f, 0.78f);

        private static int nextLaneSerial;

        private readonly List<Vector3> movePath = new List<Vector3>();
        private MazeRenderer mazeRenderer;
        private Vector3 moveStartPosition;
        private Vector3 targetPosition;
        private Transform visualRoot;
        private Transform leftLegPivot;
        private Transform rightLegPivot;
        private Transform swordArmPivot;
        private Transform shieldArmPivot;
        private Transform cape;
        private Transform bodyArmor;
        private Transform chestPlate;
        private Transform belt;
        private Transform head;
        private Transform helmetDome;
        private Transform helmetVisor;
        private Transform helmetCrest;
        private Transform lanternGlow;
        private Light lanternLight;
        private Transform leftBoot;
        private Transform rightBoot;
        private GameObject selectionMarker;
        private float animationTime;
        private float attackTimer;
        private Vector3 attackLocalDirection;
        private bool defeated;
        private float moveTimer;
        private float activeMoveSpeed;
        private int moveWaypointIndex;
        private int laneSeed;
        private Vector2Int visualGridPosition;

        private const float AttackDuration = 0.28f;

        public HeroController Controller { get; private set; }

        public static HeroView Create(MazeRenderer mazeRenderer, Vector2Int startPosition)
        {
            var heroObject = new GameObject("Hero Knight");
            var view = heroObject.AddComponent<HeroView>();
            view.Initialize(mazeRenderer, startPosition);
            return view;
        }

        public void SetController(HeroController controller)
        {
            Controller = controller;
        }

        public void MoveTo(Vector2Int gridPosition)
        {
            var nextPath = SubCellPathBuilder.BuildStep(
                mazeRenderer,
                visualGridPosition,
                gridPosition,
                0f,
                laneSeed,
                SubCellPathProfile.Hero,
                transform.position);
            if (nextPath.Count == 0)
            {
                return;
            }

            var nextTarget = nextPath[nextPath.Count - 1];
            if ((nextTarget - targetPosition).sqrMagnitude <= 0.0001f && movePath.Count == 0)
            {
                return;
            }

            movePath.Clear();
            movePath.AddRange(nextPath);
            moveWaypointIndex = Mathf.Min(1, movePath.Count);
            moveStartPosition = transform.position;
            targetPosition = nextTarget;
            activeMoveSpeed = Mathf.Max(0.01f, SubCellPathBuilder.CalculateLength(movePath) / MoveDuration);
            moveTimer = 0f;
            visualGridPosition = gridPosition;
            GameAudioController.Play(GameSfx.Footstep, nextTarget);
        }

        public void SetGridPositionImmediate(Vector2Int gridPosition)
        {
            targetPosition = ToWorldPosition(gridPosition);
            moveStartPosition = targetPosition;
            moveTimer = MoveDuration;
            movePath.Clear();
            moveWaypointIndex = 0;
            visualGridPosition = gridPosition;
            transform.position = targetPosition;
        }

        public void FaceGridPosition(Vector2Int gridPosition)
        {
            var direction = Vector3.ProjectOnPlane(ToWorldPosition(gridPosition) - transform.position, Vector3.up);
            if (direction.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            }
        }

        public void PlayAttack(Vector2Int targetGridPosition)
        {
            if (defeated)
            {
                return;
            }

            FaceGridPosition(targetGridPosition);
            var direction = Vector3.ProjectOnPlane(ToWorldPosition(targetGridPosition) - transform.position, Vector3.up).normalized;
            attackLocalDirection = transform.InverseTransformDirection(direction);
            attackTimer = AttackDuration;
        }

        public void SetSelected(bool selected)
        {
            if (selectionMarker != null)
            {
                selectionMarker.SetActive(selected);
            }
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public void SetDefeated()
        {
            defeated = true;
            targetPosition = transform.position;
            moveStartPosition = transform.position;
            moveTimer = MoveDuration;
            if (visualRoot != null)
            {
                visualRoot.localRotation = Quaternion.Euler(0f, 0f, 76f);
                visualRoot.localPosition = new Vector3(0f, 0.16f, 0f);
                visualRoot.localScale = VisualFootprintScale;
            }
        }

        private void Update()
        {
            if (defeated)
            {
                return;
            }

            var isMoving = movePath.Count > 0 && moveWaypointIndex < movePath.Count;

            if (isMoving)
            {
                moveTimer += Time.deltaTime;
                var direction = Vector3.ProjectOnPlane(movePath[moveWaypointIndex] - transform.position, Vector3.up).normalized;
                if (direction.sqrMagnitude > 0.01f)
                {
                    var targetRotation = Quaternion.LookRotation(direction, Vector3.up);
                    transform.rotation = Quaternion.RotateTowards(
                        transform.rotation,
                        targetRotation,
                        RotationSpeed * Time.deltaTime);
                }

                MoveAlongPath(activeMoveSpeed * Time.deltaTime);
                if (moveWaypointIndex >= movePath.Count)
                {
                    moveStartPosition = targetPosition;
                    transform.position = targetPosition;
                    movePath.Clear();
                }
            }

            AnimateKnight(isMoving);
        }

        private void Initialize(MazeRenderer renderer, Vector2Int startPosition)
        {
            mazeRenderer = renderer;
            laneSeed = BuildLaneSeed(startPosition, ++nextLaneSerial);
            visualGridPosition = startPosition;
            BuildKnightModel();
            AddMapClickCollider();
            transform.localScale = Vector3.one * renderer.ModelUnitSize;
            SetGridPositionImmediate(startPosition);
        }

        private void MoveAlongPath(float distance)
        {
            var remaining = distance;
            while (remaining > 0f && moveWaypointIndex < movePath.Count)
            {
                var target = movePath[moveWaypointIndex];
                var offset = target - transform.position;
                var stepDistance = offset.magnitude;
                if (stepDistance <= Mathf.Max(remaining, 0.001f))
                {
                    transform.position = target;
                    remaining -= stepDistance;
                    moveWaypointIndex++;
                    continue;
                }

                transform.position += offset / stepDistance * remaining;
                remaining = 0f;
            }
        }

        private static int BuildLaneSeed(Vector2Int startPosition, int serial)
        {
            return serial * 265443576
                ^ startPosition.x * 73856093
                ^ startPosition.y * 19349663
                ^ 0x6a57;
        }

        private void AddMapClickCollider()
        {
            var collider = gameObject.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, MapClickColliderHeight * 0.5f, 0f);
            collider.size = new Vector3(0.72f, MapClickColliderHeight, 0.72f);
        }

        private void BuildKnightModel()
        {
            visualRoot = new GameObject("Knight Visual").transform;
            visualRoot.SetParent(transform, false);
            visualRoot.localScale = VisualFootprintScale;

            var armor = CreateMaterial("Knight Armor", new Color(0.72f, 0.74f, 0.78f));
            var darkArmor = CreateMaterial("Knight Dark Armor", new Color(0.32f, 0.34f, 0.38f));
            var cloth = CreateMaterial("Knight Tabard", new Color(0.12f, 0.22f, 0.68f));
            var capeMaterial = CreateMaterial("Knight Cape", new Color(0.55f, 0.05f, 0.08f));
            var skin = CreateMaterial("Knight Face", new Color(0.86f, 0.62f, 0.42f));
            var leather = CreateMaterial("Knight Leather", new Color(0.22f, 0.12f, 0.05f));
            var blade = CreateMaterial("Knight Sword", new Color(0.88f, 0.9f, 0.95f));
            var shield = CreateMaterial("Knight Shield", new Color(0.7f, 0.04f, 0.05f));
            var lantern = VoxelVisuals.CreateEmissiveMaterial("Knight Lantern", new Color(1f, 0.58f, 0.16f), 1.55f);
            var selection = CreateSelectionMaterial("Hero Selection", new Color(1f, 0.82f, 0.22f, 0.36f));

            VoxelVisuals.CreateContactShadow(
                "Hero Contact Shadow",
                transform,
                new Vector3(0f, 0.006f, 0f),
                new Vector3(0.68f, 0.004f, 0.56f),
                0.42f);

            bodyArmor = CreatePart("Body Armor", visualRoot, PrimitiveType.Cube, new Vector3(0f, 0.62f, 0f), new Vector3(0.42f, 0.54f, 0.26f), armor);
            chestPlate = CreatePart("Chest Plate", visualRoot, PrimitiveType.Cube, new Vector3(0f, 0.68f, 0.15f), new Vector3(0.34f, 0.38f, 0.05f), cloth);
            belt = CreatePart("Belt", visualRoot, PrimitiveType.Cube, new Vector3(0f, 0.42f, -0.01f), new Vector3(0.48f, 0.08f, 0.3f), leather);
            cape = CreatePart("Cape", visualRoot, PrimitiveType.Cube, new Vector3(0f, 0.61f, -0.19f), new Vector3(0.46f, 0.62f, 0.06f), capeMaterial);

            head = CreatePart("Head", visualRoot, PrimitiveType.Sphere, new Vector3(0f, 1.02f, 0.01f), new Vector3(0.3f, 0.28f, 0.3f), skin);
            helmetDome = CreatePart("Helmet Dome", visualRoot, PrimitiveType.Sphere, new Vector3(0f, 1.08f, 0.01f), new Vector3(0.34f, 0.24f, 0.34f), armor);
            helmetVisor = CreatePart("Helmet Visor", visualRoot, PrimitiveType.Cube, new Vector3(0f, 1.03f, 0.18f), new Vector3(0.32f, 0.08f, 0.06f), darkArmor);
            helmetCrest = CreatePart("Helmet Crest", visualRoot, PrimitiveType.Cube, new Vector3(0f, 1.27f, 0f), new Vector3(0.08f, 0.2f, 0.38f), capeMaterial);

            leftLegPivot = CreatePivot("Left Leg Pivot", new Vector3(-0.13f, 0.42f, 0f));
            rightLegPivot = CreatePivot("Right Leg Pivot", new Vector3(0.13f, 0.42f, 0f));
            CreatePart("Left Greave", leftLegPivot, PrimitiveType.Cube, new Vector3(0f, -0.22f, -0.01f), new Vector3(0.14f, 0.44f, 0.14f), darkArmor);
            CreatePart("Right Greave", rightLegPivot, PrimitiveType.Cube, new Vector3(0f, -0.22f, -0.01f), new Vector3(0.14f, 0.44f, 0.14f), darkArmor);
            leftBoot = CreatePart("Left Boot", leftLegPivot, PrimitiveType.Cube, new Vector3(0f, -0.46f, 0.06f), new Vector3(0.18f, 0.08f, 0.24f), leather);
            rightBoot = CreatePart("Right Boot", rightLegPivot, PrimitiveType.Cube, new Vector3(0f, -0.46f, 0.06f), new Vector3(0.18f, 0.08f, 0.24f), leather);

            shieldArmPivot = CreatePivot("Shield Arm Pivot", new Vector3(-0.3f, 0.82f, 0f));
            swordArmPivot = CreatePivot("Sword Arm Pivot", new Vector3(0.3f, 0.82f, 0f));
            CreatePart("Shield Arm", shieldArmPivot, PrimitiveType.Cube, new Vector3(-0.02f, -0.2f, 0f), new Vector3(0.12f, 0.4f, 0.12f), armor);
            CreatePart("Sword Arm", swordArmPivot, PrimitiveType.Cube, new Vector3(0.02f, -0.2f, 0f), new Vector3(0.12f, 0.4f, 0.12f), armor);
            CreatePart("Shield", shieldArmPivot, PrimitiveType.Cube, new Vector3(-0.11f, -0.18f, 0.17f), new Vector3(0.08f, 0.44f, 0.34f), shield);
            CreatePart("Shield Boss", shieldArmPivot, PrimitiveType.Sphere, new Vector3(-0.16f, -0.18f, 0.34f), new Vector3(0.12f, 0.12f, 0.06f), armor);
            lanternGlow = CreatePart("Lantern Flame", shieldArmPivot, PrimitiveType.Cube, new Vector3(-0.19f, -0.46f, 0.22f), new Vector3(0.12f, 0.16f, 0.12f), lantern);
            lanternLight = CreateLanternLight(shieldArmPivot, new Vector3(-0.19f, -0.36f, 0.22f));
            CreatePart("Sword Blade", swordArmPivot, PrimitiveType.Cube, new Vector3(0.12f, 0.08f, 0.08f), new Vector3(0.05f, 0.72f, 0.05f), blade);
            CreatePart("Sword Guard", swordArmPivot, PrimitiveType.Cube, new Vector3(0.12f, -0.25f, 0.08f), new Vector3(0.25f, 0.05f, 0.05f), leather);

            selectionMarker = CreateSelectionRing(selection);
            selectionMarker.SetActive(false);
            SetIdlePose();
        }

        private void AnimateKnight(bool isMoving)
        {
            if (visualRoot == null)
            {
                return;
            }

            if (!isMoving)
            {
                visualRoot.localPosition = GetAttackOffset();
                if (attackTimer <= 0f)
                {
                    SetIdlePose();
                }

                AnimateLantern();
                return;
            }

            var attacking = attackTimer > 0f;
            var attackOffset = GetAttackOffset();
            animationTime += Time.deltaTime * WalkAnimationSpeed;
            var wave = Mathf.Sin(animationTime);
            var counter = Mathf.Cos(animationTime);
            var stepImpact = Mathf.Abs(counter);
            var leftLift = Mathf.Max(0f, wave);
            var rightLift = Mathf.Max(0f, -wave);
            var bob = 0.012f + stepImpact * 0.045f;
            var sway = wave * 0.024f;
            var squash = stepImpact * 0.018f;

            visualRoot.localPosition = new Vector3(sway * 0.35f, bob, counter * 0.008f) + attackOffset;
            visualRoot.localRotation = Quaternion.Euler(counter * 1.4f, wave * 1.6f, -wave * 3.2f);
            visualRoot.localScale = Vector3.Scale(
                VisualFootprintScale,
                new Vector3(1f + squash * 0.45f, 1f - squash, 1f + squash * 0.25f));
            SetBodyOffset(new Vector3(sway * 0.28f, stepImpact * 0.01f, 0f), Quaternion.Euler(counter * 1.2f, 0f, -wave * 2.4f));
            SetHeadOffset(new Vector3(-sway * 0.35f, stepImpact * 0.016f, 0f), Quaternion.Euler(-counter * 0.9f, wave * 1.1f, wave * 2.2f));

            leftLegPivot.localRotation = Quaternion.Euler(wave * 30f, 0f, -leftLift * 5f);
            rightLegPivot.localRotation = Quaternion.Euler(-wave * 30f, 0f, rightLift * 5f);
            leftBoot.localPosition = new Vector3(0f, -0.46f + leftLift * 0.065f, 0.06f + leftLift * 0.035f);
            rightBoot.localPosition = new Vector3(0f, -0.46f + rightLift * 0.065f, 0.06f + rightLift * 0.035f);
            leftBoot.localRotation = Quaternion.Euler(-wave * 12f, 0f, -leftLift * 8f);
            rightBoot.localRotation = Quaternion.Euler(wave * 12f, 0f, rightLift * 8f);

            if (!attacking)
            {
                swordArmPivot.localRotation = Quaternion.Euler(-wave * 12f - stepImpact * 2f, 0f, -14f - counter * 3f);
            }

            shieldArmPivot.localRotation = Quaternion.Euler(wave * 10f, 0f, 14f + counter * 2.5f);
            cape.localPosition = new Vector3(0f, 0.61f + stepImpact * 0.012f, -0.19f - stepImpact * 0.018f);
            cape.localRotation = Quaternion.Euler(-8f - counter * 5f, wave * 1.5f, -wave * 2f);
            AnimateLantern();
        }

        private Vector3 GetAttackOffset()
        {
            if (attackTimer <= 0f)
            {
                return Vector3.zero;
            }

            var phase = 1f - attackTimer / AttackDuration;
            attackTimer = Mathf.Max(0f, attackTimer - Time.deltaTime);
            swordArmPivot.localRotation = Quaternion.Euler(-65f * Mathf.Sin(phase * Mathf.PI), 0f, -18f);
            return attackLocalDirection * (Mathf.Sin(phase * Mathf.PI) * 0.18f);
        }

        private void SetIdlePose()
        {
            visualRoot.localRotation = Quaternion.identity;
            visualRoot.localScale = VisualFootprintScale;
            SetBodyOffset(Vector3.zero, Quaternion.identity);
            SetHeadOffset(Vector3.zero, Quaternion.identity);
            leftLegPivot.localRotation = Quaternion.identity;
            rightLegPivot.localRotation = Quaternion.identity;
            leftBoot.localPosition = new Vector3(0f, -0.46f, 0.06f);
            rightBoot.localPosition = new Vector3(0f, -0.46f, 0.06f);
            leftBoot.localRotation = Quaternion.identity;
            rightBoot.localRotation = Quaternion.identity;
            swordArmPivot.localRotation = Quaternion.Euler(0f, 0f, -14f);
            shieldArmPivot.localRotation = Quaternion.Euler(0f, 0f, 14f);
            cape.localPosition = new Vector3(0f, 0.61f, -0.19f);
            cape.localRotation = Quaternion.identity;
        }

        private void AnimateLantern()
        {
            if (lanternGlow == null)
            {
                return;
            }

            var pulse = 1f + Mathf.Sin(Time.time * 7.3f) * 0.08f + Mathf.Sin(Time.time * 11.7f) * 0.035f;
            lanternGlow.localScale = new Vector3(0.12f, 0.16f * pulse, 0.12f);
            lanternGlow.localRotation = Quaternion.Euler(0f, Time.time * 35f, 0f);
            if (lanternLight != null)
            {
                lanternLight.intensity = LanternLightBaseIntensity * Mathf.Clamp(pulse, 0.88f, 1.1f);
            }
        }

        private void SetBodyOffset(Vector3 offset, Quaternion rotation)
        {
            bodyArmor.localPosition = new Vector3(0f, 0.62f, 0f) + offset;
            chestPlate.localPosition = new Vector3(0f, 0.68f, 0.15f) + offset;
            belt.localPosition = new Vector3(0f, 0.42f, -0.01f) + offset * 0.6f;
            bodyArmor.localRotation = rotation;
            chestPlate.localRotation = rotation;
            belt.localRotation = rotation;
        }

        private void SetHeadOffset(Vector3 offset, Quaternion rotation)
        {
            head.localPosition = new Vector3(0f, 1.02f, 0.01f) + offset;
            helmetDome.localPosition = new Vector3(0f, 1.08f, 0.01f) + offset;
            helmetVisor.localPosition = new Vector3(0f, 1.03f, 0.18f) + offset;
            helmetCrest.localPosition = new Vector3(0f, 1.27f, 0f) + offset * 1.15f;
            head.localRotation = rotation;
            helmetDome.localRotation = rotation;
            helmetVisor.localRotation = rotation;
            helmetCrest.localRotation = rotation * Quaternion.Euler(-Mathf.Sign(offset.x) * 1.2f, 0f, 0f);
        }

        private Transform CreatePivot(string pivotName, Vector3 localPosition)
        {
            var pivot = new GameObject(pivotName).transform;
            pivot.SetParent(visualRoot, false);
            pivot.localPosition = localPosition;
            return pivot;
        }

        private Transform CreatePart(
            string partName,
            Transform parent,
            PrimitiveType primitiveType,
            Vector3 localPosition,
            Vector3 localScale,
            Material material)
        {
            var part = GameObject.CreatePrimitive(VoxelVisuals.ResolvePrimitive(primitiveType, partName));
            part.name = partName;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            part.GetComponent<Renderer>().sharedMaterial = material;

            var collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            VoxelVisuals.ApplyBlockStyle(part, primitiveType, material, false);
            return part.transform;
        }

        private GameObject CreateSelectionRing(Material material)
        {
            var marker = new GameObject("Selection Marker");
            marker.transform.SetParent(transform, false);
            marker.transform.localPosition = new Vector3(0f, 0.018f, 0f);
            marker.transform.localRotation = Quaternion.identity;

            const int segments = 64;
            const float innerRadius = 0.29f;
            const float outerRadius = 0.41f;
            var vertices = new Vector3[segments * 2];
            var colors = new Color[segments * 2];
            var uvs = new Vector2[segments * 2];
            var triangles = new int[segments * 6];

            for (var i = 0; i < segments; i++)
            {
                var t = i / (float)segments * Mathf.PI * 2f;
                var direction = new Vector3(Mathf.Cos(t), 0f, Mathf.Sin(t));
                vertices[i * 2] = direction * innerRadius;
                vertices[i * 2 + 1] = direction * outerRadius;
                colors[i * 2] = new Color(1f, 0.86f, 0.24f, 0.1f);
                colors[i * 2 + 1] = new Color(1f, 0.86f, 0.24f, 0.36f);
                uvs[i * 2] = new Vector2(0f, i / (float)segments);
                uvs[i * 2 + 1] = new Vector2(1f, i / (float)segments);

                var next = (i + 1) % segments;
                var triangleIndex = i * 6;
                triangles[triangleIndex] = i * 2;
                triangles[triangleIndex + 1] = next * 2;
                triangles[triangleIndex + 2] = i * 2 + 1;
                triangles[triangleIndex + 3] = i * 2 + 1;
                triangles[triangleIndex + 4] = next * 2;
                triangles[triangleIndex + 5] = next * 2 + 1;
            }

            var mesh = new Mesh { name = "Selection Ring Mesh" };
            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();

            marker.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = marker.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return marker;
        }

        private Light CreateLanternLight(Transform parent, Vector3 localPosition)
        {
            var lightObject = new GameObject("Knight Lantern Point Light");
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.localPosition = localPosition;
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.68f, 0.34f);
            light.range = mazeRenderer.CellSize * LanternLightRangeCells;
            light.intensity = LanternLightBaseIntensity;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.34f;
            light.shadowBias = 0.04f;
            light.shadowNormalBias = 0.24f;
            light.bounceIntensity = 0.55f;
            light.renderMode = LightRenderMode.ForcePixel;
            return light;
        }

        private Vector3 ToWorldPosition(Vector2Int gridPosition)
        {
            return mazeRenderer.GridToWorld(gridPosition);
        }

        private static Material CreateMaterial(string materialName, Color color)
        {
            return VoxelVisuals.CreateLitMaterial(materialName, color);
        }

        private static Material CreateSelectionMaterial(string materialName, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            if (shader == null)
            {
                return CreateMaterial(materialName, color);
            }

            var material = new Material(shader)
            {
                name = materialName,
                color = color
            };
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = 3100;
            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
            }

            if (material.HasProperty("_SrcBlend"))
            {
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            }

            if (material.HasProperty("_DstBlend"))
            {
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            }

            if (material.HasProperty("_ZWrite"))
            {
                material.SetInt("_ZWrite", 0);
            }

            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.EnableKeyword("_ALPHABLEND_ON");
            return material;
        }
    }
}
