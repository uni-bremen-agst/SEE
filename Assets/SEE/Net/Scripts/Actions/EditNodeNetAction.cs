using SEE.DataModel.DG;
using SEE.GO;
using SEE.Net;
using UnityEngine;


public class EditNodeNetAction : AbstractAction
{
    public Node nodeToEdit = null;
    public string gameObjectID;
    /// <summary>
    /// Constructs a EditNodeNetAction
    /// </summary>
    /// <param name="GameObjectID">The id from the GameObject which should be edited through the Network</param>
    /// <param name="Node">The Node with the changes</param>
    public EditNodeNetAction(Node node, string GameObjectID) : base()
    {
        nodeToEdit = node;
        gameObjectID = GameObjectID;
    }
    /// <summary>
    /// Things to Execute on the Server (None for this Class)
    /// </summary>
    protected override void ExecuteOnServer()
    {

    }
    /// <summary>
    /// Things to Execute on the Client Sets finds the GameObject on the Client and sets its scale and position
    /// </summary>
    protected override void ExecuteOnClient()
    {
        if (!IsRequester())
        {
            GameObject goTMP = GameObject.Find(gameObjectID);
            Node node = goTMP.GetComponent<NodeRef>().node;
            if (node != null)
            {
                node.SourceName = nodeToEdit.SourceName;
                node.Type = nodeToEdit.Type;
            }
            else
            {
                //FIXME: Controll if a Debug Log is the right thing
                Debug.LogWarning("Kein GameObject gefunden: " + gameObjectID);
            }
        }

    }


}