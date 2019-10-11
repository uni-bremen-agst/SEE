#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

[ExecuteInEditMode]

public class CombineBuildings : MonoBehaviour
{
    public bool merge = false;

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (merge) MergeMeshes();
        merge = false;
    }


    public void MergeMeshes()
    {

        GameObject rooftopHolder = gameObject;
        MeshFilter[] meshFilters = rooftopHolder.GetComponentsInChildren<MeshFilter>();
        CombineInstance[] combine = new CombineInstance[meshFilters.Length - 1];

        int index = 0;
        for (int i = 0; i < 30; i++)
        {
            if (meshFilters[i].sharedMesh != null)
            {
                // if (meshFilters[i].sharedMesh == null) continue;
                combine[index].mesh = meshFilters[i].sharedMesh;
                combine[index++].transform = meshFilters[i].transform.localToWorldMatrix;
            }
        }
        GameObject mergedObject = new GameObject("SectionOne");
        // mergedObject.transform.parent = gameObject.transform;
        mergedObject.AddComponent<MeshFilter>();
        mergedObject.AddComponent<MeshRenderer>();
        MeshFilter meshF = mergedObject.transform.GetComponent<MeshFilter>();
        meshF.sharedMesh = new Mesh();
        meshF.sharedMesh.CombineMeshes(combine);
        meshF.sharedMesh.RecalculateBounds();
        mergedObject.AddComponent<MeshCollider>();

        //if (lodComponent != null) lodComponent.RecalculateBounds();

        foreach (Transform go in rooftopHolder.transform.Cast<Transform>().Reverse())
        {
            DestroyImmediate(go.gameObject);
        }

        //if (rooftopHolder.GetComponent<MeshRenderer>() == null)
        //{
        //    rh = rooftopHolder.AddComponent<MeshRenderer>() as MeshRenderer;
        //    rh = rooftopHolder.GetComponent<MeshRenderer>();
        //    rh.sharedMaterial = greebleMat;

        //}
        //rh = rooftopHolder.GetComponent<MeshRenderer>();
        //rh.sharedMaterial = greebleMat;
        //Renderer[] renderers = new Renderer[1];
        //LOD[] lod = new LOD[1];
        //renderers[0] = rooftopHolder.GetComponent<Renderer>();
        //lod[0] = new LOD(lodDistance, renderers);
        //lodComponent.SetLODs(lod);

    }
}
#endif