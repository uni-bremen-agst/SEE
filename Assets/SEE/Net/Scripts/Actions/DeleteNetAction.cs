using SEE.Controls.Actions;
using SEE.Net;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeleteNetAction : AbstractAction
{
    public string gameObject;

    public DeleteNetAction(string gameObject)
    {
        this.gameObject = gameObject;
    }

    protected override void ExecuteOnServer()
    {
    }

    protected override void ExecuteOnClient()
    {
        if (!IsRequester())
        {
            Debug.Log(gameObject);
            DeleteAction.DeleteSelectedObject(GameObject.Find(gameObject));
        }
    }

    
}
