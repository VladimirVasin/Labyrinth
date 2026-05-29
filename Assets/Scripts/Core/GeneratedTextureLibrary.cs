using System;
using System.Collections.Generic;
using UnityEngine;

namespace Labyrinth.Core
{
    public static class GeneratedTextureLibrary
    {
        private enum TextureProfile
        {
            None,
            Generic,
            Stone,
            Moss,
            Wood,
            Straw,
            RoofTile,
            Clay,
            Dirt,
            Grass,
            Metal,
            Gold,
            Cloth,
            Leather,
            Skin,
            Parchment,
            Glass,
            Ore,
            Coal
        }

        private const int SmallSize = 32;
        private const int LargeSize = 64;
        private static readonly Dictionary<string, Texture2D> Cache = new Dictionary<string, Texture2D>();

        public static bool TryGetTexture(string materialName, Color baseColor, out Texture2D texture, out Vector2 scale)
        {
            texture = null;
            scale = Vector2.one;

            var profile = ResolveProfile(materialName);
            if (profile == TextureProfile.None || baseColor.a < 0.98f)
            {
                return false;
            }

            var size = UsesLargeTexture(profile) ? LargeSize : SmallSize;
            var key = $"{profile}:{Quantize(baseColor.r)}:{Quantize(baseColor.g)}:{Quantize(baseColor.b)}:{size}";
            if (!Cache.TryGetValue(key, out texture))
            {
                texture = CreateTexture(profile, baseColor, size, StableHash(key));
                Cache[key] = texture;
            }

            scale = GetScale(profile);
            return texture != null;
        }

        private static TextureProfile ResolveProfile(string materialName)
        {
            if (string.IsNullOrEmpty(materialName) || ShouldSkip(materialName))
            {
                return TextureProfile.None;
            }

            if (Contains(materialName, "Gold")
                || Contains(materialName, "Coin")
                || Contains(materialName, "Brass")
                || Contains(materialName, "Key")
                || Contains(materialName, "Ingot"))
            {
                return TextureProfile.Gold;
            }

            if (Contains(materialName, "Coal"))
            {
                return TextureProfile.Coal;
            }

            if (Contains(materialName, "Ore") || Contains(materialName, "Vein"))
            {
                return TextureProfile.Ore;
            }

            if (Contains(materialName, "Metal")
                || Contains(materialName, "Iron")
                || Contains(materialName, "Armor")
                || Contains(materialName, "Sword")
                || Contains(materialName, "Shield")
                || Contains(materialName, "Blade")
                || Contains(materialName, "Anvil")
                || Contains(materialName, "Lock"))
            {
                return TextureProfile.Metal;
            }

            if (Contains(materialName, "Wood")
                || Contains(materialName, "Timber")
                || Contains(materialName, "Plank")
                || Contains(materialName, "Beam")
                || Contains(materialName, "Door")
                || Contains(materialName, "Fence")
                || Contains(materialName, "Chest")
                || Contains(materialName, "Crate")
                || Contains(materialName, "Barrel")
                || Contains(materialName, "Cart")
                || Contains(materialName, "Handle")
                || Contains(materialName, "Club")
                || Contains(materialName, "Post")
                || Contains(materialName, "Rack"))
            {
                return TextureProfile.Wood;
            }

            if (Contains(materialName, "Straw")
                || Contains(materialName, "Thatch")
                || Contains(materialName, "Bread")
                || Contains(materialName, "Ration")
                || Contains(materialName, "Rope"))
            {
                return TextureProfile.Straw;
            }

            if (Contains(materialName, "Roof"))
            {
                return TextureProfile.RoofTile;
            }

            if (Contains(materialName, "Leaves")
                || Contains(materialName, "Leaf")
                || Contains(materialName, "Bush")
                || Contains(materialName, "Crop")
                || Contains(materialName, "Herb")
                || Contains(materialName, "Grass"))
            {
                return TextureProfile.Grass;
            }

            if (Contains(materialName, "Moss"))
            {
                return TextureProfile.Moss;
            }

            if (Contains(materialName, "Stone")
                || Contains(materialName, "Rock")
                || Contains(materialName, "Maze Wall")
                || Contains(materialName, "Floor")
                || Contains(materialName, "Path")
                || Contains(materialName, "Stairs")
                || Contains(materialName, "Castle"))
            {
                return TextureProfile.Stone;
            }

            if (Contains(materialName, "Ground") || Contains(materialName, "Dirt") || Contains(materialName, "Road"))
            {
                return TextureProfile.Dirt;
            }

            if (Contains(materialName, "Clay") || Contains(materialName, "Wall"))
            {
                return TextureProfile.Clay;
            }

            if (Contains(materialName, "Cloth")
                || Contains(materialName, "Cape")
                || Contains(materialName, "Tabard")
                || Contains(materialName, "Flag")
                || Contains(materialName, "Awning")
                || Contains(materialName, "Sheet")
                || Contains(materialName, "Body"))
            {
                return TextureProfile.Cloth;
            }

            if (Contains(materialName, "Leather")
                || Contains(materialName, "Fur")
                || Contains(materialName, "Hide")
                || Contains(materialName, "Pack")
                || Contains(materialName, "Bag")
                || Contains(materialName, "Strap"))
            {
                return TextureProfile.Leather;
            }

            if (Contains(materialName, "Skin") || Contains(materialName, "Face") || Contains(materialName, "Head") || Contains(materialName, "Ear") || Contains(materialName, "Tail"))
            {
                return TextureProfile.Skin;
            }

            if (Contains(materialName, "Parchment")
                || Contains(materialName, "Scroll")
                || Contains(materialName, "Map")
                || Contains(materialName, "Paper")
                || Contains(materialName, "Bone")
                || Contains(materialName, "Tusk")
                || Contains(materialName, "Token"))
            {
                return TextureProfile.Parchment;
            }

            if (Contains(materialName, "Glass") || Contains(materialName, "Potion") || Contains(materialName, "Bottle") || Contains(materialName, "Eye"))
            {
                return TextureProfile.Glass;
            }

            return TextureProfile.Generic;
        }

