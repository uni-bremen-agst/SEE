using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using SEE.Controls;
using SEE.Controls.KeyActions;
using SEE.GO;
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

        /// <summary>
        /// A mapping of the short name of the key binding onto the label of the button that allows to
        /// change the binding. This dictionary is used to update the label if the key binding
        /// was changed by the user.
        /// </summary>
        private readonly Dictionary<string, TextMeshProUGUI> shortNameOfBindingToLabel = new();

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

            // Displays all bindings grouped by their category.
            foreach (var group in KeyBindings.AllBindings())
            {
                // Creates a list of keybinding descriptions for the given category.
                GameObject scrollView = PrefabInstantiator.InstantiatePrefab(scrollPrefab, Canvas.transform, false).transform.gameObject;
                scrollView.transform.SetParent(settingsMenuGameObject.transform.Find("KeybindingsPanel/KeybindingsText/Viewport/Content"));
                // set the titles of the scrollViews to the scopes
                TextMeshProUGUI groupTitle = scrollView.transform.Find("Group").gameObject.MustGetComponent<TextMeshProUGUI>();
                groupTitle.text = $"{group.Key}";

                foreach ((_, KeyActionDescriptor descriptor) in group)
                {
                    GameObject keyBindingContent = PrefabInstantiator.InstantiatePrefab(SettingsMenu.keyBindingContent, Canvas.transform, false).transform.gameObject;
                    keyBindingContent.transform.SetParent(scrollView.transform.Find("Scroll View/Viewport/Content"));

                    // set the text to the short name of the binding
                    TextMeshProUGUI bindingText = keyBindingContent.transform.Find("Binding").gameObject.MustGetComponent<TextMeshProUGUI>();
                    // The short name of the binding.
                    bindingText.text = descriptor.Name;
                    // set the label of the key button
                    TextMeshProUGUI key = keyBindingContent.transform.Find("Key/Text (TMP)").gameObject.MustGetComponent<TextMeshProUGUI>();
                    // The name of the key code bound.
                    key.text = descriptor.KeyCode.ToString();
                    shortNameOfBindingToLabel[descriptor.Name] = key;
                    // add the actionlistener to be able to change the key code of a binding.
                    keyBindingContent.transform.Find("Key").gameObject.MustGetComponent<Button>().onClick.AddListener(() => StartRebindFor(descriptor));
                }
            }
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
                Transform keybindingsPanel = settingsMenuGameObject.transform.Find("KeybindingsPanel");
                GameObject settingsPanel = settingsMenuGameObject.transform.Find("SettingsPanel").gameObject;
                if (keybindingsPanel.gameObject.activeSelf && !settingsPanel.activeSelf)
                {
                    // handles the case where the user is in the KeybindingsPanel but wants to close it
                    keybindingsPanel.gameObject.SetActive(false);
                }
                else
                {
                    // handles the case where the user wants to open/close the SettingsPanel
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
        /// The key binding that gets updated.
        /// </summary>
        private KeyActionDescriptor bindingToRebind;

        /// <summary>
        /// The notification telling the user to press a key to rebind the key binding.
        /// </summary>
        private Notification.Notification bindingNotification;

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
    }
}
