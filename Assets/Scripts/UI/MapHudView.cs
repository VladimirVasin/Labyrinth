using System;
using System.Collections.Generic;
using Labyrinth.Core;
using Labyrinth.Hero;
using Labyrinth.Maze;
using UnityEngine;

namespace Labyrinth.UI
{
    public sealed class MapHudView : MonoBehaviour
    {
        private Func<MazeGenerationResult> mazeProvider;
        private Func<HeroMemory> knowledgeProvider;
        private Func<HashSet<Vector2Int>> visibleCellsProvider;
        private Func<bool> commonMapUnlockedProvider;
        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle hintStyle;
        private bool expanded;

        public bool Visible { get; set; }

        public bool IsExpanded => expanded;

        public void Configure(
            Func<MazeGenerationResult> onMazeRequested,
            Func<HeroMemory> onKnowledgeRequested,
            Func<HashSet<Vector2Int>> onVisibleCellsRequested,
            Func<bool> onCommonMapUnlockedRequested)
        {
            mazeProvider = onMazeRequested;
            knowledgeProvider = onKnowledgeRequested;
            visibleCellsProvider = onVisibleCellsRequested;
            commonMapUnlockedProvider = onCommonMapUnlockedRequested;
        }

        public void ToggleExpanded()
        {
            expanded = !expanded;
            GameAudioController.PlayUi(expanded ? GameSfx.HudOpen : GameSfx.HudClose, 0.72f);
        }

        public void HideExpanded()
        {
            if (expanded)
            {
                GameAudioController.PlayUi(GameSfx.HudClose, 0.72f);
            }

            expanded = false;
        }

        public bool ContainsScreenPoint(Vector2 screenPosition)
        {
            if (!Visible || commonMapUnlockedProvider == null || !commonMapUnlockedProvider.Invoke())
            {
                return false;
            }

            var result = mazeProvider?.Invoke();
            if (result == null || result.Grid == null)
            {
                return false;
            }

            var guiPoint = ToGuiPoint(screenPosition);
            var smallRect = BuildPanelRect(BuildSmallRect(result.Grid), false);
            if (smallRect.Contains(guiPoint))
            {
                return true;
            }

            return expanded && BuildPanelRect(BuildLargeRect(result.Grid), true).Contains(guiPoint);
        }

        private void OnGUI()
        {
            if (!Visible)
            {
                return;
            }

            if (commonMapUnlockedProvider == null || !commonMapUnlockedProvider.Invoke())
            {
                expanded = false;
                return;
            }

            var result = mazeProvider?.Invoke();
            if (result == null || result.Grid == null)
            {
                return;
            }

            EnsureStyles();
            var smallRect = BuildSmallRect(result.Grid);
            DrawMapPanel(smallRect, result, false);
            HandleSmallMapClick(smallRect);

            if (expanded)
            {
                var largeRect = BuildLargeRect(result.Grid);
                DrawMapPanel(largeRect, result, true);
                HandleExpandedClicks(largeRect);
            }
        }

        private Rect BuildSmallRect(MazeGrid grid)
        {
            var maxWidth = Mathf.Min(280f, Screen.width * 0.26f);
            var mapWidth = Mathf.Max(180f, maxWidth);
            var mapHeight = Mathf.Clamp(mapWidth * grid.Height / Mathf.Max(1f, grid.Width), 120f, 240f);
            return new Rect(Screen.width - mapWidth - 18f, Screen.height - mapHeight - 18f, mapWidth, mapHeight);
        }

        private Rect BuildLargeRect(MazeGrid grid)
        {
            var maxWidth = Screen.width * 0.76f;
            var maxHeight = Screen.height * 0.78f;
            var widthByHeight = maxHeight * grid.Width / Mathf.Max(1f, grid.Height);
            var mapWidth = Mathf.Min(maxWidth, widthByHeight);
            var mapHeight = mapWidth * grid.Height / Mathf.Max(1f, grid.Width);
            if (mapHeight > maxHeight)
            {
                mapHeight = maxHeight;
                mapWidth = mapHeight * grid.Width / Mathf.Max(1f, grid.Height);
            }

            return new Rect((Screen.width - mapWidth) * 0.5f, (Screen.height - mapHeight) * 0.5f, mapWidth, mapHeight);
        }

        private void DrawMapPanel(Rect rect, MazeGenerationResult result, bool large)
        {
            var panelRect = BuildPanelRect(rect, large);
            FillRect(panelRect, new Color(0.06f, 0.06f, 0.055f, large ? 0.96f : 0.9f));
            FillRect(new Rect(panelRect.x, panelRect.y, panelRect.width, 3f), new Color(0.87f, 0.72f, 0.34f, 0.95f));
            DrawOutline(panelRect, new Color(0f, 0f, 0f, 0.8f));

            GUI.Label(new Rect(panelRect.x + 12f, panelRect.y + 7f, panelRect.width - 24f, 22f), "Общая карта", titleStyle);
            GUI.Label(
                new Rect(panelRect.x + 12f, panelRect.y + 29f, panelRect.width - 24f, 16f),
                large ? "M или клик вне карты - закрыть" : "клик или M - открыть",
                large ? subtitleStyle : hintStyle);

            DrawCells(rect, result);
        }

