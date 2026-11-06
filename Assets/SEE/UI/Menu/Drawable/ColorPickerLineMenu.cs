using SEE.Game.Drawable.Line;
using Michsky.UI.ModernUIPack;
using SEE.Game;
using SEE.Game.Drawable;
using SEE.Game.Drawable.ActionHelpers;
using SEE.Game.Drawable.Configurations;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// Identifies the visual part of a line from which a color should be picked.
    /// </summary>
    internal enum LineColorTarget
    {
        /// <summary>
        /// The main line.
        /// </summary>
        Main,

        /// <summary>
        /// The start line cap.
        /// </summary>
        StartCap,

        /// <summary>
        /// The end line cap.
        /// </summary>
        EndCap
    }

    /// <summary>
    /// Provides the color selection workflow for lines and their line caps.
    /// </summary>
    public class ColorPickerLineMenu : SingletonMenu
    {
        /// <summary>
        /// The location where the line color picker menu prefab is placed.
        /// </summary>
        private const string menuPrefab = "Prefabs/UI/Drawable/ColorPickerLine";

        /// <summary>
        /// We do not want to create an instance of this singleton class outside of this class.
        /// </summary>
        private ColorPickerLineMenu() { }

        /// <summary>
        /// The only instance of this singleton class.
        /// </summary>
        public static ColorPickerLineMenu Instance { get; private set; }

        /// <summary>
        /// Whether a selected color is waiting to be consumed.
        /// </summary>
        private bool gotColor;

        /// <summary>
        /// The selected color if <see cref="gotColor"/> is true.
        /// </summary>
        private Color chosenColor;

        /// <summary>
        /// Initializes the singleton instance.
        /// </summary>
        static ColorPickerLineMenu()
        {
            Instance = new ColorPickerLineMenu();
        }

        /// <summary>
        /// Starts a line color selection if <paramref name="hitObject"/> belongs to a line.
        /// Direct hits on a start or end cap skip the line-part selection.
        /// </summary>
        /// <param name="hitObject">The object hit by the color picker raycast.</param>
        /// <param name="primaryColor">
        /// Whether the primary color should be selected if no explicit color selection is required.
        /// </param>
        /// <returns>
        /// True if the hit belongs to a line and the color selection was started;
        /// otherwise, false.
        /// </returns>
        public bool TryBeginSelection(GameObject hitObject, bool primaryColor)
        {
            if (hitObject == null)
            {
                return false;
            }

            GameObject line = GameFinder.GetDrawableTypObject(hitObject);
            if (line == null || !line.CompareTag(Tags.Line))
            {
                return false;
            }

            LineConf conf = LineConf.GetLine(line);
            if (conf == null)
            {
                return false;
            }

            LineColorTarget? directTarget = ResolveDirectTarget(hitObject, line);
            BeginSelection(conf, primaryColor, directTarget);
            return true;
        }

        /// <summary>
        /// Starts the color selection for the given line configuration.
        /// </summary>
        /// <param name="conf">The line configuration whose color should be selected.</param>
        /// <param name="primaryColor">
        /// Whether the primary color should be selected if no explicit color selection is required.
        /// </param>
        /// <param name="directTarget">
        /// A line part that was selected directly, or null if the player selected the main line.
        /// </param>
        /// <remarks>
        /// This overload separates the scene-object lookup from the selection logic
        /// and allows the menu behavior to be tested independently.
        /// </remarks>
        internal void BeginSelection(LineConf conf, bool primaryColor, LineColorTarget? directTarget = null)
        {
            if (conf == null)
            {
                throw new ArgumentNullException(nameof(conf));
            }

            if (gameObject != null || gotColor)
            {
                return;
            }

            chosenColor = Color.clear;

            if (directTarget.HasValue)
            {
                ILineVisualConf directConf = GetVisualConf(conf, directTarget.Value);
                if (directConf != null)
                {
                    BeginColorSelection(directConf, primaryColor);
                    return;
                }
            }

            if (HasAnyLineCap(conf))
            {
                ShowTargetSelection(conf, primaryColor);
            }
            else
            {
                BeginColorSelection(conf, primaryColor);
            }
        }

        /// <summary>
        /// Determines whether the hit occurred on a generated start or end line cap.
        /// Nested cap objects such as fill-out objects are also recognized.
        /// </summary>
        /// <param name="hitObject">The object hit by the raycast.</param>
        /// <param name="line">The owning line.</param>
        /// <returns>
        /// The directly selected cap target, or null if the main line was selected.
        /// </returns>
        internal static LineColorTarget? ResolveDirectTarget(GameObject hitObject, GameObject line)
        {
            if (hitObject == null || line == null)
            {
                return null;
            }

            Transform current = hitObject.transform;

            while (current != null && current.gameObject != line)
            {
                if (current.name.StartsWith(ValueHolder.LineStartCapPrefix, StringComparison.Ordinal))
                {
                    return LineColorTarget.StartCap;
                }

                if (current.name.StartsWith(ValueHolder.LineEndCapPrefix, StringComparison.Ordinal))
                {
                    return LineColorTarget.EndCap;
                }

                current = current.parent;
            }

            return null;
        }

        /// <summary>
        /// Displays the selection of the main line, start cap, and end cap.
        /// Missing line caps are disabled.
        /// </summary>
        /// <param name="conf">The selected line configuration.</param>
        /// <param name="primaryColor">
        /// Whether the primary or secondary color should be represented by the target buttons.
        /// </param>
        private void ShowTargetSelection(LineConf conf, bool primaryColor)
        {
            EnsureMenu();

            bool hasStartCap = HasLineCap(conf.LineCapStart);
            bool hasEndCap = HasLineCap(conf.LineCapEnd);

            ConfigureButton(
                "Primary",
                "Main",
                GetRequestedColor(conf, primaryColor),
                true,
                () => BeginColorSelection(conf, primaryColor));

            ConfigureButton(
                "Secondary",
                "Start Cap",
                hasStartCap
                    ? GetRequestedColor(conf.LineCapStart, primaryColor)
                    : Color.gray,
                hasStartCap,
                () => BeginColorSelection(conf.LineCapStart, primaryColor));

            ConfigureButton(
                "FillOut",
                "End Cap",
                hasEndCap
                    ? GetRequestedColor(conf.LineCapEnd, primaryColor)
                    : Color.gray,
                hasEndCap,
                () => BeginColorSelection(conf.LineCapEnd, primaryColor));
        }

        /// <summary>
        /// Selects a color immediately if the visual configuration has no fill-out.
        /// Otherwise, an explicit color selection is displayed because the fill-out
        /// adds another selectable color.
        /// </summary>
        /// <param name="conf">The visual line configuration.</param>
        /// <param name="primaryColor">Whether the primary color is requested.</param>
        private void BeginColorSelection(ILineVisualConf conf, bool primaryColor)
        {
            if (conf == null)
            {
                return;
            }

            if (!conf.FillOutStatus)
            {
                chosenColor = GetRequestedColor(conf, primaryColor);
                gotColor = true;
                return;
            }

            ShowColorSelection(conf);
        }

        /// <summary>
        /// Displays all colors that can explicitly be selected from the given visual configuration.
        /// </summary>
        /// <param name="conf">The visual line configuration.</param>
        private void ShowColorSelection(ILineVisualConf conf)
        {
            EnsureMenu();

            bool hasSecondaryColor = conf.ColorKind != ColorKind.Monochrome;

            ConfigureButton(
                "Primary",
                "Primary",
                conf.PrimaryColor,
                true,
                () => SelectColor(conf.PrimaryColor));

            ConfigureButton(
                "Secondary",
                "Secondary",
                hasSecondaryColor ? conf.SecondaryColor : Color.gray,
                hasSecondaryColor,
                () => SelectColor(conf.SecondaryColor));

            ConfigureButton(
                "FillOut",
                "Fill Out",
                conf.FillOutStatus ? conf.FillOutColor : Color.gray,
                conf.FillOutStatus,
                () => SelectColor(conf.FillOutColor));
        }

        /// <summary>
        /// Ensures that the line color picker menu is instantiated.
        /// </summary>
        private void EnsureMenu()
        {
            if (gameObject == null)
            {
                Instantiate(menuPrefab);
            }
        }

        /// <summary>
        /// Configures one of the three reusable buttons of the line color picker menu.
        /// </summary>
        /// <param name="name">The game-object name of the button.</param>
        /// <param name="label">The label displayed by the button.</param>
        /// <param name="color">The background color displayed by the button.</param>
        /// <param name="enabled">Whether the button can be selected.</param>
        /// <param name="action">The action executed when the button is selected.</param>
        private void ConfigureButton(string name, string label, Color color, bool enabled, UnityAction action)
        {
            GameObject buttonObject = GameFinder.FindAttachedOrLocalDescendant(gameObject, name);
            ButtonManagerBasic manager = buttonObject.GetComponent<ButtonManagerBasic>();
            Button button = buttonObject.GetComponent<Button>();
            TextMeshProUGUI text = buttonObject.GetComponentInChildren<TextMeshProUGUI>();

            manager.clickEvent.RemoveAllListeners();
            manager.buttonText = label;
            manager.enabled = enabled;
            button.interactable = enabled;

            buttonObject.GetComponent<Image>().color = color;

            if (text != null)
            {
                text.text = label;
                text.color = GetReadableTextColor(color);
            }

            if (enabled && action != null)
            {
                manager.clickEvent.AddListener(action);
            }
        }

        /// <summary>
        /// Returns either black or white text depending on which one provides
        /// better contrast against the given background color.
        /// </summary>
        /// <param name="backgroundColor">The background color of the button.</param>
        /// <returns>
        /// Black for bright backgrounds and white for dark backgrounds.
        /// </returns>
        internal static Color GetReadableTextColor(Color backgroundColor)
        {
            float luminance =
                0.2126f * backgroundColor.r
                + 0.7152f * backgroundColor.g
                + 0.0722f * backgroundColor.b;

            return luminance > 0.5f
                ? Color.black
                : Color.white;
        }

        /// <summary>
        /// Stores the selected color until it is consumed by the color picker action.
        /// </summary>
        /// <param name="color">The selected color.</param>
        private void SelectColor(Color color)
        {
            chosenColor = color;
            gotColor = true;
        }

        /// <summary>
        /// Gets the requested color of the given visual line configuration.
        /// Monochrome configurations always use their primary color.
        /// </summary>
        /// <param name="conf">The visual configuration whose color should be returned.</param>
        /// <param name="primaryColor">Whether the primary color is requested.</param>
        /// <returns>The requested visible color.</returns>
        private static Color GetRequestedColor(ILineVisualConf conf, bool primaryColor)
        {
            if (primaryColor || conf.ColorKind == ColorKind.Monochrome)
            {
                return conf.PrimaryColor;
            }

            return conf.SecondaryColor;
        }

        /// <summary>
        /// Gets the visual configuration of the requested line part.
        /// </summary>
        /// <param name="conf">The complete line configuration.</param>
        /// <param name="target">The requested line part.</param>
        /// <returns>
        /// The visual configuration of the requested part, or null if the requested cap does not exist.
        /// </returns>
        private static ILineVisualConf GetVisualConf(LineConf conf, LineColorTarget target)
        {
            return target switch
            {
                LineColorTarget.Main => conf,
                LineColorTarget.StartCap when HasLineCap(conf.LineCapStart) => conf.LineCapStart,
                LineColorTarget.EndCap when HasLineCap(conf.LineCapEnd) => conf.LineCapEnd,
                _ => null
            };
        }

        /// <summary>
        /// Returns whether the given line contains at least one line cap.
        /// </summary>
        /// <param name="conf">The line configuration.</param>
        /// <returns>True if a start or end cap exists; otherwise, false.</returns>
        private static bool HasAnyLineCap(LineConf conf)
        {
            return HasLineCap(conf.LineCapStart) || HasLineCap(conf.LineCapEnd);
        }

        /// <summary>
        /// Returns whether the given line-cap configuration represents an existing cap.
        /// </summary>
        /// <param name="conf">The line-cap configuration.</param>
        /// <returns>True if the cap exists; otherwise, false.</returns>
        private static bool HasLineCap(LineCapConf conf)
        {
            return conf != null
                && conf.CapKind != LineCapPointsCalculator.LineCap.None;
        }

        /// <summary>
        /// Tries to consume the color selected by the player.
        /// </summary>
        /// <param name="color">
        /// The selected color if one is available; otherwise <see cref="Color.clear"/>.
        /// </param>
        /// <returns>True if a selected color was available; otherwise, false.</returns>
        public bool TryGetColor(out Color color)
        {
            if (gotColor)
            {
                Color selectedColor = chosenColor;
                Destroy();
                color = selectedColor;
                return true;
            }

            color = Color.clear;
            return false;
        }

        /// <summary>
        /// Destroys the helper menu and clears any pending color selection.
        /// </summary>
        public override void Destroy()
        {
            base.Destroy();
            gotColor = false;
            chosenColor = Color.clear;
        }
    }
}
