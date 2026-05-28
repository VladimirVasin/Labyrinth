using Labyrinth.Mobs;
using UnityEngine;

namespace Labyrinth.Hero
{
    public enum HeroVengeanceKind
    {
        None,
        SmallTeeth,
        GreenGrin,
        OrcScar,
        BrokenBanner,
        LastMonster,
        CarriedName,
        UndeliveredGold,
        BlackCell,
        ClosedDoor,
        LowerStone
    }

    public readonly struct HeroDeathContext
    {
        public HeroDeathContext(
            bool hasKiller,
            MobSpecies killerSpecies,
            MobRank killerRank,
            bool killerSpawnedFromDarkness,
            int dungeonLevel,
            Vector2Int deathPosition,
            bool carriedGoldIngot,
            bool carriedDeathToken,
            bool diedInDarkness,
            bool nearBarrier,
            Vector2Int barrierPosition,
            string barrierName)
        {
            HasKiller = hasKiller;
            KillerSpecies = killerSpecies;
            KillerRank = killerRank;
            KillerSpawnedFromDarkness = killerSpawnedFromDarkness;
            DungeonLevel = Mathf.Max(1, dungeonLevel);
            DeathPosition = deathPosition;
            CarriedGoldIngot = carriedGoldIngot;
            CarriedDeathToken = carriedDeathToken;
            DiedInDarkness = diedInDarkness;
            NearBarrier = nearBarrier;
            BarrierPosition = barrierPosition;
            BarrierName = string.IsNullOrWhiteSpace(barrierName) ? "преграда" : barrierName;
        }

        public bool HasKiller { get; }

        public MobSpecies KillerSpecies { get; }

        public MobRank KillerRank { get; }

        public bool KillerSpawnedFromDarkness { get; }

        public int DungeonLevel { get; }

        public Vector2Int DeathPosition { get; }

        public bool CarriedGoldIngot { get; }

        public bool CarriedDeathToken { get; }

        public bool DiedInDarkness { get; }

        public bool NearBarrier { get; }

        public Vector2Int BarrierPosition { get; }

        public string BarrierName { get; }

        public string CauseText
        {
            get
            {
                if (HasKiller)
                {
                    return $"{FormatRank(KillerRank)} {FormatSpecies(KillerSpecies)}";
                }

                if (NearBarrier)
                {
                    return $"у преграды: {BarrierName}";
                }

                return DiedInDarkness ? "во тьме" : "в подземелье";
            }
        }

        public static string FormatSpecies(MobSpecies species)
        {
            switch (species)
            {
                case MobSpecies.Rat:
                    return "крыса";
                case MobSpecies.Goblin:
                    return "гоблин";
                case MobSpecies.Orc:
                default:
                    return "орк";
            }
        }

        public static string FormatRank(MobRank rank)
        {
            switch (rank)
            {
                case MobRank.Boss:
                    return "босс";
                case MobRank.MiniBoss:
                    return "минибосс";
                default:
                    return "моб";
            }
        }
    }

    public readonly struct HeroVengeanceProgressResult
    {
        public HeroVengeanceProgressResult(
            bool completed,
            string message,
            int bonusGold,
            int bonusExperience,
            int gainedLevels,
            int maxHitPointBonus,
            int maxStaminaBonus)
        {
            Completed = completed;
            Message = message ?? string.Empty;
            BonusGold = Mathf.Max(0, bonusGold);
            BonusExperience = Mathf.Max(0, bonusExperience);
            GainedLevels = Mathf.Max(0, gainedLevels);
            MaxHitPointBonus = Mathf.Max(0, maxHitPointBonus);
            MaxStaminaBonus = Mathf.Max(0, maxStaminaBonus);
        }

        public bool Completed { get; }

        public string Message { get; }

        public int BonusGold { get; }

        public int BonusExperience { get; }

        public int GainedLevels { get; }

        public int MaxHitPointBonus { get; }

        public int MaxStaminaBonus { get; }

        public bool HasAnyFeedback => Completed || BonusGold > 0 || BonusExperience > 0 || MaxHitPointBonus > 0 || MaxStaminaBonus > 0;

        public static HeroVengeanceProgressResult None =>
            new HeroVengeanceProgressResult(false, string.Empty, 0, 0, 0, 0, 0);

        public HeroVengeanceProgressResult WithAppliedBonuses(int bonusGold, int bonusExperience, int gainedLevels)
        {
            return new HeroVengeanceProgressResult(
                Completed,
                Message,
                bonusGold,
                bonusExperience,
                gainedLevels,
                MaxHitPointBonus,
                MaxStaminaBonus);
        }
    }

