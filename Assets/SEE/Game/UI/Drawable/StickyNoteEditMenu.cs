using Assets.SEE.Game.Drawable;
using Michsky.UI.ModernUIPack;
using SEE.Controls.Actions.Drawable;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Net.Actions.Drawable;
using SEE.Utils;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Assets.SEE.Game.UI.Drawable
{
    /// <summary>
    /// This class provides the edit menu for sticky notes.
    /// </summary>
    public static class StickyNoteEditMenu
    {
        /// <summary>
        /// The prefab of the sticky note edit menu.
        /// </summary>
        private const string editMenuPrefab = "Prefabs/UI/Drawable/StickyNoteEdit";

        /// <summary>
        /// The instance for the sticky note edit menu.
        /// </summary>
        private static GameObject instance;

        /// <summary>
        /// Destroys the menu.
        /// </summary>
        public static void Disable()
        {
            if (instance != null)
            {
                Destroyer.Destroy(instance);
            }
        }

        /// <summary>
        /// Create and enables the edit menu.
        /// It add's the necressary Handler to the GUI areas.
        /// </summary>
        public static void Enable(GameObject stickyNote, DrawableConfig newConfig)
        {
            instance = PrefabInstantiator.InstantiatePrefab(editMenuPrefab,
                GameObject.Find("UI Canvas").transform, false);
            LayerSliderController orderInLayerSlider = instance.GetComponentInChildren<LayerSliderController>();
            orderInLayerSlider.AssignValue(newConfig.Order);
            orderInLayerSlider.onValueChanged.AddListener(order =>
            {
                newConfig.Order = order;
                GameStickyNoteManager.ChangeLayer(stickyNote, order);
                new EditLayerNetAction(GameFinder.FindDrawable(stickyNote).name, stickyNote.name, "", order).Execute();
            });

            HSVPicker.ColorPicker picker = instance.GetComponentInChildren<HSVPicker.ColorPicker>();
            picker.AssignColor(newConfig.Color);
            picker.onValueChanged.AddListener(color =>
            {
                newConfig.Color = color;
                GameStickyNoteManager.ChangeColor(stickyNote, color);
                new StickyNoteChangeColorNetAction(newConfig).Execute();
            });

            ButtonManagerBasic rotation = GameFinder.FindChild(instance, "Rotation").GetComponent<ButtonManagerBasic>();
            rotation.clickEvent.AddListener(() => 
            {
                StickyNoteRotationMenu.Enable(GameFinder.GetHighestParent(stickyNote));
                Disable();
            });

            ButtonManagerBasic scale = GameFinder.FindChild(instance, "Scale").GetComponent<ButtonManagerBasic>();
            scale.clickEvent.AddListener(() =>
            {
                ScaleMenu.Enable(stickyNote, true);
                Disable();
            });
        }
    }
}