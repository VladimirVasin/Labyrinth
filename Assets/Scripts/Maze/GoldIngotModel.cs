using UnityEngine;

namespace Labyrinth.Maze
{
    public enum GoldIngotState
    {
        Available,
        Carried,
        Delivered
    }

    public sealed class GoldIngotModel
    {
        private GameObject visualObject;

        public GoldIngotModel(int id, Vector2Int position)
        {
            Id = id;
            Position = position;
            State = GoldIngotState.Available;
        }

        public int Id { get; }

        public Vector2Int Position { get; private set; }

        public GoldIngotState State { get; private set; }

        public bool IsAvailable => State == GoldIngotState.Available;

        public void AttachVisual(GameObject visual)
        {
            visualObject = visual;
            if (visualObject != null)
            {
                visualObject.SetActive(IsAvailable);
            }
        }

        public void PickUp()
        {
            State = GoldIngotState.Carried;
            SetVisualActive(false);
        }

        public void Deliver()
        {
            State = GoldIngotState.Delivered;
            SetVisualActive(false);
        }

        public void Drop(Vector2Int position)
        {
            Position = position;
            State = GoldIngotState.Available;
            SetVisualActive(false);
            visualObject = null;
        }

        private void SetVisualActive(bool active)
        {
            if (visualObject != null)
            {
                visualObject.SetActive(active);
            }
        }
    }
}
