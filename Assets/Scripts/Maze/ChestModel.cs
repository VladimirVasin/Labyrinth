using System.Collections.Generic;
using Labyrinth.Hero;
using UnityEngine;

namespace Labyrinth.Maze
{
    public enum ChestRewardType
    {
        Gold,
        WeaponTier2,
        ArmorTier2
    }

    public sealed class ChestModel
    {
        private const int CaveRadius = 1;

        private ChestView view;

        public ChestModel(CaveInfo cave, ChestRewardType rewardType, int rewardGold)
        {
            Cave = cave;
            Position = cave.Center;
            RewardType = rewardType;
            RewardGold = rewardType == ChestRewardType.Gold ? rewardGold : 0;
            RewardItemName = BuildRewardItemName(rewardType);
        }

        public CaveInfo Cave { get; }

        public Vector2Int Position { get; }

        public ChestRewardType RewardType { get; }

        public int RewardGold { get; }

        public string RewardItemName { get; }

        public bool IsOpened { get; private set; }

        public bool Contains(Vector2Int position)
        {
            return Mathf.Abs(position.x - Cave.Center.x) <= CaveRadius
                && Mathf.Abs(position.y - Cave.Center.y) <= CaveRadius;
        }

        public IEnumerable<Vector2Int> CaveCells()
        {
            for (var x = Cave.Center.x - CaveRadius; x <= Cave.Center.x + CaveRadius; x++)
            {
                for (var y = Cave.Center.y - CaveRadius; y <= Cave.Center.y + CaveRadius; y++)
                {
                    yield return new Vector2Int(x, y);
                }
            }
        }

        public void AttachView(ChestView chestView)
        {
            view = chestView;
            if (IsOpened)
            {
                view.ShowOpenedImmediate();
            }
        }

        public bool Open()
        {
            if (IsOpened)
            {
                return false;
            }

            IsOpened = true;
            if (view != null)
            {
                view.PlayOpen();
            }

            return true;
        }

        private static string BuildRewardItemName(ChestRewardType rewardType)
        {
            switch (rewardType)
            {
                case ChestRewardType.WeaponTier2:
                    return HeroInventory.SteelSwordItemName;
                case ChestRewardType.ArmorTier2:
                    return HeroInventory.ChainmailItemName;
                case ChestRewardType.Gold:
                default:
                    return string.Empty;
            }
        }
    }
}
