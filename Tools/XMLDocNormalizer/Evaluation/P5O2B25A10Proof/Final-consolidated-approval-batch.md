# A10 final consolidated fixture-preflight approval batch

Status: STOP BEFORE IMPLEMENTATION. R1-R7 APPROVED, not requested again.

Updated Phase-0 status (2026-10-23): all 85 / 429 / 3,202 supplementary bindings
are resolved in `Supplementary-resolved-bindings-r2.json`. Both protected obj
differences are S1, byte-exact generated revision metadata drift; the actual
writer remains unproved. The historical raw-hash exit 1 record is preserved,
not retried or accepted as a new baseline. See `Supplementary-binding-closure.md`
and `Protected-obj-drift-analysis.md`. This is definitive **static preflight
closure**, NOT implementation validation or clearance to implement in this run.
R8–R10 below remain the single final pending conditional request; no R11.

All 202 original exclusions are reconstructed (203 parameter cases, 228 calls). Expanded source review: 438 semantic/helper/primitive methods, 464 cases, 527 calls (including the original primitive-only case); 340 current test/helper files and 2,194 attributed test methods indexed; all 237 Worker methods reviewed. The JSON manifest retains exact source inputs, references, helper paths, per-root witnesses and original assertions. Runtime observations are inherited Current analysis, NOT an implemented A9 certificate, a genuine Historical execution, or fresh unit gates.

These are requests, NOT permissions. Each row is conditional on a sound implemented A9-FiniteOrbit-v1 certificate for that exact input; if admitted, KEEP its assertion. No extra parameter cases, blanket helper weakening, raw graph facade/fallback, new wire DTO, compiler/routing/PID/isolation changes, or B25B. Historical B25-A9 evidence stays frozen. Only new A10 witnesses may record corrections.

## R8 (32 exact rows)

Normal finding/cardinality and dependent finding dereference/context assertions ONLY. Outside requests get classified incomplete output and no normal exact Finding; no partial Direct fallback.

### R8-01: `DOC611_ConditionalThrowReachabilityTests.ExplicitThrowWithUnknownStringValue_ProducesFinding#0`

- File: `Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/DOC611_ConditionalThrowReachabilityTests.cs`
- Parameter case: existing Fact only
- Entry/modes: ProjectTransitive
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: no modeled executable body for string.IsNullOrWhiteSpace(string?)
- New semantics: ONLY named outside request: classified incomplete certificate; block its normal exact DOC610/DOC611 (and normal exact output generally), no partial Direct fallback. Replace only positive finding/cardinality and dependent dereference/context scaffolding; independent diagnostics and admitted requests stay unchanged.

Current line 168:

```csharp
Assert.Single(findings)
```

Current line 170:

```csharp
Assert.Contains(
                "System.ArgumentException",
                finding.Message,
                StringComparison.Ordinal)
```

### R8-02: `DOC611_EnclosingConditionDereferenceTests.ConditionWritingReceiverAfterDereference_ProjectTransitive_DoesNotProveFinalValueNonNull#0`

- File: `Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/DOC611_EnclosingConditionDereferenceTests.cs`
- Parameter case: existing Fact only
- Entry/modes: ProjectTransitive
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: no modeled executable body for TestClass.Item.Value.get
- New semantics: ONLY named outside request: classified incomplete certificate; block its normal exact DOC610/DOC611 (and normal exact output generally), no partial Direct fallback. Replace only positive finding/cardinality and dependent dereference/context scaffolding; independent diagnostics and admitted requests stay unchanged.

Current line 288:

```csharp
Assert.Single(
                    findings)
```

Current line 291:

```csharp
Assert.Equal(
                XmlDocSmells
                    .MissingTransitiveExceptionDocumentation
                    .ID,
                finding.Smell.ID)
```

Current line 297:

```csharp
Assert.Equal(
                "M",
                finding.Context.SymbolName)
```

Current line 301:

```csharp
Assert.Equal(
                "System.ArgumentNullException",
                finding.Context.TargetName)
```

### R8-03: `DOC611_EnclosingConditionDereferenceTests.ConditionWritingReceiverAfterDereference_SolutionTransitive_DoesNotProveFinalValueNonNull#0`

- File: `Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/DOC611_EnclosingConditionDereferenceTests.cs`
- Parameter case: existing Fact only
- Entry/modes: SolutionTransitive
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: no modeled executable body for TestClass.Item.Value.get
- New semantics: ONLY named outside request: classified incomplete certificate; block its normal exact DOC610/DOC611 (and normal exact output generally), no partial Direct fallback. Replace only positive finding/cardinality and dependent dereference/context scaffolding; independent diagnostics and admitted requests stay unchanged.

Current line 288:

```csharp
Assert.Single(
                    findings)
```

Current line 291:

```csharp
Assert.Equal(
                XmlDocSmells
                    .MissingTransitiveExceptionDocumentation
                    .ID,
                finding.Smell.ID)
```

Current line 297:

```csharp
Assert.Equal(
                "M",
                finding.Context.SymbolName)
```

Current line 301:

```csharp
Assert.Equal(
                "System.ArgumentNullException",
                finding.Context.TargetName)
```

### R8-04: `DOC611_EnclosingConditionDereferenceTests.ConditionalAccessInIfCondition_ProjectTransitive_DoesNotProveReceiverNonNull#0`

- File: `Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/DOC611_EnclosingConditionDereferenceTests.cs`
- Parameter case: existing Fact only
- Entry/modes: ProjectTransitive
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: no modeled executable body for TestClass.Item.Value.get
- New semantics: ONLY named outside request: classified incomplete certificate; block its normal exact DOC610/DOC611 (and normal exact output generally), no partial Direct fallback. Replace only positive finding/cardinality and dependent dereference/context scaffolding; independent diagnostics and admitted requests stay unchanged.

Current line 288:

```csharp
Assert.Single(
                    findings)
```

Current line 291:

```csharp
Assert.Equal(
                XmlDocSmells
                    .MissingTransitiveExceptionDocumentation
                    .ID,
                finding.Smell.ID)
```

Current line 297:

```csharp
Assert.Equal(
                "M",
                finding.Context.SymbolName)
```

Current line 301:

```csharp
Assert.Equal(
                "System.ArgumentNullException",
                finding.Context.TargetName)
```

### R8-05: `DOC611_EnclosingConditionDereferenceTests.ConditionalAccessInIfCondition_SolutionTransitive_DoesNotProveReceiverNonNull#0`

- File: `Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/DOC611_EnclosingConditionDereferenceTests.cs`
- Parameter case: existing Fact only
- Entry/modes: SolutionTransitive
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: no modeled executable body for TestClass.Item.Value.get
- New semantics: ONLY named outside request: classified incomplete certificate; block its normal exact DOC610/DOC611 (and normal exact output generally), no partial Direct fallback. Replace only positive finding/cardinality and dependent dereference/context scaffolding; independent diagnostics and admitted requests stay unchanged.

Current line 288:

```csharp
Assert.Single(
                    findings)
```

Current line 291:

```csharp
Assert.Equal(
                XmlDocSmells
                    .MissingTransitiveExceptionDocumentation
                    .ID,
                finding.Smell.ID)
```

Current line 297:

```csharp
Assert.Equal(
                "M",
                finding.Context.SymbolName)
```

Current line 301:

```csharp
Assert.Equal(
                "System.ArgumentNullException",
                finding.Context.TargetName)
```

### R8-06: `DOC611_EnclosingForEachDereferenceTests.ConditionalAccessInForEachSource_ProjectTransitive_DoesNotProveReceiverNonNull#0`

