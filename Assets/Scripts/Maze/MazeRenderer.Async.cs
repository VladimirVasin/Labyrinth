using System;
using System.Collections;
using Labyrinth.Base;
using UnityEngine;

namespace Labyrinth.Maze
{
    public sealed partial class MazeRenderer
    {
        private const float AsyncRenderFrameBudgetSeconds = 0.018f;

        public IEnumerator RenderAsync(
            MazeGenerationResult result,
            Action<float, string> reportProgress,
            Action<BaseView> onCompleted,
            int cellsPerFrame = 96)
        {
            EnsureMaterials();
            Clear();
            if (result == null || result.Grid == null)
            {
                onCompleted?.Invoke(null);
                yield break;
            }

            root = new GameObject("MazeRoot").transform;
            root.SetParent(transform, false);

            reportProgress?.Invoke(0.02f, "Подготовка темноты");
            CreateDungeonSeamUnderlay(result.Grid);
            CreateLightingFogCover(result.Grid);
            yield return null;

            var totalCells = Mathf.Max(1, result.Grid.Width * result.Grid.Height);
            var renderedCells = 0;
            var frameBudget = Mathf.Max(1, cellsPerFrame);
            var nextFrameAt = Time.realtimeSinceStartup + AsyncRenderFrameBudgetSeconds;
            foreach (var cell in result.Grid.Cells())
            {
                RenderCell(cell, result.Grid);
                renderedCells++;
                if (renderedCells % frameBudget != 0
                    && renderedCells < totalCells
                    && Time.realtimeSinceStartup < nextFrameAt)
                {
                    continue;
                }

                var progress = renderedCells / (float)totalCells;
                reportProgress?.Invoke(
                    Mathf.Lerp(0.06f, 0.78f, progress),
                    $"Отрисовка клеток {renderedCells}/{totalCells}");
                yield return null;
                nextFrameAt = Time.realtimeSinceStartup + AsyncRenderFrameBudgetSeconds;
            }

            reportProgress?.Invoke(0.82f, "Двери и ключи");
            RenderCentralDoors(result);
            RenderKeyPickups(result);
            yield return null;

            reportProgress?.Invoke(0.88f, "Сундуки и руда");
            RenderChests(result);
            OreDepositRenderer.Render(this, result);
            yield return null;

            reportProgress?.Invoke(0.93f, "Лестницы и вход");
            DungeonStairsRenderer.Render(this, result);
            if (result.LevelNumber <= 1 || result.UpStairs == null)
            {
                RenderEntranceMarker(result.EntrancePosition);
            }

            yield return null;
            reportProgress?.Invoke(0.98f, "Замок");
            var baseView = RenderBase(result);
            ApplyStaticVoxelLightGrid(result);
            onCompleted?.Invoke(baseView);

            reportProgress?.Invoke(1f, "Лабиринт готов");
            yield return null;
        }
    }
}
