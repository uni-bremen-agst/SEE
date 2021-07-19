using System;
using SEE.GO;
using SEE.Utils;
using UnityEngine;

namespace SEE.Controls.Actions.Architecture
{

    public delegate AbstractArchitectureAction CreateAbstractArchitectureAction();
    /// <summary>
    /// An abstract superclass of all ArchitectureActions. 
    /// </summary>
    public abstract class AbstractArchitectureAction : IArchitectureAction
    {
        public virtual void Update()
        {
            // Empty on purpose. Can be implemented by subclasses
        }

        public virtual void Awake()
        {
            // Empty on purpose. Can be implemented by subclasses
        }

        public virtual void Start()
        {
            // Empty on purpose. Can be implemented by subclasses
        }

        public virtual void Stop()
        {
            // Empty on purpose. Can be implemented by subclasses
        }
        
        /// <summary>
        /// Returns the <see cref="ArchitectureActionType"/> of this implementation.
        /// </summary>
        /// <returns>The action type</returns>
        public abstract  ArchitectureActionType GetActionType();

        /// <summary>
        /// The hovered graph element.
        /// </summary>
        protected InteractableObject hoveredObject;

        /// <summary>
        /// Event listener for <see cref="InteractableObject.AnyHoverIn"/>.
        /// </summary>
        /// <param name="hoveredObject">The hovered object</param>
        /// <param name="isInitiator">Whether this client is the initiator.</param>
        protected void OnAnyHoverIn(InteractableObject hoveredObject, bool isInitiator)
        {
            this.hoveredObject = hoveredObject;
        }

        /// <summary>
        /// Event listener for <see cref="InteractableObject.AnyHoverOut"/>.
        /// </summary>
        /// <param name="hoveredObject">The de-hovered object</param>
        /// <param name="isInitiator">Whether this client is the initiator.</param>
        protected void OnAnyHoverOut(InteractableObject hoveredObject, bool isInitiator)
        {
            this.hoveredObject = null;
        }
        
        
        /// <summary>
        /// Raycasts to find a node graph element to find a node graph element. Uses the pen position as the ray origin.
        /// </summary>
        /// <param name="raycastHit">The raycast hit.</param>
        /// <param name="position">The position of the pen</param>
        /// <returns>True if the raycast target is a node graph element.</returns>
        protected bool TryRaycast(out RaycastHit raycastHit, Vector2 position)
        {
            if (Raycasting.RaycastGraphElement(out var raycast, out GraphElementRef _, position) ==
                HitGraphElement.Node)
            {
                raycastHit = raycast;
                return true;
            }

            raycastHit = default(RaycastHit);
            return false;
        }

        
    }
}