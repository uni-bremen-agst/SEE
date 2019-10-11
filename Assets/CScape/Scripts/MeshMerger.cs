using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CScape;

[ExecuteInEditMode]
public class MeshMerger : MonoBehaviour
{

    public CityRandomizer cr;
    public bool organize = false;
    public bool unify = false;
    public bool makeEditable = false;
    public bool cleanScene = false;
    public Transform[] sm;
    public Material material;
    public bool maximizeEfficiency = false;

    // Use this for initialization


    // Update is called once per frame
    void Update()
    {

    }

    public void CleanScene()
    {
        Transform[] bm = GetComponentsInChildren<Transform>(true);
        for (int b = 0; b < bm.Length; b++)
        {
            DestroyImmediate(bm[b].gameObject);
        }
    }

    public void Unify()
    {
        UnifyMeshes[] um = gameObject.GetComponentsInChildren<UnifyMeshes>();
        for (int i = 0; i < um.Length; i++)
        {
            um[i].mat = material;
            um[i].unify = true;
            MeshCollider collider = um[i].gameObject.AddComponent<MeshCollider>();

        }
    }

    public void Organize()
    {

        sm = gameObject.GetComponentsInChildren<Transform>();

            int counter = 0;
            MeshRenderer[] bm = gameObject.GetComponentsInChildren<MeshRenderer>();
            GameObject newGO = new GameObject("District " + counter);
            newGO.transform.SetParent(gameObject.transform);
            UnifyMeshes um = newGO.AddComponent<UnifyMeshes>();
            um.mat = material;

            for (int i = 0; i < bm.Length; i++)
            {
                Mesh mr = bm[i].gameObject.GetComponent<MeshFilter>().sharedMesh;
                counter = counter + mr.vertexCount;
                if (counter < 63000)
                {
                    bm[i].gameObject.transform.SetParent(newGO.transform);
                }
                else
                {
                    newGO = new GameObject("District " + counter);
                    newGO.transform.SetParent(gameObject.transform);
                    bm[i].gameObject.transform.SetParent(newGO.transform);
                    um = newGO.AddComponent<UnifyMeshes>();
                    um.mat = material;
                    counter = 0;
                }

            }

        
    }
    public void DeOrganize()
    {
        UnifyMeshes[] um = gameObject.GetComponentsInChildren<UnifyMeshes>();
        for (int i = 0; i < um.Length; i++)
        {
            um[i].DeOrganize();
        }
    }
}

