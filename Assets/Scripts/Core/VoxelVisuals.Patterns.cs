using UnityEngine;

namespace Labyrinth.Core
{
    public static partial class VoxelVisuals
    {
        private enum VoxelPatternProfile
        {
            Generic,
            Stone,
            Wood,
            Roof,
            Straw,
            Metal,
            Gold,
            Cloth,
            Leather,
            Skin,
            Foliage,
            Glass,
            Ore,
            Dirt,
            Parchment
        }

        private static Material[] GetBlockGridMaterials(Material baseMaterial, string objectName)
        {
            if (BlockGridMaterials.TryGetValue(baseMaterial, out var cached))
            {
                return cached;
            }

            // Keep generated voxel grids to one submesh/material. The visible pattern is carried by
            // vertex colors and the generated texture, which is much cheaper than per-object palettes.
            var materials = new[] { baseMaterial };
            BlockGridMaterials[baseMaterial] = materials;
            return materials;
        }

        private static Material[] CreateSemanticPalette(Material baseMaterial, Color color, VoxelPatternProfile profile)
        {
            var light = Color.Lerp(color, Color.white, profile == VoxelPatternProfile.Metal || profile == VoxelPatternProfile.Gold ? 0.42f : 0.22f);
            var dark = Color.Lerp(color, Color.black, profile == VoxelPatternProfile.Stone || profile == VoxelPatternProfile.Wood ? 0.42f : 0.32f);
            var line = dark;
            var accent = light;

            switch (profile)
            {
                case VoxelPatternProfile.Stone:
                    line = Color.Lerp(color, Color.black, 0.72f);
                    accent = Color.Lerp(color, new Color(0.38f, 0.48f, 0.34f, color.a), 0.32f);
                    break;
                case VoxelPatternProfile.Wood:
                    line = Color.Lerp(color, new Color(0.06f, 0.03f, 0.012f, color.a), 0.7f);
                    accent = Color.Lerp(color, new Color(0.92f, 0.58f, 0.22f, color.a), 0.3f);
                    break;
                case VoxelPatternProfile.Roof:
                    line = Color.Lerp(color, Color.black, 0.6f);
                    accent = Color.Lerp(color, new Color(0.92f, 0.32f, 0.18f, color.a), 0.28f);
                    break;
                case VoxelPatternProfile.Straw:
                    line = Color.Lerp(color, new Color(0.28f, 0.17f, 0.04f, color.a), 0.62f);
                    accent = Color.Lerp(color, new Color(1f, 0.9f, 0.36f, color.a), 0.42f);
                    break;
                case VoxelPatternProfile.Metal:
                    line = Color.Lerp(color, Color.black, 0.55f);
                    accent = Color.Lerp(color, new Color(0.92f, 0.98f, 1f, color.a), 0.58f);
                    break;
                case VoxelPatternProfile.Gold:
                    line = Color.Lerp(color, new Color(0.45f, 0.24f, 0.02f, color.a), 0.5f);
                    accent = Color.Lerp(color, new Color(1f, 0.96f, 0.42f, color.a), 0.62f);
                    break;
                case VoxelPatternProfile.Cloth:
                    line = Color.Lerp(color, Color.black, 0.46f);
                    accent = Color.Lerp(color, Color.white, 0.28f);
                    break;
                case VoxelPatternProfile.Leather:
                    line = Color.Lerp(color, new Color(0.08f, 0.035f, 0.012f, color.a), 0.66f);
                    accent = Color.Lerp(color, new Color(0.9f, 0.54f, 0.24f, color.a), 0.22f);
                    break;
                case VoxelPatternProfile.Skin:
                    line = Color.Lerp(color, new Color(0.35f, 0.18f, 0.12f, color.a), 0.32f);
                    accent = Color.Lerp(color, new Color(1f, 0.8f, 0.56f, color.a), 0.2f);
                    break;
                case VoxelPatternProfile.Foliage:
                    line = Color.Lerp(color, new Color(0.035f, 0.16f, 0.035f, color.a), 0.52f);
                    accent = Color.Lerp(color, new Color(0.54f, 0.86f, 0.34f, color.a), 0.42f);
                    break;
                case VoxelPatternProfile.Glass:
                    line = Color.Lerp(color, new Color(0.08f, 0.2f, 0.24f, color.a), 0.36f);
                    accent = Color.Lerp(color, Color.white, 0.7f);
                    break;
                case VoxelPatternProfile.Ore:
                    line = Color.Lerp(color, Color.black, 0.58f);
                    accent = Color.Lerp(color, new Color(0.95f, 0.95f, 0.72f, color.a), 0.55f);
                    break;
                case VoxelPatternProfile.Dirt:
                    line = Color.Lerp(color, Color.black, 0.5f);
                    accent = Color.Lerp(color, new Color(0.72f, 0.56f, 0.32f, color.a), 0.22f);
                    break;
                case VoxelPatternProfile.Parchment:
                    line = Color.Lerp(color, new Color(0.42f, 0.28f, 0.12f, color.a), 0.38f);
                    accent = Color.Lerp(color, new Color(1f, 0.92f, 0.68f, color.a), 0.32f);
                    break;
            }

            light.a = color.a;
            dark.a = color.a;
            line.a = color.a;
            accent.a = color.a;
            return new[]
            {
                baseMaterial,
                CreateMaterialVariant(baseMaterial, $"{baseMaterial.name} Voxel Light", light),
                CreateMaterialVariant(baseMaterial, $"{baseMaterial.name} Voxel Dark", dark),
                CreateMaterialVariant(baseMaterial, $"{baseMaterial.name} Voxel Line", line),
                CreateMaterialVariant(baseMaterial, $"{baseMaterial.name} Voxel Accent", accent)
            };
        }

