using Michsky.MUIP;
using NUnit.Framework;
using System.Collections;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using SEE.UserSettings;
using SEE.Utils.Paths;

/// <summary>
/// User interface components and their tests.
/// </summary>
namespace SEE.UI
{
    /// <summary>
    /// Exercises the actual MUIP controls without starting a network connection or saving user settings.
    /// </summary>
    internal class TestOpeningMenuView
    {
        /// <summary>
        /// The isolated settings object owned by the fixture.
        /// </summary>
        private GameObject settingsObject;
        /// <summary>
        /// The menu object owned by the fixture.
        /// </summary>
        private GameObject menuObject;
        /// <summary>
        /// The settings edited only in memory by the fixture.
        /// </summary>
        private UserSetting settings;
        /// <summary>
        /// The view under test.
        /// </summary>
        private OpeningMenuView menu;
        /// <summary>
        /// The number of startup callbacks received by the fixture.
        /// </summary>
        private int startupRequests;
        /// <summary>
        /// Whether the most recent startup callback requested host mode.
        /// </summary>
        private bool hostRequested;
        /// <summary>
        /// The root transform of the menu's own canvas.
        /// </summary>
        private Transform MenuRoot => menu.GetComponentInChildren<Canvas>(true).transform;

        /// <summary>
        /// Creates isolated settings and a view whose startup callback never starts networking.
        /// </summary>
        /// <returns>An enumerator waiting for control initialization.</returns>
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            startupRequests = 0;
            hostRequested = true;
            settingsObject = new("Opening menu test settings");
            settingsObject.SetActive(false);
            settings = settingsObject.AddComponent<UserSetting>();
            settings.enabled = false;
            settings.ConfigPath = new DataPath(Path.Combine(Application.temporaryCachePath, "SEE-opening-menu-tests-not-saved.cfg"));
            settingsObject.SetActive(true);
            menuObject = new("Opening menu test");
            menu = menuObject.AddComponent<OpeningMenuView>();
            menu.Initialize(settings, PlayerInputType.DesktopPlayer, new OpeningMenuConnection(), host =>
            {
                startupRequests++;
                hostRequested = host;
            }, () =>
            {
            });
            yield return null;
        }

