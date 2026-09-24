using Michsky.UI.ModernUIPack;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.StickyNote;
using SEE.GO;
using SEE.Net.Actions.Drawable;
using SEE.UI.Drawable;
using SEE.UI.Menu.Drawable.StickyNoteRotation;
using UnityEngine;
using UnityEngine.Events;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// Provides the edit menu for sticky notes.
    /// </summary>
    public class StickyNoteEditMenu : SingletonMenu
    {
        /// <summary>
        /// The prefab of the sticky note edit menu.
        /// </summary>
        private const string editMenuPrefab = "Prefabs/UI/Drawable/StickyNoteEdit";

        /// <summary>
        /// We do not want to create an instance of this singleton class outside of this class.
        /// </summary>
        private StickyNoteEditMenu() { }

        /// <summary>
        /// The only instance of this singleton class.
        /// </summary>
        public static StickyNoteEditMenu Instance { get; private set; }

        /// <summary>
        /// Initializes the singleton instance.
        /// </summary>
        static StickyNoteEditMenu()
        {
            Instance = new StickyNoteEditMenu();
        }

        /// <summary>
        /// Creates and enables the edit menu and registers the required handlers.
        /// </summary>
        /// <param name="stickyNote">The sticky note that should be edited.</param>
        /// <param name="newConfig">The configuration storing the changed values.</param>
        public void Enable(GameObject stickyNote, DrawableConfig newConfig)
        {
            /// Instantiate the menu.
            Instantiate(editMenuPrefab);

            /// Initialize the order-in-layer slider.
            InitializeLayerSlider(stickyNote, newConfig);

            /// Initialize the color picker for the sticky note color.
            InitializeColorPicker(stickyNote, newConfig);

            UnityAction returnCall = () =>
            {
                Enable();
                StickyNoteRotationMenu.Destroy();
                ScaleMenu.Instance.Destroy();
            };

            /// Initialize the edit rotation button.
            InitializeRotation(stickyNote, returnCall);

            /// Initialize the edit scale button.
            InitializeScale(stickyNote, returnCall);

            /// Initialize the lighting switch.
            InitializeLighting(stickyNote, newConfig);
        }

        /// <summary>
        /// Initializes the order-in-layer slider and registers its change handler.
        /// </summary>
        /// <param name="stickyNote">The sticky note whose order should be changed.</param>
        /// <param name="newConfig">The configuration storing the changed order.</param>
        private void InitializeLayerSlider(GameObject stickyNote, DrawableConfig newConfig)
        {
            LayerSliderController orderInLayerSlider =
                gameObject.GetComponentInChildren<LayerSliderController>(true);

            orderInLayerSlider.AssignValue(newConfig.Order);
            orderInLayerSlider.OnValueChanged.AddListener(order =>
            {
                newConfig.Order = order;
                GameStickyNoteEdit.ChangeLayer(stickyNote, order);

                new EditLayerNetAction(GameFinder.GetDrawableSurface(stickyNote).name,
                    stickyNote.name, "", order).Execute();
            });
        }

        /// <summary>
        /// Initializes the lighting switch and registers its change handlers.
        /// </summary>
        /// <param name="stickyNote">The sticky note whose lighting should be changed.</param>
        /// <param name="newConfig">The configuration storing the changed lighting state.</param>
        private void InitializeLighting(GameObject stickyNote, DrawableConfig newConfig)
        {
            SwitchManager lightingManager = GameFinder.FindAttachedOrLocalDescendant(
                gameObject, "LightningSwitch").GetComponent<SwitchManager>();

            lightingManager.OffEvents.RemoveAllListeners();
            lightingManager.OnEvents.RemoveAllListeners();

            lightingManager.OffEvents.AddListener(() =>
            {
                newConfig.Lighting = false;
                GameDrawableManager.ChangeLighting(stickyNote, false);
                new DrawableChangeLightingNetAction(newConfig).Execute();
            });

            lightingManager.OnEvents.AddListener(() =>
            {
                newConfig.Lighting = true;
                GameDrawableManager.ChangeLighting(stickyNote, true);
                new DrawableChangeLightingNetAction(newConfig).Execute();
            });

            /// Assign the current state to the switch and update its UI.
            lightingManager.isOn = newConfig.Lighting;
            lightingManager.UpdateUI();
        }

        /// <summary>
        /// Initializes the color picker and registers its change handler.
        /// </summary>
        /// <param name="stickyNote">The sticky note whose color should be changed.</param>
        /// <param name="newConfig">The configuration storing the changed color.</param>
        private void InitializeColorPicker(GameObject stickyNote, DrawableConfig newConfig)
        {
            HSVPicker.ColorPicker picker =
                gameObject.GetComponentInChildren<HSVPicker.ColorPicker>(true);

            picker.AssignColor(newConfig.Color);
            picker.onValueChanged.AddListener(color =>
            {
                newConfig.Color = color;
                GameDrawableManager.ChangeColor(stickyNote, color);
                new DrawableChangeColorNetAction(newConfig).Execute();
            });
        }

        /// <summary>
        /// Initializes the rotation button.
        /// The button opens the sticky note rotation menu.
        /// </summary>
        /// <param name="stickyNote">The sticky note whose rotation should be changed.</param>
        /// <param name="returnCall">The callback used to return to this edit menu.</param>
        private void InitializeRotation(GameObject stickyNote, UnityAction returnCall)
        {
            ButtonManagerBasic rotationButton = GameFinder.FindAttachedOrLocalDescendant(
                gameObject, "Rotation").GetComponent<ButtonManagerBasic>();

            rotationButton.clickEvent.RemoveAllListeners();
            rotationButton.clickEvent.AddListener(() =>
            {
                Disable();
                StickyNoteRotationMenu.Enable(stickyNote.GetRootParent(), null, returnCall);
            });
        }

        /// <summary>
        /// Initializes the scale button.
        /// The button opens the scale menu.
        /// </summary>
        /// <param name="stickyNote">The sticky note whose scale should be changed.</param>
        /// <param name="returnCall">The callback used to return to this edit menu.</param>
        private void InitializeScale(GameObject stickyNote, UnityAction returnCall)
        {
            ButtonManagerBasic scaleButton = GameFinder.FindAttachedOrLocalDescendant(
                gameObject, "Scale").GetComponent<ButtonManagerBasic>();

            scaleButton.clickEvent.RemoveAllListeners();
            scaleButton.clickEvent.AddListener(() =>
            {
                Disable();
                ScaleMenu.Instance.Enable(stickyNote, true, returnCall);
            });
        }
    }
}
