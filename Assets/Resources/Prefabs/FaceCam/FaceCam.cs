using Unity.Netcode;
using UnityEngine;

public class FaceCam : NetworkBehaviour
{
    // The eobject with the position where the face of the player is at.
    Transform playersFace;
    bool testetForOwner = false;

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
        transform.localScale = new Vector3(0.2f, -0.48f, -1); // z = -1 to face away from the player.
        //transform.position = transform.parent.position;
        playersFace = transform.parent.Find("Root/Global/Position/Hips/LowerBack/Spine/Spine1/Neck/Head/NoseBase");
        Debug.Log("playersFace: " + playersFace);
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

        //transform.parent = transform.parent.parent.Find("Root/Global/Position/Hips/LowerBack/Spine/Spine1/Neck/Head/NoseBase").transform;
        //Debug.Log("Nose");



        // Send an RPC
        if (IsServer) // Only the Server is able to do so even without this if statement
        {
            if (Input.GetKeyDown(KeyCode.O))
            {
                PongClientRpc(Time.frameCount, "hello clients, this is server"); // Server -> Client (To all Clients including Server)

                //Only sent to client number 1
                DoSomethingServerSide(1);
            }


        }

        if (IsClient)
        {
            if (Input.GetKeyDown(KeyCode.L))
            {
                MyGlobalServerRpc(); // serverRpcParams will be filled in automatically
            }
        }

        // Mark this network instance as owner on the client it belongs to.
        if (!testetForOwner)
        {
            if (IsOwner)
            {
                GetComponent<MeshRenderer>().material = Resources.Load<Material>("Materials/LSHMetal/29");
            }
            testetForOwner = true;
        }
    }

    private void LateUpdate()
    {
        transform.position = playersFace.position;
        transform.rotation = playersFace.rotation;
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

    private void DoSomethingServerSide(int clientId)
    {
        // If isn't the Server/Host then we should early return here!
        if (!IsServer) return;


        // NOTE! In case you know a list of ClientId's ahead of time, that does not need change,
        // Then please consider caching this (as a member variable), to avoid Allocating Memory every time you run this function
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { (ulong)clientId }
            }
        };

        // Let's imagine that you need to compute a Random integer and want to send that to a client
        const int maxValue = 4;
        int randomInteger = Random.Range(0, maxValue);
        DoSomethingClientRpc(randomInteger, clientRpcParams);
    }

    // RPC Client method which might be invoked, only runs on Clients
    [ClientRpc]
    void PongClientRpc(int somenumber, string sometext)
    {
        Debug.Log("RPC: " + sometext + " @ Time: " + somenumber);
    }

    // RPC Server method which might be invoked, only runs on Server
    [ServerRpc(RequireOwnership = false)]
    public void MyGlobalServerRpc(ServerRpcParams serverRpcParams = default)
    {
        var clientId = serverRpcParams.Receive.SenderClientId;
        if (NetworkManager.ConnectedClients.ContainsKey(clientId))
        {
            var client = NetworkManager.ConnectedClients[clientId];
            // Do things for this client

            // On Server only, display Client
            Debug.Log("RPC: This is Client: " + clientId);
        }
    }

    [ClientRpc]
    private void DoSomethingClientRpc(int randomInteger, ClientRpcParams clientRpcParams = default)
    {
        //if (IsOwner) return;
        Debug.Log("O W N E R !");

        // Run your client-side logic here!!
        Debug.LogFormat("GameObject: {0} has received a randomInteger with value: {1}", gameObject.name, randomInteger);
    }
}
