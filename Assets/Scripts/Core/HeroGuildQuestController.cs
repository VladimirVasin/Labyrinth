using System;
using System.Collections.Generic;
using Labyrinth.Base;
using Labyrinth.Combat;
using Labyrinth.Hero;
using Labyrinth.Maze;
using Labyrinth.Mobs;
using Labyrinth.UI;
using UnityEngine;

namespace Labyrinth.Core
{
    public sealed class HeroGuildQuestController : MonoBehaviour
    {
        private const int AvailablePoolLimit = 3;
        private const int GenerationAttemptsPerSlot = 8;

        private readonly List<HeroGuildQuestModel> quests = new List<HeroGuildQuestModel>();
        private ResourceWallet resources;
        private BaseDevelopment baseDevelopment;
        private MazeRenderer mazeRenderer;
        private Func<MazeGenerationResult> currentMazeProvider;
        private BuildingView guildView;
        private System.Random random;
        private int nextQuestId = 1;
        private bool autoGenerateQuests = true;

        public bool AutoGenerateQuests => autoGenerateQuests;

        public void Configure(
            ResourceWallet resourceWallet,
            BaseDevelopment development,
            MazeRenderer renderer,
            Func<MazeGenerationResult> getCurrentMaze)
        {
            resources = resourceWallet;
            baseDevelopment = development;
            mazeRenderer = renderer;
            currentMazeProvider = getCurrentMaze;
        }

        public void Clear()
        {
            quests.Clear();
            guildView = null;
            random = null;
            nextQuestId = 1;
            autoGenerateQuests = true;
        }

        public void SetGuildView(BuildingView view)
        {
            guildView = view;
            UpdateGuildEffect();
        }

        public void SetAutoGenerateQuests(bool enabled)
        {
            if (autoGenerateQuests == enabled)
            {
                return;
            }

            autoGenerateQuests = enabled;
            UpdateGuildEffect();
            GameDebugLog.Info("HeroGuild", $"Quest auto-generation set to {autoGenerateQuests}.");
        }

        public string GetStatusText()
        {
            if (baseDevelopment == null || !baseDevelopment.HasHeroesGuild)
            {
                return "не построена, открывает контракты зачистки";
            }

            CountBoard(out var available, out var active, out var ready);
            return $"построена ({baseDevelopment.HeroesGuildPosition.x}, {baseDevelopment.HeroesGuildPosition.y}), автогенерация {(autoGenerateQuests ? "вкл." : "выкл.")}, пул {available}/{AvailablePoolLimit}, {active} в работе, {ready} ждут сдачи";
        }

        public BuildingServiceEntry[] BuildServiceEntries(HeroController selectedHero)
        {
            _ = selectedHero;
            if (baseDevelopment == null || !baseDevelopment.HasHeroesGuild)
            {
                return Array.Empty<BuildingServiceEntry>();
            }

            var entries = new BuildingServiceEntry[quests.Count];
            for (var i = 0; i < quests.Count; i++)
            {
                entries[i] = BuildServiceEntry(quests[i]);
            }

            return entries;
        }

        public HeroGuildQuestHudInfo GetHeroQuestHudInfo(HeroController hero)
        {
            if (hero == null
                || hero.Model == null
                || baseDevelopment == null
                || !baseDevelopment.HasHeroesGuild)
            {
                return HeroGuildQuestHudInfo.None;
            }

            var heroNumber = hero.Model.DisplayNumber;
            for (var i = 0; i < quests.Count; i++)
            {
                var quest = quests[i];
                if (!quest.IsActiveForHero(heroNumber))
                {
                    continue;
                }

                var target = FormatSpeciesPlural(quest.TargetSpecies);
                var completed = quest.State == HeroGuildQuestState.CompletedPendingReward;
                var state = completed ? "сдать" : "в работе";
                var tooltip = completed
                    ? $"Зачистка выполнена: {quest.FormatProgress()} {target}. Верните героя ко входу первого уровня или к лестнице, чтобы получить {quest.RewardGold} золота из уже зарезервированной награды."
                    : $"Активный контракт гильдии: победить {quest.TargetCount} {target}. Прогресс засчитывается только этому герою; при смерти контракт будет провален и исчезнет.";

                return new HeroGuildQuestHudInfo(
                    true,
                    target,
                    quest.FormatProgress(),
                    $"{quest.RewardGold} зол.",
                    state,
                    tooltip);
            }

            return HeroGuildQuestHudInfo.None;
        }

