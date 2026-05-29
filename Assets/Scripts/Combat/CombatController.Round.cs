using Labyrinth.Core;
using Labyrinth.Hero;
using Labyrinth.Mobs;
using UnityEngine;

namespace Labyrinth.Combat
{
    public sealed partial class CombatController
    {
        private const float CombatSupportFeedbackDelay = 0.24f;
        private const float CombatSecondaryFeedbackDelay = 0.28f;
        private const float CombatStateFeedbackDelay = 0.58f;

        private CombatActionDefinition pendingHeroAction;
        private CombatActionDefinition pendingMobAction;
        private bool pendingHeroFirst;
        private bool pendingSecondAction;

        private void ExecuteRound()
        {
            if (hero == null || mob == null || heroCombat == null || mobCombat == null)
            {
                CancelCombat();
                return;
            }

            if (pendingSecondAction)
            {
                ResolvePendingSecondAction();
                return;
            }

            roundNumber++;
            pendingHeroAction = SelectHeroAction();
            pendingMobAction = SelectMobAction();
            var heroInitiative = roundNumber == 1 ? heroCombat.Initiative : RollRoundInitiative(heroCombat, true);
            var mobInitiative = roundNumber == 1 ? mobCombat.Initiative : RollRoundInitiative(mobCombat, false);
            pendingHeroFirst = heroInitiative >= mobInitiative;
            heroTurn = pendingHeroFirst;
            GameDebugLog.Info(
                "Combat",
                $"Round {roundNumber} plan: initiative=Hero({heroInitiative}) vs Mob({mobInitiative}), first={(heroTurn ? "Hero" : "Mob")}, heroAction={pendingHeroAction.Type}, heroCST={heroCombat.Stamina}/{heroCombat.MaxStamina}, heroGuard={heroCombat.Guard}, heroArmorBreak={heroCombat.ArmorBreak}, heroWounds={heroCombat.Wounds}, mobAction={pendingMobAction.Type}, mobCST={mobCombat.Stamina}/{mobCombat.MaxStamina}, mobGuard={mobCombat.Guard}, mobArmorBreak={mobCombat.ArmorBreak}, mobWounds={mobCombat.Wounds}, heroHP={hero.Model.HitPoints}/{hero.Model.MaxHitPoints}, mobHP={mob.Model.HitPoints}/{mob.Model.MaxHitPoints}.");

            ResolveAction(pendingHeroFirst ? pendingHeroAction : pendingMobAction, pendingHeroFirst);
            if (pendingHeroFirst)
            {
                if (!mob.Model.IsAlive)
                {
                    ResetPendingRound();
                    BeginFinish();
                    return;
                }
            }
            else
            {
                if (!hero.Model.IsAlive)
                {
                    ResetPendingRound();
                    BeginFinish();
                    return;
                }
            }

            pendingSecondAction = true;
            timer = ActionDelay;
        }

        private void ResolvePendingSecondAction()
        {
            if (hero == null || mob == null || heroCombat == null || mobCombat == null)
            {
                CancelCombat();
                return;
            }

            ResolveAction(pendingHeroFirst ? pendingMobAction : pendingHeroAction, !pendingHeroFirst);
            pendingSecondAction = false;
            if (pendingHeroFirst)
            {
                if (!hero.Model.IsAlive)
                {
                    ResetPendingRound();
                    BeginFinish();
                    return;
                }
            }
            else if (!mob.Model.IsAlive)
            {
                ResetPendingRound();
                BeginFinish();
                return;
            }

            EndRound();
            ResetPendingRound();
            timer = TurnDelay;
        }

        private void ResetPendingRound()
        {
            pendingHeroAction = default;
            pendingMobAction = default;
            pendingHeroFirst = false;
            pendingSecondAction = false;
        }

        private CombatActorState BuildHeroCombatState(HeroController controller)
        {
            var maxStamina = Mathf.Clamp(
                5 + controller.Model.MaxStamina / 3 + controller.Model.MoveSpeedBonusPercent / 20 - controller.Model.CombatStaminaWoundPenalty,
                4,
                14);
            return new CombatActorState($"Hero #{controller.DisplayNumber}", true, maxStamina, controller.Model.CombatWounds);
        }

        private CombatActorState BuildMobCombatState(MobController controller)
        {
            var model = controller.Model;
            var maxStamina = GetMobSpeciesCombatStamina(model.Species)
                + (model.IsBoss ? 4 : model.IsMiniBoss ? 2 : 0)
                + Mathf.Max(0, model.DungeonLevel - 1) * 2
                + (model.SpawnedFromDarkness ? 1 : 0);
            return new CombatActorState(controller.DebugName, false, Mathf.Clamp(maxStamina, 4, 18), 0);
        }

