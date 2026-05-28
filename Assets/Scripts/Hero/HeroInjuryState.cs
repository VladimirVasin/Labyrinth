using Labyrinth.Mobs;
using UnityEngine;

namespace Labyrinth.Hero
{
    public enum HeroSevereInjuryType
    {
        None,
        Limp,
        CutArm,
        CrackedRibs,
        PiercedSide,
        Concussion
    }

    public enum HeroScarType
    {
        None,
        RatBites,
        GoblinMockery,
        OrcFracture,
        DarkBurn,
        LastBreathMark
    }

    public enum HeroCharacterTraitType
    {
        None,
        BarrierHeir,
        DarkWary,
        BannerHeir,
        GoldGuilt,
        NameKeeper
    }

    public static class HeroInjuryCatalog
    {
        public static string GetSevereInjuryName(HeroSevereInjuryType type)
        {
            switch (type)
            {
                case HeroSevereInjuryType.Limp:
                    return "Хромота";
                case HeroSevereInjuryType.CutArm:
                    return "Рассеченная рука";
                case HeroSevereInjuryType.CrackedRibs:
                    return "Треснувшие ребра";
                case HeroSevereInjuryType.PiercedSide:
                    return "Пробитый бок";
                case HeroSevereInjuryType.Concussion:
                    return "Потрясение";
                default:
                    return "нет";
            }
        }

        public static string GetScarName(HeroScarType type)
        {
            switch (type)
            {
                case HeroScarType.RatBites:
                    return "Шрам от крысиных зубов";
                case HeroScarType.GoblinMockery:
                    return "Гоблинская насмешка";
                case HeroScarType.OrcFracture:
                    return "Орочий перелом";
                case HeroScarType.DarkBurn:
                    return "Ожог тьмы";
                case HeroScarType.LastBreathMark:
                    return "Рубец последнего вздоха";
                default:
                    return "нет";
            }
        }

        public static string GetSevereInjuryShortName(HeroSevereInjuryType type)
        {
            switch (type)
            {
                case HeroSevereInjuryType.Limp:
                    return "хромота";
                case HeroSevereInjuryType.CutArm:
                    return "рука";
                case HeroSevereInjuryType.CrackedRibs:
                    return "ребра";
                case HeroSevereInjuryType.PiercedSide:
                    return "бок";
                case HeroSevereInjuryType.Concussion:
                    return "сотряс.";
                default:
                    return "нет";
            }
        }

        public static string GetScarShortName(HeroScarType type)
        {
            switch (type)
            {
                case HeroScarType.RatBites:
                    return "крысы";
                case HeroScarType.GoblinMockery:
                    return "гоблин";
                case HeroScarType.OrcFracture:
                    return "орк";
                case HeroScarType.DarkBurn:
                    return "тьма";
                case HeroScarType.LastBreathMark:
                    return "вдох";
                default:
                    return "нет";
            }
        }

        public static string GetCharacterTraitShortName(HeroCharacterTraitType type)
        {
            switch (type)
            {
                case HeroCharacterTraitType.BarrierHeir:
                    return "двери";
                case HeroCharacterTraitType.DarkWary:
                    return "тьма";
                case HeroCharacterTraitType.BannerHeir:
                    return "знамя";
                case HeroCharacterTraitType.GoldGuilt:
                    return "золото";
                case HeroCharacterTraitType.NameKeeper:
                    return "имя";
                default:
                    return "нет";
            }
        }

        public static string GetCharacterTraitName(HeroCharacterTraitType type)
        {
            switch (type)
            {
                case HeroCharacterTraitType.BarrierHeir:
                    return "Наследник преграды";
                case HeroCharacterTraitType.DarkWary:
                    return "Осторожен во тьме";
                case HeroCharacterTraitType.BannerHeir:
                    return "Наследник знамени";
                case HeroCharacterTraitType.GoldGuilt:
                    return "Золотая вина";
                case HeroCharacterTraitType.NameKeeper:
                    return "Хранитель имен";
                default:
                    return "нет";
            }
        }

