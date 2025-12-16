using Cysharp.Threading.Tasks;
using SEE.Tools.LiveKit;
using SEE.UI.Menu;
using SEE.User;

namespace SEE.Net.Actions
{
    /// <summary>
    /// This class is responsible for the sharing of LiveKit settings via network from one
    /// client to all others and to the server.
    /// </summary>
    public class LiveKitSettingsNetAction : AbstractNetAction
    {
        /// <summary>
        /// The LiveKit server URL to be applied on the client.
        /// </summary>
        public string LiveKitUrl;

        /// <summary>
        /// The URL of the token service used for acquiring LiveKit access tokens.
        /// </summary>
        public string TokenUrl;

        /// <summary>
        /// The name of the LiveKit room the client should join.
        /// </summary>
        public string RoomName;

        /// <summary>
        /// Creates a new network action containing updated LiveKit
        /// configuration values to be sent to the client.
        /// </summary>
        /// <param name="liveKitUrl">The LiveKit server URL.</param>
        /// <param name="tokenUrl">The token service URL.</param>
        /// <param name="roomName">The LiveKit room name.</param>
        public LiveKitSettingsNetAction(string liveKitUrl, string tokenUrl, string roomName)
        {
            LiveKitUrl = liveKitUrl;
            TokenUrl = tokenUrl;
            RoomName = roomName;
        }

        /// <summary>
        /// Executes the action on the client side by applying the received
        /// LiveKit configuration values to the local <see cref="LiveKitVideoManager"/>.
        /// </summary>
        public override void ExecuteOnClient()
        {
            ApplyLiveKitUpdateAsync().Forget();
        }

        /// <summary>
        /// Ask the user whether the received LiveKit configuration should be applied.
        /// If confirmed, the local <see cref="UserSettings.Video"/> is updated.
        /// </summary>
        private async UniTask ApplyLiveKitUpdateAsync()
        {
            string message = $"Do you want to apply the LiveKit changes?\n"
                + $"LiveKit URL: {LiveKitUrl}\n"
                + $"Token URL: {TokenUrl}\n"
                + $"Room Name: {RoomName}";

            if (await ConfirmDialog.ConfirmAsync(ConfirmConfiguration.YesNo(message)))
            {
                UserSettings.Instance.Video.UpdateLiveKitSettings(LiveKitUrl, TokenUrl, RoomName);
                UserSettings.Instance.Save();
            }
        }
    }
}
