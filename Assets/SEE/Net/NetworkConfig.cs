using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SEE.Net
{
    /// <summary>
    /// Start-up configuration of the network in the SEEStart scene to let the user select what kind
    /// of connection is requested (server, client or host = client+server).
    /// </summary>
    public class NetworkConfig : MonoBehaviour
    {
        /// <summary>
        /// The name of the scene to be loaded when the game starts.
        /// </summary>
        [Tooltip("The name of the game scene.")]
        public string GameScene = "SEEWorld";

        /// <summary>
        /// Registers <see cref="OnServerStarted"/> at the <see cref="NetworkManager"/>.
        /// </summary>
        private void Start()
        {
            NetworkManager.Singleton.OnServerStarted += OnServerStarted;
        }

        /// <summary>
        /// Loads the <see cref="GameScene"/>. Will be called when the server was started.
        /// </summary>
        private void OnServerStarted()
        {
            NetworkManager.Singleton.SceneManager.LoadScene(GameScene, LoadSceneMode.Single);
        }

        /// <summary>
        /// Starts a host process, i.e., a server and a local client.
        /// </summary>
        public void StartHost()
        {
            NetworkManager.Singleton.StartHost();
        }

        /// <summary>
        /// Starts a client.
        /// </summary>
        public void StartClient()
        {
            NetworkManager.Singleton.StartClient();
        }

        /// <summary>
        /// Starts a dedicated server without client.
        /// </summary>
        public void StartServer()
        {
            NetworkManager.Singleton.StartServer();
        }

        public void OnOK()
        {
            voiceChat = preliminaryVoiceChat;
            Debug.Log($"OK {voiceChat}\n");
        }

        public void OnCancel()
        {
            Debug.Log($"Cancel {voiceChat}\n");
            preliminaryVoiceChat = voiceChat;
        }

        private enum VoiceChatSystems
        {
            None,
            Dissonance,
            Vivox
        }

        private VoiceChatSystems voiceChat = VoiceChatSystems.None;

        private VoiceChatSystems preliminaryVoiceChat = VoiceChatSystems.None;

        public void SelectNoVoiceChat()
        {
            preliminaryVoiceChat = VoiceChatSystems.None;
        }

        public void SelectDissonanceVoiceChat()
        {
            preliminaryVoiceChat = VoiceChatSystems.Dissonance;
        }

        public void SelectVivoxVoiceChat()
        {
            preliminaryVoiceChat = VoiceChatSystems.Vivox;
        }

        /// <summary>
        /// The IP address of the server.
        /// </summary>
        public string IPAddress { set; get; } = "127.0.0.1";

        /// <summary>
        /// The port where the server listens to NetCode and Dissonance traffic.
        /// </summary>
        public int ServerPort { set; get; } = 55555;

        /// <summary>
        /// The port where the server listens to SEE actions.
        /// </summary>
        public int ServerActionPort { set; get; } = 7777;
    }
}