using System;
using Labyrinth.Core;
using Labyrinth.Maze;
using UnityEngine;

namespace Labyrinth.UI
{
    public sealed class BaseHudView : MonoBehaviour
    {
        private MazeGenerationResult result;
        private Action buildFarmRequested;
        private Action buildLumberjackCampRequested;
        private Action buildAlchemistShopRequested;
        private Action buildTavernRequested;
        private Action buildForgeRequested;
        private Action buildInfirmaryRequested;
        private Action buildCartographerHouseRequested;
        private Action buildChapelRequested;
        private Action buildMinersGuildRequested;
        private Action buildMarketRequested;
        private Action createHeroRequested;
        private Action mineSelectionRequested;
        private Action<BuildingUpgradeType> upgradeRequested;
        private Func<string> farmStatusProvider;
        private Func<string> lumberjackCampStatusProvider;
        private Func<string> alchemistShopStatusProvider;
        private Func<string> tavernStatusProvider;
        private Func<string> forgeStatusProvider;
        private Func<string> infirmaryStatusProvider;
        private Func<string> cartographerHouseStatusProvider;
        private Func<string> chapelStatusProvider;
        private Func<string> minersGuildStatusProvider;
        private Func<string> marketStatusProvider;
        private Func<string> mineStatusProvider;
        private Func<string> heroHouseStatusProvider;
        private Func<BuildingCost> farmCostProvider;
        private Func<BuildingCost> lumberjackCampCostProvider;
        private Func<BuildingCost> alchemistShopCostProvider;
        private Func<BuildingCost> tavernCostProvider;
        private Func<BuildingCost> forgeCostProvider;
        private Func<BuildingCost> infirmaryCostProvider;
        private Func<BuildingCost> cartographerHouseCostProvider;
        private Func<BuildingCost> chapelCostProvider;
        private Func<BuildingCost> minersGuildCostProvider;
        private Func<BuildingCost> marketCostProvider;
        private Func<BuildingCost> heroCostProvider;
        private Func<bool> canBuildFarmProvider;
        private Func<bool> canBuildLumberjackCampProvider;
        private Func<bool> canBuildAlchemistShopProvider;
        private Func<bool> canBuildTavernProvider;
        private Func<bool> canBuildForgeProvider;
        private Func<bool> canBuildInfirmaryProvider;
        private Func<bool> canBuildCartographerHouseProvider;
        private Func<bool> canBuildChapelProvider;
        private Func<bool> canBuildMinersGuildProvider;
        private Func<bool> canBuildMarketProvider;
        private Func<bool> canStartMineSelectionProvider;
        private Func<bool> canCreateHeroProvider;
        private Func<BuildingUpgradeType, string> upgradeStatusProvider;
        private Func<BuildingUpgradeType, bool> canUpgradeProvider;
        private Func<BuildingUpgradeType, BuildingCost> upgradeCostProvider;
        private bool visible;
        private BaseHudTab selectedTab;
        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle tabStyle;
        private GUIStyle activeTabStyle;
        private GUIStyle sectionStyle;
        private GUIStyle statLabelStyle;
        private GUIStyle statValueStyle;
        private GUIStyle cardTitleStyle;
        private GUIStyle cardIconStyle;
        private GUIStyle cardSubtitleStyle;
        private GUIStyle cardStatusStyle;
        private GUIStyle costStyle;
        private GUIStyle unavailableCostStyle;
        private GUIStyle commandButtonStyle;
        private GUIStyle closeButtonStyle;

        private enum BaseHudTab
        {
            Buildings,
            Heroes,
            Dungeon,
            Upgrades
        }

        public bool IsVisible => visible;

        public bool ContainsScreenPoint(Vector2 screenPosition)
        {
            if (!visible)
            {
                return false;
            }

            return CalculatePanelRect().Contains(ToGuiPoint(screenPosition));
        }

