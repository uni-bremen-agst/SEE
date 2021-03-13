using SEE.Game;
using SEE.GO;
using System;
using UnityEngine;
using SEE.Net;
using SEE.Controls.Actions;

/// <summary>
/// Creates a new edge through the network on each client.
/// </summary>
public class DummyNetAction : AbstractAction
{
    string action = "create";
    string gameObjectID;
    float posx;
    float posy;
    float posz;
    Vector3 pos;

    public DummyNetAction(Vector3 pos)
    {
        /* this.action = action;
         this.gameObjectID = gameObjectID;
         this.posx = posx;
         this.posy = posy;
         this.posz = posz; */
        this.pos = pos;
    }

    /// <summary>
    /// Stuff to execute on the Server. Nothing to be done here.
    /// </summary>
    protected override void ExecuteOnServer()
    {
        // Intentionally left blank.
    }

    /// <summary>
    /// Creates the new edge on each client.
    /// </summary>
    protected override void ExecuteOnClient()
    {
        Debug.Log("NET FUNZT");
        Debug.Log(pos.x);
        if (!IsRequester())
        {
            action = "create";
            if (action.Equals("create"))
            {
                
                DummyAction dummy = new DummyAction();
                dummy.CreateObjectAt(pos);

            }
            else if (action == "undo")
            {

            }
            else if (action == "redo")
            {

            }
        }
    }
}
