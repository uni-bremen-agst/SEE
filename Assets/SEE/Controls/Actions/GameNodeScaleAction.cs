using SEE.Game;
using SEE.GO;
using SEE.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameNodeScaleAction : MonoBehaviour
{

    Vector3 oldSpherPos;

    GameObject sphere;
    public void Start()
    {
        Renderer render = gameObject.GetComponent<Renderer>();
        sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
        Vector3 pos= gameObject.transform.position;
        pos.y = BoundingBox.GetRoof(new List<GameObject> { gameObject });
        sphere.transform.position = pos;

        oldSpherPos = sphere.transform.position;

        
    }

    private void Update()
    {
        //ScaleNode(gameObject);
        if (Input.GetMouseButton(0)) 
        {
            GameNodeMover.MoveTo(sphere);
            ScaleNode();
        }
      

    }

    /// <summary>
    /// Scales a node
    /// 
    /// </summary>
    /// <param name="node">The node to be scaled</param>
    
    public void ScaleNode()
    {
        
        Vector3 scale = Vector3.one;

        scale = sphere.transform.position - oldSpherPos;
        oldSpherPos = sphere.transform.position;
        if (scale.y > 0 && scale.x > 0 && scale.z > 0)
        {
            Vector3 position = gameObject.transform.position;
            position.y += scale.y / 2;
             gameObject.transform.position = position;
             gameObject.SetScale(scale + gameObject.transform.localScale);
            gameObject.SetScale(scale + gameObject.transform.localScale);
        }
    }

    public void removeScript()
    {
        Destroy(sphere);
        Destroy(this);
    }
}