        private static int SelectBlockGridMaterial(string objectName, int x, int y, int z, int materialCount)
        {
            if (materialCount <= 1)
            {
                return 0;
            }

            if (materialCount <= 3)
            {
                var hash = Hash(objectName, x, y, z);
                return hash % 7 == 0 ? 2 : hash % 5 == 0 ? 1 : 0;
            }

            var profile = ResolveVoxelPatternProfile(objectName);
            var h = Hash(objectName, x, y, z);
            switch (profile)
            {
                case VoxelPatternProfile.Roof:
                    return y % 3 == 0 || z % 4 == 0 ? 3 : h % 7 == 0 ? 4 : h % 5 == 0 ? 2 : 0;
                case VoxelPatternProfile.Straw:
                    return (x + z) % 4 == 0 ? 3 : h % 5 == 0 ? 4 : h % 6 == 0 ? 2 : 0;
                case VoxelPatternProfile.Wood:
                    return y % 4 == 0 || x % 7 == 0 ? 3 : h % 11 == 0 ? 4 : h % 5 == 0 ? 2 : 0;
                case VoxelPatternProfile.Stone:
                    return y % 3 == 0 || x % 4 == 0 || z % 4 == 0 ? 3 : h % 9 == 0 ? 4 : h % 4 == 0 ? 2 : 0;
                case VoxelPatternProfile.Metal:
                    return (x + y + z) % 6 == 0 ? 4 : (x + z) % 5 == 0 ? 3 : h % 5 == 0 ? 2 : 0;
                case VoxelPatternProfile.Gold:
                    return h % 4 == 0 ? 4 : h % 9 == 0 ? 3 : 0;
                case VoxelPatternProfile.Cloth:
                    return x % 3 == 0 || y % 3 == 0 ? 3 : h % 8 == 0 ? 4 : h % 5 == 0 ? 2 : 0;
                case VoxelPatternProfile.Leather:
                case VoxelPatternProfile.Skin:
                    return h % 5 == 0 ? 3 : h % 7 == 0 ? 4 : h % 3 == 0 ? 2 : 0;
                case VoxelPatternProfile.Foliage:
                    return h % 4 == 0 ? 4 : h % 3 == 0 ? 2 : h % 5 == 0 ? 3 : 0;
                case VoxelPatternProfile.Glass:
                    return x % 5 == 0 || y % 5 == 0 ? 4 : h % 6 == 0 ? 3 : 0;
                case VoxelPatternProfile.Ore:
                    return (x + y * 2 + z) % 5 == 0 ? 4 : h % 4 == 0 ? 3 : 0;
                case VoxelPatternProfile.Dirt:
                    return h % 5 == 0 ? 3 : h % 7 == 0 ? 4 : h % 3 == 0 ? 2 : 0;
                case VoxelPatternProfile.Parchment:
                    return x % 6 == 0 || h % 9 == 0 ? 3 : h % 6 == 0 ? 4 : 0;
                default:
                    return h % 7 == 0 ? 4 : h % 5 == 0 ? 2 : h % 3 == 0 ? 1 : 0;
            }
        }

