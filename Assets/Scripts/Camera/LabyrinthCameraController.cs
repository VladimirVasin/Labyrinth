using Labyrinth.Maze;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Labyrinth.CameraSystem
{
    public sealed class LabyrinthCameraController : MonoBehaviour
    {
        private const float WheelZoomFactor = 0.72f;
        private const float MousePanMultiplier = 1.9f;
        private const float WheelPanPixels = 140f;

        private Camera targetCamera;
        private Vector3 focusPoint;
        private float cameraDistance;
        private float minOrthographicSize;
        private float maxOrthographicSize;
        private Vector2 boundsMin;
        private Vector2 boundsMax;
        private bool interactionEnabled;

        private void Update()
        {
            if (!interactionEnabled || targetCamera == null)
            {
                return;
            }

            HandleZoom();
            HandleMousePan();
            HandleKeyboardPan();
        }

        private void OnGUI()
        {
            if (!interactionEnabled || targetCamera == null)
            {
                return;
            }

            var currentEvent = Event.current;
            if (currentEvent == null)
            {
                return;
            }

            if (currentEvent.type == EventType.ScrollWheel)
            {
                var screenPosition = new Vector2(
                    currentEvent.mousePosition.x,
                    Screen.height - currentEvent.mousePosition.y);
                var steps = -NormalizeScrollSteps(currentEvent.delta.y);

                if (IsShiftPressed())
                {
                    PanByScreenDelta(new Vector2(0f, -steps * WheelPanPixels));
                }
                else
                {
                    ApplyZoom(steps, screenPosition);
                }

                currentEvent.Use();
                return;
            }

            if (currentEvent.type == EventType.MouseDrag && (currentEvent.button == 1 || currentEvent.button == 2))
            {
                PanByScreenDelta(currentEvent.delta * MousePanMultiplier);
                currentEvent.Use();
            }
        }

        public void Focus(Camera targetCamera, MazeGenerationResult result, float cellSize, bool startNearCastle = false)
        {
            if (targetCamera == null || result == null)
            {
                return;
            }

            this.targetCamera = targetCamera;
            targetCamera.orthographic = true;
            targetCamera.nearClipPlane = 0.1f;
            targetCamera.farClipPlane = 500f;
            targetCamera.transform.rotation = Quaternion.Euler(55f, 45f, 0f);

            var grid = result.Grid;
            var mazeCenter = new Vector3(
                (grid.Width - 1) * cellSize * 0.5f - cellSize * 0.35f,
                0f,
                (grid.Height - 1) * cellSize * 0.5f);
            var castleCenter = new Vector3(
                result.BasePosition.x * cellSize,
                0f,
                result.BasePosition.y * cellSize);
            var entranceCenter = new Vector3(
                result.EntrancePosition.x * cellSize,
                0f,
                result.EntrancePosition.y * cellSize);
            var largestSide = Mathf.Max(grid.Width, grid.Height) * cellSize;
            var distance = Mathf.Max(18f, largestSide * 1.35f);

            cameraDistance = distance;
            minOrthographicSize = Mathf.Max(3f, Mathf.Min(6f, largestSide * 0.16f));
            maxOrthographicSize = Mathf.Max(9f, largestSide * 0.5f);
            boundsMin = new Vector2(-cellSize * (MazeTerrain.PaddingCells + 2f), -cellSize * (MazeTerrain.PaddingCells + 2f));
            boundsMax = new Vector2(
                (grid.Width - 1) * cellSize + cellSize * (MazeTerrain.PaddingCells + 2f),
                (grid.Height - 1) * cellSize + cellSize * (MazeTerrain.PaddingCells + 2f));

            if (startNearCastle)
            {
                focusPoint = Vector3.Lerp(castleCenter, entranceCenter, 0.28f);
                targetCamera.orthographicSize = Mathf.Clamp(cellSize * 7.2f, minOrthographicSize, maxOrthographicSize);
            }
            else
            {
                focusPoint = mazeCenter;
                targetCamera.orthographicSize = Mathf.Max(8f, largestSide * 0.48f);
            }

            UpdateCameraPosition();
            interactionEnabled = true;
        }

        public void SetInteractionEnabled(bool enabled)
        {
            interactionEnabled = enabled && targetCamera != null;
        }

        private void HandleZoom()
        {
            if (Mouse.current == null)
            {
                return;
            }

            var scroll = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Approximately(scroll, 0f))
            {
                return;
            }

            if (IsShiftPressed())
            {
                PanByScreenDelta(new Vector2(0f, scroll * WheelPanPixels));
                return;
            }

            var steps = NormalizeScrollSteps(scroll);
            ApplyZoom(steps, Mouse.current.position.ReadValue());
        }

        private static float NormalizeScrollSteps(float scroll)
        {
            var absoluteScroll = Mathf.Abs(scroll);
            if (absoluteScroll < 0.01f)
            {
                return 0f;
            }

            if (absoluteScroll < 120f)
            {
                return Mathf.Sign(scroll);
            }

            return scroll / 120f;
        }

        private void HandleMousePan()
        {
            if (Mouse.current == null)
            {
                return;
            }

            var dragging = Mouse.current.rightButton.isPressed || Mouse.current.middleButton.isPressed;
            if (!dragging)
            {
                return;
            }

            var delta = Mouse.current.delta.ReadValue();
            if (delta.sqrMagnitude <= 0.01f)
            {
                return;
            }

            PanByScreenDelta(delta * MousePanMultiplier);
        }

        private void HandleKeyboardPan()
        {
            if (Keyboard.current == null)
            {
                return;
            }

            var direction = Vector3.zero;
            var right = Vector3.ProjectOnPlane(targetCamera.transform.right, Vector3.up).normalized;
            var up = Vector3.ProjectOnPlane(targetCamera.transform.up, Vector3.up).normalized;

            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            {
                direction -= right;
            }

            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            {
                direction += right;
            }

            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
            {
                direction += up;
            }

            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
            {
                direction -= up;
            }

            if (direction.sqrMagnitude <= 0.01f)
            {
                HandleKeyboardZoom();
                return;
            }

            var speed = Mathf.Max(14f, targetCamera.orthographicSize * 4.2f);
            if (IsShiftPressed())
            {
                speed *= 1.8f;
            }

            focusPoint += direction.normalized * speed * Time.unscaledDeltaTime;
            ClampFocusPoint();
            UpdateCameraPosition();
            HandleKeyboardZoom();
        }

        private void HandleKeyboardZoom()
        {
            if (Keyboard.current == null)
            {
                return;
            }

            var zoomDirection = 0f;
            if (Keyboard.current.equalsKey.isPressed || Keyboard.current.numpadPlusKey.isPressed)
            {
                zoomDirection -= 1f;
            }

            if (Keyboard.current.minusKey.isPressed || Keyboard.current.numpadMinusKey.isPressed)
            {
                zoomDirection += 1f;
            }

            if (Mathf.Approximately(zoomDirection, 0f))
            {
                return;
            }

            var zoomSpeed = Mathf.Max(12f, targetCamera.orthographicSize * 3.5f);
            targetCamera.orthographicSize = Mathf.Clamp(
                targetCamera.orthographicSize + zoomDirection * zoomSpeed * Time.unscaledDeltaTime,
                minOrthographicSize,
                maxOrthographicSize);
            UpdateCameraPosition();
        }

        private void ApplyZoom(float steps, Vector2 screenPosition)
        {
            if (Mathf.Approximately(steps, 0f))
            {
                return;
            }

            var hasGroundPoint = TryGetGroundPoint(screenPosition, out var beforeZoomPoint);
            var zoomFactor = Mathf.Pow(WheelZoomFactor, steps);
            targetCamera.orthographicSize = Mathf.Clamp(
                targetCamera.orthographicSize * zoomFactor,
                minOrthographicSize,
                maxOrthographicSize);
            UpdateCameraPosition();

            if (hasGroundPoint && TryGetGroundPoint(screenPosition, out var afterZoomPoint))
            {
                focusPoint += beforeZoomPoint - afterZoomPoint;
                ClampFocusPoint();
                UpdateCameraPosition();
            }
        }

        private void PanByScreenDelta(Vector2 delta)
        {
            var worldUnitsPerPixel = targetCamera.orthographicSize * 2f / Mathf.Max(1f, Screen.height);
            var right = Vector3.ProjectOnPlane(targetCamera.transform.right, Vector3.up).normalized;
            var up = Vector3.ProjectOnPlane(targetCamera.transform.up, Vector3.up).normalized;
            focusPoint -= (right * delta.x + up * delta.y) * worldUnitsPerPixel;
            ClampFocusPoint();
            UpdateCameraPosition();
        }

        private bool TryGetGroundPoint(Vector2 screenPosition, out Vector3 point)
        {
            var plane = new Plane(Vector3.up, Vector3.zero);
            var ray = targetCamera.ScreenPointToRay(screenPosition);

            if (plane.Raycast(ray, out var distance))
            {
                point = ray.GetPoint(distance);
                return true;
            }

            point = Vector3.zero;
            return false;
        }

        private void ClampFocusPoint()
        {
            focusPoint.x = Mathf.Clamp(focusPoint.x, boundsMin.x, boundsMax.x);
            focusPoint.z = Mathf.Clamp(focusPoint.z, boundsMin.y, boundsMax.y);
            focusPoint.y = 0f;
        }

        private void UpdateCameraPosition()
        {
            ClampFocusPoint();
            targetCamera.transform.position = focusPoint - targetCamera.transform.forward * cameraDistance;
        }

        private static bool IsShiftPressed()
        {
            return Keyboard.current != null
                && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
        }
    }
}
