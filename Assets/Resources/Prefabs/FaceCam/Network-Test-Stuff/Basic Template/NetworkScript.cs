using Unity.Netcode;
using UnityEngine;

public class NetworkScript : NetworkBehaviour
{
    // Called on Network Spawn before Start
    public override void OnNetworkSpawn()
    {
        // Do things
        if (IsServer)
        {
            Debug.Log("IsServer");
        }
        else
        {
            Debug.Log("IsClient (But not the Server)");
        }
        if (IsClient)
        {
            Debug.Log("IsClient (Server is a Client too)");
        }

        // Always invoked the base 
        base.OnNetworkSpawn();
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        // If the NetworkObject is not yet spawned, exit early.
        if (!IsSpawned)
        {
            return;
        }
        // Netcode specific logic executed when spawned.
    }

    // FixedUpdate is normally called 50 times per Second
    void FixedUpdate()
    {
        // If the NetworkObject is not yet spawned, exit early.
        if (!IsSpawned)
        {
            return;
        }
        // Netcode specific logic executed when spawned.
    }

    // Happens on destroying
    public override void OnDestroy()
    {
        // Clean up your NetworkBehaviour

        // Always invoked the base 
        base.OnDestroy();
    }
}
