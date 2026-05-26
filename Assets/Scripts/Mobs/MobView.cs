using Labyrinth.Maze;
using UnityEngine;

namespace Labyrinth.Mobs
{
    public sealed class MobView : MonoBehaviour
    {
        private const float MoveSpeed = 3.4f;
        private const float WalkAnimationSpeed = 7.5f;
        private const float AttackDuration = 0.3f;

        private MazeRenderer mazeRenderer;
        private Vector3 targetPosition;
        private Transform visualRoot;
        private Transform leftLegPivot;
        private Transform rightLegPivot;
        private Transform clubArmPivot;
        private Transform freeArmPivot;
        private float animationTime;
        private float attackTimer;
        private Vector3 attackLocalDirection;
        private float moveSpeed = MoveSpeed;
        private MobSpecies species;
        private MobRank rank;

        public MobController Controller { get; private set; }

        public static MobView Create(
            MazeRenderer renderer,
            Vector2Int startPosition,
            MobSpecies species = MobSpecies.Orc,
            MobRank rank = MobRank.Regular)
        {
            var mobObject = new GameObject(BuildObjectName(species, rank));
            var view = mobObject.AddComponent<MobView>();
            view.Initialize(renderer, startPosition, species, rank);
            return view;
        }

        public void SetController(MobController controller)
        {
            Controller = controller;
        }

        public void SetVisible(bool visible)
        {
            foreach (var renderer in GetComponentsInChildren<Renderer>())
            {
                renderer.enabled = visible;
            }

            foreach (var collider in GetComponentsInChildren<Collider>())
            {
                collider.enabled = visible;
            }
        }

        public void MoveTo(Vector2Int gridPosition)
        {
            targetPosition = ToWorldPosition(gridPosition);
        }

        public void SetGridPositionImmediate(Vector2Int gridPosition)
        {
            targetPosition = ToWorldPosition(gridPosition);
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
            FaceGridPosition(targetGridPosition);
            var direction = Vector3.ProjectOnPlane(ToWorldPosition(targetGridPosition) - transform.position, Vector3.up).normalized;
            attackLocalDirection = transform.InverseTransformDirection(direction);
            attackTimer = AttackDuration;
        }

        public void SetDefeated()
        {
            visualRoot.localRotation = Quaternion.Euler(0f, 0f, 72f);
            visualRoot.localPosition = new Vector3(0f, 0.16f, 0f);
        }

        private void Update()
        {
            var offsetToTarget = targetPosition - transform.position;
            var isMoving = offsetToTarget.sqrMagnitude > 0.0025f;

            if (isMoving)
            {
                var direction = Vector3.ProjectOnPlane(offsetToTarget, Vector3.up).normalized;
                if (direction.sqrMagnitude > 0.01f)
                {
                    transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
                }
            }

            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            AnimateMob(isMoving);
        }

        private void Initialize(MazeRenderer renderer, Vector2Int startPosition, MobSpecies species, MobRank rank)
        {
            this.species = species;
            this.rank = rank;
            mazeRenderer = renderer;
            BuildModel();
            transform.localScale = Vector3.one * renderer.ModelUnitSize * BuildScaleMultiplier(species, rank);
            moveSpeed = MoveSpeed * renderer.CellSize * (species == MobSpecies.Rat && rank == MobRank.Regular ? 1.18f : 1f);
            MoveTo(startPosition);
            transform.position = targetPosition;
        }

