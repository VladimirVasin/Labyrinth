using System.Collections.Generic;
using Labyrinth.Hero;
using Labyrinth.Maze;
using UnityEngine;

namespace Labyrinth.Mobs
{
    public sealed partial class MobManager
    {
        private enum MobThreatStage
        {
            Early,
            RatFade,
            GoblinCore,
            OrcRise,
            OrcDominant
        }

        private static List<MobSpecies> SelectRegularMobSpecies(
            IReadOnlyList<Vector2Int> spawnPositions,
            Dictionary<Vector2Int, int> distancesFromEntrance,
            int maxDistanceFromEntrance,
            System.Random random,
            bool useOpeningSpawnRules)
        {
            var species = new List<MobSpecies>(spawnPositions.Count);
            for (var i = 0; i < spawnPositions.Count; i++)
            {
                species.Add(SelectRegularMobSpecies(
                    spawnPositions[i],
                    distancesFromEntrance,
                    maxDistanceFromEntrance,
                    random,
                    useOpeningSpawnRules));
            }

            if (species.Count == 0)
            {
                return species;
            }

            if (!species.Contains(MobSpecies.Goblin))
            {
                species[FindClosestToEntranceIndex(spawnPositions, distancesFromEntrance)] = MobSpecies.Goblin;
            }

            if (!useOpeningSpawnRules && species.Count >= 10 && !species.Contains(MobSpecies.Orc))
            {
                var farthestIndex = FindFarthestFromEntranceIndex(spawnPositions, distancesFromEntrance);
                if (IsDeepEnoughForInitialOrc(spawnPositions[farthestIndex], distancesFromEntrance, maxDistanceFromEntrance, 0.62f))
                {
                    species[farthestIndex] = MobSpecies.Orc;
                }
            }

            return species;
        }

        private static MobSpecies SelectRegularMobSpecies(
            Vector2Int position,
            Dictionary<Vector2Int, int> distancesFromEntrance,
            int maxDistanceFromEntrance,
            System.Random random,
            bool useOpeningSpawnRules)
        {
            if (maxDistanceFromEntrance <= 0 || !distancesFromEntrance.TryGetValue(position, out var distance))
            {
                return useOpeningSpawnRules ? MobSpecies.Goblin : MobSpecies.Orc;
            }

            var distanceRatio = distance / (float)maxDistanceFromEntrance;
            if (useOpeningSpawnRules)
            {
                if (distanceRatio <= 0.82f)
                {
                    return MobSpecies.Goblin;
                }

                return random.NextDouble() < 0.94 ? MobSpecies.Goblin : MobSpecies.Orc;
            }

            var goblinChance = distanceRatio <= 0.55f
                ? 0.96
                : distanceRatio <= 0.78f
                    ? 0.84
                    : 0.58;
            return random.NextDouble() < goblinChance ? MobSpecies.Goblin : MobSpecies.Orc;
        }

        private static MobSpecies SelectDarkRespawnSpecies(System.Random random, MobThreatStage stage)
        {
            var weights = GetRespawnWeights(stage);
            var roll = random.Next(weights.Rat + weights.Goblin + weights.Orc);
            if (roll < weights.Rat)
            {
                return MobSpecies.Rat;
            }

            if (roll < weights.Rat + weights.Goblin)
            {
                return MobSpecies.Goblin;
            }

            return MobSpecies.Orc;
        }

        private static MobSpecies[] GetRespawnFallbackOrder(MobThreatStage stage)
        {
            switch (stage)
            {
                case MobThreatStage.Early:
                    return new[] { MobSpecies.Rat, MobSpecies.Goblin, MobSpecies.Orc };
                case MobThreatStage.RatFade:
                    return new[] { MobSpecies.Rat, MobSpecies.Goblin, MobSpecies.Orc };
                case MobThreatStage.GoblinCore:
                    return new[] { MobSpecies.Rat, MobSpecies.Goblin, MobSpecies.Orc };
                case MobThreatStage.OrcRise:
                    return new[] { MobSpecies.Rat, MobSpecies.Goblin, MobSpecies.Orc };
                case MobThreatStage.OrcDominant:
                default:
                    return new[] { MobSpecies.Rat, MobSpecies.Goblin, MobSpecies.Orc };
            }
        }

        private static RespawnWeights GetRespawnWeights(MobThreatStage stage)
        {
            switch (stage)
            {
                case MobThreatStage.Early:
                    return new RespawnWeights(85, 14, 1);
                case MobThreatStage.RatFade:
                    return new RespawnWeights(74, 23, 3);
                case MobThreatStage.GoblinCore:
                    return new RespawnWeights(62, 32, 6);
                case MobThreatStage.OrcRise:
                    return new RespawnWeights(50, 39, 11);
                case MobThreatStage.OrcDominant:
                default:
                    return new RespawnWeights(42, 42, 16);
            }
        }

