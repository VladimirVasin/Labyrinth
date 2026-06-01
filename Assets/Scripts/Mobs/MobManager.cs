using System.Collections.Generic;
using Labyrinth.Core;
using Labyrinth.Hero;
using Labyrinth.Maze;
using UnityEngine;

namespace Labyrinth.Mobs
{
    public sealed partial class MobManager : MonoBehaviour
    {
        private const int WalkableCellsPerRegularMob = 38;
        private const int WalkableCellsPerRatMob = 22;
        private const int MinimumRegularMobCount = 4;
        private const int MaximumRegularMobCount = 120;
        private const int MaximumRatMobCount = 80;
        private const int NormalEntranceMobBuffer = 4;
        private const int OpeningEntranceMobBuffer = 8;
        private const int MinimumMobDistanceFloor = 4;
        private const int MinimumMobDistanceDivisor = 12;
        private const float RespawnCheckInterval = 1.35f;
        private const float RespawnChancePerCheck = 0.88f;
        private const int RespawnDarkPaddingCells = 1;
        private const int RespawnMaxCandidateChecks = 180;
        private const float RespawnSummaryInterval = 12f;
        private const float OpeningRespawnGraceSeconds = 24f;
        private const float OpeningMobWanderDelaySeconds = 5.25f;
        private const float OpeningRegularTargetMultiplier = 1.35f;
        private const float RegularTargetMultiplier = 1.75f;

        private readonly List<MobController> mobs = new List<MobController>();
        private MazeGenerationResult result;
        private MazeRenderer mazeRenderer;
        private Dictionary<Vector2Int, int> distancesFromEntrance;
        private System.Random respawnRandom;
        private Transform root;
        private MobController centralMiniBoss;
        private int regularMobTargetCount;
        private int maxDistanceFromEntrance;
        private int respawnSerial;
        private int respawnAtTargetSkips;
        private int respawnChanceSkips;
        private int respawnNoCandidateSkips;
        private int respawnPendingRemovalSkips;
        private int respawnSuccessesSinceSummary;
        private float respawnTimer;
        private float respawnSummaryTimer;
        private float openingRespawnGraceTimer;

        public bool HasCentralMiniBossAlive => centralMiniBoss != null && centralMiniBoss.Model != null && centralMiniBoss.Model.IsAlive;

