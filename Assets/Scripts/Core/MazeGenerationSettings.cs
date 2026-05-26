using System;

namespace Labyrinth.Core
{
    public sealed class MazeGenerationSettings
    {
        public const int MinSize = 15;
        public const int MaxSize = 101;

        public MazeGenerationSettings(int width, int height, int seed, MazeSizePreset preset)
        {
            Width = width;
            Height = height;
            Seed = seed;
            Preset = preset;
        }

        public int Width { get; }

        public int Height { get; }

        public int Seed { get; }

        public MazeSizePreset Preset { get; }

        public string DisplayName
        {
            get
            {
                switch (Preset)
                {
                    case MazeSizePreset.Small:
                        return "Маленький";
                    case MazeSizePreset.Medium:
                        return "Средний";
                    case MazeSizePreset.Large:
                        return "Большой";
                    case MazeSizePreset.Custom:
                        return "Свой размер";
                    default:
                        return "Свой размер";
                }
            }
        }

        public static MazeGenerationSettings Create(MazeSizePreset preset)
        {
            return Create(preset, Environment.TickCount);
        }

        public static MazeGenerationSettings Create(MazeSizePreset preset, int seed)
        {
            switch (preset)
            {
                case MazeSizePreset.Small:
                    return new MazeGenerationSettings(15, 15, seed, preset);
                case MazeSizePreset.Medium:
                    return new MazeGenerationSettings(25, 25, seed, preset);
                case MazeSizePreset.Large:
                    return new MazeGenerationSettings(41, 41, seed, preset);
                case MazeSizePreset.Custom:
                    return CreateCustom(25, 25, seed);
                default:
                    return new MazeGenerationSettings(25, 25, seed, MazeSizePreset.Medium);
            }
        }

        public static MazeGenerationSettings CreateCustom(int width, int height, int seed)
        {
            return new MazeGenerationSettings(
                NormalizeSize(width),
                NormalizeSize(height),
                seed,
                MazeSizePreset.Custom);
        }

        public static int NormalizeSize(int size)
        {
            var normalized = Math.Max(MinSize, Math.Min(MaxSize, size));
            if (normalized % 2 == 0)
            {
                normalized = normalized == MaxSize ? normalized - 1 : normalized + 1;
            }

            return normalized;
        }
    }
}
