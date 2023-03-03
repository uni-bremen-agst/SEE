using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.GO;
using SEE.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Hides selected nodes including their descendants and all
    /// incoming and outgoing edges of this node and their descendants.
    /// </summary>
    internal class HideNodeAction : AbstractHideAction
    {
        /// <summary>
        /// Returns a new instance of <see cref="HideNodeAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new HideNodeAction();
        }

        /// <summary>
        /// Returns a new instance of <see cref="HideAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public override ReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.HideConnectedEdges;
        }

        /// <summary>
        /// Hides outgoing edges of currently selected node including the connected nodes.
        /// </summary>
        /// <returns> true if outgoing edges of currently selected node including the connected nodes were hidden </returns>
        private bool HideOutgoingEdges(GameObject selectedNode)
        {
            if (selectedNode != null && selectedNode.TryGetComponent(out NodeRef nodeRef))
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
        private bool HideIncomingEdges(GameObject selectedNode)
        {
            if (selectedNode != null && selectedNode.TryGetComponent(out NodeRef nodeRef))
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
                }
                else if (childGameObject.CompareTag(Tags.Node))
                {
                    HideNodeIncludingConnectedEdges(childGameObject);
                }
            }
            return true;
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

        /// <summary>
        /// Hides incoming edges of currently selected node including the connected nodes.
        /// </summary>
        /// <returns> true if incoming edges of currently selected node including the connected nodes were hidden </returns>
        private bool HideAllConnectedEdges(GameObject selectedNode)
        {
            return HideIncomingEdges(selectedNode) && HideOutgoingEdges(selectedNode);
        }

        protected override ISet<GameObject> Hide(GameObject selection)
        {
            HashSet<GameObject> result = new() { selection };
            return result;
        }
    }
}