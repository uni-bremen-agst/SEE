using System.Collections.Generic;
using UnityEngine;

namespace SEE.Controls
{

    public abstract class NavigationAction : CityAction
    {
        protected class ZoomCommand
        {
            public readonly float TargetZoomSteps;
            public readonly Vector2 ZoomCenter;
            private readonly float duration;
            private readonly float startTime;

            internal ZoomCommand(Vector2 zoomCenter, float targetZoomSteps, float duration)
            {
                TargetZoomSteps = targetZoomSteps;
                ZoomCenter = zoomCenter;
                this.duration = duration;
                startTime = Time.realtimeSinceStartup;
            }

            internal bool IsFinished()
            {
                bool result = Time.realtimeSinceStartup - startTime >= duration;
                return result;
            }

            internal float CurrentDeltaScale()
            {
                float x = Mathf.Min((Time.realtimeSinceStartup - startTime) / duration, 1.0f);
                float t = 0.5f - 0.5f * Mathf.Cos(x * Mathf.PI);
                float result = t * TargetZoomSteps;
                return result;
            }
        }

        protected struct ZoomState
        {
            internal const float DefaultZoomDuration = 0.1f;
            internal const uint ZoomMaxSteps = 32;
            internal const float ZoomFactor = 0.5f;

            internal Vector3 originalScale;
            internal List<ZoomCommand> zoomCommands;
            internal uint currentTargetZoomSteps;
            internal float currentZoomFactor;
        }

        protected ZoomState zoomState;

        [Tooltip("The unique ID used for network synchronization. This must be set via" +
            "inspector to ensure that every client will have the correct ID assigned" +
            "to the appropriate NavigationAction! If a GameObject contains both a" +
            "Desktop- and XRNavigationAction, those IDs must be identical.")]
        // TODO(torben): a better alternative would be to use the SEECity and hash the path of the graph or something...
        [SerializeField] protected int id;
        public int ID => id;

        protected static readonly Dictionary<int, NavigationAction> idToActionDict = new Dictionary<int, NavigationAction>(2);
        public static NavigationAction Get(int id)
        {
            bool result = idToActionDict.TryGetValue(id, out NavigationAction value);
            if (result)
            {
                return value;
            }
            else
            {
                Debug.LogWarning("ID does not match any NavigationAction!");
                return null;
            }
        }

        internal void PushZoomCommand(Vector2 zoomCenter, float zoomSteps, float duration)
        {
            zoomSteps = Mathf.Clamp(zoomSteps, -(int)zoomState.currentTargetZoomSteps, (int)ZoomState.ZoomMaxSteps - (int)zoomState.currentTargetZoomSteps);
            if (zoomSteps != 0)
            {
                uint newZoomStepsInProgress = (uint)((int)zoomState.currentTargetZoomSteps + zoomSteps);
                zoomState.zoomCommands.Add(new ZoomCommand(zoomCenter, zoomSteps, duration));
                zoomState.currentTargetZoomSteps = newZoomStepsInProgress;
            }
        }
    }

}
