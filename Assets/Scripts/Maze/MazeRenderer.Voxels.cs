using System.Collections.Generic;
using Labyrinth.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Labyrinth.Maze
{
    public sealed partial class MazeRenderer
    {
        private const MazeVisualMode ActiveMazeVisualMode = MazeVisualMode.VoxelReadable;

        private static readonly MazeVisualProfile StableMazeProfile = new MazeVisualProfile(
            MazeVisualMode.Stable,
            1,
            1,
            1,
            1,
            0.02f,
            0.02f);

        private static readonly MazeVisualProfile ReadableVoxelMazeProfile = new MazeVisualProfile(
            MazeVisualMode.VoxelReadable,
            3,
            2,
            2,
            2,
            0.12f,
            0.07f);

        private Material voxelWallMaterial;
        private Material voxelPathMaterial;
        private Material voxelEntranceMaterial;
        private static readonly int AmbientScaleId = Shader.PropertyToID("_AmbientScale");
        private static readonly int MainLightScaleId = Shader.PropertyToID("_MainLightScale");
        private static readonly int AdditionalLightScaleId = Shader.PropertyToID("_AdditionalLightScale");

        private enum MazeVisualMode
        {
            Stable,
            VoxelReadable
        }

        [System.Flags]
        private enum MazeWallSide
        {
            None = 0,
            North = 1,
            East = 2,
            South = 4,
            West = 8
        }

        private readonly struct MazeVisualProfile
        {
            public MazeVisualProfile(
                MazeVisualMode mode,
                int floorDivisions,
                int wallTopDivisions,
                int wallSideColumns,
                int wallSideRows,
                float wallNoise,
                float floorNoise)
            {
                Mode = mode;
                FloorDivisions = floorDivisions;
                WallTopDivisions = wallTopDivisions;
                WallSideColumns = wallSideColumns;
                WallSideRows = wallSideRows;
                WallNoise = wallNoise;
                FloorNoise = floorNoise;
            }

            public MazeVisualMode Mode { get; }

            public int FloorDivisions { get; }

            public int WallTopDivisions { get; }

            public int WallSideColumns { get; }

            public int WallSideRows { get; }

            public float WallNoise { get; }

            public float FloorNoise { get; }
        }

        partial void EnsureVoxelMaterials()
        {
            if (voxelWallMaterial != null)
            {
                return;
            }

            voxelWallMaterial = CreatePlainMazeMaterial("Maze Dungeon Wall", new Color(0.255f, 0.245f, 0.225f));
            voxelPathMaterial = CreatePlainMazeMaterial("Maze Dungeon Floor", new Color(0.46f, 0.405f, 0.315f));
            voxelEntranceMaterial = CreatePlainMazeMaterial("Maze Dungeon Entrance", new Color(0.13f, 0.56f, 0.58f));
        }

        private bool RenderVoxelCell(MazeCell cell, MazeGrid grid)
        {
            if (!VoxelVisuals.Enabled)
            {
                return false;
            }

            var profile = GetActiveMazeProfile();
            if (profile.Mode == MazeVisualMode.Stable)
            {
                return RenderStableVoxelCell(cell);
            }

            return RenderReadableVoxelCell(cell, grid, profile);
        }

        private static Material CreatePlainMazeMaterial(string materialName, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            var material = new Material(shader)
            {
                name = materialName,
                color = color,
                mainTexture = Texture2D.whiteTexture
            };
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", Texture2D.whiteTexture);
            }

            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", Texture2D.whiteTexture);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", 0.01f);
            }

            if (material.HasProperty("_Glossiness"))
            {
                material.SetFloat("_Glossiness", 0.01f);
            }

            if (material.HasProperty("_SpecularHighlights"))
            {
                material.SetFloat("_SpecularHighlights", 0f);
            }

            if (material.HasProperty("_EnvironmentReflections"))
            {
                material.SetFloat("_EnvironmentReflections", 0f);
            }

            return material;
        }

        private static MazeVisualProfile GetActiveMazeProfile()
        {
            return ActiveMazeVisualMode == MazeVisualMode.Stable
                ? StableMazeProfile
                : ReadableVoxelMazeProfile;
        }

        private bool RenderStableVoxelCell(MazeCell cell)
        {
            var cellPosition = new Vector2Int(cell.X, cell.Y);
            var position = GridToWorld(cellPosition);
            if (cell.Type == MazeCellType.Wall)
            {
                var currentWallHeight = WallHeight;
                var wall = CreateStableMazeBlock(
                    "Maze Wall Stable",
                    position + new Vector3(0f, currentWallHeight * 0.5f, 0f),
                    new Vector3(cellSize * VisualWallWidthRatio, currentWallHeight, cellSize * VisualWallWidthRatio),
                    wallMaterial);
                var wallRenderer = wall.GetComponent<Renderer>();
                VoxelVisuals.ApplyStaticMazeLightingProfile(wallRenderer, true);
                TrackCellRenderer(cellPosition, wallRenderer);
                return true;
            }

            var isEntrance = cell.Type == MazeCellType.Entrance;
            var floor = CreateStableMazeBlock(
                isEntrance ? "Maze Entrance Stable" : "Maze Path Stable",
                position + new Vector3(0f, Scale(-0.03f), 0f),
                new Vector3(cellSize * VisualFloorWidthRatio, Scale(0.05f), cellSize * VisualFloorWidthRatio),
                isEntrance ? entranceMaterial : pathMaterial);
            var floorRenderer = floor.GetComponent<Renderer>();
            VoxelVisuals.ApplyStaticMazeLightingProfile(floorRenderer);
            TrackCellRenderer(cellPosition, floorRenderer);
            return true;
        }

        private bool RenderReadableVoxelCell(MazeCell cell, MazeGrid grid, MazeVisualProfile profile)
        {
            var cellPosition = new Vector2Int(cell.X, cell.Y);
            if (cell.Type == MazeCellType.Wall)
            {
                var wall = CreateMazeVoxelSurface(
                    "Maze Wall Voxel Surface",
                    cellPosition,
                    voxelWallMaterial ?? wallMaterial,
                    true,
                    GetExposedWallSides(grid, cellPosition),
                    profile);
                var wallRenderer = wall.GetComponent<Renderer>();
                VoxelVisuals.ApplyStaticMazeLightingProfile(wallRenderer, true);
                TrackCellRenderer(cellPosition, wallRenderer);
                return true;
            }

            var isEntrance = cell.Type == MazeCellType.Entrance;
            var floor = CreateMazeVoxelSurface(
                isEntrance ? "Maze Entrance Voxel Surface" : "Maze Path Voxel Surface",
                cellPosition,
                isEntrance ? (voxelEntranceMaterial ?? entranceMaterial) : (voxelPathMaterial ?? pathMaterial),
                false,
                MazeWallSide.None,
                profile);
            var floorRenderer = floor.GetComponent<Renderer>();
            VoxelVisuals.ApplyStaticMazeLightingProfile(floorRenderer);
            TrackCellRenderer(cellPosition, floorRenderer);
            return true;
        }

        private GameObject CreateStableMazeBlock(string objectName, Vector3 position, Vector3 scale, Material material)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = objectName;
            block.transform.SetParent(root, false);
            block.transform.position = position;
            block.transform.localScale = scale;

            var renderer = block.GetComponent<Renderer>();
            renderer.sharedMaterial = material;

            RemoveCollider(block);
            return block;
        }

        private GameObject CreateMazeVoxelSurface(
            string objectName,
            Vector2Int cellPosition,
            Material material,
            bool wall,
            MazeWallSide sides,
            MazeVisualProfile profile)
        {
            var target = new GameObject(objectName);
            target.transform.SetParent(root, false);
            target.transform.position = GridToWorld(cellPosition);

            var mesh = BuildMazeVoxelSurfaceMesh(objectName, cellPosition, wall, sides, profile);
            target.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = target.AddComponent<MeshRenderer>();
            var voxelMaterial = VoxelVisuals.GetVoxelLitMaterial(material);
            ConfigureDungeonLightScales(voxelMaterial);
            renderer.sharedMaterial = voxelMaterial;
            renderer.receiveShadows = true;
            renderer.shadowCastingMode = wall ? ShadowCastingMode.On : ShadowCastingMode.Off;
            return target;
        }

        private static void ConfigureDungeonLightScales(Material material)
        {
            if (material == null)
            {
                return;
            }

            if (material.HasProperty(AmbientScaleId))
            {
                material.SetFloat(AmbientScaleId, 0.08f);
            }

            if (material.HasProperty(MainLightScaleId))
            {
                material.SetFloat(MainLightScaleId, 0.03f);
            }

            if (material.HasProperty(AdditionalLightScaleId))
            {
                material.SetFloat(AdditionalLightScaleId, 1.25f);
            }
        }

        private Mesh BuildMazeVoxelSurfaceMesh(
            string objectName,
            Vector2Int cellPosition,
            bool wall,
            MazeWallSide sides,
            MazeVisualProfile profile)
        {
            var vertices = new List<Vector3>(wall ? 96 : 64);
            var normals = new List<Vector3>(wall ? 96 : 64);
            var colors = new List<Color>(wall ? 96 : 64);
            var uvs = new List<Vector2>(wall ? 96 : 64);
            var triangles = new List<int>(wall ? 144 : 96);
            var half = cellSize * (wall ? VisualWallWidthRatio : VisualFloorWidthRatio) * 0.5f;

            if (wall)
            {
                AppendHorizontalSurface(
                    vertices,
                    normals,
                    colors,
                    uvs,
                    triangles,
                    cellPosition,
                    objectName,
                    -half,
                    half,
                    WallHeight,
                    -half,
                    half,
                    profile.WallTopDivisions,
                    true,
                    profile.WallNoise);

                AppendWallSides(vertices, normals, colors, uvs, triangles, cellPosition, objectName, half, sides, profile);
                AppendWallTopRelief(vertices, normals, colors, uvs, triangles, cellPosition, objectName, half, profile);
                AppendWallSideRelief(vertices, normals, colors, uvs, triangles, cellPosition, objectName, half, sides, profile);
            }
            else
            {
                AppendHorizontalSurface(
                    vertices,
                    normals,
                    colors,
                    uvs,
                    triangles,
                    cellPosition,
                    objectName,
                    -half,
                    half,
                    Scale(-0.006f),
                    -half,
                    half,
                    profile.FloorDivisions,
                    false,
                    profile.FloorNoise);
                AppendFloorRelief(vertices, normals, colors, uvs, triangles, cellPosition, objectName, half, profile);
            }

            var mesh = new Mesh { name = $"{objectName} Mesh" };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetColors(colors);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private void AppendWallSides(
            List<Vector3> vertices,
            List<Vector3> normals,
            List<Color> colors,
            List<Vector2> uvs,
            List<int> triangles,
            Vector2Int cellPosition,
            string objectName,
            float half,
            MazeWallSide sides,
            MazeVisualProfile profile)
        {
            if ((sides & MazeWallSide.North) != 0)
            {
                AppendVerticalSurface(vertices, normals, colors, uvs, triangles, cellPosition, objectName, Vector3.forward, -half, half, half, profile);
            }

            if ((sides & MazeWallSide.East) != 0)
            {
                AppendVerticalSurface(vertices, normals, colors, uvs, triangles, cellPosition, objectName, Vector3.right, -half, half, half, profile);
            }

            if ((sides & MazeWallSide.South) != 0)
            {
                AppendVerticalSurface(vertices, normals, colors, uvs, triangles, cellPosition, objectName, Vector3.back, -half, half, -half, profile);
            }

            if ((sides & MazeWallSide.West) != 0)
            {
                AppendVerticalSurface(vertices, normals, colors, uvs, triangles, cellPosition, objectName, Vector3.left, -half, half, -half, profile);
            }
        }

        private void AppendWallTopRelief(
            List<Vector3> vertices,
            List<Vector3> normals,
            List<Color> colors,
            List<Vector2> uvs,
            List<int> triangles,
            Vector2Int cellPosition,
            string objectName,
            float half,
            MazeVisualProfile profile)
        {
            const int blockCount = 5;
            var margin = cellSize * 0.1f;
            for (var i = 0; i < blockCount; i++)
            {
                var width = cellSize * Mathf.Lerp(0.28f, 0.58f, Random01(cellPosition, i * 19 + 3));
                var depth = cellSize * Mathf.Lerp(0.24f, 0.52f, Random01(cellPosition, i * 23 + 7));
                var x = Mathf.Lerp(-half + margin + width * 0.5f, half - margin - width * 0.5f, Random01(cellPosition, i * 31 + 11));
                var z = Mathf.Lerp(-half + margin + depth * 0.5f, half - margin - depth * 0.5f, Random01(cellPosition, i * 37 + 17));
                var height = cellSize * Mathf.Lerp(0.055f, 0.125f, Random01(cellPosition, i * 41 + 29));
                AppendReliefBox(
                    vertices,
                    normals,
                    colors,
                    uvs,
                    triangles,
                    cellPosition,
                    objectName,
                    new Vector3(x, WallHeight + height * 0.5f, z),
                    new Vector3(width, height, depth),
                    i + 100,
                    true,
                    profile.WallNoise);
            }
        }

        private void AppendFloorRelief(
            List<Vector3> vertices,
            List<Vector3> normals,
            List<Color> colors,
            List<Vector2> uvs,
            List<int> triangles,
            Vector2Int cellPosition,
            string objectName,
            float half,
            MazeVisualProfile profile)
        {
            var hashGate = StableVisualHash(cellPosition, objectName, 5, 17, Vector3.up) % 5;
            var blockCount = hashGate <= 2 ? 2 : 1;
            var baseY = Scale(-0.006f);
            var margin = cellSize * 0.16f;
            for (var i = 0; i < blockCount; i++)
            {
                var width = cellSize * Mathf.Lerp(0.18f, 0.38f, Random01(cellPosition, i * 13 + 43));
                var depth = cellSize * Mathf.Lerp(0.16f, 0.34f, Random01(cellPosition, i * 17 + 47));
                var x = Mathf.Lerp(-half + margin + width * 0.5f, half - margin - width * 0.5f, Random01(cellPosition, i * 29 + 53));
                var z = Mathf.Lerp(-half + margin + depth * 0.5f, half - margin - depth * 0.5f, Random01(cellPosition, i * 31 + 59));
                var height = cellSize * Mathf.Lerp(0.012f, 0.028f, Random01(cellPosition, i * 37 + 61));
                AppendReliefBox(
                    vertices,
                    normals,
                    colors,
                    uvs,
                    triangles,
                    cellPosition,
                    objectName,
                    new Vector3(x, baseY + height * 0.5f, z),
                    new Vector3(width, height, depth),
                    i + 200,
                    false,
                    profile.FloorNoise);
            }
        }

        private void AppendWallSideRelief(
            List<Vector3> vertices,
            List<Vector3> normals,
            List<Color> colors,
            List<Vector2> uvs,
            List<int> triangles,
            Vector2Int cellPosition,
            string objectName,
            float half,
            MazeWallSide sides,
            MazeVisualProfile profile)
        {
            if ((sides & MazeWallSide.North) != 0)
            {
                AppendSideReliefForNormal(vertices, normals, colors, uvs, triangles, cellPosition, objectName, Vector3.forward, half, profile, 300);
            }

            if ((sides & MazeWallSide.East) != 0)
            {
                AppendSideReliefForNormal(vertices, normals, colors, uvs, triangles, cellPosition, objectName, Vector3.right, half, profile, 400);
            }

            if ((sides & MazeWallSide.South) != 0)
            {
                AppendSideReliefForNormal(vertices, normals, colors, uvs, triangles, cellPosition, objectName, Vector3.back, half, profile, 500);
            }

            if ((sides & MazeWallSide.West) != 0)
            {
                AppendSideReliefForNormal(vertices, normals, colors, uvs, triangles, cellPosition, objectName, Vector3.left, half, profile, 600);
            }
        }

        private void AppendSideReliefForNormal(
            List<Vector3> vertices,
            List<Vector3> normals,
            List<Color> colors,
            List<Vector2> uvs,
            List<int> triangles,
            Vector2Int cellPosition,
            string objectName,
            Vector3 normal,
            float half,
            MazeVisualProfile profile,
            int saltBase)
        {
            const int blockCount = 1;
            var depth = cellSize * 0.075f;
            for (var i = 0; i < blockCount; i++)
            {
                var width = cellSize * Mathf.Lerp(0.34f, 0.68f, Random01(cellPosition, saltBase + i * 11));
                var height = WallHeight * Mathf.Lerp(0.18f, 0.36f, Random01(cellPosition, saltBase + i * 13));
                var along = Mathf.Lerp(-half + width * 0.62f, half - width * 0.62f, Random01(cellPosition, saltBase + i * 17));
                var y = Mathf.Lerp(WallHeight * 0.18f + height * 0.5f, WallHeight * 0.78f, Random01(cellPosition, saltBase + i * 19));
                Vector3 center;
                Vector3 size;
                if (normal == Vector3.forward || normal == Vector3.back)
                {
                    center = new Vector3(along, y, normal.z > 0f ? half + depth * 0.5f : -half - depth * 0.5f);
                    size = new Vector3(width, height, depth);
                }
                else
                {
                    center = new Vector3(normal.x > 0f ? half + depth * 0.5f : -half - depth * 0.5f, y, along);
                    size = new Vector3(depth, height, width);
                }

                AppendReliefBox(
                    vertices,
                    normals,
                    colors,
                    uvs,
                    triangles,
                    cellPosition,
                    objectName,
                    center,
                    size,
                    saltBase + i,
                    true,
                    profile.WallNoise);
            }
        }

        private void AppendHorizontalSurface(
            List<Vector3> vertices,
            List<Vector3> normals,
            List<Color> colors,
            List<Vector2> uvs,
            List<int> triangles,
            Vector2Int cellPosition,
            string objectName,
            float minX,
            float maxX,
            float y,
            float minZ,
            float maxZ,
            int divisions,
            bool wall,
            float noise)
        {
            var safeDivisions = Mathf.Max(1, divisions);
            var stepX = (maxX - minX) / safeDivisions;
            var stepZ = (maxZ - minZ) / safeDivisions;
            for (var x = 0; x < safeDivisions; x++)
            {
                for (var z = 0; z < safeDivisions; z++)
                {
                    var x0 = minX + stepX * x;
                    var x1 = x0 + stepX;
                    var z0 = minZ + stepZ * z;
                    var z1 = z0 + stepZ;
                    AppendQuad(
                        vertices,
                        normals,
                        colors,
                        uvs,
                        triangles,
                        Vector3.up,
                        SurfaceColor(cellPosition, objectName, x, z, Vector3.up, wall, noise),
                        new Vector3(x0, y, z1),
                        new Vector3(x0, y, z0),
                        new Vector3(x1, y, z0),
                        new Vector3(x1, y, z1),
                        new Vector2(x0, z1),
                        new Vector2(x0, z0),
                        new Vector2(x1, z0),
                        new Vector2(x1, z1));
                }
            }
        }

        private void AppendVerticalSurface(
            List<Vector3> vertices,
            List<Vector3> normals,
            List<Color> colors,
            List<Vector2> uvs,
            List<int> triangles,
            Vector2Int cellPosition,
            string objectName,
            Vector3 normal,
            float minAxis,
            float maxAxis,
            float fixedAxis,
            MazeVisualProfile profile)
        {
            var columns = Mathf.Max(1, profile.WallSideColumns);
            var rows = Mathf.Max(1, profile.WallSideRows);
            var stepAxis = (maxAxis - minAxis) / columns;
            var stepY = WallHeight / rows;

            for (var column = 0; column < columns; column++)
            {
                for (var row = 0; row < rows; row++)
                {
                    var axis0 = minAxis + stepAxis * column;
                    var axis1 = axis0 + stepAxis;
                    var y0 = stepY * row;
                    var y1 = y0 + stepY;
                    var color = SurfaceColor(cellPosition, objectName, column, row, normal, true, profile.WallNoise);
                    if (normal == Vector3.forward || normal == Vector3.back)
                    {
                        AppendQuad(
                            vertices,
                            normals,
                            colors,
                            uvs,
                            triangles,
                            normal,
                            color,
                            new Vector3(axis0, y0, fixedAxis),
                            new Vector3(axis0, y1, fixedAxis),
                            new Vector3(axis1, y1, fixedAxis),
                            new Vector3(axis1, y0, fixedAxis),
                            new Vector2(axis0, y0),
                            new Vector2(axis0, y1),
                            new Vector2(axis1, y1),
                            new Vector2(axis1, y0));
                    }
                    else
                    {
                        AppendQuad(
                            vertices,
                            normals,
                            colors,
                            uvs,
                            triangles,
                            normal,
                            color,
                            new Vector3(fixedAxis, y0, axis1),
                            new Vector3(fixedAxis, y1, axis1),
                            new Vector3(fixedAxis, y1, axis0),
                            new Vector3(fixedAxis, y0, axis0),
                            new Vector2(axis1, y0),
                            new Vector2(axis1, y1),
                            new Vector2(axis0, y1),
                            new Vector2(axis0, y0));
                    }
                }
            }
        }

        private static void AppendQuad(
            List<Vector3> vertices,
            List<Vector3> normals,
            List<Color> colors,
            List<Vector2> uvs,
            List<int> triangles,
            Vector3 normal,
            Color color,
            Vector3 a,
            Vector3 b,
            Vector3 c,
            Vector3 d,
            Vector2 uvA,
            Vector2 uvB,
            Vector2 uvC,
            Vector2 uvD)
        {
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), normal) < 0f)
            {
                AppendQuadUnchecked(vertices, normals, colors, uvs, triangles, normal, color, a, d, c, b, uvA, uvD, uvC, uvB);
                return;
            }

            AppendQuadUnchecked(vertices, normals, colors, uvs, triangles, normal, color, a, b, c, d, uvA, uvB, uvC, uvD);
        }

        private static void AppendReliefBox(
            List<Vector3> vertices,
            List<Vector3> normals,
            List<Color> colors,
            List<Vector2> uvs,
            List<int> triangles,
            Vector2Int cellPosition,
            string objectName,
            Vector3 center,
            Vector3 size,
            int salt,
            bool wall,
            float noise)
        {
            var half = size * 0.5f;
            var min = center - half;
            var max = center + half;

            AppendQuad(
                vertices,
                normals,
                colors,
                uvs,
                triangles,
                Vector3.up,
                SurfaceColor(cellPosition, objectName, salt, 0, Vector3.up, wall, noise),
                new Vector3(min.x, max.y, max.z),
                new Vector3(min.x, max.y, min.z),
                new Vector3(max.x, max.y, min.z),
                new Vector3(max.x, max.y, max.z),
                new Vector2(min.x, max.z),
                new Vector2(min.x, min.z),
                new Vector2(max.x, min.z),
                new Vector2(max.x, max.z));

            AppendQuad(
                vertices,
                normals,
                colors,
                uvs,
                triangles,
                Vector3.forward,
                SurfaceColor(cellPosition, objectName, salt, 1, Vector3.forward, wall, noise),
                new Vector3(min.x, min.y, max.z),
                new Vector3(min.x, max.y, max.z),
                new Vector3(max.x, max.y, max.z),
                new Vector3(max.x, min.y, max.z),
                new Vector2(min.x, min.y),
                new Vector2(min.x, max.y),
                new Vector2(max.x, max.y),
                new Vector2(max.x, min.y));

            AppendQuad(
                vertices,
                normals,
                colors,
                uvs,
                triangles,
                Vector3.back,
                SurfaceColor(cellPosition, objectName, salt, 2, Vector3.back, wall, noise),
                new Vector3(max.x, min.y, min.z),
                new Vector3(max.x, max.y, min.z),
                new Vector3(min.x, max.y, min.z),
                new Vector3(min.x, min.y, min.z),
                new Vector2(max.x, min.y),
                new Vector2(max.x, max.y),
                new Vector2(min.x, max.y),
                new Vector2(min.x, min.y));

            AppendQuad(
                vertices,
                normals,
                colors,
                uvs,
                triangles,
                Vector3.right,
                SurfaceColor(cellPosition, objectName, salt, 3, Vector3.right, wall, noise),
                new Vector3(max.x, min.y, max.z),
                new Vector3(max.x, max.y, max.z),
                new Vector3(max.x, max.y, min.z),
                new Vector3(max.x, min.y, min.z),
                new Vector2(max.z, min.y),
                new Vector2(max.z, max.y),
                new Vector2(min.z, max.y),
                new Vector2(min.z, min.y));

            AppendQuad(
                vertices,
                normals,
                colors,
                uvs,
                triangles,
                Vector3.left,
                SurfaceColor(cellPosition, objectName, salt, 4, Vector3.left, wall, noise),
                new Vector3(min.x, min.y, min.z),
                new Vector3(min.x, max.y, min.z),
                new Vector3(min.x, max.y, max.z),
                new Vector3(min.x, min.y, max.z),
                new Vector2(min.z, min.y),
                new Vector2(min.z, max.y),
                new Vector2(max.z, max.y),
                new Vector2(max.z, min.y));
        }

        private static void AppendQuadUnchecked(
            List<Vector3> vertices,
            List<Vector3> normals,
            List<Color> colors,
            List<Vector2> uvs,
            List<int> triangles,
            Vector3 normal,
            Color color,
            Vector3 a,
            Vector3 b,
            Vector3 c,
            Vector3 d,
            Vector2 uvA,
            Vector2 uvB,
            Vector2 uvC,
            Vector2 uvD)
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
            uvs.Add(uvA);
            uvs.Add(uvB);
            uvs.Add(uvC);
            uvs.Add(uvD);
            triangles.Add(index);
            triangles.Add(index + 1);
            triangles.Add(index + 2);
            triangles.Add(index);
            triangles.Add(index + 2);
            triangles.Add(index + 3);
        }

        private MazeWallSide GetExposedWallSides(MazeGrid grid, Vector2Int position)
        {
            if (grid == null)
            {
                return MazeWallSide.North | MazeWallSide.East | MazeWallSide.South | MazeWallSide.West;
            }

            var sides = MazeWallSide.None;
            if (ShouldRenderWallSide(grid, position + Vector2Int.up))
            {
                sides |= MazeWallSide.North;
            }

            if (ShouldRenderWallSide(grid, position + Vector2Int.right))
            {
                sides |= MazeWallSide.East;
            }

            if (ShouldRenderWallSide(grid, position + Vector2Int.down))
            {
                sides |= MazeWallSide.South;
            }

            if (ShouldRenderWallSide(grid, position + Vector2Int.left))
            {
                sides |= MazeWallSide.West;
            }

            return sides;
        }

        private static bool ShouldRenderWallSide(MazeGrid grid, Vector2Int neighbor)
        {
            return !grid.InBounds(neighbor) || grid.Get(neighbor).Type != MazeCellType.Wall;
        }

        private static Color SurfaceColor(
            Vector2Int cellPosition,
            string objectName,
            int a,
            int b,
            Vector3 normal,
            bool wall,
            float noiseScale)
        {
            var light = normal == Vector3.up ? (wall ? 1.14f : 1.02f) : 0.78f;
            if (normal == Vector3.right)
            {
                light = wall ? 0.86f : 0.94f;
            }
            else if (normal == Vector3.left)
            {
                light = wall ? 0.62f : 0.86f;
            }
            else if (normal == Vector3.forward)
            {
                light = wall ? 0.74f : 0.9f;
            }
            else if (normal == Vector3.back)
            {
                light = wall ? 0.66f : 0.86f;
            }

            var hash = StableVisualHash(cellPosition, objectName, a, b, normal);
            var noise = ((hash & 1023) / 1023f - 0.5f) * noiseScale;
            var value = Mathf.Clamp(light + noise, wall ? 0.56f : 0.82f, wall ? 1.22f : 1.08f);
            var tint = wall ? new Color(1.08f, 1.02f, 0.91f, 1f) : new Color(1.08f, 1.01f, 0.88f, 1f);
            return new Color(tint.r * value, tint.g * value, tint.b * value, 1f);
        }

        private static int StableVisualHash(Vector2Int cellPosition, string objectName, int a, int b, Vector3 normal)
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 31 + cellPosition.x;
                hash = hash * 31 + cellPosition.y;
                hash = hash * 31 + a;
                hash = hash * 31 + b;
                hash = hash * 31 + Mathf.RoundToInt(normal.x * 11f);
                hash = hash * 31 + Mathf.RoundToInt(normal.y * 13f);
                hash = hash * 31 + Mathf.RoundToInt(normal.z * 17f);
                for (var i = 0; i < objectName.Length; i++)
                {
                    hash = hash * 31 + objectName[i];
                }

                hash ^= hash >> 13;
                hash *= 1274126177;
                hash ^= hash >> 16;
                return hash & 0x7fffffff;
            }
        }

        private static float Random01(Vector2Int cellPosition, int salt)
        {
            unchecked
            {
                var hash = 23;
                hash = hash * 31 + cellPosition.x * 73856093;
                hash = hash * 31 + cellPosition.y * 19349663;
                hash = hash * 31 + salt * 83492791;
                hash ^= hash >> 13;
                hash *= 1274126177;
                hash ^= hash >> 16;
                return (hash & 0xffff) / 65535f;
            }
        }
    }
}
