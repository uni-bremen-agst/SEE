using Michsky.MUIP;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;
using SEE.Audio;
using SEE.Controls.KeyActions;
using SEE.Tools.OpenTelemetry;
using SEE.UserSettings;
using SEE.Utils;

/// <summary>
/// User interface components for SEE.
/// </summary>
namespace SEE.UI
{
    /// <summary>
    /// Canvas-based prototype of the SEE Main Menu Penpot design. The hierarchy is
    /// created locally for this screen so shared in-game UI prefabs keep their style.
    /// </summary>
    internal class OpeningMenuView : MonoBehaviour
    {
        /// <summary>
        /// The blue accent used for the selected tab and primary action.
        /// </summary>
        private static readonly Color accent = new(0.02f, 0.56f, 0.77f, 1);
        /// <summary>
        /// The background color of an unselected tab or switch.
        /// </summary>
        private static readonly Color tabColor = new(0.07f, 0.25f, 0.25f, 0.95f);
        /// <summary>
        /// The translucent background color of the main panels.
        /// </summary>
        private static readonly Color panelColor = new(0.055f, 0.06f, 0.065f, 0.86f);
        /// <summary>
        /// The background color of editable fields and secondary buttons.
        /// </summary>
        private static readonly Color fieldColor = new(0.04f, 0.045f, 0.05f, 0.7f);
        /// <summary>
        /// The color used for secondary graphics and disabled controls.
        /// </summary>
        private static readonly Color muted = new(0.7f, 0.75f, 0.77f, 1);
        /// <summary>
        /// The canvas owned by this menu, created during initialization.
        /// </summary>
        private Canvas canvas;
        /// <summary>
        /// The non-null settings instance edited by this view after initialization.
        /// </summary>
        private UserSetting userSettings;
        /// <summary>
        /// The optional audio manager receiving immediate playback changes.
        /// </summary>
        private AudioManagerImpl audioManager;
        /// <summary>
        /// The screen containing the SEE wordmark and main actions.
        /// </summary>
        private RectTransform home;
        /// <summary>
        /// The screen containing the host and client connection form.
        /// </summary>
        private RectTransform connection;
        /// <summary>
        /// The screen containing the audio, video and keyboard tabs.
        /// </summary>
        private RectTransform settings;
        /// <summary>
        /// The screen shown while network startup is pending.
        /// </summary>
        private RectTransform loading;
        /// <summary>
        /// The scrollable audio preferences page.
        /// </summary>
        private RectTransform audioPage;
        /// <summary>
        /// The scrollable video preferences page.
        /// </summary>
        private RectTransform videoPage;
        /// <summary>
        /// The keyboard categories and editable binding list.
        /// </summary>
        private RectTransform keysPage;
        /// <summary>
        /// The content transform containing all categories of bindings.
        /// </summary>
        private RectTransform keyRows;
        /// <summary>
        /// The scroll view containing the editable key bindings.
        /// </summary>
        private ScrollRect keyScroll;
        /// <summary>
        /// The independently scrollable category navigation.
        /// </summary>
        private ScrollRect keyCategoryScroll;
        /// <summary>
        /// The section anchors in the same order as the category navigation.
        /// </summary>
        private readonly Dictionary<KeyActionCategory, RectTransform> keySections = new();
        /// <summary>
        /// The category currently highlighted in the navigation, or null before initialization.
        /// </summary>
        private KeyActionCategory? activeKeyCategory;
        /// <summary>
        /// The label displaying validation and network startup errors.
        /// </summary>
        private TextMeshProUGUI error;
        /// <summary>
        /// The MUIP action button for the selected network mode.
        /// </summary>
        private ButtonManager startButton;
        /// <summary>
        /// The label describing the pending network operation.
        /// </summary>
        private TextMeshProUGUI loadingLabel;
        /// <summary>
        /// The instruction displayed below the selected settings page.
        /// </summary>
        private TextMeshProUGUI settingsHint;
        /// <summary>
        /// The MUIP button selecting host mode.
        /// </summary>
        private ButtonManager hostButton;
        /// <summary>
        /// The MUIP button selecting client mode.
        /// </summary>
        private ButtonManager joinButton;
        /// <summary>
        /// The MUIP buttons selecting audio, video and keybindings.
        /// </summary>
        private ButtonManager[] settingsTabs;
        /// <summary>
        /// The MUIP buttons selecting keyboard action categories.
        /// </summary>
        private readonly Dictionary<KeyActionCategory, ButtonManager> categoryButtons = new();
        /// <summary>
        /// The field holding the draft IPv4 address.
        /// </summary>
        private TMP_InputField serverIP;
        /// <summary>
        /// The field holding the draft UDP port.
        /// </summary>
        private TMP_InputField port;
        /// <summary>
        /// The masked field holding the draft room password.
        /// </summary>
        private TMP_InputField password;
        /// <summary>
        /// The field holding the draft backend API URL.
        /// </summary>
        private TMP_InputField backend;
        /// <summary>
        /// The field holding the draft remote telemetry endpoint.
        /// </summary>
        private TMP_InputField telemetryURL;
        /// <summary>
        /// The MUIP dropdown selecting local or remote telemetry.
        /// </summary>
        private CustomDropdown telemetryMode;
        /// <summary>
        /// The MUIP switch enabling or disabling telemetry.
        /// </summary>
        private SwitchManager telemetryEnabled;
        /// <summary>
        /// The callback requesting a host or client startup after user selection.
        /// </summary>
        private Action<bool> startNetwork;
        /// <summary>
        /// Whether the connection form currently selects host mode.
        /// </summary>
        private bool host = true;
        /// <summary>
        /// Whether an audio or video preference needs to be saved.
        /// </summary>
        private bool dirty;
        /// <summary>
        /// The keyboard shortcut state restored when the menu releases input.
        /// </summary>
        private bool previousShortcuts;
        /// <summary>
        /// Whether this view currently owns keyboard shortcut suppression.
        /// </summary>
        private bool shortcutsCaptured;
        /// <summary>
        /// The action awaiting a new key, or null when no rebinding is pending.
        /// </summary>
        private KeyActionDescriptor bindingToRebind;
        /// <summary>
        /// The MUIP button displaying feedback for the pending key binding.
        /// </summary>
        private ButtonManager bindingButton;
        /// <summary>
        /// Whether the initiating pointer event must be skipped before reading a new key.
        /// </summary>
        private bool skipRebindFrame;
        /// <summary>
        /// The existing rounded fill sprite used by panel and text-field graphics.
        /// </summary>
        private Sprite rounded;
        /// <summary>
        /// The existing rounded outline sprite used by text-field borders.
        /// </summary>
        private Sprite roundedOutline;
        /// <summary>
        /// The existing TextMesh Pro font used by the menu.
        /// </summary>
        private TMP_FontAsset font;
        /// <summary>
        /// The downsampled background texture owned and released by this view.
        /// </summary>
        private RenderTexture backdropTexture;
        /// <summary>
        /// The player input mode to apply after the connection form is confirmed.
        /// </summary>
        internal PlayerInputType SelectedInputType { get; private set; }

