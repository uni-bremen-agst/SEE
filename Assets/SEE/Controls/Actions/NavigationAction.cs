using System.Collections.Generic;
using SEE.Game;
using SEE.Utils;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// The abstract navigation action handles generic interactions with the city.
    /// </summary>
    [RequireComponent(typeof(GO.Plane))]
    public abstract class NavigationAction : MonoBehaviour
    {
        /// <summary>
        /// Zoom commands hold data about zooming into or out of the city.
        /// </summary>
        internal class ZoomCommand
        {
            /// <summary>
            /// The amount of zoom steps this command is trying to reach.
            /// </summary>
            public readonly float TargetZoomSteps;

            /// <summary>
            /// The position onto where this command is supposed to zoom on the XZ-plane.
            /// </summary>
            public readonly Vector2 ZoomCenter;

            /// <summary>
            /// The amount of time in seconds that it should take to reach the target zoom stage.
            /// <see cref="TargetZoomSteps"/>.
            /// </summary>
            private readonly float duration;

            /// <summary>
            /// The creation time of the zoom command.
            /// </summary>
            private readonly float startTime;

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="zoomCenter"><see cref="ZoomCenter"/>.</param>
            /// <param name="targetZoomSteps"><see cref="TargetZoomSteps"/>.</param>
            /// <param name="duration"><see cref="this.duration"/>.</param>
            internal ZoomCommand(Vector2 zoomCenter, float targetZoomSteps, float duration)
            {
                TargetZoomSteps = targetZoomSteps;
                ZoomCenter = zoomCenter;
                this.duration = duration;
                startTime = Time.realtimeSinceStartup;
            }

            /// <summary>
            /// Whether the command has finished zooming.
            /// </summary>
            /// <returns>Whether the command has finished zooming.</returns>
            internal bool IsFinished()
            {
                return Time.realtimeSinceStartup - startTime >= duration;
            }

            /// <summary>
            /// The current delta in zoom steps, depending on the elapsed time since
            /// creation and the desired amount of target zoom steps.
            /// </summary>
            /// <returns>The current delta in scale between zero and
            /// <see cref="TargetZoomSteps"/> converted to a zoom factor.</returns>
            internal float CurrentDeltaScale()
            {
                float x = Mathf.Min((Time.realtimeSinceStartup - startTime) / duration, 1.0f);
                float t = 0.5f - 0.5f * Mathf.Cos(x * Mathf.PI);
                return t * TargetZoomSteps;
            }
        }

        /// <summary>
        /// The zoom state contains the active zoom commands and further relevant data.
        /// </summary>
        internal struct ZoomState
        {
            internal const float DefaultZoomDuration = 0.1f;
            internal const uint ZoomMaxSteps = 32;
            internal const float ZoomFactor = 0.5f;          // handles the speed in which is zoomed into the city

            internal Vector3 originalScale;                  // original scale of city for reset
            internal List<ZoomCommand> zoomCommands;
            internal float currentTargetZoomSteps;           // the desired amount of zoom steps
            internal float currentZoomFactor;                // current zoom factor or scale of the city, relative to its original scale
        }

        /// <summary>
        /// The transform of the city.
        /// </summary>
        public Transform CityTransform { get; protected set; }

        /// <summary>
        /// The current zoom state.
        /// </summary>
        internal ZoomState zoomState;

        /// <summary>
        /// The plane of the table
        /// </summary>
        [Tooltip("The area in which to draw the code city")]
        [SerializeField] public GO.Plane portalPlane;

        /// <summary>
        /// See the tooltip.
        /// </summary>
        [Tooltip("The unique ID used for network synchronization. This must be set via" +
            "inspector to ensure that every client will have the correct ID assigned" +
            "to the appropriate NavigationAction! If a GameObject contains both a" +
            "Desktop- and XRNavigationAction, those IDs must be identical.")]
        // TODO(torben): a better alternative would be to use the SEECity and hash the path of the graph or something...
        // also, this will most likely be replaced by an automatic approach. IGNORE THIS TODO, BECAUSE THIS SCRIPT WILL BE REMOVED ANYWAY!
        [SerializeField] protected int id;
        public int ID => id;

        /// <summary>
        /// Dictionary mapping the unique id to a navigation action object.
        /// </summary>
        private static readonly Dictionary<int, NavigationAction> idToActionDict = new Dictionary<int, NavigationAction>(2);

        /// <summary>
        /// Returns the navigation action of given id.
        /// </summary>
        /// <param name="id">Returns the navigation action of given id or
        /// <code>null</code>, if it does not exist.</param>
        /// <returns></returns>
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

        protected virtual void Awake()
        {
            Assertions.DisableOnCondition(this, portalPlane == null, "The culling plane must not be null!");
            Assertions.DisableOnCondition(this, idToActionDict.ContainsKey(id), "A unique ID must be assigned to every NavigationAction!");
            idToActionDict.Add(id, this);

            Update();
        }

        public virtual void Update()
        {
            Debug.Log($"NavigationAction of {name}.\n");
            Transform currentCityTransform = SceneQueries.GetCityRootNode(gameObject);
            // Nothing to be done if the city root node has not changed (including
            // the case that it was null before and is still null).
            if (currentCityTransform != CityTransform)
            {
                // The city root node has changed. This may be caused by a new city
                // root node during the visualization of an evolving graph series.
                // The new node may be valid, but could also be null (for the empty
                // graph).
                if (currentCityTransform == null)
                {
                    CityTransform = null;
                }
                else
                {
                    // There is a new valid root node. We must update the state.
                    CityTransform = currentCityTransform;

                    zoomState.originalScale = CityTransform.localScale;
                    zoomState.zoomCommands = new List<ZoomCommand>((int)ZoomState.ZoomMaxSteps);
                    zoomState.currentTargetZoomSteps = 0;
                    zoomState.currentZoomFactor = 1.0f;

                    OnCityAvailable();
                }
            }
        }

        /// <summary>
        /// Is called if the city is made available. This might be called delayed if the
        /// city is initialized on load by server.
        /// </summary>
        protected virtual void OnCityAvailable() { }

        /// <summary>
        /// Converts zoom steps to an actual zoom factor (scale factor).
        /// </summary>
        /// <param name="zoomSteps">The amount of zoom steps.</param>
        /// <returns>The zoom factor.</returns>
        protected float ConvertZoomStepsToZoomFactor(float zoomSteps)
        {
            float result = Mathf.Pow(2, zoomSteps * ZoomState.ZoomFactor);
            return result;
        }

        /// <summary>
        /// Converts a zoom factor (scale) to actual zoom steps.
        /// </summary>
        /// <param name="zoomFactor">The zoom factor.</param>
        /// <returns>The amount of zoom steps.</returns>
        protected float ConvertZoomFactorToZoomSteps(float zoomFactor)
        {
            float result = Mathf.Log(zoomFactor, 2) / ZoomState.ZoomFactor;
            return result;
        }

        /// <summary>
        /// Applies the currently active zoom commands to the city.
        /// </summary>
        /// <returns>Whether the transform of the city has been changed at all.</returns>
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

        /// <summary>
        /// Pushes a zoom command for execution. Zoom commands are automatically removed
        /// once they are finished.
        /// </summary>
        /// <param name="zoomCenter">The position to be zoomed towards.</param>
        /// <param name="zoomSteps">The desired amount of zoom steps.</param>
        /// <param name="duration">The desired duration of the zooming.</param>
        internal void PushZoomCommand(Vector2 zoomCenter, float zoomSteps, float duration)
        {
            zoomSteps = Mathf.Clamp(zoomSteps, -zoomState.currentTargetZoomSteps, ZoomState.ZoomMaxSteps - zoomState.currentTargetZoomSteps);
            if (zoomSteps != 0.0f)
            {
                float newZoomStepsInProgress = zoomState.currentTargetZoomSteps + zoomSteps;
                zoomState.zoomCommands.Add(new ZoomCommand(zoomCenter, zoomSteps, duration));
                zoomState.currentTargetZoomSteps = newZoomStepsInProgress;
            }
        }
    }
}
