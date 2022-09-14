using SEE.Game;
using SEE.GO;
using SEE.Utils;
using static SEE.GO.GameObjectExtensions;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Zoom actions holding data about zooming into or out of the city
    /// for a Desktop environment.
    /// </summary>
    /// <remarks>This component is attached to a desktop player in the respective
    /// desktop player prefab.</remarks>
    public class ZoomActionDesktop : ZoomAction
    {
        /// <summary>
        /// Checks for input and potentially creates new zoom commands to be executed.
        /// </summary>
        private void Update()
        {
            // Whether the user presses a keyboard shortcut to zoom into the city.
            bool zoomInto = SEEInput.ZoomInto();
            // Alternatively, the user can select the mouse wheel for zooming.
            float zoomStepsDelta = Input.mouseScrollDelta.y;
            // Whether zooming per mouse wheel was requested.
            bool zoomTowards = Mathf.Abs(zoomStepsDelta) >= 1.0f;

            if (!zoomInto && !zoomTowards)
            {
                return;
            }

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
                    ZoomState zoomState = GetZoomStateCopy(rootTransform);

                    // Zoom into city containing the currently hovered element upon the
                    // request of SEEInput.ZoomInto().
                    if (zoomInto)
                    {
                        rootTransform.parent.gameObject.MustGetComponent(out CityCursor cursor);
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
                                // Note: zoomFactor will be different from 1 because ConvertZoomStepsToZoomFactor(zoomSteps) yields 1
                                // only if zoomSteps equals 0, which is excluded because of the if condition.
                                Vector2 centerOfTableAfterZoom = zoomSteps == -(int)zoomState.currentTargetZoomSteps ? rootTransform.position.XZ() : cursor.E.ComputeCenter().XZ();
                                Vector2 toCenterOfTable = clippingPlane.CenterXZ - centerOfTableAfterZoom;
                                Vector2 zoomCenter = clippingPlane.CenterXZ - (toCenterOfTable * (zoomFactor / (zoomFactor - 1.0f)));
                                float duration = 2.0f * ZoomState.DefaultZoomDuration;
                                zoomState.PushZoomCommand(zoomCenter, zoomSteps, duration);
                            }
                        }
                    }

                    // Apply zoom steps towards the city containing the currently hovered element
                    // as requested per mouse wheel.
                    if (zoomTowards)
                    {
                        int zoomSteps = Mathf.RoundToInt(zoomStepsDelta);
                        zoomSteps = Mathf.Clamp(zoomSteps, -(int)zoomState.currentTargetZoomSteps, (int)ZoomState.ZoomMaxSteps - (int)zoomState.currentTargetZoomSteps);
                        zoomState.PushZoomCommand(hitPointOnPlane.XZ(), zoomSteps, ZoomState.DefaultZoomDuration);
                    }

                    UpdateZoomState(rootTransform, zoomState);
                }
            }
        }
    }
}
