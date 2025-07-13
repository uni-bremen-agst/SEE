using SEE.Controls;
using SEE.Controls.Actions;
using SEE.DataModel.DG;
using SEE.Game.Operator;
using SEE.GO;
using SEE.Tools.ReflexionAnalysis;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ShowEdgesOfSelectedNode : MonoBehaviour
{
  NodeRef nodeRef;


  public string ShowEdgesHere(InteractableObject obj)
  {
    if (obj == null) return "-1";
    
    obj.TryGetComponent<NodeRef>(out nodeRef);
    if (nodeRef == null) return "-2";
    
    Node? node = nodeRef.Value;

    ISet<Edge> edges = node?.Edges;

    string types = "";
    foreach (Edge edge in edges ?? new HashSet<Edge>())
    {

      types += edge.State() + " : ";
    }
    return types;
  }
}