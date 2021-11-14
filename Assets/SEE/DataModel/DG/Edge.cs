using System;

namespace SEE.DataModel.DG
{
    /// <summary>
    /// Directed and typed edges of the graph with source and target node.
    /// </summary>
    public sealed class Edge : GraphElement
    {
        // IMPORTANT NOTES:
        //
        // If you use Clone() to create a copy of an edge, be aware that the clone
        // will have a deep copy of all attributes and the type of the edge only.
        // Source and target will be a shallow copy instead.

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="ID">unique ID of edge</param>
        /// <param name="source">source of the edge</param>
        /// <param name="target">target of the edge</param>
        /// <param name="type">type of the edge</param>
        public Edge(string ID, Node source, Node target, string type)
        {
            this.Source = source;
            this.Target = target;
            Type = type;
            id = ID;
        }

        /// <summary>
        /// Constructor. A random ID will be used for this edge.
        /// </summary>
        /// <param name="source">source of the edge</param>
        /// <param name="target">target of the edge</param>
        public Edge(Node source, Node target)
        {
            this.Source = source;
            this.Target = target;
            id = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Constructor. Source, target, and type of the edge remain undefined.
        /// </summary>
        /// <param name="ID">unique ID of edge</param>
        public Edge(string ID)
        {
            id = ID;
        }

        /// <summary>
        /// Constructor. Source, target, and type of the edge remain undefined.
        /// A random ID will be used for this edge.
        /// </summary>
        public Edge()
        {
            id = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// The name of the toggle attribute that marks edges that where lifted from
        /// lower level nodes to higher level nodes rather than being part of the
        /// original graph loaded. Such edges are introduced artificially.
        /// </summary>
        public const string IsLiftedToggle = "IsLifted";

        /// <summary>
        /// The source of the edge.
        /// </summary>
        public Node Source { get; set; }

        /// <summary>
        /// The target of the edge.
        /// </summary>
        public Node Target { get; set; }

        /// <summary>
        /// Returns true if <paramref name="other"/> meets all of the following conditions:
        /// (1) is not null
        /// (2) has exactly the same C# type
        /// (3) has exactly the same attributes with exactly the same values as this edge
        /// (4) has the same type name
        /// (5) the ID of its source is the same as the ID of the source of this edge
        /// (6) the ID of its target is the same as the ID of the target of this edge
        ///
        /// Note: This edge and the other edge may or may not be in the same graph.
        /// </summary>
        /// <param name="other">to be compared to</param>
        /// <returns>true if equal</returns>
        public override bool Equals(object other)
        {
            if (!base.Equals(other))
            {
                return false;
            }
            else
            {
                Edge otherEdge = other as Edge;
                bool equal = Target.ID == otherEdge.Target.ID
                    && Source.ID == otherEdge.Source.ID;
                if (!equal)
                {
                    Report(ID + ": Source or target are different.");
                }
                return equal;
            }
        }

        /// <summary>
        /// Returns a hash code.
        /// </summary>
        /// <returns>hash code</returns>
        public override int GetHashCode()
        {
            // we are using the ID which is intended to be unique
            return ID.GetHashCode();
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
            Edge cloned = (Edge)clone;
            cloned.id = id;
            cloned.Source = Source;
            cloned.Target = Target;
        }

        public override string ToString()
        {
            string result = "{\n";
            result += " \"kind\": edge,\n";
            result += " \"id\":  \"" + ID + "\",\n";
            result += " \"source\":  \"" + Source.ID + "\",\n";
            result += " \"target\": \"" + Target.ID + "\",\n";
            result += base.ToString();
            result += "}";
            return result;
        }

        /// <summary>
        /// Unique ID of this edge.
        /// </summary>
        private string id;

        /// <summary>
        /// Unique ID.
        /// </summary>
        public override string ID
        {
            get => id;
            set 
            {
                if (ItsGraph != null)
                {
                    throw new InvalidOperationException("ID must not be changed once added to graph.");
                }
                
                id = value;
            }
        }

        /// <summary>
        /// Returns true if <paramref name="edge"/> is not null.
        /// </summary>
        /// <param name="edge">edge to be compared</param>
        public static implicit operator bool(Edge edge)
        {
            return edge != null;
        }
    }
}
