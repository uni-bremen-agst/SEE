using SEE.Controls.Actions.HolisticMetrics;
using SEE.Game.HolisticMetrics.WidgetControllers;
using UnityEngine;

namespace SEE.Game.HolisticMetrics.Components
{
    /// <summary>
    /// This component can be attached to a widget. It will allow the player to move the widget around by dragging it
    /// around with the mouse (left mouse button held down).
    /// </summary>
    internal class WidgetMover : MonoBehaviour
    {
        /// <summary>
        /// The old position of the widget, so we can potentially restore it later.
        /// </summary>
        private Vector3 oldPosition;

        /// <summary>
        /// The name of the board which the widget to be moved is attached to.
        /// </summary>
        private string boardName;

        /// <summary>
        /// The plane in which the canvas lies (the canvas that contains the widget that contains this component).
        /// </summary>
        private Plane plane;

        /// <summary>
        /// Initializes some fields.
        /// </summary>
        private void Start()
        {
            boardName = transform.parent.GetComponent<WidgetsManager>().GetTitle();
        }
        
        /// <summary>
        /// Before the player starts dragging the widget around, we save the old position of the widget so we can
        /// potentially restore it later.
        /// </summary>
        private void OnMouseDown()
        {
            oldPosition = transform.position;
            plane = new Plane(transform.parent.forward, transform.parent.position);
        }

        /// <summary>
        /// While the player drags the mouse around over the widget, we want to move the widget with the mouse.
        /// </summary>
        private void OnMouseDrag()
        {
            if (Camera.main != null)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                plane.Raycast(ray, out float enter);
                Vector3 enterPoint = ray.GetPoint(enter);
                transform.position = enterPoint;
                Vector3 localPositionTemp = transform.localPosition;
                localPositionTemp.z = oldPosition.z;
                transform.localPosition = localPositionTemp;
            }
        }

        /// <summary>
        /// When the player releases the left mouse button, we want to finally position the widget at the position
        /// where it is at. 
        /// </summary>
        private void OnMouseUp()
        {
            new MoveWidgetAction(
                boardName, 
                GetComponent<WidgetController>().ID, 
                oldPosition, 
                transform.position)
                .Execute();
        }
    }
}
