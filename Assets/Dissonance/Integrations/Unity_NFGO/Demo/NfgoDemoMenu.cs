using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dissonance.Integrations.Unity_NFGO.Demo
{
    public class NfgoDemoMenu
        : MonoBehaviour
    {
        private void Start()
        {
            NetworkManager.Singleton.OnServerStarted += OnServerStarted;
        }

        private static void OnServerStarted()
        {
            NetworkManager.Singleton.SceneManager.LoadScene("NFGO Game World", LoadSceneMode.Single);
        }

        private void OnGUI()
        {
            using (new GUILayout.AreaScope(new Rect(30, 30, 300, 300)))
            { 
                if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
                    StartButtons();
            }
        }

        private static void StartButtons()
        {
            if (GUILayout.Button("Host"))
                NetworkManager.Singleton.StartHost();
            if (GUILayout.Button("Client"))
                NetworkManager.Singleton.StartClient();
            if (GUILayout.Button("Server"))
                NetworkManager.Singleton.StartServer();
        }
    }
}