using System.Collections.Generic;

namespace Labyrinth.Hero
{
    public sealed class HeroBlessings
    {
        public const int MaxActiveBlessings = 1;

        private readonly HashSet<HeroBlessingType> active = new HashSet<HeroBlessingType>();

        public IEnumerable<HeroBlessingType> Active => active;

        public int ActiveCount => active.Count;

        public bool HasLeftEntrance { get; private set; }

        public bool Has(HeroBlessingType type)
        {
            return active.Contains(type);
        }

        public bool TryActivate(HeroBlessingType type)
        {
            if (active.Count >= MaxActiveBlessings || active.Contains(type))
            {
                return false;
            }

            active.Add(type);
            return true;
        }

        public bool TryConsume(HeroBlessingType type)
        {
            return active.Remove(type);
        }

        public void MarkLeftEntrance()
        {
            if (active.Count > 0)
            {
                HasLeftEntrance = true;
            }
        }

        public void ClearExpedition()
        {
            active.Clear();
            HasLeftEntrance = false;
        }
    }
}