        private static float CalculateSemanticPatternLight(string objectName, Vector3 normal, int x, int y, int z)
        {
            if (Contains(objectName, "Roof") || Contains(objectName, "Straw") || Contains(objectName, "Thatch"))
            {
                var row = normal == Vector3.up ? z : y;
                var seam = row % 3 == 0;
                var stagger = (x + row / 2) % 5 == 0;
                return seam ? 0.94f : stagger ? 1.14f : 1.04f;
            }

            if (Contains(objectName, "Wood")
                || Contains(objectName, "Timber")
                || Contains(objectName, "Plank")
                || Contains(objectName, "Beam")
                || Contains(objectName, "Door")
                || Contains(objectName, "Fence")
                || Contains(objectName, "Chest")
                || Contains(objectName, "Crate")
                || Contains(objectName, "Barrel")
                || Contains(objectName, "Cart")
                || Contains(objectName, "Club")
                || Contains(objectName, "Post")
                || Contains(objectName, "Rack"))
            {
                var grain = normal == Vector3.up ? z : y;
                if (grain % 4 == 0)
                {
                    return 0.92f;
                }

                return (x + z) % 7 == 0 ? 1.14f : 1.04f;
            }

            if (Contains(objectName, "Stone")
                || Contains(objectName, "Wall")
                || Contains(objectName, "Voxels")
                || Contains(objectName, "Rock")
                || Contains(objectName, "Floor")
                || Contains(objectName, "Path")
                || Contains(objectName, "Stairs")
                || Contains(objectName, "Castle"))
            {
                var mortar = y % 3 == 0 || x % 4 == 0 || z % 4 == 0;
                if (mortar)
                {
                    return 0.9f;
                }

                return (Hash(objectName, x / 2, y / 2, z / 2) % 5 == 0) ? 1.1f : 1.03f;
            }

            if (Contains(objectName, "Metal")
                || Contains(objectName, "Iron")
                || Contains(objectName, "Armor")
                || Contains(objectName, "Sword")
                || Contains(objectName, "Shield")
                || Contains(objectName, "Blade")
                || Contains(objectName, "Anvil")
                || Contains(objectName, "Lock"))
            {
                return (x + y + z) % 5 == 0 ? 1.18f : ((x + z) % 6 == 0 ? 0.96f : 1.04f);
            }

            if (Contains(objectName, "Gold") || Contains(objectName, "Coin") || Contains(objectName, "Brass"))
            {
                return (x * 2 + y + z) % 5 == 0 ? 1.2f : 1.03f;
            }

            if (Contains(objectName, "Cloth")
                || Contains(objectName, "Cape")
                || Contains(objectName, "Tabard")
                || Contains(objectName, "Flag")
                || Contains(objectName, "Awning")
                || Contains(objectName, "Sheet")
                || Contains(objectName, "Body"))
            {
                return x % 3 == 0 || y % 3 == 0 ? 0.96f : 1.08f;
            }

            if (Contains(objectName, "Leather")
                || Contains(objectName, "Fur")
                || Contains(objectName, "Skin")
                || Contains(objectName, "Face")
                || Contains(objectName, "Head")
                || Contains(objectName, "Pack")
                || Contains(objectName, "Bag"))
            {
                return Hash(objectName, x, y, z) % 4 == 0 ? 0.98f : 1.06f;
            }

            if (Contains(objectName, "Leaves")
                || Contains(objectName, "Bush")
                || Contains(objectName, "Crop")
                || Contains(objectName, "Grass")
                || Contains(objectName, "Herb"))
            {
                return Hash(objectName, x, y, z) % 3 == 0 ? 1.16f : 1f;
            }

            return Hash(objectName, x, y, z) % 6 == 0 ? 0.98f : 1.04f;
        }

