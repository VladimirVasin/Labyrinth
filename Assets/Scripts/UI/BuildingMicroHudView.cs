using System;
using Labyrinth.Base;
using Labyrinth.Core;
using UnityEngine;

namespace Labyrinth.UI
{
    public sealed class BuildingMicroHudView : MonoBehaviour
    {
        private BuildingView selectedBuilding;
        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle labelStyle;
        private GUIStyle valueStyle;
        private GUIStyle effectStyle;
        private GUIStyle typeBadgeStyle;
        private GUIStyle closeButtonStyle;
        private GUIStyle serviceButtonStyle;
        private GUIStyle serviceTitleStyle;
        private GUIStyle servicePriceStyle;
        private GUIStyle tooltipTitleStyle;
        private GUIStyle tooltipBodyStyle;
        private Texture2D circleTexture;
        private bool servicesVisible;
        private Func<BuildingType, int> buildingLevelProvider;
        private Func<BuildingType, int, BuildingServiceEntry[]> buildingServicesProvider;
        private Action<BuildingType, int> buildingServiceActionHandler;

        public bool IsVisible => selectedBuilding != null;

        public void Configure(
            Func<BuildingType, int> getBuildingLevel,
            Func<BuildingType, int, BuildingServiceEntry[]> getBuildingServices = null,
            Action<BuildingType, int> onBuildingServiceAction = null)
        {
            buildingLevelProvider = getBuildingLevel;
            buildingServicesProvider = getBuildingServices;
            buildingServiceActionHandler = onBuildingServiceAction;
        }

        public bool ContainsScreenPoint(Vector2 screenPosition)
        {
            return selectedBuilding != null && CalculatePanelRect().Contains(ToGuiPoint(screenPosition));
        }

        public void Show(BuildingView building)
        {
            if (selectedBuilding == null || selectedBuilding != building)
            {
                GameAudioController.PlayUi(GameSfx.HudOpen);
                servicesVisible = false;
            }

            if (selectedBuilding != null && selectedBuilding != building)
            {
                selectedBuilding.SetSelected(false);
            }

            selectedBuilding = building;
            if (selectedBuilding != null)
            {
                selectedBuilding.SetSelected(true);
            }
        }

        public void Hide()
        {
            if (selectedBuilding != null)
            {
                GameAudioController.PlayUi(GameSfx.HudClose);
                selectedBuilding.SetSelected(false);
            }

            selectedBuilding = null;
            servicesVisible = false;
        }

        private void OnGUI()
        {
            if (selectedBuilding == null)
            {
                return;
            }

            EnsureStyles();

            var rect = CalculatePanelRect();
            var buildingLevel = GetBuildingLevel(selectedBuilding.Type);
            var services = GetSelectedBuildingServices();

            DrawPanel(rect);
            var contentX = rect.x + 16f;
            var contentWidth = rect.width - 32f;
            var y = rect.y + 14f;

            DrawBuildingHeader(new Rect(contentX, y, contentWidth, 70f), selectedBuilding);
            y += 84f;
            DrawStatRow(new Rect(contentX, y, contentWidth, 28f), "Тип", FormatType(selectedBuilding.Type));
            y += 36f;
            DrawStatRow(new Rect(contentX, y, contentWidth, 28f), "Уровень", $"Ур. {buildingLevel}");
            y += 36f;
            DrawEffectBox(new Rect(contentX, y, contentWidth, 58f), selectedBuilding.EffectText);
            y += 72f;

            if (services.Length > 0)
            {
                var buttonText = servicesVisible ? "Скрыть услуги" : "Показать услуги";
                if (GUI.Button(new Rect(contentX, y, contentWidth, 30f), buttonText, serviceButtonStyle))
                {
                    servicesVisible = !servicesVisible;
                    GameAudioController.PlayUi(GameSfx.HudClick);
                }

                y += 40f;
                if (servicesVisible)
                {
                    DrawServiceList(new Rect(contentX, y, contentWidth, services.Length * 30f + 14f), rect, services);
                }
            }

            if (GUI.Button(new Rect(contentX, rect.yMax - 40f, contentWidth, 30f), "Закрыть", closeButtonStyle))
            {
                Hide();
            }
        }

