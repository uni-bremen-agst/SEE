using System;
using UnityEngine;

namespace SEE.Controls.Architecture
{
    /// <summary>
    /// Abstract component class that can be extended to implement behaviours
    /// that rely on the pen interaction like e.g hovering, selection.
    /// </summary>
    public abstract class PenInteractionAction : MonoBehaviour
    {
        /// <summary>
        /// The <see cref="PenInteraction"/> component that is attached to this game object.
        /// </summary>
        protected PenInteraction interactionObject;
        private void Awake()
        {
            //This component automatically gets disabled when no PenInteraction component was attached.
            if (!gameObject.TryGetComponent(out interactionObject))
            {
                enabled = false;
            }
        }
    }
}