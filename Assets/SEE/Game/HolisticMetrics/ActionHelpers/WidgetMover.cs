using SEE.Controls.Actions.HolisticMetrics;
using SEE.Utils;
using UnityEngine;

namespace SEE.Game.HolisticMetrics.ActionHelpers
{
    /// <summary>
    /// This component can be attached to a widget. It will allow the player to move the widget around by dragging it
    /// around with the mouse (left mouse button held down).
    /// </summary>
    internal class WidgetMover : MonoBehaviour
    {
        /// <summary>
        /// Whether this WidgetMover has registered a movement that has not yet been fetched by the
        /// <see cref="MoveWidgetAction"/>.
        /// </summary>
        private bool hasMovement;

        /// <summary>
        /// The old position of the widget, this might be needed later if the movement needs to be reverted.
        /// </summary>
        private Vector3 oldPosition;

        /// <summary>
        /// The distance of the widget to the board.
        /// </summary>
        private float boardDistance;

        /// <summary>
        /// The plane in which the canvas lies (the canvas that contains the widget that contains this component).
        /// </summary>
        private Plane plane;

        /// <summary>
        /// Initializes some fields.
        /// </summary>
        private void Start()
        {
            hasMovement = false;
        }

        /// <summary>
        /// Before the player starts dragging the widget around, we save the old position of the widget so we can
        /// potentially restore it later.
        /// </summary>
        private void OnMouseDown()
        {
            oldPosition = transform.position;
            boardDistance = transform.localPosition.z;
            plane = new Plane(transform.parent.forward, transform.parent.position);
        }

        /// <summary>
        /// While the player drags the mouse around over the widget, we want to move the widget with the mouse.
        /// </summary>
        private void OnMouseDrag()
        {
            if (MainCamera.Camera != null && !Raycasting.IsMouseOverGUI())
            {
                Ray ray = Raycasting.UserPointsTo();
                plane.Raycast(ray, out float enter);
                Vector3 enterPoint = ray.GetPoint(enter);
                transform.position = enterPoint;
                Vector3 localPositionTemp = transform.localPosition;
                localPositionTemp.z = boardDistance;
                transform.localPosition = localPositionTemp;
            }
        }

        /// <summary>
        /// When the player releases the left mouse button, we want to finally position the widget at the position
        /// where it is at.
        /// </summary>
        private void OnMouseUp()
        {
            hasMovement = true;
        }

        /// <summary>
        /// Can be used to try to get a widget movement that has not yet been fetched.
        /// </summary>
        /// <param name="originalPosition">The position of the widget before the movement, in case the player wants to
        /// restore it later on.</param>
        /// <param name="newPosition">The <see cref="Vector3"/> of where the widget has been moved to.</param>
        /// <returns>The value of <see cref="hasMovement"/> at the time this method is called.</returns>
        internal bool TryGetMovement(out Vector3 originalPosition, out Vector3 newPosition)
        {
            if (hasMovement)
            {
                originalPosition = oldPosition;
                newPosition = transform.position;
                hasMovement = false;
                return true;
            }

            originalPosition = Vector3.zero;
            newPosition = Vector3.zero;
            return false;
        }
    }
}
