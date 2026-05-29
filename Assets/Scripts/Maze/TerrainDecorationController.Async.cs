using System;
using System.Collections;
using Labyrinth.Base;
using Labyrinth.Core;
using UnityEngine;

namespace Labyrinth.Maze
{
    public sealed partial class TerrainDecorationController
    {
        public IEnumerator RenderAsync(
            MazeGenerationResult generationResult,
            MazeRenderer renderer,
            BaseDevelopment development,
            Action<float, string> reportProgress)
        {
            Clear();
            result = generationResult;
            mazeRenderer = renderer;
            baseDevelopment = development;
            if (result == null || result.Grid == null || mazeRenderer == null)
            {
                yield break;
            }

            EnsureMaterials();
            root = new GameObject("Terrain Decorations").transform;
            root.SetParent(transform, false);

            var random = new System.Random(CreateSeed(0x341a91));
            var outsideCells = CalculateOutsideCellCount();
            var riverCount = CalculateRiverCount(outsideCells);
            var riverSegments = 0;
            for (var i = 0; i < riverCount; i++)
            {
                riverSegments += CreateRiver(i, random);
                reportProgress?.Invoke(Mathf.Lerp(0.06f, 0.24f, (i + 1f) / riverCount), "Реки и мосты");
                yield return null;
            }

            var treeCount = Mathf.Clamp(outsideCells / 68, 18, 150);
            var bushCount = Mathf.Clamp(outsideCells / 58, 24, 170);
            var rockCount = Mathf.Clamp(outsideCells / 150, 10, 60);
            var flowerPatchCount = Mathf.Clamp(outsideCells / 126, 12, 90);

            yield return CreateNatureAsync(treeCount, bushCount, rockCount, flowerPatchCount, random, reportProgress);
            GameDebugLog.Info(
                "Terrain",
                $"Decorations generated: outsideCells={outsideCells}, rivers={riverCount}, riverCells={riverSegments}, terrainHills=mesh, trees={treeCount}, bushes={bushCount}, rocks={rockCount}, flowers={flowerPatchCount}.");
            reportProgress?.Invoke(1f, "Декорации готовы");
            yield return null;
        }

        private IEnumerator CreateNatureAsync(
            int trees,
            int bushes,
            int rocks,
            int flowers,
            System.Random random,
            Action<float, string> reportProgress)
        {
            var total = Mathf.Max(1, trees + bushes + rocks + flowers);
            var created = 0;
            var nextFrameAt = Time.realtimeSinceStartup + 0.012f;

            for (var i = 0; i < trees; i++)
            {
                if (TryFindDecorPosition(random, 1, out var cell))
                {
                    CreateTree(cell, random);
                }

                created++;
                if (created % 48 == 0 || created == total || Time.realtimeSinceStartup >= nextFrameAt)
                {
                    reportProgress?.Invoke(Mathf.Lerp(0.24f, 0.94f, created / (float)total), $"Деревья {i + 1}/{trees}");
                    yield return null;
                    nextFrameAt = Time.realtimeSinceStartup + 0.012f;
                }
            }

            for (var i = 0; i < bushes; i++)
            {
                if (TryFindDecorPosition(random, 0, out var cell))
                {
                    CreateBush(cell, random);
                }

                created++;
                if (created % 56 == 0 || created == total || Time.realtimeSinceStartup >= nextFrameAt)
                {
                    reportProgress?.Invoke(Mathf.Lerp(0.24f, 0.94f, created / (float)total), $"Кусты {i + 1}/{bushes}");
                    yield return null;
                    nextFrameAt = Time.realtimeSinceStartup + 0.012f;
                }
            }

            for (var i = 0; i < rocks; i++)
            {
                if (TryFindDecorPosition(random, 0, out var cell))
                {
                    CreateRock(cell, random);
                }

                created++;
                if (created % 56 == 0 || created == total || Time.realtimeSinceStartup >= nextFrameAt)
                {
                    reportProgress?.Invoke(Mathf.Lerp(0.24f, 0.94f, created / (float)total), $"Камни {i + 1}/{rocks}");
                    yield return null;
                    nextFrameAt = Time.realtimeSinceStartup + 0.012f;
                }
            }

            for (var i = 0; i < flowers; i++)
            {
                if (TryFindDecorPosition(random, 0, out var cell))
                {
                    CreateFlowerPatch(cell, random);
                }

                created++;
                if (created % 64 == 0 || created == total || Time.realtimeSinceStartup >= nextFrameAt)
                {
                    reportProgress?.Invoke(Mathf.Lerp(0.24f, 0.94f, created / (float)total), $"Цветы {i + 1}/{flowers}");
                    yield return null;
                    nextFrameAt = Time.realtimeSinceStartup + 0.012f;
                }
            }
        }
    }
}
