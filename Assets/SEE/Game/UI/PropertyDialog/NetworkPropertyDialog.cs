using SEE.Controls;
using SEE.Game.UI.Notification;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using static SEE.Net.Network;

namespace SEE.Game.UI.PropertyDialog
{
    /// <summary>
    /// A dialog to enter the network properties (<see cref="NetworkConfig"/>).
    /// </summary>
    public class NetworkPropertyDialog
    {
        /// <summary>
        /// Callback to be called when this dialog closes.
        /// </summary>
        public delegate void OnClosed();

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="node">the network configuration to be manipulated by this dialog</param>
        /// <param name="callBack">delegate to be called when this dialog is closed</param>
        public NetworkPropertyDialog(Net.Network networkConfig, OnClosed callBack = null)
        {
            this.networkConfig = networkConfig;
            this.callBack = callBack;
        }

        /// <summary>
        /// Delegate to be called when this dialog is closed. May be null in which case
        /// nothing is called.
        /// </summary>
        private OnClosed callBack;

        /// <summary>
        /// The maximal valid network port number.
        /// </summary>
        private const int MaximalPortNumber = 65353;

        /// <summary>
        /// The network configuration to be manipulated by this dialog.
        /// </summary>
        private Net.Network networkConfig;

        /// <summary>
        /// Event triggered when the user presses the OK button. Clients can
        /// register on this event to receive a notification when this happens.
        /// </summary>
        public readonly UnityEvent OnConfirm = new UnityEvent();
        /// <summary>
        /// Event triggered when the user presses the Cancel button. Clients can
        /// register on this event to receive a notification when this happens.
        /// </summary>
        public readonly UnityEvent OnCancel = new UnityEvent();

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
        /// The server port for SEE's action traffic.
        /// </summary>
        private StringProperty serverActionPort;

        /// <summary>
        /// The selector for the voice chat system.
        /// </summary>
        private SelectionProperty voiceChatSelector;

        /// <summary>
        /// Creates and opens the dialog.
        /// </summary>
        public void Open()
        {
            networkConfig.Load();

            dialog = new GameObject("Network settings");

            // Group for network properties (one group for all).
            PropertyGroup group = dialog.AddComponent<PropertyGroup>();
            group.Name = "Network settings";

            {
                ipAddress = dialog.AddComponent<StringProperty>();
                ipAddress.Name = "IP4 Address";
                ipAddress.Value = networkConfig.ServerIP4Address;
                ipAddress.Description = "IP4 Address of the server";
                group.AddProperty(ipAddress);
            }
            {
                serverPort = dialog.AddComponent<StringProperty>();
                serverPort.Name = "Server Port";
                serverPort.Value = networkConfig.ServerPort.ToString();
                serverPort.Description = "Server port for NetCode and Voice Chat";
                group.AddProperty(serverPort);
            }
            {
                serverActionPort = dialog.AddComponent<StringProperty>();
                serverActionPort.Name = "Server Action Port";
                serverActionPort.Value = networkConfig.ServerActionPort.ToString();
                serverActionPort.Description = "Server port for SEE actions";
                group.AddProperty(serverActionPort);
            }
            {
                voiceChatSelector = dialog.AddComponent<SelectionProperty>();
                voiceChatSelector.Name = "Voice Chat";
                voiceChatSelector.Description = "Select a voice chat system";
                IList<string> voiceChats = VoiceChatSystemsToStrings();
                voiceChatSelector.AddOptions(voiceChats);
                voiceChatSelector.Value = networkConfig.VoiceChat.ToString();
                group.AddProperty(voiceChatSelector);
            }
            // Dialog
            PropertyDialog propertyDialog = dialog.AddComponent<PropertyDialog>();
            propertyDialog.Title = "Network settings";
            propertyDialog.Description = "Enter the network settings";
            propertyDialog.AddGroup(group);

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
        private IList<string> VoiceChatSystemsToStrings()
        {
            return Enum.GetNames(typeof(VoiceChatSystems)).ToList();
        }

        /// <summary>
        /// Notifies all listeners on <see cref="OnCancel"/> and closes the dialog.
        /// </summary>
        private void CancelButtonPressed()
        {
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
            {
                // Server IP Address
                string ipAddressValue = ipAddress.Value.Trim();
                if (!HasCorrectIPAddressSyntax(ipAddressValue))
                {
                    ShowNotification.Error("IP Syntax Error",
                        "IP addresses must have syntax number.number.number.number where number is a value in between 0 and 255.");
                }
                networkConfig.ServerIP4Address = ipAddressValue;
            }
            {
                // Server Port Number
                if (Int32.TryParse(serverPort.Value.Trim(), out int serverPortNumber)
                    && 0 <= serverPortNumber && serverPortNumber <= MaximalPortNumber)
                {
                    networkConfig.ServerPort = serverPortNumber;
                }
                else
                {
                    Debug.LogError($"{serverPort.Value.Trim()} {serverPortNumber}  {0 <= serverPortNumber}  {serverPortNumber <= MaximalPortNumber}\n");
                    ShowPortError("Server");
                }
            }
            {
                // Server Action Port Number
                if (Int32.TryParse(serverActionPort.Value.Trim(), out int serverActionPortNumber)
                    && 0 <= serverActionPortNumber && serverActionPortNumber <= MaximalPortNumber)
                {
                    networkConfig.ServerActionPort = serverActionPortNumber;
                }
                else
                {
                    ShowPortError("Server Action");
                }
            }
            {
                // Voice Chat
                string value = voiceChatSelector.Value.Trim();
                if (Enum.TryParse(value, out VoiceChatSystems voiceChat))
                {
                    networkConfig.VoiceChat = voiceChat;
                }
                else
                {
                    Debug.LogError($"Invalid value for {typeof(VoiceChatSystems)}: {value}.\n");
                    ShowNotification.Error("Invalid Voice Chat", "Your choice is not available");
                }
            }

            OnConfirm.Invoke();
            SEEInput.KeyboardShortcutsEnabled = true;
            Close();
            callBack?.Invoke();
            networkConfig.Save();

            static void ShowPortError(string portPrefix)
            {
                ShowNotification.Error(portPrefix + " Port Error", $"A port must be an integer in the range of 0 to {MaximalPortNumber}.");
            }
        }

        /// <summary>
        /// True if <paramref name="ipAddress"/> conforms to the syntax of numeric IP
        /// addresses, i.e., number.number.number.number where number is an integer in
        /// the range of 0 to 255.
        /// </summary>
        /// <param name="ipAddress">the IP address to be validated syntactically</param>
        /// <returns>true if <paramref name="ipAddress"/> conforms to the syntax</returns>
        private bool HasCorrectIPAddressSyntax(string ipAddress)
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
            UnityEngine.Object.Destroy(dialog);
            dialog = null;
        }
    }
}
