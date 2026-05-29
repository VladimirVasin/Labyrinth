using System;
using System.Collections.Generic;
using Labyrinth.Maze;
using UnityEngine;
using UnityEngine.Rendering;

namespace Labyrinth.Core
{
    public static partial class VoxelVisuals
    {
        public static readonly bool Enabled = true;

        private const float TopCapLocalHeight = 0.085f;
        private const float MinimumCapHeight = 0.09f;
        private const float MinimumCapFootprint = 0.055f;
        private static readonly Dictionary<Material, Material> TopCapMaterials = new Dictionary<Material, Material>();
        private static readonly Dictionary<Material, Material[]> BlockGridMaterials = new Dictionary<Material, Material[]>();
        private static readonly Dictionary<Material, Material> VoxelLitMaterials = new Dictionary<Material, Material>();
        private static readonly int VoxelLightColorId = Shader.PropertyToID("_VoxelLightColor");
        private static readonly MaterialPropertyBlock VoxelLightBlock = new MaterialPropertyBlock();

        public static PrimitiveType ResolvePrimitive(PrimitiveType requestedPrimitive, string objectName)
        {
            if (!Enabled || requestedPrimitive == PrimitiveType.Cube || requestedPrimitive == PrimitiveType.Quad || requestedPrimitive == PrimitiveType.Plane)
            {
                return requestedPrimitive;
            }

            return ShouldKeepRoundPrimitive(objectName) ? requestedPrimitive : PrimitiveType.Cube;
        }

        public static Material CreateLitMaterial(string materialName, Color color)
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
            ApplyMaterialProfile(material, color);
            ApplyGeneratedTexture(material, materialName, color);
            return material;
        }

        public static Material CreateEmissiveMaterial(string materialName, Color color, float intensity = 1.75f)
        {
            var boostedColor = new Color(
                color.r * Mathf.Max(1f, intensity * 0.82f),
                color.g * Mathf.Max(1f, intensity * 0.82f),
                color.b * Mathf.Max(1f, intensity * 0.82f),
                color.a);
            var material = CreateLitMaterial(materialName, boostedColor);
            var emission = new Color(color.r * intensity, color.g * intensity, color.b * intensity, color.a);
            if (material.HasProperty("_EmissionColor"))
            {
                material.SetColor("_EmissionColor", emission);
                material.EnableKeyword("_EMISSION");
            }

            return material;
        }

        public static void ApplyMaterialProfile(Material material, Color color)
        {
            if (material == null)
            {
                return;
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", 0f);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", 0.08f);
            }

            if (material.HasProperty("_Glossiness"))
            {
                material.SetFloat("_Glossiness", 0.08f);
            }

            if (material.HasProperty("_Cull"))
            {
                material.SetFloat("_Cull", (float)CullMode.Off);
            }

