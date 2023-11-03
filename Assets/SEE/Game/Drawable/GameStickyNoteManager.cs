using Michsky.UI.ModernUIPack;
using RTG;
using SEE.Controls.Actions.Drawable;
using SEE.Game;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Game.HolisticMetrics;
using SEE.Net.Actions.Drawable;
using SEE.Utils;
using System.Collections;
using UnityEngine;

namespace Assets.SEE.Game.Drawable
{
    /// <summary>
    /// This class manages the drawable sticky notes
    /// </summary>
    public static class GameStickyNoteManager
    {
        /// <summary>
        /// The prefab of the sticky note.
        /// </summary>
        private const string stickyNotePrefabName = "Prefabs/Whiteboard/StickyNote";

        /// <summary>
        /// Create a new sticky note on the position of the raycast hit.
        /// The rotation of the sticky notes is based on the raycast hit object.
        /// </summary>
        /// <param name="raycastHit">The chosen raycast hit</param>
        /// <returns>The created sticky note</returns>
        public static GameObject Spawn(RaycastHit raycastHit)
        {
            GameObject stickyNote = PrefabInstantiator.InstantiatePrefab(stickyNotePrefabName);
            stickyNote.name = ValueHolder.StickyNotePrefix + "-" + DrawableHolder.GetRandomString(8);
            stickyNote.transform.rotation = raycastHit.collider.gameObject.transform.rotation;
            stickyNote.transform.position = raycastHit.point - stickyNote.transform.forward * ValueHolder.distanceToDrawable.z * ValueHolder.currentOrderInLayer;
            stickyNote.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            stickyNote.transform.Find("Front").GetComponent<MeshRenderer>().material.color = Random.ColorHSV().Darker();

            OrderInLayerValueHolder holder = stickyNote.AddComponent<OrderInLayerValueHolder>();
            holder.SetOriginPosition(raycastHit.point);
            holder.SetOrderInLayer(ValueHolder.currentOrderInLayer);
            ValueHolder.currentOrderInLayer++;
            return stickyNote;
        }

        /// <summary>
        /// Spawn a sticky note from configuration.
        /// </summary>
        /// <param name="config">The configuration which holds the sticky note.</param>
        /// <returns>The created sticky note.</returns>
        public static GameObject Spawn(DrawableConfig config)
        {
            if (config.Order >= ValueHolder.currentOrderInLayer)
            {
                ValueHolder.currentOrderInLayer = config.Order + 1;
            }

            GameObject stickyNote = PrefabInstantiator.InstantiatePrefab(stickyNotePrefabName);
            stickyNote.transform.eulerAngles = config.Rotation;
            stickyNote.transform.position = config.Position;
            stickyNote.name = config.DrawableParentName;
            stickyNote.transform.localScale = config.Scale;
            stickyNote.transform.Find("Front").GetComponent<MeshRenderer>().material.color = config.Color;

            OrderInLayerValueHolder holder = stickyNote.AddComponent<OrderInLayerValueHolder>();
            holder.SetOriginPosition(config.Position + stickyNote.transform.forward * ValueHolder.distanceToDrawable.z * config.Order);
            holder.SetOrderInLayer(config.Order);
            return stickyNote;
        }

        /// <summary>
        /// Moves and rotates a sticky note holder.
        /// </summary>
        /// <param name="stickyNoteHolder">The sticky note holder</param>
        /// <param name="position">The new position</param>
        /// <param name="eulerAngles">The new rotation</param>
        public static void Move(GameObject stickyNoteHolder, Vector3 position, Vector3 eulerAngles)
        {
            stickyNoteHolder.transform.position = position;
            stickyNoteHolder.transform.eulerAngles = eulerAngles;
        }

        /// <summary>
        /// Maintains the minimum distance to the objects.
        /// </summary>
        /// <param name="stickyNoteHolder">The moved sticky note holder.</param>
        /// <returns>The new position</returns>
        public static Vector3 FinishMoving(GameObject stickyNoteHolder)
        {
            stickyNoteHolder.transform.position -= stickyNoteHolder.transform.forward * ValueHolder.distanceToDrawable.z *
                                stickyNoteHolder.GetComponent<OrderInLayerValueHolder>().GetOrderInLayer();
            return stickyNoteHolder.transform.position;
        }

