using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.Game;
using SEE.GO;
using SEE.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Action to hide/show the currently selected game object (edge or node).
    /// </summary>
    internal class HideAction : AbstractPlayerAction
    {
        /// <summary>
        /// The currently selected object (a node or edge).
        /// </summary>
        private GameObject selectedObject;

        /// <summary>
        /// The list of currently selected objects.
        /// </summary>
        private HashSet<GameObject> selectedObjects = new HashSet<GameObject>();

        /// <summary>
        /// The list of currently hidden objects.
        /// </summary>
        private readonly ISet<GameObject> hiddenObjects = new HashSet<GameObject>();

        /// <summary>
        /// The list of currently transparent objects.
        /// </summary>
        private readonly ISet<GameObject> transparentObjects = new HashSet<GameObject>();

        /// <summary>
        /// The list of objects whose visibility was changed in recent undo (needed for redo).
        /// </summary>
        private readonly ISet<GameObject> undoneList = new HashSet<GameObject>();

        /// <summary>
        /// Saves all highlighted edges
        /// </summary>
        private readonly ISet<GameObject> highlightedEdges = new HashSet<GameObject>();

        /// <summary>
        /// Saves the edges that were highlighted and would then be reset by an undo.(Needed for redo)
        /// </summary>
        private readonly ISet<GameObject> redoHighlightedEdges = new HashSet<GameObject>();

        /// <summary>
        /// Saves the mode selected in <see cref="OpenDialog"/> to be used for hiding/highlighting the objects.
        /// </summary>
        private HideModeSelector mode;

        /// <summary>
        /// Returns a new instance of <see cref="HideAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new HideAction();
        }

        /// <summary>
        /// Returns a new instance of <see cref="HideAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public override ReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Opens the dialog and registers this action for local selection events.
        /// </summary>
        public override void Start()
        {
            base.Stop();
            OpenDialog();
            InteractableObject.LocalAnySelectIn += LocalAnySelectIn;
            InteractableObject.LocalAnySelectOut += LocalAnySelectOut;
        }

        /// <summary>
        /// Opens dialog to select the function to be executed.
        /// Takes over the desired selection at the end.
        /// </summary>
        private void OpenDialog()
        {
            Game.UI.PropertyDialog.HidePropertyDialog dialog = new Game.UI.PropertyDialog.HidePropertyDialog();

            dialog.OnConfirm.AddListener(OKButtonPressed);
            dialog.OnCancel.AddListener(Cancelled);

            dialog.Open();

            void OKButtonPressed()
            {
                mode = dialog.mode;
            }
            void Cancelled()
            {
                Stop();
            }
        }

        /// <summary>
        /// Clears <see cref="selectedObjects"/> and unregisters from local selection events.
        /// </summary>
        public override void Stop()
        {
            base.Stop();
            ReverseMakeUnselectedTransparent();
            selectedObjects.Clear();
            InteractableObject.LocalAnySelectIn -= LocalAnySelectIn;
            InteractableObject.LocalAnySelectOut -= LocalAnySelectOut;
        }

        /// <summary>
        /// See <see cref="ReversibleAction.Update"/>.
        /// </summary>
        /// <returns>true if completed</returns>
        public override bool Update()
        {
            MakeUnselectedTransparent();
            switch (mode)
            {
                case HideModeSelector.HideAll:
                    if (HideAllEdges())
                    {
                        currentState = ReversibleAction.Progress.Completed;
                        return true;
                    }
                    break;
                case HideModeSelector.HideSelected:
                    if (HideSelected())
                    {
                        currentState = ReversibleAction.Progress.Completed;
                        return true;
                    }
                    break;
                case HideModeSelector.HideUnselected:
                    if (HideUnselected())
                    {
                        currentState = ReversibleAction.Progress.Completed;
                        return true;
                    }
                    break;
                case HideModeSelector.HideOutgoing:
                    if (HideOutgoingEdges())
                    {
                        currentState = ReversibleAction.Progress.Completed;
                        return true;
                    }
                    break;
                case HideModeSelector.HideIncoming:
                    if (HideIncomingEdges())
                    {
                        currentState = ReversibleAction.Progress.Completed;
                        return true;
                    }
                    break;
                case HideModeSelector.HideAllEdgesOfSelected:
                    if (HideAllConnectedEdges())
                    {
                        currentState = ReversibleAction.Progress.Completed;
                        return true;
                    }
                    break;
                case HideModeSelector.HideForwardTransitiveClosure:
                    if (HideForwardTransitive())
                    {
                        currentState = ReversibleAction.Progress.Completed;
                        return true;
                    }
                    break;
                case HideModeSelector.HideBackwardTransitiveClosure:
                    if (HideBackwardTransitive())
                    {
                        currentState = ReversibleAction.Progress.Completed;
                        return true;
                    }
                    break;
                case HideModeSelector.HideAllTransitiveClosure:
                    if (HideAllTransitive())
                    {
                        currentState = ReversibleAction.Progress.Completed;
                        return true;
                    }
                    break;
                case HideModeSelector.HighlightEdges:
                    HighlightEdges();
                    currentState = ReversibleAction.Progress.Completed;
                    return true;
                default: return false;
            }
            return false;
        }

        /// <summary>
        /// Hides all selected objects when the enter key is pressed.
        /// </summary>
        /// <returns> true if all selected objects could be successfully hidden </returns>
        private bool HideSelected()
        {
            if (selectedObjects.Count > 0)
            {
                foreach (GameObject g in selectedObjects)
                {
                    Assert.IsTrue(g.HasNodeRef() || g.HasEdgeRef());
                    if (g.CompareTag(Tags.Edge))
                    {
                        HideEdge(g);
                    }
                    else if (g.CompareTag(Tags.Node))
                    {
                        HideNodeIncludingConnectedEdges(g);
                    }
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Hides all unselected objects when the enter key is pressed.
        /// </summary>
        /// <returns> true if all unselected objects could be successfully hidden </returns>
        private bool HideUnselected()
        {
            if (selectedObjects.Count > 0 && selectedObject != null)
            {
                GameObject city = selectedObject;
                while (!city.CompareTag(Tags.CodeCity))
                {
                    city = city.transform.parent.gameObject;
                }

                List<GameObject> nodesEdges = GetAllChildrenRecursively(city.transform, new List<GameObject>());

                foreach (GameObject g in selectedObjects)
                {
                    //remove all parent objects of selected object from list of nodes and edges
                    Transform parent = g.transform.parent;
                    while (parent != null)
                    {
                        nodesEdges.Remove(parent.gameObject);
                        parent = parent.transform.parent;
                    }
                    //remove selected object
                    nodesEdges.Remove(g);
                }
                foreach (GameObject g in nodesEdges)
                {
                    Assert.IsTrue(g.HasNodeRef() || g.HasEdgeRef());
                    if (g.CompareTag(Tags.Edge))
                    {
                        HideEdge(g);
                    }
                    else if (g.CompareTag(Tags.Node))
                    {
                        HideNodeIncludingConnectedEdges(g);
                    }
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Hides an edge.
        /// </summary>
        /// <param name="edge"> edge to hide </param>
        /// <returns> true if edge was hidden </returns>
        private bool HideEdge(GameObject edge)
        {
            bool rendered = false;
            if (edge.TryGetComponent(out Renderer renderer))
            {
                rendered = renderer.enabled;
            }
            if (rendered)
            {
                hiddenObjects.Add(edge);
                edge.SetVisibility(false);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Highlights edge by making the edge thicker
        /// </summary>
        /// <param name="edge">The edge to be highlighted</param>
        /// <returns>true if the edge could be made thicker</returns>
        private bool HighlightEdge(GameObject edge)
        {
            if (edge.TryGetComponent(out LineRenderer lineRenderer))
            {
                lineRenderer.widthMultiplier = 5f;
                highlightedEdges.Add(edge);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Makes all edges thinner again
        /// </summary>
        private void ReverseHighlightEdges()
        {
            foreach (GameObject edge in highlightedEdges)
            {
                if (edge.TryGetComponent(out LineRenderer lineRenderer))
                {
                    lineRenderer.widthMultiplier = 1f;
                    redoHighlightedEdges.Add(edge);
                }
            }
            highlightedEdges.Clear();
        }

        /// <summary>
        /// Hides a node including all the connected edges.
        /// </summary>
        /// <param name="node"> Node to hide </param>
        /// <returns> true if node was hidden successfully </returns>
        private bool HideNodeIncludingConnectedEdges(GameObject node)
        {
            if (node.TryGetComponent(out NodeRef nodeRef))
            {
                HashSet<string> edgeIDs = GetEdgeIds(nodeRef);

                foreach (GameObject edge in GameObject.FindGameObjectsWithTag(Tags.Edge))
                {
                    bool rendered = false;
                    if (edge.TryGetComponent(out Renderer renderer))
                    {
                        rendered = renderer.enabled;
                    }
                    if (edge.activeInHierarchy && edgeIDs.Contains(edge.name) && rendered)
                    {
                        hiddenObjects.Add(edge);
                        edge.SetVisibility(false);
                    }
                }
            }
            hiddenObjects.Add(node);
            node.SetVisibility(false);
            foreach (Transform child in node.transform)
            {
                GameObject childGameObject = child.gameObject;
                if (childGameObject.CompareTag(Tags.Edge))
                {
                    HideEdge(childGameObject);
                } else if (childGameObject.CompareTag(Tags.Node))
                {
                    HideNodeIncludingConnectedEdges(childGameObject);
                }
            }
            return true;
        }

        /// <summary>
        /// Makes all the objects that are not selected transparent.
        /// </summary>
        /// <returns>true if all unselected objects have been made transparent</returns>
        private bool MakeUnselectedTransparent()
        {
            if (selectedObject != null)
            {
                GameObject city = selectedObject;
                while (!city.CompareTag(Tags.CodeCity))
                {
                    city = city.transform.parent.gameObject;
                }

                List<GameObject> nodesEdges = GetAllChildrenRecursively(city.transform, new List<GameObject>());

                foreach (GameObject g in nodesEdges)
                {
                    if (!hiddenObjects.Contains(g) && g != null)
                    {
                        g.SetTransparency(0.5f);
                        transparentObjects.Add(g);
                    }
                }
                foreach (GameObject g in selectedObjects)
                {
                    if (!hiddenObjects.Contains(g) && g != null)
                    {
                        g.SetTransparency(1.0f);
                        transparentObjects.Remove(g);
                    }
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Makes all objects opaque again
        /// </summary>
        /// <returns>true if the objects could be made visible again</returns>
        private bool ReverseMakeUnselectedTransparent()
        {
            if (selectedObject != null)
            {
                GameObject city = SceneQueries.GetCodeCity(selectedObject.transform).gameObject;
                List<GameObject> nodesEdges = GetAllChildrenRecursively(city.transform, new List<GameObject>());

                foreach (GameObject g in transparentObjects)
                {
                    g.SetTransparency(1f);
                }
                transparentObjects.Clear();
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Hides all edges of the selected city.
        /// </summary>
        /// <returns>true if all edges could be successfully hidden </returns>
        private bool HideAllEdges()
        {
            if (selectedObject != null)
            {
                GameObject city = SceneQueries.GetCodeCity(selectedObject.transform).gameObject;
                List<GameObject> nodesEdges = GetAllChildrenRecursively(city.transform, new List<GameObject>());

                foreach (GameObject g in nodesEdges)
                {
                    if (g.CompareTag(Tags.Edge))
                    {
                        g.SetVisibility(false);
                        hiddenObjects.Add(g);
                    }
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Recursive function to get all node and edge children of a game object.
        /// </summary>
        /// <param name="transform"> Transform of the game object </param>
        /// <param name="objectList"> Current list of all node and edge children </param>
        /// <returns> list of all node and edge children of a game object </returns>
        private static List<GameObject> GetAllChildrenRecursively(Transform transform, List<GameObject> objectList)
        {
            foreach (Transform child in transform)
            {
                if (child.CompareTag(Tags.Node) || child.CompareTag(Tags.Edge))
                {
                    objectList.Add(child.gameObject);
                }
                if (child.childCount > 0)
                {
                    GetAllChildrenRecursively(child, objectList);
                }
            }
            return objectList;
        }

        /// <summary>
        /// Hides outgoing edges of currently selected node including the connected nodes.
        /// </summary>
        /// <returns> true if outgoing edges of currently selected node including the connected nodes were hidden </returns>
        private bool HideOutgoingEdges()
        {
            if (selectedObject != null && selectedObject.TryGetComponent(out NodeRef nodeRef))
            {
                HashSet<string> edgeIDs = new HashSet<string>();
                HashSet<string> nodeIDs = new HashSet<string>();

                foreach (Edge edge in nodeRef.Value.Outgoings)
                {
                    edgeIDs.Add(edge.ID);
                    nodeIDs.Add(edge.Target.ID);
                }

                foreach (GameObject edge in GameObject.FindGameObjectsWithTag(Tags.Edge))
                {
                    if (edge.activeInHierarchy && edgeIDs.Contains(edge.name))
                    {
                        hiddenObjects.Add(edge);
                        edge.SetVisibility(false);
                    }
                }
                foreach (GameObject node in GameObject.FindGameObjectsWithTag(Tags.Node))
                {

                    if (node.activeInHierarchy && nodeIDs.Contains(node.name))
                    {
                        HideNodeIncludingConnectedEdges(node);
                    }
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Hides incoming edges of currently selected node including the connected nodes.
        /// </summary>
        /// <returns> true if incoming edges of currently selected node including the connected nodes were hidden</returns>
        private bool HideIncomingEdges()
        {
            if (selectedObject != null && selectedObject.TryGetComponent(out NodeRef nodeRef))
            {
                HashSet<string> edgeIDs = new HashSet<string>();
                HashSet<string> nodeIDs = new HashSet<string>();

                foreach (Edge edge in nodeRef.Value.Incomings)
                {
                    edgeIDs.Add(edge.ID);
                    nodeIDs.Add(edge.Source.ID);
                }

                foreach (GameObject edge in GameObject.FindGameObjectsWithTag(Tags.Edge))
                {
                    if (edge.activeInHierarchy && edgeIDs.Contains(edge.name))
                    {
                        hiddenObjects.Add(edge);
                        edge.SetVisibility(false);
                    }
                }
                foreach (GameObject node in GameObject.FindGameObjectsWithTag(Tags.Node))
                {
                    if (node.activeInHierarchy && nodeIDs.Contains(node.name))
                    {
                        HideNodeIncludingConnectedEdges(node);
                    }
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Hides incoming edges of currently selected node including the connected nodes.
        /// </summary>
        /// <returns> true if incoming edges of currently selected node including the connected nodes were hidden </returns>
        private bool HideAllConnectedEdges()
        {
            return HideIncomingEdges() && HideOutgoingEdges();
        }

        /// <summary>
        /// Undoes the action.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            ReverseHighlightEdges();
            foreach (GameObject g in hiddenObjects)
            {
                g.SetVisibility(true, false);
                undoneList.Add(g);
            }
            hiddenObjects.Clear();
        }

        /// <summary>
        /// Redoes the action.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            foreach (GameObject edge in redoHighlightedEdges)
            {
                HighlightEdge(edge);
            }
            redoHighlightedEdges.Clear();
            foreach (GameObject g in undoneList)
            {
                g.SetVisibility(false, false);
                hiddenObjects.Add(g);
            }
            undoneList.Clear();
        }

        /// <summary>
        /// Hides the forward transitive closure (all nodes reachable from the selected node by going forwards).
        /// </summary>
        private bool HideForwardTransitive()
        {
            if (selectedObject != null && selectedObject.TryGetComponent(out NodeRef nodeRef))
            {
                (HashSet<string> edgeIDs, HashSet<string> nodeIDs) = ForwardTransitiveRecursive(nodeRef.Value, new HashSet<string>(), new HashSet<string>());

                foreach (GameObject edge in GameObject.FindGameObjectsWithTag(Tags.Edge))
                {
                    if (edge.activeInHierarchy && edgeIDs.Contains(edge.name))
                    {
                        hiddenObjects.Add(edge);
                        edge.SetVisibility(false);
                    }
                }
                foreach (GameObject node in GameObject.FindGameObjectsWithTag(Tags.Node))
                {
                    if (node.activeInHierarchy && nodeIDs.Contains(node.name) && !node.name.Equals(nodeRef.Value.ID))
                    {
                        HideNodeIncludingConnectedEdges(node);
                    }
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Recursive function for finding the forward transitive closure of a given node.
        /// </summary>
        /// <param name="node"> node to calculate  the forward transitive closure for</param>
        /// <param name="edgeIDs"> list of IDs of edges reachable from the node</param>
        /// <param name="nodeIDs"> list of IDs of nodes reachable from the node</param>
        /// <returns> a tuple of two hashsets of strings containing the edge IDs and the node IDs </returns>
        private static (HashSet<string>, HashSet<string>) ForwardTransitiveRecursive(Node node, HashSet<string> edgeIDs, HashSet<string> nodeIDs)
        {
            nodeIDs.Add(node.ID);
            foreach (Edge edge in node.Outgoings)
            {
                edgeIDs.Add(edge.ID);
                if (!nodeIDs.Contains(edge.Target.ID))
                {
                    ForwardTransitiveRecursive(edge.Target, edgeIDs, nodeIDs);
                }
            }
            return (edgeIDs, nodeIDs);
        }

        /// <summary>
        /// Hides the backward transitive closure (all nodes reachable from the selected node by going backwards).
        /// </summary>
        private bool HideBackwardTransitive()
        {
            if (selectedObject != null && selectedObject.TryGetComponent(out NodeRef nodeRef))
            {
                (HashSet<string> edgeIDs, HashSet<string> nodeIDs) = BackwardTransitiveRecursive(nodeRef.Value, new HashSet<string>(), new HashSet<string>());

                foreach (GameObject edge in GameObject.FindGameObjectsWithTag(Tags.Edge))
                {
                    if (edge.activeInHierarchy && edgeIDs.Contains(edge.name))
                    {
                        hiddenObjects.Add(edge);
                        edge.SetVisibility(false);
                    }
                }
                foreach (GameObject node in GameObject.FindGameObjectsWithTag(Tags.Node))
                {
                    if (node.activeInHierarchy && nodeIDs.Contains(node.name) && !node.name.Equals(nodeRef.Value.ID))
                    {
                        HideNodeIncludingConnectedEdges(node);
                    }
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Recursive function for finding the backward transitive closure of a given node.
        /// </summary>
        /// <param name="node"> node to calculate the backward transitive closure for</param>
        /// <param name="edgeIDs"> list of IDs of edges reachable from the node</param>
        /// <param name="nodeIDs"> list of IDs of nodes reachable from the node</param>
        /// <returns> a tuple of two hashsets of strings containing the edge IDs and the node IDs </returns>
        private static (HashSet<string>, HashSet<string>) BackwardTransitiveRecursive(Node node, HashSet<string> edgeIDs, HashSet<string> nodeIDs)
        {
            nodeIDs.Add(node.ID);
            foreach (Edge edge in node.Incomings)
            {
                edgeIDs.Add(edge.ID);
                if (!nodeIDs.Contains(edge.Source.ID))
                {
                    BackwardTransitiveRecursive(edge.Source, edgeIDs, nodeIDs);
                }
            }
            return (edgeIDs, nodeIDs);
        }

        /// <summary>
        /// Hides the transitive closure (all nodes reachable from the selected node).
        /// </summary>
        private bool HideAllTransitive()
        {
            return HideForwardTransitive() && HideBackwardTransitive();
        }

        /// <summary>
        /// Selects all edges that lie between the selected nodes
        /// </summary>
        /// <param name="nodes">The nodes that are selected</param>
        private static void SelectEdgesBetweenSubsetOfNodes(ICollection<GameObject> nodes)
        {
            if (nodes != null && nodes.Count > 0)
            {
                List<string> subsetNames = nodes.Select(g => g.name).ToList();
                foreach (GameObject edge in GameObject.FindGameObjectsWithTag(Tags.Edge))
                {
                    if (edge.TryGetComponent(out EdgeRef edgeRef))
                    {
                        if (subsetNames.Contains(edgeRef.Value.Source.ID) && subsetNames.Contains(edgeRef.Value.Target.ID))
                        {
                            if (edge.TryGetComponent(out InteractableObject interactable))
                            {
                                interactable.SetSelect(true, true);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Highlights all edges that lie between nodes by making the edges thicker.
        /// </summary>
        private void HighlightEdges()
        {
            SelectEdgesBetweenSubsetOfNodes(selectedObjects);
            foreach (GameObject edge in selectedObjects)
            {
                HighlightEdge(edge);
            }
        }

        /// <summary>
        /// Returns the IDs of all incoming and outgoing edges for <paramref name="nodeRef"/>.
        /// </summary>
        /// <param name="nodeRef">node whose incoming and outgoing edges are requested</param>
        /// <returns>IDs of all incoming and outgoing edges</returns>
        private static HashSet<string> GetEdgeIds(NodeRef nodeRef)
        {
            HashSet<string> edgeIDs = new HashSet<string>();
            foreach (Edge edge in nodeRef.Value.Outgoings)
            {
                edgeIDs.Add(edge.ID);
            }
            foreach (Edge edge in nodeRef.Value.Incomings)
            {
                edgeIDs.Add(edge.ID);
            }
            return edgeIDs;
        }

        private void LocalAnySelectIn(InteractableObject interactableObject)
        {
            if (interactableObject != null)
            {
                selectedObjects.Add(selectedObject = interactableObject.gameObject);
            }
        }

        private void LocalAnySelectOut(InteractableObject interactableObject)
        {
            selectedObjects.Remove(interactableObject.gameObject);
            selectedObject = null;
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.Hide"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateType.Hide;
        }

        /// <summary>
        /// Returns the set of IDs of all game objects hidden by this action.
        /// <see cref="ReversibleAction.GetChangedObjects"/>.
        /// </summary>
        /// <returns>The set of the IDs of all game objects hidden by this action</returns>
        public override HashSet<string> GetChangedObjects()
        {
            return new HashSet<string>(hiddenObjects.Select(o => o.name));
        }
    }
}
