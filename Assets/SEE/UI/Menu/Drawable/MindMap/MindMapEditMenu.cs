using Michsky.UI.ModernUIPack;
using SEE.Game;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.MindMap;
using SEE.Game.Drawable.ValueHolders;
using SEE.GO;
using SEE.Net.Actions.Drawable;
using SEE.UI.Drawable;
using SEE.UI.Menu.Drawable.Line;
using SEE.UI.Menu.Drawable.Text;
using SEE.Utils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SEE.UI.Menu.Drawable.MindMap
{
    /// <summary>
    /// Provides the edit menu for Mind Map nodes.
    /// </summary>
    public class MindMapEditMenu : SingletonMenu
    {
        /// <summary>
        /// The location of the Mind Map edit menu prefab.
        /// </summary>
        private const string mmEditPrefab = "Prefabs/UI/Drawable/MMEdit";

        /// <summary>
        /// Prevents instances of this singleton class from being created outside this class.
        /// </summary>
        private MindMapEditMenu() { }

        /// <summary>
        /// The only instance of this singleton class.
        /// </summary>
        public static MindMapEditMenu Instance { get; private set; }

        /// <summary>
        /// Initializes the singleton instance.
        /// </summary>
        static MindMapEditMenu()
        {
            Instance = new MindMapEditMenu();
        }

        /// <summary>
        /// Creates the edit menu for the given Mind Map node and initializes all
        /// available editing options.
        /// </summary>
        /// <param name="node">The Mind Map node that should be edited.</param>
        /// <param name="newValueHolder">The configuration that holds the changes.</param>
        /// <param name="returned">
        /// Whether the menu is reopened after returning from the parent selection
        /// or node kind selection menu.
        /// </param>
        public static void Enable(GameObject node, DrawableType newValueHolder, bool returned = false)
        {
            if (newValueHolder is MindMapNodeConf conf)
            {
                /// Apply the changes from ChangeParent and ChangeNodeKind if returned.
                if (returned)
                {
                    RefreshConfiguration(node, conf);
                }

                Instance = new MindMapEditMenu();
                Instance.Instantiate(mmEditPrefab);

                GameObject surface = GameFinder.GetDrawableSurface(node);
                GameObject attached = GameFinder.GetAttachedObjectsObject(surface);

                /// The return callback to return to this parent menu.
                UnityAction callback = CreateReturnCallback();

                /// The return callback that recreates this menu to adopt parent or node kind changes.
                UnityAction callbackWithRefresh = CreateReturnCallback(node, conf);

                /// Initialize the buttons for the modification options.
                InitializeChangeParent(attached, node, conf, callbackWithRefresh);
                InitializeChangeNodeKind(node, conf, callbackWithRefresh);
                InitializeChangeBorder(node, conf, callback);
                InitializeChangeText(node, conf, callback);
                InitializeChangeBranchLine(attached, conf, callback);
                InitializeChangeOrderInLayer(node, conf);
            }
        }

        /// <summary>
        /// Refreshes the editable configuration with the current state of the given
        /// Mind Map node.
        /// </summary>
        /// <param name="node">The Mind Map node whose current configuration should be used.</param>
        /// <param name="configuration">The configuration that should receive the current values.</param>
        private static void RefreshConfiguration(GameObject node, MindMapNodeConf configuration)
        {
            MindMapNodeConf currentConfiguration = (MindMapNodeConf)DrawableType.Get(node);

            configuration.ParentNode = currentConfiguration.ParentNode;
            configuration.BranchLineToParent = currentConfiguration.BranchLineToParent;
            configuration.BranchLineConf = currentConfiguration.BranchLineConf;
            configuration.NodeKind = currentConfiguration.NodeKind;
            configuration.ID = currentConfiguration.ID;
            configuration.TextConf = currentConfiguration.TextConf;
            configuration.BorderConf = currentConfiguration.BorderConf;
            configuration.OrderInLayer = currentConfiguration.OrderInLayer;
        }

        /// <summary>
        /// Creates the callback used when returning from an editing submenu that does
        /// not require the Mind Map edit menu to be recreated.
        /// </summary>
        /// <returns>The callback for returning to the existing edit menu.</returns>
        private static UnityAction CreateReturnCallback()
        {
            return () =>
            {
                Instance.gameObject.SetActive(true);
                CloseChildMenus();
            };
        }

        /// <summary>
        /// Creates the callback used when returning from an editing submenu whose
        /// changes require the Mind Map edit menu and its configuration to be refreshed.
        /// </summary>
        /// <param name="node">The edited Mind Map node.</param>
        /// <param name="configuration">The configuration that holds the current changes.</param>
        /// <returns>The callback for recreating the edit menu.</returns>
        private static UnityAction CreateReturnCallback(GameObject node, MindMapNodeConf configuration)
        {
            return () =>
            {
                Enable(node, configuration, true);
                CloseChildMenus();
            };
        }

        /// <summary>
        /// Closes all child menus that can be opened from the Mind Map edit menu.
        /// </summary>
        private static void CloseChildMenus()
        {
            LineMenu.Instance.Disable();
            TextMenu.Instance.Disable();
            MindMapParentSelectionMenu.Instance.Destroy();
            MindMapChangeNodeKindMenu.Instance.Destroy();
        }

        /// <summary>
        /// Returns the button with the given name from the currently instantiated
        /// Mind Map edit menu.
        /// </summary>
        /// <param name="name">The name of the requested button.</param>
        /// <returns>The requested button manager.</returns>
        private static ButtonManagerBasic GetButton(string name)
        {
            return GameFinder.FindAttachedOrLocalDescendant(Instance.gameObject, name)
                .GetComponent<ButtonManagerBasic>();
        }

        /// <summary>
        /// Initializes the button for changing the parent and opens the
        /// <see cref="MindMapParentSelectionMenu"/> when selected.
        /// </summary>
        /// <param name="attached">The object containing the attached Drawable objects.</param>
        /// <param name="node">The selected Mind Map node.</param>
        /// <param name="conf">The configuration that holds the changes.</param>
        /// <param name="callback">The callback used to return to the edit menu.</param>
        private static void InitializeChangeParent(GameObject attached, GameObject node,
            MindMapNodeConf conf, UnityAction callback)
        {
            ButtonManagerBasic changeParent = GetButton("Parent");
            changeParent.clickEvent.AddListener(() =>
            {
                /// At this point, immediately is required because Destroyer.Destroy() does not
                /// delete quickly enough in case a theme node has been selected.
                GameObject.DestroyImmediate(Instance.gameObject);
                MindMapParentSelectionMenu.EnableForEditing(attached, node, conf, callback);
            });
        }

        /// <summary>
        /// Initializes the button for changing the node kind and opens the
        /// <see cref="MindMapChangeNodeKindMenu"/> when selected.
        /// </summary>
        /// <param name="node">The selected Mind Map node.</param>
        /// <param name="conf">The configuration that holds the changes.</param>
        /// <param name="callback">The callback used to return to the edit menu.</param>
        private static void InitializeChangeNodeKind(GameObject node, MindMapNodeConf conf, UnityAction callback)
        {
            ButtonManagerBasic changeNodeKind = GetButton("NodeKind");
            changeNodeKind.clickEvent.AddListener(() =>
            {
                Destroyer.Destroy(Instance.gameObject);
                MindMapChangeNodeKindMenu.Enable(node, conf, callback);
            });
        }

        /// <summary>
        /// Initializes the button for changing the node border and opens the
        /// <see cref="LineMenu"/> when selected.
        /// </summary>
        /// <param name="node">The selected Mind Map node.</param>
        /// <param name="conf">The configuration that holds the changes.</param>
        /// <param name="callback">The callback used to return to the edit menu.</param>
        private static void InitializeChangeBorder(GameObject node, MindMapNodeConf conf, UnityAction callback)
        {
            ButtonManagerBasic changeBorder = GetButton("Border");
            changeBorder.clickEvent.AddListener(() =>
            {
                Instance.gameObject.SetActive(false);
                LineMenu.Instance.EnableForEditing(node.FindDescendantWithTag(Tags.Line), conf.BorderConf, callback);
            });
        }

        /// <summary>
        /// Initializes the button for changing the node text and opens the
        /// <see cref="TextMenu"/> when selected.
        /// </summary>
        /// <param name="node">The selected Mind Map node.</param>
        /// <param name="conf">The configuration that holds the changes.</param>
        /// <param name="callback">The callback used to return to the edit menu.</param>
        private static void InitializeChangeText(GameObject node, MindMapNodeConf conf, UnityAction callback)
        {
            ButtonManagerBasic changeText = GetButton("NodeText");
            changeText.clickEvent.AddListener(() =>
            {
                Instance.gameObject.SetActive(false);
                TextMenu.Instance.EnableForEditing(node.FindDescendantWithTag(Tags.DText), conf.TextConf, callback);
            });
        }

        /// <summary>
        /// Initializes the button for changing the branch line to the parent.
        /// The button is disabled if the node has no parent branch line.
        /// </summary>
        /// <param name="attached">The object containing the attached Drawable objects.</param>
        /// <param name="conf">The configuration that holds the changes.</param>
        /// <param name="callback">The callback used to return to the edit menu.</param>
        private static void InitializeChangeBranchLine(GameObject attached, MindMapNodeConf conf, UnityAction callback)
        {
            GameObject branchLineButtonArea = GameFinder.FindAttachedOrLocalDescendant(Instance.gameObject, "BranchLine");

            if (conf.BranchLineToParent != "")
            {
                ButtonManagerBasic branchButton = branchLineButtonArea.GetComponent<ButtonManagerBasic>();
                branchButton.clickEvent.AddListener(() =>
                {
                    Instance.gameObject.SetActive(false);
                    GameObject branchLine = GameFinder.FindAttachedOrLocalDescendant(attached, conf.BranchLineToParent);
                    LineMenu.Instance.EnableForEditing(branchLine, conf.BranchLineConf, callback);
                });
            }
            else
            {
                /// If no parent branch line exists for this node, deactivate the button.
                branchLineButtonArea.GetComponent<ButtonManagerBasic>().enabled = false;
                branchLineButtonArea.GetComponent<Button>().interactable = false;
            }
        }

        /// <summary>
        /// Initializes the order in layer slider of the Mind Map edit menu.
        /// </summary>
        /// <param name="node">The selected Mind Map node.</param>
        /// <param name="conf">The configuration that holds the changes.</param>
        private static void InitializeChangeOrderInLayer(GameObject node, MindMapNodeConf conf)
        {
            LayerSliderController layerSlider = Instance.gameObject.GetComponentInChildren<LayerSliderController>();

            /// Assigns the current value to the slider.
            layerSlider.AssignValue(conf.OrderInLayer);

            GameObject surface = GameFinder.GetDrawableSurface(node);
            string surfaceParentName = GameFinder.GetDrawableSurfaceParentName(surface);
            layerSlider.AssignMaxOrder(surface.GetComponent<DrawableHolder>().OrderInLayer);

            /// Adds the handler for changing the order in layer.
            layerSlider.OnValueChanged.AddListener(layerOrder =>
            {
                GameEdit.ChangeLayer(node, layerOrder);
                conf.OrderInLayer = layerOrder;
                new EditLayerNetAction(surface.name, surfaceParentName, node.name, layerOrder).Execute();
                GameMindMapBranch.ReDrawBranchLines(node);
                new MindMapRefreshBranchLinesNetAction(
                    surface.name, surfaceParentName, MindMapNodeConf.GetNodeConf(node)).Execute();
            });
        }

        /// <summary>
        /// Destroys the edit menu and the parent selection and node kind child menus.
        /// </summary>
        public override void Destroy()
        {
            base.Destroy();
            MindMapParentSelectionMenu.Instance.Destroy();
            MindMapChangeNodeKindMenu.Instance.Destroy();
        }
    }
}