        private bool RollInitialInitiative(out int heroInitiative, out int mobInitiative)
        {
            heroInitiative = hero.Model.Level
                + heroCombat.Stamina
                + hero.Model.MoveSpeedBonusPercent / 10
                + rewardRandom.Next(1, 7);
            mobInitiative = GetMobSpeciesInitiative(mob.Model.Species)
                + (mob.Model.IsBoss ? 4 : mob.Model.IsMiniBoss ? 2 : 0)
                + (mob.Model.SpawnedFromDarkness ? 3 : 0)
                + rewardRandom.Next(1, 7);
            return heroInitiative >= mobInitiative;
        }

        private int RollRoundInitiative(CombatActorState state, bool isHeroActor)
        {
            var baseInitiative = isHeroActor
                ? hero.Model.Level + hero.Model.MoveSpeedBonusPercent / 12
                : GetMobSpeciesInitiative(mob.Model.Species) + GetMobPhaseAttackBonus();
            var woundPenalty = Mathf.Min(4, state.Wounds);
            return baseInitiative + state.Stamina - woundPenalty + rewardRandom.Next(1, 5);
        }

        private CombatActionDefinition SelectHeroAction()
        {
            var hpRatio = hero.Model.MaxHitPoints > 0 ? hero.Model.HitPoints / (float)hero.Model.MaxHitPoints : 1f;
            if (heroCombat.Stamina <= 1)
            {
                return GetAction(CombatActionType.Recover);
            }

            if (hpRatio <= 0.34f && CanUse(heroCombat, CombatActionType.DesperateStrike))
            {
                return GetAction(CombatActionType.DesperateStrike);
            }

            if (mobCombat.Guard >= 3 && CanUse(heroCombat, CombatActionType.BreakArmor))
            {
                return GetAction(CombatActionType.BreakArmor);
            }

            if (mob.Model.ArmorPoints - mobCombat.ArmorBreak >= 3
                && mobCombat.ArmorBreak < 3
                && CanUse(heroCombat, CombatActionType.BreakArmor)
                && roundNumber > 0)
            {
                return GetAction(CombatActionType.BreakArmor);
            }

            if (hpRatio <= 0.58f && heroCombat.Guard <= 1 && CanUse(heroCombat, CombatActionType.GuardedStrike))
            {
                return GetAction(CombatActionType.GuardedStrike);
            }

            if (mobCombat.Guard > 0 && CanUse(heroCombat, CombatActionType.Feint))
            {
                return GetAction(CombatActionType.Feint);
            }

            if (mob.Model.HitPoints > hero.Model.AttackPoints && CanUse(heroCombat, CombatActionType.HeavyStrike))
            {
                return GetAction(CombatActionType.HeavyStrike);
            }

            return CanUse(heroCombat, CombatActionType.LightStrike)
                ? GetAction(CombatActionType.LightStrike)
                : GetAction(CombatActionType.Recover);
        }

