using SEE.Game.Drawable;
using SEE.GO;
using SEE.Net.Actions.Drawable;
using UnityEngine;

namespace SEE.Controls.Actions.Drawable.CutCopyPaste
{
    /// <summary>
    /// Provides shared positioning functionality for pasted drawable objects.
    /// </summary>
    internal static class CutCopyPastePositioning
    {
        /// <summary>
        /// Moves the pasted object to the requested world position while retaining
        /// the local depth of the original object.
        /// </summary>
        /// <param name="sourceObject">The original object from which the local depth is taken.</param>
        /// <param name="pastedObject">The pasted object to move.</param>
        /// <param name="targetSurface">The drawable surface containing the pasted object.</param>
        /// <param name="worldPosition">The requested world position.</param>
        internal static void MoveToWorldPosition(GameObject sourceObject, GameObject pastedObject,
            GameObject targetSurface, Vector3 worldPosition)
        {
            Vector3 localPosition = targetSurface.GetRootParent().transform
                .InverseTransformPoint(worldPosition);

            localPosition = new Vector3(
                localPosition.x,
                localPosition.y,
                sourceObject.transform.localPosition.z);

            GameMoveRotator.SetPosition(pastedObject, localPosition, true);

            new MoveNetAction(
                targetSurface.name,
                GameFinder.GetDrawableSurfaceParentName(targetSurface),
                pastedObject.name,
                localPosition,
                true).Execute();
        }
    }
}