        public void Spawn(MazeGenerationResult result, MazeRenderer renderer)
        {
            Clear();

            if (result == null || renderer == null)
            {
                return;
            }

            this.result = result;
            mazeRenderer = renderer;
            root = new GameObject("MobsRoot").transform;
            root.SetParent(transform, false);

            var useOpeningRegularStats = result.LevelNumber <= 1;
            var spawnCandidates = CollectSpawnCandidates(result, useOpeningRegularStats);
            var random = new System.Random(result.Settings.Seed ^ 0x51f3a8d);
            if (spawnCandidates.Count == 0)
            {
                GameDebugLog.Warning("Mobs", "No valid spawn candidates found.");
                return;
            }

            var initialCandidateCount = spawnCandidates.Count;
            var bossCandidates = CollectBossSpawnCandidates(result, spawnCandidates);
            var bossPosition = SelectBossSpawnPosition(
                result,
                bossCandidates.Count > 0 ? bossCandidates : spawnCandidates,
                random);
            var bossSpecies = SelectBossSpecies(random);
            var boss = MobController.Create(
                result.Grid,
                renderer,
                bossPosition,
                result.Settings.Seed ^ 0x6b05f,
                bossSpecies,
                MobRank.Boss,
                result.LevelNumber);
            boss.transform.SetParent(root, true);
            AddManagedMob(boss);
            spawnCandidates.Remove(bossPosition);

            var majorMobPositions = new List<Vector2Int> { bossPosition };
            var miniBossSpawned = false;
            var miniBossPosition = default(Vector2Int);
            var miniBossSpecies = MobSpecies.Orc;
            if (TrySelectMiniBossSpawnPosition(result, random, out miniBossPosition))
            {
                miniBossSpecies = SelectMiniBossSpecies(random);
                var miniBoss = MobController.Create(
                    result.Grid,
                    renderer,
                    miniBossPosition,
                    result.Settings.Seed ^ 0x4c1a91,
                    miniBossSpecies,
                    MobRank.MiniBoss,
                    result.LevelNumber);
                miniBoss.transform.SetParent(root, true);
                miniBoss.SetWanderingPaused(true);
                AddManagedMob(miniBoss);
                centralMiniBoss = miniBoss;
                spawnCandidates.Remove(miniBossPosition);
                majorMobPositions.Add(miniBossPosition);
                miniBossSpawned = true;
            }

            distancesFromEntrance = MazeValidation.GetReachableDistances(result.Grid, result.EntrancePosition, true);
            maxDistanceFromEntrance = CalculateMaxDistance(distancesFromEntrance);
            respawnRandom = new System.Random(result.Settings.Seed ^ 0x7b61c21);
            respawnTimer = RespawnCheckInterval;
            openingRespawnGraceTimer = useOpeningRegularStats ? OpeningRespawnGraceSeconds : 0f;
            var minimumDistance = CalculateMinimumMobDistance(result.Grid);
            var ratCandidates = CollectRatSpawnCandidates(result, spawnCandidates);
            var ratSpawnCount = CalculateRatMobCount(ratCandidates.Count);
            var ratPositions = SelectSpreadSpawnPositions(
                ratCandidates,
                ratSpawnCount,
                result.EntrancePosition,
                random,
                Mathf.Max(2, minimumDistance - 1),
                majorMobPositions);
            foreach (var ratPosition in ratPositions)
            {
                spawnCandidates.Remove(ratPosition);
            }

            var easyGoblinCandidates = CollectRatSpawnCandidates(result, spawnCandidates);
            var easyGoblinCount = easyGoblinCandidates.Count <= 0
                ? 0
                : Mathf.Clamp(Mathf.RoundToInt(easyGoblinCandidates.Count / 65f), Mathf.Min(2, easyGoblinCandidates.Count), Mathf.Min(30, easyGoblinCandidates.Count));
            var easyGoblinPositions = SelectSpreadSpawnPositions(
                easyGoblinCandidates,
                easyGoblinCount,
                result.EntrancePosition,
                random,
                Mathf.Max(2, minimumDistance - 1),
                majorMobPositions);
            foreach (var goblinPosition in easyGoblinPositions)
            {
                spawnCandidates.Remove(goblinPosition);
            }

            var spawnCount = CalculateRegularMobCount(spawnCandidates.Count);
            var occupiedPositions = new List<Vector2Int>(majorMobPositions);
            occupiedPositions.AddRange(ratPositions);
            occupiedPositions.AddRange(easyGoblinPositions);
            var spawnPositions = SelectSpreadSpawnPositions(
                spawnCandidates,
                spawnCount,
                result.EntrancePosition,
                random,
                minimumDistance,
                occupiedPositions);
            var spawnSpecies = SelectRegularMobSpecies(
                spawnPositions,
                distancesFromEntrance,
                maxDistanceFromEntrance,
                random,
                useOpeningRegularStats);
            var targetMultiplier = useOpeningRegularStats ? OpeningRegularTargetMultiplier : RegularTargetMultiplier;
            regularMobTargetCount = Mathf.CeilToInt((ratPositions.Count + easyGoblinPositions.Count + spawnPositions.Count) * targetMultiplier);

            for (var i = 0; i < ratPositions.Count; i++)
            {
                var mob = MobController.Create(
                    result.Grid,
                    renderer,
                    ratPositions[i],
                    result.Settings.Seed + i * 7919,
                    MobSpecies.Rat,
                    MobRank.Regular,
                    result.LevelNumber,
                    useOpeningRegularStats,
                    useOpeningRegularStats ? OpeningMobWanderDelaySeconds : 0f);
                mob.transform.SetParent(root, true);
                AddManagedMob(mob);
            }

            for (var i = 0; i < easyGoblinPositions.Count; i++)
            {
                var mob = MobController.Create(
                    result.Grid,
                    renderer,
                    easyGoblinPositions[i],
                    result.Settings.Seed + i * 6827,
                    MobSpecies.Goblin,
                    MobRank.Regular,
                    result.LevelNumber,
                    useOpeningRegularStats,
                    useOpeningRegularStats ? OpeningMobWanderDelaySeconds : 0f);
                mob.transform.SetParent(root, true);
                AddManagedMob(mob);
            }

            for (var i = 0; i < spawnPositions.Count; i++)
            {
                var mob = MobController.Create(
                    result.Grid,
                    renderer,
                    spawnPositions[i],
                    result.Settings.Seed + i * 9973,
                    spawnSpecies[i],
                    MobRank.Regular,
                    result.LevelNumber,
                    useOpeningRegularStats,
                    useOpeningRegularStats ? OpeningMobWanderDelaySeconds : 0f);
                mob.transform.SetParent(root, true);
                AddManagedMob(mob);
            }

            GameDebugLog.Info(
                "Mobs",
                $"Spawned regular={CountRegularMobs()}/{regularMobTargetCount} (rats={ratPositions.Count}, easyGoblins={easyGoblinPositions.Count}, goblins={easyGoblinPositions.Count + CountSpecies(spawnSpecies, MobSpecies.Goblin)}, orcs={CountSpecies(spawnSpecies, MobSpecies.Orc)}), openingRegularStats={useOpeningRegularStats}, entranceBuffer={GetEntranceMobBuffer(result)}, openingRespawnGrace={openingRespawnGraceTimer:0.0}, targetMultiplier={targetMultiplier:0.00}, miniBoss={(miniBossSpawned ? $"{miniBossSpecies} at {GameDebugLog.Position(miniBossPosition)} {BuildStatsText(centralMiniBoss)}" : "none")}, boss={bossSpecies} at {GameDebugLog.Position(bossPosition)} {BuildStatsText(boss)}, dungeonLevel={result.LevelNumber}, candidates={initialCandidateCount}, ratCandidates={ratCandidates.Count}, bossCandidates={bossCandidates.Count}, minDistance={minimumDistance}, maxEntranceDistance={maxDistanceFromEntrance}");
        }

