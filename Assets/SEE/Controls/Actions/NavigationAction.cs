using System.Collections.Generic;
using UnityEngine;

namespace SEE.Controls
{

    public abstract class NavigationAction : CityAction
    {
        internal class ZoomCommand
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

        internal struct ZoomState
        {
            internal const float DefaultZoomDuration = 0.1f;
            internal const uint ZoomMaxSteps = 32;
            internal const float ZoomFactor = 0.5f;

            internal Vector3 originalScale;
            internal List<ZoomCommand> zoomCommands;
            internal float currentTargetZoomSteps;
            internal float currentZoomFactor;
        }

        public Transform CityTransform { get; protected set; }
        internal ZoomState zoomState;

        [Tooltip("The area in which to draw the code city")]
        [SerializeField] protected Plane portalPlane;

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
            zoomSteps = Mathf.Clamp(zoomSteps, -zoomState.currentTargetZoomSteps, (float)ZoomState.ZoomMaxSteps - (float)zoomState.currentTargetZoomSteps);
            if (zoomSteps != 0.0f)
            {
                float newZoomStepsInProgress = zoomState.currentTargetZoomSteps + zoomSteps;
                zoomState.zoomCommands.Add(new ZoomCommand(zoomCenter, zoomSteps, duration));
                zoomState.currentTargetZoomSteps = newZoomStepsInProgress;
            }
        }

        protected virtual void Start()
        {
            UnityEngine.Assertions.Assert.IsNotNull(portalPlane, "The culling plane must not be null!");
            UnityEngine.Assertions.Assert.IsTrue(!idToActionDict.ContainsKey(id), "A unique ID must be assigned to every NavigationAction!");
            idToActionDict.Add(id, this);

            CityTransform = GetCityRootNode(gameObject);
            UnityEngine.Assertions.Assert.IsNotNull(CityTransform, "This NavigationAction is not attached to a code city!");

            zoomState.originalScale = CityTransform.localScale;
            zoomState.zoomCommands = new List<ZoomCommand>((int)ZoomState.ZoomMaxSteps);
            zoomState.currentTargetZoomSteps = 0;
            zoomState.currentZoomFactor = 1.0f;
        }

        protected float ConvertZoomStepsToZoomFactor(float zoomSteps)
        {
            float result = Mathf.Pow(2, zoomSteps * ZoomState.ZoomFactor);
            return result;
        }

        protected float ConvertZoomFactorToZoomSteps(float zoomFactor)
        {
            float result = Mathf.Log(zoomFactor, 2) / ZoomState.ZoomFactor;
            return result;
        }

        protected bool UpdateZoom()
        {
            bool hasChanged = false;

            if (zoomState.zoomCommands.Count != 0)
            {
                hasChanged = true;

                float zoomSteps = zoomState.currentTargetZoomSteps;
                int positionCount = 0;
                Vector2 positionSum = Vector3.zero;

                for (int i = 0; i < zoomState.zoomCommands.Count; i++)
                {
                    positionCount++;
                    positionSum += zoomState.zoomCommands[i].ZoomCenter;
                    if (zoomState.zoomCommands[i].IsFinished())
                    {
                        zoomState.zoomCommands.RemoveAt(i--);
                    }
                    else
                    {
                        zoomSteps -= zoomState.zoomCommands[i].TargetZoomSteps - zoomState.zoomCommands[i].CurrentDeltaScale();
                    }
                }
                Vector3 averagePosition = new Vector3(positionSum.x / positionCount, CityTransform.position.y, positionSum.y / positionCount);

                zoomState.currentZoomFactor = ConvertZoomStepsToZoomFactor(zoomSteps);
                Vector3 cityCenterToHitPoint = averagePosition - CityTransform.position;
                Vector3 cityCenterToHitPointUnscaled = cityCenterToHitPoint.DividePairwise(CityTransform.localScale);

                CityTransform.position += cityCenterToHitPoint;
                CityTransform.localScale = zoomState.currentZoomFactor * zoomState.originalScale;
                CityTransform.position -= Vector3.Scale(cityCenterToHitPointUnscaled, CityTransform.localScale);

                // TODO(torben): i believe in desktop mode this made sure that zooming
                // will always happen towards the current mouse position and not the
                // starting position ? not sure... this might actually be an
                // uninteresting feature

                //moveState.dragStartTransformPosition += moveState.dragStartOffset;
                //moveState.dragStartOffset = Vector3.Scale(moveState.dragCanonicalOffset, cityTransform.localScale);
                //moveState.dragStartTransformPosition -= moveState.dragStartOffset;
            }
            else
            {
                float lastZoomFactor = zoomState.currentZoomFactor;
                zoomState.currentZoomFactor = ConvertZoomStepsToZoomFactor(zoomState.currentTargetZoomSteps);
                if (lastZoomFactor != zoomState.currentZoomFactor)
                {
                    Vector3 scale = zoomState.currentZoomFactor * zoomState.originalScale;
                    CityTransform.localScale = zoomState.currentZoomFactor * zoomState.originalScale;
                }
            }

            return hasChanged;
        }
    }

}
