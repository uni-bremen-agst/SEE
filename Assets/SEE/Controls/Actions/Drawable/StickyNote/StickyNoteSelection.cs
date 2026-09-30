using SEE.Game;
using SEE.Game.Drawable;
using UnityEngine;

namespace SEE.Controls.Actions.Drawable.StickyNote
{
    /// <summary>
    /// Provides shared selection checks for sticky note operations.
    /// </summary>
    internal static class StickyNoteSelection
    {
        /// <summary>
        /// Checks whether the selected object can belong to a sticky note.
        /// </summary>
        /// <param name="selectedObject">The selected object.</param>
        /// <returns>Whether the object can belong to a sticky note.</returns>
        internal static bool IsSelectableObject(GameObject selectedObject)
        {
            return selectedObject.CompareTag(Tags.Drawable)
                || GameFinder.HasDrawableSurface(selectedObject)
                || IsPartOfStickyNote(selectedObject);
        }

        /// <summary>
        /// Checks whether the selected object is a direct child of a sticky note.
        /// </summary>
        /// <param name="selectedObject">The selected object.</param>
        /// <returns>Whether the object is part of a sticky note.</returns>
        internal static bool IsPartOfStickyNote(GameObject selectedObject)
        {
            return selectedObject.transform.parent != null
                && selectedObject.transform.parent.name.StartsWith(ValueHolder.StickyNotePrefix);
        }
    }
}
