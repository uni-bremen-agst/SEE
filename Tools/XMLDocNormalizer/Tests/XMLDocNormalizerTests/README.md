# XMLDocNormalizer Tests

## Standard test suite

The regular test suite can be executed with:

```powershell
dotnet test .\XMLDocNormalizer.sln --no-build
```

Self-analysis tests are excluded from the standard test run because they execute XMLDocNormalizer against its own production solution and can take several minutes.

Self-analysis tests are marked with:

```csharp
[Trait("Category", "SelfAnalysis")]
```

The default exclusion is configured in `default.runsettings`.

## Solution-transitive self-analysis

The solution-transitive self-analysis runs XMLDocNormalizer against its own solution using the `SolutionTransitive` exception-analysis mode.

Run only the self-analysis test with:

```powershell
dotnet test .\XMLDocNormalizer.sln `
    --no-build `
    -p:IncludeSelfAnalysis=true `
    --filter "Category=SelfAnalysis"
```

The self-analysis has no test-level timeout. It runs until the XMLDocNormalizer process terminates and can therefore take several minutes.

A manually started self-analysis can be aborted with `Ctrl+C`.

### Verify self-analysis discovery

To verify that only the self-analysis test is selected without executing it:

```powershell
dotnet test .\XMLDocNormalizer.sln `
    --no-build `
    -p:IncludeSelfAnalysis=true `
    --list-tests `
    --filter "Category=SelfAnalysis"
```

The result should contain only:

```text
XMLDocNormalizerTests.Execution.SolutionTransitiveSelfAnalysisTests.ProductionProject_CompletesWithoutUnhandledException
```

## Run all tests including self-analysis

To execute the regular test suite together with the self-analysis tests:

```powershell
dotnet test .\XMLDocNormalizer.sln `
    --no-build `
    -p:IncludeSelfAnalysis=true
```
