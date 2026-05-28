using System;
using System.Collections.Generic;
using Labyrinth.Maze;
using UnityEngine;

namespace Labyrinth.Core
{
    public sealed class BaseDevelopment
    {
        public const int FarmGoldCost = 25;
        public const int FarmWoodCost = 5;
        public const int LumberjackCampGoldCost = 25;
        public const int LumberjackCampWoodCost = 10;
        public const int AlchemistShopGoldCost = 50;
        public const int AlchemistShopWoodCost = 15;
        public const int TavernGoldCost = 40;
        public const int TavernWoodCost = 20;
        public const int ForgeGoldCost = 60;
        public const int ForgeWoodCost = 25;
        public const int InfirmaryGoldCost = 45;
        public const int InfirmaryWoodCost = 15;
        public const int CartographerHouseGoldCost = 50;
        public const int CartographerHouseWoodCost = 15;
        public const int ChapelGoldCost = 55;
        public const int ChapelWoodCost = 20;
        public const int MinersGuildGoldCost = 60;
        public const int MinersGuildWoodCost = 25;
        public const int MarketGoldCost = 70;
        public const int MarketWoodCost = 20;
        public const int AntiquaryGoldCost = 80;
        public const int AntiquaryWoodCost = 25;
        public const int HeroGoldCost = 25;
        public const int HeroFoodCost = 10;
        public const int ReturnStoneGoldCost = 100;
        public const int HealthPotionGoldCost = 10;
        public const int HealthPotionBaseHealAmount = 5;
        public const int HealthPotionUpgradedHealAmount = 7;
        public const int HealthPotionBaseMaxCount = 3;
        public const int HealthPotionUpgradedMaxCount = 4;
        public const int InfirmaryFoodPerHitPoint = 5;
        public const int InfirmaryFoodPerSevereInjury = 35;
        public const int RationFoodCost = 10;
        public const int RationGoldCost = 10;
        public const int RationBaseStaminaRestore = 10;
        public const int RationUpgradedStaminaRestore = 12;
        public const int RationBaseMaxCount = 3;
        public const int RationUpgradedMaxCount = 4;
        public const int SteelSwordGoldCost = 35;
        public const int ChainmailGoldCost = 35;
        public const int LeatherBootsGoldCost = 30;
        public const int KnightSwordGoldCost = 55;
        public const int BrigandineGoldCost = 55;
        public const int PathfinderBootsGoldCost = 65;
        public const int MasterBladeGoldCost = 70;
        public const int PlateHarnessGoldCost = 70;
        public const int SwiftwalkerBootsGoldCost = 110;
        public const int WeaponTier2GoldCost = SteelSwordGoldCost;
        public const int ArmorTier2GoldCost = ChainmailGoldCost;
        public const int WeaponTier3GoldCost = MasterBladeGoldCost;
        public const int ArmorTier3GoldCost = PlateHarnessGoldCost;
        public const int CastleFootprintRadiusCells = 3;
        public const int FarmFootprintRadiusCells = 2;
        public const int LumberjackCampFootprintRadiusCells = 2;
        public const int HeroHouseFootprintRadiusCells = 2;
        public const int PeasantHutFootprintRadiusCells = 1;
        public const int AlchemistShopFootprintRadiusCells = 2;
        public const int TavernFootprintRadiusCells = 2;
        public const int ForgeFootprintRadiusCells = 2;
        public const int InfirmaryFootprintRadiusCells = 2;
        public const int CartographerHouseFootprintRadiusCells = 2;
        public const int ChapelFootprintRadiusCells = 2;
        public const int MinersGuildFootprintRadiusCells = 2;
        public const int MarketFootprintRadiusCells = 2;
        public const int AntiquaryFootprintRadiusCells = 2;
        public const int BuildingVisibilityPaddingCells = 1;

        private const int MinimumBuildingGapCells = 1;
        private const int PreferredBuildingSearchRadius = 18;

