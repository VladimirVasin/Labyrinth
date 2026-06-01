using UnityEngine;

namespace Labyrinth.Mobs
{
    public sealed partial class MobManager
    {
        public bool HasInteractableMobNear(Vector2Int heroPosition, Vector2Int interactionCell, int radius)
        {
            var normalizedRadius = Mathf.Max(0, radius);
            foreach (var mob in mobs)
            {
                if (mob == null
                    || mob.Model == null
                    || !mob.Model.IsAlive
                    || mob.Model.State != MobState.Wandering)
                {
                    continue;
                }

                if (GridDistance(heroPosition, mob.Position) > normalizedRadius
                    || GridDistance(interactionCell, mob.Position) > 1)
                {
                    continue;
                }

                if (interactionCell == mob.Position && heroPosition != interactionCell)
                {
                    continue;
                }

                return true;
            }

            return false;
        }
    }
}
