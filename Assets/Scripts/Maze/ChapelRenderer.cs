using Labyrinth.Base;
using Labyrinth.Core;
using Labyrinth.Hero;
using UnityEngine;

namespace Labyrinth.Maze
{
    public static class ChapelRenderer
    {
        public static BuildingView Render(MazeRenderer renderer, Vector2Int position)
        {
            if (renderer == null || renderer.ContentRoot == null)
            {
                return null;
            }

            var unit = renderer.ModelUnitSize * 2f;
            var center = renderer.GridToWorld(position);
            var root = new GameObject($"Chapel {position.x},{position.y}");
            root.transform.SetParent(renderer.ContentRoot, false);
            root.transform.position = center;

            var building = root.AddComponent<BuildingView>();
            building.Configure(
                BuildingType.Chapel,
                "Часовня",
                "одно благословение",
                "Рыцари покупают одно благословение у входа.",
                position,
                BaseDevelopment.ChapelFootprintRadiusCells);

            var wall = CreateMaterial("Chapel Stone", new Color(0.62f, 0.6f, 0.54f));
            var roof = CreateMaterial("Chapel Roof", new Color(0.32f, 0.12f, 0.18f));
            var wood = CreateMaterial("Chapel Wood", new Color(0.34f, 0.18f, 0.08f));
            var gold = CreateMaterial("Chapel Gold", new Color(1f, 0.78f, 0.24f));
            var glass = CreateMaterial("Chapel Glass", new Color(0.32f, 0.74f, 1f));
            var candle = CreateMaterial("Chapel Candle", new Color(1f, 0.86f, 0.42f));

            CreateCube("Chapel Nave", root.transform, center + new Vector3(0f, unit * 0.5f, 0f), new Vector3(unit * 1.08f, unit, unit * 0.86f), wall, true);
            CreateCube("Chapel Roof", root.transform, center + new Vector3(0f, unit * 1.1f, 0f), new Vector3(unit * 1.3f, unit * 0.32f, unit * 1.04f), roof, true);
            CreateCube("Chapel Tower", root.transform, center + new Vector3(unit * -0.38f, unit * 1.18f, 0f), new Vector3(unit * 0.44f, unit * 1.15f, unit * 0.48f), wall, true);
            CreateCube("Chapel Tower Roof", root.transform, center + new Vector3(unit * -0.38f, unit * 1.88f, 0f), new Vector3(unit * 0.62f, unit * 0.26f, unit * 0.66f), roof, true);
            CreateCube("Chapel Door", root.transform, center + new Vector3(unit * 0.56f, unit * 0.35f, 0f), new Vector3(unit * 0.08f, unit * 0.7f, unit * 0.32f), wood, false);
            CreateCube("Chapel Window", root.transform, center + new Vector3(unit * 0.6f, unit * 0.78f, unit * -0.32f), new Vector3(unit * 0.08f, unit * 0.28f, unit * 0.22f), glass, false);
            CreateCube("Chapel Cross Vertical", root.transform, center + new Vector3(unit * -0.38f, unit * 2.22f, 0f), new Vector3(unit * 0.09f, unit * 0.54f, unit * 0.09f), gold, false);
            CreateCube("Chapel Cross Horizontal", root.transform, center + new Vector3(unit * -0.38f, unit * 2.29f, 0f), new Vector3(unit * 0.38f, unit * 0.08f, unit * 0.08f), gold, false);
            CreateCube("Chapel Prayer Bench", root.transform, center + new Vector3(unit * 0.04f, unit * 0.22f, unit * 0.36f), new Vector3(unit * 0.64f, unit * 0.16f, unit * 0.18f), wood, false);
            CreateCube("Chapel Candle Left", root.transform, center + new Vector3(unit * 0.34f, unit * 0.52f, unit * -0.3f), new Vector3(unit * 0.08f, unit * 0.32f, unit * 0.08f), candle, false);
            CreateCube("Chapel Candle Right", root.transform, center + new Vector3(unit * 0.34f, unit * 0.52f, unit * 0.3f), new Vector3(unit * 0.08f, unit * 0.32f, unit * 0.08f), candle, false);
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
