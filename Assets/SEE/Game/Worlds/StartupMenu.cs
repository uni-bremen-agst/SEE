using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SEE.Game.Worlds
{
    /// <summary>
    /// Start-up menu in the SEEStart scene to let the user select what kind
    /// of connection is requested (server, client or host = client+server).
    /// </summary>
    public class StartupMenu
        : MonoBehaviour
    {
        [Tooltip("The name of the game scene.")]
        public string GameScene = "SEEWorld";

        private void Start()
        {
            NetworkManager.Singleton.OnServerStarted += OnServerStarted;
        }

        private void OnServerStarted()
        {
            NetworkManager.Singleton.SceneManager.LoadScene(GameScene, LoadSceneMode.Single);
        }

        private void OnGUI()
        {
            using (new GUILayout.AreaScope(new Rect(30, 30, 300, 300)))
            {
                if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
                {
                    StartButtons();
                }
            }
        }

        private static void StartButtons()
        {
            if (GUILayout.Button("Host (Client and Server)"))
                NetworkManager.Singleton.StartHost();
            if (GUILayout.Button("Client"))
                NetworkManager.Singleton.StartClient();
            if (GUILayout.Button("Server"))
                NetworkManager.Singleton.StartServer();
        }
    }
}