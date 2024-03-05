using UnityEngine.Assertions;

namespace SEE.DataModel.DG.SourceRange
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
        /// <param name="end">End line of the range.</param>
        /// <param name="node">The node whose source-code range is represented.</param>
        public SourceRange(int start, int end, Node node)
        {
            Assert.IsNotNull(node);
            Assert.IsTrue(start <= end);

            Range = new Range(start, end);
            Node = node;
        }

        public override string ToString()
        {
            return $"{Node.ID} declared at {Node.Path()}:{Range}";
        }
    }
}