        private readonly List<Vector2Int> farmPositions = new List<Vector2Int>();
        private readonly List<Vector2Int> lumberjackCampPositions = new List<Vector2Int>();
        private readonly List<Vector2Int> heroHousePositions = new List<Vector2Int>();
        private readonly List<Vector2Int> peasantHutPositions = new List<Vector2Int>();
        private Vector2Int? alchemistShopPosition;
        private Vector2Int? tavernPosition;
        private Vector2Int? forgePosition;
        private Vector2Int? infirmaryPosition;
        private Vector2Int? cartographerHousePosition;
        private Vector2Int? chapelPosition;
        private Vector2Int? minersGuildPosition;
        private Vector2Int? marketPosition;
        private Vector2Int? antiquaryPosition;
        private Func<Vector2Int, int, bool> buildingPlacementBlocker;
        private int castleLevel = 1;
        private int farmLevel = 1;
        private int lumberjackCampLevel = 1;
        private int alchemistShopLevel = 1;
        private int tavernLevel = 1;
        private int forgeLevel = 1;

        public int FarmCount => farmPositions.Count;

        public int LumberjackCampCount => lumberjackCampPositions.Count;

        public int HeroHouseCount => heroHousePositions.Count;

        public int PeasantHutCount => peasantHutPositions.Count;

        public int CastleLevel => castleLevel;

        public int FarmLevel => farmLevel;

        public int LumberjackCampLevel => lumberjackCampLevel;

        public int AlchemistShopLevel => alchemistShopLevel;

        public int TavernLevel => tavernLevel;

        public int ForgeLevel => forgeLevel;

        public int MaxHeroCount => castleLevel >= 3 ? 8 : castleLevel >= 2 ? 6 : 5;

        public int FarmBatchCapacity => farmLevel >= 3 ? 25 : farmLevel >= 2 ? 15 : 10;

        public int FarmUnitsPerTick => 1;

        public int LumberjackBatchCapacity => lumberjackCampLevel >= 2 ? 15 : 10;

        public int LumberjackUnitsPerTick => lumberjackCampLevel >= 3 ? 2 : 1;

        public int HealthPotionHealAmount => alchemistShopLevel >= 2 ? HealthPotionUpgradedHealAmount : HealthPotionBaseHealAmount;

        public int HealthPotionMaxCount => alchemistShopLevel >= 3 ? HealthPotionUpgradedMaxCount : HealthPotionBaseMaxCount;

        public int RationStaminaRestore => tavernLevel >= 2 ? RationUpgradedStaminaRestore : RationBaseStaminaRestore;

        public int RationMaxCount => tavernLevel >= 3 ? RationUpgradedMaxCount : RationBaseMaxCount;

        public int ActivePlayerBuildingCount =>
            FarmCount
            + LumberjackCampCount
            + HeroHouseCount
            + (HasAlchemistShop ? 1 : 0)
            + (HasTavern ? 1 : 0)
            + (HasForge ? 1 : 0)
            + (HasInfirmary ? 1 : 0)
            + (HasCartographerHouse ? 1 : 0)
            + (HasChapel ? 1 : 0)
            + (HasMinersGuild ? 1 : 0)
            + (HasMarket ? 1 : 0)
            + (HasAntiquary ? 1 : 0);

        public int RequiredPeasantHutCount => ActivePlayerBuildingCount / 2;

        public bool HasAlchemistShop => alchemistShopPosition.HasValue;

        public bool HasTavern => tavernPosition.HasValue;

        public bool HasForge => forgePosition.HasValue;

        public bool HasInfirmary => infirmaryPosition.HasValue;

        public bool HasCartographerHouse => cartographerHousePosition.HasValue;

        public bool HasChapel => chapelPosition.HasValue;

        public bool HasMinersGuild => minersGuildPosition.HasValue;

        public bool HasMarket => marketPosition.HasValue;

        public bool HasAntiquary => antiquaryPosition.HasValue;

        public int FoodPerTimeUnit => FarmCount;

        public int WoodPerTimeUnit => LumberjackCampCount;

        public IReadOnlyList<Vector2Int> FarmPositions => farmPositions;

        public IReadOnlyList<Vector2Int> LumberjackCampPositions => lumberjackCampPositions;

        public IReadOnlyList<Vector2Int> HeroHousePositions => heroHousePositions;

        public IReadOnlyList<Vector2Int> PeasantHutPositions => peasantHutPositions;

        public Vector2Int AlchemistShopPosition => alchemistShopPosition ?? Vector2Int.zero;