        private static VoxelPatternProfile ResolveVoxelPatternProfile(string name)
        {
            if (Contains(name, "Gold") || Contains(name, "Coin") || Contains(name, "Brass"))
            {
                return VoxelPatternProfile.Gold;
            }

            if (Contains(name, "Key") || Contains(name, "Ingot"))
            {
                return VoxelPatternProfile.Gold;
            }

            if (Contains(name, "Ore") || Contains(name, "Vein") || Contains(name, "Coal"))
            {
                return VoxelPatternProfile.Ore;
            }

            if (Contains(name, "Metal")
                || Contains(name, "Iron")
                || Contains(name, "Armor")
                || Contains(name, "Sword")
                || Contains(name, "Shield")
                || Contains(name, "Blade")
                || Contains(name, "Anvil")
                || Contains(name, "Lock")
                || Contains(name, "Axe")
                || Contains(name, "Hammer")
                || Contains(name, "Pick"))
            {
                return VoxelPatternProfile.Metal;
            }

            if (Contains(name, "Roof"))
            {
                return VoxelPatternProfile.Roof;
            }

            if (Contains(name, "Straw") || Contains(name, "Thatch") || Contains(name, "Bread") || Contains(name, "Ration") || Contains(name, "Rope"))
            {
                return VoxelPatternProfile.Straw;
            }

            if (Contains(name, "Wood")
                || Contains(name, "Timber")
                || Contains(name, "Plank")
                || Contains(name, "Beam")
                || Contains(name, "Door")
                || Contains(name, "Fence")
                || Contains(name, "Chest")
                || Contains(name, "Crate")
                || Contains(name, "Barrel")
                || Contains(name, "Cart")
                || Contains(name, "Club")
                || Contains(name, "Post")
                || Contains(name, "Rack")
                || Contains(name, "Counter")
                || Contains(name, "Logs"))
            {
                return VoxelPatternProfile.Wood;
            }

            if (Contains(name, "Leaves")
                || Contains(name, "Leaf")
                || Contains(name, "Bush")
                || Contains(name, "Crop")
                || Contains(name, "Grass")
                || Contains(name, "Herb")
                || Contains(name, "Pine"))
            {
                return VoxelPatternProfile.Foliage;
            }

            if (Contains(name, "Cloth")
                || Contains(name, "Cape")
                || Contains(name, "Tabard")
                || Contains(name, "Flag")
                || Contains(name, "Awning")
                || Contains(name, "Sheet")
                || Contains(name, "Body")
                || Contains(name, "Tent")
                || Contains(name, "Canvas"))
            {
                return VoxelPatternProfile.Cloth;
            }

            if (Contains(name, "Leather") || Contains(name, "Fur") || Contains(name, "Hide") || Contains(name, "Pack") || Contains(name, "Bag") || Contains(name, "Sack") || Contains(name, "Strap"))
            {
                return VoxelPatternProfile.Leather;
            }

            if (Contains(name, "Skin") || Contains(name, "Face") || Contains(name, "Head") || Contains(name, "Ear") || Contains(name, "Tail"))
            {
                return VoxelPatternProfile.Skin;
            }

            if (Contains(name, "Glass") || Contains(name, "Potion") || Contains(name, "Bottle") || Contains(name, "Eye"))
            {
                return VoxelPatternProfile.Glass;
            }

            if (Contains(name, "Parchment") || Contains(name, "Scroll") || Contains(name, "Map") || Contains(name, "Paper") || Contains(name, "Bone") || Contains(name, "Tusk") || Contains(name, "Token"))
            {
                return VoxelPatternProfile.Parchment;
            }

            if (Contains(name, "Ground") || Contains(name, "Dirt") || Contains(name, "Road") || Contains(name, "Yard"))
            {
                return VoxelPatternProfile.Dirt;
            }

            if (Contains(name, "Stone")
                || Contains(name, "Wall")
                || Contains(name, "Voxels")
                || Contains(name, "Rock")
                || Contains(name, "Floor")
                || Contains(name, "Path")
                || Contains(name, "Stairs")
                || Contains(name, "Castle")
                || Contains(name, "Clay"))
            {
                return VoxelPatternProfile.Stone;
            }

            return VoxelPatternProfile.Generic;
        }
    }
}
