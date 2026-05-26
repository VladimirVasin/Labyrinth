using System.Collections.Generic;
using Labyrinth.Base;
using Labyrinth.CameraSystem;
using Labyrinth.Combat;
using Labyrinth.Hero;
using Labyrinth.Maze;
using Labyrinth.Mobs;
using Labyrinth.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Labyrinth.Core
{
    public sealed partial class GameBootstrap : MonoBehaviour
    {
        private enum GameState
        {
            MainMenu,
            Generating,
            Playing,
            BaseHudOpen,
            PauseMenu
        }

        private MazeGenerator generator;
        private MazeRenderer mazeRenderer;
        private MazeTerrain mazeTerrain;
        private LabyrinthCameraController cameraController;
        private TimeScaleController timeScaleController;
        private ResourceWallet resources;
        private BaseDevelopment baseDevelopment;
        private BaseAmbienceController baseAmbience;
        private CityAmbienceController cityAmbience;
        private ResourceProductionController productionController;
        private HeroConsumableAutomation consumableAutomation;
        private GoldIngotManager goldIngotManager;
        private TaxCollectorController taxCollectorController;
        private DungeonFortificationController dungeonFortificationController;
        private MineConstructionController mineConstructionController;
        private MobManager mobManager;
        private CombatController combatController;
        private MainMenuUI mainMenu;
        private BaseHudView baseHud;
        private HeroHudView heroHud;
        private MobHudView mobHud;
        private BuildingMicroHudView buildingMicroHud;
        private ObjectMicroHudView objectMicroHud;
        private VictoryHudView victoryHud;
        private TopHudView topHud;
        private MapHudView mapHud;
        private DungeonLevelHudView levelHud;
        private FogOfWarView fogOfWarView;
        private readonly Color normalAmbientLight = new Color(0.7f, 0.72f, 0.78f);
        private Camera mainCamera;
        private CameraClearFlags normalCameraClearFlags;
        private Color normalCameraBackgroundColor;
        private MazeGenerationResult currentMaze;
        private BaseView currentBase;
        private HeroMemory cartographerMemory;
        private HeroMemoryView sharedHeroMemoryView;
        private HeroVisibilityView selectedHeroVisibilityView;
        private HeroVisibilityDisplayMode visibilityDisplayMode = HeroVisibilityDisplayMode.Lighting;
        private readonly List<HeroController> heroes = new List<HeroController>();
        private readonly List<HeroController> fallenHeroes = new List<HeroController>();
        private readonly List<HeroController> visibilityHeroes = new List<HeroController>();
        private HeroController selectedHero;
        private int nextHeroNumber = 1;
        private bool victoryAchieved;
        private bool adventureMusicStarted;
        private MazeGenerationSettings rootGenerationSettings;
        private MazeGenerationResult levelOneMaze;
        private MazeGenerationResult levelTwoMaze;
        private int currentDungeonLevel = 1;
        private int unlockedDungeonLevel = 1;
        private GameState state;
        private GameState stateBeforePause = GameState.Playing;

        private void Awake()
        {
            resources = ResourceWallet.CreateDefault();
            baseDevelopment = new BaseDevelopment();
            generator = new MazeGenerator();
            mazeTerrain = gameObject.AddComponent<MazeTerrain>();
            mazeRenderer = gameObject.AddComponent<MazeRenderer>();
            baseAmbience = gameObject.AddComponent<BaseAmbienceController>();
            cityAmbience = gameObject.AddComponent<CityAmbienceController>();
            cameraController = gameObject.AddComponent<LabyrinthCameraController>();
            timeScaleController = gameObject.AddComponent<TimeScaleController>();
            productionController = gameObject.AddComponent<ResourceProductionController>();
            productionController.Configure(resources, baseDevelopment, baseAmbience, mazeRenderer);
            consumableAutomation = new HeroConsumableAutomation(resources, baseDevelopment, mazeRenderer);
            goldIngotManager = gameObject.AddComponent<GoldIngotManager>();
            goldIngotManager.Configure(resources);
            taxCollectorController = gameObject.AddComponent<TaxCollectorController>();
            taxCollectorController.Configure(resources, baseDevelopment, mazeRenderer);
            dungeonFortificationController = gameObject.AddComponent<DungeonFortificationController>();
            dungeonFortificationController.Configure(resources, mazeRenderer);
            mineConstructionController = gameObject.AddComponent<MineConstructionController>();
            mineConstructionController.Configure(resources, baseDevelopment, mazeRenderer, baseAmbience);
            mobManager = gameObject.AddComponent<MobManager>();
            combatController = gameObject.AddComponent<CombatController>();
            combatController.MobDefeated += HandleMobDefeated;
            mainMenu = gameObject.AddComponent<MainMenuUI>();
            baseHud = gameObject.AddComponent<BaseHudView>();
            heroHud = gameObject.AddComponent<HeroHudView>();
            heroHud.Configure(() => heroes, () => selectedHero, SelectHero);
            mobHud = gameObject.AddComponent<MobHudView>();
            buildingMicroHud = gameObject.AddComponent<BuildingMicroHudView>();
            buildingMicroHud.Configure(GetBuildingMicroHudLevel, GetBuildingMicroHudServices, HandleBuildingMicroHudServiceAction);
            objectMicroHud = gameObject.AddComponent<ObjectMicroHudView>();
            victoryHud = gameObject.AddComponent<VictoryHudView>();
            topHud = gameObject.AddComponent<TopHudView>();
            topHud.Configure(resources, () => heroes.Count, () => baseDevelopment.MaxHeroCount);
            mapHud = gameObject.AddComponent<MapHudView>();
            mapHud.Configure(GetCurrentMaze, GetDisplayedKnowledgeMemory, BuildMapVisibleCells, IsCommonMapUnlocked);
            levelHud = gameObject.AddComponent<DungeonLevelHudView>();
            levelHud.Configure(() => currentDungeonLevel, () => unlockedDungeonLevel, SwitchDungeonLevel);
            fogOfWarView = gameObject.AddComponent<FogOfWarView>();
            fogOfWarView.Configure(mazeRenderer);

            mainCamera = EnsureCamera();
            normalCameraClearFlags = mainCamera.clearFlags;
            normalCameraBackgroundColor = mainCamera.backgroundColor;
            EnsureLight();
            RenderSettings.ambientLight = normalAmbientLight;

            state = GameState.MainMenu;
            cameraController.SetInteractionEnabled(false);
            SetGameHudVisible(false);
            mainMenu.Show(StartGame);
            GameAudioController.StartMenuMusic();
        }

        private void Update()
        {
            if (WasEscapePressed())
            {
                if (TryCloseOpenRuntimeHud())
                {
                    return;
                }

                if (IsMineSelectionActive())
                {
                    CancelMineSelection();
                    return;
                }

                if (IsDungeonFortificationSelectionActive())
                {
                    CancelDungeonFortificationSelection();
                    return;
                }

                TogglePauseMenu();
                return;
            }

            if (state == GameState.PauseMenu)
            {
                return;
            }

            if (state == GameState.Playing && currentMaze == null)
            {
                RecoverFromMissingMazeState();
                return;
            }

            HandleVisibilityModeHotkeys();
            HandleMapHotkey();
            RetireDefeatedHeroes();
            DestroyExpiredFallenHeroes();

            if (TryCloseOpenRuntimeHudFromOutsideClick())
            {
                return;
            }

            if (state == GameState.BaseHudOpen && !baseHud.IsVisible)
            {
                state = GameState.Playing;
                cameraController.SetInteractionEnabled(true);
            }

            RefreshSelectedHeroVisibility();
            RefreshMapSelectionMarker();
            UpdateMineConstructionHover();
            UpdateDungeonFortificationHover();

            if (state != GameState.Playing)
            {
                return;
            }

            if (currentDungeonLevel == 1)
            {
                consumableAutomation.Update(heroes, currentMaze.EntrancePosition);
                dungeonFortificationController.UpdateFortification();
                mineConstructionController.UpdateConstruction();
            }
            if (!victoryAchieved)
            {
                var visibilityHeroesForRespawn = BuildVisibilityHeroes();
                mobManager.UpdateRespawns(
                    BuildRespawnBlockedCells(visibilityHeroesForRespawn),
                    visibilityDisplayMode == HeroVisibilityDisplayMode.Lighting,
                    heroes);
            }

            TryStartHeroEncounter();

            if (!TryReadPrimaryClick(out var screenPosition))
            {
                return;
            }

            if (IsPointerInsideRuntimeHud(screenPosition))
            {
                return;
            }

            var ray = mainCamera.ScreenPointToRay(screenPosition);
            if (!Physics.Raycast(ray, out var hit, 500f))
            {
                HideWorldHuds();
                return;
            }

            if (TryHandleMineSelection(hit))
            {
                return;
            }

            if (TryHandleDungeonFortificationSelection(hit))
            {
                return;
            }

            if (TrySelectHeroOrMobFromHit(hit))
            {
                return;
            }

            if (combatController.IsActive)
            {
                return;
            }

            var objectTarget = hit.collider.GetComponentInParent<ObjectMicroHudTarget>();
            if (objectTarget != null && IsObjectHudTargetVisible(objectTarget))
            {
                baseHud.Hide();
                buildingMicroHud.Hide();
                mobHud.Hide();
                ClearSelectedMob();
                objectMicroHud.Show(objectTarget);
                return;
            }

            var buildingView = hit.collider.GetComponentInParent<BuildingView>();
            if (buildingView == null)
            {
                HideWorldHuds();
                return;
            }

            ShowBuildingHud(buildingView);
        }

        private bool TryCloseOpenRuntimeHudFromOutsideClick()
        {
            if (!HasOpenClosableRuntimeHud() || !TryReadPrimaryClick(out var screenPosition))
            {
                return false;
            }

            if (IsPointerInsideRuntimeHud(screenPosition))
            {
                return false;
            }

            CloseOpenRuntimeHud();
            return true;
        }

        private bool TryCloseOpenRuntimeHud()
        {
            if (!HasOpenClosableRuntimeHud())
            {
                return false;
            }

            CloseOpenRuntimeHud();
            return true;
        }

        private bool HasOpenClosableRuntimeHud()
        {
            return baseHud.IsVisible
                || heroHud.IsVisible
                || buildingMicroHud.IsVisible
                || mobHud.IsVisible
                || objectMicroHud.IsVisible
                || victoryHud.IsVisible
                || mapHud.IsExpanded;
        }

        private void CloseOpenRuntimeHud()
        {
            var wasBaseHudOpen = baseHud.IsVisible;

            baseHud.Hide();
            heroHud.Hide();
            buildingMicroHud.Hide();
            objectMicroHud.Hide();
            mobHud.Hide();
            victoryHud.Hide();
            mapHud.HideExpanded();
            ClearSelectedMob();

            if (wasBaseHudOpen && state == GameState.BaseHudOpen)
            {
                state = GameState.Playing;
                cameraController.SetInteractionEnabled(true);
            }
        }

        private bool IsPointerInsideRuntimeHud(Vector2 screenPosition)
        {
            return baseHud.ContainsScreenPoint(screenPosition)
                || heroHud.ContainsScreenPoint(screenPosition)
                || buildingMicroHud.ContainsScreenPoint(screenPosition)
                || mobHud.ContainsScreenPoint(screenPosition)
                || objectMicroHud.ContainsScreenPoint(screenPosition)
                || victoryHud.ContainsScreenPoint(screenPosition)
                || mapHud.ContainsScreenPoint(screenPosition);
        }

        private void HideWorldHuds()
        {
            buildingMicroHud.Hide();
            mobHud.Hide();
            objectMicroHud.Hide();
            ClearSelectedMob();
        }

        private bool IsObjectHudTargetVisible(ObjectMicroHudTarget target)
        {
            if (target == null || currentMaze == null || currentMaze.Grid == null)
            {
                return true;
            }

            if (visibilityDisplayMode != HeroVisibilityDisplayMode.Lighting || !currentMaze.Grid.InBounds(target.GridPosition))
            {
                return true;
            }

            return BuildLightingVisibleCells(BuildVisibilityHeroes()).Contains(target.GridPosition);
        }

        private void ShowBuildingHud(BuildingView buildingView)
        {
            mobHud.Hide();
            objectMicroHud.Hide();
            ClearSelectedMob();

            if (buildingView.Type != BuildingType.Castle)
            {
                baseHud.Hide();
                buildingMicroHud.Show(buildingView);
                return;
            }

            buildingMicroHud.Hide();
            var baseView = buildingView.GetComponent<BaseView>();
            if (baseView == null || baseView != currentBase)
            {
                return;
            }

            baseHud.Show(
                baseView.GenerationResult,
                BuildFarmFromBase,
                GetFarmStatus,
                CanBuildFarm,
                GetFarmCost,
                BuildLumberjackCampFromBase,
                GetLumberjackCampStatus,
                CanBuildLumberjackCamp,
                GetLumberjackCampCost,
                BuildAlchemistShopFromBase,
                GetAlchemistShopStatus,
                CanBuildAlchemistShop,
                GetAlchemistShopCost,
                BuildTavernFromBase,
                GetTavernStatus,
                CanBuildTavern,
                GetTavernCost,
                BuildForgeFromBase,
                GetForgeStatus,
                CanBuildForge,
                GetForgeCost,
                BuildInfirmaryFromBase,
                GetInfirmaryStatus,
                CanBuildInfirmary,
                GetInfirmaryCost,
                BuildCartographerHouseFromBase,
                GetCartographerHouseStatus,
                CanBuildCartographerHouse,
                GetCartographerHouseCost,
                BuildChapelFromBase,
                GetChapelStatus,
                CanBuildChapel,
                GetChapelCost,
                BuildMinersGuildFromBase,
                GetMinersGuildStatus,
                CanBuildMinersGuild,
                GetMinersGuildCost,
                BuildMarketFromBase,
                GetMarketStatus,
                CanBuildMarket,
                GetMarketCost,
                GetHeroHouseStatus,
                CreateHeroFromBase,
                CanCreateHero,
                GetHeroCost,
                GetMineStatus,
                CanStartMineSelection,
                BeginMineSelection,
                GetBuildingUpgradeStatus,
                CanUpgradeBuilding,
                GetBuildingUpgradeCost,
                UpgradeBuildingFromBase);
            cameraController.SetInteractionEnabled(false);
            state = GameState.BaseHudOpen;
        }

        private void StartGame(MazeGenerationSettings settings)
        {
            if (settings == null)
            {
                GameDebugLog.Error("Game", "Start requested without generation settings.");
                return;
            }

            GameDebugLog.Info(
                "Game",
                $"Start requested: size={settings.Width}x{settings.Height}, seed={settings.Seed}, preset={settings.Preset}");
            state = GameState.Generating;
            mainMenu.Hide();
            GameAudioController.StopMenuMusic();
            GameAudioController.StopWorldMusic();
            timeScaleController.ResetToNormal();
            SetGameHudVisible(false);
            mapHud.HideExpanded();
            baseHud.Hide();
            heroHud.Hide();
            mobHud.Hide();
            buildingMicroHud.Hide();
            objectMicroHud.Hide();
            victoryHud.Hide();
            cameraController.SetInteractionEnabled(false);
            mazeTerrain.Clear();
            mazeRenderer.Clear();
            fogOfWarView.Clear();
            goldIngotManager.Clear();
            taxCollectorController.Clear();
            dungeonFortificationController.Clear();
            mineConstructionController.Clear();
            baseAmbience.Clear();
            cityAmbience.Clear();
            DestroyHeroes();
            DestroyHeroMemoryView();
            DestroyHeroVisibilityView();
            cartographerMemory = null;
            levelOneCartographerMemory = null;
            levelTwoCartographerMemory = null;
            resources.ResetToDefault();
            baseDevelopment.Reset();
            productionController.ResetProgress();
            victoryAchieved = false;
            adventureMusicStarted = false;
            rootGenerationSettings = settings;
            visibilityDisplayMode = HeroVisibilityDisplayMode.Lighting;
            currentDungeonLevel = 1;
            unlockedDungeonLevel = 1;
            levelOneMaze = null;
            levelTwoMaze = null;

            currentMaze = generator.Generate(settings);
            if (!MazeValidation.ValidateGeneratedMaze(currentMaze, out var error))
            {
                GameDebugLog.Error("Maze", $"Generation failed: {error}");
                state = GameState.MainMenu;
                mainMenu.Show(StartGame);
                GameAudioController.StartMenuMusic();
                return;
            }

            LogMazeSummary(currentMaze);
            levelOneMaze = currentMaze;
            mazeTerrain.Render(currentMaze, mazeRenderer.CellSize);
            mazeTerrain.SetVisualVisible(true);
            currentBase = mazeRenderer.Render(currentMaze);
            baseAmbience.Initialize(currentMaze, mazeRenderer);
            cityAmbience.Initialize(currentMaze, mazeRenderer);
            taxCollectorController.Initialize(currentMaze);
            cartographerMemory = new HeroMemory(currentMaze.Grid);
            cartographerMemory.Remember(currentMaze.EntrancePosition);
            levelOneCartographerMemory = cartographerMemory;
            levelTwoCartographerMemory = null;
            dungeonFortificationController.Initialize(currentMaze, cartographerMemory);
            mineConstructionController.Initialize(currentMaze);
            RefreshAllBuildingUpgradeVisuals();
            sharedHeroMemoryView = HeroMemoryView.Create(mazeRenderer);
            sharedHeroMemoryView.transform.SetParent(transform, true);
            selectedHeroVisibilityView = HeroVisibilityView.Create(mazeRenderer);
            selectedHeroVisibilityView.transform.SetParent(transform, true);
            selectedHeroVisibilityView.SetMode(visibilityDisplayMode);
            mobManager.Spawn(currentMaze, mazeRenderer);
            RefreshCentralExitSeal();
            var mobPositions = new HashSet<Vector2Int>();
            mobManager.CollectOccupiedPositions(mobPositions);
            goldIngotManager.Spawn(currentMaze, mazeRenderer, mobPositions);
            cameraController.Focus(mainCamera, currentMaze, mazeRenderer.CellSize, true);
            state = GameState.Playing;
            SetGameHudVisible(true);
            GameDebugLog.Info("Game", "Generation completed and play mode started. Music waits for first hero creation.");
        }

        private void TogglePauseMenu()
        {
            if (state == GameState.MainMenu || state == GameState.Generating)
            {
                return;
            }

            if (state == GameState.PauseMenu)
            {
                ClosePauseMenu();
                return;
            }

            OpenPauseMenu();
        }

        private void OpenPauseMenu()
        {
            stateBeforePause = state;
            baseHud.Hide();
            heroHud.Hide();
            mobHud.Hide();
            buildingMicroHud.Hide();
            objectMicroHud.Hide();
            SetGameHudVisible(false);
            cameraController.SetInteractionEnabled(false);
            timeScaleController.Pause();
            mainMenu.Show(StartGame, true);
            GameAudioController.PlayUi(GameSfx.MenuOpen);
            state = GameState.PauseMenu;
            GameDebugLog.Info("Game", $"Pause menu opened from state={stateBeforePause}.");
        }

        private void ClosePauseMenu()
        {
            mainMenu.Hide();
            GameAudioController.PlayUi(GameSfx.MenuClose);
            timeScaleController.ResumePaused();
            state = stateBeforePause == GameState.BaseHudOpen ? GameState.Playing : stateBeforePause;
            SetGameHudVisible(true);
            cameraController.SetInteractionEnabled(state == GameState.Playing);
            GameDebugLog.Info("Game", $"Pause menu closed, restored state={state}.");
        }

        private void ReturnToMainMenu()
        {
            baseHud.Hide();
            heroHud.Hide();
            mobHud.Hide();
            buildingMicroHud.Hide();
            objectMicroHud.Hide();
            victoryHud.Hide();
            GameAudioController.StopMenuMusic();
            GameAudioController.StopWorldMusic();
            combatController.CancelCombat();
            mobManager.Clear();
            goldIngotManager.Clear();
            taxCollectorController.Clear();
            dungeonFortificationController.Clear();
            mineConstructionController.Clear();
            cameraController.SetInteractionEnabled(false);
            mazeTerrain.Clear();
            mazeRenderer.Clear();
            fogOfWarView.Clear();
            baseAmbience.Clear();
            cityAmbience.Clear();
            DestroyHeroes();
            DestroyHeroMemoryView();
            DestroyHeroVisibilityView();
            currentMaze = null;
            currentBase = null;
            cartographerMemory = null;
            levelOneCartographerMemory = null;
            levelTwoCartographerMemory = null;
            rootGenerationSettings = null;
            levelOneMaze = null;
            levelTwoMaze = null;
            currentDungeonLevel = 1;
            unlockedDungeonLevel = 1;
            victoryAchieved = false;
            adventureMusicStarted = false;
            ApplyVisibilityEnvironment(HeroVisibilityDisplayMode.Schematic);
            timeScaleController.ResetToNormal();
            SetGameHudVisible(false);
            mapHud.HideExpanded();
            state = GameState.MainMenu;
            mainMenu.Show(StartGame);
            GameAudioController.PlayUi(GameSfx.MenuOpen, 0.75f);
            GameAudioController.StartMenuMusic();
            GameDebugLog.Info("Game", "Returned to main menu and cleared runtime state.");
        }

        private void RecoverFromMissingMazeState()
        {
            GameDebugLog.Error("Game", "Playing state had no generated maze. Returning to main menu to avoid null runtime updates.");
            ReturnToMainMenu();
        }

        private void HandleMobDefeated(MobController defeatedMob)
        {
            var defeatedBoss = defeatedMob != null && defeatedMob.Model != null && defeatedMob.Model.IsBoss;
            var defeatedMiniBoss = defeatedMob != null && defeatedMob.Model != null && defeatedMob.Model.IsMiniBoss;
            mobManager.Remove(defeatedMob);
            if (defeatedMiniBoss)
            {
                UnsealCentralExitDoorAfterMiniBoss();
                mobHud.Hide();
            }

            if (!defeatedBoss || victoryAchieved)
            {
                return;
            }

            mobHud.Hide();
            victoryHud.Show("Босс уровня повержен. Рыцарь получил ключ спуска.");
            if (defeatedMob != null && defeatedMob.Model != null)
            {
                GameAudioController.Play(GameSfx.Victory, mazeRenderer.GridToWorld(defeatedMob.Position));
            }

            GameDebugLog.Info("Dungeon", $"Boss defeated on level {currentDungeonLevel}. Descent key is now available to the victorious hero.");
        }

        private void SetGameHudVisible(bool visible)
        {
            if (topHud != null)
            {
                topHud.Visible = visible;
            }

            if (timeScaleController != null)
            {
                timeScaleController.Visible = visible;
            }

            if (mapHud != null)
            {
                mapHud.Visible = visible;
            }

            if (levelHud != null)
            {
                levelHud.Visible = visible;
            }
        }

        private void BuildFarmFromBase()
        {
            if (currentMaze == null)
            {
                return;
            }

            var cost = GetFarmCost();
            if (!resources.CanAfford(cost))
            {
                baseDevelopment.ReportBuildBlocked($"нужно {cost.Format()}");
                GameDebugLog.Warning(
                    "Base",
                    $"Farm build blocked: gold={resources.Gold}, wood={resources.Wood}, required={cost.Format()}");
                return;
            }

            if (!baseDevelopment.TryBuildFarm(currentMaze, out var farmPosition))
            {
                var blockMessage = baseDevelopment.LastBuildMessage;
                baseDevelopment.ReportBuildBlocked($"ферма: {blockMessage}");
                GameDebugLog.Warning("Base", $"Farm build blocked: {blockMessage}");
                return;
            }

            if (resources.TrySpend(cost))
            {
                mazeRenderer.RenderFarm(farmPosition);
                RefreshAllBuildingUpgradeVisuals();
                baseAmbience.RegisterBuilding(BuildingType.Farm, farmPosition);
                cityAmbience.RegisterBuilding(BuildingType.Farm, farmPosition);
                SyncPeasantHuts();
                GameAudioController.Play(GameSfx.Build, mazeRenderer.GridToWorld(farmPosition));
                GameDebugLog.Info(
                    "Base",
                    $"Farm built at {GameDebugLog.Position(farmPosition)}. farms={baseDevelopment.FarmCount}, cost={cost.Format()}, goldLeft={resources.Gold}, woodLeft={resources.Wood}, foodPerSecond={baseDevelopment.FoodPerTimeUnit}");
            }
        }

        private void BuildAlchemistShopFromBase()
        {
            if (currentMaze == null)
            {
                return;
            }

            var cost = GetAlchemistShopCost();
            if (!resources.CanAfford(cost))
            {
                baseDevelopment.ReportBuildBlocked($"лавка алхимика: нужно {cost.Format()}");
                GameDebugLog.Warning(
                    "Base",
                    $"Alchemist shop build blocked: gold={resources.Gold}, wood={resources.Wood}, required={cost.Format()}");
                return;
            }

            if (!baseDevelopment.TryBuildAlchemistShop(currentMaze, out var shopPosition))
            {
                var blockMessage = baseDevelopment.LastBuildMessage;
                baseDevelopment.ReportBuildBlocked($"лавка алхимика: {blockMessage}");
                GameDebugLog.Warning("Base", $"Alchemist shop build blocked: {blockMessage}");
                return;
            }

            if (resources.TrySpend(cost))
            {
                mazeRenderer.RenderAlchemistShop(shopPosition);
                RefreshAllBuildingUpgradeVisuals();
                baseAmbience.RegisterBuilding(BuildingType.AlchemistShop, shopPosition);
                cityAmbience.RegisterBuilding(BuildingType.AlchemistShop, shopPosition);
                SyncPeasantHuts();
                RefreshSelectedHeroVisibility();
                GameAudioController.Play(GameSfx.Build, mazeRenderer.GridToWorld(shopPosition));
                GameDebugLog.Info(
                    "Base",
                    $"Alchemist shop built at {GameDebugLog.Position(shopPosition)}. buildCost={cost.Format()}, goldLeft={resources.Gold}, woodLeft={resources.Wood}, potionCost={BaseDevelopment.HealthPotionGoldCost}");
            }
        }

        private void BuildTavernFromBase()
        {
            if (currentMaze == null)
            {
                return;
            }

            var cost = GetTavernCost();
            if (!resources.CanAfford(cost))
            {
                baseDevelopment.ReportBuildBlocked($"харчевня: нужно {cost.Format()}");
                GameDebugLog.Warning(
                    "Base",
                    $"Tavern build blocked: gold={resources.Gold}, wood={resources.Wood}, required={cost.Format()}");
                return;
            }

            if (!baseDevelopment.TryBuildTavern(currentMaze, out var tavernPosition))
            {
                var blockMessage = baseDevelopment.LastBuildMessage;
                baseDevelopment.ReportBuildBlocked($"харчевня: {blockMessage}");
                GameDebugLog.Warning("Base", $"Tavern build blocked: {blockMessage}");
                return;
            }

            if (resources.TrySpend(cost))
            {
                mazeRenderer.RenderTavern(tavernPosition);
                RefreshAllBuildingUpgradeVisuals();
                baseAmbience.RegisterBuilding(BuildingType.Tavern, tavernPosition);
                cityAmbience.RegisterBuilding(BuildingType.Tavern, tavernPosition);
                SyncPeasantHuts();
                RefreshSelectedHeroVisibility();
                GameAudioController.Play(GameSfx.Build, mazeRenderer.GridToWorld(tavernPosition));
                GameDebugLog.Info(
                    "Base",
                    $"Tavern built at {GameDebugLog.Position(tavernPosition)}. buildCost={cost.Format()}, goldLeft={resources.Gold}, woodLeft={resources.Wood}, ration={BaseDevelopment.RationFoodCost} food -> {BaseDevelopment.RationGoldCost} gold.");
            }
        }

        private bool CanBuildFarm()
        {
            return currentMaze != null && resources.CanAfford(GetFarmCost());
        }

        private bool CanBuildAlchemistShop()
        {
            return currentMaze != null
                && !baseDevelopment.HasAlchemistShop
                && resources.CanAfford(GetAlchemistShopCost());
        }

        private bool CanBuildTavern()
        {
            return currentMaze != null
                && !baseDevelopment.HasTavern
                && resources.CanAfford(GetTavernCost());
        }

        private string GetFarmStatus()
        {
            var status = $"{baseDevelopment.FarmCount} (ур. {baseDevelopment.FarmLevel}, +{baseDevelopment.FarmUnitsPerTick}/{ResourceProductionController.FarmProductionIntervalSeconds:0.#} сек, караван {baseDevelopment.FarmBatchCapacity}, постройка {GetFarmCost().Format()})";
            if (baseDevelopment.LastBuildMessage.Contains("ферм")
                || baseDevelopment.LastBuildMessage.StartsWith("нужно"))
            {
                status += $", {baseDevelopment.LastBuildMessage}";
            }

            return status;
        }

        private string GetAlchemistShopStatus()
        {
            var status = baseDevelopment.HasAlchemistShop
                ? $"построена ({baseDevelopment.AlchemistShopPosition.x}, {baseDevelopment.AlchemistShopPosition.y}), ур. {baseDevelopment.AlchemistShopLevel}, зелье {baseDevelopment.HealthPotionHealAmount} HP, запас {baseDevelopment.HealthPotionMaxCount}"
                : $"не построена, постройка {GetAlchemistShopCost().Format()}, зелье {BaseDevelopment.HealthPotionGoldCost} зол.";
            if (baseDevelopment.LastBuildMessage.Contains("лавка алхимика"))
            {
                status += $", {baseDevelopment.LastBuildMessage}";
            }

            return status;
        }

        private string GetTavernStatus()
        {
            var status = baseDevelopment.HasTavern
                ? $"построена ({baseDevelopment.TavernPosition.x}, {baseDevelopment.TavernPosition.y}), ур. {baseDevelopment.TavernLevel}, паёк +{baseDevelopment.RationStaminaRestore} выносл., запас {baseDevelopment.RationMaxCount}"
                : $"не построена, постройка {GetTavernCost().Format()}, паёк {BaseDevelopment.RationFoodCost} пищи -> {BaseDevelopment.RationGoldCost} зол.";
            if (baseDevelopment.LastBuildMessage.Contains("харчев"))
            {
                status += $", {baseDevelopment.LastBuildMessage}";
            }

            return status;
        }

        private void DestroyHeroMemoryView()
        {
            if (sharedHeroMemoryView == null)
            {
                return;
            }

            Destroy(sharedHeroMemoryView.gameObject);
            sharedHeroMemoryView = null;
        }

        private void DestroyHeroVisibilityView()
        {
            if (selectedHeroVisibilityView == null)
            {
                return;
            }

            Destroy(selectedHeroVisibilityView.gameObject);
            selectedHeroVisibilityView = null;
        }

        private Camera EnsureCamera()
        {
            var existingCamera = Camera.main;
            if (existingCamera != null)
            {
                return existingCamera;
            }

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var createdCamera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            return createdCamera;
        }

        private static void EnsureLight()
        {
            if (FindAnyObjectByType<Light>() != null)
            {
                return;
            }

            var lightObject = new GameObject("Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 2.1f;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private static void LogMazeSummary(MazeGenerationResult result)
        {
            GameDebugLog.Info(
                "Maze",
                $"Generated {result.Grid.Width}x{result.Grid.Height}, seed={result.Settings.Seed}, entrance={GameDebugLog.Position(result.EntrancePosition)}, base={GameDebugLog.Position(result.BasePosition)}, centralRoom={BuildCentralRoomSummary(result)}, caves={result.Caves.Count}, chests={result.Chests.Count}, oreDeposits={result.OreDeposits.Count}{BuildCaveSummary(result)}");
        }

        private static string BuildCentralRoomSummary(MazeGenerationResult result)
        {
            var room = result.CentralRoom;
            if (!room.IsValid)
            {
                return "none";
            }

            var key = result.CentralRoomKey == null ? "none" : GameDebugLog.Position(result.CentralRoomKey.Position);
            return $"min={GameDebugLog.Position(room.Min)} max={GameDebugLog.Position(room.Max)} entranceDoor={GameDebugLog.Position(room.EntranceExternalPosition)}->{GameDebugLog.Position(room.EntrancePosition)} exitDoor={GameDebugLog.Position(room.ExitPosition)}->{GameDebugLog.Position(room.ExitExternalPosition)} key={key}";
        }

        private static string BuildCaveSummary(MazeGenerationResult result)
        {
            if (result.Caves.Count == 0)
            {
                return string.Empty;
            }

            var summary = ": ";
            for (var i = 0; i < result.Caves.Count; i++)
            {
                var cave = result.Caves[i];
                if (i > 0)
                {
                    summary += "; ";
                }

                summary += $"#{i + 1} center={GameDebugLog.Position(cave.Center)} entrance={GameDebugLog.Position(cave.EntrancePosition)}";
            }

            return summary;
        }

        private static bool TryReadPrimaryClick(out Vector2 screenPosition)
        {
            screenPosition = Vector2.zero;

            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            {
                return false;
            }

            screenPosition = Mouse.current.position.ReadValue();
            return true;
        }

        private static bool WasEscapePressed()
        {
            return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        }
    }
}
