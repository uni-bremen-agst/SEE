using SEE.Game.Drawable.ValueHolders;
using SEE.GO;
using UnityEngine;

namespace SEE.Game.Drawable.StickyNote
{
    /// <summary>
    /// Provides movement and rotation operations for sticky notes.
    /// </summary>
    public static class GameStickyNoteTransform
    {
        /// <summary>
        /// Moves and rotates a sticky note holder.
        /// </summary>
        /// <param name="stickyNoteHolder">The sticky note holder.</param>
        /// <param name="position">The new position.</param>
        /// <param name="eulerAngles">The new rotation.</param>
        public static void Move(GameObject stickyNoteHolder, Vector3 position, Vector3 eulerAngles)
        {
            stickyNoteHolder.transform.position = position;
            stickyNoteHolder.transform.eulerAngles = eulerAngles;
            stickyNoteHolder.GetComponent<OrderInLayerValueHolder>().OriginPosition = position;
        }

        /// <summary>
        /// Maintains the minimum distance to the objects.
        /// </summary>
        /// <param name="stickyNoteHolder">The moved sticky note holder.</param>
        /// <returns>The new position.</returns>
        public static Vector3 FinishMoving(GameObject stickyNoteHolder)
        {
            stickyNoteHolder.transform.position -=
                stickyNoteHolder.GetComponent<OrderInLayerValueHolder>().OrderInLayer
                * ValueHolder.DistanceToDrawable.z
                * stickyNoteHolder.transform.forward;

            stickyNoteHolder.GetComponent<OrderInLayerValueHolder>().OriginPosition =
                stickyNoteHolder.transform.position;

            return stickyNoteHolder.transform.position;
        }

        /// <summary>
        /// Moves the sticky note holder in the given direction.
        /// </summary>
        /// <param name="stickyNoteHolder">The sticky note holder to move.</param>
        /// <param name="direction">The direction in which the holder should be moved.</param>
        /// <param name="speed">The movement speed.</param>
        /// <returns>The new position.</returns>
        public static Vector3 MoveByMenu(GameObject stickyNoteHolder,
            ValueHolder.MoveDirection direction, float speed)
        {
            switch (direction)
            {
                case ValueHolder.MoveDirection.Left:
                    stickyNoteHolder.transform.position -= stickyNoteHolder.transform.right * speed;
                    break;
                case ValueHolder.MoveDirection.Right:
                    stickyNoteHolder.transform.position += stickyNoteHolder.transform.right * speed;
                    break;
                case ValueHolder.MoveDirection.Up:
                    stickyNoteHolder.transform.position += stickyNoteHolder.transform.up * speed;
                    break;
                case ValueHolder.MoveDirection.Down:
                    stickyNoteHolder.transform.position -= stickyNoteHolder.transform.up * speed;
                    break;
                case ValueHolder.MoveDirection.Forward:
                    stickyNoteHolder.transform.position += stickyNoteHolder.transform.forward * speed;
                    break;
                case ValueHolder.MoveDirection.Back:
                    stickyNoteHolder.transform.position -= stickyNoteHolder.transform.forward * speed;
                    break;
            }

            stickyNoteHolder.GetComponent<OrderInLayerValueHolder>().OriginPosition =
                stickyNoteHolder.transform.position;

            return stickyNoteHolder.transform.position;
        }

        /// <summary>
        /// Sets the y rotation of a sticky note holder.
        /// </summary>
        /// <param name="obj">The sticky note holder.</param>
        /// <param name="localEulerAngleY">The new y rotation.</param>
        public static void SetRotateY(GameObject obj, float localEulerAngleY)
        {
            Transform transform = obj.transform;
            transform.localEulerAngles = new Vector3(
                transform.localEulerAngles.x,
                localEulerAngleY,
                transform.localEulerAngles.z);
        }

        /// <summary>
        /// Sets the x rotation of a sticky note holder and optionally preserves
        /// its minimum distance to the surface.
        /// </summary>
        /// <param name="obj">The sticky note holder.</param>
        /// <param name="localEulerAngleX">The new x rotation.</param>
        /// <param name="oldPos">The previous position of the object.</param>
        /// <param name="changePos">
        /// Whether the minimum distance to the surface should be preserved.
        /// </param>
        public static void SetRotateX(GameObject obj, float localEulerAngleX,
            Vector3 oldPos, bool changePos)
        {
            Transform transform = obj.transform;
            transform.localEulerAngles = new Vector3(
                localEulerAngleX,
                transform.localEulerAngles.y,
                transform.localEulerAngles.z);

            if (changePos)
            {
                obj.transform.position = oldPos
                    - obj.transform.forward
                    * ValueHolder.DistanceToDrawable.z
                    * ValueHolder.MaxOrderInLayer;
            }
        }

        /// <summary>
        /// Sets the x rotation of a sticky note holder.
        /// </summary>
        /// <param name="obj">The sticky note holder.</param>
        /// <param name="localEulerAngleX">The new x rotation.</param>
        public static void SetRotateX(GameObject obj, float localEulerAngleX)
        {
            Transform transform = obj.transform;
            transform.localEulerAngles = new Vector3(
                localEulerAngleX,
                transform.localEulerAngles.y,
                transform.localEulerAngles.z);
        }

        /// <summary>
        /// Sets the position of a sticky note.
        /// </summary>
        /// <param name="obj">An object belonging to the sticky note.</param>
        /// <param name="position">The new position.</param>
        public static void SetPosition(GameObject obj, Vector3 position)
        {
            obj.GetRootParent().transform.position = position;
        }
    }
}
