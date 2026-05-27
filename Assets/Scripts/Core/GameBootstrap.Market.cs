using Labyrinth.Base;
using Labyrinth.Combat;
using Labyrinth.Maze;
using Labyrinth.UI;
using UnityEngine;

namespace Labyrinth.Core
{
    public sealed partial class GameBootstrap
    {
        private void BuildMarketFromBase()
        {
            if (currentMaze == null)
            {
                return;
            }

            var cost = GetMarketCost();
            if (!resources.CanAfford(cost))
            {
                baseDevelopment.ReportBuildBlocked($"рынок: нужно {cost.Format()}");
                GameDebugLog.Warning("Base", $"Market build blocked: gold={resources.Gold}, wood={resources.Wood}, required={cost.Format()}");
                return;
            }

            if (!baseDevelopment.TryBuildMarket(currentMaze, out var marketPosition))
            {
                var blockMessage = baseDevelopment.LastBuildMessage;
                baseDevelopment.ReportBuildBlocked($"рынок: {blockMessage}");
                GameDebugLog.Warning("Base", $"Market build blocked: {blockMessage}");
                return;
            }

            if (!resources.TrySpend(cost))
            {
                baseDevelopment.ReportBuildBlocked($"рынок: нужно {cost.Format()}");
                return;
            }

            ClearTerrainDecorationsAround(marketPosition, BaseDevelopment.MarketFootprintRadiusCells);
            MarketRenderer.Render(mazeRenderer, marketPosition);
            baseAmbience.RegisterBuilding(BuildingType.Market, marketPosition);
            cityAmbience.RegisterBuilding(BuildingType.Market, marketPosition);
            SyncPeasantHuts();
            RefreshSelectedHeroVisibility();
            GameAudioController.Play(GameSfx.Build, mazeRenderer.GridToWorld(marketPosition));
            GameDebugLog.Info(
                "Base",
                $"Market built at {GameDebugLog.Position(marketPosition)}. buildCost={cost.Format()}, goldLeft={resources.Gold}, woodLeft={resources.Wood}.");
        }

        private bool CanBuildMarket()
        {
            return currentMaze != null
                && !baseDevelopment.HasMarket
                && resources.CanAfford(GetMarketCost());
        }

        private BuildingCost GetMarketCost()
        {
            return BaseDevelopment.MarketCost;
        }

        private string GetMarketStatus()
        {
            var foodBuy = MarketExchange.GetQuote(resources, MarketResourceKind.Food, MarketTradeDirection.Buy);
            var woodBuy = MarketExchange.GetQuote(resources, MarketResourceKind.Wood, MarketTradeDirection.Buy);
            var ironBuy = MarketExchange.GetQuote(resources, MarketResourceKind.Iron, MarketTradeDirection.Buy);
            var status = baseDevelopment.HasMarket
                ? $"построен ({baseDevelopment.MarketPosition.x}, {baseDevelopment.MarketPosition.y}), покупка: пища {foodBuy.Gold}, дерево {woodBuy.Gold}, железо {ironBuy.Gold} зол."
                : $"не построен, постройка {GetMarketCost().Format()}, обмен ресурсов";
            if (baseDevelopment.LastBuildMessage.Contains("рынок"))
            {
                status += $", {baseDevelopment.LastBuildMessage}";
            }

            return status;
        }

        private BuildingServiceEntry[] GetBuildingMicroHudServices(BuildingType buildingType, int buildingLevel)
        {
            if (buildingType == BuildingType.Market)
            {
                return GetMarketServiceEntries();
            }

            return BuildingServiceCatalog.Get(buildingType, buildingLevel);
        }

        private void HandleBuildingMicroHudServiceAction(BuildingType buildingType, int serviceIndex)
        {
            if (buildingType != BuildingType.Market)
            {
                return;
            }

            switch (serviceIndex)
            {
                case 0:
                    TryBuyMarketResource(MarketResourceKind.Food);
                    break;
                case 1:
                    TrySellMarketResource(MarketResourceKind.Food);
                    break;
                case 2:
                    TryBuyMarketResource(MarketResourceKind.Wood);
                    break;
                case 3:
                    TrySellMarketResource(MarketResourceKind.Wood);
                    break;
                case 4:
                    TryBuyMarketResource(MarketResourceKind.Iron);
                    break;
                case 5:
                    TrySellMarketResource(MarketResourceKind.Iron);
                    break;
            }
        }

        private BuildingServiceEntry[] GetMarketServiceEntries()
        {
            return new[]
            {
                CreateMarketServiceEntry(MarketResourceKind.Food, MarketTradeDirection.Buy),
                CreateMarketServiceEntry(MarketResourceKind.Food, MarketTradeDirection.Sell),
                CreateMarketServiceEntry(MarketResourceKind.Wood, MarketTradeDirection.Buy),
                CreateMarketServiceEntry(MarketResourceKind.Wood, MarketTradeDirection.Sell),
                CreateMarketServiceEntry(MarketResourceKind.Iron, MarketTradeDirection.Buy),
                CreateMarketServiceEntry(MarketResourceKind.Iron, MarketTradeDirection.Sell)
            };
        }

