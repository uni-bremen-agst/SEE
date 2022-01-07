using SEE.Game;
using UnityEngine;

namespace SEE.GO
{
    /// <summary>
    /// Common abstract super class of every NodeFactory for game objects that
    /// may represent inner graph nodes. All these must provide the method
    /// SetLineWidth(). There may be subclasses of InnerNodeFactory which can
    /// also be used for leaf nodes (e.g., cubes); so this hierarchy is not
    /// completely strict. Yet, because they can be used for inner nodes, they
    /// must have SetLineWidth().
    /// </summary>
    internal abstract class InnerNodeFactory : NodeFactory
    {
        public InnerNodeFactory(Materials.ShaderType shaderType, ColorRange colorRange)
        {
            Materials = new Materials(shaderType, colorRange);
        }

        /// <summary>
        /// Sets the width of lines drawn for the given object. If an object is not
        /// drawn by lines, this method has no effect.
        ///
        /// Precondition: The given object must have been created by subclasses.
        /// </summary>
        /// <param name="gameNode">game object to be drawn with different line width</param>
        /// <param name="lineWidth">new width of the lines</param>
        public virtual void SetLineWidth(GameObject gameNode, float lineWidth) { }

        /// <summary>
        /// The collection of materials to be used as styles by this node factory.
        /// </summary>
        public Materials Materials;

        /// <summary>
        /// The default height of an inner node in world space Unity unit.
        /// </summary>
        protected const float DefaultHeight = 0.000001f;

        public override uint NumberOfStyles()
        {
            return Materials.NumberOfMaterials;
        }

        public override void SetStyle(GameObject block, int style)
        {
            Renderer renderer = block.GetComponent<Renderer>();
            if (renderer != null)
            {
                UnityEngine.Assertions.Assert.IsNotNull(block.GetComponent<NodeRef>());
                UnityEngine.Assertions.Assert.IsNotNull(block.GetComponent<NodeRef>().Value);
                int level = block.GetComponent<NodeRef>().Value.Level;
                Materials.SetSharedMaterial(renderer, renderQueueOffset: level, index: style);
            }
        }
    }
}