        /// <summary>
        /// Moves the sticky note holder by menu.
        /// </summary>
        /// <param name="stickyNoteHolder">The sticky note holder that should be moved.</param>
        /// <param name="direction">The direction in which the holder should moved.</param>
        /// <param name="speed">The movement speed</param>
        /// <returns>The new position</returns>
        public static Vector3 MoveByMenu(GameObject stickyNoteHolder, ValueHolder.MoveDirection direction, float speed)
        {
            switch(direction)
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
            return stickyNoteHolder.transform.position;
        }

        /// <summary>
        /// Sets the y rotation of an sticky note (holder)
        /// </summary>
        /// <param name="obj">The sticky note (holder)</param>
        /// <param name="localEulerAngleY">The new y rotation degree</param>
        /// <param name="oldPos">The old position of the object.</param>
        public static void SetRotateY(GameObject obj, float localEulerAngleY, Vector3 oldPos)
        {
            Transform transform = obj.transform;
            transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, localEulerAngleY, transform.localEulerAngles.z);
            obj.transform.position = oldPos - obj.transform.forward * ValueHolder.distanceToDrawable.z * ValueHolder.currentOrderInLayer;
        }
        /// <summary>
        /// Sets the x rotation of an sticky note (holder)
        /// </summary>
        /// <param name="obj">The sticky note (holder)</param>
        /// <param name="localEulerAngleX">The new x rotation degree</param>
        /// <param name="oldPos">The old position of the object.</param>
        /// <param name="changePos">Indicates whether the minimum distance should be maintained.</param>
        public static void SetRotateX(GameObject obj, float localEulerAngleX, Vector3 oldPos, bool changePos)
        {
            Transform transform = obj.transform;
            transform.localEulerAngles = new Vector3(localEulerAngleX, transform.localEulerAngles.y, transform.localEulerAngles.z);
            if (changePos)
            {
                obj.transform.position = oldPos - obj.transform.forward * ValueHolder.distanceToDrawable.z * ValueHolder.currentOrderInLayer;
            }
        }

        /// <summary>
        /// Sets the x rotation of an sticky note (holder)
        /// </summary>
        /// <param name="obj">The sticky note (holder)</param>
        /// <param name="localEulerAngleX">The new x rotation degree</param>
        public static void SetRotateX(GameObject obj, float localEulerAngleX)
        {
            Transform transform = obj.transform;
            transform.localEulerAngles = new Vector3(localEulerAngleX, transform.localEulerAngles.y, transform.localEulerAngles.z);
        }

        /// <summary>
        /// This method changes the order in layer of a sticky note.
        /// </summary>
        /// <param name="stickyNote">The sticky note whose order should be changed.</param>
        /// <param name="newLayer">The new order in layer.</param>
        public static void ChangeLayer(GameObject stickyNote, int newLayer)
        {
            int oldLayer = 0;
            if (stickyNote.GetComponent<OrderInLayerValueHolder>() != null)
            {
                oldLayer = stickyNote.GetComponent<OrderInLayerValueHolder>().GetOrderInLayer();
            } else
            {
                oldLayer = stickyNote.GetComponentInParent<OrderInLayerValueHolder>().GetOrderInLayer();
            }
            if (newLayer - oldLayer > 0)
            {
                GameLayerChanger.Increase(GameFinder.GetHighestParent(stickyNote), newLayer, false, true);
            }
            else
            {
                GameLayerChanger.Decrease(GameFinder.GetHighestParent(stickyNote), newLayer, false, true);
            }
        }

        /// <summary>
        /// Changes the color of a sticky note drawable.
        /// </summary>
        /// <param name="stickyNote">The sticky note which holds the drawable</param>
        /// <param name="color">The new color for the drawable</param>
        public static void ChangeColor(GameObject stickyNote, Color color)
        {
            stickyNote.transform.Find("Front").GetComponent<MeshRenderer>().material.color = color;
        }

        /// <summary>
        /// Combine all edit method together
        /// </summary>
        /// <param name="stickyNote">The sticky note on that the changes should be executed.</param>
        /// <param name="config">The configuration which holds the values for the changing.</param>
        public static void Change(GameObject stickyNote, DrawableConfig config)
        {
            GameObject root = GameFinder.GetHighestParent(stickyNote);

            ChangeColor(stickyNote, config.Color);
            ChangeLayer(root, config.Order);
            SetRotateX(root, config.Rotation.x);
            SetRotateY(root, config.Rotation.y, config.Position);
            GameScaler.SetScale(stickyNote, config.Scale);
        }
    }
}