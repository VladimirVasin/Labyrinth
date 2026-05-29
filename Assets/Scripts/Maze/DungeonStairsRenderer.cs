using Labyrinth.Core;
using UnityEngine;

namespace Labyrinth.Maze
{
    public static class DungeonStairsRenderer
    {
        public static void Render(MazeRenderer renderer, MazeGenerationResult result)
        {
            if (renderer == null || renderer.ContentRoot == null || result == null)
            {
                return;
            }

            RenderStairs(renderer, result.DownStairs);
            RenderStairs(renderer, result.UpStairs);
        }

        private static void RenderStairs(MazeRenderer renderer, DungeonStairsModel stairs)
        {
            if (stairs == null)
            {
                return;
            }

            var center = renderer.GridToWorld(stairs.Position);
            var cellSize = renderer.CellSize;
            var root = new GameObject(stairs.DisplayName);
            root.transform.SetParent(renderer.ContentRoot, false);
            root.transform.position = center;

            var closedRoot = new GameObject("Closed Stairs Visual");
            closedRoot.transform.SetParent(root.transform, false);
            var openRoot = new GameObject("Open Stairs Visual");
            openRoot.transform.SetParent(root.transform, false);

            var stone = CreateMaterial("Dungeon Stairs Stone", new Color(0.22f, 0.23f, 0.25f));
            var dark = CreateMaterial("Dungeon Stairs Dark", new Color(0.025f, 0.022f, 0.02f));
            var metal = CreateMaterial("Dungeon Stairs Lock", new Color(0.78f, 0.62f, 0.22f));
            var light = VoxelVisuals.CreateEmissiveMaterial("Dungeon Stairs Light", new Color(0.15f, 0.72f, 0.82f), 1.9f);

            CreateCube(
                "Closed Hatch",
                closedRoot.transform,
                center + new Vector3(0f, cellSize * 0.07f, 0f),
                new Vector3(cellSize * 0.72f, cellSize * 0.12f, cellSize * 0.72f),
                stone);
            CreateCube(
                "Closed Lock",
                closedRoot.transform,
                center + new Vector3(0f, cellSize * 0.17f, 0f),
                new Vector3(cellSize * 0.2f, cellSize * 0.08f, cellSize * 0.2f),
                metal);

            CreateCube(
                "Stairs Hole",
                openRoot.transform,
                center + new Vector3(0f, cellSize * 0.045f, 0f),
                new Vector3(cellSize * 0.78f, cellSize * 0.08f, cellSize * 0.78f),
                dark);
            for (var i = 0; i < 4; i++)
            {
                CreateCube(
                    "Stair Step",
                    openRoot.transform,
                    center + new Vector3(cellSize * -0.2f + i * cellSize * 0.13f, cellSize * (0.1f + i * 0.025f), cellSize * (0.2f - i * 0.12f)),
                    new Vector3(cellSize * 0.48f, cellSize * 0.05f, cellSize * 0.09f),
                    stone);
            }

            if (stairs.Direction == DungeonStairsDirection.Up)
            {
                CreateCube(
                    "Up Glow",
                    openRoot.transform,
                    center + new Vector3(0f, cellSize * 0.2f, 0f),
                    new Vector3(cellSize * 0.28f, cellSize * 0.08f, cellSize * 0.28f),
                    light);
            }

            renderer.TrackExternalCellRenderer(stairs.Position, root);
            stairs.AttachVisual(closedRoot, openRoot);
        }

        private static void CreateCube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.position = position;
            cube.transform.localScale = scale;
            cube.GetComponent<Renderer>().sharedMaterial = material;
            var collider = cube.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }

            VoxelVisuals.ApplyBlockStyle(cube, PrimitiveType.Cube, material, false);
        }

        private static Material CreateMaterial(string name, Color color)
        {
            return VoxelVisuals.CreateLitMaterial(name, color);
        }
    }
}