        private void BuildModel()
        {
            visualRoot = new GameObject($"{species} Visual").transform;
            visualRoot.SetParent(transform, false);

            var isBoss = rank == MobRank.Boss;
            var isMiniBoss = rank == MobRank.MiniBoss;
            if (species == MobSpecies.Rat)
            {
                BuildRatModel();
                SetIdlePose();
                return;
            }

            var isGoblin = species == MobSpecies.Goblin;
            var materialPrefix = BuildRankedMaterialPrefix(species, rank);
            var skinColor = isBoss ? new Color(0.62f, 0.08f, 0.07f) : isMiniBoss ? new Color(0.48f, 0.2f, 0.1f) : isGoblin ? new Color(0.34f, 0.72f, 0.17f) : new Color(0.22f, 0.55f, 0.18f);
            var darkSkinColor = isBoss ? new Color(0.25f, 0.02f, 0.02f) : isMiniBoss ? new Color(0.14f, 0.05f, 0.035f) : isGoblin ? new Color(0.12f, 0.38f, 0.08f) : new Color(0.12f, 0.34f, 0.11f);
            var leatherColor = isBoss ? new Color(0.08f, 0.05f, 0.04f) : isMiniBoss ? new Color(0.12f, 0.06f, 0.035f) : isGoblin ? new Color(0.13f, 0.09f, 0.05f) : new Color(0.24f, 0.13f, 0.05f);
            var clothColor = isBoss ? new Color(0.42f, 0.02f, 0.02f) : isMiniBoss ? new Color(0.38f, 0.04f, 0.02f) : isGoblin ? new Color(0.12f, 0.18f, 0.08f) : new Color(0.45f, 0.18f, 0.06f);
            var boneColor = isBoss ? new Color(1f, 0.78f, 0.48f) : isMiniBoss ? new Color(1f, 0.66f, 0.32f) : isGoblin ? new Color(0.78f, 0.9f, 0.55f) : new Color(0.9f, 0.82f, 0.62f);
            var clubColor = isBoss ? new Color(0.12f, 0.11f, 0.1f) : isMiniBoss ? new Color(0.13f, 0.08f, 0.055f) : isGoblin ? new Color(0.22f, 0.16f, 0.08f) : new Color(0.34f, 0.2f, 0.09f);
            var skin = CreateMaterial($"{materialPrefix} Skin", skinColor);
            var darkSkin = CreateMaterial($"{materialPrefix} Dark Skin", darkSkinColor);
            var leather = CreateMaterial($"{materialPrefix} Leather", leatherColor);
            var cloth = CreateMaterial($"{materialPrefix} Cloth", clothColor);
            var bone = CreateMaterial($"{materialPrefix} Tusks", boneColor);
            var club = CreateMaterial($"{materialPrefix} Club", clubColor);

            var collider = gameObject.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, isGoblin ? 0.52f : 0.62f, 0f);
            collider.size = isGoblin ? new Vector3(0.58f, 1.08f, 0.58f) : new Vector3(0.72f, 1.28f, 0.72f);

            CreatePart("Body", visualRoot, PrimitiveType.Cube, isGoblin ? new Vector3(0f, 0.49f, 0f) : new Vector3(0f, 0.56f, 0f), isGoblin ? new Vector3(0.38f, 0.42f, 0.24f) : new Vector3(0.46f, 0.5f, 0.28f), skin);
            CreatePart("Belt", visualRoot, PrimitiveType.Cube, isGoblin ? new Vector3(0f, 0.3f, 0f) : new Vector3(0f, 0.34f, 0f), isGoblin ? new Vector3(0.44f, 0.07f, 0.28f) : new Vector3(0.52f, 0.08f, 0.32f), leather);
            CreatePart("Head", visualRoot, PrimitiveType.Sphere, isGoblin ? new Vector3(0f, 0.82f, 0.03f) : new Vector3(0f, 0.96f, 0.03f), isGoblin ? new Vector3(0.36f, 0.32f, 0.34f) : new Vector3(0.34f, 0.3f, 0.32f), skin);
            CreatePart("Brow", visualRoot, PrimitiveType.Cube, isGoblin ? new Vector3(0f, 0.88f, 0.19f) : new Vector3(0f, 1.02f, 0.19f), new Vector3(0.34f, 0.07f, 0.05f), darkSkin);
            CreatePart("Left Tusk", visualRoot, PrimitiveType.Cube, isGoblin ? new Vector3(-0.08f, 0.74f, 0.23f) : new Vector3(-0.09f, 0.88f, 0.23f), isGoblin ? new Vector3(0.04f, 0.07f, 0.04f) : new Vector3(0.05f, 0.1f, 0.05f), bone);
            CreatePart("Right Tusk", visualRoot, PrimitiveType.Cube, isGoblin ? new Vector3(0.08f, 0.74f, 0.23f) : new Vector3(0.09f, 0.88f, 0.23f), isGoblin ? new Vector3(0.04f, 0.07f, 0.04f) : new Vector3(0.05f, 0.1f, 0.05f), bone);

