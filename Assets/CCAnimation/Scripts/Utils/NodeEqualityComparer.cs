using SEE.DataModel;
using System.Collections.Generic;

/// <summary>
/// Compares two nodes by Node.LinkName for equality.
/// </summary>
public class NodeEqualityComparer : IEqualityComparer<Node>
{
    public bool Equals(Node x, Node y)
    {
        return x.LinkName.Equals(y.LinkName);
    }

    public int GetHashCode(Node obj)
    {
        return obj.LinkName.GetHashCode();
    }
}
