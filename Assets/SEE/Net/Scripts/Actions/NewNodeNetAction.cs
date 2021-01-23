using SEE.Controls;
using SEE.Game;
using SEE.GO;
using SEE.Net;
using System;
using UnityEngine;


public class NewNodeNetAction : AbstractAction
{
    public SEECity city;
    public bool isInnerNode;
    public Tuple<string, string, string> nodeMetrics;
    public Vector3 position;

    public NewNodeNetAction(SEECity City, bool IsInnerNode, Tuple<string,string,string> NodeMetrics, Vector3 Position) : base()
    {
        city = City;
        isInnerNode = IsInnerNode;
        nodeMetrics = NodeMetrics;
        position = Position;
    }
    /// <summary>
    /// Things to Execute on the Server (None for this Class)
    /// </summary>
    protected override void ExecuteOnServer()
    {

    }
    /// <summary>
    /// Things to Execute on the Client Creates a new GameObject on each client
    /// </summary>
    protected override void ExecuteOnClient()
    {
        if (!IsRequester())
        {
            GameObject dummy = new GameObject();
            dummy.AddComponent<DesktopNewNodeAction>();
            dummy.GetComponent<DesktopNewNodeAction>().SetCity(city);
            dummy.GetComponent<DesktopNewNodeAction>().SetIsInnerNode(isInnerNode);
            dummy.GetComponent<DesktopNewNodeAction>().SetNodeMetrics(nodeMetrics);
            dummy.GetComponent<DesktopNewNodeAction>().NetworkNewNode(position);



           // DesktopNewNodeAction desktopNewNodeAction = new DesktopNewNodeAction();
           // desktopNewNodeAction.SetIsInnerNode(isInnerNode);
           // desktopNewNodeAction.SetCity(city);
           // desktopNewNodeAction.SetNodeMetrics(nodeMetrics);
          //  desktopNewNodeAction.NetworkNewNode(position);
                
        }
    }


}