            leftLegPivot = CreatePivot("Left Leg Pivot", isGoblin ? new Vector3(-0.12f, 0.31f, 0f) : new Vector3(-0.14f, 0.35f, 0f));
            rightLegPivot = CreatePivot("Right Leg Pivot", isGoblin ? new Vector3(0.12f, 0.31f, 0f) : new Vector3(0.14f, 0.35f, 0f));
            CreatePart("Left Leg", leftLegPivot, PrimitiveType.Cube, isGoblin ? new Vector3(0f, -0.15f, 0f) : new Vector3(0f, -0.19f, 0f), isGoblin ? new Vector3(0.12f, 0.28f, 0.12f) : new Vector3(0.15f, 0.36f, 0.14f), cloth);
            CreatePart("Right Leg", rightLegPivot, PrimitiveType.Cube, isGoblin ? new Vector3(0f, -0.15f, 0f) : new Vector3(0f, -0.19f, 0f), isGoblin ? new Vector3(0.12f, 0.28f, 0.12f) : new Vector3(0.15f, 0.36f, 0.14f), cloth);

            freeArmPivot = CreatePivot("Free Arm Pivot", isGoblin ? new Vector3(-0.28f, 0.66f, 0f) : new Vector3(-0.33f, 0.76f, 0f));
            clubArmPivot = CreatePivot("Club Arm Pivot", isGoblin ? new Vector3(0.28f, 0.68f, 0f) : new Vector3(0.33f, 0.78f, 0f));
            CreatePart("Free Arm", freeArmPivot, PrimitiveType.Cube, isGoblin ? new Vector3(0f, -0.14f, 0.02f) : new Vector3(0f, -0.18f, 0.02f), isGoblin ? new Vector3(0.11f, 0.3f, 0.1f) : new Vector3(0.14f, 0.38f, 0.13f), skin);
            CreatePart("Club Arm", clubArmPivot, PrimitiveType.Cube, isGoblin ? new Vector3(0f, -0.14f, 0.02f) : new Vector3(0f, -0.18f, 0.02f), isGoblin ? new Vector3(0.11f, 0.3f, 0.1f) : new Vector3(0.14f, 0.38f, 0.13f), skin);
            CreatePart("Club", clubArmPivot, PrimitiveType.Cube, isGoblin ? new Vector3(0.07f, 0.06f, 0.12f) : new Vector3(0.09f, 0.1f, 0.13f), isGoblin ? new Vector3(0.08f, 0.42f, 0.08f) : new Vector3(0.12f, 0.62f, 0.12f), club);
            if (isBoss || isMiniBoss)
            {
                var prefix = isBoss ? "Boss" : "MiniBoss";
                var hornY = isGoblin ? 1.02f : 1.18f;
                var plateY = isGoblin ? 0.52f : 0.62f;
                CreatePart($"{prefix} Left Horn", visualRoot, PrimitiveType.Cube, new Vector3(-0.18f, hornY, 0.02f), new Vector3(0.08f, 0.26f, 0.08f), bone);
                CreatePart($"{prefix} Right Horn", visualRoot, PrimitiveType.Cube, new Vector3(0.18f, hornY, 0.02f), new Vector3(0.08f, 0.26f, 0.08f), bone);
                CreatePart($"{prefix} Chest Plate", visualRoot, PrimitiveType.Cube, new Vector3(0f, plateY, 0.16f), new Vector3(0.38f, 0.28f, 0.06f), leather);
            }

            SetIdlePose();
        }

        private void BuildRatModel()
        {
            var isBoss = rank == MobRank.Boss;
            var isMiniBoss = rank == MobRank.MiniBoss;
            var isElite = isBoss || isMiniBoss;
            var materialPrefix = BuildRankedMaterialPrefix(species, rank);
            var fur = CreateMaterial($"{materialPrefix} Fur", isBoss ? new Color(0.55f, 0.04f, 0.035f) : isMiniBoss ? new Color(0.34f, 0.08f, 0.065f) : new Color(0.24f, 0.22f, 0.2f));
            var darkFur = CreateMaterial($"{materialPrefix} Dark Fur", isBoss ? new Color(0.12f, 0.015f, 0.015f) : isMiniBoss ? new Color(0.08f, 0.035f, 0.03f) : new Color(0.11f, 0.1f, 0.095f));
            var ear = CreateMaterial($"{materialPrefix} Ear", isBoss ? new Color(0.8f, 0.1f, 0.06f) : isMiniBoss ? new Color(0.55f, 0.18f, 0.12f) : new Color(0.34f, 0.2f, 0.18f));
            var eye = CreateMaterial($"{materialPrefix} Eye", isBoss ? new Color(1f, 0.78f, 0.18f) : isMiniBoss ? new Color(1f, 0.56f, 0.08f) : new Color(0.95f, 0.08f, 0.06f));
            var tail = CreateMaterial($"{materialPrefix} Tail", isBoss ? new Color(0.64f, 0.08f, 0.06f) : isMiniBoss ? new Color(0.5f, 0.12f, 0.1f) : new Color(0.42f, 0.24f, 0.2f));

            var collider = gameObject.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.22f, 0.03f);
            collider.size = new Vector3(0.72f, 0.42f, 0.82f);

