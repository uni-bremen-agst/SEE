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
    /// This class provides the node kind selection menu for the mind map.
    /// </summary>
    public static class MindMapChangeNodeKindMenu
    {
        /// <summary>
        /// The location where the menu prefeb is placed.
        /// </summary>
        private const string nodeKindSelectionMenuPrefab = "Prefabs/UI/Drawable/MMChangeNodeKind";

        /// <summary>
        /// The instance for the menu.
        /// </summary>
        private static GameObject instance;

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
                instance = PrefabInstantiator.InstantiatePrefab(nodeKindSelectionMenuPrefab,
                    GameObject.Find("UI Canvas").transform, false);
                GameFinder.FindChild(instance, "ReturnBtn").GetComponent<ButtonManagerBasic>().clickEvent.AddListener(returnCall);

                nodeKindSelector = GameFinder.FindChild(instance, "Selection").GetComponent<HorizontalSelector>();
                foreach (GameMindMap.NodeKind kind in GameMindMap.GetNodeKinds())
                {
                    nodeKindSelector.CreateNewItem(kind.ToString());
                }

                int index = GameMindMap.GetNodeKinds().IndexOf(newConf.nodeKind);
                GameObject drawable = GameFinder.GetDrawable(addedNode);
                string drawableParent = GameFinder.GetDrawableParentName(drawable);

                nodeKindSelector.selectorEvent.AddListener(index =>
                {
                    GameMindMap.NodeKind newNodeKind = GameMindMap.GetNodeKinds()[index];
                    GameMindMap.NodeKind oldKind = newConf.nodeKind;
                    GameMindMap.ChangeNodeKind(addedNode, newNodeKind);
                    new MindMapChangeNodeKindNetAction(drawable.name, drawableParent, newConf, newNodeKind).Execute();
                    newConf.nodeKind = newNodeKind;

                    if (GameMindMap.CheckValidNodeKindChange(addedNode, newNodeKind, oldKind) || newNodeKind == oldKind)
                    {
                        if (newNodeKind != GameMindMap.NodeKind.Theme && addedNode.GetComponent<MMNodeValueHolder>().GetParent() == null)
                        {
                            MindMapParentSelectionMenu.Disable();
                            GameObject pMenu = MindMapParentSelectionMenu.EnableForEditing(GameFinder.GetAttachedObjectsObject(drawable), addedNode, newConf, null);
                            GameFinder.FindChild(pMenu, "Dragger").GetComponent<WindowDragger>().enabled = false;
                            pMenu.transform.SetParent(GameFinder.FindChild(instance, "Content").transform);
                        }
                        else
                        {
                            MindMapParentSelectionMenu.Disable();

                        }
                    }
                    else
                    {
                        ShowNotification.Warn("Can't transform", "The new chosen node kind can't apply to this node.");
                    }

                });
                nodeKindSelector.defaultIndex = index;
            }
        }

        /// <summary>
        /// Get the current selected node kind.
        /// </summary>
        /// <returns>The selected node kind.</returns>
        public static GameMindMap.NodeKind GetSelectedNodeKind()
        {
            return GameMindMap.GetNodeKinds()[nodeKindSelector.index];
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
    }
}