        public void Clear()
        {
            mobs.Clear();
            result = null;
            mazeRenderer = null;
            distancesFromEntrance = null;
            respawnRandom = null;
            regularMobTargetCount = 0;
            maxDistanceFromEntrance = 0;
            respawnSerial = 0;
            respawnAtTargetSkips = 0;
            respawnChanceSkips = 0;
            respawnNoCandidateSkips = 0;
            respawnPendingRemovalSkips = 0;
            respawnSuccessesSinceSummary = 0;
            respawnTimer = 0f;
            respawnSummaryTimer = 0f;
            openingRespawnGraceTimer = 0f;
            centralMiniBoss = null;

            if (root == null)
            {
                return;
            }

            Destroy(root.gameObject);
            root = null;
        }

        public void Remove(MobController mob)
        {
            if (mob == null)
            {
                return;
            }

            mobs.Remove(mob);
            if (mob == centralMiniBoss) { centralMiniBoss = null; }
            GameDebugLog.Info(
                "Mobs",
                $"Removed {mob.DebugName} at {GameDebugLog.Position(mob.Position)}. aliveRegular={CountRegularMobs()}/{regularMobTargetCount}");
            Destroy(mob.gameObject);
        }

        public bool TryBeginRespawnCheck()
        {
            if (result == null
                || mazeRenderer == null
                || respawnRandom == null
                || regularMobTargetCount <= 0)
            {
                return false;
            }

            respawnTimer -= Time.deltaTime;
            if (openingRespawnGraceTimer > 0f)
            {
                openingRespawnGraceTimer = Mathf.Max(0f, openingRespawnGraceTimer - Time.deltaTime);
                return false;
            }

            if (respawnTimer > 0f)
            {
                return false;
            }

            respawnTimer = RespawnCheckInterval;
            return true;
        }

