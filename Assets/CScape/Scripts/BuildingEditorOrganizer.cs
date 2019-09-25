using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CScape;
using System.Linq;

[ExecuteInEditMode]
public class BuildingEditorOrganizer : MonoBehaviour
{

    public CityRandomizer cr;
    public bool organize = false;
    public bool unify = false;
    public bool makeEditable = false;
    public bool cleanScene = false;
    public StreetModifier[] sm;
    public Material material;
    public bool maximizeEfficiency = false;
    public Material[] mats;
    public Material[] uniqueMat;
    public string stats;
    public bool isMerged = false;

    // Use this for initialization


    // Update is called once per frame
    void Update()
    {

    }

    public void CleanScene()
    {
        BuildingModifier[] bm = GetComponentsInChildren<BuildingModifier>(true);
        for (int b = 0; b < bm.Length; b++)
        {
            bm[b].dontDestroyChildren = true;
            DestroyImmediate(bm[b].gameObject);
        }
    }

    public void Unify()
    {
        UnifyMeshes[] um = cr.buildings.GetComponentsInChildren<UnifyMeshes>();
        for (int i = 0; i < um.Length; i++)
        {
            //um[i].mat = material;
            um[i].unify = true;
            MeshCollider collider = um[i].gameObject.AddComponent<MeshCollider>();

        }
        isMerged = true;
    }

    public void Organize()
    {

        sm = cr.streets.GetComponentsInChildren<StreetModifier>();
        if (!maximizeEfficiency)
        {
            for (int i = 0; i < sm.Length; i++)
            {
                if (sm[i].streetType == StreetModifier.CScapeStreetType.Street)
                {
                    GameObject newGO = new GameObject("District " + i);
                    newGO.transform.SetParent(cr.buildings.transform);
                    newGO.isStatic = true;

                    UnifyMeshes um = newGO.AddComponent<UnifyMeshes>();
                    um.mat = material;



                    if (sm[i].frontBuildings.Length > 0)
                    {
                        for (int j = 0; j < sm[i].frontBuildings.Length; j++)
                        {
                            //  Debug.Log(sm[i].frontBuildings.Length + " " + sm[i]);
                            if (sm[i].frontBuildings[j] != null) sm[i].frontBuildings[j].transform.SetParent(newGO.transform);
                        }
                    }
                    if (sm[i].backBuildings.Length > 0)
                    {
                        for (int j = 0; j < sm[i].backBuildings.Length; j++)
                        {
                            if (sm[i].backBuildings[j] != null) sm[i].backBuildings[j].transform.SetParent(newGO.transform);
                        }
                    }

                    if (sm[i].leftBuildings.Length > 0)
                    {
                        for (int j = 0; j < sm[i].leftBuildings.Length; j++)
                        {
                            if (sm[i].leftBuildings[j] != null) sm[i].leftBuildings[j].transform.SetParent(newGO.transform);
                        }
                    }
                    if (sm[i].rightBuildings.Length > 0)
                    {
                        for (int j = 0; j < sm[i].rightBuildings.Length; j++)
                        {
                            if (sm[i].rightBuildings[j] != null) sm[i].rightBuildings[j].transform.SetParent(newGO.transform);

                        }
                    }

                }
            }

        }

        else
        {

            BuildingModifier[] bm = cr.buildings.GetComponentsInChildren<BuildingModifier>();
            Material[] uniqueMats;
            System.Array.Resize(ref mats, 0);
            //checkMaterials
            for (int i = 0; i < bm.Length; i++)
            {
                Material currentMat = bm[i].gameObject.GetComponent<Renderer>().sharedMaterial;
                if (currentMat != null)
                {
                    System.Array.Resize(ref mats, mats.Length + 1);
                    mats[i] = currentMat;
                }
            }

            uniqueMats = mats.Distinct().ToArray();
            mats = uniqueMats;

            
            for (int m = 0; m < mats.Length; m++)
            {

                int counter = 0;
                GameObject newGO = new GameObject("District " + counter);
            newGO.transform.SetParent(cr.buildings.transform);
            newGO.isStatic = true;
            newGO.layer = 19;
                UnifyMeshes um = newGO.AddComponent<UnifyMeshes>();
                um.mat = mats[m];
                //um.mat = material;

                for (int i = 0; i < bm.Length; i++)
                {
                    Mesh mr = bm[i].gameObject.GetComponent<MeshFilter>().sharedMesh;
                    if (bm[i].GetComponent<Renderer>().sharedMaterial == um.mat)
                    {
                        counter = counter + mr.vertexCount;
                        if (counter < 63000)
                        {
                            if (bm[i].GetComponent<Renderer>().sharedMaterial == um.mat)
                                bm[i].gameObject.transform.SetParent(newGO.transform);
                        }

                        else
                        {

                            newGO = new GameObject("District " + counter);
                            newGO.transform.SetParent(cr.buildings.transform);
                            newGO.isStatic = true;
                            newGO.layer = 19;
                            bm[i].gameObject.transform.SetParent(newGO.transform);
                            um = newGO.AddComponent<UnifyMeshes>();
                            um.mat = mats[m];
                            counter = 0;
                        }
                    }
                }
            }

        }
        Unify();
    }
    public void DeOrganize()
    {
        UnifyMeshes[] um = cr.buildings.GetComponentsInChildren<UnifyMeshes>();
        for (int i = 0; i < um.Length; i++)
        {
            um[i].DeOrganize();
        }
        isMerged = false;
    }
    public void printStats()
    {
        BuildingModifier[] bm = cr.buildings.GetComponentsInChildren<BuildingModifier>(true);
        Debug.Log("This scene uses " + bm.Length + " buildings");
    }
}
