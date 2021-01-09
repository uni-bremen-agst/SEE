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
    Vector3 originalPosition;
    GameObject topSphere;
    GameObject fstCornerSphere; //x0 y0
    GameObject sndCornerSphere; //x1 y0
    GameObject thrdCornerSphere; //x1 y1
    GameObject forthCornerSphere; //x0 y1
    GameObject fstSideSphere; //x0 y0
    GameObject sndSideSphere; //x1 y0
    GameObject thrdSideSphere; //x1 y1
    GameObject forthSideSphere; //x0 y1

    //FIXMEE REPLACE WITH GUI
    GameObject endWithSave;
    GameObject endWithOutSave;

    GameObject tmpSphere = null;
    public void Start()
    {
        originalScale = gameObject.transform.lossyScale;
        originalPosition = gameObject.transform.position;
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


        //End Operations
        endWithSave = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        sphereRadius(endWithSave);
        endWithSave.GetComponent<Renderer>().material.color = Color.green;

        endWithOutSave = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        sphereRadius(endWithOutSave);
        endWithOutSave.GetComponent<Renderer>().material.color = Color.red;


        //Positioning
        setOnRoof();
        setOnSide();
    }

    private void Update()
    {

        //ScaleNode(gameObject);
        if (Input.GetMouseButton(0))
        {
            if (tmpSphere == null)
            {
                Ray ray = MainCamera.Camera.ScreenPointToRay(Input.mousePosition);

                RaycastHit hit;
                // Casts the ray and get the first game object hit
                Physics.Raycast(ray, out hit);

                //Moves the Sphere which was hit
                //top
                if (hit.collider == topSphere.GetComponent<Collider>())
                {
                    tmpSphere = topSphere;
                } //Corners
                else if (hit.collider == fstCornerSphere.GetComponent<Collider>())
                {
                    tmpSphere = fstCornerSphere;
                }
                else if (hit.collider == sndCornerSphere.GetComponent<Collider>())
                {
                    tmpSphere = sndCornerSphere;
                }
                else if (hit.collider == thrdCornerSphere.GetComponent<Collider>())
                {
                    tmpSphere = thrdCornerSphere;
                }
                else if (hit.collider == forthCornerSphere.GetComponent<Collider>())
                {
                    tmpSphere = forthCornerSphere;
                }
                //Sides
                else if (hit.collider == fstSideSphere.GetComponent<Collider>())
                {
                    tmpSphere = fstSideSphere;
                }
                else if (hit.collider == sndSideSphere.GetComponent<Collider>())
                {
                    tmpSphere = sndSideSphere;
                }
                else if (hit.collider == thrdSideSphere.GetComponent<Collider>())
                {
                    tmpSphere = thrdSideSphere;
                }
                else if (hit.collider == forthSideSphere.GetComponent<Collider>())
                {
                    tmpSphere = forthSideSphere;
                }
                //End Scalling
                else if (hit.collider == endWithSave.GetComponent<Collider>())
                {
                    endScale(true);
                }
                else if (hit.collider == endWithOutSave.GetComponent<Collider>())
                {
                    endScale(false);
                }
            }

            if (tmpSphere == topSphere)
            {
                GameNodeMover.MoveToLockAxes(tmpSphere, false, true, false);
            }
            else if (tmpSphere == fstCornerSphere || tmpSphere == sndCornerSphere || tmpSphere == thrdCornerSphere || tmpSphere == forthCornerSphere)
            {
                GameNodeMover.MoveToLockAxes(tmpSphere, true, false, true);
            }
            else if(tmpSphere == fstSideSphere ||tmpSphere == sndSideSphere)
            {
                GameNodeMover.MoveToLockAxes(tmpSphere, true, false, false);
            }
            else if(tmpSphere == thrdSideSphere ||tmpSphere == forthSideSphere)
            {
                GameNodeMover.MoveToLockAxes(tmpSphere, false, false, true);
            }
            else
            {
                tmpSphere = null;
            }
            
            scaleNode();
            setOnRoof();
            setOnSide();
        }
        else
        {
            tmpSphere = null;
            //Adjust the size of the scaling elements
            sphereRadius(topSphere);
            sphereRadius(fstSideSphere);
            sphereRadius(sndSideSphere);
            sphereRadius(thrdSideSphere);
            sphereRadius(forthSideSphere);
            sphereRadius(fstCornerSphere);
            sphereRadius(sndCornerSphere);
            sphereRadius(thrdCornerSphere);
            sphereRadius(forthCornerSphere);

            sphereRadius(endWithOutSave);
            sphereRadius(endWithSave);
        }

    }

    /// <summary>
    /// Sets the new Scale of a Node based on the sphere elements
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

        //Corner Scaling
        float scaleCorner = 0;
        scaleCorner -= (fstCornerSphere.transform.position.x - fstCornerOldSpherPos.x) + (fstCornerSphere.transform.position.z - fstCornerOldSpherPos.z); //* 0.5f;
        scaleCorner += (sndCornerSphere.transform.position.x - sndCornerOldSpherPos.x) - (sndCornerSphere.transform.position.z - sndCornerOldSpherPos.z); //* 0.5f;
        scaleCorner += (thrdCornerSphere.transform.position.x - thrdCornerOldSpherPos.x) + (thrdCornerSphere.transform.position.z - thrdCornerOldSpherPos.z);// * 0.5f;
        scaleCorner -= (forthCornerSphere.transform.position.x - forthCornerOldSpherPos.x) - (forthCornerSphere.transform.position.z - forthCornerOldSpherPos.z);// * 0.5f;

        scale.x += scaleCorner;
        scale.z += scaleCorner;

        //Move the gameObject so the user thinks he scaled only in one direction
        Vector3 position = gameObject.transform.position;
        position.y += scale.y * 0.5f;
        // position.x += (fstSideSphere.transform.position.x - fstSideOldSpherPos.x) * 0.5f;
        // position.x += (sndSideSphere.transform.position.x - sndSideOldSpherPos.x) * 0.5f;
        // position.z += (thrdSideSphere.transform.position.z - thrdSideOldSpherPos.z) * 0.5f;
        // position.z += (forthSideSphere.transform.position.z - forthSideOldSpherPos.z) * 0.5f;


        //Setting the old positions
        topOldSpherPos = topSphere.transform.position;
        fstCornerOldSpherPos = fstCornerSphere.transform.position;
        sndCornerOldSpherPos = sndCornerSphere.transform.position;
        thrdCornerOldSpherPos = thrdCornerSphere.transform.position;
        forthCornerOldSpherPos = forthCornerSphere.transform.position;
        fstSideOldSpherPos = fstSideSphere.transform.position;
        sndSideOldSpherPos = sndSideSphere.transform.position;
        thrdSideOldSpherPos = thrdSideSphere.transform.position;
        forthSideOldSpherPos = forthSideSphere.transform.position;


        
        scale = gameObject.transform.lossyScale + scale;

        //Fixes negative dimension
        if (scale.x <= 0 )
        {
            scale.x = gameObject.transform.lossyScale.x;
        }
        if(scale.y <= 0)
        {
            scale.y = gameObject.transform.lossyScale.y;
            position.y = gameObject.transform.position.y;
        }

        if(scale.z <= 0)
        {
            scale.z = gameObject.transform.lossyScale.z;

        }

        
        //transform the new pos and scale
        gameObject.transform.position = position;
        gameObject.SetScale(scale);

    }

    /// <summary>
    /// Sets the top Sphere on the Top of a GameObject and the Save and Discard objects
    /// </summary>
    private void setOnRoof()
    {
        Vector3 pos = gameObject.transform.position;
        pos.y = gameObject.GetRoof() + 0.01f;
        topSphere.transform.position = pos;

        topOldSpherPos = topSphere.transform.position;
        pos.y += 0.2f;
        pos.x += 0.1f;
        endWithSave.transform.position = pos;
        pos.x -= 0.2f;
        endWithOutSave.transform.position = pos;
    }

    /// <summary>
    /// Sets the Side Spheres
    /// </summary>
    private void setOnSide()
    {
        Transform trns = gameObject.transform;

        //fstcorner
        Vector3 pos = gameObject.transform.position;
        pos.y = gameObject.GetRoof();
        pos.x -= trns.lossyScale.x / 2 + 0.02f;
        pos.z -= trns.lossyScale.z / 2 + 0.02f;
        fstCornerSphere.transform.position = pos;
        fstCornerOldSpherPos = pos;

        //sndcorner
        pos = gameObject.transform.position;
        pos.y = gameObject.GetRoof();
        pos.x += trns.lossyScale.x / 2 + 0.02f;
        pos.z -= trns.lossyScale.z / 2 + 0.02f;
        sndCornerSphere.transform.position = pos;
        sndCornerOldSpherPos = pos;

        //thrd corner
        pos = gameObject.transform.position;
        pos.y = gameObject.GetRoof();
        pos.x += trns.lossyScale.x / 2 + 0.02f;
        pos.z += trns.lossyScale.z / 2 + 0.02f;
        thrdCornerSphere.transform.position = pos;
        thrdCornerOldSpherPos = pos;

        //forth corner
        pos = gameObject.transform.position;
        pos.y = gameObject.GetRoof();
        pos.x -= trns.lossyScale.x / 2 + 0.02f;
        pos.z += trns.lossyScale.z / 2 + 0.02f;
        forthCornerSphere.transform.position = pos;
        forthCornerOldSpherPos = pos;

        //fst side
        pos = gameObject.transform.position;
        pos.y = gameObject.GetRoof();
        pos.x -= trns.lossyScale.x / 2 + 0.01f;

        fstSideSphere.transform.position = pos;
        fstSideOldSpherPos = pos;

        //snd side
        pos = gameObject.transform.position;
        pos.y = gameObject.GetRoof();
        pos.x += trns.lossyScale.x / 2 + 0.01f;

        sndSideSphere.transform.position = pos;
        sndSideOldSpherPos = pos;

        //thrd side
        pos = gameObject.transform.position;
        pos.y = gameObject.GetRoof();

        pos.z -= trns.lossyScale.z / 2 + 0.01f;
        thrdSideSphere.transform.position = pos;
        thrdSideOldSpherPos = pos;

        //forth side
        pos = gameObject.transform.position;
        pos.y = gameObject.GetRoof();

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
        if(goScale.x > goScale.z && goScale.z > 0.1f )
        {
            sphere.transform.localScale = new Vector3(goScale.z, goScale.z, goScale.z) * 0.1f; ;
        }
        else if(goScale.x > 0.1f)
        {
            sphere.transform.localScale = new Vector3(goScale.x, goScale.x, goScale.x)*0.1f;
        }
        else
        {
            sphere.transform.localScale = new Vector3(0.01f,0.01f,0.01f);
        }

        //float scale = (goScale.x + goScale.z) / 4;
        
    }

    /// <summary>
    /// This will end the Scalling Action the user Can Choose between Safe and Discard
    /// </summary>
    /// <param name="save">Should the changes be saved</param>
    public void endScale(bool save)
    {
        if (save)//FIXME WITH USER INPUT
        {
            //SAFE THE CHANGES
            removeScript();
        }
        else
        {
            gameObject.SetScale(originalScale);
            gameObject.transform.position = originalPosition;
            removeScript();
        }
    }

    /// <summary>
    /// Removes this Script from the GameObject
    /// </summary>
    public void removeScript()
    {
        Destroy(topSphere);
        Destroy(fstCornerSphere);
        Destroy(sndCornerSphere);
        Destroy(thrdCornerSphere);
        Destroy(forthCornerSphere);
        Destroy(fstSideSphere);
        Destroy(sndSideSphere);
        Destroy(thrdSideSphere);
        Destroy(forthSideSphere);
        Destroy(endWithSave);
        Destroy(endWithOutSave);
        Destroy(this);
    }
}
