using Labyrinth.Mobs;
using UnityEngine;

namespace Labyrinth.Hero
{
    public sealed partial class HeroModel
    {
        private const int SevereInjuryMinimumWounds = 2;
        private const int ScarMinimumWounds = 3;

        public HeroSevereInjuryType SevereInjury { get; private set; }

        public HeroScarType PersonalScar { get; private set; }

        public HeroCharacterTraitType CharacterTrait { get; private set; }

        public bool HasSevereInjury => SevereInjury != HeroSevereInjuryType.None;

        public bool HasPersonalScar => PersonalScar != HeroScarType.None;

        public string SevereInjuryText => HeroInjuryCatalog.GetSevereInjuryName(SevereInjury);

        public string PersonalScarText => HeroInjuryCatalog.GetScarName(PersonalScar);

        public string CharacterTraitText => HeroInjuryCatalog.GetCharacterTraitName(CharacterTrait);

        public string SevereInjuryCompactText => HeroInjuryCatalog.GetSevereInjuryShortName(SevereInjury);

        public string PersonalScarCompactText => HeroInjuryCatalog.GetScarShortName(PersonalScar);

        public string CharacterTraitCompactText => HeroInjuryCatalog.GetCharacterTraitShortName(CharacterTrait);

        public string InjuryDebugText => $"wounds={CombatWounds}, severe={SevereInjury}, scar={PersonalScar}, trait={CharacterTrait}";

        private int SevereInjuryAttackPenalty => HeroInjuryCatalog.GetSevereAttackPenalty(SevereInjury);

        private int SevereInjuryArmorPenalty => HeroInjuryCatalog.GetSevereArmorPenalty(SevereInjury);

        private int SevereInjuryCombatStaminaPenalty => HeroInjuryCatalog.GetSevereCombatStaminaPenalty(SevereInjury);

        private int SevereInjuryMoveSpeedPenaltyPercent => HeroInjuryCatalog.GetSevereMoveSpeedPenaltyPercent(SevereInjury);

        private int SevereInjurySightPenalty => HeroInjuryCatalog.GetSevereSightPenalty(SevereInjury);

        private int PersonalScarAttackPenalty => HeroInjuryCatalog.GetScarAttackPenalty(PersonalScar);

        private int PersonalScarCombatStaminaPenalty => HeroInjuryCatalog.GetScarCombatStaminaPenalty(PersonalScar);

        private int PersonalScarSightPenalty => HeroInjuryCatalog.GetScarSightPenalty(PersonalScar);

        private int CharacterMoveSpeedBonusPercent => HeroInjuryCatalog.GetCharacterMoveSpeedBonusPercent(CharacterTrait);

        private int CharacterSightBonus => HeroInjuryCatalog.GetCharacterSightBonus(CharacterTrait);

        public void AssignCharacterTrait(HeroCharacterTraitType trait)
        {
            CharacterTrait = trait;
        }

        public bool TryApplySevereInjuryFromCombat(
            MobModel attacker,
            int damage,
            System.Random random,
            out HeroSevereInjuryType severeGained,
            out HeroScarType scarGained)
        {
            severeGained = HeroSevereInjuryType.None;
            scarGained = HeroScarType.None;
            if (!IsAlive || damage <= 0)
            {
                return false;
            }

            var elite = attacker != null && (attacker.IsMiniBoss || attacker.IsBoss);
            var dark = attacker != null && attacker.SpawnedFromDarkness;
            var highDamage = damage >= Mathf.Max(5, Mathf.CeilToInt(MaxHitPoints * 0.42f));
            var lowHealth = HitPoints <= Mathf.Max(2, Mathf.CeilToInt(MaxHitPoints * 0.35f));
            var criticalHealth = HitPoints <= Mathf.Max(1, Mathf.CeilToInt(MaxHitPoints * 0.18f));
            if (!HasSevereInjury && (CombatWounds >= SevereInjuryMinimumWounds || highDamage || elite || lowHealth))
            {
                var chance = Mathf.Clamp(
                    12
                    + CombatWounds * 10
                    + damage * 4
                    + (elite ? 20 : 0)
                    + (dark ? 12 : 0)
                    + (lowHealth ? 18 : 0)
                    + (criticalHealth ? 14 : 0),
                    0,
                    85);
                if (RollPercent(chance, random))
                {
                    SevereInjury = HeroInjuryCatalog.ChooseSevereInjury(attacker, damage, MaxHitPoints);
                    severeGained = SevereInjury;
                    return true;
                }
            }

            if (!HasSevereInjury
                || HasPersonalScar
                || (!highDamage && !elite && !criticalHealth && !(lowHealth && damage >= 2) && CombatWounds < ScarMinimumWounds))
            {
                return false;
            }

            var scarChance = Mathf.Clamp(
                6
                + CombatWounds * 8
                + damage * 3
                + (elite ? 10 : 0)
                + (dark ? 12 : 0)
                + (lowHealth ? 10 : 0)
                + (criticalHealth ? 18 : 0),
                0,
                70);
            if (!RollPercent(scarChance, random))
            {
                return false;
            }

            return TryGainScar(HeroInjuryCatalog.ChooseScar(SevereInjury, attacker), out scarGained);
        }

        public bool TryGainLastBreathScar(out HeroScarType scarGained)
        {
            return TryGainScar(HeroScarType.LastBreathMark, out scarGained);
        }

        public HeroSevereInjuryType HealSevereInjury()
        {
            var healed = SevereInjury;
            SevereInjury = HeroSevereInjuryType.None;
            return healed;
        }

        public int GetPersonalAttackBonus(MobModel target, bool isOpeningAttack)
        {
            return HeroInjuryCatalog.GetPersonalAttackBonus(PersonalScar, CharacterTrait, target, isOpeningAttack);
        }

        public int ApplyPersonalIncomingAttackModifier(
            MobModel attacker,
            int incomingAttack,
            bool isOpeningAttack,
            out int reduction)
        {
            return HeroInjuryCatalog.ApplyPersonalIncomingAttackModifier(
                PersonalScar,
                attacker,
                incomingAttack,
                isOpeningAttack,
                out reduction);
        }

        public int GetPersonalExperienceRewardBonus(MobModel defeatedMob)
        {
            return HeroInjuryCatalog.GetPersonalExperienceRewardBonus(PersonalScar, CharacterTrait, defeatedMob);
        }

        public int GetCharacterGoldRewardBonus(MobModel defeatedMob, int goldReward)
        {
            return defeatedMob != null
                ? HeroInjuryCatalog.GetCharacterGoldRewardBonus(CharacterTrait, goldReward)
                : 0;
        }

        public int GetCharacterDeathTokenExperienceBonus()
        {
            return CharacterTrait == HeroCharacterTraitType.NameKeeper ? 3 : 0;
        }

        private bool TryGainScar(HeroScarType scar, out HeroScarType scarGained)
        {
            scarGained = HeroScarType.None;
            if (scar == HeroScarType.None || HasPersonalScar)
            {
                return false;
            }

            PersonalScar = scar;
            scarGained = scar;
            return true;
        }

        private static bool RollPercent(int chance, System.Random random)
        {
            var roll = random != null ? random.Next(100) : Random.Range(0, 100);
            return roll < Mathf.Clamp(chance, 0, 100);
        }
    }
}
