using SEE.Game;
using SEE.Game.SceneManipulation;
using UnityEngine;

namespace SEE.Controls
{
    /// <summary>
    /// Super class of the behaviours of game objects the player interacts with.
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