        private CombatActionDefinition SelectMobAction()
        {
            var hpRatio = mob.Model.MaxHitPoints > 0 ? mob.Model.HitPoints / (float)mob.Model.MaxHitPoints : 1f;
            var openingRookieCombat = mob.Model.Rank == MobRank.Regular
                && mob.Model.DungeonLevel <= 1
                && hero.Model.DungeonLevel <= 1
                && hero.Model.Level <= OpeningRookieSafetyMaxHeroLevel
                && hero.Model.StepsTaken <= OpeningRookieSafetyMaxHeroSteps;
            if (mobCombat.Stamina <= 1)
            {
                return GetAction(CombatActionType.Recover);
            }

            if (!openingRookieCombat && hpRatio <= 0.26f && CanUse(mobCombat, CombatActionType.DesperateStrike))
            {
                return GetAction(CombatActionType.DesperateStrike);
            }

            if (mob.Model.IsBoss
                && mobCombat.Phase >= 1
                && heroCombat.Guard >= 2
                && CanUse(mobCombat, CombatActionType.BreakArmor))
            {
                return GetAction(CombatActionType.BreakArmor);
            }

            switch (mob.Model.Species)
            {
                case MobSpecies.Rat:
                    if (heroCombat.Guard > 1 && CanUse(mobCombat, CombatActionType.Feint))
                    {
                        return GetAction(CombatActionType.Feint);
                    }

                    if (hpRatio <= 0.45f && CanUse(mobCombat, CombatActionType.GuardedStrike))
                    {
                        return GetAction(CombatActionType.GuardedStrike);
                    }

                    return CanUse(mobCombat, CombatActionType.LightStrike)
                        ? GetAction(CombatActionType.LightStrike)
                        : GetAction(CombatActionType.Recover);
                case MobSpecies.Goblin:
                    if (heroCombat.Guard >= 2 && CanUse(mobCombat, CombatActionType.Feint))
                    {
                        return GetAction(CombatActionType.Feint);
                    }

                    if (CanUse(mobCombat, CombatActionType.BreakArmor)
                        && (hero.Model.ArmorPoints - heroCombat.ArmorBreak >= 2
                            || (!openingRookieCombat && rewardRandom.Next(100) < 35)))
                    {
                        return GetAction(CombatActionType.BreakArmor);
                    }

                    if (hpRatio <= 0.52f && CanUse(mobCombat, CombatActionType.GuardedStrike))
                    {
                        return GetAction(CombatActionType.GuardedStrike);
                    }

                    return CanUse(mobCombat, CombatActionType.HeavyStrike) && rewardRandom.Next(100) < 45
                        ? GetAction(CombatActionType.HeavyStrike)
                        : GetAction(CombatActionType.LightStrike);
                case MobSpecies.Orc:
                default:
                    if (CanUse(mobCombat, CombatActionType.BreakArmor)
                        && (heroCombat.Guard >= 2 || hero.Model.ArmorPoints - heroCombat.ArmorBreak >= 2))
                    {
                        return GetAction(CombatActionType.BreakArmor);
                    }

                    if (CanUse(mobCombat, CombatActionType.HeavyStrike))
                    {
                        return GetAction(CombatActionType.HeavyStrike);
                    }

                    if (hpRatio <= 0.5f && CanUse(mobCombat, CombatActionType.GuardedStrike))
                    {
                        return GetAction(CombatActionType.GuardedStrike);
                    }

                    return CanUse(mobCombat, CombatActionType.LightStrike)
                        ? GetAction(CombatActionType.LightStrike)
                        : GetAction(CombatActionType.Recover);
            }
        }

