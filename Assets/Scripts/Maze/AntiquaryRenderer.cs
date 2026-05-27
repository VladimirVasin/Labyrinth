using Labyrinth.Base;
using Labyrinth.Core;
using Labyrinth.Hero;
using UnityEngine;

namespace Labyrinth.Maze
{
    public static class AntiquaryRenderer
    {
        public static BuildingView Render(MazeRenderer renderer, Vector2Int position)
        {
            if (renderer == null || renderer.ContentRoot == null)
            {
                return null;
            }

            var unit = renderer.ModelUnitSize * 2f;
            var center = renderer.GridToWorld(position);
            var root = new GameObject($"Antiquary {position.x},{position.y}");
            root.transform.SetParent(renderer.ContentRoot, false);
            root.transform.position = center;

            var building = root.AddComponent<BuildingView>();
            building.Configure(
                BuildingType.Antiquary,
                "Антиквариат",
                "редкие артефакты",
                $"{HeroInventory.ReturnStoneItemName}: {BaseDevelopment.ReturnStoneGoldCost} зол.",
                position,
                BaseDevelopment.AntiquaryFootprintRadiusCells);

            var wall = CreateMaterial("Antiquary Wall", new Color(0.36f, 0.28f, 0.22f));
            var roof = CreateMaterial("Antiquary Roof", new Color(0.18f, 0.1f, 0.16f));
            var wood = CreateMaterial("Antiquary Wood", new Color(0.28f, 0.15f, 0.07f));
            var brass = CreateMaterial("Antiquary Brass", new Color(0.92f, 0.66f, 0.22f));
            var cloth = CreateMaterial("Antiquary Cloth", new Color(0.28f, 0.12f, 0.38f));
            var parchment = CreateMaterial("Antiquary Parchment", new Color(0.78f, 0.64f, 0.42f));
            var stone = CreateMaterial("Return Stone Glow", new Color(0.38f, 0.72f, 1f));
            var glass = CreateMaterial("Antiquary Glass", new Color(0.7f, 0.9f, 1f, 0.8f));

            CreateCube("Antiquary Walls", root.transform, center + new Vector3(0f, unit * 0.5f, 0f), new Vector3(unit * 1.02f, unit, unit * 0.86f), wall, true);
            CreateCube("Antiquary Roof", root.transform, center + new Vector3(0f, unit * 1.12f, 0f), new Vector3(unit * 1.26f, unit * 0.32f, unit * 1.06f), roof, true);
            CreateCube("Antiquary Door", root.transform, center + new Vector3(unit * 0.53f, unit * 0.32f, 0f), new Vector3(unit * 0.08f, unit * 0.64f, unit * 0.3f), wood, false);
            CreateCube("Antiquary Sign", root.transform, center + new Vector3(unit * 0.58f, unit * 0.88f, unit * -0.28f), new Vector3(unit * 0.08f, unit * 0.34f, unit * 0.42f), brass, false);
            CreateCube("Antiquary Awning", root.transform, center + new Vector3(unit * 0.58f, unit * 0.68f, unit * 0.22f), new Vector3(unit * 0.08f, unit * 0.18f, unit * 0.58f), cloth, false);
            CreateCube("Antiquary Display Table", root.transform, center + new Vector3(unit * -0.18f, unit * 0.22f, unit * 0.32f), new Vector3(unit * 0.68f, unit * 0.18f, unit * 0.38f), wood, false);
            CreateSphere("Return Stone", root.transform, center + new Vector3(unit * -0.18f, unit * 0.48f, unit * 0.32f), Vector3.one * unit * 0.24f, stone);
            CreateSphere("Return Stone Shine", root.transform, center + new Vector3(unit * -0.08f, unit * 0.57f, unit * 0.22f), Vector3.one * unit * 0.08f, glass);
            CreateCube("Antiquary Scroll Rack", root.transform, center + new Vector3(unit * -0.42f, unit * 0.44f, unit * -0.3f), new Vector3(unit * 0.16f, unit * 0.52f, unit * 0.16f), brass, false);
            CreateCube("Antiquary Scroll", root.transform, center + new Vector3(unit * -0.42f, unit * 0.74f, unit * -0.3f), new Vector3(unit * 0.28f, unit * 0.1f, unit * 0.22f), parchment, false);
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
        }

        private static void CreateSphere(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = name;
            sphere.transform.SetParent(parent, false);
            sphere.transform.position = position;
            sphere.transform.localScale = scale;
            sphere.GetComponent<Renderer>().sharedMaterial = material;
            RemoveCollider(sphere);
        }

        private static Material CreateMaterial(string name, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            var material = new Material(shader) { name = name, color = color };
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            return material;
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
