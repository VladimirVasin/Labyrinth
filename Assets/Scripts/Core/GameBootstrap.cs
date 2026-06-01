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
        private TerrainDecorationController terrainDecorations;
        private LabyrinthCameraController cameraController;
        private TimeScaleController timeScaleController;
        private ResourceWallet resources;
        private BaseDevelopment baseDevelopment;
        private BaseConstructionController baseConstructionController;
        private BaseAmbienceController baseAmbience;
        private HeroHouseFundCourierController houseFundCouriers;
        private CityAmbienceController cityAmbience;
        private ResourceProductionController productionController;
        private HeroConsumableAutomation consumableAutomation;
        private GoldIngotManager goldIngotManager;
        private HeroDeathTokenManager deathTokenManager;
        private TaxCollectorController taxCollectorController;
        private DungeonFortificationController dungeonFortificationController;
        private MineConstructionController mineConstructionController;
        private HeroGuildQuestController heroGuildQuestController;
        private HeroExplorationCoordinator explorationCoordinator;
        private MobManager mobManager;
        private CombatController combatController;
        private MainMenuUI mainMenu;
        private BaseHudView baseHud;
        private HeroHudView heroHud;
        private MobHudView mobHud;
        private BuildingMicroHudView buildingMicroHud;
        private HeroLineageHudView heroLineageHud;
        private ObjectMicroHudView objectMicroHud;
        private VictoryHudView victoryHud;
        private TopHudView topHud;
        private MapHudView mapHud;
        private DungeonLevelHudView levelHud;
        private FogOfWarView fogOfWarView;
        private readonly Color normalAmbientLight = new Color(0.58f, 0.61f, 0.68f);
        private Camera mainCamera;
        private CameraClearFlags normalCameraClearFlags;
        private Color normalCameraBackgroundColor;
        private MazeGenerationResult currentMaze;
        private BaseView currentBase;
        private HeroMemory cartographerMemory;
        private HeroMemoryView sharedHeroMemoryView;
        private HeroVisibilityView selectedHeroVisibilityView;
        private BuildingView heroesGuildView;
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
        private bool debugBuildingMode;

        private void Awake()
        {
            Application.runInBackground = true;
            resources = ResourceWallet.CreateDefault();
            baseDevelopment = new BaseDevelopment();
            generator = new MazeGenerator();
            mazeTerrain = gameObject.AddComponent<MazeTerrain>();
            mazeRenderer = gameObject.AddComponent<MazeRenderer>();
            terrainDecorations = gameObject.AddComponent<TerrainDecorationController>();
            baseDevelopment.ConfigurePlacementBlocker((position, footprintRadius) => terrainDecorations.BlocksBuilding(position, footprintRadius));
            baseConstructionController = gameObject.AddComponent<BaseConstructionController>();
            baseConstructionController.Configure(mazeRenderer, terrainDecorations, () => currentMaze, HandleBaseConstructionCompleted);
            baseAmbience = gameObject.AddComponent<BaseAmbienceController>();
            baseAmbience.Configure(terrainDecorations);
            baseAmbience.RoadCompleted += HandleBaseRoadCompleted;
            houseFundCouriers = gameObject.AddComponent<HeroHouseFundCourierController>();
            houseFundCouriers.Configure(mazeRenderer, baseAmbience);
            cityAmbience = gameObject.AddComponent<CityAmbienceController>();
            cityAmbience.Configure(terrainDecorations);
            cameraController = gameObject.AddComponent<LabyrinthCameraController>();
            timeScaleController = gameObject.AddComponent<TimeScaleController>();
            productionController = gameObject.AddComponent<ResourceProductionController>();
            productionController.Configure(resources, baseDevelopment, baseAmbience, mazeRenderer);
            consumableAutomation = new HeroConsumableAutomation(resources, baseDevelopment, mazeRenderer);
            goldIngotManager = gameObject.AddComponent<GoldIngotManager>();
            goldIngotManager.Configure(resources);
            goldIngotManager.IngotDeliveredByHero += HandleHeroCarryObjectiveDelivered;
            deathTokenManager = gameObject.AddComponent<HeroDeathTokenManager>();
            deathTokenManager.TokenDelivered += HandleHeroDeathTokenDelivered;
            deathTokenManager.TokenDeliveredByHero += HandleHeroCarryObjectiveDelivered;
            taxCollectorController = gameObject.AddComponent<TaxCollectorController>();
            taxCollectorController.Configure(resources, baseDevelopment, mazeRenderer, terrainDecorations);
            dungeonFortificationController = gameObject.AddComponent<DungeonFortificationController>();
            dungeonFortificationController.Configure(resources, mazeRenderer);
            mineConstructionController = gameObject.AddComponent<MineConstructionController>();
            mineConstructionController.Configure(resources, baseDevelopment, mazeRenderer, baseAmbience);
            heroGuildQuestController = gameObject.AddComponent<HeroGuildQuestController>();
            heroGuildQuestController.Configure(resources, baseDevelopment, mazeRenderer, () => currentMaze, () => mobManager);
            explorationCoordinator = new HeroExplorationCoordinator();
            mobManager = gameObject.AddComponent<MobManager>();
            mobManager.SetEncounterHeroes(heroes);
            combatController = gameObject.AddComponent<CombatController>();
            combatController.MobDefeated += HandleMobDefeated;
            mainMenu = gameObject.AddComponent<MainMenuUI>();
            baseHud = gameObject.AddComponent<BaseHudView>();
            heroHud = gameObject.AddComponent<HeroHudView>();
            heroHud.Configure(() => heroes, () => selectedHero, SelectHero, GetHeroGuildQuestHudInfo);
            mobHud = gameObject.AddComponent<MobHudView>();
            buildingMicroHud = gameObject.AddComponent<BuildingMicroHudView>();
            buildingMicroHud.Configure(
                GetBuildingMicroHudLevel,
                GetBuildingMicroHudServices,
                HandleBuildingMicroHudServiceAction,
                ShowHeroHouseLineage,
                GetBuildingQuestGenerationToggle,
                SetBuildingQuestGenerationToggle);
            heroLineageHud = gameObject.AddComponent<HeroLineageHudView>();
            heroLineageHud.Configure(GetActiveHeroByNumber);
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

            if (TryToggleDebugBuildingMode())
            {
                return;
            }

            if (state == GameState.PauseMenu)
            {
                return;
            }

            if (IsRuntimeSimulationState() && currentMaze == null)
            {
                RecoverFromMissingMazeState();
                return;
            }

            if (TryToggleCastleHudHotkey())
            {
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

            if (!IsRuntimeSimulationState())
            {
                return;
            }

            UpdateHeroEntranceCommutes();

            if (currentDungeonLevel == 1)
            {
                consumableAutomation.Update(heroes, currentMaze.EntrancePosition);
                dungeonFortificationController.UpdateFortification();
                mineConstructionController.UpdateConstruction();
            }

            heroGuildQuestController.UpdateQuests(heroes);
            if (!victoryAchieved)
            {
                if (mobManager.TryBeginRespawnCheck())
                {
                    var visibilityHeroesForRespawn = BuildVisibilityHeroes();
                    mobManager.UpdateRespawns(
                        BuildRespawnBlockedCells(visibilityHeroesForRespawn),
                        visibilityDisplayMode == HeroVisibilityDisplayMode.Lighting,
                        heroes);
                }
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

        private void LateUpdate()
        {
            if (IsRuntimeSimulationState())
            {
                TryStartHeroEncounter();
            }
        }

        private bool IsRuntimeSimulationState()
        {
            return state == GameState.Playing || state == GameState.BaseHudOpen;
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
            if (buildingView.Type != BuildingType.HeroHouse)
            {
                heroLineageHud.Hide();
            }

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
                BuildAntiquaryFromBase,
                GetAntiquaryStatus,
                CanBuildAntiquary,
                GetAntiquaryCost,
                BuildHeroesGuildFromBase,
                GetHeroesGuildStatus,
                CanBuildHeroesGuild,
                GetHeroesGuildCost,
                GetHeroHouseStatus,
                CreateHeroFromBase,
                CanCreateHero,
                GetHeroCost,
                GetMineStatus,
                CanStartMineSelection,
                BeginMineSelection,
                GetOutpostStatus,
                CanStartOutpostSelection,
                BeginOutpostSelection,
                IsBuildingUnlocked,
                HasPendingBuilding,
                GetBuildingUpgradeStatus,
                CanUpgradeBuilding,
                GetBuildingUpgradeCost,
                UpgradeBuildingFromBase);
            cameraController.SetInteractionEnabled(false);
            state = GameState.BaseHudOpen;
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
            heroLineageHud.Hide();
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
            heroLineageHud.Hide();
            objectMicroHud.Hide();
            victoryHud.Hide();
            GameAudioController.StopMenuMusic();
            GameAudioController.StopWorldMusic();
            combatController.CancelCombat();
            mobManager.Clear();
            goldIngotManager.Clear();
            deathTokenManager.Clear();
            taxCollectorController.Clear();
            dungeonFortificationController.Clear();
            mineConstructionController.Clear();
            heroGuildQuestController.Clear();
            cameraController.SetInteractionEnabled(false);
            mazeTerrain.Clear();
            terrainDecorations.Clear();
            mazeRenderer.Clear();
            fogOfWarView.Clear();
            baseConstructionController.Clear();
            baseAmbience.Clear();
            houseFundCouriers.Clear();
            cityAmbience.Clear();
            DestroyHeroes();
            DestroyHeroMemoryView();
            DestroyHeroVisibilityView();
            currentMaze = null;
            currentBase = null;
            heroesGuildView = null;
            cartographerMemory = null;
            levelOneCartographerMemory = null;
            levelTwoCartographerMemory = null;
            ClearBaseConstructionPayloads();
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

        private void HandleMobDefeated(HeroController victoriousHero, MobController defeatedMob)
        {
            var defeatedBoss = defeatedMob != null && defeatedMob.Model != null && defeatedMob.Model.IsBoss;
            var defeatedMiniBoss = defeatedMob != null && defeatedMob.Model != null && defeatedMob.Model.IsMiniBoss;
            heroGuildQuestController.NotifyMobDefeated(victoriousHero, defeatedMob);
            mobManager.Remove(defeatedMob);
            RefreshHeroHouseEffect(victoriousHero != null ? victoriousHero.DisplayNumber : 0);
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
            TryStartBaseBuildingConstruction(BuildingType.Farm, GetFarmCost(), "Farm", out _);
        }

        private void BuildAlchemistShopFromBase()
        {
            TryStartBaseBuildingConstruction(BuildingType.AlchemistShop, GetAlchemistShopCost(), "Alchemist shop", out _);
        }

        private void BuildTavernFromBase()
        {
            TryStartBaseBuildingConstruction(BuildingType.Tavern, GetTavernCost(), "Tavern", out _);
        }

        private bool CanBuildFarm()
        {
            return currentMaze != null
                && IsBuildingUnlocked(BuildingType.Farm)
                && resources.CanAfford(GetFarmCost());
        }

        private bool CanBuildAlchemistShop()
        {
            return currentMaze != null
                && !baseDevelopment.HasAlchemistShop
                && !HasPendingBuilding(BuildingType.AlchemistShop)
                && IsBuildingUnlocked(BuildingType.AlchemistShop)
                && resources.CanAfford(GetAlchemistShopCost());
        }

        private bool CanBuildTavern()
        {
            return currentMaze != null
                && !baseDevelopment.HasTavern
                && !HasPendingBuilding(BuildingType.Tavern)
                && IsBuildingUnlocked(BuildingType.Tavern)
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

            return AppendBuildingUnlockStatus(BuildingType.Farm, status);
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

            return AppendBuildingUnlockStatus(BuildingType.AlchemistShop, status);
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

            return AppendBuildingUnlockStatus(BuildingType.Tavern, status);
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
            var light = FindSceneDirectionalLight();
            if (light == null)
            {
                var lightObject = new GameObject("Directional Light");
                light = lightObject.AddComponent<Light>();
            }

            light.type = LightType.Directional;
            light.intensity = 1.35f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.66f;
            light.shadowBias = 0.035f;
            light.shadowNormalBias = 0.28f;
            light.shadowNearPlane = 0.12f;
            light.transform.rotation = Quaternion.Euler(46f, -38f, 0f);
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowDistance = 90f;
        }

        private static Light FindSceneDirectionalLight()
        {
            var lights = Object.FindObjectsByType<Light>();
            for (var i = 0; i < lights.Length; i++)
            {
                if (lights[i] != null && lights[i].type == LightType.Directional)
                {
                    return lights[i];
                }
            }

            return null;
        }

        private static void SetSceneDirectionalIntensity(float intensity)
        {
            var light = FindSceneDirectionalLight();
            if (light != null)
            {
                light.intensity = Mathf.Max(0f, intensity);
            }
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

        private bool TryToggleDebugBuildingMode()
        {
            if (Keyboard.current == null || !Keyboard.current.f9Key.wasPressedThisFrame)
            {
                return false;
            }

            SetDebugBuildingMode(!debugBuildingMode);
            return true;
        }

        private void SetDebugBuildingMode(bool enabled)
        {
            if (debugBuildingMode == enabled)
            {
                return;
            }

            debugBuildingMode = enabled;
            if (baseDevelopment != null)
            {
                baseDevelopment.DebugAllBuildingsUnlocked = enabled;
            }

            GameAudioController.PlayUi(enabled ? GameSfx.HudConfirm : GameSfx.HudClick);
            GameDebugLog.Info(
                "Debug",
                $"Building debug mode {(enabled ? "enabled" : "disabled")}: freeBuildings={enabled}, allBuildingUnlocks={enabled}.");
        }
    }
}
