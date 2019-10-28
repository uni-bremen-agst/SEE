using SEE.DataModel;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Extension that simplifies access to information of a node.
/// </summary>
public static class NodeExtension
{
    /// <summary>
    /// Returns whether the attribute CodeHistory.WasAdded is set in a node.
    /// </summary>
    /// <param name="node">The node to check in.</param>
    /// <returns>True if CodeHistory.WasAdded is set in the given node.</returns>
    public static bool WasAdded(this Node node)
    {
        return node.TryGetInt("CodeHistory.WasAdded", out _);
    }

    /// <summary>
    /// Returns whether the attribute CodeHistory.WasAdded is set in a node.
    /// </summary>
    /// <param name="node">The node to check in.</param>
    /// <returns>True if CodeHistory.WasModified is set in the given node.</returns>
    public static bool WasModified(this Node node)
    {
        return node.TryGetInt("CodeHistory.WasModified", out _);
    }

    /// <summary>
    /// Returns whether the attribute CodeHistory.WasRelocated is set in a node
    /// and returns the set value.
    /// </summary>
    /// <param name="node">The node to check in.</param>
    /// <param name="oldLinkageName">The value set for CodeHistory.WasRelocated in node or null if none is set.</param>
    /// <returns>True if CodeHistory.WasRelocated is set in the given node.</returns>
    public static bool WasRelocated(this Node node, out string oldLinkageName)
    {
        oldLinkageName = null;
        return node.TryGetString("CodeHistory.WasRelocated", out oldLinkageName);
    }
}
