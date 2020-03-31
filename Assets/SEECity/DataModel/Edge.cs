namespace SEE.DataModel
{
    /// <summary>
    /// Directed and typed edges of the graph with source and target node.
    /// </summary>
    public class Edge : GraphElement
    {
        // IMPORTANT NOTES:
        //
        // If you use Clone() to create a copy of an edge, be aware that the clone
        // will have a deep copy of all attributes and the type of the edge only.
        // Source and target will be a shallow copy instead.

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="source">source of the edge</param>
        /// <param name="target">target of the edge</param>
        /// <param name="type">type of the edge</param>
        public Edge(Node source, Node target, string type)
        {
            this.source = source;
            this.target = target;
            this.Type   = type;
        }

        /// <summary>
        /// Constructor. Source, target, and type of the edge remain undefined.
        /// </summary>
        public Edge()
        {
            // intentionally left blank
        }

        /// <summary>
        /// The source of the edge.
        /// </summary
        private Node source;

        /// <summary>
        /// The source of the edge.
        /// </summary>
        public Node Source
        {
            get => source;
            set => source = value;
        }

        /// <summary>
        /// The target of the edge.
        /// </summary>
        private Node target;

        /// <summary>
        /// The target of the edge.
        /// </summary>
        public Node Target
        {
            get => target;
            set => target = value;
        }

        /// <summary>
        /// Creates deep copies of attributes where necessary. Is called by
        /// Clone() once the copy is created. Must be extended by every 
        /// subclass that adds fields that should be cloned, too.
        /// 
        /// IMPORTANT NOTE: Cloning an edge means only to create deep copies of its
        /// type and attributes. The source and target node will be shallow copies.
        /// </summary>
        /// <param name="clone">the clone receiving the copied attributes</param>
        protected override void HandleCloned(object clone)
        {
            base.HandleCloned(clone);
            Edge target = (Edge)clone;
            target.source = this.source;
            target.target = this.target;
        }

        public override string ToString()
        {
            string result = "{\n";
            result += " \"kind\": edge,\n";
            result += " \"source\":  \"" + source.LinkName + "\",\n";
            result += " \"target\": \"" + target.LinkName + "\",\n";
            result += base.ToString();
            result += "}";
            return result;
        }

        /// <summary>
        /// Creates a unique string representing the edge as the concatentation of its edge
        /// type and the two linknames of the edge's source and target node.
        /// IMPORTANT NOTE: This linkname is unique only if there is only a single edge
        /// between those nodes with the edge's type.
        /// </summary>
        /// <returns>a string from both nodes' LinkNames as follows: Type + "#" + Source.LinkName + '#' + Target.LinkName</returns>
        public string LinkName
        {
            get => Type + "#" + Source.LinkName + "#" + Target.LinkName;
        }

    }
}