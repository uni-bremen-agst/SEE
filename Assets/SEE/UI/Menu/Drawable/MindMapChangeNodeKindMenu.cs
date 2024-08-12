using Michsky.UI.ModernUIPack;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.ValueHolders;
using SEE.UI.Notification;
using SEE.Net.Actions.Drawable;
using UnityEngine;
using UnityEngine.Events;

namespace SEE.UI.Menu.Drawable
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

                /// Adds the return call to the return button, to return to the parent menu.
                GameFinder.FindChild(Instance.gameObject, "ReturnBtn").GetComponent<ButtonManagerBasic>()
                    .clickEvent.AddListener(returnCall);

                /// Initialize the node kind selector.
                nodeKindSelector = GameFinder.FindChild(Instance.gameObject, "Selection").GetComponent<HorizontalSelector>();

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
        /// Register the handler for the node kind selector.
        /// </summary>
        /// <param name="addedNode">The selected node</param>
        /// <param name="newConf">The configuration of the node which saves the changes.</param>
        private static void AddNodeKindSelectorHandler(GameObject addedNode, MindMapNodeConf newConf)
        {
            GameObject surface = GameFinder.GetDrawableSurface(addedNode);
            string surfaceParentName = GameFinder.GetDrawableSurfaceParentName(surface);

            /// Gets the configurations for the mind map border and the branch line to the parent.
            LineConf boarderConf = newConf.BorderConf;
            LineConf parentBranchLineConf = newConf.BranchLineConf;

            nodeKindSelector.selectorEvent.AddListener(index =>
            {
                /// Gets the new and the old node kind.
                GameMindMap.NodeKind newNodeKind = GameMindMap.GetNodeKinds()[index];
                GameMindMap.NodeKind oldKind = newConf.NodeKind;

                /// If the change was possible or the new kind corresponds to the old one.
                if (GameMindMap.CheckValidNodeKindChange(addedNode, newNodeKind, oldKind) || newNodeKind == oldKind)
                {
                    /// Executes the Node Kind change if it is possible.
                    GameMindMap.ChangeNodeKind(addedNode, newNodeKind, boarderConf);
                    new MindMapChangeNodeKindNetAction(surface.name, surfaceParentName, newConf, newNodeKind).Execute();
                    newConf.NodeKind = newNodeKind;
                    newConf.TextConf = ((MindMapNodeConf)DrawableType.Get(addedNode)).TextConf;
                    if (newNodeKind == GameMindMap.NodeKind.Theme)
                    {
                        newConf.ParentNode = "";
                        newConf.BranchLineToParent = "";
                    }

                    /// If the new one is not a <see cref="GameMindMap.NodeKind.Theme"/>
                    /// and the node has no parent, initiate the parent selection.
                    if (newNodeKind != GameMindMap.NodeKind.Theme
                        && addedNode.GetComponent<MMNodeValueHolder>().GetParent() == null)
                    {
                        MindMapParentSelectionMenu.Instance.Destroy();
                        GameObject pMenu = MindMapParentSelectionMenu.EnableForEditing(
                            GameFinder.GetAttachedObjectsObject(surface), addedNode, newConf, null);
                        GameFinder.FindChild(pMenu, "Dragger").GetComponent<WindowDragger>().enabled = false;
                        pMenu.transform.SetParent(GameFinder.FindChild(Instance.gameObject, "Content").transform);

                        /// Restore the appearance of the previous branch line, if there was one.
                        if (addedNode.GetComponent<MMNodeValueHolder>().GetParentBranchLine() != null
                           && parentBranchLineConf != null)
                        {
                            GameObject pBranchLine = addedNode.GetComponent<MMNodeValueHolder>().GetParentBranchLine();
                            GameEdit.ChangeLine(pBranchLine, parentBranchLineConf);
                            new EditLineNetAction(surface.name, surfaceParentName,
                                LineConf.GetLineWithoutRenderPos(pBranchLine)).Execute();
                        }
                    }
                    else
                    {
                        MindMapParentSelectionMenu.Instance.Destroy();

                    }
                }
                else
                {
                    ShowNotification.Warn("Cannot transform", "The newly chosen node kind cannot be applied to this node.");
                }

            });
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