        private static string FormatType(BuildingType type)
        {
            switch (type)
            {
                case BuildingType.Castle:
                    return "Замок";
                case BuildingType.Farm:
                    return "Ферма";
                case BuildingType.LumberjackCamp:
                    return "Лагерь лесорубов";
                case BuildingType.HeroHouse:
                    return "Дом героя";
                case BuildingType.PeasantHut:
                    return "Лачуга крестьянина";
                case BuildingType.AlchemistShop:
                    return "Лавка алхимика";
                case BuildingType.Tavern:
                    return "Харчевня";
                case BuildingType.Forge:
                    return "Кузница";
                case BuildingType.Infirmary:
                    return "Лазарет";
                case BuildingType.CartographerHouse:
                    return "Дом картографа";
                case BuildingType.Chapel:
                    return "Часовня";
                case BuildingType.MinersGuild:
                    return "Гильдия шахтёров";
                case BuildingType.Market:
                    return "Рынок";
                case BuildingType.Antiquary:
                    return "Антиквариат";
                default:
                    return "Здание";
            }
        }

        private static void DrawPanel(Rect rect)
        {
            FillRect(rect, new Color(0.11f, 0.105f, 0.1f, 0.94f));
            FillRect(new Rect(rect.x, rect.y, rect.width, 3f), new Color(0.87f, 0.72f, 0.34f, 0.95f));
            DrawOutline(rect, new Color(0f, 0f, 0f, 0.75f));
        }

        private Rect CalculatePanelRect()
        {
            var serviceCount = GetSelectedBuildingServices().Length;
            var width = Mathf.Min(420f, Screen.width - 80f);
            var wantedHeight = servicesVisible && serviceCount > 0
                ? 360f + serviceCount * 30f
                : 314f;
            var height = Mathf.Min(wantedHeight, Screen.height - 96f);
            return new Rect(Screen.width - width - 18f, Screen.height - height - 18f, width, height);
        }

        private BuildingServiceEntry[] GetSelectedBuildingServices()
        {
            if (selectedBuilding == null)
            {
                return Array.Empty<BuildingServiceEntry>();
            }

            var level = GetBuildingLevel(selectedBuilding.Type);
            var services = buildingServicesProvider != null
                ? buildingServicesProvider.Invoke(selectedBuilding.Type, level)
                : BuildingServiceCatalog.Get(selectedBuilding.Type, level);
            return services ?? Array.Empty<BuildingServiceEntry>();
        }

        private int GetBuildingLevel(BuildingType type)
        {
            var level = buildingLevelProvider != null ? buildingLevelProvider.Invoke(type) : 1;
            return Mathf.Clamp(level, 1, 3);
        }

        private static Vector2 ToGuiPoint(Vector2 screenPosition)
        {
            return new Vector2(screenPosition.x, Screen.height - screenPosition.y);
        }

        private void DrawStatRow(Rect rect, string label, string value)
        {
            FillRect(rect, new Color(1f, 1f, 1f, 0.045f));
            GUI.Label(new Rect(rect.x + 8f, rect.y, rect.width * 0.36f, rect.height), label, labelStyle);
            GUI.Label(new Rect(rect.x + rect.width * 0.36f, rect.y, rect.width * 0.61f - 8f, rect.height), value, valueStyle);
        }

        private void DrawBuildingHeader(Rect rect, BuildingView building)
        {
            FillRect(rect, new Color(1f, 1f, 1f, 0.055f));
            DrawOutline(rect, new Color(1f, 1f, 1f, 0.11f));
            DrawBuildingIcon(new Rect(rect.x + 10f, rect.y + 13f, 44f, 44f), building.Type);
            var textX = rect.x + 66f;
            GUI.Label(new Rect(textX, rect.y + 8f, rect.width - 152f, 25f), building.DisplayName, titleStyle);
            GUI.Label(new Rect(textX, rect.y + 34f, rect.width - 84f, 18f), building.Subtitle, subtitleStyle);
            DrawTypeBadge(new Rect(rect.xMax - 106f, rect.y + 14f, 92f, 26f), FormatType(building.Type));
        }

