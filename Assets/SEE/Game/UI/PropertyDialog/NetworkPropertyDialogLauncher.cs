using SEE.Net;
using UnityEngine;

namespace SEE.Game.UI.PropertyDialog
{
    /// <summary>
    /// A component that can launch a <see cref="NetworkPropertyDialog"/>.
    /// This component will be attached to a GUI element for network
    /// settings.
    /// </summary>
    public class NetworkPropertyDialogLauncher : MonoBehaviour
    {
        /// <summary>
        /// Launches a <see cref="NetworkPropertyDialog"/> to set the
        /// attributes of <paramref name="networkConfig"/>.
        ///
        /// The dialog will be closed by the user by either pressing a cancel or OK button.
        /// Until then, it remains open.
        /// </summary>
        /// <param name="networkConfig">a network configuration whose values are to be set by the dialog</param>
        public void OpenDialog(NetworkConfig networkConfig)
        {
            NetworkPropertyDialog dialog = new NetworkPropertyDialog(networkConfig);
            dialog.Open();
        }
    }
}