        public void Show(
            MazeGenerationResult generationResult,
            Action onBuildFarmRequested,
            Func<string> onFarmStatusRequested,
            Func<bool> onCanBuildFarmRequested,
            Func<BuildingCost> onFarmCostRequested,
            Action onBuildLumberjackCampRequested,
            Func<string> onLumberjackCampStatusRequested,
            Func<bool> onCanBuildLumberjackCampRequested,
            Func<BuildingCost> onLumberjackCampCostRequested,
            Action onBuildAlchemistShopRequested,
            Func<string> onAlchemistShopStatusRequested,
            Func<bool> onCanBuildAlchemistShopRequested,
            Func<BuildingCost> onAlchemistShopCostRequested,
            Action onBuildTavernRequested,
            Func<string> onTavernStatusRequested,
            Func<bool> onCanBuildTavernRequested,
            Func<BuildingCost> onTavernCostRequested,
            Action onBuildForgeRequested,
            Func<string> onForgeStatusRequested,
            Func<bool> onCanBuildForgeRequested,
            Func<BuildingCost> onForgeCostRequested,
            Action onBuildInfirmaryRequested,
            Func<string> onInfirmaryStatusRequested,
            Func<bool> onCanBuildInfirmaryRequested,
            Func<BuildingCost> onInfirmaryCostRequested,
            Action onBuildCartographerHouseRequested,
            Func<string> onCartographerHouseStatusRequested,
            Func<bool> onCanBuildCartographerHouseRequested,
            Func<BuildingCost> onCartographerHouseCostRequested,
            Action onBuildChapelRequested,
            Func<string> onChapelStatusRequested,
            Func<bool> onCanBuildChapelRequested,
            Func<BuildingCost> onChapelCostRequested,
            Action onBuildMinersGuildRequested,
            Func<string> onMinersGuildStatusRequested,
            Func<bool> onCanBuildMinersGuildRequested,
            Func<BuildingCost> onMinersGuildCostRequested,
            Action onBuildMarketRequested,
            Func<string> onMarketStatusRequested,
            Func<bool> onCanBuildMarketRequested,
            Func<BuildingCost> onMarketCostRequested,
            Func<string> onHeroHouseStatusRequested,
            Action onCreateHeroRequested,
            Func<bool> onCanCreateHeroRequested,
            Func<BuildingCost> onHeroCostRequested,
            Func<string> onMineStatusRequested,
            Func<bool> onCanStartMineSelectionRequested,
            Action onMineSelectionRequested,
            Func<BuildingUpgradeType, string> onUpgradeStatusRequested,
            Func<BuildingUpgradeType, bool> onCanUpgradeRequested,
            Func<BuildingUpgradeType, BuildingCost> onUpgradeCostRequested,
            Action<BuildingUpgradeType> onUpgradeRequested)
        {
            result = generationResult;
            buildFarmRequested = onBuildFarmRequested;
            buildLumberjackCampRequested = onBuildLumberjackCampRequested;
            buildAlchemistShopRequested = onBuildAlchemistShopRequested;
            buildTavernRequested = onBuildTavernRequested;
            buildForgeRequested = onBuildForgeRequested;
            buildInfirmaryRequested = onBuildInfirmaryRequested;
            buildCartographerHouseRequested = onBuildCartographerHouseRequested;
            buildChapelRequested = onBuildChapelRequested;
            buildMinersGuildRequested = onBuildMinersGuildRequested;
            buildMarketRequested = onBuildMarketRequested;
            farmStatusProvider = onFarmStatusRequested;
            canBuildFarmProvider = onCanBuildFarmRequested;
            farmCostProvider = onFarmCostRequested;
            lumberjackCampStatusProvider = onLumberjackCampStatusRequested;
            canBuildLumberjackCampProvider = onCanBuildLumberjackCampRequested;
            lumberjackCampCostProvider = onLumberjackCampCostRequested;
            alchemistShopStatusProvider = onAlchemistShopStatusRequested;
            canBuildAlchemistShopProvider = onCanBuildAlchemistShopRequested;
            alchemistShopCostProvider = onAlchemistShopCostRequested;
            tavernStatusProvider = onTavernStatusRequested;
            canBuildTavernProvider = onCanBuildTavernRequested;
            tavernCostProvider = onTavernCostRequested;
            forgeStatusProvider = onForgeStatusRequested;
            canBuildForgeProvider = onCanBuildForgeRequested;
            forgeCostProvider = onForgeCostRequested;
            infirmaryStatusProvider = onInfirmaryStatusRequested;
            canBuildInfirmaryProvider = onCanBuildInfirmaryRequested;
            infirmaryCostProvider = onInfirmaryCostRequested;
            cartographerHouseStatusProvider = onCartographerHouseStatusRequested;
            canBuildCartographerHouseProvider = onCanBuildCartographerHouseRequested;
            cartographerHouseCostProvider = onCartographerHouseCostRequested;
            chapelStatusProvider = onChapelStatusRequested;
            canBuildChapelProvider = onCanBuildChapelRequested;
            chapelCostProvider = onChapelCostRequested;
            minersGuildStatusProvider = onMinersGuildStatusRequested;
            canBuildMinersGuildProvider = onCanBuildMinersGuildRequested;
            minersGuildCostProvider = onMinersGuildCostRequested;
            marketStatusProvider = onMarketStatusRequested;
            canBuildMarketProvider = onCanBuildMarketRequested;
            marketCostProvider = onMarketCostRequested;
            heroHouseStatusProvider = onHeroHouseStatusRequested;
            createHeroRequested = onCreateHeroRequested;
            canCreateHeroProvider = onCanCreateHeroRequested;
            heroCostProvider = onHeroCostRequested;
            mineStatusProvider = onMineStatusRequested;
            canStartMineSelectionProvider = onCanStartMineSelectionRequested;
            mineSelectionRequested = onMineSelectionRequested;
            upgradeStatusProvider = onUpgradeStatusRequested;
            canUpgradeProvider = onCanUpgradeRequested;
            upgradeCostProvider = onUpgradeCostRequested;
            upgradeRequested = onUpgradeRequested;
            if (!visible)
            {
                GameAudioController.PlayUi(GameSfx.HudOpen);
            }

            visible = true;
        }

