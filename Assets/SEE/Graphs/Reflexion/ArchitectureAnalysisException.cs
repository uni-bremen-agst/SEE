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
