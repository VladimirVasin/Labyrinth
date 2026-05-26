using Labyrinth.Base;
using Labyrinth.Core;
using Labyrinth.Hero;

namespace Labyrinth.UI
{
    public static class BuildingServiceCatalog
    {
        private static BuildingServiceEntry[] blessingEntries;

        private static readonly BuildingServiceEntry[] FarmEntries =
        {
            new BuildingServiceEntry(
                "Караван пищи",
                "10 пищи",
                "Ферма копит пищу и отправляет повозку в замок только после заполнения склада.")
        };

        private static readonly BuildingServiceEntry[] LumberjackEntries =
        {
            new BuildingServiceEntry(
                "Караван дерева",
                "10 дер.",
                "Лагерь копит дерево и доставляет его в замок повозкой.")
        };

        private static readonly BuildingServiceEntry[] ForgeBaseEntries =
        {
            new BuildingServiceEntry(
                HeroInventory.SteelSwordItemName,
                $"{BaseDevelopment.SteelSwordGoldCost} зол.",
                $"+{HeroInventory.SteelSwordAttackBonus} Attack Points вместо ржавого меча.",
                "Ур. 1"),
            new BuildingServiceEntry(
                HeroInventory.ChainmailItemName,
                $"{BaseDevelopment.ChainmailGoldCost} зол.",
                $"+{HeroInventory.ChainmailArmorBonus} Armor Points вместо обычной одежды.",
                "Ур. 1"),
            new BuildingServiceEntry(
                HeroInventory.LeatherBootsItemName,
                $"{BaseDevelopment.LeatherBootsGoldCost} зол.",
                $"+{HeroInventory.LeatherBootsMoveSpeedBonusPercent}% скорости передвижения.",
                "Ур. 1")
        };

        private static readonly BuildingServiceEntry[] ForgeLevel3Entries =
        {
            new BuildingServiceEntry(
                HeroInventory.MasterBladeItemName,
                $"{BaseDevelopment.MasterBladeGoldCost} зол.",
                $"+{HeroInventory.MasterBladeAttackBonus} Attack Points.",
                "Ур. 3"),
            new BuildingServiceEntry(
                HeroInventory.PlateHarnessItemName,
                $"{BaseDevelopment.PlateHarnessGoldCost} зол.",
                $"+{HeroInventory.PlateHarnessArmorBonus} Armor Points.",
                "Ур. 3"),
            new BuildingServiceEntry(
                HeroInventory.SwiftwalkerBootsItemName,
                $"{BaseDevelopment.SwiftwalkerBootsGoldCost} зол.",
                $"+{HeroInventory.SwiftwalkerBootsMoveSpeedBonusPercent}% скорости передвижения.",
                "Ур. 3")
        };

        private static readonly BuildingServiceEntry[] ForgeLevel2Entries =
        {
            new BuildingServiceEntry(
                HeroInventory.KnightSwordItemName,
                $"{BaseDevelopment.KnightSwordGoldCost} зол.",
                $"+{HeroInventory.KnightSwordAttackBonus} Attack Points.",
                "Ур. 2"),
            new BuildingServiceEntry(
                HeroInventory.BrigandineItemName,
                $"{BaseDevelopment.BrigandineGoldCost} зол.",
                $"+{HeroInventory.BrigandineArmorBonus} Armor Points.",
                "Ур. 2"),
            new BuildingServiceEntry(
                HeroInventory.PathfinderBootsItemName,
                $"{BaseDevelopment.PathfinderBootsGoldCost} зол.",
                $"+{HeroInventory.PathfinderBootsMoveSpeedBonusPercent}% скорости передвижения.",
                "Ур. 2")
        };

        private static readonly BuildingServiceEntry[] InfirmaryEntries =
        {
            new BuildingServiceEntry(
                "Лечение",
                $"{BaseDevelopment.InfirmaryFoodPerHitPoint} пищи / HP",
                "У входа восстанавливает раненым рыцарям недостающее здоровье за пищу из казны.")
        };

        private static readonly BuildingServiceEntry[] CartographerEntries =
        {
            new BuildingServiceEntry(
                "Общая карта",
                "бесплатно",
                "Рыцари у входа сдают личные знания карты и получают разведку других рыцарей.")
        };