        private void DrawTypeBadge(Rect rect, string text)
        {
            FillRect(rect, new Color(0.87f, 0.72f, 0.34f, 0.16f));
            DrawOutline(rect, new Color(0.87f, 0.72f, 0.34f, 0.45f));
            GUI.Label(rect, text, typeBadgeStyle);
        }

        private void DrawEffectBox(Rect rect, string effectText)
        {
            FillRect(rect, new Color(1f, 1f, 1f, 0.045f));
            DrawOutline(rect, new Color(1f, 1f, 1f, 0.08f));
            GUI.Label(new Rect(rect.x + 10f, rect.y + 5f, rect.width - 20f, 16f), "Эффект", labelStyle);
            GUI.Label(new Rect(rect.x + 10f, rect.y + 24f, rect.width - 20f, rect.height - 28f), effectText, effectStyle);
        }

        private void DrawServiceList(Rect rect, Rect panelRect, BuildingServiceEntry[] services)
        {
            FillRect(rect, new Color(0.87f, 0.72f, 0.34f, 0.055f));
            DrawOutline(rect, new Color(0.87f, 0.72f, 0.34f, 0.18f));

            BuildingServiceEntry? hovered = null;
            var hoveredRect = Rect.zero;
            for (var i = 0; i < services.Length; i++)
            {
                var rowRect = new Rect(rect.x + 8f, rect.y + 7f + i * 30f, rect.width - 16f, 26f);
                var isHovered = rowRect.Contains(Event.current.mousePosition);
                FillRect(rowRect, isHovered ? new Color(1f, 0.88f, 0.42f, 0.13f) : new Color(1f, 1f, 1f, 0.04f));
                var titleX = rowRect.x + 8f;
                var titleWidth = rowRect.width * 0.58f;
                if (!string.IsNullOrEmpty(services[i].LevelText))
                {
                    DrawServiceLevelBadge(new Rect(rowRect.x + 8f, rowRect.y + 4f, 48f, rowRect.height - 8f), services[i].LevelText);
                    titleX += 58f;
                    titleWidth = Mathf.Max(60f, titleWidth - 58f);
                }

                var hasAction = !string.IsNullOrEmpty(services[i].ActionLabel);
                var buttonWidth = hasAction ? 78f : 0f;
                var priceWidth = hasAction ? 86f : rowRect.width * 0.39f - 8f;
                var priceX = hasAction ? rowRect.xMax - buttonWidth - priceWidth - 8f : rowRect.x + rowRect.width * 0.58f;
                titleWidth = Mathf.Max(56f, priceX - titleX - 8f);
                GUI.Label(new Rect(titleX, rowRect.y, titleWidth, rowRect.height), services[i].Title, serviceTitleStyle);
                GUI.Label(new Rect(priceX, rowRect.y, priceWidth, rowRect.height), services[i].Price, servicePriceStyle);
                if (hasAction)
                {
                    GUI.enabled = services[i].ActionEnabled;
                    if (GUI.Button(new Rect(rowRect.xMax - buttonWidth, rowRect.y + 2f, buttonWidth, rowRect.height - 4f), services[i].ActionLabel, serviceButtonStyle))
                    {
                        GameAudioController.PlayUi(services[i].ActionEnabled ? GameSfx.HudConfirm : GameSfx.HudBlocked);
                        buildingServiceActionHandler?.Invoke(selectedBuilding.Type, i);
                    }

                    GUI.enabled = true;
                }

                if (isHovered)
                {
                    hovered = services[i];
                    hoveredRect = rowRect;
                }
            }

            if (hovered.HasValue)
            {
                DrawServiceTooltip(hoveredRect, panelRect, hovered.Value);
            }
        }

        private void DrawServiceLevelBadge(Rect rect, string text)
        {
            FillRect(rect, new Color(0.87f, 0.72f, 0.34f, 0.13f));
            DrawOutline(rect, new Color(0.87f, 0.72f, 0.34f, 0.36f));
            GUI.Label(rect, text, typeBadgeStyle);
        }

