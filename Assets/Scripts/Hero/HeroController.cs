using System;
using Labyrinth.Combat;
using Labyrinth.Core;
using Labyrinth.Maze;
using UnityEngine;

namespace Labyrinth.Hero
{
    public sealed class HeroController : MonoBehaviour
    {
        private const float StepInterval = 0.34f;
        private const float CorpseVisibilityDuration = 5f;
        private const float VowOfReturnStepIntervalMultiplier = 0.45f;
        private const float FortifiedCellSpeedMultiplier = 1.2f;
        private const float VengeanceTokenReturnSpeedMultiplier = 1.1f;
        private const float ActivityTraceInterval = 8f;

        private MazeGrid grid;
        private MazeRenderer mazeRenderer;
        private HeroExplorer explorer;
        private HeroView heroView;
        private HeroMemoryView memoryView;
        private Vector2Int entrancePosition;
        private HeroState stateBeforeCombat = HeroState.Exploring;
        private Func<Vector2Int, bool> fortifiedCellProvider;
        private bool explorationPaused;
        private bool corpseExpired;
        private float timeUntilNextStep;
        private float corpseVisibilityRemaining;
        private float activityTraceTimer;
        private HeroState lastLoggedState;
        private int lastTraceSteps;
        private int lastTraceMemory;

        public HeroModel Model { get; private set; }

        public int DisplayNumber { get; private set; }

        public string DisplayName { get; private set; }

        public bool ProvidesVisibility => Model != null && (Model.IsAlive || !corpseExpired);

        public bool IsExpiredCorpse => Model != null && !Model.IsAlive && corpseExpired;

        public float CorpseVisibilityRemaining =>
            Model != null && !Model.IsAlive && !corpseExpired
                ? Mathf.Max(0f, corpseVisibilityRemaining)
                : 0f;

        public static HeroController Create(
            MazeGenerationResult result,
            Vector2Int startPosition,
            int displayNumber,
            string displayName,
            MazeRenderer mazeRenderer,
            HeroMemory memory,
            HeroMemoryView memoryView,
            GoldIngotManager goldIngotManager,
            HeroDeathTokenManager deathTokenManager,
            Action<HeroModel, int> entranceKnowledgeSync,
            Action<HeroModel, int, DungeonStairsModel> downStairsOpened,
            HeroExplorer.NearbyMobInteractionCellProvider nearbyMobInteractionCellProvider,
            HeroExplorationCoordinator explorationCoordinator,
            int statSeed = 0)
        {
            var controllerObject = new GameObject("HeroController");
            var controller = controllerObject.AddComponent<HeroController>();
            controller.Initialize(result, startPosition, displayNumber, displayName, mazeRenderer, memory, memoryView, goldIngotManager, deathTokenManager, entranceKnowledgeSync, downStairsOpened, nearbyMobInteractionCellProvider, explorationCoordinator, statSeed);
            return controller;
        }

        public string StatusText
        {
            get
            {
                if (Model == null)
                {
                    return "Герой не создан";
                }

                switch (Model.State)
                {
                    case HeroState.GoingToEntrance:
                        return $"идет к входу: HP {Model.HitPoints}/{Model.MaxHitPoints}, выносл. {Model.Stamina}/{Model.MaxStamina}";
                    case HeroState.Exploring:
                        return $"исследует: ур. {Model.Level}, XP {Model.Experience}/{Model.ExperienceForNextLevel}, выносл. {Model.Stamina}/{Model.MaxStamina}";
                    case HeroState.SearchingKey:
                        return $"ищет ключ: выносл. {Model.Stamina}/{Model.MaxStamina}";
                    case HeroState.ReturningToDoor:
                        return $"идет к двери: выносл. {Model.Stamina}/{Model.MaxStamina}";
                    case HeroState.OpeningDoor:
                        return "открывает дверь";
                    case HeroState.ReturningToCastle:
                        return $"возвращается к замку: выносл. {Model.Stamina}/{Model.MaxStamina}";
                    case HeroState.Fighting:
                        return $"сражается: HP {Model.HitPoints}/{Model.MaxHitPoints}, выносл. {Model.Stamina}/{Model.MaxStamina}";
                    case HeroState.Stuck:
                        return $"ждет цель: шагов {Model.StepsTaken}, память {Model.Memory.RememberedCount}";
                    case HeroState.Defeated:
                        return !corpseExpired
                            ? $"погиб: видимость {CorpseVisibilityRemaining:0.0} сек."
                            : $"погиб: ур. {Model.Level}, XP {Model.Experience}";
                    default:
                        return "неизвестное состояние героя";
                }
            }
        }

