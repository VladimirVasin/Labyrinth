using UnityEngine;
using UnityEngine.Rendering;

namespace Labyrinth.Base
{
    public enum BuildingType
    {
        Castle,
        Farm,
        LumberjackCamp,
        HeroHouse,
        PeasantHut,
        AlchemistShop,
        Tavern,
        Forge,
        Infirmary,
        CartographerHouse,
        Chapel,
        MinersGuild,
        Market
    }

    public sealed class BuildingView : MonoBehaviour
    {
        private const float DefaultCellSize = 1.65f;
        private const float LabelDepthOffset = -0.035f;
        private static Material labelBackgroundMaterial;
        private static Material selectionMaterial;
        private GameObject labelRoot;
        private TextMesh labelText;
        private TextMesh labelShadow;
        private GameObject selectionRoot;

        public BuildingType Type { get; private set; }

        public string DisplayName { get; private set; }

        public string Subtitle { get; private set; }

        public string EffectText { get; private set; }

        public Vector2Int GridPosition { get; private set; }

        public int FootprintRadius { get; private set; }

        public void Configure(
            BuildingType type,
            string displayName,
            string subtitle,
            string effectText,
            Vector2Int gridPosition,
            int footprintRadius)
        {
            Type = type;
            DisplayName = displayName;
            Subtitle = subtitle;
            EffectText = effectText;
            GridPosition = gridPosition;
            FootprintRadius = footprintRadius;
            RefreshWorldLabel();
            RefreshSelectionOutline();
        }

        public void SetEffectText(string effectText)
        {
            EffectText = effectText;
        }

        public void SetSelected(bool selected)
        {
            if (selectionRoot == null)
            {
                RefreshSelectionOutline();
            }

            if (selectionRoot != null)
            {
                selectionRoot.SetActive(selected);
            }
        }

        private void LateUpdate()
        {
            RefreshLabelBillboard();
        }

        private void RefreshWorldLabel()
        {
            if (labelRoot == null)
            {
                labelRoot = new GameObject("Building Label");
                labelRoot.transform.SetParent(transform, false);

                var background = GameObject.CreatePrimitive(PrimitiveType.Quad);
                background.name = "Building Label Background";
                background.transform.SetParent(labelRoot.transform, false);
                background.transform.localPosition = Vector3.zero;
                background.GetComponent<Renderer>().sharedMaterial = GetLabelBackgroundMaterial();
                RemoveCollider(background);

                labelShadow = CreateLabelText("Building Label Shadow", new Vector3(0.045f, -0.045f, LabelDepthOffset), new Color(0f, 0f, 0f, 0.92f));
                labelText = CreateLabelText("Building Label Text", new Vector3(0f, 0f, LabelDepthOffset - 0.01f), new Color(1f, 0.93f, 0.68f, 1f));
            }

            var text = BuildLabelText();
            labelText.text = text;
            labelShadow.text = text;

            var width = Mathf.Clamp(1.55f + text.Length * 0.18f, 2.25f, 5.2f);
            var backgroundTransform = labelRoot.transform.Find("Building Label Background");
            if (backgroundTransform != null)
            {
                backgroundTransform.localScale = new Vector3(width, 0.56f, 1f);
            }

            RefreshLabelBillboard();
        }

        private TextMesh CreateLabelText(string objectName, Vector3 localPosition, Color color)
        {
            var textObject = new GameObject(objectName);
            textObject.transform.SetParent(labelRoot.transform, false);
            textObject.transform.localPosition = localPosition;
            var textMesh = textObject.AddComponent<TextMesh>();
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

            return textMesh;
        }

        private void RefreshLabelBillboard()
        {
            if (labelRoot == null)
            {
                return;
            }

            labelRoot.transform.position = transform.position + Vector3.up * GetLabelHeight();
            var camera = Camera.main;
            if (camera != null)
            {
                labelRoot.transform.rotation = camera.transform.rotation;
            }
        }

