using System;
using System.Collections.Generic;

namespace Labyrinth.Hero
{
    public enum HeroInventorySlotType
    {
        Weapon,
        Armor,
        Footwear,
        Potion,
        Ration,
        Artifact,
        Empty
    }

    public readonly struct HeroInventorySlot
    {
        public HeroInventorySlot(
            HeroInventorySlotType type,
            string label,
            string itemName,
            string hoverInfo = "",
            int count = 0,
            int tier = 0,
            int attackBonus = 0,
            int armorBonus = 0,
            int moveSpeedBonusPercent = 0)
        {
            Type = type;
            Label = label;
            ItemName = itemName;
            HoverInfo = hoverInfo;
            Count = string.IsNullOrEmpty(itemName) ? 0 : Math.Max(1, count);
            Tier = string.IsNullOrEmpty(itemName) ? 0 : tier;
            AttackBonus = attackBonus;
            ArmorBonus = armorBonus;
            MoveSpeedBonusPercent = moveSpeedBonusPercent;
        }

        public HeroInventorySlotType Type { get; }

        public string Label { get; }

        public string ItemName { get; }

        public string HoverInfo { get; }

        public int Count { get; }

        public int Tier { get; }

        public int AttackBonus { get; }

        public int ArmorBonus { get; }

        public int MoveSpeedBonusPercent { get; }

        public bool HasItem => !string.IsNullOrEmpty(ItemName);

        public string DisplayItem
        {
            get
            {
                if (!HasItem)
                {
                    return "пусто";
                }

                return Type == HeroInventorySlotType.Potion || Type == HeroInventorySlotType.Ration
                    ? $"{ItemName} x{Count}"
                    : ItemName;
            }
        }
    }

    public sealed class HeroInventory
    {
        public const string CentralRoomKeyItemName = "Ключ";
        public const string DescentKeyItemName = "Ключ спуска";
        public const string DescentKeyHoverInfo = "Открывает спуск на следующий уровень подземелья";
        public const string RustySwordItemName = "Ржавый меч";
        public const string CommonClothesItemName = "Обычная одежда";
        public const string SandalsItemName = "Сандалии";
        public const string SteelSwordItemName = "Стальной меч";
        public const string ChainmailItemName = "Кольчуга";
        public const string KnightSwordItemName = "Рыцарский меч";
        public const string BrigandineItemName = "Бригантина";
        public const string MasterBladeItemName = "Мастерский клинок";
        public const string PlateHarnessItemName = "Латный доспех";
        public const string LeatherBootsItemName = "Кожаные сапоги";
        public const string PathfinderBootsItemName = "Сапоги следопыта";
        public const string SwiftwalkerBootsItemName = "Сапоги-скороходы";
        public const string TemperedSwordItemName = MasterBladeItemName;
        public const string PlateArmorItemName = PlateHarnessItemName;
        public const string HealthPotionItemName = "Зелье здоровья";
        public const string HealthPotionHoverInfo = "+5 HP при использовании";
        public const string RationItemName = "Паёк";
        public const string RationHoverInfo = "+10 выносливости при использовании";
        public const string ReturnStoneItemName = "Камень возвращения";
        public const string ReturnStoneHoverInfo = "Одноразово переносит рыцаря ко входу, когда он возвращается из подземелья";
        public const string GoldIngotItemName = "Золотой слиток";
        public const string GoldIngotHoverInfo = "Доставить к входу: +20 зол. в казну, +5 XP";
        public const string DeathTokenItemPrefix = "Жетон #";
        public const string DeathTokenHoverInfo = "Вернуть к входу: жетон прикрепится к дому погибшего рыцаря, +10 XP";
        public const int SteelSwordAttackBonus = 3;
        public const int ChainmailArmorBonus = 2;
        public const int KnightSwordAttackBonus = 5;
        public const int BrigandineArmorBonus = 3;
        public const int TemperedSwordAttackBonus = 6;
        public const int PlateArmorBonus = 4;
        public const int MasterBladeAttackBonus = TemperedSwordAttackBonus;
        public const int PlateHarnessArmorBonus = PlateArmorBonus;
        public const int LeatherBootsMoveSpeedBonusPercent = 10;
        public const int PathfinderBootsMoveSpeedBonusPercent = 20;
        public const int SwiftwalkerBootsMoveSpeedBonusPercent = 30;
        public const int HealthPotionHealAmount = 5;
        public const int MaxHealthPotionCount = 3;
        public const int RationStaminaRestoreAmount = 10;
        public const int MaxRationCount = 3;

