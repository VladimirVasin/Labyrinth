using System;
using System.Collections.Generic;
using UnityEngine;

namespace Labyrinth.Hero
{
    public enum HeroLineageMemberStatus
    {
        Alive,
        Dead
    }

    public readonly struct HeroLineageTrainingBonus
    {
        public HeroLineageTrainingBonus(int score, int hitPointBonus, int staminaBonus, int attackBonus)
        {
            Score = Mathf.Clamp(score, 0, HeroLineageState.MaxTrainingScore);
            HitPointBonus = Mathf.Max(0, hitPointBonus);
            StaminaBonus = Mathf.Max(0, staminaBonus);
            AttackBonus = Mathf.Max(0, attackBonus);
        }

        public int Score { get; }

        public int HitPointBonus { get; }

        public int StaminaBonus { get; }

        public int AttackBonus { get; }

        public bool HasAnyBonus => HitPointBonus > 0 || StaminaBonus > 0 || AttackBonus > 0;

        public string ToCompactText()
        {
            return HasAnyBonus ? BuildBonusText() : "нет";
        }

        public string ToDisplayText()
        {
            return HasAnyBonus ? $"Выучка {Score}/{HeroLineageState.MaxTrainingScore}: {BuildBonusText()}" : $"Выучка {Score}/{HeroLineageState.MaxTrainingScore}: нет";
        }

        public static HeroLineageTrainingBonus FromScore(int score)
        {
            var normalizedScore = Mathf.Clamp(score, 0, HeroLineageState.MaxTrainingScore);
            switch (normalizedScore)
            {
                case 1: return new HeroLineageTrainingBonus(normalizedScore, 1, 0, 0);
                case 2: return new HeroLineageTrainingBonus(normalizedScore, 1, 1, 0);
                case 3: return new HeroLineageTrainingBonus(normalizedScore, 2, 1, 0);
                case 4: return new HeroLineageTrainingBonus(normalizedScore, 2, 1, 1);
                case 5: return new HeroLineageTrainingBonus(normalizedScore, 2, 2, 1);
                default: return new HeroLineageTrainingBonus(normalizedScore, 0, 0, 0);
            }
        }

        private string BuildBonusText()
        {
            var text = string.Empty;
            AppendBonus(ref text, HitPointBonus, "HP");
            AppendBonus(ref text, StaminaBonus, "вын.");
            AppendBonus(ref text, AttackBonus, "ATK");
            return text;
        }

        private static void AppendBonus(ref string text, int value, string label)
        {
            if (value <= 0)
            {
                return;
            }

            if (text.Length > 0)
            {
                text += ", ";
            }

            text += $"+{value} {label}";
        }
    }

    public sealed class HeroLineageMember
    {
        public HeroLineageMember(int heroNumber, int generation, string displayName)
        {
            HeroNumber = heroNumber;
            Generation = generation;
            DisplayName = displayName;
            Status = HeroLineageMemberStatus.Alive;
            VengeanceQuest = HeroVengeanceQuest.CreateNone();
        }

        public int HeroNumber { get; }

        public int Generation { get; }

        public string DisplayName { get; }

        public HeroLineageMemberStatus Status { get; private set; }

        public int LevelAtDeath { get; private set; }

        public int ExperienceAtDeath { get; private set; }

        public Vector2Int DeathPosition { get; private set; }

        public int DeathTokenId { get; private set; }

        public bool IsDeathTokenReturned { get; private set; }

        public int ContributedGold { get; private set; }

        public bool HasDeathContext { get; private set; }

        public HeroDeathContext DeathContext { get; private set; }

        public HeroVengeanceQuest VengeanceQuest { get; private set; }

        public HeroSevereInjuryType SevereInjuryAtDeath { get; private set; }

        public HeroScarType ScarAtDeath { get; private set; }

        public HeroCharacterTraitType CharacterTrait { get; private set; }

        public bool HasDeathToken => DeathTokenId > 0;

        public void MarkDead(HeroModel model, int deathTokenId, Vector2Int deathPosition, HeroDeathContext deathContext)
        {
            Status = HeroLineageMemberStatus.Dead;
            LevelAtDeath = model != null ? model.Level : 1;
            ExperienceAtDeath = model != null ? model.Experience : 0;
            DeathPosition = deathPosition;
            DeathTokenId = Mathf.Max(0, deathTokenId);
            IsDeathTokenReturned = false;
            DeathContext = deathContext;
            HasDeathContext = true;
            SevereInjuryAtDeath = model != null ? model.SevereInjury : HeroSevereInjuryType.None;
            ScarAtDeath = model != null ? model.PersonalScar : HeroScarType.None;
            CharacterTrait = model != null ? model.CharacterTrait : CharacterTrait;
        }

        public void MarkTokenReturned()
        {
            IsDeathTokenReturned = true;
        }

        public void AddContribution(int amount)
        {
            ContributedGold += Mathf.Max(0, amount);
        }

        public void SetVengeanceQuest(HeroVengeanceQuest quest)
        {
            VengeanceQuest = quest ?? HeroVengeanceQuest.CreateNone();
        }

        public void SetCharacterTrait(HeroCharacterTraitType trait)
        {
            CharacterTrait = trait;
        }
    }

    public sealed class HeroLineageState
    {
        public const int MaxTrainingScore = 5;
        private const int MaxDeathTrainingScore = 3;
        private const int FundTrainingThreshold = 100;
        private const float LegacyExperienceFraction = 0.25f;
        private readonly List<HeroLineageMember> members = new List<HeroLineageMember>();

