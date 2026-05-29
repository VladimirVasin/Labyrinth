using Labyrinth.Core;
using UnityEngine;

namespace Labyrinth.Maze
{
    public sealed partial class MazeRenderer
    {
        private Material wallMortarMaterial;
        private Material pathMortarMaterial;
        private Material[] wallVoxelMaterials;
        private Material[] pathVoxelMaterials;
        private Material[] entranceVoxelMaterials;

        partial void EnsureVoxelMaterials()
        {
            wallMortarMaterial = wallMortarMaterial != null ? wallMortarMaterial : CreateMaterial("Voxel Wall Mortar", new Color(0.115f, 0.125f, 0.145f));
            pathMortarMaterial = pathMortarMaterial != null ? pathMortarMaterial : CreateMaterial("Voxel Floor Mortar", new Color(0.5f, 0.48f, 0.38f));
            wallVoxelMaterials = wallVoxelMaterials ?? new[] { wallMaterial, wallMortarMaterial };
            pathVoxelMaterials = pathVoxelMaterials ?? new[] { pathMaterial, pathMortarMaterial };
            entranceVoxelMaterials = entranceVoxelMaterials ?? new[] { entranceMaterial, pathMortarMaterial };
        }

        private bool RenderVoxelCell(MazeCell cell)
        {
            if (!VoxelVisuals.Enabled)
            {
                return false;
            }

            var cellPosition = new Vector2Int(cell.X, cell.Y);
            var position = GridToWorld(cellPosition);
            if (cell.Type == MazeCellType.Wall)
            {
                var currentWallHeight = WallHeight;
                var wall = VoxelVisuals.CreateVoxelBlockGrid(
                    "Wall Voxels",
                    root,
                    position + new Vector3(0f, currentWallHeight * 0.5f, 0f),
                    new Vector3(cellSize * VisualWallWidthRatio, currentWallHeight, cellSize * VisualWallWidthRatio),
                    wallVoxelMaterials,
                    (x, y, z) => SelectWallVoxelMaterial(cellPosition, x, y, z),
                    6,
                    7,
                    6,
                    0.045f,
                    false,
                    null,
                    IncludeWallVoxelBlock);
                var wallRenderer = wall.GetComponent<Renderer>();
                VoxelVisuals.ApplyStaticMazeLightingProfile(wallRenderer);
                TrackCellRenderer(cellPosition, wallRenderer);
                return true;
            }

            var isEntrance = cell.Type == MazeCellType.Entrance;
            var materials = isEntrance ? entranceVoxelMaterials : pathVoxelMaterials;
            var floor = VoxelVisuals.CreateVoxelBlockGrid(
                $"{cell.Type} Voxels",
                root,
                position + new Vector3(0f, Scale(-0.03f), 0f),
                new Vector3(cellSize * VisualFloorWidthRatio, Scale(0.05f), cellSize * VisualFloorWidthRatio),
                materials,
                (x, y, z) => SelectFloorVoxelMaterial(cellPosition, x, z, isEntrance),
                7,
                1,
                7,
                0.018f,
                false);
            var floorRenderer = floor.GetComponent<Renderer>();
            VoxelVisuals.ApplyStaticMazeLightingProfile(floorRenderer);
            TrackCellRenderer(cellPosition, floorRenderer);
            return true;
        }

        private static int SelectWallVoxelMaterial(Vector2Int cellPosition, int x, int y, int z)
        {
            var hash = HashVoxel(cellPosition, x, y, z);
            var verticalJoint = ((x + cellPosition.x * 2) % 5 == 0 || (z + cellPosition.y * 2) % 5 == 0) && hash % 3 != 0;
            var horizontalJoint = y > 0 && y % 4 == 0 && hash % 3 == 0;
            if (verticalJoint || horizontalJoint)
            {
                return 1;
            }

            return 0;
        }

        private static bool IncludeWallVoxelBlock(int x, int y, int z)
        {
            var isCorner = (x == 0 || x == 5) && (z == 0 || z == 5);
            if (isCorner && y >= 4)
            {
                return false;
            }

            var isOuterEdge = x == 0 || x == 5 || z == 0 || z == 5;
            return !isOuterEdge || y < 6 || ((x + z) & 1) != 0;
        }

        private static int SelectFloorVoxelMaterial(Vector2Int cellPosition, int x, int z, bool isEntrance)
        {
            var hash = HashVoxel(cellPosition, x, 0, z);
            if (isEntrance)
            {
                return hash % 5 == 0 ? 1 : 0;
            }

            var edgeJoint = x == 0 || z == 0 || x == 6 || z == 6;
            if ((edgeJoint && hash % 3 == 0) || hash % 29 == 0)
            {
                return 1;
            }

            return 0;
        }

        private static int HashVoxel(Vector2Int cellPosition, int x, int y, int z)
        {
            unchecked
            {
                var hash = cellPosition.x * 73856093
                    ^ cellPosition.y * 19349663
                    ^ x * 83492791
                    ^ y * 265443576
                    ^ z * 374761393;
                hash ^= hash >> 13;
                hash *= 1274126177;
                hash ^= hash >> 16;
                return hash & 0x7fffffff;
            }
        }
    }
}