        public static string BuildSevereInjuryTooltip(HeroSevereInjuryType type)
        {
            switch (type)
            {
                case HeroSevereInjuryType.Limp:
                    return "Тяжелая рана. Замедляет движение и снижает боевую выносливость. Лечится только в лазарете.";
                case HeroSevereInjuryType.CutArm:
                    return "Тяжелая рана. Снижает Attack Points. Лечится только в лазарете.";
                case HeroSevereInjuryType.CrackedRibs:
                    return "Тяжелая рана. Снижает боевую выносливость. Лечится только в лазарете.";
                case HeroSevereInjuryType.PiercedSide:
                    return "Тяжелая рана. Снижает броню и боевую выносливость. Лечится только в лазарете.";
                case HeroSevereInjuryType.Concussion:
                    return "Тяжелая рана. Снижает видимость и боевую выносливость. Лечится только в лазарете.";
                default:
                    return "Тяжелой раны нет.";
            }
        }

        public static string BuildScarTooltip(HeroScarType type)
        {
            switch (type)
            {
                case HeroScarType.RatBites:
                    return "Личный шрам. Боевой темп ниже на 1, но +1 урон крысам. Не лечится и не наследуется.";
                case HeroScarType.GoblinMockery:
                    return "Личный шрам. Гоблины бесят сильнее: +1 XP за гоблинов. Не лечится и не наследуется.";
                case HeroScarType.OrcFracture:
                    return "Личный шрам. -1 Attack Points, но входящий урон от орков ниже на 1. Не лечится и не наследуется.";
                case HeroScarType.DarkBurn:
                    return "Личный шрам. -1 видимость, но +1 урон мобам из тьмы. Не лечится и не наследуется.";
                case HeroScarType.LastBreathMark:
                    return "Личный шрам после чудом пережитой смерти. Первый входящий удар в бою слабее на 1. Не лечится и не наследуется.";
                default:
                    return "Личного шрама нет.";
            }
        }

        public static string BuildCharacterTraitTooltip(HeroCharacterTraitType type)
        {
            switch (type)
            {
                case HeroCharacterTraitType.BarrierHeir:
                    return "Черта наследника из истории смерти предка у преграды. Герой немного быстрее на маршрутах к дверям и спускам.";
                case HeroCharacterTraitType.DarkWary:
                    return "Черта наследника из темной смерти предка. +1 к личной видимости.";
                case HeroCharacterTraitType.BannerHeir:
                    return "Черта наследника после смерти от элиты или босса. +1 урон мини-боссам и боссам.";
                case HeroCharacterTraitType.GoldGuilt:
                    return "Черта наследника после смерти с ценной добычей. +10% личного золота за победы.";
                case HeroCharacterTraitType.NameKeeper:
                    return "Черта наследника после смерти с чужим жетоном. Возвращение жетонов дает +3 XP.";
                default:
                    return "Особой черты характера нет.";
            }
        }

        public static int GetSevereAttackPenalty(HeroSevereInjuryType type)
        {
            return type == HeroSevereInjuryType.CutArm ? 1 : 0;
        }

        public static int GetSevereArmorPenalty(HeroSevereInjuryType type)
        {
            return type == HeroSevereInjuryType.PiercedSide ? 1 : 0;
        }

        public static int GetSevereCombatStaminaPenalty(HeroSevereInjuryType type)
        {
            switch (type)
            {
                case HeroSevereInjuryType.Limp:
                case HeroSevereInjuryType.PiercedSide:
                case HeroSevereInjuryType.Concussion:
                    return 1;
                case HeroSevereInjuryType.CrackedRibs:
                    return 2;
                default:
                    return 0;
            }
        }

        public static int GetSevereMoveSpeedPenaltyPercent(HeroSevereInjuryType type)
        {
            return type == HeroSevereInjuryType.Limp ? 15 : 0;
        }

        public static int GetSevereSightPenalty(HeroSevereInjuryType type)
        {
            return type == HeroSevereInjuryType.Concussion ? 1 : 0;
        }

        public static int GetScarAttackPenalty(HeroScarType type)
        {
            return type == HeroScarType.OrcFracture ? 1 : 0;
        }

        public static int GetScarCombatStaminaPenalty(HeroScarType type)
        {
            return type == HeroScarType.RatBites ? 1 : 0;
        }

        public static int GetScarSightPenalty(HeroScarType type)
        {
            return type == HeroScarType.DarkBurn ? 1 : 0;
        }

        public static int GetCharacterMoveSpeedBonusPercent(HeroCharacterTraitType type)
        {
            return type == HeroCharacterTraitType.BarrierHeir ? 6 : 0;
        }

        public static int GetCharacterSightBonus(HeroCharacterTraitType type)
        {
            return type == HeroCharacterTraitType.DarkWary ? 1 : 0;
        }

