using Assets.SEE.Game.Drawable;
using Michsky.UI.ModernUIPack;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.SEE.Game.UI.Drawable
{
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

                ButtonManagerBasic border = GameFinder.FindChild(instance, "Border").GetComponent<ButtonManagerBasic>();
                border.clickEvent.AddListener(() =>
                {
                    if (primaryColor)
                    {
                        chosenColor = conf.borderConf.primaryColor;
                    } else
                    {
                        chosenColor = conf.borderConf.secondaryColor;
                        if (conf.borderConf.colorKind == GameDrawer.ColorKind.Monochrome)
                        {
                            chosenColor = conf.borderConf.primaryColor;
                        }
                    }
                    gotColor = true;
                });
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
                    branchLineButtonArea.GetComponent<ButtonManagerBasic>().enabled = false;
                    branchLineButtonArea.GetComponent<Button>().interactable = false;
                }
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