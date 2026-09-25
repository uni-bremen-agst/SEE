namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Provides the compilation-bound semantic capabilities required by the
    /// exception-flow analyzer.
    /// </summary>
    /// <remarks>
    /// One environment belongs to one analysis context. Its host-supplied
    /// partial implementation provides semantic-model lookup, analysis-scope
    /// enumeration, exact supporting-source method resolution, and exact
    /// external supporting-scope lookup. The nonvirtual compile-time seam
    /// preserves one Roslyn type universe and fails closed when identity or
    /// ownership cannot be proven.
    /// </remarks>
    internal sealed partial class ExceptionFlowSemanticEnvironment
    {
    }
}
