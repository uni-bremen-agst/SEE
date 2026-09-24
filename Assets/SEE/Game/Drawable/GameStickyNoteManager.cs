using Cysharp.Threading.Tasks;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.StickyNote;
using SEE.Game.Drawable.ValueHolders;
using SEE.GO;
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
        /// <param name="raycastHit">The chosen raycast hit.</param>
        /// <returns>The created sticky note.</returns>
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

            /// Ensures that the visible side of the sticky note faces away from
            /// the surface, even if the hit object's forward axis points in the
            /// opposite direction.
            EnsureFrontFacesAwayFromSurface(
                stickyNote,
                raycastHit.normal);

            /// Adopt the position of the hit object, but preserve the distance.
            stickyNote.transform.position = raycastHit.point
                - ValueHolder.MaxOrderInLayer
                * ValueHolder.DistanceToDrawable.z
                * stickyNote.transform.forward;

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
        /// Aligns the sticky note with the surface on which it was placed while
        /// preserving its existing orientation around the surface normal as far
        /// as possible.
        /// </summary>
        /// <param name="stickyNote">
        /// The sticky note whose orientation should be aligned.
        /// </param>
        /// <param name="surfaceNormal">
        /// The normal of the surface on which the sticky note was placed.
        /// </param>
        private static void EnsureFrontFacesAwayFromSurface(
            GameObject stickyNote,
            Vector3 surfaceNormal)
        {
            Transform transform = stickyNote.transform;

            Vector3 desiredForward = -surfaceNormal.normalized;

            if (Vector3.Dot(transform.forward, desiredForward) > 0.999f)
            {
                return;
            }

            Vector3 desiredUp = Vector3.ProjectOnPlane(transform.up, desiredForward);

            if (desiredUp.sqrMagnitude < Mathf.Epsilon)
            {
                Vector3 desiredRight = Vector3.ProjectOnPlane(transform.right, desiredForward);

                if (desiredRight.sqrMagnitude >= Mathf.Epsilon)
                {
                    desiredUp = Vector3.Cross(desiredForward, desiredRight.normalized);
                }
            }

            if (desiredUp.sqrMagnitude < Mathf.Epsilon)
            {
                desiredUp = Vector3.up;

                if (Mathf.Abs(Vector3.Dot(desiredUp, desiredForward)) > 0.999f)
                {
                    desiredUp = Vector3.forward;
                }
            }

            transform.rotation = Quaternion.LookRotation(desiredForward, desiredUp.normalized);
        }

        /// <summary>
        /// Creates an unused name for a sticky note.
        /// </summary>
        /// <returns>Unused name.</returns>
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
            while (!stickyNote.GetRootParent().name.Contains(ValueHolder.DrawableHolderPrefix))
            {
                await UniTask.Yield();
            }
            GameDrawableManager.ChangeVisibility(stickyNote, config.Visibility);
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
                GameLayerChanger.ChangeOrderInLayer(stickyNote.GetRootParent(), newLayer,
                    GameLayerChanger.LayerChangerStates.Increase, false, true);
            }
            else
            {
                GameLayerChanger.ChangeOrderInLayer(stickyNote.GetRootParent(), newLayer,
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
            GameObject root = stickyNote.GetRootParent();
            GameObject surface = GameFinder.GetDrawableSurface(stickyNote);
            GameObject surfaceParent = surface.transform.parent.gameObject;

            if (root.name.Contains(ValueHolder.StickyNotePrefix))
            {
                GameDrawableManager.Change(surface, config);
                ChangeLayer(root, config.Order);
                GameStickyNoteTransform.SetRotateX(root, config.Rotation.x);
                GameStickyNoteTransform.SetRotateY(root, config.Rotation.y);
                GameScaler.SetScale(surfaceParent, config.Scale);
                GameStickyNoteTransform.SetPosition(root, config.Position);
            }
        }
    }
}