#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;

[ExecuteInEditMode]
public class MergeMeshes : MonoBehaviour
{

    public bool merge = false;
    public Mesh combinedMesh;
    public Vector3[] vertexPositions;
    public Color[] vertexColors;
    public Vector3[] vertexNormals;
    public Vector3[] tangents;
    public int[] triangles;
    public Vector2[] uvs;


    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (merge)
        {
        //    Transform[] transforms = gameObject.GetComponentsInChildren<Transform>();
            MeshFilter[] meshFilters = gameObject.GetComponentsInChildren<MeshFilter>();
            int vertexNr = 0;
            int triangleNr = 0;
            int currentVnr = 0;

            for (int i = 0; i < meshFilters.Length; i++)
            {
                // Get The Size of vertex arrays
                vertexNr = vertexNr + meshFilters[i].sharedMesh.vertexCount;
                System.Array.Resize(ref vertexPositions, vertexNr);
                System.Array.Resize(ref vertexNormals, vertexNr);
                System.Array.Resize(ref vertexColors, vertexNr);
                System.Array.Resize(ref uvs, vertexNr);
                System.Array.Resize(ref tangents, vertexNr);

                for (int j = 0; j < meshFilters[i].sharedMesh.vertexCount; j++)
                {
                    vertexPositions[j + currentVnr] = meshFilters[i].sharedMesh.vertices[j];
                    vertexNormals[j + currentVnr] = meshFilters[i].sharedMesh.normals[j];
                 //   vertexColors[j + currentVnr] = meshFilters[i].sharedMesh.colors[j];
                    uvs[j + currentVnr] = meshFilters[i].sharedMesh.uv[j];
                    tangents[j + currentVnr] = meshFilters[i].sharedMesh.tangents[j];
                }

                currentVnr = currentVnr + meshFilters[i].sharedMesh.vertexCount;
            }
            currentVnr = 0;
            int currentTri = 0;

            for (int i = 0; i < meshFilters.Length; i++)
            {
                // Get The Size of vertex arrays
                triangleNr = triangleNr + meshFilters[i].sharedMesh.triangles.Length;
                System.Array.Resize(ref triangles, triangleNr);

                for (int j = 0; j < meshFilters[i].sharedMesh.triangles.Length; j++)
                {
                    triangles[j + currentTri] = meshFilters[i].sharedMesh.triangles[j] + currentVnr;
                }

                currentTri = currentTri + meshFilters[i].sharedMesh.triangles.Length;
                currentVnr = currentVnr + meshFilters[i].sharedMesh.vertexCount;
            }



            combinedMesh = new Mesh();
            combinedMesh.vertices = vertexPositions;
            combinedMesh.colors = vertexColors;
            combinedMesh.triangles = triangles;
            combinedMesh.uv = uvs;

            GameObject mergedObject = new GameObject("SectionOne");
            // mergedObject.transform.parent = gameObject.transform;
            mergedObject.AddComponent<MeshFilter>();
            mergedObject.AddComponent<MeshRenderer>();
            MeshFilter meshF = mergedObject.transform.GetComponent<MeshFilter>();
            meshF.sharedMesh = combinedMesh;
            meshF.sharedMesh.RecalculateBounds();
            meshF.sharedMesh.RecalculateNormals();
            mergedObject.AddComponent<MeshCollider>();

            merge = false;
        }
    }
}
#endif
