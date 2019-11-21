using UnityEngine;

namespace SEE.Layout
{
    /// <summary>
    /// Abstract super class of all inner nodes that use a line renderer 
    /// for being drawn.
    /// </summary>
    public abstract class LineInnerNodeFactory : InnerNodeFactory
    {
        /// <summary>
        /// The default width of lines when inner nodes are drawn by lines.
        /// </summary>
        protected const float defaultLineWidth = 0.1f;

        /// <summary>
        /// Sets the width of lines drawn for the given object.
        /// 
        /// Precondition: circle must have been created by this factory and must contain
        /// a LineRenderer component.
        /// </summary>
        /// <param name="circle">game object to be drawn with different line width</param>
        /// <param name="lineWidth">new width of the lines</param>
        /// <summary>
        public override void SetLineWidth(GameObject circle, float lineWidth)
        {
            LineRenderer line = circle.GetComponent<LineRenderer>();
            LineFactory.SetWidth(line, lineWidth);
        }
    }
}
