using Michsky.UI.ModernUIPack;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.Game.UI.Menu.Drawable
{
    /// <summary>
    /// This class provides a menu for the color picking of mind map nodes.
    /// </summary>
    public static class ColorPickerMindMapMenu
    {
        /// <summary>
        /// The location where the menu prefeb is placed.
        /// </summary>
        private const string menuPrefab = "Prefabs/UI/Drawable/ColorPickerMindMap";

        /// <summary>
        /// The instance for the mind map menu.
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
        /// <param name="node">The selected node</param>
        /// <param name="primaryColor">Indicates whether the primary or secondary color should be chosen</param>
        public static void Enable(GameObject node, bool primaryColor)
        {
            if (instance == null)
            {
                MindMapNodeConf conf = MindMapNodeConf.GetNodeConf(node);
                instance = PrefabInstantiator.InstantiatePrefab(menuPrefab,
                    GameObject.Find("UI Canvas").transform, false);

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
            ButtonManagerBasic border = GameFinder.FindChild(instance, "Border").GetComponent<ButtonManagerBasic>();
            border.clickEvent.AddListener(() =>
            {
                if (primaryColor)
                {
                    chosenColor = conf.borderConf.primaryColor;
                }
                else
                {
                    chosenColor = conf.borderConf.secondaryColor;
                    /// For monochromatic lines, the secondary color is clear; 
                    /// therefore, the primary color is taken instead.
                    if (conf.borderConf.colorKind == GameDrawer.ColorKind.Monochrome)
                    {
                        chosenColor = conf.borderConf.primaryColor;
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
            ButtonManagerBasic text = GameFinder.FindChild(instance, "NodeText").GetComponent<ButtonManagerBasic>();
            text.clickEvent.AddListener(() =>
            {
                if (primaryColor)
                {
                    chosenColor = conf.textConf.fontColor;
                }
                else
                {
                    chosenColor = conf.textConf.outlineColor;
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
            GameObject branchLineButtonArea = GameFinder.FindChild(instance, "BranchLine");
            if (conf.branchLineToParent != "")
            {
                ButtonManagerBasic branchButton = branchLineButtonArea.GetComponent<ButtonManagerBasic>();
                branchButton.clickEvent.AddListener(() =>
                {
                    if (primaryColor)
                    {
                        chosenColor = conf.branchLineConf.primaryColor;
                    }
                    else
                    {
                        chosenColor = conf.branchLineConf.secondaryColor;
                        /// For monochromatic lines, the secondary color is clear; 
                        /// therefore, the primary color is taken instead.
                        if (conf.branchLineConf.colorKind == GameDrawer.ColorKind.Monochrome)
                        {
                            chosenColor = conf.branchLineConf.primaryColor;
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
        /// <param name="color">The chosen color the player confirmed, if that doesn't exist, some dummy value</param>
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

            color = Color.black;
            return false;
        }
    }
}