        public void UpdateRespawns(HashSet<Vector2Int> respawnBlockedCells, bool hideUnlitMobs, IReadOnlyList<HeroController> activeHeroes)
        {
            if (result == null
                || mazeRenderer == null
                || respawnRandom == null
                || respawnBlockedCells == null
                || regularMobTargetCount <= 0)
            {
                return;
            }
            var threatStage = CalculateThreatStage(activeHeroes);
            if (CountPendingRemovalMobs() > 0)
            {
                respawnPendingRemovalSkips++;
                TraceRespawnSummary(respawnBlockedCells.Count, threatStage);
                return;
            }

            var regularCount = CountRegularMobs();
            if (regularCount >= regularMobTargetCount)
            {
                respawnAtTargetSkips++;
                TraceRespawnSummary(respawnBlockedCells.Count, threatStage);
                return;
            }

            if (respawnRandom.NextDouble() > RespawnChancePerCheck)
            {
                respawnChanceSkips++;
                TraceRespawnSummary(respawnBlockedCells.Count, threatStage);
                return;
            }

            if (!TrySelectRespawn(respawnBlockedCells, threatStage, out var species, out var position))
            {
                respawnNoCandidateSkips++;
                TraceRespawnSummary(respawnBlockedCells.Count, threatStage);
                return;
            }

            var respawnRank = MobRank.Regular;
            if (!CanRespawnInDarkness(respawnRank))
            {
                GameDebugLog.Warning("Mobs", $"Dark respawn blocked for forbidden rank {respawnRank}.");
                return;
            }

            var mob = MobController.Create(
                result.Grid,
                mazeRenderer,
                position,
                result.Settings.Seed ^ 0x348f17 ^ (++respawnSerial * 104729),
                species,
                respawnRank,
                result.LevelNumber,
                result.LevelNumber <= 1 && threatStage == MobThreatStage.Early);
            if (mob.Model == null || !CanRespawnInDarkness(mob.Model.Rank))
            {
                Destroy(mob.gameObject);
                GameDebugLog.Warning("Mobs", $"Dark respawn rejected after creation: species={species}, rank={mob.Model?.Rank.ToString() ?? "none"}.");
                return;
            }

            mob.transform.SetParent(root, true);
            mob.MarkSpawnedFromDarkness();
            mob.SetVisible(!hideUnlitMobs || respawnBlockedCells.Contains(position));
            AddManagedMob(mob);
            respawnSuccessesSinceSummary++;
            GameDebugLog.Info(
                "Mobs",
                $"Respawned {mob.DebugName} at {GameDebugLog.Position(position)} in unlit cell. threatStage={threatStage}, regular={CountRegularMobs()}/{regularMobTargetCount}, respawnBlockedCells={respawnBlockedCells.Count}, aliveMobs={CountAliveMobs()}, runtimeMobs={mobs.Count}");
            TraceRespawnSummary(respawnBlockedCells.Count, threatStage);
        }

        private static bool CanRespawnInDarkness(MobRank rank)
        {
            return rank == MobRank.Regular;
        }

        public void ShowAllMobs()
        {
            foreach (var mob in mobs)
            {
                if (mob != null)
                {
                    mob.SetVisible(true);
                }
            }
        }

        public void ApplyVisibility(HashSet<Vector2Int> visibleCells)
        {
            if (visibleCells == null)
            {
                ShowAllMobs();
                return;
            }

            foreach (var mob in mobs)
            {
                if (mob == null || mob.Model == null)
                {
                    continue;
                }

                mob.SetVisible(visibleCells.Contains(mob.Position));
            }
        }

        public void CollectOccupiedPositions(HashSet<Vector2Int> occupiedPositions)
        {
            if (occupiedPositions == null)
            {
                return;
            }

            foreach (var mob in mobs)
            {
                if (mob != null && mob.Model != null && mob.Model.IsAlive)
                {
                    occupiedPositions.Add(mob.Position);
                }
            }
        }

