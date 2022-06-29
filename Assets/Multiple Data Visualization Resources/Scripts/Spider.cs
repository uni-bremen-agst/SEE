/* ==============================================================================
* escrip：SimpleSpider
* Create by DFYStudio
* ==============================================================================*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Spider : MonoBehaviour
{

    #region  How To Use
    public GameObject linePrefab;
    public GameObject Point;
    public float radius=173;   // the radius of you spiderMap, Change it to match your background spider map
    public float[] testArray = new float[] { };
    float[] testArray1 = { 0.5f, 0.7f, 0.5f, 0.5f, 0.5f, 0.7f, 0.5f, 0.5f , 0.5f, 0.7f, 0.5f, 0.5f };
    public void GenerateSpider(params float[] values0To1)  //Provide a capability array to create a spider diagram. Each value in the array ranges from 0 to 1. The lenght of the array == the count of attributes
    {
        ClearAll();

        CreatePointsLines(values0To1.Length);

        SetDataToSpider(values0To1);

        InitPointAndLine();

        if(FillPolyGon)
        {
            myUIShape.UpdateShape(allPoint);
        }
    }


    public bool FillPolyGon = true;
    public MyUIShape myUIShape;


    public List<Transform> allPoint = new List<Transform>();
     GameObject theRoot;
     List<GameObject> allRoot = new List<GameObject>();

    private void Start()
    {
        GenerateSpider(testArray);
    }


    private void Update()
    {
        //Test  delete 
        //if (Input.GetKeyDown(KeyCode.A))   // It is very easy to update the spider diagram.  delete it ,instead with code  to call GenerateSpider if you have multiply spider diagram
        //{
        //    GenerateSpider(testArray1);
        //}


        if(FillPolyGon)
        {
            myUIShape.gameObject.SetActive(true);
    

        }
        else
        {
            myUIShape.gameObject.SetActive(false);
        }


        myUIShape.UpdateShape(allPoint);

        UpdateLineChart();
    }
    #endregion

    /// <summary>
    /// 
    /// </summary>
    /// <param name="attributes"> how many attributes, So 360 / attributes get degree;</param>
    void CreatePointsLines(int attributes)
    {
        theRoot = new GameObject("Root");
        theRoot.transform.SetParent(transform);
        theRoot.transform.localPosition = Vector3.zero;
        theRoot.transform.localEulerAngles = Vector3.zero;

        for (int i = 0; i < attributes; i++)
        {

            GameObject o = Instantiate(theRoot, transform);
            o.transform.localPosition = Vector3.zero;
            theRoot.transform.localEulerAngles += new Vector3(0, 0, 360f / attributes);
            allRoot.Add(o);


        }
        for (int i = 0; i < allRoot.Count; i++)
        {
            allPoint.Add(Instantiate(Point, allRoot[i].transform).GetComponent<RectTransform>());
        }
    }

 
    void SetDataToSpider(params float[] values0To1)
    {
        for (int i = 0; i < allPoint.Count; i++)
        {
            allPoint[i].localPosition = values0To1[i] * new Vector3(radius, 0,0);
        }
    }

    void InitPointAndLine()
    {
 

        for (int a = 0; a < allPoint.Count; a++)
        {
            if (a < allPoint.Count)
            {
                GameObject line = GameObject.Instantiate<GameObject>(linePrefab);
                line.transform.SetParent(allPoint[a]);
                line.transform.localPosition = Vector3.zero;
            }
        }

    }



    void ClearAll()
    {
        if(theRoot!=null)
             Destroy(theRoot);
        foreach (var item in allPoint)
        {
            Destroy(item.gameObject);
        }
        foreach (var item in allRoot)
        {
            Destroy(item.gameObject);
        }
        allPoint.Clear();
        allRoot.Clear();
    }



    public void UpdateLineChart()
    {

        for (int a = 0; a < allPoint.Count-1; a++)
        {

            if (allPoint[a + 1] != null)
            {
                Vector3 v = (allPoint[a + 1].position - allPoint[a].position);

                allPoint[a].GetChild(0).GetComponent<RectTransform>().sizeDelta = new Vector2(v.magnitude, 50);
                allPoint[a].GetChild(0).right = v;

            }
        }

        if(allPoint.Count>=2)
        {
            Vector3 v1 = (allPoint[0].position - allPoint[allPoint.Count - 1].position);

            allPoint[allPoint.Count - 1].GetChild(0).GetComponent<RectTransform>().sizeDelta = new Vector2(v1.magnitude, 50);
            allPoint[allPoint.Count - 1].GetChild(0).right = v1;
        }
      
    }
}