        public HeroLineageState(int heroNumber, string baseName)
        {
            HeroNumber = Math.Max(1, heroNumber);
            BaseName = string.IsNullOrWhiteSpace(baseName) ? $"Рыцарь {HeroNumber}" : baseName;
            Generation = 1;
            members.Add(new HeroLineageMember(HeroNumber, Generation, CurrentDisplayName));
        }

        public int HeroNumber { get; }

        public string BaseName { get; }

        public int Generation { get; private set; }

        public int DeathsCount { get; private set; }

        public int PendingLegacyExperience { get; private set; }

        public int HouseFundGold { get; private set; }

        public int TotalContributedGold { get; private set; }

        public int LastContributionGold { get; private set; }

        public string CurrentDisplayName => FormatDisplayName(BaseName, Generation);

        public int TrainingScore => CalculateTrainingScore();

        public HeroLineageTrainingBonus TrainingBonus => HeroLineageTrainingBonus.FromScore(TrainingScore);

        public string TrainingSummaryText => TrainingBonus.ToDisplayText();

        public string TrainingCompactText => $"Выучка {TrainingScore}/{MaxTrainingScore}";

        public string CurrentVengeanceSummaryText
        {
            get
            {
                var quest = CurrentMember?.VengeanceQuest;
                return quest != null ? quest.SummaryText : "Клятва: нет";
            }
        }

        public IReadOnlyList<HeroLineageMember> Members => members;

        public HeroLineageMember CurrentMember => members.Count > 0 ? members[members.Count - 1] : null;

        public string RecordDeath(HeroModel model, int deathTokenId, Vector2Int deathPosition, HeroDeathContext deathContext)
        {
            var member = CurrentMember;
            if (member != null && member.Status == HeroLineageMemberStatus.Dead)
            {
                return CurrentDisplayName;
            }

            DeathsCount++;
            if (model != null)
            {
                PendingLegacyExperience += Mathf.FloorToInt(model.Experience * LegacyExperienceFraction);
            }

            member?.MarkDead(model, deathTokenId, deathPosition, deathContext);
            return CurrentDisplayName;
        }

        public string AdvanceToNextGeneration()
        {
            var fallenMember = CurrentMember;
            Generation++;
            var nextMember = new HeroLineageMember(HeroNumber, Generation, CurrentDisplayName);
            nextMember.SetVengeanceQuest(HeroVengeanceQuest.CreateForSuccessor(fallenMember));
            nextMember.SetCharacterTrait(HeroInjuryCatalog.ChooseSuccessorTrait(fallenMember));
            members.Add(nextMember);
            return CurrentDisplayName;
        }

        public int ConsumePendingLegacyExperience()
        {
            var value = Mathf.Max(0, PendingLegacyExperience);
            PendingLegacyExperience = 0;
            return value;
        }

        public void ContributeGold(int amount, int generation = 0)
        {
            var normalizedAmount = Mathf.Max(0, amount);
            if (normalizedAmount == 0)
            {
                LastContributionGold = 0;
                return;
            }

            HouseFundGold += normalizedAmount;
            TotalContributedGold += normalizedAmount;
            LastContributionGold = normalizedAmount;
            FindMemberByGeneration(generation)?.AddContribution(normalizedAmount);
        }

        public bool TrySpendHouseFund(int amount)
        {
            var normalizedAmount = Mathf.Max(0, amount);
            if (HouseFundGold < normalizedAmount)
            {
                return false;
            }

            HouseFundGold -= normalizedAmount;
            return true;
        }

        public bool MarkDeathTokenReturned(int tokenId)
        {
            if (tokenId <= 0)
            {
                return false;
            }

            for (var i = 0; i < members.Count; i++)
            {
                if (members[i].DeathTokenId != tokenId)
                {
                    continue;
                }

                members[i].MarkTokenReturned();
                return true;
            }

            return false;
        }

        private int CalculateTrainingScore()
        {
            var score = Mathf.Min(MaxDeathTrainingScore, DeathsCount);
            if (HasReturnedDeathToken())
            {
                score++;
            }

            if (HouseFundGold >= FundTrainingThreshold)
            {
                score++;
            }

            return Mathf.Clamp(score, 0, MaxTrainingScore);
        }

        private bool HasReturnedDeathToken()
        {
            for (var i = 0; i < members.Count; i++)
            {
                if (members[i].IsDeathTokenReturned)
                {
                    return true;
                }
            }

            return false;
        }

        private HeroLineageMember FindMemberByGeneration(int generation)
        {
            if (generation <= 0)
            {
                return CurrentMember;
            }

            for (var i = 0; i < members.Count; i++)
            {
                if (members[i].Generation == generation)
                {
                    return members[i];
                }
            }

            return CurrentMember;
        }

        public static string FormatDisplayName(string baseName, int generation)
        {
            var normalizedName = string.IsNullOrWhiteSpace(baseName) ? "Рыцарь" : baseName;
            return generation <= 1
                ? normalizedName
                : $"{normalizedName} {FormatOrdinal(generation)}";
        }

        private static string FormatOrdinal(int generation)
        {
            switch (generation)
            {
                case 2: return "Второй";
                case 3: return "Третий";
                case 4: return "Четвёртый";
                case 5: return "Пятый";
                case 6: return "Шестой";
                case 7: return "Седьмой";
                case 8: return "Восьмой";
                case 9: return "Девятый";
                case 10: return "Десятый";
                case 11: return "Одиннадцатый";
                case 12: return "Двенадцатый";
                case 13: return "Тринадцатый";
                case 14: return "Четырнадцатый";
                case 15: return "Пятнадцатый";
                case 16: return "Шестнадцатый";
                case 17: return "Семнадцатый";
                case 18: return "Восемнадцатый";
                case 19: return "Девятнадцатый";
                case 20: return "Двадцатый";
                default: return $"{Math.Max(1, generation)}-й";
            }
        }
    }
}