        public bool TryGetEncounter(HeroController hero, out MobController encounteredMob)
        {
            encounteredMob = null;
            if (hero == null || hero.Model == null)
            {
                return false;
            }

            if (HasCentralMiniBossAlive
                && centralMiniBoss.Model.State == MobState.Wandering
                && result != null && result.CentralRoom.IsValid
                && result.CentralRoom.Contains(hero.Model.Position))
            {
                encounteredMob = centralMiniBoss;
                GameDebugLog.Info(
                    "Mobs",
                    $"Encounter forced by central room: hero=#{hero.DisplayNumber} pos={GameDebugLog.Position(hero.Model.Position)} vs {encounteredMob.DebugName} pos={GameDebugLog.Position(encounteredMob.Position)}.");
                return true;
            }

            foreach (var mob in mobs)
            {
                if (mob == null || mob.Model == null || !mob.Model.IsAlive || mob.Model.State != MobState.Wandering)
                {
                    continue;
                }

                if (GridDistance(hero.Model.Position, mob.Position) <= 1)
                {
                    encounteredMob = mob;
                    GameDebugLog.Info(
                        "Mobs",
                        $"Encounter triggered: hero=#{hero.DisplayNumber} pos={GameDebugLog.Position(hero.Model.Position)} vs {mob.DebugName} pos={GameDebugLog.Position(mob.Position)}, distance={GridDistance(hero.Model.Position, mob.Position)}.");
                    return true;
                }
            }

            return false;
        }

        private void TraceRespawnSummary(int respawnBlockedCellCount, MobThreatStage threatStage)
        {
            respawnSummaryTimer -= RespawnCheckInterval;
            if (respawnSummaryTimer > 0f)
            {
                return;
            }

            if (respawnAtTargetSkips == 0
                && respawnChanceSkips == 0
                && respawnNoCandidateSkips == 0
                && respawnPendingRemovalSkips == 0
                && respawnSuccessesSinceSummary == 0)
            {
                respawnSummaryTimer = RespawnSummaryInterval;
                return;
            }

            GameDebugLog.Info(
                "Mobs",
                $"Dark respawn summary: threatStage={threatStage}, regular={CountRegularMobs()}/{regularMobTargetCount}, successes={respawnSuccessesSinceSummary}, skippedTarget={respawnAtTargetSkips}, skippedPendingRemoval={respawnPendingRemovalSkips}, skippedChance={respawnChanceSkips}, noCandidates={respawnNoCandidateSkips}, respawnBlockedCells={respawnBlockedCellCount}, aliveMobs={CountAliveMobs()}, runtimeMobs={mobs.Count}, pendingRemoval={CountPendingRemovalMobs()}.");
            respawnAtTargetSkips = 0;
            respawnChanceSkips = 0;
            respawnNoCandidateSkips = 0;
            respawnPendingRemovalSkips = 0;
            respawnSuccessesSinceSummary = 0;
            respawnSummaryTimer = RespawnSummaryInterval;
        }

        private static List<Vector2Int> CollectSpawnCandidates(MazeGenerationResult result, bool useOpeningSpawnRules)
        {
            var candidates = new List<Vector2Int>();
            var entranceBuffer = useOpeningSpawnRules ? OpeningEntranceMobBuffer : NormalEntranceMobBuffer;
            foreach (var cell in result.Grid.Cells())
            {
                if (!cell.IsWalkable || cell.Type == MazeCellType.Entrance)
                {
                    continue;
                }

                var position = new Vector2Int(cell.X, cell.Y);
                if (result.CentralRoom.IsValid && result.CentralRoom.Contains(position))
                {
                    continue;
                }

                if (GridDistance(position, result.EntrancePosition) <= entranceBuffer)
                {
                    continue;
                }

                candidates.Add(position);
            }

            return candidates;
        }

        private static List<Vector2Int> CollectRatSpawnCandidates(
            MazeGenerationResult result,
            IReadOnlyList<Vector2Int> spawnCandidates)
        {
            var candidates = new List<Vector2Int>();
            foreach (var candidate in spawnCandidates)
            {
                if (IsRatSection(result, candidate))
                {
                    candidates.Add(candidate);
                }
            }

            return candidates;
        }

