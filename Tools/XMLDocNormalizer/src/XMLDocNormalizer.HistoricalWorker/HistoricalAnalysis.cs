using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.Canonical;

namespace XMLDocNormalizer.HistoricalWorker
{
    /// <summary>Input/compilation orchestration only; uses the unchanged productive summary engine.</summary>
    internal static class HistoricalAnalysis
    {
        /// <summary>Runs one genuine source-backed transitive root, without emitting/executing input code.</summary>
        internal static WorkerResponse Analyze(WorkerAnalysisInput input, WorkerIdentity identity)
        {
            CSharpCompilation compilation;
            SyntaxTree tree;
            var supporting = new List<Compilation>();
            if (input.Context?.Solution is { } graph)
            {
                var reconstructed = new Dictionary<WorkerProjectIdentity, (CSharpCompilation Compilation, SyntaxTree Root)>();
                long nodes = 0;
                foreach (var project in graph.Projects!)
                {
                    var failure = Reconstruct(project.Input!, identity, out var projectCompilation, out var projectTree);
                    if (failure != null) { return failure; }
                    nodes += projectCompilation.SyntaxTrees.Sum(source => source.GetRoot().DescendantNodesAndSelf().Count());
                    if (nodes > 16384) { return Failure(WorkerFailureCode.InvalidRequest, "Solution exceeds the syntax-node budget.", identity); }
                    reconstructed.Add(project.Identity!, (projectCompilation, projectTree));
                }
                (compilation, tree) = reconstructed[graph.Target!];
                supporting.AddRange(reconstructed.Where(pair => pair.Key != graph.Target).Select(pair => pair.Value.Compilation));
            }
            else
            {
                var failure = Reconstruct(input, identity, out compilation, out tree);
                if (failure != null) { return failure; }
            }

            INamedTypeSymbol? type = compilation.GetTypeByMetadataName(input.TypeMetadataName!);
            IMethodSymbol[] methods = type?.GetMembers(input.MethodName!).OfType<IMethodSymbol>()
                .Where(method => method.MethodKind == MethodKind.Ordinary && method.IsStatic
                    && method.Parameters.Length == 0 && method.TypeParameters.Length == 0).ToArray() ?? [];
            if (type == null || type.IsGenericType || !SymbolEqualityComparer.Default.Equals(type.ContainingAssembly, compilation.Assembly)
                || methods.Length != 1 || methods[0].DeclaringSyntaxReferences.Length != 1
                || methods[0].DeclaringSyntaxReferences[0].GetSyntax() is not MethodDeclarationSyntax member
                || !ReferenceEquals(member.SyntaxTree, tree)
                || (member.Body == null && member.ExpressionBody == null))
            {
                return Failure(WorkerFailureCode.InvalidRequest, "Root must identify one source-owned parameterless static method with a body.", identity);
            }

            ExceptionFlowSemanticEnvironment environment = new(compilation, supporting);
            if (!environment.TryGetSemanticModel(member.SyntaxTree, out SemanticModel rootModel)
                || !SymbolEqualityComparer.Default.Equals(rootModel.GetDeclaredSymbol(member), methods[0]))
            {
                return Failure(WorkerFailureCode.AnalysisFailure, "Root semantic ownership could not be established.", identity);
            }

            CanonicalExceptionFlowAnalysisResult result;
            try
            {
                var native =
                    input.Context?.AnalysisMode == "direct" || input.Context?.DeclaredExecution == WorkerDeclaredExecution.DirectEvidence
                        || input.Context?.Finding?.Execution == WorkerDeclaredExecution.DirectEvidence
                        ? ExceptionFlowLocalSourceAnalyzer.AnalyzeDirectlyThrownExceptions(member, environment)
                        : ExceptionFlowSummaryAnalysisSession.AnalyzeSolutionTransitivelyThrownExceptions(member, environment);
                if (native.ProfileOutcome is { Complete: false } outside)
                    return Failure(WorkerFailureCode.UnsupportedContext, "Exception-flow request is outside A9-FiniteOrbit-v1.", identity, outside.Details);
                result = RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(native);
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("Historical analysis failure: " + exception);
                return Failure(WorkerFailureCode.AnalysisFailure, "Historical analysis could not complete.", identity);
            }
            if (result.Uncertainties.Length != 0 || result.Entries.Any(entry => entry.PathsTruncated))
            {
                return Failure(WorkerFailureCode.AnalysisFailure, "Exception-flow analysis is incomplete.", identity,
                    result.Uncertainties.Select(item => item.DisplayText).OrderBy(text => text, StringComparer.Ordinal).ToArray());
            }

            WorkerDeclaredPolicyEvidence? policy = null;
            WorkerDeclaredDocumentationEvidence? documentation = null;
            if (input.Context?.DeclaredPolicy != null)
            {
                try
                {
                    policy = HistoricalDeclaredPolicyFacts.Create(input, compilation, member, environment);
                    if (policy != null && input.Context.DeclaredPolicy.Documentation != null)
                    { documentation = HistoricalDeclaredDocumentationFacts.Create(rootModel, member, policy.DirectResult!, result); }
                }
                catch (Exception exception)
                {
                    Console.Error.WriteLine("Historical policy fact projection failed: " + exception);
                    return Failure(WorkerFailureCode.AnalysisFailure, "Policy facts could not be completed.", identity);
                }
                if (policy == null) { return Failure(WorkerFailureCode.UnsupportedContext, "Policy facts exceed the bounded complete nongeneric source-declaration profile.", identity); }
            }
            CanonicalExceptionFlowAnalysisResult? findingDirect = null;
            if (input.Context?.Finding is { } finding)
            {
                try
                {
                    findingDirect = RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(
                        ExceptionFlowLocalSourceAnalyzer.AnalyzeDirectlyThrownExceptions(member, environment));
                    if (findingDirect.Uncertainties.Length != 0 || findingDirect.Entries.Any(entry => entry.PathsTruncated))
                    { return Failure(WorkerFailureCode.AnalysisFailure, "Independent Direct evidence is incomplete.", identity); }
                    documentation = HistoricalDeclaredDocumentationFacts.Create(rootModel, member, findingDirect, result)
                        with
                    { Profile = WorkerFindingContract.Profile, Mode = finding.Documentation!.Mode };
                }
                catch (Exception exception)
                {
                    Console.Error.WriteLine("Historical Finding facts failed: " + exception);
                    return Failure(WorkerFailureCode.AnalysisFailure, "Finding facts could not be completed.", identity);
                }
            }
            return new WorkerResponse(WorkerProtocol.Version, "analyze", true, identity, result,
                Provenance: WorkerProtocol.Provenance(input), DeclaredPolicy: policy, DeclaredDocumentation: documentation, FindingDirect: findingDirect);
        }

