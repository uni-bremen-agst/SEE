using System;
using UnityEngine;
using SEE.Audio;
using SEE.UI.Notification;
using SEE.UI.PropertyDialog;
using SEE.UserSettings;

/// <summary>
/// User interface components for SEE.
/// </summary>
namespace SEE.UI
{
    /// <summary>
    /// Connects the Penpot start menu to SEE's existing network and user-settings services.
    /// </summary>
    /// <remarks>
    /// Attached to the existing SEEStart scene. The menu is constructed at startup and remains
    /// a desktop interface even when the selected player will use VR after connecting.
    /// </remarks>
    internal class OpeningDialog : MonoBehaviour
    {
        /// <summary>
        /// The view created for this startup dialog.
        /// </summary>
        private OpeningMenuView menu;
        /// <summary>
        /// Whether a network startup is pending and duplicate requests must be ignored.
        /// </summary>
        private bool starting;
        /// <summary>
        /// Disables this dialog if the scene has no user settings.
        /// </summary>
        private void Awake()
        {
            if (UserSetting.Instance == null)
            {
                Debug.LogWarning($"{nameof(OpeningDialog)} requires a {nameof(UserSetting)} component.\n");
                enabled = false;
            }
        }

        /// <summary>
        /// Restores the configuration and constructs the startup menu.
        /// </summary>
        private void Start()
        {
            UserSetting.Instance.Load();
            RestoreDisplaySettings();
            PlayerInputType inputType = UserSetting.Instance.InputType;
            UserSetting.Instance.InputType = PlayerInputType.DesktopPlayer;
            menu = gameObject.AddComponent<OpeningMenuView>();
            menu.Initialize(UserSetting.Instance, inputType, OpeningMenuConnection.FromSettings(UserSetting.Instance), StartNetwork, OpenPlayerSettings, AudioManagerImpl.Instance());
        }

        /// <summary>
        /// Restores the display preferences previously changed in the menu.
        /// </summary>
        private static void RestoreDisplaySettings()
        {
            if (PlayerPrefs.HasKey("SEE.DisplayWidth") && PlayerPrefs.HasKey("SEE.DisplayHeight"))
            {
                int width = PlayerPrefs.GetInt("SEE.DisplayWidth");
                int height = PlayerPrefs.GetInt("SEE.DisplayHeight");
                FullScreenMode mode = (FullScreenMode)PlayerPrefs.GetInt("SEE.DisplayMode", (int)Screen.fullScreenMode);
                if (width > 0 && height > 0 && Enum.IsDefined(typeof(FullScreenMode), mode))
                {
                    Screen.SetResolution(width, height, mode);
                }
            }
        }

        /// <summary>
        /// Validates and saves the draft, then starts the selected network mode once.
        /// </summary>
        /// <param name="host">Whether to start a host rather than connect as a client.</param>
        private void StartNetwork(bool host)
        {
            if (starting || !menu.TryApplyConnection())
            {
                return;
            }

            starting = true;
            try
            {
                UserSetting.Instance.InputType = menu.SelectedInputType;
                UserSetting.Instance.Save();
                menu.ShowLoading(host);
                if (host)
                {
                    UserSetting.Instance.Network.StartHost(NetworkCallBack);
                }
                else
                {
                    UserSetting.Instance.Network.StartClient(NetworkCallBack);
                }
            }
            catch (Exception exception)
            {
                NetworkCallBack(false, exception.Message);
            }
        }

        /// <summary>
        /// Hides the menu after success or restores the form after a startup failure.
        /// </summary>
        /// <param name="success">Whether the network started successfully.</param>
        /// <param name="message">The network's result message.</param>
        private void NetworkCallBack(bool success, string message)
        {
            starting = false;
            if (menu == null)
            {
                return;
            }

            if (success)
            {
                menu.SetVisible(false);
            }
            else
            {
                UserSetting.Instance.InputType = PlayerInputType.DesktopPlayer;
                menu.ShowConnection();
                menu.ShowError(message);
                ShowNotification.Error("Network problem", message, log: false);
            }
        }

        /// <summary>
        /// Opens SEE's existing player, avatar and voice-chat settings and refreshes the form on return.
        /// </summary>
        private void OpenPlayerSettings()
        {
            menu.SaveSettings();
            menu.SetVisible(false);
            UserSettingsDialog dialog = new(UserSetting.Instance.Network, () =>
            {
                if (menu != null)
                {
                    menu.RefreshConnection(OpeningMenuConnection.FromSettings(UserSetting.Instance));
                    menu.SetVisible(true);
                }
            });
            dialog.Open();
        }
    }
}
