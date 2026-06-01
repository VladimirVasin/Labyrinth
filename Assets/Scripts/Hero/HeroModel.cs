using Labyrinth.Mobs;
using Labyrinth.Core;
using UnityEngine;

namespace Labyrinth.Hero
{
    public sealed partial class HeroModel
    {
        public const int DefaultHitPoints = 10;
        public const int DefaultMaxStamina = 10;
        public const int DefaultBaseAttackPoints = 10;
        public const int DefaultBaseArmorPoints = 0;
        public const int StaminaPerLevel = 1;
        public const int HitPointsPerLevel = 1;
        public const int FirstLevelExperienceCost = 15;
        public const int ExperienceCostGrowthPerLevel = 5;
        public const int ExperiencePerNewCell = 1;
        public const int GoldPerCommonMapCell = 1;
        public const int PilgrimLightSightBonus = 2;
        public const int StrongBonesHitPointBonus = 4;
        public const int TrueHandAttackBonus = 5;
        public const int HeartyPathStaminaBonus = 7;
        public const int DarkHunterExperienceBonus = 8;
        public const float GoldenVowRewardMultiplier = 1.35f;

        private static readonly IntRange HeroHitPointRange = new IntRange(9, 12);
        private static readonly IntRange HeroStaminaRange = new IntRange(9, 12);
        private static readonly IntRange HeroAttackRange = new IntRange(9, 11);
        private static readonly IntRange HeroArmorRange = new IntRange(0, 1);

        public HeroModel(Vector2Int startPosition, HeroMemory memory, int statSeed = 0)
        {
            Position = startPosition;
            Memory = memory;
            Visibility = new HeroVisibility();
            Inventory = HeroInventory.CreateDefault();
            Blessings = new HeroBlessings();
            var baseStats = BuildBaseStats(statSeed);
            BaseMaxHitPoints = baseStats.MaxHitPoints;
            BaseMaxStamina = baseStats.MaxStamina;
            BaseAttackPoints = baseStats.AttackPoints;
            BaseArmorPoints = baseStats.ArmorPoints;
            MaxHitPoints = BaseMaxHitPoints;
            HitPoints = MaxHitPoints;
            Gold = 0;
            Level = 1;
            Experience = 0;
            MaxStamina = BaseMaxStamina;
            Stamina = MaxStamina;
            State = HeroState.Exploring;
        }

        public Vector2Int Position { get; private set; }

        public HeroMemory Memory { get; }

        public HeroVisibility Visibility { get; }

        public HeroInventory Inventory { get; }

        public int DisplayNumber { get; private set; }

        public string DisplayName { get; private set; } = string.Empty;

        public HeroBlessings Blessings { get; }

        public string BlessingText => HeroBlessingCatalog.FormatActiveNames(Blessings.Active);

        public HeroVengeanceQuest VengeanceQuest { get; private set; } = HeroVengeanceQuest.CreateNone();

        public string VengeanceText => VengeanceQuest != null ? VengeanceQuest.DisplayTitle : "нет";

        public string VengeanceTooltipText => VengeanceQuest != null ? VengeanceQuest.TooltipText : string.Empty;

        public int LineageTrainingScore { get; private set; }

        public int LineageHitPointBonus { get; private set; }

        public int LineageStaminaBonus { get; private set; }

        public int LineageAttackBonus { get; private set; }

        public string LineageBonusText => FormatLineageBonusText();

        public int HitPoints { get; private set; }

        public int MaxHitPoints { get; private set; }

        public int BaseMaxHitPoints { get; }

        public int BaseMaxStamina { get; }

        public int BaseAttackPoints { get; }

        public int BaseArmorPoints { get; }

        public int CombatWounds { get; private set; }

        public int AttackWoundPenalty => Mathf.Min(CombatWounds, 4);

        public int CombatStaminaWoundPenalty => Mathf.Min(CombatWounds, 5)
            + SevereInjuryCombatStaminaPenalty
            + PersonalScarCombatStaminaPenalty;

        public int AttackPoints => Mathf.Max(
            1,
            BaseAttackPoints
                + Inventory.AttackBonus
                + LineageAttackBonus
                - AttackWoundPenalty
                - SevereInjuryAttackPenalty
                - PersonalScarAttackPenalty);

        public int ArmorPoints => Mathf.Max(
            0,
            BaseArmorPoints
                + Inventory.ArmorBonus
                + GetVengeanceArmorBonus()
                - SevereInjuryArmorPenalty);

