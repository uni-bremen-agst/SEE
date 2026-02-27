using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Models;

namespace XMLDocNormalizer.Checks.Infrastructure
{
    /// <summary>
    /// Collects per-file documentation-related totals (denominators) for coverage metrics.
    /// </summary>
    /// <remarks>
    /// This collector traverses the syntax tree of a single file and counts occurrences of relevant
    /// code elements such as namespaces, types, members, parameters and type parameters.
    ///
    /// The collected totals are intended to be accumulated into <c>RunResult.Totals</c> and can be
    /// used in combination with smell counts to compute coverage percentages such as:
    /// - "x% of parameters are missing &lt;param&gt; tags"
    /// - "x% of returnable members are missing &lt;returns&gt;"
    ///
    /// This class does not emit findings. It is purely a statistics collector.
    /// </remarks>
    internal sealed class DocumentationStatisticsCollector : CSharpSyntaxWalker
    {
        private readonly Dictionary<string, int> totals;

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentationStatisticsCollector"/> class.
        /// </summary>
        /// <remarks>
        /// <see cref="SyntaxWalkerDepth.StructuredTrivia"/> is used to ensure the walker behaves consistently
        /// across files that include structured trivia. The collector itself counts declarations and does
        /// not rely on trivia content.
        /// </remarks>
        public DocumentationStatisticsCollector()
            : base(SyntaxWalkerDepth.StructuredTrivia)
        {
            totals = new Dictionary<string, int>(StringComparer.Ordinal);
        }

        /// <summary>
        /// Collects totals from the specified syntax tree.
        /// </summary>
        /// <param name="tree">The syntax tree to analyze.</param>
        /// <returns>A dictionary containing per-file totals keyed by <see cref="StatisticsKeys"/>.</returns>
        public static IReadOnlyDictionary<string, int> Collect(SyntaxTree tree)
        {
            ArgumentNullException.ThrowIfNull(tree);

            DocumentationStatisticsCollector collector = new DocumentationStatisticsCollector();
            SyntaxNode root = tree.GetRoot();
            collector.Visit(root);

            return collector.totals;
        }

        /// <inheritdoc/>
        public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
        {
            Increment(StatisticsKeys.NamespaceDeclarationsTotal);
            base.VisitNamespaceDeclaration(node);
        }

        /// <inheritdoc/>
        public override void VisitFileScopedNamespaceDeclaration(FileScopedNamespaceDeclarationSyntax node)
        {
            Increment(StatisticsKeys.NamespaceDeclarationsTotal);
            base.VisitFileScopedNamespaceDeclaration(node);
        }

        /// <inheritdoc/>
        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            Increment(StatisticsKeys.ClassDeclarationsTotal);
            AddTypeParameters(node.TypeParameterList);
            base.VisitClassDeclaration(node);
        }

        /// <inheritdoc/>
        public override void VisitStructDeclaration(StructDeclarationSyntax node)
        {
            Increment(StatisticsKeys.StructDeclarationsTotal);
            AddTypeParameters(node.TypeParameterList);
            base.VisitStructDeclaration(node);
        }

