using System;
using Labyrinth.Core;
using Labyrinth.Hero;
using Labyrinth.Maze;
using Labyrinth.Mobs;
using UnityEngine;

namespace Labyrinth.Combat
{
    public sealed class CombatController : MonoBehaviour
    {
        private const float FirstHitDelay = 0.38f;
        private const float TurnDelay = 0.78f;
        private const float FinishDelay = 0.55f;
        private const int MinOrcGoldReward = 14;
        private const int MaxOrcGoldReward = 24;
        private const int OrcExperienceReward = 14;
        private const int MinGoblinGoldReward = 7;
        private const int MaxGoblinGoldReward = 13;
        private const int GoblinExperienceReward = 7;
        private const int MinRatGoldReward = 3;
        private const int MaxRatGoldReward = 6;
        private const int RatExperienceReward = 2;
        private const int MinMiniBossGoldReward = 36;
        private const int MaxMiniBossGoldReward = 64;
        private const int MiniBossExperienceReward = 48;
        private const int MinBossGoldReward = 110;
        private const int MaxBossGoldReward = 170;
        private const int BossExperienceReward = 125;

        private readonly System.Random rewardRandom = new System.Random();
        private HeroController hero;
        private MobController mob;
        private MazeGrid grid;
        private MazeRenderer mazeRenderer;
        private bool heroTurn;
        private bool finishing;
        private bool heroOpeningAttackUsed;
        private float timer;

        public event Action<MobController> MobDefeated;

        public bool IsActive { get; private set; }

        public void CancelCombat()
        {
            hero = null;
            mob = null;
            grid = null;
            mazeRenderer = null;
            IsActive = false;
            finishing = false;
        }

        public bool StartCombat(HeroController heroController, MobController mobController, MazeGrid mazeGrid, MazeRenderer mazeRenderer)
        {
            if (IsActive || heroController == null || mobController == null || mazeGrid == null || mazeRenderer == null)
            {
                return false;
            }

            if (heroController.Model == null || mobController.Model == null || !heroController.Model.IsAlive || !mobController.Model.IsAlive)
            {
                return false;
            }

            hero = heroController;
            mob = mobController;
            grid = mazeGrid;
            this.mazeRenderer = mazeRenderer;
            heroTurn = true;
            finishing = false;
            heroOpeningAttackUsed = false;
            timer = FirstHitDelay;
            IsActive = true;

            PlaceOpponents();
            hero.EnterCombat();
            mob.EnterCombat();
            FaceOpponents();
            GameAudioController.Play(GameSfx.CombatStart, mazeRenderer.GridToWorld(hero.Model.Position));
            GameDebugLog.Info(
                "Combat",
                $"Started: hero=#{hero.DisplayNumber} pos={GameDebugLog.Position(hero.Model.Position)}, heroHP={hero.Model.HitPoints}/{hero.Model.MaxHitPoints}, heroAtk={hero.Model.AttackPoints}, heroArmor={hero.Model.ArmorPoints}, mob={mob.DebugName}, mobPos={GameDebugLog.Position(mob.Position)}, mobHP={mob.Model.HitPoints}/{mob.Model.MaxHitPoints}, mobAtk={mob.Model.AttackPoints}, mobArmor={mob.Model.ArmorPoints}");
            return true;
        }

        private void Update()
        {
            if (!IsActive)
            {
                return;
            }

            timer -= Time.deltaTime;
            if (timer > 0f)
            {
                return;
            }

            if (finishing)
            {
                FinishCombat();
                return;
            }

            ExecuteTurn();
        }

