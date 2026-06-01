using System.Collections.Generic;
using Labyrinth.Core;
using Labyrinth.Maze;
using UnityEngine;
using UnityEngine.Rendering;

namespace Labyrinth.Mobs
{
    public sealed class MobView : MonoBehaviour
    {
        private const float MoveSpeed = 3.4f;
        private const float WalkAnimationSpeed = 7.5f;
        private const float AttackDuration = 0.3f;
        private const float DefeatAnimationDuration = 0.82f;
        private const float DefeatImpactDuration = 0.18f;
        private const float LabelDepthOffset = -0.035f;
        private static readonly Vector3 HumanoidVisualFootprintScale = new Vector3(0.82f, 0.94f, 0.82f);
        private static readonly Vector3 RatVisualFootprintScale = new Vector3(0.76f, 0.92f, 0.76f);

        private static int nextLaneSerial;
        private static Material levelLabelBackgroundMaterial;

        private readonly List<Vector3> movePath = new List<Vector3>();
        private MazeRenderer mazeRenderer;
        private Vector3 targetPosition;
        private Transform visualRoot;
        private Transform levelLabelRoot;
        private Transform leftLegPivot;
        private Transform rightLegPivot;
        private Transform clubArmPivot;
        private Transform freeArmPivot;
        private Transform bodyPart;
        private Transform beltPart;
        private Transform headPart;
        private Transform secondaryBodyPart;
        private Transform tailPart;
        private Vector3 bodyBaseScale;
        private Vector3 headBasePosition;
        private Vector3 secondaryBodyBasePosition;
        private float animationTime;
        private float attackTimer;
        private Vector3 attackLocalDirection;
        private float moveSpeed = MoveSpeed;
        private int moveWaypointIndex;
        private int laneSeed;
        private Vector2Int visualGridPosition;
        private MobSpecies species;
        private MobRank rank;
        private int level;
        private bool defeated;
        private float defeatTimer;
        private Vector3 defeatStartPosition;
        private Vector3 defeatTargetPosition;
        private Vector3 defeatStartScale;
        private Vector3 defeatTargetScale;
        private Quaternion defeatStartRotation;
        private Quaternion defeatTargetRotation;

        public MobController Controller { get; private set; }

        public static MobView Create(
            MazeRenderer renderer,
            Vector2Int startPosition,
            MobSpecies species = MobSpecies.Orc,
            MobRank rank = MobRank.Regular,
            int level = 1)
        {
            var mobObject = new GameObject(BuildObjectName(species, rank));
            var view = mobObject.AddComponent<MobView>();
            view.Initialize(renderer, startPosition, species, rank, level);
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
                collider.enabled = visible && !defeated;
            }

            SetLevelLabelVisible(visible && !defeated);
        }

        public void MoveTo(Vector2Int gridPosition)
        {
            var path = SubCellPathBuilder.BuildStep(
                mazeRenderer,
                visualGridPosition,
                gridPosition,
                0f,
                laneSeed,
                SubCellPathProfile.Mob,
                transform.position);
            if (path.Count == 0)
            {
                return;
            }

            movePath.Clear();
            movePath.AddRange(path);
            moveWaypointIndex = Mathf.Min(1, movePath.Count);
            targetPosition = movePath[movePath.Count - 1];
            visualGridPosition = gridPosition;
        }

        public void SetGridPositionImmediate(Vector2Int gridPosition)
        {
            targetPosition = ToWorldPosition(gridPosition);
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
            FaceGridPosition(targetGridPosition);
            var direction = Vector3.ProjectOnPlane(ToWorldPosition(targetGridPosition) - transform.position, Vector3.up).normalized;
            attackLocalDirection = transform.InverseTransformDirection(direction);
            attackTimer = AttackDuration;
        }

