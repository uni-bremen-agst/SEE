using SEE.Game;
using SEE.GO;
using SEE.Utils;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Zoom actions holding data about zooming into or out of the city
    /// for a Desktop environment.
    /// </summary>
    public class ZoomActionDesktop : ZoomAction
    {
        /// <summary>
        /// Checks for input and potentially creates new zoom commands to be executed.
        /// </summary>
        private void Update()
        {
            InteractableObject obj = InteractableObject.HoveredObjectWithWorldFlag;

            // If we don't hover over any part of a city, we can't initiate any zooming related action
            if (obj)
            {
                Transform rootTransform = SceneQueries.GetCityRootTransformUpwards(obj.transform);
                if (rootTransform == null)
                {
                    Debug.LogError($"ZoomActionDesktop.Update received null rootTransform for hovered {obj.name}.\n");
                    return;
                }
                else if (rootTransform.parent == null)
                {
                    Debug.LogError($"ZoomActionDesktop.Update: rootTransform for hovered {obj.name} has no parent.\n");
                    return;
                }
                if (!rootTransform.parent.TryGetComponent(out GO.Plane clippingPlane) || clippingPlane == null)
                {
                    Debug.LogError($"ZoomActionDesktop.Update: parent for hovered {obj.name} has no {typeof(GO.Plane)}.\n");
                    return;
                }

                Raycasting.RaycastClippingPlane(clippingPlane, out _, out bool hitInsideClippingArea, out Vector3 hitPointOnPlane);

                // We need to hit something inside of its clipping area
                // TODO(torben): do we even want to hover things that are not inside of the clipping area? look at InteractableObject for that
                if (hitInsideClippingArea)
                {
                    float zoomStepsDelta = Input.mouseScrollDelta.y;
                    bool zoomInto = SEEInput.ZoomInto();
                    bool zoomTowards = Mathf.Abs(zoomStepsDelta) >= 1.0f;

                    if (zoomInto || zoomTowards)
                    {
                        ZoomState zoomState = GetZoomStateCopy(rootTransform);

                        // Zoom into city containing the currently hovered element
                        if (zoomInto)
                        {
                            CityCursor cursor = rootTransform.parent.GetComponent<CityCursor>();
                            if (cursor.E.HasFocus())
                            {
                                float optimalTargetZoomFactor = clippingPlane.MinLengthXZ / (cursor.E.ComputeDiameterXZ() / zoomState.currentZoomFactor);
                                float optimalTargetZoomSteps = ConvertZoomFactorToZoomSteps(optimalTargetZoomFactor);
                                int actualTargetZoomSteps = Mathf.FloorToInt(optimalTargetZoomSteps);

                                int zoomSteps = actualTargetZoomSteps - (int)zoomState.currentTargetZoomSteps;
                                if (zoomSteps == 0)
                                {
                                    zoomSteps = -(int)zoomState.currentTargetZoomSteps;
                                }

                                if (zoomSteps != 0)
                                {
                                    float zoomFactor = ConvertZoomStepsToZoomFactor(zoomSteps);
                                    Vector2 centerOfTableAfterZoom = zoomSteps == -(int)zoomState.currentTargetZoomSteps ? rootTransform.position.XZ() : cursor.E.ComputeCenter().XZ();
                                    Vector2 toCenterOfTable = clippingPlane.CenterXZ - centerOfTableAfterZoom;
                                    Vector2 zoomCenter = clippingPlane.CenterXZ - (toCenterOfTable * (zoomFactor / (zoomFactor - 1.0f)));
                                    float duration = 2.0f * ZoomState.DefaultZoomDuration;
                                    zoomState.PushZoomCommand(zoomCenter, zoomSteps, duration);
                                }
                            }
                        }

                        // Apply zoom steps towards the city containing the currently hovered element
                        if (zoomTowards)
                        {
                            int zoomSteps = zoomStepsDelta >= 0 ? Mathf.FloorToInt(zoomStepsDelta) : Mathf.FloorToInt(zoomStepsDelta);
                            zoomSteps = Mathf.Clamp(zoomSteps, -(int)zoomState.currentTargetZoomSteps, (int)ZoomState.ZoomMaxSteps - (int)zoomState.currentTargetZoomSteps);
                            zoomState.PushZoomCommand(hitPointOnPlane.XZ(), zoomSteps, ZoomState.DefaultZoomDuration);
                        }

                        UpdateZoomState(rootTransform, zoomState);
                    }
                }
            }
        }
    }
}