        private void ResolveAction(CombatActionDefinition selectedAction, bool actorIsHero)
        {
            var actorState = actorIsHero ? heroCombat : mobCombat;
            var targetState = actorIsHero ? mobCombat : heroCombat;
            var action = selectedAction;
            var forcedRecover = false;
            if (!actorState.HasStamina(action.StaminaCost))
            {
                action = GetAction(CombatActionType.Recover);
                forcedRecover = true;
            }

            var actorPosition = actorIsHero ? hero.Model.Position : mob.Position;
            var targetPosition = actorIsHero ? mob.Position : hero.Model.Position;
            actorState.SpendStamina(action.StaminaCost);
            var recoveredStamina = actorState.RecoverStamina(action.StaminaRestore);
            var guardAdded = actorState.AddGuard(action.GuardGain);
            ShowActionLabel(actorIsHero, action.DisplayName, forcedRecover);

            if (!action.DealsDamage)
            {
                ShowSupportFeedback(actorPosition, recoveredStamina, guardAdded, forcedRecover);
                GameDebugLog.Info(
                    "Combat",
                    $"Round {roundNumber} action: actor={(actorIsHero ? "Hero" : "Mob")}, action={action.Type}, forcedRecover={forcedRecover}, recoveredCST={recoveredStamina}, guardAdded={guardAdded}, actorCST={actorState.Stamina}/{actorState.MaxStamina}, actorGuard={actorState.Guard}.");
                return;
            }

            var baseAttack = BuildAttack(
                actorIsHero,
                action,
                out var firstHitBonus,
                out var vengeanceBonus,
                out var vengeanceReduction,
                out var personalBonus,
                out var personalReduction,
                out var phaseBonus,
                out var desperateBonus);
            var scaledAttack = Mathf.Max(1, Mathf.RoundToInt((baseAttack + action.FlatAttackBonus) * action.DamageMultiplier));
            var targetBaseArmor = actorIsHero ? mob.Model.ArmorPoints : hero.Model.ArmorPoints;
            var armorBeforePierce = targetState.EffectiveArmor(targetBaseArmor);
            var effectiveArmor = Mathf.Max(0, armorBeforePierce - action.ArmorPierce);
            var guardBroken = targetState.ConsumeGuard(action.GuardDamage);
            var guardAbsorb = targetState.ConsumeGuard(Mathf.CeilToInt(scaledAttack * 0.45f));
            var attackAfterGuard = Mathf.Max(0, scaledAttack - guardAbsorb);
            var resolvedDamage = attackAfterGuard <= 0 ? 0 : Mathf.Max(1, attackAfterGuard - effectiveArmor);
            var hpBefore = actorIsHero ? mob.Model.HitPoints : hero.Model.HitPoints;
            var actualDamage = ApplyResolvedDamage(actorIsHero, resolvedDamage, targetPosition);
            var hpAfter = actorIsHero ? mob.Model.HitPoints : hero.Model.HitPoints;
            var armorBreakApplied = action.ArmorBreak > 0 && (actualDamage > 0 || guardBroken > 0)
                ? targetState.ApplyArmorBreak(action.ArmorBreak)
                : 0;
            var staminaDamaged = action.StaminaDamage > 0 && actualDamage > 0
                ? targetState.DrainStamina(action.StaminaDamage)
                : 0;
            var woundApplied = TryApplyCombatWound(actorIsHero, actualDamage, action, targetState);

            actorState.AddDamageDealt(actualDamage);
            targetState.AddDamageTaken(actualDamage);
            ShowAttackFeedback(targetPosition, actualDamage, guardAbsorb, guardBroken, armorBreakApplied, staminaDamaged, woundApplied, actorIsHero);
            ShowSupportFeedback(actorPosition, recoveredStamina, guardAdded, forcedRecover);
            GameDebugLog.Info(
                "Combat",
                $"Round {roundNumber} action: actor={(actorIsHero ? "Hero" : "Mob")}, action={action.Type}, forcedRecover={forcedRecover}, baseAttack={baseAttack}, scaledAttack={scaledAttack}, firstHitBonus={firstHitBonus}, vengeanceBonus={vengeanceBonus}, vengeanceReduction={vengeanceReduction}, personalBonus={personalBonus}, personalReduction={personalReduction}, phaseBonus={phaseBonus}, desperateBonus={desperateBonus}, targetArmorBase={targetBaseArmor}, targetArmorBreak={targetState.ArmorBreak}, armorBeforePierce={armorBeforePierce}, pierce={action.ArmorPierce}, effectiveArmor={effectiveArmor}, guardBroken={guardBroken}, guardAbsorb={guardAbsorb}, resolvedDamage={resolvedDamage}, damage={actualDamage}, targetHP={hpBefore}->{hpAfter}, armorBreakApplied={armorBreakApplied}, staminaDamaged={staminaDamaged}, woundApplied={woundApplied}, actorCST={actorState.Stamina}/{actorState.MaxStamina}, targetCST={targetState.Stamina}/{targetState.MaxStamina}, targetGuard={targetState.Guard}, targetWounds={targetState.Wounds}.");

            if (actorIsHero)
            {
                CheckElitePhase();
            }
        }

        private int BuildAttack(
            bool actorIsHero,
            CombatActionDefinition action,
            out int firstHitBonus,
            out int vengeanceBonus,
            out int vengeanceReduction,
            out int personalBonus,
            out int personalReduction,
            out int phaseBonus,
            out int desperateBonus)
        {
            firstHitBonus = 0;
            vengeanceBonus = 0;
            vengeanceReduction = 0;
            personalBonus = 0;
            personalReduction = 0;
            phaseBonus = 0;
            desperateBonus = 0;

            var attack = actorIsHero
                ? hero.Model.AttackPoints
                : Mathf.Max(1, mob.Model.AttackPoints - mobCombat.Wounds);
            var isOpeningAttack = actorIsHero ? !heroOpeningAttackUsed : !mobOpeningAttackUsed;
            if (actorIsHero)
            {
                if (!heroOpeningAttackUsed)
                {
                    firstHitBonus = hero.Model.FirstHitBlessingBonus;
                    attack += firstHitBonus;
                    heroOpeningAttackUsed = true;
                }

                vengeanceBonus = hero.Model.GetVengeanceAttackBonus(mob.Model, isOpeningAttack, attack);
                attack += vengeanceBonus;
                personalBonus = hero.Model.GetPersonalAttackBonus(mob.Model, isOpeningAttack);
                attack += personalBonus;
            }
            else
            {
                hero.Model.RememberCombatThreat(mob.Model);
                phaseBonus = GetMobPhaseAttackBonus();
                attack += phaseBonus;
                attack = hero.Model.ApplyVengeanceIncomingAttackModifier(
                    mob.Model,
                    attack,
                    isOpeningAttack,
                    out vengeanceReduction);
                attack = hero.Model.ApplyPersonalIncomingAttackModifier(
                    mob.Model,
                    attack,
                    isOpeningAttack,
                    out personalReduction);
                mobOpeningAttackUsed = true;
            }

            if (action.Type == CombatActionType.DesperateStrike)
            {
                desperateBonus = Mathf.Max(1, GetMissingHitPoints(actorIsHero) / 3);
                attack += desperateBonus;
            }

            return Mathf.Max(1, attack);
        }

