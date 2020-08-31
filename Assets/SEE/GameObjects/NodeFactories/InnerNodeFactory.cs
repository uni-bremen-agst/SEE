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
    public abstract class InnerNodeFactory : NodeFactory
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="shader">shader to be used for rendering the materials the created objects consist of</param>
        /// <param name="colorRange">the color range of the created objects</param>
        public InnerNodeFactory(Shader shader, ColorRange colorRange)
        {
            materials = new Materials(shader, colorRange);
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
        protected Materials materials;

        /// <summary>
        /// The default height of a inner node in world space Unity unit.
        /// </summary>
        protected const float DefaultHeight = 0.000001f;

        public override uint NumberOfStyles()
        {
            return materials.NumberOfMaterials;
        }

        public override void SetStyle(GameObject block, int style)
        {
            Renderer renderer = block.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = materials.DefaultMaterial(0, Mathf.Clamp(style, 0, (int)NumberOfStyles() - 1));
            }
        }
    }
}
