namespace SEE.Layout
{
    public class CoseCoarsenGraph : CoseGraph
    {
        /// <summary>
        /// the layout
        /// </summary>
        private CoseLayout layout;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="parent">the parent node</param>
        /// <param name="graphManager">the graphmanager for this graph</param>
        public CoseCoarsenGraph(CoseNode parent, CoseGraphManager graphManager) : base(parent, graphManager)
        {
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="layout">the layout</param>
        public CoseCoarsenGraph(CoseLayout layout) : base(null, layout.GraphManager)
        {
            this.layout = layout;
        }

        /// <summary>
        /// Coarsen this graph
        /// </summary>
        public void Coarsen()
        {
            UnmatchAll();

            CoseCoarsenNode node1;
            CoseCoarsenNode node2;

            if (Nodes.Count > 0)
            {
                while (!((CoseCoarsenNode)Nodes[0]).Matched)
                {
                    node1 = (CoseCoarsenNode)Nodes[0];
                    node2 = node1.GetMatching();

                    Contract(node1, node2);
                }

                foreach (CoseCoarsenNode node in Nodes)
                {
                    CoseNode newNode = layout.NewNode();
                    newNode.LayoutValues.Pred1 = node.Node1.Reference;
                    node.Node1.Reference.LayoutValues.Next = newNode;

                    if (node.Node2 != null)
                    {
                        newNode.LayoutValues.Pred2 = node.Node2.Reference;
                        node.Node2.Reference.LayoutValues.Next = newNode;
                    }

                    node.Reference = newNode;
                }
            }
        }

        /// <summary>
        /// unmatches all nodes of this graph
        /// </summary>
        private void UnmatchAll()
        {
            foreach (CoseCoarsenNode node in Nodes)
            {
                node.Matched = false;
            }
        }

        /// <summary>
        /// Creates a matching node for the two given nodes
        /// </summary>
        /// <param name="node1">the first node</param>
        /// <param name="node2">the second node</param>
        private void Contract(CoseCoarsenNode node1, CoseCoarsenNode node2)
        {
            CoseCoarsenNode node3 = new CoseCoarsenNode();
            AddNode(node3);
            node3.Node1 = node1;

            foreach (CoseCoarsenNode node in node1.GetNeighborsList())
            {
                if (node != node3)
                {
                    Add(new CoseCoarsenEdge(), node3, node);
                }
            }
            node3.Weight = node1.Weight;

            Remove(node1);

            if (node2 != null)
            {
                node3.Node2 = node2;

                foreach (CoseCoarsenNode node in node2.GetNeighborsList())
                {
                    if (node != node3)
                    {
                        Add(new CoseCoarsenEdge(), node3, node);
                    }
                }
                node3.Weight += node2.Weight;

                Remove(node2);
            }

            node3.Matched = true;
        }
    }
}

