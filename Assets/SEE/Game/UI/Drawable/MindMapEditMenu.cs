using Michsky.UI.ModernUIPack;
using SEE.Game;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Utils;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Assets.SEE.Game.UI.Drawable
{
    /// <summary>
    /// This class provides the edit menu for mind map nodes.
    /// </summary>
    public static class MindMapEditMenu
    {
        /// <summary>
        /// The location where the menu prefeb is placed.
        /// </summary>
        private const string mmEditPrefab = "Prefabs/UI/Drawable/MMEdit";

        /// <summary>
        /// The instance for the mind map edit menu.
        /// </summary>
        private static GameObject instance;

        /// <summary>
        /// Creates the edit menu for the given mind map node.
        /// It adds the necessary handler to the different buttons.
        /// If the node is a theme, the branch line button will be inactive.
        /// </summary>
        /// <param name="node">The node that should be edit.</param>
        /// <param name="newValueHolder">The configuration which holds the changes.</param>
        public static void Enable(GameObject node, DrawableType newValueHolder)
        {
            if (newValueHolder is MindMapNodeConf conf && instance == null)
            {
                instance = PrefabInstantiator.InstantiatePrefab(mmEditPrefab,
                    GameObject.Find("UI Canvas").transform, false);

                GameObject drawable = GameFinder.GetDrawable(node);
                GameObject attached = GameFinder.GetAttachedObjectsObject(drawable);
                string drawableParentName = GameFinder.GetDrawableParentName(drawable);

                UnityAction callback = () =>
                {
                    instance.SetActive(true);
                    LineMenu.disableLineMenu();
                    TextMenu.Disable();
                    MindMapParentSelectionMenu.Disable();
                    MindMapChangeNodeKindMenu.Disable();
                };

                ButtonManagerBasic changeParent = GameFinder.FindChild(instance, "Parent").GetComponent<ButtonManagerBasic>();
                changeParent.clickEvent.AddListener(() =>
                {
                    instance.SetActive(false);
                    MindMapParentSelectionMenu.EnableForEditing(attached, node, conf, callback);
                });
                ButtonManagerBasic changeNodeKind = GameFinder.FindChild(instance, "NodeKind").GetComponent<ButtonManagerBasic>();
                changeNodeKind.clickEvent.AddListener(() =>
                {
                    instance.SetActive(false);
                    MindMapChangeNodeKindMenu.Enable(node, conf, callback);
                });
                ButtonManagerBasic changeBorder = GameFinder.FindChild(instance, "Border").GetComponent<ButtonManagerBasic>();
                changeBorder.clickEvent.AddListener(() =>
                {
                    instance.SetActive(false);
                    LineMenu.EnableForEditing(GameFinder.FindChildWithTag(node, Tags.Line), conf.borderConf, callback);
                });
                ButtonManagerBasic changeText = GameFinder.FindChild(instance, "NodeText").GetComponent<ButtonManagerBasic>();
                changeText.clickEvent.AddListener(() =>
                {
                    instance.SetActive(false);
                    TextMenu.EnableForEditing(GameFinder.FindChildWithTag(node, Tags.DText), conf.textConf, callback);
                });

                GameObject branchLineButtonArea = GameFinder.FindChild(instance, "BranchLine");
                if (conf.branchLineToParent != "")
                {
                    ButtonManagerBasic branchButton = branchLineButtonArea.GetComponent<ButtonManagerBasic>();
                    branchButton.clickEvent.AddListener(() =>
                    {
                        instance.SetActive(false);
                        GameObject bLine = GameFinder.FindChild(attached, conf.branchLineToParent);
                        LineMenu.EnableForEditing(bLine, conf.branchLineConf, callback);
                    });
                } else
                {
                    branchLineButtonArea.GetComponent<ButtonManagerBasic>().enabled = false;
                    branchLineButtonArea.GetComponent<Button>().interactable = false;
                }
            }
        }

        /// <summary>
        /// Destroys the edit menu and disables the parent selection and
        /// node kind menu.
        /// </summary>
        public static void Disable()
        {
            if (instance != null)
            {
                Destroyer.Destroy(instance);
            }
            MindMapParentSelectionMenu.Disable();
            MindMapChangeNodeKindMenu.Disable();
        }
    }
}