        private readonly HeroInventorySlot[] slots;

        private HeroInventory(HeroInventorySlot[] inventorySlots)
        {
            slots = inventorySlots;
        }

        public IReadOnlyList<HeroInventorySlot> Slots => slots;

        public int HealthPotionCount => GetStackCount(HeroInventorySlotType.Potion, HealthPotionItemName);

        public int RationCount => GetStackCount(HeroInventorySlotType.Ration, RationItemName);

        public bool CanAddHealthPotion => HealthPotionCount < MaxHealthPotionCount;

        public bool CanAddRation => RationCount < MaxRationCount;

        public bool HasReturnStone => HasItem(ReturnStoneItemName);

        public bool CanAddReturnStone => !HasReturnStone && IsSlotEmpty(HeroInventorySlotType.Artifact);

        public int AttackBonus => GetEquipmentBonus(HeroInventorySlotType.Weapon, true);

        public int ArmorBonus => GetEquipmentBonus(HeroInventorySlotType.Armor, false);

        public int MoveSpeedBonusPercent => GetEquipmentMoveSpeedBonusPercent(HeroInventorySlotType.Footwear);

        public bool CanEquipSteelSword => GetEquipmentTier(HeroInventorySlotType.Weapon) < 2;

        public bool CanEquipChainmail => GetEquipmentTier(HeroInventorySlotType.Armor) < 2;

        public bool CanEquipLeatherBoots => GetEquipmentTier(HeroInventorySlotType.Footwear) < 2;

        public bool CanEquipKnightSword => GetEquipmentTier(HeroInventorySlotType.Weapon) < 3;

        public bool CanEquipBrigandine => GetEquipmentTier(HeroInventorySlotType.Armor) < 3;

        public bool CanEquipPathfinderBoots => GetEquipmentTier(HeroInventorySlotType.Footwear) < 3;

        public bool CanEquipTemperedSword => CanEquipMasterBlade;

        public bool CanEquipPlateArmor => CanEquipPlateHarness;

        public bool CanEquipMasterBlade => GetEquipmentTier(HeroInventorySlotType.Weapon) < 4;

        public bool CanEquipPlateHarness => GetEquipmentTier(HeroInventorySlotType.Armor) < 4;

        public bool CanEquipSwiftwalkerBoots => GetEquipmentTier(HeroInventorySlotType.Footwear) < 4;

        public bool HasGoldIngot => HasItem(GoldIngotItemName);

        public bool HasDeathToken => TryGetDeathTokenItemName(out _);

        public static string BuildDeathTokenItemName(int tokenId)
        {
            return $"{DeathTokenItemPrefix}{Math.Max(0, tokenId)}";
        }

        public static bool IsDeathTokenItem(string itemName)
        {
            return !string.IsNullOrEmpty(itemName)
                && itemName.StartsWith(DeathTokenItemPrefix, StringComparison.Ordinal);
        }

        public static HeroInventory CreateDefault()
        {
            return new HeroInventory(new[]
            {
                new HeroInventorySlot(HeroInventorySlotType.Weapon, "Оружие", RustySwordItemName, "+0 к атаке", 1, 1),
                new HeroInventorySlot(HeroInventorySlotType.Armor, "Броня", CommonClothesItemName, "0% снижения урона", 1, 1),
                new HeroInventorySlot(HeroInventorySlotType.Footwear, "Обувь", SandalsItemName, "0% буста к скорости", 1, 1),
                new HeroInventorySlot(HeroInventorySlotType.Potion, "Расходники", string.Empty),
                new HeroInventorySlot(HeroInventorySlotType.Ration, "Расходники", string.Empty),
                new HeroInventorySlot(HeroInventorySlotType.Artifact, "Артефакт", string.Empty),
                new HeroInventorySlot(HeroInventorySlotType.Empty, "Слот", string.Empty)
            });
        }

