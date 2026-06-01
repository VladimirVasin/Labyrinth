using System;
using System.Collections.Generic;
using Labyrinth.Core;
using Labyrinth.Maze;
using UnityEngine;

namespace Labyrinth.Mobs
{
    public sealed class MobController : MonoBehaviour
    {
        private const float WanderInterval = 0.78f;
        private const int EliteWanderLogStep = 5;

        private MazeGrid grid;
        private MobView view;
        private System.Random random;
        private bool wanderingPaused;
        private int wanderSteps;
        private float timeUntilNextWander;
        private static int nextDebugId;

        public MobModel Model { get; private set; }

        public int DebugId { get; private set; }

        public Vector2Int Position => Model.Position;

        public string DebugName => BuildDebugName(Model, DebugId);

        public Func<Vector2Int, bool> ShouldHoldWanderAtPosition { get; set; }

        public static MobController Create(
            MazeGrid mazeGrid,
            MazeRenderer renderer,
            Vector2Int spawnPosition,
            int seed,
            MobSpecies species = MobSpecies.Orc,
            MobRank rank = MobRank.Regular,
            int dungeonLevel = 1,
            bool useOpeningSpawnStats = false,
            float initialWanderDelaySeconds = 0f)
        {
            var controllerObject = new GameObject(BuildControllerName(rank));
            var controller = controllerObject.AddComponent<MobController>();
            controller.Initialize(mazeGrid, renderer, spawnPosition, seed, species, rank, dungeonLevel, useOpeningSpawnStats, initialWanderDelaySeconds);
            return controller;
        }

        public void SetWanderingPaused(bool paused)
        {
            wanderingPaused = paused;
        }

        public void SetVisible(bool visible)
        {
            if (view != null)
            {
                view.SetVisible(visible);
            }
        }

        public void MarkSpawnedFromDarkness()
        {
            Model?.MarkSpawnedFromDarkness();
        }

        public void EnterCombat()
        {
            wanderingPaused = true;
            Model.SetState(MobState.Fighting);
            GameDebugLog.Info("Mobs", $"{DebugName} entered combat at {GameDebugLog.Position(Model.Position)}: hp={Model.HitPoints}/{Model.MaxHitPoints}, atk={Model.AttackPoints}, armor={Model.ArmorPoints}.");
        }

        public void LeaveCombat()
        {
            if (Model.IsAlive)
            {
                Model.SetState(MobState.Wandering);
                wanderingPaused = Model.Rank == MobRank.MiniBoss;
                timeUntilNextWander = WanderInterval;
                GameDebugLog.Info("Mobs", $"{DebugName} left combat: pos={GameDebugLog.Position(Model.Position)}, hp={Model.HitPoints}/{Model.MaxHitPoints}, wanderingPaused={wanderingPaused}.");
            }
        }

        public void SetGridPositionImmediate(Vector2Int position)
        {
            Model.SetPosition(position);
            view.SetGridPositionImmediate(position);
        }

        public void FaceGridPosition(Vector2Int position)
        {
            view.FaceGridPosition(position);
        }

        public void PlayAttack(Vector2Int targetPosition)
        {
            view.PlayAttack(targetPosition);
        }

        public int ReceiveDamage(int incomingDamage)
        {
            var damage = Model.ReceiveDamage(incomingDamage);
            if (!Model.IsAlive)
            {
                wanderingPaused = true;
                view.SetDefeated();
                GameDebugLog.Info("Mobs", $"{DebugName} defeated at {GameDebugLog.Position(Model.Position)} by incomingAttack={incomingDamage}, damage={damage}.");
            }

            return damage;
        }

        public int ReceiveResolvedDamage(int resolvedDamage)
        {
            var damage = Model.ReceiveResolvedDamage(resolvedDamage);
            if (!Model.IsAlive)
            {
                wanderingPaused = true;
                view.SetDefeated();
                GameDebugLog.Info("Mobs", $"{DebugName} defeated at {GameDebugLog.Position(Model.Position)} by resolvedDamage={resolvedDamage}, damage={damage}.");
            }

            return damage;
        }

        private void Update()
        {
            if (grid == null || Model == null || Model.State != MobState.Wandering || wanderingPaused)
            {
                return;
            }

            timeUntilNextWander -= Time.deltaTime;
            if (timeUntilNextWander > 0f)
            {
                return;
            }

            if (ShouldHoldWanderAtPosition != null && ShouldHoldWanderAtPosition.Invoke(Model.Position))
            {
                timeUntilNextWander = Mathf.Min(WanderInterval, 0.08f);
                return;
            }

            timeUntilNextWander = WanderInterval;
            TryWander();
        }

        private void Initialize(
            MazeGrid mazeGrid,
            MazeRenderer renderer,
            Vector2Int spawnPosition,
            int seed,
            MobSpecies species,
            MobRank rank,
            int dungeonLevel,
            bool useOpeningSpawnStats,
            float initialWanderDelaySeconds)
        {
            grid = mazeGrid;
            random = new System.Random(seed);
            DebugId = ++nextDebugId;
            Model = new MobModel(spawnPosition, species, rank, dungeonLevel, seed, useOpeningSpawnStats);
            view = MobView.Create(renderer, spawnPosition, species, rank);
            view.SetController(this);
            view.transform.SetParent(transform, true);
            timeUntilNextWander = initialWanderDelaySeconds > 0f
                ? initialWanderDelaySeconds + (float)random.NextDouble() * WanderInterval
                : (float)random.NextDouble() * WanderInterval;
            if (rank == MobRank.Regular && useOpeningSpawnStats)
            {
                GameDebugLog.Info(
                    "Mobs",
                    $"{DebugName} rookie spawn at {GameDebugLog.Position(spawnPosition)}: hp={Model.HitPoints}/{Model.MaxHitPoints}, atk={Model.AttackPoints}, armor={Model.ArmorPoints}, initialWanderDelay={timeUntilNextWander:0.00}s.");
            }

            if (rank != MobRank.Regular)
            {
                GameDebugLog.Info(
                    "Mobs",
                    $"{DebugName} spawned at {GameDebugLog.Position(spawnPosition)}: level={dungeonLevel}, hp={Model.HitPoints}/{Model.MaxHitPoints}, atk={Model.AttackPoints}, armor={Model.ArmorPoints}.");
            }
        }

        private void TryWander()
        {
            var candidates = new List<Vector2Int>();
            foreach (var neighbor in grid.WalkableNeighbors(Model.Position))
            {
                candidates.Add(neighbor);
            }

            if (candidates.Count == 0)
            {
                return;
            }

            var next = candidates[random.Next(candidates.Count)];
            Model.SetPosition(next);
            view.MoveTo(next);
            wanderSteps++;
            if (GameDebugLog.VerboseTrace
                && Model.Rank != MobRank.Regular
                && wanderSteps % EliteWanderLogStep == 0)
            {
                GameDebugLog.Info("Mobs", $"{DebugName} patrol trace: pos={GameDebugLog.Position(Model.Position)}, steps={wanderSteps}, candidates={candidates.Count}.");
            }
        }

        private static string BuildControllerName(MobRank rank)
        {
            switch (rank)
            {
                case MobRank.Boss:
                    return "BossController";
                case MobRank.MiniBoss:
                    return "MiniBossController";
                default:
                    return "MobController";
            }
        }

        private static string BuildDebugName(MobModel model, int id)
        {
            if (model == null)
            {
                return $"Mob #{id}";
            }

            return $"{model.Rank} {model.Species} #{id}";
        }
    }
}
