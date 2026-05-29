using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Labyrinth.Maze
{
    public sealed partial class MazeTerrain
    {
        public IEnumerator RenderAsync(
            MazeGenerationResult result,
            float cellSize,
            Action<float, string> reportProgress)
        {
            Clear();
            if (result == null)
            {
                yield break;
            }

            var bounds = CalculateBounds(result, cellSize);
            terrainData = new TerrainData
            {
                heightmapResolution = HeightmapResolution,
                alphamapResolution = AlphamapResolution,
                baseMapResolution = DiffuseTextureSize,
                size = new Vector3(bounds.width, TerrainHeight, bounds.depth)
            };

            reportProgress?.Invoke(0.08f, "Высоты земли");
            yield return null;
            terrainData.SetHeights(0, 0, CreateGroundHeights(result));

            reportProgress?.Invoke(0.2f, "Текстура земли");
            yield return null;
            terrainLayer = CreateTerrainLayer(cellSize);
            terrainData.terrainLayers = new[] { terrainLayer };
            terrainData.SetAlphamaps(0, 0, CreateFullAlphamap());

            reportProgress?.Invoke(0.32f, "Физическая земля");
            yield return null;
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

            yield return CreateVisualGroundAsync(bounds, result, cellSize, reportProgress);
        }

        private IEnumerator CreateVisualGroundAsync(
            TerrainBounds bounds,
            MazeGenerationResult result,
            float cellSize,
            Action<float, string> reportProgress)
        {
            visualGroundMesh = new Mesh
            {
                name = "Terrain Meadow Visual Mesh"
            };

            var cellsX = (result.Grid.Width + PaddingCells * 2) * VisualGroundSubdivisionsPerCell;
            var cellsZ = (result.Grid.Height + PaddingCells * 2) * VisualGroundSubdivisionsPerCell;
            var verticesX = cellsX + 1;
            var verticesZ = cellsZ + 1;
            var vertices = new Vector3[verticesX * verticesZ];
            var uvs = new Vector2[vertices.Length];
            var visualStep = cellSize / VisualGroundSubdivisionsPerCell;
            if (vertices.Length > 65535)
            {
                visualGroundMesh.indexFormat = IndexFormat.UInt32;
            }

            var vertexRowsPerFrame = Mathf.Max(8, verticesZ / 12);
            for (var z = 0; z < verticesZ; z++)
            {
                for (var x = 0; x < verticesX; x++)
                {
                    var index = z * verticesX + x;
                    var localX = x * visualStep;
                    var localZ = z * visualStep;
                    var gridX = x / (float)VisualGroundSubdivisionsPerCell - PaddingCells - 0.5f;
                    var gridY = z / (float)VisualGroundSubdivisionsPerCell - PaddingCells - 0.5f;
                    vertices[index] = new Vector3(localX, SampleVisualGroundHeight(result, gridX, gridY, cellSize), localZ);
                    uvs[index] = new Vector2(localX / bounds.width, localZ / bounds.depth);
                }

                if (z % vertexRowsPerFrame == 0 || z == verticesZ - 1)
                {
                    reportProgress?.Invoke(
                        Mathf.Lerp(0.38f, 0.66f, (z + 1f) / verticesZ),
                        $"Сетка земли {z + 1}/{verticesZ}");
                    yield return null;
                }
            }

            var triangles = new int[cellsX * cellsZ * 6];
            var triangleIndex = 0;
            var triangleRowsPerFrame = Mathf.Max(8, cellsZ / 12);
            for (var z = 0; z < cellsZ; z++)
            {
                for (var x = 0; x < cellsX; x++)
                {
                    var a = z * verticesX + x;
                    var b = a + 1;
                    var c = a + verticesX;
                    var d = c + 1;
                    triangles[triangleIndex++] = a;
                    triangles[triangleIndex++] = c;
                    triangles[triangleIndex++] = b;
                    triangles[triangleIndex++] = b;
                    triangles[triangleIndex++] = c;
                    triangles[triangleIndex++] = d;
                }

                if (z % triangleRowsPerFrame == 0 || z == cellsZ - 1)
                {
                    reportProgress?.Invoke(
                        Mathf.Lerp(0.66f, 0.88f, (z + 1f) / cellsZ),
                        $"Полигоны земли {z + 1}/{cellsZ}");
                    yield return null;
                }
            }

            reportProgress?.Invoke(0.92f, "Материал земли");
            yield return null;
            visualGroundMesh.vertices = vertices;
            visualGroundMesh.triangles = triangles;
            visualGroundMesh.uv = uvs;
            visualGroundMesh.RecalculateNormals();
            visualGroundMesh.RecalculateBounds();

            var ground = new GameObject("Terrain Meadow Visual");
            ground.transform.SetParent(transform, false);
            ground.transform.position = new Vector3(bounds.minX, VisualSurfaceYOffset, bounds.minZ);
            ground.AddComponent<MeshFilter>().sharedMesh = visualGroundMesh;
            var renderer = ground.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = CreateVisualGroundMaterial();
            renderer.receiveShadows = true;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            visualGroundObject = ground;

            reportProgress?.Invoke(1f, "Земля готова");
            yield return null;
        }
    }
}
