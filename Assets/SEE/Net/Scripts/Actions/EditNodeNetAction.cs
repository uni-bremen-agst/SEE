using SEE.DataModel.DG;
using SEE.GO;
using SEE.Net;
using UnityEngine;


public class EditNodeNetAction : AbstractAction
{
    public string scname;
    public string type;
    public string gameObjectID;
   // private Node nodeToEdit;
    /// <summary>
    /// Constructs a EditNodeNetAction
    /// </summary>
    /// <param name="GameObjectID">The id from the GameObject which should be edited through the Network</param>
    /// <param name="Node">The Node with the changes</param>
    public EditNodeNetAction(string SourceName, string Type, string GameObjectID) : base()
    {
       // nodeToEdit = node;
        scname = SourceName;
        type = Type;
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
            //GameObject goTMP = GameObject.Find(gameObjectID);
            //Node node = goTMP.GetComponent<NodeRef>().node;
            Node node = GameObject.Find(gameObjectID).GetNode();
            if (node != null)
            {
                node.SourceName = scname;
                node.Type = type;

            }
            else
            {
                //FIXME: Controll if a Debug Log is the right thing
                Debug.LogWarning("Kein GameObject gefunden: " + gameObjectID);
            }
        }

    }


}