        public bool HasItem(string itemName)
        {
            for (var i = 0; i < slots.Length; i++)
            {
                if (slots[i].ItemName == itemName)
                {
                    return true;
                }
            }

            return false;
        }

        public bool TryFindItemSlot(string itemName, out int slotIndex, out HeroInventorySlot slot)
        {
            for (var i = 0; i < slots.Length; i++)
            {
                if (slots[i].ItemName != itemName)
                {
                    continue;
                }

                slotIndex = i;
                slot = slots[i];
                return true;
            }

            slotIndex = -1;
            slot = default;
            return false;
        }

        public bool TryGetDeathTokenItemName(out string itemName)
        {
            for (var i = 0; i < slots.Length; i++)
            {
                if (!IsDeathTokenItem(slots[i].ItemName))
                {
                    continue;
                }

                itemName = slots[i].ItemName;
                return true;
            }

            itemName = string.Empty;
            return false;
        }

        public bool TryPlaceInEmptySlot(string itemName, string hoverInfo = "")
        {
            for (var i = 0; i < slots.Length; i++)
            {
                if (slots[i].Type != HeroInventorySlotType.Empty || slots[i].HasItem)
                {
                    continue;
                }

                slots[i] = new HeroInventorySlot(slots[i].Type, slots[i].Label, itemName, hoverInfo);
                return true;
            }

            return false;
        }

        public bool TryRemoveItem(string itemName)
        {
            for (var i = 0; i < slots.Length; i++)
            {
                if (slots[i].ItemName != itemName)
                {
                    continue;
                }

                slots[i] = new HeroInventorySlot(slots[i].Type, slots[i].Label, string.Empty);
                return true;
            }

            return false;
        }

        public bool TryEquipSteelSword(out string previousItem)
        {
            return TryEquip(
                HeroInventorySlotType.Weapon,
                SteelSwordItemName,
                "+3 к атаке",
                2,
                SteelSwordAttackBonus,
                0,
                out previousItem);
        }

        public bool TryEquipChainmail(out string previousItem)
        {
            return TryEquip(
                HeroInventorySlotType.Armor,
                ChainmailItemName,
                "+2 Armor Points",
                2,
                0,
                ChainmailArmorBonus,
                out previousItem);
        }

        public bool TryEquipLeatherBoots(out string previousItem)
        {
            return TryEquip(
                HeroInventorySlotType.Footwear,
                LeatherBootsItemName,
                BuildMoveSpeedHover(LeatherBootsMoveSpeedBonusPercent),
                2,
                0,
                0,
                LeatherBootsMoveSpeedBonusPercent,
                out previousItem);
        }

        public bool TryEquipTemperedSword(out string previousItem)
        {
            return TryEquipMasterBlade(out previousItem);
        }

        public bool TryEquipPlateArmor(out string previousItem)
        {
            return TryEquipPlateHarness(out previousItem);
        }

        public bool TryEquipKnightSword(out string previousItem)
        {
            return TryEquip(
                HeroInventorySlotType.Weapon,
                KnightSwordItemName,
                $"+{KnightSwordAttackBonus} к атаке",
                3,
                KnightSwordAttackBonus,
                0,
                out previousItem);
        }

        public bool TryEquipBrigandine(out string previousItem)
        {
            return TryEquip(
                HeroInventorySlotType.Armor,
                BrigandineItemName,
                $"+{BrigandineArmorBonus} Armor Points",
                3,
                0,
                BrigandineArmorBonus,
                out previousItem);
        }

        public bool TryEquipPathfinderBoots(out string previousItem)
        {
            return TryEquip(
                HeroInventorySlotType.Footwear,
                PathfinderBootsItemName,
                BuildMoveSpeedHover(PathfinderBootsMoveSpeedBonusPercent),
                3,
                0,
                0,
                PathfinderBootsMoveSpeedBonusPercent,
                out previousItem);
        }

