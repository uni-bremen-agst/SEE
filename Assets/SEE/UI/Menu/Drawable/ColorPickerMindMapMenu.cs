using Michsky.UI.ModernUIPack;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// Provides a menu for picking colors from Mind Map nodes.
    /// </summary>
    public class ColorPickerMindMapMenu : SingletonMenu
    {
        /// <summary>
        /// The location where the menu prefab is placed.
        /// </summary>
        private const string menuPrefab = "Prefabs/UI/Drawable/ColorPickerMindMap";

        /// <summary>
        /// We do not want to create an instance of this singleton class outside of this class.
        /// </summary>
        private ColorPickerMindMapMenu() { }

        /// <summary>
        /// The only instance of this singleton class.
        /// </summary>
        public static ColorPickerMindMapMenu Instance { get; private set; }

        /// <summary>
        /// Whether this menu has a chosen color that has not yet been consumed.
        /// </summary>
        private bool gotColor;

        /// <summary>
        /// The color selected by the player when <see cref="gotColor"/> is true.
        /// </summary>
        private Color chosenColor;

        /// <summary>
        /// Initializes the singleton instance.
        /// </summary>
        static ColorPickerMindMapMenu()
        {
            Instance = new ColorPickerMindMapMenu();
        }

        /// <summary>
        /// Creates the menu for the given Mind Map node and registers the required handlers.
        /// </summary>
        /// <param name="node">The selected Mind Map node.</param>
        /// <param name="primaryColor">
        /// Whether the primary or secondary color should be selected.
        /// </param>
        public void Enable(GameObject node, bool primaryColor)
        {
            if (gameObject == null)
            {
                Enable(MindMapNodeConf.GetNodeConf(node), primaryColor);
            }
        }

        /// <summary>
        /// Creates the menu for the given Mind Map node configuration and registers the required handlers.
        /// </summary>
        /// <param name="conf">The configuration of the selected Mind Map node.</param>
        /// <param name="primaryColor">
        /// Whether the primary or secondary color should be selected.
        /// </param>
        /// <remarks>
        /// This overload separates configuration based menu setup from scene object lookup
        /// and also allows the menu behavior to be tested independently.
        /// </remarks>
        internal void Enable(MindMapNodeConf conf, bool primaryColor)
        {
            if (gameObject != null)
            {
                return;
            }

            gotColor = false;
            chosenColor = Color.clear;

            Instantiate(menuPrefab);

            /// Initialize the button to obtain the color of the border.
            InitializeColorOfBorderButton(conf, primaryColor);

            /// Initialize the button to obtain the color of the text.
            InitializeColorOfTextButton(conf, primaryColor);

            /// Initialize the button to obtain the color of the branch line.
            /// If there is no branch line for the node, this button becomes inactive.
            InitializeColorOfBranchLine(conf, primaryColor);
        }

        /// <summary>
        /// Initializes the button used to obtain the border color.
        /// The primary color is selected when <paramref name="primaryColor"/> is true.
        /// Otherwise the secondary color is selected. Monochrome lines use their primary color.
        /// </summary>
        /// <param name="conf">The configuration of the selected node.</param>
        /// <param name="primaryColor">Whether the primary color should be selected.</param>
        private void InitializeColorOfBorderButton(MindMapNodeConf conf, bool primaryColor)
        {
            ButtonManagerBasic border = GameFinder.FindAttachedOrLocalDescendant(gameObject, "Border").GetComponent<ButtonManagerBasic>();
            border.clickEvent.AddListener(() =>
            {
                chosenColor = GetLineColor(conf.BorderConf, primaryColor);
                gotColor = true;
            });
        }

        /// <summary>
        /// Initializes the button used to obtain the text color.
        /// The font color is selected when <paramref name="primaryColor"/> is true.
        /// Otherwise the outline color is selected.
        /// </summary>
        /// <param name="conf">The configuration of the selected node.</param>
        /// <param name="primaryColor">Whether the primary color should be selected.</param>
        private void InitializeColorOfTextButton(MindMapNodeConf conf, bool primaryColor)
        {
            ButtonManagerBasic text = GameFinder.FindAttachedOrLocalDescendant(gameObject, "NodeText").GetComponent<ButtonManagerBasic>();
            text.clickEvent.AddListener(() =>
            {
                chosenColor = primaryColor ? conf.TextConf.FontColor : conf.TextConf.OutlineColor;
                gotColor = true;
            });
        }

        /// <summary>
        /// Initializes the button used to obtain the branch-line color.
        /// If the node has no parent branch line, the button is disabled.
        /// </summary>
        /// <param name="conf">The configuration of the selected node.</param>
        /// <param name="primaryColor">Whether the primary color should be selected.</param>
        private void InitializeColorOfBranchLine(MindMapNodeConf conf, bool primaryColor)
        {
            GameObject branchLineButtonArea = GameFinder.FindAttachedOrLocalDescendant(gameObject, "BranchLine");

            /// Checks if the node has a parent. If not, this area will be disabled.
            if (conf.BranchLineToParent != "")
            {
                ButtonManagerBasic branchButton = branchLineButtonArea.GetComponent<ButtonManagerBasic>();
                branchButton.clickEvent.AddListener(() =>
                {
                    chosenColor = GetLineColor(conf.BranchLineConf, primaryColor);
                    gotColor = true;
                });
            }
            else
            {
                /// If the node has no parent, there is consequently no branch line to the parent.
                /// Therefore, in this case, the button will be disabled.
                branchLineButtonArea.GetComponent<ButtonManagerBasic>().enabled = false;
                branchLineButtonArea.GetComponent<Button>().interactable = false;
            }
        }

        /// <summary>
        /// Gets the requested color of the given line configuration.
        /// Monochrome lines always use their primary color.
        /// </summary>
        /// <param name="conf">The line configuration whose color should be obtained.</param>
        /// <param name="primaryColor">Whether the primary color should be selected.</param>
        /// <returns>The requested line color.</returns>
        private static Color GetLineColor(LineConf conf, bool primaryColor)
        {
            if (primaryColor || conf.ColorKind == GameDrawer.ColorKind.Monochrome)
            {
                return conf.PrimaryColor;
            }

            return conf.SecondaryColor;
        }

        /// <summary>
        /// Tries to consume the color selected by the player.
        /// </summary>
        /// <param name="color">
        /// The selected color if one is available; otherwise <see cref="Color.clear"/>.
        /// </param>
        /// <returns>True if a selected color was available; otherwise false.</returns>
        public bool TryGetColor(out Color color)
        {
            if (gotColor)
            {
                color = chosenColor;
                gotColor = false;
                chosenColor = Color.clear;
                Destroy();
                return true;
            }

            color = Color.clear;
            return false;
        }

        /// <summary>
        /// Destroys the menu and clears any pending color selection.
        /// </summary>
        public override void Destroy()
        {
            base.Destroy();
            gotColor = false;
            chosenColor = Color.clear;
        }
    }
}
