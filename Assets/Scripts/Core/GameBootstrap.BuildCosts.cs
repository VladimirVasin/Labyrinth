namespace Labyrinth.Core
{
    public sealed partial class GameBootstrap
    {
        private BuildingCost GetFarmCost()
        {
            return BaseDevelopment.FarmCost;
        }

        private BuildingCost GetLumberjackCampCost()
        {
            return BaseDevelopment.GetLumberjackCampCost(baseDevelopment.LumberjackCampCount);
        }

        private BuildingCost GetAlchemistShopCost()
        {
            return BaseDevelopment.AlchemistShopCost;
        }

        private BuildingCost GetTavernCost()
        {
            return BaseDevelopment.TavernCost;
        }

        private BuildingCost GetForgeCost()
        {
            return BaseDevelopment.ForgeCost;
        }

        private BuildingCost GetInfirmaryCost()
        {
            return BaseDevelopment.InfirmaryCost;
        }

        private BuildingCost GetCartographerHouseCost()
        {
            return BaseDevelopment.CartographerHouseCost;
        }

        private BuildingCost GetChapelCost()
        {
            return BaseDevelopment.ChapelCost;
        }

        private BuildingCost GetMinersGuildCost()
        {
            return BaseDevelopment.MinersGuildCost;
        }

        private BuildingCost GetAntiquaryCost()
        {
            return BaseDevelopment.AntiquaryCost;
        }

        private BuildingCost GetHeroCost()
        {
            return BaseDevelopment.HeroCost;
        }
    }
}