    public sealed class HeroVengeanceQuest
    {
        private HeroVengeanceQuest(
            HeroVengeanceKind kind,
            string oathName,
            string oathQuote,
            string objectiveText,
            string rewardName,
            string rewardText,
            int requiredProgress,
            MobSpecies targetSpecies = MobSpecies.Orc,
            MobRank targetRank = MobRank.Regular,
            int targetDungeonLevel = 1,
            Vector2Int targetPosition = default,
            string targetName = "")
        {
            Kind = kind;
            OathName = oathName ?? string.Empty;
            OathQuote = oathQuote ?? string.Empty;
            ObjectiveText = objectiveText ?? string.Empty;
            RewardName = rewardName ?? string.Empty;
            RewardText = rewardText ?? string.Empty;
            RequiredProgress = Mathf.Max(0, requiredProgress);
            TargetSpecies = targetSpecies;
            TargetRank = targetRank;
            TargetDungeonLevel = Mathf.Max(1, targetDungeonLevel);
            TargetPosition = targetPosition;
            TargetName = targetName ?? string.Empty;
        }

        public HeroVengeanceKind Kind { get; }

        public string OathName { get; }

        public string OathQuote { get; }

        public string ObjectiveText { get; }

        public string RewardName { get; }

        public string RewardText { get; }

        public int Progress { get; private set; }

        public int RequiredProgress { get; }

        public MobSpecies TargetSpecies { get; }

        public MobRank TargetRank { get; }

        public int TargetDungeonLevel { get; }

        public Vector2Int TargetPosition { get; }

        public string TargetName { get; }

        public bool IsCompleted { get; private set; }

        public bool IsActive => Kind != HeroVengeanceKind.None;

        public string DisplayTitle => !IsActive
            ? "нет"
            : IsCompleted
                ? $"Исполнена: {RewardName}"
                : OathName;

        public string SummaryText => !IsActive
            ? "Клятва: нет"
            : IsCompleted
                ? $"Клятва исполнена: {RewardName}"
                : $"Клятва: {OathName} {Progress}/{RequiredProgress}";

        public string TooltipText => !IsActive
            ? "У этого наследника нет личной клятвы мести."
            : $"{OathQuote}\nЦель: {ObjectiveText}\nНаграда: {RewardText}";

        public static HeroVengeanceQuest CreateNone()
        {
            return new HeroVengeanceQuest(HeroVengeanceKind.None, "нет", string.Empty, string.Empty, string.Empty, string.Empty, 0);
        }

        public static HeroVengeanceQuest CreateForSuccessor(HeroLineageMember fallenMember)
        {
            if (fallenMember == null || !fallenMember.HasDeathContext)
            {
                return CreateNone();
            }

            var death = fallenMember.DeathContext;
            if (death.HasKiller && death.KillerRank == MobRank.Boss)
            {
                return CreateLastMonster(death);
            }

            if (death.HasKiller && death.KillerRank == MobRank.MiniBoss)
            {
                return CreateBrokenBanner(death);
            }

            if (death.NearBarrier)
            {
                return CreateClosedDoor(death);
            }

            if (death.CarriedDeathToken)
            {
                return CreateCarriedName(death);
            }

            if (death.CarriedGoldIngot)
            {
                return CreateUndeliveredGold(death);
            }

            if (death.DiedInDarkness || death.KillerSpawnedFromDarkness)
            {
                return CreateBlackCell(death);
            }

            if (death.HasKiller)
            {
                switch (death.KillerSpecies)
                {
                    case MobSpecies.Rat:
                        return CreateSmallTeeth(death);
                    case MobSpecies.Goblin:
                        return CreateGreenGrin(death);
                    case MobSpecies.Orc:
                        return CreateOrcScar(death);
                }
            }

            return death.DungeonLevel > 1 ? CreateLowerStone(death) : CreateNone();
        }

        public HeroVengeanceProgressResult RegisterMobDefeated(MobModel mob)
        {
            if (!CanProgress() || mob == null)
            {
                return HeroVengeanceProgressResult.None;
            }

            switch (Kind)
            {
                case HeroVengeanceKind.SmallTeeth:
                    return mob.Species == MobSpecies.Rat ? IncrementProgress() : HeroVengeanceProgressResult.None;
                case HeroVengeanceKind.GreenGrin:
                    return mob.Species == MobSpecies.Goblin ? IncrementProgress() : HeroVengeanceProgressResult.None;
                case HeroVengeanceKind.OrcScar:
                    return mob.Species == MobSpecies.Orc ? Complete() : HeroVengeanceProgressResult.None;
                case HeroVengeanceKind.BrokenBanner:
                    return mob.IsMiniBoss ? Complete() : HeroVengeanceProgressResult.None;
                case HeroVengeanceKind.LastMonster:
                    return mob.IsBoss ? Complete() : HeroVengeanceProgressResult.None;
                case HeroVengeanceKind.BlackCell:
                    return mob.SpawnedFromDarkness ? IncrementProgress() : HeroVengeanceProgressResult.None;
                default:
                    return HeroVengeanceProgressResult.None;
            }
        }

