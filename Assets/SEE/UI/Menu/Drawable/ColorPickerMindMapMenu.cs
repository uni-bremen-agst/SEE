using Michsky.UI.ModernUIPack;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// This singleton class provides a menu for the color picking of mind-map nodes.
    /// </summary>
    public class ColorPickerMindMapMenu : SingletonMenu
    {
        /// <summary>
        /// The location where the menu prefeb is placed.
        /// </summary>
        private const string menuPrefab = "Prefabs/UI/Drawable/ColorPickerMindMap";

        /// <summary>
        /// We do not want to create an instance of this singleton class outside of this class.
        /// </summary>
        private ColorPickerMindMapMenu() { }

        /// <summary>
        /// The only instance of this singleton class.
        /// </summary>
        public static ColorPickerMindMapMenu Instance { get; set; }

        static ColorPickerMindMapMenu()
        {
            Instance = new ColorPickerMindMapMenu();
        }

        /// <summary>
        /// Whether this class has a color in store that has not been fetched yet.
        /// </summary>
        private static bool gotColor;

        /// <summary>
        /// If <see cref="gotColor"/> is true, this contains the button kind which the player selected.
        /// </summary>
        private static Color chosenColor;

        /// <summary>
        /// Creates the menu and registers the needed handler.
        /// </summary>
        /// <param name="node">The selected node.</param>
        /// <param name="primaryColor">Indicates whether the primary or secondary color should be chosen.</param>
        public static void Enable(GameObject node, bool primaryColor)
        {
            if (Instance.gameObject == null)
            {
                MindMapNodeConf conf = MindMapNodeConf.GetNodeConf(node);
                Instance.Instantiate(menuPrefab);

                /// Initialize the button to obtain the color of the border.
                InitializeColorOfBorderButton(conf, primaryColor);

                /// Initialize the button to obtain the color of the text.
                InitializeColorOfTextButton(conf, primaryColor);

                /// Initialize the button to obtain the color of the branch line.
                /// If there is no branch line for the node, this button becomes inactive.
                InitializeColorOfBranchLine(conf, primaryColor);
            }
        }

        /// <summary>
        /// Initialize the button to obtain the color of the border.
        /// Selects the color based on the <paramref name="primaryColor"/>.
        /// If it is true, the primary color is chosen.
        /// Otherwise, the secondary color is chosen.
        ///     (For monochromatic lines, the primary color is also chosen.)
        /// </summary>
        /// <param name="conf">The configuration of the selected node.</param>
        /// <param name="primaryColor">Option whether the primary color is being sought.</param>
        private static void InitializeColorOfBorderButton(MindMapNodeConf conf, bool primaryColor)
        {
            ButtonManagerBasic border = GameFinder.FindChild(Instance.gameObject, "Border").GetComponent<ButtonManagerBasic>();
            border.clickEvent.AddListener(() =>
            {
                if (primaryColor)
                {
                    chosenColor = conf.BorderConf.PrimaryColor;
                }
                else
                {
                    chosenColor = conf.BorderConf.SecondaryColor;
                    /// For monochromatic lines, the secondary color is clear;
                    /// therefore, the primary color is taken instead.
                    if (conf.BorderConf.ColorKind == GameDrawer.ColorKind.Monochrome)
                    {
                        chosenColor = conf.BorderConf.PrimaryColor;
                    }
                }
                gotColor = true;
            });
        }

        /// <summary>
        /// Initialize the button to obtain the color of the text.
        /// Selects the color based on the <paramref name="primaryColor"/>.
        /// If it is true, the primary color is chosen.
        /// Otherwise, the secondary (outline) color is chosen.
        /// </summary>
        /// <param name="conf">The configuration of the selected node.</param>
        /// <param name="primaryColor">Option whether the primary color is being sought.</param>
        private static void InitializeColorOfTextButton(MindMapNodeConf conf, bool primaryColor)
        {
            ButtonManagerBasic text = GameFinder.FindChild(Instance.gameObject, "NodeText").GetComponent<ButtonManagerBasic>();
            text.clickEvent.AddListener(() =>
            {
                if (primaryColor)
                {
                    chosenColor = conf.TextConf.FontColor;
                }
                else
                {
                    chosenColor = conf.TextConf.OutlineColor;
                }
                gotColor = true;
            });
        }

        /// <summary>
        /// Initialize the button to obtain the color of the branch line.
        /// Selects the color based on the <paramref name="primaryColor"/>.
        /// If it is true, the primary color is chosen.
        /// Otherwise, the secondary color is chosen.
        ///     (For monochromatic lines, the primary color is also chosen.)
        /// </summary>
        /// <param name="conf">The configuration of the selected node.</param>
        /// <param name="primaryColor">Option whether the primary color is being sought.</param>
        private static void InitializeColorOfBranchLine(MindMapNodeConf conf, bool primaryColor)
        {
            /// Checks if the node has a parent. If not this area will be disabled.
            GameObject branchLineButtonArea = GameFinder.FindChild(Instance.gameObject, "BranchLine");
            if (conf.BranchLineToParent != "")
            {
                ButtonManagerBasic branchButton = branchLineButtonArea.GetComponent<ButtonManagerBasic>();
                branchButton.clickEvent.AddListener(() =>
                {
                    if (primaryColor)
                    {
                        chosenColor = conf.BranchLineConf.PrimaryColor;
                    }
                    else
                    {
                        chosenColor = conf.BranchLineConf.SecondaryColor;
                        /// For monochromatic lines, the secondary color is clear;
                        /// therefore, the primary color is taken instead.
                        if (conf.BranchLineConf.ColorKind == GameDrawer.ColorKind.Monochrome)
                        {
                            chosenColor = conf.BranchLineConf.PrimaryColor;
                        }
                    }
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
        /// If <see cref="gotColor"/> is true, the <paramref name="color"/> will be the chosen color by the
        /// player. Otherwise it will be some dummy value.
        /// </summary>
        /// <param name="color">The chosen color the player confirmed, if that doesn't exist, some dummy value.</param>
        /// <returns><see cref="gotColor"/>.</returns>
        public static bool TryGetColor(out Color color)
        {
            if (gotColor)
            {
                color = chosenColor;
                gotColor = false;
                Instance.Destroy();
                return true;
            }

            color = Color.clear;
            return false;
        }
    }
}
