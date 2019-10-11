using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CScape;
using System.Linq;

[ExecuteInEditMode]
public class PowerMerger : MonoBehaviour
{

   // public CityRandomizer cr;
    public bool organize = false;
    public bool unify = false;
    public bool makeEditable = false;
    public bool cleanScene = false;
    public Renderer[] objectsHolder;
    public Material material;
    public bool maximizeEfficiency = false;
    public Material[] mats;
    public Material[] uniqueMat;
    public GameObject holder;
    // Use this for initialization


    // Update is called once per frame


    public void CleanScene()
    {
        holder = gameObject;
        BuildingModifier[] bm = GetComponentsInChildren<BuildingModifier>(true);
        for (int b = 0; b < bm.Length; b++)
        {
            bm[b].dontDestroyChildren = true;
            DestroyImmediate(bm[b].gameObject);
        }
    }

    public void Unify()
    {
        holder = gameObject;
        UnifyMeshes[] um = holder.GetComponentsInChildren<UnifyMeshes>();
        for (int i = 0; i < um.Length; i++)
        {
            //um[i].mat = material;
            um[i].unify = true;
            MeshCollider collider = um[i].gameObject.AddComponent<MeshCollider>();

        }
    }

    public void Organize()
    {
        holder = gameObject;
        objectsHolder = holder.GetComponentsInChildren<Renderer>(false);
        


        

            Renderer[] bm = holder.GetComponentsInChildren<Renderer>(false);
            Material[] uniqueMats;
            System.Array.Resize(ref mats, 0);
            //checkMaterials
            for (int i = 0; i < bm.Length; i++)
            {
                Material currentMat = bm[i].sharedMaterial;
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
                newGO.transform.SetParent(holder.transform);
                newGO.isStatic = true;
                newGO.layer = 19;
                UnifyMeshes um = newGO.AddComponent<UnifyMeshes>();
                um.mat = mats[m];
                //um.mat = material;

                for (int i = 0; i < bm.Length; i++)
                {
                    Mesh mr = bm[i].gameObject.GetComponent<MeshFilter>().sharedMesh;
                    if (bm[i].sharedMaterial == um.mat)
                    {
                        counter = counter + mr.vertexCount;
                        if (counter < 63000)
                        {
                            if (bm[i].sharedMaterial == um.mat)
                                bm[i].gameObject.transform.SetParent(newGO.transform);
                        }

                        else
                        {

                            newGO = new GameObject("District " + counter);
                            newGO.transform.SetParent(holder.transform);
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

        
        Unify();
    }
    public void DeOrganize()
    {
        holder = gameObject;
        UnifyMeshes[] um = holder.GetComponentsInChildren<UnifyMeshes>();
        for (int i = 0; i < um.Length; i++)
        {
            um[i].DeOrganize();
        }
    }
}

