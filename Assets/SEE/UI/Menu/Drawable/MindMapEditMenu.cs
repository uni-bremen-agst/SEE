using Michsky.UI.ModernUIPack;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.ValueHolders;
using SEE.GO;
using SEE.Net.Actions.Drawable;
using SEE.UI.Drawable;
using SEE.UI.Menu.Drawable;
using SEE.Utils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SEE.Game.UI.Menu.Drawable
{
    /// <summary>
    /// This class provides the edit menu for mind map nodes.
    /// </summary>
    public class MindMapEditMenu : SingletonMenu
    {
        /// <summary>
        /// The location where the menu prefeb is placed.
        /// </summary>
        private const string mmEditPrefab = "Prefabs/UI/Drawable/MMEdit";

        /// <summary>
        /// We do not want to create an instance of this singleton class outside of this class.
        /// </summary>
        private MindMapEditMenu() { }

        /// <summary>
        /// The only instance of this singleton class.
        /// </summary>
        public static MindMapEditMenu Instance { get; private set; }

        static MindMapEditMenu()
        {
            Instance = new MindMapEditMenu();
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
            if (newValueHolder is MindMapNodeConf conf)
            {
                /// Apply the changes from ChangeParent and ChangeNodeKind if returned.
                if (returned)
                {
                    MindMapNodeConf confOfReturn = (MindMapNodeConf)DrawableType.Get(node);
                    conf.ParentNode = confOfReturn.ParentNode;
                    conf.BranchLineToParent = confOfReturn.BranchLineToParent;
                    conf.BranchLineConf = confOfReturn.BranchLineConf;
                    conf.NodeKind = confOfReturn.NodeKind;
                    conf.Id = confOfReturn.Id;
                    conf.TextConf = confOfReturn.TextConf;
                    conf.BorderConf = confOfReturn.BorderConf;
                    conf.OrderInLayer = confOfReturn.OrderInLayer;
                }

                Instance = new MindMapEditMenu();
                Instance.Instantiate(mmEditPrefab);

                GameObject surface = GameFinder.GetDrawableSurface(node);
                GameObject attached = GameFinder.GetAttachedObjectsObject(surface);

                /// The return call back, to return to the (this) parent menu.
                UnityAction callback = () =>
                {
                    Instance.gameObject.SetActive(true);
                    LineMenu.Instance.Disable();
                    TextMenu.Instance.Disable();
                    MindMapParentSelectionMenu.Instance.Destroy();
                    MindMapChangeNodeKindMenu.Instance.Destroy();
                };

                /// The return call back with destroying. Will be needed to get the changes of parent and node kind change.
                UnityAction callBackWithDestroy = () =>
                {
                    Enable(node, conf, true);
                    LineMenu.Instance.Disable();
                    TextMenu.Instance.Disable();
                    MindMapParentSelectionMenu.Instance.Destroy();
                    MindMapChangeNodeKindMenu.Instance.Destroy();
                };

                /// Initialize the buttons for the modification options.
                InitializeChangeParent(attached, node, conf, callBackWithDestroy);
                InitializeChangeNodeKind(attached, node, conf, callBackWithDestroy);
                InitializeChangeBorder(node, conf, callback);
                InitializeChangeText(node, conf, callback);
                InitializeChangeBranchLine(attached, conf, callback);
                InitializeChangeOrderInLayer(node, conf);
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
            ButtonManagerBasic changeParent = GameFinder.FindChild(Instance.gameObject, "Parent")
                    .GetComponent<ButtonManagerBasic>();
            changeParent.clickEvent.AddListener(() =>
            {
                /// At this point, immediately is required because Destroyer.Destroy() does not
                /// delete quickly enough in case a theme node has been selected.
                GameObject.DestroyImmediate(Instance.gameObject);
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
            ButtonManagerBasic changeNodeKind = GameFinder.FindChild(Instance.gameObject, "NodeKind")
                    .GetComponent<ButtonManagerBasic>();
            changeNodeKind.clickEvent.AddListener(() =>
            {
                Destroyer.Destroy(Instance.gameObject);
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
            ButtonManagerBasic changeBorder = GameFinder.FindChild(Instance.gameObject, "Border")
                    .GetComponent<ButtonManagerBasic>();
            changeBorder.clickEvent.AddListener(() =>
            {
                Instance.gameObject.SetActive(false);
                LineMenu.Instance.EnableForEditing(node.FindChildWithTag(Tags.Line), conf.BorderConf, callback);
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
            ButtonManagerBasic changeText = GameFinder.FindChild(Instance.gameObject, "NodeText")
                    .GetComponent<ButtonManagerBasic>();
            changeText.clickEvent.AddListener(() =>
            {
                Instance.gameObject.SetActive(false);
                TextMenu.EnableForEditing(node.FindChildWithTag(Tags.DText), conf.TextConf, callback);
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
            GameObject branchLineButtonArea = GameFinder.FindChild(Instance.gameObject, "BranchLine");
            if (conf.BranchLineToParent != "")
            {
                ButtonManagerBasic branchButton = branchLineButtonArea.GetComponent<ButtonManagerBasic>();
                branchButton.clickEvent.AddListener(() =>
                {
                    Instance.gameObject.SetActive(false);
                    GameObject bLine = GameFinder.FindChild(attached, conf.BranchLineToParent);
                    LineMenu.Instance.EnableForEditing(bLine, conf.BranchLineConf, callback);
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
        /// Init the order in layer slider for the mind-map menu.
        /// </summary>
        /// <param name="node">The selected mind-map node.</param>
        /// <param name="conf">The configuration which holds the changes.</param>
        private static void InitializeChangeOrderInLayer(GameObject node, MindMapNodeConf conf)
        {
            LayerSliderController layerSlider = Instance.gameObject.GetComponentInChildren<LayerSliderController>();

            /// Assigns the current value to the slider.
            layerSlider.AssignValue(conf.OrderInLayer);
            GameObject surface = GameFinder.GetDrawableSurface(node);
            string surfaceParentName = GameFinder.GetDrawableSurfaceParentName(surface);
            layerSlider.AssignMaxOrder(surface.GetComponent<DrawableHolder>().OrderInLayer);
            /// Adds the handler for changing.
            layerSlider.OnValueChanged.AddListener(layerOrder =>
            {
                GameEdit.ChangeLayer(node, layerOrder);
                conf.OrderInLayer = layerOrder;
                new EditLayerNetAction(surface.name, surfaceParentName,
                    node.name, layerOrder).Execute();
                GameMindMap.ReDrawBranchLines(node);
                new MindMapRefreshBranchLinesNetAction(surface.name, surfaceParentName,
                    MindMapNodeConf.GetNodeConf(node)).Execute();
            });
        }

        /// <summary>
        /// Destroys the edit menu and disables the parent selection and
        /// node kind menu.
        /// </summary>
        public override void Destroy()
        {
            base.Destroy();
            MindMapParentSelectionMenu.Instance.Destroy();
            MindMapChangeNodeKindMenu.Instance.Destroy();
        }
    }
}