        private int ApplyResolvedDamage(bool actorIsHero, int resolvedDamage, Vector2Int targetPosition)
        {
            if (actorIsHero)
            {
                hero.PlayAttack(targetPosition);
                if (resolvedDamage <= 0)
                {
                    return 0;
                }

                var damage = mob.ReceiveResolvedDamage(resolvedDamage);
                GameAudioController.Play(GameSfx.CombatHit, mazeRenderer.GridToWorld(targetPosition));
                return damage;
            }

            mob.PlayAttack(targetPosition);
            if (resolvedDamage <= 0)
            {
                return 0;
            }

            var received = hero.ReceiveResolvedDamage(resolvedDamage);
            GameAudioController.Play(GameSfx.CombatHit, mazeRenderer.GridToWorld(targetPosition));
            return received;
        }

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

            var threshold = targetIsHero
                ? HeroWoundDamageThreshold
                : mob.Model.IsBoss || mob.Model.IsMiniBoss
                    ? EliteWoundDamageThreshold
                    : MobWoundDamageThreshold;
            if (action.Type == CombatActionType.HeavyStrike || action.Type == CombatActionType.DesperateStrike)
            {
                threshold--;
            }

            if (damage < threshold)
            {
                return false;
            }

            var chance = Mathf.Clamp((targetIsHero ? 25 : 16) + damage * (targetIsHero ? 4 : 2), 0, targetIsHero ? 78 : 55);
            if (rewardRandom.Next(100) >= chance)
            {
                return false;
            }

            if (targetIsHero)
            {
                targetState.SetWounds(hero.Model.ApplyCombatWound());
                if (hero.Model.TryApplySevereInjuryFromCombat(mob.Model, damage, rewardRandom, out var severeGained, out var scarGained))
                {
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
            }
            else
            {
                targetState.ApplyWound();
            }

            GameDebugLog.Info(
                "Combat",
                $"Round {roundNumber} wound: target={(targetIsHero ? "Hero" : "Mob")}, damage={damage}, threshold={threshold}, chance={chance}, wounds={targetState.Wounds}.");
            return true;
        }

        private void CheckElitePhase()
        {
            if (mob == null || mobCombat == null || !mob.Model.IsAlive || mob.Model.Rank == MobRank.Regular)
            {
                return;
            }

            var hpRatio = mob.Model.MaxHitPoints > 0 ? mob.Model.HitPoints / (float)mob.Model.MaxHitPoints : 1f;
            if (mobCombat.Phase < 1 && hpRatio <= 0.5f)
            {
                mobCombat.SetPhase(1);
                var restored = mobCombat.RecoverStamina(3);
                var guard = mobCombat.AddGuard(3);
                ShowElitePhase("Фаза ярости", restored, guard);
                GameDebugLog.Info(
                    "Combat",
                    $"Elite phase triggered: {mob.DebugName}, phase=1, hp={mob.Model.HitPoints}/{mob.Model.MaxHitPoints}, restoredCST={restored}, guardAdded={guard}, attackBonus={GetMobPhaseAttackBonus()}.");
            }

            if (mob.Model.IsBoss && mobCombat.Phase < 2 && hpRatio <= 0.25f)
            {
                mobCombat.SetPhase(2);
                var restored = mobCombat.RecoverStamina(4);
                var guard = mobCombat.AddGuard(4);
                var repairedArmorBreak = mobCombat.RepairArmorBreak(1);
                ShowElitePhase("Последняя фаза", restored, guard);
                GameDebugLog.Info(
                    "Combat",
                    $"Elite phase triggered: {mob.DebugName}, phase=2, hp={mob.Model.HitPoints}/{mob.Model.MaxHitPoints}, restoredCST={restored}, guardAdded={guard}, repairedArmorBreak={repairedArmorBreak}, attackBonus={GetMobPhaseAttackBonus()}.");
            }
        }

