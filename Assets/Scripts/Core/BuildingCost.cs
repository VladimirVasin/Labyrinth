namespace Labyrinth.Core
{
    public readonly struct BuildingCost
    {
        public BuildingCost(int gold, int wood, int food = 0, int iron = 0)
        {
            Gold = gold;
            Wood = wood;
            Food = food;
            Iron = iron;
        }

        public int Food { get; }

        public int Gold { get; }

        public int Wood { get; }

        public int Iron { get; }

        public bool IsFree => Food <= 0 && Gold <= 0 && Wood <= 0 && Iron <= 0;

        public string Format()
        {
            if (IsFree)
            {
                return "бесплатно";
            }

            var text = string.Empty;
            AppendPart(ref text, Gold, "зол.");
            AppendPart(ref text, Food, "пищи");
            AppendPart(ref text, Wood, "дер.");
            AppendPart(ref text, Iron, "жел.");
            return text;
        }

        private static void AppendPart(ref string text, int amount, string label)
        {
            if (amount <= 0)
            {
                return;
            }

            if (!string.IsNullOrEmpty(text))
            {
                text += ", ";
            }

            text += $"{amount} {label}";
        }

        public override string ToString()
        {
            return Format();
        }
    }
}
