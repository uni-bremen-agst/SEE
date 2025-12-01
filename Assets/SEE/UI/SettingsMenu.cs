using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Michsky.UI.ModernUIPack;
using SEE.Audio;
using SEE.Controls;
using SEE.Controls.KeyActions;
using SEE.Game;
using SEE.GO;
using SEE.Net.Actions;
using SEE.Tools.Livekit;
using SEE.UI.Menu;
using SEE.UI.Notification;
using SEE.Utils;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.UI
{
    /// <summary>
    /// Handles the user interactions with the settings menu.
    /// </summary>
    public class SettingsMenu : PlatformDependentComponent
    {
        /// <summary>
        /// Prefab for the <see cref="SettingsMenu"/>.
        /// </summary>
        private const string settingsPrefab = UIPrefabFolder + "SettingsMenu";

        /// <summary>
        /// Prefab for the keyBindingContent.
        /// </summary>
        private const string keyBindingContent = UIPrefabFolder + "KeyBindingContent";

        /// <summary>
        /// Prefab for the ScrollView.
        /// </summary>
        private const string scrollPrefab = UIPrefabFolder + "ScrollPrefab";

        /// <summary>
        /// The game object instantiated for the <see cref="settingsPrefab"/>.
        /// </summary>
        private GameObject settingsMenuGameObject;

        #region Key-Binding
        /// <summary>
        /// A mapping of the short name of the key binding onto the label of the button that allows to
        /// change the binding. This dictionary is used to update the label if the key binding
        /// was changed by the user.
        /// </summary>
        private readonly Dictionary<string, TextMeshProUGUI> shortNameOfBindingToLabel = new();

        /// <summary>
        /// The key binding that gets updated.
        /// </summary>
        private KeyActionDescriptor bindingToRebind;

        /// <summary>
        /// The notification telling the user to press a key to rebind the key binding.
        /// </summary>
        private Notification.Notification bindingNotification;
        #endregion

        #region Audio components
        /// <summary>
        /// The cached audio manager instance.
        /// </summary>
        private AudioManagerImpl audioManager;

        /// <summary>
        /// The toggle that allows to mute the music.
        /// </summary>
        private Toggle musicToggle;

        /// <summary>
        /// The toggle that allows to mute the local sound effects.
        /// </summary>
        private Toggle sfxToggle;

        /// <summary>
        /// The toggle that allows to mute the remote sound effects.
        /// </summary>
        private Toggle remoteSfxToggle;

        /// <summary>
        /// The slider that allows to change the sound effect volume.
        /// </summary>
        private Slider sfxVolumeSlider;

        /// <summary>
        /// The slider that allows to change the music volume.
        /// </summary>
        private Slider musicVolumeSlider;
        #endregion

        #region LiveKit components
        /// <summary>
        /// The input field of the LiveKit URL.
        /// </summary>
        TMP_InputField liveKitURLInputField;

        /// <summary>
        /// The input field of the Token URL.
        /// </summary>
        TMP_InputField tokenURLInputField;

        /// <summary>
        /// The input field of the room name.
        /// </summary>
        TMP_InputField roomNameInputField;
        #endregion

        /// <summary>
        /// Sets the <see cref="keyBindingContent"/> and adds the onClick event
        /// <see cref="ExitGame"/> to the ExitButton.
        /// </summary>
        protected override void StartDesktop()
        {
            KeyBindings.LoadKeyBindings();
            // instantiates the SettingsMenu
            settingsMenuGameObject = PrefabInstantiator.InstantiatePrefab(settingsPrefab, Canvas.transform, false);
            // adds the ExitGame method to the exit button
            settingsMenuGameObject.transform.Find("ExitPanel/Buttons/Content/Exit").gameObject.MustGetComponent<Button>()
                                  .onClick.AddListener(ExitGame);

            InitializeVideoSettings();
            InitializeAudioSettings();
            InitializeKeyBindings();
        }

        /// <summary>
        /// Toggles the settings panel with the Pause button and handles
        /// the case that the user wants to change the key of a keyBinding.
        /// </summary>
        protected override void UpdateDesktop()
        {
            // If the buttonToRebind is not null, the user clicked a button to start the rebinding.
            if (bindingToRebind != null)
            {
                SEEInput.KeyboardShortcutsEnabled = false;
                // the next button that gets pressed will be the new keyBind.
                if (Input.anyKeyDown)
                {
                    KeyCode newKey = Enum.GetValues(typeof(KeyCode)).Cast<KeyCode>()
                                         .FirstOrDefault(key => Input.GetKeyDown(key) && KeyBindings.AssignableKeyCode(key));
                    if (newKey != KeyCode.None)
                    {
                        ReassignKeyAsync(bindingToRebind, newKey).Forget();
                    }
                }
            }
            if (SEEInput.ToggleSettings())
            {
                GameObject settingsPanel = settingsMenuGameObject.transform.Find("SettingsPanel").gameObject;
                GameObject keybindingsPanel = settingsMenuGameObject.transform.Find("KeybindingsPanel").gameObject;
                GameObject audioSettingsPanel = settingsMenuGameObject.transform.Find("AudioSettingsPanel").gameObject;
                GameObject videoPanel = settingsMenuGameObject.transform.Find("VideoPanel").gameObject;
                GameObject exitPanel = settingsMenuGameObject.transform.Find("ExitPanel").gameObject;

                // Hide specific setting panels if they are active
                if (keybindingsPanel.activeSelf)
                {
                    keybindingsPanel.SetActive(false);
                }
                else if (audioSettingsPanel.activeSelf)
                {
                    audioSettingsPanel.SetActive(false);
                }
                else if (videoPanel.activeSelf)
                {
                    videoPanel.SetActive(false);
                }
                else if (exitPanel.activeSelf)
                {
                    exitPanel.SetActive(false);
                }
                // Toggle main settings panel
                else
                {
                    settingsPanel.SetActive(!settingsPanel.activeSelf);
                }
            }
        }

        /// <summary>
        /// Reassigns the binding of the given <paramref name="descriptor"/> to the new key <paramref name="newKey"/>
        /// after confirming the action with the user.
        /// </summary>
        /// <param name="descriptor">The key binding to reassign.</param>
        /// <param name="newKey">The new key code to assign to the key binding.</param>
        private async UniTaskVoid ReassignKeyAsync(KeyActionDescriptor descriptor, KeyCode newKey)
        {
            bindingToRebind = null;
            SEEInput.KeyboardShortcutsEnabled = true;
            bindingNotification.Close();
            string question = $"Do you really want to reassign action \"{descriptor.Name}\" to key {newKey}?";
            if (!await ConfirmDialog.ConfirmAsync(new(question, Title: "Change Key?")))
            {
                return;
            }
            try
            {
                KeyBindings.SetBindingForKey(descriptor, newKey);
                shortNameOfBindingToLabel[descriptor.Name].text = newKey.ToString();
            }
            catch (KeyMap.KeyBindingsExistsException ex)
            {
                ShowNotification.Error("Key code already bound", ex.Message, log: false);
            }
        }

        /// <summary>
        /// Sets the <see cref="bindingToRebind"/>.
        /// </summary>
        private void StartRebindFor(KeyActionDescriptor binding)
        {
            bindingNotification = ShowNotification.Info("Bind action to key", "Press a key to bind this action.");
            bindingToRebind = binding;
        }

        /// <summary>
        /// Terminates the application (exits the game).
        /// </summary>
        private static void ExitGame()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>
        /// Initializes the video settings section of the settings menu, specifically the camera selection dropdown.
        /// This method populates the dropdown with all available webcam devices, restores the previously
        /// selected camera from <see cref="PlayerPrefs"/>, and connects the dropdown's selection event
        /// to the <see cref="WebcamManager.SwitchCamera(int)"/> handler,
        /// and invokes <see cref="InitializeLiveKitSettings"/> to configure LiveKit-related options.
        /// </summary>
        /// <remarks>
        /// - If no webcam devices are found, the dropdown remains uninitialized.
        /// - The first available device is used as a fallback if no previous selection is stored.
        /// - The dropdown is expected to be a child object named "CameraDropdown" within
        ///   <see cref="settingsMenuGameObject"/>.
        /// - The currently selected camera is immediately applied to <see cref="WebcamManager"/>.
        /// - LiveKit settings are initialized afterwards to ensure they are bound to the video settings page.
        /// </remarks>

        private void InitializeVideoSettings()
        {
            Dropdown cameraDropdown = settingsMenuGameObject.FindDescendant("CameraDropdown").MustGetComponent<Dropdown>();
            // Load available cameras and populate the dropdown.
            WebCamDevice[] devices = WebCamTexture.devices;

            if (devices.Length > 0)
            {
                cameraDropdown.options.Clear();
                foreach (WebCamDevice device in devices)
                {
                    cameraDropdown.options.Add(new Dropdown.OptionData(string.IsNullOrEmpty(device.name) ? "Unnamed Camera" : device.name));
                }

                cameraDropdown.value = WebcamManager.ActiveIndex;

                // Add a listener for dropdown changes.
                cameraDropdown.onValueChanged.AddListener(WebcamManager.SwitchCamera);
            }

            // Initialize LiveKit settings.
            InitializeLiveKitSettings();
        }

        /// <summary>
        ///
        /// </summary>
        private void InitializeLiveKitSettings()
        {
            // InputFields of the LiveKit settings.
            liveKitURLInputField = settingsMenuGameObject.FindDescendant(PlayerPrefsKeys.LiveKitURL)
                .GetComponentInChildren<TMP_InputField>();
            tokenURLInputField = settingsMenuGameObject.FindDescendant(PlayerPrefsKeys.TokenURL)
                .GetComponentInChildren<TMP_InputField>();
            roomNameInputField = settingsMenuGameObject.FindDescendant(PlayerPrefsKeys.RoomName)
                .GetComponentInChildren<TMP_InputField>();

            // Buttons and importent GameObjects of the LiveKit settings page.
            ButtonManagerWithIcon share = settingsMenuGameObject.FindDescendant("Share")
                .MustGetComponent<ButtonManagerWithIcon>();
            GameObject connectGO = settingsMenuGameObject.FindDescendant("Connect");
            ButtonManagerWithIcon connect = connectGO.MustGetComponent<ButtonManagerWithIcon>();
            GameObject disconnectGO = settingsMenuGameObject.FindDescendant("Disconnect");
            ButtonManagerWithIcon disconnect = disconnectGO.MustGetComponent<ButtonManagerWithIcon>();
            disconnectGO.SetActive(false);

            if (LocalPlayer.TryGetLiveKitVideoManager(out LiveKitVideoManager liveKitVideoManager))
            {
                UpdateLiveKitSettings();

                // Deactivates the shortcuts while typing.
                DisableShortcutsWhileTyping(liveKitURLInputField);
                DisableShortcutsWhileTyping(tokenURLInputField);
                DisableShortcutsWhileTyping(roomNameInputField);

                // Bind input fields to depending attributes
                liveKitURLInputField.onValueChanged.AddListener(input =>
                {
                    liveKitVideoManager.LiveKitUrl = input;
                    SaveToPlayerPrefs(PlayerPrefsKeys.LiveKitURL);
                });

                tokenURLInputField.onValueChanged.AddListener(input =>
                {
                    liveKitVideoManager.TokenUrl = input;
                    SaveToPlayerPrefs(PlayerPrefsKeys.TokenURL);
                });

                roomNameInputField.onValueChanged.AddListener(input =>
                {
                    liveKitVideoManager.RoomName = input;
                    SaveToPlayerPrefs(PlayerPrefsKeys.RoomName);
                });

                share.clickEvent.AddListener(() =>
                {
                    new LiveKitSettingsNetAction(liveKitVideoManager.LiveKitUrl,
                                                 liveKitVideoManager.TokenUrl,
                                                 liveKitVideoManager.RoomName).Execute();
                });

                connect.clickEvent.AddListener(() =>
                {
                    // TODO
                });

                disconnect.clickEvent.AddListener(() =>
                {
                    // TODO
                });
            }

            void SaveToPlayerPrefs(string keyword)
            {
                switch (keyword)
                {
                    case PlayerPrefsKeys.LiveKitURL:
                        PlayerPrefs.SetString(PlayerPrefsKeys.LiveKitURL, liveKitVideoManager.LiveKitUrl);
                        break;
                    case PlayerPrefsKeys.TokenURL:
                        PlayerPrefs.SetString(PlayerPrefsKeys.TokenURL, liveKitVideoManager.TokenUrl);
                        break;
                    case PlayerPrefsKeys.RoomName:
                        PlayerPrefs.SetString(PlayerPrefsKeys.RoomName, liveKitVideoManager.RoomName);
                        break;
                }
            }

            static void DisableShortcutsWhileTyping(TMP_InputField inputField)
            {
                inputField.onSelect.AddListener(input => SEEInput.KeyboardShortcutsEnabled = false);
                inputField.onDeselect.AddListener(input => SEEInput.KeyboardShortcutsEnabled = true);
            }
        }

        /// <summary>
        /// Updates the UI input fields with the current LiveKit configuration
        /// retrieved from the local player's <see cref="LiveKitVideoManager"/>.
        /// If available, the LiveKit URL, token URL, and room name are copied
        /// into their corresponding input fields.
        /// </summary>
        public void UpdateLiveKitSettings()
        {
            if (LocalPlayer.TryGetLiveKitVideoManager(out LiveKitVideoManager manager))
            {
                liveKitURLInputField.text = manager.LiveKitUrl;
                tokenURLInputField.text = manager.TokenUrl;
                roomNameInputField.text = manager.RoomName;
            }
        }

        /// <summary>
        /// Initializes the audio settings section of the settings menu.
        /// This method wires up the UI controls (toggles and sliders) for music,
        /// sound effects, and remote sound effects, synchronizes them with the
        /// current <see cref="AudioManagerImpl"/> state, and registers listeners
        /// to update audio settings when the user interacts with the controls.
        /// </summary>
        /// <remarks>
        /// - The method expects child objects named "MusicToggle", "MusicVolumeSlider",
        ///   "SFXToggle", "SFXVolumeSlider", and "RemoteSFXToggle" within
        ///   <see cref="settingsMenuGameObject"/>.
        /// - Initial values are loaded from <see cref="AudioManagerImpl"/>.
        /// - Toggles control muting/unmuting, while sliders adjust volume levels.
        /// - Interactivity of sliders and toggles is updated dynamically based on mute state.
        /// </remarks>

        private void InitializeAudioSettings()
        {
            musicToggle = settingsMenuGameObject.FindDescendant("MusicToggle").MustGetComponent<Toggle>();
            musicVolumeSlider = settingsMenuGameObject.FindDescendant("MusicVolumeSlider").MustGetComponent<Slider>();
            sfxToggle = settingsMenuGameObject.FindDescendant("SFXToggle").MustGetComponent<Toggle>();
            sfxVolumeSlider = settingsMenuGameObject.FindDescendant("SFXVolumeSlider").MustGetComponent<Slider>();
            remoteSfxToggle = settingsMenuGameObject.FindDescendant("RemoteSFXToggle").MustGetComponent<Toggle>();

            audioManager = AudioManagerImpl.Instance();
            musicVolumeSlider.value = audioManager.MusicVolume;
            musicVolumeSlider.interactable = !audioManager.MusicMuted;
            musicToggle.isOn = !audioManager.MusicMuted;
            sfxVolumeSlider.value = audioManager.SoundEffectsVolume;
            sfxVolumeSlider.interactable = !audioManager.SoundEffectsMuted;
            sfxToggle.isOn = !audioManager.SoundEffectsMuted;
            remoteSfxToggle.isOn = !audioManager.RemoteSoundEffectsMuted;
            remoteSfxToggle.interactable = !audioManager.SoundEffectsMuted;

            sfxVolumeSlider.onValueChanged.AddListener((value) =>
            {
                audioManager.SoundEffectsVolume = value;
            });

            musicVolumeSlider.onValueChanged.AddListener((value) =>
            {
                audioManager.MusicVolume = value;
            });

            musicToggle.onValueChanged.AddListener((value) =>
            {
                audioManager.MusicMuted = !value;
                musicVolumeSlider.interactable = value;
            });

            sfxToggle.onValueChanged.AddListener((value) =>
            {
                audioManager.SoundEffectsMuted = !value;
                sfxVolumeSlider.interactable = value;
                remoteSfxToggle.interactable = value;
            });

            remoteSfxToggle.onValueChanged.AddListener((value) =>
            {
                audioManager.RemoteSoundEffectsMuted = !value;
            });
        }

        /// <summary>
        /// Initializes the key bindings section of the settings menu.
        /// This method creates UI elements for each binding group, displays
        /// their associated actions and current key codes, and registers
        /// listeners to allow rebinding of keys at runtime.
        /// </summary>
        /// <remarks>
        /// - Key bindings are retrieved from <see cref="KeyBindings.AllBindings"/>.
        /// - Each binding group is displayed in a scroll view with a group title.
        /// - For each binding, a content element is instantiated showing the
        ///   action name and the bound key.
        /// - Clicking the key button triggers <see cref="StartRebindFor"/> to
        ///   allow the user to change the binding.
        /// - The method expects prefabs for scroll views and key binding content
        ///   to be available via <see cref="PrefabInstantiator"/>.
        /// </remarks>
        private void InitializeKeyBindings()
        {
            // Displays all bindings grouped by their category.
            foreach (var group in KeyBindings.AllBindings())
            {
                // Creates a list of keybinding descriptions for the given category.
                GameObject scrollView = PrefabInstantiator
                    .InstantiatePrefab(scrollPrefab, Canvas.transform, false)
                    .transform.gameObject;

                scrollView.transform.SetParent(
                    settingsMenuGameObject.transform.Find("KeybindingsPanel/KeybindingsText/Viewport/Content"));

                // set the titles of the scrollViews to the scopes
                TextMeshProUGUI groupTitle = scrollView.transform
                    .Find("Group").gameObject.MustGetComponent<TextMeshProUGUI>();

                groupTitle.text = $"{group.Key}";

                foreach ((_, KeyActionDescriptor descriptor) in group)
                {
                    GameObject keyBindingContent = PrefabInstantiator
                        .InstantiatePrefab(SettingsMenu.keyBindingContent, Canvas.transform, false)
                        .transform.gameObject;

                    keyBindingContent.transform.SetParent(scrollView.transform.Find("Scroll View/Viewport/Content"));

                    // set the text to the short name of the binding
                    TextMeshProUGUI bindingText = keyBindingContent.transform
                        .Find("Binding").gameObject.MustGetComponent<TextMeshProUGUI>();

                    // The short name of the binding.
                    bindingText.text = descriptor.Name;

                    // set the label of the key button
                    TextMeshProUGUI key = keyBindingContent.transform
                        .Find("Key/Text (TMP)").gameObject.MustGetComponent<TextMeshProUGUI>();

                    // The name of the key code bound.
                    key.text = descriptor.KeyCode.ToString();
                    shortNameOfBindingToLabel[descriptor.Name] = key;

                    // add the actionlistener to be able to change the key code of a binding.
                    keyBindingContent.transform.Find("Key").gameObject.MustGetComponent<Button>()
                        .onClick.AddListener(() => StartRebindFor(descriptor));
                }
            }
        }
    }
}
