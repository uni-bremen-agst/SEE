using UnityEngine;
using SEE.GO;
using Valve.VR.InteractionSystem;

namespace SEE.Controls
{
    public class HighlightableObject : InteractableObject
    {
        [Tooltip("The color to be used when the object is to be highlighted by this client.")]
        public Color LocalHightlightColor = new Color(0.0f, 1.0f, 0.0f);

        [Tooltip("The color to be used when the object is to be highlighted some other client.")]
        public Color RemoteHightlightColor = new Color(0.8f, 1.0f, 0.2f);

        public class MaterialChanger
        {
            public MaterialChanger(GameObject gameObject, Material localSpecialMaterial, Material remoteSpecialMaterial)
            {
                this.gameObject = gameObject;
                this.localSpecialMaterial = localSpecialMaterial;
                this.remoteSpecialMaterial = remoteSpecialMaterial;
            }

            private readonly GameObject gameObject;

            private Material localSpecialMaterial;
            private Material remoteSpecialMaterial;

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
            public void UseSpecialMaterial(bool isOwner)
            {
                Renderer renderer = gameObject.GetComponent<Renderer>();
                oldMaterial = renderer.sharedMaterial;
                if (isOwner)
                {
                    renderer.sharedMaterial = localSpecialMaterial;
                }
                else
                {
                    renderer.sharedMaterial = remoteSpecialMaterial;
                }
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
        public bool IsHovered { get; private set; } = false;

        /// <summary>
        /// For highlighting the gameObject while it is being hovered over.
        /// </summary>
        public MaterialChanger HighlightMaterialChanger { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            HighlightMaterialChanger = new MaterialChanger(gameObject, Materials.NewMaterial(LocalHightlightColor), Materials.NewMaterial(RemoteHightlightColor));
        }

        public virtual void Hovered(bool isOwner)
        {
            IsHovered = true;
            HighlightMaterialChanger.UseSpecialMaterial(isOwner);
        }

        public virtual void Unhovered()
        {
            IsHovered = false;
            HighlightMaterialChanger.ResetMaterial();
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
            // TODO: multiplayersupport
            //Hovered(true);
        }

        /// <summary>
        /// Called by the Hand when that Hand stops hovering over this object
        /// 
        /// Deactivates the source name and detail text and restores the original material.
        /// </summary>
        /// <param name="hand">the hand hovering over the object</param>
        private void OnHandHoverEnd(Hand hand)
        {
            // TODO: multiplayersupport
            //Unhovered();
        }
    }
}