using System;

namespace SEE.DataModel.DG
{
    /// <summary>
    /// Directed and typed edges of the graph with source and target node.
    /// </summary>
    public sealed class Edge : GraphElement
    {
        /// <summary>
        /// The most general edge type for all dependencies extracted from the code.
        /// This name must correspond to Axivion's nomenclatura.
        /// </summary>
        public const string SourceDependency = "Source_Dependency";

        /// <summary>
        /// Edge type for absences (reflexion analysis).
        /// This name must correspond to Axivion's nomenclatura.
        /// </summary>
        public const string Absence = "Absence";
        /// <summary>
        /// Edge type for convergences (reflexion analysis).
        /// This name must correspond to Axivion's nomenclatura.
        /// </summary>
        public const string Convergence = "Convergence";
        /// <summary>
        /// Edge type for divergences (reflexion analysis).
        /// This name must correspond to Axivion's nomenclatura.
        /// </summary>
        public const string Divergence = "Divergence";

        // IMPORTANT NOTES:
        //
        // If you use Clone() to create a copy of an edge, be aware that the clone
        // will have a deep copy of all attributes and the type of the edge only.
        // Source and target will be a shallow copy instead.

        /// <summary>
        /// Constructor.
        ///
        /// Note: The edge ID will be created lazily upon the first access to <see cref="ID"/>.
        /// An edge ID can be set and changed as long as the edge is not yet added to a graph.
        /// </summary>
        /// <param name="source">Source of the edge.</param>
        /// <param name="target">Target of the edge.</param>
        /// <param name="type">Type of the edge.</param>
        public Edge(Node source, Node target, string type)
        {
            Source = source;
            Target = target;
            Type = type;
        }

        /// <summary>
        /// Constructor. Source, target, and type of the edge remain undefined.
        ///
        /// Note: The edge ID will be created lazily upon the first access to <see cref="ID"/>.
        /// An edge ID can be set and changed as long as the edge is not yet added to a graph.
        /// </summary>
        public Edge()
        {
        }

        /// <summary>
        /// The name of the toggle attribute that marks edges that where lifted from
        /// lower level nodes to higher level nodes rather than being part of the
        /// original graph loaded. Such edges are introduced artificially.
        /// </summary>
        public const string IsLiftedToggle = "IsLifted";

        /// <summary>
        /// The name of the toggle attribute that marks hidden edges.
        /// Hidden edges exist in the scene, but have an alpha value set to zero.
        /// The <see cref="ShowEdges"/> action will only display these edges when hovering over them.
        /// </summary>
        public const string IsHiddenToggle = "IsHidden";

        /// <summary>
        /// The source of the edge.
        /// </summary>
        public Node Source { get; set; }

        /// <summary>
        /// The target of the edge.
        /// </summary>
        public Node Target { get; set; }

        /// <summary>
        /// Creates deep copies of attributes where necessary. Is called by
        /// Clone() once the copy is created. Must be extended by every
        /// subclass that adds fields that should be cloned, too.
        ///
        /// IMPORTANT NOTE: Cloning an edge means only to create deep copies of its
        /// type and attributes. The source and target node will be shallow copies.
        /// </summary>
        /// <param name="clone">The clone receiving the copied attributes.</param>
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

        public override string ToShortString()
        {
            return $"({Source.ToShortString()}) --({Type})-> ({Target.ToShortString()})";
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
            get
            {
                if (string.IsNullOrEmpty(id))
                {
                    id = GetGeneratedID(Source, Target, Type);
                }
                return id;
            }
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
        /// Returns the auto-generated ID of an edge with the given source, target, and type.
        /// </summary>
        /// <param name="source">The source node of the edge.</param>
        /// <param name="target">The target node of the edge.</param>
        /// <param name="type">The type of the edge.</param>
        /// <returns>The auto-generated ID of an edge with the given source, target, and type.</returns>
        public static string GetGeneratedID(Node source, Node target, string type)
        {
            return type + "#" + source.ID + "#" + target.ID;
        }

        /// <summary>
        /// Returns true if <paramref name="edge"/> is not null.
        /// </summary>
        /// <param name="edge">Edge to be compared.</param>
        public static implicit operator bool(Edge edge)
        {
            return edge != null;
        }
    }
}