        public bool TryEquipMasterBlade(out string previousItem)
        {
            return TryEquip(
                HeroInventorySlotType.Weapon,
                MasterBladeItemName,
                $"+{MasterBladeAttackBonus} к атаке",
                4,
                MasterBladeAttackBonus,
                0,
                out previousItem);
        }

        public bool TryEquipSwiftwalkerBoots(out string previousItem)
        {
            return TryEquip(
                HeroInventorySlotType.Footwear,
                SwiftwalkerBootsItemName,
                BuildMoveSpeedHover(SwiftwalkerBootsMoveSpeedBonusPercent),
                4,
                0,
                0,
                SwiftwalkerBootsMoveSpeedBonusPercent,
                out previousItem);
        }

        public bool TryEquipPlateHarness(out string previousItem)
        {
            return TryEquip(
                HeroInventorySlotType.Armor,
                PlateHarnessItemName,
                $"+{PlateHarnessArmorBonus} Armor Points",
                4,
                0,
                PlateHarnessArmorBonus,
                out previousItem);
        }

        public bool TryAddHealthPotion()
        {
            return TryAddStack(HeroInventorySlotType.Potion, HealthPotionItemName, HealthPotionHoverInfo, MaxHealthPotionCount);
        }

        public bool CanAddHealthPotionWithLimit(int maxCount)
        {
            return HealthPotionCount < Math.Max(0, maxCount);
        }

        public bool TryAddHealthPotion(int maxCount, int healAmount)
        {
            return TryAddStack(HeroInventorySlotType.Potion, HealthPotionItemName, BuildHealthPotionHover(healAmount), maxCount);
        }

        public bool TryAddRation()
        {
            return TryAddStack(HeroInventorySlotType.Ration, RationItemName, RationHoverInfo, MaxRationCount);
        }

        public bool CanAddRationWithLimit(int maxCount)
        {
            return RationCount < Math.Max(0, maxCount);
        }

        public bool TryAddRation(int maxCount, int restoreAmount)
        {
            return TryAddStack(HeroInventorySlotType.Ration, RationItemName, BuildRationHover(restoreAmount), maxCount);
        }

        public bool TryAddReturnStone()
        {
            return TrySetUniqueSlot(HeroInventorySlotType.Artifact, ReturnStoneItemName, ReturnStoneHoverInfo);
        }

        public bool TryConsumeHealthPotion()
        {
            return TryConsumeStack(HeroInventorySlotType.Potion, HealthPotionItemName, HealthPotionHoverInfo);
        }

        public bool TryConsumeHealthPotion(int healAmount)
        {
            return TryConsumeStack(HeroInventorySlotType.Potion, HealthPotionItemName, BuildHealthPotionHover(healAmount));
        }

        public bool TryConsumeRation()
        {
            return TryConsumeStack(HeroInventorySlotType.Ration, RationItemName, RationHoverInfo);
        }

        public bool TryConsumeRation(int restoreAmount)
        {
            return TryConsumeStack(HeroInventorySlotType.Ration, RationItemName, BuildRationHover(restoreAmount));
        }

        public bool TryConsumeReturnStone()
        {
            for (var i = 0; i < slots.Length; i++)
            {
                if (slots[i].Type != HeroInventorySlotType.Artifact
                    || slots[i].ItemName != ReturnStoneItemName)
                {
                    continue;
                }

                slots[i] = new HeroInventorySlot(slots[i].Type, slots[i].Label, string.Empty);
                return true;
            }

            return false;
        }

        private static string BuildHealthPotionHover(int healAmount)
        {
            return $"+{Math.Max(0, healAmount)} HP при использовании";
        }

        private static string BuildRationHover(int restoreAmount)
        {
            return $"+{Math.Max(0, restoreAmount)} выносливости при использовании";
        }

        private static string BuildMoveSpeedHover(int bonusPercent)
        {
            return $"+{Math.Max(0, bonusPercent)}% скорости передвижения";
        }

        private int GetStackCount(HeroInventorySlotType slotType, string itemName)
        {
            for (var i = 0; i < slots.Length; i++)
            {
                if (slots[i].Type == slotType && slots[i].ItemName == itemName)
                {
                    return slots[i].Count;
                }
            }

            return 0;
        }

