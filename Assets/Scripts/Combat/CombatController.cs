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
        private const int MinOrcGoldReward = 18;
        private const int MaxOrcGoldReward = 32;
        private const int MinOrcExperienceReward = 15;
        private const int MaxOrcExperienceReward = 20;
        private const int MinGoblinGoldReward = 8;
        private const int MaxGoblinGoldReward = 16;
        private const int MinGoblinExperienceReward = 7;
        private const int MaxGoblinExperienceReward = 10;
        private const int MinRatGoldReward = 4;
        private const int MaxRatGoldReward = 8;
        private const int MinRatExperienceReward = 2;
        private const int MaxRatExperienceReward = 4;
        private const int MinMiniBossRatGoldReward = 44;
        private const int MaxMiniBossRatGoldReward = 76;
        private const int MinMiniBossRatExperienceReward = 50;
        private const int MaxMiniBossRatExperienceReward = 62;
        private const int MinMiniBossGoblinGoldReward = 48;
        private const int MaxMiniBossGoblinGoldReward = 82;
        private const int MinMiniBossGoblinExperienceReward = 55;
        private const int MaxMiniBossGoblinExperienceReward = 68;
        private const int MinMiniBossOrcGoldReward = 55;
        private const int MaxMiniBossOrcGoldReward = 95;
        private const int MinMiniBossOrcExperienceReward = 62;
        private const int MaxMiniBossOrcExperienceReward = 78;
        private const int MinBossRatGoldReward = 120;
        private const int MaxBossRatGoldReward = 200;
        private const int MinBossRatExperienceReward = 125;
        private const int MaxBossRatExperienceReward = 150;
        private const int MinBossGoblinGoldReward = 135;
        private const int MaxBossGoblinGoldReward = 215;
        private const int MinBossGoblinExperienceReward = 135;
        private const int MaxBossGoblinExperienceReward = 160;
        private const int MinBossOrcGoldReward = 155;
        private const int MaxBossOrcGoldReward = 250;
        private const int MinBossOrcExperienceReward = 155;
        private const int MaxBossOrcExperienceReward = 185;

        private readonly System.Random rewardRandom = new System.Random();
        private HeroController hero;
        private MobController mob;
        private MazeGrid grid;
        private MazeRenderer mazeRenderer;
        private bool heroTurn;
        private bool finishing;
        private bool heroOpeningAttackUsed;
        private bool mobOpeningAttackUsed;
        private float timer;

        public event Action<HeroController, MobController> MobDefeated;

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
            mobOpeningAttackUsed = false;
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
                var isOpeningAttack = !heroOpeningAttackUsed;
                if (!heroOpeningAttackUsed)
                {
                    firstHitBonus = hero.Model.FirstHitBlessingBonus;
                    attack += firstHitBonus;
                    heroOpeningAttackUsed = true;
                }

                var vengeanceBonus = hero.Model.GetVengeanceAttackBonus(mob.Model, isOpeningAttack, attack);
                attack += vengeanceBonus;
                var hpBefore = mob.Model.HitPoints;
                var damage = mob.ReceiveDamage(attack);
                hero.PlayAttack(mob.Position);
                DamageNumberView.Create(mazeRenderer, mob.Position, damage, new Color(1f, 0.72f, 0.24f));
                GameAudioController.Play(GameSfx.CombatHit, mazeRenderer.GridToWorld(mob.Position));
                GameDebugLog.Info(
                    "Combat",
                    $"Hero #{hero.DisplayNumber} hit {mob.DebugName}: attack={attack}, firstHitBonus={firstHitBonus}, vengeanceBonus={vengeanceBonus}, mobArmor={mob.Model.ArmorPoints}, damage={damage}, mobHP={hpBefore}->{mob.Model.HitPoints}/{mob.Model.MaxHitPoints}.");

                if (!mob.Model.IsAlive)
                {
                    BeginFinish();
                    return;
                }
            }
            else
            {
                hero.Model.RememberCombatThreat(mob.Model);
                var incomingAttack = mob.Model.AttackPoints;
                var modifiedAttack = hero.Model.ApplyVengeanceIncomingAttackModifier(
                    mob.Model,
                    incomingAttack,
                    !mobOpeningAttackUsed,
                    out var vengeanceReduction);
                mobOpeningAttackUsed = true;
                var hpBefore = hero.Model.HitPoints;
                var damage = hero.ReceiveDamage(modifiedAttack);
                mob.PlayAttack(hero.Model.Position);
                DamageNumberView.Create(mazeRenderer, hero.Model.Position, damage, new Color(1f, 0.3f, 0.24f));
                GameAudioController.Play(GameSfx.CombatHit, mazeRenderer.GridToWorld(hero.Model.Position));
                GameDebugLog.Info(
                    "Combat",
                    $"{mob.DebugName} hit Hero #{hero.DisplayNumber}: attack={incomingAttack}, modifiedAttack={modifiedAttack}, vengeanceReduction={vengeanceReduction}, heroArmor={hero.Model.ArmorPoints}, damage={damage}, heroHP={hpBefore}->{hero.Model.HitPoints}/{hero.Model.MaxHitPoints}.");

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
                MobDefeated?.Invoke(hero, mob);
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
            var vengeanceGoldBonus = hero.Model.GetVengeanceGoldRewardBonus(mob.Model, reward);
            reward += vengeanceGoldBonus;
            hero.Model.AddGold(reward);
            var vengeanceExperienceBonus = hero.Model.GetVengeanceExperienceRewardBonus(mob.Model);
            var experienceReward = rewardRandom.Next(rewardProfile.MinExperience, rewardProfile.MaxExperience + 1)
                + GetDarkHunterExperienceBonus()
                + vengeanceExperienceBonus;
            var gainedLevels = hero.Model.AddExperience(experienceReward);
            var vengeanceProgress = hero.Model.RegisterVengeanceMobDefeated(mob.Model);
            gainedLevels += vengeanceProgress.GainedLevels;
            GameAudioController.Play(GameSfx.Deposit, mazeRenderer.GridToWorld(hero.Model.Position));
            GameDebugLog.Info(
                "Combat",
                $"Mob defeated: {mob.DebugName}, rewardGold={reward}, rewardXP={experienceReward}, vengeanceGoldBonus={vengeanceGoldBonus}, vengeanceXPBonus={vengeanceExperienceBonus}, vengeanceProgress={vengeanceProgress.Message}, darkSpawn={mob.Model.SpawnedFromDarkness}, heroGold={hero.Model.Gold}, heroXP={hero.Model.Experience}/{hero.Model.ExperienceForNextLevel}, heroLevel={hero.Model.Level}, gainedLevels={gainedLevels}");
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
            ShowVengeanceProgress(vengeanceProgress, rewardProfile.IsBoss ? 2.65f : 2.35f);

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

        private void ShowVengeanceProgress(HeroVengeanceProgressResult result, float baseDelay)
        {
            if (!result.HasAnyFeedback || mazeRenderer == null || hero == null || hero.Model == null)
            {
                return;
            }

            var delay = baseDelay;
            if (result.Completed)
            {
                DamageNumberView.CreateText(
                    mazeRenderer,
                    hero.Model.Position,
                    result.Message,
                    new Color(1f, 0.72f, 0.28f),
                    delay);
                delay += 0.3f;
                GameAudioController.Play(GameSfx.LevelUp, mazeRenderer.GridToWorld(hero.Model.Position), 0.65f);
            }

            if (result.BonusGold > 0)
            {
                DamageNumberView.CreateText(
                    mazeRenderer,
                    hero.Model.Position,
                    $"+{result.BonusGold} зол. клятвы",
                    new Color(1f, 0.84f, 0.26f),
                    delay);
                delay += 0.3f;
            }

            if (result.BonusExperience > 0)
            {
                DamageNumberView.CreateText(
                    mazeRenderer,
                    hero.Model.Position,
                    $"+{result.BonusExperience} XP клятвы",
                    new Color(0.55f, 0.86f, 1f),
                    delay);
            }
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
                switch (model.Species)
                {
                    case MobSpecies.Rat:
                        return new RewardProfile(MinBossRatGoldReward, MaxBossRatGoldReward, MinBossRatExperienceReward, MaxBossRatExperienceReward, true);
                    case MobSpecies.Goblin:
                        return new RewardProfile(MinBossGoblinGoldReward, MaxBossGoblinGoldReward, MinBossGoblinExperienceReward, MaxBossGoblinExperienceReward, true);
                    case MobSpecies.Orc:
                    default:
                        return new RewardProfile(MinBossOrcGoldReward, MaxBossOrcGoldReward, MinBossOrcExperienceReward, MaxBossOrcExperienceReward, true);
                }
            }

            if (model != null && model.IsMiniBoss)
            {
                switch (model.Species)
                {
                    case MobSpecies.Rat:
                        return new RewardProfile(MinMiniBossRatGoldReward, MaxMiniBossRatGoldReward, MinMiniBossRatExperienceReward, MaxMiniBossRatExperienceReward, false);
                    case MobSpecies.Goblin:
                        return new RewardProfile(MinMiniBossGoblinGoldReward, MaxMiniBossGoblinGoldReward, MinMiniBossGoblinExperienceReward, MaxMiniBossGoblinExperienceReward, false);
                    case MobSpecies.Orc:
                    default:
                        return new RewardProfile(MinMiniBossOrcGoldReward, MaxMiniBossOrcGoldReward, MinMiniBossOrcExperienceReward, MaxMiniBossOrcExperienceReward, false);
                }
            }

            switch (model?.Species)
            {
                case MobSpecies.Rat:
                    return new RewardProfile(MinRatGoldReward, MaxRatGoldReward, MinRatExperienceReward, MaxRatExperienceReward, false);
                case MobSpecies.Goblin:
                    return new RewardProfile(MinGoblinGoldReward, MaxGoblinGoldReward, MinGoblinExperienceReward, MaxGoblinExperienceReward, false);
                case MobSpecies.Orc:
                default:
                    return new RewardProfile(MinOrcGoldReward, MaxOrcGoldReward, MinOrcExperienceReward, MaxOrcExperienceReward, false);
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
            public RewardProfile(int minGold, int maxGold, int minExperience, int maxExperience, bool isBoss)
            {
                MinGold = minGold;
                MaxGold = maxGold;
                MinExperience = minExperience;
                MaxExperience = maxExperience;
                IsBoss = isBoss;
            }

            public int MinGold { get; }

            public int MaxGold { get; }

            public int MinExperience { get; }

            public int MaxExperience { get; }

            public bool IsBoss { get; }
        }
    }
}
