namespace SEE.DataModel
{ 
    /// <summary>
    /// An edge between two nodes of the graph.
    /// </summary>
    public interface IEdge : IGraphElement
    {
        INode Source { get; set; }
        INode Target { get; set; }
    }
}