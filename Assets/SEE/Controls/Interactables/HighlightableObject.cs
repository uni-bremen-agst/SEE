using UnityEngine;
using SEE.GO;
using Valve.VR.InteractionSystem;

namespace SEE.Controls
{
    public class HighlightableObject : InteractableObject
    {
        [Tooltip("The color to be used when the object is to be highlighted.")]
        public Color HightlightColor = Color.green;

        protected class MaterialChanger
        {
            public MaterialChanger(GameObject gameObject, Material special)
            {
                this.gameObject = gameObject;
                this.specialMaterial = special;
            }

            private readonly GameObject gameObject;

            /// <summary>
            /// This material will be used for gameObject when .
            /// </summary>
            private Material specialMaterial;

            /// <summary>
            /// The material before the object was hovered so that it can be restored
            /// when the object is no longer hovered. While hovering, a highlighting
            /// material will be used.
            /// </summary>
            private Material oldMaterial;

            /// <summary>
            /// Assigns the special material to the gameObject and stores the original 
            /// material in oldMaterial.
            /// </summary>
            public void UseSpecialMaterial()
            {
                Renderer renderer = gameObject.GetComponent<Renderer>();
                oldMaterial = renderer.sharedMaterial;
                renderer.sharedMaterial = specialMaterial;
            }

            /// <summary>
            /// Resets the original material of gameObject using the material stored in oldMaterial.
            /// </summary>
            public void ResetMaterial()
            {
                gameObject.GetComponent<Renderer>().sharedMaterial = oldMaterial;
            }
        }

        /// <summary>
        /// True if the object is currently being hovered over.
        /// </summary>
        protected bool isHovered = false;

        /// <summary>
        /// For highlighting the gameObject while it is being hovered over.
        /// </summary>
        private MaterialChanger hightlight;

        protected override void Start()
        {
            base.Start();
            hightlight = new MaterialChanger(gameObject, Materials.NewMaterial(HightlightColor));
        }

        public virtual void Hovered()
        {
            isHovered = true;
            hightlight.UseSpecialMaterial();
        }

        public virtual void Unhovered()
        {
            isHovered = false;
            hightlight.ResetMaterial();
        }

        //----------------------------------------------------------------
        // Private actions called by the hand when the object is hovered.
        // These methods are called by SteamVR by way of the interactable.
        //----------------------------------------------------------------

        /// <summary>
        /// Called by the Hand when that Hand starts hovering over this object.
        /// 
        /// Activates the source name and detail text and highlights the object by
        /// material with a different color.
        /// </summary>
        /// <param name="hand">the hand hovering over the object</param>
        private void OnHandHoverBegin(Hand hand)
        {
            Hovered();
        }

        /// <summary>
        /// Called by the Hand when that Hand stops hovering over this object
        /// 
        /// Deactivates the source name and detail text and restores the original material.
        /// </summary>
        /// <param name="hand">the hand hovering over the object</param>
        private void OnHandHoverEnd(Hand hand)
        {
            Unhovered();
        }
    }
}