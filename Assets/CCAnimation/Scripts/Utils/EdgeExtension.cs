using SEE.DataModel;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class EdgeExtension
{
    public static string LinkName(this Edge edge)
    {
        return edge.Source.LinkName + edge.Target.LinkName;
    }
}