        public int MoveSpeedBonusPercent => Mathf.Clamp(
            Inventory.MoveSpeedBonusPercent + CharacterMoveSpeedBonusPercent - SevereInjuryMoveSpeedPenaltyPercent,
            -40,
            80);

        public int SightRange => Mathf.Max(
            1,
            HeroVisibility.SightRange
                + (HasBlessing(HeroBlessingType.PilgrimLight) ? PilgrimLightSightBonus : 0)
                + GetVengeanceSightBonus()
                + CharacterSightBonus
                - SevereInjurySightPenalty
                - PersonalScarSightPenalty);

        public int FirstHitBlessingBonus => HasBlessing(HeroBlessingType.TrueHand)
            ? TrueHandAttackBonus
            : 0;

        public int Gold { get; private set; }

        public int HouseFundEligibleGold { get; private set; }

        public int Experience { get; private set; }

        public int Level { get; private set; }

        public int ExperienceForNextLevel => GetExperienceRequiredForLevel(Level + 1);

        public int MaxStamina { get; private set; }

        public int Stamina { get; private set; }

        public HeroState State { get; private set; }

        public int StepsTaken { get; private set; }

        public bool IsAlive => HitPoints > 0;

        public int DungeonLevel { get; private set; } = 1;

        public bool HasCompletedVengeance(HeroVengeanceKind kind)
        {
            return VengeanceQuest != null && VengeanceQuest.Kind == kind && VengeanceQuest.IsCompleted;
        }

        public void SetIdentity(int displayNumber, string displayName)
        {
            DisplayNumber = Mathf.Max(0, displayNumber);
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? string.Empty : displayName;
        }

        public void AssignVengeanceQuest(HeroVengeanceQuest quest)
        {
            VengeanceQuest = quest ?? HeroVengeanceQuest.CreateNone();
        }

        public void ApplyLineageBonus(HeroLineageTrainingBonus bonus)
        {
            if (LineageTrainingScore > 0 || LineageHitPointBonus > 0 || LineageStaminaBonus > 0 || LineageAttackBonus > 0)
            {
                return;
            }

            LineageTrainingScore = Mathf.Clamp(bonus.Score, 0, HeroLineageState.MaxTrainingScore);
            LineageHitPointBonus = Mathf.Max(0, bonus.HitPointBonus);
            LineageStaminaBonus = Mathf.Max(0, bonus.StaminaBonus);
            LineageAttackBonus = Mathf.Max(0, bonus.AttackBonus);

            MaxHitPoints += LineageHitPointBonus;
            HitPoints += LineageHitPointBonus;
            MaxStamina += LineageStaminaBonus;
            Stamina += LineageStaminaBonus;
        }

        public void SetDungeonLevel(int dungeonLevel)
        {
            DungeonLevel = Mathf.Max(1, dungeonLevel);
        }

        public void RememberCombatThreat(MobModel mob)
        {
            if (mob == null)
            {
                hasLastCombatThreat = false;
                return;
            }

            hasLastCombatThreat = true;
            lastCombatThreatSpecies = mob.Species;
            lastCombatThreatRank = mob.Rank;
            lastCombatThreatSpawnedFromDarkness = mob.SpawnedFromDarkness;
        }

        public HeroDeathContext BuildDeathContext(
            bool carriedGoldIngot,
            bool carriedDeathToken,
            bool diedInDarkness,
            bool nearBarrier,
            Vector2Int barrierPosition,
            string barrierName)
        {
            return new HeroDeathContext(
                hasLastCombatThreat,
                lastCombatThreatSpecies,
                lastCombatThreatRank,
                lastCombatThreatSpawnedFromDarkness,
                DungeonLevel,
                Position,
                carriedGoldIngot,
                carriedDeathToken,
                diedInDarkness || lastCombatThreatSpawnedFromDarkness,
                nearBarrier,
                barrierPosition,
                barrierName);
        }

        public int GetVengeanceAttackBonus(MobModel target, bool isOpeningAttack, int currentAttack)
        {
            if (target == null || VengeanceQuest == null || !VengeanceQuest.IsCompleted)
            {
                return 0;
            }

            switch (VengeanceQuest.Kind)
            {
                case HeroVengeanceKind.SmallTeeth:
                    return target.Species == MobSpecies.Rat ? 2 : 0;
                case HeroVengeanceKind.GreenGrin:
                    return target.Species == MobSpecies.Goblin && isOpeningAttack ? 3 : 0;
                case HeroVengeanceKind.OrcScar:
                    return target.Species == MobSpecies.Orc && isOpeningAttack ? 5 : 0;
                case HeroVengeanceKind.BrokenBanner:
                    return target.IsMiniBoss ? Mathf.CeilToInt(currentAttack * 0.1f) : 0;
                case HeroVengeanceKind.LastMonster:
                    return target.IsBoss ? Mathf.CeilToInt(currentAttack * 0.15f) : 0;
                default:
                    return 0;
            }
        }