        public void Hide()
        {
            if (visible)
            {
                GameAudioController.PlayUi(GameSfx.HudClose);
            }

            visible = false;
        }

        private void OnGUI()
        {
            if (!visible || result == null)
            {
                return;
            }

            EnsureStyles();

            var rect = CalculatePanelRect();

            DrawPanel(rect);
            GUI.Label(new Rect(rect.x + 20f, rect.y + 12f, rect.width - 40f, 30f), "Замок", titleStyle);
            GUI.Label(new Rect(rect.x + 20f, rect.y + 42f, rect.width - 40f, 18f), "центр базы", subtitleStyle);

            var contentX = rect.x + 20f;
            var contentWidth = rect.width - 40f;
            var tabY = rect.y + 72f;
            DrawTabs(new Rect(contentX, tabY, contentWidth, 38f));

            var contentRect = new Rect(contentX, tabY + 50f, contentWidth, rect.yMax - tabY - 106f);
            switch (selectedTab)
            {
                case BaseHudTab.Heroes:
                    DrawHeroesTab(contentRect);
                    break;
                case BaseHudTab.Dungeon:
                    DrawDungeonTab(contentRect);
                    break;
                case BaseHudTab.Upgrades:
                    DrawUpgradesTab(contentRect);
                    break;
                case BaseHudTab.Buildings:
                default:
                    DrawBuildingsTab(contentRect);
                    break;
            }

            if (GUI.Button(new Rect(contentX, rect.yMax - 48f, contentWidth, 36f), "Закрыть", closeButtonStyle))
            {
                Hide();
            }
        }

        private void DrawTabs(Rect rect)
        {
            var gap = 8f;
            var tabWidth = (rect.width - gap * 3f) / 4f;
            DrawTabButton(new Rect(rect.x, rect.y, tabWidth, rect.height), BaseHudTab.Buildings, "Здания");
            DrawTabButton(new Rect(rect.x + tabWidth + gap, rect.y, tabWidth, rect.height), BaseHudTab.Heroes, "Герои");
            DrawTabButton(new Rect(rect.x + (tabWidth + gap) * 2f, rect.y, tabWidth, rect.height), BaseHudTab.Dungeon, "Подземелье");
            DrawTabButton(new Rect(rect.x + (tabWidth + gap) * 3f, rect.y, tabWidth, rect.height), BaseHudTab.Upgrades, "Улучшения");
        }

        private void DrawTabButton(Rect rect, BaseHudTab tab, string label)
        {
            var active = selectedTab == tab;
            if (GUI.Button(rect, label, active ? activeTabStyle : tabStyle))
            {
                if (!active)
                {
                    GameAudioController.PlayUi(GameSfx.HudTab);
                }

                selectedTab = tab;
            }
        }

