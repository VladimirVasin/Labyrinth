using Labyrinth.Base;

namespace Labyrinth.Core
{
    public sealed partial class BaseDevelopment
    {
        public int FarmStorageCapacity => FarmBatchCapacity * 2;

        public int LumberjackStorageCapacity => LumberjackBatchCapacity * 2;

        public bool DebugAllBuildingsUnlocked { get; set; }

        public bool IsBuildingUnlocked(BuildingType type)
        {
            if (DebugAllBuildingsUnlocked)
            {
                return true;
            }

            switch (type)
            {
                case BuildingType.Castle:
                case BuildingType.Farm:
                case BuildingType.LumberjackCamp:
                case BuildingType.PeasantHut:
                    return true;
                case BuildingType.HeroHouse:
                    return FarmCount >= 1 && LumberjackCampCount >= 1;
                case BuildingType.AlchemistShop:
                case BuildingType.Tavern:
                    return FarmCount >= 1 && HeroHouseCount >= 1;
                case BuildingType.Forge:
                    return LumberjackCampCount >= 1 && HeroHouseCount >= 1;
                case BuildingType.Infirmary:
                    return HasTavern || HeroHouseCount >= 2;
                case BuildingType.CartographerHouse:
                    return HasTavern && HeroHouseCount >= 2;
                case BuildingType.HeroesGuild:
                    return HasTavern && HasForge;
                case BuildingType.Chapel:
                    return HasInfirmary && PeasantHutCount >= 1;
                case BuildingType.Market:
                    return PeasantHutCount >= 2 && (HasTavern || HasForge);
                case BuildingType.MinersGuild:
                    return HasForge && HasCartographerHouse;
                case BuildingType.Antiquary:
                    return HasMarket && HasMinersGuild;
                default:
                    return true;
            }
        }

        public string GetBuildingUnlockHint(BuildingType type)
        {
            switch (type)
            {
                case BuildingType.HeroHouse:
                    return "нужны ферма и лагерь лесорубов";
                case BuildingType.AlchemistShop:
                    return "нужны ферма и дом героя";
                case BuildingType.Tavern:
                    return "нужны ферма и дом героя";
                case BuildingType.Forge:
                    return "нужны лагерь лесорубов и дом героя";
                case BuildingType.Infirmary:
                    return "нужна харчевня или два дома героев";
                case BuildingType.CartographerHouse:
                    return "нужны харчевня и два дома героев";
                case BuildingType.HeroesGuild:
                    return "нужны харчевня и кузница";
                case BuildingType.Chapel:
                    return "нужны лазарет и лачужка крестьянина";
                case BuildingType.Market:
                    return "нужны две лачужки и харчевня или кузница";
                case BuildingType.MinersGuild:
                    return "нужны кузница и дом картографа";
                case BuildingType.Antiquary:
                    return "нужны рынок и гильдия шахтёров";
                default:
                    return "доступно";
            }
        }
    }
}
