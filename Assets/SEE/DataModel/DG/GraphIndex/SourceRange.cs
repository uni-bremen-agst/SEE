using UnityEngine.Assertions;

namespace SEE.DataModel.DG.GraphIndex
{
    /// <summary>
    /// A representation of a source-code range in the index.
    /// </summary>
    internal class SourceRange : FileRanges
    {
        /// <summary>
        /// The bounds of the range.
        /// </summary>
        public readonly Range Range;

        /// <summary>
        /// The node whose source-code range is represented.
        /// </summary>
        public readonly Node Node;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="start">Start line of the range.</param>
        /// <param name="end">End line of the range (inclusive).</param>
        /// <param name="node">The node whose source-code range is represented.</param>
        public SourceRange(int start, int end, Node node)
        {
            Assert.IsNotNull(node);
            Assert.IsTrue(start <= end, $"Start line {start} must be less than or equal to end line {end}");

            Range = new Range(start, end+1);  // In a Range, the end line is exclusive
            Node = node;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="range">The range that is represented.</param>
        /// <param name="node">The node whose source-code range is represented.</param>
        public SourceRange(Range range, Node node)
        {
            Assert.IsNotNull(node);
            Assert.IsNotNull(range);

            Range = range;
            Node = node;
        }

        public override string ToString()
        {
            return $"{Node.ID} declared at {Node.Path()}:{Range}";
        }
    }
}