        public void SetDefeated()
        {
            if (defeated || visualRoot == null)
            {
                return;
            }

            defeated = true;
            defeatTimer = 0f;
            attackTimer = 0f;
            movePath.Clear();
            moveWaypointIndex = 0;
            targetPosition = transform.position;
            defeatStartPosition = visualRoot.localPosition;
            defeatStartRotation = visualRoot.localRotation;
            defeatStartScale = visualRoot.localScale;
            defeatTargetPosition = species == MobSpecies.Rat
                ? new Vector3(0f, 0.07f, 0.02f)
                : new Vector3(0f, 0.12f, 0f);
            defeatTargetRotation = Quaternion.Euler(
                species == MobSpecies.Rat ? 0f : -4f,
                0f,
                species == MobSpecies.Rat ? -86f : 82f);
            defeatTargetScale = Vector3.Scale(
                GetVisualFootprintScale(),
                species == MobSpecies.Rat ? new Vector3(1.12f, 0.66f, 1.08f) : new Vector3(1.08f, 0.72f, 1.06f));

            foreach (var collider in GetComponentsInChildren<Collider>())
            {
                collider.enabled = false;
            }

            SetLevelLabelVisible(false);
        }

        private void Update()
        {
            if (defeated)
            {
                AnimateDefeat();
                return;
            }

            var isMoving = movePath.Count > 0 && moveWaypointIndex < movePath.Count;

            if (isMoving)
            {
                var direction = Vector3.ProjectOnPlane(movePath[moveWaypointIndex] - transform.position, Vector3.up).normalized;
                if (direction.sqrMagnitude > 0.01f)
                {
                    transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
                }

                MoveAlongPath(moveSpeed * Time.deltaTime);
                if (moveWaypointIndex >= movePath.Count)
                {
                    transform.position = targetPosition;
                    movePath.Clear();
                }
            }

            AnimateMob(isMoving);
            RefreshLevelLabelBillboard();
        }

        private void AnimateDefeat()
        {
            if (visualRoot == null)
            {
                return;
            }

            defeatTimer += Time.deltaTime;
            var t = Mathf.Clamp01(defeatTimer / DefeatAnimationDuration);
            var eased = t * t * (3f - 2f * t);
            var impact = Mathf.Sin(Mathf.Clamp01(defeatTimer / DefeatImpactDuration) * Mathf.PI) * (1f - t);
            visualRoot.localPosition = Vector3.Lerp(defeatStartPosition, defeatTargetPosition, eased)
                + new Vector3(0f, impact * 0.045f, 0f);
            visualRoot.localRotation = Quaternion.Slerp(defeatStartRotation, defeatTargetRotation, eased);
            visualRoot.localScale = Vector3.Lerp(defeatStartScale, defeatTargetScale, eased);

            if (species == MobSpecies.Rat)
            {
                AnimateRatDefeat(eased);
                return;
            }

            AnimateHumanoidDefeat(eased);
        }