        private void DrawBuildingsTab(Rect rect)
        {
            DrawSection(new Rect(rect.x, rect.y, rect.width, 22f), "Строительство");
            const float gap = 8f;
            var y = rect.y + 30f;
            var cardWidth = (rect.width - gap) * 0.5f;
            var cardHeight = Mathf.Min(108f, (rect.height - 30f - gap * 3f) / 4f);
            var leftX = rect.x;
            var rightX = rect.x + cardWidth + gap;

            DrawActionCard(
                new Rect(leftX, y, cardWidth, cardHeight),
                "🌾",
                "Фермы",
                "Пища и караваны",
                CleanStatus(GetFarmStatus(), GetFarmCost()),
                GetFarmCost(),
                CanBuildFarm(),
                "Построить",
                buildFarmRequested);

            DrawActionCard(
                new Rect(rightX, y, cardWidth, cardHeight),
                "🪓",
                "Лагеря лесорубов",
                "Дерево и караваны",
                CleanStatus(GetLumberjackCampStatus(), GetLumberjackCampCost()),
                GetLumberjackCampCost(),
                CanBuildLumberjackCamp(),
                "Построить",
                buildLumberjackCampRequested);
            y += cardHeight + gap;

            DrawActionCard(
                new Rect(leftX, y, cardWidth, cardHeight),
                "⚗",
                "Лавка алхимика",
                "Зелья здоровья",
                CleanStatus(GetAlchemistShopStatus(), GetAlchemistShopCost()),
                GetAlchemistShopCost(),
                CanBuildAlchemistShop(),
                GetAlchemistShopStatus().StartsWith("построена") ? "Построено" : "Построить",
                buildAlchemistShopRequested);

            DrawActionCard(
                new Rect(rightX, y, cardWidth, cardHeight),
                "🍖",
                "Харчевня",
                "Пайки для рыцарей",
                CleanStatus(GetTavernStatus(), GetTavernCost()),
                GetTavernCost(),
                CanBuildTavern(),
                GetTavernStatus().StartsWith("построена") ? "Построено" : "Построить",
                buildTavernRequested);
            y += cardHeight + gap;

            DrawActionCard(
                new Rect(leftX, y, cardWidth, cardHeight),
                "✚",
                "Лазарет",
                "Лечение за пищу",
                CleanStatus(GetInfirmaryStatus(), GetInfirmaryCost()),
                GetInfirmaryCost(),
                CanBuildInfirmary(),
                GetInfirmaryStatus().StartsWith("построен") ? "Построено" : "Построить",
                buildInfirmaryRequested);

            DrawActionCard(
                new Rect(rightX, y, cardWidth, cardHeight),
                "⚒",
                "Кузница",
                "Оружие и броня 2 уровня",
                CleanStatus(GetForgeStatus(), GetForgeCost()),
                GetForgeCost(),
                CanBuildForge(),
                GetForgeStatus().StartsWith("построена") ? "Построено" : "Построить",
                buildForgeRequested);
            y += cardHeight + gap;

            DrawActionCard(
                new Rect(leftX, y, cardWidth, cardHeight),
                "⚖",
                "Рынок",
                "Обмен ресурсов",
                CleanStatus(GetMarketStatus(), GetMarketCost()),
                GetMarketCost(),
                CanBuildMarket(),
                GetMarketStatus().StartsWith("построен") ? "Построено" : "Построить",
                buildMarketRequested);
        }

        private void DrawHeroesTab(Rect rect)
        {
            DrawSection(new Rect(rect.x, rect.y, rect.width, 22f), "Герои");
            var y = rect.y + 30f;
            DrawActionCard(
                new Rect(rect.x, y, rect.width, 96f),
                "🛡",
                "Рыцари",
                "Дома героя и лимит отряда",
                CleanStatus(GetHeroHouseStatus(), GetHeroCost()),
                GetHeroCost(),
                CanCreateHero(),
                "Создать",
                createHeroRequested);
            y += 104f;

            DrawActionCard(
                new Rect(rect.x, y, rect.width, 104f),
                "✦",
                "Часовня",
                "Благословения для вылазок",
                CleanStatus(GetChapelStatus(), GetChapelCost()),
                GetChapelCost(),
                CanBuildChapel(),
                GetChapelStatus().StartsWith("построена") ? "Построено" : "Построить",
                buildChapelRequested);
        }

