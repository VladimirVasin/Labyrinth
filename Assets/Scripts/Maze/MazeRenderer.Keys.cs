using Labyrinth.Core;
using Labyrinth.Hero;
using UnityEngine;

namespace Labyrinth.Maze
{
    public sealed partial class MazeRenderer
    {
        private void RenderKeyPickups(MazeGenerationResult result)
        {
            if (result == null || result.KeyPickups == null)
            {
                return;
            }

            for (var i = 0; i < result.KeyPickups.Count; i++)
            {
                RenderKeyPickup(result.KeyPickups[i]);
            }
        }

        public void RenderKeyPickup(KeyPickupModel key)
        {
            if (key == null || !key.IsAvailable || root == null)
            {
                return;
            }

            var keyRoot = new GameObject($"{key.ItemName} {key.Position.x},{key.Position.y}");
            keyRoot.transform.SetParent(root, false);
            var position = GridToWorld(key.Position);
            keyRoot.transform.position = position;

            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Key Head";
            head.transform.SetParent(keyRoot.transform, false);
            head.transform.position = position + new Vector3(-cellSize * 0.14f, Scale(0.11f), 0f);
            head.transform.localScale = new Vector3(cellSize * 0.18f, Scale(0.06f), cellSize * 0.18f);
            head.GetComponent<Renderer>().sharedMaterial = keyGoldMaterial;
            RemoveCollider(head);
            TrackCellRenderer(key.Position, head);

            var shaft = CreateCube(
                "Key Shaft",
                position + new Vector3(cellSize * 0.12f, Scale(0.11f), 0f),
                new Vector3(cellSize * 0.36f, Scale(0.05f), cellSize * 0.07f),
                keyGoldMaterial,
                keyRoot.transform,
                false);
            TrackCellRenderer(key.Position, shaft);

            var tooth = CreateCube(
                "Key Tooth",
                position + new Vector3(cellSize * 0.3f, Scale(0.11f), cellSize * 0.07f),
                new Vector3(cellSize * 0.08f, Scale(0.05f), cellSize * 0.16f),
                keyGoldMaterial,
                keyRoot.transform,
                false);
            TrackCellRenderer(key.Position, tooth);

            var hudTarget = keyRoot.AddComponent<ObjectMicroHudTarget>();
            hudTarget.Configure(
                key.ItemName,
                "ключ",
                "Ключ",
                key.Position,
                new Color(1f, 0.74f, 0.16f),
                () => key.IsAvailable ? "лежит в лабиринте" : "у рыцаря",
                () => BuildKeyPickupHover(key));
            var collider = keyRoot.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, Scale(0.12f), 0f);
            collider.size = new Vector3(cellSize * 0.62f, Scale(0.28f), cellSize * 0.42f);

            key.AttachVisual(keyRoot);
        }

        private static string BuildKeyPickupHover(KeyPickupModel key)
        {
            if (key == null)
            {
                return "-";
            }

            return key.ItemName == HeroInventory.DescentKeyItemName
                ? HeroInventory.DescentKeyHoverInfo
                : "Открывает входную дверь центральной комнаты.";
        }
    }
}
