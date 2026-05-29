using Labyrinth.Base;
using Labyrinth.Core;
using UnityEngine;

namespace Labyrinth.Maze
{
    public static class MinersGuildRenderer
    {
        public static BuildingView Render(MazeRenderer renderer, Vector2Int position)
        {
            if (renderer == null || renderer.ContentRoot == null)
            {
                return null;
            }

            var unit = renderer.ModelUnitSize * 2f;
            var center = renderer.GridToWorld(position);
            var root = new GameObject($"Miners Guild {position.x},{position.y}");
            root.transform.SetParent(renderer.ContentRoot, false);
            root.transform.position = center;

            var building = root.AddComponent<BuildingView>();
            building.Configure(
                BuildingType.MinersGuild,
                "Гильдия шахтёров",
                "разведка руды",
                "Открывает закладку шахт в изученных малых пещерах.",
                position,
                BaseDevelopment.MinersGuildFootprintRadiusCells);

            var wall = CreateMaterial("Miners Guild Wall", new Color(0.42f, 0.34f, 0.25f));
            var roof = CreateMaterial("Miners Guild Roof", new Color(0.18f, 0.14f, 0.11f));
            var timber = CreateMaterial("Miners Guild Timber", new Color(0.32f, 0.18f, 0.08f));
            var metal = CreateMaterial("Miners Guild Metal", new Color(0.58f, 0.62f, 0.64f));
            var lamp = VoxelVisuals.CreateEmissiveMaterial("Miners Guild Lamp", new Color(1f, 0.68f, 0.2f), 1.95f);
            var ore = CreateMaterial("Miners Guild Ore", new Color(0.2f, 0.22f, 0.25f));

            CreateCube("Miners Guild Hall", root.transform, center + new Vector3(0f, unit * 0.5f, 0f), new Vector3(unit * 1.12f, unit, unit * 0.88f), wall, true);
            CreateCube("Miners Guild Roof", root.transform, center + new Vector3(0f, unit * 1.12f, 0f), new Vector3(unit * 1.36f, unit * 0.34f, unit * 1.06f), roof, true);
            CreateCube("Mine Door", root.transform, center + new Vector3(unit * 0.58f, unit * 0.34f, 0f), new Vector3(unit * 0.08f, unit * 0.68f, unit * 0.34f), timber, false);
            CreateCube("Timber Beam Left", root.transform, center + new Vector3(unit * -0.38f, unit * 0.68f, unit * -0.42f), new Vector3(unit * 0.13f, unit * 0.9f, unit * 0.13f), timber, false);
            CreateCube("Timber Beam Right", root.transform, center + new Vector3(unit * 0.38f, unit * 0.68f, unit * -0.42f), new Vector3(unit * 0.13f, unit * 0.9f, unit * 0.13f), timber, false);
            CreateCube("Pick Handle", root.transform, center + new Vector3(unit * 0.62f, unit * 0.95f, unit * -0.28f), new Vector3(unit * 0.06f, unit * 0.5f, unit * 0.06f), timber, false).transform.rotation = Quaternion.Euler(0f, 0f, 35f);
            CreateCube("Pick Head", root.transform, center + new Vector3(unit * 0.69f, unit * 1.14f, unit * -0.28f), new Vector3(unit * 0.38f, unit * 0.06f, unit * 0.08f), metal, false).transform.rotation = Quaternion.Euler(0f, 0f, 35f);
            CreateCube("Ore Crate", root.transform, center + new Vector3(unit * -0.34f, unit * 0.18f, unit * 0.36f), new Vector3(unit * 0.48f, unit * 0.24f, unit * 0.28f), timber, false);
            CreateSphere("Ore Chunk A", root.transform, center + new Vector3(unit * -0.42f, unit * 0.38f, unit * 0.32f), Vector3.one * unit * 0.12f, ore);
            CreateSphere("Ore Chunk B", root.transform, center + new Vector3(unit * -0.24f, unit * 0.38f, unit * 0.42f), Vector3.one * unit * 0.1f, metal);
            CreateSphere("Guild Lamp", root.transform, center + new Vector3(unit * 0.58f, unit * 1.34f, unit * 0.3f), Vector3.one * unit * 0.12f, lamp);

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