        /// <summary>
        /// Builds the menu once using the current user preferences and the supplied connection draft.
        /// </summary>
        /// <param name="configuration">The non-null settings instance edited by this view.</param>
        /// <param name="inputType">The player input mode to select initially.</param>
        /// <param name="connectionSettings">The non-null draft used to populate the connection form.</param>
        /// <param name="onStart">The non-null callback receiving true for a host or false for a client.</param>
        /// <param name="playerSettings">The non-null callback opening the existing player settings dialog.</param>
        /// <param name="audioManager">The optional manager for immediate audio playback updates; may be null.</param>
        /// <exception cref="ArgumentNullException">A required configuration, draft or callback is null.</exception>
        /// <exception cref="InvalidOperationException">The view is already initialized.</exception>
        /// <exception cref="Exception">A required SEE prefab cannot be instantiated.</exception>
        internal void Initialize(UserSetting configuration, PlayerInputType inputType, OpeningMenuConnection connectionSettings, Action<bool> onStart, Action playerSettings, AudioManagerImpl audioManager = null)
        {
            if (canvas != null)
            {
                throw new InvalidOperationException("The opening menu is already initialized.");
            }

            if (connectionSettings == null)
            {
                throw new ArgumentNullException(nameof(connectionSettings));
            }

            if (onStart == null)
            {
                throw new ArgumentNullException(nameof(onStart));
            }

            if (playerSettings == null)
            {
                throw new ArgumentNullException(nameof(playerSettings));
            }

            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            userSettings = configuration;
            this.audioManager = audioManager;
            SelectedInputType = inputType;
            startNetwork = onStart;
            rounded = Resources.Load<Sprite>("UI/OpeningMenu/Rounded");
            roundedOutline = Resources.Load<Sprite>("UI/OpeningMenu/Outline");
            font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF") ?? TMP_Settings.defaultFontAsset;
            GameObject root = new("SEE Main Menu", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            // SEEStart already supplies its camera, background and EventSystem.
            if (EventSystem.current == null)
            {
                new GameObject("Menu EventSystem", typeof(EventSystem), typeof(StandaloneInputModule)).transform.SetParent(transform, false);
            }

            BuildBackdrop(root.transform);
            RectTransform shade = Stretch("BackdropShade", root.transform);
            Image(shade, new Color(0, 0, 0, 0.3f));
            home = Stretch("Home", root.transform);
            connection = Screen("Connection", root.transform);
            settings = Screen("Settings", root.transform);
            loading = Screen("Loading", root.transform);
            BuildHome();
            BuildConnection(playerSettings, connectionSettings);
            KeyBindings.LoadKeyBindings();
            BuildSettings();
            BuildLoading();
            RefreshConnection(connectionSettings);
            ShowHome();
            CaptureShortcuts();
        }

        /// <summary>
        /// Builds the SEE wordmark and the Start, Settings and Exit actions.
        /// </summary>
        private void BuildHome()
        {
            RectTransform sidebar = Rect("Sidebar", home, 0, 0, 660, 1080);
            sidebar.anchorMin = Vector2.zero;
            sidebar.anchorMax = new Vector2(0, 1);
            sidebar.sizeDelta = new Vector2(660, 0);
            Image(sidebar, new Color(0.06f, 0.06f, 0.06f, 0.88f));
            string[] initials =
            {
                "S",
                "E",
                "E"
            };
            string[] words =
            {
                "O F T W A R E",
                "N G I N E E R I N G",
                "X P E R I E N C E"
            };
            for (int i = 0; i < 3; i++)
            {
                Label(sidebar, initials[i], 72, 94 + i * 92, 100, 105, 98, FontStyles.Bold).overflowMode = TextOverflowModes.Overflow;
                Label(sidebar, words[i], 162, 115 + i * 92, 470, 66, 37);
            }

            Button(sidebar, "Start", 92, 540, 470, 82, () => ShowConnection());
            Button(sidebar, "Settings", 92, 650, 470, 82, () => ShowSettings(0));
            Button(sidebar, "Exit", 92, 840, 470, 82, Exit);
        }

        /// <summary>
        /// Builds the network form with MUIP host, join and telemetry controls.
        /// </summary>
        /// <param name="playerSettings">The non-null callback opening the existing player settings dialog.</param>
        /// <param name="connectionSettings">The non-null draft used to populate the connection form.</param>
        private void BuildConnection(Action playerSettings, OpeningMenuConnection connectionSettings)
        {
            RectTransform panel = Rect("Panel", connection, 0, 0, 1600, 760);
            Image(panel, panelColor);
            hostButton = Button(panel, "Host", 532, 28, 260, 58, () => SelectHost(true));
            joinButton = Button(panel, "Join", 808, 28, 260, 58, () => SelectHost(false));
            Label(panel, "Server IP", 64, 185, 320, 64);
            serverIP = Input(panel, "ServerIP", 400, 185, 560, "127.0.0.1");
            Label(panel, "Port", 64, 290, 320, 64);
            port = Input(panel, "Port", 400, 290, 560, "7777");
            port.contentType = TMP_InputField.ContentType.IntegerNumber;
            Label(panel, "Room Password", 64, 395, 320, 64);
            password = Input(panel, "RoomPassword", 400, 395, 560, "Password...");
            password.contentType = TMP_InputField.ContentType.Password;
            Label(panel, "Backend API", 64, 500, 320, 64);
            backend = Input(panel, "BackendAPI", 400, 500, 560, "http://localhost:8080/api/v1/");
            Image(Rect("Divider", panel, 995, 160, 2, 490), muted);
            Label(panel, "Options", 1030, 175, 480, 60, 42, FontStyles.Bold);
#if ENABLE_VR
            Switch(panel, "Enable VR", 1050, 290, 475,
                   SelectedInputType == PlayerInputType.VRPlayer,
                   value => SelectedInputType = value ? PlayerInputType.VRPlayer : PlayerInputType.DesktopPlayer);
#else
            SwitchManager vr = Switch(panel, "Enable VR (unavailable)", 1050, 290, 475, false, _ =>
            {
            });
            vr.switchButton.interactable = false;
#endif
            telemetryEnabled = Switch(panel, "Telemetry", 1050, 380, 475, connectionSettings.TelemetryMode != TelemetryMode.Disabled, enabled =>
            {
                telemetryMode.isInteractable = enabled;
                telemetryURL.gameObject.SetActive(enabled && telemetryMode.selectedItemIndex == 1);
            });
            telemetryMode = Dropdown(panel, "TelemetryMode", 1050, 465, 475, new[] { "Local", "Remote" }, connectionSettings.TelemetryMode == TelemetryMode.Remote ? 1 : 0, index => telemetryURL.gameObject.SetActive(telemetryEnabled.isOn && index == 1));
            telemetryURL = Input(panel, "TelemetryURL", 1050, 550, 475, "https://telemetry-server/v1/traces");
            telemetryURL.textComponent.fontSize = 23;
            Button(panel, "Player settings", 1050, 650, 475, 54, () => playerSettings(), false);
            Button(connection, "<  Back", 0, 810, 185, 66, ShowHome, false);
            startButton = Button(connection, "Start hosting", 540, 810, 520, 72, () => startNetwork(host), true);
            error = Label(connection, "", 225, 898, 1150, 54, 25);
            error.color = new Color(1, 0.62f, 0.55f);
            error.alignment = TextAlignmentOptions.Center;
        }

        /// <summary>
        /// Builds the settings tabs and their scrollable pages.
        /// </summary>
        private void BuildSettings()
        {
            RectTransform panel = Rect("Panel", settings, 0, 0, 1600, 760);
            Image(panel, panelColor);
            settingsTabs = new ButtonManager[3];
            string[] names =
            {
                "Audio",
                "Video",
                "Keybindings"
            };
            for (int i = 0; i < names.Length; i++)
            {
                int tabIndex = i;
                settingsTabs[i] = Button(panel, names[i], 400 + i * 270, 28, 260, 58, () => ShowSettings(tabIndex));
            }

            audioPage = Rect("Audio", panel, 64, 138, 1472, 570);
            videoPage = Rect("Video", panel, 64, 138, 1472, 570);
            keysPage = Rect("Keybindings", panel, 64, 138, 1472, 570);
            BuildAudio(ScrollContent(audioPage, 1472, 570, 680));
            BuildVideo(ScrollContent(videoPage, 1472, 570, 930));
            BuildKeys();
            Button(settings, "<  Back", 0, 810, 185, 66, () =>
            {
                SaveSettings();
                ShowHome();
            }, false);
            settingsHint = Label(settings, "Changes are saved when you go back.", 640, 818, 960, 52, 24);
            settingsHint.alignment = TextAlignmentOptions.Right;
        }

        /// <summary>
        /// Binds the audio controls to the current SEE audio preferences.
        /// </summary>
        /// <param name="content">The non-null parent of the controls.</param>
        private void BuildAudio(RectTransform content)
        {
            SEE.UserSettings.Audio audio = userSettings.Audio;
            Volume(content, "Sound Effects", 20, audio.SoundEffectsVolume, value =>
            {
                audio.SoundEffectsVolume = value;
                audio.SoundEffectsMuted = value <= 0;
                if (audioManager is { } manager)
                {
                    manager.SoundEffectsVolume = value;
                    manager.SoundEffectsMuted = audio.SoundEffectsMuted;
                }
            });
            // SEE stores remote SFX as a mute preference, not as a separate volume.
            Switch(content, "Remote SFX", 100, 120, 1240, !audio.RemoteSoundEffectsMuted, value =>
            {
                audio.RemoteSoundEffectsMuted = !value;
                if (audioManager is { } manager)
                {
                    manager.RemoteSoundEffectsMuted = !value;
                }

                dirty = true;
            });
            Volume(content, "Music", 220, audio.MusicMuted ? 0 : audio.MusicVolume, value =>
            {
                audio.MusicVolume = value;
                audio.MusicMuted = value <= 0;
                if (audioManager is { } manager)
                {
                    manager.MusicVolume = value;
                    manager.MusicMuted = audio.MusicMuted;
                }
            });
            string[] devices = UnityEngine.Microphone.devices;
            string[] microphones = new[]
            {
                "System default"
            }.Concat(devices).ToArray();
            Label(content, "Microphone", 100, 320, 700, 64);
            Dropdown(content, "Microphone", 900, 320, 440, microphones, Mathf.Max(0, Array.IndexOf(devices, audio.Microphone.MicrophoneDevice) + 1), index =>
            {
                audio.Microphone.MicrophoneDevice = index == 0 ? "" : devices[index - 1];
                dirty = true;
            });
            Switch(content, "Auto Gain Control", 100, 420, 1240, audio.Microphone.AutoGainControl, value =>
            {
                audio.Microphone.AutoGainControl = value;
                dirty = true;
            });
            Switch(content, "Noise Cancellation", 100, 510, 1240, audio.Microphone.NoiseSuppression, value =>
            {
                audio.Microphone.NoiseSuppression = value;
                dirty = true;
            });
            Switch(content, "Echo Cancellation", 100, 600, 1240, audio.Microphone.EchoCancellation, value =>
            {
                audio.Microphone.EchoCancellation = value;
                dirty = true;
            });
        }

        /// <summary>
        /// Binds the display, webcam and LiveKit controls to the current preferences.
        /// </summary>
        /// <param name="content">The non-null parent of the controls.</param>
        private void BuildVideo(RectTransform content)
        {
            Section(content, "General", 10, 1400);
            Label(content, "Display mode", 100, 95, 700, 64);
            FullScreenMode[] modes =
            {
                FullScreenMode.FullScreenWindow,
                FullScreenMode.Windowed,
                FullScreenMode.ExclusiveFullScreen
            };
            Dropdown(content, "DisplayMode", 860, 95, 480, new[] { "Fullscreen", "Windowed", "Exclusive fullscreen" }, Mathf.Max(0, Array.IndexOf(modes, UnityEngine.Screen.fullScreenMode)), index =>
            {
                UnityEngine.Screen.fullScreenMode = modes[index];
                PlayerPrefs.SetInt("SEE.DisplayMode", (int)modes[index]);
                dirty = true;
            });
            Vector2Int current = new(UnityEngine.Screen.width, UnityEngine.Screen.height);
            Vector2Int[] resolutions = UnityEngine.Screen.resolutions.Select(r => new Vector2Int(r.width, r.height)).Append(current).Distinct().OrderBy(r => r.x).ThenBy(r => r.y).ToArray();
            Label(content, "Resolution", 100, 195, 700, 64);
            Dropdown(content, "Resolution", 860, 195, 480, resolutions.Select(r => $"{r.x} x {r.y}").ToArray(), Array.IndexOf(resolutions, current), index =>
            {
                Vector2Int resolution = resolutions[index];
                UnityEngine.Screen.SetResolution(resolution.x, resolution.y, UnityEngine.Screen.fullScreenMode);
                PlayerPrefs.SetInt("SEE.DisplayWidth", resolution.x);
                PlayerPrefs.SetInt("SEE.DisplayHeight", resolution.y);
                dirty = true;
            });
            string[] cameras = WebCamTexture.devices.Select(d => d.name).ToArray();
            Label(content, "Select Camera", 100, 295, 700, 64);
            CustomDropdown camera = Dropdown(content, "Camera", 860, 295, 480, cameras.Length == 0 ? new[] { "No camera detected" } : cameras, Mathf.Max(0, Array.IndexOf(cameras, userSettings.Video.WebcamName)), index =>
            {
                userSettings.Video.WebcamName = cameras[index];
                dirty = true;
            });
            camera.isInteractable = cameras.Length > 0;
            Section(content, "LiveKit", 400, 1400);
            VideoField(content, "LiveKit URL", "LiveKitURL", 490, userSettings.Video.LiveKitUrl, value => userSettings.Video.LiveKitUrl = value);
            VideoField(content, "Token URL", "TokenURL", 590, userSettings.Video.TokenUrl, value => userSettings.Video.TokenUrl = value);
            VideoField(content, "Room name", "RoomName", 690, userSettings.Video.RoomName, value => userSettings.Video.RoomName = value);
        }

        /// <summary>
        /// Creates an editable video preference and updates its value after editing.
        /// </summary>
        /// <param name="content">The non-null parent of the controls.</param>
        /// <param name="label">The text displayed to the user.</param>
        /// <param name="name">The hierarchy name assigned to the created control.</param>
        /// <param name="y">The downward offset from the top of the parent, in reference pixels.</param>
        /// <param name="value">The initial value of the control.</param>
        /// <param name="changed">The non-null callback receiving subsequent user changes.</param>
        private void VideoField(RectTransform content, string label, string name, float y, string value, Action<string> changed)
        {
            Label(content, label, 100, y, 700, 64);
            TMP_InputField input = Input(content, name, 860, y, 480, "");
            input.SetTextWithoutNotify(value);
            input.onEndEdit.AddListener(text =>
            {
                changed(text.Trim());
                dirty = true;
            });
        }

        /// <summary>
        /// Builds the actual keyboard categories and the scrollable binding list.
        /// </summary>
        private void BuildKeys()
        {
            var groups = KeyBindings.AllBindings().ToArray();
            RectTransform categories = Rect("Categories", keysPage, 0, 0, 320, 570);
            RectTransform categoryContent = ScrollContent(categories, 320, 570, groups.Length * 58);
            keyCategoryScroll = categories.GetComponent<ScrollRect>();
            Image(Rect("Divider", keysPage, 350, 0, 2, 570), muted);
            RectTransform bindings = Rect("Bindings", keysPage, 390, 0, 1082, 570);
            keyRows = ScrollContent(bindings, 1082, 570, 570);
            keyScroll = bindings.GetComponent<ScrollRect>();
            float y = 0;
            for (int i = 0; i < groups.Length; i++)
            {
                KeyActionCategory category = groups[i].Key;
                categoryButtons[category] = Button(categoryContent, CategoryName(category), 0, i * 58, 290, 50, () => ShowKeyCategory(category), false);
                categoryButtons[category].normalText.alignment = TextAlignmentOptions.MidlineLeft;
                KeyActionDescriptor[] actions = groups[i].Select(pair => pair.Value).ToArray();
                float height = 100 + actions.Length * 84;
                RectTransform section = Rect(category + "Section", keyRows, 0, y, 1030, height);
                keySections[category] = section;
                Section(section, CategoryName(category), 0, 1020);
                for (int row = 0; row < actions.Length; row++)
                {
                    KeyActionDescriptor binding = actions[row];
                    Label(section, binding.Name, 20, 100 + row * 84, 635, 64, 30);
                    ButtonManager button = Button(section, binding.KeyCode.ToString(), 700, 100 + row * 84, 310, 64, () =>
                    {
                    }, false);
                    button.onClick.AddListener(() =>
                    {
                        CancelRebind();
                        bindingToRebind = binding;
                        bindingButton = button;
                        SetButtonLabel(bindingButton, "Press a key...");
                        skipRebindFrame = true;
                    });
                }

                y += height + 40;
            }

            // Leave enough space to align even a short final section with the viewport top.
            float lastTop = groups.Length == 0 ? 0 : -keySections[groups.Last().Key].anchoredPosition.y;
            keyRows.sizeDelta = new Vector2(0, Mathf.Max(y - 40, lastTop + 570));
            keyScroll.onValueChanged.AddListener(_ => UpdateKeyCategory());
            UpdateKeyCategory();
        }

        /// <summary>
        /// Returns the label displayed for a keyboard action category.
        /// </summary>
        /// <param name="category">The action category to display.</param>
        /// <returns>The category label shown in the menu.</returns>
        private static string CategoryName(KeyActionCategory category) => category == KeyActionCategory.CodeViewer ? "Code Viewer" : category.ToString();
        /// <summary>
        /// Scrolls the continuous binding list to the requested section heading.
        /// </summary>
        /// <param name="category">The action category to display.</param>
        private void ShowKeyCategory(KeyActionCategory category)
        {
            CancelRebind();
            Canvas.ForceUpdateCanvases();
            keyScroll.StopMovement();
            float maximum = Mathf.Max(0, keyRows.rect.height - keyScroll.viewport.rect.height);
            float offset = Mathf.Clamp(-keySections[category].anchoredPosition.y, 0, maximum);
            keyScroll.verticalNormalizedPosition = maximum > 0 ? 1 - offset / maximum : 1;
            UpdateKeyCategory();
        }

        /// <summary>
        /// Highlights the section at the viewport top after scrolling and reveals its anchor.
        /// </summary>
        private void UpdateKeyCategory()
        {
            KeyActionCategory? category = null;
            float top = Mathf.Max(0, keyRows.anchoredPosition.y);
            foreach (var section in keySections)
            {
                if (category == null || -section.Value.anchoredPosition.y <= top + 1)
                {
                    category = section.Key;
                }
            }

            if (category == activeKeyCategory)
            {
                return;
            }

            activeKeyCategory = category;
            foreach (var button in categoryButtons)
            {
                SetButtonColor(button.Value, button.Key == category ? accent : Color.clear);
            }

            if (category.HasValue)
            {
                RectTransform anchor = (RectTransform)categoryButtons[category.Value].transform.parent;
                float anchorTop = -anchor.anchoredPosition.y;
                float visibleTop = keyCategoryScroll.content.anchoredPosition.y;
                float viewportHeight = keyCategoryScroll.viewport.rect.height;
                float target = Mathf.Clamp(visibleTop, anchorTop + anchor.rect.height - viewportHeight, anchorTop);
                float maximum = Mathf.Max(0, keyCategoryScroll.content.rect.height - viewportHeight);
                keyCategoryScroll.StopMovement();
                keyCategoryScroll.verticalNormalizedPosition = maximum > 0 ? 1 - Mathf.Clamp(target, 0, maximum) / maximum : 1;
            }
        }

        /// <summary>
        /// Processes key rebinding and returns to the main screen when Escape is pressed.
        /// </summary>
        private void Update()
        {
            if (canvas == null || !canvas.gameObject.activeInHierarchy)
            {
                return;
            }

            if (bindingToRebind != null)
            {
                if (skipRebindFrame)
                {
                    skipRebindFrame = false;
                    return;
                }

                if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
                {
                    CancelRebind();
                    return;
                }

                if (!UnityEngine.Input.anyKeyDown)
                {
                    return;
                }

                KeyCode key = Enum.GetValues(typeof(KeyCode)).Cast<KeyCode>().FirstOrDefault(k => UnityEngine.Input.GetKeyDown(k) && KeyBindings.AssignableKeyCode(k));
                if (key == KeyCode.None)
                {
                    return;
                }

                try
                {
                    if (key != bindingToRebind.KeyCode)
                    {
                        KeyBindings.SetBindingForKey(bindingToRebind, key);
                    }

                    CancelRebind();
                }
                catch (KeyMap.KeyBindingsExistsException)
                {
                    SetButtonLabel(bindingButton, "Key in use");
                }

                return;
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape) && !loading.gameObject.activeSelf)
            {
                SaveSettings();
                ShowHome();
            }
        }

