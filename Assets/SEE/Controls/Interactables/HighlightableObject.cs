using UnityEngine;
using SEE.GO;
using Valve.VR.InteractionSystem;
using SEE.Game;

namespace SEE.Controls
{
    public class HighlightableObject : InteractableObject
    {
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

            Color color = GetComponent<MeshRenderer>().sharedMaterial.color;
            Color.RGBToHSV(color, out float h, out float s, out float v);

            Color localColor = Color.HSVToRGB((h - 0.05f) % 1.0f, s, v);
            Color remoteColor = Color.HSVToRGB((h + 0.05f) % 1.0f, s, v);

            HighlightMaterialChanger = new MaterialChanger(
                gameObject, 
                Materials.New(Materials.ShaderType.Transparent, localColor), 
                Materials.New(Materials.ShaderType.Transparent, remoteColor)
            );
            Transform parent = transform;
            while (parent.parent != null)
            {
                parent = parent.parent;
            }
            Portal.GetDimensions(parent.gameObject, out Vector2 min, out Vector2 max);
            Portal.SetPortal(min, max, HighlightMaterialChanger.LocalSpecialMaterial);
            Portal.SetPortal(min, max, HighlightMaterialChanger.RemoteSpecialMaterial);
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
            Hovered(true);
            new Net.HighlightBuildingAction(this).Execute();
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
            new Net.UnhighlightBuildingAction(this).Execute();
        }
    }
}