        private static Rect BuildPanelRect(Rect mapRect, bool large)
        {
            var headerHeight = large ? 44f : 32f;
            return new Rect(mapRect.x - 12f, mapRect.y - headerHeight - 10f, mapRect.width + 24f, mapRect.height + headerHeight + 22f);
        }

        private void DrawCells(Rect rect, MazeGenerationResult result)
        {
            var grid = result.Grid;
            var knowledge = knowledgeProvider?.Invoke();
            var visibleCells = visibleCellsProvider?.Invoke() ?? new HashSet<Vector2Int>();
            var cellWidth = rect.width / grid.Width;
            var cellHeight = rect.height / grid.Height;

            for (var y = 0; y < grid.Height; y++)
            {
                for (var x = 0; x < grid.Width; x++)
                {
                    var position = new Vector2Int(x, y);
                    var visible = visibleCells.Contains(position);
                    var known = visible
                        || position == result.EntrancePosition
                        || (knowledge != null && knowledge.IsKnown(position));
                    var color = GetCellColor(grid.Get(position), known, visible);
                    FillRect(new Rect(rect.x + x * cellWidth, rect.y + (grid.Height - y - 1) * cellHeight, Mathf.Ceil(cellWidth), Mathf.Ceil(cellHeight)), color);
                }
            }

            DrawEntranceMarker(rect, grid, result.EntrancePosition, cellWidth, cellHeight);
        }

        private static Color GetCellColor(MazeCell cell, bool known, bool visible)
        {
            if (!known)
            {
                return new Color(0.015f, 0.015f, 0.018f, 1f);
            }

            if (!visible)
            {
                if (cell.Type == MazeCellType.LockedDownStairs || cell.Type == MazeCellType.OpenDownStairs || cell.Type == MazeCellType.UpStairs)
                {
                    return new Color(0.32f, 0.26f, 0.12f, 1f);
                }

                return cell.Type == MazeCellType.Wall || cell.Type == MazeCellType.ClosedDoor
                    ? new Color(0.2f, 0.205f, 0.21f, 1f)
                    : new Color(0.32f, 0.32f, 0.32f, 1f);
            }

            if (cell.Type == MazeCellType.LockedDownStairs || cell.Type == MazeCellType.OpenDownStairs || cell.Type == MazeCellType.UpStairs)
            {
                return new Color(0.95f, 0.68f, 0.2f, 1f);
            }

            if (cell.Type == MazeCellType.Wall || cell.Type == MazeCellType.ClosedDoor)
            {
                return new Color(0.25f, 0.26f, 0.28f, 1f);
            }

            return cell.Type == MazeCellType.Entrance
                ? new Color(0.1f, 0.78f, 0.86f, 1f)
                : new Color(0.74f, 0.72f, 0.64f, 1f);
        }

        private static void DrawEntranceMarker(Rect rect, MazeGrid grid, Vector2Int entrance, float cellWidth, float cellHeight)
        {
            var marker = new Rect(
                rect.x + entrance.x * cellWidth,
                rect.y + (grid.Height - entrance.y - 1) * cellHeight,
                Mathf.Max(3f, cellWidth * 1.8f),
                Mathf.Max(3f, cellHeight * 1.8f));
            FillRect(marker, new Color(0.1f, 0.9f, 0.95f, 1f));
        }

        private void HandleSmallMapClick(Rect rect)
        {
            var current = Event.current;
            if (current == null || current.type != EventType.MouseDown || !rect.Contains(current.mousePosition))
            {
                return;
            }

            expanded = true;
            GameAudioController.PlayUi(GameSfx.HudOpen, 0.72f);
            current.Use();
        }

        private void HandleExpandedClicks(Rect rect)
        {
            var current = Event.current;
            if (current == null || current.type != EventType.MouseDown || rect.Contains(current.mousePosition))
            {
                return;
            }

            HideExpanded();
            current.Use();
        }

        private static Vector2 ToGuiPoint(Vector2 screenPosition)
        {
            return new Vector2(screenPosition.x, Screen.height - screenPosition.y);
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 15,
                fontStyle = FontStyle.Bold
            };
            titleStyle.normal.textColor = new Color(0.96f, 0.93f, 0.86f);
            subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 12,
                fontStyle = FontStyle.Italic
            };
            subtitleStyle.normal.textColor = new Color(0.78f, 0.76f, 0.7f);
            hintStyle = new GUIStyle(subtitleStyle)
            {
                fontSize = 10
            };
        }

        private static void DrawOutline(Rect rect, Color color)
        {
            FillRect(new Rect(rect.x, rect.y, rect.width, 1f), color);
            FillRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), color);
            FillRect(new Rect(rect.x, rect.y, 1f, rect.height), color);
            FillRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), color);
        }

        private static void FillRect(Rect rect, Color color)
        {
            var previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previousColor;
        }
    }
}