        private void Initialize(MazeRenderer renderer, Vector2Int startPosition, MobSpecies species, MobRank rank, int level)
        {
            this.species = species;
            this.rank = rank;
            this.level = Mathf.Max(1, level);
            mazeRenderer = renderer;
            laneSeed = BuildLaneSeed(startPosition, species, rank, ++nextLaneSerial);
            visualGridPosition = startPosition;
            BuildModel();
            transform.localScale = Vector3.one * renderer.ModelUnitSize * BuildScaleMultiplier(species, rank);
            BuildLevelLabel();
            moveSpeed = MoveSpeed * renderer.CellSize * (species == MobSpecies.Rat && rank == MobRank.Regular ? 1.18f : 1f);
            MoveTo(startPosition);
            transform.position = targetPosition;
            RefreshLevelLabelBillboard();
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

        private static int BuildLaneSeed(Vector2Int startPosition, MobSpecies mobSpecies, MobRank mobRank, int serial)
        {
            return serial * 265443576
                ^ startPosition.x * 73856093
                ^ startPosition.y * 19349663
                ^ (int)mobSpecies * 83492791
                ^ (int)mobRank * 265443576
                ^ 0x41d3;
        }

        private void BuildModel()
        {
            visualRoot = new GameObject($"{species} Visual").transform;
            visualRoot.SetParent(transform, false);
            visualRoot.localScale = GetVisualFootprintScale();
            VoxelVisuals.CreateContactShadow(
                "Mob Contact Shadow",
                transform,
                new Vector3(0f, 0.006f, 0f),
                species == MobSpecies.Rat ? new Vector3(0.72f, 0.004f, 0.86f) : new Vector3(0.78f, 0.004f, 0.64f),
                rank == MobRank.Regular ? 0.36f : 0.48f);

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

            bodyPart = CreatePart("Body", visualRoot, PrimitiveType.Cube, isGoblin ? new Vector3(0f, 0.49f, 0f) : new Vector3(0f, 0.56f, 0f), isGoblin ? new Vector3(0.38f, 0.42f, 0.24f) : new Vector3(0.46f, 0.5f, 0.28f), skin);
            beltPart = CreatePart("Belt", visualRoot, PrimitiveType.Cube, isGoblin ? new Vector3(0f, 0.3f, 0f) : new Vector3(0f, 0.34f, 0f), isGoblin ? new Vector3(0.44f, 0.07f, 0.28f) : new Vector3(0.52f, 0.08f, 0.32f), leather);
            headPart = CreatePart("Head", visualRoot, PrimitiveType.Sphere, isGoblin ? new Vector3(0f, 0.82f, 0.03f) : new Vector3(0f, 0.96f, 0.03f), isGoblin ? new Vector3(0.36f, 0.32f, 0.34f) : new Vector3(0.34f, 0.3f, 0.32f), skin);
            bodyBaseScale = bodyPart.localScale;
            headBasePosition = headPart.localPosition;
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

            bodyPart = CreatePart("Rat Body", visualRoot, PrimitiveType.Sphere, new Vector3(0f, 0.24f, 0f), new Vector3(0.58f, 0.34f, 0.7f), fur);
            secondaryBodyPart = CreatePart("Rat Back", visualRoot, PrimitiveType.Sphere, new Vector3(0f, 0.28f, -0.16f), new Vector3(0.5f, 0.28f, 0.42f), darkFur);
            headPart = CreatePart("Rat Head", visualRoot, PrimitiveType.Sphere, new Vector3(0f, 0.3f, 0.43f), new Vector3(0.36f, 0.28f, 0.34f), fur);
            bodyBaseScale = bodyPart.localScale;
            headBasePosition = headPart.localPosition;
            secondaryBodyBasePosition = secondaryBodyPart.localPosition;
            CreatePart("Rat Snout", visualRoot, PrimitiveType.Sphere, new Vector3(0f, 0.25f, 0.64f), new Vector3(0.24f, 0.16f, 0.24f), darkFur);
            CreatePart("Rat Left Ear", visualRoot, PrimitiveType.Sphere, new Vector3(-0.14f, 0.48f, 0.38f), new Vector3(0.14f, 0.12f, 0.08f), ear);
            CreatePart("Rat Right Ear", visualRoot, PrimitiveType.Sphere, new Vector3(0.14f, 0.48f, 0.38f), new Vector3(0.14f, 0.12f, 0.08f), ear);
            CreatePart("Rat Left Eye", visualRoot, PrimitiveType.Sphere, new Vector3(-0.1f, 0.34f, 0.66f), new Vector3(0.055f, 0.055f, 0.055f), eye);
            CreatePart("Rat Right Eye", visualRoot, PrimitiveType.Sphere, new Vector3(0.1f, 0.34f, 0.66f), new Vector3(0.055f, 0.055f, 0.055f), eye);
            tailPart = CreatePart("Rat Tail", visualRoot, PrimitiveType.Cube, new Vector3(0f, 0.21f, -0.56f), new Vector3(0.08f, 0.08f, 0.58f), tail);

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

        private void BuildLevelLabel()
        {
            if (rank != MobRank.MiniBoss && rank != MobRank.Boss)
            {
                return;
            }

            levelLabelRoot = new GameObject("Mob Level Label").transform;
            levelLabelRoot.SetParent(transform, false);

            var background = GameObject.CreatePrimitive(PrimitiveType.Quad);
            background.name = "Mob Level Label Background";
            background.transform.SetParent(levelLabelRoot, false);
            background.transform.localPosition = Vector3.zero;
            background.GetComponent<Renderer>().sharedMaterial = GetLevelLabelBackgroundMaterial();
            RemoveCollider(background);

            var text = $"Lvl {level}";
            CreateLevelLabelText("Mob Level Label Shadow", text, new Vector3(0.045f, -0.045f, LabelDepthOffset), new Color(0f, 0f, 0f, 0.92f));
            CreateLevelLabelText("Mob Level Label Text", text, new Vector3(0f, 0f, LabelDepthOffset - 0.01f), new Color(1f, 0.93f, 0.68f, 1f));

            background.transform.localScale = new Vector3(Mathf.Clamp(1.55f + text.Length * 0.18f, 2.25f, 5.2f), 0.56f, 1f);
        }

        private void CreateLevelLabelText(string objectName, string text, Vector3 localPosition, Color color)
        {
            var textObject = new GameObject(objectName);
            textObject.transform.SetParent(levelLabelRoot, false);
            textObject.transform.localPosition = localPosition;
            var textMesh = textObject.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.fontSize = 72;
            textMesh.characterSize = 0.085f;
            textMesh.fontStyle = FontStyle.Bold;
            textMesh.color = color;

            var meshRenderer = textObject.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.sortingOrder = 40;
            }
        }

        private void RefreshLevelLabelBillboard()
        {
            if (levelLabelRoot == null)
            {
                return;
            }

            levelLabelRoot.position = transform.position + Vector3.up * GetLevelLabelHeight();
            levelLabelRoot.localScale = BuildInverseParentScale();

            var camera = Camera.main;
            if (camera != null)
            {
                levelLabelRoot.rotation = camera.transform.rotation;
            }
        }

        private Vector3 BuildInverseParentScale()
        {
            var scale = transform.lossyScale;
            return new Vector3(SafeInverse(scale.x), SafeInverse(scale.y), SafeInverse(scale.z));
        }

        private float GetLevelLabelHeight()
        {
            var unit = mazeRenderer != null ? mazeRenderer.ModelUnitSize : 1f;
            if (rank == MobRank.Boss)
            {
                return unit * (species == MobSpecies.Rat ? 1.35f : 2.55f);
            }

            switch (species)
            {
                case MobSpecies.Rat:
                    return unit * 0.95f;
                case MobSpecies.Goblin:
                    return unit * 1.55f;
                default:
                    return unit * 2.05f;
            }
        }

        private void SetLevelLabelVisible(bool visible)
        {
            if (levelLabelRoot == null)
            {
                return;
            }

            levelLabelRoot.gameObject.SetActive(visible);
            foreach (var renderer in levelLabelRoot.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = visible;
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

            animationTime += Time.deltaTime * WalkAnimationSpeed * (species == MobSpecies.Rat ? 1.45f : 1f);
            if (species == MobSpecies.Rat)
            {
                AnimateRatMove(lunge);
                return;
            }

            AnimateHumanoidMove(lunge, attackTimer > 0f);
        }

        private void AnimateHumanoidMove(Vector3 lunge, bool attacking)
        {
            var isGoblin = species == MobSpecies.Goblin;
            var wave = Mathf.Sin(animationTime);
            var counter = Mathf.Cos(animationTime);
            var impact = Mathf.Abs(counter);
            var swing = wave * (isGoblin ? 27f : 22f);
            var stomp = impact * (isGoblin ? 0.038f : 0.052f);
            var sway = wave * (isGoblin ? 0.025f : 0.035f);

            visualRoot.localPosition = new Vector3(sway * 0.22f, stomp, counter * 0.008f) + lunge;
            visualRoot.localRotation = Quaternion.Euler(counter * 2f, 0f, -wave * (isGoblin ? 4f : 6f));
            bodyPart.localRotation = Quaternion.Euler(counter * 1.4f, wave * 1.2f, -wave * 3f);
            bodyPart.localScale = Vector3.Scale(
                bodyBaseScale,
                new Vector3(1f + impact * 0.012f, 1f - impact * 0.02f, 1f + impact * 0.01f));
            beltPart.localRotation = bodyPart.localRotation;
            headPart.localPosition = headBasePosition + new Vector3(-sway * 0.3f, impact * 0.014f, 0f);
            headPart.localRotation = Quaternion.Euler(-counter * 1.8f, wave * 1.5f, wave * 3f);

            leftLegPivot.localRotation = Quaternion.Euler(swing, 0f, -Mathf.Max(0f, wave) * 5f);
            rightLegPivot.localRotation = Quaternion.Euler(-swing, 0f, Mathf.Max(0f, -wave) * 5f);
            freeArmPivot.localRotation = Quaternion.Euler(-swing * 0.45f, 0f, 12f + counter * 4f);
            if (!attacking)
            {
                clubArmPivot.localRotation = Quaternion.Euler(swing * 0.38f - impact * 4f, 0f, -18f - counter * 4f);
            }
        }

        private void AnimateRatMove(Vector3 lunge)
        {
            var wave = Mathf.Sin(animationTime);
            var fast = Mathf.Sin(animationTime * 2f);
            var impact = Mathf.Abs(fast);
            visualRoot.localPosition = new Vector3(wave * 0.018f, impact * 0.028f, fast * 0.012f) + lunge;
            visualRoot.localRotation = Quaternion.Euler(fast * 2.2f, wave * 4.5f, -wave * 3.5f);
            bodyPart.localScale = Vector3.Scale(
                bodyBaseScale,
                new Vector3(1f + impact * 0.035f, 1f - impact * 0.025f, 1f + Mathf.Max(0f, fast) * 0.035f));
            secondaryBodyPart.localPosition = secondaryBodyBasePosition
                + new Vector3(0f, impact * 0.012f, -Mathf.Max(0f, fast) * 0.025f);
            headPart.localPosition = headBasePosition + new Vector3(0f, impact * 0.018f, Mathf.Max(0f, fast) * 0.03f);
            headPart.localRotation = Quaternion.Euler(-fast * 4f, wave * 5f, 0f);
            tailPart.localRotation = Quaternion.Euler(wave * 8f, 0f, -fast * 12f);
            freeArmPivot.localRotation = Quaternion.Euler(fast * 28f, 0f, 0f);
            clubArmPivot.localRotation = Quaternion.Euler(-fast * 28f, 0f, 0f);
            leftLegPivot.localRotation = Quaternion.Euler(-fast * 24f, 0f, 0f);
            rightLegPivot.localRotation = Quaternion.Euler(fast * 24f, 0f, 0f);
        }

        private void AnimateHumanoidDefeat(float t)
        {
            if (bodyPart != null)
            {
                bodyPart.localRotation = Quaternion.Slerp(bodyPart.localRotation, Quaternion.Euler(-8f, 0f, -10f), t);
                bodyPart.localScale = Vector3.Lerp(bodyPart.localScale, Vector3.Scale(bodyBaseScale, new Vector3(1.04f, 0.82f, 1.08f)), t);
            }

            if (beltPart != null)
            {
                beltPart.localRotation = bodyPart != null ? bodyPart.localRotation : Quaternion.identity;
            }

            if (headPart != null)
            {
                headPart.localPosition = Vector3.Lerp(headPart.localPosition, headBasePosition + new Vector3(0.03f, -0.12f, 0.02f), t);
                headPart.localRotation = Quaternion.Slerp(headPart.localRotation, Quaternion.Euler(18f, -10f, 16f), t);
            }

            leftLegPivot.localRotation = Quaternion.Slerp(leftLegPivot.localRotation, Quaternion.Euler(16f, 0f, 22f), t);
            rightLegPivot.localRotation = Quaternion.Slerp(rightLegPivot.localRotation, Quaternion.Euler(-14f, 0f, -24f), t);
            freeArmPivot.localRotation = Quaternion.Slerp(freeArmPivot.localRotation, Quaternion.Euler(36f, 0f, 42f), t);
            clubArmPivot.localRotation = Quaternion.Slerp(clubArmPivot.localRotation, Quaternion.Euler(-24f, 0f, -58f), t);
        }

        private void AnimateRatDefeat(float t)
        {
            if (bodyPart != null)
            {
                bodyPart.localScale = Vector3.Lerp(bodyPart.localScale, Vector3.Scale(bodyBaseScale, new Vector3(1.12f, 0.74f, 1.02f)), t);
                bodyPart.localRotation = Quaternion.Slerp(bodyPart.localRotation, Quaternion.Euler(0f, -8f, 0f), t);
            }

            if (secondaryBodyPart != null)
            {
                secondaryBodyPart.localPosition = Vector3.Lerp(secondaryBodyPart.localPosition, secondaryBodyBasePosition + new Vector3(0f, -0.07f, 0.02f), t);
                secondaryBodyPart.localRotation = Quaternion.Slerp(secondaryBodyPart.localRotation, Quaternion.Euler(0f, 12f, -8f), t);
            }

            if (headPart != null)
            {
                headPart.localPosition = Vector3.Lerp(headPart.localPosition, headBasePosition + new Vector3(0.04f, -0.08f, -0.03f), t);
                headPart.localRotation = Quaternion.Slerp(headPart.localRotation, Quaternion.Euler(12f, -18f, 8f), t);
            }

            if (tailPart != null)
            {
                tailPart.localRotation = Quaternion.Slerp(tailPart.localRotation, Quaternion.Euler(0f, 0f, -34f), t);
            }

            freeArmPivot.localRotation = Quaternion.Slerp(freeArmPivot.localRotation, Quaternion.Euler(24f, 0f, 16f), t);
            clubArmPivot.localRotation = Quaternion.Slerp(clubArmPivot.localRotation, Quaternion.Euler(-18f, 0f, -18f), t);
            leftLegPivot.localRotation = Quaternion.Slerp(leftLegPivot.localRotation, Quaternion.Euler(-16f, 0f, 24f), t);
            rightLegPivot.localRotation = Quaternion.Slerp(rightLegPivot.localRotation, Quaternion.Euler(16f, 0f, -24f), t);
        }

        private void SetIdlePose()
        {
            visualRoot.localRotation = Quaternion.identity;
            visualRoot.localScale = GetVisualFootprintScale();
            if (bodyPart != null)
            {
                bodyPart.localRotation = Quaternion.identity;
                bodyPart.localScale = bodyBaseScale;
            }

            if (beltPart != null)
            {
                beltPart.localRotation = Quaternion.identity;
            }

            if (headPart != null)
            {
                headPart.localRotation = Quaternion.identity;
                headPart.localPosition = headBasePosition;
            }

            if (secondaryBodyPart != null)
            {
                secondaryBodyPart.localPosition = secondaryBodyBasePosition;
                secondaryBodyPart.localRotation = Quaternion.identity;
            }

            if (tailPart != null)
            {
                tailPart.localRotation = Quaternion.identity;
            }

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

        private Vector3 GetVisualFootprintScale()
        {
            return species == MobSpecies.Rat ? RatVisualFootprintScale : HumanoidVisualFootprintScale;
        }

        private Transform CreatePart(string partName, Transform parent, PrimitiveType primitiveType, Vector3 localPosition, Vector3 localScale, Material material)
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

        private Vector3 ToWorldPosition(Vector2Int gridPosition)
        {
            return mazeRenderer.GridToWorld(gridPosition);
        }

        private static Material CreateMaterial(string materialName, Color color)
        {
            return VoxelVisuals.CreateLitMaterial(materialName, color);
        }

        private static Material GetLevelLabelBackgroundMaterial()
        {
            if (levelLabelBackgroundMaterial == null)
            {
                levelLabelBackgroundMaterial = CreateTransparentMaterial(
                    "Mob Level Label Background",
                    new Color(0.055f, 0.047f, 0.035f, 0.86f));
            }

            return levelLabelBackgroundMaterial;
        }

        private static Material CreateTransparentMaterial(string materialName, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            var material = new Material(shader) { name = materialName, color = color };
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.renderQueue = (int)RenderQueue.Transparent;
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            return material;
        }

        private static float SafeInverse(float value)
        {
            return Mathf.Abs(value) > 0.0001f ? 1f / value : 1f;
        }

        private static void RemoveCollider(GameObject target)
        {
            var collider = target.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }
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