- File: `Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/DOC611_EnclosingForEachDereferenceTests.cs`
- Parameter case: existing Fact only
- Entry/modes: ProjectTransitive
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.Generic.IEnumerable<int>.GetEnumerator()' on receiver type 'System.Collections.Generic.IReadOnlyList<int>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.Generic.IEnumerator<int>.Current.get' on receiver type 'System.Collections.Generic.IEnumerator<int>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.IEnumerator.MoveNext()' on receiver type 'System.Collections.Generic.IEnumerator<int>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.IDisposable.Dispose()' on receiver type 'System.Collections.Generic.IEnumerator<int>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.IEnumerable<T>.GetEnumerator(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.IEnumerator<T>.Current.get; TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.IEnumerator.MoveNext(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.IDisposable.Dispose(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for TestClass.Container.Values.get
- New semantics: ONLY named outside request: classified incomplete certificate; block its normal exact DOC610/DOC611 (and normal exact output generally), no partial Direct fallback. Replace only positive finding/cardinality and dependent dereference/context scaffolding; independent diagnostics and admitted requests stay unchanged.

Current line 400:

```csharp
Assert.Single(
                    findings)
```

Current line 403:

```csharp
Assert.Equal(
                XmlDocSmells
                    .MissingTransitiveExceptionDocumentation
                    .ID,
                finding.Smell.ID)
```

Current line 409:

```csharp
Assert.Equal(
                "System.ArgumentNullException",
                finding.Context.TargetName)
```

### R8-07: `DOC611_EnclosingForEachDereferenceTests.ConditionalAccessInForEachSource_SolutionTransitive_DoesNotProveReceiverNonNull#0`

- File: `Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/DOC611_EnclosingForEachDereferenceTests.cs`
- Parameter case: existing Fact only
- Entry/modes: SolutionTransitive
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.Generic.IEnumerable<int>.GetEnumerator()' on receiver type 'System.Collections.Generic.IReadOnlyList<int>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.Generic.IEnumerator<int>.Current.get' on receiver type 'System.Collections.Generic.IEnumerator<int>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.IEnumerator.MoveNext()' on receiver type 'System.Collections.Generic.IEnumerator<int>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.IDisposable.Dispose()' on receiver type 'System.Collections.Generic.IEnumerator<int>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.IEnumerable<T>.GetEnumerator(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.IEnumerator<T>.Current.get; TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.IEnumerator.MoveNext(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.IDisposable.Dispose(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for TestClass.Container.Values.get
- New semantics: ONLY named outside request: classified incomplete certificate; block its normal exact DOC610/DOC611 (and normal exact output generally), no partial Direct fallback. Replace only positive finding/cardinality and dependent dereference/context scaffolding; independent diagnostics and admitted requests stay unchanged.

Current line 400:

```csharp
Assert.Single(
                    findings)
```

Current line 403:

```csharp
Assert.Equal(
                XmlDocSmells
                    .MissingTransitiveExceptionDocumentation
                    .ID,
                finding.Smell.ID)
```

Current line 409:

```csharp
Assert.Equal(
                "System.ArgumentNullException",
                finding.Context.TargetName)
```

### R8-08: `DOC611_EnclosingForEachDereferenceTests.ReceiverReassignedAfterUseInsideForEachBody_ProjectTransitive_DoesNotPreserveNonNullFactAcrossIterations#0`

- File: `Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/DOC611_EnclosingForEachDereferenceTests.cs`
- Parameter case: existing Fact only
- Entry/modes: ProjectTransitive
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.Generic.IEnumerable<int>.GetEnumerator()' on receiver type 'System.Collections.Generic.IReadOnlyList<int>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.Generic.IEnumerator<int>.Current.get' on receiver type 'System.Collections.Generic.IEnumerator<int>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.IEnumerator.MoveNext()' on receiver type 'System.Collections.Generic.IEnumerator<int>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.IDisposable.Dispose()' on receiver type 'System.Collections.Generic.IEnumerator<int>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.IEnumerable<T>.GetEnumerator(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.IEnumerator<T>.Current.get; TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.IEnumerator.MoveNext(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.IDisposable.Dispose(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for TestClass.Container.Values.get
- New semantics: ONLY named outside request: classified incomplete certificate; block its normal exact DOC610/DOC611 (and normal exact output generally), no partial Direct fallback. Replace only positive finding/cardinality and dependent dereference/context scaffolding; independent diagnostics and admitted requests stay unchanged.

Current line 400:

```csharp
Assert.Single(
                    findings)
```

Current line 403:

```csharp
Assert.Equal(
                XmlDocSmells
                    .MissingTransitiveExceptionDocumentation
                    .ID,
                finding.Smell.ID)
```

Current line 409:

```csharp
Assert.Equal(
                "System.ArgumentNullException",
                finding.Context.TargetName)
```

### R8-09: `DOC611_EnclosingForEachDereferenceTests.ReceiverReassignedAfterUseInsideForEachBody_SolutionTransitive_DoesNotPreserveNonNullFactAcrossIterations#0`

- File: `Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/DOC611_EnclosingForEachDereferenceTests.cs`
- Parameter case: existing Fact only
- Entry/modes: SolutionTransitive
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.Generic.IEnumerable<int>.GetEnumerator()' on receiver type 'System.Collections.Generic.IReadOnlyList<int>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.Generic.IEnumerator<int>.Current.get' on receiver type 'System.Collections.Generic.IEnumerator<int>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.IEnumerator.MoveNext()' on receiver type 'System.Collections.Generic.IEnumerator<int>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.IDisposable.Dispose()' on receiver type 'System.Collections.Generic.IEnumerator<int>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.IEnumerable<T>.GetEnumerator(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.IEnumerator<T>.Current.get; TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.IEnumerator.MoveNext(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.IDisposable.Dispose(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for TestClass.Container.Values.get
- New semantics: ONLY named outside request: classified incomplete certificate; block its normal exact DOC610/DOC611 (and normal exact output generally), no partial Direct fallback. Replace only positive finding/cardinality and dependent dereference/context scaffolding; independent diagnostics and admitted requests stay unchanged.

Current line 400:

```csharp
Assert.Single(
                    findings)
```

Current line 403:

```csharp
Assert.Equal(
                XmlDocSmells
                    .MissingTransitiveExceptionDocumentation
                    .ID,
                finding.Smell.ID)
```

Current line 409:

```csharp
Assert.Equal(
                "System.ArgumentNullException",
                finding.Context.TargetName)
```

### R8-10: `DOC611_EnclosingForEachDereferenceTests.ReceiverReassignedInsideForEachBody_ProjectTransitive_DoesNotPreserveNonNullFact#0`

- File: `Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/DOC611_EnclosingForEachDereferenceTests.cs`
- Parameter case: existing Fact only
- Entry/modes: ProjectTransitive
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.Generic.IEnumerable<int>.GetEnumerator()' on receiver type 'System.Collections.Generic.IReadOnlyList<int>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.Generic.IEnumerator<int>.Current.get' on receiver type 'System.Collections.Generic.IEnumerator<int>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.IEnumerator.MoveNext()' on receiver type 'System.Collections.Generic.IEnumerator<int>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.IDisposable.Dispose()' on receiver type 'System.Collections.Generic.IEnumerator<int>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.IEnumerable<T>.GetEnumerator(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.IEnumerator<T>.Current.get; TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.IEnumerator.MoveNext(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.IDisposable.Dispose(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for TestClass.Container.Values.get
- New semantics: ONLY named outside request: classified incomplete certificate; block its normal exact DOC610/DOC611 (and normal exact output generally), no partial Direct fallback. Replace only positive finding/cardinality and dependent dereference/context scaffolding; independent diagnostics and admitted requests stay unchanged.

Current line 400:

```csharp
Assert.Single(
                    findings)
```

Current line 403:

```csharp
Assert.Equal(
                XmlDocSmells
                    .MissingTransitiveExceptionDocumentation
                    .ID,
                finding.Smell.ID)
```

Current line 409:

```csharp
Assert.Equal(
                "System.ArgumentNullException",
                finding.Context.TargetName)
```

### R8-11: `DOC611_EnclosingForEachDereferenceTests.ReceiverReassignedInsideForEachBody_SolutionTransitive_DoesNotPreserveNonNullFact#0`

- File: `Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/DOC611_EnclosingForEachDereferenceTests.cs`
- Parameter case: existing Fact only
- Entry/modes: SolutionTransitive
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.Generic.IEnumerable<int>.GetEnumerator()' on receiver type 'System.Collections.Generic.IReadOnlyList<int>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.Generic.IEnumerator<int>.Current.get' on receiver type 'System.Collections.Generic.IEnumerator<int>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.IEnumerator.MoveNext()' on receiver type 'System.Collections.Generic.IEnumerator<int>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.IDisposable.Dispose()' on receiver type 'System.Collections.Generic.IEnumerator<int>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.IEnumerable<T>.GetEnumerator(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.IEnumerator<T>.Current.get; TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.IEnumerator.MoveNext(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.IDisposable.Dispose(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for TestClass.Container.Values.get
- New semantics: ONLY named outside request: classified incomplete certificate; block its normal exact DOC610/DOC611 (and normal exact output generally), no partial Direct fallback. Replace only positive finding/cardinality and dependent dereference/context scaffolding; independent diagnostics and admitted requests stay unchanged.

Current line 400:

```csharp
Assert.Single(
                    findings)
```

Current line 403:

```csharp
Assert.Equal(
                XmlDocSmells
                    .MissingTransitiveExceptionDocumentation
                    .ID,
                finding.Smell.ID)
```

Current line 409:

```csharp
Assert.Equal(
                "System.ArgumentNullException",
                finding.Context.TargetName)
```

### R8-12: `DOC611_EnclosingForEachDereferenceTests.ReceiverReassignedInsideForEachSource_ProjectTransitive_DoesNotProveReceiverNonNull#0`

- File: `Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/DOC611_EnclosingForEachDereferenceTests.cs`
- Parameter case: existing Fact only
- Entry/modes: ProjectTransitive
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.Generic.IEnumerable<int>.GetEnumerator()' on receiver type 'System.Collections.Generic.IReadOnlyList<int>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.Generic.IEnumerator<int>.Current.get' on receiver type 'System.Collections.Generic.IEnumerator<int>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.IEnumerator.MoveNext()' on receiver type 'System.Collections.Generic.IEnumerator<int>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.IDisposable.Dispose()' on receiver type 'System.Collections.Generic.IEnumerator<int>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.IEnumerable<T>.GetEnumerator(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.IEnumerator<T>.Current.get; TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.IEnumerator.MoveNext(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.IDisposable.Dispose(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for TestClass.Container.Values.get
- New semantics: ONLY named outside request: classified incomplete certificate; block its normal exact DOC610/DOC611 (and normal exact output generally), no partial Direct fallback. Replace only positive finding/cardinality and dependent dereference/context scaffolding; independent diagnostics and admitted requests stay unchanged.

Current line 400:

```csharp
Assert.Single(
                    findings)
```

Current line 403:

```csharp
Assert.Equal(
                XmlDocSmells
                    .MissingTransitiveExceptionDocumentation
                    .ID,
                finding.Smell.ID)
```

Current line 409:

```csharp
Assert.Equal(
                "System.ArgumentNullException",
                finding.Context.TargetName)
```

### R8-13: `DOC611_EnclosingForEachDereferenceTests.ReceiverReassignedInsideForEachSource_SolutionTransitive_DoesNotProveReceiverNonNull#0`

- File: `Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/DOC611_EnclosingForEachDereferenceTests.cs`
- Parameter case: existing Fact only
- Entry/modes: SolutionTransitive
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.Generic.IEnumerable<int>.GetEnumerator()' on receiver type 'System.Collections.Generic.IReadOnlyList<int>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.Generic.IEnumerator<int>.Current.get' on receiver type 'System.Collections.Generic.IEnumerator<int>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.IEnumerator.MoveNext()' on receiver type 'System.Collections.Generic.IEnumerator<int>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.IDisposable.Dispose()' on receiver type 'System.Collections.Generic.IEnumerator<int>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.IEnumerable<T>.GetEnumerator(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.IEnumerator<T>.Current.get; TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.IEnumerator.MoveNext(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.IDisposable.Dispose(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for TestClass.Container.Values.get
- New semantics: ONLY named outside request: classified incomplete certificate; block its normal exact DOC610/DOC611 (and normal exact output generally), no partial Direct fallback. Replace only positive finding/cardinality and dependent dereference/context scaffolding; independent diagnostics and admitted requests stay unchanged.

Current line 400:

```csharp
Assert.Single(
                    findings)
```

Current line 403:

```csharp
Assert.Equal(
                XmlDocSmells
                    .MissingTransitiveExceptionDocumentation
                    .ID,
                finding.Smell.ID)
```

Current line 409:

```csharp
Assert.Equal(
                "System.ArgumentNullException",
                finding.Context.TargetName)
```

### R8-14: `DOC611_ImmutableMemberValueFactsTests.GetOnlyPropertyAssignedWithoutGuard_ProducesFindings#0`

- File: `Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/DOC611_ImmutableMemberValueFactsTests.cs`
- Parameter case: existing Fact only
- Entry/modes: ProjectTransitive
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: no modeled executable body for TestClass.Holder.Path.get
- New semantics: ONLY named outside request: classified incomplete certificate; block its normal exact DOC610/DOC611 (and normal exact output generally), no partial Direct fallback. Replace only positive finding/cardinality and dependent dereference/context scaffolding; independent diagnostics and admitted requests stay unchanged.

Current line 190:

```csharp
Assert.Equal(2, findings.Count)
```

Current line 192:

```csharp
Assert.Contains(
                findings,
                finding => finding.Message.Contains(
                    "System.ArgumentNullException",
                    StringComparison.Ordinal))
```

Current line 198:

```csharp
Assert.Contains(
                findings,
                finding => finding.Message.Contains(
                    "System.ArgumentException",
                    StringComparison.Ordinal))
```

### R8-15: `DOC611_ImmutableMemberValueFactsTests.PropertyWithOneUnguardedConstructor_ProducesFindings#0`

- File: `Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/DOC611_ImmutableMemberValueFactsTests.cs`
- Parameter case: existing Fact only
- Entry/modes: ProjectTransitive
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: no modeled executable body for TestClass.Holder.Path.get
- New semantics: ONLY named outside request: classified incomplete certificate; block its normal exact DOC610/DOC611 (and normal exact output generally), no partial Direct fallback. Replace only positive finding/cardinality and dependent dereference/context scaffolding; independent diagnostics and admitted requests stay unchanged.

Current line 243:

```csharp
Assert.Equal(2, findings.Count)
```

Current line 245:

```csharp
Assert.Contains(
                findings,
                finding => finding.Message.Contains(
                    "System.ArgumentNullException",
                    StringComparison.Ordinal))
```

Current line 251:

```csharp
Assert.Contains(
                findings,
                finding => finding.Message.Contains(
                    "System.ArgumentException",
                    StringComparison.Ordinal))
```

### R8-16: `DOC611_ImmutableMemberValueFactsTests.PropertyWithSetter_DoesNotReuseConstructorFacts#0`

- File: `Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/DOC611_ImmutableMemberValueFactsTests.cs`
- Parameter case: existing Fact only
- Entry/modes: ProjectTransitive
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: no modeled executable body for TestClass.Holder.Path.get
- New semantics: ONLY named outside request: classified incomplete certificate; block its normal exact DOC610/DOC611 (and normal exact output generally), no partial Direct fallback. Replace only positive finding/cardinality and dependent dereference/context scaffolding; independent diagnostics and admitted requests stay unchanged.

Current line 143:

```csharp
Assert.Equal(2, findings.Count)
```

Current line 145:

```csharp
Assert.Contains(
                findings,
                finding => finding.Message.Contains(
                    "System.ArgumentNullException",
                    StringComparison.Ordinal))
```

Current line 151:

```csharp
Assert.Contains(
                findings,
                finding => finding.Message.Contains(
                    "System.ArgumentException",
                    StringComparison.Ordinal))
```

### R8-17: `DOC611_IsPatternConditionDereferenceTests.ConditionalAccessInsideIsPattern_ProjectTransitive_DoesNotProveReceiverNonNull#0`

- File: `Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/DOC611_IsPatternConditionDereferenceTests.cs`
- Parameter case: existing Fact only
- Entry/modes: ProjectTransitive
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: no modeled executable body for TestClass.Item.Value.get
- New semantics: ONLY named outside request: classified incomplete certificate; block its normal exact DOC610/DOC611 (and normal exact output generally), no partial Direct fallback. Replace only positive finding/cardinality and dependent dereference/context scaffolding; independent diagnostics and admitted requests stay unchanged.

Current line 147:

```csharp
Assert.Single(
                    findings)
```

Current line 150:

```csharp
Assert.Equal(
                XmlDocSmells
                    .MissingTransitiveExceptionDocumentation
                    .ID,
                finding.Smell.ID)
```

Current line 156:

```csharp
Assert.Equal(
                "System.ArgumentNullException",
                finding.Context.TargetName)
```

### R8-18: `DOC611_IsPatternConditionDereferenceTests.ConditionalAccessInsideIsPattern_SolutionTransitive_DoesNotProveReceiverNonNull#0`

- File: `Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/DOC611_IsPatternConditionDereferenceTests.cs`
- Parameter case: existing Fact only
- Entry/modes: SolutionTransitive
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: no modeled executable body for TestClass.Item.Value.get
- New semantics: ONLY named outside request: classified incomplete certificate; block its normal exact DOC610/DOC611 (and normal exact output generally), no partial Direct fallback. Replace only positive finding/cardinality and dependent dereference/context scaffolding; independent diagnostics and admitted requests stay unchanged.

Current line 147:

```csharp
Assert.Single(
                    findings)
```

Current line 150:

```csharp
Assert.Equal(
                XmlDocSmells
                    .MissingTransitiveExceptionDocumentation
                    .ID,
                finding.Smell.ID)
```

Current line 156:

```csharp
Assert.Equal(
                "System.ArgumentNullException",
                finding.Context.TargetName)
```

### R8-19: `DOC611_NestedSuccessfulCalleeDereferenceTests.ConditionalSourceHelperInInvocationReceiver_ProjectTransitive_DoesNotProveArgumentNonNull#0`

- File: `Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/DOC611_NestedSuccessfulCalleeDereferenceTests.cs`
- Parameter case: existing Fact only
- Entry/modes: ProjectTransitive
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: no modeled executable body for object.GetHashCode()
- New semantics: ONLY named outside request: classified incomplete certificate; block its normal exact DOC610/DOC611 (and normal exact output generally), no partial Direct fallback. Replace only positive finding/cardinality and dependent dereference/context scaffolding; independent diagnostics and admitted requests stay unchanged.

Current line 276:

```csharp
Assert.Single(
                    findings)
```

Current line 279:

```csharp
Assert.Equal(
                XmlDocSmells
                    .MissingTransitiveExceptionDocumentation
                    .ID,
                finding.Smell.ID)
```

Current line 285:

```csharp
Assert.Equal(
                "System.ArgumentNullException",
                finding.Context.TargetName)
```

### R8-20: `DOC611_NestedSuccessfulCalleeDereferenceTests.ConditionalSourceHelperInInvocationReceiver_SolutionTransitive_DoesNotProveArgumentNonNull#0`

- File: `Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/DOC611_NestedSuccessfulCalleeDereferenceTests.cs`
- Parameter case: existing Fact only
- Entry/modes: SolutionTransitive
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: no modeled executable body for object.GetHashCode()
- New semantics: ONLY named outside request: classified incomplete certificate; block its normal exact DOC610/DOC611 (and normal exact output generally), no partial Direct fallback. Replace only positive finding/cardinality and dependent dereference/context scaffolding; independent diagnostics and admitted requests stay unchanged.

Current line 276:

```csharp
Assert.Single(
                    findings)
```

Current line 279:

```csharp
Assert.Equal(
                XmlDocSmells
                    .MissingTransitiveExceptionDocumentation
                    .ID,
                finding.Smell.ID)
```

Current line 285:

```csharp
Assert.Equal(
                "System.ArgumentNullException",
                finding.Context.TargetName)
```

### R8-21: `DOC611_OneBasedSourcePositionFactsTests.ArbitraryLinePosition_DoesNotReceiveSourceCoordinateFact#0`

- File: `Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/DOC611_OneBasedSourcePositionFactsTests.cs`
- Parameter case: existing Fact only
- Entry/modes: ProjectTransitive
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: no modeled executable body for Microsoft.CodeAnalysis.Text.LinePosition.Line.get; TargetOrAnalysisCoverageIncomplete: no modeled executable body for Microsoft.CodeAnalysis.Text.LinePosition.LinePosition(int, int)
- New semantics: ONLY named outside request: classified incomplete certificate; block its normal exact DOC610/DOC611 (and normal exact output generally), no partial Direct fallback. Replace only positive finding/cardinality and dependent dereference/context scaffolding; independent diagnostics and admitted requests stay unchanged.

Current line 148:

```csharp
Assert.Contains(
                findings,
                finding => finding.Context.TargetName == "System.ArgumentOutOfRangeException")
```

### R8-22: `DOC611_OneBasedSourcePositionFactsTests.ZeroLengthRoslynSpan_DoesNotReceiveOneBasedCoordinateFact#0`

- File: `Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/DOC611_OneBasedSourcePositionFactsTests.cs`
- Parameter case: existing Fact only
- Entry/modes: ProjectTransitive
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'Microsoft.CodeAnalysis.SyntaxTree.GetLineSpan(Microsoft.CodeAnalysis.Text.TextSpan, System.Threading.CancellationToken)' on receiver type 'Microsoft.CodeAnalysis.SyntaxTree' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: no modeled executable body for Microsoft.CodeAnalysis.FileLinePositionSpan.StartLinePosition.get; TargetOrAnalysisCoverageIncomplete: no modeled executable body for Microsoft.CodeAnalysis.SyntaxTree.GetLineSpan(Microsoft.CodeAnalysis.Text.TextSpan, System.Threading.CancellationToken); TargetOrAnalysisCoverageIncomplete: no modeled executable body for Microsoft.CodeAnalysis.Text.LinePosition.Line.get; TargetOrAnalysisCoverageIncomplete: no modeled executable body for Microsoft.CodeAnalysis.Text.TextSpan.TextSpan(int, int)
- New semantics: ONLY named outside request: classified incomplete certificate; block its normal exact DOC610/DOC611 (and normal exact output generally), no partial Direct fallback. Replace only positive finding/cardinality and dependent dereference/context scaffolding; independent diagnostics and admitted requests stay unchanged.

Current line 189:

```csharp
Assert.Contains(
                findings,
                finding => finding.Context.TargetName == "System.ArgumentOutOfRangeException")
```

### R8-23: `DOC611_PrecedingSuccessfulDereferenceTests.DereferenceBeforeForEachWithLaterBodyWrite_ProjectTransitive_RemainsPotentiallyNull#0`

- File: `Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/DOC611_PrecedingSuccessfulDereferenceTests.cs`
- Parameter case: existing Fact only
- Entry/modes: ProjectTransitive
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: no modeled executable body for Holder.Value.get
- New semantics: ONLY named outside request: classified incomplete certificate; block its normal exact DOC610/DOC611 (and normal exact output generally), no partial Direct fallback. Replace only positive finding/cardinality and dependent dereference/context scaffolding; independent diagnostics and admitted requests stay unchanged.

Current line 464:

```csharp
Assert.Contains(
                findings,
                finding =>
                    finding.Message.Contains(
                        "System.ArgumentNullException",
                        StringComparison.Ordinal))
```

### R8-24: `DOC611_PrecedingSuccessfulDereferenceTests.DereferenceBeforeForEachWithLaterBodyWrite_SolutionTransitive_RemainsPotentiallyNull#0`

- File: `Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/DOC611_PrecedingSuccessfulDereferenceTests.cs`
- Parameter case: existing Fact only
- Entry/modes: SolutionTransitive
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: no modeled executable body for Holder.Value.get
- New semantics: ONLY named outside request: classified incomplete certificate; block its normal exact DOC610/DOC611 (and normal exact output generally), no partial Direct fallback. Replace only positive finding/cardinality and dependent dereference/context scaffolding; independent diagnostics and admitted requests stay unchanged.

Current line 464:

```csharp
Assert.Contains(
                findings,
                finding =>
                    finding.Message.Contains(
                        "System.ArgumentNullException",
                        StringComparison.Ordinal))
```

### R8-25: `DOC611_SequenceElementCallContextTests.PossiblyNullSequenceElementsPassedToHelper_StillProduceFinding#0`

- File: `Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/DOC611_SequenceElementCallContextTests.cs`
- Parameter case: existing Fact only
- Entry/modes: ProjectTransitive, SolutionTransitive
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.Generic.IEnumerable<TestClass.Item?>.GetEnumerator()' on receiver type 'System.Collections.Generic.IReadOnlyList<TestClass.Item?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.Generic.IEnumerator<TestClass.Item?>.Current.get' on receiver type 'System.Collections.Generic.IEnumerator<TestClass.Item?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.IEnumerator.MoveNext()' on receiver type 'System.Collections.Generic.IEnumerator<TestClass.Item?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.IDisposable.Dispose()' on receiver type 'System.Collections.Generic.IEnumerator<TestClass.Item?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.IEnumerable<T>.GetEnumerator(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.IEnumerator<T>.Current.get; TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.IEnumerator.MoveNext(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.IDisposable.Dispose()
- New semantics: ONLY named outside request: classified incomplete certificate; block its normal exact DOC610/DOC611 (and normal exact output generally), no partial Direct fallback. Replace only positive finding/cardinality and dependent dereference/context scaffolding; independent diagnostics and admitted requests stay unchanged.

Current line 273:

```csharp
Assert.Single(findings)
```

Current line 275:

```csharp
Assert.Equal(
                XmlDocSmells.MissingTransitiveExceptionDocumentation.ID,
                finding.Smell.ID)
```

Current line 279:

```csharp
Assert.Equal(
                "System.ArgumentNullException",
                finding.Context.TargetName)
```

### R8-26: `DOC611_SequenceElementCallContextTests.SequenceParameterMutatedBeforeForeach_StillProducesFinding#0`

- File: `Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/DOC611_SequenceElementCallContextTests.cs`
- Parameter case: existing Fact only
- Entry/modes: ProjectTransitive, SolutionTransitive
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.IEnumerable<TSource>.ToArray<TSource>(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.IEnumerable.OfType<TResult>()
- New semantics: ONLY named outside request: classified incomplete certificate; block its normal exact DOC610/DOC611 (and normal exact output generally), no partial Direct fallback. Replace only positive finding/cardinality and dependent dereference/context scaffolding; independent diagnostics and admitted requests stay unchanged.

Current line 273:

```csharp
Assert.Single(findings)
```

Current line 275:

```csharp
Assert.Equal(
                XmlDocSmells.MissingTransitiveExceptionDocumentation.ID,
                finding.Smell.ID)
```

Current line 279:

```csharp
Assert.Equal(
                "System.ArgumentNullException",
                finding.Context.TargetName)
```

### R8-27: `DOC611_SequenceElementFactSoundnessTests.MaterializedArrayElementMutation_StillProducesFinding#0`

- File: `Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/DOC611_SequenceElementFactSoundnessTests.cs`
- Parameter case: existing Fact only
- Entry/modes: ProjectTransitive, SolutionTransitive
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.IEnumerable<TSource>.ToArray<TSource>(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.IEnumerable.OfType<TResult>()
- New semantics: ONLY named outside request: classified incomplete certificate; block its normal exact DOC610/DOC611 (and normal exact output generally), no partial Direct fallback. Replace only positive finding/cardinality and dependent dereference/context scaffolding; independent diagnostics and admitted requests stay unchanged.

Current line 315:

```csharp
Assert.Single(projectFindings)
```

Current line 318:

```csharp
Assert.Single(solutionFindings)
```

Current line 334:

```csharp
Assert.Equal(
                XmlDocSmells.MissingTransitiveExceptionDocumentation.ID,
                finding.Smell.ID)
```

Current line 338:

```csharp
Assert.Contains(
                "System.ArgumentNullException",
                finding.Message,
                StringComparison.Ordinal)
```

### R8-28: `DOC611_SequenceElementFactSoundnessTests.PossiblyNullDictionaryInsertion_StillProducesFinding#0`

- File: `Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/DOC611_SequenceElementFactSoundnessTests.cs`
- Parameter case: existing Fact only
- Entry/modes: ProjectTransitive, SolutionTransitive
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.Dictionary<TKey, TValue>.Dictionary(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.Dictionary<TKey, TValue>.TryAdd(TKey, TValue); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.Dictionary<TKey, TValue>.ValueCollection.Enumerator.Current.get; TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.Dictionary<TKey, TValue>.ValueCollection.Enumerator.MoveNext(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.Dictionary<TKey, TValue>.ValueCollection.GetEnumerator(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.Dictionary<TKey, TValue>.Values.get; TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.IDisposable.Dispose()
- New semantics: ONLY named outside request: classified incomplete certificate; block its normal exact DOC610/DOC611 (and normal exact output generally), no partial Direct fallback. Replace only positive finding/cardinality and dependent dereference/context scaffolding; independent diagnostics and admitted requests stay unchanged.

Current line 315:

```csharp
Assert.Single(projectFindings)
```

Current line 318:

```csharp
Assert.Single(solutionFindings)
```

Current line 334:

```csharp
Assert.Equal(
                XmlDocSmells.MissingTransitiveExceptionDocumentation.ID,
                finding.Smell.ID)
```

Current line 338:

```csharp
Assert.Contains(
                "System.ArgumentNullException",
                finding.Message,
                StringComparison.Ordinal)
```

### R8-29: `DOC611_SequenceElementFactSoundnessTests.ReassignedSequenceLocal_StillProducesFinding#0`

- File: `Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/DOC611_SequenceElementFactSoundnessTests.cs`
- Parameter case: existing Fact only
- Entry/modes: ProjectTransitive, SolutionTransitive
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.Generic.IEnumerable<string?>.GetEnumerator()' on receiver type 'System.Collections.Generic.IEnumerable<string?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.Generic.IEnumerator<string?>.Current.get' on receiver type 'System.Collections.Generic.IEnumerator<string?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.IEnumerator.MoveNext()' on receiver type 'System.Collections.Generic.IEnumerator<string?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.IDisposable.Dispose()' on receiver type 'System.Collections.Generic.IEnumerator<string?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.IEnumerable<T>.GetEnumerator(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.IEnumerator<T>.Current.get; TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.IEnumerable.OfType<TResult>(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.IEnumerator.MoveNext(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.IDisposable.Dispose()
- New semantics: ONLY named outside request: classified incomplete certificate; block its normal exact DOC610/DOC611 (and normal exact output generally), no partial Direct fallback. Replace only positive finding/cardinality and dependent dereference/context scaffolding; independent diagnostics and admitted requests stay unchanged.

Current line 315:

```csharp
Assert.Single(projectFindings)
```

Current line 318:

```csharp
Assert.Single(solutionFindings)
```

Current line 334:

```csharp
Assert.Equal(
                XmlDocSmells.MissingTransitiveExceptionDocumentation.ID,
                finding.Smell.ID)
```

Current line 338:

```csharp
Assert.Contains(
                "System.ArgumentNullException",
                finding.Message,
                StringComparison.Ordinal)
```

### R8-30: `DOC611_SequenceElementFactSoundnessTests.SelectAfterOfTypeCanIntroduceNull_StillProducesFinding#0`

- File: `Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/DOC611_SequenceElementFactSoundnessTests.cs`
- Parameter case: existing Fact only
- Entry/modes: ProjectTransitive, SolutionTransitive
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.Generic.IEnumerable<string?>.GetEnumerator()' on receiver type 'System.Collections.Generic.IEnumerable<string?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.Generic.IEnumerator<string?>.Current.get' on receiver type 'System.Collections.Generic.IEnumerator<string?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.IEnumerator.MoveNext()' on receiver type 'System.Collections.Generic.IEnumerator<string?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.IDisposable.Dispose()' on receiver type 'System.Collections.Generic.IEnumerator<string?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.IEnumerable<T>.GetEnumerator(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.IEnumerable<TSource>.Select<TSource, TResult>(System.Func<TSource, TResult>); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.IEnumerator<T>.Current.get; TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.IEnumerable.OfType<TResult>(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.IEnumerator.MoveNext(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.IDisposable.Dispose()
- New semantics: ONLY named outside request: classified incomplete certificate; block its normal exact DOC610/DOC611 (and normal exact output generally), no partial Direct fallback. Replace only positive finding/cardinality and dependent dereference/context scaffolding; independent diagnostics and admitted requests stay unchanged.

Current line 315:

```csharp
Assert.Single(projectFindings)
```

Current line 318:

```csharp
Assert.Single(solutionFindings)
```

Current line 334:

```csharp
Assert.Equal(
                XmlDocSmells.MissingTransitiveExceptionDocumentation.ID,
                finding.Smell.ID)
```

Current line 338:

```csharp
Assert.Contains(
                "System.ArgumentNullException",
                finding.Message,
                StringComparison.Ordinal)
```

### R8-31: `DOC611_SuccessfulCalleeDereferenceTests.CaughtNullReferenceDereference_DoesNotProveArgumentNonNullAfterCall#0`

- File: `Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/DOC611_SuccessfulCalleeDereferenceTests.cs`
- Parameter case: existing Fact only
- Entry/modes: ProjectTransitive, SolutionTransitive
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: no modeled executable body for object.Object()
- New semantics: ONLY named outside request: classified incomplete certificate; block its normal exact DOC610/DOC611 (and normal exact output generally), no partial Direct fallback. Replace only positive finding/cardinality and dependent dereference/context scaffolding; independent diagnostics and admitted requests stay unchanged.

Current line 270:

```csharp
Assert.Single(
                    findings)
```

Current line 273:

```csharp
Assert.Equal(
                XmlDocSmells
                    .MissingTransitiveExceptionDocumentation
                    .ID,
                finding.Smell.ID)
```

Current line 279:

```csharp
Assert.Equal(
                "M",
                finding.Context.SymbolName)
```

Current line 283:

```csharp
Assert.Equal(
                "System.ArgumentNullException",
                finding.Context.TargetName)
```

### R8-32: `DOC611_SuccessfulCalleeDereferenceTests.ConditionalHelperDereference_DoesNotProveArgumentNonNullAfterCall#0`

- File: `Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/DOC611_SuccessfulCalleeDereferenceTests.cs`
- Parameter case: existing Fact only
- Entry/modes: ProjectTransitive, SolutionTransitive
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: no modeled executable body for object.Object()
- New semantics: ONLY named outside request: classified incomplete certificate; block its normal exact DOC610/DOC611 (and normal exact output generally), no partial Direct fallback. Replace only positive finding/cardinality and dependent dereference/context scaffolding; independent diagnostics and admitted requests stay unchanged.

Current line 270:

```csharp
Assert.Single(
                    findings)
```

Current line 273:

```csharp
Assert.Equal(
                XmlDocSmells
                    .MissingTransitiveExceptionDocumentation
                    .ID,
                finding.Smell.ID)
```

Current line 279:

```csharp
Assert.Equal(
                "M",
                finding.Context.SymbolName)
```

Current line 283:

```csharp
Assert.Equal(
                "System.ArgumentNullException",
                finding.Context.TargetName)
```

## R9 (27 exact rows)

Positive normal escaping-path projection from the existing productive SolutionTransitive helper ONLY, where unavailable target/body coverage rejects production graph registration. Its same test's recursive Project/Direct/raw observations are NOT authorized for change. Empty-path, uncertainty-only, XML-only evidence and the two already-incomplete path-truncation tests are NOT included. The latter retain diagnostic 64-path/truncation assertions but may not produce a complete normal result.

### R9-01: `DOC611_ConditionalWeakTableReturnFactsTests.ExistingNullableTableWrite_PropagatesDownstreamGuard#0`

- File: `Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/DOC611_ConditionalWeakTableReturnFactsTests.cs`
- Parameter case: existing Fact only
- Entry/modes: SolutionTransitiveNative ONLY
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Runtime.CompilerServices.ConditionalWeakTable<TKey, TValue>.Add(TKey, TValue); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Runtime.CompilerServices.ConditionalWeakTable<TKey, TValue>.GetValue(TKey, System.Runtime.CompilerServices.ConditionalWeakTable<TKey, TValue>.CreateValueCallback)
- New semantics: ONLY summary/Session.Analyze invocation rejected for required target/body coverage: observe classified incomplete/non-exact outcome, not ordinary escaping-path projection from a graph forbidden before production registration. Preserve recursive Project/Direct helper assertions; no raw production fallback or second evaluator.

Current line 596:

```csharp
Assert.NotEmpty(
                run.Result.GetExceptionPaths(argumentNullException))
```

### R9-02: `DOC611_ConditionalWeakTableReturnFactsTests.ExplicitNullFactory_PropagatesDownstreamGuard#0`

- File: `Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/DOC611_ConditionalWeakTableReturnFactsTests.cs`
- Parameter case: existing Fact only
- Entry/modes: SolutionTransitiveNative ONLY
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Runtime.CompilerServices.ConditionalWeakTable<TKey, TValue>.GetValue(TKey, System.Runtime.CompilerServices.ConditionalWeakTable<TKey, TValue>.CreateValueCallback)
- New semantics: ONLY summary/Session.Analyze invocation rejected for required target/body coverage: observe classified incomplete/non-exact outcome, not ordinary escaping-path projection from a graph forbidden before production registration. Preserve recursive Project/Direct helper assertions; no raw production fallback or second evaluator.

Current line 596:

```csharp
Assert.NotEmpty(
                run.Result.GetExceptionPaths(argumentNullException))
```

### R9-03: `DOC611_ConditionalWeakTableReturnFactsTests.MultipleCallbackTargets_PropagateDownstreamGuard#0`

- File: `Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/DOC611_ConditionalWeakTableReturnFactsTests.cs`
- Parameter case: existing Fact only
- Entry/modes: SolutionTransitiveNative ONLY
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Runtime.CompilerServices.ConditionalWeakTable<TKey, TValue>.GetValue(TKey, System.Runtime.CompilerServices.ConditionalWeakTable<TKey, TValue>.CreateValueCallback)
- New semantics: ONLY summary/Session.Analyze invocation rejected for required target/body coverage: observe classified incomplete/non-exact outcome, not ordinary escaping-path projection from a graph forbidden before production registration. Preserve recursive Project/Direct helper assertions; no raw production fallback or second evaluator.

Current line 596:

```csharp
Assert.NotEmpty(
                run.Result.GetExceptionPaths(argumentNullException))
```

### R9-04: `DOC611_ConditionalWeakTableReturnFactsTests.NullableConditionalFactory_PropagatesDownstreamGuard#0`

- File: `Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/DOC611_ConditionalWeakTableReturnFactsTests.cs`
- Parameter case: existing Fact only
- Entry/modes: SolutionTransitiveNative ONLY
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Runtime.CompilerServices.ConditionalWeakTable<TKey, TValue>.GetValue(TKey, System.Runtime.CompilerServices.ConditionalWeakTable<TKey, TValue>.CreateValueCallback)
- New semantics: ONLY summary/Session.Analyze invocation rejected for required target/body coverage: observe classified incomplete/non-exact outcome, not ordinary escaping-path projection from a graph forbidden before production registration. Preserve recursive Project/Direct helper assertions; no raw production fallback or second evaluator.

Current line 596:

```csharp
Assert.NotEmpty(
                run.Result.GetExceptionPaths(argumentNullException))
```

### R9-05: `DOC611_ConditionalWeakTableReturnFactsTests.UnknownCallback_PropagatesDownstreamGuard#0`

- File: `Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/DOC611_ConditionalWeakTableReturnFactsTests.cs`
- Parameter case: existing Fact only
- Entry/modes: SolutionTransitiveNative ONLY
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Runtime.CompilerServices.ConditionalWeakTable<TKey, TValue>.GetValue(TKey, System.Runtime.CompilerServices.ConditionalWeakTable<TKey, TValue>.CreateValueCallback)
- New semantics: ONLY summary/Session.Analyze invocation rejected for required target/body coverage: observe classified incomplete/non-exact outcome, not ordinary escaping-path projection from a graph forbidden before production registration. Preserve recursive Project/Direct helper assertions; no raw production fallback or second evaluator.

Current line 596:

```csharp
Assert.NotEmpty(
                run.Result.GetExceptionPaths(argumentNullException))
```

### R9-06: `DOC611_ImmutablePropertyInvariantTests.NullableConstructorOverwrite_RemainsReported#0`

- File: `Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/DOC611_ImmutablePropertyInvariantTests.cs`
- Parameter case: existing Fact only
- Entry/modes: SolutionTransitiveNative ONLY
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: no modeled executable body for Holder.Value.get
- New semantics: ONLY summary/Session.Analyze invocation rejected for required target/body coverage: observe classified incomplete/non-exact outcome, not ordinary escaping-path projection from a graph forbidden before production registration. Preserve recursive Project/Direct helper assertions; no raw production fallback or second evaluator.

Current line 138:

```csharp
Assert.Single(
                run.Result.GetExceptionPaths(
                    argumentNullException))
```

### R9-07: `DOC611_SourceSequenceReturnFactsTests.ConditionalEntryMutatesSourceReturn_RemainsPotentiallyNull#0`

- File: `Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/DOC611_SourceSequenceReturnFactsTests.cs`
- Parameter case: existing Fact only
- Entry/modes: SolutionTransitiveNative ONLY
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.Generic.IEnumerable<EntryPoint.Item?>.GetEnumerator()' on receiver type 'System.Collections.Generic.IReadOnlyList<EntryPoint.Item?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.Generic.IEnumerator<EntryPoint.Item?>.Current.get' on receiver type 'System.Collections.Generic.IEnumerator<EntryPoint.Item?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.IEnumerator.MoveNext()' on receiver type 'System.Collections.Generic.IEnumerator<EntryPoint.Item?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.IDisposable.Dispose()' on receiver type 'System.Collections.Generic.IEnumerator<EntryPoint.Item?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.IEnumerable<T>.GetEnumerator(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.IEnumerator<T>.Current.get; TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.List<T>.Add(T); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.List<T>.List(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.IEnumerator.MoveNext(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.IDisposable.Dispose()
- New semantics: ONLY summary/Session.Analyze invocation rejected for required target/body coverage: observe classified incomplete/non-exact outcome, not ordinary escaping-path projection from a graph forbidden before production registration. Preserve recursive Project/Direct helper assertions; no raw production fallback or second evaluator.

Current line 1317:

```csharp
Assert.NotEmpty(run.Result.GetExceptionPaths(argumentNullException))
```

### R9-08: `DOC611_SourceSequenceReturnFactsTests.DirectoryElementFact_DoesNotProveSequenceInstanceNonNull#0`

- File: `Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/DOC611_SourceSequenceReturnFactsTests.cs`
- Parameter case: existing Fact only
- Entry/modes: SolutionTransitiveNative ONLY
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.IO.Directory.EnumerateFiles(string, string, System.IO.SearchOption)
- New semantics: ONLY summary/Session.Analyze invocation rejected for required target/body coverage: observe classified incomplete/non-exact outcome, not ordinary escaping-path projection from a graph forbidden before production registration. Preserve recursive Project/Direct helper assertions; no raw production fallback or second evaluator.

Current line 1317:

```csharp
Assert.NotEmpty(run.Result.GetExceptionPaths(argumentNullException))
```

### R9-09: `DOC611_SourceSequenceReturnFactsTests.DirectoryEnumerateFilesDifferentOverload_IsNotModeled#0`

- File: `Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/DOC611_SourceSequenceReturnFactsTests.cs`
- Parameter case: existing Fact only
- Entry/modes: SolutionTransitiveNative ONLY
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.Generic.IEnumerable<string>.GetEnumerator()' on receiver type 'System.Collections.Generic.IEnumerable<string>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.Generic.IEnumerator<string>.Current.get' on receiver type 'System.Collections.Generic.IEnumerator<string>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.IEnumerator.MoveNext()' on receiver type 'System.Collections.Generic.IEnumerator<string>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.IDisposable.Dispose()' on receiver type 'System.Collections.Generic.IEnumerator<string>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.IEnumerable<T>.GetEnumerator(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.IEnumerator<T>.Current.get; TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.IEnumerator.MoveNext(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.IDisposable.Dispose(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.IO.Directory.EnumerateFiles(string)
- New semantics: ONLY summary/Session.Analyze invocation rejected for required target/body coverage: observe classified incomplete/non-exact outcome, not ordinary escaping-path projection from a graph forbidden before production registration. Preserve recursive Project/Direct helper assertions; no raw production fallback or second evaluator.

Current line 1317:

```csharp
Assert.NotEmpty(run.Result.GetExceptionPaths(argumentNullException))
```

### R9-10: `DOC611_SourceSequenceReturnFactsTests.ForeignDirectoryWithMatchingTypeAndMethodNames_IsNotModeled#0`

- File: `Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/DOC611_SourceSequenceReturnFactsTests.cs`
- Parameter case: existing Fact only
- Entry/modes: SolutionTransitiveNative ONLY
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.Generic.IEnumerable<string?>.GetEnumerator()' on receiver type 'System.Collections.Generic.IEnumerable<string?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.Generic.IEnumerator<string?>.Current.get' on receiver type 'System.Collections.Generic.IEnumerator<string?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.IEnumerator.MoveNext()' on receiver type 'System.Collections.Generic.IEnumerator<string?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.IDisposable.Dispose()' on receiver type 'System.Collections.Generic.IEnumerator<string?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.IEnumerable<T>.GetEnumerator(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.IEnumerator<T>.Current.get; TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.IEnumerator.MoveNext(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.IDisposable.Dispose()
- New semantics: ONLY summary/Session.Analyze invocation rejected for required target/body coverage: observe classified incomplete/non-exact outcome, not ordinary escaping-path projection from a graph forbidden before production registration. Preserve recursive Project/Direct helper assertions; no raw production fallback or second evaluator.

Current line 1317:

```csharp
Assert.NotEmpty(run.Result.GetExceptionPaths(argumentNullException))
```

### R9-11: `DOC611_SourceSequenceReturnFactsTests.ForeignEnumerateFilesMethod_IsNotFrameworkModeled#0`

- File: `Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/DOC611_SourceSequenceReturnFactsTests.cs`
- Parameter case: existing Fact only
- Entry/modes: SolutionTransitiveNative ONLY
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.Generic.IEnumerable<string?>.GetEnumerator()' on receiver type 'System.Collections.Generic.IEnumerable<string?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.Generic.IEnumerator<string?>.Current.get' on receiver type 'System.Collections.Generic.IEnumerator<string?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.IEnumerator.MoveNext()' on receiver type 'System.Collections.Generic.IEnumerator<string?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.IDisposable.Dispose()' on receiver type 'System.Collections.Generic.IEnumerator<string?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.IEnumerable<T>.GetEnumerator(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.IEnumerator<T>.Current.get; TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.IEnumerator.MoveNext(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.IDisposable.Dispose()
- New semantics: ONLY summary/Session.Analyze invocation rejected for required target/body coverage: observe classified incomplete/non-exact outcome, not ordinary escaping-path projection from a graph forbidden before production registration. Preserve recursive Project/Direct helper assertions; no raw production fallback or second evaluator.

Current line 1317:

```csharp
Assert.NotEmpty(run.Result.GetExceptionPaths(argumentNullException))
```

### R9-12: `DOC611_SourceSequenceReturnFactsTests.IteratorSourceCanYieldNull_RemainsPotentiallyNull#0`

- File: `Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/DOC611_SourceSequenceReturnFactsTests.cs`
- Parameter case: existing Fact only
- Entry/modes: SolutionTransitiveNative ONLY
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.Generic.IEnumerable<EntryPoint.Item?>.GetEnumerator()' on receiver type 'System.Collections.Generic.IEnumerable<EntryPoint.Item?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.Generic.IEnumerator<EntryPoint.Item?>.Current.get' on receiver type 'System.Collections.Generic.IEnumerator<EntryPoint.Item?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.IEnumerator.MoveNext()' on receiver type 'System.Collections.Generic.IEnumerator<EntryPoint.Item?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.IDisposable.Dispose()' on receiver type 'System.Collections.Generic.IEnumerator<EntryPoint.Item?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.IEnumerable<T>.GetEnumerator(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.IEnumerator<T>.Current.get; TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.IEnumerator.MoveNext(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.IDisposable.Dispose()
- New semantics: ONLY summary/Session.Analyze invocation rejected for required target/body coverage: observe classified incomplete/non-exact outcome, not ordinary escaping-path projection from a graph forbidden before production registration. Preserve recursive Project/Direct helper assertions; no raw production fallback or second evaluator.

Current line 1317:

```csharp
Assert.NotEmpty(run.Result.GetExceptionPaths(argumentNullException))
```

### R9-13: `DOC611_SourceSequenceReturnFactsTests.MutuallyRecursiveAddRangeSourceReturns_RemainPotentiallyNull#0`

- File: `Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/DOC611_SourceSequenceReturnFactsTests.cs`
- Parameter case: existing Fact only
- Entry/modes: SolutionTransitiveNative ONLY
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.Generic.IEnumerable<EntryPoint.Item?>.GetEnumerator()' on receiver type 'System.Collections.Generic.IEnumerable<EntryPoint.Item?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.Generic.IEnumerator<EntryPoint.Item?>.Current.get' on receiver type 'System.Collections.Generic.IEnumerator<EntryPoint.Item?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.IEnumerator.MoveNext()' on receiver type 'System.Collections.Generic.IEnumerator<EntryPoint.Item?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.IDisposable.Dispose()' on receiver type 'System.Collections.Generic.IEnumerator<EntryPoint.Item?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.IEnumerable<T>.GetEnumerator(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.IEnumerator<T>.Current.get; TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.List<T>.AddRange(System.Collections.Generic.IEnumerable<T>); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.List<T>.List(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.IEnumerator.MoveNext(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.IDisposable.Dispose()
- New semantics: ONLY summary/Session.Analyze invocation rejected for required target/body coverage: observe classified incomplete/non-exact outcome, not ordinary escaping-path projection from a graph forbidden before production registration. Preserve recursive Project/Direct helper assertions; no raw production fallback or second evaluator.

Current line 1317:

```csharp
Assert.NotEmpty(run.Result.GetExceptionPaths(argumentNullException))
```

### R9-14: `DOC611_SourceSequenceReturnFactsTests.MutuallyRecursiveSourceSequenceArgumentFacts_RemainPotentiallyNull#0`

- File: `Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/DOC611_SourceSequenceReturnFactsTests.cs`
- Parameter case: existing Fact only
- Entry/modes: SolutionTransitiveNative ONLY
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.Generic.IEnumerable<EntryPoint.Item?>.GetEnumerator()' on receiver type 'System.Collections.Generic.IEnumerable<EntryPoint.Item?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.Generic.IEnumerator<EntryPoint.Item?>.Current.get' on receiver type 'System.Collections.Generic.IEnumerator<EntryPoint.Item?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.IEnumerator.MoveNext()' on receiver type 'System.Collections.Generic.IEnumerator<EntryPoint.Item?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.IDisposable.Dispose()' on receiver type 'System.Collections.Generic.IEnumerator<EntryPoint.Item?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.IEnumerable<T>.GetEnumerator(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.IEnumerator<T>.Current.get; TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.IEnumerator.MoveNext(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.IDisposable.Dispose()
- New semantics: ONLY summary/Session.Analyze invocation rejected for required target/body coverage: observe classified incomplete/non-exact outcome, not ordinary escaping-path projection from a graph forbidden before production registration. Preserve recursive Project/Direct helper assertions; no raw production fallback or second evaluator.

Current line 1317:

```csharp
Assert.NotEmpty(run.Result.GetExceptionPaths(argumentNullException))
```

### R9-15: `DOC611_SourceSequenceReturnFactsTests.RecursiveAddRangeSourceReturn_RemainsPotentiallyNull#0`

- File: `Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/DOC611_SourceSequenceReturnFactsTests.cs`
- Parameter case: existing Fact only
- Entry/modes: SolutionTransitiveNative ONLY
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.Generic.IEnumerable<EntryPoint.Item?>.GetEnumerator()' on receiver type 'System.Collections.Generic.IEnumerable<EntryPoint.Item?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.Generic.IEnumerator<EntryPoint.Item?>.Current.get' on receiver type 'System.Collections.Generic.IEnumerator<EntryPoint.Item?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.IEnumerator.MoveNext()' on receiver type 'System.Collections.Generic.IEnumerator<EntryPoint.Item?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.IDisposable.Dispose()' on receiver type 'System.Collections.Generic.IEnumerator<EntryPoint.Item?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.IEnumerable<T>.GetEnumerator(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.IEnumerator<T>.Current.get; TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.List<T>.AddRange(System.Collections.Generic.IEnumerable<T>); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.List<T>.List(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.IEnumerator.MoveNext(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.IDisposable.Dispose()
- New semantics: ONLY summary/Session.Analyze invocation rejected for required target/body coverage: observe classified incomplete/non-exact outcome, not ordinary escaping-path projection from a graph forbidden before production registration. Preserve recursive Project/Direct helper assertions; no raw production fallback or second evaluator.

Current line 1317:

```csharp
Assert.NotEmpty(run.Result.GetExceptionPaths(argumentNullException))
```

### R9-16: `DOC611_SourceSequenceReturnFactsTests.RecursiveSourceSequenceReturn_RemainsPotentiallyNull#0`

- File: `Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/DOC611_SourceSequenceReturnFactsTests.cs`
- Parameter case: existing Fact only
- Entry/modes: SolutionTransitiveNative ONLY
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.Generic.IEnumerable<EntryPoint.Item?>.GetEnumerator()' on receiver type 'System.Collections.Generic.IEnumerable<EntryPoint.Item?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.Generic.IEnumerator<EntryPoint.Item?>.Current.get' on receiver type 'System.Collections.Generic.IEnumerator<EntryPoint.Item?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.IEnumerator.MoveNext()' on receiver type 'System.Collections.Generic.IEnumerator<EntryPoint.Item?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.IDisposable.Dispose()' on receiver type 'System.Collections.Generic.IEnumerator<EntryPoint.Item?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.IEnumerable<T>.GetEnumerator(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.IEnumerator<T>.Current.get; TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.IEnumerator.MoveNext(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.IDisposable.Dispose()
- New semantics: ONLY summary/Session.Analyze invocation rejected for required target/body coverage: observe classified incomplete/non-exact outcome, not ordinary escaping-path projection from a graph forbidden before production registration. Preserve recursive Project/Direct helper assertions; no raw production fallback or second evaluator.

Current line 1317:

```csharp
Assert.NotEmpty(run.Result.GetExceptionPaths(argumentNullException))
```

### R9-17: `DOC611_SourceSequenceReturnFactsTests.SameSourceMethodWithSafeAndUnknownArguments_DoesNotMixContexts#0`

- File: `Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/DOC611_SourceSequenceReturnFactsTests.cs`
- Parameter case: existing Fact only
- Entry/modes: SolutionTransitiveNative ONLY
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.Generic.IEnumerable<EntryPoint.Item?>.GetEnumerator()' on receiver type 'System.Collections.Generic.IReadOnlyList<EntryPoint.Item?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.Generic.IEnumerator<EntryPoint.Item?>.Current.get' on receiver type 'System.Collections.Generic.IEnumerator<EntryPoint.Item?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.IEnumerator.MoveNext()' on receiver type 'System.Collections.Generic.IEnumerator<EntryPoint.Item?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.IDisposable.Dispose()' on receiver type 'System.Collections.Generic.IEnumerator<EntryPoint.Item?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.IEnumerable<T>.GetEnumerator(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.IEnumerator<T>.Current.get; TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.List<T>.Add(T); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.List<T>.List(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.IEnumerator.MoveNext(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.IDisposable.Dispose()
- New semantics: ONLY summary/Session.Analyze invocation rejected for required target/body coverage: observe classified incomplete/non-exact outcome, not ordinary escaping-path projection from a graph forbidden before production registration. Preserve recursive Project/Direct helper assertions; no raw production fallback or second evaluator.

Current line 1317:

```csharp
Assert.NotEmpty(run.Result.GetExceptionPaths(argumentNullException))
```

### R9-18: `DOC611_SourceSequenceReturnFactsTests.SourceListAliasMutation_RemainsPotentiallyNull#0`

- File: `Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/DOC611_SourceSequenceReturnFactsTests.cs`
- Parameter case: existing Fact only
- Entry/modes: SolutionTransitiveNative ONLY
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.Generic.IEnumerable<EntryPoint.Item?>.GetEnumerator()' on receiver type 'System.Collections.Generic.IReadOnlyList<EntryPoint.Item?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.Generic.IEnumerator<EntryPoint.Item?>.Current.get' on receiver type 'System.Collections.Generic.IEnumerator<EntryPoint.Item?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.IEnumerator.MoveNext()' on receiver type 'System.Collections.Generic.IEnumerator<EntryPoint.Item?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.IDisposable.Dispose()' on receiver type 'System.Collections.Generic.IEnumerator<EntryPoint.Item?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.IEnumerable<T>.GetEnumerator(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.IEnumerator<T>.Current.get; TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.List<T>.Add(T); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.List<T>.List(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.IEnumerator.MoveNext(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.IDisposable.Dispose()
- New semantics: ONLY summary/Session.Analyze invocation rejected for required target/body coverage: observe classified incomplete/non-exact outcome, not ordinary escaping-path projection from a graph forbidden before production registration. Preserve recursive Project/Direct helper assertions; no raw production fallback or second evaluator.

Current line 1317:

```csharp
Assert.NotEmpty(run.Result.GetExceptionPaths(argumentNullException))
```

### R9-19: `DOC611_SourceSequenceReturnFactsTests.SourceListIndexMutation_RemainsPotentiallyNull#0`

- File: `Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/DOC611_SourceSequenceReturnFactsTests.cs`
- Parameter case: existing Fact only
- Entry/modes: SolutionTransitiveNative ONLY
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.Generic.IEnumerable<EntryPoint.Item?>.GetEnumerator()' on receiver type 'System.Collections.Generic.IReadOnlyList<EntryPoint.Item?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.Generic.IEnumerator<EntryPoint.Item?>.Current.get' on receiver type 'System.Collections.Generic.IEnumerator<EntryPoint.Item?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.IEnumerator.MoveNext()' on receiver type 'System.Collections.Generic.IEnumerator<EntryPoint.Item?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.IDisposable.Dispose()' on receiver type 'System.Collections.Generic.IEnumerator<EntryPoint.Item?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.IEnumerable<T>.GetEnumerator(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.IEnumerator<T>.Current.get; TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.List<T>.Add(T); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.List<T>.List(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.List<T>.this[int].set; TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.IEnumerator.MoveNext(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.IDisposable.Dispose()
- New semantics: ONLY summary/Session.Analyze invocation rejected for required target/body coverage: observe classified incomplete/non-exact outcome, not ordinary escaping-path projection from a graph forbidden before production registration. Preserve recursive Project/Direct helper assertions; no raw production fallback or second evaluator.

Current line 1317:

```csharp
Assert.NotEmpty(run.Result.GetExceptionPaths(argumentNullException))
```

### R9-20: `DOC611_SourceSequenceReturnFactsTests.SourceListPassedToUnknownCode_RemainsPotentiallyNull#0`

- File: `Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/DOC611_SourceSequenceReturnFactsTests.cs`
- Parameter case: existing Fact only
- Entry/modes: SolutionTransitiveNative ONLY
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.Generic.IEnumerable<EntryPoint.Item?>.GetEnumerator()' on receiver type 'System.Collections.Generic.IReadOnlyList<EntryPoint.Item?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.Generic.IEnumerator<EntryPoint.Item?>.Current.get' on receiver type 'System.Collections.Generic.IEnumerator<EntryPoint.Item?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.IEnumerator.MoveNext()' on receiver type 'System.Collections.Generic.IEnumerator<EntryPoint.Item?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.IDisposable.Dispose()' on receiver type 'System.Collections.Generic.IEnumerator<EntryPoint.Item?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Delegate invocation; TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.IEnumerable<T>.GetEnumerator(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.IEnumerator<T>.Current.get; TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.List<T>.Add(T); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.List<T>.List(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.IEnumerator.MoveNext(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.IDisposable.Dispose()
- New semantics: ONLY summary/Session.Analyze invocation rejected for required target/body coverage: observe classified incomplete/non-exact outcome, not ordinary escaping-path projection from a graph forbidden before production registration. Preserve recursive Project/Direct helper assertions; no raw production fallback or second evaluator.

Current line 1317:

```csharp
Assert.NotEmpty(run.Result.GetExceptionPaths(argumentNullException))
```

### R9-21: `DOC611_SourceSequenceReturnFactsTests.SourceListRefEscape_RemainsPotentiallyNull#0`

- File: `Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/DOC611_SourceSequenceReturnFactsTests.cs`
- Parameter case: existing Fact only
- Entry/modes: SolutionTransitiveNative ONLY
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.Generic.IEnumerable<EntryPoint.Item?>.GetEnumerator()' on receiver type 'System.Collections.Generic.IReadOnlyList<EntryPoint.Item?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.Generic.IEnumerator<EntryPoint.Item?>.Current.get' on receiver type 'System.Collections.Generic.IEnumerator<EntryPoint.Item?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.IEnumerator.MoveNext()' on receiver type 'System.Collections.Generic.IEnumerator<EntryPoint.Item?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.IDisposable.Dispose()' on receiver type 'System.Collections.Generic.IEnumerator<EntryPoint.Item?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.IEnumerable<T>.GetEnumerator(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.IEnumerator<T>.Current.get; TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.List<T>.Add(T); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.List<T>.List(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.IEnumerator.MoveNext(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.IDisposable.Dispose()
- New semantics: ONLY summary/Session.Analyze invocation rejected for required target/body coverage: observe classified incomplete/non-exact outcome, not ordinary escaping-path projection from a graph forbidden before production registration. Preserve recursive Project/Direct helper assertions; no raw production fallback or second evaluator.

Current line 1317:

```csharp
Assert.NotEmpty(run.Result.GetExceptionPaths(argumentNullException))
```

### R9-22: `DOC611_SourceSequenceReturnFactsTests.SourceReturnCanContainNull_RemainsPotentiallyNull#0`

- File: `Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/DOC611_SourceSequenceReturnFactsTests.cs`
- Parameter case: existing Fact only
- Entry/modes: SolutionTransitiveNative ONLY
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.Generic.IEnumerable<EntryPoint.Item?>.GetEnumerator()' on receiver type 'System.Collections.Generic.IReadOnlyList<EntryPoint.Item?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.Generic.IEnumerator<EntryPoint.Item?>.Current.get' on receiver type 'System.Collections.Generic.IEnumerator<EntryPoint.Item?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.IEnumerator.MoveNext()' on receiver type 'System.Collections.Generic.IEnumerator<EntryPoint.Item?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.IDisposable.Dispose()' on receiver type 'System.Collections.Generic.IEnumerator<EntryPoint.Item?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.IEnumerable<T>.GetEnumerator(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.IEnumerator<T>.Current.get; TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.List<T>.Add(T); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.List<T>.List(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.IEnumerator.MoveNext(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.IDisposable.Dispose()
- New semantics: ONLY summary/Session.Analyze invocation rejected for required target/body coverage: observe classified incomplete/non-exact outcome, not ordinary escaping-path projection from a graph forbidden before production registration. Preserve recursive Project/Direct helper assertions; no raw production fallback or second evaluator.

Current line 1317:

```csharp
Assert.NotEmpty(run.Result.GetExceptionPaths(argumentNullException))
```

### R9-23: `DOC611_SourceSequenceReturnFactsTests.SourceReturnedListAddsUnknownRange_RemainsPotentiallyNull#0`

- File: `Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/DOC611_SourceSequenceReturnFactsTests.cs`
- Parameter case: existing Fact only
- Entry/modes: SolutionTransitiveNative ONLY
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.Generic.IEnumerable<EntryPoint.Item?>.GetEnumerator()' on receiver type 'System.Collections.Generic.IReadOnlyList<EntryPoint.Item?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.Generic.IEnumerator<EntryPoint.Item?>.Current.get' on receiver type 'System.Collections.Generic.IEnumerator<EntryPoint.Item?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.IEnumerator.MoveNext()' on receiver type 'System.Collections.Generic.IEnumerator<EntryPoint.Item?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.IDisposable.Dispose()' on receiver type 'System.Collections.Generic.IEnumerator<EntryPoint.Item?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.IEnumerable<T>.GetEnumerator(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.IEnumerator<T>.Current.get; TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.List<T>.AddRange(System.Collections.Generic.IEnumerable<T>); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.List<T>.List(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.IEnumerator.MoveNext(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.IDisposable.Dispose()
- New semantics: ONLY summary/Session.Analyze invocation rejected for required target/body coverage: observe classified incomplete/non-exact outcome, not ordinary escaping-path projection from a graph forbidden before production registration. Preserve recursive Project/Direct helper assertions; no raw production fallback or second evaluator.

Current line 1317:

```csharp
Assert.NotEmpty(run.Result.GetExceptionPaths(argumentNullException))
```

### R9-24: `DOC611_SourceSequenceReturnFactsTests.SourceReturnedListMutatedBeforeForeach_RemainsPotentiallyNull#0`

- File: `Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/DOC611_SourceSequenceReturnFactsTests.cs`
- Parameter case: existing Fact only
- Entry/modes: SolutionTransitiveNative ONLY
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.List<T>.Add(T); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.List<T>.Enumerator.Current.get; TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.List<T>.Enumerator.MoveNext(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.List<T>.GetEnumerator(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.List<T>.List(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.IDisposable.Dispose()
- New semantics: ONLY summary/Session.Analyze invocation rejected for required target/body coverage: observe classified incomplete/non-exact outcome, not ordinary escaping-path projection from a graph forbidden before production registration. Preserve recursive Project/Direct helper assertions; no raw production fallback or second evaluator.

Current line 1317:

```csharp
Assert.NotEmpty(run.Result.GetExceptionPaths(argumentNullException))
```

### R9-25: `DOC611_SourceSequenceReturnFactsTests.SourceReturnsCollectionInitializerWithNull_RemainsPotentiallyNull#0`

- File: `Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/DOC611_SourceSequenceReturnFactsTests.cs`
- Parameter case: existing Fact only
- Entry/modes: SolutionTransitiveNative ONLY
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.Generic.IEnumerable<EntryPoint.Item?>.GetEnumerator()' on receiver type 'System.Collections.Generic.IReadOnlyList<EntryPoint.Item?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.Generic.IEnumerator<EntryPoint.Item?>.Current.get' on receiver type 'System.Collections.Generic.IEnumerator<EntryPoint.Item?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.IEnumerator.MoveNext()' on receiver type 'System.Collections.Generic.IEnumerator<EntryPoint.Item?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.IDisposable.Dispose()' on receiver type 'System.Collections.Generic.IEnumerator<EntryPoint.Item?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.IEnumerable<T>.GetEnumerator(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.IEnumerator<T>.Current.get; TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.List<T>.Add(T); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.List<T>.List(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.IEnumerator.MoveNext(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.IDisposable.Dispose()
- New semantics: ONLY summary/Session.Analyze invocation rejected for required target/body coverage: observe classified incomplete/non-exact outcome, not ordinary escaping-path projection from a graph forbidden before production registration. Preserve recursive Project/Direct helper assertions; no raw production fallback or second evaluator.

Current line 1317:

```csharp
Assert.NotEmpty(run.Result.GetExceptionPaths(argumentNullException))
```

### R9-26: `DOC611_SourceSequenceReturnFactsTests.VirtualSourceReturn_DoesNotAssumeOneImplementation#0`

- File: `Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/DOC611_SourceSequenceReturnFactsTests.cs`
- Parameter case: existing Fact only
- Entry/modes: SolutionTransitiveNative ONLY
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'Provider.CreateItems()' on receiver type 'Provider' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.Generic.IEnumerable<Item?>.GetEnumerator()' on receiver type 'System.Collections.Generic.IReadOnlyList<Item?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.Generic.IEnumerator<Item?>.Current.get' on receiver type 'System.Collections.Generic.IEnumerator<Item?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.IEnumerator.MoveNext()' on receiver type 'System.Collections.Generic.IEnumerator<Item?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.IDisposable.Dispose()' on receiver type 'System.Collections.Generic.IEnumerator<Item?>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.IEnumerable<T>.GetEnumerator(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.IEnumerator<T>.Current.get; TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.List<T>.Add(T); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.List<T>.List(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.IEnumerator.MoveNext(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.IDisposable.Dispose()
- New semantics: ONLY summary/Session.Analyze invocation rejected for required target/body coverage: observe classified incomplete/non-exact outcome, not ordinary escaping-path projection from a graph forbidden before production registration. Preserve recursive Project/Direct helper assertions; no raw production fallback or second evaluator.

Current line 1317:

```csharp
Assert.NotEmpty(run.Result.GetExceptionPaths(argumentNullException))
```

### R9-27: `ExceptionFlowSequenceSourceHelperTests.MutatingSourceHelper_InvalidatesNonNullElements#0`

- File: `Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/ExceptionFlowSequenceSourceHelperTests.cs`
- Parameter case: existing Fact only
- Entry/modes: SolutionTransitiveNative ONLY
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.Generic.IEnumerable<Item>.GetEnumerator()' on receiver type 'System.Collections.Generic.IReadOnlyList<Item>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.Generic.IEnumerator<Item>.Current.get' on receiver type 'System.Collections.Generic.IEnumerator<Item>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.Collections.IEnumerator.MoveNext()' on receiver type 'System.Collections.Generic.IEnumerator<Item>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: Additional runtime dispatch targets for 'System.IDisposable.Dispose()' on receiver type 'System.Collections.Generic.IEnumerator<Item>' may exist outside the analyzed project closure.; TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.IEnumerable<T>.GetEnumerator(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.IEnumerable<TSource>.ToList<TSource>(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.IEnumerator<T>.Current.get; TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.Generic.List<T>.Add(T); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.IEnumerable.OfType<TResult>(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.Collections.IEnumerator.MoveNext(); TargetOrAnalysisCoverageIncomplete: no modeled executable body for System.IDisposable.Dispose()
- New semantics: ONLY summary/Session.Analyze invocation rejected for required target/body coverage: observe classified incomplete/non-exact outcome, not ordinary escaping-path projection from a graph forbidden before production registration. Preserve recursive Project/Direct helper assertions; no raw production fallback or second evaluator.

Current line 273:

```csharp
Assert.NotEmpty(
                run.Result.GetExceptionPaths(
                    argumentNullException))
```

## R10 (14 exact rows)

Only listed actual profile-rejection code expectations: AnalysisFailure -> existing UnsupportedContext plus deterministic profile/reason/context Details. All failure/null-result/no-receipt/no-fallback, compiler/transport, parity, PID and isolation assertions stay. This is NOT an enum/protocol change or a blanket rewrite of AnalysisFailure.

### R10-01: `ExceptionFlowAnalysisDispatchTests.WorkerStructuredFailure_PropagatesOriginalFailureAndStopsProcess [source=async Task Root await Task.CompletedTask ONLY; not Missing()]`

- File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisDispatchTests.cs`
- Parameter case: source=async Task Root await Task.CompletedTask ONLY; not Missing()
- Entry/modes: Historical selected Summary/profile query ONLY
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: Runtime-await required capability unavailable in historical adapter
- New semantics: ONLY if the actual new A9 rejection is certified: AnalysisFailure -> existing UnsupportedContext envelope plus deterministic profile/reason/context Details. Preserve unsuccessful status, null success payloads, old compiler/transport/other AnalysisFailure behavior, no fallback, all parity/receipt/PID/isolation assertions.

Current line 189:

```csharp
[InlineData("public static class Fixture { public static async System.Threading.Tasks.Task Root() { await System.Threading.Tasks.Task.CompletedTask; } }", "AnalysisFailure")]
```

### R10-02: `ExceptionFlowAnalysisRouterTests.HistoricalAnalysisFailure_PreservesOriginalB4FailureContract [source=async Task Root await Task.CompletedTask ONLY; not Missing()]`

- File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowAnalysisRouterTests.cs`
- Parameter case: source=async Task Root await Task.CompletedTask ONLY; not Missing()
- Entry/modes: Historical selected Summary/profile query ONLY
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: Runtime-await required capability unavailable in historical adapter
- New semantics: ONLY if the actual new A9 rejection is certified: AnalysisFailure -> existing UnsupportedContext envelope plus deterministic profile/reason/context Details. Preserve unsuccessful status, null success payloads, old compiler/transport/other AnalysisFailure behavior, no fallback, all parity/receipt/PID/isolation assertions.

Current line 161:

```csharp
[InlineData("public static class Fixture { public static async System.Threading.Tasks.Task Root() { await System.Threading.Tasks.Task.CompletedTask; } }", "AnalysisFailure")]
```

### R10-03: `HistoricalWorkerClientTests.RealWorkerFailure_IsStructuredAndNeverEmptySuccess [source=async Task Root await Task.CompletedTask ONLY; other InlineData unchanged]`

- File: `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerClientTests.cs`
- Parameter case: source=async Task Root await Task.CompletedTask ONLY; other InlineData unchanged
- Entry/modes: Historical selected Summary/profile query ONLY
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: Runtime-await required capability unavailable in historical adapter
- New semantics: ONLY if the actual new A9 rejection is certified: AnalysisFailure -> existing UnsupportedContext envelope plus deterministic profile/reason/context Details. Preserve unsuccessful status, null success payloads, old compiler/transport/other AnalysisFailure behavior, no fallback, all parity/receipt/PID/isolation assertions.

Current line 84:

```csharp
[InlineData("public static class Fixture { public static async System.Threading.Tasks.Task Root() { await System.Threading.Tasks.Task.CompletedTask; } }", "AnalysisFailure")]
```

### R10-04: `HistoricalWorkerProcessTests.HistoricalAwait_ActuallyPropagatesUnavailableInformationAsFailure [Fact]`

- File: `Tests/XMLDocNormalizerTests/Worker/HistoricalWorkerProcessTests.cs`
- Parameter case: Fact
- Entry/modes: Historical selected Summary/profile query ONLY
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: Runtime-await required capability unavailable; retain original information-unavailable detail assertion
- New semantics: ONLY if the actual new A9 rejection is certified: AnalysisFailure -> existing UnsupportedContext envelope plus deterministic profile/reason/context Details. Preserve unsuccessful status, null success payloads, old compiler/transport/other AnalysisFailure behavior, no fallback, all parity/receipt/PID/isolation assertions.

Current line 146:

```csharp
AssertFailure(result, "analysisFailure");
```

### R10-05: `ExceptionFlowHistoricalDeclaredBoundaryTests.Uncertainty_RelevanceIsDetectorPolicy_NotFilteredRawAnalysisOrXmlProof [Payload(uncertain), worker code ONLY]`

- File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredBoundaryTests.cs`
- Parameter case: Payload(uncertain), worker code ONLY
- Entry/modes: Historical selected Summary/profile query ONLY
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: GC.Collect metadata target has no modeled executable body; preserve DOC631 relevance differential
- New semantics: ONLY if the actual new A9 rejection is certified: AnalysisFailure -> existing UnsupportedContext envelope plus deterministic profile/reason/context Details. Preserve unsuccessful status, null success payloads, old compiler/transport/other AnalysisFailure behavior, no fallback, all parity/receipt/PID/isolation assertions.

Current line 190:

```csharp
Assert.Equal(WorkerFailureCode.AnalysisFailure, worker.Failure!.WorkerFailure!.Code)
```

### R10-06: `ExceptionFlowHistoricalDeclaredPolicyTests.IncompleteRequestOriginalReceiptsAndUnsupportedShapes_DoNotActivateOrDowngrade [Payload(uncertain), final worker failure ONLY]`

- File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredPolicyTests.cs`
- Parameter case: Payload(uncertain), final worker failure ONLY
- Entry/modes: Historical selected Summary/profile query ONLY
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: GC.Collect metadata body unavailable; unrelated negative requests remain unchanged
- New semantics: ONLY if the actual new A9 rejection is certified: AnalysisFailure -> existing UnsupportedContext envelope plus deterministic profile/reason/context Details. Preserve unsuccessful status, null success payloads, old compiler/transport/other AnalysisFailure behavior, no fallback, all parity/receipt/PID/isolation assertions.

Current line 236:

```csharp
Assert.Equal(WorkerFailureCode.AnalysisFailure, failed.Failure!.WorkerFailure!.Code)
```

### R10-07: `ExceptionFlowHistoricalDeclaredReadinessTests.NoLocalDeclarationGuard_CannotBeReplacedByUnguardedProjectExecution [unguarded Project worker ONLY; original Declared guard unchanged]`

- File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredReadinessTests.cs`
- Parameter case: unguarded Project worker ONLY; original Declared guard unchanged
- Entry/modes: Historical selected Summary/profile query ONLY
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: GC.Collect target/body unavailable; do not widen original guard
- New semantics: ONLY if the actual new A9 rejection is certified: AnalysisFailure -> existing UnsupportedContext envelope plus deterministic profile/reason/context Details. Preserve unsuccessful status, null success payloads, old compiler/transport/other AnalysisFailure behavior, no fallback, all parity/receipt/PID/isolation assertions.

Current line 109:

```csharp
Assert.Equal(WorkerFailureCode.AnalysisFailure, call.Failure.WorkerFailure!.Code)
```

### R10-08: `ExceptionFlowHistoricalFindingContractTests.OriginalContextReceipt_RequestAdmissionLegacyAndUncertainGuard_AreNotBypassed [uncertain external payload final worker ONLY]`

- File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalFindingContractTests.cs`
- Parameter case: uncertain external payload final worker ONLY
- Entry/modes: Historical selected Summary/profile query ONLY
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: GC.Collect target/body unavailable; receipts/replay assertions unchanged
- New semantics: ONLY if the actual new A9 rejection is certified: AnalysisFailure -> existing UnsupportedContext envelope plus deterministic profile/reason/context Details. Preserve unsuccessful status, null success payloads, old compiler/transport/other AnalysisFailure behavior, no fallback, all parity/receipt/PID/isolation assertions.

Current line 234:

```csharp
Assert.Equal(WorkerFailureCode.AnalysisFailure, call.Failure!.WorkerFailure!.Code)
```

### R10-09: `ExceptionFlowHistoricalProductionAdmissionTests.ActualProductionGuard_AndGenuineB19Inputs_AreRecordedWithoutPartialActivation [scenario=guard ONLY]`

- File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalProductionAdmissionTests.cs`
- Parameter case: scenario=guard ONLY
- Entry/modes: Historical selected Summary/profile query ONLY
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: GC.Collect target/body unavailable; no local Declared summary request
- New semantics: ONLY if the actual new A9 rejection is certified: AnalysisFailure -> existing UnsupportedContext envelope plus deterministic profile/reason/context Details. Preserve unsuccessful status, null success payloads, old compiler/transport/other AnalysisFailure behavior, no fallback, all parity/receipt/PID/isolation assertions.

Current line 153:

```csharp
Assert.Equal(WorkerFailureCode.AnalysisFailure, call.Failure!.WorkerFailure!.Code)
```

### R10-10: `ExceptionFlowHistoricalDeclaredProductionFindingTests.ProductiveDetector_GenuineAttestedFindingsFullParityAndRepeat [scenario=guard-direct, unguarded projectCall ONLY]`

- File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs`
- Parameter case: scenario=guard-direct, unguarded projectCall ONLY
- Entry/modes: Historical selected Summary/profile query ONLY
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: GC.Collect unknown residual; guarded Direct DOC610 remains unchanged
- New semantics: ONLY if the actual new A9 rejection is certified: AnalysisFailure -> existing UnsupportedContext envelope plus deterministic profile/reason/context Details. Preserve unsuccessful status, null success payloads, old compiler/transport/other AnalysisFailure behavior, no fallback, all parity/receipt/PID/isolation assertions.

Current line 86:

```csharp
Assert.Equal(WorkerFailureCode.AnalysisFailure,
                    projectCall.Dispatch!.Routing!.HistoricalAnalysis!.Failure!.WorkerFailure!.Code)
```

### R10-11: `ExceptionFlowHistoricalDeclaredProductionFindingTests.SelectedSummaryWorkerFailure_DoesNotPublishPartialDirectFindings [Fact, selected Summary code ONLY]`

- File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalDeclaredProductionFindingTests.cs`
- Parameter case: Fact, selected Summary code ONLY
- Entry/modes: Historical selected Summary/profile query ONLY
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: GC.Collect target/body unavailable; selected Direct facts are separate and not fallback output
- New semantics: ONLY if the actual new A9 rejection is certified: AnalysisFailure -> existing UnsupportedContext envelope plus deterministic profile/reason/context Details. Preserve unsuccessful status, null success payloads, old compiler/transport/other AnalysisFailure behavior, no fallback, all parity/receipt/PID/isolation assertions.

Current line 256:

```csharp
Assert.Equal(WorkerFailureCode.AnalysisFailure, result.Summary!.Dispatch!.Routing!.HistoricalAnalysis!.Failure!.WorkerFailure!.Code)
```

### R10-12: `ExceptionFlowHistoricalBoundedFindingTests.UncertainSummary_IsNotRequestedByDirect_AndFailsClosedForProject [project=true ONLY; project=false unchanged]`

- File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalBoundedFindingTests.cs`
- Parameter case: project=true ONLY; project=false unchanged
- Entry/modes: Historical selected Summary/profile query ONLY
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: guard-direct GC.Collect unavailable; no partial finding receipt
- New semantics: ONLY if the actual new A9 rejection is certified: AnalysisFailure -> existing UnsupportedContext envelope plus deterministic profile/reason/context Details. Preserve unsuccessful status, null success payloads, old compiler/transport/other AnalysisFailure behavior, no fallback, all parity/receipt/PID/isolation assertions.

Current line 140:

```csharp
Assert.Equal(WorkerFailureCode.AnalysisFailure, result.Summary!.Dispatch!.Routing!.HistoricalAnalysis!.Failure!.WorkerFailure!.Code)
```

### R10-13: `ExceptionFlowHistoricalSolutionScopeTests.LocalModes_DoNotWidenToSupportingProjects [modeName=ProjectTransitive ONLY; Direct unchanged]`

- File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs`
- Parameter case: modeName=ProjectTransitive ONLY; Direct unchanged
- Entry/modes: Historical selected Summary/profile query ONLY
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: Reachable PE-only supporting target outside original local source scope
- New semantics: ONLY if the actual new A9 rejection is certified: AnalysisFailure -> existing UnsupportedContext envelope plus deterministic profile/reason/context Details. Preserve unsuccessful status, null success payloads, old compiler/transport/other AnalysisFailure behavior, no fallback, all parity/receipt/PID/isolation assertions.

Current line 104:

```csharp
Assert.Equal(WorkerFailureCode.AnalysisFailure, worker.Failure!.WorkerFailure!.Code)
```

### R10-14: `ExceptionFlowHistoricalSolutionScopeTests.MissingReachableSourceProject_DoesNotReturnACompleteSmallerScope [Fact, missing C source ONLY; full graph baseline unchanged]`

- File: `Tests/XMLDocNormalizerTests/Worker/ExceptionFlowHistoricalSolutionScopeTests.cs`
- Parameter case: Fact, missing C source ONLY; full graph baseline unchanged
- Entry/modes: Historical selected Summary/profile query ONLY
- A9: OUTSIDE PROFILE / TargetOrAnalysisCoverageIncomplete. No new permission previously received.
- Witness: TargetOrAnalysisCoverageIncomplete: C PE retained but reachable source receipt absent; no smaller exact graph
- New semantics: ONLY if the actual new A9 rejection is certified: AnalysisFailure -> existing UnsupportedContext envelope plus deterministic profile/reason/context Details. Preserve unsuccessful status, null success payloads, old compiler/transport/other AnalysisFailure behavior, no fallback, all parity/receipt/PID/isolation assertions.

Current line 188:

```csharp
Assert.Equal(WorkerFailureCode.AnalysisFailure, routing.HistoricalFailure!.WorkerFailure!.Code)
```

## Explicit exclusions and preserved contracts

R1-R6 keep their existing exact case lists. All 32 R7 methods are APPROVED conditionally and are marked approved in the manifest; no re-request. No DOC631-only assertion is inferred from incidental normal output. The two catch-all DOC631 absence tests, exact-coverage DOC631 test, XML-only evidence, invalid cref and non-exception diagnostics remain protected. The three Current uncertainty route tests assert execution success, not exact completeness; no need to edit them or reroute an engine. Positive finite Current/Historical finding families remain unchanged. Acquisition/reconstruction must use existing authorized source resolution before rejecting a missing body, preserving acquisition counts and scope/receipt checks. No self-analysis, Dual/Broad/Full/Boundary/WAE or test gate was run by this source audit.

The complete machine-readable assertion/case inventory is `Complete-fixture-preflight.json`. Approval of this ONE additional batch is requested before any production/test change. It cannot authorize falsely rejecting an exact fixture, silently deleting assertions, changing any unlisted assertion, or rewriting historical witness files.


## E — supplementary bindings resolved; no additional R11 request

All 85 Worker methods / 429 parameter cases / 3,202 original ambiguous
observations now have concrete parameter/owner/entry/input/scope bindings. The
original [gap catalog](Supplementary-assertion-binding-ambiguities.md) is retained
as the input list; [closure](Supplementary-binding-closure.md) and
[r2 bindings](Supplementary-resolved-bindings-r2.json) supersede its unresolved
status. All required changes are covered by R1–R10 / earlier grants. R8-R10
remain 73 pending rows, not permissions. No additional R11 is needed, and no
unlisted assertion or parameter case is authorized. No implementation in this run.
