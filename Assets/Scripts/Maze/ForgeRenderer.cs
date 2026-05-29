using Labyrinth.Base;
using Labyrinth.Core;
using UnityEngine;

namespace Labyrinth.Maze
{
    public static class ForgeRenderer
    {
        public static BuildingView Render(MazeRenderer renderer, Vector2Int position)
        {
            if (renderer == null || renderer.ContentRoot == null)
            {
                return null;
            }

            var unit = renderer.ModelUnitSize * 2f;
            var center = renderer.GridToWorld(position);
            var root = new GameObject($"Forge {position.x},{position.y}");
            root.transform.SetParent(renderer.ContentRoot, false);
            root.transform.position = center;

            var building = root.AddComponent<BuildingView>();
            building.Configure(
                BuildingType.Forge,
                "Кузница",
                "оружие, броня и обувь",
                $"Меч {BaseDevelopment.SteelSwordGoldCost}; броня {BaseDevelopment.ChainmailGoldCost}; сапоги {BaseDevelopment.LeatherBootsGoldCost} зол.",
                position,
                BaseDevelopment.ForgeFootprintRadiusCells);

            var wall = CreateMaterial("Forge Wall", new Color(0.38f, 0.34f, 0.3f));
            var roof = CreateMaterial("Forge Roof", new Color(0.16f, 0.13f, 0.12f));
            var metal = CreateMaterial("Forge Metal", new Color(0.58f, 0.6f, 0.62f));
            var fire = VoxelVisuals.CreateEmissiveMaterial("Forge Fire", new Color(1f, 0.28f, 0.06f), 2.2f);
            var wood = CreateMaterial("Forge Wood", new Color(0.28f, 0.15f, 0.07f));
            var coal = CreateMaterial("Forge Coal", new Color(0.04f, 0.04f, 0.04f));
            var smoke = CreateMaterial("Forge Smoke", new Color(0.42f, 0.42f, 0.42f, 0.8f));

            CreateCube("Forge Walls", root.transform, center + new Vector3(0f, unit * 0.5f, 0f), new Vector3(unit * 1.08f, unit, unit * 0.88f), wall, true);
            CreateCube("Forge Roof", root.transform, center + new Vector3(0f, unit * 1.12f, 0f), new Vector3(unit * 1.28f, unit * 0.32f, unit * 1.06f), roof, true);
            CreateCube("Forge Door", root.transform, center + new Vector3(unit * 0.56f, unit * 0.32f, 0f), new Vector3(unit * 0.08f, unit * 0.64f, unit * 0.3f), wood, false);
            CreateCube("Forge Chimney", root.transform, center + new Vector3(unit * -0.32f, unit * 1.45f, unit * 0.22f), new Vector3(unit * 0.22f, unit * 0.72f, unit * 0.22f), wall, false);
            CreateCube("Forge Anvil", root.transform, center + new Vector3(unit * 0.18f, unit * 0.22f, unit * -0.24f), new Vector3(unit * 0.44f, unit * 0.2f, unit * 0.26f), metal, false);
            CreateCube("Forge Firebox", root.transform, center + new Vector3(unit * -0.34f, unit * 0.32f, unit * -0.2f), new Vector3(unit * 0.34f, unit * 0.22f, unit * 0.28f), fire, false);
            CreateCube("Forge Sword Sign", root.transform, center + new Vector3(unit * 0.6f, unit * 0.88f, unit * -0.3f), new Vector3(unit * 0.08f, unit * 0.5f, unit * 0.08f), metal, false);
            CreateCube("Forge Guard Sign", root.transform, center + new Vector3(unit * 0.6f, unit * 0.65f, unit * -0.3f), new Vector3(unit * 0.08f, unit * 0.08f, unit * 0.34f), metal, false);
            CreateCube("Forge Hammer Handle", root.transform, center + new Vector3(unit * 0.42f, unit * 0.52f, unit * 0.32f), new Vector3(unit * 0.08f, unit * 0.52f, unit * 0.08f), wood, false);
            CreateCube("Forge Hammer Head", root.transform, center + new Vector3(unit * 0.42f, unit * 0.78f, unit * 0.32f), new Vector3(unit * 0.38f, unit * 0.14f, unit * 0.16f), metal, false);
            CreateCube("Forge Coal Pile", root.transform, center + new Vector3(unit * -0.18f, unit * 0.1f, unit * 0.34f), new Vector3(unit * 0.38f, unit * 0.16f, unit * 0.28f), coal, false);
            CreateCube("Forge Water Trough", root.transform, center + new Vector3(unit * 0.36f, unit * 0.14f, unit * -0.34f), new Vector3(unit * 0.42f, unit * 0.18f, unit * 0.24f), metal, false);
            CreateSphere("Forge Smoke Puff", root.transform, center + new Vector3(unit * -0.32f, unit * 1.92f, unit * 0.22f), Vector3.one * unit * 0.24f, smoke);
            CreateSphere("Forge Smoke Puff Small", root.transform, center + new Vector3(unit * -0.18f, unit * 2.16f, unit * 0.18f), Vector3.one * unit * 0.18f, smoke);
            return building;
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

        private static void CreateSphere(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var sphere = GameObject.CreatePrimitive(VoxelVisuals.ResolvePrimitive(PrimitiveType.Sphere, name));
            sphere.name = name;
            sphere.transform.SetParent(parent, false);
            sphere.transform.position = position;
            sphere.transform.localScale = scale;
            sphere.GetComponent<Renderer>().sharedMaterial = material;
            RemoveCollider(sphere);
            VoxelVisuals.ApplyBlockStyle(sphere, PrimitiveType.Sphere, material, false);
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
