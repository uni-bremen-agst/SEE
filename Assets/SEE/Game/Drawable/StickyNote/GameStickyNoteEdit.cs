using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.ValueHolders;
using SEE.GO;
using UnityEngine;

namespace SEE.Game.Drawable.StickyNote
{
    /// <summary>
    /// Provides editing operations for sticky notes.
    /// </summary>
    public static class GameStickyNoteEdit
    {
        /// <summary>
        /// Changes the order in layer of the given sticky note.
        /// </summary>
        /// <param name="stickyNote">The sticky note whose order should be changed.</param>
        /// <param name="newLayer">The new order in layer.</param>
        public static void ChangeLayer(GameObject stickyNote, int newLayer)
        {
            int oldLayer;

            if (stickyNote.GetComponent<OrderInLayerValueHolder>() != null)
            {
                oldLayer = stickyNote.GetComponent<OrderInLayerValueHolder>().OrderInLayer;
            }
            else
            {
                oldLayer = stickyNote.GetComponentInParent<OrderInLayerValueHolder>().OrderInLayer;
            }

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
        /// Changes all editable values of the given sticky note.
        /// </summary>
        /// <param name="stickyNote">The sticky note whose values should be changed.</param>
        /// <param name="config">The configuration containing the new values.</param>
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