        public bool TryAssignQuest(int serviceIndex, HeroController selectedHero)
        {
            if (selectedHero == null
                || selectedHero.Model == null
                || !selectedHero.Model.IsAlive
                || serviceIndex < 0
                || serviceIndex >= quests.Count)
            {
                GameAudioController.PlayUi(GameSfx.HudBlocked);
                return false;
            }

            var quest = quests[serviceIndex];
            if (quest.State != HeroGuildQuestState.Available || HasActiveQuest(selectedHero.Model.DisplayNumber))
            {
                GameAudioController.PlayUi(GameSfx.HudBlocked);
                return false;
            }

            AssignQuestToHero(quest, selectedHero, "manual");
            return true;
        }

        public void NotifyMobDefeated(HeroController victoriousHero, MobController defeatedMob)
        {
            if (victoriousHero == null
                || victoriousHero.Model == null
                || defeatedMob == null
                || defeatedMob.Model == null
                || baseDevelopment == null
                || !baseDevelopment.HasHeroesGuild)
            {
                return;
            }

            var heroNumber = victoriousHero.Model.DisplayNumber;
            for (var i = 0; i < quests.Count; i++)
            {
                var quest = quests[i];
                if (!quest.IsActiveForHero(heroNumber) || !quest.RegisterKill(defeatedMob.Model.Species))
                {
                    continue;
                }

                var completed = quest.State == HeroGuildQuestState.CompletedPendingReward;
                var text = completed ? "Зачистка готова" : $"Зачистка {quest.FormatProgress()}";
                DamageNumberView.CreateText(
                    mazeRenderer,
                    victoriousHero.Model.Position,
                    text,
                    completed ? new Color(0.66f, 1f, 0.42f) : new Color(1f, 0.86f, 0.36f),
                    2.3f);
                UpdateGuildEffect();
                GameDebugLog.Info(
                    "HeroGuild",
                    $"Quest progress: quest={quest.Id}, hero=#{heroNumber}, target={quest.TargetSpecies}, progress={quest.Progress}/{quest.TargetCount}, completed={completed}.");
                return;
            }
        }

        public void UpdateQuests(IReadOnlyList<HeroController> heroes)
        {
            if (baseDevelopment == null || !baseDevelopment.HasHeroesGuild)
            {
                return;
            }

            ResolveAssignedQuests(heroes);
            GenerateAvailableQuests();
            AssignAvailableQuests(heroes);
            UpdateGuildEffect();
        }

        private void ResolveAssignedQuests(IReadOnlyList<HeroController> heroes)
        {
            for (var i = quests.Count - 1; i >= 0; i--)
            {
                var quest = quests[i];
                if (quest.State == HeroGuildQuestState.Available)
                {
                    continue;
                }

                var hero = FindHero(heroes, quest.AssignedHeroNumber);
                if (hero == null || hero.Model == null || !hero.Model.IsAlive)
                {
                    FailQuest(quest, "hero-dead");
                    quests.RemoveAt(i);
                    continue;
                }

                if (quest.State == HeroGuildQuestState.CompletedPendingReward && IsAtGuildTurnIn(hero))
                {
                    PayQuest(quest, hero);
                    quests.RemoveAt(i);
                }
            }
        }

        private void GenerateAvailableQuests()
        {
            if (!autoGenerateQuests || resources == null)
            {
                return;
            }

            EnsureRandom();
            while (CountAvailableQuests() < AvailablePoolLimit && TryGenerateAvailableQuest())
            {
            }
        }

        private bool TryGenerateAvailableQuest()
        {
            for (var attempt = 0; attempt < GenerationAttemptsPerSlot; attempt++)
            {
                var quest = CreateQuest(nextQuestId);
                if (!resources.TrySpendGold(quest.RewardGold))
                {
                    continue;
                }

                nextQuestId++;
                quests.Add(quest);
                ShowGuildText("Новый контракт", new Color(1f, 0.82f, 0.35f));
                GameDebugLog.Info(
                    "HeroGuild",
                    $"Quest generated: quest={quest.Id}, target={quest.TargetSpecies}, count={quest.TargetCount}, reservedReward={quest.RewardGold}, pool={CountAvailableQuests()}/{AvailablePoolLimit}, treasuryGold={resources.Gold}.");
                return true;
            }

            return false;
        }

