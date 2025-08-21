namespace SEE.Net
{
    /// <summary>
    /// Represents an edge in the SEE network.
    /// </summary>
    public class SnapshotEdge
    {

        public string EdgeId { get; set; }

        public string EdgeType { get; set; }

        /// <summary>
        /// Unique identifier of the source node.
        /// </summary>
        public string SourceNodeId { get; set; }

        /// <summary>
        /// Unique identifier of the target node.
        /// </summary>
        public string TargetNodeId { get; set; }

        public SnapshotEdge(string edgeId, string sourceNodeId, string targetNodeId, string edgeType)
        {
            EdgeId = edgeId;
            EdgeType = edgeType;
            SourceNodeId = sourceNodeId;
            TargetNodeId = targetNodeId;
        }

    }
}
