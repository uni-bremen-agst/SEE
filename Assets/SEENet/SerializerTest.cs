using SEE.Net.Internal;
using UnityEngine;

public class SerializerTest : MonoBehaviour
{
    void Start()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();

        SMeshFilter sMeshFilter0 = Serializer.ToSerializableObject(meshFilter);
        string str = Serializer.Serialize(sMeshFilter0);
        Debug.Log(str.Length);
        Debug.Log(str);
        SMeshFilter sMeshFilter1 = Serializer.Deserialize<SMeshFilter>(str);

        sMeshFilter1.Initialize(meshFilter);



        GameObject go = new GameObject("HI, MY NAME IS?!");
        go.transform.position = new Vector3(1.1f, 0.0f, 0.0f);
        MeshFilter goMF = go.AddComponent<MeshFilter>();
        sMeshFilter1.Initialize(goMF);
        MeshRenderer goMR = go.AddComponent<MeshRenderer>();
    }
}
