using System.Collections.Generic;
using SEE.Game.Operator;
using SEE.GO;
using SEE.Net.Actions;
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
            /// Should the position be reset to original position?
            /// </summary>
            internal readonly bool ResetPosition;
            /// <summary>
            /// The amount of time in seconds that it should take to reach <see cref="TargetZoomSteps"/>.
            /// </summary>
            private readonly float duration;
            /// <summary>
            /// The creation time of the zoom command.
            /// </summary>
            private readonly float startTime;

            internal ZoomCommand(Vector2 zoomCenter, float targetZoomSteps, float duration, bool resetPosition = false)
            {
                TargetZoomSteps = targetZoomSteps;
                ZoomCenter = zoomCenter;
                this.duration = duration;
                startTime = Time.realtimeSinceStartup;
                ResetPosition = resetPosition;
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
            internal const uint ZoomMaxSteps = 10;
            /// <summary>
            /// Handles the speed in which we zoom into the city.
            /// </summary>
            internal const float ZoomFactor = 0.5f;

            /// <summary>
            /// Original local scale of city for reset.
            /// </summary>
            internal Vector3 OriginalLocalScale;
            /// <summary>
            /// Original world-space position of city for reset.
            /// </summary>
            internal Vector3 OriginalPosition;

            /// <summary>
            /// The list of active zoom commands, that is, those that are still to be executed.
            /// </summary>
            internal List<ZoomCommand> ZoomCommands;
            /// <summary>
            /// The desired amount of zoom steps.
            /// </summary>
            internal float CurrentTargetZoomSteps;
            /// <summary>
            /// Current zoom factor or scale of the city, relative to its original scale.
            /// </summary>
            internal float CurrentZoomFactor;

            /// <summary>
            /// Pushes a zoom command for execution. Zoom commands are automatically removed
            /// once they are finished.
            /// <para>
            /// The position will automatically be reset if <see cref="CurrentTargetZoomSteps"/> +
            /// <paramref name="zoomSteps"/> equals 0, i.e., the zoom factor reaches 1.
            /// Otherwise, <paramref name="zoomCenter"/> is used for calculating the new position.
            /// </para>
            /// </summary>
            /// <param name="zoomCenter">The position to be zoomed towards.</param>
            /// <param name="zoomSteps">The desired amount of zoom steps.</param>
            /// <param name="duration">The desired duration of the zooming.</param>
            internal void PushZoomCommand(Vector2 zoomCenter, float zoomSteps, float duration)
            {
                zoomSteps = Mathf.Clamp(zoomSteps, -CurrentTargetZoomSteps, ZoomMaxSteps - CurrentTargetZoomSteps);
                if (zoomSteps != 0.0f)
                {
                    float newZoomStepsInProgress = CurrentTargetZoomSteps + zoomSteps;
                    ZoomCommands.Add(new ZoomCommand(zoomCenter, zoomSteps, duration, Mathf.Approximately(CurrentTargetZoomSteps + zoomSteps, 0)));
                    CurrentTargetZoomSteps = newZoomStepsInProgress;
                }
            }

            /// <summary>
            /// Pushes a zoom command for execution that will reset the code city to its
            /// original un-zoomed state. Zoom commands are automatically removed
            /// once they are finished.
            /// <para>
            /// <see cref="CurrentTargetZoomSteps"/> will be reset to 0.
            /// </para>
            /// </summary>
            /// <param name="duration">The desired duration of the zooming.</param>
            internal void PushResetCommand(float duration)
            {
                CurrentTargetZoomSteps = 0;
                ZoomCommands.Add(new ZoomCommand(Vector2.zero, CurrentTargetZoomSteps, duration, true));
            }
        }

        /// <summary>
        /// The zoom states for every root transform of a city. Once the first zoom is initiated,
        /// the zoom state will be inserted here.
        /// <para>
        /// The key of this mapping is the root game node (tagged by <see cref="Tags.Node"/>)
        /// of the game-object hierarchy. It is the game object that will be zoomed;
        /// all its descendants are scaled along with it.
        /// </para>
        /// Because we may have multiple code cities in the scene, there is not only one such
        /// root node.
        /// </summary>
        private readonly Dictionary<Transform, ZoomState> rootTransformToZoomStates = new();

        /// <summary>
        /// The node operator for every root transform of a city.
        /// </summary>
        private readonly Dictionary<Transform, NodeOperator> rootTransformToOperator = new();

        /// <summary>
        /// Factor to apply to the animation.
        /// </summary>
        public const float AnimationFactor = 0.5f;

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
                Transform rootTransform = pair.Key;
                // Its zoom state.
                ZoomState zoomState = pair.Value;
                // Its node operator.
                NodeOperator nodeOperator = rootTransformToOperator[rootTransform];

                if (zoomState.ZoomCommands.Count > 0)
                {
                    // If there is any zoom command, execute it.
                    float zoomSteps = zoomState.CurrentTargetZoomSteps;
                    int positionCount = 0;
                    Vector2 positionSum = Vector2.zero;

                    bool resetPosition = false;
                    for (int i = 0; i < zoomState.ZoomCommands.Count; i++)
                    {
                        positionCount++;
                        positionSum += zoomState.ZoomCommands[i].ZoomCenter;
                        resetPosition = zoomState.ZoomCommands[i].ResetPosition;
                        if (zoomState.ZoomCommands[i].IsFinished())
                        {
                            zoomState.ZoomCommands.RemoveAt(i--);
                        }
                        else
                        {
                            zoomSteps -= zoomState.ZoomCommands[i].TargetZoomSteps - zoomState.ZoomCommands[i].CurrentDeltaScale();
                        }
                    }

                    zoomState.CurrentZoomFactor = ConvertZoomStepsToZoomFactor(zoomSteps);
                    Vector3 targetScale = zoomState.CurrentZoomFactor * zoomState.OriginalLocalScale;
                    Vector3 targetPosition = zoomState.OriginalPosition;
                    if (!resetPosition)
                    {
                        Vector3 averagePosition = new(positionSum.x / positionCount, rootTransform.position.y, positionSum.y / positionCount);
                        Vector3 cityCenterToHitPoint = averagePosition - rootTransform.position;
                        Vector3 cityCenterToHitPointUnscaled = cityCenterToHitPoint.DividePairwise(rootTransform.localScale);
                        targetPosition = rootTransform.position + cityCenterToHitPoint - Vector3.Scale(cityCenterToHitPointUnscaled, nodeOperator.TargetScale);
                    }

                    nodeOperator.ResizeTo(targetScale, targetPosition, AnimationFactor, true, false, true);
                    new ResizeNodeNetAction(rootTransform.name, targetScale, targetPosition, true, false, true, AnimationFactor).Execute();
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
                    OriginalLocalScale = transform.localScale,
                    OriginalPosition = transform.position,
                    ZoomCommands = new List<ZoomCommand>((int)ZoomState.ZoomMaxSteps),
                    CurrentTargetZoomSteps = 0,
                    CurrentZoomFactor = 1.0f
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
            rootTransformToOperator[transform] = transform.gameObject.NodeOperator();
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
