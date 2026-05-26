namespace Labyrinth.Hero
{
    public readonly struct HeroBlessingDefinition
    {
        public HeroBlessingDefinition(
            HeroBlessingType type,
            string displayName,
            string description,
            int goldCost)
        {
            Type = type;
            DisplayName = displayName;
            Description = description;
            GoldCost = goldCost;
        }

        public HeroBlessingType Type { get; }

        public string DisplayName { get; }

        public string Description { get; }

        public int GoldCost { get; }
    }
}
