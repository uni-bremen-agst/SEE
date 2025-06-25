namespace SEE.Net
{
    /// <summary>
    /// Represents an edge in the SEE network.
    /// </summary>
    public class SnapshotEdge
    {
        /// <summary>
        /// Unique identifier of the source node.
        /// </summary>
        public string SourceNodeId { get; set; }

        /// <summary>
        /// Unique identifier of the target node.
        /// </summary>
        public string TargetNodeId { get; set; }

        public SnapshotEdge(string sourceNodeId, string targetNodeId)
        {
            SourceNodeId = sourceNodeId;
            TargetNodeId = targetNodeId;
        }

    }
}
