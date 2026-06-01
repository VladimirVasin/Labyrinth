using Labyrinth.Base;
using Labyrinth.Core;
using UnityEngine;

namespace Labyrinth.Maze
{
    public static class HeroesGuildRenderer
    {
        public static BuildingView Render(MazeRenderer renderer, Vector2Int position)
        {
            if (renderer == null || renderer.ContentRoot == null)
            {
                return null;
            }

            var unit = renderer.ModelUnitSize * 2f;
            var center = renderer.GridToWorld(position);
            var root = new GameObject($"Heroes Guild {position.x},{position.y}");
            root.transform.SetParent(renderer.ContentRoot, false);
            root.transform.position = center;

            var building = root.AddComponent<BuildingView>();
            building.Configure(
                BuildingType.HeroesGuild,
                "Гильдия героев",
                "контракты зачистки",
                "Контракты зачистки: 3 доступно, 0 в работе, 0 ждут сдачи.",
                position,
                BaseDevelopment.HeroesGuildFootprintRadiusCells);

            var stone = CreateMaterial("Heroes Guild Stone", new Color(0.46f, 0.43f, 0.38f));
            var roof = CreateMaterial("Heroes Guild Roof", new Color(0.18f, 0.2f, 0.28f));
            var timber = CreateMaterial("Heroes Guild Timber", new Color(0.34f, 0.19f, 0.08f));
            var bannerBlue = CreateMaterial("Heroes Guild Banner Blue", new Color(0.1f, 0.22f, 0.62f));
            var bannerGold = CreateMaterial("Heroes Guild Banner Gold", new Color(0.94f, 0.7f, 0.22f));
            var steel = CreateMaterial("Heroes Guild Steel", new Color(0.62f, 0.66f, 0.68f));
            var parchment = CreateMaterial("Heroes Guild Parchment", new Color(0.78f, 0.66f, 0.43f));
            var lamp = VoxelVisuals.CreateEmissiveMaterial("Heroes Guild Lamp", new Color(1f, 0.72f, 0.28f), 1.8f);

            CreateCube("Heroes Guild Hall", root.transform, center + new Vector3(0f, unit * 0.52f, 0f), new Vector3(unit * 1.2f, unit * 1.04f, unit * 0.92f), stone, true);
            CreateCube("Heroes Guild Roof", root.transform, center + new Vector3(0f, unit * 1.17f, 0f), new Vector3(unit * 1.44f, unit * 0.34f, unit * 1.12f), roof, true);
            CreateCube("Heroes Guild Door", root.transform, center + new Vector3(unit * 0.62f, unit * 0.36f, 0f), new Vector3(unit * 0.08f, unit * 0.72f, unit * 0.34f), timber, false);
            CreateCube("Heroes Guild Banner Left", root.transform, center + new Vector3(unit * 0.64f, unit * 0.82f, unit * -0.34f), new Vector3(unit * 0.07f, unit * 0.58f, unit * 0.22f), bannerBlue, false);
            CreateCube("Heroes Guild Banner Right", root.transform, center + new Vector3(unit * 0.64f, unit * 0.82f, unit * 0.34f), new Vector3(unit * 0.07f, unit * 0.58f, unit * 0.22f), bannerBlue, false);
            CreateCube("Heroes Guild Banner Mark L", root.transform, center + new Vector3(unit * 0.69f, unit * 0.88f, unit * -0.34f), new Vector3(unit * 0.03f, unit * 0.22f, unit * 0.05f), bannerGold, false);
            CreateCube("Heroes Guild Banner Mark R", root.transform, center + new Vector3(unit * 0.69f, unit * 0.88f, unit * 0.34f), new Vector3(unit * 0.03f, unit * 0.22f, unit * 0.05f), bannerGold, false);
            CreateCube("Heroes Guild Notice Board", root.transform, center + new Vector3(unit * -0.44f, unit * 0.42f, unit * 0.5f), new Vector3(unit * 0.58f, unit * 0.42f, unit * 0.08f), timber, false);
            CreateCube("Heroes Guild Contract A", root.transform, center + new Vector3(unit * -0.52f, unit * 0.47f, unit * 0.55f), new Vector3(unit * 0.16f, unit * 0.22f, unit * 0.03f), parchment, false);
            CreateCube("Heroes Guild Contract B", root.transform, center + new Vector3(unit * -0.32f, unit * 0.42f, unit * 0.55f), new Vector3(unit * 0.16f, unit * 0.18f, unit * 0.03f), parchment, false);
            CreateCube("Heroes Guild Sword A", root.transform, center + new Vector3(unit * -0.46f, unit * 0.9f, unit * -0.48f), new Vector3(unit * 0.06f, unit * 0.74f, unit * 0.05f), steel, false).transform.rotation = Quaternion.Euler(0f, 0f, 34f);
            CreateCube("Heroes Guild Sword B", root.transform, center + new Vector3(unit * -0.22f, unit * 0.9f, unit * -0.48f), new Vector3(unit * 0.06f, unit * 0.74f, unit * 0.05f), steel, false).transform.rotation = Quaternion.Euler(0f, 0f, -34f);
            CreateCube("Heroes Guild Shield", root.transform, center + new Vector3(unit * -0.34f, unit * 0.72f, unit * -0.52f), new Vector3(unit * 0.28f, unit * 0.36f, unit * 0.06f), bannerBlue, false);
            CreateSphere("Heroes Guild Lamp", root.transform, center + new Vector3(unit * 0.62f, unit * 1.16f, 0f), Vector3.one * unit * 0.11f, lamp);
            return building;
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