        public void SetSelected(bool selected)
        {
            if (heroView != null)
            {
                heroView.SetSelected(selected);
            }
        }

        public void SetExplorationPaused(bool paused)
        {
            explorationPaused = paused;
        }

        public void BeginEntranceCommute(Vector2Int housePosition)
        {
            if (Model == null || heroView == null)
            {
                return;
            }

            explorationPaused = true;
            Model.SetState(HeroState.GoingToEntrance);
            Model.SetPosition(housePosition);
            RefreshVisibility();
            heroView.SetGridPositionImmediate(housePosition);
            timeUntilNextStep = GetCurrentStepInterval();
            LogStateChangeIfNeeded("house-departure");
        }

        public void MoveEntranceCommuteTo(Vector2Int position)
        {
            if (Model == null || heroView == null || !Model.IsAlive)
            {
                return;
            }

            Model.SetState(HeroState.GoingToEntrance);
            Model.SetPosition(position);
            RefreshVisibility();
            heroView.MoveTo(position);
        }

        public void CompleteEntranceCommute()
        {
            if (Model == null || heroView == null || !Model.IsAlive)
            {
                return;
            }

            Model.SetPosition(entrancePosition);
            Model.Memory.Remember(entrancePosition);
            Model.SetState(HeroState.Exploring);
            explorationPaused = false;
            stateBeforeCombat = HeroState.Exploring;
            RefreshVisibility();
            RefreshMemoryView();
            heroView.MoveTo(entrancePosition);
            timeUntilNextStep = GetCurrentStepInterval();
            LogStateChangeIfNeeded("entrance-arrival");
        }

        public void SetFortifiedCellProvider(Func<Vector2Int, bool> provider)
        {
            fortifiedCellProvider = provider;
            timeUntilNextStep = GetCurrentStepInterval();
        }

        public void EnterCombat()
        {
            explorationPaused = true;
            if (Model != null && Model.IsAlive)
            {
                stateBeforeCombat = Model.State;
                Model.SetState(HeroState.Fighting);
                LogStateChangeIfNeeded("combat-start");
            }
        }

        public void LeaveCombat()
        {
            if (Model != null && Model.IsAlive && Model.State == HeroState.Fighting)
            {
                Model.SetState(stateBeforeCombat == HeroState.Fighting || stateBeforeCombat == HeroState.Defeated
                    ? HeroState.Exploring
                    : stateBeforeCombat);
                explorationPaused = false;
                timeUntilNextStep = GetCurrentStepInterval();
                LogStateChangeIfNeeded("combat-end");
            }
        }

        public void RetreatFromCombatToCastle()
        {
            if (Model == null || !Model.IsAlive)
            {
                return;
            }

            explorer?.ReleaseExplorationTarget("combat retreat");
            Model.SetState(HeroState.ReturningToCastle);
            explorationPaused = false;
            timeUntilNextStep = GetCurrentStepInterval();
            LogStateChangeIfNeeded("combat-retreat");
        }

        public void SetGridPositionImmediate(Vector2Int position)
        {
            if (Model == null)
            {
                return;
            }

            Model.SetPosition(position);
            RefreshVisibility();
            heroView.SetGridPositionImmediate(position);
        }

        public bool TryUseReturnStoneToEntrance()
        {
            if (Model == null || explorer == null || mazeRenderer == null || !explorer.TryUseReturnStoneToEntrance())
            {
                return false;
            }

            RefreshVisibility();
            RefreshMemoryView();
            heroView.SetGridPositionImmediate(Model.Position);
            timeUntilNextStep = GetCurrentStepInterval();
            DamageNumberView.CreateText(
                mazeRenderer,
                Model.Position,
                HeroInventory.ReturnStoneItemName,
                new Color(0.58f, 0.82f, 1f),
                2.1f);
            GameAudioController.Play(GameSfx.LevelSwitch, mazeRenderer.GridToWorld(Model.Position), 0.78f);
            LogStateChangeIfNeeded("return-stone");
            return true;
        }

