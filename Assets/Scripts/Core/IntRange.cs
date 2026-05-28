using UnityEngine;

namespace Labyrinth.Core
{
    public readonly struct IntRange
    {
        public IntRange(int min, int max)
        {
            Min = Mathf.Min(min, max);
            Max = Mathf.Max(min, max);
        }

        public int Min { get; }

        public int Max { get; }

        public int Roll(System.Random random)
        {
            if (random == null || Min == Max)
            {
                return Min;
            }

            return random.Next(Min, Max + 1);
        }
    }
}
