using SEE.Game;
using UnityEngine;

namespace SEE.GO
{
    /// <summary>
    /// A factory for empty plain (vanilla) game objects.
    /// </summary>
    internal class VanillaFactory : InnerNodeFactory
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="shader">shader to be used for rendering the materials the created objects consist of</param>
        /// <param name="colorRange">the color range of the created objects</param>
        public VanillaFactory(Materials.ShaderType shaderType, ColorRange colorRange)
            : base(shaderType, colorRange)
        { }

        /// <summary>
        /// Returns a new empty plain game object with an ordinary Renderer component.
        /// </summary>
        /// <returns></returns>
        public override GameObject NewBlock(int index = 0, int renderQueueOffset = 0)
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
