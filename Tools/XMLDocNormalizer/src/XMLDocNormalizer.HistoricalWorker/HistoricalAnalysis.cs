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
            SyntaxTree tree = CSharpSyntaxTree.ParseText(
                input.Source!, new CSharpParseOptions(LanguageVersion.CSharp12), path: "worker-input.cs");
            if (tree.GetRoot().DescendantNodesAndSelf().Take(8193).Count() > 8192)
            {
                return Failure(WorkerFailureCode.InvalidRequest, "Source exceeds the syntax-node budget.", identity);
            }

            MetadataReference[] references;
            try
            {
                references = GetFrameworkReferences();
            }
            catch (Exception exception) when (exception is InvalidOperationException or IOException or BadImageFormatException)
            {
                Console.Error.WriteLine("Historical compilation input failure: " + exception);
                return Failure(WorkerFailureCode.CompilationFailure, "Runtime reference profile could not be resolved.", identity);
            }

            CSharpCompilation compilation = CSharpCompilation.Create(
                "HistoricalWorkerInput", [tree], references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
            string[] errors = compilation.GetDiagnostics()
                .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                .Select(diagnostic => diagnostic.Id + ": " + diagnostic.GetMessage(CultureInfo.InvariantCulture))
                .OrderBy(message => message, StringComparer.Ordinal).ToArray();
            if (errors.Length != 0)
            {
                return Failure(WorkerFailureCode.CompilationFailure, "Historical compilation failed.", identity, errors);
            }

            INamedTypeSymbol? type = compilation.GetTypeByMetadataName(input.TypeMetadataName!);
            IMethodSymbol[] methods = type?.GetMembers(input.MethodName!).OfType<IMethodSymbol>()
                .Where(method => method.MethodKind == MethodKind.Ordinary && method.IsStatic
                    && method.Parameters.Length == 0 && method.TypeParameters.Length == 0).ToArray() ?? [];
            if (type == null || type.IsGenericType || !SymbolEqualityComparer.Default.Equals(type.ContainingAssembly, compilation.Assembly)
                || methods.Length != 1 || methods[0].DeclaringSyntaxReferences.Length != 1
                || methods[0].DeclaringSyntaxReferences[0].GetSyntax() is not MethodDeclarationSyntax member
                || (member.Body == null && member.ExpressionBody == null))
            {
                return Failure(WorkerFailureCode.InvalidRequest, "Root must identify one source-owned parameterless static method with a body.", identity);
            }

            ExceptionFlowSemanticEnvironment environment = new(compilation);
            if (!environment.TryGetSemanticModel(member.SyntaxTree, out SemanticModel rootModel)
                || !SymbolEqualityComparer.Default.Equals(rootModel.GetDeclaredSymbol(member), methods[0]))
            {
                return Failure(WorkerFailureCode.AnalysisFailure, "Root semantic ownership could not be established.", identity);
            }

            CanonicalExceptionFlowAnalysisResult result;
            try
            {
                result = RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(
                    ExceptionFlowSummaryAnalysisSession.AnalyzeSolutionTransitivelyThrownExceptions(member, environment));
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

            return new WorkerResponse(WorkerProtocol.Version, "analyze", true, identity, result,
                Provenance: new(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input.Source!))),
                    input.TypeMetadataName!, input.MethodName!, "worker-input.cs", "solution-transitive",
                    "net8-runtime-bounded-v1", "12", "enable"));
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