        private bool TrySelectRespawn(HashSet<Vector2Int> respawnBlockedCells, MobThreatStage threatStage, out MobSpecies species, out Vector2Int position)
        {
            species = MobSpecies.Goblin;
            position = default;

            for (var attempt = 0; attempt < 6; attempt++)
            {
                species = SelectDarkRespawnSpecies(respawnRandom, threatStage);
                if (TrySelectRespawnPosition(species, respawnBlockedCells, out position))
                {
                    return true;
                }
            }

            var fallbackOrder = GetRespawnFallbackOrder(threatStage);
            for (var i = 0; i < fallbackOrder.Length; i++)
            {
                if (TrySelectRespawnPosition(fallbackOrder[i], respawnBlockedCells, out position))
                {
                    species = fallbackOrder[i];
                    return true;
                }
            }

            return false;
        }

        private bool TrySelectRespawnPosition(MobSpecies species, HashSet<Vector2Int> respawnBlockedCells, out Vector2Int position)
        {
            position = default;
            var best = new List<Vector2Int>();
            var checkedCells = 0;
            foreach (var cell in result.Grid.Cells())
            {
                if (checkedCells >= RespawnMaxCandidateChecks && best.Count > 0)
                {
                    break;
                }

                if (!cell.IsWalkable || cell.Type == MazeCellType.Entrance)
                {
                    continue;
                }

                var candidate = new Vector2Int(cell.X, cell.Y);
                if (respawnRandom.NextDouble() > 0.35)
                {
                    continue;
                }

                checkedCells++;
                if (!IsValidRespawnCell(candidate, respawnBlockedCells, species))
                {
                    continue;
                }

                best.Add(candidate);
            }

            if (best.Count == 0)
            {
                return false;
            }

            position = best[respawnRandom.Next(best.Count)];
            return true;
        }

        private bool IsValidRespawnCell(Vector2Int position, HashSet<Vector2Int> respawnBlockedCells, MobSpecies species)
        {
            if (GridDistance(position, result.EntrancePosition) <= GetEntranceMobBuffer(result)
                || IsNearRespawnBlockedCell(position, respawnBlockedCells)
                || IsOccupiedByMob(position))
            {
                return false;
            }

            switch (species)
            {
                case MobSpecies.Rat:
                    return IsRatSection(result, position);
                case MobSpecies.Orc:
                    return IsStrongRespawnSection(position);
                default:
                    return true;
            }
        }

        private bool IsStrongRespawnSection(Vector2Int position)
        {
            if (result != null
                && result.CentralRoom.IsValid
                && result.CentralRoom.IsBeyondExitSide(position)
                && !result.CentralRoom.Contains(position))
            {
                return true;
            }

            if (maxDistanceFromEntrance <= 0 || distancesFromEntrance == null || !distancesFromEntrance.TryGetValue(position, out var distance))
            {
                return false;
            }

            return distance / (float)maxDistanceFromEntrance >= 0.45f;
        }

