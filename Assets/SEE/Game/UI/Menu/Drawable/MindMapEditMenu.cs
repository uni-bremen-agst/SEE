using Michsky.UI.ModernUIPack;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Utils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SEE.Game.UI.Menu.Drawable
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
        /// Gets the state if the menu is already opened.
        /// </summary>
        /// <returns>true, if the menu is alreay opened. Otherwise false.</returns>
        public static bool IsOpen()
        {
            return instance != null;
        }

        /// <summary>
        /// Creates the edit menu for the given mind map node.
        /// It adds the necessary handler to the different buttons.
        /// If the node is a theme, the branch line button will be inactive.
        /// </summary>
        /// <param name="node">The node that should be edit.</param>
        /// <param name="newValueHolder">The configuration which holds the changes.</param>
        /// <param name="returned">Specifies whether the return was from the parent selection menu 
        /// or the child menu of the change node.</param>
        public static void Enable(GameObject node, DrawableType newValueHolder, bool returned = false)
        {
            if (newValueHolder is MindMapNodeConf conf && instance == null)
            {
                /// Apply the changes from ChangeParent and ChangeNodeKind if returned.
                if (returned)
                {
                    MindMapNodeConf confOfReturn = (MindMapNodeConf)DrawableType.Get(node);
                    conf.parentNode = confOfReturn.parentNode;
                    conf.branchLineToParent = confOfReturn.branchLineToParent;
                    conf.branchLineConf = confOfReturn.branchLineConf;
                    conf.nodeKind = confOfReturn.nodeKind;
                    conf.id = confOfReturn.id;
                    conf.textConf = confOfReturn.textConf;
                }
                
                /// Instantiates the menu.
                instance = PrefabInstantiator.InstantiatePrefab(mmEditPrefab,
                    GameObject.Find("UI Canvas").transform, false);

                GameObject drawable = GameFinder.GetDrawable(node);
                GameObject attached = GameFinder.GetAttachedObjectsObject(drawable);

                /// The return call back, to return to the (this) parent menu.
                UnityAction callback = () =>
                {
                    instance.SetActive(true);
                    LineMenu.DisableLineMenu();
                    TextMenu.Disable();
                    MindMapParentSelectionMenu.Disable();
                    MindMapChangeNodeKindMenu.Disable();
                };

                /// The return call back with destroying. Will be needed to get the changes of parent and node kind change.
                UnityAction callBackWithDestroy = () =>
                {
                    Enable(node, conf, true);
                    LineMenu.DisableLineMenu();
                    TextMenu.Disable();
                    MindMapParentSelectionMenu.Disable();
                    MindMapChangeNodeKindMenu.Disable();
                };

                /// Initialize the buttons for the modification options.
                InitializeChangeParent(attached, node, conf, callBackWithDestroy);
                InitializeChangeNodeKind(attached, node, conf, callBackWithDestroy);
                InitializeChangeBorder(node, conf, callback);
                InitializeChangeText(node, conf, callback);
                InitializeChangeBranchLine(attached, conf, callback);
            }
        }

        /// <summary>
        /// Initializes the button for changing the parent. 
        /// It calls the <see cref="MindMapParentSelectionMenu"/>.
        /// </summary>
        /// <param name="attached">The attached object - object where the <see cref="DrawableType"/> are placed.</param>
        /// <param name="node">The selected node to change.</param>
        /// <param name="conf">The configuration which holds the changes</param>
        /// <param name="callback">The call back to return to the parent menu.</param>
        private static void InitializeChangeParent(GameObject attached, GameObject node, 
            MindMapNodeConf conf, UnityAction callback)
        {
            ButtonManagerBasic changeParent = GameFinder.FindChild(instance, "Parent")
                    .GetComponent<ButtonManagerBasic>();
            changeParent.clickEvent.AddListener(() =>
            {
                /// At this point, immediately is required because Destroyer.Destroy() does not delete quickly enough in case a theme node has been selected.
                GameObject.DestroyImmediate(instance);
                MindMapParentSelectionMenu.EnableForEditing(attached, node, conf, callback);
            });
        }

        /// <summary>
        /// Initializes the button for changing the node kind. 
        /// It calls the <see cref="MindMapChangeNodeKindMenu"/>.
        /// </summary>
        /// <param name="attached">The attached object - object where the <see cref="DrawableType"/> are placed.</param>
        /// <param name="node">The selected node to change.</param>
        /// <param name="conf">The configuration which holds the changes</param>
        /// <param name="callback">The call back to return to the parent menu.</param>
        private static void InitializeChangeNodeKind(GameObject attached, GameObject node, 
            MindMapNodeConf conf, UnityAction callback)
        {
            ButtonManagerBasic changeNodeKind = GameFinder.FindChild(instance, "NodeKind")
                    .GetComponent<ButtonManagerBasic>();
            changeNodeKind.clickEvent.AddListener(() =>
            {
                Destroyer.Destroy(instance);
                MindMapChangeNodeKindMenu.Enable(node, conf, callback);
            });
        }

        /// <summary>
        /// Initializes the button for changing the border. 
        /// It calls the <see cref="LineMenu"/>.
        /// </summary>
        /// <param name="attached">The attached object - object where the <see cref="DrawableType"/> are placed.</param>
        /// <param name="node">The selected node to change.</param>
        /// <param name="conf">The configuration which holds the changes</param>
        /// <param name="callback">The call back to return to the parent menu.</param>
        private static void InitializeChangeBorder(GameObject node, MindMapNodeConf conf, UnityAction callback)
        {
            ButtonManagerBasic changeBorder = GameFinder.FindChild(instance, "Border")
                    .GetComponent<ButtonManagerBasic>();
            changeBorder.clickEvent.AddListener(() =>
            {
                instance.SetActive(false);
                LineMenu.EnableForEditing(GameFinder.FindChildWithTag(node, Tags.Line), conf.borderConf, callback);
            });
        }

        /// <summary>
        /// Initializes the button for changing the text. 
        /// It calls the <see cref="TextMenu"/>.
        /// </summary>
        /// <param name="attached">The attached object - object where the <see cref="DrawableType"/> are placed.</param>
        /// <param name="node">The selected node to change.</param>
        /// <param name="conf">The configuration which holds the changes</param>
        /// <param name="callback">The call back to return to the parent menu.</param>
        private static void InitializeChangeText(GameObject node, MindMapNodeConf conf, UnityAction callback)
        {
            ButtonManagerBasic changeText = GameFinder.FindChild(instance, "NodeText")
                    .GetComponent<ButtonManagerBasic>();
            changeText.clickEvent.AddListener(() =>
            {
                instance.SetActive(false);
                TextMenu.EnableForEditing(GameFinder.FindChildWithTag(node, Tags.DText), conf.textConf, callback);
            });
        }

        /// <summary>
        /// Initializes the button for changing the branch line. 
        /// It calls the <see cref="LineMenu"/>, if a branch line exist.
        /// Otherwise the branch line button will be inactive.
        /// </summary>
        /// <param name="attached">The attached object - object where the <see cref="DrawableType"/> are placed.</param>
        /// <param name="node">The selected node to change.</param>
        /// <param name="conf">The configuration which holds the changes</param>
        /// <param name="callback">The call back to return to the parent menu.</param>
        private static void InitializeChangeBranchLine(GameObject attached, MindMapNodeConf conf, UnityAction callback)
        {
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
            }
            else
            {
                /// If no parent branch line exist for this node, deactivate the button.
                branchLineButtonArea.GetComponent<ButtonManagerBasic>().enabled = false;
                branchLineButtonArea.GetComponent<Button>().interactable = false;
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