        /// <summary>
        /// Cancels pending key capture and restores the current binding label.
        /// </summary>
        private void CancelRebind()
        {
            if (bindingToRebind != null && bindingButton != null)
            {
                SetButtonLabel(bindingButton, bindingToRebind.KeyCode.ToString());
            }

            bindingToRebind = null;
            bindingButton = null;
        }

        /// <summary>
        /// Refreshes the connection form from a supplied draft without applying it.
        /// </summary>
        /// <param name="connectionSettings">The non-null draft used to populate the connection form.</param>
        /// <exception cref="ArgumentNullException">The supplied draft is null.</exception>
        internal void RefreshConnection(OpeningMenuConnection connectionSettings)
        {
            if (connectionSettings == null)
            {
                throw new ArgumentNullException(nameof(connectionSettings));
            }

            serverIP.SetTextWithoutNotify(connectionSettings.ServerAddress);
            port.SetTextWithoutNotify(connectionSettings.Port);
            password.SetTextWithoutNotify(connectionSettings.RoomPassword);
            backend.SetTextWithoutNotify(connectionSettings.BackendAPI);
            telemetryURL.SetTextWithoutNotify(connectionSettings.TelemetryURL);
            telemetryEnabled.isOn = connectionSettings.TelemetryMode != TelemetryMode.Disabled;
            telemetryEnabled.UpdateUI();
            telemetryMode.SetDropdownIndex(connectionSettings.TelemetryMode == TelemetryMode.Remote ? 1 : 0);
            telemetryMode.isInteractable = telemetryEnabled.isOn;
            telemetryURL.gameObject.SetActive(telemetryEnabled.isOn && telemetryMode.selectedItemIndex == 1);
            SelectHost(host);
        }