        public void FaceGridPosition(Vector2Int position)
        {
            heroView.FaceGridPosition(position);
        }

        public void PlayAttack(Vector2Int targetPosition)
        {
            heroView.PlayAttack(targetPosition);
        }

        public int ReceiveDamage(int incomingDamage)
        {
            var wasAlive = Model.IsAlive;
            var damage = Model.ReceiveDamage(incomingDamage);
            if (wasAlive && !Model.IsAlive)
            {
                explorer?.ReleaseExplorationTarget("defeated");
                StartCorpseVisibility();
            }

            LogStateChangeIfNeeded("damage");
            return damage;
        }

        public int ReceiveResolvedDamage(int resolvedDamage)
        {
            var wasAlive = Model.IsAlive;
            var damage = Model.ReceiveResolvedDamage(resolvedDamage);
            if (wasAlive && !Model.IsAlive)
            {
                explorer?.ReleaseExplorationTarget("defeated");
                StartCorpseVisibility();
            }

            LogStateChangeIfNeeded("damage");
            return damage;
        }

        private void Update()
        {
            if (Model == null)
            {
                return;
            }

            if (!Model.IsAlive)
            {
                UpdateCorpseVisibility();
                return;
            }

            if (explorationPaused)
            {
                LogActivityTrace();
                return;
            }

            if ((Model.State == HeroState.ReturningToCastle || Model.State == HeroState.Stuck)
                && Model.Inventory != null
                && Model.Inventory.HasReturnStone
                && TryUseReturnStoneToEntrance())
            {
                LogActivityTrace();
                return;
            }

            if (Model.State != HeroState.Exploring
                && Model.State != HeroState.SearchingKey
                && Model.State != HeroState.ReturningToDoor
                && Model.State != HeroState.OpeningDoor
                && Model.State != HeroState.ReturningToCastle)
            {
                LogStateChangeIfNeeded("idle");
                LogActivityTrace();
                return;
            }

            timeUntilNextStep -= Time.deltaTime;
            if (timeUntilNextStep > 0f)
            {
                LogStateChangeIfNeeded("waiting-step");
                LogActivityTrace();
                return;
            }

            explorer.Step();
            if (Model.Position != entrancePosition)
            {
                Model.MarkBlessingsLeftEntrance();
            }

            timeUntilNextStep = GetCurrentStepInterval();
            LogStateChangeIfNeeded("step");
            LogActivityTrace();
            RefreshVisibility();
            heroView.MoveTo(Model.Position);
            RefreshMemoryView();
        }

        private void Initialize(
            MazeGenerationResult result,
            Vector2Int startPosition,
            int displayNumber,
            string displayName,
            MazeRenderer mazeRenderer,
            HeroMemory memory,
            HeroMemoryView sharedMemoryView,
            GoldIngotManager goldIngotManager,
            HeroDeathTokenManager deathTokenManager,
            Action<HeroModel, int> entranceKnowledgeSync,
            Action<HeroModel, int, DungeonStairsModel> downStairsOpened,
            HeroExplorer.NearbyMobInteractionCellProvider nearbyMobInteractionCellProvider,
            HeroExplorationCoordinator explorationCoordinator,
            int statSeed)
        {
            grid = result.Grid;
            this.mazeRenderer = mazeRenderer;
            entrancePosition = startPosition;
            DisplayNumber = displayNumber;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? $"Рыцарь {displayNumber}" : displayName;
            Model = new HeroModel(startPosition, memory, statSeed);
            Model.SetIdentity(DisplayNumber, DisplayName);
            Model.SetDungeonLevel(result.LevelNumber);
            explorer = new HeroExplorer(result, Model, startPosition, displayNumber, mazeRenderer, goldIngotManager, deathTokenManager, entranceKnowledgeSync, downStairsOpened, nearbyMobInteractionCellProvider, explorationCoordinator);
            corpseExpired = false;
            corpseVisibilityRemaining = 0f;

            heroView = HeroView.Create(mazeRenderer, startPosition);
            heroView.SetController(this);
            heroView.transform.SetParent(transform, true);

            memoryView = sharedMemoryView;

            memory.Remember(startPosition);
            RefreshMemoryView();
            RefreshVisibility();
            timeUntilNextStep = GetCurrentStepInterval();
            ResetActivityLogging();
        }

