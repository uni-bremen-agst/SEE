using SEE.Controls;
using SEE.UI.Notification;
using SEE.GO;
using System;
using System.Collections.Generic;
using System.Linq;
using SEE.Utils;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using static SEE.Net.Network;
using SEE.Game.Worlds;

namespace SEE.UI.PropertyDialog
{
    /// <summary>
    /// A dialog to enter the network properties (<see cref="NetworkConfig"/>)
    /// and scene settings (<see cref="SceneSettings"/>).
    /// </summary>
    internal class NetworkPropertyDialog
    {
        /// <summary>
        /// Callback to be called when this dialog closes.
        /// </summary>
        public Action callBack;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="networkConfig">the network configuration to be manipulated by this dialog</param>
        /// <param name="callBack">delegate to be called when this dialog is closed</param>
        public NetworkPropertyDialog(Net.Network networkConfig, Action callBack = null)
        {
            this.networkConfig = networkConfig;
            this.callBack = callBack;
        }

        /// <summary>
        /// The maximal valid network port number.
        /// </summary>
        private const int maximalPortNumber = 65535;

        /// <summary>
        /// The network configuration to be manipulated by this dialog.
        /// </summary>
        private readonly Net.Network networkConfig;

        /// <summary>
        /// Event triggered when the user presses the OK button. Clients can
        /// register on this event to receive a notification when this happens.
        /// </summary>
        private readonly UnityEvent OnConfirm = new();
        /// <summary>
        /// Event triggered when the user presses the Cancel button. Clients can
        /// register on this event to receive a notification when this happens.
        /// </summary>
        private readonly UnityEvent OnCancel = new();

        /// <summary>
        /// The dialog used to manipulate the node.
        /// </summary>
        private GameObject dialog;

        /// <summary>
        /// The dialog property for the IP address to be entered in the dialog.
        /// </summary>
        private StringProperty ipAddress;

        /// <summary>
        /// The server port for Netcode and Dissonance traffic.
        /// </summary>
        private StringProperty serverPort;

        /// <summary>
        /// The password to protect the server from unauthorized clients
        /// </summary>
        private StringProperty roomPassword;

        /// <summary>
        /// The player name to be shown to others.
        /// </summary>
        private StringProperty playerName;

        /// <summary>
        /// The selector for the avatar index
        /// </summary>
        private SelectionProperty avatarSelector;

        /// <summary>
        /// The selector for the voice chat system.
        /// </summary>
        private SelectionProperty voiceChatSelector;

        /// <summary>
        /// The dialog for the user input.
        /// </summary>
        private PropertyDialog propertyDialog;

        /// <summary>
        /// Creates and opens the dialog.
        /// </summary>
        public void Open()
        {
            User.UserSettings.Instance.Load();

            dialog = new GameObject("Network settings");

            // Group for network properties (one group for all).
            PropertyGroup group = dialog.AddComponent<PropertyGroup>();
            group.Name = "Network settings";
            {
                ipAddress = dialog.AddComponent<StringProperty>();
                ipAddress.Name = "Server IPv4 Address";
                ipAddress.Value = networkConfig.ServerIP4Address;
                ipAddress.Description = "IPv4 address of the server";
                group.AddProperty(ipAddress);
            }
            {
                serverPort = dialog.AddComponent<StringProperty>();
                serverPort.Name = "Server UDP Port";
                serverPort.Value = networkConfig.ServerPort.ToString();
                serverPort.Description = "Server UDP port for NetCode and Voice Chat";
                group.AddProperty(serverPort);
            }
            {
                roomPassword = dialog.AddComponent<StringProperty>();
                roomPassword.Name = "Room Password";
                roomPassword.Value = networkConfig.RoomPassword.ToString();
                roomPassword.Description = "Password for a meeting room";
                group.AddProperty(roomPassword);
            }
            {
                playerName = dialog.AddComponent<StringProperty>();
                playerName.Name = "User name";
                playerName.Value = User.UserSettings.Instance.Player.PlayerName.ToString();
                playerName.Description = "Name of the player to be shown to others";
                group.AddProperty(playerName);
            }
            {
                avatarSelector = dialog.AddComponent<SelectionProperty>();
                avatarSelector.Name = "Avatar";
                avatarSelector.Description = "Select an avatar";
                avatarSelector.AddOptions(PlayerSpawner.Prefabs);
                avatarSelector.Value = PlayerSpawner.Prefabs[(int)User.UserSettings.Instance.Player.AvatarIndex % PlayerSpawner.Prefabs.Count];
                group.AddProperty(avatarSelector);
            }
            {
                voiceChatSelector = dialog.AddComponent<SelectionProperty>();
                voiceChatSelector.Name = "Voice Chat";
                voiceChatSelector.Description = "Select a voice chat system";
                voiceChatSelector.AddOptions(VoiceChatSystemsToStrings());
                voiceChatSelector.Value = SEE.User.UserSettings.Instance.VoiceChat.ToString();
                group.AddProperty(voiceChatSelector);
            }
            // Dialog
            propertyDialog = dialog.AddComponent<PropertyDialog>();
            propertyDialog.Title = "Network settings";
            propertyDialog.Description = "Enter the network settings";
            propertyDialog.AddGroup(group);

            // Because we will validate the input, we do not want the propertyDialog
            // to be closed until the input is valid. That is why we will handle the
            // closing ourselves.
            propertyDialog.AllowClosing(false);

            // Register listeners
            propertyDialog.OnConfirm.AddListener(OKButtonPressed);
            propertyDialog.OnCancel.AddListener(CancelButtonPressed);

            SEEInput.KeyboardShortcutsEnabled = false;
            // Go online
            propertyDialog.DialogShouldBeShown = true;
        }

