namespace Labyrinth.Core
{
    public sealed class ResourceWallet
    {
        private const int DefaultFood = 50;
        private const int DefaultGold = 75;
        private const int DefaultWood = 5;
        private const int DefaultIron = 0;

        public ResourceWallet(int food, int gold, int wood, int iron)
        {
            Food = food;
            Gold = gold;
            Wood = wood;
            Iron = iron;
        }

        public int Food { get; private set; }

        public int Gold { get; private set; }

        public int Wood { get; private set; }

        public int Iron { get; private set; }

        public void AddFood(int amount)
        {
            Food += amount;
        }

        public void AddGold(int amount)
        {
            Gold += amount;
        }

        public void AddWood(int amount)
        {
            Wood += amount;
        }

        public void AddIron(int amount)
        {
            Iron += amount;
        }

        public bool CanSpendGold(int amount)
        {
            return Gold >= amount;
        }

        public bool CanSpendFood(int amount)
        {
            return Food >= amount;
        }

        public bool CanSpendWood(int amount)
        {
            return Wood >= amount;
        }

        public bool CanSpendIron(int amount)
        {
            return Iron >= amount;
        }

        public bool CanAfford(BuildingCost cost)
        {
            return CanSpendFood(cost.Food)
                && CanSpendGold(cost.Gold)
                && CanSpendWood(cost.Wood)
                && CanSpendIron(cost.Iron);
        }

        public bool TrySpendGold(int amount)
        {
            if (!CanSpendGold(amount))
            {
                return false;
            }

            Gold -= amount;
            return true;
        }

        public bool TrySpendFood(int amount)
        {
            if (!CanSpendFood(amount))
            {
                return false;
            }

            Food -= amount;
            return true;
        }

        public bool TrySpendWood(int amount)
        {
            if (!CanSpendWood(amount))
            {
                return false;
            }

            Wood -= amount;
            return true;
        }

        public bool TrySpendIron(int amount)
        {
            if (!CanSpendIron(amount))
            {
                return false;
            }

            Iron -= amount;
            return true;
        }

        public bool TrySpend(BuildingCost cost)
        {
            if (!CanAfford(cost))
            {
                return false;
            }

            Food -= cost.Food;
            Gold -= cost.Gold;
            Wood -= cost.Wood;
            Iron -= cost.Iron;
            return true;
        }

        public void ResetToDefault()
        {
            Food = DefaultFood;
            Gold = DefaultGold;
            Wood = DefaultWood;
            Iron = DefaultIron;
        }

        public static ResourceWallet CreateDefault()
        {
            return new ResourceWallet(DefaultFood, DefaultGold, DefaultWood, DefaultIron);
        }
    }
}
