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
        /// Constructor setting the available styles.
        /// </summary>
        public InnerNodeFactory()
        {
            materials = new Materials(10, Color.white, Color.red);
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

        public override int NumberOfStyles()
        {
            return materials.NumberOfMaterials();
        }

        public override void SetStyle(GameObject block, int style)
        {
            Renderer renderer = block.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = materials.DefaultMaterial(Mathf.Clamp(style, 0, NumberOfStyles() - 1));
            }
        }
    }
}
