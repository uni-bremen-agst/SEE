using System;
using SEE.Graphs;
using UnityEngine.Assertions;

namespace SEE.Graphs.Reflexion
{
    /// <summary>
    /// Super class for all exceptions thrown by the architecture analysis.
    /// </summary>
    public abstract class ArchitectureAnalysisException : Exception
    {
        /// <summary>
        /// Constructs a new <see cref="ArchitectureAnalysisException"/>.
        /// </summary>
        protected ArchitectureAnalysisException()
        {
        }

        /// <summary>
        /// Constructs a new <see cref="ArchitectureAnalysisException"/> with the given <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        protected ArchitectureAnalysisException(string message) : base(message)
        {
        }

        /// <summary>
        /// Constructs a new <see cref="ArchitectureAnalysisException"/> with the
        /// given <paramref name="message"/> and <paramref name="innerException"/>.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        protected ArchitectureAnalysisException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Thrown if given node or edge is not contained in the correct (or any) subgraph.
    /// </summary>
    public class NotInSubgraphException : ArchitectureAnalysisException
    {
        /// <summary>
        /// Subgraph the <see cref="Element"/> was expected to be in.
        /// </summary>
        public readonly ReflexionSubgraphs ExpectedSubgraph;

        /// <summary>
        /// The graph element that was not contained in <see cref="ExpectedSubgraph"/>.
        /// </summary>
        public readonly GraphElement Element;

        /// <summary>
        /// Constructs a new <see cref="NotInSubgraphException"/> with the given
        /// <paramref name="expectedSubgraph"/> and <paramref name="element"/>.
        /// </summary>
        /// <param name="expectedSubgraph">The subgraph the <paramref name="element"/> was expected to be in.</param>
        /// <param name="element">The graph element that was not contained in <paramref name="expectedSubgraph"/>.</param>
        public NotInSubgraphException(ReflexionSubgraphs expectedSubgraph, GraphElement element)
            : base($"Given {element.GetType().Name} '{element.ToShortString()}' must be contained in the {expectedSubgraph} graph!")
        {
            ExpectedSubgraph = expectedSubgraph;
            Element = element;
        }
    }

    /// <summary>
    /// Thrown if an unspecified edge was given when a specified edge was expected in an operation.
    /// </summary>
    public class ExpectedSpecifiedEdgeException : ArchitectureAnalysisException
    {
        /// <summary>
        /// The edge that was unexpectedly unspecified.
        /// </summary>
        public readonly Edge Edge;

        /// <summary>
        /// Constructs a new <see cref="ExpectedSpecifiedEdgeException"/> with the given <paramref name="edge"/>.
        /// </summary>
        /// <param name="edge">The edge that was unexpectedly unspecified.</param>
        public ExpectedSpecifiedEdgeException(Edge edge)
            : base($"Given edge '{edge.ToShortString()}' is not a specified edge!")
        {
            Edge = edge;
        }
    }

    /// <summary>
    /// Thrown if a specified edge was given when a propagated edge was expected in an operation.
    /// </summary>
    public class ExpectedPropagatedEdgeException : ArchitectureAnalysisException
    {
        /// <summary>
        /// The edge that was unexpectedly specified.
        /// </summary>
        public readonly Edge Edge;

        /// <summary>
        /// Constructs a new <see cref="ExpectedPropagatedEdgeException"/> with the given <paramref name="edge"/>.
        /// </summary>
        /// <param name="edge">The edge that was unexpectedly propagated.</param>
        public ExpectedPropagatedEdgeException(Edge edge)
            : base($"Given edge '{edge.ToShortString()}' is a specified (not propagated) edge!")
        {
            Edge = edge;
        }
    }

    /// <summary>
    /// Thrown if a propagated edge can't be created because such a propagated edge already exists.
    /// </summary>
    public class AlreadyPropagatedException : ArchitectureAnalysisException
    {
        /// <summary>
        /// The edge that was already propagated.
        /// </summary>
        public readonly Edge PropagatedEdge;

        /// <summary>
        /// The edge from which <see cref="PropagatedEdge"/> originates from.
        ///
        /// NOTE: This is the edge that was passed along to the function which is supposed to construct the
        /// propagated edge. As such, it may not necessarily be accurate.
        /// </summary>
        public readonly Edge OriginatingEdge;