            CreatePart("Rat Body", visualRoot, PrimitiveType.Sphere, new Vector3(0f, 0.24f, 0f), new Vector3(0.58f, 0.34f, 0.7f), fur);
            CreatePart("Rat Back", visualRoot, PrimitiveType.Sphere, new Vector3(0f, 0.28f, -0.16f), new Vector3(0.5f, 0.28f, 0.42f), darkFur);
            CreatePart("Rat Head", visualRoot, PrimitiveType.Sphere, new Vector3(0f, 0.3f, 0.43f), new Vector3(0.36f, 0.28f, 0.34f), fur);
            CreatePart("Rat Snout", visualRoot, PrimitiveType.Sphere, new Vector3(0f, 0.25f, 0.64f), new Vector3(0.24f, 0.16f, 0.24f), darkFur);
            CreatePart("Rat Left Ear", visualRoot, PrimitiveType.Sphere, new Vector3(-0.14f, 0.48f, 0.38f), new Vector3(0.14f, 0.12f, 0.08f), ear);
            CreatePart("Rat Right Ear", visualRoot, PrimitiveType.Sphere, new Vector3(0.14f, 0.48f, 0.38f), new Vector3(0.14f, 0.12f, 0.08f), ear);
            CreatePart("Rat Left Eye", visualRoot, PrimitiveType.Sphere, new Vector3(-0.1f, 0.34f, 0.66f), new Vector3(0.055f, 0.055f, 0.055f), eye);
            CreatePart("Rat Right Eye", visualRoot, PrimitiveType.Sphere, new Vector3(0.1f, 0.34f, 0.66f), new Vector3(0.055f, 0.055f, 0.055f), eye);
            CreatePart("Rat Tail", visualRoot, PrimitiveType.Cube, new Vector3(0f, 0.21f, -0.56f), new Vector3(0.08f, 0.08f, 0.58f), tail);