        public void TransferToLevel(
            MazeGenerationResult result,
            Vector2Int startPosition,
            MazeRenderer mazeRenderer,
            GoldIngotManager goldIngotManager,
            HeroDeathTokenManager deathTokenManager,
            Action<HeroModel, int> entranceKnowledgeSync,
            Action<HeroModel, int, DungeonStairsModel> downStairsOpened,
            HeroExplorer.NearbyMobInteractionCellProvider nearbyMobInteractionCellProvider,
            HeroExplorationCoordinator explorationCoordinator)
        {
            if (Model == null || result == null)
            {
                return;
            }

            explorer?.ReleaseExplorationTarget("level transfer");
            grid = result.Grid;
            this.mazeRenderer = mazeRenderer;
            entrancePosition = startPosition;
            Model.SetDungeonLevel(result.LevelNumber);
            Model.Memory.Reset(result.Grid);
            Model.Memory.Remember(startPosition);
            Model.SetPosition(startPosition);
            Model.RestoreStamina();
            Model.ClearExpeditionBlessings();
            Model.SetState(HeroState.Exploring);
            Model.Visibility.Clear();
            explorer = new HeroExplorer(result, Model, startPosition, DisplayNumber, mazeRenderer, goldIngotManager, deathTokenManager, entranceKnowledgeSync, downStairsOpened, nearbyMobInteractionCellProvider, explorationCoordinator);
            corpseExpired = false;
            corpseVisibilityRemaining = 0f;
            explorationPaused = false;
            stateBeforeCombat = HeroState.Exploring;
            heroView.SetGridPositionImmediate(startPosition);
            RefreshVisibility();
            RefreshMemoryView();
            timeUntilNextStep = GetCurrentStepInterval();
            ResetActivityLogging();
            GameDebugLog.Info(
                "Hero",
                $"Hero #{DisplayNumber} transferred to dungeon level {result.LevelNumber}: start={GameDebugLog.Position(startPosition)}, hp={Model.HitPoints}/{Model.MaxHitPoints}, stamina={Model.Stamina}/{Model.MaxStamina}, memory={Model.Memory.RememberedCount}.");
        }

        private void StartCorpseVisibility()
        {
            explorationPaused = true;
            corpseExpired = false;
            corpseVisibilityRemaining = CorpseVisibilityDuration;
            RefreshVisibility();
            heroView.SetDefeated();
            GameDebugLog.Info(
                "Hero",
                $"Hero defeated at {GameDebugLog.Position(Model.Position)}. Corpse visibility remains for {CorpseVisibilityDuration:0.#} game seconds.");
        }

        private void UpdateCorpseVisibility()
        {
            if (corpseExpired)
            {
                return;
            }

            corpseVisibilityRemaining -= Time.deltaTime;
            if (corpseVisibilityRemaining > 0f)
            {
                return;
            }

            corpseVisibilityRemaining = 0f;
            corpseExpired = true;
            Model.Visibility.Clear();
            if (heroView != null)
            {
                heroView.SetVisible(false);
            }

            GameDebugLog.Info("Hero", $"Hero corpse disappeared at {GameDebugLog.Position(Model.Position)}.");
        }

        private void LogStateChangeIfNeeded(string reason)
        {
            if (Model == null || Model.State == lastLoggedState)
            {
                return;
            }

            GameDebugLog.Info(
                "Hero",
                $"Hero #{DisplayNumber} state {lastLoggedState} -> {Model.State} ({reason}), pos={GameDebugLog.Position(Model.Position)}, hp={Model.HitPoints}/{Model.MaxHitPoints}, stamina={Model.Stamina}/{Model.MaxStamina}, steps={Model.StepsTaken}, memory={Model.Memory.RememberedCount}, gold={Model.Gold}, xp={Model.Experience}/{Model.ExperienceForNextLevel}.");
            lastLoggedState = Model.State;
        }

