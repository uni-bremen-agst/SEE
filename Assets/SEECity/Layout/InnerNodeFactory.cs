using UnityEngine;

namespace SEE.Layout
{
    /// <summary>
    /// Common abstract super class of every NodeFactory for inner game nodes.
    /// </summary>
    public abstract class InnerNodeFactory : NodeFactory
    {
        /// <summary>
        /// Sets the width of lines drawn for the given object.
        /// 
        /// Precondition: The given object must have been created by subclasses.
        /// </summary>
        /// <param name="gameNode">game object to be drawn with different line width</param>
        /// <param name="lineWidth">new width of the lines</param>
        public virtual void SetLineWidth(GameObject gameNode, float lineWidth) { }
    }
}
