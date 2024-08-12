using Michsky.UI.ModernUIPack;
using SEE.Game;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.ValueHolders;
using SEE.UI.Notification;
using SEE.Net.Actions.Drawable;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace SEE.UI.Menu.Drawable
{
    /// <summary>
    /// This class provides the parent selection menu for the mind map.
    /// </summary>
    public class MindMapParentSelectionMenu : SingletonMenu
    {
        /// <summary>
        /// The location where the menu prefeb is placed.
        /// </summary>
        private const string parentSelectionMenuPrefab = "Prefabs/UI/Drawable/MMSelectParent";

        /// <summary>
        /// We do not want to create an instance of this singleton class outside of this class.
        /// </summary>
        private MindMapParentSelectionMenu() { }

        /// <summary>
        /// The only instance of this singleton class.
        /// </summary>
        public static MindMapParentSelectionMenu Instance { get; private set; }

        static MindMapParentSelectionMenu()
        {
            Instance = new MindMapParentSelectionMenu();
        }

        /// <summary>
        /// Whether this class has an operation in store that hasn't been fetched yet.
        /// </summary>
        private static bool gotSelection;

        /// <summary>
        /// If <see cref="gotSelection"/> is true, this contains the chosen node which the player selected.
        /// </summary>
        private static GameObject chosenObject;

        /// <summary>
        /// Creates the parent selection menu for mind maps.
        /// It adds the necessary Handler to the selector and to the finish button.
        /// </summary>
        /// <param name="attachedObjects">The attached objects object of the chosen drawable</param>
        /// <param name="addedNode">The node for that a parent should be chosen.</param>
        public static void Enable(GameObject attachedObjects, GameObject addedNode)
        {
            Instance.Instantiate(parentSelectionMenuPrefab);

            /// Disable the return button.
            GameFinder.FindChild(Instance.gameObject, "ReturnBtn").SetActive(false);

            /// Initialize the parent selector.
            HorizontalSelector parentSelector = GameFinder.FindChild(Instance.gameObject, "ParentSelection")
                .GetComponent<HorizontalSelector>();

            /// Gets all Mind Map Nodes of the given attached object - object.
            IList<GameObject> allNodes = GameFinder.FindAllChildrenWithTag(attachedObjects, Tags.MindMapNode);

            /// Gather all Mind Map Nodes with the <see cref="GameMindMap.NodeKind"/>:
            /// <see cref="GameMindMap.NodeKind.Theme"/> or <see cref="GameMindMap.NodeKind.Subtheme"/>.
            /// Note: A <see cref="GameMindMap.NodeKind.Leaf"/> can not be a parent.
            List<GameObject> nodes = new();
            foreach (GameObject node in allNodes)
            {
                if (node.GetComponent<MMNodeValueHolder>().NodeKind != GameMindMap.NodeKind.Leaf
                    && node != addedNode)
                {
                    nodes.Add(node);
                }
            }

            /// Create an item in the parent selector for the collected nodes.
            foreach (GameObject node in nodes)
            {
                parentSelector.CreateNewItem(node.GetComponentInChildren<TextMeshPro>().text);
            }

            GameObject surface = GameFinder.GetDrawableSurface(attachedObjects);
            string surfaceParentName = GameFinder.GetDrawableSurfaceParentName(surface);

            /// Adds a handler to the parent selector so that the selected node is set as the chosen node.
            parentSelector.selectorEvent.AddListener(index =>
            {
                chosenObject = nodes[index];
            });

            parentSelector.defaultIndex = 0;

            /// The parent selection can be completed through the Finish button.
            ButtonManagerBasic finish = GameFinder.FindChild(Instance.gameObject, "Finish").GetComponent<ButtonManagerBasic>();
            finish.clickEvent.AddListener(() =>
            {
                gotSelection = true;

                /// In case it is not selected, but 'Finish' is clicked directly.
                if (chosenObject == null)
                {
                    chosenObject = nodes[parentSelector.index];
                }
            });
        }

        /// <summary>
        /// Creates the parent selection menu for mind maps for editing mode.
        /// It adds the necessary Handler to the selector and to the finish button.
        /// </summary>
        /// <param name="attachedObjects">The attached objects object of the chosen drawable</param>
        /// <param name="addedNode">The node for that a parent should be chosen.</param>
        /// <param name="valueHolder">The new configuration in which the changes are saved.</param>
        /// <param name="returnCall">The call which should be executed, if the return button is pressed.</param>
        /// <param name="cutCopyMode">Indicates that this method was called by CutCopyPaste Action</param>
        /// <returns>The instance of the menu. Can be null if the <see cref="DrawableType"/> isn't a <see cref="MindMapNodeConf"/></returns>
        public static GameObject EnableForEditing(GameObject attachedObjects, GameObject addedNode,
            DrawableType valueHolder, UnityAction returnCall, bool cutCopyMode = false)
        {
            if (valueHolder is MindMapNodeConf newConf)
            {
                Instance.Instantiate(parentSelectionMenuPrefab);

                if (returnCall != null)
                {
                    /// Adds the return call back to the return button.
                    GameFinder.FindChild(Instance.gameObject, "ReturnBtn").GetComponent<ButtonManagerBasic>()
                        .clickEvent.AddListener(returnCall);
                }
                else
                {
                    /// Disables the return button.
                    GameFinder.FindChild(Instance.gameObject, "ReturnBtn").SetActive(false);
                }

                /// Initalize the parent selector.
                HorizontalSelector parentSelector = GameFinder.FindChild(Instance.gameObject, "ParentSelection")
                    .GetComponent<HorizontalSelector>();

                /// Gets all Mind Map Nodes of the given attached object - object.
                IList<GameObject> allNodes = GameFinder.FindAllChildrenWithTag(attachedObjects, Tags.MindMapNode, false);

                /// Collect all Mind Map Nodes with the <see cref="GameMindMap.NodeKind"/>:
                /// <see cref="GameMindMap.NodeKind.Theme"/> or <see cref="GameMindMap.NodeKind.Subtheme"/>
                /// that qualify as a new (valid) parent.
                /// Note: A <see cref="GameMindMap.NodeKind.Leaf"/> can not be a parent
                ///     and nodes are prohibited as a parent if selecting them would create a cycle.
                List<GameObject> nodes = new();
                foreach (GameObject node in allNodes)
                {
                    if (node.GetComponent<MMNodeValueHolder>().NodeKind != GameMindMap.NodeKind.Leaf
                        && node != addedNode
                        && GameMindMap.ParentChangeIsValid(addedNode, node))
                    {
                        nodes.Add(node);
                    }
                }
                /// If the user try to change the parent for a <see cref="GameMindMap.NodeKind.Theme"/>,
                /// then show an error and close the menu.
                if (addedNode.GetComponent<MMNodeValueHolder>().NodeKind == GameMindMap.NodeKind.Theme)
                {
                    ShowNotification.Warn("Unauthorized action", "A theme can't have a parent.");
                    returnCall.Invoke();
                    Instance.Destroy();
                    return null;
                }

                /// If no suitable parents are found, close the menu with an appropriate warning.
                if (nodes.Count == 0)
                {
                    ShowNotification.Warn("Add a Theme", "You need a theme for the mind map. First add one");
                    Instance.Destroy();
                    return null;
                }

                /// For all valid parents, create an item in the parent selector.
                foreach (GameObject node in nodes)
                {
                    parentSelector.CreateNewItem(node.GetComponentInChildren<TextMeshPro>().text);
                }

                /// Get the index of the current parent.
                int index = nodes.IndexOf(GameFinder.FindChild(attachedObjects, newConf.ParentNode));

                /// If the index can't be found, take the default index 0.
                index = index < 0 ? 0 : index;

                GameObject surface = GameFinder.GetDrawableSurface(addedNode);
                string surfaceParentName = GameFinder.GetDrawableSurfaceParentName(surface);

                /// If the node has no parent branch line, initially create a branch line to the index.
                if (addedNode.GetComponent<MMNodeValueHolder>().GetParentBranchLine() == null)
                {
                    chosenObject = nodes[index];
                    ChangeParent(addedNode, newConf, surface);
                }

                /// Adds the handler for changing the parent to the parent selector.
                parentSelector.selectorEvent.AddListener(index =>
                {
                    chosenObject = nodes[index];
                    ChangeParent(addedNode, newConf, surface);
                });
                parentSelector.defaultIndex = index;

                /// For the <paramref name="cutCopyMode", provide a Finish Button.
                /// Disable it for all others.
                if (!cutCopyMode)
                {
                    GameObject finish = GameFinder.FindChild(Instance.gameObject, "Finish");
                    finish.SetActive(false);
                }
                else
                {
                    ButtonManagerBasic finish = GameFinder.FindChild(Instance.gameObject, "Finish").GetComponent<ButtonManagerBasic>();
                    finish.clickEvent.AddListener(() =>
                    {
                        gotSelection = true;
                    });
                }
            }
            return Instance.gameObject;
        }

        /// <summary>
        /// Returns the change parent call.
        /// </summary>
        /// <param name="addedNode">The selected node.</param>
        /// <param name="newConf">The configuration that holds the changes.</param>
        /// <param name="surface">The drawable surface on which the node is placed.</param>
        private static void ChangeParent(GameObject addedNode, MindMapNodeConf newConf, GameObject surface)
        {
            GameMindMap.ChangeParent(addedNode, chosenObject);
            newConf.ParentNode = chosenObject.name;
            new MindMapChangeParentNetAction(surface.name, GameFinder.GetDrawableSurfaceParentName(surface),
                newConf).Execute();
        }

        /// <summary>
        /// Returns the chosen node.
        /// </summary>
        /// <returns>parent node.</returns>
        public static GameObject GetChosenParent()
        {
            return chosenObject;
        }

        /// <summary>
        /// If <see cref="gotSelection"/> is true, the <paramref name="parent"/> will be the chosen node by the
        /// player. Otherwise it will be some dummy value.
        /// </summary>
        /// <param name="parent">The chosen node the player confirmed, if that doesn't exist, some dummy value</param>
        /// <returns><see cref="gotSelection"/></returns>
        public static bool TryGetParent(out GameObject parent)
        {
            if (gotSelection)
            {
                parent = chosenObject;
                gotSelection = false;
                return true;
            }

            parent = null;
            return false;
        }
    }
}