        public Vector2Int TavernPosition => tavernPosition ?? Vector2Int.zero;

        public Vector2Int ForgePosition => forgePosition ?? Vector2Int.zero;

        public Vector2Int InfirmaryPosition => infirmaryPosition ?? Vector2Int.zero;

        public Vector2Int CartographerHousePosition => cartographerHousePosition ?? Vector2Int.zero;

        public Vector2Int ChapelPosition => chapelPosition ?? Vector2Int.zero;

        public Vector2Int MinersGuildPosition => minersGuildPosition ?? Vector2Int.zero;

        public Vector2Int MarketPosition => marketPosition ?? Vector2Int.zero;

        public Vector2Int AntiquaryPosition => antiquaryPosition ?? Vector2Int.zero;

        public string LastBuildMessage { get; private set; } = string.Empty;

        public void ConfigurePlacementBlocker(Func<Vector2Int, int, bool> blocker)
        {
            buildingPlacementBlocker = blocker;
        }

        public static BuildingCost FarmCost => new BuildingCost(FarmGoldCost, FarmWoodCost);

        public static BuildingCost AlchemistShopCost => new BuildingCost(AlchemistShopGoldCost, AlchemistShopWoodCost);

        public static BuildingCost TavernCost => new BuildingCost(TavernGoldCost, TavernWoodCost);

        public static BuildingCost ForgeCost => new BuildingCost(ForgeGoldCost, ForgeWoodCost);

        public static BuildingCost InfirmaryCost => new BuildingCost(InfirmaryGoldCost, InfirmaryWoodCost);

        public static BuildingCost CartographerHouseCost => new BuildingCost(CartographerHouseGoldCost, CartographerHouseWoodCost);

        public static BuildingCost ChapelCost => new BuildingCost(ChapelGoldCost, ChapelWoodCost);

        public static BuildingCost MinersGuildCost => new BuildingCost(MinersGuildGoldCost, MinersGuildWoodCost);

        public static BuildingCost MarketCost => new BuildingCost(MarketGoldCost, MarketWoodCost);

        public static BuildingCost AntiquaryCost => new BuildingCost(AntiquaryGoldCost, AntiquaryWoodCost);

        public static BuildingCost HeroCost => new BuildingCost(HeroGoldCost, 0, HeroFoodCost);

        public static BuildingCost GetUpgradeCost(BuildingUpgradeType type, int currentLevel)
        {
            var nextLevel = currentLevel + 1;
            if (nextLevel < 2 || nextLevel > 3)
            {
                return new BuildingCost(0, 0);
            }

            switch (type)
            {
                case BuildingUpgradeType.Castle:
                    return nextLevel == 2
                        ? new BuildingCost(100, 40, 0, 25)
                        : new BuildingCost(180, 80, 0, 60);
                case BuildingUpgradeType.Farm:
                    return nextLevel == 2
                        ? new BuildingCost(50, 20, 0, 15)
                        : new BuildingCost(90, 35, 0, 35);
                case BuildingUpgradeType.LumberjackCamp:
                    return nextLevel == 2
                        ? new BuildingCost(50, 20, 0, 15)
                        : new BuildingCost(95, 35, 0, 35);
                case BuildingUpgradeType.AlchemistShop:
                    return nextLevel == 2
                        ? new BuildingCost(70, 25, 0, 20)
                        : new BuildingCost(120, 45, 0, 45);
                case BuildingUpgradeType.Tavern:
                    return nextLevel == 2
                        ? new BuildingCost(65, 25, 0, 18)
                        : new BuildingCost(110, 40, 0, 40);
                case BuildingUpgradeType.Forge:
                    return nextLevel == 2
                        ? new BuildingCost(90, 30, 0, 30)
                        : new BuildingCost(160, 60, 0, 70);
                default:
                    return new BuildingCost(0, 0);
            }
        }

        public static BuildingCost GetLumberjackCampCost(int existingCampCount)
        {
            return new BuildingCost(
                LumberjackCampGoldCost,
                existingCampCount <= 0 ? 0 : LumberjackCampWoodCost);
        }

