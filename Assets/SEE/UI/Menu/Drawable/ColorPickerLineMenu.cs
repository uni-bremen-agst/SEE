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
    /// This class provides a menu for the color picking of lines.
    /// </summary>
    public static class ColorPickerLineMenu
    {
        /// <summary>
        /// The location where the menu prefeb is placed.
        /// </summary>
        private const string menuPrefab = "Prefabs/UI/Drawable/ColorPickerLine";

        /// <summary>
        /// The instance for the line menu.
        /// </summary>
        private static GameObject instance;

        /// <summary>
        /// Whether this class has an color in store that wasn't yet fetched.
        /// </summary>
        private static bool gotColor;

        /// <summary>
        /// If <see cref="gotColor"/> is true, this contains the button kind which the player selected.
        /// </summary>
        private static Color chosenColor;

        /// <summary>
        /// Creates the menu and register the needed handler.
        /// </summary>
        /// <param name="line">The selected line</param>
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
        /// Initializes the primary button.
        /// </summary>
        /// <param name="conf">The line configuration.</param>
        private static void InitializePrimaryButton(LineConf conf)
        {
            GameObject primary = GameFinder.FindChild(instance, "Primary");
            SetImageColor(primary, conf.PrimaryColor);
            ButtonManagerBasic bmb = primary.GetComponent<ButtonManagerBasic>();
            bmb.clickEvent.AddListener(() =>
            {
                chosenColor = conf.PrimaryColor;
                gotColor = true;
            });
        }

        /// <summary>
        /// Initializes the secondary button.
        /// </summary>
        /// <param name="conf">The line configuration.</param>
        private static void InitializeSecondaryButton(LineConf conf)
        {
            GameObject secondary = GameFinder.FindChild(instance, "Secondary");
            SetImageColor(secondary, conf.SecondaryColor);
            ButtonManagerBasic bmb = secondary.GetComponent<ButtonManagerBasic>();
            bmb.clickEvent.AddListener(() =>
            {
                chosenColor = conf.SecondaryColor;
                gotColor = true;
            });
        }

        /// <summary>
        /// Initializes the fill-out button.
        /// </summary>
        /// <param name="conf">The line configuration.</param>
        private static void InitializeFillOutButton(LineConf conf)
        {
            GameObject fillOut = GameFinder.FindChild(instance, "FillOut");
            SetImageColor(fillOut, conf.FillOutColor);
            ButtonManagerBasic bmb = fillOut.GetComponent<ButtonManagerBasic>();
            bmb.clickEvent.AddListener(() =>
            {
                chosenColor = conf.FillOutColor;
                gotColor = true;
            });
        }

        /// <summary>
        /// Sets the <paramref name="buttonHolder"/>'s background color to <paramref name="color"/>
        /// and button text's color to the complementary color of <paramref name="color"/>.
        /// </summary>
        /// <param name="buttonHolder">The object which holds the button.</param>
        /// <param name="color">The background color.</param>
        private static void SetImageColor(GameObject buttonHolder, Color color)
        {
            buttonHolder.GetComponent<Image>().color = color;
            buttonHolder.GetComponentInChildren<TextMeshProUGUI>().color = ColorConverter.Complementary(color);
        }

        /// <summary>
        /// Destroy's the menu.
        /// </summary>
        public static void Disable()
        {
            if (instance != null)
            {
                Destroyer.Destroy(instance);
            }
        }

        /// <summary>
        /// If <see cref="gotColor"/> is true, the <paramref name="color"/> will be the chosen color by the
        /// player. Otherwise it will be some dummy value.
        /// </summary>
        /// <param name="color">The chosen color the player confirmed; if that doesn't exist,
        /// some dummy value <see cref="Color.clear"/> is used instead</param>
        /// <returns><see cref="gotColor"/></returns>
        public static bool TryGetColor(out Color color)
        {
            if (gotColor)
            {
                color = chosenColor;
                gotColor = false;
                Disable();
                return true;
            }

            color = Color.clear;
            return false;
        }
    }
}
