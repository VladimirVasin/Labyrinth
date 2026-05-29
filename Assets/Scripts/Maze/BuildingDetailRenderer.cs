using Labyrinth.Core;
using UnityEngine;

namespace Labyrinth.Maze
{
    public static class BuildingDetailRenderer
    {
        public static void AddHeroHouseDetails(Transform parent, Vector3 center, float unit)
        {
            var wood = CreateMaterial("Hero House Detail Wood", new Color(0.24f, 0.13f, 0.06f));
            var metal = CreateMaterial("Hero House Detail Metal", new Color(0.66f, 0.68f, 0.72f));
            var blue = CreateMaterial("Hero House Detail Blue", new Color(0.14f, 0.24f, 0.74f));
            var red = CreateMaterial("Hero House Detail Red", new Color(0.62f, 0.08f, 0.06f));
            var straw = CreateMaterial("Hero House Detail Straw", new Color(0.76f, 0.56f, 0.22f));

            CreateCube("Hero House Shield Plate", parent, center + new Vector3(unit * 0.52f, unit * 0.62f, unit * 0.2f), new Vector3(unit * 0.06f, unit * 0.36f, unit * 0.26f), blue, false);
            CreateCube("Hero House Shield Stripe", parent, center + new Vector3(unit * 0.56f, unit * 0.62f, unit * 0.2f), new Vector3(unit * 0.04f, unit * 0.28f, unit * 0.08f), red, false);
            CreateCube("Hero House Sword Rack", parent, center + new Vector3(unit * -0.52f, unit * 0.28f, unit * -0.35f), new Vector3(unit * 0.08f, unit * 0.56f, unit * 0.08f), metal, false);
            CreateCube("Hero House Sword Guard", parent, center + new Vector3(unit * -0.52f, unit * 0.46f, unit * -0.35f), new Vector3(unit * 0.08f, unit * 0.06f, unit * 0.32f), metal, false);
            CreateCube("Hero House Training Post", parent, center + new Vector3(unit * -0.42f, unit * 0.28f, unit * 0.35f), new Vector3(unit * 0.12f, unit * 0.56f, unit * 0.12f), wood, false);
            CreateCube("Hero House Training Straw", parent, center + new Vector3(unit * -0.42f, unit * 0.62f, unit * 0.35f), new Vector3(unit * 0.3f, unit * 0.18f, unit * 0.18f), straw, false);
        }

        public static void AddAlchemistDetails(Transform parent, Vector3 center, float unit)
        {
            var wood = CreateMaterial("Alchemist Detail Wood", new Color(0.22f, 0.12f, 0.06f));
            var glass = CreateMaterial("Alchemist Detail Glass", new Color(0.12f, 0.85f, 0.72f));
            var purple = CreateMaterial("Alchemist Detail Purple", new Color(0.58f, 0.18f, 0.86f));
            var green = CreateMaterial("Alchemist Detail Green", new Color(0.22f, 0.95f, 0.28f));
            var metal = CreateMaterial("Alchemist Detail Metal", new Color(0.12f, 0.13f, 0.14f));

            CreateCube("Alchemist Front Shelf", parent, center + new Vector3(unit * 0.58f, unit * 0.58f, unit * 0.22f), new Vector3(unit * 0.08f, unit * 0.08f, unit * 0.56f), wood, false);
            CreateBottle("Alchemist Cyan Bottle", parent, center + new Vector3(unit * 0.64f, unit * 0.82f, unit * 0.04f), unit, glass);
            CreateBottle("Alchemist Purple Bottle", parent, center + new Vector3(unit * 0.64f, unit * 0.78f, unit * 0.24f), unit * 0.86f, purple);
            CreateCube("Alchemist Cauldron", parent, center + new Vector3(unit * -0.42f, unit * 0.32f, unit * 0.34f), new Vector3(unit * 0.36f, unit * 0.22f, unit * 0.36f), metal, false);
            CreateCube("Alchemist Cauldron Glow", parent, center + new Vector3(unit * -0.42f, unit * 0.47f, unit * 0.34f), new Vector3(unit * 0.26f, unit * 0.08f, unit * 0.26f), green, false);
            CreateCube("Alchemist Moon Sign", parent, center + new Vector3(unit * 0.62f, unit * 1.02f, unit * -0.32f), new Vector3(unit * 0.08f, unit * 0.28f, unit * 0.28f), purple, false);
        }

        public static void AddTavernDetails(Transform parent, Vector3 center, float unit)
        {
            var wood = CreateMaterial("Tavern Detail Wood", new Color(0.34f, 0.18f, 0.08f));
            var gold = CreateMaterial("Tavern Detail Gold", new Color(0.9f, 0.62f, 0.16f));
            var foam = CreateMaterial("Tavern Detail Foam", new Color(0.95f, 0.88f, 0.7f));
            var red = CreateMaterial("Tavern Detail Red", new Color(0.54f, 0.08f, 0.05f));

            CreateCube("Tavern Beer Sign Mug", parent, center + new Vector3(unit * 0.64f, unit * 0.96f, unit * -0.3f), new Vector3(unit * 0.08f, unit * 0.3f, unit * 0.22f), gold, false);
            CreateCube("Tavern Beer Sign Foam", parent, center + new Vector3(unit * 0.69f, unit * 1.13f, unit * -0.3f), new Vector3(unit * 0.06f, unit * 0.08f, unit * 0.26f), foam, false);
            CreateCube("Tavern Barrel", parent, center + new Vector3(unit * -0.48f, unit * 0.25f, unit * 0.28f), new Vector3(unit * 0.3f, unit * 0.36f, unit * 0.3f), wood, false);
            CreateCube("Tavern Barrel Band", parent, center + new Vector3(unit * -0.48f, unit * 0.26f, unit * 0.28f), new Vector3(unit * 0.34f, unit * 0.06f, unit * 0.34f), gold, false);
            CreateCube("Tavern Outdoor Table", parent, center + new Vector3(unit * 0.12f, unit * 0.2f, unit * 0.46f), new Vector3(unit * 0.48f, unit * 0.08f, unit * 0.34f), wood, false);
            CreateCube("Tavern Awning", parent, center + new Vector3(unit * 0.62f, unit * 0.72f, unit * 0.24f), new Vector3(unit * 0.08f, unit * 0.18f, unit * 0.58f), red, false);
        }

        private static void CreateBottle(string name, Transform parent, Vector3 position, float unit, Material material)
        {
            var bottle = GameObject.CreatePrimitive(VoxelVisuals.ResolvePrimitive(PrimitiveType.Sphere, name));
            bottle.name = name;
            bottle.transform.SetParent(parent, false);
            bottle.transform.position = position;
            bottle.transform.localScale = new Vector3(unit * 0.12f, unit * 0.18f, unit * 0.12f);
            bottle.GetComponent<Renderer>().sharedMaterial = material;
            RemoveCollider(bottle);
            VoxelVisuals.ApplyBlockStyle(bottle, PrimitiveType.Sphere, material, false);
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