        public int ApplyVengeanceIncomingAttackModifier(MobModel attacker, int incomingAttack, bool isOpeningAttack, out int reduction)
        {
            reduction = 0;
            if (attacker == null || VengeanceQuest == null || !VengeanceQuest.IsCompleted)
            {
                return incomingAttack;
            }

            if (VengeanceQuest.Kind == HeroVengeanceKind.LastMonster && attacker.IsBoss && isOpeningAttack)
            {
                reduction = Mathf.Max(1, Mathf.CeilToInt(incomingAttack * 0.25f));
            }
            else if (VengeanceQuest.Kind == HeroVengeanceKind.BrokenBanner && attacker.IsMiniBoss)
            {
                reduction = 1;
            }

            return Mathf.Max(0, incomingAttack - reduction);
        }

        public int GetVengeanceGoldRewardBonus(MobModel defeatedMob, int goldReward)
        {
            return defeatedMob != null
                && defeatedMob.Species == MobSpecies.Rat
                && HasCompletedVengeance(HeroVengeanceKind.SmallTeeth)
                ? Mathf.CeilToInt(Mathf.Max(0, goldReward) * 0.1f)
                : 0;
        }

        public int GetVengeanceExperienceRewardBonus(MobModel defeatedMob)
        {
            if (defeatedMob == null || VengeanceQuest == null || !VengeanceQuest.IsCompleted)
            {
                return 0;
            }

            switch (VengeanceQuest.Kind)
            {
                case HeroVengeanceKind.SmallTeeth:
                    return defeatedMob.Species == MobSpecies.Rat ? 1 : 0;
                case HeroVengeanceKind.OrcScar:
                    return defeatedMob.Species == MobSpecies.Orc ? 2 : 0;
                default:
                    return 0;
            }
        }

        public HeroVengeanceProgressResult RegisterVengeanceMobDefeated(MobModel defeatedMob)
        {
            return ApplyVengeanceCompletionReward(VengeanceQuest != null
                ? VengeanceQuest.RegisterMobDefeated(defeatedMob)
                : HeroVengeanceProgressResult.None);
        }

        public HeroVengeanceProgressResult RegisterVengeanceBarrierOpened(Vector2Int position, bool isStairs)
        {
            return ApplyVengeanceCompletionReward(VengeanceQuest != null
                ? VengeanceQuest.RegisterBarrierOpened(position, isStairs)
                : HeroVengeanceProgressResult.None);
        }

        public HeroVengeanceProgressResult ApplyGoldIngotDeliveryVengeance()
        {
            var progress = ApplyVengeanceCompletionReward(VengeanceQuest != null
                ? VengeanceQuest.RegisterGoldIngotDelivered()
                : HeroVengeanceProgressResult.None);
            if (!HasCompletedVengeance(HeroVengeanceKind.UndeliveredGold))
            {
                return progress;
            }

            AddGold(5);
            var gainedLevels = AddExperience(2);
            return progress.WithAppliedBonuses(5, 2, progress.GainedLevels + gainedLevels);
        }

        public HeroVengeanceProgressResult ApplyDeathTokenDeliveryVengeance()
        {
            var progress = ApplyVengeanceCompletionReward(VengeanceQuest != null
                ? VengeanceQuest.RegisterDeathTokenDelivered()
                : HeroVengeanceProgressResult.None);
            var characterExperienceBonus = GetCharacterDeathTokenExperienceBonus();
            if (!HasCompletedVengeance(HeroVengeanceKind.CarriedName))
            {
                if (characterExperienceBonus <= 0)
                {
                    return progress;
                }

                var traitLevels = AddExperience(characterExperienceBonus);
                return progress.WithAppliedBonuses(0, characterExperienceBonus, progress.GainedLevels + traitLevels);
            }

            var experienceBonus = 5 + characterExperienceBonus;
            var gainedLevels = AddExperience(experienceBonus);
            return progress.WithAppliedBonuses(0, experienceBonus, progress.GainedLevels + gainedLevels);
        }

