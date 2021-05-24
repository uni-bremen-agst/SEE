using System.Collections.Generic;
using UnityEngine;

namespace SEE.Controls.Actions
{
    public class ZoomAction : MonoBehaviour
    {
        /// <summary>
        /// Zoom commands hold data about zooming into or out of the city.
        /// </summary>
        protected class ZoomCommand
        {
            internal readonly float TargetZoomSteps; // The amount of zoom steps this command is trying to reach
            internal readonly Vector2 ZoomCenter;    // The position onto where this command is supposed to zoom on the XZ-plane
            private readonly float duration;         // The amount of time in seconds that it should take to reach <see cref="TargetZoomSteps"/>
            private readonly float startTime;        // The creation time of the zoom command

            internal ZoomCommand(Vector2 zoomCenter, float targetZoomSteps, float duration)
            {
                TargetZoomSteps = targetZoomSteps;
                ZoomCenter = zoomCenter;
                this.duration = duration;
                startTime = Time.realtimeSinceStartup;
            }

            /// <returns>Whether the command has finished zooming.</returns>
            internal bool IsFinished()
            {
                bool result = Time.realtimeSinceStartup - startTime >= duration;
                return result;
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
                float result = t * TargetZoomSteps;
                return result;
            }
        }

        /// <summary>
        /// The zoom state contains the active zoom commands and further relevant data.
        /// </summary>
        protected struct ZoomState
        {
            internal const float DefaultZoomDuration = 0.1f;
            internal const uint ZoomMaxSteps = 32;
            internal const float ZoomFactor = 0.5f;          // Handles the speed in which is zoomed into the city

            internal Vector3 originalScale;                  // Original scale of city for reset
            internal List<ZoomCommand> zoomCommands;
            internal float currentTargetZoomSteps;           // The desired amount of zoom steps
            internal float currentZoomFactor;                // Current zoom factor or scale of the city, relative to its original scale

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
        /// </summary>
        private Dictionary<Transform, ZoomState> rootTransformToZoomStates = new Dictionary<Transform, ZoomState>();

        /// <summary>
        /// Executes every active zoom command. Logic is done in fixed time steps to ensure
        /// framerate independence for physical movement. This way, the result will look the same,
        /// no matter what the framerate of the game may be.
        /// </summary>
        private void FixedUpdate()
        {
            // TODO(torben): the remaining part should work both for desktop and vr and can be extracted!!!
            Dictionary<Transform, ZoomState> newDict = new Dictionary<Transform, ZoomState>(rootTransformToZoomStates.Count);
            foreach (KeyValuePair<Transform, ZoomState> pair in rootTransformToZoomStates)
            {
                Transform t = pair.Key;
                ZoomState s = pair.Value;

                if (s.zoomCommands.Count != 0)
                {
                    float zoomSteps = s.currentTargetZoomSteps;
                    int positionCount = 0;
                    Vector2 positionSum = Vector3.zero;

                    for (int i = 0; i < s.zoomCommands.Count; i++)
                    {
                        positionCount++;
                        positionSum += s.zoomCommands[i].ZoomCenter;
                        if (s.zoomCommands[i].IsFinished())
                        {
                            s.zoomCommands.RemoveAt(i--);
                        }
                        else
                        {
                            zoomSteps -= s.zoomCommands[i].TargetZoomSteps - s.zoomCommands[i].CurrentDeltaScale();
                        }
                    }
                    Vector3 averagePosition = new Vector3(positionSum.x / positionCount, t.position.y, positionSum.y / positionCount);

                    s.currentZoomFactor = ConvertZoomStepsToZoomFactor(zoomSteps);
                    Vector3 cityCenterToHitPoint = averagePosition - t.position;
                    Vector3 cityCenterToHitPointUnscaled = cityCenterToHitPoint.DividePairwise(t.localScale);

                    t.position += cityCenterToHitPoint;
                    t.localScale = s.currentZoomFactor * s.originalScale;
                    t.position -= Vector3.Scale(cityCenterToHitPointUnscaled, t.localScale);

                    // TODO(torben): i believe in desktop mode this made sure that zooming
                    // will always happen towards the current mouse position and not the
                    // starting position ? not sure... this might actually be an
                    // uninteresting feature

                    //moveState.dragStartTransformPosition += moveState.dragStartOffset;
                    //moveState.dragStartOffset = Vector3.Scale(moveState.dragCanonicalOffset, cityTransform.localScale);
                    //moveState.dragStartTransformPosition -= moveState.dragStartOffset;

                    // TODO(torben): synchronize here
                }
                else
                {
                    float lastZoomFactor = s.currentZoomFactor;
                    s.currentZoomFactor = ConvertZoomStepsToZoomFactor(s.currentTargetZoomSteps);
                    if (lastZoomFactor != s.currentZoomFactor)
                    {
                        t.localScale = s.currentZoomFactor * s.originalScale;
                    }
                }

                newDict[t] = s;
            }
            rootTransformToZoomStates = newDict;
        }

        /// <param name="transform">The transform to get the copy of its zoom state for.</param>
        /// <returns>A copy of the current zoom state of given transform or a new instance, if no
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
        /// Updates the zoom state of the given transform with a copy of the given zoom state.
        /// </summary>
        /// <param name="transform">The transform to update the zoom state for.</param>
        /// <param name="zoomState">The zoom state to use a copy of for given transform.</param>
        protected void UpdateZoomState(Transform transform, ZoomState zoomState)
        {
            rootTransformToZoomStates[transform] = zoomState;
        }

        /// <summary>
        /// Converts zoom steps to an actual zoom factor (scale factor).
        /// </summary>
        /// <param name="zoomSteps">The amount of zoom steps.</param>
        /// <returns>The zoom factor.</returns>
        protected static float ConvertZoomStepsToZoomFactor(float zoomSteps)
        {
            float result = Mathf.Pow(2, zoomSteps * ZoomState.ZoomFactor);
            return result;
        }

        /// <summary>
        /// Converts a zoom factor (scale) to actual zoom steps.
        /// </summary>
        /// <param name="zoomFactor">The zoom factor.</param>
        /// <returns>The amount of zoom steps.</returns>
        protected static float ConvertZoomFactorToZoomSteps(float zoomFactor)
        {
            float result = Mathf.Log(zoomFactor, 2) / ZoomState.ZoomFactor;
            return result;
        }
    }
}
