using SEE.Game;
using SEE.Utils;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Zoom actions holding data about zooming into or out of the city
    /// for a Mobile environment.
    /// </summary>
    public class ZoomActionMobile : ZoomAction
    {
        /// <summary>
        /// Checks for input and potentially creates new zoom commands to be executed.
        /// </summary>
        private void Update()
        {
            if (Input.touchCount != 2)
            {
                return;
            }
            Touch firstTouch = Input.GetTouch(0);
            Touch secondTouch = Input.GetTouch(1);
            Transform cityRootNodeFirstTouch = null;
            Transform cityRootNodeSecondTouch = null;
            Ray rayFirstTouch = Camera.main.ScreenPointToRay(firstTouch.position);
            Ray raySecondTouch = Camera.main.ScreenPointToRay(secondTouch.position);

            RaycastHit raycastHitFirstTouch;
            if (Physics.Raycast(rayFirstTouch, out raycastHitFirstTouch))
            {
                if (raycastHitFirstTouch.collider.tag == DataModel.Tags.Node ||
                    raycastHitFirstTouch.collider.tag == DataModel.Tags.Edge)
                {
                    cityRootNodeFirstTouch = SceneQueries.GetCityRootTransformUpwards(raycastHitFirstTouch.transform);
                }
            }

            RaycastHit raycastHitSecondTouch;
            if (Physics.Raycast(raySecondTouch, out raycastHitSecondTouch))
            {
                if (raycastHitSecondTouch.collider.tag == DataModel.Tags.Node ||
                    raycastHitSecondTouch.collider.tag == DataModel.Tags.Edge)
                {
                    cityRootNodeSecondTouch = SceneQueries.GetCityRootTransformUpwards(raycastHitSecondTouch.transform);
                }
            }

            // If both touches are not on the same city root node or not on a city root node at all, no zooming is wanted.
            if (cityRootNodeFirstTouch != cityRootNodeSecondTouch ||
                cityRootNodeFirstTouch == null ||
                cityRootNodeSecondTouch == null)
            {
                return;
            }

            if (cityRootNodeFirstTouch.parent == null)
            {
                Debug.LogError($"ZoomActionDesktop.Update: rootTransform for touched {cityRootNodeFirstTouch.name} has no parent. Zooming turned off.\n");
                enabled = false;
                return;
            }
            if (!cityRootNodeFirstTouch.parent.TryGetComponent(out GO.Plane clippingPlane) || clippingPlane == null)
            {
                Debug.LogError($"ZoomActionDesktop.Update: parent for touched {cityRootNodeFirstTouch.name} has no GO.Plane. Zooming turned off.\n");
                enabled = false;
                return;
            }

            UnityEngine.Plane raycastPlane = new UnityEngine.Plane(Vector3.up, clippingPlane.transform.position);
            Vector2 centerTouchPosition = (firstTouch.position + secondTouch.position) / 2;
            Ray centerRay = Camera.main.ScreenPointToRay(centerTouchPosition);
            Vector3 hitPointOnPlane;
            float touchesPrevPosDifference, touchesCurPosDifference, zoomModifier;
            Vector2 firstTouchPrevPos, secondTouchPrevPos;

            // Finding the center position in between the two touch positions.
            raycastPlane.Raycast(centerRay, out float enter);
            hitPointOnPlane = centerRay.GetPoint(enter);

            firstTouchPrevPos = firstTouch.position - firstTouch.deltaPosition;
            secondTouchPrevPos = secondTouch.position - secondTouch.deltaPosition;

            touchesPrevPosDifference = (firstTouchPrevPos - secondTouchPrevPos).magnitude;
            touchesCurPosDifference = (firstTouch.position - secondTouch.position).magnitude;

            zoomModifier = (firstTouch.deltaPosition - secondTouch.deltaPosition).magnitude * 0.005f;

            // Checking whether the two touches move closer or further apart. When the two touch inputs move closer, the player zooms out.
            if (touchesPrevPosDifference > touchesCurPosDifference)
            {
                zoomModifier = -zoomModifier;
            }

            ZoomState zoomState = GetZoomStateCopy(cityRootNodeFirstTouch);

            zoomState.PushZoomCommand(hitPointOnPlane.XZ(), zoomModifier, ZoomState.DefaultZoomDuration);

            UpdateZoomState(cityRootNodeFirstTouch, zoomState);
        }
    }
}