        private void DrawDungeonTab(Rect rect)
        {
            DrawSection(new Rect(rect.x, rect.y, rect.width, 22f), "Подземелье");
            var y = rect.y + 30f;
            DrawActionCard(
                new Rect(rect.x, y, rect.width, 96f),
                "🗺",
                "Дом картографа",
                "Общая карта рыцарей",
                CleanStatus(GetCartographerHouseStatus(), GetCartographerHouseCost()),
                GetCartographerHouseCost(),
                CanBuildCartographerHouse(),
                GetCartographerHouseStatus().StartsWith("построен") ? "Построено" : "Построить",
                buildCartographerHouseRequested);
            y += 104f;

            DrawActionCard(
                new Rect(rect.x, y, rect.width, 96f),
                "⛏",
                "Гильдия шахтёров",
                "Подготовка шахт",
                CleanStatus(GetMinersGuildStatus(), GetMinersGuildCost()),
                GetMinersGuildCost(),
                CanBuildMinersGuild(),
                GetMinersGuildStatus().StartsWith("построена") ? "Построено" : "Построить",
                buildMinersGuildRequested);
            y += 104f;

            DrawActionCard(
                new Rect(rect.x, y, rect.width, 104f),
                "▣",
                "Шахта",
                "Стройзона в изученной минипещере",
                GetMineStatus(),
                new BuildingCost(0, MineConstructionController.RouteWoodCost),
                CanStartMineSelection(),
                "Выбрать пещеру",
                mineSelectionRequested,
                $"Маршрут: {MineConstructionController.RouteWoodCost} дер./клетка, шахта {MineConstructionController.MineWoodCost} дер.");
        }

        private void DrawUpgradesTab(Rect rect)
        {
            DrawSection(new Rect(rect.x, rect.y, rect.width, 22f), "Улучшения за железо");
            var y = rect.y + 30f;
            const float gap = 8f;
            var cardHeight = Mathf.Min(82f, (rect.height - 30f - gap * 5f) / 6f);

            DrawUpgradeCard(new Rect(rect.x, y, rect.width, cardHeight), "🏰", BuildingUpgradeType.Castle);
            y += cardHeight + gap;
            DrawUpgradeCard(new Rect(rect.x, y, rect.width, cardHeight), "🌾", BuildingUpgradeType.Farm);
            y += cardHeight + gap;
            DrawUpgradeCard(new Rect(rect.x, y, rect.width, cardHeight), "🪓", BuildingUpgradeType.LumberjackCamp);
            y += cardHeight + gap;
            DrawUpgradeCard(new Rect(rect.x, y, rect.width, cardHeight), "⚗", BuildingUpgradeType.AlchemistShop);
            y += cardHeight + gap;
            DrawUpgradeCard(new Rect(rect.x, y, rect.width, cardHeight), "🍖", BuildingUpgradeType.Tavern);
            y += cardHeight + gap;
            DrawUpgradeCard(new Rect(rect.x, y, rect.width, cardHeight), "⚒", BuildingUpgradeType.Forge);
        }

        private void DrawUpgradeCard(Rect rect, string icon, BuildingUpgradeType type)
        {
            var canUpgrade = CanUpgrade(type);
            DrawActionCard(
                rect,
                icon,
                BaseDevelopment.GetUpgradeName(type),
                "Улучшение здания",
                GetUpgradeStatus(type),
                GetUpgradeCost(type),
                canUpgrade,
                canUpgrade ? "Улучшить" : "Недоступно",
                () => upgradeRequested?.Invoke(type));
        }

        private string GetFarmStatus()
        {
            return farmStatusProvider != null ? farmStatusProvider.Invoke() : "0 (+0 пищи/сек)";
        }

        private string GetLumberjackCampStatus()
        {
            return lumberjackCampStatusProvider != null ? lumberjackCampStatusProvider.Invoke() : "0 (+0 дерева/сек)";
        }

        private string GetHeroHouseStatus()
        {
            return heroHouseStatusProvider != null ? heroHouseStatusProvider.Invoke() : "0";
        }

        private string GetAlchemistShopStatus()
        {
            return alchemistShopStatusProvider != null ? alchemistShopStatusProvider.Invoke() : "не построена";
        }

