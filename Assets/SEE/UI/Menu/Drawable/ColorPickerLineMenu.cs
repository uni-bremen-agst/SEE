using Michsky.UI.ModernUIPack;
using SEE.Game.Drawable;
using SEE.Game.Drawable.ActionHelpers;
using SEE.Game.Drawable.Configurations;
using SEE.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// Provides a menu for picking colors from lines.
    /// </summary>
    public static class ColorPickerLineMenu
    {
        /// <summary>
        /// The location where the menu prefab is placed.
        /// </summary>
        private const string menuPrefab = "Prefabs/UI/Drawable/ColorPickerLine";

        /// <summary>
        /// The current line color picker menu instance.
        /// </summary>
        private static GameObject instance;

        /// <summary>
        /// Whether a selected color is waiting to be consumed.
        /// </summary>
        private static bool gotColor;

        /// <summary>
        /// The selected color if <see cref="gotColor"/> is true.
        /// </summary>
        private static Color chosenColor;

        /// <summary>
        /// Creates the menu for the selected line and registers the required handlers.
        /// </summary>
        /// <param name="line">The selected line.</param>
        public static void Enable(GameObject line)
        {
            if (instance == null)
            {
                LineConf conf = LineConf.GetLine(line);
                instance = PrefabInstantiator.InstantiatePrefab(menuPrefab, UICanvas.Canvas.transform, false);

                /// Initializes the buttons.
                InitializePrimaryButton(conf);
                InitializeSecondaryButton(conf);
                InitializeFillOutButton(conf);
            }
        }

        /// <summary>
        /// Initializes the primary color button.
        /// </summary>
        /// <param name="conf">The line configuration.</param>
        private static void InitializePrimaryButton(LineConf conf)
        {
            GameObject primary = GameFinder.FindAttachedOrLocalDescendant(instance, "Primary");
            SetImageColor(primary, conf.PrimaryColor);

            ButtonManagerBasic button = primary.GetComponent<ButtonManagerBasic>();
            button.clickEvent.AddListener(() =>
            {
                chosenColor = conf.PrimaryColor;
                gotColor = true;
            });
        }

        /// <summary>
        /// Initializes the secondary color button.
        /// </summary>
        /// <param name="conf">The line configuration.</param>
        private static void InitializeSecondaryButton(LineConf conf)
        {
            GameObject secondary = GameFinder.FindAttachedOrLocalDescendant(instance, "Secondary");
            SetImageColor(secondary, conf.SecondaryColor);

            ButtonManagerBasic button = secondary.GetComponent<ButtonManagerBasic>();
            button.clickEvent.AddListener(() =>
            {
                chosenColor = conf.SecondaryColor;
                gotColor = true;
            });
        }

        /// <summary>
        /// Initializes the fill-out color button.
        /// </summary>
        /// <param name="conf">The line configuration.</param>
        private static void InitializeFillOutButton(LineConf conf)
        {
            GameObject fillOut = GameFinder.FindAttachedOrLocalDescendant(instance, "FillOut");
            SetImageColor(fillOut, conf.FillOutColor);

            ButtonManagerBasic button = fillOut.GetComponent<ButtonManagerBasic>();
            button.clickEvent.AddListener(() =>
            {
                chosenColor = conf.FillOutColor;
                gotColor = true;
            });
        }

        /// <summary>
        /// Sets the background color of <paramref name="buttonHolder"/> to <paramref name="color"/>
        /// and its text color to the complementary color.
        /// </summary>
        /// <param name="buttonHolder">The object holding the button.</param>
        /// <param name="color">The background color.</param>
        private static void SetImageColor(GameObject buttonHolder, Color color)
        {
            buttonHolder.GetComponent<Image>().color = color;
            buttonHolder.GetComponentInChildren<TextMeshProUGUI>().color = ColorConverter.Complementary(color);
        }

        /// <summary>
        /// Destroys the menu and clears any pending color selection.
        /// </summary>
        public static void Disable()
        {
            if (instance != null)
            {
                Destroyer.Destroy(instance);
            }

            instance = null;
            gotColor = false;
            chosenColor = Color.clear;
        }

        /// <summary>
        /// Tries to consume the color selected by the player.
        /// </summary>
        /// <param name="color">
        /// The selected color if one is available; otherwise <see cref="Color.clear"/>.
        /// </param>
        /// <returns>True if a selected color was available; otherwise false.</returns>
        public static bool TryGetColor(out Color color)
        {
            if (gotColor)
            {
                color = chosenColor;
                Disable();
                return true;
            }

            color = Color.clear;
            return false;
        }
    }
}
