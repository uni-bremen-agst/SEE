using SEE.Game;
using SEE.GO;
using SEE.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameNodeScaleAction : MonoBehaviour
{

    Vector3 topOldSpherPos;
    Vector3 fstCornerOldSpherPos;
    Vector3 sndCornerOldSpherPos;
    Vector3 thrdCornerOldSpherPos;
    Vector3 forthCornerOldSpherPos;
    Vector3 fstSideOldSpherPos;
    Vector3 sndSideOldSpherPos;
    Vector3 thrdSideOldSpherPos;
    Vector3 forthSideOldSpherPos;
    Vector3 originalScale;
    GameObject topSphere;
    GameObject fstCornerSphere; //x0 y0
    GameObject sndCornerSphere; //x1 y0
    GameObject thrdCornerSphere; //x1 y1
    GameObject forthCornerSphere; //x0 y1
    GameObject fstSideSphere; //x0 y0
    GameObject sndSideSphere; //x1 y0
    GameObject thrdSideSphere; //x1 y1
    GameObject forthSideSphere; //x0 y1
    public void Start()
    {
        originalScale = gameObject.transform.lossyScale;
        Renderer render = gameObject.GetComponent<Renderer>();

        //TOP SPHERE
        topSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphereRadius(topSphere);


        //corner SPHERES
        fstCornerSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphereRadius(fstCornerSphere);

        sndCornerSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphereRadius(sndCornerSphere);

        thrdCornerSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphereRadius(thrdCornerSphere);

        forthCornerSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphereRadius(forthCornerSphere);


        //Side Spheres
        fstSideSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphereRadius(fstSideSphere);

        sndSideSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphereRadius(sndSideSphere);

        thrdSideSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphereRadius(thrdSideSphere);

        forthSideSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphereRadius(forthSideSphere);


        //Positioning
        setOnRoof();
        setOnSide();
    }

    private void Update()
    {

        //ScaleNode(gameObject);
        if (Input.GetMouseButton(0))
        {
            Ray ray = MainCamera.Camera.ScreenPointToRay(Input.mousePosition);

            RaycastHit hit;
            // Casts the ray and get the first game object hit
            Physics.Raycast(ray, out hit);
            //top
            if (hit.collider == topSphere.GetComponent<Collider>())
            {
                GameNodeMover.MoveToLockAxes(topSphere, false, true, false);

            }
            //Sides
            else if (hit.collider == fstSideSphere.GetComponent<Collider>())
            {
                GameNodeMover.MoveToLockAxes(fstSideSphere, true, false, false);
            }
            else if (hit.collider == sndSideSphere.GetComponent<Collider>())
            {
                GameNodeMover.MoveToLockAxes(sndSideSphere, true, false, false);
            }
            else if (hit.collider == thrdSideSphere.GetComponent<Collider>())
            {
                GameNodeMover.MoveToLockAxes(thrdSideSphere, false, false, true);
            }
            else if (hit.collider == forthSideSphere.GetComponent<Collider>())
            {
                GameNodeMover.MoveToLockAxes(forthSideSphere, false, false, true);
            }

            scaleNode();
            setOnRoof();
            setOnSide();
        }
        else
        {
           
            //sphereRadius(topSphere);
        }


    }

    /// <summary>
    /// Scales a node
    /// 
    /// </summary>
    private void scaleNode()
    {
        Vector3 scale = Vector3.zero;
        scale.y += topSphere.transform.position.y - topOldSpherPos.y;
        scale.x -= fstSideSphere.transform.position.x - fstSideOldSpherPos.x;
        scale.x += sndSideSphere.transform.position.x - sndSideOldSpherPos.x;
        scale.z -= thrdSideSphere.transform.position.z - thrdSideOldSpherPos.z;
        scale.z += forthSideSphere.transform.position.z - forthSideOldSpherPos.z;

        //Move the gameObject so the user thinks he scaled only in one direction
        Vector3 position = gameObject.transform.position;
        position.y += scale.y / 2;
        
        if (fstSideSphere.transform.position.x - fstSideOldSpherPos.x != 0 || sndSideSphere.transform.position.x - sndSideOldSpherPos.x != 0)
        {
            position.x += (fstSideSphere.transform.position.x - fstSideOldSpherPos.x) /2;
            position.x += (sndSideSphere.transform.position.x - sndSideOldSpherPos.x) / 2;
        } 
        else if(thrdSideSphere.transform.position.z - thrdSideOldSpherPos.z != 0 || forthSideSphere.transform.position.z - forthSideOldSpherPos.z != 0)
        {
            position.z += (thrdSideSphere.transform.position.z - thrdSideOldSpherPos.z )/ 2;
            position.z += (forthSideSphere.transform.position.z - forthSideOldSpherPos.z) / 2;
        }

        
        topOldSpherPos = topSphere.transform.position;
        fstSideOldSpherPos = fstSideSphere.transform.position;
        sndSideOldSpherPos = sndSideSphere.transform.position;
        thrdSideOldSpherPos = thrdSideSphere.transform.position;
        forthSideOldSpherPos = forthSideSphere.transform.position;
        

        gameObject.transform.position = position;
        gameObject.SetScale(gameObject.transform.lossyScale + scale);


    }

    /// <summary>
    /// Sets the top Sphere on the Top of a GameObject
    /// </summary>
    private void setOnRoof()
    {
        Vector3 pos = gameObject.transform.position;
        pos.y = BoundingBox.GetRoof(new List<GameObject> { gameObject }) + 0.01f;
        topSphere.transform.position = pos;

        topOldSpherPos = topSphere.transform.position;
    }

    /// <summary>
    /// Sets the Side Spheres
    /// </summary>
    private void setOnSide()
    {
        Transform trns = gameObject.transform;

        //fstcorner
        Vector3 pos = gameObject.transform.position;
        pos.y = BoundingBox.GetRoof(new List<GameObject> { gameObject });
        pos.x -= trns.lossyScale.x / 2 + 0.02f;
        pos.z -= trns.lossyScale.y / 2 + 0.02f;
        fstCornerSphere.transform.position = pos;
        fstCornerOldSpherPos = pos;

        //sndcorner
        pos = gameObject.transform.position;
        pos.y = BoundingBox.GetRoof(new List<GameObject> { gameObject });
        pos.x += trns.lossyScale.x / 2 + 0.02f;
        pos.z -= trns.lossyScale.y / 2 + 0.02f;
        sndCornerSphere.transform.position = pos;
        sndCornerOldSpherPos = pos;

        //thrd corner
        pos = gameObject.transform.position;
        pos.y = BoundingBox.GetRoof(new List<GameObject> { gameObject });
        pos.x += trns.lossyScale.x / 2 + 0.02f;
        pos.z += trns.lossyScale.y / 2 + 0.02f;
        thrdCornerSphere.transform.position = pos;
        thrdCornerOldSpherPos = pos;

        //forth corner
        pos = gameObject.transform.position;
        pos.y = BoundingBox.GetRoof(new List<GameObject> { gameObject });
        pos.x -= trns.lossyScale.x / 2 + 0.02f;
        pos.z += trns.lossyScale.y / 2 + 0.02f;
        forthCornerSphere.transform.position = pos;
        forthCornerOldSpherPos = pos;

        //fst side
        pos = gameObject.transform.position;
        pos.y = BoundingBox.GetRoof(new List<GameObject> { gameObject });
        pos.x -= trns.lossyScale.x / 2 + 0.01f;
   
        fstSideSphere.transform.position = pos;
        fstSideOldSpherPos = pos;

        //snd side
        pos = gameObject.transform.position;
        pos.y = BoundingBox.GetRoof(new List<GameObject> { gameObject });
        pos.x += trns.lossyScale.x / 2 + 0.01f;
   
        sndSideSphere.transform.position = pos;
        sndSideOldSpherPos = pos;

        //thrd side
        pos = gameObject.transform.position;
        pos.y = BoundingBox.GetRoof(new List<GameObject> { gameObject });
     
        pos.z -= trns.lossyScale.z / 2 + 0.01f;
        thrdSideSphere.transform.position = pos;
        thrdSideOldSpherPos = pos;

        //forth side
        pos = gameObject.transform.position;
        pos.y = BoundingBox.GetRoof(new List<GameObject> { gameObject });
      
        pos.z += trns.lossyScale.z / 2 + 0.01f;
        forthSideSphere.transform.position = pos;
        forthSideOldSpherPos = pos;

    }

    /// <summary>
    /// Sets the Radius of a Sphere dependend on the X and Z Scale of the GameObject that is scaled
    /// </summary>
    /// <param name="sphere">the Sphere to be Scaled</param>
    private void sphereRadius(GameObject sphere)
    {
        Vector3 goScale = gameObject.transform.lossyScale;
        float scale = (goScale.x + goScale.z) / 3;
        sphere.transform.localScale = new Vector3(scale, scale, scale);
    }

    /// <summary>
    /// This will end the Scalling Action the user Can Choose between Safe and Discard
    /// </summary>
    public void endScale()
    {
        if (true)//FIXME WITH USER INPUT
        {
            //SAFE THE CHANGES
            removeScript();
        }
        else
        {
            gameObject.SetScale(originalScale);
            removeScript();
        }
    }
    /// <summary>
    /// Removes this Script from the GameObject
    /// </summary>
    public void removeScript()
    {
        Destroy(topSphere);
        Destroy(this);
    }
}