        private void DrawServiceTooltip(Rect sourceRect, Rect panelRect, BuildingServiceEntry service)
        {
            const float tooltipWidth = 300f;
            const float gap = 16f;
            var bodyHeight = Mathf.Clamp(tooltipBodyStyle.CalcHeight(new GUIContent(service.Description), tooltipWidth - 20f), 36f, 130f);
            var tooltipHeight = Mathf.Clamp(bodyHeight + 43f, 82f, 174f);
            var showOnLeft = sourceRect.x - tooltipWidth - gap >= 10f;
            var x = showOnLeft
                ? sourceRect.x - tooltipWidth - gap
                : Mathf.Min(Screen.width - tooltipWidth - 10f, panelRect.xMax + gap);
            var y = Mathf.Clamp(sourceRect.center.y - tooltipHeight * 0.5f, 10f, Screen.height - tooltipHeight - 10f);
            var rect = new Rect(x, y, tooltipWidth, tooltipHeight);

            DrawServiceTooltipConnector(sourceRect, rect, showOnLeft);
            FillRect(rect, new Color(0.07f, 0.065f, 0.055f, 0.98f));
            FillRect(new Rect(rect.x, rect.y, rect.width, 2f), new Color(0.92f, 0.72f, 0.28f, 0.95f));
            DrawOutline(rect, new Color(0f, 0f, 0f, 0.85f));
            GUI.Label(new Rect(rect.x + 10f, rect.y + 7f, rect.width - 20f, 22f), service.Title, tooltipTitleStyle);
            GUI.Label(new Rect(rect.x + 10f, rect.y + 31f, rect.width - 20f, bodyHeight), service.Description, tooltipBodyStyle);
        }

        private static void DrawServiceTooltipConnector(Rect sourceRect, Rect tooltipRect, bool showOnLeft)
        {
            var color = new Color(0.92f, 0.72f, 0.28f, 0.92f);
            var y = sourceRect.center.y;
            if (showOnLeft)
            {
                FillRect(new Rect(tooltipRect.xMax + 2f, y - 1f, Mathf.Max(1f, sourceRect.x - tooltipRect.xMax - 4f), 2f), color);
                FillRect(new Rect(tooltipRect.xMax + 2f, y - 4f, 5f, 2f), color);
                FillRect(new Rect(tooltipRect.xMax + 2f, y - 1f, 7f, 2f), color);
                FillRect(new Rect(tooltipRect.xMax + 2f, y + 2f, 5f, 2f), color);
                return;
            }

            FillRect(new Rect(sourceRect.xMax + 2f, y - 1f, Mathf.Max(1f, tooltipRect.x - sourceRect.xMax - 4f), 2f), color);
            FillRect(new Rect(tooltipRect.x - 7f, y - 4f, 5f, 2f), color);
            FillRect(new Rect(tooltipRect.x - 9f, y - 1f, 7f, 2f), color);
            FillRect(new Rect(tooltipRect.x - 7f, y + 2f, 5f, 2f), color);
        }