        public HeroVengeanceProgressResult RegisterGoldIngotDelivered()
        {
            return CanProgress() && Kind == HeroVengeanceKind.UndeliveredGold
                ? Complete()
                : HeroVengeanceProgressResult.None;
        }

        public HeroVengeanceProgressResult RegisterDeathTokenDelivered()
        {
            return CanProgress() && Kind == HeroVengeanceKind.CarriedName
                ? Complete()
                : HeroVengeanceProgressResult.None;
        }

        public HeroVengeanceProgressResult RegisterBarrierOpened(Vector2Int position, bool isStairs)
        {
            if (!CanProgress())
            {
                return HeroVengeanceProgressResult.None;
            }

            if (Kind == HeroVengeanceKind.LastMonster && isStairs)
            {
                return Complete();
            }

            if (Kind != HeroVengeanceKind.ClosedDoor)
            {
                return HeroVengeanceProgressResult.None;
            }

            return TargetPosition == default || GridDistance(position, TargetPosition) <= 1
                ? Complete()
                : HeroVengeanceProgressResult.None;
        }

        public HeroVengeanceProgressResult RegisterNewCellExplored(int dungeonLevel)
        {
            return CanProgress() && Kind == HeroVengeanceKind.LowerStone && dungeonLevel == TargetDungeonLevel
                ? IncrementProgress()
                : HeroVengeanceProgressResult.None;
        }

        private bool CanProgress()
        {
            return IsActive && !IsCompleted;
        }

        private HeroVengeanceProgressResult IncrementProgress()
        {
            Progress = Mathf.Min(RequiredProgress, Progress + 1);
            return Progress >= RequiredProgress ? Complete() : HeroVengeanceProgressResult.None;
        }

        private HeroVengeanceProgressResult Complete()
        {
            if (IsCompleted)
            {
                return HeroVengeanceProgressResult.None;
            }

            IsCompleted = true;
            Progress = RequiredProgress;
            return BuildCompletionReward();
        }

        private HeroVengeanceProgressResult BuildCompletionReward()
        {
            switch (Kind)
            {
                case HeroVengeanceKind.BrokenBanner:
                    return new HeroVengeanceProgressResult(true, $"{OathName} исполнена", 0, 5, 0, 0, 0);
                case HeroVengeanceKind.LastMonster:
                    return new HeroVengeanceProgressResult(true, $"{OathName} исполнена", 0, 10, 0, 0, 0);
                case HeroVengeanceKind.ClosedDoor:
                    return new HeroVengeanceProgressResult(true, $"{OathName} исполнена", 0, 0, 0, 0, 1);
                case HeroVengeanceKind.LowerStone:
                    return new HeroVengeanceProgressResult(true, $"{OathName} исполнена", 0, 0, 0, 1, 0);
                default:
                    return new HeroVengeanceProgressResult(true, $"{OathName} исполнена", 0, 0, 0, 0, 0);
            }
        }

        private static HeroVengeanceQuest CreateSmallTeeth(HeroDeathContext death)
        {
            return new HeroVengeanceQuest(
                HeroVengeanceKind.SmallTeeth,
                "Клятва мелких зубов",
                "\"Пусть смеются. Я принесу домой мешок их хвостов.\"",
                "Истребить 5 крыс.",
                "Крысолов дома",
                "+2 атаки против крыс, +1 XP за крыс.",
                5,
                MobSpecies.Rat,
                MobRank.Regular,
                death.DungeonLevel);
        }

        private static HeroVengeanceQuest CreateGreenGrin(HeroDeathContext death)
        {
            return new HeroVengeanceQuest(
                HeroVengeanceKind.GreenGrin,
                "Клятва зелёной ухмылки",
                "\"Я не поверю ни одному шороху. Пусть их ухмылки гаснут первыми.\"",
                "Убить 3 гоблинов-засадников.",
                "Глаз на ухмылку",
                "Первый удар по гоблину +3 атаки, видимость +1.",
                3,
                MobSpecies.Goblin,
                MobRank.Regular,
                death.DungeonLevel);
        }

