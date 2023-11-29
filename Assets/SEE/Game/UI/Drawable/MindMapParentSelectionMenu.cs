using Assets.SEE.Game.Drawable;
using Michsky.UI.ModernUIPack;
using SEE.Game;
using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Game.UI.Notification;
using SEE.Net.Actions.Drawable;
using SEE.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Assets.SEE.Game.UI.Drawable
{
    /// <summary>
    /// This class provides the parent selection menu for the mind map.
    /// </summary>
    public static class MindMapParentSelectionMenu
    {
        /// <summary>
        /// The location where the menu prefeb is placed.
        /// </summary>
        private const string parentSelectionMenuPrefab = "Prefabs/UI/Drawable/MMSelectParent";

        /// <summary>
        /// The instance for the menu.
        /// </summary>
        private static GameObject instance;

        /// <summary>
        /// Whether this class has an operation in store that wasn't yet fetched.
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
            instance = PrefabInstantiator.InstantiatePrefab(parentSelectionMenuPrefab,
                GameObject.Find("UI Canvas").transform, false);
            GameFinder.FindChild(instance, "ReturnBtn").SetActive(false);

            HorizontalSelector parentSelector = GameFinder.FindChild(instance, "ParentSelection").GetComponent<HorizontalSelector>();
            List<GameObject> allNodes = GameFinder.FindAllChildrenWithTag(attachedObjects, Tags.MindMapNode);
            List<GameObject> nodes = new();
            foreach (GameObject node in allNodes)
            {
                if (node.GetComponent<MMNodeValueHolder>().GetNodeKind() != GameMindMap.NodeKind.Leaf && node != addedNode)
                {
                    nodes.Add(node);
                }
            }

            foreach (GameObject node in nodes)
            {
                parentSelector.CreateNewItem(node.GetComponentInChildren<TextMeshPro>().text);
            }

            GameObject drawable = GameFinder.GetDrawable(attachedObjects);
            string drawableParentName = GameFinder.GetDrawableParentName(drawable);
            parentSelector.selectorEvent.AddListener(index =>
            {
                chosenObject = nodes[index];
            });
            parentSelector.defaultIndex = 0;

            ButtonManagerBasic finish = GameFinder.FindChild(instance, "Finish").GetComponent<ButtonManagerBasic>();
            finish.clickEvent.AddListener(() =>
            {
                gotSelection = true;
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
        public static GameObject EnableForEditing(GameObject attachedObjects, GameObject addedNode, DrawableType valueHolder, UnityAction returnCall, bool cutCopyMode = false)
        {
            if (valueHolder is MindMapNodeConf newConf)
            {
                instance = PrefabInstantiator.InstantiatePrefab(parentSelectionMenuPrefab,
                    GameObject.Find("UI Canvas").transform, false);
                if (returnCall != null)
                {
                    GameFinder.FindChild(instance, "ReturnBtn").GetComponent<ButtonManagerBasic>().clickEvent.AddListener(returnCall);
                }
                else
                {
                    GameFinder.FindChild(instance, "ReturnBtn").SetActive(false);
                }

                HorizontalSelector parentSelector = GameFinder.FindChild(instance, "ParentSelection").GetComponent<HorizontalSelector>();
                List<GameObject> allNodes = GameFinder.FindAllChildrenWithTag(attachedObjects, Tags.MindMapNode);
                List<GameObject> nodes = new();
                foreach (GameObject node in allNodes)
                {
                    if (node.GetComponent<MMNodeValueHolder>().GetNodeKind() != GameMindMap.NodeKind.Leaf
                        && node != addedNode
                        && GameMindMap.CheckValidParentChange(addedNode, node))
                    {
                        nodes.Add(node);
                    }
                }
                if (addedNode.GetComponent<MMNodeValueHolder>().GetNodeKind() == GameMindMap.NodeKind.Theme)
                {
                    ShowNotification.Warn("Unauthorized action", "A theme can't have a parent.");
                    returnCall.Invoke();
                    Disable();
                    return null;
                }
                if (nodes.Count == 0)
                {
                    ShowNotification.Warn("Add a Theme", "You need a theme for the mind map, first add one");
                    Disable();
                    return null;
                }

                foreach (GameObject node in nodes)
                {
                    parentSelector.CreateNewItem(node.GetComponentInChildren<TextMeshPro>().text);
                }
                
                int index = nodes.IndexOf(GameFinder.FindChild(attachedObjects, newConf.parentNode));
                index = index < 0 ? 0 : index;
                GameObject drawable = GameFinder.GetDrawable(addedNode);
                string drawableParent = GameFinder.GetDrawableParentName(drawable);
                if (addedNode.GetComponent<MMNodeValueHolder>().GetParentBranchLine() == null)
                {
                    chosenObject = nodes[index];
                    ChangeParent(addedNode, newConf, drawable);
                }

                parentSelector.selectorEvent.AddListener(index =>
                {
                    chosenObject = nodes[index];
                    ChangeParent(addedNode, newConf, drawable);
                });
                parentSelector.defaultIndex = index;

                if (!cutCopyMode)
                {
                    GameObject finish = GameFinder.FindChild(instance, "Finish");
                    finish.SetActive(false);
                } else
                {
                    ButtonManagerBasic finish = GameFinder.FindChild(instance, "Finish").GetComponent<ButtonManagerBasic>();
                    finish.clickEvent.AddListener(() =>
                    {
                        gotSelection = true;
                    });
                }
            }
            return instance;
        }

        /// <summary>
        /// Method that provides the change parent call.
        /// </summary>
        /// <param name="addedNode">The selected node.</param>
        /// <param name="newConf">The configuration that holds the changes.</param>
        /// <param name="drawable">The drawable on that the node is placed.</param>
        private static void ChangeParent(GameObject addedNode, MindMapNodeConf newConf, GameObject drawable)
        {
            GameMindMap.ChangeParent(addedNode, chosenObject);
            newConf.parentNode = chosenObject.name;
            new MindMapChangeParentNetAction(drawable.name, GameFinder.GetDrawableParentName(drawable), newConf).Execute();
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
                Disable();
                return true;
            }

            parent = null;
            return false;
        }

        /// <summary>
        /// Check if the instance is still active.
        /// </summary>
        /// <returns>true, if the instance is still active.</returns>
        public static bool IsActive()
        {
            return instance != null;
        }
    }
}