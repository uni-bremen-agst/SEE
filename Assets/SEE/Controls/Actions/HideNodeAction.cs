using SEE.DataModel.DG;
using SEE.Game;
using SEE.GO;
using SEE.Utils;
using System.Collections.Generic;
using System.Linq;
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
            return ActionStateTypes.HideNodes;
        }

        protected override ISet<GameObject> HiddenObjects(GameObject selection)
        {
            if (selection.TryGetNode(out Node root))
            {
                // This node and all its descendants.
                IList<Node> nodes = root.PostOrderDescendants();
                // All edges related to any of the nodes.
                IEnumerable<Edge> edges = nodes.SelectMany(n => n.Incomings.Union(n.Outgoings))
                                               .Where(edge => !edge.HasToggle(GraphElement.IsVirtualToggle))
                                               .Distinct();
                HashSet<GameObject> result = new(nodes.Count + edges.Count());
                foreach (Node node in nodes)
                {
                    GameObject go = GraphElementIDMap.Find(node.ID);
                    if (go)
                    {
                        result.Add(go);
                    }
                }
                foreach (Edge edge in edges)
                {
                    GameObject go = GraphElementIDMap.Find(edge.ID);
                    if (go)
                    {
                        result.Add(go);
                    }
                }
                return result;
            }
            else
            {
                throw new System.Exception($"Game object representing a {nameof(Node)} expected");
            }
        }
    }
}