            if (color.a < 0.99f)
            {
                ConfigureTransparent(material);
            }
        }

        public static void ApplyBlockStyle(GameObject target, PrimitiveType requestedPrimitive, Material material, bool keepCollider)
        {
            if (!Enabled || target == null)
            {
                return;
            }

            var figurePart = IsFigureVoxelPart(target);
            var renderer = target.GetComponent<Renderer>();
            if (renderer != null)
            {
                ConfigureRendererShadows(renderer, target.name, figurePart);
            }

            TryAddProjectedGroundShadow(target, material, figurePart);

            if (TryApplyVoxelBlockGrid(target, requestedPrimitive, material, renderer))
            {
                return;
            }

            if (!ShouldAddTopCap(target, material))
            {
                return;
            }

            var cap = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cap.name = "Voxel Top Cap";
            cap.transform.SetParent(target.transform, false);
            cap.transform.localPosition = new Vector3(0f, 0.5f + TopCapLocalHeight * 0.5f, 0f);
            cap.transform.localRotation = Quaternion.identity;
            cap.transform.localScale = new Vector3(1.025f, TopCapLocalHeight, 1.025f);
            cap.GetComponent<Renderer>().sharedMaterial = GetTopCapMaterial(material);

            var collider = cap.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.Destroy(collider);
            }
        }

        public static void SpawnCombatBurst(MazeRenderer renderer, Vector2Int gridPosition, Color color, float delay = 0f)
        {
            SpawnBurst(renderer, gridPosition, color, 1.18f, 10, 0.22f, delay, true);
        }

        public static void SpawnPickupBurst(MazeRenderer renderer, Vector2Int gridPosition, Color color, float delay = 0f)
        {
            SpawnBurst(renderer, gridPosition, color, 1.05f, 8, 0.18f, delay, false);
        }

        public static GameObject CreateVoxelBlockGrid(
            string objectName,
            Transform parent,
            Vector3 position,
            Vector3 size,
            Material material,
            int blocksX,
            int blocksY,
            int blocksZ,
            float gapRatio,
            bool keepCollider)
        {
            return CreateVoxelBlockGrid(
                objectName,
                parent,
                position,
                size,
                new[] { material },
                null,
                blocksX,
                blocksY,
                blocksZ,
                gapRatio,
                keepCollider);
        }

        public static GameObject CreateVoxelBlockGrid(
            string objectName,
            Transform parent,
            Vector3 position,
            Vector3 size,
            Material[] materials,
            Func<int, int, int, int> selectMaterialIndex,
            int blocksX,
            int blocksY,
            int blocksZ,
            float gapRatio,
            bool keepCollider,
            string patternName = null,
            Func<int, int, int, bool> includeBlock = null)
        {
            var target = new GameObject(objectName);
            target.transform.SetParent(parent, false);
            target.transform.position = position;
            var visualPatternName = string.IsNullOrEmpty(patternName) ? objectName : patternName;

            var safeBlocksX = Mathf.Max(1, blocksX);
            var safeBlocksY = Mathf.Max(1, blocksY);
            var safeBlocksZ = Mathf.Max(1, blocksZ);
            var safeMaterials = NormalizeMaterials(materials);
            var blockCount = safeBlocksX * safeBlocksY * safeBlocksZ;
            var faceCount = blockCount * 6;
            var vertexCapacity = faceCount * 4;
            var triangleCapacity = Mathf.Max(36, faceCount * 6 / safeMaterials.Length + 36);
            var vertices = new List<Vector3>(vertexCapacity);
            var normals = new List<Vector3>(vertexCapacity);
            var colors = new List<Color32>(vertexCapacity);
            var uvs = new List<Vector2>(vertexCapacity);
            var submeshTriangles = new List<int>[safeMaterials.Length];
            for (var i = 0; i < submeshTriangles.Length; i++)
            {
                submeshTriangles[i] = new List<int>(triangleCapacity);
            }

            var step = new Vector3(
                size.x / safeBlocksX,
                size.y / safeBlocksY,
                size.z / safeBlocksZ);
            var blockSize = new Vector3(
                Mathf.Max(0.001f, step.x * (1f - gapRatio)),
                Mathf.Max(0.001f, step.y * (1f - gapRatio * 0.45f)),
                Mathf.Max(0.001f, step.z * (1f - gapRatio)));

            for (var x = 0; x < safeBlocksX; x++)
            {
                for (var y = 0; y < safeBlocksY; y++)
                {
                    for (var z = 0; z < safeBlocksZ; z++)
                    {
                        if (includeBlock != null && !includeBlock(x, y, z))
                        {
                            continue;
                        }

                        if (IsHiddenInteriorBlock(x, y, z, safeBlocksX, safeBlocksY, safeBlocksZ))
                        {
                            continue;
                        }

                        var center = new Vector3(
                            -size.x * 0.5f + step.x * (x + 0.5f),
                            -size.y * 0.5f + step.y * (y + 0.5f),
                            -size.z * 0.5f + step.z * (z + 0.5f));
                        var materialIndex = selectMaterialIndex == null ? 0 : selectMaterialIndex(x, y, z);
                        materialIndex = Mathf.Clamp(materialIndex, 0, safeMaterials.Length - 1);
                        AppendBox(
                            vertices,
                            normals,
                            colors,
                            uvs,
                            submeshTriangles[materialIndex],
                            center,
                            blockSize,
                            visualPatternName,
                            x,
                            y,
                            z,
                            safeBlocksX,
                            safeBlocksY,
                            safeBlocksZ);
                    }
                }
            }

            var mesh = new Mesh { name = $"{objectName} Mesh" };
            if (vertices.Count > 65535)
            {
                mesh.indexFormat = IndexFormat.UInt32;
            }

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetColors(colors);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = submeshTriangles.Length;
            for (var i = 0; i < submeshTriangles.Length; i++)
            {
                mesh.SetTriangles(submeshTriangles[i], i);
            }

            mesh.RecalculateBounds();
            target.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = target.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = GetVoxelLitMaterials(safeMaterials);
            ConfigureRendererShadows(renderer, objectName, false);

            if (keepCollider)
            {
                var collider = target.AddComponent<BoxCollider>();
                collider.center = Vector3.zero;
                collider.size = size;
            }

            return target;
        }

        public static void ApplyStaticMazeLightingProfile(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            foreach (var renderer in target.GetComponentsInChildren<Renderer>())
            {
                renderer.receiveShadows = false;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        public static void ApplyStaticMazeLightingProfile(Renderer renderer)
        {
            if (renderer == null)
            {
                return;
            }

            renderer.receiveShadows = false;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        public static void ApplyVoxelLightTint(Renderer renderer, Color tint)
        {
            if (renderer == null)
            {
                return;
            }

            renderer.GetPropertyBlock(VoxelLightBlock);
            VoxelLightBlock.SetColor(VoxelLightColorId, tint);
            renderer.SetPropertyBlock(VoxelLightBlock);
        }

        private static bool ShouldKeepRoundPrimitive(string objectName)
        {
            if (string.IsNullOrEmpty(objectName))
            {
                return false;
            }

            return Contains(objectName, "Selection")
                || Contains(objectName, "Hover")
                || Contains(objectName, "Marker")
                || Contains(objectName, "Fog")
                || Contains(objectName, "Vision")
                || Contains(objectName, "Range");
        }

        private static bool ShouldAddTopCap(GameObject target, Material material)
        {
            if (target == null || material == null || target.transform.Find("Voxel Top Cap") != null)
            {
                return false;
            }

            if (IsTransparent(material) || ShouldKeepRoundPrimitive(target.name) || Contains(target.name, "Shadow") || Contains(target.name, "River"))
            {
                return false;
            }

            var scale = target.transform.localScale;
            return Mathf.Abs(scale.y) >= MinimumCapHeight
                && Mathf.Abs(scale.x) >= MinimumCapFootprint
                && Mathf.Abs(scale.z) >= MinimumCapFootprint;
        }

        private static Material GetTopCapMaterial(Material baseMaterial)
        {
            if (baseMaterial == null)
            {
                return null;
            }

            if (TopCapMaterials.TryGetValue(baseMaterial, out var cached))
            {
                return cached;
            }

            var color = GetMaterialColor(baseMaterial);
            var capColor = Color.Lerp(color, Color.white, 0.12f);
            capColor.a = color.a;
            var capMaterial = new Material(baseMaterial)
            {
                name = $"{baseMaterial.name} Top Cap"
            };
            ApplyMaterialProfile(capMaterial, capColor);
            TopCapMaterials[baseMaterial] = capMaterial;
            return capMaterial;
        }

        private static Color GetMaterialColor(Material material)
        {
            if (material.HasProperty("_BaseColor"))
            {
                return material.GetColor("_BaseColor");
            }

            return material.HasProperty("_Color") ? material.GetColor("_Color") : material.color;
        }

        private static bool IsTransparent(Material material)
        {
            return GetMaterialColor(material).a < 0.98f;
        }

        private static bool Contains(string value, string pattern)
        {
            return value.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void ConfigureTransparent(Material material)
        {
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
            }

            if (material.HasProperty("_SrcBlend"))
            {
                material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            }

            if (material.HasProperty("_DstBlend"))
            {
                material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            }

            if (material.HasProperty("_ZWrite"))
            {
                material.SetInt("_ZWrite", 0);
            }

            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.EnableKeyword("_ALPHABLEND_ON");
        }

        private static bool TryApplyVoxelBlockGrid(
            GameObject target,
            PrimitiveType requestedPrimitive,
            Material material,
            Renderer renderer)
        {
            if (target == null
                || material == null
                || renderer == null
                || requestedPrimitive == PrimitiveType.Quad
                || requestedPrimitive == PrimitiveType.Plane
                || target.transform.Find("Voxel Block Grid") != null
                || !ShouldUseVoxelBlockGrid(target, material))
            {
                return false;
            }

            var figurePart = IsFigureVoxelPart(target);
            var scale = target.transform.localScale;
            var blocksX = CalculateBlockGridAxis(scale.x, figurePart ? 9 : 14, figurePart);
            var blocksY = CalculateBlockGridAxis(scale.y, figurePart ? 11 : 16, figurePart);
            var blocksZ = CalculateBlockGridAxis(scale.z, figurePart ? 9 : 14, figurePart);
            var materials = GetBlockGridMaterials(material, target.name);
            var gridObject = CreateVoxelBlockGrid(
                "Voxel Block Grid",
                target.transform,
                target.transform.position,
                Vector3.one,
                materials,
                (x, y, z) => SelectBlockGridMaterial(target.name, x, y, z, materials.Length),
                blocksX,
                blocksY,
                blocksZ,
                figurePart ? 0.095f : 0.075f,
                false,
                target.name);
            if (figurePart)
            {
                var animator = gridObject.AddComponent<VoxelFigurePartAnimator>();
                animator.Initialize(Hash(target.name, blocksX, blocksY, blocksZ), IsAccessoryFigurePart(target.name));
            }

            renderer.forceRenderingOff = true;
            renderer.receiveShadows = false;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            ConfigureRendererShadows(gridObject.GetComponent<Renderer>(), target.name, figurePart);
            return true;
        }

        private static bool ShouldUseVoxelBlockGrid(GameObject target, Material material)
        {
            var figurePart = IsFigureVoxelPart(target);
            if (IsTransparent(material)
                || ShouldKeepRoundPrimitive(target.name)
                || Contains(target.name, "Shadow")
                || Contains(target.name, "River")
                || Contains(target.name, "Voxel")
                || Contains(target.name, "Flow"))
            {
                return false;
            }

            var scale = target.transform.localScale;
            if (figurePart)
            {
                return Mathf.Abs(scale.x) >= 0.008f
                    && Mathf.Abs(scale.y) >= 0.008f
                    && Mathf.Abs(scale.z) >= 0.008f
                    && Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)) >= 0.02f;
            }

            return Mathf.Abs(scale.x) >= 0.018f
                && Mathf.Abs(scale.y) >= 0.012f
                && Mathf.Abs(scale.z) >= 0.018f
                && Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)) >= 0.045f;
        }

        private static int CalculateBlockGridAxis(float size, int maxBlocks, bool figurePart)
        {
            var absolute = Mathf.Abs(size);
            if (!figurePart)
            {
                var blocks = Mathf.Clamp(Mathf.RoundToInt(absolute / 0.14f), 1, maxBlocks);
                return absolute >= 0.18f ? Mathf.Max(2, blocks) : blocks;
            }

            var figureBlocks = Mathf.Clamp(Mathf.RoundToInt(absolute / 0.043f), 1, maxBlocks);
            return absolute >= 0.055f ? Mathf.Max(2, figureBlocks) : figureBlocks;
        }

        private static bool IsFigureVoxelPart(GameObject target)
        {
            return target != null
                && (IsFigureContextName(target.name) || HasFigureVoxelParent(target.transform));
        }

        private static bool HasFigureVoxelParent(Transform transform)
        {
            var current = transform == null ? null : transform.parent;
            for (var depth = 0; depth < 4 && current != null; depth++)
            {
                if (IsFigureContextName(current.name))
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private static bool IsFigureContextName(string name)
        {
            return Contains(name, "Knight Visual")
                || Contains(name, "Hero Knight")
                || Contains(name, "Mob")
                || Contains(name, "Rat")
                || Contains(name, "Orc")
                || Contains(name, "Goblin")
                || Contains(name, "Worker")
                || Contains(name, "Walker")
                || Contains(name, "Courier")
                || Contains(name, "Tax Collector")
                || Contains(name, "Farm Cart")
                || Contains(name, "Mine Cart")
                || Contains(name, "Gold Ingot")
                || Contains(name, "Hero Death Token")
                || Contains(name, "Key");
        }

        private static bool IsAccessoryFigurePart(string name)
        {
            return Contains(name, "Shield")
                || Contains(name, "Sword")
                || Contains(name, "Club")
                || Contains(name, "Guard")
                || Contains(name, "Spear")
                || Contains(name, "Axe")
                || Contains(name, "Hammer")
                || Contains(name, "Pick")
                || Contains(name, "Bottle")
                || Contains(name, "Mug")
                || Contains(name, "Candle")
                || Contains(name, "Cross")
                || Contains(name, "Basket")
                || Contains(name, "Satchel")
                || Contains(name, "Scroll")
                || Contains(name, "Pack")
                || Contains(name, "Coin");
        }

        private static Material CreateMaterialVariant(Material baseMaterial, string name, Color color)
        {
            var material = new Material(baseMaterial)
            {
                name = name
            };
            ApplyMaterialProfile(material, color);
            return material;
        }

        private static void ApplyGeneratedTexture(Material material, string materialName, Color color)
        {
            if (GeneratedTextureLibrary.TryGetTexture(materialName, color, out var texture, out var scale))
            {
                ApplyTexture(material, texture, scale);
            }
        }

        private static bool CopyMaterialTexture(Material source, Material target)
        {
            if (source == null || target == null)
            {
                return false;
            }

            var texture = source.mainTexture;
            var scale = source.mainTextureScale;
            if (source.HasProperty("_BaseMap"))
            {
                var baseMap = source.GetTexture("_BaseMap");
                if (baseMap != null)
                {
                    texture = baseMap;
                    scale = source.GetTextureScale("_BaseMap");
                }
            }

            if (texture == null && source.HasProperty("_MainTex"))
            {
                texture = source.GetTexture("_MainTex");
                scale = source.GetTextureScale("_MainTex");
            }

            if (texture == null)
            {
                return false;
            }

            ApplyTexture(target, texture, scale);
            return true;
        }

        private static void ApplyTexture(Material material, Texture texture, Vector2 scale)
        {
            if (material == null || texture == null)
            {
                return;
            }

            material.mainTexture = texture;
            material.mainTextureScale = scale;
            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
                material.SetTextureScale("_BaseMap", scale);
            }

            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", texture);
                material.SetTextureScale("_MainTex", scale);
            }
        }

        private static void ConfigureRendererShadows(Renderer renderer, string objectName, bool figurePart)
        {
            if (renderer == null)
            {
                return;
            }

            renderer.receiveShadows = ShouldReceiveRealtimeShadows(objectName);
            renderer.shadowCastingMode = ShouldCastRealtimeShadow(objectName, figurePart)
                ? ShadowCastingMode.On
                : ShadowCastingMode.Off;
        }

        private static bool ShouldReceiveRealtimeShadows(string objectName)
        {
            return false;
        }

        private static bool ShouldCastRealtimeShadow(string objectName, bool figurePart)
        {
            if (Contains(objectName, "Shadow")
                || Contains(objectName, "Selection")
                || Contains(objectName, "Marker")
                || Contains(objectName, "Label")
                || Contains(objectName, "Overlay")
                || Contains(objectName, "River")
                || Contains(objectName, "Flow")
                || Contains(objectName, "Fog")
                || Contains(objectName, "Floor")
                || Contains(objectName, "Path")
                || Contains(objectName, "Entrance"))
            {
                return false;
            }

            return false;
        }

        private static Material[] GetVoxelLitMaterials(Material[] baseMaterials)
        {
            var materials = new Material[baseMaterials.Length];
            for (var i = 0; i < baseMaterials.Length; i++)
            {
                materials[i] = GetVoxelLitMaterial(baseMaterials[i]);
            }

            return materials;
        }

        private static Material GetVoxelLitMaterial(Material baseMaterial)
        {
            if (baseMaterial == null)
            {
                return null;
            }

            if (VoxelLitMaterials.TryGetValue(baseMaterial, out var cached))
            {
                return cached;
            }

            var shader = Shader.Find("Labyrinth/Voxel Vertex Color Lit");
            if (shader == null)
            {
                VoxelLitMaterials[baseMaterial] = baseMaterial;
                return baseMaterial;
            }

            var color = GetMaterialColor(baseMaterial);
            var material = new Material(shader)
            {
                name = $"{baseMaterial.name} Voxel Lit",
                color = color
            };
            ApplyMaterialProfile(material, color);
            if (!CopyMaterialTexture(baseMaterial, material))
            {
                ApplyGeneratedTexture(material, baseMaterial.name, color);
            }

            VoxelLitMaterials[baseMaterial] = material;
            return material;
        }

        private static void SpawnBurst(
            MazeRenderer renderer,
            Vector2Int gridPosition,
            Color color,
            float height,
            int count,
            float spread,
            float delay,
            bool impact)
        {
            if (!Enabled || renderer == null)
            {
                return;
            }

            var root = new GameObject(impact ? "Voxel Impact Burst" : "Voxel Pickup Burst");
            root.transform.position = renderer.GridToWorld(gridPosition) + new Vector3(0f, height * renderer.ModelUnitSize, 0f);
            var burst = root.AddComponent<VoxelBurstView>();
            burst.Initialize(color, renderer.ModelUnitSize, count, spread, Mathf.Max(0f, delay), impact);
        }

        private static int Hash(string value, int x, int y, int z)
        {
            unchecked
            {
                var hash = string.IsNullOrEmpty(value) ? 17 : value.GetHashCode();
                hash = hash * 397 ^ x * 73856093;
                hash = hash * 397 ^ y * 19349663;
                hash = hash * 397 ^ z * 83492791;
                hash ^= hash >> 13;
                hash *= 1274126177;
                hash ^= hash >> 16;
                return hash & 0x7fffffff;
            }
        }

        private static Material[] NormalizeMaterials(Material[] materials)
        {
            if (materials == null || materials.Length == 0)
            {
                return new[] { CreateLitMaterial("Voxel Fallback", Color.magenta) };
            }

            return materials;
        }

        private static bool IsHiddenInteriorBlock(int x, int y, int z, int blocksX, int blocksY, int blocksZ)
        {
            return blocksX > 2
                && blocksY > 2
                && blocksZ > 2
                && x > 0
                && y > 0
                && z > 0
                && x < blocksX - 1
                && y < blocksY - 1
                && z < blocksZ - 1;
        }

        private static void AppendBox(
            List<Vector3> vertices,
            List<Vector3> normals,
            List<Color32> colors,
            List<Vector2> uvs,
            List<int> triangles,
            Vector3 center,
            Vector3 size,
            string objectName,
            int blockX,
            int blockY,
            int blockZ,
            int blocksX,
            int blocksY,
            int blocksZ)
        {
            var half = size * 0.5f;
            var min = center - half;
            var max = center + half;

            AppendFace(vertices, normals, colors, uvs, triangles, Vector3.forward, CalculateFaceColor(objectName, Vector3.forward, blockX, blockY, blockZ, blocksX, blocksY, blocksZ),
                new Vector3(min.x, min.y, max.z),
                new Vector3(min.x, max.y, max.z),
                new Vector3(max.x, max.y, max.z),
                new Vector3(max.x, min.y, max.z));
            AppendFace(vertices, normals, colors, uvs, triangles, Vector3.back, CalculateFaceColor(objectName, Vector3.back, blockX, blockY, blockZ, blocksX, blocksY, blocksZ),
                new Vector3(max.x, min.y, min.z),
                new Vector3(max.x, max.y, min.z),
                new Vector3(min.x, max.y, min.z),
                new Vector3(min.x, min.y, min.z));
            AppendFace(vertices, normals, colors, uvs, triangles, Vector3.right, CalculateFaceColor(objectName, Vector3.right, blockX, blockY, blockZ, blocksX, blocksY, blocksZ),
                new Vector3(max.x, min.y, max.z),
                new Vector3(max.x, max.y, max.z),
                new Vector3(max.x, max.y, min.z),
                new Vector3(max.x, min.y, min.z));
            AppendFace(vertices, normals, colors, uvs, triangles, Vector3.left, CalculateFaceColor(objectName, Vector3.left, blockX, blockY, blockZ, blocksX, blocksY, blocksZ),
                new Vector3(min.x, min.y, min.z),
                new Vector3(min.x, max.y, min.z),
                new Vector3(min.x, max.y, max.z),
                new Vector3(min.x, min.y, max.z));
            AppendFace(vertices, normals, colors, uvs, triangles, Vector3.up, CalculateFaceColor(objectName, Vector3.up, blockX, blockY, blockZ, blocksX, blocksY, blocksZ),
                new Vector3(min.x, max.y, max.z),
                new Vector3(min.x, max.y, min.z),
                new Vector3(max.x, max.y, min.z),
                new Vector3(max.x, max.y, max.z));
            AppendFace(vertices, normals, colors, uvs, triangles, Vector3.down, CalculateFaceColor(objectName, Vector3.down, blockX, blockY, blockZ, blocksX, blocksY, blocksZ),
                new Vector3(min.x, min.y, min.z),
                new Vector3(min.x, min.y, max.z),
                new Vector3(max.x, min.y, max.z),
                new Vector3(max.x, min.y, min.z));
        }

        private static void AppendFace(
            List<Vector3> vertices,
            List<Vector3> normals,
            List<Color32> colors,
            List<Vector2> uvs,
            List<int> triangles,
            Vector3 normal,
            Color32 color,
            Vector3 a,
            Vector3 b,
            Vector3 c,
            Vector3 d)
        {
            var index = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            vertices.Add(d);
            normals.Add(normal);
            normals.Add(normal);
            normals.Add(normal);
            normals.Add(normal);
            colors.Add(color);
            colors.Add(color);
            colors.Add(color);
            colors.Add(color);
            uvs.Add(new Vector2(0f, 0f));
            uvs.Add(new Vector2(0f, 1f));
            uvs.Add(new Vector2(1f, 1f));
            uvs.Add(new Vector2(1f, 0f));
            triangles.Add(index);
            triangles.Add(index + 1);
            triangles.Add(index + 2);
            triangles.Add(index);
            triangles.Add(index + 2);
            triangles.Add(index + 3);
        }

        private static Color32 CalculateFaceColor(
            string objectName,
            Vector3 normal,
            int blockX,
            int blockY,
            int blockZ,
            int blocksX,
            int blocksY,
            int blocksZ)
        {
            var y01 = blocksY <= 1 ? 1f : blockY / (float)(blocksY - 1);
            var heightLight = Mathf.Lerp(0.92f, 1.12f, y01);
            var faceLight = 0.96f;
            var tint = new Color(0.95f, 0.96f, 1f, 1f);
            if (normal == Vector3.up)
            {
                faceLight = 1.24f;
                tint = new Color(1f, 0.98f, 0.92f, 1f);
            }
            else if (normal == Vector3.down)
            {
                faceLight = 0.72f;
                tint = new Color(0.86f, 0.88f, 0.94f, 1f);
            }
            else if (normal == Vector3.right)
            {
                faceLight = 1.04f;
                tint = new Color(1f, 0.95f, 0.88f, 1f);
            }
            else if (normal == Vector3.left)
            {
                faceLight = 0.9f;
                tint = new Color(0.9f, 0.93f, 1f, 1f);
            }
            else if (normal == Vector3.forward)
            {
                faceLight = 0.98f;
                tint = new Color(0.92f, 0.96f, 1f, 1f);
            }
            else if (normal == Vector3.back)
            {
                faceLight = 0.9f;
                tint = new Color(0.9f, 0.92f, 0.98f, 1f);
            }

            var contactShadow = blockY == 0 ? 0.96f : 1f;
            var edgeDistance = Mathf.Min(
                Mathf.Min(blockX, blocksX - 1 - blockX),
                Mathf.Min(blockZ, blocksZ - 1 - blockZ));
            var edgeLight = edgeDistance <= 0 ? 1.04f : 1f;
            var noise = ((Hash(objectName, blockX, blockY, blockZ) % 1000) / 999f - 0.5f) * 0.045f;
            var pattern = CalculateSemanticPatternLight(objectName, normal, blockX, blockY, blockZ);
            var light = Mathf.Clamp(faceLight * heightLight * contactShadow * edgeLight * pattern + noise, 0.68f, 1.3f);
            return new Color(tint.r * light, tint.g * light, tint.b * light, 1f);
        }
    }
}
