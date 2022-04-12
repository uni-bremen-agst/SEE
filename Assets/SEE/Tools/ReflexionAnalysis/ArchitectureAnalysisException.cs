using System;
using SEE.DataModel.DG;
using UnityEngine.Assertions;

namespace SEE.Tools.ReflexionAnalysis
{
    /// <summary>
    /// Super class for all exceptions thrown by the architecture analysis.
    /// </summary>
    public abstract class ArchitectureAnalysisException : Exception
    {
        protected ArchitectureAnalysisException()
        {
        }

        protected ArchitectureAnalysisException(string message) : base(message)
        {
        }

        protected ArchitectureAnalysisException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
    
    /// <summary>
    /// Thrown if the hierarchy is not a tree structure, i.e., if it contains cycles.
    /// </summary>
    public class CyclicHierarchyException : ArchitectureAnalysisException
    {
        public CyclicHierarchyException() : base("The hierarchy must be a tree, that is, no cycles may exist!")
        {
        }
    }

    /// <summary>
    ///  Thrown if the analysis is in an invalid state.
    /// </summary>
    public class CorruptStateException : ArchitectureAnalysisException
    {
        public CorruptStateException()
        {
        }

        public CorruptStateException(string message) : base(message)
        {
        }

        public CorruptStateException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Thrown if a redundant specified edge would come into existence as the result of an operation.
    /// </summary>
    public class RedundantSpecifiedEdgeException : ArchitectureAnalysisException
    {
        /// <summary>
        /// First of the two redundant edges.
        /// </summary>
        public readonly Edge FirstEdge;
        
        /// <summary>
        /// Second of the two redundant edges.
        /// <b>Note that this edge isn't necessarily contained in any graph!</b>
        /// </summary>
        public readonly Edge SecondEdge;

        public RedundantSpecifiedEdgeException(Edge firstEdge, Edge secondEdge) 
            : base($"Edge '{firstEdge.ToShortString()}' would be redundant to '{secondEdge.ToShortString()}'!")
        {
            FirstEdge = firstEdge;
            SecondEdge = secondEdge;
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
        public readonly ReflexionSubgraph ExpectedSubgraph;
        
        /// <summary>
        /// The graph element that was not contained in <see cref="ExpectedSubgraph"/>.
        /// </summary>
        public readonly GraphElement Element;

        public NotInSubgraphException(ReflexionSubgraph expectedSubgraph, GraphElement element) 
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

        public ExpectedSpecifiedEdgeException(Edge edge) 
            : base($"Given edge '{edge.ToShortString()}' is not a specified edge!")
        {
            Edge = edge;
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

        public IsAnOrphanException(Node node) : base($"Node '{node.ToShortString()}' does not have any parents!")
        {
            Node = node;
        }
    }

}