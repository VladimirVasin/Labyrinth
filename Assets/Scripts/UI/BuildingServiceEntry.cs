namespace Labyrinth.UI
{
    public readonly struct BuildingServiceEntry
    {
        public BuildingServiceEntry(
            string title,
            string price,
            string description,
            string levelText = "",
            string actionLabel = "",
            bool actionEnabled = true)
        {
            Title = title;
            Price = price;
            Description = description;
            LevelText = levelText ?? string.Empty;
            ActionLabel = actionLabel ?? string.Empty;
            ActionEnabled = actionEnabled;
        }

        public string Title { get; }

        public string Price { get; }

        public string Description { get; }

        public string LevelText { get; }

        public string ActionLabel { get; }

        public bool ActionEnabled { get; }
    }
}