        private float GetLabelHeight()
        {
            switch (Type)
            {
                case BuildingType.Castle:
                    return 7.5f;
                case BuildingType.Forge:
                    return 6.3f;
                case BuildingType.AlchemistShop:
                case BuildingType.Infirmary:
                case BuildingType.CartographerHouse:
                case BuildingType.Chapel:
                case BuildingType.MinersGuild:
                case BuildingType.Market:
                case BuildingType.HeroHouse:
                case BuildingType.Tavern:
                    return 4.25f;
                case BuildingType.LumberjackCamp:
                    return 4f;
                case BuildingType.Farm:
                    return 3.15f;
                case BuildingType.PeasantHut:
                    return 2.25f;
                default:
                    return Mathf.Max(2.4f, FootprintRadius * 0.72f + 1.9f);
            }
        }

        private string BuildLabelText()
        {
            switch (Type)
            {
                case BuildingType.LumberjackCamp:
                    return "Лесорубы";
                case BuildingType.AlchemistShop:
                    return "Алхимик";
                case BuildingType.Infirmary:
                    return "Лазарет";
                case BuildingType.CartographerHouse:
                    return "Картограф";
                case BuildingType.Chapel:
                    return "Часовня";
                case BuildingType.MinersGuild:
                    return "Шахтёры";
                case BuildingType.Market:
                    return "Рынок";
                case BuildingType.PeasantHut:
                    return "Лачуга";
                default:
                    return string.IsNullOrEmpty(DisplayName) ? "Здание" : DisplayName;
            }
        }

        private void RefreshSelectionOutline()
        {
            if (selectionRoot != null)
            {
                Destroy(selectionRoot);
            }

            selectionRoot = new GameObject("Building Selection Outline");
            selectionRoot.transform.SetParent(transform, false);
            selectionRoot.transform.localPosition = Vector3.zero;
            selectionRoot.SetActive(false);

            var cellSize = EstimateCellSize();
            var halfExtent = FootprintRadius * cellSize + cellSize * 0.58f;
            var thickness = Mathf.Max(0.1f, cellSize * 0.08f);
            var length = halfExtent * 2f + thickness;
            var y = 0.055f;

            CreateOutlineSegment(
                "Selection North",
                new Vector3(0f, y, halfExtent),
                new Vector3(length, 0.035f, thickness));
            CreateOutlineSegment(
                "Selection South",
                new Vector3(0f, y, -halfExtent),
                new Vector3(length, 0.035f, thickness));
            CreateOutlineSegment(
                "Selection East",
                new Vector3(halfExtent, y, 0f),
                new Vector3(thickness, 0.035f, length));
            CreateOutlineSegment(
                "Selection West",
                new Vector3(-halfExtent, y, 0f),
                new Vector3(thickness, 0.035f, length));
        }

        private void CreateOutlineSegment(string objectName, Vector3 localPosition, Vector3 localScale)
        {
            var segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
            segment.name = objectName;
            segment.transform.SetParent(selectionRoot.transform, false);
            segment.transform.localPosition = localPosition;
            segment.transform.localScale = localScale;
            segment.GetComponent<Renderer>().sharedMaterial = GetSelectionMaterial();
            RemoveCollider(segment);
        }

        private float EstimateCellSize()
        {
            if (GridPosition.x != 0)
            {
                return Mathf.Abs(transform.position.x / GridPosition.x);
            }

            if (GridPosition.y != 0)
            {
                return Mathf.Abs(transform.position.z / GridPosition.y);
            }

            return DefaultCellSize;
        }

        private static Material GetLabelBackgroundMaterial()
        {
            if (labelBackgroundMaterial == null)
            {
                labelBackgroundMaterial = CreateTransparentMaterial(
                    "Building Label Background",
                    new Color(0.055f, 0.047f, 0.035f, 0.86f));
            }

            return labelBackgroundMaterial;
        }

        private static Material GetSelectionMaterial()
        {
            if (selectionMaterial == null)
            {
                selectionMaterial = CreateTransparentMaterial(
                    "Building Selection Gold",
                    new Color(1f, 0.76f, 0.22f, 0.78f));
            }

            return selectionMaterial;
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

        private static void RemoveCollider(GameObject target)
        {
            var collider = target.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }
        }
    }
}
