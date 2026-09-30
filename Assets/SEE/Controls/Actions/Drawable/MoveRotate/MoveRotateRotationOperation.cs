using SEE.Game;
using SEE.Game.Drawable;
using SEE.Net.Actions.Drawable;
using SEE.UI.Menu.Drawable;
using SEE.Utils;
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
        /// <param name="newPosition">The position resulting from the rotation.</param>
        /// <param name="newLocalEulerAngles">The local rotation resulting from the rotation.</param>
        /// <returns>Whether the rotation was finished.</returns>
        internal bool TryExecute(GameObject selectedObject, ref Vector3 newPosition, ref Vector3 newLocalEulerAngles)
        {
            if (selectedObject.GetComponent<BlinkEffect>() != null)
            {
                RotationMenu.Instance.Enable(selectedObject);
                RotateByWheel(selectedObject, ref newPosition, ref newLocalEulerAngles);

                if (SEEInput.LeftMouseInteraction())
                {
                    selectedObject.GetComponent<BlinkEffect>().Deactivate();
                }
            }

            return SEEInput.MouseUp(MouseButton.Left);
        }

        /// <summary>
        /// Restores the rotation from before the operation.
        /// </summary>
        /// <param name="selectedObject">The rotated object.</param>
        /// <param name="oldLocalEulerAngleZ">The rotation to restore.</param>
        internal void Restore(GameObject selectedObject, float oldLocalEulerAngleZ)
        {
            GameObject surface = GameFinder.GetDrawableSurface(selectedObject);
            string surfaceParentName = GameFinder.GetDrawableSurfaceParentName(surface);
            bool includeChildren = RotationMenu.Instance.IncludeChildren;

            GameMoveRotator.SetRotate(selectedObject, oldLocalEulerAngleZ, includeChildren);
            new RotatorNetAction(surface.name, surfaceParentName, selectedObject.name,
                oldLocalEulerAngleZ, includeChildren).Execute();
        }

        /// <summary>
        /// Handles rotation through the mouse wheel.
        /// </summary>
        private static void RotateByWheel(GameObject selectedObject, ref Vector3 newPosition,
            ref Vector3 newLocalEulerAngles)
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
                PerformRotate(selectedObject, direction, degree, ref newPosition, ref newLocalEulerAngles);
            }
        }

        /// <summary>
        /// Performs the requested rotation.
        /// </summary>
        private static void PerformRotate(GameObject selectedObject, Vector3 direction, float degree,
            ref Vector3 newPosition, ref Vector3 newLocalEulerAngles)
        {
            GameObject surface = GameFinder.GetDrawableSurface(selectedObject);
            string surfaceParentName = GameFinder.GetDrawableSurfaceParentName(surface);

            newLocalEulerAngles = GameMoveRotator.RotateObject(selectedObject, direction, degree,
                RotationMenu.Instance.IncludeChildren);

            if (Tags.DrawableTypes.Contains(selectedObject.tag))
            {
                newPosition = selectedObject.transform.localPosition;
            }

            new RotatorNetAction(surface.name, surfaceParentName, selectedObject.name, direction,
                degree, RotationMenu.Instance.IncludeChildren).Execute();
        }
    }
}
