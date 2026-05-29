using Labyrinth.Base;
using Labyrinth.Core;
using UnityEngine;

namespace Labyrinth.Maze
{
    public static class InfirmaryRenderer
    {
        public static BuildingView Render(MazeRenderer renderer, Vector2Int position)
        {
            if (renderer == null || renderer.ContentRoot == null)
            {
                return null;
            }

            var unit = renderer.ModelUnitSize * 2f;
            var center = renderer.GridToWorld(position);
            var root = new GameObject($"Infirmary {position.x},{position.y}");
            root.transform.SetParent(renderer.ContentRoot, false);
            root.transform.position = center;

            var building = root.AddComponent<BuildingView>();
            building.Configure(
                BuildingType.Infirmary,
                "Лазарет",
                "лечение рыцарей",
                $"У входа восстанавливает HP и лечит боевые раны: 1 HP за {BaseDevelopment.InfirmaryFoodPerHitPoint} пищи, 1 рана за {BaseDevelopment.InfirmaryFoodPerHitPoint * 2}.",
                position,
                BaseDevelopment.InfirmaryFootprintRadiusCells);

            var wall = CreateMaterial("Infirmary Wall", new Color(0.72f, 0.68f, 0.58f));
            var roof = CreateMaterial("Infirmary Roof", new Color(0.48f, 0.09f, 0.08f));
            var wood = CreateMaterial("Infirmary Wood", new Color(0.32f, 0.18f, 0.08f));
            var sheet = CreateMaterial("Infirmary Sheet", new Color(0.92f, 0.9f, 0.82f));
            var red = CreateMaterial("Infirmary Cross", new Color(0.86f, 0.08f, 0.08f));
            var herb = CreateMaterial("Infirmary Herbs", new Color(0.2f, 0.56f, 0.22f));

            CreateCube("Infirmary Walls", root.transform, center + new Vector3(0f, unit * 0.5f, 0f), new Vector3(unit * 1.12f, unit, unit * 0.9f), wall, true);
            CreateCube("Infirmary Roof", root.transform, center + new Vector3(0f, unit * 1.12f, 0f), new Vector3(unit * 1.34f, unit * 0.32f, unit * 1.1f), roof, true);
            CreateCube("Infirmary Door", root.transform, center + new Vector3(unit * 0.58f, unit * 0.32f, 0f), new Vector3(unit * 0.08f, unit * 0.64f, unit * 0.3f), wood, false);
            CreateCube("Infirmary Sign Board", root.transform, center + new Vector3(unit * 0.62f, unit * 0.86f, unit * -0.3f), new Vector3(unit * 0.08f, unit * 0.46f, unit * 0.46f), sheet, false);
            CreateCube("Infirmary Cross Vertical", root.transform, center + new Vector3(unit * 0.67f, unit * 0.86f, unit * -0.3f), new Vector3(unit * 0.055f, unit * 0.34f, unit * 0.09f), red, false);
            CreateCube("Infirmary Cross Horizontal", root.transform, center + new Vector3(unit * 0.675f, unit * 0.86f, unit * -0.3f), new Vector3(unit * 0.055f, unit * 0.1f, unit * 0.32f), red, false);
            CreateCube("Infirmary Bed", root.transform, center + new Vector3(unit * -0.26f, unit * 0.24f, unit * -0.25f), new Vector3(unit * 0.68f, unit * 0.2f, unit * 0.34f), wood, false);
            CreateCube("Infirmary Bed Sheet", root.transform, center + new Vector3(unit * -0.26f, unit * 0.39f, unit * -0.25f), new Vector3(unit * 0.58f, unit * 0.08f, unit * 0.28f), sheet, false);
            CreateCube("Infirmary Herb Rack", root.transform, center + new Vector3(unit * -0.46f, unit * 0.55f, unit * 0.34f), new Vector3(unit * 0.08f, unit * 0.58f, unit * 0.28f), wood, false);
            CreateCube("Infirmary Herb Bundle", root.transform, center + new Vector3(unit * -0.52f, unit * 0.76f, unit * 0.34f), new Vector3(unit * 0.14f, unit * 0.2f, unit * 0.2f), herb, false);
            CreateCube("Infirmary Small Chimney", root.transform, center + new Vector3(unit * -0.28f, unit * 1.42f, unit * 0.24f), new Vector3(unit * 0.2f, unit * 0.48f, unit * 0.2f), wall, false);
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