        /// <summary>Reuse the exact B12/B13/B14 reconstruction for each independently owned project.</summary>
        internal static WorkerResponse? Reconstruct(WorkerAnalysisInput input, WorkerIdentity identity,
            out CSharpCompilation compilation, out SyntaxTree tree)
        {
            compilation = null!;
            tree = null!;
            if (!TryCreateParseOptions(input.Context?.ParseContext, out var parseOptions))
            { return Failure(WorkerFailureCode.UnsupportedContext, "The loaded Historical compiler does not support the concrete language version.", identity); }
            tree = CSharpSyntaxTree.ParseText(
                input.Source!, parseOptions, path: WorkerInputValidation.GetLogicalPath(input));
            var treesByPath = new Dictionary<string, SyntaxTree>(StringComparer.Ordinal) { [tree.FilePath] = tree };
            foreach (var source in input.Context?.AdditionalSources ?? [])
            {
                if (!TryCreateParseOptions(source.ParseContext, out var options))
                { return Failure(WorkerFailureCode.UnsupportedContext, "The loaded Historical compiler does not support a supporting source language version.", identity); }
                treesByPath.Add(source.LogicalPath, CSharpSyntaxTree.ParseText(source.Text, options, source.LogicalPath));
            }
            SyntaxTree[] trees = input.Context?.Scope is { } scope
                ? scope.OrderedSourcePaths!.Select(path => treesByPath[path]).ToArray() : [tree];
            if (trees.Sum(source => source.GetRoot().DescendantNodesAndSelf().Take(8193).Count()) > 8192)
            {
                return Failure(WorkerFailureCode.InvalidRequest, "Source exceeds the syntax-node budget.", identity);
            }

            MetadataReference[] references;
            try
            {
                references = input.Context?.ReferenceProfile == WorkerProtocol.ExactReferenceProfile
                    ? input.Context.References!.Select(reference => MetadataReference.CreateFromImage(reference.Image,
                        new MetadataReferenceProperties(MetadataImageKind.Assembly, aliases: [.. reference.Aliases!]))).ToArray()
                    : GetFrameworkReferences();
            }
            catch (Exception exception) when (exception is InvalidOperationException or IOException or BadImageFormatException or ArgumentException)
            {
                Console.Error.WriteLine("Historical compilation input failure: " + exception);
                return Failure(WorkerFailureCode.CompilationFailure, "Runtime reference profile could not be resolved.", identity);
            }

            compilation = CSharpCompilation.Create(
                input.Context?.AssemblyName ?? "HistoricalWorkerInput", trees, references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
            if (input.Context?.ReferenceProfile == WorkerProtocol.ExactReferenceProfile
                && !HaveExpectedIdentities(compilation, references, input.Context))
            {
                return Failure(WorkerFailureCode.CompilationFailure, "Reconstructed assembly/module identities contradict the exact reference description.", identity);
            }
            string[] errors = compilation.GetDiagnostics()
                .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                .Select(diagnostic => diagnostic.Id + ": " + diagnostic.GetMessage(CultureInfo.InvariantCulture))
                .OrderBy(message => message, StringComparer.Ordinal).ToArray();
            if (errors.Length != 0)
            {
                return Failure(WorkerFailureCode.CompilationFailure, "Historical compilation failed.", identity, errors);
            }

            return null;
        }

        /// <summary>B13 capability policy reused for each source, never a default for a missing explicit context.</summary>
        private static bool TryCreateParseOptions(WorkerParseContext? parse, out CSharpParseOptions options)
        {
            options = new CSharpParseOptions(LanguageVersion.CSharp12);
            if (parse == null) { return true; } // Only the legacy root profile permits absence.
            if (!LanguageVersionFacts.TryParse(parse.LanguageVersion, out var language)
                || !Enum.IsDefined(language) || language is LanguageVersion.Default or LanguageVersion.Latest
                    or LanguageVersion.LatestMajor or LanguageVersion.Preview)
            { return false; }
            options = new CSharpParseOptions(language, DocumentationMode.Parse, SourceCodeKind.Regular, parse.PreprocessorSymbols);
            return true;
        }

        /// <summary>Compare actual Historical binding to original neutral identities; paths never stand in for identity.</summary>
        private static bool HaveExpectedIdentities(CSharpCompilation compilation, MetadataReference[] references, WorkerCompilationContext context)
        {
            try
            {
                if (!RoslynCanonicalIdentityFactory.CreateAssemblyIdentity(compilation.Assembly).Equals(context.TargetIdentity)
                    || compilation.SourceModule.Name != context.TargetModuleName)
                { return false; }
                for (int index = 0; index < references.Length; index++)
                {
                    var expected = context.References![index];
                    if (compilation.GetAssemblyOrModuleSymbol(references[index]) is not IAssemblySymbol assembly
                        || !RoslynCanonicalIdentityFactory.CreateAssemblyIdentity(assembly).Equals(expected.Identity))
                    { return false; }
                    var modules = assembly.Modules.ToArray();
                    if (modules.Length != 1 || modules[0].Name != expected.ModuleName
                        || modules[0].GetMetadata()?.GetModuleVersionId() != expected.ModuleVersionId)
                    { return false; }
                }
                return true;
            }
            catch (Exception exception) when (exception is BadImageFormatException or InvalidOperationException or ArgumentException)
            { return false; }
        }

        /// <summary>Bounded framework-only reference profile; never imports Worker/Analyzer/Roslyn as input metadata.</summary>
        private static MetadataReference[] GetFrameworkReferences()
        {
            string[] allowed = ["System.Private.CoreLib.dll", "System.Runtime.dll", "System.Threading.Tasks.dll",
                "System.Collections.dll", "System.Console.dll", "System.Threading.dll", "System.Runtime.Extensions.dll"];
            string[] trusted = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")
                ?? throw new InvalidOperationException("Runtime reference profile is unavailable.")).Split(Path.PathSeparator);
            string[] selected = trusted.Where(path => allowed.Contains(Path.GetFileName(path), StringComparer.Ordinal))
                .Select(Path.GetFullPath).Distinct(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal)
                .OrderBy(path => Path.GetFileName(path), StringComparer.Ordinal).ToArray();
            if (selected.Length != allowed.Length)
            {
                throw new InvalidOperationException("Runtime reference profile is incomplete or ambiguous.");
            }

            return selected.Select(path => MetadataReference.CreateFromFile(path)).ToArray();
        }

        /// <summary>Creates a failure with no successful/partial result payload.</summary>
        private static WorkerResponse Failure(WorkerFailureCode code, string message, WorkerIdentity identity, params string[] details)
            => new(WorkerProtocol.Version, "analyze", false, identity, Failure: new(code, message, details));
    }
}