        private void LogActivityTrace()
        {
            if (Model == null || !Model.IsAlive)
            {
                return;
            }

            if (!GameDebugLog.VerboseTrace)
            {
                return;
            }

            activityTraceTimer -= Time.deltaTime;
            if (activityTraceTimer > 0f)
            {
                return;
            }

            activityTraceTimer = ActivityTraceInterval;
            var stepDelta = Model.StepsTaken - lastTraceSteps;
            var memoryDelta = Model.Memory.RememberedCount - lastTraceMemory;
            if (stepDelta == 0 && memoryDelta == 0 && Model.State == lastLoggedState)
            {
                return;
            }

            GameDebugLog.Info(
                "Hero",
                $"Hero #{DisplayNumber} trace: state={Model.State}, pos={GameDebugLog.Position(Model.Position)}, steps={Model.StepsTaken}(+{stepDelta}), memory={Model.Memory.RememberedCount}(+{memoryDelta}), walls={Model.Memory.RememberedWallCount}, visible={Model.Visibility.VisibleCount}, hp={Model.HitPoints}/{Model.MaxHitPoints}, stamina={Model.Stamina}/{Model.MaxStamina}, gold={Model.Gold}, level={Model.Level}, xp={Model.Experience}/{Model.ExperienceForNextLevel}, blessing={Model.BlessingText}.");
            lastTraceSteps = Model.StepsTaken;
            lastTraceMemory = Model.Memory.RememberedCount;
        }

        private void ResetActivityLogging()
        {
            lastLoggedState = Model.State;
            lastTraceSteps = Model.StepsTaken;
            lastTraceMemory = Model.Memory.RememberedCount;
            activityTraceTimer = ActivityTraceInterval;
        }

        private void RefreshVisibility()
        {
            if (Model == null)
            {
                return;
            }

            Model.Visibility.Refresh(grid, Model.Position, Model.SightRange);
            RememberVisibleWalls();
        }

        private float GetCurrentStepInterval()
        {
            var interval = StepInterval;
            if (Model != null
                && Model.State == HeroState.ReturningToCastle
                && Model.HasBlessing(HeroBlessingType.VowOfReturn))
            {
                interval *= VowOfReturnStepIntervalMultiplier;
            }

            interval = ApplyFootwearSpeedBonus(interval);
            interval = ApplyVengeanceMovementBonus(interval);
            return ApplyFortifiedCellSpeedBonus(interval);
        }

        private float ApplyFootwearSpeedBonus(float interval)
        {
            var bonusPercent = Model != null ? Model.MoveSpeedBonusPercent : 0;
            if (bonusPercent == 0)
            {
                return interval;
            }

            return bonusPercent > 0
                ? interval / (1f + bonusPercent / 100f)
                : interval * (1f + Mathf.Abs(bonusPercent) / 100f);
        }

        private float ApplyVengeanceMovementBonus(float interval)
        {
            if (Model == null || Model.VengeanceQuest == null || !Model.VengeanceQuest.IsCompleted)
            {
                return interval;
            }

            if (Model.State == HeroState.ReturningToCastle
                && Model.Inventory != null
                && Model.Inventory.HasDeathToken
                && Model.HasCompletedVengeance(HeroVengeanceKind.CarriedName))
            {
                interval /= VengeanceTokenReturnSpeedMultiplier;
            }

            return interval;
        }

        private float ApplyFortifiedCellSpeedBonus(float interval)
        {
            if (Model == null || fortifiedCellProvider == null || !fortifiedCellProvider.Invoke(Model.Position))
            {
                return interval;
            }

            return interval / FortifiedCellSpeedMultiplier;
        }

        private void RememberVisibleWalls()
        {
            if (grid == null || Model == null || Model.Memory == null)
            {
                return;
            }

            foreach (var position in Model.Visibility.VisibleCells)
            {
                Model.Memory.RememberWall(position);
            }
        }

        private void RefreshMemoryView()
        {
            if (Model == null || memoryView == null)
            {
                return;
            }

            foreach (var position in Model.Memory.RememberedCells)
            {
                memoryView.ShowRemembered(position);
            }
        }
    }
}