        public bool TryBuildFarm(MazeGenerationResult result, out Vector2Int farmPosition)
        {
            farmPosition = Vector2Int.zero;
            if (!TryBuild(result, FarmFootprintRadiusCells, 101, out farmPosition))
            {
                return false;
            }

            farmPositions.Add(farmPosition);
            LastBuildMessage = $"ферма построена ({farmPosition.x}, {farmPosition.y})";
            return true;
        }

        public bool TryBuildLumberjackCamp(MazeGenerationResult result, out Vector2Int campPosition)
        {
            campPosition = Vector2Int.zero;
            if (!TryBuild(result, LumberjackCampFootprintRadiusCells, 151, out campPosition))
            {
                return false;
            }

            lumberjackCampPositions.Add(campPosition);
            LastBuildMessage = $"лагерь лесорубов построен ({campPosition.x}, {campPosition.y})";
            return true;
        }

        public bool TryBuildHeroHouse(MazeGenerationResult result, out Vector2Int housePosition)
        {
            housePosition = Vector2Int.zero;
            if (!TryBuild(result, HeroHouseFootprintRadiusCells, 211, out housePosition))
            {
                return false;
            }

            heroHousePositions.Add(housePosition);
            LastBuildMessage = $"дом героя построен ({housePosition.x}, {housePosition.y})";
            return true;
        }

        public bool TryBuildPeasantHut(MazeGenerationResult result, out Vector2Int hutPosition)
        {
            hutPosition = Vector2Int.zero;
            if (!TryBuild(result, PeasantHutFootprintRadiusCells, 263, out hutPosition))
            {
                return false;
            }

            peasantHutPositions.Add(hutPosition);
            LastBuildMessage = $"лачужка крестьянина построена ({hutPosition.x}, {hutPosition.y})";
            return true;
        }

        public bool RemoveHeroHouse(Vector2Int housePosition)
        {
            var removed = heroHousePositions.Remove(housePosition);
            if (removed)
            {
                LastBuildMessage = $"дом героя удалён ({housePosition.x}, {housePosition.y})";
            }

            return removed;
        }

        public bool TryBuildAlchemistShop(MazeGenerationResult result, out Vector2Int shopPosition)
        {
            shopPosition = Vector2Int.zero;
            if (HasAlchemistShop)
            {
                LastBuildMessage = "лавка алхимика уже построена";
                return false;
            }

            if (!TryBuild(result, AlchemistShopFootprintRadiusCells, 317, out shopPosition))
            {
                return false;
            }

            alchemistShopPosition = shopPosition;
            LastBuildMessage = $"лавка алхимика построена ({shopPosition.x}, {shopPosition.y})";
            return true;
        }

        public bool TryBuildTavern(MazeGenerationResult result, out Vector2Int tavern)
        {
            tavern = Vector2Int.zero;
            if (HasTavern)
            {
                LastBuildMessage = "харчевня уже построена";
                return false;
            }

            if (!TryBuild(result, TavernFootprintRadiusCells, 419, out tavern))
            {
                return false;
            }

            tavernPosition = tavern;
            LastBuildMessage = $"харчевня построена ({tavern.x}, {tavern.y})";
            return true;
        }

        public bool TryBuildForge(MazeGenerationResult result, out Vector2Int forge)
        {
            forge = Vector2Int.zero;
            if (HasForge)
            {
                LastBuildMessage = "кузница уже построена";
                return false;
            }

            if (!TryBuild(result, ForgeFootprintRadiusCells, 523, out forge))
            {
                return false;
            }

            forgePosition = forge;
            LastBuildMessage = $"кузница построена ({forge.x}, {forge.y})";
            return true;
        }

        public bool TryBuildInfirmary(MazeGenerationResult result, out Vector2Int infirmary)
        {
            infirmary = Vector2Int.zero;
            if (HasInfirmary)
            {
                LastBuildMessage = "лазарет уже построен";
                return false;
            }

            if (!TryBuild(result, InfirmaryFootprintRadiusCells, 577, out infirmary))
            {
                return false;
            }

            infirmaryPosition = infirmary;
            LastBuildMessage = $"лазарет построен ({infirmary.x}, {infirmary.y})";
            return true;
        }

