using SEE.DataModel;
using System.Collections.Generic;

/// <summary>
/// Compares two nodes by Edge.LinkName() for equality.
/// </summary>
public class EdgeEqualityComparer : IEqualityComparer<Edge>
{
    public bool Equals(Edge x, Edge y)
    {
        return x.LinkName().Equals(y.LinkName());
    }

    public int GetHashCode(Edge obj)
    {
        return obj.LinkName().GetHashCode();
    }
}
