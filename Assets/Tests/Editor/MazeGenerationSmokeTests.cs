using Labyrinth.Core;
using Labyrinth.Maze;
using NUnit.Framework;

namespace Labyrinth.Tests
{
    public sealed class MazeGenerationSmokeTests
    {
        [Test]
        public void RegressionSeedFromLog_GeneratesValidMaze()
        {
            AssertValidMaze(101, 25, 634146015);
        }

        [TestCase(21, 15, 20260529)]
        [TestCase(25, 25, 31415926)]
        [TestCase(41, 41, 27182818)]
        [TestCase(101, 25, 634146016)]
        [TestCase(101, 51, 1700000001)]
        [TestCase(101, 25, 705257437)]
        public void CustomMapsKeepEarlyAlternativeRoutes(int width, int height, int seed)
        {
            AssertValidMaze(width, height, seed);
        }

        private static void AssertValidMaze(int width, int height, int seed)
        {
            var settings = MazeGenerationSettings.CreateCustom(width, height, seed);
            var result = new MazeGenerator().Generate(settings);

            Assert.That(MazeValidation.ValidateGeneratedMaze(result, out var error), Is.True, error);
            Assert.That(result.BossCave.IsValid, Is.True);
            Assert.That(result.CentralRoom.IsBeyondExitSide(result.BossCave.Center), Is.True);
            Assert.That(result.DownStairs.Position, Is.Not.EqualTo(result.BossCave.Center));
            foreach (var chest in result.Chests)
            {
                Assert.That(chest.Position, Is.Not.EqualTo(result.BossCave.Center));
            }

            foreach (var deposit in result.OreDeposits)
            {
                Assert.That(deposit.Cave.Center, Is.Not.EqualTo(result.BossCave.Center));
            }
        }
    }
}
