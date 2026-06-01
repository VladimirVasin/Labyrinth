using System.Collections.Generic;
using UnityEngine;

namespace Labyrinth.Core
{
    public static partial class VoxelVisuals
    {
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
            int blocksZ,
            bool[,,] includedBlocks)
        {
            var half = size * 0.5f;
            var min = center - half;
            var max = center + half;

            if (!HasIncludedBlock(includedBlocks, blockX, blockY, blockZ + 1))
            {
                AppendFace(vertices, normals, colors, uvs, triangles, Vector3.forward, CalculateFaceColor(objectName, Vector3.forward, blockX, blockY, blockZ, blocksX, blocksY, blocksZ),
                    new Vector3(min.x, min.y, max.z),
                    new Vector3(min.x, max.y, max.z),
                    new Vector3(max.x, max.y, max.z),
                    new Vector3(max.x, min.y, max.z));
            }

            if (!HasIncludedBlock(includedBlocks, blockX, blockY, blockZ - 1))
            {
                AppendFace(vertices, normals, colors, uvs, triangles, Vector3.back, CalculateFaceColor(objectName, Vector3.back, blockX, blockY, blockZ, blocksX, blocksY, blocksZ),
                    new Vector3(max.x, min.y, min.z),
                    new Vector3(max.x, max.y, min.z),
                    new Vector3(min.x, max.y, min.z),
                    new Vector3(min.x, min.y, min.z));
            }

            if (!HasIncludedBlock(includedBlocks, blockX + 1, blockY, blockZ))
            {
                AppendFace(vertices, normals, colors, uvs, triangles, Vector3.right, CalculateFaceColor(objectName, Vector3.right, blockX, blockY, blockZ, blocksX, blocksY, blocksZ),
                    new Vector3(max.x, min.y, max.z),
                    new Vector3(max.x, max.y, max.z),
                    new Vector3(max.x, max.y, min.z),
                    new Vector3(max.x, min.y, min.z));
            }

            if (!HasIncludedBlock(includedBlocks, blockX - 1, blockY, blockZ))
            {
                AppendFace(vertices, normals, colors, uvs, triangles, Vector3.left, CalculateFaceColor(objectName, Vector3.left, blockX, blockY, blockZ, blocksX, blocksY, blocksZ),
                    new Vector3(min.x, min.y, min.z),
                    new Vector3(min.x, max.y, min.z),
                    new Vector3(min.x, max.y, max.z),
                    new Vector3(min.x, min.y, max.z));
            }

            if (!HasIncludedBlock(includedBlocks, blockX, blockY + 1, blockZ))
            {
                AppendFace(vertices, normals, colors, uvs, triangles, Vector3.up, CalculateFaceColor(objectName, Vector3.up, blockX, blockY, blockZ, blocksX, blocksY, blocksZ),
                    new Vector3(min.x, max.y, max.z),
                    new Vector3(min.x, max.y, min.z),
                    new Vector3(max.x, max.y, min.z),
                    new Vector3(max.x, max.y, max.z));
            }

            if (!HasIncludedBlock(includedBlocks, blockX, blockY - 1, blockZ))
            {
                AppendFace(vertices, normals, colors, uvs, triangles, Vector3.down, CalculateFaceColor(objectName, Vector3.down, blockX, blockY, blockZ, blocksX, blocksY, blocksZ),
                    new Vector3(min.x, min.y, min.z),
                    new Vector3(min.x, min.y, max.z),
                    new Vector3(max.x, min.y, max.z),
                    new Vector3(max.x, min.y, min.z));
            }
        }

        private static bool HasIncludedBlock(bool[,,] includedBlocks, int x, int y, int z)
        {
            return includedBlocks != null
                && x >= 0
                && y >= 0
                && z >= 0
                && x < includedBlocks.GetLength(0)
                && y < includedBlocks.GetLength(1)
                && z < includedBlocks.GetLength(2)
                && includedBlocks[x, y, z];
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
            var noiseScale = Contains(objectName, "Voxels") ? 0.012f : 0.045f;
            var noise = ((Hash(objectName, blockX, blockY, blockZ) % 1000) / 999f - 0.5f) * noiseScale;
            var pattern = CalculateSemanticPatternLight(objectName, normal, blockX, blockY, blockZ);
            var light = Mathf.Clamp(faceLight * heightLight * contactShadow * edgeLight * pattern + noise, 0.68f, 1.3f);
            return new Color(tint.r * light, tint.g * light, tint.b * light, 1f);
        }
    }
}
