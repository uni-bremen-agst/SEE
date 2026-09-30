using System;
using SEE.Graphs;

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