        public bool TryBuildCartographerHouse(MazeGenerationResult result, out Vector2Int house)
        {
            house = Vector2Int.zero;
            if (HasCartographerHouse)
            {
                LastBuildMessage = "дом картографа уже построен";
                return false;
            }

            if (!TryBuild(result, CartographerHouseFootprintRadiusCells, 631, out house))
            {
                return false;
            }

            cartographerHousePosition = house;
            LastBuildMessage = $"дом картографа построен ({house.x}, {house.y})";
            return true;
        }

        public bool TryBuildChapel(MazeGenerationResult result, out Vector2Int chapel)
        {
            chapel = Vector2Int.zero;
            if (HasChapel)
            {
                LastBuildMessage = "часовня уже построена";
                return false;
            }

            if (!TryBuild(result, ChapelFootprintRadiusCells, 683, out chapel))
            {
                return false;
            }

            chapelPosition = chapel;
            LastBuildMessage = $"часовня построена ({chapel.x}, {chapel.y})";
            return true;
        }

        public bool TryBuildMinersGuild(MazeGenerationResult result, out Vector2Int guild)
        {
            guild = Vector2Int.zero;
            if (HasMinersGuild)
            {
                LastBuildMessage = "гильдия шахтёров уже построена";
                return false;
            }

            if (!TryBuild(result, MinersGuildFootprintRadiusCells, 739, out guild))
            {
                return false;
            }

            minersGuildPosition = guild;
            LastBuildMessage = $"гильдия шахтёров построена ({guild.x}, {guild.y})";
            return true;
        }

        public bool TryBuildMarket(MazeGenerationResult result, out Vector2Int market)
        {
            market = Vector2Int.zero;
            if (HasMarket)
            {
                LastBuildMessage = "рынок уже построен";
                return false;
            }

            if (!TryBuild(result, MarketFootprintRadiusCells, 811, out market))
            {
                return false;
            }

            marketPosition = market;
            LastBuildMessage = $"рынок построен ({market.x}, {market.y})";
            return true;
        }

        public bool TryBuildAntiquary(MazeGenerationResult result, out Vector2Int antiquary)
        {
            antiquary = Vector2Int.zero;
            if (HasAntiquary)
            {
                LastBuildMessage = "антиквариат уже построен";
                return false;
            }

            if (!TryBuild(result, AntiquaryFootprintRadiusCells, 887, out antiquary))
            {
                return false;
            }

            antiquaryPosition = antiquary;
            LastBuildMessage = $"антиквариат построен ({antiquary.x}, {antiquary.y})";
            return true;
        }

        public void Reset()
        {
            farmPositions.Clear();
            lumberjackCampPositions.Clear();
            heroHousePositions.Clear();
            peasantHutPositions.Clear();
            alchemistShopPosition = null;
            tavernPosition = null;
            forgePosition = null;
            infirmaryPosition = null;
            cartographerHousePosition = null;
            chapelPosition = null;
            minersGuildPosition = null;
            marketPosition = null;
            antiquaryPosition = null;
            castleLevel = 1;
            farmLevel = 1;
            lumberjackCampLevel = 1;
            alchemistShopLevel = 1;
            tavernLevel = 1;
            forgeLevel = 1;
            LastBuildMessage = string.Empty;
        }

        public void ReportBuildBlocked(string message)
        {
            LastBuildMessage = message;
        }

        public int GetUpgradeLevel(BuildingUpgradeType type)
        {
            switch (type)
            {
                case BuildingUpgradeType.Castle:
                    return castleLevel;
                case BuildingUpgradeType.Farm:
                    return farmLevel;
                case BuildingUpgradeType.LumberjackCamp:
                    return lumberjackCampLevel;
                case BuildingUpgradeType.AlchemistShop:
                    return alchemistShopLevel;
                case BuildingUpgradeType.Tavern:
                    return tavernLevel;
                case BuildingUpgradeType.Forge:
                    return forgeLevel;
                default:
                    return 1;
            }
        }

        public bool CanUpgrade(BuildingUpgradeType type)
        {
            return GetUpgradeLevel(type) < 3 && HasUpgradeTarget(type);
        }

        public BuildingCost GetUpgradeCost(BuildingUpgradeType type)
        {
            return GetUpgradeCost(type, GetUpgradeLevel(type));
        }

