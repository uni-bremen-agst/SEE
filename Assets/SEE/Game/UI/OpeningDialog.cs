using System.Collections.Generic;
using SEE.Game.UI.Menu;
using SEE.Utils;
using SEE.GO;
using UnityEngine;
using SEE.Game.UI.PropertyDialog;

namespace SEE.UI
{
    /// <summary>
    /// Implements the behaviour of the in-game menu for the selection of the networking
    /// configuration (host, server, client, settings) that is shown at the start up.
    /// </summary>
    public class OpeningDialog : MonoBehaviour
    {
        /// <summary>
        /// The UI object representing the menu the user chooses the action from.
        /// </summary>
        private SelectionMenu actionMenu;

        /// <summary>
        /// This creates and returns the action menu, with which a user can configure the
        /// networking.
        /// </summary>
        /// <param name="attachTo">The game object the menu should be attached to. If <c>null</c>, a
        /// new game object will be created.</param>
        /// <returns>the newly created action menu component.</returns>
        private SelectionMenu CreateModeMenu(GameObject attachTo = null)
        {
            // Note: A ?? expression can't be used here, or Unity's overloaded null-check will be overridden.
            GameObject actionMenuGO = attachTo ? attachTo : new GameObject { name = "Network Menu" };
            IList<ToggleMenuEntry> entries = SelectionEntries();
            SelectionMenu actionMenu = actionMenuGO.AddComponent<SelectionMenu>();
            actionMenu.EnableClosing(false); // the menu cannot be closed; user must make a decision
            actionMenu.Title = "Network Configuration";
            actionMenu.Description = "Please select the network configuration you want to activate.";
            actionMenu.AddEntries(entries);
            return actionMenu;
        }

        /// <summary>
        /// Returns the menu entries for this dialog.
        /// </summary>
        /// <returns>menu entries for this dialog</returns>
        private IList<ToggleMenuEntry> SelectionEntries()
        {
            Color color = Color.blue;

            return new List<ToggleMenuEntry>
                    { new ToggleMenuEntry(active: false,
                                          entryAction: StartHost,
                                          exitAction: null,
                                          title: "Host",
                                          description: "Starts a server and local client process.",
                                          entryColor: NextColor(),
                                          icon: Resources.Load<Sprite>("Icons/Host")),
                      new ToggleMenuEntry(active: false,
                                          entryAction: StartClient,
                                          exitAction: null,
                                          title: "Client",
                                          description: "Starts a local client connection to a server.",
                                          entryColor: NextColor(),
                                          icon: Resources.Load<Sprite>("Icons/Client")),
                      new ToggleMenuEntry(active: false,
                                          entryAction: StartServer,
                                          exitAction: null,
                                          title: "Server",
                                          description: "Starts a dedicated server without local client.",
                                          entryColor: NextColor(),
                                          icon: Resources.Load<Sprite>("Icons/Server")),
                      new ToggleMenuEntry(active: false,
                                          entryAction: Settings,
                                          exitAction: null,
                                          title: "Settings",
                                          description: "Allows to set additional network settings.",
                                          entryColor: Color.gray,
                                          icon: Resources.Load<Sprite>("Icons/Settings")),
            };

            Color NextColor()
            {
                Color result = color;
                color = color.Lighter();
                return result;
            }
        }

        /// <summary>
        /// The <see cref="Net.Network"/> component configured by this dialog.
        /// </summary>
        private Net.Network network;

        /// <summary>
        /// Starts a host (= server + local client) on this machine.
        /// </summary>
        private void StartHost()
        {
            network.StartHost();
        }

        /// <summary>
        /// Starts a client on this machine.
        /// </summary>
        private void StartClient()
        {
            network.StartClient();
        }

        /// <summary>
        /// Starts a dedicated server on this machine.
        /// </summary>
        private void StartServer()
        {
            network.StartServer();
        }

        /// <summary>
        /// Opens the dialog to configure the network settings.
        /// </summary>
        private void Settings()
        {
            Debug.Log($"{actionMenu.gameObject.GetFullName()}\n");
            actionMenu.ToggleMenu();
            NetworkPropertyDialog dialog = new NetworkPropertyDialog(network, Reactivate);
            dialog.Open();
        }

        private void Reactivate()
        {
            //actionMenu.ToggleMenu();
        }

        /// <summary>
        /// Sets <see cref="network"/> if it exists as a component attached to
        /// this game object; otherwise this component is diabled.
        /// </summary>
        private void Awake()
        {
            if (!gameObject.TryGetComponentOrLog(out network))
            {
                enabled = false;
            }
        }

        /// <summary>
        /// Creates and shows the action menu.
        /// </summary>
        private void Start()
        {
            actionMenu = CreateModeMenu(gameObject);
            actionMenu.ToggleMenu();
        }
    }
}
