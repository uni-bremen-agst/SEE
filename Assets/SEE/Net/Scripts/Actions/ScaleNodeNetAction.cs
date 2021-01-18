using SEE.GO;
using SEE.Net;
using UnityEngine;


public class ScaleNodeNetAction : AbstractAction
{
    public string gameObjectID;
    public Vector3 scale;
    public Vector3 position;
    public ScaleNodeNetAction(string GameObjectID, Vector3 Scale, Vector3 Positon) : base()
    {
        gameObjectID = GameObjectID;
        scale = Scale;
        position = Positon;
    }

    protected override void ExecuteOnServer()
    {

    }

    protected override void ExecuteOnClient()
    {
        if (!IsRequester())
        {
           Debug.Log(gameObjectID);
          GameObject thrdCornerSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            GameObject scaleObj = GameObject.Find(gameObjectID);
            if(scaleObj != null)
            {
                scaleObj.SetScale(scale);
                scaleObj.transform.position = position;
            }
            else
            {
                Debug.LogWarning("Kein GameObject gefunden: " + gameObjectID);
            }
        }
        
    }

   
}