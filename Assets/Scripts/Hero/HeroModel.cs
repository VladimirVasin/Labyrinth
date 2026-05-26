using UnityEngine;

namespace Labyrinth.Hero
{
    public sealed class HeroModel
    {
        public const int DefaultHitPoints = 10;
        public const int DefaultMaxStamina = 10;
        public const int BaseAttackPoints = 10;
        public const int BaseArmorPoints = 0;
        public const int StaminaPerLevel = 1;
        public const int HitPointsPerLevel = 1;
        public const int FirstLevelExperienceCost = 15;
        public const int ExperienceCostGrowthPerLevel = 5;
        public const int ExperiencePerNewCell = 1;
        public const int GoldPerNewCell = 1;
        public const int PilgrimLightSightBonus = 2;
        public const int StrongBonesHitPointBonus = 4;
        public const int TrueHandAttackBonus = 5;
        public const int HeartyPathStaminaBonus = 7;
        public const int DarkHunterExperienceBonus = 8;
        public const float GoldenVowRewardMultiplier = 1.35f;

        public HeroModel(Vector2Int startPosition, HeroMemory memory)
        {
            Position = startPosition;
            Memory = memory;
            Visibility = new HeroVisibility();
            Inventory = HeroInventory.CreateDefault();
            Blessings = new HeroBlessings();
            MaxHitPoints = DefaultHitPoints;
            HitPoints = MaxHitPoints;
            Gold = 0;
            Level = 1;
            Experience = 0;
            MaxStamina = DefaultMaxStamina;
            Stamina = MaxStamina;
            State = HeroState.Exploring;
        }

        public Vector2Int Position { get; private set; }

        public HeroMemory Memory { get; }

        public HeroVisibility Visibility { get; }

        public HeroInventory Inventory { get; }

        public HeroBlessings Blessings { get; }

        public string BlessingText => HeroBlessingCatalog.FormatActiveNames(Blessings.Active);

        public int HitPoints { get; private set; }

        public int MaxHitPoints { get; private set; }

        public int AttackPoints => BaseAttackPoints + Inventory.AttackBonus;

        public int ArmorPoints => BaseArmorPoints + Inventory.ArmorBonus;

        public int MoveSpeedBonusPercent => Inventory.MoveSpeedBonusPercent;

        public int SightRange => HeroVisibility.SightRange
            + (HasBlessing(HeroBlessingType.PilgrimLight) ? PilgrimLightSightBonus : 0);

        public int FirstHitBlessingBonus => HasBlessing(HeroBlessingType.TrueHand)
            ? TrueHandAttackBonus
            : 0;

        public int Gold { get; private set; }

        public int Experience { get; private set; }

        public int Level { get; private set; }

        public int ExperienceForNextLevel => GetExperienceRequiredForLevel(Level + 1);

        public int MaxStamina { get; private set; }

        public int Stamina { get; private set; }

        public HeroState State { get; private set; }

        public int StepsTaken { get; private set; }

        public bool IsAlive => HitPoints > 0;

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
            var hitPointsBeforeDamage = HitPoints;
            if (damage >= HitPoints && Blessings.TryConsume(HeroBlessingType.LastBreath))
            {
                HitPoints = 1;
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

            MaxHitPoints = Mathf.Max(DefaultHitPoints, MaxHitPoints - StrongBonesHitPointBonus);
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
            Gold += Mathf.Max(0, amount);
        }

        public bool TrySpendGold(int amount)
        {
            var normalizedAmount = Mathf.Max(0, amount);
            if (Gold < normalizedAmount)
            {
                return false;
            }

            Gold -= normalizedAmount;
            return true;
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
            AddGold(GoldPerNewCell);
            return AddExperience(ExperiencePerNewCell);
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
    }
}
