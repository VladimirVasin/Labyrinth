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
        private readonly HashSet<MobSpecies> discoveredQuestSpecies = new HashSet<MobSpecies>();
        private readonly HashSet<MobSpecies> visibleSpeciesScratch = new HashSet<MobSpecies>();
        private ResourceWallet resources;
        private BaseDevelopment baseDevelopment;
        private MazeRenderer mazeRenderer;
        private Func<MazeGenerationResult> currentMazeProvider;
        private Func<MobManager> mobManagerProvider;
        private BuildingView guildView;
        private System.Random random;
        private int nextQuestId = 1;
        private bool autoGenerateQuests = true;

        public bool AutoGenerateQuests => autoGenerateQuests;

        public void Configure(
            ResourceWallet resourceWallet,
            BaseDevelopment development,
            MazeRenderer renderer,
            Func<MazeGenerationResult> getCurrentMaze,
            Func<MobManager> getMobManager = null)
        {
            resources = resourceWallet;
            baseDevelopment = development;
            mazeRenderer = renderer;
            currentMazeProvider = getCurrentMaze;
            mobManagerProvider = getMobManager;
        }

        public void Clear()
        {
            quests.Clear();
            discoveredQuestSpecies.Clear();
            visibleSpeciesScratch.Clear();
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

            if (!CanHeroTakeQuest(selectedHero, quest))
            {
                GameAudioController.PlayUi(GameSfx.HudBlocked);
                GameDebugLog.Info(
                    "HeroGuild",
                    $"Quest assignment blocked: quest={quest.Id}, hero=#{selectedHero.Model.DisplayNumber}, target={quest.TargetSpecies}, reason=hero-not-ready.");
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

            RememberDiscoveredSpecies(defeatedMob.Model.Species, "defeated");
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
            RefreshDiscoveredSpecies(heroes);
            GenerateAvailableQuests(heroes);
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

        private void GenerateAvailableQuests(IReadOnlyList<HeroController> heroes)
        {
            if (!autoGenerateQuests || resources == null)
            {
                return;
            }

            EnsureRandom();
            while (CountAvailableQuests() < AvailablePoolLimit && TryGenerateAvailableQuest(heroes))
            {
            }
        }

        private bool TryGenerateAvailableQuest(IReadOnlyList<HeroController> heroes)
        {
            var allowedSpecies = BuildAllowedQuestSpecies(heroes);
            if (allowedSpecies.Count == 0)
            {
                return false;
            }

            for (var attempt = 0; attempt < GenerationAttemptsPerSlot; attempt++)
            {
                var quest = CreateQuest(nextQuestId, RollSpecies(GetDungeonLevel(), allowedSpecies));
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

                var quest = FindFirstAvailableQuest(hero);
                if (quest == null)
                {
                    continue;
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

        private HeroGuildQuestModel CreateQuest(int questId, MobSpecies species)
        {
            var level = GetDungeonLevel();
            var count = GetBaseTargetCount(species) + Mathf.Max(0, level - 1) + random.Next(0, 2);
            var reward = count * GetRewardPerKill(species) + level * 4 + random.Next(0, 5);
            return new HeroGuildQuestModel(questId, species, count, reward);
        }

        private MobSpecies RollSpecies(int dungeonLevel, IReadOnlyList<MobSpecies> allowedSpecies)
        {
            var totalWeight = 0;
            for (var i = 0; i < allowedSpecies.Count; i++)
            {
                totalWeight += GetQuestSpeciesWeight(allowedSpecies[i], dungeonLevel);
            }

            var roll = random.Next(Mathf.Max(1, totalWeight));
            for (var i = 0; i < allowedSpecies.Count; i++)
            {
                roll -= GetQuestSpeciesWeight(allowedSpecies[i], dungeonLevel);
                if (roll < 0)
                {
                    return allowedSpecies[i];
                }
            }

            return allowedSpecies[0];
        }

        private static int GetQuestSpeciesWeight(MobSpecies species, int dungeonLevel)
        {
            if (dungeonLevel <= 1)
            {
                switch (species)
                {
                    case MobSpecies.Rat:
                        return 52;
                    case MobSpecies.Goblin:
                        return 34;
                    case MobSpecies.Orc:
                    default:
                        return 14;
                }
            }

            switch (species)
            {
                case MobSpecies.Rat:
                    return 30;
                case MobSpecies.Goblin:
                    return 40;
                case MobSpecies.Orc:
                default:
                    return 30;
            }
        }

        private List<MobSpecies> BuildAllowedQuestSpecies(IReadOnlyList<HeroController> heroes)
        {
            var allowed = new List<MobSpecies> { MobSpecies.Rat };
            if (IsQuestSpeciesUnlocked(MobSpecies.Goblin, heroes))
            {
                allowed.Add(MobSpecies.Goblin);
            }

            if (IsQuestSpeciesUnlocked(MobSpecies.Orc, heroes) && HasAnyHeroReadyForSpecies(heroes, MobSpecies.Orc))
            {
                allowed.Add(MobSpecies.Orc);
            }

            return allowed;
        }

        private bool IsQuestSpeciesUnlocked(MobSpecies species, IReadOnlyList<HeroController> heroes)
        {
            switch (species)
            {
                case MobSpecies.Rat:
                    return true;
                case MobSpecies.Goblin:
                    return discoveredQuestSpecies.Contains(MobSpecies.Goblin)
                        || GetDungeonLevel() > 1
                        || GetBestHeroLevel(heroes) >= 2
                        || GetBestRememberedCellCount(heroes) >= 80;
                case MobSpecies.Orc:
                default:
                    return discoveredQuestSpecies.Contains(MobSpecies.Orc)
                        || GetDungeonLevel() > 1
                        || IsCentralExitOpen(currentMazeProvider != null ? currentMazeProvider.Invoke() : null);
            }
        }

        private void RefreshDiscoveredSpecies(IReadOnlyList<HeroController> heroes)
        {
            var mobManager = mobManagerProvider != null ? mobManagerProvider.Invoke() : null;
            if (mobManager == null)
            {
                return;
            }

            visibleSpeciesScratch.Clear();
            mobManager.CollectVisibleRegularSpecies(heroes, visibleSpeciesScratch);
            foreach (var species in visibleSpeciesScratch)
            {
                RememberDiscoveredSpecies(species, "visible");
            }
        }

        private void RememberDiscoveredSpecies(MobSpecies species, string reason)
        {
            if (!discoveredQuestSpecies.Add(species))
            {
                return;
            }

            GameDebugLog.Info(
                "HeroGuild",
                $"Quest target discovered: target={species}, reason={reason}.");
        }

        private static bool HasAnyHeroReadyForSpecies(IReadOnlyList<HeroController> heroes, MobSpecies species)
        {
            if (heroes == null)
            {
                return false;
            }

            for (var i = 0; i < heroes.Count; i++)
            {
                if (CanHeroTakeSpecies(heroes[i], species))
                {
                    return true;
                }
            }

            return false;
        }

        private bool CanHeroTakeQuest(HeroController hero, HeroGuildQuestModel quest)
        {
            return quest != null && CanHeroTakeSpecies(hero, quest.TargetSpecies);
        }

        private static bool CanHeroTakeSpecies(HeroController hero, MobSpecies species)
        {
            if (hero == null || hero.Model == null || !hero.Model.IsAlive)
            {
                return false;
            }

            if (species != MobSpecies.Orc)
            {
                return true;
            }

            var model = hero.Model;
            return model.Level >= 4
                || model.AttackPoints >= 13
                || model.AttackPoints + model.ArmorPoints >= 15;
        }

        private int GetDungeonLevel()
        {
            var result = currentMazeProvider != null ? currentMazeProvider.Invoke() : null;
            return Mathf.Max(1, result?.LevelNumber ?? 1);
        }

        private static int GetBestHeroLevel(IReadOnlyList<HeroController> heroes)
        {
            var best = 0;
            if (heroes == null)
            {
                return best;
            }

            for (var i = 0; i < heroes.Count; i++)
            {
                var hero = heroes[i];
                if (hero?.Model != null && hero.Model.IsAlive)
                {
                    best = Mathf.Max(best, hero.Model.Level);
                }
            }

            return best;
        }

        private static int GetBestRememberedCellCount(IReadOnlyList<HeroController> heroes)
        {
            var best = 0;
            if (heroes == null)
            {
                return best;
            }

            for (var i = 0; i < heroes.Count; i++)
            {
                var hero = heroes[i];
                if (hero?.Model?.Memory != null && hero.Model.IsAlive)
                {
                    best = Mathf.Max(best, hero.Model.Memory.RememberedCount);
                }
            }

            return best;
        }

        private static bool IsCentralExitOpen(MazeGenerationResult result)
        {
            if (result == null || !result.CentralRoom.IsValid)
            {
                return false;
            }

            if (result.CentralDoors != null)
            {
                for (var i = 0; i < result.CentralDoors.Count; i++)
                {
                    var door = result.CentralDoors[i];
                    if (door != null && door.Position == result.CentralRoom.ExitPosition)
                    {
                        return door.IsOpen;
                    }
                }
            }

            return result.Grid != null
                && result.Grid.InBounds(result.CentralRoom.ExitPosition)
                && result.Grid.Get(result.CentralRoom.ExitPosition).IsWalkable;
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

        private HeroGuildQuestModel FindFirstAvailableQuest(HeroController hero)
        {
            for (var i = 0; i < quests.Count; i++)
            {
                if (quests[i].State == HeroGuildQuestState.Available && CanHeroTakeQuest(hero, quests[i]))
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
