using Michsky.UI.ModernUIPack;
using SEE.Game;
using SEE.Game.Drawable;
using SEE.Net.Actions.Drawable;
using SEE.UI.Drawable;
using SEE.UI.Menu.Drawable;
using SEE.Utils;
using UnityEngine;

namespace SEE.Controls.Actions.Drawable.MoveRotate
{
    /// <summary>
    /// Handles moving the selected drawable object.
    /// </summary>
    internal sealed class MoveRotateMoveOperation
    {
        /// <summary>
        /// Updates the movement of the selected object.
        /// </summary>
        /// <param name="selectedObject">The object to move.</param>
        /// <param name="newPosition">The final position when the movement finishes.</param>
        /// <returns>Whether the movement was finished.</returns>
        internal bool TryExecute(GameObject selectedObject, out Vector3 newPosition)
        {
            newPosition = selectedObject.transform.localPosition;

            if (selectedObject.GetComponent<BlinkEffect>() != null)
            {
                GameObject surface = GameFinder.GetDrawableSurface(selectedObject);
                string surfaceParentName = GameFinder.GetDrawableSurfaceParentName(surface);

                MoveMenu.Instance.Enable(selectedObject);
                SwitchManager speedUp = MoveMenu.Instance.GetSpeedUpManager();
                SwitchManager moveByMouse = MoveMenu.Instance.GetMoveByMouseManager();

                if (Input.GetKeyDown(KeyCode.LeftControl))
                {
                    speedUp.isOn = !speedUp.isOn;
                    speedUp.UpdateUI();
                }

                MoveByKey(selectedObject, moveByMouse, speedUp, surface, surfaceParentName);

                if (SEEInput.MouseDown(MouseButton.Middle))
                {
                    moveByMouse.isOn = !moveByMouse.isOn;
                    moveByMouse.UpdateUI();
                }

                bool childInCollision = CheckChildrenCollision(selectedObject);
                MoveByMouse(selectedObject, moveByMouse, childInCollision, surface, surfaceParentName);

                if (SEEInput.LeftMouseInteraction())
                {
                    BlinkEffect.Deactivate(selectedObject);
                }
            }

            if (!SEEInput.MouseUp(MouseButton.Left))
            {
                return false;
            }

            newPosition = selectedObject.transform.localPosition;
            return true;
        }

        /// <summary>
        /// Restores the position from before the movement.
        /// </summary>
        /// <param name="selectedObject">The moved object.</param>
        /// <param name="oldPosition">The position to restore.</param>
        internal void Restore(GameObject selectedObject, Vector3 oldPosition)
        {
            GameObject surface = GameFinder.GetDrawableSurface(selectedObject);
            string surfaceParentName = GameFinder.GetDrawableSurfaceParentName(surface);
            bool includeChildren = MoveMenu.Instance.IncludeChildren;

            GameMoveRotator.SetPosition(selectedObject, oldPosition, includeChildren);
            new MoveNetAction(surface.name, surfaceParentName, selectedObject.name,
                oldPosition, includeChildren).Execute();
        }

        /// <summary>
        /// Moves the selected object using the keyboard.
        /// </summary>
        private static void MoveByKey(GameObject selectedObject, SwitchManager moveByMouse, SwitchManager speedUp,
            GameObject surface, string surfaceParentName)
        {
            if (!SEEInput.MoveObjectLeft() && !SEEInput.MoveObjectRight()
                && !SEEInput.MoveObjectUp() && !SEEInput.MoveObjectDown())
            {
                return;
            }

            ValueHolder.MoveDirection direction = GetDirection();
            moveByMouse.isOn = false;
            moveByMouse.UpdateUI();

            Vector3 newPosition = GameMoveRotator.MoveObjectByKeyboard(selectedObject, direction,
                speedUp.isOn, MoveMenu.Instance.IncludeChildren);

            new MoveNetAction(surface.name, surfaceParentName, selectedObject.name,
                newPosition, MoveMenu.Instance.IncludeChildren).Execute();
        }

        /// <summary>
        /// Checks whether any child node is involved in a collision.
        /// </summary>
        private static bool CheckChildrenCollision(GameObject selectedObject)
        {
            bool childInCollision = false;

            if (selectedObject.CompareTag(Tags.MindMapNode))
            {
                CollisionController[] collisionControllers = GameFinder.GetAttachedObjectsObject(selectedObject)
                    .GetComponentsInChildren<CollisionController>();

                foreach (CollisionController collisionController in collisionControllers)
                {
                    childInCollision = childInCollision || collisionController.IsInCollision();
                }
            }

            return childInCollision;
        }

        /// <summary>
        /// Moves the selected object based on the mouse position.
        /// </summary>
        private static void MoveByMouse(GameObject selectedObject, SwitchManager moveByMouse,
            bool childInCollision, GameObject surface, string surfaceParentName)
        {
            if (!moveByMouse.isOn || !Raycasting.RaycastAnything(out RaycastHit hit)
                || selectedObject.GetComponent<CollisionController>().IsInCollision() || childInCollision)
            {
                return;
            }

            if (hit.collider.gameObject.CompareTag(Tags.Drawable)
                    && hit.collider.gameObject.Equals(surface)
                || GameFinder.HasDrawableSurface(hit.collider.gameObject)
                    && GameFinder.GetDrawableSurface(hit.collider.gameObject).Equals(surface))
            {
                Vector3 newPosition = GameMoveRotator.MoveObjectByMouse(selectedObject,
                    hit.point, MoveMenu.Instance.IncludeChildren);

                new MoveNetAction(surface.name, surfaceParentName, selectedObject.name,
                    newPosition, MoveMenu.Instance.IncludeChildren).Execute();
            }
        }

        /// <summary>
        /// Gets the movement direction represented by the currently pressed key.
        /// </summary>
        private static ValueHolder.MoveDirection GetDirection()
        {
            if (SEEInput.MoveObjectLeft())
            {
                return ValueHolder.MoveDirection.Left;
            }

            if (SEEInput.MoveObjectRight())
            {
                return ValueHolder.MoveDirection.Right;
            }

            if (SEEInput.MoveObjectUp())
            {
                return ValueHolder.MoveDirection.Up;
            }

            return ValueHolder.MoveDirection.Down;
        }
    }
}