        /// <summary>
        /// Constructs a new <see cref="AlreadyPropagatedException"/> with the given <paramref name="propagatedEdge"/>
        /// and <paramref name="originatingEdge"/>.
        /// </summary>
        /// <param name="propagatedEdge">The edge that was already propagated.</param>
        /// <param name="originatingEdge">The edge from which <paramref name="propagatedEdge"/> originates.</param>
        public AlreadyPropagatedException(Edge propagatedEdge, Edge originatingEdge)
            : base($"Propagated edge already exists: '{propagatedEdge.ToShortString()}' "
                   + $"(originated from '{originatingEdge.ToShortString()}')!")
        {
            PropagatedEdge = propagatedEdge;
            OriginatingEdge = originatingEdge;
        }
    }

    /// <summary>
    /// Thrown if an already explicitly mapped node is mapped somewhere else.
    /// </summary>
    public class AlreadyExplicitlyMappedException : ArchitectureAnalysisException
    {
        /// <summary>
        /// The node that is already mapped to <see cref="MappedTo"/>.
        /// </summary>
        public readonly Node AlreadyMapped;

        /// <summary>
        /// The node that <see cref="AlreadyMapped"/> is mapped to.
        /// </summary>
        public readonly Node MappedTo;

        /// <summary>
        /// Constructs a new <see cref="AlreadyExplicitlyMappedException"/> with the given <paramref name="alreadyMapped"/>
        /// and <paramref name="mappedTo"/>.
        /// </summary>
        /// <param name="alreadyMapped">The node that is already mapped to <paramref name="mappedTo"/>.</param>
        /// <param name="mappedTo">The node that <paramref name="alreadyMapped"/> is mapped to.</param>
        public AlreadyExplicitlyMappedException(Node alreadyMapped, Node mappedTo)
            : base($"Node '{alreadyMapped.ToShortString()}' is already explicitly mapped to '{mappedTo.ToShortString()}'.")
        {
            Assert.IsNotNull(mappedTo);
            AlreadyMapped = alreadyMapped;
            MappedTo = mappedTo;
        }
    }

    /// <summary>
    /// Thrown if a node is not explicitly mapped, but was expected to.
    /// </summary>
    public class NotExplicitlyMappedException : ArchitectureAnalysisException
    {
        /// <summary>
        /// The node that is not explicitly mapped.
        /// </summary>
        public readonly Node UnmappedNode;

        /// <summary>
        /// Constructs a new <see cref="NotExplicitlyMappedException"/> with the given <paramref name="unmappedNode"/>.
        /// </summary>
        /// <param name="unmappedNode">The node that is not explicitly mapped.</param>
        public NotExplicitlyMappedException(Node unmappedNode)
            : base($"Implementation node '{unmappedNode.ToShortString()}' is not explicitly mapped.")
        {
            Assert.IsTrue(unmappedNode.IsInImplementation());
            UnmappedNode = unmappedNode;
        }
    }

    /// <summary>
    /// Thrown if a new graph element was already added to a graph.
    /// </summary>
    public class AlreadyContainedException : ArchitectureAnalysisException
    {
        /// <summary>
        /// The element that already exists in the graph.
        /// </summary>
        public readonly GraphElement ExistingElement;

        /// <summary>
        /// Constructs a new <see cref="AlreadyContainedException"/> with the given <paramref name="existingElement"/>.
        /// </summary>
        /// <param name="existingElement">The element that already exists in the graph.</param>
        public AlreadyContainedException(GraphElement existingElement)
            : base($"'{existingElement.ToShortString()}' is already present in the graph!")
        {
            ExistingElement = existingElement;
        }
    }

    /// <summary>
    /// Thrown if a node is not an orphan (i.e., has a parent) when it's expected to be one.
    /// </summary>
    public class NotAnOrphanException : ArchitectureAnalysisException
    {
        /// <summary>
        /// The node that is not an orphan.
        /// </summary>
        public readonly Node Node;

        /// <summary>
        /// Constructs a new <see cref="NotAnOrphanException"/> with the given <paramref name="node"/>.
        /// </summary>
        /// <param name="node">The node that is not an orphan.</param>
        public NotAnOrphanException(Node node)
            : base($"Node '{node.ToShortString()}' is already a child of '{node.Parent.ToShortString()}'!")
        {
            Node = node;
        }
    }

    /// <summary>
    /// Thrown if a node is an orphan (i.e., has a parent) when it's not expected to be one.
    /// </summary>
    public class IsAnOrphanException : ArchitectureAnalysisException
    {
        /// <summary>
        /// The node that is an orphan.
        /// </summary>
        public readonly Node Node;

        /// <summary>
        /// Constructs a new <see cref="IsAnOrphanException"/> with the given <paramref name="node"/>.
        /// </summary>
        /// <param name="node">The node that is an orphan.</param>
        public IsAnOrphanException(Node node) : base($"Node '{node.ToShortString()}' does not have any parents!")
        {
            Node = node;
        }
    }
}