        private void EndRound()
        {
            heroCombat.DecayGuard(1);
            mobCombat.DecayGuard(1);
            ShowCombatState();
            GameDebugLog.Info(
                "Combat",
                $"Round {roundNumber} end: heroHP={hero.Model.HitPoints}/{hero.Model.MaxHitPoints}, heroCST={heroCombat.Stamina}/{heroCombat.MaxStamina}, heroGuard={heroCombat.Guard}, heroArmorBreak={heroCombat.ArmorBreak}, heroWounds={heroCombat.Wounds}, mobHP={mob.Model.HitPoints}/{mob.Model.MaxHitPoints}, mobCST={mobCombat.Stamina}/{mobCombat.MaxStamina}, mobGuard={mobCombat.Guard}, mobArmorBreak={mobCombat.ArmorBreak}, mobWounds={mobCombat.Wounds}, mobPhase={mobCombat.Phase}.");
        }

        private void ShowActionLabel(bool actorIsHero, string text, bool forcedRecover)
        {
            DamageNumberView.CreateCombatText(
                mazeRenderer,
                actorIsHero ? hero.Model.Position : mob.Position,
                forcedRecover ? "срывает темп" : text,
                actorIsHero ? new Color(0.58f, 0.86f, 1f) : new Color(1f, 0.48f, 0.32f),
                actorIsHero ? 2.05f : 1.9f);
        }

        private void ShowSupportFeedback(Vector2Int position, int recoveredStamina, int guardAdded, bool forcedRecover)
        {
            if (recoveredStamina <= 0 && guardAdded <= 0 && !forcedRecover)
            {
                return;
            }

            var text = recoveredStamina > 0 && guardAdded > 0
                ? $"+{recoveredStamina} БВ, +{guardAdded} щит"
                : recoveredStamina > 0
                    ? $"+{recoveredStamina} БВ"
                    : $"+{guardAdded} щит";
            DamageNumberView.CreateCombatText(
                mazeRenderer,
                position,
                text,
                new Color(0.72f, 0.95f, 1f),
                1.75f,
                CombatSupportFeedbackDelay);
        }

        private void ShowAttackFeedback(
            Vector2Int targetPosition,
            int damage,
            int guardAbsorb,
            int guardBroken,
            int armorBreakApplied,
            int staminaDamaged,
            bool woundApplied,
            bool actorIsHero)
        {
            if (damage > 0)
            {
                DamageNumberView.CreateCombatText(
                    mazeRenderer,
                    targetPosition,
                    $"-{damage}",
                    actorIsHero ? new Color(1f, 0.72f, 0.24f) : new Color(1f, 0.3f, 0.24f),
                    1.34f);
            }
            else
            {
                DamageNumberView.CreateCombatText(mazeRenderer, targetPosition, "блок", new Color(0.78f, 0.82f, 0.86f), 1.34f);
            }

            var effectText = BuildAttackEffectText(
                guardAbsorb,
                guardBroken,
                armorBreakApplied,
                staminaDamaged,
                woundApplied);
            if (!string.IsNullOrEmpty(effectText))
            {
                DamageNumberView.CreateCombatText(
                    mazeRenderer,
                    targetPosition,
                    effectText,
                    new Color(0.68f, 0.88f, 1f),
                    1.62f,
                    CombatSecondaryFeedbackDelay);
            }
        }

        private static string BuildAttackEffectText(
            int guardAbsorb,
            int guardBroken,
            int armorBreakApplied,
            int staminaDamaged,
            bool woundApplied)
        {
            var text = string.Empty;
            if (guardAbsorb > 0 || guardBroken > 0)
            {
                AppendEffect(ref text, guardBroken > 0 ? $"-{guardBroken} щит, блок {guardAbsorb}" : $"блок {guardAbsorb}");
            }

            if (armorBreakApplied > 0)
            {
                AppendEffect(ref text, "броня треснула");
            }

            if (staminaDamaged > 0)
            {
                AppendEffect(ref text, $"-{staminaDamaged} БВ");
            }

            if (woundApplied)
            {
                AppendEffect(ref text, "рана");
            }

            return text;
        }

        private static void AppendEffect(ref string text, string effect)
        {
            if (string.IsNullOrEmpty(effect))
            {
                return;
            }

            text = string.IsNullOrEmpty(text) ? effect : $"{text}, {effect}";
        }

