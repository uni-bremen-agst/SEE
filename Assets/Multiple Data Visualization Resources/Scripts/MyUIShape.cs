/* ==============================================================================
* escrip：MyUIShape
* Create by DFYStudio
* ==============================================================================*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class MyUIShape : Graphic
{
   

    public List<Transform> thepoints = new List<Transform>();



    protected override void Start()
    {
        thepoints.Clear();

    }

    //public List<Transform> tempListPoints = new List<Transform>();


    public void UpdateShape(List<Transform> theList)
    {


        foreach (Transform item in thepoints)
        {
            Destroy(item.gameObject);
        }

        thepoints.Clear();

        for (int i = 0; i < theList.Count; i++)
        {
            GameObject o = new GameObject("point");
            o.transform.SetParent(transform);
            o.transform.position = theList[i].transform.position;
            thepoints.Add(o.transform);
 
        }

 
        //Debug.Log("thePointAdd"+thepoints.Count);
 

        UpdateGeometry();
    }

    private void Update()
    {
        //if (Input.GetKeyDown(KeyCode.Space))
        //{

        //    UpdateShape(tempListPoints);
        //}
    }

    public void ClearShape()
    {
         
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
 
        UIVertex vertext = UIVertex.simpleVert;
        vertext.color = color;
        vertext.position = Vector2.zero;
        vh.AddVert(vertext);
        for (int i = 0; i < thepoints.Count; i++)
        {
            vertext.position = new Vector3(thepoints[i].localPosition.x, thepoints[i].localPosition.y);
            vh.AddVert(vertext);
        }

        if(thepoints.Count>2)
        {
            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }
        if (thepoints.Count >3)
        {
            vh.AddTriangle(0, 3, 4);
          
        }
        if (thepoints.Count > 4)
        {
            vh.AddTriangle(0, 4, 5);
        }
        if (thepoints.Count > 5)
        {
            vh.AddTriangle(0, 5, 6);
        }

        if (thepoints.Count > 6)
        {
            vh.AddTriangle(0, 6, 7);
        }

        if (thepoints.Count > 7)
        {
            vh.AddTriangle(0, 7, 8);
        }

        if (thepoints.Count > 8)
        {
            vh.AddTriangle(0, 9, 8);
        }

        if (thepoints.Count > 9)
        {
            vh.AddTriangle(0, 9, 10);
        }

        if (thepoints.Count > 10)
        {
            vh.AddTriangle(0, 10, 11);
        }

        if (thepoints.Count > 11)
        {
            vh.AddTriangle(0, 11, 12);
        }
        if (thepoints.Count > 12)
        {
            vh.AddTriangle(0, 12, 13);
        }

        vh.AddTriangle(0, 1, thepoints.Count);
    }

 
}