        public bool TryUpgrade(BuildingUpgradeType type, ResourceWallet wallet)
        {
            if (!CanUpgrade(type))
            {
                LastBuildMessage = $"{GetUpgradeName(type)}: недоступно";
                return false;
            }

            var cost = GetUpgradeCost(type);
            if (wallet == null || !wallet.TrySpend(cost))
            {
                LastBuildMessage = $"{GetUpgradeName(type)}: нужно {cost.Format()}";
                return false;
            }

            SetUpgradeLevel(type, GetUpgradeLevel(type) + 1);
            LastBuildMessage = $"{GetUpgradeName(type)} улучшено до ур. {GetUpgradeLevel(type)}";
            return true;
        }

        public bool HasUpgradeTarget(BuildingUpgradeType type)
        {
            switch (type)
            {
                case BuildingUpgradeType.Castle:
                    return true;
                case BuildingUpgradeType.Farm:
                    return FarmCount > 0;
                case BuildingUpgradeType.LumberjackCamp:
                    return LumberjackCampCount > 0;
                case BuildingUpgradeType.AlchemistShop:
                    return HasAlchemistShop;
                case BuildingUpgradeType.Tavern:
                    return HasTavern;
                case BuildingUpgradeType.Forge:
                    return HasForge;
                default:
                    return false;
            }
        }

        public static string GetUpgradeName(BuildingUpgradeType type)
        {
            switch (type)
            {
                case BuildingUpgradeType.Castle:
                    return "Замок";
                case BuildingUpgradeType.Farm:
                    return "Фермы";
                case BuildingUpgradeType.LumberjackCamp:
                    return "Лесорубы";
                case BuildingUpgradeType.AlchemistShop:
                    return "Лавка алхимика";
                case BuildingUpgradeType.Tavern:
                    return "Харчевня";
                case BuildingUpgradeType.Forge:
                    return "Кузница";
                default:
                    return "Здание";
            }
        }

        private void SetUpgradeLevel(BuildingUpgradeType type, int level)
        {
            var normalizedLevel = Math.Max(1, Math.Min(3, level));
            switch (type)
            {
                case BuildingUpgradeType.Castle:
                    castleLevel = normalizedLevel;
                    break;
                case BuildingUpgradeType.Farm:
                    farmLevel = normalizedLevel;
                    break;
                case BuildingUpgradeType.LumberjackCamp:
                    lumberjackCampLevel = normalizedLevel;
                    break;
                case BuildingUpgradeType.AlchemistShop:
                    alchemistShopLevel = normalizedLevel;
                    break;
                case BuildingUpgradeType.Tavern:
                    tavernLevel = normalizedLevel;
                    break;
                case BuildingUpgradeType.Forge:
                    forgeLevel = normalizedLevel;
                    break;
            }
        }

        private bool TryBuild(MazeGenerationResult result, int footprintRadius, int seedSalt, out Vector2Int position)
        {
            position = Vector2Int.zero;
            if (result == null)
            {
                LastBuildMessage = "нет карты";
                return false;
            }

            if (!TryFindBuildingPosition(result, footprintRadius, seedSalt, out position))
            {
                LastBuildMessage = "нет свободной клетки на внешнем terrain";
                return false;
            }

            return true;
        }

        private bool TryFindBuildingPosition(
            MazeGenerationResult result,
            int footprintRadius,
            int seedSalt,
            out Vector2Int position)
        {
            position = Vector2Int.zero;
            var random = new System.Random(CreateBuildSeed(result, seedSalt));
            var minimumRadius = CastleFootprintRadiusCells + footprintRadius + MinimumBuildingGapCells;
            var maximumRadius = CalculateMaximumBuildingSearchRadius(result, footprintRadius);

            for (var radius = minimumRadius; radius <= maximumRadius; radius++)
            {
                var candidates = CollectBuildingCandidates(result, radius, footprintRadius);
                if (candidates.Count == 0)
                {
                    continue;
                }

                position = candidates[random.Next(candidates.Count)];
                if (radius > PreferredBuildingSearchRadius)
                {
                    GameDebugLog.Info(
                        "Base",
                        $"Building placement expanded: position={GameDebugLog.Position(position)}, radius={radius}, maxRadius={maximumRadius}, footprint={footprintRadius}, terrainPadding={MazeTerrain.PaddingCells}.");
                }

                return true;
            }

            return false;
        }

