using UnityEngine;
using UnityEngine.Rendering;

namespace Labyrinth.Maze
{
    public sealed partial class MazeRenderer
    {
        private void CreateDungeonSeamUnderlay(MazeGrid grid)
        {
            if (grid == null)
            {
                return;
            }

            var y = Scale(-0.018f);
            var margin = cellSize * 0.08f;
            var half = cellSize * 0.5f + margin;
            var first = GridToWorld(Vector2Int.zero);
            var last = GridToWorld(new Vector2Int(grid.Width - 1, grid.Height - 1));
            var minX = first.x - half;
            var minZ = first.z - half;
            var maxX = last.x + half;
            var maxZ = last.z + half;

            var mesh = new Mesh { name = "Dungeon Seam Underlay Mesh" };
            mesh.SetVertices(new[]
            {
                new Vector3(minX, y, minZ),
                new Vector3(minX, y, maxZ),
                new Vector3(maxX, y, maxZ),
                new Vector3(maxX, y, minZ)
            });
            mesh.SetTriangles(new[] { 0, 1, 2, 0, 2, 3 }, 0);
            mesh.RecalculateBounds();

            dungeonSeamUnderlay = new GameObject("Dungeon Seam Underlay");
            dungeonSeamUnderlay.transform.SetParent(root, false);
            dungeonSeamUnderlay.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = dungeonSeamUnderlay.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = dungeonSeamMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }
}
