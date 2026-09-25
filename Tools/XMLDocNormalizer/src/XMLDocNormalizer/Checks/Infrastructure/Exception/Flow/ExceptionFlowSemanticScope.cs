using Microsoft.CodeAnalysis;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Represents the analyzer-visible portion of one source-backed Roslyn
    /// compilation.
    /// </summary>
    /// <remarks>
    /// A scope is bound to exactly one Roslyn compilation and may be shared
    /// for the lifetime of one analysis environment. It deliberately carries
    /// no Workspace project, acquisition, catalog, or reporting-role state.
    /// </remarks>
    internal class ExceptionFlowSemanticScope
    {
        /// <summary>
        /// Caches source-declared named types after their first access.
        /// </summary>
        private IReadOnlyList<INamedTypeSymbol>? sourceTypes;

        /// <summary>
        /// Initializes an analyzer semantic scope.
        /// </summary>
        /// <param name="compilation">The represented source compilation.</param>
        public ExceptionFlowSemanticScope(Compilation compilation)
        {
            Compilation = compilation;
        }

        /// <summary>
        /// Gets the represented compilation.
        /// </summary>
        /// <value>The compilation defining this Roslyn symbol universe.</value>
        public Compilation Compilation { get; }

        /// <summary>
        /// Gets all named source types declared by the compilation, including
        /// nested types.
        /// </summary>
        /// <value>The cached deterministic source-type sequence.</value>
        public IReadOnlyList<INamedTypeSymbol> SourceTypes
        {
            get
            {
                sourceTypes ??= CollectSourceTypes(Compilation);
                return sourceTypes;
            }
        }

        /// <summary>
        /// Collects source-declared named types from one compilation.
        /// </summary>
        /// <param name="compilation">The compilation to inspect.</param>
        /// <returns>The deterministically ordered source types.</returns>
        private static IReadOnlyList<INamedTypeSymbol> CollectSourceTypes(Compilation compilation)
        {
            List<INamedTypeSymbol> collectedTypes = new();
            CollectSourceTypes(compilation.Assembly.GlobalNamespace, collectedTypes);

            collectedTypes.Sort(
                static (left, right) =>
                    StringComparer.Ordinal.Compare(
                        left.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        right.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));

            return collectedTypes;
        }

        /// <summary>
        /// Recursively collects source types from one namespace.
        /// </summary>
        /// <param name="namespaceSymbol">The namespace to traverse.</param>
        /// <param name="collectedTypes">The destination list.</param>
        private static void CollectSourceTypes(
            INamespaceSymbol namespaceSymbol,
            List<INamedTypeSymbol> collectedTypes)
        {
            foreach (INamedTypeSymbol typeSymbol in namespaceSymbol.GetTypeMembers())
            {
                CollectSourceType(typeSymbol, collectedTypes);
            }

            foreach (INamespaceSymbol nestedNamespace in namespaceSymbol.GetNamespaceMembers())
            {
                CollectSourceTypes(nestedNamespace, collectedTypes);
            }
        }

        /// <summary>
        /// Adds one source type and all nested source types.
        /// </summary>
        /// <param name="typeSymbol">The type to inspect.</param>
        /// <param name="collectedTypes">The destination list.</param>
        private static void CollectSourceType(
            INamedTypeSymbol typeSymbol,
            List<INamedTypeSymbol> collectedTypes)
        {
            if (!typeSymbol.DeclaringSyntaxReferences.IsDefaultOrEmpty)
            {
                collectedTypes.Add(typeSymbol);
            }

            foreach (INamedTypeSymbol nestedType in typeSymbol.GetTypeMembers())
            {
                CollectSourceType(nestedType, collectedTypes);
            }
        }
    }
}
