using SEE.DataModel.DG;
using SEE.GO;
using SEE.Net;
using UnityEngine;


public class EditNodeNetAction : AbstractAction
{
    public string scname;
    public string type;
    public string gameObjectID;
   /// <summary>
   /// Constructs a EditNodeNetAction
   /// </summary>
   /// <param name="SourceName">The new sourcename</param>
   /// <param name="Type">the new type</param>
   /// <param name="GameObjectID">the gameobject id which the node belongs to</param>
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
    /// Things to Execute on the Client Sets finds the GameObject on the Client and sets its parameter
    /// </summary>
    protected override void ExecuteOnClient()
    {
        if (!IsRequester())
        {

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