        private void ShowElitePhase(string label, int restoredStamina, int guardAdded)
        {
            DamageNumberView.CreateCombatText(mazeRenderer, mob.Position, label, new Color(1f, 0.38f, 0.18f), 2.15f);
            DamageNumberView.CreateCombatText(
                mazeRenderer,
                mob.Position,
                $"+{restoredStamina} БВ, +{guardAdded} щит",
                new Color(1f, 0.74f, 0.32f),
                1.88f,
                CombatSupportFeedbackDelay);
        }

        private void ShowCombatState()
        {
            if (hero == null || mob == null || heroCombat == null || mobCombat == null || mazeRenderer == null)
            {
                return;
            }

            DamageNumberView.CreateCombatText(
                mazeRenderer,
                hero.Model.Position,
                BuildStateText(hero.Model.HitPoints, hero.Model.MaxHitPoints, heroCombat),
                new Color(0.76f, 0.92f, 1f),
                2.38f,
                CombatStateFeedbackDelay);
            DamageNumberView.CreateCombatText(
                mazeRenderer,
                mob.Position,
                BuildStateText(mob.Model.HitPoints, mob.Model.MaxHitPoints, mobCombat),
                new Color(1f, 0.74f, 0.56f),
                2.22f,
                CombatStateFeedbackDelay);
        }

        private static string BuildStateText(int hitPoints, int maxHitPoints, CombatActorState state)
        {
            var text = $"HP {hitPoints}/{maxHitPoints} БВ {state.Stamina}/{state.MaxStamina} Щ{state.Guard}";
            if (state.ArmorBreak > 0)
            {
                text += $" Бр-{state.ArmorBreak}";
            }

            if (state.Wounds > 0)
            {
                text += $" Р{state.Wounds}";
            }

            return text;
        }

        private int GetMissingHitPoints(bool actorIsHero)
        {
            return actorIsHero
                ? Mathf.Max(0, hero.Model.MaxHitPoints - hero.Model.HitPoints)
                : Mathf.Max(0, mob.Model.MaxHitPoints - mob.Model.HitPoints);
        }

        private int GetMobPhaseAttackBonus()
        {
            if (mob == null || mobCombat == null || mobCombat.Phase <= 0)
            {
                return 0;
            }

            return mob.Model.IsBoss ? mobCombat.Phase * 3 : 2;
        }

        private static bool CanUse(CombatActorState state, CombatActionType type)
        {
            return state != null && state.HasStamina(GetAction(type).StaminaCost);
        }

        private static int GetMobSpeciesCombatStamina(MobSpecies species)
        {
            switch (species)
            {
                case MobSpecies.Rat:
                    return 5;
                case MobSpecies.Goblin:
                    return 7;
                case MobSpecies.Orc:
                default:
                    return 8;
            }
        }

        private static int GetMobSpeciesInitiative(MobSpecies species)
        {
            switch (species)
            {
                case MobSpecies.Rat:
                    return 8;
                case MobSpecies.Goblin:
                    return 6;
                case MobSpecies.Orc:
                default:
                    return 4;
            }
        }

        private static CombatActionDefinition GetAction(CombatActionType type)
        {
            switch (type)
            {
                case CombatActionType.HeavyStrike:
                    return new CombatActionDefinition(type, "тяжёлый удар", 1.45f, 1, 4, 0, 0, 1, 1, 0, 0);
                case CombatActionType.GuardedStrike:
                    return new CombatActionDefinition(type, "удар из-за щита", 0.72f, 0, 2, 0, 4, 0, 0, 0, 0);
                case CombatActionType.Recover:
                    return new CombatActionDefinition(type, "передышка", 0f, 0, 0, 5, 2, 0, 0, 0, 0);
                case CombatActionType.Feint:
                    return new CombatActionDefinition(type, "финт", 0.65f, 0, 2, 0, 0, 0, 4, 0, 1);
                case CombatActionType.BreakArmor:
                    return new CombatActionDefinition(type, "пролом брони", 0.9f, 0, 4, 0, 0, 2, 3, 1, 0);
                case CombatActionType.DesperateStrike:
                    return new CombatActionDefinition(type, "отчаянный выпад", 1.7f, 2, 3, 0, 0, 1, 2, 0, 0);
                case CombatActionType.LightStrike:
                default:
                    return new CombatActionDefinition(CombatActionType.LightStrike, "быстрый удар", 1f, 0, 1, 0, 0, 0, 1, 0, 0);
            }
        }

        private enum CombatActionType
        {
            LightStrike,
            HeavyStrike,
            GuardedStrike,
            Recover,
            Feint,
            BreakArmor,
            DesperateStrike
        }

