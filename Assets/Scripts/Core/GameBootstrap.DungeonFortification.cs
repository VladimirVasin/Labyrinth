namespace Labyrinth.Core
{
    public sealed partial class GameBootstrap
    {
        private string GetDungeonFortificationStatus()
        {
            if (dungeonFortificationController == null)
            {
                return "недоступно";
            }

            return $"{dungeonFortificationController.StatusText}, клетка {DungeonFortificationController.FloorWoodCost} дер., факел {DungeonFortificationController.TorchWoodCost} дер.";
        }

        private void BeginDungeonFortificationSelection()
        {
            if (dungeonFortificationController == null || currentMaze == null)
            {
                return;
            }

            dungeonFortificationController.BeginSelectionMode();
            baseHud.Hide();
            buildingMicroHud.Hide();
            objectMicroHud.Hide();
            state = GameState.Playing;
            cameraController.SetInteractionEnabled(true);
            GameDebugLog.Info("Base", "Dungeon fortification selection mode started from castle HUD.");
        }

        private void CancelDungeonFortificationSelection()
        {
            dungeonFortificationController?.CancelSelectionMode();
        }

        private bool IsDungeonFortificationSelectionActive()
        {
            return dungeonFortificationController != null && dungeonFortificationController.SelectionModeActive;
        }

        private void UpdateDungeonFortificationHover()
        {
            if (!IsDungeonFortificationSelectionActive() || mainCamera == null || UnityEngine.InputSystem.Mouse.current == null)
            {
                return;
            }

            var screenPosition = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            var ray = mainCamera.ScreenPointToRay(screenPosition);
            if (!UnityEngine.Physics.Raycast(ray, out var hit, 500f))
            {
                dungeonFortificationController.ClearHoverCell();
                return;
            }

            dungeonFortificationController.UpdateHoverCell(WorldToGridCell(hit.point));
        }

        private bool TryHandleDungeonFortificationSelection(UnityEngine.RaycastHit hit)
        {
            if (!IsDungeonFortificationSelectionActive() || currentMaze == null || mazeRenderer == null)
            {
                return false;
            }

            var cell = WorldToGridCell(hit.point);
            dungeonFortificationController.TryQueueCell(cell);
            RefreshSelectedHeroVisibility();
            return true;
        }

        private UnityEngine.Vector2Int WorldToGridCell(UnityEngine.Vector3 worldPosition)
        {
            var cellSize = mazeRenderer.CellSize;
            return new UnityEngine.Vector2Int(
                UnityEngine.Mathf.RoundToInt(worldPosition.x / cellSize),
                UnityEngine.Mathf.RoundToInt(worldPosition.z / cellSize));
        }
    }
}