            freeArmPivot = CreatePivot("Rat Front Left Pivot", new Vector3(-0.2f, 0.18f, 0.28f));
            clubArmPivot = CreatePivot("Rat Front Right Pivot", new Vector3(0.2f, 0.18f, 0.28f));
            leftLegPivot = CreatePivot("Rat Back Left Pivot", new Vector3(-0.22f, 0.18f, -0.22f));
            rightLegPivot = CreatePivot("Rat Back Right Pivot", new Vector3(0.22f, 0.18f, -0.22f));
            CreatePart("Rat Front Left Leg", freeArmPivot, PrimitiveType.Cube, new Vector3(0f, -0.12f, 0f), new Vector3(0.08f, 0.24f, 0.08f), darkFur);
            CreatePart("Rat Front Right Leg", clubArmPivot, PrimitiveType.Cube, new Vector3(0f, -0.12f, 0f), new Vector3(0.08f, 0.24f, 0.08f), darkFur);
            CreatePart("Rat Back Left Leg", leftLegPivot, PrimitiveType.Cube, new Vector3(0f, -0.12f, 0f), new Vector3(0.09f, 0.24f, 0.08f), darkFur);
            CreatePart("Rat Back Right Leg", rightLegPivot, PrimitiveType.Cube, new Vector3(0f, -0.12f, 0f), new Vector3(0.09f, 0.24f, 0.08f), darkFur);
            if (isElite)
            {
                var prefix = isBoss ? "Boss" : "MiniBoss";
                var spikeHeight = isBoss ? 0.36f : 0.26f;
                CreatePart($"{prefix} Rat Shoulder Spikes", visualRoot, PrimitiveType.Cube, new Vector3(0f, 0.48f, 0.1f), new Vector3(0.5f, 0.08f, 0.14f), eye);
                CreatePart($"{prefix} Rat Back Spike", visualRoot, PrimitiveType.Cube, new Vector3(0f, 0.5f, -0.18f), new Vector3(0.12f, spikeHeight, 0.12f), eye);
                CreatePart($"{prefix} Rat Head Crown", visualRoot, PrimitiveType.Cube, new Vector3(0f, 0.58f, 0.44f), new Vector3(0.32f, 0.08f, 0.12f), eye);
            }
        }

        private void AnimateMob(bool isMoving)
        {
            if (visualRoot == null)
            {
                return;
            }

            var lunge = Vector3.zero;
            if (attackTimer > 0f)
            {
                var phase = 1f - attackTimer / AttackDuration;
                lunge = attackLocalDirection * (Mathf.Sin(phase * Mathf.PI) * 0.18f);
                attackTimer = Mathf.Max(0f, attackTimer - Time.deltaTime);
                clubArmPivot.localRotation = Quaternion.Euler(-60f * Mathf.Sin(phase * Mathf.PI), 0f, -24f);
            }

            if (!isMoving)
            {
                visualRoot.localPosition = lunge;
                if (attackTimer <= 0f)
                {
                    SetIdlePose();
                }

                return;
            }

            animationTime += Time.deltaTime * WalkAnimationSpeed;
            var swing = Mathf.Sin(animationTime) * 20f;
            var bob = Mathf.Abs(Mathf.Sin(animationTime)) * 0.035f;

            visualRoot.localPosition = new Vector3(0f, bob, 0f) + lunge;
            leftLegPivot.localRotation = Quaternion.Euler(swing, 0f, 0f);
            rightLegPivot.localRotation = Quaternion.Euler(-swing, 0f, 0f);
            freeArmPivot.localRotation = Quaternion.Euler(-swing * 0.35f, 0f, 12f);
            if (attackTimer <= 0f)
            {
                clubArmPivot.localRotation = Quaternion.Euler(swing * 0.3f, 0f, -18f);
            }
        }

        private void SetIdlePose()
        {
            leftLegPivot.localRotation = Quaternion.identity;
            rightLegPivot.localRotation = Quaternion.identity;
            freeArmPivot.localRotation = Quaternion.Euler(0f, 0f, 12f);
            clubArmPivot.localRotation = Quaternion.Euler(0f, 0f, -18f);
        }

        private Transform CreatePivot(string pivotName, Vector3 localPosition)
        {
            var pivot = new GameObject(pivotName).transform;
            pivot.SetParent(visualRoot, false);
            pivot.localPosition = localPosition;
            return pivot;
        }

        private Transform CreatePart(string partName, Transform parent, PrimitiveType primitiveType, Vector3 localPosition, Vector3 localScale, Material material)
        {
            var part = GameObject.CreatePrimitive(primitiveType);
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

            return part.transform;
        }

        private Vector3 ToWorldPosition(Vector2Int gridPosition)
        {
            return mazeRenderer.GridToWorld(gridPosition);
        }

        private static Material CreateMaterial(string materialName, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
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

            return material;
        }

        private static string BuildSpeciesMaterialPrefix(MobSpecies mobSpecies)
        {
            switch (mobSpecies)
            {
                case MobSpecies.Orc:
                    return "Orc";
                case MobSpecies.Goblin:
                    return "Goblin";
                case MobSpecies.Rat:
                    return "Rat";
                default:
                    return "Mob";
            }
        }

        private static string BuildRankedMaterialPrefix(MobSpecies mobSpecies, MobRank mobRank)
        {
            if (mobRank == MobRank.Boss)
            {
                return $"Boss {BuildSpeciesMaterialPrefix(mobSpecies)}";
            }

            if (mobRank == MobRank.MiniBoss)
            {
                return $"MiniBoss {BuildSpeciesMaterialPrefix(mobSpecies)}";
            }

            return BuildSpeciesMaterialPrefix(mobSpecies);
        }

        private static float BuildScaleMultiplier(MobSpecies mobSpecies, MobRank mobRank)
        {
            if (mobRank == MobRank.Boss)
            {
                return 1.55f;
            }

            if (mobRank == MobRank.MiniBoss)
            {
                switch (mobSpecies)
                {
                    case MobSpecies.Rat:
                        return 0.68f;
                    case MobSpecies.Goblin:
                        return 0.96f;
                    default:
                        return 1.25f;
                }
            }

            switch (mobSpecies)
            {
                case MobSpecies.Rat:
                    return 0.42f;
                case MobSpecies.Goblin:
                    return 0.72f;
                default:
                    return 1f;
            }
        }

        private static string BuildObjectName(MobSpecies mobSpecies, MobRank mobRank)
        {
            switch (mobRank)
            {
                case MobRank.Boss:
                    return "Maze Boss";
                case MobRank.MiniBoss:
                    return $"MiniBoss {mobSpecies}";
                default:
                    return $"{mobSpecies} Mob";
            }
        }
    }
}
