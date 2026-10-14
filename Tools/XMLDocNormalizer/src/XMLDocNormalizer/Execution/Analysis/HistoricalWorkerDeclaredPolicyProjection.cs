using System.Text.Json;
using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.Canonical;
using XMLDocNormalizer.HistoricalWorker;
using XMLDocNormalizer.Utils;

namespace XMLDocNormalizer.Execution.Analysis
{
    /// <summary>Project already-bound original PE type facts; no second PE/PDB/ref/source acquisition or analyzer.</summary>
    internal static class HistoricalWorkerDeclaredPolicyProjection
    {
        internal static bool TryProject(HistoricalWorkerPayloadProjectionInput input, out WorkerDeclaredPolicyRequest? request)
        {
            request = null;
            try { return TryProjectBound(input, out request); }
            catch (Exception exception) when (exception is BadImageFormatException or ObjectDisposedException or InvalidOperationException)
            { return false; }
        }

        private static bool TryProjectBound(HistoricalWorkerPayloadProjectionInput input, out WorkerDeclaredPolicyRequest? request)
        {
            request = null;
            if (input.DeclaredPolicy is not { } optIn || optIn.ExpectedDeclarations != null
                || optIn.Profile != WorkerDeclaredPolicyContract.Profile || optIn.Mode != WorkerDeclaredPolicyMode.ProjectTransitiveDeclaredExceptions)
            { return false; }
            var bases = input.Root.ContainingModule.ReferencedAssemblySymbols.Select(assembly => assembly.GetTypeByMetadataName("System.Exception"))
                .Where(type => type != null).Distinct(SymbolEqualityComparer.Default).ToArray();
            if (bases.Length != 1 || bases[0] is not INamedTypeSymbol exceptionBase) { return false; }
            var definitions = new List<CanonicalTypeIdentity>();
            var pending = new Queue<INamespaceOrTypeSymbol>(); pending.Enqueue(input.Root.ContainingAssembly.GlobalNamespace);
            int visited = 0;
            while (pending.Count != 0)
            {
                if (++visited > 8192) { return false; }
                var symbol = pending.Dequeue();
                if (symbol is INamespaceSymbol space)
                { foreach (var child in space.GetMembers()) { pending.Enqueue(child); } continue; }
                var type = (INamedTypeSymbol)symbol;
                foreach (var nested in type.GetTypeMembers()) { pending.Enqueue(nested); }
                if (!type.InheritsFromOrEquals(exceptionBase)) { continue; }
                for (var container = type; container != null; container = container.ContainingType)
                { if (container.IsGenericType) { return false; } }
                definitions.Add(RoslynCanonicalIdentityFactory.CreateTypeIdentity(type));
                if (definitions.Count > WorkerDeclaredPolicyContract.MaximumDeclarations) { return false; }
            }
            request = optIn with { ExpectedDeclarations = definitions.OrderBy(type => JsonSerializer.Serialize(type, WorkerProtocol.JsonOptions), StringComparer.Ordinal).ToArray() };
            return true;
        }
    }
}
