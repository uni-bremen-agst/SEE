using SEE.DataModel;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class NodeExtension
{
    public static bool WasAdded(this Node node)
    {
        return node.TryGetInt("CodeHistory.WasAdded", out _);
    }

    public static bool WasModified(this Node node)
    {
        return node.TryGetInt("CodeHistory.WasModified", out _);
    }

    public static bool WasRelocated(this Node node, out string oldLinkageName)
    {
        // TODO flo export: setString for oldLinkageName
        oldLinkageName = "";
        return node.TryGetString("CodeHistory.WasRelocated", out oldLinkageName);
    }
}