        private static int CalculateMaximumBuildingSearchRadius(MazeGenerationResult result, int footprintRadius)
        {
            if (result == null || result.Grid == null)
            {
                return PreferredBuildingSearchRadius;
            }

            var minX = -MazeTerrain.PaddingCells + footprintRadius;
            var minY = -MazeTerrain.PaddingCells + footprintRadius;
            var maxX = result.Grid.Width - 1 + MazeTerrain.PaddingCells - footprintRadius;
            var maxY = result.Grid.Height - 1 + MazeTerrain.PaddingCells - footprintRadius;
            var basePosition = result.BasePosition;
            var maximumRadius = 0;

            maximumRadius = Math.Max(maximumRadius, ChebyshevDistance(basePosition, new Vector2Int(minX, minY)));
            maximumRadius = Math.Max(maximumRadius, ChebyshevDistance(basePosition, new Vector2Int(minX, maxY)));
            maximumRadius = Math.Max(maximumRadius, ChebyshevDistance(basePosition, new Vector2Int(maxX, minY)));
            maximumRadius = Math.Max(maximumRadius, ChebyshevDistance(basePosition, new Vector2Int(maxX, maxY)));

            return Math.Max(PreferredBuildingSearchRadius, maximumRadius);
        }

        private List<Vector2Int> CollectBuildingCandidates(
            MazeGenerationResult result,
            int radius,
            int footprintRadius)
        {
            var candidates = new List<Vector2Int>();
            var basePosition = result.BasePosition;
            var minX = basePosition.x - radius;
            var maxX = basePosition.x + radius;
            var minY = basePosition.y - radius;
            var maxY = basePosition.y + radius;

            for (var x = minX; x <= maxX; x++)
            {
                AddBuildingCandidate(result, candidates, new Vector2Int(x, minY), footprintRadius);
                AddBuildingCandidate(result, candidates, new Vector2Int(x, maxY), footprintRadius);
            }

            for (var y = minY + 1; y <= maxY - 1; y++)
            {
                AddBuildingCandidate(result, candidates, new Vector2Int(minX, y), footprintRadius);
                AddBuildingCandidate(result, candidates, new Vector2Int(maxX, y), footprintRadius);
            }

            return candidates;
        }

        private void AddBuildingCandidate(
            MazeGenerationResult result,
            List<Vector2Int> candidates,
            Vector2Int position,
            int footprintRadius)
        {
            if (CanPlaceBuilding(result, position, footprintRadius))
            {
                candidates.Add(position);
            }
        }

