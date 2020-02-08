using UnityEngine;

public class NetworkInstantiateTest : MonoBehaviour
{
    void Start()
    {
        SEE.Net.Network.Instantiate(
            "Prefabs/NetworkTest",
            new Vector3(-2.0f * Mathf.PI, 0.0f, 1.7f),
            Quaternion.Euler(15.0f, 45.0f, 30.0f),
            new Vector3(1.0f, 2.0f, 0.5f)
        );
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            SEE.Net.Network.Instantiate(
                "Prefabs/NetworkTest",
                new Vector3(-2.0f * Mathf.PI, 0.0f, 1.7f),
                Quaternion.Euler(15.0f, 45.0f, 30.0f),
                new Vector3(1.0f, 2.0f, 0.5f)
            );
        }
    }
}
