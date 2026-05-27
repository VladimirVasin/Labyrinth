using UnityEngine;

namespace Labyrinth.Mobs
{
    public enum MobSpecies
    {
        Orc,
        Goblin,
        Rat
    }

    public enum MobRank
    {
        Regular,
        MiniBoss,
        Boss
    }

    public sealed class MobModel
    {
        public MobModel(Vector2Int startPosition, MobSpecies species, MobRank rank, int dungeonLevel = 1)
        {
            Species = species;
            Rank = rank;
            DungeonLevel = Mathf.Max(1, dungeonLevel);
            Position = startPosition;
            var stats = BuildStats(species, rank, DungeonLevel);
            MaxHitPoints = stats.MaxHitPoints;
            HitPoints = MaxHitPoints;
            AttackPoints = stats.AttackPoints;
            ArmorPoints = stats.ArmorPoints;
            State = MobState.Wandering;
        }

        public MobSpecies Species { get; }

        public MobRank Rank { get; }

        public int DungeonLevel { get; }

        public bool IsMiniBoss => Rank == MobRank.MiniBoss;

        public bool IsBoss => Rank == MobRank.Boss;

        public Vector2Int Position { get; private set; }

        public int MaxHitPoints { get; }

        public int HitPoints { get; private set; }

        public int AttackPoints { get; }

        public int ArmorPoints { get; }

        public MobState State { get; private set; }

        public bool SpawnedFromDarkness { get; private set; }

        public bool IsAlive => HitPoints > 0;

        public void SetPosition(Vector2Int position)
        {
            Position = position;
        }

        public int ReceiveDamage(int incomingDamage)
        {
            var damage = Mathf.Max(1, incomingDamage - ArmorPoints);
            HitPoints = Mathf.Max(0, HitPoints - damage);
            if (HitPoints <= 0)
            {
                State = MobState.Defeated;
            }

            return damage;
        }

        public void SetState(MobState state)
        {
            State = state;
        }

        public void MarkSpawnedFromDarkness()
        {
            SpawnedFromDarkness = true;
        }

        private static MobStats BuildStats(MobSpecies species, MobRank rank, int dungeonLevel)
        {
            MobStats stats;
            if (rank == MobRank.MiniBoss)
            {
                stats = BuildMiniBossStats(species);
            }
            else if (rank == MobRank.Boss)
            {
                stats = BuildBossStats(species);
            }
            else
            {
                stats = BuildRegularStats(species);
            }

            return ApplyDungeonLevelMultiplier(stats, dungeonLevel);
        }

        private static MobStats BuildMiniBossStats(MobSpecies species)
        {
            switch (species)
            {
                case MobSpecies.Rat:
                    return new MobStats(30, 5, 1);
                case MobSpecies.Goblin:
                    return new MobStats(48, 7, 3);
                case MobSpecies.Orc:
                default:
                    return new MobStats(72, 10, 4);
            }
        }

        private static MobStats BuildBossStats(MobSpecies species)
        {
            switch (species)
            {
                case MobSpecies.Rat:
                    return new MobStats(78, 8, 3);
                case MobSpecies.Goblin:
                    return new MobStats(104, 9, 5);
                case MobSpecies.Orc:
                default:
                    return new MobStats(142, 12, 6);
            }
        }

        private static MobStats BuildRegularStats(MobSpecies species)
        {
            switch (species)
            {
                case MobSpecies.Orc:
                    return new MobStats(38, 8, 3);
                case MobSpecies.Goblin:
                    return new MobStats(18, 5, 1);
                case MobSpecies.Rat:
                    return new MobStats(9, 3, 0);
                default:
                    return new MobStats(18, 5, 1);
            }
        }

        private static MobStats ApplyDungeonLevelMultiplier(MobStats stats, int dungeonLevel)
        {
            if (dungeonLevel < 2)
            {
                return stats;
            }

            return new MobStats(
                stats.MaxHitPoints * 2,
                stats.AttackPoints * 2,
                stats.ArmorPoints * 2);
        }

        private readonly struct MobStats
        {
            public MobStats(int maxHitPoints, int attackPoints, int armorPoints)
            {
                MaxHitPoints = maxHitPoints;
                AttackPoints = attackPoints;
                ArmorPoints = armorPoints;
            }

            public int MaxHitPoints { get; }

            public int AttackPoints { get; }

            public int ArmorPoints { get; }
        }
    }
}
