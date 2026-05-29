using Labyrinth.Base;
using Labyrinth.Core;
using UnityEngine;

namespace Labyrinth.Maze
{
    public static class CartographerHouseRenderer
    {
        public static BuildingView Render(MazeRenderer renderer, Vector2Int position)
        {
            if (renderer == null || renderer.ContentRoot == null)
            {
                return null;
            }

            var unit = renderer.ModelUnitSize * 2f;
            var center = renderer.GridToWorld(position);
            var root = new GameObject($"Cartographer House {position.x},{position.y}");
            root.transform.SetParent(renderer.ContentRoot, false);
            root.transform.position = center;

            var building = root.AddComponent<BuildingView>();
            building.Configure(
                BuildingType.CartographerHouse,
                "Дом картографа",
                "общая карта",
                "Рыцари сдают знания у входа и получают общую карту.",
                position,
                BaseDevelopment.CartographerHouseFootprintRadiusCells);

            var wall = CreateMaterial("Cartographer Wall", new Color(0.48f, 0.42f, 0.32f));
            var roof = CreateMaterial("Cartographer Roof", new Color(0.12f, 0.2f, 0.34f));
            var wood = CreateMaterial("Cartographer Wood", new Color(0.34f, 0.19f, 0.08f));
            var parchment = CreateMaterial("Cartographer Parchment", new Color(0.86f, 0.76f, 0.55f));
            var ink = CreateMaterial("Cartographer Ink", new Color(0.05f, 0.06f, 0.07f));
            var brass = CreateMaterial("Cartographer Brass", new Color(0.95f, 0.68f, 0.2f));
            var blue = CreateMaterial("Cartographer Blue", new Color(0.2f, 0.44f, 0.76f));

            CreateCube("Cartographer Walls", root.transform, center + new Vector3(0f, unit * 0.5f, 0f), new Vector3(unit * 1.08f, unit, unit * 0.86f), wall, true);
            CreateCube("Cartographer Roof", root.transform, center + new Vector3(0f, unit * 1.12f, 0f), new Vector3(unit * 1.28f, unit * 0.32f, unit * 1.04f), roof, true);
            CreateCube("Cartographer Door", root.transform, center + new Vector3(unit * 0.56f, unit * 0.32f, 0f), new Vector3(unit * 0.08f, unit * 0.64f, unit * 0.28f), wood, false);
            CreateCube("Map Table", root.transform, center + new Vector3(unit * -0.18f, unit * 0.24f, unit * -0.26f), new Vector3(unit * 0.74f, unit * 0.14f, unit * 0.48f), wood, false);
            CreateCube("Open Map", root.transform, center + new Vector3(unit * -0.18f, unit * 0.34f, unit * -0.26f), new Vector3(unit * 0.62f, unit * 0.035f, unit * 0.36f), parchment, false);
            CreateCube("Map Line X", root.transform, center + new Vector3(unit * -0.18f, unit * 0.37f, unit * -0.26f), new Vector3(unit * 0.48f, unit * 0.025f, unit * 0.035f), ink, false);
            CreateCube("Map Line Z", root.transform, center + new Vector3(unit * -0.18f, unit * 0.385f, unit * -0.26f), new Vector3(unit * 0.035f, unit * 0.025f, unit * 0.28f), ink, false);
            CreateCube("Compass Sign", root.transform, center + new Vector3(unit * 0.6f, unit * 0.84f, unit * -0.28f), new Vector3(unit * 0.08f, unit * 0.42f, unit * 0.42f), parchment, false);
            CreateCube("Compass Needle", root.transform, center + new Vector3(unit * 0.65f, unit * 0.84f, unit * -0.28f), new Vector3(unit * 0.055f, unit * 0.06f, unit * 0.34f), blue, false);
            CreateSphere("Brass Compass", root.transform, center + new Vector3(unit * 0.65f, unit * 0.84f, unit * -0.28f), Vector3.one * unit * 0.13f, brass);
            CreateCube("Scroll Rack", root.transform, center + new Vector3(unit * 0.28f, unit * 0.26f, unit * 0.34f), new Vector3(unit * 0.48f, unit * 0.16f, unit * 0.18f), parchment, false);
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
