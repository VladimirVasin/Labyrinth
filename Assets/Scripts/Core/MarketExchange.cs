using UnityEngine;

namespace Labyrinth.Core
{
    public enum MarketResourceKind
    {
        Food,
        Wood,
        Iron
    }

    public enum MarketTradeDirection
    {
        Buy,
        Sell
    }

    public readonly struct MarketTradeQuote
    {
        public MarketTradeQuote(
            MarketResourceKind resource,
            MarketTradeDirection direction,
            int amount,
            int gold,
            int stock)
        {
            Resource = resource;
            Direction = direction;
            Amount = amount;
            Gold = gold;
            Stock = stock;
        }

        public MarketResourceKind Resource { get; }

        public MarketTradeDirection Direction { get; }

        public int Amount { get; }

        public int Gold { get; }

        public int Stock { get; }
    }

    public static class MarketExchange
    {
        public static MarketTradeQuote GetQuote(ResourceWallet wallet, MarketResourceKind resource, MarketTradeDirection direction)
        {
            var stock = GetStock(wallet, resource);
            var amount = GetBatchSize(resource);
            var buyPrice = CalculateBuyPrice(resource, stock);
            var gold = direction == MarketTradeDirection.Buy
                ? buyPrice
                : Mathf.Max(GetMinimumSellPrice(resource), Mathf.RoundToInt(buyPrice * 0.55f));
            return new MarketTradeQuote(resource, direction, amount, gold, stock);
        }

        public static int GetStock(ResourceWallet wallet, MarketResourceKind resource)
        {
            if (wallet == null)
            {
                return 0;
            }

            switch (resource)
            {
                case MarketResourceKind.Wood:
                    return wallet.Wood;
                case MarketResourceKind.Iron:
                    return wallet.Iron;
                case MarketResourceKind.Food:
                default:
                    return wallet.Food;
            }
        }

        public static int GetBatchSize(MarketResourceKind resource)
        {
            return resource == MarketResourceKind.Iron ? 5 : 10;
        }

        public static string GetDisplayName(MarketResourceKind resource)
        {
            switch (resource)
            {
                case MarketResourceKind.Wood:
                    return "дерева";
                case MarketResourceKind.Iron:
                    return "железа";
                case MarketResourceKind.Food:
                default:
                    return "пищи";
            }
        }

        private static int CalculateBuyPrice(MarketResourceKind resource, int stock)
        {
            var basePrice = GetBaseBuyPrice(resource);
            return Mathf.Max(1, Mathf.RoundToInt(basePrice * GetStockMultiplier(stock)));
        }

        private static int GetBaseBuyPrice(MarketResourceKind resource)
        {
            switch (resource)
            {
                case MarketResourceKind.Wood:
                    return 30;
                case MarketResourceKind.Iron:
                    return 70;
                case MarketResourceKind.Food:
                default:
                    return 25;
            }
        }

        private static int GetMinimumSellPrice(MarketResourceKind resource)
        {
            switch (resource)
            {
                case MarketResourceKind.Wood:
                    return 10;
                case MarketResourceKind.Iron:
                    return 25;
                case MarketResourceKind.Food:
                default:
                    return 8;
            }
        }

        private static float GetStockMultiplier(int stock)
        {
            if (stock < 10)
            {
                return 2f;
            }

            if (stock < 25)
            {
                return 1.6f;
            }

            if (stock < 50)
            {
                return 1.25f;
            }

            return stock >= 100 ? 0.85f : 1f;
        }
    }
}
