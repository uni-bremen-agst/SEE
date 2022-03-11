using System;

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
    /// Thrown if the hierarchy is not a tree structure.
    /// </summary>
    public class HierarchyIsNotATreeException : ArchitectureAnalysisException
    {
        public HierarchyIsNotATreeException()
        {
        }

        public HierarchyIsNotATreeException(string message) : base(message)
        {
        }

        public HierarchyIsNotATreeException(string message, Exception innerException) : base(message, innerException)
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

}