        private string GetTavernStatus()
        {
            return tavernStatusProvider != null ? tavernStatusProvider.Invoke() : "не построена";
        }

        private string GetForgeStatus()
        {
            return forgeStatusProvider != null ? forgeStatusProvider.Invoke() : "не построена";
        }

        private string GetInfirmaryStatus()
        {
            return infirmaryStatusProvider != null ? infirmaryStatusProvider.Invoke() : "не построен";
        }

        private string GetCartographerHouseStatus()
        {
            return cartographerHouseStatusProvider != null ? cartographerHouseStatusProvider.Invoke() : "не построен";
        }

        private string GetChapelStatus()
        {
            return chapelStatusProvider != null ? chapelStatusProvider.Invoke() : "не построена";
        }

        private string GetMinersGuildStatus()
        {
            return minersGuildStatusProvider != null ? minersGuildStatusProvider.Invoke() : "не построена";
        }

        private string GetMarketStatus()
        {
            return marketStatusProvider != null ? marketStatusProvider.Invoke() : "не построен";
        }

        private string GetMineStatus()
        {
            return mineStatusProvider != null ? mineStatusProvider.Invoke() : "нужна Гильдия шахтёров";
        }

        private bool CanBuildFarm()
        {
            return canBuildFarmProvider == null || canBuildFarmProvider.Invoke();
        }

        private BuildingCost GetFarmCost()
        {
            return farmCostProvider != null ? farmCostProvider.Invoke() : BaseDevelopment.FarmCost;
        }

        private bool CanBuildLumberjackCamp()
        {
            return canBuildLumberjackCampProvider == null || canBuildLumberjackCampProvider.Invoke();
        }

        private BuildingCost GetLumberjackCampCost()
        {
            return lumberjackCampCostProvider != null
                ? lumberjackCampCostProvider.Invoke()
                : BaseDevelopment.GetLumberjackCampCost(0);
        }

        private bool CanBuildAlchemistShop()
        {
            return canBuildAlchemistShopProvider == null || canBuildAlchemistShopProvider.Invoke();
        }

        private BuildingCost GetAlchemistShopCost()
        {
            return alchemistShopCostProvider != null ? alchemistShopCostProvider.Invoke() : BaseDevelopment.AlchemistShopCost;
        }

        private bool CanBuildTavern()
        {
            return canBuildTavernProvider == null || canBuildTavernProvider.Invoke();
        }

        private BuildingCost GetTavernCost()
        {
            return tavernCostProvider != null ? tavernCostProvider.Invoke() : BaseDevelopment.TavernCost;
        }

        private bool CanBuildForge()
        {
            return canBuildForgeProvider == null || canBuildForgeProvider.Invoke();
        }

        private BuildingCost GetForgeCost()
        {
            return forgeCostProvider != null ? forgeCostProvider.Invoke() : BaseDevelopment.ForgeCost;
        }

        private bool CanBuildInfirmary()
        {
            return canBuildInfirmaryProvider == null || canBuildInfirmaryProvider.Invoke();
        }

        private BuildingCost GetInfirmaryCost()
        {
            return infirmaryCostProvider != null ? infirmaryCostProvider.Invoke() : BaseDevelopment.InfirmaryCost;
        }

        private bool CanBuildCartographerHouse()
        {
            return canBuildCartographerHouseProvider == null || canBuildCartographerHouseProvider.Invoke();
        }

        private BuildingCost GetCartographerHouseCost()
        {
            return cartographerHouseCostProvider != null
                ? cartographerHouseCostProvider.Invoke()
                : BaseDevelopment.CartographerHouseCost;
        }

        private bool CanBuildChapel()
        {
            return canBuildChapelProvider == null || canBuildChapelProvider.Invoke();
        }

        private BuildingCost GetChapelCost()
        {
            return chapelCostProvider != null ? chapelCostProvider.Invoke() : BaseDevelopment.ChapelCost;
        }

        private bool CanBuildMinersGuild()
        {
            return canBuildMinersGuildProvider == null || canBuildMinersGuildProvider.Invoke();
        }

        private BuildingCost GetMinersGuildCost()
        {
            return minersGuildCostProvider != null
                ? minersGuildCostProvider.Invoke()
                : BaseDevelopment.MinersGuildCost;
        }

        private bool CanBuildMarket()
        {
            return canBuildMarketProvider == null || canBuildMarketProvider.Invoke();
        }