        public void MoveTo(Vector2Int position)
        {
            Position = position;
            StepsTaken++;
        }

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

        public int ApplyCombatWound()
        {
            if (!IsAlive)
            {
                return CombatWounds;
            }

            CombatWounds = Mathf.Min(9, CombatWounds + 1);
            return CombatWounds;
        }

        public int HealCombatWounds(int amount)
        {
            var healed = Mathf.Min(Mathf.Max(0, amount), CombatWounds);
            CombatWounds -= healed;
            return healed;
        }

        private int ApplyDamage(int damage)
        {
            var hitPointsBeforeDamage = HitPoints;
            if (damage >= HitPoints && Blessings.TryConsume(HeroBlessingType.LastBreath))
            {
                HitPoints = 1;
                if (TryGainLastBreathScar(out var scarGained))
                {
                    GameDebugLog.Info("Hero", $"Last Breath scar gained: scar={scarGained}, {InjuryDebugText}");
                }

                return Mathf.Max(1, hitPointsBeforeDamage - HitPoints);
            }

            HitPoints = Mathf.Max(0, HitPoints - damage);
            if (HitPoints <= 0)
            {
                State = HeroState.Defeated;
            }

            return damage;
        }

        public bool HasBlessing(HeroBlessingType type)
        {
            return Blessings.Has(type);
        }

        public bool TryActivateBlessing(HeroBlessingType type)
        {
            if (!Blessings.TryActivate(type))
            {
                return false;
            }

            if (type == HeroBlessingType.StrongBones)
            {
                MaxHitPoints += StrongBonesHitPointBonus;
                HitPoints += StrongBonesHitPointBonus;
            }

            return true;
        }

        public void ClearExpeditionBlessings()
        {
            var hadStrongBones = HasBlessing(HeroBlessingType.StrongBones);
            Blessings.ClearExpedition();
            if (!hadStrongBones)
            {
                return;
            }

            MaxHitPoints = Mathf.Max(1, MaxHitPoints - StrongBonesHitPointBonus);
            HitPoints = Mathf.Min(HitPoints, MaxHitPoints);
        }

        public void MarkBlessingsLeftEntrance()
        {
            Blessings.MarkLeftEntrance();
        }

        public int ConsumeRationBlessingBonus()
        {
            return Blessings.TryConsume(HeroBlessingType.HeartyPath)
                ? HeartyPathStaminaBonus
                : 0;
        }

        public int ApplyGoldRewardBlessing(int baseReward)
        {
            return HasBlessing(HeroBlessingType.GoldenVow)
                ? Mathf.CeilToInt(baseReward * GoldenVowRewardMultiplier)
                : baseReward;
        }

        public void AddGold(int amount)
        {
            var normalizedAmount = Mathf.Max(0, amount);
            Gold += normalizedAmount;
            HouseFundEligibleGold += normalizedAmount;
        }

        public bool TrySpendGold(int amount)
        {
            var normalizedAmount = Mathf.Max(0, amount);
            if (Gold < normalizedAmount)
            {
                return false;
            }

            Gold -= normalizedAmount;
            HouseFundEligibleGold = Mathf.Max(0, HouseFundEligibleGold - normalizedAmount);
            return true;
        }

        public int ConsumeHouseFundEligibleGold()
        {
            var eligible = Mathf.Min(Gold, HouseFundEligibleGold);
            HouseFundEligibleGold = 0;
            return Mathf.Max(0, eligible);
        }

        public int RestoreHitPoints(int amount)
        {
            if (!IsAlive)
            {
                return 0;
            }

            var missingHitPoints = MaxHitPoints - HitPoints;
            var restored = Mathf.Min(Mathf.Max(0, amount), missingHitPoints);
            HitPoints += restored;
            return restored;
        }

        public int AddExperience(int amount)
        {
            var normalizedAmount = Mathf.Max(0, amount);
            if (normalizedAmount == 0)
            {
                return 0;
            }

            Experience += normalizedAmount;
            var gainedLevels = 0;
            while (Experience >= ExperienceForNextLevel)
            {
                Level++;
                MaxStamina += StaminaPerLevel;
                Stamina += StaminaPerLevel;
                MaxHitPoints += HitPointsPerLevel;
                HitPoints += HitPointsPerLevel;
                gainedLevels++;
            }

            return gainedLevels;
        }

