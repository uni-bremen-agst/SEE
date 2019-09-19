using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class CSMaterialsAssign : MonoBehaviour
{

    private Vector3[] originalVertices;
    private Vector2[] originalUVs;
    private Color[] originalColors;
    private Vector4[] vColors;
    private Vector3 originalNormals;
    public Mesh meshOriginal;
    public Mesh mesh;
    public int diffuseID; // diff + mettalic
    public int normalID; // norm + depth + roughness
    public int transparencyID; // transparentMask, illumination
    public bool refresh;
    public Vector3 offsetVector;
    public bool offset = false;

    public void Awake()
    {

        originalVertices = meshOriginal.vertices;
        originalUVs = meshOriginal.uv;
        originalColors = meshOriginal.colors;
       // originalNormals = meshOriginal.normals;
        vColors = new Vector4[originalVertices.Length];
        mesh = Instantiate(meshOriginal) as Mesh;
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        meshFilter.mesh = mesh;
        Vector4[] vColorsFloat = new Vector4[mesh.uv.Length];
        Vector3[] vertices = mesh.vertices;
        Vector2[] uV = mesh.uv;

        Vector3[] transformVertices = mesh.vertices;
        Vector2[] transformUV = mesh.uv;

        Vector3[] normals = mesh.normals;

        int i = 0;
        while (i < vertices.Length)
        {
            vColors[i] = new Vector4(diffuseID, 0, 0, 0);
            if (offset)
                vertices[i] = vertices[i] + offsetVector;
            i++;
        }



        var list = new List<Vector4>(vColors);
        if (offset)
          mesh.vertices = vertices;
        //  mesh.uv = uV;
        mesh.SetUVs(3, list);
        mesh.normals = meshOriginal.normals;
        mesh.tangents = meshOriginal.tangents;

    //    mesh.RecalculateNormals();
        //  ModifyMesh();
    }

    // Use this for initialization
    void Update()
    {
        if (refresh)
        {
            Awake();
            refresh = false;
        }


       
    //    mesh.RecalculateBounds();

       
    }




    // Update is called once per frame

}
