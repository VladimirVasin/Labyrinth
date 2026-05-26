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

        private MazeRenderer mazeRenderer;
        private Vector3 moveStartPosition;
        private Vector3 targetPosition;
        private Transform visualRoot;
        private Transform leftLegPivot;
        private Transform rightLegPivot;
        private Transform swordArmPivot;
        private Transform shieldArmPivot;
        private Transform cape;
        private GameObject selectionMarker;
        private float animationTime;
        private float attackTimer;
        private Vector3 attackLocalDirection;
        private bool defeated;
        private float moveTimer;

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
            var nextTarget = ToWorldPosition(gridPosition);
            if ((nextTarget - targetPosition).sqrMagnitude <= 0.0001f)
            {
                return;
            }

            moveStartPosition = transform.position;
            targetPosition = nextTarget;
            moveTimer = 0f;
            GameAudioController.Play(GameSfx.Footstep, nextTarget);
        }

        public void SetGridPositionImmediate(Vector2Int gridPosition)
        {
            targetPosition = ToWorldPosition(gridPosition);
            moveStartPosition = targetPosition;
            moveTimer = MoveDuration;
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
            }
        }

        private void Update()
        {
            if (defeated)
            {
                return;
            }

            var progress = Mathf.Clamp01(moveTimer / MoveDuration);
            var isMoving = progress < 1f || (targetPosition - transform.position).sqrMagnitude > 0.0004f;

            if (isMoving)
            {
                moveTimer += Time.deltaTime;
                progress = Mathf.Clamp01(moveTimer / MoveDuration);
                var eased = progress * progress * (3f - 2f * progress);
                var nextPosition = Vector3.Lerp(moveStartPosition, targetPosition, eased);
                var direction = Vector3.ProjectOnPlane(targetPosition - transform.position, Vector3.up).normalized;
                if (direction.sqrMagnitude > 0.01f)
                {
                    var targetRotation = Quaternion.LookRotation(direction, Vector3.up);
                    transform.rotation = Quaternion.RotateTowards(
                        transform.rotation,
                        targetRotation,
                        RotationSpeed * Time.deltaTime);
                }

                transform.position = nextPosition;
                if (progress >= 1f)
                {
                    moveStartPosition = targetPosition;
                    transform.position = targetPosition;
                }
            }

            AnimateKnight(isMoving);
        }

        private void Initialize(MazeRenderer renderer, Vector2Int startPosition)
        {
            mazeRenderer = renderer;
            BuildKnightModel();
            AddMapClickCollider();
            transform.localScale = Vector3.one * renderer.ModelUnitSize;
            SetGridPositionImmediate(startPosition);
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

            var armor = CreateMaterial("Knight Armor", new Color(0.72f, 0.74f, 0.78f));
            var darkArmor = CreateMaterial("Knight Dark Armor", new Color(0.32f, 0.34f, 0.38f));
            var cloth = CreateMaterial("Knight Tabard", new Color(0.12f, 0.22f, 0.68f));
            var capeMaterial = CreateMaterial("Knight Cape", new Color(0.55f, 0.05f, 0.08f));
            var skin = CreateMaterial("Knight Face", new Color(0.86f, 0.62f, 0.42f));
            var leather = CreateMaterial("Knight Leather", new Color(0.22f, 0.12f, 0.05f));
            var blade = CreateMaterial("Knight Sword", new Color(0.88f, 0.9f, 0.95f));
            var shield = CreateMaterial("Knight Shield", new Color(0.7f, 0.04f, 0.05f));
            var selection = CreateMaterial("Hero Selection", new Color(1f, 0.86f, 0.24f, 0.75f));

            CreatePart("Body Armor", visualRoot, PrimitiveType.Cube, new Vector3(0f, 0.62f, 0f), new Vector3(0.42f, 0.54f, 0.26f), armor);
            CreatePart("Chest Plate", visualRoot, PrimitiveType.Cube, new Vector3(0f, 0.68f, 0.15f), new Vector3(0.34f, 0.38f, 0.05f), cloth);
            CreatePart("Belt", visualRoot, PrimitiveType.Cube, new Vector3(0f, 0.42f, -0.01f), new Vector3(0.48f, 0.08f, 0.3f), leather);
            cape = CreatePart("Cape", visualRoot, PrimitiveType.Cube, new Vector3(0f, 0.61f, -0.19f), new Vector3(0.46f, 0.62f, 0.06f), capeMaterial);

            CreatePart("Head", visualRoot, PrimitiveType.Sphere, new Vector3(0f, 1.02f, 0.01f), new Vector3(0.3f, 0.28f, 0.3f), skin);
            CreatePart("Helmet Dome", visualRoot, PrimitiveType.Sphere, new Vector3(0f, 1.08f, 0.01f), new Vector3(0.34f, 0.24f, 0.34f), armor);
            CreatePart("Helmet Visor", visualRoot, PrimitiveType.Cube, new Vector3(0f, 1.03f, 0.18f), new Vector3(0.32f, 0.08f, 0.06f), darkArmor);
            CreatePart("Helmet Crest", visualRoot, PrimitiveType.Cube, new Vector3(0f, 1.27f, 0f), new Vector3(0.08f, 0.2f, 0.38f), capeMaterial);

            leftLegPivot = CreatePivot("Left Leg Pivot", new Vector3(-0.13f, 0.42f, 0f));
            rightLegPivot = CreatePivot("Right Leg Pivot", new Vector3(0.13f, 0.42f, 0f));
            CreatePart("Left Greave", leftLegPivot, PrimitiveType.Cube, new Vector3(0f, -0.22f, -0.01f), new Vector3(0.14f, 0.44f, 0.14f), darkArmor);
            CreatePart("Right Greave", rightLegPivot, PrimitiveType.Cube, new Vector3(0f, -0.22f, -0.01f), new Vector3(0.14f, 0.44f, 0.14f), darkArmor);
            CreatePart("Left Boot", leftLegPivot, PrimitiveType.Cube, new Vector3(0f, -0.46f, 0.06f), new Vector3(0.18f, 0.08f, 0.24f), leather);
            CreatePart("Right Boot", rightLegPivot, PrimitiveType.Cube, new Vector3(0f, -0.46f, 0.06f), new Vector3(0.18f, 0.08f, 0.24f), leather);

            shieldArmPivot = CreatePivot("Shield Arm Pivot", new Vector3(-0.3f, 0.82f, 0f));
            swordArmPivot = CreatePivot("Sword Arm Pivot", new Vector3(0.3f, 0.82f, 0f));
            CreatePart("Shield Arm", shieldArmPivot, PrimitiveType.Cube, new Vector3(-0.02f, -0.2f, 0f), new Vector3(0.12f, 0.4f, 0.12f), armor);
            CreatePart("Sword Arm", swordArmPivot, PrimitiveType.Cube, new Vector3(0.02f, -0.2f, 0f), new Vector3(0.12f, 0.4f, 0.12f), armor);
            CreatePart("Shield", shieldArmPivot, PrimitiveType.Cube, new Vector3(-0.11f, -0.18f, 0.17f), new Vector3(0.08f, 0.44f, 0.34f), shield);
            CreatePart("Shield Boss", shieldArmPivot, PrimitiveType.Sphere, new Vector3(-0.16f, -0.18f, 0.34f), new Vector3(0.12f, 0.12f, 0.06f), armor);
            CreatePart("Sword Blade", swordArmPivot, PrimitiveType.Cube, new Vector3(0.12f, 0.08f, 0.08f), new Vector3(0.05f, 0.72f, 0.05f), blade);
            CreatePart("Sword Guard", swordArmPivot, PrimitiveType.Cube, new Vector3(0.12f, -0.25f, 0.08f), new Vector3(0.25f, 0.05f, 0.05f), leather);

            selectionMarker = CreatePart("Selection Marker", transform, PrimitiveType.Cylinder, new Vector3(0f, 0.02f, 0f), new Vector3(0.72f, 0.02f, 0.72f), selection).gameObject;
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

                return;
            }

            animationTime += Time.deltaTime * WalkAnimationSpeed;
            var swing = Mathf.Sin(animationTime) * 22f;
            var bob = Mathf.Abs(Mathf.Sin(animationTime)) * 0.045f;

            visualRoot.localPosition = new Vector3(0f, bob, 0f) + GetAttackOffset();
            leftLegPivot.localRotation = Quaternion.Euler(swing, 0f, 0f);
            rightLegPivot.localRotation = Quaternion.Euler(-swing, 0f, 0f);
            swordArmPivot.localRotation = Quaternion.Euler(-swing * 0.45f, 0f, -14f);
            shieldArmPivot.localRotation = Quaternion.Euler(swing * 0.35f, 0f, 14f);
            cape.localRotation = Quaternion.Euler(Mathf.Sin(animationTime) * 3f, 0f, 0f);
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
            leftLegPivot.localRotation = Quaternion.identity;
            rightLegPivot.localRotation = Quaternion.identity;
            swordArmPivot.localRotation = Quaternion.Euler(0f, 0f, -14f);
            shieldArmPivot.localRotation = Quaternion.Euler(0f, 0f, 14f);
            cape.localRotation = Quaternion.identity;
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
    }
}