        private bool CanPlaceBuilding(MazeGenerationResult result, Vector2Int position, int footprintRadius)
        {
            if (!HasTerrainClearance(result.Grid, position, footprintRadius))
            {
                return false;
            }

            if (!HasMazeClearance(result.Grid, position, footprintRadius))
            {
                return false;
            }

            if (buildingPlacementBlocker != null && buildingPlacementBlocker(position, footprintRadius))
            {
                return false;
            }

            if (IsTooClose(result.BasePosition, CastleFootprintRadiusCells, position, footprintRadius))
            {
                return false;
            }

            foreach (var farmPosition in farmPositions)
            {
                if (IsTooClose(farmPosition, FarmFootprintRadiusCells, position, footprintRadius))
                {
                    return false;
                }
            }

            foreach (var campPosition in lumberjackCampPositions)
            {
                if (IsTooClose(campPosition, LumberjackCampFootprintRadiusCells, position, footprintRadius))
                {
                    return false;
                }
            }

            foreach (var housePosition in heroHousePositions)
            {
                if (IsTooClose(housePosition, HeroHouseFootprintRadiusCells, position, footprintRadius))
                {
                    return false;
                }
            }

            foreach (var hutPosition in peasantHutPositions)
            {
                if (IsTooClose(hutPosition, PeasantHutFootprintRadiusCells, position, footprintRadius))
                {
                    return false;
                }
            }

            if (alchemistShopPosition.HasValue
                && IsTooClose(alchemistShopPosition.Value, AlchemistShopFootprintRadiusCells, position, footprintRadius))
            {
                return false;
            }

            if (tavernPosition.HasValue
                && IsTooClose(tavernPosition.Value, TavernFootprintRadiusCells, position, footprintRadius))
            {
                return false;
            }

            if (forgePosition.HasValue
                && IsTooClose(forgePosition.Value, ForgeFootprintRadiusCells, position, footprintRadius))
            {
                return false;
            }

            if (infirmaryPosition.HasValue
                && IsTooClose(infirmaryPosition.Value, InfirmaryFootprintRadiusCells, position, footprintRadius))
            {
                return false;
            }

            if (cartographerHousePosition.HasValue
                && IsTooClose(cartographerHousePosition.Value, CartographerHouseFootprintRadiusCells, position, footprintRadius))
            {
                return false;
            }

            if (chapelPosition.HasValue
                && IsTooClose(chapelPosition.Value, ChapelFootprintRadiusCells, position, footprintRadius))
            {
                return false;
            }

            if (minersGuildPosition.HasValue
                && IsTooClose(minersGuildPosition.Value, MinersGuildFootprintRadiusCells, position, footprintRadius))
            {
                return false;
            }

            if (marketPosition.HasValue
                && IsTooClose(marketPosition.Value, MarketFootprintRadiusCells, position, footprintRadius))
            {
                return false;
            }

            if (antiquaryPosition.HasValue
                && IsTooClose(antiquaryPosition.Value, AntiquaryFootprintRadiusCells, position, footprintRadius))
            {
                return false;
            }

            return true;
        }

        private int CreateBuildSeed(MazeGenerationResult result, int seedSalt)
        {
            unchecked
            {
                return result.Settings.Seed
                    ^ (seedSalt * 83492791)
                    ^ ((FarmCount + 1) * 73856093)
                    ^ ((LumberjackCampCount + 1) * 83492791)
                    ^ ((HeroHouseCount + 1) * 19349663)
                    ^ ((PeasantHutCount + 1) * 961748927)
                    ^ (HasAlchemistShop ? 433494437 : 0)
                    ^ (HasTavern ? 961748941 : 0)
                    ^ (HasForge ? 645599791 : 0)
                    ^ (HasInfirmary ? 49979687 : 0)
                    ^ (HasCartographerHouse ? 15485863 : 0)
                    ^ (HasChapel ? 32452843 : 0)
                    ^ (HasMinersGuild ? 67867967 : 0)
                    ^ (HasMarket ? 86028121 : 0)
                    ^ (HasAntiquary ? 98602363 : 0)
                    ^ (result.BasePosition.y * 1274126177);
            }
        }

        private static bool IsTooClose(
            Vector2Int existingBuilding,
            int existingFootprintRadius,
            Vector2Int candidate,
            int candidateFootprintRadius)
        {
            var minimumDistance = existingFootprintRadius + candidateFootprintRadius + MinimumBuildingGapCells;
            return ChebyshevDistance(existingBuilding, candidate) <= minimumDistance;
        }

        private static int ChebyshevDistance(Vector2Int a, Vector2Int b)
        {
            return Math.Max(Math.Abs(a.x - b.x), Math.Abs(a.y - b.y));
        }

        private static bool HasMazeClearance(MazeGrid grid, Vector2Int position, int footprintRadius)
        {
            for (var x = position.x - footprintRadius; x <= position.x + footprintRadius; x++)
            {
                for (var y = position.y - footprintRadius; y <= position.y + footprintRadius; y++)
                {
                    if (grid.InBounds(new Vector2Int(x, y)))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool HasTerrainClearance(MazeGrid grid, Vector2Int position, int footprintRadius)
        {
            if (grid == null)
            {
                return false;
            }

            var minX = -MazeTerrain.PaddingCells;
            var minY = -MazeTerrain.PaddingCells;
            var maxX = grid.Width - 1 + MazeTerrain.PaddingCells;
            var maxY = grid.Height - 1 + MazeTerrain.PaddingCells;

            return position.x - footprintRadius >= minX
                && position.y - footprintRadius >= minY
                && position.x + footprintRadius <= maxX
                && position.y + footprintRadius <= maxY;
        }
    }
}
