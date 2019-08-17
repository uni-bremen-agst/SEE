namespace SEE
{
    public interface IEdge : IGraphElement
    {
        INode Source { get; set; }
        INode Target { get; set; }
    }
}