using SEE.Game.Drawable;
using SEE.Game.Drawable.StickyNote;
using SEE.GO;
using SEE.Net.Actions.Drawable;
using SEE.UI.Menu.Drawable;
using SEE.UI.Menu.Drawable.StickyNoteRotation;
using UnityEngine;

namespace SEE.Controls.Actions.Drawable.StickyNote
{
    /// <summary>
    /// Handles keyboard and mouse-wheel interactions for moving,
    /// rotating, and scaling sticky notes.
    /// </summary>
    internal sealed class StickyNoteTransformInteraction
    {
        /// <summary>
        /// The supported mouse-wheel interactions.
        /// </summary>
        private enum WheelInteractionType
        {
            Rotate,
            Scale
        }

        /// <summary>
        /// Moves the given sticky note according to the currently pressed movement key.
        /// </summary>
        /// <param name="stickyNote">The sticky note to move.</param>
        /// <param name="synchronize">
        /// Whether the new position should be synchronized with other clients.
        /// </param>
        internal void MoveByKey(GameObject stickyNote, bool synchronize)
        {
            if (!SEEInput.MoveObjectLeft()
                && !SEEInput.MoveObjectRight()
                && !SEEInput.MoveObjectUp()
                && !SEEInput.MoveObjectDown()
                && !SEEInput.MoveObjectForward()
                && !SEEInput.MoveObjectBackward())
            {
                return;
            }

            ValueHolder.MoveDirection direction = GetDirection();
            GameObject holder = stickyNote.GetRootParent();

            Vector3 newPosition = GameStickyNoteTransform.MoveByMenu(
                holder,
                direction,
                StickyNoteMoveMenu.Instance.GetSpeed());

            if (synchronize)
            {
                GameObject surface = GameFinder.GetDrawableSurface(stickyNote);

                new StickyNoteMoveNetAction(
                    surface.name,
                    GameFinder.GetDrawableSurfaceParentName(surface),
                    newPosition,
                    holder.transform.eulerAngles).Execute();
            }
        }

        /// <summary>
        /// Rotates the given sticky note around its Y-axis according to
        /// the current mouse-wheel interaction.
        /// </summary>
        /// <param name="stickyNote">The sticky note or sticky note holder to rotate.</param>
        /// <param name="synchronize">
        /// Whether the new rotation should be synchronized with other clients.
        /// </param>
        internal void RotateByWheel(GameObject stickyNote, bool synchronize)
        {
            if (!TryGetWheelInteraction(WheelInteractionType.Rotate, out float degree))
            {
                return;
            }

            GameObject holder = stickyNote.GetRootParent();
            float newDegree = holder.transform.localEulerAngles.y + degree;

            GameStickyNoteTransform.SetRotateY(holder, newDegree);
            StickyNoteRotationMenu.AssignValueToYSlider(newDegree);

            if (synchronize)
            {
                GameObject surface = GameFinder.GetDrawableSurface(stickyNote);

                new StickyNoteRoateYNetAction(
                    surface.name,
                    GameFinder.GetDrawableSurfaceParentName(surface),
                    newDegree,
                    holder.transform.position).Execute();
            }
        }

        /// <summary>
        /// Gets the scale factor represented by the current mouse-wheel interaction.
        /// </summary>
        /// <param name="scaleFactor">The requested scale factor.</param>
        /// <returns>Whether a scale interaction occurred.</returns>
        internal bool TryGetScaleFactor(out float scaleFactor)
        {
            return TryGetWheelInteraction(
                WheelInteractionType.Scale,
                out scaleFactor);
        }

        /// <summary>
        /// Gets the movement direction represented by the currently pressed key.
        /// </summary>
        /// <returns>The requested movement direction.</returns>
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

            if (SEEInput.MoveObjectForward())
            {
                return ValueHolder.MoveDirection.Forward;
            }

            if (SEEInput.MoveObjectBackward())
            {
                return ValueHolder.MoveDirection.Back;
            }

            if (SEEInput.MoveObjectUp())
            {
                return ValueHolder.MoveDirection.Up;
            }

            return ValueHolder.MoveDirection.Down;
        }

        /// <summary>
        /// Calculates the value represented by the current mouse-wheel interaction.
        /// </summary>
        /// <param name="interactionType">The requested interaction type.</param>
        /// <param name="value">The calculated interaction value.</param>
        /// <returns>Whether a mouse-wheel interaction occurred.</returns>
        private static bool TryGetWheelInteraction(
            WheelInteractionType interactionType,
            out float value)
        {
            value = 0;

            if (SEEInput.ScrollUp())
            {
                value = interactionType == WheelInteractionType.Rotate
                    ? Input.GetKey(KeyCode.LeftControl)
                        ? ValueHolder.RotateFast
                        : ValueHolder.Rotate
                    : Input.GetKey(KeyCode.LeftControl)
                        ? ValueHolder.ScaleUpFast
                        : ValueHolder.ScaleUp;

                return true;
            }

            if (SEEInput.ScrollDown())
            {
                value = interactionType == WheelInteractionType.Rotate
                    ? Input.GetKey(KeyCode.LeftControl)
                        ? -ValueHolder.RotateFast
                        : -ValueHolder.Rotate
                    : Input.GetKey(KeyCode.LeftControl)
                        ? ValueHolder.ScaleDownFast
                        : ValueHolder.ScaleDown;

                return true;
            }

            return false;
        }
    }
}
