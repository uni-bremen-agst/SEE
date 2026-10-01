using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.ValueHolders;
using UnityEngine;

namespace SEE.Controls.Actions.Drawable.CutCopyPaste
{
    /// <summary>
    /// Handles creating and positioning pasted non-mind-map drawable objects.
    /// </summary>
    internal sealed class CutCopyPastePrimitivePasteOperation
    {
        /// <summary>
        /// Creates a copy of the selected drawable object on the target surface
        /// and moves it to the requested world position.
        /// </summary>
        /// <param name="selectedObject">The drawable object to copy.</param>
        /// <param name="targetSurface">The surface on which the copy is created.</param>
        /// <param name="worldPosition">The requested world position of the copy.</param>
        /// <returns>The newly created object.</returns>
        internal GameObject Execute(GameObject selectedObject, GameObject targetSurface,
            Vector3 worldPosition)
        {
            DrawableType configuration = DrawableType.Get(selectedObject);
            configuration.ID = "";
            configuration.AssociatedPage =
                targetSurface.GetComponent<DrawableHolder>().CurrentPage;

            GameObject pastedObject =
                DrawableType.Restore(configuration, targetSurface);

            CutCopyPastePositioning.MoveToWorldPosition(
                selectedObject,
                pastedObject,
                targetSurface,
                worldPosition);

            return pastedObject;
        }
    }
}
