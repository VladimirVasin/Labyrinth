using Labyrinth.Base;
using Labyrinth.Core;
using UnityEngine;

namespace Labyrinth.Maze
{
    public static class MarketRenderer
    {
        public static BuildingView Render(MazeRenderer renderer, Vector2Int position)
        {
            if (renderer == null || renderer.ContentRoot == null)
            {
                return null;
            }

            var unit = renderer.ModelUnitSize * 2f;
            var center = renderer.GridToWorld(position);
            var root = new GameObject($"Market {position.x},{position.y}");
            root.transform.SetParent(renderer.ContentRoot, false);
            root.transform.position = center;

            var building = root.AddComponent<BuildingView>();
            building.Configure(
                BuildingType.Market,
                "Рынок",
                "обмен ресурсов",
                "Покупка и продажа пищи, дерева и железа за золото. Чем ниже запас, тем выше цена.",
                position,
                BaseDevelopment.MarketFootprintRadiusCells);

            var timber = CreateMaterial("Market Timber", new Color(0.42f, 0.23f, 0.09f));
            var counter = CreateMaterial("Market Counter", new Color(0.56f, 0.36f, 0.16f));
            var redCloth = CreateMaterial("Market Red Cloth", new Color(0.72f, 0.16f, 0.1f));
            var blueCloth = CreateMaterial("Market Blue Cloth", new Color(0.12f, 0.28f, 0.62f));
            var gold = CreateMaterial("Market Coin Gold", new Color(1f, 0.73f, 0.18f));
            var sack = CreateMaterial("Market Sacks", new Color(0.72f, 0.58f, 0.34f));
            var crate = CreateMaterial("Market Crates", new Color(0.36f, 0.2f, 0.08f));
            var iron = CreateMaterial("Market Iron", new Color(0.58f, 0.62f, 0.66f));

            CreateStall(root.transform, center + new Vector3(-unit * 0.42f, 0f, -unit * 0.28f), unit, redCloth, counter, timber, true);
            CreateStall(root.transform, center + new Vector3(unit * 0.42f, 0f, unit * 0.28f), unit, blueCloth, counter, timber, true);
            CreateCube("Market Central Counter", root.transform, center + new Vector3(0f, unit * 0.28f, 0f), new Vector3(unit * 0.9f, unit * 0.22f, unit * 0.52f), counter, true);
            CreateCube("Market Sign Pole", root.transform, center + new Vector3(0f, unit * 0.92f, unit * -0.6f), new Vector3(unit * 0.08f, unit * 1.25f, unit * 0.08f), timber, false);
            CreateCube("Market Sign Board", root.transform, center + new Vector3(0f, unit * 1.36f, unit * -0.62f), new Vector3(unit * 0.76f, unit * 0.26f, unit * 0.08f), gold, false);
            CreateCube("Market Food Sack", root.transform, center + new Vector3(-unit * 0.52f, unit * 0.16f, unit * 0.44f), Vector3.one * unit * 0.24f, sack, false);
            CreateCube("Market Wood Crate", root.transform, center + new Vector3(unit * 0.12f, unit * 0.18f, unit * 0.54f), new Vector3(unit * 0.34f, unit * 0.28f, unit * 0.26f), crate, false);
            CreateCube("Market Iron Box", root.transform, center + new Vector3(unit * 0.52f, unit * 0.18f, unit * -0.42f), new Vector3(unit * 0.32f, unit * 0.2f, unit * 0.26f), iron, false);
            CreateSphere("Market Coin A", root.transform, center + new Vector3(-unit * 0.1f, unit * 0.48f, -unit * 0.1f), Vector3.one * unit * 0.08f, gold);
            CreateSphere("Market Coin B", root.transform, center + new Vector3(unit * 0.04f, unit * 0.48f, -unit * 0.12f), Vector3.one * unit * 0.07f, gold);
            return building;
        }

        private static void CreateStall(
            Transform parent,
            Vector3 center,
            float unit,
            Material cloth,
            Material counter,
            Material timber,
            bool keepCollider)
        {
            CreateCube("Market Stall Counter", parent, center + new Vector3(0f, unit * 0.28f, 0f), new Vector3(unit * 0.72f, unit * 0.24f, unit * 0.44f), counter, keepCollider);
            CreateCube("Market Stall Awning", parent, center + new Vector3(0f, unit * 0.72f, 0f), new Vector3(unit * 0.88f, unit * 0.18f, unit * 0.58f), cloth, false);
            CreateCube("Market Stall Post A", parent, center + new Vector3(-unit * 0.36f, unit * 0.44f, -unit * 0.22f), new Vector3(unit * 0.06f, unit * 0.62f, unit * 0.06f), timber, false);
            CreateCube("Market Stall Post B", parent, center + new Vector3(unit * 0.36f, unit * 0.44f, unit * 0.22f), new Vector3(unit * 0.06f, unit * 0.62f, unit * 0.06f), timber, false);
        }

        private static GameObject CreateCube(string name, Transform parent, Vector3 position, Vector3 scale, Material material, bool keepCollider)
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
            return cube;
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