        /// <summary>
        /// Validates the complete form before applying it to the active settings.
        /// </summary>
        /// <returns>Whether validation succeeded and the complete draft was applied.</returns>
        internal bool TryApplyConnection()
        {
            OpeningMenuConnection connectionSettings = new()
            {
                ServerAddress = serverIP.text,
                Port = port.text,
                RoomPassword = password.text,
                BackendAPI = backend.text,
                TelemetryMode = !telemetryEnabled.isOn ? TelemetryMode.Disabled : telemetryMode.selectedItemIndex == 0 ? TelemetryMode.Local : TelemetryMode.Remote,
                TelemetryURL = telemetryURL.text
            };
            if (!connectionSettings.TryValidate(out string message))
            {
                ShowError(message);
                return false;
            }

            connectionSettings.ApplyTo(userSettings);
            return true;
        }

        /// <summary>
        /// Displays a connection validation or startup error below the form.
        /// </summary>
        /// <param name="message">The error message to display.</param>
        internal void ShowError(string message) => error.text = message;
        /// <summary>
        /// Selects the requested network mode and updates its action label.
        /// </summary>
        /// <param name="value">Whether to select host mode.</param>
        private void SelectHost(bool value)
        {
            host = value;
            SetButtonColor(hostButton, host ? accent : tabColor);
            SetButtonColor(joinButton, host ? tabColor : accent);
            if (startButton != null)
            {
                SetButtonLabel(startButton, host ? "Start hosting" : "Start joining");
            }
        }