        private void DrawBuildingIcon(Rect rect, BuildingType type)
        {
            switch (type)
            {
                case BuildingType.Castle:
                    FillRect(new Rect(rect.x + 8f, rect.y + 15f, 28f, 22f), new Color(0.62f, 0.64f, 0.66f));
                    FillRect(new Rect(rect.x + 12f, rect.y + 5f, 20f, 14f), new Color(0.52f, 0.12f, 0.12f));
                    FillRect(new Rect(rect.x + 3f, rect.y + 19f, 7f, 18f), new Color(0.38f, 0.42f, 0.46f));
                    FillRect(new Rect(rect.x + 34f, rect.y + 19f, 7f, 18f), new Color(0.38f, 0.42f, 0.46f));
                    return;
                case BuildingType.Farm:
                    FillRect(new Rect(rect.x + 6f, rect.y + 23f, 32f, 14f), new Color(0.62f, 0.38f, 0.15f));
                    FillRect(new Rect(rect.x + 12f, rect.y + 12f, 20f, 15f), new Color(0.68f, 0.1f, 0.08f));
                    FillRect(new Rect(rect.x + 28f, rect.y + 10f, 5f, 12f), new Color(0.4f, 0.76f, 0.28f));
                    return;
                case BuildingType.LumberjackCamp:
                    FillRect(new Rect(rect.x + 8f, rect.y + 25f, 30f, 12f), new Color(0.44f, 0.24f, 0.08f));
                    DrawCircle(new Rect(rect.x + 8f, rect.y + 8f, 14f, 14f), new Color(0.2f, 0.62f, 0.22f));
                    DrawCircle(new Rect(rect.x + 22f, rect.y + 6f, 17f, 17f), new Color(0.25f, 0.7f, 0.27f));
                    return;
                case BuildingType.AlchemistShop:
                    DrawCircle(new Rect(rect.x + 13f, rect.y + 16f, 18f, 18f), new Color(0.2f, 0.86f, 0.92f));
                    FillRect(new Rect(rect.x + 18f, rect.y + 7f, 8f, 12f), new Color(0.84f, 0.92f, 1f));
                    return;
                case BuildingType.Tavern:
                    FillRect(new Rect(rect.x + 9f, rect.y + 19f, 26f, 18f), new Color(0.58f, 0.34f, 0.16f));
                    FillRect(new Rect(rect.x + 6f, rect.y + 11f, 32f, 10f), new Color(0.46f, 0.14f, 0.1f));
                    return;
                case BuildingType.Forge:
                    FillRect(new Rect(rect.x + 9f, rect.y + 22f, 26f, 15f), new Color(0.36f, 0.36f, 0.38f));
                    FillRect(new Rect(rect.x + 14f, rect.y + 11f, 16f, 13f), new Color(0.9f, 0.34f, 0.16f));
                    return;
                case BuildingType.Infirmary:
                    FillRect(new Rect(rect.x + 9f, rect.y + 21f, 26f, 16f), new Color(0.8f, 0.78f, 0.7f));
                    FillRect(new Rect(rect.x + 7f, rect.y + 12f, 30f, 10f), new Color(0.46f, 0.09f, 0.09f));
                    FillRect(new Rect(rect.x + 18f, rect.y + 10f, 8f, 26f), new Color(0.9f, 0.15f, 0.15f));
                    FillRect(new Rect(rect.x + 10f, rect.y + 19f, 24f, 8f), new Color(0.9f, 0.15f, 0.15f));
                    return;
                case BuildingType.CartographerHouse:
                    FillRect(new Rect(rect.x + 8f, rect.y + 21f, 28f, 16f), new Color(0.5f, 0.4f, 0.24f));
                    FillRect(new Rect(rect.x + 7f, rect.y + 11f, 30f, 10f), new Color(0.12f, 0.22f, 0.4f));
                    FillRect(new Rect(rect.x + 13f, rect.y + 25f, 18f, 8f), new Color(0.86f, 0.74f, 0.48f));
                    FillRect(new Rect(rect.x + 16f, rect.y + 28f, 12f, 2f), new Color(0.05f, 0.06f, 0.07f));
                    return;
                case BuildingType.Chapel:
                    FillRect(new Rect(rect.x + 10f, rect.y + 20f, 24f, 17f), new Color(0.68f, 0.66f, 0.58f));
                    FillRect(new Rect(rect.x + 8f, rect.y + 11f, 28f, 10f), new Color(0.34f, 0.12f, 0.16f));
                    FillRect(new Rect(rect.x + 20f, rect.y + 5f, 5f, 28f), new Color(1f, 0.78f, 0.24f));
                    FillRect(new Rect(rect.x + 14f, rect.y + 11f, 17f, 5f), new Color(1f, 0.78f, 0.24f));
                    return;
                case BuildingType.MinersGuild:
                    FillRect(new Rect(rect.x + 8f, rect.y + 21f, 28f, 16f), new Color(0.42f, 0.34f, 0.24f));
                    FillRect(new Rect(rect.x + 7f, rect.y + 11f, 30f, 10f), new Color(0.18f, 0.14f, 0.11f));
                    FillRect(new Rect(rect.x + 16f, rect.y + 8f, 5f, 28f), new Color(0.32f, 0.18f, 0.08f));
                    FillRect(new Rect(rect.x + 17f, rect.y + 8f, 20f, 5f), new Color(0.62f, 0.64f, 0.66f));
                    return;
                case BuildingType.Market:
                    FillRect(new Rect(rect.x + 8f, rect.y + 24f, 28f, 13f), new Color(0.5f, 0.28f, 0.1f));
                    FillRect(new Rect(rect.x + 6f, rect.y + 12f, 32f, 11f), new Color(0.72f, 0.18f, 0.12f));
                    FillRect(new Rect(rect.x + 10f, rect.y + 8f, 24f, 5f), new Color(0.94f, 0.78f, 0.28f));
                    DrawCircle(new Rect(rect.x + 15f, rect.y + 24f, 8f, 8f), new Color(1f, 0.74f, 0.2f));
                    DrawCircle(new Rect(rect.x + 23f, rect.y + 24f, 8f, 8f), new Color(1f, 0.74f, 0.2f));
                    return;
                case BuildingType.Antiquary:
                    FillRect(new Rect(rect.x + 9f, rect.y + 20f, 26f, 17f), new Color(0.36f, 0.28f, 0.22f));
                    FillRect(new Rect(rect.x + 7f, rect.y + 11f, 30f, 10f), new Color(0.18f, 0.1f, 0.16f));
                    DrawCircle(new Rect(rect.x + 15f, rect.y + 18f, 14f, 14f), new Color(0.38f, 0.72f, 1f));
                    FillRect(new Rect(rect.x + 20f, rect.y + 7f, 4f, 28f), new Color(0.92f, 0.66f, 0.22f));
                    return;
                default:
                    FillRect(new Rect(rect.x + 9f, rect.y + 19f, 26f, 18f), new Color(0.54f, 0.38f, 0.22f));
                    FillRect(new Rect(rect.x + 7f, rect.y + 11f, 30f, 10f), new Color(0.36f, 0.16f, 0.1f));
                    return;
            }
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

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 20,
                fontStyle = FontStyle.Bold
            };
            titleStyle.normal.textColor = new Color(0.96f, 0.93f, 0.86f);
            subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 12,
                fontStyle = FontStyle.Italic
            };
            subtitleStyle.normal.textColor = new Color(0.72f, 0.7f, 0.64f);
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };
            labelStyle.normal.textColor = new Color(0.73f, 0.72f, 0.68f);
            valueStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleRight,
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };
            valueStyle.normal.textColor = Color.white;
            effectStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };
            effectStyle.normal.textColor = new Color(0.92f, 0.91f, 0.86f);
            typeBadgeStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };
            typeBadgeStyle.normal.textColor = new Color(1f, 0.88f, 0.48f);
            closeButtonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };
            closeButtonStyle.normal.textColor = new Color(0.92f, 0.92f, 0.88f);
            serviceButtonStyle = new GUIStyle(closeButtonStyle);
            serviceButtonStyle.normal.textColor = new Color(1f, 0.9f, 0.6f);
            serviceTitleStyle = new GUIStyle(labelStyle)
            {
                fontSize = 12
            };
            serviceTitleStyle.normal.textColor = new Color(0.94f, 0.9f, 0.78f);
            servicePriceStyle = new GUIStyle(valueStyle)
            {
                fontSize = 12
            };
            servicePriceStyle.normal.textColor = new Color(1f, 0.84f, 0.34f);
            tooltipTitleStyle = new GUIStyle(labelStyle)
            {
                fontSize = 14
            };
            tooltipTitleStyle.normal.textColor = new Color(1f, 0.9f, 0.64f);
            tooltipBodyStyle = new GUIStyle(effectStyle)
            {
                fontSize = 12
            };
            tooltipBodyStyle.normal.textColor = new Color(0.86f, 0.86f, 0.8f);
            circleTexture = CreateCircleTexture();
        }

        private void DrawCircle(Rect rect, Color color)
        {
            var previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, circleTexture);
            GUI.color = previousColor;
        }

        private static Texture2D CreateCircleTexture()
        {
            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear
            };

            var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            var radius = size * 0.47f;
            var pixels = new Color[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var distance = Vector2.Distance(new Vector2(x, y), center);
                    pixels[y * size + x] = distance <= radius ? Color.white : Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }
    }
}