        /// <summary>
        /// Releases the view before removing its isolated settings.
        /// </summary>
        /// <returns>An enumerator waiting for deferred object destruction.</returns>
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(menuObject);
            yield return null;
            Object.Destroy(settingsObject);
            yield return null;
        }

        /// <summary>
        /// Starts on the home screen with real MUIP buttons and exactly one EventSystem.
        /// </summary>
        [Test]
        public void StartsAtHomeWithModernUI()
        {
            Assert.That(MenuRoot.Find("Home").gameObject.activeSelf, Is.True);
            Assert.That(MenuRoot.Find("Connection").gameObject.activeSelf, Is.False);
            Assert.That(MenuRoot.Find("Home/Sidebar/StartButton").GetComponentInChildren<ButtonManager>(), Is.Not.Null);
            Assert.That(Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None), Has.Length.EqualTo(1));
        }

        /// <summary>
        /// Sends client mode through the real Join and startup controls.
        /// </summary>
        [Test]
        public void JoinSelectionReachesStartupCallback()
        {
            Click("Home/Sidebar/StartButton");
            Click("Connection/Panel/JoinButton");
            Click("Connection/StarthostingButton");
            Assert.That(startupRequests, Is.EqualTo(1));
            Assert.That(hostRequested, Is.False);
        }

        /// <summary>
        /// Rejects malformed form input without applying network settings or starting a connection.
        /// </summary>
        [Test]
        public void InvalidPortShowsValidationMessage()
        {
            Click("Home/Sidebar/StartButton");
            MenuRoot.Find("Connection/Panel/Port").GetComponent<TMP_InputField>().SetTextWithoutNotify("65536");
            Assert.That(menu.TryApplyConnection(), Is.False);
            Assert.That(MenuRoot.Find("Connection").GetComponentsInChildren<TMP_Text>().Any(text => text.text.Contains("0 and 65535")), Is.True);
            Assert.That(startupRequests, Is.Zero);
        }

        /// <summary>
        /// Resets a MUIP dropdown's logical open state when changing tabs.
        /// </summary>
        /// <returns>An enumerator waiting for page initialization between interactions.</returns>
        [UnityTest]
        public IEnumerator DropdownReopensAfterChangingTabs()
        {
            Click("Home/Sidebar/SettingsButton");
            yield return null;
            CustomDropdown dropdown = MenuRoot.Find("Settings/Panel/Audio/Viewport/Content/MicrophoneDropdown").GetComponentInChildren<CustomDropdown>(true);
            dropdown.Animate();
            Assert.That(dropdown.listCG.blocksRaycasts, Is.True);
            Click("Settings/Panel/VideoButton");
            Click("Settings/Panel/AudioButton");
            yield return null;
            Assert.That(dropdown.listCG.blocksRaycasts, Is.False);
            dropdown.Animate();
            Assert.That(dropdown.listCG.blocksRaycasts, Is.True);
        }

        /// <summary>
        /// Applies actual MUIP slider and switch events to in-memory audio preferences.
        /// </summary>
        [Test]
        public void ModernUIAudioControlsUpdatePreferences()
        {
            Click("Home/Sidebar/SettingsButton");
            Transform content = MenuRoot.Find("Settings/Panel/Audio/Viewport/Content");
            SliderManager volume = content.Find("MusicSlider").GetComponentInChildren<SliderManager>(true);
            Assert.That(volume.mainSlider.wholeNumbers, Is.False);
            volume.mainSlider.value = 0.37f;
            Assert.That(volume.mainSlider.value, Is.EqualTo(0.37f).Within(0.0001f));
            Assert.That(settings.Audio.MusicVolume, Is.EqualTo(0.37f).Within(0.0001f));
            Assert.That(settings.Audio.MusicMuted, Is.False);
            SwitchManager gain = content.Find("AutoGainControl").GetComponentInChildren<SwitchManager>(true);
            gain.SetOn();
            Assert.That(settings.Audio.Microphone.AutoGainControl, Is.True);
            Assert.That(gain.saveValue, Is.False);
            Assert.That(volume.enableSaving, Is.False);
        }

        /// <summary>
        /// Cancels pending key capture when another action category is selected.
        /// </summary>
        [Test]
        public void ChangingCategoryCancelsKeyCapture()
        {
            Click("Home/Sidebar/SettingsButton");
            Click("Settings/Panel/KeybindingsButton");
            Transform bindings = MenuRoot.Find("Settings/Panel/Keybindings/Bindings/Viewport/Content");
            ButtonManager binding = bindings.GetComponentsInChildren<ButtonManager>().First();
            binding.onClick.Invoke();
            Assert.That(binding.buttonText, Is.EqualTo("Press a key..."));
            Click("Settings/Panel/Keybindings/Categories/Viewport/Content/GeneralButton");
            Assert.That(MenuRoot.GetComponentsInChildren<TMP_Text>().Any(text => text.text == "Press a key..."), Is.False);
        }

        /// <summary>
        /// Keeps all binding sections alive and aligns every clicked anchor, including the final one.
        /// </summary>
        /// <returns>An enumerator waiting for scroll updates between anchor selections.</returns>
        [UnityTest]
        public IEnumerator CategoryAnchorsScrollWithoutReplacingBindings()
        {
            Click("Home/Sidebar/SettingsButton");
            Click("Settings/Panel/KeybindingsButton");
            yield return null;
            ScrollRect scroll = MenuRoot.Find("Settings/Panel/Keybindings/Bindings").GetComponent<ScrollRect>();
            Transform categories = MenuRoot.Find("Settings/Panel/Keybindings/Categories/Viewport/Content");
            ButtonManager[] anchors = categories.GetComponentsInChildren<ButtonManager>();
            ButtonManager[] bindings = scroll.content.GetComponentsInChildren<ButtonManager>();
            Assert.That(scroll.content.childCount, Is.EqualTo(anchors.Length));
            Assert.That(scroll.content.rect.height, Is.GreaterThan(scroll.viewport.rect.height));
            foreach (ButtonManager anchor in anchors)
            {
                anchor.OnSubmit(new BaseEventData(EventSystem.current));
                yield return null;
                RectTransform section = scroll.content.GetChild(anchor.transform.parent.GetSiblingIndex()) as RectTransform;
                Assert.That(scroll.content.anchoredPosition.y, Is.EqualTo(-section.anchoredPosition.y).Within(1));
                AssertActiveCategory(anchor);
                Assert.That(scroll.content.GetComponentsInChildren<ButtonManager>(), Is.EqualTo(bindings));
            }
        }

        /// <summary>
        /// Tracks wheel and scrollbar movement and keeps the highlighted sidebar anchor visible.
        /// </summary>
        /// <returns>An enumerator waiting for scroll events to update the highlighted category.</returns>
        [UnityTest]
        public IEnumerator ManualScrollingUpdatesCategoryHighlight()
        {
            Click("Home/Sidebar/SettingsButton");
            Click("Settings/Panel/KeybindingsButton");
            yield return null;
            ScrollRect scroll = MenuRoot.Find("Settings/Panel/Keybindings/Bindings").GetComponent<ScrollRect>();
            ScrollRect categories = MenuRoot.Find("Settings/Panel/Keybindings/Categories").GetComponent<ScrollRect>();
            int index = scroll.content.childCount / 2;
            RectTransform section = (RectTransform)scroll.content.GetChild(index);
            float offset = -section.anchoredPosition.y + 120;
            scroll.OnScroll(new PointerEventData(EventSystem.current)
            {
                scrollDelta = new Vector2(0, -offset / scroll.scrollSensitivity)
            });
            yield return null;
            AssertActiveCategory(categories.content.GetChild(index).GetComponentInChildren<ButtonManager>());
            scroll.verticalScrollbar.value = 0;
            yield return null;
            RectTransform lastAnchor = (RectTransform)categories.content.GetChild(categories.content.childCount - 1);
            AssertActiveCategory(lastAnchor.GetComponentInChildren<ButtonManager>());
            float anchorTop = -lastAnchor.anchoredPosition.y;
            float visibleTop = categories.content.anchoredPosition.y;
            Assert.That(anchorTop, Is.GreaterThanOrEqualTo(visibleTop - 1));
            Assert.That(anchorTop + lastAnchor.rect.height, Is.LessThanOrEqualTo(visibleTop + categories.viewport.rect.height + 1));
            scroll.verticalScrollbar.value = 1;
            yield return null;
            AssertActiveCategory(categories.content.GetChild(0).GetComponentInChildren<ButtonManager>());
        }

        /// <summary>
        /// Checks that exactly the expected sidebar anchor has the visible accent background.
        /// </summary>
        /// <param name="expected">The non-null anchor expected to be highlighted.</param>
        private void AssertActiveCategory(ButtonManager expected)
        {
            Transform categories = MenuRoot.Find("Settings/Panel/Keybindings/Categories/Viewport/Content");
            ButtonManager[] highlighted = categories.GetComponentsInChildren<ButtonManager>()
                .Where(button => button.GetComponent<UnityEngine.UI.Image>().color.a > 0.5f).ToArray();
            Assert.That(highlighted, Is.EqualTo(new[] { expected }));
        }

        /// <summary>
        /// Submits a visible MUIP button through the same event interface as keyboard navigation.
        /// </summary>
        /// <param name="path">The hierarchy path relative to the menu canvas.</param>
        private void Click(string path)
        {
            Transform container = MenuRoot.Find(path);
            Assert.That(container, Is.Not.Null, path);
            ButtonManager button = container.GetComponentInChildren<ButtonManager>(true);
            Assert.That(button, Is.Not.Null, path);
            button.OnSubmit(new BaseEventData(EventSystem.current));
        }
    }
}