        private int CalculateEffectiveRegularMobTarget(IReadOnlyList<HeroController> activeHeroes)
        {
            if (regularMobTargetCount <= MinimumRegularMobCount)
            {
                return regularMobTargetCount;
            }

            if (activeHeroes == null || activeHeroes.Count == 0)
            {
                return Mathf.Min(regularMobTargetCount, MinimumRegularMobCount + 2);
            }

            var aliveCount = 0;
            var highestLevel = 1;
            var highestMemory = 0;
            for (var i = 0; i < activeHeroes.Count; i++)
            {
                var hero = activeHeroes[i];
                if (hero == null || hero.Model == null || !hero.Model.IsAlive)
                {
                    continue;
                }

                aliveCount++;
                highestLevel = Mathf.Max(highestLevel, hero.Model.Level);
                highestMemory = Mathf.Max(highestMemory, hero.Model.Memory.RememberedCount);
            }

            if (aliveCount <= 0)
            {
                return Mathf.Min(regularMobTargetCount, MinimumRegularMobCount + 2);
            }

            var knownCellBudget = distancesFromEntrance != null && distancesFromEntrance.Count > 0
                ? distancesFromEntrance.Count
                : Mathf.Max(highestMemory, 1);
            var explorationRatio = Mathf.Clamp01(highestMemory / (float)Mathf.Max(1, knownCellBudget));
            var progressTarget = MinimumRegularMobCount
                + Mathf.RoundToInt((regularMobTargetCount - MinimumRegularMobCount) * Mathf.Clamp01(0.18f + explorationRatio * 0.82f));
            var heroTarget = MinimumRegularMobCount + aliveCount + highestLevel * 2;
            return Mathf.Clamp(Mathf.Max(progressTarget, heroTarget), MinimumRegularMobCount, regularMobTargetCount);
        }

        private static MobThreatStage CalculateThreatStage(IReadOnlyList<HeroController> activeHeroes)
        {
            if (activeHeroes == null || activeHeroes.Count == 0)
            {
                return MobThreatStage.Early;
            }

            var aliveCount = 0;
            var totalLevel = 0;
            var highestLevel = 1;
            for (var i = 0; i < activeHeroes.Count; i++)
            {
                var hero = activeHeroes[i];
                if (hero == null || hero.Model == null || !hero.Model.IsAlive)
                {
                    continue;
                }

                aliveCount++;
                totalLevel += hero.Model.Level;
                highestLevel = Mathf.Max(highestLevel, hero.Model.Level);
            }

            if (aliveCount == 0)
            {
                return MobThreatStage.Early;
            }

            var averageLevel = totalLevel / (float)aliveCount;
            var threatLevel = Mathf.Max(averageLevel, highestLevel - 2f);
            if (threatLevel <= 3f)
            {
                return MobThreatStage.Early;
            }

            if (threatLevel <= 6f)
            {
                return MobThreatStage.RatFade;
            }

            if (threatLevel <= 9f)
            {
                return MobThreatStage.GoblinCore;
            }

            return threatLevel <= 13f ? MobThreatStage.OrcRise : MobThreatStage.OrcDominant;
        }

        private static bool IsRatSection(MazeGenerationResult result, Vector2Int position)
        {
            if (result == null)
            {
                return false;
            }

            return result.CentralRoom.IsValid
                ? position.x < result.CentralRoom.Min.x && !result.CentralRoom.Contains(position)
                : IsInEntranceHalf(result, position);
        }

        private static bool IsDeepEnoughForInitialOrc(
            Vector2Int position,
            Dictionary<Vector2Int, int> distancesFromEntrance,
            int maxDistanceFromEntrance,
            float threshold)
        {
            if (maxDistanceFromEntrance <= 0
                || distancesFromEntrance == null
                || !distancesFromEntrance.TryGetValue(position, out var distance))
            {
                return false;
            }

            return distance / (float)maxDistanceFromEntrance >= threshold;
        }

        private static bool IsInEntranceHalf(MazeGenerationResult result, Vector2Int position)
        {
            var entranceOnLeft = result.EntrancePosition.x <= result.Grid.Width / 2;
            return entranceOnLeft
                ? position.x < result.Grid.Width / 2
                : position.x > result.Grid.Width / 2;
        }

        private static int FindClosestToEntranceIndex(
            IReadOnlyList<Vector2Int> positions,
            Dictionary<Vector2Int, int> distancesFromEntrance)
        {
            var bestIndex = 0;
            var bestDistance = int.MaxValue;
            for (var i = 0; i < positions.Count; i++)
            {
                var distance = distancesFromEntrance.TryGetValue(positions[i], out var value) ? value : int.MaxValue;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        private static int FindFarthestFromEntranceIndex(
            IReadOnlyList<Vector2Int> positions,
            Dictionary<Vector2Int, int> distancesFromEntrance)
        {
            var bestIndex = 0;
            var bestDistance = int.MinValue;
            for (var i = 0; i < positions.Count; i++)
            {
                var distance = distancesFromEntrance.TryGetValue(positions[i], out var value) ? value : int.MinValue;
                if (distance > bestDistance)
                {
                    bestDistance = distance;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        private static int CalculateMaxDistance(Dictionary<Vector2Int, int> distances)
        {
            var maxDistance = 0;
            foreach (var distance in distances.Values)
            {
                maxDistance = Mathf.Max(maxDistance, distance);
            }

            return maxDistance;
        }

        private static int CountSpecies(IReadOnlyList<MobSpecies> species, MobSpecies target)
        {
            var count = 0;
            for (var i = 0; i < species.Count; i++)
            {
                if (species[i] == target)
                {
                    count++;
                }
            }

            return count;
        }

        private static int GridDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        private readonly struct RespawnWeights
        {
            public RespawnWeights(int rat, int goblin, int orc)
            {
                Rat = rat;
                Goblin = goblin;
                Orc = orc;
            }

            public int Rat { get; }

            public int Goblin { get; }

            public int Orc { get; }
        }
    }
}
