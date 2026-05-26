using UnityEngine;

namespace Labyrinth.Maze
{
    public sealed class MazeTerrain : MonoBehaviour
    {
        private const int HeightmapResolution = 65;
        private const int AlphamapResolution = 32;
        private const int DiffuseTextureSize = 128;
        public const int PaddingCells = 24;
        private const float SurfaceYOffset = -0.08f;
        private const float VisualSurfaceYOffset = -0.062f;
        private const float TerrainHeight = 0.16f;

        private GameObject terrainObject;
        private GameObject visualGroundObject;
        private TerrainData terrainData;
        private Terrain terrain;
        private TerrainLayer terrainLayer;
        private Mesh visualGroundMesh;
        private Texture2D diffuseTexture;
        private Material visualGroundMaterial;

        public void Render(MazeGenerationResult result, float cellSize)
        {
            Clear();
            if (result == null)
            {
                return;
            }

            var bounds = CalculateBounds(result, cellSize);
            terrainData = new TerrainData
            {
                heightmapResolution = HeightmapResolution,
                alphamapResolution = AlphamapResolution,
                baseMapResolution = DiffuseTextureSize,
                size = new Vector3(bounds.width, TerrainHeight, bounds.depth)
            };

            terrainData.SetHeights(0, 0, CreateGroundHeights());
            terrainLayer = CreateTerrainLayer(cellSize);
            terrainData.terrainLayers = new[] { terrainLayer };
            terrainData.SetAlphamaps(0, 0, CreateFullAlphamap());

            terrainObject = Terrain.CreateTerrainGameObject(terrainData);
            terrainObject.name = "Physical Terrain";
            terrainObject.transform.SetParent(transform, false);
            terrainObject.transform.position = new Vector3(bounds.minX, SurfaceYOffset, bounds.minZ);

            terrain = terrainObject.GetComponent<Terrain>();
            if (terrain != null)
            {
                terrain.drawInstanced = false;
                terrain.drawHeightmap = false;
                terrain.drawTreesAndFoliage = false;
                terrain.basemapDistance = 10000f;
            }

            var collider = terrainObject.GetComponent<TerrainCollider>();
            if (collider != null)
            {
                collider.terrainData = terrainData;
            }

            visualGroundObject = CreateVisualGround(bounds, terrainLayer.tileSize.x);
        }

        public void Clear()
        {
            if (terrainObject != null)
            {
                Destroy(terrainObject);
                terrainObject = null;
            }

            if (visualGroundObject != null)
            {
                Destroy(visualGroundObject);
                visualGroundObject = null;
            }

            DestroyRuntimeObject(terrainData);
            DestroyRuntimeObject(terrainLayer);
            DestroyRuntimeObject(visualGroundMesh);
            DestroyRuntimeObject(diffuseTexture);
            DestroyRuntimeObject(visualGroundMaterial);
            terrainData = null;
            terrain = null;
            terrainLayer = null;
            visualGroundMesh = null;
            diffuseTexture = null;
            visualGroundMaterial = null;
        }

        public void SetVisualVisible(bool visible)
        {
            if (terrain == null)
            {
                return;
            }

            terrain.enabled = true;
            terrain.drawHeightmap = false;
            terrain.drawTreesAndFoliage = false;
            if (visualGroundObject != null)
            {
                visualGroundObject.SetActive(visible);
            }
        }

        private static TerrainBounds CalculateBounds(MazeGenerationResult result, float cellSize)
        {
            var halfCell = cellSize * 0.5f;
            var padding = PaddingCells * cellSize;
            var minX = -padding - halfCell;
            var minZ = -padding - halfCell;
            var maxX = (result.Grid.Width - 1) * cellSize + padding + halfCell;
            var maxZ = (result.Grid.Height - 1) * cellSize + padding + halfCell;
            return new TerrainBounds(minX, minZ, maxX - minX, maxZ - minZ);
        }

        private TerrainLayer CreateTerrainLayer(float cellSize)
        {
            diffuseTexture = CreateDiffuseTexture();
            return new TerrainLayer
            {
                diffuseTexture = diffuseTexture,
                tileSize = Vector2.one * Mathf.Max(3.5f, cellSize * 5.5f),
                metallic = 0f,
                smoothness = 0.08f
            };
        }

        private GameObject CreateVisualGround(TerrainBounds bounds, float tileSize)
        {
            visualGroundMesh = new Mesh
            {
                name = "Terrain Meadow Visual Mesh"
            };
            visualGroundMesh.vertices = new[]
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(bounds.width, 0f, 0f),
                new Vector3(0f, 0f, bounds.depth),
                new Vector3(bounds.width, 0f, bounds.depth)
            };
            visualGroundMesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            visualGroundMesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(bounds.width / tileSize, 0f),
                new Vector2(0f, bounds.depth / tileSize),
                new Vector2(bounds.width / tileSize, bounds.depth / tileSize)
            };
            visualGroundMesh.RecalculateNormals();
            visualGroundMesh.RecalculateBounds();

