using SEE.Controls.Actions.HolisticMetrics;
using SEE.Utils;
using UnityEngine;

namespace SEE.Game.HolisticMetrics.ActionHelpers
{
    /// <summary>
    /// This component can be attached to a metrics board. It will then start listening for left mouse clicks on the
    /// board and once that occurs, it will create a widget where the click happened. Also whenever a left click
    /// happens, all instances of this class will delete themselves.
    /// </summary>
    public class WidgetAdder : MonoBehaviour
    {
        /// <summary>
        /// The position at which to add the widget.
        /// </summary>
        private Vector3 position;

        /// <summary>
        /// Whether or not this class has a position in store that could be fetched by the
        /// <see cref="AddWidgetAction"/>.
        /// </summary>
        private bool positionInStore;

        /// <summary>
        /// When the mouse is lifted up after clicking on the metrics board, we get the position of the mouse and then
        /// add the widget there.
        /// </summary>
        private void OnMouseUp()
        {
            if (MainCamera.Camera != null && !Raycasting.IsMouseOverGUI() && Raycasting.RaycastAnything(out RaycastHit hit))
            {
                position = transform.InverseTransformPoint(hit.point);
                positionInStore = true;
            }
        }

        /// <summary>
        /// If there is a position in store, this method returns that position in the <paramref name="clickPosition"/>
        /// parameter.
        /// </summary>
        /// <param name="clickPosition">The position where the player clicked.</param>
        /// <returns>Whether there is a position in store.</returns>
        internal bool GetPosition(out Vector3 clickPosition)
        {
            if (positionInStore)
            {
                positionInStore = false;
                clickPosition = position;
                return true;
            }

            clickPosition = Vector3.zero;
            return false;
        }
    }
}