        /// <summary>
        /// Returns the enum values of <see cref="VoiceChatSystems"/> as a list of strings.
        /// </summary>
        /// <returns>enum values of <see cref="VoiceChatSystems"/> as a list of strings</returns>
        private static IList<string> VoiceChatSystemsToStrings()
        {
            return Enum.GetNames(typeof(User.VoiceChatSystems)).ToList();
        }

        /// <summary>
        /// Returns the enum values of <see cref="PlayerInputType"/> as a list of strings.
        /// </summary>
        /// <returns>enum values of <see cref="PlayerInputType"/> as a list of strings</returns>
        private static IList<string> PlayerInputTypesToStrings()
        {
            return Enum.GetNames(typeof(PlayerInputType)).ToList();
        }

        /// <summary>
        /// Notifies all listeners on <see cref="OnCancel"/> and closes the dialog.
        /// </summary>
        private void CancelButtonPressed()
        {
            propertyDialog.Close();
            OnCancel.Invoke();
            SEEInput.KeyboardShortcutsEnabled = true;
            Close();
            callBack?.Invoke();
        }

        /// <summary>
        /// Validates and sets the attributes of <see cref="networkConfig"/> to the trimmed values
        ///  entered in the dialog, notifies all listeners on <see cref="OnConfirm"/>, and closes
        ///  the dialog.
        /// </summary>
        private void OKButtonPressed()
        {
            bool errorOccurred = false;

            {
                // Server IP Address
                string ipAddressValue = ipAddress.Value.Trim();
                if (HasCorrectIPv4AddressSyntax(ipAddressValue))
                {
                    networkConfig.ServerIP4Address = ipAddressValue;
                }
                else
                {
                    ShowNotification.Error("IPv4 Syntax Error", "IPv4 addresses must have syntax number.number.number.number where number is a value in between 0 and 255.");
                    errorOccurred = true;
                }
            }
            {
                // Server Port Number
                if (Int32.TryParse(serverPort.Value.Trim(), out int serverPortNumber)
                    && 0 <= serverPortNumber && serverPortNumber <= maximalPortNumber)
                {
                    networkConfig.ServerPort = serverPortNumber;
                }
                else
                {
                    ShowPortError("Server");
                    errorOccurred = true;
                }
            }
            {
                // Player Name
                string playerNameValue = playerName.Value.Trim();
                if (!string.IsNullOrWhiteSpace(playerNameValue))
                {
                    User.UserSettings.Instance.Player.PlayerName = playerNameValue;
                }
                else
                {
                    ShowNotification.Error("Invalid User Name", "User name needs to be at least one character long and must not consist of whitespace only.");
                    errorOccurred = true;
                }
            }
            {
                // Avatar
                string value = avatarSelector.Value.Trim();
                User.UserSettings.Instance.Player.AvatarIndex = (uint)PlayerSpawner.Prefabs.IndexOf(value);
            }
            {
                // Room Password
                networkConfig.RoomPassword = roomPassword.Value.ToString();
            }
            {
                // Voice Chat
                string value = voiceChatSelector.Value.Trim();
                if (Enum.TryParse(value, out User.VoiceChatSystems voiceChat))
                {
                    User.UserSettings.Instance.VoiceChat = voiceChat;
                }
                else
                {
                    ShowNotification.Error("Invalid Voice Chat", "Your choice of a voice chat is not available.");
                    errorOccurred = true;
                }
            }

            if (!errorOccurred)
            {
                propertyDialog.Close();
                User.UserSettings.Instance.Save();
                OnConfirm.Invoke();
                SEEInput.KeyboardShortcutsEnabled = true;
                Close();
                callBack?.Invoke();
            }

            static void ShowPortError(string portPrefix)
            {
                ShowNotification.Error(portPrefix + " Port Error", $"A port must be an integer in the range of 0 to {maximalPortNumber}.");
            }
        }

        /// <summary>
        /// True if <paramref name="ipAddress"/> conforms to the syntax of numeric IPv4
        /// addresses, i.e., number.number.number.number where number is an integer in
        /// the range of 0 to 255.
        /// </summary>
        /// <param name="ipAddress">the IP address to be validated syntactically</param>
        /// <returns>true if <paramref name="ipAddress"/> conforms to the syntax</returns>
        private static bool HasCorrectIPv4AddressSyntax(string ipAddress)
        {
            if (String.IsNullOrWhiteSpace(ipAddress))
            {
                return false;
            }

            string[] numbers = ipAddress.Split('.');
            if (numbers.Length != 4)
            {
                return false;
            }
            return numbers.All(r => byte.TryParse(r, out byte _));
        }

        /// <summary>
        /// Destroys <see cref="dialog"/>. <see cref="dialog"/> will be null afterwards.
        /// </summary>
        private void Close()
        {
            Destroyer.Destroy(dialog);
            dialog = null;
        }
    }
}
