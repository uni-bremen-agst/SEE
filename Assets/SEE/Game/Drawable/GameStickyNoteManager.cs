using Cysharp.Threading.Tasks;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.ValueHolders;
using SEE.Utils;
using UnityEngine;

namespace SEE.Game.Drawable
{
    /// <summary>
    /// This class manages the drawable sticky notes.
    /// </summary>
    public static class GameStickyNoteManager
    {
        /// <summary>
        /// The prefab of the sticky note.
        /// </summary>
        private const string stickyNotePrefabName = "Prefabs/Drawable/StickyNote";

        /// <summary>
        /// Creates a new sticky note on the position of the raycast hit.
        /// The rotation of the sticky note is based on the raycast hit object.
        /// </summary>
        /// <param name="raycastHit">The chosen raycast hit</param>
        /// <returns>The created sticky note</returns>
        public static GameObject Spawn(RaycastHit raycastHit)
        {
            /// Instantiates the sticky note.
            GameObject stickyNote = PrefabInstantiator.InstantiatePrefab(stickyNotePrefabName);

            /// Sets the name of the sticky note.
            stickyNote.name = CreateUnusedName();//ValueHolder.StickyNotePrefix + "-" + RandomStrings.GetRandomString(8);

            /// Adopts the rotation of the hit object,
            /// unless it is a <see cref="DrawableType"/> object.
            /// In that case, takes the rotation of the drawable.
            if (DrawableType.Get(raycastHit.collider.gameObject) == null)
            {
                stickyNote.transform.rotation = raycastHit.collider.gameObject.transform.rotation;
            }
            else
            {
                GameObject surface = GameFinder.GetDrawableSurface(raycastHit.collider.gameObject);
                stickyNote.transform.rotation = surface.transform.rotation;
            }

            /// Adopt the position of the hit object, but preserve the distance.
            stickyNote.transform.position = raycastHit.point - ValueHolder.MaxOrderInLayer * ValueHolder.DistanceToDrawable.z * stickyNote.transform.forward;

            /// Sets the initial scale for sticky notes
            stickyNote.transform.localScale = ValueHolder.StickyNoteScale;

            /// Sets a random color for the drawable.
            GameFinder.GetDrawableSurface(stickyNote).GetComponent<MeshRenderer>().material.color = Random.ColorHSV().Darker();

            /// Adds an order in layer value holder to the sticky note and sets the necessary values.
            OrderInLayerValueHolder holder = stickyNote.AddComponent<OrderInLayerValueHolder>();
            holder.OriginPosition = raycastHit.point;
            holder.OrderInLayer = ValueHolder.MaxOrderInLayer;

            ValueHolder.MaxOrderInLayer++;
            return stickyNote;
        }

        /// <summary>
        /// Creates an unused name for a sticky note.
        /// </summary>
        /// <returns>unused name</returns>
        public static string CreateUnusedName()
        {
            string name = ValueHolder.StickyNotePrefix + "-" + RandomStrings.GetRandomString(8);
            while (GameObject.Find(name) != null)
            {
                name = ValueHolder.StickyNotePrefix + "-" + RandomStrings.GetRandomString(8);
            }
            return name;
        }

        /// <summary>
        /// Spawns a sticky note from given configuration.
        /// </summary>
        /// <param name="config">The configuration which holds the sticky note.</param>
        /// <returns>The created sticky note.</returns>
        public static GameObject Spawn(DrawableConfig config)
        {
            /// Adjusts the current order in the layer if the
            /// order in layer for the line is greater than or equal to it.
            if (config.Order >= ValueHolder.MaxOrderInLayer)
            {
                ValueHolder.MaxOrderInLayer = config.Order + 1;
            }

            /// Instantiates the sticky note.
            GameObject stickyNote = PrefabInstantiator.InstantiatePrefab(stickyNotePrefabName);

            /// Restores the old values.
            stickyNote.transform.eulerAngles = config.Rotation;
            stickyNote.transform.position = config.Position;
            stickyNote.name = config.ParentID;
            stickyNote.transform.localScale = config.Scale;
            GameFinder.GetDrawableSurface(stickyNote).GetComponent<MeshRenderer>().material.color = config.Color;
            stickyNote.transform.GetComponentInChildren<Light>().enabled = config.Lighting;

            /// Adds an order in layer value holder to the sticky note and sets the necessary values.
            OrderInLayerValueHolder holder = stickyNote.AddComponent<OrderInLayerValueHolder>();
            holder.OriginPosition = config.Position + config.Order * ValueHolder.DistanceToDrawable.z * stickyNote.transform.forward;
            holder.OrderInLayer = config.Order;

            DrawableHolder drawableHolder = stickyNote.GetComponentInChildren<DrawableHolder>();
            drawableHolder.OrderInLayer = config.OrderInLayer;
            drawableHolder.Description = config.Description;
            drawableHolder.CurrentPage = config.CurrentPage;
            drawableHolder.MaxPageSize = config.MaxPageSize;

            if (config.GetAllDrawableTypes().Count > 0)
            {
                ChangeVisibilityAfterLoadDrawablesAsync(stickyNote, config).Forget();
            }
            else
            {
                GameDrawableManager.ChangeVisibility(stickyNote, config.Visibility);
            }
            return stickyNote;
        }

