using UnityEngine;
using System.Collections;
using CScape;

namespace CScape
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [ExecuteInEditMode]
    public class UnifyMeshes : MonoBehaviour
    {
        public bool unify = false;
        public bool modify = false;
        public GameObject[] buildingsArray;
        public Material mat;
        public string exportFolderName = "Default";
        


        public void Awake()
        {
            if (Application.isPlaying) this.enabled = false;
        }


        public void Update()
        {
            if (unify)
            {
                Unify();
            }
            if (modify)
            {
                Modify();
            }
        }

        public void Modify()
        {
            transform.GetComponent<MeshFilter>().mesh = null;
            BuildingModifier[] mfArray = GetComponentsInChildren<BuildingModifier>(true);
            int x = 0;
            while (x < mfArray.Length)
            {
                mfArray[x].gameObject.SetActive(true);
                mfArray[x].gameObject.isStatic = true;
                x++;
            }
        }

        public void Unify()
        {
            Merge();
            MeshCollider mc = gameObject.GetComponent<MeshCollider>();
            if (mc)
                DestroyImmediate(mc);

            MeshCollider collider = gameObject.AddComponent<MeshCollider>();
            unify = false;
        }

        public void Merge()
        {

            MeshCollider mc = gameObject.GetComponent<MeshCollider>();
            if (mc)
                DestroyImmediate(mc);
            BuildingModifier[] mfArray = GetComponentsInChildren<BuildingModifier>(true);
            transform.GetComponent<MeshFilter>().mesh = null;
            int x = 0;
            while (x < mfArray.Length)
            {
                mfArray[x].gameObject.SetActive(true);
                mfArray[x].gameObject.isStatic = true;
                x++;
            }
            MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();
            CombineInstance[] combine = new CombineInstance[meshFilters.Length];
            int i = 0;
            while (i < meshFilters.Length)
            {
                combine[i].mesh = meshFilters[i].sharedMesh;
                combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
                meshFilters[i].gameObject.active = false;
                meshFilters[i].gameObject.isStatic = false;
                i++;
            }
            transform.GetComponent<MeshFilter>().sharedMesh = new Mesh();
            transform.GetComponent<MeshFilter>().sharedMesh.CombineMeshes(combine);
            transform.GetComponent<MeshRenderer>().material = mat;
            transform.gameObject.active = true;
            transform.gameObject.isStatic = true;
        }
        public void DeOrganize()
        {
            BuildingModifier[] bm = gameObject.GetComponentsInChildren<BuildingModifier>(true);
            int i = 0;
            while (i < bm.Length)
            {
                bm[i].gameObject.transform.parent = gameObject.transform.parent;
                bm[i].gameObject.SetActive(true);
                bm[i].gameObject.isStatic = true;
                i++;
            }
            DestroyImmediate(gameObject);
        }

        public void ExportMesh()
        {

#if UNITY_EDITOR

            Unify();
            Transform currentParent = gameObject.transform.parent;
            System.IO.Directory.CreateDirectory("Assets/CScape/Exports/Mesh" + exportFolderName);
            string path = "Assets/CScape/Exports/Mesh" + exportFolderName + "/" + gameObject.name + "_" + Random.Range(1, 100000) + "_mesh.asset";
            Mesh m = gameObject.GetComponent<MeshFilter>().sharedMesh;
            UnityEditor.MeshUtility.SetMeshCompression(m, UnityEditor.ModelImporterMeshCompression.High);
            UnityEditor.AssetDatabase.CreateAsset(m, path);

            Debug.Log("Saved asset to " + path);
            Transform[] children = gameObject.GetComponentsInChildren<Transform>(true);

            for (int i = 0; i < children.Length; i++)
            {
                children[i].transform.SetParent(transform.root);
            }
            path = "Assets/CScape/Exports/Mesh" + exportFolderName + "/" + gameObject.name + "_" + Random.Range(1, 100000) + ".prefab";
           // UnityEditor.AssetDatabase.CreateNew(gameObject, path);
            Object prefab = UnityEditor.PrefabUtility.CreatePrefab(path, gameObject);
          //  UnityEditor.PrefabUtility.ReplacePrefab(gameObject, prefab, ReplacePrefabOptions.ConnectToPrefab);

            for (int i = 0; i < children.Length; i++)
            {
                children[i].transform.SetParent(gameObject.transform);
            }

            gameObject.transform.SetParent(currentParent);

#endif
        }
    }
}