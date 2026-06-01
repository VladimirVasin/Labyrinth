using System.Collections.Generic;
using Labyrinth.Hero;
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

        public void CollectVisibleRegularSpecies(IReadOnlyList<HeroController> heroes, ISet<MobSpecies> species)
        {
            if (heroes == null || species == null)
            {
                return;
            }

            for (var i = 0; i < mobs.Count; i++)
            {
                var mob = mobs[i];
                if (mob == null
                    || mob.Model == null
                    || !mob.Model.IsAlive
                    || mob.Model.Rank != MobRank.Regular)
                {
                    continue;
                }

                for (var heroIndex = 0; heroIndex < heroes.Count; heroIndex++)
                {
                    var hero = heroes[heroIndex];
                    if (hero == null
                        || hero.Model == null
                        || !hero.Model.IsAlive
                        || hero.Model.Visibility == null
                        || !hero.Model.Visibility.IsVisible(mob.Position))
                    {
                        continue;
                    }

                    species.Add(mob.Model.Species);
                    break;
                }
            }
        }

        public bool TryGetCentralMiniBossTarget(HeroModel hero, out Vector2Int target, out string label)
        {
            target = default;
            label = string.Empty;
            if (hero == null
                || hero.Memory == null
                || !HasCentralMiniBossAlive
                || centralMiniBoss.Model.State != MobState.Wandering
                || result == null
                || result.Grid == null
                || !result.CentralRoom.IsValid
                || !IsCentralRoomOpen())
            {
                return false;
            }

            var bestScore = int.MaxValue;
            var room = result.CentralRoom;
            for (var x = room.Min.x; x <= room.Max.x; x++)
            {
                for (var y = room.Min.y; y <= room.Max.y; y++)
                {
                    var candidate = new Vector2Int(x, y);
                    if (!result.Grid.InBounds(candidate)
                        || !result.Grid.Get(candidate).IsWalkable
                        || !hero.Memory.IsRemembered(candidate))
                    {
                        continue;
                    }

                    var score = GridDistance(hero.Position, candidate) * 3
                        + GridDistance(candidate, centralMiniBoss.Position);
                    if (score >= bestScore)
                    {
                        continue;
                    }

                    bestScore = score;
                    target = candidate;
                }
            }

            if (bestScore == int.MaxValue)
            {
                return false;
            }

            label = centralMiniBoss.DebugName;
            return true;
        }

        private bool IsCentralRoomOpen()
        {
            if (result?.CentralDoors == null || result.CentralDoors.Count == 0)
            {
                return true;
            }

            for (var i = 0; i < result.CentralDoors.Count; i++)
            {
                var door = result.CentralDoors[i];
                if (door != null && door.Position == result.CentralRoom.EntrancePosition)
                {
                    return door.IsOpen;
                }
            }

            return result.Grid != null
                && result.Grid.InBounds(result.CentralRoom.EntrancePosition)
                && result.Grid.Get(result.CentralRoom.EntrancePosition).IsWalkable;
        }
    }
}