        private bool TryAddStack(HeroInventorySlotType slotType, string itemName, string hoverInfo, int maxCount)
        {
            for (var i = 0; i < slots.Length; i++)
            {
                if (slots[i].Type != slotType)
                {
                    continue;
                }

                if (slots[i].HasItem && slots[i].ItemName != itemName)
                {
                    return false;
                }

                if (slots[i].Count >= maxCount)
                {
                    return false;
                }

                slots[i] = new HeroInventorySlot(slots[i].Type, slots[i].Label, itemName, hoverInfo, slots[i].Count + 1);
                return true;
            }

            return false;
        }

        private bool TrySetUniqueSlot(HeroInventorySlotType slotType, string itemName, string hoverInfo)
        {
            for (var i = 0; i < slots.Length; i++)
            {
                if (slots[i].Type != slotType || slots[i].HasItem)
                {
                    continue;
                }

                slots[i] = new HeroInventorySlot(slots[i].Type, slots[i].Label, itemName, hoverInfo);
                return true;
            }

            return false;
        }

        private bool IsSlotEmpty(HeroInventorySlotType slotType)
        {
            for (var i = 0; i < slots.Length; i++)
            {
                if (slots[i].Type == slotType)
                {
                    return !slots[i].HasItem;
                }
            }

            return false;
        }

        private bool TryConsumeStack(HeroInventorySlotType slotType, string itemName, string hoverInfo)
        {
            for (var i = 0; i < slots.Length; i++)
            {
                if (slots[i].Type != slotType || slots[i].ItemName != itemName || slots[i].Count <= 0)
                {
                    continue;
                }

                var nextCount = slots[i].Count - 1;
                slots[i] = nextCount > 0
                    ? new HeroInventorySlot(slots[i].Type, slots[i].Label, itemName, hoverInfo, nextCount)
                    : new HeroInventorySlot(slots[i].Type, slots[i].Label, string.Empty);
                return true;
            }

            return false;
        }

        private bool TryEquip(
            HeroInventorySlotType slotType,
            string itemName,
            string hoverInfo,
            int tier,
            int attackBonus,
            int armorBonus,
            out string previousItem)
        {
            return TryEquip(slotType, itemName, hoverInfo, tier, attackBonus, armorBonus, 0, out previousItem);
        }

        private bool TryEquip(
            HeroInventorySlotType slotType,
            string itemName,
            string hoverInfo,
            int tier,
            int attackBonus,
            int armorBonus,
            int moveSpeedBonusPercent,
            out string previousItem)
        {
            previousItem = string.Empty;
            for (var i = 0; i < slots.Length; i++)
            {
                if (slots[i].Type != slotType)
                {
                    continue;
                }

                previousItem = slots[i].ItemName;
                if (slots[i].Tier >= tier)
                {
                    return false;
                }

                slots[i] = new HeroInventorySlot(
                    slots[i].Type,
                    slots[i].Label,
                    itemName,
                    hoverInfo,
                    1,
                    tier,
                    attackBonus,
                    armorBonus,
                    moveSpeedBonusPercent);
                return true;
            }

            return false;
        }

        private int GetEquipmentBonus(HeroInventorySlotType slotType, bool attackBonus)
        {
            for (var i = 0; i < slots.Length; i++)
            {
                if (slots[i].Type == slotType)
                {
                    return attackBonus ? slots[i].AttackBonus : slots[i].ArmorBonus;
                }
            }

            return 0;
        }

        private int GetEquipmentMoveSpeedBonusPercent(HeroInventorySlotType slotType)
        {
            for (var i = 0; i < slots.Length; i++)
            {
                if (slots[i].Type == slotType)
                {
                    return slots[i].MoveSpeedBonusPercent;
                }
            }

            return 0;
        }

        private int GetEquipmentTier(HeroInventorySlotType slotType)
        {
            for (var i = 0; i < slots.Length; i++)
            {
                if (slots[i].Type == slotType)
                {
                    return slots[i].Tier;
                }
            }

            return 0;
        }
    }
}
