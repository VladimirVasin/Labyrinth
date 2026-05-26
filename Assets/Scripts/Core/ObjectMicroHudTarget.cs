using System;
using UnityEngine;

namespace Labyrinth.Core
{
    public sealed class ObjectMicroHudTarget : MonoBehaviour
    {
        private Func<string> statusProvider;
        private Func<string> effectProvider;
        private Func<string> actionLabelProvider;
        private Func<bool> canActionProvider;
        private Action action;

        public string DisplayName { get; private set; }

        public string Subtitle { get; private set; }

        public string TypeName { get; private set; }

        public Vector2Int GridPosition { get; private set; }

        public Color AccentColor { get; private set; } = new Color(0.87f, 0.72f, 0.34f);

        public string StatusText => statusProvider != null ? statusProvider.Invoke() : string.Empty;

        public string EffectText => effectProvider != null ? effectProvider.Invoke() : string.Empty;

        public bool HasAction => action != null;

        public string ActionLabel => actionLabelProvider != null ? actionLabelProvider.Invoke() : string.Empty;

        public bool CanInvokeAction => canActionProvider == null || canActionProvider.Invoke();

        public void Configure(
            string displayName,
            string subtitle,
            string typeName,
            Vector2Int gridPosition,
            Color accentColor,
            Func<string> getStatus,
            Func<string> getEffect)
        {
            DisplayName = displayName;
            Subtitle = subtitle;
            TypeName = typeName;
            GridPosition = gridPosition;
            AccentColor = accentColor;
            statusProvider = getStatus;
            effectProvider = getEffect;
        }

        public void ConfigureAction(Func<string> getActionLabel, Func<bool> canInvoke, Action onAction)
        {
            actionLabelProvider = getActionLabel;
            canActionProvider = canInvoke;
            action = onAction;
        }

        public void InvokeAction()
        {
            if (!HasAction || !CanInvokeAction)
            {
                return;
            }

            action.Invoke();
        }
    }
}
