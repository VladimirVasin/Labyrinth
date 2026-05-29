using Labyrinth.Base;
using Labyrinth.Core;
using UnityEngine;

namespace Labyrinth.Maze
{
    public static class BuildingUpgradeVisuals
    {
        private const string RootName = "Building Upgrade Details";

        public static void Apply(BuildingView building, int level, float unit)
        {
            if (building == null)
            {
                return;
            }

            var existing = building.transform.Find(RootName);
            if (existing != null)
            {
                Object.Destroy(existing.gameObject);
            }

            if (level <= 1)
            {
                return;
            }

            var root = new GameObject(RootName).transform;
            root.SetParent(building.transform, false);
            var center = building.transform.position;
            AddLevelTwo(building.Type, root, center, unit);
            if (level >= 3)
            {
                AddLevelThree(building.Type, root, center, unit);
            }
        }

        private static void AddLevelTwo(BuildingType type, Transform root, Vector3 center, float unit)
        {
            var metal = CreateMaterial("Upgrade Iron", new Color(0.62f, 0.66f, 0.7f));
            var cloth = CreateMaterial("Upgrade Flag Blue", new Color(0.15f, 0.28f, 0.78f));
            var wood = CreateMaterial("Upgrade Wood", new Color(0.45f, 0.26f, 0.1f));
            var glow = VoxelVisuals.CreateEmissiveMaterial("Upgrade Glow", new Color(0.12f, 0.9f, 0.72f), 1.8f);

            switch (type)
            {
                case BuildingType.Castle:
                    CreateCube("Iron Gate Bar A", root, center + new Vector3(unit * 1.53f, unit * 0.5f, unit * -0.18f), new Vector3(unit * 0.05f, unit * 0.78f, unit * 0.06f), metal);
                    CreateCube("Iron Gate Bar B", root, center + new Vector3(unit * 1.53f, unit * 0.5f, unit * 0.18f), new Vector3(unit * 0.05f, unit * 0.78f, unit * 0.06f), metal);
                    CreateCube("Iron Gate Cross", root, center + new Vector3(unit * 1.54f, unit * 0.72f, 0f), new Vector3(unit * 0.06f, unit * 0.08f, unit * 0.7f), metal);
                    AddTowerBands(root, center, unit, metal);
                    break;
                case BuildingType.Farm:
                    CreateCube("Iron Hoe Handle", root, center + new Vector3(unit * 0.6f, unit * 0.28f, unit * -0.42f), new Vector3(unit * 0.05f, unit * 0.58f, unit * 0.05f), wood);
                    CreateCube("Iron Hoe Head", root, center + new Vector3(unit * 0.6f, unit * 0.58f, unit * -0.42f), new Vector3(unit * 0.24f, unit * 0.06f, unit * 0.08f), metal);
                    CreateCube("Second Crop Bed", root, center + new Vector3(unit * 0.12f, unit * 0.06f, unit * 0.58f), new Vector3(unit * 0.72f, unit * 0.08f, unit * 0.11f), CreateMaterial("Upgrade Crop", new Color(0.22f, 0.7f, 0.18f)));
                    break;
                case BuildingType.LumberjackCamp:
                    CreateCube("Iron Axe Rack", root, center + new Vector3(unit * -0.58f, unit * 0.36f, unit * 0.46f), new Vector3(unit * 0.08f, unit * 0.58f, unit * 0.08f), wood);
                    CreateCube("Iron Axe Head", root, center + new Vector3(unit * -0.58f, unit * 0.65f, unit * 0.46f), new Vector3(unit * 0.3f, unit * 0.1f, unit * 0.08f), metal);
                    CreateCube("Saw Blade Upgrade", root, center + new Vector3(unit * 0.48f, unit * 0.36f, unit * -0.48f), new Vector3(unit * 0.58f, unit * 0.06f, unit * 0.08f), metal);
                    break;
                case BuildingType.AlchemistShop:
                    CreateCube("Iron Cauldron Upgrade", root, center + new Vector3(unit * -0.48f, unit * 0.42f, unit * 0.42f), new Vector3(unit * 0.42f, unit * 0.24f, unit * 0.42f), metal);
                    CreateCube("Potion Glow Upgrade", root, center + new Vector3(unit * -0.48f, unit * 0.58f, unit * 0.42f), new Vector3(unit * 0.3f, unit * 0.08f, unit * 0.3f), glow);
                    break;
                case BuildingType.Tavern:
                    CreateCube("Tavern Oven", root, center + new Vector3(unit * -0.56f, unit * 0.34f, unit * -0.42f), new Vector3(unit * 0.36f, unit * 0.42f, unit * 0.34f), wood);
                    CreateCube("Tavern Oven Iron", root, center + new Vector3(unit * -0.36f, unit * 0.34f, unit * -0.42f), new Vector3(unit * 0.06f, unit * 0.26f, unit * 0.26f), metal);
                    break;
                case BuildingType.Forge:
                    CreateCube("Large Anvil Upgrade", root, center + new Vector3(unit * 0.28f, unit * 0.28f, unit * -0.36f), new Vector3(unit * 0.62f, unit * 0.24f, unit * 0.34f), metal);
                    CreateCube("Weapon Rack Upgrade", root, center + new Vector3(unit * -0.56f, unit * 0.5f, unit * 0.42f), new Vector3(unit * 0.1f, unit * 0.78f, unit * 0.1f), wood);
                    CreateCube("Rack Blade Upgrade", root, center + new Vector3(unit * -0.56f, unit * 0.72f, unit * 0.42f), new Vector3(unit * 0.08f, unit * 0.08f, unit * 0.5f), metal);
                    break;
            }
        }

