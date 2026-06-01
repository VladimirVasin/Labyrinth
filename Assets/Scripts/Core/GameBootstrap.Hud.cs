using UnityEngine;

namespace Labyrinth.Core
{
    public sealed partial class GameBootstrap
    {
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
                || heroLineageHud.IsVisible
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
            heroLineageHud.Hide();
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
                || heroLineageHud.ContainsScreenPoint(screenPosition)
                || mobHud.ContainsScreenPoint(screenPosition)
                || objectMicroHud.ContainsScreenPoint(screenPosition)
                || victoryHud.ContainsScreenPoint(screenPosition)
                || mapHud.ContainsScreenPoint(screenPosition);
        }

        private void HideWorldHuds()
        {
            buildingMicroHud.Hide();
            heroLineageHud.Hide();
            mobHud.Hide();
            objectMicroHud.Hide();
            ClearSelectedMob();
        }

        private bool TryToggleCastleHudHotkey()
        {
            if (UnityEngine.InputSystem.Keyboard.current == null
                || !UnityEngine.InputSystem.Keyboard.current.cKey.wasPressedThisFrame
                || currentMaze == null
                || currentBase == null
                || (state != GameState.Playing && state != GameState.BaseHudOpen))
            {
                return false;
            }

            if (baseHud.IsVisible)
            {
                baseHud.Hide();
                if (state == GameState.BaseHudOpen)
                {
                    state = GameState.Playing;
                    cameraController.SetInteractionEnabled(true);
                }

                GameDebugLog.Info("UI", "Castle HUD closed by C hotkey.");
                return true;
            }

            var castleBuilding = currentBase.GetComponent<Labyrinth.Base.BuildingView>();
            if (castleBuilding == null)
            {
                GameDebugLog.Warning("UI", "C hotkey could not open castle HUD: castle BuildingView missing.");
                return true;
            }

            CloseOpenRuntimeHud();
            ShowBuildingHud(castleBuilding);
            GameDebugLog.Info("UI", "Castle HUD opened by C hotkey.");
            return true;
        }
    }
}
