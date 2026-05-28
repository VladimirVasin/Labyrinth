using Labyrinth.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Labyrinth.Maze
{
    public sealed partial class TerrainDecorationController
    {
        private void EnsureMaterials()
        {
            trunkMaterial = trunkMaterial != null ? trunkMaterial : CreateMaterial("Terrain Decor Trunks", new Color(0.32f, 0.18f, 0.08f));
            leafMaterial = leafMaterial != null ? leafMaterial : CreateMaterial("Terrain Decor Leaves", new Color(0.2f, 0.55f, 0.19f));
            darkLeafMaterial = darkLeafMaterial != null ? darkLeafMaterial : CreateMaterial("Terrain Decor Dark Leaves", new Color(0.12f, 0.36f, 0.15f));
            bushMaterial = bushMaterial != null ? bushMaterial : CreateMaterial("Terrain Decor Bush", new Color(0.24f, 0.62f, 0.22f));
            rockMaterial = rockMaterial != null ? rockMaterial : CreateMaterial("Terrain Decor Rock", new Color(0.44f, 0.44f, 0.4f));
            riverStraightHorizontalMaterial = riverStraightHorizontalMaterial != null ? riverStraightHorizontalMaterial : CreateTexturedMaterial("Terrain River Straight Horizontal", RiverStraightHorizontalTexturePath, new Color(0.12f, 0.42f, 0.72f));
            riverStraightVerticalMaterial = riverStraightVerticalMaterial != null ? riverStraightVerticalMaterial : CreateTexturedMaterial("Terrain River Straight Vertical", RiverStraightVerticalTexturePath, new Color(0.12f, 0.42f, 0.72f));
            riverCornerNorthWestMaterial = riverCornerNorthWestMaterial != null ? riverCornerNorthWestMaterial : CreateTexturedMaterial("Terrain River Corner NW", RiverCornerNorthWestTexturePath, new Color(0.12f, 0.42f, 0.72f));
            riverCornerNorthEastMaterial = riverCornerNorthEastMaterial != null ? riverCornerNorthEastMaterial : CreateTexturedMaterial("Terrain River Corner NE", RiverCornerNorthEastTexturePath, new Color(0.12f, 0.42f, 0.72f));
            riverCornerSouthEastMaterial = riverCornerSouthEastMaterial != null ? riverCornerSouthEastMaterial : CreateTexturedMaterial("Terrain River Corner SE", RiverCornerSouthEastTexturePath, new Color(0.12f, 0.42f, 0.72f));
            riverCornerSouthWestMaterial = riverCornerSouthWestMaterial != null ? riverCornerSouthWestMaterial : CreateTexturedMaterial("Terrain River Corner SW", RiverCornerSouthWestTexturePath, new Color(0.12f, 0.42f, 0.72f));
            bankMaterial = bankMaterial != null ? bankMaterial : CreateMaterial("Terrain Decor Bank", new Color(0.42f, 0.34f, 0.2f));
            bridgeMaterial = bridgeMaterial != null ? bridgeMaterial : CreateMaterial("Terrain Decor Bridge", new Color(0.48f, 0.28f, 0.11f));
            flowerMaterial = flowerMaterial != null ? flowerMaterial : CreateMaterial("Terrain Decor Flowers", new Color(0.96f, 0.78f, 0.24f));
            riverFlowMaterial = riverFlowMaterial != null ? riverFlowMaterial : CreateRiverFlowMaterial();
        }

        private static Material CreateMaterial(string materialName, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            var material = new Material(shader) { name = materialName, color = color };
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            return material;
        }

        private Material CreateRiverFlowMaterial()
        {
            riverFlowTexture = riverFlowTexture != null ? riverFlowTexture : CreateRiverFlowTexture();
            var shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Transparent");
            }

            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            }

            if (shader == null)
            {
                GameDebugLog.Warning("Terrain", "River flow animation disabled: no transparent shader found.");
                return null;
            }

            var material = new Material(shader)
            {
                name = "Terrain River Flow",
                mainTexture = riverFlowTexture,
                mainTextureScale = Vector2.one
            };
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", riverFlowTexture);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", Color.white);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", Color.white);
            }

            return material;
        }

        private static Texture2D CreateRiverFlowTexture()
        {
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Terrain River Flow Texture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Repeat
            };
            var pixels = new Color[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var u = x / (float)size;
                    var v = y / (float)size;
                    var wave = Mathf.Sin((u * 7.5f + v * 3.2f) * Mathf.PI * 2f);
                    var fine = Mathf.Sin((u * 18.3f - v * 5.6f) * Mathf.PI * 2f);
                    var alpha = Mathf.Clamp01((wave * 0.55f + fine * 0.22f - 0.48f) * 1.4f) * 0.34f;
                    pixels[y * size + x] = new Color(0.7f, 0.94f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, false);
            return texture;
        }

        private static Material CreateTexturedMaterial(string materialName, string resourcePath, Color fallbackColor)
        {
            var texture = Resources.Load<Texture2D>(resourcePath);
            var material = CreateMaterial(materialName, texture != null ? Color.white : fallbackColor);
            if (texture == null)
            {
                return material;
            }

            material.mainTexture = texture;
            material.mainTextureScale = Vector2.one;
            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", Color.white);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", 0.22f);
            }

            return material;
        }

        private static void RemoveCollider(GameObject target)
        {
            var collider = target.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }
        }

        private enum TerrainSide
        {
            Left,
            Right,
            Bottom,
            Top
        }

        private enum DecorationKind
        {
            Nature,
            River,
            Bridge
        }

        private sealed class DecorationRuntime
        {
            private readonly GameObject root;

            public DecorationRuntime(GameObject root, Vector2Int position, DecorationKind kind, int clearance)
            {
                this.root = root;
                Position = position;
                Kind = kind;
                Clearance = clearance;
            }

            public Vector2Int Position { get; }

            public DecorationKind Kind { get; }

            public int Clearance { get; }

            public void Destroy()
            {
                if (root != null)
                {
                    Object.Destroy(root);
                }
            }
        }
    }
}