        private void ExecuteTurn()
        {
            if (heroTurn)
            {
                var attack = hero.Model.AttackPoints;
                var firstHitBonus = 0;
                if (!heroOpeningAttackUsed)
                {
                    firstHitBonus = hero.Model.FirstHitBlessingBonus;
                    attack += firstHitBonus;
                    heroOpeningAttackUsed = true;
                }

                var hpBefore = mob.Model.HitPoints;
                var damage = mob.ReceiveDamage(attack);
                hero.PlayAttack(mob.Position);
                DamageNumberView.Create(mazeRenderer, mob.Position, damage, new Color(1f, 0.72f, 0.24f));
                GameAudioController.Play(GameSfx.CombatHit, mazeRenderer.GridToWorld(mob.Position));
                GameDebugLog.Info(
                    "Combat",
                    $"Hero #{hero.DisplayNumber} hit {mob.DebugName}: attack={attack}, firstHitBonus={firstHitBonus}, mobArmor={mob.Model.ArmorPoints}, damage={damage}, mobHP={hpBefore}->{mob.Model.HitPoints}/{mob.Model.MaxHitPoints}.");

                if (!mob.Model.IsAlive)
                {
                    BeginFinish();
                    return;
                }
            }
            else
            {
                var hpBefore = hero.Model.HitPoints;
                var damage = hero.ReceiveDamage(mob.Model.AttackPoints);
                mob.PlayAttack(hero.Model.Position);
                DamageNumberView.Create(mazeRenderer, hero.Model.Position, damage, new Color(1f, 0.3f, 0.24f));
                GameAudioController.Play(GameSfx.CombatHit, mazeRenderer.GridToWorld(hero.Model.Position));
                GameDebugLog.Info(
                    "Combat",
                    $"{mob.DebugName} hit Hero #{hero.DisplayNumber}: attack={mob.Model.AttackPoints}, heroArmor={hero.Model.ArmorPoints}, damage={damage}, heroHP={hpBefore}->{hero.Model.HitPoints}/{hero.Model.MaxHitPoints}.");

                if (!hero.Model.IsAlive)
                {
                    BeginFinish();
                    return;
                }
            }

            heroTurn = !heroTurn;
            timer = TurnDelay;
        }

        private void PlaceOpponents()
        {
            var heroPosition = hero.Model.Position;
            var mobPosition = mob.Position;

            if (IsAdjacent(heroPosition, mobPosition))
            {
                hero.SetGridPositionImmediate(heroPosition);
                mob.SetGridPositionImmediate(mobPosition);
                return;
            }

            if (TryFindNeighbor(heroPosition, out var adjacentMobPosition))
            {
                hero.SetGridPositionImmediate(heroPosition);
                mob.SetGridPositionImmediate(adjacentMobPosition);
            }
        }

        private bool TryFindNeighbor(Vector2Int position, out Vector2Int neighbor)
        {
            foreach (var direction in MazeDirections.Cardinal)
            {
                var candidate = position + direction;
                if (grid.InBounds(candidate) && grid.Get(candidate).IsWalkable)
                {
                    neighbor = candidate;
                    return true;
                }
            }

            neighbor = default;
            return false;
        }

        private void FaceOpponents()
        {
            hero.FaceGridPosition(mob.Position);
            mob.FaceGridPosition(hero.Model.Position);
        }

        private void BeginFinish()
        {
            finishing = true;
            timer = FinishDelay;
        }

        private void FinishCombat()
        {
            if (mob != null && !mob.Model.IsAlive)
            {
                GiveHeroVictoryReward();
                MobDefeated?.Invoke(mob);
            }
            else if (mob != null)
            {
                if (hero != null && hero.Model != null && !hero.Model.IsAlive)
                {
                    GameDebugLog.Info(
                        "Combat",
                        $"Hero defeated by {BuildMobName(mob.Model)} at {GameDebugLog.Position(mob.Position)}.");
                    GameAudioController.Play(GameSfx.Defeat, mazeRenderer.GridToWorld(hero.Model.Position));
                }

                mob.LeaveCombat();
            }

            if (hero != null && hero.Model.IsAlive)
            {
                hero.LeaveCombat();
            }

            hero = null;
            mob = null;
            grid = null;
            mazeRenderer = null;
            IsActive = false;
            finishing = false;
        }

