using Labyrinth.Hero;

namespace Labyrinth.Core
{
    public sealed partial class GameBootstrap
    {
        private string GetMineStatus()
        {
            if (!baseDevelopment.HasMinersGuild)
            {
                return "нужна Гильдия шахтёров";
            }

            if (mineConstructionController == null)
            {
                return "недоступно";
            }

            var knowledge = GetMineKnowledgeMemory();
            if (knowledge == null)
            {
                return "нужна разведанная общая карта";
            }

            return mineConstructionController.StatusText;
        }

        private string GetOutpostStatus()
        {
            if (!baseDevelopment.HasMinersGuild)
            {
                return "нужна Гильдия шахтёров";
            }

            if (mineConstructionController == null)
            {
                return "недоступно";
            }

            var knowledge = GetMineKnowledgeMemory();
            if (knowledge == null)
            {
                return "нужна разведанная общая карта";
            }

            return mineConstructionController.OutpostStatusText;
        }

        private bool CanStartMineSelection()
        {
            return baseDevelopment.HasMinersGuild
                && mineConstructionController != null
                && mineConstructionController.CanBeginSelection(GetMineKnowledgeMemory());
        }

        private bool CanStartOutpostSelection()
        {
            return baseDevelopment.HasMinersGuild
                && mineConstructionController != null
                && mineConstructionController.CanBeginOutpostSelection(GetMineKnowledgeMemory());
        }

        private void BeginMineSelection()
        {
            if (!baseDevelopment.HasMinersGuild || mineConstructionController == null)
            {
                return;
            }

            var knowledge = GetMineKnowledgeMemory();
            if (knowledge == null)
            {
                GameDebugLog.Warning("Mine", "Mine selection blocked: no shared cartographer knowledge.");
                return;
            }

            mineConstructionController.BeginSelectionMode(knowledge);
            baseHud.Hide();
            buildingMicroHud.Hide();
            objectMicroHud.Hide();
            state = GameState.Playing;
            cameraController.SetInteractionEnabled(true);
            GameDebugLog.Info("Mine", "Mine cave selection mode started from castle HUD.");
        }

        private void BeginOutpostSelection()
        {
            if (!baseDevelopment.HasMinersGuild || mineConstructionController == null)
            {
                return;
            }

            var knowledge = GetMineKnowledgeMemory();
            if (knowledge == null)
            {
                GameDebugLog.Warning("Mine", "Outpost selection blocked: no shared cartographer knowledge.");
                return;
            }

            mineConstructionController.BeginOutpostSelectionMode(knowledge);
            baseHud.Hide();
            buildingMicroHud.Hide();
            objectMicroHud.Hide();
            state = GameState.Playing;
            cameraController.SetInteractionEnabled(true);
            GameDebugLog.Info("Mine", "Outpost cave selection mode started from castle HUD.");
        }

        private void CancelMineSelection()
        {
            mineConstructionController?.CancelSelectionMode();
        }

        private bool IsMineSelectionActive()
        {
            return mineConstructionController != null && mineConstructionController.SelectionModeActive;
        }

        private void UpdateMineConstructionHover()
        {
            if (!IsMineSelectionActive() || mainCamera == null || UnityEngine.InputSystem.Mouse.current == null)
            {
                return;
            }

            var screenPosition = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            var ray = mainCamera.ScreenPointToRay(screenPosition);
            if (!UnityEngine.Physics.Raycast(ray, out var hit, 500f))
            {
                mineConstructionController.ClearHoverCell();
                return;
            }

            mineConstructionController.UpdateHoverCell(WorldToGridCell(hit.point));
        }

        private bool TryHandleMineSelection(UnityEngine.RaycastHit hit)
        {
            if (!IsMineSelectionActive() || currentMaze == null || mazeRenderer == null)
            {
                return false;
            }

            mineConstructionController.TrySelectCave(WorldToGridCell(hit.point));
            RefreshSelectedHeroVisibility();
            return true;
        }

        private HeroMemory GetMineKnowledgeMemory()
        {
            return baseDevelopment != null && baseDevelopment.HasCartographerHouse
                ? cartographerMemory
                : null;
        }
    }
}
