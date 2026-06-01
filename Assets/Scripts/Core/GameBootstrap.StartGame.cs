using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Labyrinth.Base;
using Labyrinth.Hero;
using Labyrinth.Maze;
using UnityEngine;

namespace Labyrinth.Core
{
    public sealed partial class GameBootstrap
    {
        private Coroutine generationRoutine;

        private void StartGame(MazeGenerationSettings settings)
        {
            if (settings == null)
            {
                GameDebugLog.Error("Game", "Start requested without generation settings.");
                return;
            }

            if (generationRoutine != null)
            {
                StopCoroutine(generationRoutine);
                generationRoutine = null;
            }

            generationRoutine = StartCoroutine(StartGameRoutine(settings));
        }

        private IEnumerator StartGameRoutine(MazeGenerationSettings settings)
        {
            var totalTimer = Stopwatch.StartNew();
            var stageTimer = Stopwatch.StartNew();
            GameDebugLog.Info(
                "Game",
                $"Start requested: size={settings.Width}x{settings.Height}, seed={settings.Seed}, preset={settings.Preset}");
            state = GameState.Generating;
            mainMenu.ShowLoading(settings);
            GameAudioController.StopMenuMusic();
            GameAudioController.StopWorldMusic();
            timeScaleController.ResetToNormal();
            yield return ReportGenerationProgress(0.04f, "Очистка прошлого лабиринта");

            SetGameHudVisible(false);
            mapHud.HideExpanded();
            baseHud.Hide();
            heroHud.Hide();
            mobHud.Hide();
            buildingMicroHud.Hide();
            heroLineageHud.Hide();
            objectMicroHud.Hide();
            victoryHud.Hide();
            cameraController.SetInteractionEnabled(false);
            mazeTerrain.Clear();
            terrainDecorations.Clear();
            mazeRenderer.Clear();
            fogOfWarView.Clear();
            baseConstructionController.Clear();
            goldIngotManager.Clear();
            deathTokenManager.Clear();
            taxCollectorController.Clear();
            dungeonFortificationController.Clear();
            mineConstructionController.Clear();
            heroGuildQuestController.Clear();
            explorationCoordinator.Clear();
            baseAmbience.Clear();
            houseFundCouriers.Clear();
            cityAmbience.Clear();
            DestroyHeroes();
            DestroyHeroMemoryView();
            DestroyHeroVisibilityView();
            yield return ReportGenerationProgress(0.16f, "Подготовка систем");

            cartographerMemory = null;
            levelOneCartographerMemory = null;
            levelTwoCartographerMemory = null;
            resources.ResetToDefault();
            baseDevelopment.Reset();
            ClearBaseConstructionPayloads();
            productionController.ResetProgress();
            victoryAchieved = false;
            adventureMusicStarted = false;
            rootGenerationSettings = settings;
            visibilityDisplayMode = HeroVisibilityDisplayMode.Lighting;
            currentDungeonLevel = 1;
            unlockedDungeonLevel = 1;
            levelOneMaze = null;
            levelTwoMaze = null;
            heroesGuildView = null;
            yield return ReportGenerationProgress(0.24f, "Генерация лабиринта");

            currentMaze = generator.Generate(settings);
            TraceGenerationStage("maze model", stageTimer);
            yield return ReportGenerationProgress(0.42f, "Проверка маршрутов");
            if (!MazeValidation.ValidateGeneratedMaze(currentMaze, out var error))
            {
                GameDebugLog.Error("Maze", $"Generation failed: {error}");
                state = GameState.MainMenu;
                generationRoutine = null;
                mainMenu.Show(StartGame);
                GameAudioController.StartMenuMusic();
                yield break;
            }

            TraceGenerationStage("maze validation", stageTimer);
            LogMazeSummary(currentMaze);
            explorationCoordinator.Reset(currentMaze.Grid, currentMaze.EntrancePosition, currentMaze.LevelNumber);
            levelOneMaze = currentMaze;
            yield return ReportGenerationProgress(0.46f, "Отрисовка земли");
            yield return mazeTerrain.RenderAsync(
                currentMaze,
                mazeRenderer.CellSize,
                (progress, status) => SetGenerationProgressRange(0.46f, 0.58f, progress, status));
            TraceGenerationStage("terrain render", stageTimer);
            mazeTerrain.SetVisualVisible(true);

            yield return ReportGenerationProgress(0.58f, "Отрисовка лабиринта");
            BaseView renderedBase = null;
            yield return mazeRenderer.RenderAsync(
                currentMaze,
                (progress, status) => SetGenerationProgressRange(0.58f, 0.76f, progress, status),
                view => renderedBase = view,
                96);
            currentBase = renderedBase;
            TraceGenerationStage("maze render", stageTimer);

            yield return ReportGenerationProgress(0.76f, "Декорации местности");
            yield return terrainDecorations.RenderAsync(
                currentMaze,
                mazeRenderer,
                baseDevelopment,
                (progress, status) => SetGenerationProgressRange(0.76f, 0.84f, progress, status));
            TraceGenerationStage("terrain decorations", stageTimer);

            yield return ReportGenerationProgress(0.84f, "Запуск города");
            baseAmbience.Initialize(currentMaze, mazeRenderer);
            houseFundCouriers.Clear();
            cityAmbience.Initialize(currentMaze, mazeRenderer);
            taxCollectorController.Initialize(currentMaze);
            cartographerMemory = new HeroMemory(currentMaze.Grid);
            cartographerMemory.Remember(currentMaze.EntrancePosition);
            levelOneCartographerMemory = cartographerMemory;
            levelTwoCartographerMemory = null;
            dungeonFortificationController.Initialize(currentMaze, cartographerMemory);
            mineConstructionController.Initialize(currentMaze);
            RefreshAllBuildingUpgradeVisuals();
            TraceGenerationStage("city systems", stageTimer);

            yield return ReportGenerationProgress(0.90f, "Размещение существ и предметов");
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
            deathTokenManager.Initialize(currentMaze, mazeRenderer, GetHeroHouseView);
            TraceGenerationStage("actors and pickups", stageTimer);

            yield return ReportGenerationProgress(0.98f, "Фокус камеры");
            cameraController.Focus(mainCamera, currentMaze, mazeRenderer.CellSize, true);
            state = GameState.Playing;
            SetGameHudVisible(true);
            mainMenu.Hide();
            generationRoutine = null;
            GameDebugLog.Info("Perf", $"Generation total: {totalTimer.ElapsedMilliseconds}ms.");
            GameDebugLog.Info("Game", "Generation completed and play mode started. Music waits for first hero creation.");
        }

        private IEnumerator ReportGenerationProgress(float progress, string status)
        {
            mainMenu.SetLoadingProgress(progress, status);
            yield return null;
        }

        private void SetGenerationProgressRange(float start, float end, float progress, string status)
        {
            mainMenu.SetLoadingProgress(Mathf.Lerp(start, end, Mathf.Clamp01(progress)), status);
        }

        private static void TraceGenerationStage(string stage, Stopwatch timer)
        {
            GameDebugLog.Info("Perf", $"Generation stage '{stage}': {timer.ElapsedMilliseconds}ms.");
            timer.Restart();
        }
    }
}