        /// <inheritdoc/>
        public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            Increment(StatisticsKeys.InterfaceDeclarationsTotal);
            AddTypeParameters(node.TypeParameterList);
            base.VisitInterfaceDeclaration(node);
        }

        /// <inheritdoc/>
        public override void VisitEnumDeclaration(EnumDeclarationSyntax node)
        {
            Increment(StatisticsKeys.EnumDeclarationsTotal);
            base.VisitEnumDeclaration(node);
        }

        /// <inheritdoc/>
        public override void VisitEnumMemberDeclaration(EnumMemberDeclarationSyntax node)
        {
            Increment(StatisticsKeys.EnumMembersTotal);
            base.VisitEnumMemberDeclaration(node);
        }

        /// <inheritdoc/>
        public override void VisitDelegateDeclaration(DelegateDeclarationSyntax node)
        {
            Increment(StatisticsKeys.DelegateDeclarationsTotal);
            AddTypeParameters(node.TypeParameterList);
            AddParameters(node.ParameterList);
            AddReturnsRequired(node.ReturnType);
            base.VisitDelegateDeclaration(node);
        }

        /// <inheritdoc/>
        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            Increment(StatisticsKeys.MethodsTotal);
            AddTypeParameters(node.TypeParameterList);
            AddParameters(node.ParameterList);
            AddReturnsRequired(node.ReturnType);
            base.VisitMethodDeclaration(node);
        }

        /// <inheritdoc/>
        public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            Increment(StatisticsKeys.ConstructorsTotal);
            AddParameters(node.ParameterList);
            base.VisitConstructorDeclaration(node);
        }

        /// <inheritdoc/>
        public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            Increment(StatisticsKeys.PropertiesTotal);
            base.VisitPropertyDeclaration(node);
        }

        /// <inheritdoc/>
        public override void VisitIndexerDeclaration(IndexerDeclarationSyntax node)
        {
            Increment(StatisticsKeys.IndexersTotal);
            AddBracketedParameters(node.ParameterList);
            base.VisitIndexerDeclaration(node);
        }

        /// <inheritdoc/>
        public override void VisitEventDeclaration(EventDeclarationSyntax node)
        {
            Increment(StatisticsKeys.EventsTotal);
            base.VisitEventDeclaration(node);
        }

        /// <inheritdoc/>
        public override void VisitEventFieldDeclaration(EventFieldDeclarationSyntax node)
        {
            Increment(StatisticsKeys.EventsTotal);
            base.VisitEventFieldDeclaration(node);
        }

        /// <inheritdoc/>
        public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            Increment(StatisticsKeys.FieldsTotal);
            base.VisitFieldDeclaration(node);
        }

        /// <inheritdoc/>
        public override void VisitOperatorDeclaration(OperatorDeclarationSyntax node)
        {
            Increment(StatisticsKeys.OperatorsTotal);
            AddParameters(node.ParameterList);
            AddReturnsRequired(node.ReturnType);
            base.VisitOperatorDeclaration(node);
        }

        /// <inheritdoc/>
        public override void VisitConversionOperatorDeclaration(ConversionOperatorDeclarationSyntax node)
        {
            Increment(StatisticsKeys.ConversionsTotal);
            AddParameters(node.ParameterList);
            AddReturnsRequired(node.Type);
            base.VisitConversionOperatorDeclaration(node);
        }

        /// <summary>
        /// Increments a total counter by one.
        /// </summary>
        /// <param name="key">The statistics key to increment.</param>
        private void Increment(string key)
        {
            totals[key] = totals.GetValueOrDefault(key) + 1;
        }

        /// <summary>
        /// Adds the number of parameters in the provided parameter list to the total parameter counter.
        /// </summary>
        /// <param name="parameters">The parameter list or null.</param>
        private void AddParameters(ParameterListSyntax? parameters)
        {
            if (parameters == null)
            {
                return;
            }

            int count = parameters.Parameters.Count;
            if (count <= 0)
            {
                return;
            }

            totals[StatisticsKeys.ParametersTotal] =
                totals.GetValueOrDefault(StatisticsKeys.ParametersTotal) + count;
        }

        /// <summary>
        /// Adds the number of parameters in the provided bracketed parameter list to the total parameter counter.
        /// </summary>
        /// <param name="parameters">The bracketed parameter list or null.</param>
        /// <remarks>
        /// Indexers use <see cref="BracketedParameterListSyntax"/> instead of <see cref="ParameterListSyntax"/>.
        /// </remarks>
        private void AddBracketedParameters(BracketedParameterListSyntax? parameters)
        {
            if (parameters == null)
            {
                return;
            }

            int count = parameters.Parameters.Count;
            if (count <= 0)
            {
                return;
            }

            totals[StatisticsKeys.ParametersTotal] =
                totals.GetValueOrDefault(StatisticsKeys.ParametersTotal) + count;
        }

        /// <summary>
        /// Adds the number of type parameters in the provided type parameter list to the total type parameter counter.
        /// </summary>
        /// <param name="typeParameters">The type parameter list or null.</param>
        private void AddTypeParameters(TypeParameterListSyntax? typeParameters)
        {
            if (typeParameters == null)
            {
                return;
            }

            int count = typeParameters.Parameters.Count;
            if (count <= 0)
            {
                return;
            }

            totals[StatisticsKeys.TypeParametersTotal] =
                totals.GetValueOrDefault(StatisticsKeys.TypeParametersTotal) + count;
        }

        /// <summary>
        /// Increments the total count of members that require a &lt;returns&gt; tag.
        /// </summary>
        /// <param name="returnType">The return type syntax.</param>
        /// <remarks>
        /// Members with a void return type do not require a &lt;returns&gt; tag.
        /// </remarks>
        private void AddReturnsRequired(TypeSyntax returnType)
        {
            if (returnType == null)
            {
                return;
            }

            if (returnType is PredefinedTypeSyntax predefined &&
                predefined.Keyword.IsKind(SyntaxKind.VoidKeyword))
            {
                return;
            }

            Increment(StatisticsKeys.ReturnsRequiredTotal);
        }
    }
}