using UnityEngine;

namespace Labyrinth.Core
{
    public enum HeroDeathTokenState
    {
        Available,
        Carried,
        Delivered
    }

    public sealed class HeroDeathTokenModel
    {
        private GameObject visualObject;

        public HeroDeathTokenModel(
            int id,
            int heroNumber,
            string fallenHeroName,
            int levelNumber,
            Vector2Int position,
            Vector2Int housePosition)
        {
            Id = id;
            HeroNumber = heroNumber;
            FallenHeroName = string.IsNullOrWhiteSpace(fallenHeroName) ? $"Рыцарь {heroNumber}" : fallenHeroName;
            LevelNumber = levelNumber;
            Position = position;
            HousePosition = housePosition;
            State = HeroDeathTokenState.Available;
        }

        public int Id { get; }

        public int HeroNumber { get; }

        public string FallenHeroName { get; }

        public int LevelNumber { get; private set; }

        public Vector2Int Position { get; private set; }

        public Vector2Int HousePosition { get; }

        public HeroDeathTokenState State { get; private set; }

        public string ItemName => Labyrinth.Hero.HeroInventory.BuildDeathTokenItemName(Id);

        public bool IsAvailable => State == HeroDeathTokenState.Available;

        public bool IsDelivered => State == HeroDeathTokenState.Delivered;

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
            State = HeroDeathTokenState.Carried;
            SetVisualActive(false);
        }

        public void Deliver()
        {
            State = HeroDeathTokenState.Delivered;
            SetVisualActive(false);
        }

        public void Drop(Vector2Int position, int levelNumber)
        {
            Position = position;
            LevelNumber = levelNumber;
            State = HeroDeathTokenState.Available;
            DestroyVisual();
        }

        public void DestroyVisual()
        {
            if (visualObject != null)
            {
                Object.Destroy(visualObject);
            }

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