        private BuildingCost GetMarketCost()
        {
            return marketCostProvider != null ? marketCostProvider.Invoke() : BaseDevelopment.MarketCost;
        }

        private bool CanStartMineSelection()
        {
            return canStartMineSelectionProvider == null || canStartMineSelectionProvider.Invoke();
        }

        private bool CanCreateHero()
        {
            return canCreateHeroProvider == null || canCreateHeroProvider.Invoke();
        }

        private BuildingCost GetHeroCost()
        {
            return heroCostProvider != null ? heroCostProvider.Invoke() : BaseDevelopment.HeroCost;
        }

        private string GetUpgradeStatus(BuildingUpgradeType type)
        {
            return upgradeStatusProvider != null ? upgradeStatusProvider.Invoke(type) : "нет данных";
        }

        private bool CanUpgrade(BuildingUpgradeType type)
        {
            return canUpgradeProvider != null && canUpgradeProvider.Invoke(type);
        }

        private BuildingCost GetUpgradeCost(BuildingUpgradeType type)
        {
            return upgradeCostProvider != null ? upgradeCostProvider.Invoke(type) : BaseDevelopment.GetUpgradeCost(type, 1);
        }

        private static void DrawPanel(Rect rect)
        {
            FillRect(rect, new Color(0.11f, 0.105f, 0.1f, 0.94f));
            FillRect(new Rect(rect.x, rect.y, rect.width, 3f), new Color(0.87f, 0.72f, 0.34f, 0.95f));
            DrawOutline(rect, new Color(0f, 0f, 0f, 0.75f));
        }

        private static Rect CalculatePanelRect()
        {
            var panelWidth = Mathf.Min(660f, Screen.width - 36f);
            panelWidth = Mathf.Max(560f, Mathf.Min(760f, Screen.width - 28f));
            var panelHeight = Mathf.Min(690f, Screen.height - 28f);
            return new Rect(18f, Screen.height - panelHeight - 18f, panelWidth, panelHeight);
        }

        private static Vector2 ToGuiPoint(Vector2 screenPosition)
        {
            return new Vector2(screenPosition.x, Screen.height - screenPosition.y);
        }

