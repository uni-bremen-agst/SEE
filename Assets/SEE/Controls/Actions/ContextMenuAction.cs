using System;
using System.Collections.Generic;
using System.Linq;
using SEE.Game;
using SEE.Game.SceneManipulation;
using SEE.GO;
using SEE.UI.Notification;
using SEE.UI.PopupMenu;
using SEE.UI.Window;
using SEE.UI.Window.TreeWindow;
using SEE.Utils;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Shows a context menu with available actions when the user requests it.
    /// </summary>
    public class ContextMenuAction : MonoBehaviour
    {
        /// <summary>
        /// The popup menu that is shown when the user requests the context menu.
        /// </summary>
        private PopupMenu PopupMenu;

        private void Start()
        {
            PopupMenu = gameObject.AddComponent<PopupMenu>();
        }

        private void Update()
        {
            if (SEEInput.OpenContextMenu())
            {
                // TODO: Detect multiselect and adjust options accordingly.
                HitGraphElement hit = Raycasting.RaycastInteractableObject(out _, out InteractableObject o);
                if (hit == HitGraphElement.None)
                {
                    return;
                }

                IEnumerable<PopupMenuAction> actions = o.GraphElemRef switch
                {
                    NodeRef nodeRef => GetNodeOptions(nodeRef),
                    EdgeRef edgeRef => GetEdgeOptions(edgeRef),
                    _ => throw new ArgumentOutOfRangeException()
                };

                PopupMenu.ClearActions();
                PopupMenu.AddActions(actions.Concat(GetCommonOptions(o.GraphElemRef)));

                // We want to move the popup menu to the cursor position before showing it.
                // The mouse should hover over the first menu item already rather than being just outside of it,
                // so we move the menu up and to the left a bit.
                PopupMenu.MoveTo(Input.mousePosition + new Vector3(-5, 5, 0));
                PopupMenu.ShowMenu().Forget();
            }
        }

        /// <summary>
        /// Returns the common options available for all graph elements.
        /// </summary>
        /// <param name="graphElementRef">The graph element to get the options for</param>
        /// <returns>Common options available for all graph elements</returns>
        private static IEnumerable<PopupMenuAction> GetCommonOptions(GraphElementRef graphElementRef)
        {
            return new PopupMenuAction[]
            {
                // TODO: Ask for confirmation or at least allow undo
                new("Delete", DeleteElement, '\uF1F8'),
                // TODO: Better properties view
                new("Properties", ShowProperties, '\uF05A'),
            };

            void DeleteElement()
            {
                GameElementDeleter.Delete(graphElementRef.gameObject);
            }

            void ShowProperties()
            {
                ShowNotification.Info("Node Properties", graphElementRef.Elem.ToString());
            }
        }

        /// <summary>
        /// Activates the tree window for the given graph element and returns it.
        /// </summary>
        /// <param name="graphElementRef">The graph element to activate the tree window for</param>
        /// <returns>The activated tree window</returns>
        private static TreeWindow ActivateTreeWindow(GraphElementRef graphElementRef)
        {
            WindowSpace manager = WindowSpaceManager.ManagerInstance[WindowSpaceManager.LocalPlayer];
            TreeWindow openWindow = manager.Windows.OfType<TreeWindow>().FirstOrDefault(x => x.Graph == graphElementRef.Elem.ItsGraph);
            if (openWindow == null)
            {
                // Window is not open yet, so we create it.
                GameObject city = SceneQueries.GetCodeCity(graphElementRef.transform).gameObject;
                openWindow = city.AddComponent<TreeWindow>();
                openWindow.Graph = graphElementRef.Elem.ItsGraph;
                manager.AddWindow(openWindow);
            }
            manager.ActiveWindow = openWindow;
            return openWindow;
        }

        /// <summary>
        /// Returns the options available for the given node.
        /// </summary>
        /// <param name="nodeRef">The node to get the options for</param>
        /// <returns>Options available for the given node</returns>
        private static IEnumerable<PopupMenuAction> GetNodeOptions(NodeRef nodeRef)
        {
            return new PopupMenuAction[]
            {
                new("Show in TreeView", RevealInTreeView, '\uF802'),
                // TODO: Show Code Action
                // TODO: Accept Divergence
            };

            void RevealInTreeView()
            {
                ActivateTreeWindow(nodeRef).RevealElement(nodeRef.Value).Forget();
            }
        }

        /// <summary>
        /// Returns the options available for the given edge.
        /// </summary>
        /// <param name="edgeRef">The edge to get the options for</param>
        /// <returns>Options available for the given edge</returns>
        private static IEnumerable<PopupMenuAction> GetEdgeOptions(EdgeRef edgeRef)
        {
            return new PopupMenuAction[]
            {
                new("Show at Source", RevealAtSource, '\uF802'),
                new("Show at Target", RevealAtTarget, '\uF802'),
            };

            void RevealAtSource()
            {
                ActivateTreeWindow(edgeRef).RevealElement(edgeRef.Value, viaSource: true).Forget();
            }

            void RevealAtTarget()
            {
                ActivateTreeWindow(edgeRef).RevealElement(edgeRef.Value, viaSource: false).Forget();
            }
        }
    }
}