        private static void AddLevelThree(BuildingType type, Transform root, Vector3 center, float unit)
        {
            var metal = CreateMaterial("Upgrade Dark Iron", new Color(0.36f, 0.38f, 0.42f));
            var gold = CreateMaterial("Upgrade Gold", new Color(1f, 0.72f, 0.18f));
            var red = CreateMaterial("Upgrade Flag Red", new Color(0.58f, 0.08f, 0.06f));
            var wood = CreateMaterial("Upgrade Dark Wood", new Color(0.28f, 0.15f, 0.06f));

            switch (type)
            {
                case BuildingType.Castle:
                    CreateFlag(root, center + new Vector3(0f, unit * 3.0f, unit * -0.62f), unit, red, metal);
                    CreateFlag(root, center + new Vector3(0f, unit * 3.0f, unit * 0.62f), unit, red, metal);
                    break;
                case BuildingType.Farm:
                    CreateCube("Farm Windmill Post", root, center + new Vector3(unit * -0.66f, unit * 0.68f, unit * 0.48f), new Vector3(unit * 0.08f, unit * 0.92f, unit * 0.08f), wood);
                    CreateCube("Farm Windmill Blade A", root, center + new Vector3(unit * -0.66f, unit * 1.12f, unit * 0.48f), new Vector3(unit * 0.52f, unit * 0.06f, unit * 0.06f), metal);
                    CreateCube("Farm Windmill Blade B", root, center + new Vector3(unit * -0.66f, unit * 1.12f, unit * 0.48f), new Vector3(unit * 0.06f, unit * 0.52f, unit * 0.06f), metal);
                    break;
                case BuildingType.LumberjackCamp:
                    CreateCube("Lumber Shelter Roof", root, center + new Vector3(unit * 0.34f, unit * 0.72f, unit * 0.12f), new Vector3(unit * 0.82f, unit * 0.14f, unit * 0.72f), wood);
                    CreateCube("Lumber Shelter Iron Band", root, center + new Vector3(unit * 0.34f, unit * 0.83f, unit * 0.12f), new Vector3(unit * 0.86f, unit * 0.04f, unit * 0.76f), metal);
                    break;
                case BuildingType.AlchemistShop:
                    CreateCube("Bright Alchemist Sign", root, center + new Vector3(unit * 0.64f, unit * 1.18f, unit * -0.38f), new Vector3(unit * 0.08f, unit * 0.36f, unit * 0.46f), gold);
                    CreateSphere("Grand Potion", root, center + new Vector3(unit * -0.22f, unit * 1.55f, unit * 0.34f), Vector3.one * unit * 0.2f, gold);
                    break;
                case BuildingType.Tavern:
                    CreateCube("Tavern Extra Table", root, center + new Vector3(unit * 0.38f, unit * 0.2f, unit * 0.56f), new Vector3(unit * 0.5f, unit * 0.08f, unit * 0.34f), wood);
                    CreateCube("Tavern Barrel Stack", root, center + new Vector3(unit * -0.58f, unit * 0.28f, unit * 0.4f), new Vector3(unit * 0.34f, unit * 0.36f, unit * 0.34f), gold);
                    break;
                case BuildingType.Forge:
                    CreateCube("Advanced Furnace", root, center + new Vector3(unit * -0.48f, unit * 0.46f, unit * -0.38f), new Vector3(unit * 0.44f, unit * 0.52f, unit * 0.4f), metal);
                    CreateCube("Advanced Furnace Fire", root, center + new Vector3(unit * -0.24f, unit * 0.38f, unit * -0.38f), new Vector3(unit * 0.08f, unit * 0.24f, unit * 0.26f), red);
                    CreateCube("Armor Stand Upgrade", root, center + new Vector3(unit * 0.56f, unit * 0.54f, unit * 0.36f), new Vector3(unit * 0.18f, unit * 0.62f, unit * 0.34f), metal);
                    break;
            }
        }

        private static void AddTowerBands(Transform root, Vector3 center, float unit, Material material)
        {
            var offsets = new[]
            {
                new Vector3(-0.95f, 1.2f, -0.78f),
                new Vector3(-0.95f, 1.2f, 0.78f),
                new Vector3(0.92f, 1.2f, -0.78f),
                new Vector3(0.92f, 1.2f, 0.78f)
            };

            for (var i = 0; i < offsets.Length; i++)
            {
                CreateCube("Castle Tower Iron Band", root, center + Vector3.Scale(offsets[i], new Vector3(unit, unit, unit)), new Vector3(unit * 0.7f, unit * 0.08f, unit * 0.7f), material);
            }
        }

        private static void CreateFlag(Transform root, Vector3 position, float unit, Material cloth, Material pole)
        {
            CreateCube("Castle Flag Pole", root, position, new Vector3(unit * 0.05f, unit * 0.78f, unit * 0.05f), pole);
            CreateCube("Castle Flag Cloth", root, position + new Vector3(unit * 0.18f, unit * 0.22f, 0f), new Vector3(unit * 0.34f, unit * 0.18f, unit * 0.05f), cloth);
        }

        private static void CreateCube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.position = position;
            cube.transform.localScale = scale;
            cube.GetComponent<Renderer>().sharedMaterial = material;
            RemoveCollider(cube);
            VoxelVisuals.ApplyBlockStyle(cube, PrimitiveType.Cube, material, false);
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
