using Labyrinth.Base;
using Labyrinth.Core;
using UnityEngine;

namespace Labyrinth.Maze
{
    public static class LumberjackCampRenderer
    {
        public static BuildingView Render(MazeRenderer renderer, Vector2Int position)
        {
            if (renderer == null || renderer.ContentRoot == null)
            {
                return null;
            }

            var unit = renderer.ModelUnitSize * 2f;
            var center = renderer.GridToWorld(position);
            var root = new GameObject($"Lumberjack Camp {position.x},{position.y}");
            root.transform.SetParent(renderer.ContentRoot, false);
            root.transform.position = center;

            var building = root.AddComponent<BuildingView>();
            building.Configure(
                BuildingType.LumberjackCamp,
                "Лагерь лесорубов",
                "добыча дерева",
                $"+1 дерево/{ResourceProductionController.LumberjackCampProductionIntervalSeconds:0.#} сек, караван 10",
                position,
                BaseDevelopment.LumberjackCampFootprintRadiusCells);

            var ground = CreateMaterial("Lumber Camp Ground", new Color(0.25f, 0.2f, 0.12f));
            var tent = CreateMaterial("Lumber Camp Tent", new Color(0.36f, 0.22f, 0.12f));
            var canvas = CreateMaterial("Lumber Camp Canvas", new Color(0.55f, 0.43f, 0.28f));
            var wood = CreateMaterial("Lumber Camp Logs", new Color(0.48f, 0.28f, 0.11f));
            var leaf = CreateMaterial("Lumber Camp Pine", new Color(0.16f, 0.42f, 0.18f));
            var metal = CreateMaterial("Lumber Camp Axe Metal", new Color(0.62f, 0.64f, 0.66f));
            var rope = CreateMaterial("Lumber Camp Rope", new Color(0.78f, 0.58f, 0.28f));

            CreateCube("Lumber Camp Ground", root.transform, center + new Vector3(0f, unit * -0.02f, 0f), new Vector3(unit * 1.35f, unit * 0.06f, unit * 1.2f), ground, false);
            CreateCube("Lumber Camp Hut", root.transform, center + new Vector3(unit * -0.24f, unit * 0.34f, unit * -0.16f), new Vector3(unit * 0.62f, unit * 0.68f, unit * 0.5f), canvas, true);
            CreateCube("Lumber Camp Roof", root.transform, center + new Vector3(unit * -0.24f, unit * 0.78f, unit * -0.16f), new Vector3(unit * 0.78f, unit * 0.24f, unit * 0.64f), tent, true);
            CreateCube("Lumber Camp Door", root.transform, center + new Vector3(unit * 0.09f, unit * 0.26f, unit * -0.16f), new Vector3(unit * 0.08f, unit * 0.48f, unit * 0.22f), wood, false);

            for (var i = 0; i < 3; i++)
            {
                var zOffset = (i - 1) * 0.18f;
                var log = CreateCylinder(
                    "Lumber Camp Log",
                    root.transform,
                    center + new Vector3(unit * 0.34f, unit * (0.13f + i * 0.09f), unit * zOffset),
                    new Vector3(unit * 0.13f, unit * 0.38f, unit * 0.13f),
                    Quaternion.Euler(0f, 0f, 90f),
                    wood);
                RemoveCollider(log);
            }

            CreatePine(root.transform, center + new Vector3(unit * 0.48f, 0f, unit * 0.34f), unit, wood, leaf);
            CreatePine(root.transform, center + new Vector3(unit * -0.5f, 0f, unit * 0.36f), unit * 0.86f, wood, leaf);
            CreateCylinder("Lumber Camp Stump", root.transform, center + new Vector3(unit * 0.04f, unit * 0.18f, unit * 0.42f), new Vector3(unit * 0.18f, unit * 0.18f, unit * 0.18f), Quaternion.identity, wood);
            CreateCube("Lumber Camp Axe Handle", root.transform, center + new Vector3(unit * 0.12f, unit * 0.42f, unit * 0.42f), new Vector3(unit * 0.06f, unit * 0.48f, unit * 0.06f), wood, false);
            CreateCube("Lumber Camp Axe Head", root.transform, center + new Vector3(unit * 0.12f, unit * 0.66f, unit * 0.42f), new Vector3(unit * 0.28f, unit * 0.12f, unit * 0.08f), metal, false);
            CreateCube("Lumber Camp Log Strap", root.transform, center + new Vector3(unit * 0.34f, unit * 0.25f, 0f), new Vector3(unit * 0.32f, unit * 0.05f, unit * 0.58f), rope, false);
            CreateCube("Lumber Camp Saw Blade", root.transform, center + new Vector3(unit * -0.5f, unit * 0.32f, unit * -0.42f), new Vector3(unit * 0.5f, unit * 0.06f, unit * 0.1f), metal, false);
            CreateCube("Lumber Camp Saw Handle", root.transform, center + new Vector3(unit * -0.2f, unit * 0.32f, unit * -0.42f), new Vector3(unit * 0.1f, unit * 0.16f, unit * 0.12f), wood, false);
            return building;
        }

        private static void CreatePine(Transform parent, Vector3 basePosition, float unit, Material wood, Material leaf)
        {
            CreateCylinder("Lumber Camp Trunk", parent, basePosition + new Vector3(0f, unit * 0.32f, 0f), new Vector3(unit * 0.08f, unit * 0.32f, unit * 0.08f), Quaternion.identity, wood);
            CreateCube("Lumber Camp Crown", parent, basePosition + new Vector3(0f, unit * 0.76f, 0f), new Vector3(unit * 0.42f, unit * 0.5f, unit * 0.42f), leaf, false);
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

        private static GameObject CreateCylinder(string name, Transform parent, Vector3 position, Vector3 scale, Quaternion rotation, Material material)
        {
            var cylinder = GameObject.CreatePrimitive(VoxelVisuals.ResolvePrimitive(PrimitiveType.Cylinder, name));
            cylinder.name = name;
            cylinder.transform.SetParent(parent, false);
            cylinder.transform.position = position;
            cylinder.transform.rotation = rotation;
            cylinder.transform.localScale = scale;
            cylinder.GetComponent<Renderer>().sharedMaterial = material;
            VoxelVisuals.ApplyBlockStyle(cylinder, PrimitiveType.Cylinder, material, true);
            return cylinder;
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