        private void GiveHeroVictoryReward()
        {
            if (hero == null || hero.Model == null || !hero.Model.IsAlive)
            {
                return;
            }

            var rewardProfile = BuildRewardProfile(mob.Model);
            var reward = rewardRandom.Next(rewardProfile.MinGold, rewardProfile.MaxGold + 1);
            reward = hero.Model.ApplyGoldRewardBlessing(reward);
            hero.Model.AddGold(reward);
            var experienceReward = rewardProfile.Experience + GetDarkHunterExperienceBonus();
            var gainedLevels = hero.Model.AddExperience(experienceReward);
            GameAudioController.Play(GameSfx.Deposit, mazeRenderer.GridToWorld(hero.Model.Position));
            GameDebugLog.Info(
                "Combat",
                $"Mob defeated: {mob.DebugName}, rewardGold={reward}, rewardXP={experienceReward}, darkSpawn={mob.Model.SpawnedFromDarkness}, heroGold={hero.Model.Gold}, heroXP={hero.Model.Experience}/{hero.Model.ExperienceForNextLevel}, heroLevel={hero.Model.Level}, gainedLevels={gainedLevels}");
            DamageNumberView.CreateText(
                mazeRenderer,
                hero.Model.Position,
                $"+{reward} зол.",
                new Color(1f, 0.84f, 0.26f),
                1.75f);
            DamageNumberView.CreateText(
                mazeRenderer,
                hero.Model.Position,
                $"+{experienceReward} XP",
                new Color(0.55f, 0.86f, 1f),
                2.05f);

            if (rewardProfile.IsBoss)
            {
                GiveDescentKeyReward();
                DamageNumberView.CreateText(
                    mazeRenderer,
                    hero.Model.Position,
                    "Ключ спуска найден",
                    new Color(1f, 0.32f, 0.24f),
                    2.35f);
            }

            if (gainedLevels > 0)
            {
                GameAudioController.Play(GameSfx.LevelUp, mazeRenderer.GridToWorld(hero.Model.Position));
                DamageNumberView.CreateText(
                    mazeRenderer,
                    hero.Model.Position,
                    $"Уровень {hero.Model.Level}",
                    new Color(0.72f, 1f, 0.42f),
                    rewardProfile.IsBoss ? 2.65f : 2.35f);
            }
        }

        private int GetDarkHunterExperienceBonus()
        {
            if (hero == null
                || hero.Model == null
                || mob == null
                || mob.Model == null
                || !mob.Model.SpawnedFromDarkness
                || !hero.Model.HasBlessing(HeroBlessingType.DarkHunter))
            {
                return 0;
            }

            return HeroModel.DarkHunterExperienceBonus;
        }

        private void GiveDescentKeyReward()
        {
            if (hero == null || hero.Model == null)
            {
                return;
            }

            var inventory = hero.Model.Inventory;
            var placed = inventory.TryPlaceInEmptySlot(HeroInventory.DescentKeyItemName, HeroInventory.DescentKeyHoverInfo);
            if (!placed && inventory.TryRemoveItem(HeroInventory.CentralRoomKeyItemName))
            {
                placed = inventory.TryPlaceInEmptySlot(HeroInventory.DescentKeyItemName, HeroInventory.DescentKeyHoverInfo);
            }

            if (placed)
            {
                GameAudioController.Play(GameSfx.KeyPickup, mazeRenderer.GridToWorld(hero.Model.Position), 1.15f);
            }

            GameDebugLog.Info(
                "Combat",
                placed
                    ? "Boss reward granted: descent key placed in hero inventory."
                    : "Boss reward could not place descent key: hero inventory has no empty slot.");
        }

        private static RewardProfile BuildRewardProfile(MobModel model)
        {
            if (model != null && model.IsBoss)
            {
                return new RewardProfile(MinBossGoldReward, MaxBossGoldReward, BossExperienceReward, true);
            }

            if (model != null && model.IsMiniBoss)
            {
                return new RewardProfile(MinMiniBossGoldReward, MaxMiniBossGoldReward, MiniBossExperienceReward, false);
            }

            switch (model?.Species)
            {
                case MobSpecies.Rat:
                    return new RewardProfile(MinRatGoldReward, MaxRatGoldReward, RatExperienceReward, false);
                case MobSpecies.Goblin:
                    return new RewardProfile(MinGoblinGoldReward, MaxGoblinGoldReward, GoblinExperienceReward, false);
                case MobSpecies.Orc:
                default:
                    return new RewardProfile(MinOrcGoldReward, MaxOrcGoldReward, OrcExperienceReward, false);
            }
        }

        private static bool IsAdjacent(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) == 1;
        }

        private static string BuildMobName(MobModel model)
        {
            if (model == null)
            {
                return "Unknown";
            }

            if (model.IsBoss)
            {
                return $"Boss {model.Species}";
            }

            return model.IsMiniBoss ? $"MiniBoss {model.Species}" : model.Species.ToString();
        }

        private readonly struct RewardProfile
        {
            public RewardProfile(int minGold, int maxGold, int experience, bool isBoss)
            {
                MinGold = minGold;
                MaxGold = maxGold;
                Experience = experience;
                IsBoss = isBoss;
            }

            public int MinGold { get; }

            public int MaxGold { get; }

            public int Experience { get; }

            public bool IsBoss { get; }
        }
    }
}