        /// <summary>
        /// Shows the main screen and cancels any pending key capture.
        /// </summary>
        private void ShowHome() => Show(home);
        /// <summary>
        /// Shows the connection form and clears its previous error message.
        /// </summary>
        internal void ShowConnection()
        {
            error.text = "";
            Show(connection);
        }

        /// <summary>
        /// Shows the selected settings tab and its instructions.
        /// </summary>
        /// <param name="tab">The selected tab: zero for audio, one for video, or two for keybindings.</param>
        private void ShowSettings(int tab)
        {
            CancelRebind();
            Show(settings);
            audioPage.gameObject.SetActive(tab == 0);
            videoPage.gameObject.SetActive(tab == 1);
            keysPage.gameObject.SetActive(tab == 2);
            settingsHint.text = tab == 2 ? "Select an action and press a key. Esc cancels." : "Changes are saved when you go back.";
            for (int i = 0; i < settingsTabs.Length; i++)
            {
                SetButtonColor(settingsTabs[i], tab == i ? accent : tabColor);
            }
        }

        /// <summary>
        /// Activates exactly one menu screen and clears the previous UI selection.
        /// </summary>
        /// <param name="screen">The non-null menu screen to activate.</param>
        private void Show(RectTransform screen)
        {
            CancelRebind();
            home.gameObject.SetActive(screen == home);
            connection.gameObject.SetActive(screen == connection);
            settings.gameObject.SetActive(screen == settings);
            loading.gameObject.SetActive(screen == loading);
            EventSystem.current?.SetSelectedGameObject(null);
        }

        /// <summary>
        /// Builds the status panel used while connecting.
        /// </summary>
        private void BuildLoading()
        {
            RectTransform panel = Rect("Panel", loading, 300, 200, 1000, 320);
            Image(panel, panelColor);
            loadingLabel = Label(panel, "Starting...", 40, 80, 920, 80, 48);
            loadingLabel.alignment = TextAlignmentOptions.Center;
            Label(panel, "Please wait while SEE connects.", 40, 180, 920, 60, 28).alignment = TextAlignmentOptions.Center;
        }

        /// <summary>
        /// Shows the status panel for the requested network operation.
        /// </summary>
        /// <param name="hosting">Whether the pending operation starts a host rather than joins a server.</param>
        internal void ShowLoading(bool hosting)
        {
            loadingLabel.text = hosting ? "Starting host..." : "Joining server...";
            Show(loading);
        }