        private BuildingServiceEntry CreateMarketServiceEntry(MarketResourceKind resource, MarketTradeDirection direction)
        {
            var quote = MarketExchange.GetQuote(resources, resource, direction);
            var resourceName = GetMarketResourceTitle(resource);
            var stockName = MarketExchange.GetDisplayName(resource);
            if (direction == MarketTradeDirection.Buy)
            {
                return new BuildingServiceEntry(
                    $"Купить {resourceName}",
                    $"{quote.Amount} за {quote.Gold} зол.",
                    $"Покупает {quote.Amount} {stockName} за золото казны. Текущий запас: {quote.Stock}; чем он ниже, тем дороже покупка.",
                    string.Empty,
                    "Купить",
                    resources.CanSpendGold(quote.Gold));
            }

            return new BuildingServiceEntry(
                $"Продать {resourceName}",
                $"{quote.Amount} -> {quote.Gold} зол.",
                $"Продаёт {quote.Amount} {stockName} из казны и добавляет золото. Текущий запас: {quote.Stock}; редкий ресурс ценится дороже.",
                string.Empty,
                "Продать",
                MarketExchange.GetStock(resources, resource) >= quote.Amount);
        }

        private void TryBuyMarketResource(MarketResourceKind resource)
        {
            if (!baseDevelopment.HasMarket)
            {
                return;
            }

            var quote = MarketExchange.GetQuote(resources, resource, MarketTradeDirection.Buy);
            if (!resources.TrySpendGold(quote.Gold))
            {
                GameAudioController.PlayUi(GameSfx.HudBlocked);
                GameDebugLog.Warning("Market", $"Buy blocked: resource={resource}, price={quote.Gold}, gold={resources.Gold}.");
                return;
            }

            AddMarketResource(resource, quote.Amount);
            ShowMarketTradeFeedback($"+{quote.Amount} {MarketExchange.GetDisplayName(resource)}", new Color(0.78f, 1f, 0.45f));
            GameDebugLog.Info("Market", $"Bought {quote.Amount} {resource} for {quote.Gold} gold. stockNow={MarketExchange.GetStock(resources, resource)}, goldLeft={resources.Gold}.");
        }

        private void TrySellMarketResource(MarketResourceKind resource)
        {
            if (!baseDevelopment.HasMarket)
            {
                return;
            }

            var quote = MarketExchange.GetQuote(resources, resource, MarketTradeDirection.Sell);
            if (!TrySpendMarketResource(resource, quote.Amount))
            {
                GameAudioController.PlayUi(GameSfx.HudBlocked);
                GameDebugLog.Warning("Market", $"Sell blocked: resource={resource}, amount={quote.Amount}, stock={MarketExchange.GetStock(resources, resource)}.");
                return;
            }

            resources.AddGold(quote.Gold);
            ShowMarketTradeFeedback($"+{quote.Gold} зол.", new Color(1f, 0.84f, 0.28f));
            GameDebugLog.Info("Market", $"Sold {quote.Amount} {resource} for {quote.Gold} gold. stockNow={MarketExchange.GetStock(resources, resource)}, goldNow={resources.Gold}.");
        }

        private void AddMarketResource(MarketResourceKind resource, int amount)
        {
            switch (resource)
            {
                case MarketResourceKind.Wood:
                    resources.AddWood(amount);
                    break;
                case MarketResourceKind.Iron:
                    resources.AddIron(amount);
                    break;
                case MarketResourceKind.Food:
                default:
                    resources.AddFood(amount);
                    break;
            }
        }

        private bool TrySpendMarketResource(MarketResourceKind resource, int amount)
        {
            switch (resource)
            {
                case MarketResourceKind.Wood:
                    return resources.TrySpendWood(amount);
                case MarketResourceKind.Iron:
                    return resources.TrySpendIron(amount);
                case MarketResourceKind.Food:
                default:
                    return resources.TrySpendFood(amount);
            }
        }

        private void ShowMarketTradeFeedback(string text, Color color)
        {
            var position = baseDevelopment.MarketPosition;
            DamageNumberView.CreateText(mazeRenderer, position, text, color, 3.8f);
            GameAudioController.Play(GameSfx.Purchase, mazeRenderer.GridToWorld(position), 0.9f);
        }

        private static string GetMarketResourceTitle(MarketResourceKind resource)
        {
            switch (resource)
            {
                case MarketResourceKind.Wood:
                    return "дерево";
                case MarketResourceKind.Iron:
                    return "железо";
                case MarketResourceKind.Food:
                default:
                    return "пищу";
            }
        }
    }
}
