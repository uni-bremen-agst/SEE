using SEE.Game;
using SEE.GO;
using SEE.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Controls.Actions
{
    public class ZoomAction : MonoBehaviour
    {
        /// <summary>
        /// Zoom commands hold data about zooming into or out of the city.
        /// </summary>
        private class ZoomCommand
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
        private struct ZoomState
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

        private Dictionary<Transform, ZoomState> rootTransformToZoomStates = new Dictionary<Transform, ZoomState>();

        /// <summary>
        /// Applies the currently active zoom commands to the city.
        /// </summary>
        /// <returns>Whether the transform of the city has been changed at all.</returns>
        private void Update()
        {
            InteractableObject obj = InteractableObject.HoveredObjectWithWorldFlag;

            // If we don't hover over any part of a city, we can't initiate any zooming related action
            if (obj)
            {
                Transform rootTransform = SceneQueries.GetCityRootTransformUpwards(obj.transform);
                UnityEngine.Plane raycastPlane = new UnityEngine.Plane(Vector3.up, rootTransform.position);
                GO.Plane clippingPlane = rootTransform.parent.GetComponent<GO.Plane>();
                Raycasting.RaycastClippingPlane(raycastPlane, clippingPlane, out _, out bool hitInsideClippingArea, out Vector3 hitPointOnPlane);

                // We need to hit something inside of its clipping area
                // TODO(torben): do we even want to hover things that are not inside of the clipping area? look at InteractableObject for that
                if (hitInsideClippingArea)
                {
                    float zoomStepsDelta = Input.mouseScrollDelta.y;
                    bool zoomInto = SEEInput.ZoomInto();
                    bool zoomTowards = Mathf.Abs(zoomStepsDelta) >= 1.0f;

                    if (zoomInto || zoomTowards)
                    {
                        // Determine zoomable transform
                        if (!rootTransformToZoomStates.TryGetValue(rootTransform, out ZoomState hitZoomState))
                        {
                            hitZoomState = new ZoomState
                            {
                                originalScale = rootTransform.localScale,
                                zoomCommands = new List<ZoomCommand>((int)ZoomState.ZoomMaxSteps),
                                currentTargetZoomSteps = 0,
                                currentZoomFactor = 1.0f
                            };
                        }

                        // Zoom into city containing the currently hovered element
                        if (zoomInto)
                        {
                            CityCursor cursor = rootTransform.parent.GetComponent<CityCursor>();
                            if (cursor.E.HasFocus())
                            {
                                float optimalTargetZoomFactor = clippingPlane.MinLengthXZ / (cursor.E.ComputeDiameterXZ() / hitZoomState.currentZoomFactor);
                                float optimalTargetZoomSteps = ConvertZoomFactorToZoomSteps(optimalTargetZoomFactor);
                                int actualTargetZoomSteps = Mathf.FloorToInt(optimalTargetZoomSteps);

                                int zoomSteps = actualTargetZoomSteps - (int)hitZoomState.currentTargetZoomSteps;
                                if (zoomSteps == 0)
                                {
                                    zoomSteps = -(int)hitZoomState.currentTargetZoomSteps;
                                }

                                if (zoomSteps != 0)
                                {
                                    float zoomFactor = ConvertZoomStepsToZoomFactor(zoomSteps);
                                    Vector2 centerOfTableAfterZoom = zoomSteps == -(int)hitZoomState.currentTargetZoomSteps ? rootTransform.position.XZ() : cursor.E.ComputeCenter().XZ();
                                    Vector2 toCenterOfTable = clippingPlane.CenterXZ - centerOfTableAfterZoom;
                                    Vector2 zoomCenter = clippingPlane.CenterXZ - (toCenterOfTable * (zoomFactor / (zoomFactor - 1.0f)));
                                    float duration = 2.0f * ZoomState.DefaultZoomDuration;
                                    hitZoomState.PushZoomCommand(zoomCenter, zoomSteps, duration);
                                }
                            }
                        }

                        // Apply zoom steps towards the city containing the currently hovered element
                        if (zoomTowards)
                        {
                            int zoomSteps = zoomStepsDelta >= 0 ? Mathf.FloorToInt(zoomStepsDelta) : Mathf.FloorToInt(zoomStepsDelta);
                            zoomSteps = Mathf.Clamp(zoomSteps, -(int)hitZoomState.currentTargetZoomSteps, (int)ZoomState.ZoomMaxSteps - (int)hitZoomState.currentTargetZoomSteps);
                            hitZoomState.PushZoomCommand(hitPointOnPlane.XZ(), zoomSteps, ZoomState.DefaultZoomDuration);
                        }

                        // Note: This is the only place we ever insert a zoom state. The reason for the insertion here is that a ZoomState is a struct and we only ever modify a copy of it.
                        rootTransformToZoomStates[rootTransform] = hitZoomState;
                    }
                }
            }

            // TODO(torben): the remaining part should work both for desktop and vr and can be extracted!!!
            // TODO(torben): put into FixedUpdate!
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

        /// <summary>
        /// Converts zoom steps to an actual zoom factor (scale factor).
        /// </summary>
        /// <param name="zoomSteps">The amount of zoom steps.</param>
        /// <returns>The zoom factor.</returns>
        private static float ConvertZoomStepsToZoomFactor(float zoomSteps)
        {
            float result = Mathf.Pow(2, zoomSteps * ZoomState.ZoomFactor);
            return result;
        }

        /// <summary>
        /// Converts a zoom factor (scale) to actual zoom steps.
        /// </summary>
        /// <param name="zoomFactor">The zoom factor.</param>
        /// <returns>The amount of zoom steps.</returns>
        private static float ConvertZoomFactorToZoomSteps(float zoomFactor)
        {
            float result = Mathf.Log(zoomFactor, 2) / ZoomState.ZoomFactor;
            return result;
        }
    }
}
