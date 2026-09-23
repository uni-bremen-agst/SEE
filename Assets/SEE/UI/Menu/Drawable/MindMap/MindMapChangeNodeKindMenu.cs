using Michsky.UI.ModernUIPack;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.MindMap;
using SEE.Game.Drawable.ValueHolders;
using SEE.Net.Actions.Drawable;
using SEE.UI.Notification;
using UnityEngine;
using UnityEngine.Events;

namespace SEE.UI.Menu.Drawable.MindMap
{
    /// <summary>
    /// This class provides the node kind selection menu for the mind map.
    /// </summary>
    public class MindMapChangeNodeKindMenu : SingletonMenu
    {
        /// <summary>
        /// The location where the menu prefeb is placed.
        /// </summary>
        private const string nodeKindSelectionMenuPrefab = "Prefabs/UI/Drawable/MMChangeNodeKind";

        /// <summary>
        /// We do not want to create an instance of this singleton class outside of this class.
        /// </summary>
        private MindMapChangeNodeKindMenu() { }

        /// <summary>
        /// The only instance of this singleton class.
        /// </summary>
        public static MindMapChangeNodeKindMenu Instance { get; private set; }

        static MindMapChangeNodeKindMenu()
        {
            Instance = new MindMapChangeNodeKindMenu();
        }

        /// <summary>
        /// The instance for the node kind selector.
        /// </summary>
        private static HorizontalSelector nodeKindSelector;

        /// <summary>
        /// Creates the node kind selection menu for mind maps for editing mode.
        /// It adds the necessary Handler to the selector and to the finish button.
        /// </summary>
        /// <param name="addedNode">The node for that a parent should be chosen.</param>
        /// <param name="valueHolder">The new configuration in which the changes are saved.</param>
        /// <param name="returnCall">The call which should be executed when the return button is pressed.</param>
        public static void Enable(GameObject addedNode, DrawableType valueHolder, UnityAction returnCall)
        {
            if (valueHolder is MindMapNodeConf newConf)
            {
                Instance = new MindMapChangeNodeKindMenu();
                Instance.Instantiate(nodeKindSelectionMenuPrefab);

                /// Adds the return callback to the return button.
                /// A Subtheme or Leaf must have a confirmed parent before leaving this menu.
                ButtonManagerBasic returnButton = GameFinder.FindAttachedOrLocalDescendant(
                    Instance.gameObject, "ReturnBtn").GetComponent<ButtonManagerBasic>();

                returnButton.clickEvent.AddListener(() =>
                {
                    MMNodeValueHolder nodeValueHolder = addedNode.GetComponent<MMNodeValueHolder>();

                    if (nodeValueHolder.NodeKind != GameMindMap.NodeKind.Theme
                        && nodeValueHolder.GetParent() == null)
                    {
                        ShowNotification.Warn("Select parent",
                            "A subtheme or leaf requires a parent.");
                        return;
                    }

                    returnCall?.Invoke();
                });

                /// Initialize the node kind selector.
                nodeKindSelector = GameFinder.FindAttachedOrLocalDescendant(Instance.gameObject, "Selection").GetComponent<HorizontalSelector>();

                /// Creates the items for them.
                foreach (GameMindMap.NodeKind kind in GameMindMap.GetNodeKinds())
                {
                    nodeKindSelector.CreateNewItem(kind.ToString());
                }

                /// Gets the index of the current chosen <see cref="GameMindMap.NodeKind"/>.
                int index = GameMindMap.GetNodeKinds().IndexOf(newConf.NodeKind);

                /// Adds the handler for the node kind change.
                AddNodeKindSelectorHandler(addedNode, newConf);

                /// Set the current selected node kind.
                nodeKindSelector.defaultIndex = index;
            }
        }