        private static readonly BuildingServiceEntry[] MinersGuildEntries =
        {
            new BuildingServiceEntry(
                "Шахта",
                $"{MineConstructionController.RouteWoodCost} дер./клетка + {MineConstructionController.MineWoodCost} дер.",
                "В замке открывает выбор изученной малой пещеры. Сначала рабочие укрепляют маршрут от входа, затем ставят саму шахту.")
        };

        private static readonly BuildingServiceEntry[] MarketEntries =
        {
            new BuildingServiceEntry(
                "Обмен ресурсов",
                "динамично",
                "Покупка и продажа пищи, дерева и железа за золото. Чем ниже запас ресурса, тем выше его цена.")
        };

        public static BuildingServiceEntry[] Get(BuildingType type, int buildingLevel = 1)
        {
            var level = ClampLevel(buildingLevel);
            switch (type)
            {
                case BuildingType.Farm:
                    return FarmEntries;
                case BuildingType.LumberjackCamp:
                    return LumberjackEntries;
                case BuildingType.AlchemistShop:
                    return GetAlchemistEntries(level);
                case BuildingType.Tavern:
                    return GetTavernEntries(level);
                case BuildingType.Forge:
                    return GetForgeEntries(level);
                case BuildingType.Infirmary:
                    return InfirmaryEntries;
                case BuildingType.CartographerHouse:
                    return CartographerEntries;
                case BuildingType.Chapel:
                    return GetBlessingEntries();
                case BuildingType.MinersGuild:
                    return MinersGuildEntries;
                case BuildingType.Market:
                    return MarketEntries;
                default:
                    return System.Array.Empty<BuildingServiceEntry>();
            }
        }

        private static int ClampLevel(int level)
        {
            return level < 1 ? 1 : level > 3 ? 3 : level;
        }

        private static BuildingServiceEntry[] GetAlchemistEntries(int level)
        {
            var heal = level >= 2
                ? BaseDevelopment.HealthPotionUpgradedHealAmount
                : BaseDevelopment.HealthPotionBaseHealAmount;
            var maxCount = level >= 3
                ? BaseDevelopment.HealthPotionUpgradedMaxCount
                : BaseDevelopment.HealthPotionBaseMaxCount;
            return new[]
            {
                new BuildingServiceEntry(
                    HeroInventory.HealthPotionItemName,
                    $"{BaseDevelopment.HealthPotionGoldCost} зол.",
                    $"Рыцарь покупает у входа про запас. Лечит {heal} HP, максимум в сумке: {maxCount}.",
                    $"Ур. {level}")
            };
        }

        private static BuildingServiceEntry[] GetTavernEntries(int level)
        {
            var stamina = level >= 2
                ? BaseDevelopment.RationUpgradedStaminaRestore
                : BaseDevelopment.RationBaseStaminaRestore;
            var maxCount = level >= 3
                ? BaseDevelopment.RationUpgradedMaxCount
                : BaseDevelopment.RationBaseMaxCount;
            return new[]
            {
                new BuildingServiceEntry(
                    HeroInventory.RationItemName,
                    $"{BaseDevelopment.RationGoldCost} зол. + {BaseDevelopment.RationFoodCost} пищи",
                    $"Рыцарь покупает у входа. Восстанавливает {stamina} выносливости, максимум в сумке: {maxCount}.",
                    $"Ур. {level}")
            };
        }

        private static BuildingServiceEntry[] GetForgeEntries(int level)
        {
            if (level >= 3)
            {
                return ForgeLevel3Entries;
            }

            return level >= 2 ? ForgeLevel2Entries : ForgeBaseEntries;
        }

        private static BuildingServiceEntry[] GetBlessingEntries()
        {
            if (blessingEntries != null)
            {
                return blessingEntries;
            }

            blessingEntries = new BuildingServiceEntry[HeroBlessingCatalog.PurchaseOrder.Length];
            for (var i = 0; i < blessingEntries.Length; i++)
            {
                var blessing = HeroBlessingCatalog.PurchaseOrder[i];
                blessingEntries[i] = new BuildingServiceEntry(
                    blessing.DisplayName,
                    $"{blessing.GoldCost} зол.",
                    blessing.Description);
            }

            return blessingEntries;
        }
    }
}