            var ground = new GameObject("Terrain Meadow Visual");
            ground.transform.SetParent(transform, false);
            ground.transform.position = new Vector3(bounds.minX, VisualSurfaceYOffset, bounds.minZ);
            ground.AddComponent<MeshFilter>().sharedMesh = visualGroundMesh;
            ground.AddComponent<MeshRenderer>().sharedMaterial = CreateVisualGroundMaterial();
            return ground;
        }

        private Material CreateVisualGroundMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Texture");
            }

            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            visualGroundMaterial = new Material(shader)
            {
                name = "Terrain Meadow Visual Material",
                mainTexture = diffuseTexture
            };
            if (visualGroundMaterial.HasProperty("_BaseMap"))
            {
                visualGroundMaterial.SetTexture("_BaseMap", diffuseTexture);
            }

            if (visualGroundMaterial.HasProperty("_BaseColor"))
            {
                visualGroundMaterial.SetColor("_BaseColor", Color.white);
            }

            return visualGroundMaterial;
        }

        private static Texture2D CreateDiffuseTexture()
        {
            var texture = new Texture2D(DiffuseTextureSize, DiffuseTextureSize, TextureFormat.RGBA32, true)
            {
                name = "Terrain Meadow Texture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Repeat
            };

            var pixels = new Color[DiffuseTextureSize * DiffuseTextureSize];
            for (var y = 0; y < DiffuseTextureSize; y++)
            {
                for (var x = 0; x < DiffuseTextureSize; x++)
                {
                    var u = (float)x / DiffuseTextureSize;
                    var v = (float)y / DiffuseTextureSize;
                    pixels[y * DiffuseTextureSize + x] = SampleGroundColor(u, v, x, y);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static float[,] CreateGroundHeights()
        {
            var heights = new float[HeightmapResolution, HeightmapResolution];
            for (var y = 0; y < HeightmapResolution; y++)
            {
                for (var x = 0; x < HeightmapResolution; x++)
                {
                    var u = (float)x / (HeightmapResolution - 1);
                    var v = (float)y / (HeightmapResolution - 1);
                    var broad = Mathf.PerlinNoise(u * 3.2f + 11.7f, v * 3.2f + 4.9f);
                    var fine = Mathf.PerlinNoise(u * 12.5f + 2.1f, v * 12.5f + 18.6f);
                    heights[y, x] = Mathf.Clamp01((broad * 0.62f + fine * 0.38f - 0.28f) * 0.08f);
                }
            }

            return heights;
        }

        private static Color SampleGroundColor(float u, float v, int x, int y)
        {
            var shadowGrass = new Color(0.16f, 0.32f, 0.13f);
            var meadowGrass = new Color(0.28f, 0.52f, 0.18f);
            var freshGrass = new Color(0.42f, 0.66f, 0.24f);
            var clover = new Color(0.22f, 0.44f, 0.18f);
            var bareEarth = new Color(0.34f, 0.28f, 0.18f);
            var stone = new Color(0.52f, 0.51f, 0.43f);
            var flowerYellow = new Color(0.95f, 0.82f, 0.24f);
            var flowerWhite = new Color(0.86f, 0.88f, 0.72f);

            var broad = Mathf.PerlinNoise(u * 4.8f + 7.3f, v * 4.8f + 19.1f);
            var fine = Mathf.PerlinNoise(u * 23.5f + 3.4f, v * 23.5f + 8.2f);
            var mottled = Mathf.PerlinNoise(u * 58f + 15.5f, v * 58f + 2.7f);

            var color = Color.Lerp(shadowGrass, meadowGrass, Mathf.Clamp01(broad * 0.9f + 0.2f));
            color = Color.Lerp(color, freshGrass, Mathf.Clamp01((fine - 0.3f) * 0.72f));
            color = Color.Lerp(color, clover, Mathf.Clamp01((broad - 0.64f) * 0.62f));
            color = Color.Lerp(color, bareEarth, Mathf.Clamp01((0.2f - fine) * 0.26f));
            color *= 0.86f + mottled * 0.22f;

            var detail = Hash01(x, y);
            if (detail > 0.988f)
            {
                color = Color.Lerp(color, flowerYellow, 0.74f);
            }
            else if (detail > 0.976f)
            {
                color = Color.Lerp(color, flowerWhite, 0.62f);
            }
            else if (detail > 0.955f)
            {
                color = Color.Lerp(color, stone, 0.34f);
            }

            return new Color(color.r, color.g, color.b, 1f);
        }

        private static float Hash01(int x, int y)
        {
            unchecked
            {
                var hash = x * 73856093 ^ y * 19349663 ^ 0x51f15e;
                hash ^= hash >> 13;
                hash *= 1274126177;
                hash ^= hash >> 16;
                return (hash & 0x7fffffff) / (float)int.MaxValue;
            }
        }

        private static float[,,] CreateFullAlphamap()
        {
            var alphamaps = new float[AlphamapResolution, AlphamapResolution, 1];
            for (var y = 0; y < AlphamapResolution; y++)
            {
                for (var x = 0; x < AlphamapResolution; x++)
                {
                    alphamaps[y, x, 0] = 1f;
                }
            }

            return alphamaps;
        }

        private static void DestroyRuntimeObject(Object target)
        {
            if (target != null)
            {
                Destroy(target);
            }
        }

        private readonly struct TerrainBounds
        {
            public TerrainBounds(float minX, float minZ, float width, float depth)
            {
                this.minX = minX;
                this.minZ = minZ;
                this.width = width;
                this.depth = depth;
            }

            public readonly float minX;
            public readonly float minZ;
            public readonly float width;
            public readonly float depth;
        }
    }
}