        public int RewardNewCellExploration()
        {
            return RewardNewCellExploration(out _);
        }

        public int RewardNewCellExploration(out HeroVengeanceProgressResult vengeanceProgress)
        {
            var gainedLevels = AddExperience(ExperiencePerNewCell);
            vengeanceProgress = HeroVengeanceProgressResult.None;
            return gainedLevels;
        }

        public int RewardCommonMapContribution(int newWalkableCells)
        {
            var reward = Mathf.Max(0, newWalkableCells) * GoldPerCommonMapCell;
            if (reward > 0)
            {
                AddGold(reward);
            }

            return reward;
        }

        public bool TrySpendStamina(int amount)
        {
            var normalizedAmount = Mathf.Max(0, amount);
            if (Stamina < normalizedAmount)
            {
                return false;
            }

            Stamina -= normalizedAmount;
            return true;
        }

        public void RestoreStamina()
        {
            Stamina = MaxStamina;
        }

        public int RestoreStamina(int amount)
        {
            if (!IsAlive)
            {
                return 0;
            }

            var missingStamina = MaxStamina - Stamina;
            var restored = Mathf.Min(Mathf.Max(0, amount), missingStamina);
            Stamina += restored;
            return restored;
        }

        public void SetState(HeroState state)
        {
            State = state;
        }

        private static int GetExperienceRequiredForLevel(int level)
        {
            if (level <= 1)
            {
                return 0;
            }

            var previousLevel = level - 1;
            var required = (long)previousLevel
                * (FirstLevelExperienceCost * 2L + (long)(previousLevel - 1) * ExperienceCostGrowthPerLevel)
                / 2;
            return required > int.MaxValue ? int.MaxValue : (int)required;
        }

        private static HeroBaseStats BuildBaseStats(int statSeed)
        {
            var random = new System.Random(statSeed);
            return new HeroBaseStats(
                HeroHitPointRange.Roll(random),
                HeroStaminaRange.Roll(random),
                HeroAttackRange.Roll(random),
                HeroArmorRange.Roll(random));
        }

        private string FormatLineageBonusText()
        {
            var bonus = new HeroLineageTrainingBonus(LineageTrainingScore, LineageHitPointBonus, LineageStaminaBonus, LineageAttackBonus);
            return bonus.ToCompactText();
        }

        private HeroVengeanceProgressResult ApplyVengeanceCompletionReward(HeroVengeanceProgressResult result)
        {
            if (!result.HasAnyFeedback)
            {
                return result;
            }

            if (result.MaxHitPointBonus > 0)
            {
                MaxHitPoints += result.MaxHitPointBonus;
                HitPoints += result.MaxHitPointBonus;
            }

            if (result.MaxStaminaBonus > 0)
            {
                MaxStamina += result.MaxStaminaBonus;
                Stamina += result.MaxStaminaBonus;
            }

            var gainedLevels = 0;
            if (result.BonusGold > 0)
            {
                AddGold(result.BonusGold);
            }

            if (result.BonusExperience > 0)
            {
                gainedLevels += AddExperience(result.BonusExperience);
            }

            return result.WithAppliedBonuses(result.BonusGold, result.BonusExperience, result.GainedLevels + gainedLevels);
        }

        private int GetVengeanceSightBonus()
        {
            if (VengeanceQuest == null || !VengeanceQuest.IsCompleted)
            {
                return 0;
            }

            return VengeanceQuest.Kind == HeroVengeanceKind.GreenGrin
                ? 1
                : 0;
        }

        private int GetVengeanceArmorBonus()
        {
            return Inventory != null
                && Inventory.HasDeathToken
                && HasCompletedVengeance(HeroVengeanceKind.CarriedName)
                ? 1
                : 0;
        }

        private bool hasLastCombatThreat;
        private MobSpecies lastCombatThreatSpecies = MobSpecies.Orc;
        private MobRank lastCombatThreatRank = MobRank.Regular;
        private bool lastCombatThreatSpawnedFromDarkness;

        private readonly struct HeroBaseStats
        {
            public HeroBaseStats(int maxHitPoints, int maxStamina, int attackPoints, int armorPoints)
            {
                MaxHitPoints = maxHitPoints;
                MaxStamina = maxStamina;
                AttackPoints = attackPoints;
                ArmorPoints = armorPoints;
            }

            public int MaxHitPoints { get; }

            public int MaxStamina { get; }

            public int AttackPoints { get; }

            public int ArmorPoints { get; }
        }
    }
}