        private void AssignAvailableQuests(IReadOnlyList<HeroController> heroes)
        {
            if (heroes == null)
            {
                return;
            }

            for (var i = 0; i < heroes.Count; i++)
            {
                var hero = heroes[i];
                if (!CanAutoTakeQuest(hero))
                {
                    continue;
                }

                var quest = FindFirstAvailableQuest();
                if (quest == null)
                {
                    return;
                }

                AssignQuestToHero(quest, hero, "auto");
            }
        }

        private void AssignQuestToHero(HeroGuildQuestModel quest, HeroController hero, string source)
        {
            quest.Assign(hero.Model.DisplayNumber, hero.DisplayName);
            DamageNumberView.CreateText(
                mazeRenderer,
                hero.Model.Position,
                "Контракт",
                new Color(1f, 0.82f, 0.35f),
                2.15f);
            GameDebugLog.Info(
                "HeroGuild",
                $"Quest assigned: quest={quest.Id}, hero=#{quest.AssignedHeroNumber}, source={source}, target={quest.TargetSpecies}, count={quest.TargetCount}, reservedReward={quest.RewardGold}.");
        }

        private BuildingServiceEntry BuildServiceEntry(HeroGuildQuestModel quest)
        {
            var target = FormatSpeciesPlural(quest.TargetSpecies);
            switch (quest.State)
            {
                case HeroGuildQuestState.Accepted:
                    return new BuildingServiceEntry(
                        $"В работе: {target}",
                        quest.FormatProgress(),
                        $"{quest.AssignedHeroName} очищает лабиринт. Побежденные монстры этого вида засчитываются в контракт.",
                        $"#{quest.AssignedHeroNumber}");
                case HeroGuildQuestState.CompletedPendingReward:
                    return new BuildingServiceEntry(
                        $"Сдать: {target}",
                        $"{quest.RewardGold} зол.",
                        $"{quest.AssignedHeroName} выполнил зачистку. Зарезервированная награда будет выдана, когда герой вернется ко входу.",
                        "готово");
                default:
                    return new BuildingServiceEntry(
                        $"Пул: {target}",
                        $"резерв {quest.RewardGold} зол.",
                        $"Контракт уже оплачен из казны. Свободный рыцарь без активного задания гильдии возьмет его автоматически. Цель: победить {quest.TargetCount} {target}.",
                        quest.FormatProgress());
            }
        }

        private void PayQuest(HeroGuildQuestModel quest, HeroController hero)
        {
            hero.Model.AddGold(quest.RewardGold);
            DamageNumberView.CreateText(
                mazeRenderer,
                hero.Model.Position,
                $"+{quest.RewardGold} гильдия",
                new Color(1f, 0.86f, 0.32f),
                2.3f);
            GameAudioController.Play(GameSfx.Purchase, mazeRenderer.GridToWorld(hero.Model.Position), 0.9f);
            GameDebugLog.Info(
                "HeroGuild",
                $"Quest paid: quest={quest.Id}, hero=#{quest.AssignedHeroNumber}, rewardGold={quest.RewardGold}, heroGold={hero.Model.Gold}, treasuryGold={resources?.Gold ?? 0}.");
        }

        private void FailQuest(HeroGuildQuestModel quest, string reason)
        {
            GameDebugLog.Info(
                "HeroGuild",
                $"Quest failed: quest={quest.Id}, hero=#{quest.AssignedHeroNumber}, reason={reason}, reservedRewardLost={quest.RewardGold}, treasuryGold={resources?.Gold ?? 0}.");
        }

        private bool IsAtGuildTurnIn(HeroController hero)
        {
            var result = currentMazeProvider != null ? currentMazeProvider.Invoke() : null;
            if (result == null || result.LevelNumber != 1 || hero.Model == null)
            {
                return false;
            }

            return hero.Model.Position == result.EntrancePosition
                || (result.DownStairs != null && hero.Model.Position == result.DownStairs.Position);
        }

        private void EnsureRandom()
        {
            if (random != null)
            {
                return;
            }

            var result = currentMazeProvider != null ? currentMazeProvider.Invoke() : null;
            var seed = result != null ? result.Settings.Seed ^ (result.LevelNumber * 1000003) : Environment.TickCount;
            random = new System.Random(seed ^ 0x2f4b6d1);
        }

