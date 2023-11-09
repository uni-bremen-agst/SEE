using System;
using System.Collections.Generic;
using System.Linq;
using SEE.Game;
using SEE.Game.SceneManipulation;
using SEE.GO;
using SEE.Tools.ReflexionAnalysis;
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
                // TODO (#664): Detect if multiple elements are selected and adjust options accordingly.
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
                PopupMenu.MoveTo(Input.mousePosition);
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
            IList<PopupMenuAction> actions = new List<PopupMenuAction>
            {
                // TODO (#665): Ask for confirmation or allow undo.
                new("Delete", DeleteElement, '\uF1F8'),
                // TODO (#666): Better properties view
                new("Properties", ShowProperties, '\uF05A'),
            };

            if (graphElementRef.Elem.Filename() != null)
            {
                actions.Add(new("Show Code", ShowCode, '\uF121'));
            }

            return actions;

            void DeleteElement()
            {
                GameElementDeleter.Delete(graphElementRef.gameObject);
            }

            void ShowProperties()
            {
                ShowNotification.Info("Node Properties", graphElementRef.Elem.ToString(), log: false);
            }

            void ShowCode()
            {
                ActivateWindow(ShowCodeAction.ShowCode(graphElementRef));
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
        /// Activates the given window, that is, adds it to the window space and makes it the active window.
        /// </summary>
        /// <param name="window">The window to activate</param>
        private static void ActivateWindow(BaseWindow window)
        {
            WindowSpace manager = WindowSpaceManager.ManagerInstance[WindowSpaceManager.LocalPlayer];
            if (!manager.Windows.Contains(window))
            {
                manager.AddWindow(window);
            }
            manager.ActiveWindow = window;
        }

        /// <summary>
        /// Returns the options available for the given node.
        /// </summary>
        /// <param name="nodeRef">The node to get the options for</param>
        /// <returns>Options available for the given node</returns>
        private static IEnumerable<PopupMenuAction> GetNodeOptions(NodeRef nodeRef)
        {
            IList<PopupMenuAction> actions = new List<PopupMenuAction>
            {
                new("Show in TreeView", RevealInTreeView, '\uF802'),
            };

            return actions;

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
            IList<PopupMenuAction> actions = new List<PopupMenuAction>
            {
                new("Show at Source", RevealAtSource, '\uF802'),
                new("Show at Target", RevealAtTarget, '\uF802'),
            };

            if (edgeRef.Value.Type == "Clone")
            {
                actions.Add(new("Show Unified Diff", ShowUnifiedDiff, '\uE13A'));
            }

            if (edgeRef.Value.IsInImplementation() && ReflexionGraph.IsDivergent(edgeRef.Value))
            {
                actions.Add(new("Accept Divergence", AcceptDivergence, '\uF00C'));
            }

            return actions;

            void RevealAtSource()
            {
                ActivateTreeWindow(edgeRef).RevealElement(edgeRef.Value, viaSource: true).Forget();
            }

            void RevealAtTarget()
            {
                ActivateTreeWindow(edgeRef).RevealElement(edgeRef.Value, viaSource: false).Forget();
            }

            void ShowUnifiedDiff()
            {
                ActivateWindow(ShowCodeAction.ShowUnifiedDiff(edgeRef));
            }

            void AcceptDivergence()
            {
                AcceptDivergenceAction.CreateConvergentEdge(edgeRef.Value);
            }
        }
    }
}
