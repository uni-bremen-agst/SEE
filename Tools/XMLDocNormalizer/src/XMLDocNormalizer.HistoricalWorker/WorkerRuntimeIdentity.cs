using System.Reflection;
using System.Security.Cryptography;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;

namespace XMLDocNormalizer.HistoricalWorker
{
    /// <summary>Checks the actual loaded engine before any input analysis.</summary>
    internal static class WorkerRuntimeIdentity
    {
        internal const string HistoricalVersion = "5.0.0-2.25451.107";
        internal const string CommonSha256 = "660C3D626C4B8F4CF8C231FBEF0FB6B4DB4FFFCC89EF5B31AAECA1CF4D7F66A1";
        internal const string CSharpSha256 = "B0EC1DDCA4C97DCF15845FF4BCEB5499C3989025197D5D65E04310E29C09217D";

        /// <summary>Fails closed on wrong native images or any Current Analyzer/extra Roslyn engine.</summary>
        internal static WorkerIdentity ReadAndValidate()
        {
            Assembly common = typeof(Compilation).Assembly;
            Assembly csharp = typeof(CSharpCompilation).Assembly;
            WorkerAssemblyIdentity[] engines = [Read(common), Read(csharp)];
            if (engines[0].Sha256 != CommonSha256 || engines[1].Sha256 != CSharpSha256
                || engines.Any(engine => !engine.InformationalVersion.StartsWith(HistoricalVersion + "+", StringComparison.Ordinal)))
            {
                throw new InvalidOperationException("Loaded Roslyn does not match the exact validated historical images.");
            }

            Assembly[] loaded = AppDomain.CurrentDomain.GetAssemblies();
            if (loaded.Any(assembly => assembly.GetName().Name == "XMLDocNormalizer")
                || loaded.Where(assembly => assembly.GetName().Name?.StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal) == true)
                    .Any(assembly => assembly != common && assembly != csharp))
            {
                throw new InvalidOperationException("Unexpected Analyzer or Roslyn universe in Historical Worker.");
            }

            return new WorkerIdentity(
                "1.0", WorkerProtocol.Version, Read(typeof(WorkerRuntimeIdentity).Assembly),
                Read(typeof(ExceptionFlowSummaryAnalysisSession).Assembly), engines,
                ExceptionFlowRuntimeAwaitCapability.Read(default).IsAvailable, "net8-runtime-bounded-v1");
        }

        /// <summary>Reports identities of assemblies already loaded, not metadata from arbitrary package files.</summary>
        private static WorkerAssemblyIdentity Read(Assembly assembly)
        {
            using FileStream image = File.OpenRead(assembly.Location);
            return new WorkerAssemblyIdentity(
                assembly.GetName().Name!, assembly.GetName().Version!.ToString(),
                assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "",
                assembly.ManifestModule.ModuleVersionId.ToString("D"), Convert.ToHexString(SHA256.HashData(image)));
        }
    }
}
