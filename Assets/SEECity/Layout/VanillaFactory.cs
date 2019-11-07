using UnityEngine;

namespace SEE.Layout
{
    /// <summary>
    /// A factory for empty plain (vanilla) game objects.
    /// </summary>
    internal class VanillaFactory : InnerNodeFactory
    {
        /// <summary>
        /// Returns a new empty plain game object with an ordinary Renderer component.
        /// </summary>
        /// <returns></returns>
        public override GameObject NewBlock()
        {
            GameObject gameObject = new GameObject();
            
            return gameObject;
        }

        public override Vector3 GetSize(GameObject gameObject)
        {
            // We do not have a renderer. That is why we just return the local scale.
            // This object is actually not draw, i.e., kind of invisible.
            return gameObject.transform.localScale;
        }
    }
}
