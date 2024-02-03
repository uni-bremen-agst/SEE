using UnityEngine.Assertions;

namespace SEE.DataModel.DG.SourceRange
{
    /// <summary>
    /// A representation of a source-code range in the index.
    /// </summary>
    internal class Range : FileRanges
    {
        /// <summary>
        /// Start line of the range.
        /// </summary>
        public int Start;
        /// <summary>
        /// End line of the range.
        /// </summary>
        public int End;
        /// <summary>
        /// The node whose source-code range is represented.
        /// </summary>
        public Node Node;
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="start">Start line of the range.</param>
        /// <param name="end">End line of the range.</param>
        /// <param name="node">The node whose source-code range is represented.</param>
        public Range(int start, int end, Node node)
        {
            Assert.IsNotNull(node);
            Assert.IsTrue(start <= end);

            Start = start;
            End = end;
            Node = node;
        }

        public override string ToString()
        {
            return $"{Node.ID}@{Node.Path()}:{Start}-{End}";
        }
    }
}
