using System.Text.Json;
using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.Canonical;

namespace XMLDocNormalizerTests.Helpers;

/// <summary>Snapshots actual calls to the existing sink, without consuming or replaying a saved trace.</summary>
internal sealed class AdmissionObservationRecorder(int? stopAfterEvent = null) : IExceptionFlowSummaryTargetSink
{
    private readonly Dictionary<ExceptionFlowCallableKey, int> ordinals = new(ReferenceEqualityComparer.Instance);
    internal List<string> Events { get; } = [];
    internal List<ExceptionFlowCallableKey> Keys { get; } = [];
    internal List<bool> ObservedUncertainty { get; } = [];

    public ExceptionFlowCallableKey RegisterTarget(ISymbol symbol, ExceptionFlowCallContext context)
    {
        var key = new ExceptionFlowCallableKey(symbol, context);
        int ordinal = Keys.Count;
        Keys.Add(key);
        ordinals.Add(key, ordinal);
        Record(JsonSerializer.Serialize(new
        {
            Kind = "Register", Ordinal = ordinal, Target = ExceptionFlowFiniteOrbitAdmission.BodyId(symbol),
            Invocation = CanonicalIdentityKeyWriter.Write(RoslynCanonicalIdentityFactory.CreateCallableIdentity(
                context.InvocationSymbol ?? symbol)), context.Key, Context = context.CanonicalContext
        }));
        return key;
    }

    public void ObserveLocalFragment(ExceptionFlowSummaryFragment fragment)
    {
        ObservedUncertainty.Add(fragment.UncertainTargets.Count != 0);
        // Serialize now: Merge and catch suppression subsequently mutate these collections.
        Record(JsonSerializer.Serialize(new
        {
            Kind = "Observe",
            Sources = fragment.Sources.Select(source => AdmissionCharacterization.TypeKey(source.ExceptionType)).ToArray(),
            Calls = fragment.CallEdges.Select(edge => new { Ordinal = ordinals[edge.Target], edge.CallSiteStep }).ToArray(),
            Uncertain = fragment.UncertainTargets.Order(StringComparer.Ordinal).ToArray()
        }));
    }

    private void Record(string value)
    {
        Events.Add(value);
        if (Events.Count == stopAfterEvent) throw new ObservationStoppedException();
    }

    internal sealed class ObservationStoppedException : System.Exception { }
}