        /// <summary>
        /// Registers the handler for changing the selected node kind.
        /// Changes that require a previously missing parent are only applied after the
        /// user has explicitly confirmed a valid parent.
        /// </summary>
        /// <param name="addedNode">The selected Mind Map node.</param>
        /// <param name="newConf">The configuration that stores the applied changes.</param>
        private static void AddNodeKindSelectorHandler(GameObject addedNode, MindMapNodeConf newConf)
        {
            GameObject surface = GameFinder.GetDrawableSurface(addedNode);
            string surfaceParentName = GameFinder.GetDrawableSurfaceParentName(surface);

            /// Gets the configurations for the Mind Map border and the branch line to the parent.
            LineConf borderConf = newConf.BorderConf;
            LineConf parentBranchLineConf = newConf.BranchLineConf;

            nodeKindSelector.selectorEvent.AddListener(index =>
            {
                GameMindMap.NodeKind newNodeKind = GameMindMap.GetNodeKinds()[index];
                GameMindMap.NodeKind oldNodeKind = newConf.NodeKind;

                /// Reject node kind changes that violate the structural Mind Map rules.
                if (!GameMindMapHierarchy.CheckValidNodeKindChange(addedNode, newNodeKind, oldNodeKind)
                    && newNodeKind != oldNodeKind)
                {
                    ShowNotification.Warn(
                        "Cannot transform",
                        "The newly chosen node kind cannot be applied to this node.");
                    return;
                }

                MMNodeValueHolder nodeValueHolder = addedNode.GetComponent<MMNodeValueHolder>();

                /// A transition from Theme to Subtheme or Leaf requires a parent.
                /// Do not apply the node kind change before that parent was explicitly confirmed.
                if (newNodeKind != GameMindMap.NodeKind.Theme
                    && nodeValueHolder.GetParent() == null)
                {
                    MindMapParentSelectionMenu.Instance.Destroy();

                    GameObject parentMenu = MindMapParentSelectionMenu.EnableForRequiredParentSelection(
                        GameFinder.GetAttachedObjectsObject(surface),
                        addedNode,
                        parent =>
                        {
                            ApplyNodeKindChange(
                                addedNode,
                                newConf,
                                newNodeKind,
                                borderConf,
                                surface,
                                surfaceParentName);

                            GameMindMapBranch.ChangeParent(addedNode, parent);
                            newConf.ParentNode = parent.name;

                            new MindMapChangeParentNetAction(
                                surface.name,
                                surfaceParentName,
                                newConf).Execute();

                            /// Restore the appearance of the previous parent branch line,
                            /// if a corresponding configuration exists.
                            GameObject parentBranchLine =
                                addedNode.GetComponent<MMNodeValueHolder>().GetParentBranchLine();

                            if (parentBranchLine != null && parentBranchLineConf != null)
                            {
                                GameEdit.ChangeLine(parentBranchLine, parentBranchLineConf);

                                new EditLineNetAction(
                                    surface.name,
                                    surfaceParentName,
                                    LineConf.GetLineWithoutRenderPos(parentBranchLine)).Execute();
                            }
                        });

                    if (parentMenu != null)
                    {
                        GameFinder.FindAttachedOrLocalDescendant(parentMenu, "Dragger")
                            .GetComponent<WindowDragger>().enabled = false;

                        parentMenu.transform.SetParent(
                            GameFinder.FindAttachedOrLocalDescendant(
                                Instance.gameObject, "Content").transform);
                    }

                    return;
                }

                /// The change does not require an additional parent selection and can
                /// therefore be applied immediately.
                ApplyNodeKindChange(
                    addedNode,
                    newConf,
                    newNodeKind,
                    borderConf,
                    surface,
                    surfaceParentName);

                MindMapParentSelectionMenu.Instance.Destroy();
            });
        }

        /// <summary>
        /// Applies a validated Mind Map node kind change and synchronizes the corresponding
        /// configuration and network state.
        /// </summary>
        /// <param name="addedNode">The Mind Map node whose kind should be changed.</param>
        /// <param name="newConf">The configuration that stores the applied changes.</param>
        /// <param name="newNodeKind">The node kind that should be applied.</param>
        /// <param name="borderConf">The configuration of the node border.</param>
        /// <param name="surface">The Drawable surface containing the Mind Map node.</param>
        /// <param name="surfaceParentName">The name of the Drawable surface parent.</param>
        private static void ApplyNodeKindChange(GameObject addedNode, MindMapNodeConf newConf,
            GameMindMap.NodeKind newNodeKind, LineConf borderConf,
            GameObject surface, string surfaceParentName)
        {
            GameMindMap.ChangeNodeKind(addedNode, newNodeKind, borderConf);

            new MindMapChangeNodeKindNetAction(
                surface.name,
                surfaceParentName,
                newConf,
                newNodeKind).Execute();

            newConf.NodeKind = newNodeKind;
            newConf.TextConf = ((MindMapNodeConf)DrawableType.Get(addedNode)).TextConf;

            if (newNodeKind == GameMindMap.NodeKind.Theme)
            {
                newConf.ParentNode = "";
                newConf.BranchLineToParent = "";
            }
        }

        /// <summary>
        /// Get the currently selected node kind.
        /// </summary>
        /// <returns>The selected node kind.</returns>
        public static GameMindMap.NodeKind GetSelectedNodeKind()
        {
            return GameMindMap.GetNodeKinds()[nodeKindSelector.index];
        }
    }
}
