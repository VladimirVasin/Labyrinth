using Labyrinth.Mobs;

namespace Labyrinth.Core
{
    public enum HeroGuildQuestState
    {
        Available,
        Accepted,
        CompletedPendingReward
    }

    public readonly struct HeroGuildQuestHudInfo
    {
        public HeroGuildQuestHudInfo(
            bool hasQuest,
            string target,
            string progress,
            string reward,
            string state,
            string tooltip)
        {
            HasQuest = hasQuest;
            Target = target ?? string.Empty;
            Progress = progress ?? string.Empty;
            Reward = reward ?? string.Empty;
            State = state ?? string.Empty;
            Tooltip = tooltip ?? string.Empty;
        }

        public bool HasQuest { get; }

        public string Target { get; }

        public string Progress { get; }

        public string Reward { get; }

        public string State { get; }

        public string Tooltip { get; }

        public static HeroGuildQuestHudInfo None => new HeroGuildQuestHudInfo(false, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
    }

    public sealed class HeroGuildQuestModel
    {
        public HeroGuildQuestModel(int id, MobSpecies targetSpecies, int targetCount, int rewardGold)
        {
            Id = id;
            TargetSpecies = targetSpecies;
            TargetCount = targetCount < 1 ? 1 : targetCount;
            RewardGold = rewardGold < 0 ? 0 : rewardGold;
            State = HeroGuildQuestState.Available;
        }

        public int Id { get; }

        public MobSpecies TargetSpecies { get; }

        public int TargetCount { get; }

        public int Progress { get; private set; }

        public int RewardGold { get; }

        public int AssignedHeroNumber { get; private set; }

        public string AssignedHeroName { get; private set; } = string.Empty;

        public HeroGuildQuestState State { get; private set; }

        public bool IsActiveForHero(int heroNumber)
        {
            return AssignedHeroNumber == heroNumber
                && (State == HeroGuildQuestState.Accepted || State == HeroGuildQuestState.CompletedPendingReward);
        }

        public void Assign(int heroNumber, string heroName)
        {
            AssignedHeroNumber = heroNumber;
            AssignedHeroName = string.IsNullOrWhiteSpace(heroName) ? $"Рыцарь {heroNumber}" : heroName;
            State = HeroGuildQuestState.Accepted;
        }

        public bool RegisterKill(MobSpecies species)
        {
            if (State != HeroGuildQuestState.Accepted || species != TargetSpecies)
            {
                return false;
            }

            Progress++;
            if (Progress >= TargetCount)
            {
                Progress = TargetCount;
                State = HeroGuildQuestState.CompletedPendingReward;
            }

            return true;
        }

        public string FormatProgress()
        {
            return $"{Progress}/{TargetCount}";
        }
    }
}