        private static bool ShouldSkip(string name)
        {
            return Contains(name, "Selection")
                || Contains(name, "Hover")
                || Contains(name, "Marker")
                || Contains(name, "Fog")
                || Contains(name, "Vision")
                || Contains(name, "Range")
                || Contains(name, "Overlay")
                || Contains(name, "Shadow")
                || Contains(name, "Contact")
                || Contains(name, "Flow")
                || Contains(name, "River")
                || Contains(name, "Visible")
                || Contains(name, "Torch Light")
                || Contains(name, "Queued")
                || Contains(name, "Glow")
                || Contains(name, "Fire")
                || Contains(name, "Label");
        }

        private static Texture2D CreateTexture(TextureProfile profile, Color baseColor, int size, int seed)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = $"Generated {profile} {Quantize(baseColor.r)}-{Quantize(baseColor.g)}-{Quantize(baseColor.b)}",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Repeat
            };

            var pixels = new Color[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    pixels[y * size + x] = Sample(profile, x, y, size, seed);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static Color Sample(TextureProfile profile, int x, int y, int size, int seed)
        {
            var noise = ValueNoise(x, y, seed);
            var chunk = ValueNoise(x / 4, y / 4, seed + 91);
            var broad = Mathf.PerlinNoise((x + (seed & 127)) * 0.085f, (y + ((seed >> 8) & 127)) * 0.085f);
            var fine = Mathf.PerlinNoise((x - ((seed >> 16) & 127)) * 0.31f, (y + ((seed >> 4) & 127)) * 0.31f);
            var shade = 0.9f + (broad - 0.5f) * 0.24f + (fine - 0.5f) * 0.16f + (noise - 0.5f) * 0.12f + (chunk - 0.5f) * 0.22f;

            switch (profile)
            {
                case TextureProfile.Stone:
                    shade += IsTileMortar(x, y, 12, 8) ? -0.34f : 0.04f;
                    shade += IsCrack(x, y, seed, 13) ? -0.28f : 0f;
                    return Modulation(shade, 0.98f, 0.99f, 1.02f);
                case TextureProfile.Moss:
                    shade += IsTileMortar(x, y, 12, 8) ? -0.22f : 0.02f;
                    shade += IsCrack(x, y, seed, 13) ? -0.2f : 0f;
                    return Modulation(shade, 0.86f, 1.08f, 0.86f);
                case TextureProfile.Wood:
                    shade += Mathf.Sin((y + seed % 13) * 0.62f) * 0.18f;
                    shade += x % 9 == 0 ? -0.22f : 0f;
                    shade += x % 17 == 1 ? 0.12f : 0f;
                    return Modulation(shade, 1.08f, 0.92f, 0.76f);
                case TextureProfile.Straw:
                    shade += ((x + y + seed) % 5 == 0) ? 0.22f : 0f;
                    shade += ((x - y + seed) % 9 == 0) ? -0.2f : 0f;
                    return Modulation(shade, 1.14f, 1.05f, 0.74f);
                case TextureProfile.RoofTile:
                    shade += IsTileMortar(x, y, 10, 7) ? -0.3f : 0.06f;
                    shade += y % 7 < 2 ? 0.14f : 0f;
                    return Modulation(shade, 1.08f, 0.88f, 0.82f);
                case TextureProfile.Clay:
                    shade += (noise - 0.5f) * 0.22f;
                    shade += IsCrack(x, y, seed, 17) ? -0.16f : 0f;
                    return Modulation(shade, 1.08f, 0.96f, 0.82f);
                case TextureProfile.Dirt:
                    shade += IsPebble(x, y, seed) ? -0.24f : 0f;
                    return Modulation(shade, 0.96f, 0.9f, 0.78f);
                case TextureProfile.Grass:
                    shade += ((x * 3 + y + seed) % 11 == 0) ? 0.26f : 0f;
                    shade += ((x + y * 5 + seed) % 17 == 0) ? -0.18f : 0f;
                    return Modulation(shade, 0.84f, 1.12f, 0.78f);
                case TextureProfile.Metal:
                    shade += y % 7 == 0 ? 0.28f : 0f;
                    shade += x % 11 == 0 ? -0.18f : 0f;
                    return Modulation(shade, 0.95f, 0.99f, 1.08f);
                case TextureProfile.Gold:
                    shade += ((x + y * 2 + seed) % 13 == 0) ? 0.34f : 0f;
                    shade += ((x * 5 + y + seed) % 23 == 0) ? -0.14f : 0f;
                    return Modulation(shade, 1.18f, 1.03f, 0.62f);
                case TextureProfile.Cloth:
                    shade += (x % 5 == 0 || y % 5 == 0) ? -0.16f : 0f;
                    shade += ((x + y) % 10 == 0) ? 0.1f : 0f;
                    return Modulation(shade, 0.98f, 0.98f, 1.04f);
                case TextureProfile.Leather:
                    shade += (noise - 0.5f) * 0.3f;
                    shade += IsCrack(x, y, seed, 23) ? -0.12f : 0f;
                    return Modulation(shade, 1.08f, 0.9f, 0.74f);
                case TextureProfile.Skin:
                    shade += (noise - 0.5f) * 0.12f;
                    return Modulation(shade, 1.04f, 0.96f, 0.9f);
                case TextureProfile.Parchment:
                    shade += IsPebble(x, y, seed) ? -0.18f : 0f;
                    shade += x % 13 == 0 ? -0.08f : 0f;
                    return Modulation(shade, 1.1f, 1.04f, 0.86f);
                case TextureProfile.Glass:
                    shade += x % 8 == 0 ? 0.22f : 0f;
                    shade += y % 11 == 0 ? -0.1f : 0f;
                    return Modulation(shade, 0.88f, 1.05f, 1.12f);
                case TextureProfile.Ore:
                    shade += IsCrack(x, y, seed, 9) ? 0.36f : -0.08f;
                    return Modulation(shade, 0.9f, 0.95f, 1.05f);
                case TextureProfile.Coal:
                    shade += IsCrack(x, y, seed, 9) ? 0.18f : -0.16f;
                    return Modulation(shade, 0.78f, 0.78f, 0.76f);
                default:
                    return Modulation(shade, 1f, 1f, 1f);
            }
        }

        private static bool UsesLargeTexture(TextureProfile profile)
        {
            return profile == TextureProfile.Stone
                || profile == TextureProfile.Moss
                || profile == TextureProfile.Wood
                || profile == TextureProfile.Straw
                || profile == TextureProfile.RoofTile
                || profile == TextureProfile.Clay
                || profile == TextureProfile.Dirt
                || profile == TextureProfile.Grass
                || profile == TextureProfile.Ore;
        }

        private static Vector2 GetScale(TextureProfile profile)
        {
            switch (profile)
            {
                case TextureProfile.Wood:
                case TextureProfile.Straw:
                case TextureProfile.RoofTile:
                    return new Vector2(1.5f, 1.5f);
                case TextureProfile.Stone:
                case TextureProfile.Moss:
                case TextureProfile.Clay:
                    return new Vector2(1.25f, 1.25f);
                default:
                    return Vector2.one;
            }
        }

        private static Color Modulation(float shade, float r, float g, float b)
        {
            shade = Mathf.Clamp(shade, 0.84f, 1.14f);
            return new Color(Mathf.Clamp01(shade * r), Mathf.Clamp01(shade * g), Mathf.Clamp01(shade * b), 1f);
        }

        private static bool IsTileMortar(int x, int y, int width, int height)
        {
            var rowOffset = (y / height) % 2 == 0 ? 0 : width / 2;
            return y % height == 0 || (x + rowOffset) % width == 0;
        }

        private static bool IsCrack(int x, int y, int seed, int period)
        {
            var value = Mathf.Abs(((x * 7 + y * 11 + seed) % period) - period / 2);
            return value == 0 && ValueNoise(x / 2, y / 2, seed) > 0.58f;
        }

        private static bool IsPebble(int x, int y, int seed)
        {
            return ((x * 17 + y * 31 + seed) & 31) == 0;
        }

        private static float ValueNoise(int x, int y, int seed)
        {
            unchecked
            {
                var hash = seed;
                hash = hash * 397 ^ x * 73856093;
                hash = hash * 397 ^ y * 19349663;
                hash ^= hash >> 13;
                hash *= 1274126177;
                hash ^= hash >> 16;
                return (hash & 0xffff) / 65535f;
            }
        }

        private static int StableHash(string value)
        {
            unchecked
            {
                var hash = 17;
                for (var i = 0; i < value.Length; i++)
                {
                    hash = hash * 31 + value[i];
                }

                return hash;
            }
        }

        private static int Quantize(float value)
        {
            return Mathf.Clamp(Mathf.RoundToInt(value * 255f), 0, 255);
        }

        private static bool Contains(string value, string pattern)
        {
            return value.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