        /// <summary>
        /// Saves changed audio and video preferences through SEE and PlayerPrefs.
        /// </summary>
        internal void SaveSettings()
        {
            if (!dirty)
            {
                return;
            }

            userSettings.Save();
            PlayerPrefs.Save();
            dirty = false;
        }

        /// <summary>
        /// Changes menu visibility and releases or captures keyboard shortcuts.
        /// </summary>
        /// <param name="visible">Whether to display the menu.</param>
        internal void SetVisible(bool visible)
        {
            canvas.gameObject.SetActive(visible);
            if (visible)
            {
                CaptureShortcuts();
            }
            else
            {
                RestoreShortcuts();
            }
        }

        /// <summary>
        /// Captures the previous shortcut state and suppresses gameplay shortcuts.
        /// </summary>
        private void CaptureShortcuts()
        {
            if (shortcutsCaptured)
            {
                return;
            }

            previousShortcuts = SEEInput.KeyboardShortcutsEnabled;
            shortcutsCaptured = true;
            SEEInput.KeyboardShortcutsEnabled = false;
        }

        /// <summary>
        /// Restores the shortcut state captured by this view.
        /// </summary>
        private void RestoreShortcuts()
        {
            if (!shortcutsCaptured)
            {
                return;
            }

            SEEInput.KeyboardShortcutsEnabled = previousShortcuts;
            shortcutsCaptured = false;
        }

        /// <summary>
        /// Releases keyboard capture and the background texture owned by this view.
        /// </summary>
        private void OnDestroy()
        {
            RestoreShortcuts();
            if (backdropTexture != null)
            {
                backdropTexture.Release();
                Destroyer.Destroy(backdropTexture);
            }
        }

        /// <summary>
        /// Displays the complete background image with mild softening and its original aspect ratio.
        /// </summary>
        /// <param name="parent">The non-null parent transform.</param>
        private void BuildBackdrop(Transform parent)
        {
            Texture2D source = Resources.Load<Texture2D>("UI/OpeningMenu/Backdrop");
            if (source == null)
            {
                return;
            }

            int width = Mathf.Min(source.width, 960);
            int height = Mathf.Max(1, Mathf.RoundToInt(width * (float)source.height / source.width));
            backdropTexture = new RenderTexture(width, height, 0)
            {
                filterMode = FilterMode.Bilinear
            };
            Graphics.Blit(source, backdropTexture);
            RectTransform background = Stretch("Backdrop", parent);
            Image(background, Color.black).raycastTarget = false;
            RectTransform rect = Stretch("Image", background);
            rect.pivot = new Vector2(0.5f, 0.5f);
            RawImage image = rect.gameObject.AddComponent<RawImage>();
            image.texture = backdropTexture;
            image.uvRect = new Rect(0, 0, 1, 1);
            image.raycastTarget = false;
            image.color = new Color(0.75f, 0.75f, 0.75f, 1);
            AspectRatioFitter aspect = rect.gameObject.AddComponent<AspectRatioFitter>();
            aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            aspect.aspectRatio = (float)source.width / source.height;
        }

