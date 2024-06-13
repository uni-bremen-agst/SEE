using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.ValueHolders;
using SEE.Utils;
using UnityEngine;
using UnityEngine.UIElements;

namespace SEE.Game.Drawable
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
            /// Instantiates the sticky note.
            GameObject stickyNote = PrefabInstantiator.InstantiatePrefab(stickyNotePrefabName);

            /// Sets the name of the sticky note.
            stickyNote.name = ValueHolder.StickyNotePrefix + "-" + DrawableHolder.GetRandomString(8);

            /// Adopts the rotation of the hitted object,
            /// unless it is a <see cref="DrawableType"/> object.
            /// In that case, take the rotation of the drawable.
            if (DrawableType.Get(raycastHit.collider.gameObject) == null)
            {
                stickyNote.transform.rotation = raycastHit.collider.gameObject.transform.rotation;
            } else
            {
                GameObject drawable = GameFinder.GetDrawable(raycastHit.collider.gameObject);
                stickyNote.transform.rotation = drawable.transform.rotation;
            }

            /// Adopt the position of the hitted object, but preserve the distance.
            stickyNote.transform.position = raycastHit.point - stickyNote.transform.forward
                * ValueHolder.distanceToDrawable.z * ValueHolder.currentOrderInLayer;

            /// Sets the inital scale for sticky notes
            stickyNote.transform.localScale = ValueHolder.StickyNoteScale;

            /// Sets a random color for the drawable.
            stickyNote.transform.Find("Front").GetComponent<MeshRenderer>().material.color = Random.ColorHSV().Darker();

            /// Adds a order in layer value holder to the sticky note and sets the necessary values.
            OrderInLayerValueHolder holder = stickyNote.AddComponent<OrderInLayerValueHolder>();
            holder.SetOriginPosition(raycastHit.point);
            holder.SetOrderInLayer(ValueHolder.currentOrderInLayer);

            ValueHolder.currentOrderInLayer++;
            return stickyNote;
        }

        /// <summary>
        /// Create a new sticky note on the position of the raycast hit.
        /// The rotation of the sticky notes is based on the raycast hit object.
        /// </summary>
        /// <param name="gameObject">The chosen GameObject</param>
        /// <returns>The created sticky note</returns>
        public static GameObject Spawn(GameObject gameObject)
        {
            /// Instantiates the sticky note.
            GameObject stickyNote = PrefabInstantiator.InstantiatePrefab(stickyNotePrefabName, gameObject.transform);

            /// Sets the name of the sticky note.
            stickyNote.name = ValueHolder.StickyNotePrefix + "-" + DrawableHolder.GetRandomString(8);


            stickyNote.transform.rotation = gameObject.transform.rotation;

            Renderer targetRenderer = gameObject.GetComponent<Renderer>();
            if (targetRenderer != null)
            {
                Bounds targetBounds = targetRenderer.bounds;
                Vector3 targetSize = targetBounds.size;

                // Set the position to be outside the bounds of the target object
                Vector3 newPosition = targetBounds.center + gameObject.transform.forward * (targetSize.y / 2 + ValueHolder.distanceToDrawable.y);
                stickyNote.transform.position = newPosition;
            }
            else
            {
                Debug.LogError("Target object has no Renderer component!");
                stickyNote.transform.position = gameObject.transform.position + gameObject.transform.forward * ValueHolder.distanceToDrawable.z;
            }

            /// Sets the inital scale for sticky notes
            stickyNote.transform.localScale = ValueHolder.StickyNoteScale;

            /// Sets a random color for the drawable.
            stickyNote.transform.Find("Front").GetComponent<MeshRenderer>().material.color = Random.ColorHSV().Darker();

            /// Adds a order in layer value holder to the sticky note and sets the necessary values.
            //stickyNote.transform.position = gameObject.transform.position;

            //stickyNote.transform.position.y  = gameObject.transform.lossyScale.y;

            return stickyNote;
        }


        /// <summary>
        /// Spawn a sticky note from configuration.
        /// </summary>
        /// <param name="config">The configuration which holds the sticky note.</param>
        /// <returns>The created sticky note.</returns>
        public static GameObject Spawn(DrawableConfig config)
        {
            /// Adjusts the current order in the layer if the
            /// order in layer for the line is greater than or equal to it.
            if (config.Order >= ValueHolder.currentOrderInLayer)
            {
                ValueHolder.currentOrderInLayer = config.Order + 1;
            }

            /// Instantiates the sticky note.
            GameObject stickyNote = PrefabInstantiator.InstantiatePrefab(stickyNotePrefabName);

            /// Restores the old values.
            stickyNote.transform.eulerAngles = config.Rotation;
            stickyNote.transform.position = config.Position;
            stickyNote.name = config.ParentID;
            stickyNote.transform.localScale = config.Scale;
            stickyNote.transform.Find("Front").GetComponent<MeshRenderer>().material.color = config.Color;

            /// Adds a order in layer value holder to the sticky note and sets the necessary values.
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
            stickyNoteHolder.GetComponent<OrderInLayerValueHolder>().SetOriginPosition(position);
        }

        /// <summary>
        /// Maintains the minimum distance to the objects.
        /// </summary>
        /// <param name="stickyNoteHolder">The moved sticky note holder.</param>
        /// <returns>The new position</returns>
        public static Vector3 FinishMoving(GameObject stickyNoteHolder)
        {
            stickyNoteHolder.transform.position -= stickyNoteHolder.transform.forward
                * ValueHolder.distanceToDrawable.z
                * stickyNoteHolder.GetComponent<OrderInLayerValueHolder>().GetOrderInLayer();
            stickyNoteHolder.GetComponent<OrderInLayerValueHolder>()
                .SetOriginPosition(stickyNoteHolder.transform.position);
            return stickyNoteHolder.transform.position;
        }

        /// <summary>
        /// Moves the sticky note holder by menu.
        /// </summary>
        /// <param name="stickyNoteHolder">The sticky note holder that should be moved.</param>
        /// <param name="direction">The direction in which the holder should moved.</param>
        /// <param name="speed">The movement speed</param>
        /// <returns>The new position</returns>
        public static Vector3 MoveByMenu(GameObject stickyNoteHolder,
            ValueHolder.MoveDirection direction, float speed)
        {
            /// Moves the sticky note in the desired direction with the chosen speed.
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
            stickyNoteHolder.GetComponent<OrderInLayerValueHolder>()
                .SetOriginPosition(stickyNoteHolder.transform.position);
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
            /// Sets the y euler angle.
            transform.localEulerAngles = new Vector3(transform.localEulerAngles.x,
                localEulerAngleY, transform.localEulerAngles.z);
            /// Preserve the distance.
            //obj.transform.position = oldPos - obj.transform.forward
            //    * ValueHolder.distanceToDrawable.z * ValueHolder.currentOrderInLayer;
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
            /// Sets the x euler angle.
            transform.localEulerAngles = new Vector3(localEulerAngleX,
                transform.localEulerAngles.y, transform.localEulerAngles.z);
            if (changePos)
            {
                /// Preserve the distance.
                obj.transform.position = oldPos - obj.transform.forward
                    * ValueHolder.distanceToDrawable.z * ValueHolder.currentOrderInLayer;
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
            transform.localEulerAngles = new Vector3(localEulerAngleX,
                transform.localEulerAngles.y, transform.localEulerAngles.z);
        }

        /// <summary>
        /// This method changes the order in layer of a sticky note.
        /// </summary>
        /// <param name="stickyNote">The sticky note whose order should be changed.</param>
        /// <param name="newLayer">The new order in layer.</param>
        public static void ChangeLayer(GameObject stickyNote, int newLayer)
        {
            int oldLayer;
            /// Gets the old order in layer.
            if (stickyNote.GetComponent<OrderInLayerValueHolder>() != null)
            {
                oldLayer = stickyNote.GetComponent<OrderInLayerValueHolder>().GetOrderInLayer();
            }
            else
            {
                oldLayer = stickyNote.GetComponentInParent<OrderInLayerValueHolder>().GetOrderInLayer();
            }

            /// Checks if the order in layer should increase or decrease.
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
            if (GameFinder.GetDrawable(stickyNote) != null
                && GameFinder.GetDrawable(stickyNote).GetComponent<MeshRenderer>() != null)
            {
                GameFinder.GetDrawable(stickyNote).GetComponent<MeshRenderer>().material.color = color;
            }
        }

        /// <summary>
        /// Combine all edit method together
        /// </summary>
        /// <param name="stickyNote">The sticky note on that the changes should be executed.</param>
        /// <param name="config">The configuration which holds the values for the changing.</param>
        public static void Change(GameObject stickyNote, DrawableConfig config)
        {
            GameObject root = GameFinder.GetHighestParent(stickyNote);
            if (root.name.Contains(ValueHolder.StickyNotePrefix))
            {
                ChangeColor(stickyNote, config.Color);
                ChangeLayer(root, config.Order);
                SetRotateX(root, config.Rotation.x);
                SetRotateY(root, config.Rotation.y, config.Position);
                GameScaler.SetScale(stickyNote, config.Scale);
            }
        }
    }
}