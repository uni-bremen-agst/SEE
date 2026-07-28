using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Utils;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Contains summary-graph construction for constructor initialization and
    /// instance member initializers.
    /// </summary>
    internal static partial class ExceptionFlowAnalyzer
    {
        /// <summary>
        /// Analyzes one explicitly declared instance constructor, including
        /// constructor-initializer arguments, the delegated constructor,
        /// instance member initializers, and the constructor body.
        /// </summary>
        /// <param name="constructor">
        /// The constructor declaration to analyze.
        /// </param>
        /// <param name="constructorSymbol">
        /// The symbol represented by the current summary node.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for symbol and value resolution.
        /// </param>
        /// <param name="semanticContext">
        /// The project-closure semantic context.
        /// </param>
        /// <param name="graph">
        /// The graph receiving discovered constructor and initializer targets.
        /// </param>
        /// <param name="fragment">
        /// The local constructor summary fragment.
        /// </param>
        /// <param name="callContext">
        /// The value facts known for the constructor parameters.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if executable constructor content was
        /// analyzed; otherwise <see langword="false"/>.
        /// </returns>
        private static bool AnalyzeSummaryInstanceConstructor(
            ConstructorDeclarationSyntax constructor,
            IMethodSymbol constructorSymbol,
            SemanticModel semanticModel,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callContext)
        {
            bool analyzedAnyContent = false;

            bool delegatesToThis =
                constructor.Initializer?
                    .ThisOrBaseKeyword
                    .IsKind(
                        SyntaxKind.ThisKeyword) == true;

            if (constructor.Initializer
                is ConstructorInitializerSyntax initializer)
            {
                AnalyzeSummaryNode(
                    initializer.ArgumentList,
                    semanticModel,
                    semanticContext,
                    graph,
                    fragment,
                    callContext);

                analyzedAnyContent = true;

                if (semanticModel.GetSymbolInfo(initializer).Symbol
                    is IMethodSymbol targetConstructor)
                {
                    AddSummaryConstructorCallEdge(
                        targetConstructor,
                        initializer.ArgumentList.Arguments,
                        initializer,
                        semanticModel,
                        graph,
                        fragment,
                        callContext);
                }
                else
                {
                    fragment.AddUncertainTarget(
                        "Constructor initializer");
                }
            }
            else if (ResolveImplicitBaseConstructor(
                         constructorSymbol,
                         semanticModel.Compilation)
                     is IMethodSymbol implicitBaseConstructor)
            {
                AddSummaryConstructorCallEdge(
                    implicitBaseConstructor,
                    default,
                    constructor,
                    semanticModel,
                    graph,
                    fragment,
                    callContext);

                analyzedAnyContent = true;
            }
            else if (HasNonObjectBaseType(
                         constructorSymbol))
            {
                fragment.AddUncertainTarget(
                    "Implicit base constructor");

                analyzedAnyContent = true;
            }

            if (!delegatesToThis)
            {
                analyzedAnyContent |=
                    AnalyzeSummaryInstanceMemberInitializers(
                        constructorSymbol.ContainingType,
                        semanticContext,
                        graph,
                        fragment,
                        callContext);
            }

            if (SyntaxUtils.TryGetMemberBody(
                    constructor,
                    out SyntaxNode? constructorBody) &&
                constructorBody != null)
            {
                AnalyzeSummaryNode(
                    constructorBody,
                    semanticModel,
                    semanticContext,
                    graph,
                    fragment,
                    callContext);

                analyzedAnyContent = true;
            }

            return analyzedAnyContent;
        }

        /// <summary>
        /// Analyzes a source-declared implicit instance constructor.
        /// </summary>
        /// <param name="constructorSymbol">
        /// The implicit constructor symbol.
        /// </param>
        /// <param name="semanticContext">
        /// The project-closure semantic context.
        /// </param>
        /// <param name="graph">
        /// The graph receiving discovered constructor and initializer targets.
        /// </param>
        /// <param name="fragment">
        /// The local constructor summary fragment.
        /// </param>
        /// <param name="callContext">
        /// The value facts known for the constructor parameters.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the containing source type could be
        /// analyzed; otherwise <see langword="false"/>.
        /// </returns>
        private static bool AnalyzeSummaryImplicitConstructor(
            IMethodSymbol constructorSymbol,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callContext)
        {
            TypeDeclarationSyntax? sourceDeclaration =
                constructorSymbol.ContainingType
                    .DeclaringSyntaxReferences
                    .Select(
                        static syntaxReference =>
                            syntaxReference.GetSyntax())
                    .OfType<TypeDeclarationSyntax>()
                    .FirstOrDefault();

            if (sourceDeclaration == null ||
                !semanticContext.TryGetSemanticModel(
                    sourceDeclaration.SyntaxTree,
                    out SemanticModel semanticModel) ||
                semanticModel == null)
            {
                return false;
            }

            if (ResolveImplicitBaseConstructor(
                    constructorSymbol,
                    semanticModel.Compilation)
                is IMethodSymbol implicitBaseConstructor)
            {
                AddSummaryConstructorCallEdge(
                    implicitBaseConstructor,
                    default,
                    sourceDeclaration,
                    semanticModel,
                    graph,
                    fragment,
                    callContext);
            }
            else if (HasNonObjectBaseType(
                         constructorSymbol))
            {
                fragment.AddUncertainTarget(
                    "Implicit base constructor");
            }

            AnalyzeSummaryInstanceMemberInitializers(
                constructorSymbol.ContainingType,
                semanticContext,
                graph,
                fragment,
                callContext);

            return true;
        }

        /// <summary>
        /// Adds one constructor-call edge with a context derived from the
        /// supplied arguments.
        /// </summary>
        /// <param name="targetConstructor">
        /// The delegated or base constructor.
        /// </param>
        /// <param name="arguments">
        /// The arguments supplied to the target constructor.
        /// </param>
        /// <param name="sourceNode">
        /// The source node representing the constructor call.
        /// </param>
        /// <param name="semanticModel">
        /// The semantic model used for argument analysis.
        /// </param>
        /// <param name="graph">
        /// The graph receiving the target constructor node.
        /// </param>
        /// <param name="fragment">
        /// The local summary fragment receiving the call edge.
        /// </param>
        /// <param name="callContext">
        /// The value facts known while analyzing the caller.
        /// </param>
        private static void AddSummaryConstructorCallEdge(
            IMethodSymbol targetConstructor,
            SeparatedSyntaxList<ArgumentSyntax> arguments,
            SyntaxNode sourceNode,
            SemanticModel semanticModel,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callContext)
        {
            ExceptionFlowCallContext targetContext =
                CreateCallContext(
                    targetConstructor,
                    arguments,
                    semanticModel,
                    callContext);

            ExceptionFlowCallableKey targetKey =
                new(
                    targetConstructor,
                    targetContext.Key);

            graph.GetOrAdd(
                targetKey,
                targetContext);

            fragment.AddCallEdge(
                new ExceptionFlowSummaryCallEdge(
                    targetKey,
                    CreatePathStep(
                        ExceptionFlowPathStepKind.ConstructorCall,
                        targetConstructor,
                        sourceNode)));
        }

        /// <summary>
        /// Resolves the base constructor invoked when an instance constructor
        /// has no explicit constructor initializer.
        /// </summary>
        /// <param name="constructorSymbol">
        /// The constructor whose implicit base call should be resolved.
        /// </param>
        /// <param name="compilation">
        /// The compilation used for accessibility checks.
        /// </param>
        /// <returns>
        /// The uniquely selected accessible base constructor, or
        /// <see langword="null"/> when no analyzable implicit base call exists.
        /// </returns>
        private static IMethodSymbol? ResolveImplicitBaseConstructor(
            IMethodSymbol constructorSymbol,
            Compilation compilation)
        {
            if (constructorSymbol.ContainingType.TypeKind !=
                    TypeKind.Class ||
                constructorSymbol.ContainingType.BaseType
                    is not INamedTypeSymbol baseType ||
                baseType.SpecialType ==
                    SpecialType.System_Object)
            {
                return null;
            }

            IMethodSymbol[] candidates =
                baseType.InstanceConstructors
                    .Where(
                        constructor =>
                            compilation.IsSymbolAccessibleWithin(
                                constructor,
                                constructorSymbol.ContainingType) &&
                            CanInvokeWithoutExplicitArguments(
                                constructor))
                    .ToArray();

            if (candidates.Length == 0)
            {
                return null;
            }

            int bestRank =
                candidates.Min(
                    GetImplicitConstructorCandidateRank);

            IMethodSymbol[] bestCandidates =
                candidates
                    .Where(
                        candidate =>
                            GetImplicitConstructorCandidateRank(
                                candidate) == bestRank)
                    .ToArray();

            return bestCandidates.Length == 1
                ? bestCandidates[0]
                : null;
        }

        /// <summary>
        /// Determines whether an instance constructor implicitly invokes a
        /// non-object base constructor.
        /// </summary>
        /// <param name="constructorSymbol">
        /// The constructor to inspect.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the containing class has a non-object
        /// base type; otherwise <see langword="false"/>.
        /// </returns>
        private static bool HasNonObjectBaseType(
            IMethodSymbol constructorSymbol)
        {
            return constructorSymbol.ContainingType.TypeKind ==
                       TypeKind.Class &&
                   constructorSymbol.ContainingType.BaseType
                       is INamedTypeSymbol baseType &&
                   baseType.SpecialType !=
                       SpecialType.System_Object;
        }

        /// <summary>
        /// Determines whether a constructor can be invoked without explicit
        /// arguments.
        /// </summary>
        /// <param name="constructor">
        /// The constructor to inspect.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if every parameter is optional or a
        /// <c>params</c> parameter; otherwise <see langword="false"/>.
        /// </returns>
        private static bool CanInvokeWithoutExplicitArguments(
            IMethodSymbol constructor)
        {
            return constructor.Parameters.All(
                static parameter =>
                    parameter.IsOptional ||
                    parameter.IsParams);
        }

        /// <summary>
        /// Gets a deterministic preference rank for an implicit base
        /// constructor candidate.
        /// </summary>
        /// <param name="constructor">
        /// The candidate constructor.
        /// </param>
        /// <returns>
        /// A lower value for candidates preferred by empty-argument overload
        /// resolution.
        /// </returns>
        private static int GetImplicitConstructorCandidateRank(
            IMethodSymbol constructor)
        {
            if (constructor.Parameters.Length == 0)
            {
                return 0;
            }

            bool hasParamsParameter =
                constructor.Parameters.Any(
                    static parameter =>
                        parameter.IsParams);

            int categoryOffset =
                hasParamsParameter
                    ? 200
                    : 100;

            return categoryOffset +
                   constructor.Parameters.Length;
        }

        /// <summary>
        /// Analyzes all instance field, event-field, and property initializer
        /// expressions declared directly by a source type.
        /// </summary>
        /// <param name="typeSymbol">
        /// The constructed type whose initializers should be analyzed.
        /// </param>
        /// <param name="semanticContext">
        /// The project-closure semantic context.
        /// </param>
        /// <param name="graph">
        /// The graph receiving targets referenced by initializer expressions.
        /// </param>
        /// <param name="fragment">
        /// The constructor summary fragment receiving initializer flow.
        /// </param>
        /// <param name="callContext">
        /// The value facts known for the constructor parameters.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if at least one initializer expression was
        /// analyzed; otherwise <see langword="false"/>.
        /// </returns>
        private static bool AnalyzeSummaryInstanceMemberInitializers(
            INamedTypeSymbol typeSymbol,
            ProjectClosureSemanticContext semanticContext,
            ExceptionFlowSummaryGraph graph,
            ExceptionFlowSummaryFragment fragment,
            ExceptionFlowCallContext callContext)
        {
            bool analyzedAnyInitializer = false;

            foreach (SyntaxReference syntaxReference
                     in typeSymbol.DeclaringSyntaxReferences)
            {
                if (syntaxReference.GetSyntax()
                        is not TypeDeclarationSyntax typeDeclaration ||
                    !semanticContext.TryGetSemanticModel(
                        typeDeclaration.SyntaxTree,
                        out SemanticModel semanticModel) ||
                    semanticModel == null)
                {
                    continue;
                }

                foreach (MemberDeclarationSyntax member
                         in typeDeclaration.Members)
                {
                    if (member is FieldDeclarationSyntax field)
                    {
                        if (field.Modifiers.Any(
                                static modifier =>
                                    modifier.IsKind(
                                        SyntaxKind.StaticKeyword)) ||
                            field.Modifiers.Any(
                                static modifier =>
                                    modifier.IsKind(
                                        SyntaxKind.ConstKeyword)))
                        {
                            continue;
                        }

                        foreach (VariableDeclaratorSyntax variable
                                 in field.Declaration.Variables)
                        {
                            if (variable.Initializer == null)
                            {
                                continue;
                            }

                            AnalyzeSummaryNode(
                                variable.Initializer.Value,
                                semanticModel,
                                semanticContext,
                                graph,
                                fragment,
                                callContext);

                            analyzedAnyInitializer = true;
                        }

                        continue;
                    }

                    if (member is EventFieldDeclarationSyntax eventField)
                    {
                        if (eventField.Modifiers.Any(
                                static modifier =>
                                    modifier.IsKind(
                                        SyntaxKind.StaticKeyword)))
                        {
                            continue;
                        }

                        foreach (VariableDeclaratorSyntax variable
                                 in eventField.Declaration.Variables)
                        {
                            if (variable.Initializer == null)
                            {
                                continue;
                            }

                            AnalyzeSummaryNode(
                                variable.Initializer.Value,
                                semanticModel,
                                semanticContext,
                                graph,
                                fragment,
                                callContext);

                            analyzedAnyInitializer = true;
                        }

                        continue;
                    }

                    if (member
                            is PropertyDeclarationSyntax property &&
                        property.Initializer != null &&
                        !property.Modifiers.Any(
                            static modifier =>
                                modifier.IsKind(
                                    SyntaxKind.StaticKeyword)))
                    {
                        AnalyzeSummaryNode(
                            property.Initializer.Value,
                            semanticModel,
                            semanticContext,
                            graph,
                            fragment,
                            callContext);

                        analyzedAnyInitializer = true;
                    }
                }
            }

            return analyzedAnyInitializer;
        }
    }
}
