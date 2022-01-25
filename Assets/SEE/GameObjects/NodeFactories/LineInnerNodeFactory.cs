using SEE.Game;
using UnityEngine;

namespace SEE.GO
{
    /// <summary>
    /// Abstract super class of all inner nodes that use a line renderer
    /// for being drawn.
    /// </summary>
    internal abstract class LineInnerNodeFactory : InnerNodeFactory
    {
        /// <summary>
        /// Constructor allowing to set the initial unit for the width of the lines that render this inner node.
        /// Every line width passed as a parameter to methods of this class will be multiplied by this factor
        /// for the actual rendering.
        /// </summary>
        /// <param name="colorRange">the color range of the created objects</param>
        /// <param name="unit">initial unit for the width of all lines</param>
        public LineInnerNodeFactory(ColorRange colorRange)
            : base(Materials.ShaderType.TransparentLine, colorRange)
        {
        }

        /// <summary>
        /// The default width of lines when inner nodes are drawn by lines.
        /// </summary>
        protected const float defaultLineWidth = 1.0f;

        /// <summary>
        /// Sets the width of lines drawn for the given object.
        ///
        /// Precondition: circle must have been created by this factory and must contain
        /// a LineRenderer component.
        /// </summary>
        /// <param name="circle">game object to be drawn with different line width</param>
        /// <param name="lineWidth">new width of the lines (this value will be multiplied by Unit
        /// for rendering the line)</param>
        /// <summary>
        public override void SetLineWidth(GameObject circle, float lineWidth)
        {
            LineRenderer line = circle.GetComponent<LineRenderer>();
            LineFactory.SetWidth(line, lineWidth * Unit);
        }

        public override Vector3 GetSize(GameObject block)
        {
            return block.transform.localScale;
        }
    }
}
