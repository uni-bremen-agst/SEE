using SEE.DataModel;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Extension that simplifies access to information of an edge.
/// </summary>
public static class EdgeExtension
{
    /// <summary>
    /// Creates a string representing the LinkName of the two nodes of an edge.
    /// </summary>
    /// <param name="edge">The edge whose nodes are used.</param>
    /// <returns>A string from both node LinkName (Source.LinkName + Target.LinkName)</returns>
    public static string LinkName(this Edge edge)
    {
        return edge.Source.LinkName + edge.Target.LinkName;
    }
}
