using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AOcalculation : MonoBehaviour {
    public MeshFilter mesh;
    public MeshRenderer meshRenderer;
    private List<Color> colors;
    public int steps = 16;
    public float angle = 0.5f;



    // Use this for initialization
    void Start () {
        meshRenderer = GetComponent<MeshRenderer>();
        mesh = GetComponent<MeshFilter>();
        colors = new List<Color>(mesh.mesh.vertexCount);

        for (int i = 0; i < mesh.mesh.vertices.Length; i++)
        {
            colors.Add(new Color(CastRays(mesh.mesh.vertices[i], mesh.mesh.normals[i]), 0, 0, 0));
        }
        Debug.Log(colors.Count);
        Mesh newMesh = new Mesh();
        newMesh.vertices = mesh.mesh.vertices;
        newMesh.SetColors(colors);
        newMesh.UploadMeshData(true);

        meshRenderer.additionalVertexStreams = newMesh;

        //  



    }

    // Update is called once per frame
    void Update()
    {

    }

    float CastRays(Vector3 vPosition, Vector3 vNormal) {
        float vOut = 0;
        float vOutTemp = 0;
        RaycastHit hit;
        Debug.Log(vNormal + " " + vPosition);
        for (int i = 0; i < steps; i++) {
            if (Physics.Raycast(transform.TransformPoint(vPosition), transform.TransformPoint (vNormal + vPosition) + new Vector3(Random.Range(vNormal.x - angle, vNormal.x + angle), Random.Range(vNormal.y - angle, vNormal.y + angle), Random.Range(vNormal.z - angle, vNormal.z + angle)), out hit, 1000f))
            {
                vOutTemp = hit.distance / 50000;
            }

            else vOutTemp = 1;
            vOut = vOut + vOutTemp;

        }
        vOut = vOut / steps;
        return vOut;

    }

}
