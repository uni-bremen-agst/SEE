using Michsky.UI.ModernUIPack;
using SEE.Game;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Game.Drawable.ValueHolders;
using SEE.GO;
using SEE.Net.Actions.Drawable;
using SEE.UI.Notification;
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
        /// Instantiates the parent selection menu, configures its return button,
        /// and returns the parent selector.
        /// </summary>
        /// <param name="returnCall">
        /// The callback to execute when the return button is pressed.
        /// If no callback is provided, the return button is disabled.
        /// </param>
        /// <returns>The parent selector of the instantiated menu.</returns>
        private static HorizontalSelector InitializeMenu(UnityAction returnCall)
        {
            Instance.Instantiate(parentSelectionMenuPrefab);

            GameObject returnButton =
                GameFinder.FindAttachedOrLocalDescendant(
                    Instance.gameObject,
                    "ReturnBtn");

            if (returnCall != null)
            {
                returnButton.GetComponent<ButtonManagerBasic>()
                    .clickEvent.AddListener(returnCall);
            }
            else
            {
                returnButton.SetActive(false);
            }

            return GameFinder.FindAttachedOrLocalDescendant(
                    Instance.gameObject,
                    "ParentSelection")
                .GetComponent<HorizontalSelector>();
        }

        /// <summary>
        /// Collects all Mind Map nodes that qualify as general parent candidates
        /// for the given node.
        /// </summary>
        /// <param name="attachedObjects">
        /// The attached objects object containing the Mind Map nodes.
        /// </param>
        /// <param name="addedNode">
        /// The node for which a parent should be chosen.
        /// </param>
        /// <param name="includeInactive">
        /// Whether inactive Mind Map nodes should be included.
        /// </param>
        /// <returns>
        /// All Mind Map nodes that can generally be used as parents.
        /// </returns>
        private static List<GameObject> CollectParentCandidates(
            GameObject attachedObjects,
            GameObject addedNode,
            bool includeInactive)
        {
            /// Gets all Mind Map Nodes of the given attached objects object.
            IList<GameObject> allNodes =
                attachedObjects.FindAllDescendantsWithTag(
                    Tags.MindMapNode,
                    includeInactive);

            /// Gather all Mind Map Nodes with the <see cref="GameMindMap.NodeKind"/>:
            /// <see cref="GameMindMap.NodeKind.Theme"/> or
            /// <see cref="GameMindMap.NodeKind.Subtheme"/>.
            /// Note: A <see cref="GameMindMap.NodeKind.Leaf"/> can not be a parent.
            List<GameObject> nodes = new();
            foreach (GameObject node in allNodes)
            {
                if (node.GetComponent<MMNodeValueHolder>().NodeKind
                        != GameMindMap.NodeKind.Leaf
                    && node != addedNode)
                {
                    nodes.Add(node);
                }
            }

            return nodes;
        }

        /// <summary>
        /// Creates an item in the parent selector for each provided Mind Map node.
        /// </summary>
        /// <param name="parentSelector">
        /// The selector in which the parent candidates should be displayed.
        /// </param>
        /// <param name="nodes">
        /// The Mind Map nodes that should be available for selection.
        /// </param>
        private static void PopulateParentSelector(
            HorizontalSelector parentSelector,
            IEnumerable<GameObject> nodes)
        {
            /// Create an item in the parent selector for each collected node.
            foreach (GameObject node in nodes)
            {
                parentSelector.CreateNewItem(
                    node.GetComponentInChildren<TextMeshPro>().text);
            }
        }

        /// <summary>
        /// Returns the finish button of the parent selection menu.
        /// </summary>
        /// <returns>The finish button of the instantiated menu.</returns>
        private static GameObject GetFinishButton()
        {
            return GameFinder.FindAttachedOrLocalDescendant(
                Instance.gameObject,
                "Finish");
        }

        /// <summary>
        /// Creates the parent selection menu for mind maps.
        /// It adds the necessary Handler to the selector and to the finish button.
        /// </summary>
        /// <param name="attachedObjects">The attached objects object of the chosen drawable.</param>
        /// <param name="addedNode">The node for that a parent should be chosen.</param>
        public static void Enable(GameObject attachedObjects, GameObject addedNode)
        {
            HorizontalSelector parentSelector = InitializeMenu(null);

            /// Gather all Mind Map Nodes that qualify as possible parents.
            List<GameObject> nodes =
                CollectParentCandidates(
                    attachedObjects,
                    addedNode,
                    true);

            /// Create an item in the parent selector for the collected nodes.
            PopulateParentSelector(parentSelector, nodes);

            /// Adds a handler to the parent selector so that the selected node is set as the chosen node.
            parentSelector.selectorEvent.AddListener(index =>
            {
                chosenObject = nodes[index];
            });

            parentSelector.defaultIndex = 0;

            /// The parent selection can be completed through the Finish button.
            ButtonManagerBasic finish = GetFinishButton().GetComponent<ButtonManagerBasic>();
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
        /// <param name="attachedObjects">The attached objects object of the chosen drawable.</param>
        /// <param name="addedNode">The node for that a parent should be chosen.</param>
        /// <param name="valueHolder">The new configuration in which the changes are saved.</param>
        /// <param name="returnCall">The call which should be executed, if the return button is pressed.</param>
        /// <param name="cutCopyMode">Indicates that this method was called by CutCopyPaste Action.</param>
        /// <returns>The instance of the menu. Can be null if the <see cref="DrawableType"/> isn't a <see cref="MindMapNodeConf"/>.</returns>
        public static GameObject EnableForEditing(GameObject attachedObjects, GameObject addedNode,
            DrawableType valueHolder, UnityAction returnCall, bool cutCopyMode = false)
        {
            if (valueHolder is MindMapNodeConf newConf)
            {
                HorizontalSelector parentSelector = InitializeMenu(returnCall);

                /// Collect all Mind Map Nodes with the <see cref="GameMindMap.NodeKind"/>:
                /// <see cref="GameMindMap.NodeKind.Theme"/> or
                /// <see cref="GameMindMap.NodeKind.Subtheme"/>
                /// that qualify as a new parent.
                /// Note: A <see cref="GameMindMap.NodeKind.Leaf"/> can not be a parent.
                List<GameObject> nodes =
                    CollectParentCandidates(
                        attachedObjects,
                        addedNode,
                        false);

                /// Nodes are prohibited as a parent if selecting them would create a cycle.
                nodes.RemoveAll(
                    node => !GameMindMap.ParentChangeIsValid(
                        addedNode,
                        node));

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
                PopulateParentSelector(parentSelector, nodes);

                /// Get the index of the current parent.
                int index = nodes.IndexOf(GameFinder.FindAttachedOrLocalDescendant(attachedObjects, newConf.ParentNode));

                /// If the index can't be found, take the default index 0.
                index = index < 0 ? 0 : index;

                GameObject surface = GameFinder.GetDrawableSurface(addedNode);

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
                GameObject finish = GetFinishButton();

                if (!cutCopyMode)
                {
                    finish.SetActive(false);
                }
                else
                {
                    finish.GetComponent<ButtonManagerBasic>()
                        .clickEvent.AddListener(() =>
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
        /// <returns>Parent node.</returns>
        public static GameObject GetChosenParent()
        {
            return chosenObject;
        }

        /// <summary>
        /// If <see cref="gotSelection"/> is true, the <paramref name="parent"/> will be the chosen node by the
        /// player. Otherwise it will be some dummy value.
        /// </summary>
        /// <param name="parent">The chosen node the player confirmed, if that doesn't exist, some dummy value.</param>
        /// <returns><see cref="gotSelection"/>.</returns>
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