        /// <summary>
        /// Visibility restoration can only occur after the drawables have been restored,
        /// as the parent object DrawableHolder and its associated AttachedObject object do not exist yet.
        /// Failure to wait here would result in altering the visibility of the wrong object.
        /// </summary>
        /// <param name="stickyNote">The sticky note to be restored.</param>
        /// <param name="config">The depending config to restore.</param>
        /// <returns>Nothing, it waits until the sticky note has been converted.</returns>
        private static async UniTask ChangeVisibilityAfterLoadDrawablesAsync(GameObject stickyNote, DrawableConfig config)
        {
            while (!GameFinder.GetHighestParent(stickyNote).name.Contains(ValueHolder.DrawableHolderPrefix))
            {
                await UniTask.Yield();
            }
            GameDrawableManager.ChangeVisibility(stickyNote, config.Visibility);
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
            stickyNoteHolder.GetComponent<OrderInLayerValueHolder>().OriginPosition = position;
        }

        /// <summary>
        /// Maintains the minimum distance to the objects.
        /// </summary>
        /// <param name="stickyNoteHolder">The moved sticky note holder.</param>
        /// <returns>The new position</returns>
        public static Vector3 FinishMoving(GameObject stickyNoteHolder)
        {
            stickyNoteHolder.transform.position -= stickyNoteHolder.GetComponent<OrderInLayerValueHolder>().OrderInLayer
                * ValueHolder.DistanceToDrawable.z * stickyNoteHolder.transform.forward;
            stickyNoteHolder.GetComponent<OrderInLayerValueHolder>()
                .OriginPosition = stickyNoteHolder.transform.position;
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
                .OriginPosition = stickyNoteHolder.transform.position;
            return stickyNoteHolder.transform.position;
        }

        /// <summary>
        /// Sets the y rotation of a sticky note (holder)
        /// </summary>
        /// <param name="obj">The sticky note (holder)</param>
        /// <param name="localEulerAngleY">The new y rotation degree</param>
        /// <param name="oldPos">The old position of the object.</param>
        public static void SetRotateY(GameObject obj, float localEulerAngleY)
        {
            Transform transform = obj.transform;
            /// Sets the y euler angle.
            transform.localEulerAngles = new Vector3(transform.localEulerAngles.x,
                localEulerAngleY, transform.localEulerAngles.z);
        }
        /// <summary>
        /// Sets the x rotation of a sticky note (holder)
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
                    * ValueHolder.DistanceToDrawable.z * ValueHolder.MaxOrderInLayer;
            }
        }

        /// <summary>
        /// Sets the x rotation of a sticky note (holder)
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
        /// Sets the position of a drawable.
        /// </summary>
        /// <param name="obj">A part of the drawable.</param>
        /// <param name="pos">The new position.</param>
        public static void SetPosition(GameObject obj, Vector3 pos)
        {
            Transform transform = GameFinder.GetHighestParent(obj).transform;
            transform.position = pos;
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
                oldLayer = stickyNote.GetComponent<OrderInLayerValueHolder>().OrderInLayer;
            }
            else
            {
                oldLayer = stickyNote.GetComponentInParent<OrderInLayerValueHolder>().OrderInLayer;
            }

            /// Checks if the order in layer should increase or decrease.
            if (newLayer - oldLayer > 0)
            {
                GameLayerChanger.ChangeOrderInLayer(GameFinder.GetHighestParent(stickyNote), newLayer,
                    GameLayerChanger.LayerChangerStates.Increase, false, true);
            }
            else
            {
                GameLayerChanger.ChangeOrderInLayer(GameFinder.GetHighestParent(stickyNote), newLayer,
                    GameLayerChanger.LayerChangerStates.Decrease, false, true);
            }
        }

        /// <summary>
        /// Combines all edit method together.
        /// </summary>
        /// <param name="stickyNote">The sticky note on that the changes should be executed.</param>
        /// <param name="config">The configuration which holds the values for the changing.</param>
        public static void Change(GameObject stickyNote, DrawableConfig config)
        {
            GameObject root = GameFinder.GetHighestParent(stickyNote);
            GameObject surface = GameFinder.GetDrawableSurface(stickyNote);
            GameObject surfaceParent = surface.transform.parent.gameObject;

            if (root.name.Contains(ValueHolder.StickyNotePrefix))
            {
                GameDrawableManager.Change(surface, config);
                ChangeLayer(root, config.Order);
                SetRotateX(root, config.Rotation.x);
                SetRotateY(root, config.Rotation.y);
                GameScaler.SetScale(surfaceParent, config.Scale);
                SetPosition(root, config.Position);
            }
        }
    }
}