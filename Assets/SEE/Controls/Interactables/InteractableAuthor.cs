using HighlightPlus;
using UnityEngine;

namespace SEE.Controls.Interactables
{
    /// <summary>
    /// Handles interaction with an author object.
    /// Attached to a game object representing the author of a graph element
    /// (e.g., in a city derived from a version control system).
    /// </summary>
    internal sealed class InteractableAuthor : InteractableObject
    {
        /// <summary>
        /// The cached reference to the highlight effect component.
        /// </summary>
        private HighlightEffect effect;

        /// <summary>
        /// Animates the hit.
        /// </summary>
        private void Hit()
        {
            effect.HitFX(Color.green, 1.0f, 0.7f);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        protected override void Highlight()
        {
            if (effect == null)
            {
                effect = gameObject.AddComponent<HighlightEffect>();
                // The effect strength of the glow.
                effect.glow = 0.16f;
                // The width of the glow.
                effect.glowWidth = 1f;
                // Highlight only this object, not its children (the label).
                effect.effectGroup = TargetOptions.OnlyThisObject;
            }
            Hit();
            effect.highlighted = true;
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        protected override void Unhighlight()
        {
            if (effect != null)
            {
                Hit();
                effect.highlighted = false;
            }
        }
    }
}