        private void DrawSection(Rect rect, string text)
        {
            GUI.Label(rect, text, sectionStyle);
            FillRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), new Color(1f, 1f, 1f, 0.1f));
        }

        private void DrawBuildingRow(Rect rect, string label, string value)
        {
            FillRect(rect, new Color(1f, 1f, 1f, 0.05f));
            DrawOutline(rect, new Color(1f, 1f, 1f, 0.08f));
            GUI.Label(new Rect(rect.x + 12f, rect.y, rect.width * 0.34f, rect.height), label, statLabelStyle);
            GUI.Label(new Rect(rect.x + rect.width * 0.34f, rect.y, rect.width * 0.63f - 12f, rect.height), value, statValueStyle);
        }

        private void DrawActionCard(
            Rect rect,
            string icon,
            string title,
            string subtitle,
            string status,
            BuildingCost cost,
            bool available,
            string actionLabel,
            Action action,
            string extraCost = "")
        {
            FillRect(rect, new Color(1f, 1f, 1f, 0.055f));
            DrawOutline(rect, new Color(1f, 1f, 1f, 0.11f));

            var iconRect = new Rect(rect.x + 10f, rect.y + 10f, 46f, 46f);
            FillRect(iconRect, new Color(0f, 0f, 0f, 0.2f));
            DrawOutline(iconRect, new Color(0.9f, 0.72f, 0.34f, 0.45f));
            GUI.Label(iconRect, icon, cardIconStyle);

            var textX = rect.x + 68f;
            var buttonWidth = Mathf.Min(150f, Mathf.Max(104f, rect.width * 0.3f));
            var textWidth = rect.width - buttonWidth - 92f;
            GUI.Label(new Rect(textX, rect.y + 8f, textWidth, 22f), title, cardTitleStyle);
            GUI.Label(new Rect(textX, rect.y + 29f, textWidth, 18f), subtitle, cardSubtitleStyle);
            GUI.Label(new Rect(textX, rect.y + 49f, textWidth, rect.height - 54f), status, cardStatusStyle);

            var costText = string.IsNullOrEmpty(extraCost)
                ? $"Цена: {cost.Format()}"
                : $"Цена: {cost.Format()}, {extraCost}";
            var costAvailable = available || actionLabel == "Построено";
            GUI.Label(
                new Rect(rect.x + rect.width - buttonWidth - 12f, rect.y + 8f, buttonWidth, 30f),
                costText,
                costAvailable ? costStyle : unavailableCostStyle);

            GUI.enabled = available;
            if (GUI.Button(new Rect(rect.x + rect.width - buttonWidth - 12f, rect.y + 42f, buttonWidth, 34f), actionLabel, commandButtonStyle))
            {
                GameAudioController.PlayUi(actionLabel == "Построено" ? GameSfx.HudClick : GameSfx.HudConfirm);
                action?.Invoke();
            }

            GUI.enabled = true;
        }

        private static string CleanStatus(string status, BuildingCost cost)
        {
            if (string.IsNullOrEmpty(status))
            {
                return string.Empty;
            }

            var costText = cost.Format();
            var cleaned = status
                .Replace($", постройка {costText}", string.Empty)
                .Replace($"постройка {costText}, ", string.Empty)
                .Replace($"постройка {costText}", string.Empty)
                .Replace(", )", ")")
                .Replace("(,", "(")
                .Replace("  ", " ");

            return cleaned.Trim(' ', ',');
        }

        private static void DrawOutline(Rect rect, Color color)
        {
            FillRect(new Rect(rect.x, rect.y, rect.width, 1f), color);
            FillRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), color);
            FillRect(new Rect(rect.x, rect.y, 1f, rect.height), color);
            FillRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), color);
        }

        private static void FillRect(Rect rect, Color color)
        {
            var previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previousColor;
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 27,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            titleStyle.normal.textColor = new Color(0.96f, 0.93f, 0.86f);
            subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 13,
                fontStyle = FontStyle.Italic
            };
            subtitleStyle.normal.textColor = new Color(0.72f, 0.7f, 0.64f);
            tabStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 15,
                fontStyle = FontStyle.Bold
            };
            tabStyle.normal.textColor = new Color(0.78f, 0.76f, 0.69f);
            tabStyle.hover.textColor = new Color(1f, 0.88f, 0.42f);
            activeTabStyle = new GUIStyle(tabStyle);
            activeTabStyle.normal.textColor = new Color(1f, 0.86f, 0.36f);
            activeTabStyle.hover.textColor = new Color(1f, 0.9f, 0.48f);
            sectionStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 15,
                fontStyle = FontStyle.Bold
            };
            sectionStyle.normal.textColor = new Color(0.88f, 0.76f, 0.47f);
            statLabelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 15,
                fontStyle = FontStyle.Bold
            };
            statLabelStyle.normal.textColor = new Color(0.73f, 0.72f, 0.68f);
            statValueStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleRight,
                fontSize = 15,
                fontStyle = FontStyle.Bold
            };
            statValueStyle.normal.textColor = Color.white;
            cardTitleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 17,
                fontStyle = FontStyle.Bold
            };
            cardTitleStyle.normal.textColor = new Color(0.95f, 0.92f, 0.84f);
            cardIconStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 23,
                fontStyle = FontStyle.Bold
            };
            cardIconStyle.normal.textColor = new Color(0.95f, 0.92f, 0.84f);
            cardSubtitleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 12,
                fontStyle = FontStyle.Italic
            };
            cardSubtitleStyle.normal.textColor = new Color(0.74f, 0.72f, 0.65f);
            cardStatusStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };
            cardStatusStyle.normal.textColor = new Color(0.88f, 0.87f, 0.82f);
            costStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleRight,
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };
            costStyle.normal.textColor = new Color(0.83f, 0.94f, 0.66f);
            unavailableCostStyle = new GUIStyle(costStyle);
            unavailableCostStyle.normal.textColor = new Color(0.94f, 0.55f, 0.48f);
            commandButtonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
                fontStyle = FontStyle.Bold
            };
            commandButtonStyle.normal.textColor = Color.white;
            commandButtonStyle.hover.textColor = new Color(1f, 0.88f, 0.42f);
            closeButtonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 15,
                fontStyle = FontStyle.Bold
            };
            closeButtonStyle.normal.textColor = new Color(0.92f, 0.92f, 0.88f);
        }
    }
}