        private static bool IsNearRespawnBlockedCell(Vector2Int position, HashSet<Vector2Int> respawnBlockedCells)
        {
            for (var x = position.x - RespawnDarkPaddingCells; x <= position.x + RespawnDarkPaddingCells; x++)
            {
                for (var y = position.y - RespawnDarkPaddingCells; y <= position.y + RespawnDarkPaddingCells; y++)
                {
                    if (respawnBlockedCells.Contains(new Vector2Int(x, y)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool IsOccupiedByMob(Vector2Int position)
        {
            foreach (var mob in mobs)
            {
                if (mob != null && mob.Model != null && mob.Model.IsAlive && mob.Position == position)
                {
                    return true;
                }
            }

            return false;
        }

        private int CountRegularMobs()
        {
            var count = 0;
            foreach (var mob in mobs)
            {
                if (mob != null
                    && mob.Model != null
                    && mob.Model.IsAlive
                    && mob.Model.Rank == MobRank.Regular)
                {
                    count++;
                }
            }

            return count;
        }

        private int CountAliveMobs()
        {
            var count = 0;
            foreach (var mob in mobs)
            {
                if (mob != null && mob.Model != null && mob.Model.IsAlive)
                {
                    count++;
                }
            }

            return count;
        }

        private int CountPendingRemovalMobs()
        {
            var count = 0;
            foreach (var mob in mobs)
            {
                if (mob != null && mob.Model != null && !mob.Model.IsAlive)
                {
                    count++;
                }
            }

            return count;
        }

        private static List<Vector2Int> CollectBossSpawnCandidates(
            MazeGenerationResult result,
            IReadOnlyList<Vector2Int> candidates)
        {
            var bossCandidates = new List<Vector2Int>();
            if (!result.CentralRoom.IsValid)
            {
                return bossCandidates;
            }

            foreach (var candidate in candidates)
            {
                if (result.CentralRoom.IsBeyondExitSide(candidate)
                    && !result.CentralRoom.Contains(candidate))
                {
                    bossCandidates.Add(candidate);
                }
            }

            return bossCandidates;
        }

        private static bool TrySelectMiniBossSpawnPosition(
            MazeGenerationResult result,
            System.Random random,
            out Vector2Int position)
        {
            position = default;
            if (result == null || result.Grid == null || !result.CentralRoom.IsValid)
            {
                return false;
            }

            var room = result.CentralRoom;
            var center = new Vector2Int((room.Min.x + room.Max.x) / 2, (room.Min.y + room.Max.y) / 2);
            var bestDistance = int.MaxValue;
            var best = new List<Vector2Int>();
            for (var x = room.Min.x; x <= room.Max.x; x++)
            {
                for (var y = room.Min.y; y <= room.Max.y; y++)
                {
                    var candidate = new Vector2Int(x, y);
                    if (!result.Grid.InBounds(candidate) || !result.Grid.Get(candidate).IsWalkable)
                    {
                        continue;
                    }

                    var distance = GridDistance(candidate, center);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        best.Clear();
                    }

                    if (distance == bestDistance)
                    {
                        best.Add(candidate);
                    }
                }
            }

            if (best.Count == 0)
            {
                return false;
            }

            position = best[random.Next(best.Count)];
            return true;
        }

        private static MobSpecies SelectMiniBossSpecies(System.Random random)
        {
            var roll = random.Next(100);
            return roll < 55 ? MobSpecies.Goblin : MobSpecies.Orc;
        }

        private static MobSpecies SelectBossSpecies(System.Random random)
        {
            var roll = random.Next(100);
            return roll < 35 ? MobSpecies.Goblin : MobSpecies.Orc;
        }

        private static Vector2Int SelectBossSpawnPosition(
            MazeGenerationResult result,
            List<Vector2Int> candidates,
            System.Random random)
        {
            var distances = MazeValidation.GetReachableDistances(result.Grid, result.EntrancePosition, true);
            var bestDistance = -1;
            var best = new List<Vector2Int>();

            foreach (var candidate in candidates)
            {
                if (!distances.TryGetValue(candidate, out var distance))
                {
                    continue;
                }

                if (distance > bestDistance)
                {
                    bestDistance = distance;
                    best.Clear();
                }

                if (distance == bestDistance)
                {
                    best.Add(candidate);
                }
            }

            return best.Count > 0 ? best[random.Next(best.Count)] : ChooseFarthestFromEntrance(candidates, result.EntrancePosition, random);
        }

        private static List<Vector2Int> SelectSpreadSpawnPositions(
            List<Vector2Int> candidates,
            int spawnCount,
            Vector2Int entrancePosition,
            System.Random random,
            int minimumDistance,
            IReadOnlyList<Vector2Int> occupiedPositions)
        {
            var selected = new List<Vector2Int>();
            if (candidates.Count == 0 || spawnCount <= 0)
            {
                return selected;
            }

            var occupied = occupiedPositions == null
                ? new List<Vector2Int>()
                : new List<Vector2Int>(occupiedPositions);
            var currentMinimumDistance = Mathf.Max(0, minimumDistance);

            while (selected.Count < spawnCount && candidates.Count > 0)
            {
                if (!TryChooseMostDistantFromSelected(
                    candidates,
                    occupied,
                    entrancePosition,
                    random,
                    currentMinimumDistance,
                    out var next))
                {
                    if (currentMinimumDistance <= 0)
                    {
                        break;
                    }

                    currentMinimumDistance--;
                    continue;
                }

                selected.Add(next);
                occupied.Add(next);
                candidates.Remove(next);
            }

            return selected;
        }

        private static Vector2Int ChooseFarthestFromEntrance(
            List<Vector2Int> candidates,
            Vector2Int entrancePosition,
            System.Random random)
        {
            var bestDistance = -1;
            var best = new List<Vector2Int>();
            foreach (var candidate in candidates)
            {
                var distance = GridDistance(candidate, entrancePosition);
                if (distance > bestDistance)
                {
                    bestDistance = distance;
                    best.Clear();
                }

                if (distance == bestDistance)
                {
                    best.Add(candidate);
                }
            }

            return best[random.Next(best.Count)];
        }

        private static bool TryChooseMostDistantFromSelected(
            List<Vector2Int> candidates,
            List<Vector2Int> occupied,
            Vector2Int entrancePosition,
            System.Random random,
            int minimumDistance,
            out Vector2Int selectedCandidate)
        {
            var bestScore = -1;
            var best = new List<Vector2Int>();
            foreach (var candidate in candidates)
            {
                var nearestOccupiedDistance = int.MaxValue;
                foreach (var occupiedPosition in occupied)
                {
                    nearestOccupiedDistance = Mathf.Min(nearestOccupiedDistance, GridDistance(candidate, occupiedPosition));
                }

                if (nearestOccupiedDistance < minimumDistance)
                {
                    continue;
                }

                var entranceDistance = GridDistance(candidate, entrancePosition);
                var spreadScore = occupied.Count == 0 ? 0 : nearestOccupiedDistance;
                var score = spreadScore * 1000 + entranceDistance;
                if (score > bestScore)
                {
                    bestScore = score;
                    best.Clear();
                }

                if (score == bestScore)
                {
                    best.Add(candidate);
                }
            }

            if (best.Count == 0)
            {
                selectedCandidate = default;
                return false;
            }

            selectedCandidate = best[random.Next(best.Count)];
            return true;
        }

        private static int CalculateRegularMobCount(int candidateCount)
        {
            if (candidateCount <= 0)
            {
                return 0;
            }

            var desiredCount = Mathf.RoundToInt(candidateCount / (float)WalkableCellsPerRegularMob);
            var minimumCount = Mathf.Min(MinimumRegularMobCount, candidateCount);
            var maximumCount = Mathf.Min(MaximumRegularMobCount, candidateCount);
            return Mathf.Clamp(desiredCount, minimumCount, maximumCount);
        }

        private static int GetEntranceMobBuffer(MazeGenerationResult result)
        {
            return result != null && result.LevelNumber <= 1
                ? OpeningEntranceMobBuffer
                : NormalEntranceMobBuffer;
        }

        private static string BuildStatsText(MobController mob)
        {
            if (mob == null || mob.Model == null)
            {
                return "stats=unknown";
            }

            return $"stats=HP {mob.Model.MaxHitPoints}, ATK {mob.Model.AttackPoints}, ARM {mob.Model.ArmorPoints}";
        }

        private static int CalculateRatMobCount(int candidateCount)
        {
            if (candidateCount <= 0)
            {
                return 0;
            }

            var desiredCount = Mathf.RoundToInt(candidateCount / (float)WalkableCellsPerRatMob);
            var minimumCount = Mathf.Min(2, candidateCount);
            var maximumCount = Mathf.Min(MaximumRatMobCount, candidateCount);
            return Mathf.Clamp(desiredCount, minimumCount, maximumCount);
        }

        private static int CalculateMinimumMobDistance(MazeGrid grid)
        {
            if (grid == null)
            {
                return MinimumMobDistanceFloor;
            }

            return Mathf.Max(MinimumMobDistanceFloor, Mathf.Min(grid.Width, grid.Height) / MinimumMobDistanceDivisor);
        }
    }
}