        private static HeroVengeanceQuest CreateOrcScar(HeroDeathContext death)
        {
            return new HeroVengeanceQuest(
                HeroVengeanceKind.OrcScar,
                "Клятва орочьего шрама",
                "\"Они рубят широко. Я ударю раньше.\"",
                "Победить орка один на один.",
                "Орочья расплата",
                "Первый удар по оркам +5 атаки, +2 XP за орков.",
                1,
                MobSpecies.Orc,
                MobRank.Regular,
                death.DungeonLevel);
        }

        private static HeroVengeanceQuest CreateBrokenBanner(HeroDeathContext death)
        {
            return new HeroVengeanceQuest(
                HeroVengeanceKind.BrokenBanner,
                "Клятва сломанного знамени",
                "\"Он стоял на дороге моего дома. Я сдвину его.\"",
                "Убить минибосса.",
                "Охотник на вожаков",
                "+10% урона по минибоссам, -1 к их атаке, +5 XP за исполнение.",
                1,
                death.KillerSpecies,
                MobRank.MiniBoss,
                death.DungeonLevel);
        }

        private static HeroVengeanceQuest CreateLastMonster(HeroDeathContext death)
        {
            return new HeroVengeanceQuest(
                HeroVengeanceKind.LastMonster,
                "Клятва последнего чудовища",
                "\"Отец нашёл чудовище. Мне осталось найти его сердце.\"",
                "Победить босса или открыть спуск после его смерти.",
                "Наследник вендетты",
                "+15% урона по боссам, первый удар босса слабее на 25%, +10 XP за исполнение.",
                1,
                death.KillerSpecies,
                MobRank.Boss,
                death.DungeonLevel);
        }

        private static HeroVengeanceQuest CreateCarriedName(HeroDeathContext death)
        {
            return new HeroVengeanceQuest(
                HeroVengeanceKind.CarriedName,
                "Клятва вынесенного имени",
                "\"Кто упал в камне, тот всё равно вернётся домой.\"",
                "Вернуть жетон павшего рыцаря ко входу.",
                "Не бросать своих",
                "+5 XP за возврат жетонов, +1 брони с жетоном, быстрее возврат с жетоном.",
                1,
                MobSpecies.Orc,
                MobRank.Regular,
                death.DungeonLevel);
        }

        private static HeroVengeanceQuest CreateUndeliveredGold(HeroDeathContext death)
        {
            return new HeroVengeanceQuest(
                HeroVengeanceKind.UndeliveredGold,
                "Клятва недонесённого золота",
                "\"Золото мёртвым не нужно. Живым — нужно очень.\"",
                "Доставить золотой слиток ко входу.",
                "Долг казне",
                "+5 личного золота и +2 XP при сдаче слитка.",
                1,
                MobSpecies.Orc,
                MobRank.Regular,
                death.DungeonLevel);
        }

        private static HeroVengeanceQuest CreateBlackCell(HeroDeathContext death)
        {
            return new HeroVengeanceQuest(
                HeroVengeanceKind.BlackCell,
                "Клятва чёрной клетки",
                "\"Тьма забрала его без свидетелей. Меня она увидит.\"",
                "Убить 3 врагов, появившихся из темноты.",
                "Тьма меня увидит",
                "+1 видимости, +2 атаки против тёмного респауна.",
                3,
                MobSpecies.Orc,
                MobRank.Regular,
                death.DungeonLevel);
        }

        private static HeroVengeanceQuest CreateClosedDoor(HeroDeathContext death)
        {
            return new HeroVengeanceQuest(
                HeroVengeanceKind.ClosedDoor,
                "Клятва закрытой двери",
                "\"Дверь запомнила его руку. Теперь запомнит мою.\"",
                $"Открыть или пройти преграду: {death.BarrierName}.",
                "Дверь запомнит меня",
                "+1 Max Stamina, быстрее путь к дверям и спускам.",
                1,
                MobSpecies.Orc,
                MobRank.Regular,
                death.DungeonLevel,
                death.BarrierPosition,
                death.BarrierName);
        }

        private static HeroVengeanceQuest CreateLowerStone(HeroDeathContext death)
        {
            return new HeroVengeanceQuest(
                HeroVengeanceKind.LowerStone,
                "Клятва нижнего камня",
                "\"Внизу камень тяжелее. Значит, я стану тяжелее тоже.\"",
                $"Разведать 10 новых клеток на уровне {death.DungeonLevel}.",
                "Кровь глубины",
                $"+1 HP и +1 атаки на уровне {death.DungeonLevel}.",
                10,
                MobSpecies.Orc,
                MobRank.Regular,
                death.DungeonLevel);
        }

        private static int GridDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }
    }
}
