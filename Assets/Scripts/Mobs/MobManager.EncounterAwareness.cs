using System.Collections.Generic;
using Labyrinth.Core;
using Labyrinth.Hero;
using UnityEngine;

namespace Labyrinth.Mobs
{
    public sealed partial class MobManager
    {
        private IReadOnlyList<HeroController> encounterHeroes;

        public void SetEncounterHeroes(IReadOnlyList<HeroController> activeHeroes)
        {
            encounterHeroes = activeHeroes;
        }

        private void AddManagedMob(MobController mob)
        {
            if (mob == null)
            {
                return;
            }

            mob.ShouldHoldWanderAtPosition = HasLivingHeroAdjacentTo;
            mobs.Add(mob);
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

            if (HasBossAlive
                && bossMob.Model.State == MobState.Wandering
                && result != null && result.BossCave.IsValid
                && ContainsCaveCell(result.BossCave, hero.Model.Position))
            {
                encounteredMob = bossMob;
                GameDebugLog.Info(
                    "Mobs",
                    $"Encounter forced by boss cave: hero=#{hero.DisplayNumber} pos={GameDebugLog.Position(hero.Model.Position)} vs {encounteredMob.DebugName} pos={GameDebugLog.Position(encounteredMob.Position)}, cave={GameDebugLog.Position(result.BossCave.Center)}.");
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

        private bool HasLivingHeroAdjacentTo(Vector2Int position)
        {
            if (encounterHeroes == null)
            {
                return false;
            }

            foreach (var hero in encounterHeroes)
            {
                if (hero == null
                    || hero.Model == null
                    || !hero.Model.IsAlive)
                {
                    continue;
                }

                if (GridDistance(hero.Model.Position, position) <= 1)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
