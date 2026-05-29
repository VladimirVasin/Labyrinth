using Labyrinth.Base;
using Labyrinth.Core;
using UnityEngine;

namespace Labyrinth.Maze
{
    public static class PeasantHutRenderer
    {
        public static BuildingView Render(MazeRenderer renderer, Vector2Int position)
        {
            if (renderer == null || renderer.ContentRoot == null)
            {
                return null;
            }

            var unit = renderer.ModelUnitSize * 1.15f;
            var center = renderer.GridToWorld(position);
            var root = new GameObject($"Peasant Hut {position.x},{position.y}");
            root.transform.SetParent(renderer.ContentRoot, false);
            root.transform.position = center;

            var building = root.AddComponent<BuildingView>();
            building.Configure(
                BuildingType.PeasantHut,
                "Лачуга крестьянина",
                "налоговый двор",
                "Налоги: 0 / 10 зол.",
                position,
                BaseDevelopment.PeasantHutFootprintRadiusCells);

            var wall = CreateMaterial("Peasant Hut Clay", new Color(0.48f, 0.35f, 0.22f));
            var roof = CreateMaterial("Peasant Hut Straw", new Color(0.72f, 0.56f, 0.25f));
            var wood = CreateMaterial("Peasant Hut Wood", new Color(0.28f, 0.16f, 0.07f));
            var cloth = CreateMaterial("Peasant Hut Cloth", new Color(0.68f, 0.2f, 0.13f));
            var coin = CreateMaterial("Peasant Hut Coin", new Color(0.95f, 0.68f, 0.16f));

            CreateCube("Peasant Hut Walls", root.transform, center + new Vector3(0f, unit * 0.32f, 0f), new Vector3(unit * 0.72f, unit * 0.64f, unit * 0.62f), wall, true);
            CreateCube("Peasant Hut Roof", root.transform, center + new Vector3(0f, unit * 0.76f, 0f), new Vector3(unit * 0.92f, unit * 0.24f, unit * 0.82f), roof, true);
            CreateCube("Peasant Hut Door", root.transform, center + new Vector3(unit * 0.38f, unit * 0.24f, 0f), new Vector3(unit * 0.06f, unit * 0.42f, unit * 0.22f), wood, false);
            CreateCube("Peasant Hut Yard", root.transform, center + new Vector3(0f, unit * -0.03f, 0f), new Vector3(unit * 1.05f, unit * 0.05f, unit * 0.95f), wood, false);
            CreateCube("Peasant Hut Cloth Line", root.transform, center + new Vector3(unit * -0.22f, unit * 0.54f, unit * -0.44f), new Vector3(unit * 0.48f, unit * 0.06f, unit * 0.06f), cloth, false);
            CreateCube("Peasant Hut Tax Chest", root.transform, center + new Vector3(unit * -0.36f, unit * 0.16f, unit * 0.32f), new Vector3(unit * 0.28f, unit * 0.18f, unit * 0.22f), wood, false);
            CreateCoin(root.transform, center + new Vector3(unit * -0.36f, unit * 0.3f, unit * 0.32f), unit, coin);
            return building;
        }

        private static void CreateCoin(Transform parent, Vector3 position, float unit, Material material)
        {
            var coin = GameObject.CreatePrimitive(VoxelVisuals.ResolvePrimitive(PrimitiveType.Cylinder, "Peasant Hut Coin"));
            coin.name = "Peasant Hut Coin";
            coin.transform.SetParent(parent, false);
            coin.transform.position = position;
            coin.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            coin.transform.localScale = new Vector3(unit * 0.08f, unit * 0.02f, unit * 0.08f);
            coin.GetComponent<Renderer>().sharedMaterial = material;
            RemoveCollider(coin);
            VoxelVisuals.ApplyBlockStyle(coin, PrimitiveType.Cylinder, material, false);
        }

        private static void CreateCube(string name, Transform parent, Vector3 position, Vector3 scale, Material material, bool keepCollider)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.position = position;
            cube.transform.localScale = scale;
            cube.GetComponent<Renderer>().sharedMaterial = material;
            if (!keepCollider)
            {
                RemoveCollider(cube);
            }

            VoxelVisuals.ApplyBlockStyle(cube, PrimitiveType.Cube, material, keepCollider);
        }

        private static Material CreateMaterial(string name, Color color)
        {
            return VoxelVisuals.CreateLitMaterial(name, color);
        }

        private static void RemoveCollider(GameObject target)
        {
            var collider = target.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }
        }
    }
}