        /// <summary>
        /// Saves changed preferences and stops Play Mode or exits the player.
        /// </summary>
        private void Exit()
        {
            SaveSettings();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

#region Canvas controls
        /// <summary>
        /// Creates a rectangle anchored to the top left of its parent.
        /// </summary>
        /// <param name="name">The hierarchy name assigned to the created control.</param>
        /// <param name="parent">The non-null parent transform.</param>
        /// <param name="x">The offset from the left of the parent, in reference pixels.</param>
        /// <param name="y">The downward offset from the top of the parent, in reference pixels.</param>
        /// <param name="width">The width in reference pixels.</param>
        /// <param name="height">The height in reference pixels.</param>
        /// <returns>The newly created rectangle.</returns>
        private static RectTransform Rect(string name, Transform parent, float x, float y, float width, float height)
        {
            RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        /// <summary>
        /// Creates a rectangle that fills its parent.
        /// </summary>
        /// <param name="name">The hierarchy name assigned to the created control.</param>
        /// <param name="parent">The non-null parent transform.</param>
        /// <returns>The newly created stretched rectangle.</returns>
        private static RectTransform Stretch(string name, Transform parent)
        {
            RectTransform rect = Rect(name, parent, 0, 0, 0, 0);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        /// <summary>
        /// Creates a centered menu screen with the design reference dimensions.
        /// </summary>
        /// <param name="name">The hierarchy name assigned to the created control.</param>
        /// <param name="parent">The non-null parent transform.</param>
        /// <returns>The newly created screen rectangle.</returns>
        private static RectTransform Screen(string name, Transform parent)
        {
            RectTransform rect = Rect(name, parent, 0, 0, 1600, 900);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            return rect;
        }

        /// <summary>
        /// Creates a panel graphic with an optional rounded border.
        /// </summary>
        /// <param name="rect">The non-null rectangle to configure.</param>
        /// <param name="color">The background color to apply.</param>
        /// <param name="border">Whether to add a rounded outline.</param>
        /// <returns>The newly created image component.</returns>
        private UnityEngine.UI.Image Image(RectTransform rect, Color color, bool border = false)
        {
            UnityEngine.UI.Image image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = color;
            if (rect.rect.width > 8 && rect.rect.height > 8)
            {
                image.sprite = rounded;
                image.type = UnityEngine.UI.Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = 8;
            }

            if (border)
            {
                RectTransform edge = Stretch("Border", rect);
                UnityEngine.UI.Image line = edge.gameObject.AddComponent<UnityEngine.UI.Image>();
                line.sprite = roundedOutline;
                line.type = UnityEngine.UI.Image.Type.Sliced;
                line.pixelsPerUnitMultiplier = 8;
                line.color = new Color(0.8f, 0.86f, 0.87f, 0.9f);
                line.raycastTarget = false;
            }

            return image;
        }

        /// <summary>
        /// Creates a non-interactive TextMesh Pro label.
        /// </summary>
        /// <param name="parent">The non-null parent transform.</param>
        /// <param name="value">The initial value of the control.</param>
        /// <param name="x">The offset from the left of the parent, in reference pixels.</param>
        /// <param name="y">The downward offset from the top of the parent, in reference pixels.</param>
        /// <param name="width">The width in reference pixels.</param>
        /// <param name="height">The height in reference pixels.</param>
        /// <param name="size">The text size in reference pixels.</param>
        /// <param name="style">The text style to apply.</param>
        /// <returns>The newly created text component.</returns>
        private TextMeshProUGUI Label(Transform parent, string value, float x, float y, float width, float height, float size = 34, FontStyles style = FontStyles.Normal)
        {
            RectTransform rect = Rect("Label", parent, x, y, width, height);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>
        /// Instantiates the existing SEE action-button prefab and binds its MUIP event.
        /// </summary>
        /// <param name="parent">The non-null parent transform.</param>
        /// <param name="label">The text displayed to the user.</param>
        /// <param name="x">The offset from the left of the parent, in reference pixels.</param>
        /// <param name="y">The downward offset from the top of the parent, in reference pixels.</param>
        /// <param name="width">The width in reference pixels.</param>
        /// <param name="height">The height in reference pixels.</param>
        /// <param name="clicked">The non-null action invoked by the MUIP click event.</param>
        /// <param name="primary">Whether to use the primary action color.</param>
        /// <returns>The MUIP button from the instantiated SEE prefab.</returns>
        /// <exception cref="Exception">A required SEE prefab cannot be instantiated.</exception>
        private ButtonManager Button(Transform parent, string label, float x, float y, float width, float height, UnityAction clicked, bool primary = false)
        {
            RectTransform container = InstantiateControl(label.Replace(" ", "") + "Button", "Prefabs/UI/ActionButton", parent, x, y, width, height);
            ButtonManager button = container.GetComponentInChildren<ButtonManager>(true);
            button.autoFitContent = false;
            button.enableIcon = false;
            button.enableText = true;
            button.useUINavigation = true;
            button.useRipple = false;
            button.checkForDoubleClick = false;
            button.textSize = 34;
            foreach (TextMeshProUGUI text in container.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                text.font = font;
                text.fontSize = 34;
                text.fontStyle = FontStyles.Normal;
            }

            button.onClick.AddListener(clicked);
            SetButtonLabel(button, label);
            SetButtonColor(button, primary ? accent : fieldColor);
            container.gameObject.SetActive(true);
            return button;
        }

        /// <summary>
        /// Updates the MUIP button text and all of its visual states.
        /// </summary>
        /// <param name="button">The non-null MUIP button to update.</param>
        /// <param name="text">The text to display.</param>
        private static void SetButtonLabel(ButtonManager button, string text)
        {
            button.buttonText = text;
            button.UpdateUI();
        }

        /// <summary>
        /// Applies the selected background and pointer states to a SEE MUIP button.
        /// </summary>
        /// <param name="button">The non-null MUIP button to update.</param>
        /// <param name="color">The background color to apply.</param>
        private static void SetButtonColor(ButtonManager button, Color color)
        {
            UnityEngine.UI.Button selectable = button.GetComponent<UnityEngine.UI.Button>();
            UnityEngine.UI.Image background = button.GetComponent<UnityEngine.UI.Image>();
            background.color = color;
            selectable.targetGraphic = background;
            ColorBlock colors = selectable.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.6f, 0.9f, 1);
            colors.selectedColor = new Color(0.6f, 0.9f, 1);
            colors.pressedColor = new Color(0.4f, 0.7f, 0.85f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            colors.fadeDuration = 0.12f;
            selectable.colors = colors;
        }

        /// <summary>
        /// Creates a masked single-line text field for a draft value.
        /// </summary>
        /// <param name="parent">The non-null parent transform.</param>
        /// <param name="name">The hierarchy name assigned to the created control.</param>
        /// <param name="x">The offset from the left of the parent, in reference pixels.</param>
        /// <param name="y">The downward offset from the top of the parent, in reference pixels.</param>
        /// <param name="width">The width in reference pixels.</param>
        /// <param name="placeholder">The hint displayed while the input is empty.</param>
        /// <returns>The newly created input field.</returns>
        private TMP_InputField Input(Transform parent, string name, float x, float y, float width, string placeholder)
        {
            RectTransform rect = Rect(name, parent, x, y, width, 68);
            UnityEngine.UI.Image image = Image(rect, fieldColor, true);
            TMP_InputField input = rect.gameObject.AddComponent<TMP_InputField>();
            input.targetGraphic = image;
            RectTransform viewport = Rect("TextViewport", rect, 18, 4, width - 36, 60);
            viewport.gameObject.AddComponent<RectMask2D>();
            TextMeshProUGUI text = Label(viewport, "", 0, 0, width - 36, 60, 30);
            TextMeshProUGUI hint = Label(viewport, placeholder, 0, 0, width - 36, 60, 29);
            hint.color = muted;
            input.textViewport = viewport;
            input.textComponent = text;
            input.placeholder = hint;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.characterLimit = 512;
            input.customCaretColor = true;
            input.caretColor = Color.white;
            input.selectionColor = new Color(0, 0.55f, 0.8f, 0.6f);
            return input;
        }

        /// <summary>
        /// Instantiates the existing SEE MUIP switch and binds its value change event.
        /// </summary>
        /// <param name="parent">The non-null parent transform.</param>
        /// <param name="label">The text displayed to the user.</param>
        /// <param name="x">The offset from the left of the parent, in reference pixels.</param>
        /// <param name="y">The downward offset from the top of the parent, in reference pixels.</param>
        /// <param name="width">The width in reference pixels.</param>
        /// <param name="value">The initial value of the control.</param>
        /// <param name="changed">The non-null callback receiving subsequent user changes.</param>
        /// <returns>The MUIP switch from the instantiated SEE prefab.</returns>
        /// <exception cref="Exception">A required SEE prefab cannot be instantiated.</exception>
        private SwitchManager Switch(Transform parent, string label, float x, float y, float width, bool value, UnityAction<bool> changed)
        {
            RectTransform container = InstantiateControl(label.Replace(" ", ""), "Prefabs/UI/Input Group - Switch", parent, x, y, width, 64);
            Transform group = container.GetChild(0);
            group.Find("Label").gameObject.SetActive(false);
            group.GetComponent<Animator>().enabled = false;
            Label(container, label, 0, 0, width - 120, 64, 32);
            SwitchManager manager = group.GetComponentInChildren<SwitchManager>(true);
            manager.saveValue = false;
            manager.invokeAtStart = false;
            manager.isOn = value;
            manager.onValueChanged.AddListener(changed);
            RectTransform control = (RectTransform)manager.transform;
            control.anchorMin = control.anchorMax = control.pivot = new Vector2(0, 1);
            control.anchoredPosition = new Vector2(width - 84, -12);
            control.sizeDelta = new Vector2(70, 40);
            control.localScale = Vector3.one;
            container.gameObject.SetActive(true);
            return manager;
        }

        /// <summary>
        /// Instantiates the existing SEE MUIP slider for a volume preference.
        /// </summary>
        /// <param name="parent">The non-null parent transform.</param>
        /// <param name="label">The text displayed to the user.</param>
        /// <param name="y">The downward offset from the top of the parent, in reference pixels.</param>
        /// <param name="value">The initial value of the control.</param>
        /// <param name="changed">The non-null callback receiving subsequent user changes.</param>
        /// <exception cref="Exception">A required SEE prefab cannot be instantiated.</exception>
        private void Volume(Transform parent, string label, float y, float value, Action<float> changed)
        {
            Label(parent, label, 100, y, 700, 64);
            RectTransform container = InstantiateControl(label.Replace(" ", "") + "Slider", "Prefabs/UI/Slider", parent, 900, y, 440, 64);
            SliderManager manager = container.GetComponentInChildren<SliderManager>(true);
            manager.enableSaving = false;
            manager.invokeOnAwake = false;
            manager.showValue = false;
            manager.showPopupValue = false;
            manager.valueText.gameObject.SetActive(false);
            Slider slider = manager.mainSlider;
            slider.wholeNumbers = false;
            slider.minValue = 0;
            slider.maxValue = 1;
            slider.SetValueWithoutNotify(value);
            slider.onValueChanged.AddListener(volume =>
            {
                changed(volume);
                dirty = true;
            });
            container.gameObject.SetActive(true);
        }

        /// <summary>
        /// Instantiates the existing SEE MUIP dropdown and binds its selection event.
        /// </summary>
        /// <param name="parent">The non-null parent transform.</param>
        /// <param name="name">The hierarchy name assigned to the created control.</param>
        /// <param name="x">The offset from the left of the parent, in reference pixels.</param>
        /// <param name="y">The downward offset from the top of the parent, in reference pixels.</param>
        /// <param name="width">The width in reference pixels.</param>
        /// <param name="values">The non-null, non-empty sequence of option labels.</param>
        /// <param name="selected">The initial option index, clamped to the available options.</param>
        /// <param name="changed">The non-null callback receiving subsequent user changes.</param>
        /// <returns>The MUIP dropdown from the instantiated SEE prefab.</returns>
        /// <exception cref="Exception">A required SEE prefab cannot be instantiated.</exception>
        private CustomDropdown Dropdown(Transform parent, string name, float x, float y, float width, IEnumerable<string> values, int selected, UnityAction<int> changed)
        {
            RectTransform container = InstantiateControl(name + "Dropdown", "Prefabs/UI/Input Group - Dropdown 2", parent, x, y, width, 68);
            Transform group = container.GetChild(0);
            group.Find("Label").gameObject.SetActive(false);
            CustomDropdown dropdown = group.GetComponentInChildren<CustomDropdown>(true);
            StretchRect((RectTransform)dropdown.transform);
            dropdown.enableIcon = false;
            dropdown.enableDropdownSounds = false;
            dropdown.invokeAtStart = false;
            dropdown.saveSelected = false;
            dropdown.initAtStart = true;
            dropdown.setHighPriority = true;
            dropdown.panelSize = 260;
            dropdown.animationType = CustomDropdown.AnimationType.Modular;
            dropdown.items.Clear();
            foreach (string value in values)
            {
                dropdown.items.Add(new() { itemName = value });
            }

            dropdown.selectedItemIndex = Mathf.Clamp(selected, 0, dropdown.items.Count - 1);
            dropdown.onItemTextChanged.AddListener(text =>
            {
                text.font = font;
                text.fontSize = 29;
                text.color = Color.white;
                text.textWrappingMode = TextWrappingModes.NoWrap;
                text.overflowMode = TextOverflowModes.Ellipsis;
            });
            dropdown.onValueChanged.AddListener(changed);
            container.gameObject.SetActive(true);
            return dropdown;
        }

        /// <summary>
        /// Creates an inactive wrapper containing an existing SEE control prefab.
        /// </summary>
        /// <param name="name">The hierarchy name assigned to the created control.</param>
        /// <param name="prefabPath">The existing prefab path relative to the Resources folder.</param>
        /// <param name="parent">The non-null parent transform.</param>
        /// <param name="x">The offset from the left of the parent, in reference pixels.</param>
        /// <param name="y">The downward offset from the top of the parent, in reference pixels.</param>
        /// <param name="width">The width in reference pixels.</param>
        /// <param name="height">The height in reference pixels.</param>
        /// <returns>The inactive wrapper, ready to be configured before activation.</returns>
        /// <exception cref="Exception">A required SEE prefab cannot be instantiated.</exception>
        private static RectTransform InstantiateControl(string name, string prefabPath, Transform parent, float x, float y, float width, float height)
        {
            RectTransform container = Rect(name, parent, x, y, width, height);
            container.gameObject.SetActive(false);
            GameObject instance = PrefabInstantiator.InstantiatePrefab(prefabPath, container, false);
            StretchRect((RectTransform)instance.transform);
            return container;
        }

        /// <summary>
        /// Resets a prefab rectangle to fill its wrapper without inherited scale.
        /// </summary>
        /// <param name="rect">The non-null rectangle to configure.</param>
        private static void StretchRect(RectTransform rect)
        {
            rect.localScale = Vector3.one;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// Creates a vertically clamped and masked scroll view with a scrollbar.
        /// </summary>
        /// <param name="parent">The non-null parent transform.</param>
        /// <param name="width">The width in reference pixels.</param>
        /// <param name="height">The height in reference pixels.</param>
        /// <param name="contentHeight">The height of the scrollable content, in reference pixels.</param>
        /// <returns>The content rectangle into which scrolling rows can be inserted.</returns>
        private RectTransform ScrollContent(RectTransform parent, float width, float height, float contentHeight)
        {
            ScrollRect scroll = parent.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 48;
            RectTransform viewport = Rect("Viewport", parent, 0, 0, width - 24, height);
            Image(viewport, new Color(0, 0, 0, 0.07f));
            viewport.gameObject.AddComponent<RectMask2D>();
            RectTransform content = Rect("Content", viewport, 0, 0, width - 24, contentHeight);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.sizeDelta = new Vector2(0, contentHeight);
            scroll.viewport = viewport;
            scroll.content = content;
            RectTransform bar = Rect("Scrollbar", parent, width - 10, 0, 5, height);
            Image(bar, new Color(1, 1, 1, 0.15f));
            Scrollbar scrollbar = bar.gameObject.AddComponent<Scrollbar>();
            RectTransform sliding = Stretch("SlidingArea", bar);
            RectTransform handle = Stretch("Handle", sliding);
            scrollbar.targetGraphic = Image(handle, muted);
            scrollbar.handleRect = handle;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            return content;
        }

        /// <summary>
        /// Creates a settings section heading and its divider.
        /// </summary>
        /// <param name="parent">The non-null parent transform.</param>
        /// <param name="title">The section heading.</param>
        /// <param name="y">The downward offset from the top of the parent, in reference pixels.</param>
        /// <param name="width">The width in reference pixels.</param>
        private void Section(Transform parent, string title, float y, float width)
        {
            Label(parent, title, 0, y, width, 56, 40, FontStyles.Bold);
            Image(Rect("Separator", parent, 0, y + 65, width, 2), muted);
        }
#endregion
    }
}
