using Labyrinth.Core;
using Labyrinth.Hero;
using UnityEngine;

namespace Labyrinth.Combat
{
    public sealed partial class CombatController
    {
        private const float HeroWoundHpScale = 0.2f;
        private const float HeroWoundPressureScale = 0.34f;
        private const float HeroLowHealthWoundScale = 0.35f;

        private bool TryApplyCombatWound(
            bool actorIsHero,
            int damage,
            CombatActionDefinition action,
            CombatActorState targetState)
        {
            if (damage <= 0)
            {
                return false;
            }

            var targetIsHero = !actorIsHero;
            if (targetIsHero && !hero.Model.IsAlive)
            {
                return false;
            }

            if (!targetIsHero && !mob.Model.IsAlive)
            {
                return false;
            }

            var threshold = CalculateWoundThreshold(targetIsHero, action);
            var pressureWound = targetIsHero && IsHeroUnderWoundPressure(damage);
            if (damage < threshold && !pressureWound)
            {
                return false;
            }

            var chance = CalculateWoundChance(targetIsHero, damage, action, pressureWound);
            if (rewardRandom.Next(100) >= chance)
            {
                return false;
            }

            if (targetIsHero)
            {
                targetState.SetWounds(hero.Model.ApplyCombatWound());
                TryApplyHeroTraumaFromWound(damage);
            }
            else
            {
                targetState.ApplyWound();
            }

            GameDebugLog.Info(
                "Combat",
                $"Round {roundNumber} wound: target={(targetIsHero ? "Hero" : "Mob")}, damage={damage}, threshold={threshold}, pressure={pressureWound}, chance={chance}, wounds={targetState.Wounds}.");
            return true;
        }

        private int CalculateWoundThreshold(bool targetIsHero, CombatActionDefinition action)
        {
            var threshold = targetIsHero
                ? Mathf.Clamp(Mathf.CeilToInt(hero.Model.MaxHitPoints * HeroWoundHpScale), HeroWoundDamageThreshold, 5)
                : mob.Model.IsBoss || mob.Model.IsMiniBoss
                    ? EliteWoundDamageThreshold
                    : MobWoundDamageThreshold;
            if (IsTraumaAction(action.Type))
            {
                threshold--;
            }

            return Mathf.Max(1, threshold);
        }

        private bool IsHeroUnderWoundPressure(int damage)
        {
            var pressureDamage = Mathf.Max(4, Mathf.CeilToInt(hero.Model.MaxHitPoints * HeroWoundPressureScale));
            var lowHealth = hero.Model.HitPoints <= Mathf.Max(2, Mathf.CeilToInt(hero.Model.MaxHitPoints * HeroLowHealthWoundScale));
            var accumulatedDamage = heroCombat != null && heroCombat.TotalDamageTaken + damage >= pressureDamage;
            return lowHealth || accumulatedDamage;
        }

        private int CalculateWoundChance(bool targetIsHero, int damage, CombatActionDefinition action, bool pressureWound)
        {
            if (!targetIsHero)
            {
                return Mathf.Clamp(16 + damage * 2, 0, 55);
            }

            var chance = 10 + damage * 7;
            if (action.Type == CombatActionType.HeavyStrike || action.Type == CombatActionType.DesperateStrike)
            {
                chance += 12;
            }
            else if (action.Type == CombatActionType.BreakArmor)
            {
                chance += 8;
            }
            else if (action.Type == CombatActionType.GuardedStrike)
            {
                chance += 4;
            }

            if (pressureWound)
            {
                chance += 14;
            }

            if (mob.Model.IsBoss || mob.Model.IsMiniBoss)
            {
                chance += 14;
            }

            if (mob.Model.SpawnedFromDarkness)
            {
                chance += 8;
            }

            return Mathf.Clamp(chance, 8, 82);
        }

        private void TryApplyHeroTraumaFromWound(int damage)
        {
            if (!hero.Model.TryApplySevereInjuryFromCombat(mob.Model, damage, rewardRandom, out var severeGained, out var scarGained))
            {
                return;
            }

            if (severeGained != HeroSevereInjuryType.None)
            {
                DamageNumberView.CreateCombatText(
                    mazeRenderer,
                    hero.Model.Position,
                    "тяжелая рана",
                    new Color(1f, 0.42f, 0.28f),
                    1.45f,
                    CombatStateFeedbackDelay + 0.18f);
            }

            if (scarGained != HeroScarType.None)
            {
                DamageNumberView.CreateCombatText(
                    mazeRenderer,
                    hero.Model.Position,
                    "шрам",
                    new Color(1f, 0.68f, 0.28f),
                    1.48f,
                    CombatStateFeedbackDelay + 0.36f);
            }

            GameDebugLog.Info(
                "Combat",
                $"Round {roundNumber} hero injury: severeGained={severeGained}, scarGained={scarGained}, {hero.Model.InjuryDebugText}, source={mob.DebugName}, damage={damage}.");
        }

        private static bool IsTraumaAction(CombatActionType type)
        {
            return type == CombatActionType.HeavyStrike
                || type == CombatActionType.DesperateStrike
                || type == CombatActionType.BreakArmor;
        }
    }
}
