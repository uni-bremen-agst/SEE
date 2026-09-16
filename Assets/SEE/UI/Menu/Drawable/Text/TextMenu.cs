using SEE.Controls;
using SEE.Game.Drawable.Configurations;
using SEE.UI.Drawable;
using SEE.UI.Menu.Drawable.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// This class holds the instance for the text menu.
    /// </summary>
    public class TextMenu : SingletonMenu
    {
        #region Variables
        /// <summary>
        /// The location where the text menu prefeb is placed.
        /// </summary>
        private const string textMenuPrefab = "Prefabs/UI/Drawable/TextMenu";

        /// <summary>
        /// Holds all UI references used by this text-menu instance.
        /// </summary>
        private readonly TextMenuControls controls;

        /// <summary>
        /// Manages the shared text-style behavior.
        /// </summary>
        private readonly TextStyleMenu textStyleMenu;

        /// <summary>
        /// Manages writing-specific text-menu behavior.
        /// </summary>
        private readonly WriteTextMenu writeTextMenu;

        /// <summary>
        /// Manages editing-specific text-menu behavior.
        /// </summary>
        private readonly EditTextMenu editTextMenu;
        #endregion

        /// <summary>
        /// Creates and initializes the text menu.
        /// </summary>
        private TextMenu()
        {
            Instantiate(textMenuPrefab);

            controls = new TextMenuControls(gameObject);

            textStyleMenu = new TextStyleMenu(
                gameObject,
                controls);

            writeTextMenu = new WriteTextMenu(
                gameObject,
                controls,
                EnableTextMenu,
                textStyleMenu);

            editTextMenu = new EditTextMenu(
                gameObject,
                controls,
                EnableTextMenu,
                textStyleMenu);
        }

        /// <summary>
        /// The only text-menu instance.
        /// </summary>
        public static TextMenu Instance { get; private set; }

        /// <summary>
        /// Creates the singleton text-menu instance.
        /// </summary>
        static TextMenu()
        {
            Instance = new TextMenu();
            Instance.Enable();
        }

        /// <summary>
        /// Returns true if the menu is already opened.
        /// </summary>
        /// <returns>True if the menu is alreay opened. Otherwise false.</returns>
        public override bool IsOpen()
        {
            return gameObject.activeInHierarchy;
        }

        /// <summary>
        /// Hides the text menu and restores keyboard shortcuts.
        /// </summary>
        public override void Disable()
        {
            base.Disable();
            controls.ReturnButtonObject.SetActive(false);
            SEEInput.KeyboardShortcutsEnabled = true;
        }

        /// <summary>
        /// Resets the text menu to its initial state.
        /// </summary>
        private void Reset()
        {
            textStyleMenu.Reset();

            controls.OrderInLayerSlider.OnValueChanged.RemoveAllListeners();
        }

        /// <summary>
        /// Enables the text menu in its default writing configuration.
        /// </summary>
        public override void Enable()
        {
            Enable(
                reset: true,
                showEditMode: false);
        }

        /// <summary>
        /// Enables the text menu using the requested configuration.
        /// </summary>
        /// <param name="reset">
        /// Whether the current menu handlers and state should be reset.
        /// </param>
        /// <param name="showEditMode">
        /// Whether editing-specific controls should be shown.
        /// </param>
        public void Enable(bool reset, bool showEditMode = false)
        {
            if (reset)
            {
                Reset();
            }

            ConfigureModeControls(showEditMode);

            MenuHelper.CalculateHeight(gameObject);

            gameObject.SetActive(true);
        }

        /// <summary>
        /// Configures controls that differ between writing and editing.
        /// </summary>
        /// <param name="showEditMode">
        /// Whether editing-specific controls should be visible.
        /// </param>
        private void ConfigureModeControls(bool showEditMode)
        {
            if (showEditMode)
            {
                controls.OrderInLayerObject.SetActive(true);
                controls.OrderInLayerUnitySlider.interactable = true;
                controls.EditTextObject.SetActive(true);
            }
            else
            {
                controls.OrderInLayerObject.SetActive(false);
                controls.EditTextObject.SetActive(false);
            }
        }

        /// <summary>
        /// Reveals the text menu
        /// </summary>
        /// <param name="colorAction">The inital action for the HSV color picker.</param>
        /// <param name="color">The inital color for the HSV color picker.</param>
        /// <param name="reset">Specifies whether the menu should be reset to its initial state.</param>
        /// <param name="showEditMode">Specifies whether the menu should be opened for edit mode.
        /// Otherwise it will be opened for the WriteTextAction.</param>
        private void EnableTextMenu(
            UnityAction<Color> colorAction,
            Color color,
            bool reset = true,
            bool showEditMode = false)
        {
            if (reset)
            {
                Reset();
            }

            ConfigureModeControls(showEditMode);

            gameObject.SetActive(true);

            textStyleMenu.EnsureFontColorSelected();

            textStyleMenu.AssignColorArea(colorAction, color);

            MenuHelper.CalculateHeight(gameObject);
        }

        /// <summary>
        /// Configures the text menu for writing new text.
        /// </summary>
        public void EnableForWriting()
        {
            writeTextMenu.Enable();
        }

        /// <summary>
        /// Configures the text menu for editing an existing text object.
        /// </summary>
        /// <param name="selectedText">The selected text object.</param>
        /// <param name="newValueHolder">
        /// The configuration containing the editable text values.
        /// </param>
        /// <param name="returnCall">
        /// An optional callback returning to the parent menu.
        /// </param>
        public void EnableForEditing(
            GameObject selectedText,
            DrawableType newValueHolder,
            UnityAction returnCall = null)
        {
            editTextMenu.Enable(
                selectedText,
                newValueHolder,
                returnCall);
        }

        /// <summary>
        /// Returns the currently selected font styles.
        /// </summary>
        /// <returns>The combined selected font styles.</returns>
        public FontStyles GetFontStyle()
        {
            return textStyleMenu.GetFontStyle();
        }

        /// <summary>
        /// Returns whether outlining is currently enabled.
        /// </summary>
        /// <returns>True if outlining is enabled.</returns>
        public bool IsOutlineEnabled()
        {
            return textStyleMenu.IsOutlineEnabled();
        }
    }
}
