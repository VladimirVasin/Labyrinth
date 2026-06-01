using Labyrinth.Core;
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
        public MobModel(Vector2Int startPosition, MobSpecies species, MobRank rank, int dungeonLevel = 1, int statSeed = 0, bool useOpeningSpawnStats = false)
        {
            Species = species;
            Rank = rank;
            DungeonLevel = Mathf.Max(1, dungeonLevel);
            Level = BuildDisplayLevel(species, rank, DungeonLevel, useOpeningSpawnStats);
            Position = startPosition;
            var stats = BuildStats(species, rank, DungeonLevel, statSeed, useOpeningSpawnStats);
            MaxHitPoints = stats.MaxHitPoints;
            HitPoints = MaxHitPoints;
            AttackPoints = stats.AttackPoints;
            ArmorPoints = stats.ArmorPoints;
            State = MobState.Wandering;
        }

        public MobSpecies Species { get; }

        public MobRank Rank { get; }

        public int DungeonLevel { get; }

        public int Level { get; }

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
            return ApplyDamage(damage);
        }

        public int ReceiveResolvedDamage(int resolvedDamage)
        {
            var damage = Mathf.Max(0, resolvedDamage);
            if (damage <= 0)
            {
                return 0;
            }

            return ApplyDamage(damage);
        }

        private int ApplyDamage(int damage)
        {
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

        private static MobStats BuildStats(MobSpecies species, MobRank rank, int dungeonLevel, int statSeed, bool useOpeningSpawnStats)
        {
            var random = new System.Random(statSeed);
            MobStats stats;
            if (rank == MobRank.MiniBoss)
            {
                stats = BuildMiniBossStats(species).Roll(random);
            }
            else if (rank == MobRank.Boss)
            {
                stats = BuildBossStats(species).Roll(random);
            }
            else
            {
                stats = useOpeningSpawnStats && dungeonLevel <= 1
                    ? BuildOpeningRegularStats(species).Roll(random)
                    : BuildRegularStats(species).Roll(random);
            }

            return ApplyDungeonLevelMultiplier(stats, dungeonLevel);
        }

        private static int BuildDisplayLevel(MobSpecies species, MobRank rank, int dungeonLevel, bool useOpeningSpawnStats)
        {
            var baseLevel = BuildBaseDisplayLevel(species, rank, useOpeningSpawnStats && dungeonLevel <= 1);
            return baseLevel + Mathf.Max(0, dungeonLevel - 1) * 6;
        }

        private static int BuildBaseDisplayLevel(MobSpecies species, MobRank rank, bool useOpeningSpawnStats)
        {
            switch (rank)
            {
                case MobRank.Boss:
                    switch (species)
                    {
                        case MobSpecies.Rat:
                            return 16;
                        case MobSpecies.Goblin:
                            return 20;
                        case MobSpecies.Orc:
                        default:
                            return 26;
                    }
                case MobRank.MiniBoss:
                    switch (species)
                    {
                        case MobSpecies.Rat:
                            return 7;
                        case MobSpecies.Goblin:
                            return 10;
                        case MobSpecies.Orc:
                        default:
                            return 14;
                    }
                default:
                    if (useOpeningSpawnStats)
                    {
                        switch (species)
                        {
                            case MobSpecies.Orc:
                                return 3;
                            case MobSpecies.Goblin:
                            case MobSpecies.Rat:
                            default:
                                return 1;
                        }
                    }

                    switch (species)
                    {
                        case MobSpecies.Rat:
                            return 2;
                        case MobSpecies.Goblin:
                            return 4;
                        case MobSpecies.Orc:
                        default:
                            return 8;
                    }
            }
        }

        private static MobStatRange BuildMiniBossStats(MobSpecies species)
        {
            switch (species)
            {
                case MobSpecies.Rat:
                    return new MobStatRange(new IntRange(44, 54), new IntRange(6, 8), new IntRange(1, 3));
                case MobSpecies.Goblin:
                    return new MobStatRange(new IntRange(62, 76), new IntRange(8, 11), new IntRange(3, 5));
                case MobSpecies.Orc:
                default:
                    return new MobStatRange(new IntRange(88, 108), new IntRange(12, 15), new IntRange(5, 7));
            }
        }

        private static MobStatRange BuildBossStats(MobSpecies species)
        {
            switch (species)
            {
                case MobSpecies.Rat:
                    return new MobStatRange(new IntRange(100, 122), new IntRange(10, 13), new IntRange(4, 6));
                case MobSpecies.Goblin:
                    return new MobStatRange(new IntRange(132, 160), new IntRange(12, 15), new IntRange(6, 8));
                case MobSpecies.Orc:
                default:
                    return new MobStatRange(new IntRange(178, 215), new IntRange(15, 18), new IntRange(8, 10));
            }
        }

        private static MobStatRange BuildRegularStats(MobSpecies species)
        {
            switch (species)
            {
                case MobSpecies.Orc:
                    return new MobStatRange(new IntRange(48, 60), new IntRange(7, 9), new IntRange(3, 4));
                case MobSpecies.Goblin:
                    return new MobStatRange(new IntRange(24, 30), new IntRange(4, 6), new IntRange(1, 2));
                case MobSpecies.Rat:
                    return new MobStatRange(new IntRange(15, 20), new IntRange(2, 4), new IntRange(0, 1));
                default:
                    return new MobStatRange(new IntRange(24, 30), new IntRange(4, 6), new IntRange(1, 2));
            }
        }

        private static MobStatRange BuildOpeningRegularStats(MobSpecies species)
        {
            switch (species)
            {
                case MobSpecies.Orc:
                    return new MobStatRange(new IntRange(30, 38), new IntRange(4, 5), new IntRange(1, 2));
                case MobSpecies.Goblin:
                    return new MobStatRange(new IntRange(14, 18), new IntRange(2, 3), new IntRange(0, 1));
                case MobSpecies.Rat:
                    return new MobStatRange(new IntRange(7, 10), new IntRange(1, 2), new IntRange(0, 0));
                default:
                    return new MobStatRange(new IntRange(14, 18), new IntRange(2, 3), new IntRange(0, 1));
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

        private readonly struct MobStatRange
        {
            public MobStatRange(IntRange maxHitPoints, IntRange attackPoints, IntRange armorPoints)
            {
                MaxHitPoints = maxHitPoints;
                AttackPoints = attackPoints;
                ArmorPoints = armorPoints;
            }

            private IntRange MaxHitPoints { get; }

            private IntRange AttackPoints { get; }

            private IntRange ArmorPoints { get; }

            public MobStats Roll(System.Random random)
            {
                return new MobStats(
                    MaxHitPoints.Roll(random),
                    AttackPoints.Roll(random),
                    ArmorPoints.Roll(random));
            }
        }
    }
}