        private HeroGuildQuestModel CreateQuest(int questId)
        {
            var result = currentMazeProvider != null ? currentMazeProvider.Invoke() : null;
            var level = Mathf.Max(1, result?.LevelNumber ?? 1);
            var species = RollSpecies(level);
            var count = GetBaseTargetCount(species) + Mathf.Max(0, level - 1) + random.Next(0, 2);
            var reward = count * GetRewardPerKill(species) + level * 4 + random.Next(0, 5);
            return new HeroGuildQuestModel(questId, species, count, reward);
        }

        private MobSpecies RollSpecies(int dungeonLevel)
        {
            var roll = random.Next(100);
            if (dungeonLevel <= 1)
            {
                if (roll < 52)
                {
                    return MobSpecies.Rat;
                }

                return roll < 86 ? MobSpecies.Goblin : MobSpecies.Orc;
            }

            if (roll < 30)
            {
                return MobSpecies.Rat;
            }

            return roll < 70 ? MobSpecies.Goblin : MobSpecies.Orc;
        }

        private static int GetBaseTargetCount(MobSpecies species)
        {
            switch (species)
            {
                case MobSpecies.Rat:
                    return 3;
                case MobSpecies.Goblin:
                    return 2;
                case MobSpecies.Orc:
                default:
                    return 1;
            }
        }

        private static int GetRewardPerKill(MobSpecies species)
        {
            switch (species)
            {
                case MobSpecies.Rat:
                    return 8;
                case MobSpecies.Goblin:
                    return 13;
                case MobSpecies.Orc:
                default:
                    return 22;
            }
        }

        private int CountAvailableQuests()
        {
            var available = 0;
            for (var i = 0; i < quests.Count; i++)
            {
                if (quests[i].State == HeroGuildQuestState.Available)
                {
                    available++;
                }
            }

            return available;
        }

        private HeroGuildQuestModel FindFirstAvailableQuest()
        {
            for (var i = 0; i < quests.Count; i++)
            {
                if (quests[i].State == HeroGuildQuestState.Available)
                {
                    return quests[i];
                }
            }

            return null;
        }

        private bool CanAutoTakeQuest(HeroController hero)
        {
            return hero != null
                && hero.Model != null
                && hero.Model.IsAlive
                && hero.Model.State != HeroState.Fighting
                && hero.Model.State != HeroState.GoingToEntrance
                && !HasActiveQuest(hero.Model.DisplayNumber);
        }

        private bool HasActiveQuest(int heroNumber)
        {
            for (var i = 0; i < quests.Count; i++)
            {
                if (quests[i].IsActiveForHero(heroNumber))
                {
                    return true;
                }
            }

            return false;
        }

        private static HeroController FindHero(IReadOnlyList<HeroController> heroes, int heroNumber)
        {
            if (heroes == null)
            {
                return null;
            }

            for (var i = 0; i < heroes.Count; i++)
            {
                var hero = heroes[i];
                if (hero != null && hero.Model != null && hero.Model.DisplayNumber == heroNumber)
                {
                    return hero;
                }
            }

            return null;
        }

        private void UpdateGuildEffect()
        {
            if (guildView == null)
            {
                return;
            }

            CountBoard(out var available, out var active, out var ready);
            guildView.SetEffectText($"Создание: {(autoGenerateQuests ? "вкл." : "выкл.")}. Пул: {available}/{AvailablePoolLimit}. В работе: {active}. Сдача: {ready}.");
        }

        private void CountBoard(out int available, out int active, out int ready)
        {
            available = 0;
            active = 0;
            ready = 0;
            for (var i = 0; i < quests.Count; i++)
            {
                switch (quests[i].State)
                {
                    case HeroGuildQuestState.Available:
                        available++;
                        break;
                    case HeroGuildQuestState.Accepted:
                        active++;
                        break;
                    case HeroGuildQuestState.CompletedPendingReward:
                        ready++;
                        break;
                }
            }
        }

        private void ShowGuildText(string text, Color color)
        {
            if (baseDevelopment == null || mazeRenderer == null || !baseDevelopment.HasHeroesGuild)
            {
                return;
            }

            DamageNumberView.CreateText(mazeRenderer, baseDevelopment.HeroesGuildPosition, text, color, 2.4f);
        }

        private static string FormatSpeciesPlural(MobSpecies species)
        {
            switch (species)
            {
                case MobSpecies.Rat:
                    return "крыс";
                case MobSpecies.Goblin:
                    return "гоблинов";
                case MobSpecies.Orc:
                default:
                    return "орков";
            }
        }
    }
}
