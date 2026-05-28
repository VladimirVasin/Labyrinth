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
    }
}
