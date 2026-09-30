using SEE.Game.Drawable;
using SEE.Net.Actions.Drawable;
using SEE.UI.Menu.Drawable;
using UnityEngine;

namespace SEE.Controls.Actions.Drawable.MoveRotate
{
    /// <summary>
    /// Handles rotating the selected drawable object.
    /// </summary>
    internal sealed class MoveRotateRotationOperation
    {
        /// <summary>
        /// Updates the rotation of the selected object.
        /// </summary>
        /// <param name="selectedObject">The object to rotate.</param>
        /// <returns>Whether the rotation was finished.</returns>
        internal bool TryExecute(GameObject selectedObject)
        {
            if (selectedObject.GetComponent<BlinkEffect>() != null)
            {
                RotationMenu.Instance.Enable(selectedObject);
                RotateByWheel(selectedObject);

                if (SEEInput.LeftMouseInteraction())
                {
                    selectedObject.GetComponent<BlinkEffect>().Deactivate();
                }
            }

            return SEEInput.MouseUp(MouseButton.Left);
        }

        /// <summary>
        /// Restores the transform state from before the rotation operation.
        /// This includes the position if it was changed by collision handling
        /// while the object was being rotated.
        /// </summary>
        /// <param name="selectedObject">The rotated object.</param>
        /// <param name="oldPosition">The position to restore.</param>
        /// <param name="oldLocalEulerAngleZ">The rotation to restore.</param>
        internal void Restore(GameObject selectedObject, Vector3 oldPosition, float oldLocalEulerAngleZ)
        {
            GameObject surface = GameFinder.GetDrawableSurface(selectedObject);
            string surfaceParentName = GameFinder.GetDrawableSurfaceParentName(surface);
            bool includeChildren = RotationMenu.Instance.IncludeChildren;

            GameMoveRotator.SetRotate(selectedObject, oldLocalEulerAngleZ, includeChildren);
            new RotatorNetAction(surface.name, surfaceParentName, selectedObject.name,
                oldLocalEulerAngleZ, includeChildren).Execute();

            if (selectedObject.transform.localPosition != oldPosition)
            {
                GameMoveRotator.SetPosition(selectedObject, oldPosition, includeChildren);
                new MoveNetAction(surface.name, surfaceParentName, selectedObject.name,
                    oldPosition, includeChildren).Execute();
            }
        }

        /// <summary>
        /// Handles rotation through the mouse wheel.
        /// </summary>
        /// <param name="selectedObject">The object to rotate.</param>
        private static void RotateByWheel(GameObject selectedObject)
        {
            bool rotate = false;
            Vector3 direction = Vector3.zero;
            float degree = 0;

            if (SEEInput.ScrollUp() && !Input.GetKey(KeyCode.LeftControl))
            {
                direction = Vector3.forward;
                degree = ValueHolder.Rotate;
                rotate = true;
            }

            if (SEEInput.ScrollUp() && Input.GetKey(KeyCode.LeftControl))
            {
                direction = Vector3.forward;
                degree = ValueHolder.RotateFast;
                rotate = true;
            }

            if (SEEInput.ScrollDown() && !Input.GetKey(KeyCode.LeftControl))
            {
                direction = Vector3.back;
                degree = ValueHolder.Rotate;
                rotate = true;
            }

            if (SEEInput.ScrollDown() && Input.GetKey(KeyCode.LeftControl))
            {
                direction = Vector3.back;
                degree = ValueHolder.RotateFast;
                rotate = true;
            }

            if (rotate)
            {
                PerformRotate(selectedObject, direction, degree);
            }
        }

        /// <summary>
        /// Performs the requested rotation.
        /// </summary>
        /// <param name="selectedObject">The object to rotate.</param>
        /// <param name="direction">The direction of the rotation.</param>
        /// <param name="degree">The degree by which the object is rotated.</param>
        private static void PerformRotate(GameObject selectedObject, Vector3 direction, float degree)
        {
            GameObject surface = GameFinder.GetDrawableSurface(selectedObject);
            string surfaceParentName = GameFinder.GetDrawableSurfaceParentName(surface);

            GameMoveRotator.RotateObject(selectedObject, direction, degree, RotationMenu.Instance.IncludeChildren);

            new RotatorNetAction(surface.name, surfaceParentName, selectedObject.name, direction,
                degree, RotationMenu.Instance.IncludeChildren).Execute();
        }
    }
}
