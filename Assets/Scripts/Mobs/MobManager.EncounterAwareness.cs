using System.Collections.Generic;
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
