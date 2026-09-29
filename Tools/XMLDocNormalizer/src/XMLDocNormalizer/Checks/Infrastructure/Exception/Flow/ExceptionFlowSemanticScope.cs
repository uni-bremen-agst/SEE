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
        /// Tries to obtain the semantic model for a syntax tree owned by this
        /// compilation scope.
        /// </summary>
        /// <param name="syntaxTree">The syntax tree whose model is required.</param>
        /// <param name="semanticModel">The owned semantic model when found.</param>
        /// <returns>
        /// <see langword="true"/> when the exact syntax-tree object belongs to
        /// this scope; otherwise <see langword="false"/>.
        /// </returns>
        internal bool TryGetSemanticModel(
            SyntaxTree syntaxTree,
            out SemanticModel semanticModel)
        {
            SemanticModel? resolvedModel =
                GetSemanticModelForCompilation(
                    Compilation,
                    syntaxTree);

            if (resolvedModel == null)
            {
                semanticModel = null!;
                return false;
            }

            semanticModel = resolvedModel;
            return true;
        }

        /// <summary>
        /// Gets the semantic model for a syntax tree when it belongs to the
        /// same compilation scope as an already available semantic model.
        /// </summary>
        /// <param name="semanticModel">
        /// The semantic model that identifies the required compilation scope.
        /// </param>
        /// <param name="syntaxTree">The syntax tree whose model is required.</param>
        /// <returns>
        /// The semantic model for <paramref name="syntaxTree"/>, or
        /// <see langword="null"/> when the tree is foreign to the identified
        /// compilation scope.
        /// </returns>
        internal static SemanticModel? GetSemanticModelForSyntaxTree(
            SemanticModel semanticModel,
            SyntaxTree syntaxTree)
        {
            if (ReferenceEquals(
                    semanticModel.SyntaxTree,
                    syntaxTree))
            {
                return semanticModel;
            }

            return GetSemanticModelForCompilation(
                semanticModel.Compilation,
                syntaxTree);
        }

        /// <summary>
        /// Gets a semantic model only when an exact syntax-tree object belongs
        /// to the supplied compilation.
        /// </summary>
        /// <param name="compilation">The owning compilation candidate.</param>
        /// <param name="syntaxTree">The syntax tree whose model is required.</param>
        /// <returns>The owned model, or <see langword="null"/>.</returns>
        internal static SemanticModel? GetSemanticModelForSyntaxTree(
            Compilation compilation,
            SyntaxTree syntaxTree)
        {
            return GetSemanticModelForCompilation(
                compilation,
                syntaxTree);
        }

        /// <summary>
        /// Resolves an exact compilation-local semantic model without adding a
        /// second cache or permitting cross-compilation binding.
        /// </summary>
        /// <param name="compilation">The owning compilation candidate.</param>
        /// <param name="syntaxTree">The syntax tree whose model is required.</param>
        /// <returns>The owned model, or <see langword="null"/>.</returns>
        private static SemanticModel? GetSemanticModelForCompilation(
            Compilation compilation,
            SyntaxTree syntaxTree)
        {
            if (!compilation.SyntaxTrees.Contains(syntaxTree))
            {
                return null;
            }

            return compilation.GetSemanticModel(syntaxTree);
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
