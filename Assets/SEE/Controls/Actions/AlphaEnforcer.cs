using SEE.Utils;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Enforces the given alpha value (<see cref="TargetAlpha"/>) by changing the game objects's material's
    /// alpha value. This allows the color to be changed elsewhere in the code without having to worry about
    /// keeping the same alpha value. For this, the game object must have a renderer whose material has a color
    /// attribute.
    /// 
    /// Note that after attaching this component to a game object, its alpha value should only be changed through
    /// this component, not manually.
    /// </summary>
    public class AlphaEnforcer: MonoBehaviour
    {
        /// <summary>
        /// Material of the game object. Must have a color property.
        /// </summary>
        private Material materialToEnforce;
        
        /// <summary>
        /// The alpha value which should be enforced.
        /// Will be enforced every frame using <see cref="LateUpdate"/>.
        /// </summary>
        public float TargetAlpha = 1f;

        private void Start()
        {
            materialToEnforce = GetComponent<Renderer>().material;
        }

        private void LateUpdate()
        {
            materialToEnforce.color = materialToEnforce.color.WithAlpha(TargetAlpha);
        }
    }
}