using System.Collections.Generic;
using Labyrinth.Combat;
using Labyrinth.Hero;
using Labyrinth.Maze;
using UnityEngine;

namespace Labyrinth.Core
{
    public sealed partial class GameBootstrap
    {
        private void SyncHeroKnowledgeAtEntrance(HeroModel heroModel, int heroNumber)
        {
            if (heroModel == null)
            {
                return;
            }

            if (cartographerMemory != null && baseDevelopment != null && baseDevelopment.HasCartographerHouse)
            {
                ForgetOpenedDoors(cartographerMemory);
                ForgetOpenedDoors(heroModel.Memory);
                var mercyAdded = ApplyCartographerMercy(heroModel);
                var paidWalkableCells = CountNewCommonWalkableCells(heroModel.Memory);
                var uploaded = cartographerMemory.MergeFrom(heroModel.Memory);
                var mapGoldReward = heroModel.RewardCommonMapContribution(paidWalkableCells);
                ForgetOpenedDoors(cartographerMemory);
                var downloaded = heroModel.Memory.MergeFrom(cartographerMemory);
                sharedHeroMemoryView?.ShowMemory(cartographerMemory);
                ShowCartographerPayment(heroModel, mapGoldReward);

                GameDebugLog.Info(
                    "Cartographer",
                    $"Hero #{heroNumber} synced map at entrance: mercyAdded={mercyAdded}, uploaded={uploaded}, paidWalkable={paidWalkableCells}, mapGoldReward={mapGoldReward}, heroGold={heroModel.Gold}, downloaded={downloaded}, commonCells={cartographerMemory.KnownCellCount}, commonWalls={cartographerMemory.RememberedWallCount}, commonClosedDoors={cartographerMemory.KnownClosedDoorCount}.");
            }

            TryContributeHeroHouseFundAtEntrance(heroModel, heroNumber);
        }

        private int CountNewCommonWalkableCells(HeroMemory source)
        {
            if (source == null || cartographerMemory == null)
            {
                return 0;
            }

            var count = 0;
            foreach (var position in source.RememberedCells)
            {
                if (!cartographerMemory.IsRemembered(position))
                {
                    count++;
                }
            }

            return count;
        }

        private void ShowCartographerPayment(HeroModel heroModel, int reward)
        {
            if (heroModel == null || reward <= 0 || mazeRenderer == null)
            {
                return;
            }

            DamageNumberView.CreateText(
                mazeRenderer,
                heroModel.Position,
                $"+{reward} зол. карты",
                new Color(1f, 0.84f, 0.26f),
                2.35f);
        }

        private int ApplyCartographerMercy(HeroModel heroModel)
        {
            if (heroModel == null
                || !heroModel.HasBlessing(HeroBlessingType.CartographerMercy)
                || currentMaze == null
                || currentMaze.Grid == null)
            {
                return 0;
            }

            var candidates = new HashSet<Vector2Int>();
            foreach (var remembered in heroModel.Memory.RememberedCells)
            {
                for (var x = -1; x <= 1; x++)
                {
                    for (var y = -1; y <= 1; y++)
                    {
                        if (x == 0 && y == 0)
                        {
                            continue;
                        }

                        candidates.Add(remembered + new Vector2Int(x, y));
                    }
                }
            }

            var added = 0;
            foreach (var candidate in candidates)
            {
                if (!currentMaze.Grid.InBounds(candidate))
                {
                    continue;
                }

                var cell = currentMaze.Grid.Get(candidate);
                if (cell.IsWalkable)
                {
                    if (heroModel.Memory.Remember(candidate))
                    {
                        added++;
                    }
                }
                else if (cell.Type == MazeCellType.Wall)
                {
                    if (heroModel.Memory.RememberWall(candidate))
                    {
                        added++;
                    }
                }
                else if (cell.Type == MazeCellType.ClosedDoor || cell.Type == MazeCellType.LockedDownStairs)
                {
                    if (heroModel.Memory.RememberClosedDoor(candidate))
                    {
                        added++;
                    }
                }
            }

            return added;
        }

        private void ForgetOpenedDoors(HeroMemory memory)
        {
            if (memory == null || currentMaze == null || currentMaze.CentralDoors == null)
            {
                return;
            }

            foreach (var door in currentMaze.CentralDoors)
            {
                if (door != null && door.IsOpen)
                {
                    memory.ForgetClosedDoor(door.Position);
                }
            }

            if (currentMaze.DownStairs != null && currentMaze.DownStairs.IsOpen)
            {
                memory.ForgetClosedDoor(currentMaze.DownStairs.Position);
            }
        }
    }
}
