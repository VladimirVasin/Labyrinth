namespace Labyrinth.Core
{
    public sealed partial class GameBootstrap
    {
        private static readonly BuildingCost DebugFreeBuildingCost = new BuildingCost(0, 0, 0, 0);

        private BuildingCost GetFarmCost()
        {
            return GetDebugAdjustedBuildingCost(BaseDevelopment.FarmCost);
        }

        private BuildingCost GetLumberjackCampCost()
        {
            return GetDebugAdjustedBuildingCost(BaseDevelopment.GetLumberjackCampCost(baseDevelopment.LumberjackCampCount));
        }

        private BuildingCost GetAlchemistShopCost()
        {
            return GetDebugAdjustedBuildingCost(BaseDevelopment.AlchemistShopCost);
        }

        private BuildingCost GetTavernCost()
        {
            return GetDebugAdjustedBuildingCost(BaseDevelopment.TavernCost);
        }

        private BuildingCost GetForgeCost()
        {
            return GetDebugAdjustedBuildingCost(BaseDevelopment.ForgeCost);
        }

        private BuildingCost GetInfirmaryCost()
        {
            return GetDebugAdjustedBuildingCost(BaseDevelopment.InfirmaryCost);
        }

        private BuildingCost GetCartographerHouseCost()
        {
            return GetDebugAdjustedBuildingCost(BaseDevelopment.CartographerHouseCost);
        }

        private BuildingCost GetChapelCost()
        {
            return GetDebugAdjustedBuildingCost(BaseDevelopment.ChapelCost);
        }

        private BuildingCost GetMinersGuildCost()
        {
            return GetDebugAdjustedBuildingCost(BaseDevelopment.MinersGuildCost);
        }

        private BuildingCost GetAntiquaryCost()
        {
            return GetDebugAdjustedBuildingCost(BaseDevelopment.AntiquaryCost);
        }

        private BuildingCost GetHeroesGuildCost()
        {
            return GetDebugAdjustedBuildingCost(BaseDevelopment.HeroesGuildCost);
        }

        private BuildingCost GetHeroCost()
        {
            return GetDebugAdjustedBuildingCost(BaseDevelopment.HeroCost);
        }

        private BuildingCost GetDebugAdjustedBuildingCost(BuildingCost normalCost)
        {
            return debugBuildingMode ? DebugFreeBuildingCost : normalCost;
        }
    }
}
