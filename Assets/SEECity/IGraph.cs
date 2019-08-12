public interface IGraph : IAttributable
{
    int EdgeCount { get; }
    int NodeCount { get; }
    string Name { get; set; }

    INode NewNode();
    IEdge NewEdge();

    void SetLinkname(INode node, string linkName);

    INode[] Nodes(); 

}