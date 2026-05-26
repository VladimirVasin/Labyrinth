using Labyrinth.Core;
using UnityEngine;

namespace Labyrinth.Maze
{
    public static class OreDepositRenderer
    {
        public static void Render(MazeRenderer renderer, MazeGenerationResult result)
        {
            if (renderer == null || renderer.ContentRoot == null || result == null || result.OreDeposits == null)
            {
                return;
            }

            var ironRock = CreateMaterial("Ore Iron Rock", new Color(0.18f, 0.21f, 0.24f));
            var ironVein = CreateMaterial("Ore Iron Vein", new Color(0.5f, 0.58f, 0.66f));
            var goldRock = CreateMaterial("Ore Gold Rock", new Color(0.23f, 0.2f, 0.14f));
            var goldVein = CreateMaterial("Ore Gold Vein", new Color(1f, 0.72f, 0.14f));

            foreach (var deposit in result.OreDeposits)
            {
                RenderDeposit(
                    renderer,
                    deposit,
                    deposit.Type == OreDepositType.Iron ? ironRock : goldRock,
                    deposit.Type == OreDepositType.Iron ? ironVein : goldVein);
            }
        }

        private static void RenderDeposit(
            MazeRenderer renderer,
            OreDepositModel deposit,
            Material rockMaterial,
            Material veinMaterial)
        {
            if (deposit == null || deposit.IsDepleted)
            {
                return;
            }

            var root = new GameObject($"{deposit.Type} Ore Deposit {deposit.Cave.Center.x},{deposit.Cave.Center.y}");
            root.transform.SetParent(renderer.ContentRoot, false);

            for (var i = 0; i < deposit.Cells.Count; i++)
            {
                RenderOreCell(renderer, root.transform, deposit, deposit.Cells[i], i, rockMaterial, veinMaterial);
            }
        }

        private static void RenderOreCell(
            MazeRenderer renderer,
            Transform parent,
            OreDepositModel deposit,
            Vector2Int cell,
            int index,
            Material rockMaterial,
            Material veinMaterial)
        {
            var cellRoot = new GameObject($"Ore Cell {cell.x},{cell.y}");
            cellRoot.transform.SetParent(parent, false);

            var center = renderer.GridToWorld(cell);
            var unit = renderer.CellSize;
            cellRoot.transform.position = center;
            var offset = BuildOffset(cell, index, unit);
            var rockCenter = center + offset + new Vector3(0f, unit * 0.075f, 0f);
            var rock = CreatePrimitive(
                "Ore Rock",
                PrimitiveType.Sphere,
                cellRoot.transform,
                rockCenter,
                new Vector3(unit * 0.24f, unit * 0.13f, unit * 0.19f),
                rockMaterial);
            rock.transform.rotation = Quaternion.Euler(0f, BuildAngle(cell, index), 0f);

            CreatePrimitive(
                "Ore Vein",
                PrimitiveType.Cube,
                cellRoot.transform,
                rockCenter + new Vector3(unit * 0.01f, unit * 0.075f, unit * 0.01f),
                new Vector3(unit * 0.2f, unit * 0.025f, unit * 0.035f),
                veinMaterial).transform.rotation = Quaternion.Euler(0f, BuildAngle(cell, index) + 28f, 0f);

            if ((cell.x + cell.y + index) % 2 == 0)
            {
                CreatePrimitive(
                    "Ore Pebble",
                    PrimitiveType.Sphere,
                    cellRoot.transform,
                    center - offset * 0.45f + new Vector3(0f, unit * 0.04f, 0f),
                    new Vector3(unit * 0.12f, unit * 0.07f, unit * 0.1f),
                    rockMaterial);
            }

            var hudTarget = cellRoot.AddComponent<ObjectMicroHudTarget>();
            hudTarget.Configure(
                deposit.Type == OreDepositType.Iron ? "Железная жила" : "Золотая жила",
                "залежи руды",
                "Ресурс",
                cell,
                deposit.Type == OreDepositType.Iron ? new Color(0.6f, 0.66f, 0.72f) : new Color(1f, 0.72f, 0.14f),
                () => deposit.IsDepleted ? "истощена" : "не добывается",
                () => "Пока это разведочный ресурс без добычи. Позже его смогут использовать городские здания.");
            var collider = cellRoot.AddComponent<BoxCollider>();
            collider.center = offset + new Vector3(0f, unit * 0.12f, 0f);
            collider.size = new Vector3(unit * 0.52f, unit * 0.3f, unit * 0.48f);
            renderer.TrackExternalCellRenderer(cell, cellRoot);
        }

        private static Vector3 BuildOffset(Vector2Int cell, int index, float unit)
        {
            var seed = cell.x * 37 + cell.y * 53 + index * 97;
            var x = (((seed * 17) % 100) / 100f - 0.5f) * unit * 0.36f;
            var z = (((seed * 29) % 100) / 100f - 0.5f) * unit * 0.36f;
            return new Vector3(x, 0f, z);
        }

        private static float BuildAngle(Vector2Int cell, int index)
        {
            return (cell.x * 43 + cell.y * 71 + index * 31) % 360;
        }

        private static GameObject CreatePrimitive(
            string name,
            PrimitiveType primitive,
            Transform parent,
            Vector3 position,
            Vector3 scale,
            Material material)
        {
            var obj = GameObject.CreatePrimitive(primitive);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.position = position;
            obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = material;

            var collider = obj.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }

            return obj;
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
    }
}