        public static int GetPersonalAttackBonus(
            HeroScarType scar,
            HeroCharacterTraitType trait,
            MobModel target,
            bool isOpeningAttack)
        {
            if (target == null)
            {
                return 0;
            }

            var bonus = 0;
            if (scar == HeroScarType.RatBites && target.Species == MobSpecies.Rat)
            {
                bonus++;
            }

            if (scar == HeroScarType.DarkBurn && target.SpawnedFromDarkness)
            {
                bonus++;
            }

            if (trait == HeroCharacterTraitType.BannerHeir && (target.IsMiniBoss || target.IsBoss))
            {
                bonus++;
            }

            return bonus;
        }

        public static int ApplyPersonalIncomingAttackModifier(
            HeroScarType scar,
            MobModel attacker,
            int incomingAttack,
            bool isOpeningAttack,
            out int reduction)
        {
            reduction = 0;
            if (attacker == null || incomingAttack <= 0)
            {
                return incomingAttack;
            }

            if (scar == HeroScarType.OrcFracture && attacker.Species == MobSpecies.Orc)
            {
                reduction++;
            }

            if (scar == HeroScarType.LastBreathMark && isOpeningAttack)
            {
                reduction++;
            }

            return Mathf.Max(0, incomingAttack - reduction);
        }

        public static int GetPersonalExperienceRewardBonus(
            HeroScarType scar,
            HeroCharacterTraitType trait,
            MobModel defeatedMob)
        {
            if (defeatedMob == null)
            {
                return 0;
            }

            return scar == HeroScarType.GoblinMockery && defeatedMob.Species == MobSpecies.Goblin ? 1 : 0;
        }

        public static int GetCharacterGoldRewardBonus(
            HeroCharacterTraitType trait,
            int goldReward)
        {
            return trait == HeroCharacterTraitType.GoldGuilt
                ? Mathf.CeilToInt(Mathf.Max(0, goldReward) * 0.1f)
                : 0;
        }

        public static HeroSevereInjuryType ChooseSevereInjury(MobModel attacker, int damage, int heroMaxHitPoints)
        {
            if (attacker == null)
            {
                return HeroSevereInjuryType.Concussion;
            }

            if (attacker.IsBoss || damage >= Mathf.Max(6, heroMaxHitPoints / 2))
            {
                return attacker.Species == MobSpecies.Orc
                    ? HeroSevereInjuryType.PiercedSide
                    : HeroSevereInjuryType.CrackedRibs;
            }

            if (attacker.SpawnedFromDarkness)
            {
                return HeroSevereInjuryType.Concussion;
            }

            switch (attacker.Species)
            {
                case MobSpecies.Rat:
                    return HeroSevereInjuryType.CrackedRibs;
                case MobSpecies.Goblin:
                    return HeroSevereInjuryType.CutArm;
                case MobSpecies.Orc:
                default:
                    return HeroSevereInjuryType.Limp;
            }
        }

        public static HeroScarType ChooseScar(HeroSevereInjuryType severeInjury, MobModel attacker)
        {
            if (attacker != null && attacker.SpawnedFromDarkness)
            {
                return HeroScarType.DarkBurn;
            }

            if (attacker != null)
            {
                switch (attacker.Species)
                {
                    case MobSpecies.Rat:
                        return HeroScarType.RatBites;
                    case MobSpecies.Goblin:
                        return HeroScarType.GoblinMockery;
                    case MobSpecies.Orc:
                        return HeroScarType.OrcFracture;
                }
            }

            return severeInjury == HeroSevereInjuryType.Concussion
                ? HeroScarType.DarkBurn
                : HeroScarType.LastBreathMark;
        }

        public static HeroCharacterTraitType ChooseSuccessorTrait(HeroLineageMember fallenMember)
        {
            if (fallenMember == null || !fallenMember.HasDeathContext)
            {
                return HeroCharacterTraitType.None;
            }

            var death = fallenMember.DeathContext;
            if (death.NearBarrier)
            {
                return HeroCharacterTraitType.BarrierHeir;
            }

            if (death.DiedInDarkness || death.KillerSpawnedFromDarkness)
            {
                return HeroCharacterTraitType.DarkWary;
            }

            if (death.HasKiller && death.KillerRank != MobRank.Regular)
            {
                return HeroCharacterTraitType.BannerHeir;
            }

            if (death.CarriedGoldIngot)
            {
                return HeroCharacterTraitType.GoldGuilt;
            }

            return death.CarriedDeathToken ? HeroCharacterTraitType.NameKeeper : HeroCharacterTraitType.None;
        }
    }
}
