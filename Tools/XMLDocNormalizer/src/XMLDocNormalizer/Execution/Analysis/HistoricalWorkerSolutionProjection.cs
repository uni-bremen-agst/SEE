using System.Collections.Immutable;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizer.HistoricalWorker;
using XMLDocNormalizer.Models;

namespace XMLDocNormalizer.Execution.Analysis
{
    /// <summary>Composes original B14 project handoffs and B12 reference identities into a bounded external project DAG.</summary>
    internal static class HistoricalWorkerSolutionProjection
    {
        internal static HistoricalWorkerPayloadProjectionResult Project(HistoricalWorkerPayloadProjectionInput input)
        {
            HistoricalWorkerPayloadProjectionResult Fail(string message) => new(null,
                new(HistoricalWorkerPayloadProjectionFailureCode.UnsupportedProfile, message));
            if (input.AnalysisMode != ExceptionAnalysisMode.SolutionTransitive || input.SourceSet == null
                || input.SupportingProjects.Length is < 1 or >= WorkerProtocol.MaximumSolutionProjects)
            { return Fail("An explicit SolutionTransitive target and bounded original supporting projects are required."); }
            var originals = ImmutableArray.Create(input with { SupportingProjects = default }).AddRange(input.SupportingProjects);
            var projects = new List<WorkerProjectNode>();
            HistoricalWorkerPayloadProjectionValue? target = null;
            foreach (var original in originals)
            {
                if (original == null || !original.SupportingProjects.IsDefault || original.SourceSet == null
                    || original.AnalysisMode != ExceptionAnalysisMode.SolutionTransitive)
                { return Fail("Every project must have an original independent B14 local scope, without nested solution inputs."); }
                var local = HistoricalWorkerPayloadProjection.Project(original);
                if (!local.Succeeded) { return local; }
                if (!ExternalPeDebugDirectoryDescriptorFactory.TryGetValidatedTarget(original.Provenance.DebugDirectory, out var binary))
                { return Fail("Original project PE receipt is unavailable."); }
                var context = local.Value!.Payload.Context!;
                var identity = new WorkerProjectIdentity(context.TargetIdentity, binary!.Modules[0].Name, binary.Modules[0].ModuleVersionId);
                projects.Add(new(identity, local.Value.Payload, []));
                target ??= local.Value;
            }
            for (int index = 0; index < projects.Count; index++)
            {
                var project = projects[index]; var dependencies = new List<WorkerProjectIdentity>();
                foreach (var reference in project.Input!.Context!.References!)
                {
                    var matchingName = projects.Where(other => string.Equals(other.Identity!.Assembly!.Name, reference.Identity!.Name, StringComparison.OrdinalIgnoreCase)).ToArray();
                    if (matchingName.Length == 0) { continue; } // External PE dependency; never claim an implicit source project.
                    if (matchingName.Length != 1 || !WorkerInputValidation.ReferenceMatchesProject(reference, matchingName[0].Identity!))
                    { return Fail("Cross-project reference contradicts the original full assembly/module/MVID identity."); }
                    dependencies.Add(matchingName[0].Identity!);
                }
                projects[index] = project with { Dependencies = dependencies.ToArray() };
            }
            var payload = target!.Payload with { Context = target.Payload.Context! with { Solution = new(projects[0].Identity, projects.ToArray()) } };
            if (!WorkerInputValidation.IsSupported(payload))
            { return Fail("Project graph is incomplete, conflicting, cyclic, unreachable or exceeds the bounded Worker profile."); }
            return new(target with { Payload = payload }, null);
        }
    }
}