        private sealed class CombatActorState
        {
            public CombatActorState(string name, bool isHero, int maxStamina, int wounds)
            {
                Name = name;
                IsHero = isHero;
                MaxStamina = Mathf.Max(1, maxStamina);
                Stamina = MaxStamina;
                Wounds = Mathf.Max(0, wounds);
            }

            public string Name { get; }

            public bool IsHero { get; }

            public int MaxStamina { get; }

            public int Stamina { get; private set; }

            public int Guard { get; private set; }

            public int ArmorBreak { get; private set; }

            public int Wounds { get; private set; }

            public int Phase { get; private set; }

            public int Initiative { get; private set; }

            public int TotalDamageDealt { get; private set; }

            public int TotalDamageTaken { get; private set; }

            public bool HasStamina(int cost)
            {
                return Stamina >= Mathf.Max(0, cost);
            }

            public void SetInitiative(int initiative)
            {
                Initiative = initiative;
            }

            public void SetPhase(int phase)
            {
                Phase = Mathf.Max(Phase, phase);
            }

            public void SpendStamina(int amount)
            {
                Stamina = Mathf.Max(0, Stamina - Mathf.Max(0, amount));
            }

            public int RecoverStamina(int amount)
            {
                var before = Stamina;
                Stamina = Mathf.Min(MaxStamina, Stamina + Mathf.Max(0, amount));
                return Stamina - before;
            }

            public int DrainStamina(int amount)
            {
                var before = Stamina;
                Stamina = Mathf.Max(0, Stamina - Mathf.Max(0, amount));
                return before - Stamina;
            }

            public int AddGuard(int amount)
            {
                var before = Guard;
                Guard = Mathf.Min(MaxCombatGuard, Guard + Mathf.Max(0, amount));
                return Guard - before;
            }

            public int ConsumeGuard(int amount)
            {
                var consumed = Mathf.Min(Guard, Mathf.Max(0, amount));
                Guard -= consumed;
                return consumed;
            }

            public void DecayGuard(int amount)
            {
                Guard = Mathf.Max(0, Guard - Mathf.Max(0, amount));
            }

            public int ApplyArmorBreak(int amount)
            {
                var before = ArmorBreak;
                ArmorBreak = Mathf.Min(6, ArmorBreak + Mathf.Max(0, amount));
                return ArmorBreak - before;
            }

            public int RepairArmorBreak(int amount)
            {
                var before = ArmorBreak;
                ArmorBreak = Mathf.Max(0, ArmorBreak - Mathf.Max(0, amount));
                return before - ArmorBreak;
            }

            public int EffectiveArmor(int baseArmor)
            {
                return Mathf.Max(0, baseArmor - ArmorBreak);
            }

            public void ApplyWound()
            {
                Wounds = Mathf.Min(9, Wounds + 1);
            }

            public void SetWounds(int wounds)
            {
                Wounds = Mathf.Clamp(wounds, 0, 9);
            }

            public void AddDamageDealt(int damage)
            {
                TotalDamageDealt += Mathf.Max(0, damage);
            }

            public void AddDamageTaken(int damage)
            {
                TotalDamageTaken += Mathf.Max(0, damage);
            }
        }

        private readonly struct CombatActionDefinition
        {
            public CombatActionDefinition(
                CombatActionType type,
                string displayName,
                float damageMultiplier,
                int flatAttackBonus,
                int staminaCost,
                int staminaRestore,
                int guardGain,
                int armorPierce,
                int guardDamage,
                int armorBreak,
                int staminaDamage)
            {
                Type = type;
                DisplayName = displayName;
                DamageMultiplier = damageMultiplier;
                FlatAttackBonus = flatAttackBonus;
                StaminaCost = staminaCost;
                StaminaRestore = staminaRestore;
                GuardGain = guardGain;
                ArmorPierce = armorPierce;
                GuardDamage = guardDamage;
                ArmorBreak = armorBreak;
                StaminaDamage = staminaDamage;
            }

            public CombatActionType Type { get; }

            public string DisplayName { get; }

            public float DamageMultiplier { get; }

            public int FlatAttackBonus { get; }

            public int StaminaCost { get; }

            public int StaminaRestore { get; }

            public int GuardGain { get; }

            public int ArmorPierce { get; }

            public int GuardDamage { get; }

            public int ArmorBreak { get; }

            public int StaminaDamage { get; }

            public bool DealsDamage => DamageMultiplier > 0f;
        }
    }
}
