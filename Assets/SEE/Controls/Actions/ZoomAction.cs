using System.Collections.Generic;
using SEE.Utils;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Implements zooming into or out of a code city.
    /// </summary>
    /// <remarks>This component is attached to a player. It is used in the prefab for
    /// players. This class has subclasses specialized to the environment (desktop,
    /// VR, etc.).</remarks>
    public class ZoomAction : MonoBehaviour
    {
        /// <summary>
        /// Zoom commands hold data about zooming into or out of the city.
        /// </summary>
        protected class ZoomCommand
        {
            /// <summary>
            /// The amount of zoom steps this command is trying to reach.
            /// </summary>
            internal readonly float TargetZoomSteps;
            /// <summary>
            /// The position onto where this command is supposed to zoom on the XZ-plane.
            /// </summary>
            internal readonly Vector2 ZoomCenter;
            /// <summary>
            /// The amount of time in seconds that it should take to reach <see cref="TargetZoomSteps"/>.
            /// </summary>
            private readonly float duration;
            /// <summary>
            /// The creation time of the zoom command.
            /// </summary>
            private readonly float startTime;

            internal ZoomCommand(Vector2 zoomCenter, float targetZoomSteps, float duration)
            {
                TargetZoomSteps = targetZoomSteps;
                ZoomCenter = zoomCenter;
                this.duration = duration;
                startTime = Time.realtimeSinceStartup;
            }

            /// <summary>
            /// Returns true if the command has finished zooming, more precisely,
            /// if the specified duration of the zooming is reached.
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
        protected struct ZoomState
        {
            /// <summary>
            /// The default time to reach the requested zoom degree in seconds.
            /// </summary>
            internal const float DefaultZoomDuration = 0.1f;

            /// <summary>
            /// The maximal number of zooming steps.
            /// </summary>
            internal const uint ZoomMaxSteps = 32;
            /// <summary>
            /// Handles the speed in which we zoom into the city.
            /// </summary>
            internal const float ZoomFactor = 0.5f;

            /// <summary>
            /// Original scale of city for reset.
            /// </summary>
            internal Vector3 originalScale;

            /// <summary>
            /// The list of active zoom commands, that is, those that are still to be executed.
            /// </summary>
            internal List<ZoomCommand> zoomCommands;
            /// <summary>
            /// The desired amount of zoom steps.
            /// </summary>
            internal float currentTargetZoomSteps;
            /// <summary>
            /// Current zoom factor or scale of the city, relative to its original scale.
            /// </summary>
            internal float currentZoomFactor;

            /// <summary>
            /// Pushes a zoom command for execution. Zoom commands are automatically removed
            /// once they are finished.
            /// </summary>
            /// <param name="zoomCenter">The position to be zoomed towards.</param>
            /// <param name="zoomSteps">The desired amount of zoom steps.</param>
            /// <param name="duration">The desired duration of the zooming.</param>
            internal void PushZoomCommand(Vector2 zoomCenter, float zoomSteps, float duration)
            {
                zoomSteps = Mathf.Clamp(zoomSteps, -currentTargetZoomSteps, ZoomMaxSteps - currentTargetZoomSteps);
                if (zoomSteps != 0.0f)
                {
                    float newZoomStepsInProgress = currentTargetZoomSteps + zoomSteps;
                    zoomCommands.Add(new ZoomCommand(zoomCenter, zoomSteps, duration));
                    currentTargetZoomSteps = newZoomStepsInProgress;
                }
            }
        }

        /// <summary>
        /// The zoom states for every root transform of a city. Once the first zoom is initiated,
        /// the zoom state will be inserted here.
        ///
        /// The key of this mapping is the root game node (tagged by <see cref="Tags.Node"/>)
        /// of the game-object hierarchy. It is the game object that will be zoomed;
        /// all its descendants are scaled along with it.
        ///
        /// Because we may have multiple code cities in the scene, there is not only one such
        /// root node.
        /// </summary>
        private Dictionary<Transform, ZoomState> rootTransformToZoomStates = new Dictionary<Transform, ZoomState>();

        /// <summary>
        /// Executes every active zoom command. Logic is done in fixed time steps to ensure
        /// framerate independence for physical movement. This way, the result will look the same,
        /// no matter what the framerate of the game may be.
        /// </summary>
        private void FixedUpdate()
        {
            // This loop iterates over all code cities. Because there will be only a few
            // code cities in a scene, the number of iterations per fixed update is low.
            // We will never remove a code city from rootTransformToZoomStates because
            // a user might want to reset its scale to the original one.
            foreach (KeyValuePair<Transform, ZoomState> pair in rootTransformToZoomStates)
            {
                // The root node of the current code city to be zoomed.
                Transform transform = pair.Key;
                // Its zoom state.
                ZoomState zoomState = pair.Value;

                if (zoomState.zoomCommands.Count > 0)
                {
                    // If there is any zoom command, execute it.
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
                    Vector3 averagePosition = new Vector3(positionSum.x / positionCount, transform.position.y, positionSum.y / positionCount);

                    zoomState.currentZoomFactor = ConvertZoomStepsToZoomFactor(zoomSteps);
                    Vector3 cityCenterToHitPoint = averagePosition - transform.position;
                    Vector3 cityCenterToHitPointUnscaled = cityCenterToHitPoint.DividePairwise(transform.localScale);

                    transform.position += cityCenterToHitPoint;
                    transform.localScale = zoomState.currentZoomFactor * zoomState.originalScale;
                    transform.position -= Vector3.Scale(cityCenterToHitPointUnscaled, transform.localScale);

                    // TODO(torben): I believe in desktop mode this made sure that zooming
                    // will always happen towards the current mouse position and not the
                    // starting position ? not sure... this might actually be an
                    // uninteresting feature

                    //moveState.dragStartTransformPosition += moveState.dragStartOffset;
                    //moveState.dragStartOffset = Vector3.Scale(moveState.dragCanonicalOffset, cityTransform.localScale);
                    //moveState.dragStartTransformPosition -= moveState.dragStartOffset;
                    new Net.ZoomNetAction(transform.name, transform.position, transform.localScale).Execute();
                }
                else
                {
                    float lastZoomFactor = zoomState.currentZoomFactor;
                    zoomState.currentZoomFactor = ConvertZoomStepsToZoomFactor(zoomState.currentTargetZoomSteps);
                    if (lastZoomFactor != zoomState.currentZoomFactor)
                    {
                        transform.localScale = zoomState.currentZoomFactor * zoomState.originalScale;
                    }
                }
            }
        }

        /// <summary>
        /// Returns the zoom state of <paramref name="transform"/> if one exists or a new
        /// one if none exists.
        /// </summary>
        /// <param name="transform">The transform to get the copy of its zoom state for.</param>
        /// <returns>The current zoom state of given <paramref name="transform"/> or a new instance, if no
        /// such zoom state exists yet.</returns>
        protected ZoomState GetZoomStateCopy(Transform transform)
        {
            if (!rootTransformToZoomStates.TryGetValue(transform, out ZoomState result))
            {
                result = new ZoomState
                {
                    originalScale = transform.localScale,
                    zoomCommands = new List<ZoomCommand>((int)ZoomState.ZoomMaxSteps),
                    currentTargetZoomSteps = 0,
                    currentZoomFactor = 1.0f
                };
            }
            return result;
        }

        /// <summary>
        /// Updates the zoom state of the given <paramref name="transform"/> with
        /// the given <paramref name="zoomState"/>.
        /// </summary>
        /// <param name="transform">The transform to update the zoom state for.</param>
        /// <param name="zoomState">The zoom state to use a copy of for given transform.</param>
        protected void UpdateZoomState(Transform transform, ZoomState zoomState)
        {
            rootTransformToZoomStates[transform] = zoomState;
        }

        /// <summary>
        /// Converts zoom steps to an actual zoom factor (scale factor).
        /// The result is 2^(<paramref name="zoomSteps"/> * <see cref="ZoomState.ZoomFactor"/>).
        /// </summary>
        /// <remarks>Returns 1 only if <paramref name="zoomSteps"/> equals 0.</remarks>
        /// <param name="zoomSteps">The amount of zoom steps.</param>
        /// <returns>The zoom factor.</returns>
        protected static float ConvertZoomStepsToZoomFactor(float zoomSteps)
        {
            return Mathf.Pow(2, zoomSteps * ZoomState.ZoomFactor);
        }

        /// <summary>
        /// Converts a zoom factor (scale) to actual zoom steps.
        /// </summary>
        /// <param name="zoomFactor">The zoom factor.</param>
        /// <returns>The amount of zoom steps.</returns>
        protected static float ConvertZoomFactorToZoomSteps(float zoomFactor)
        {
            return Mathf.Log(zoomFactor, 2) / ZoomState.ZoomFactor;
        }
    }
}
