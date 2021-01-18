using SEE.Net;
using UnityEngine;


public class ScaleNodeNetAction : AbstractAction
{
    public string str;

    public ScaleNodeNetAction() : base()
    {
        str = "Hello World!";
    }

   // protected override void ExecuteOnServer;

    protected override void ExecuteOnClient()
    {
        if (!IsRequester())
        {
            Debug.Log(str);
        }
        
    }

    protected override void ExecuteOnServer()
    {
       
    }
}