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
    public  string nodeMetrics1;
    public  string nodeMetrics2;
    public  string nodeMetrics3;
    public Vector3 position;

    /// <summary>
    /// Creates a new NewNodeNetActio
    /// </summary>
    /// <param name="City">the city for the new node</param>
    /// <param name="IsInnerNode">should it be a inner node</param>
    /// <param name="NodeMetrics1">metrics1 for the new node</param>
    /// <param name="NodeMetrics2">metrics2 for the new node</param>
    /// <param name="NodeMetrics3">metrics3 for the new node</param>
    /// <param name="Position">the postition for the new node</param>
    public NewNodeNetAction(SEECity City, bool IsInnerNode, string NodeMetrics1, string NodeMetrics2, string NodeMetrics3, Vector3 Position) : base()
    {
        city = City;
        isInnerNode = IsInnerNode;
        nodeMetrics1 = NodeMetrics1;
        nodeMetrics2 = NodeMetrics2;
        nodeMetrics3 = NodeMetrics3;
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
            dummy.GetComponent<DesktopNewNodeAction>().SetNodeMetrics(nodeMetrics1, nodeMetrics2, nodeMetrics3);
            //dummy.GetComponent<DesktopNewNodeAction>().NetworkNewNode(position);



           // DesktopNewNodeAction desktopNewNodeAction = new DesktopNewNodeAction();
           // desktopNewNodeAction.SetIsInnerNode(isInnerNode);
           // desktopNewNodeAction.SetCity(city);
           // desktopNewNodeAction.SetNodeMetrics(nodeMetrics);
          //  desktopNewNodeAction.NetworkNewNode(position);
                
        }
    }


}