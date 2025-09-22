using SEE.Game;
using UnityEngine;

namespace SEE.Controls
{
    /// <summary>
    /// Interactable auxiliary objects like resize handles, etc.
    /// </summary>
    public sealed class InteractableAuxiliaryObject : InteractableObjectBase
    {
        /// <inheritdoc />
        public override int InteractableLayer => Layers.InteractableAuxiliaryObjects;

        /// <inheritdoc />
        public override int NonInteractableLayer => Layers.NonInteractableAuxiliaryObjects;

        /// <inheritdoc />
        public override Color? HitColor => Color.cyan;
    }
}
