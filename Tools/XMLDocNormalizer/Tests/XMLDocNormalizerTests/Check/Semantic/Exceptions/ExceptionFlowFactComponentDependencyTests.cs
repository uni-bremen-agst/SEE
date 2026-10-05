using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Guards the acyclic lower layer shared by context construction and
    /// contextual value-fact discovery.
    /// </summary>
    public sealed class ExceptionFlowFactComponentDependencyTests
    {
        /// <summary>
        /// Ensures all extracted fact components remain closed static
        /// implementation details without interface dispatch.
        /// </summary>
        [Fact]
        public void Components_AreStaticAndImplementNoInterfaces()
        {
            Type[] componentTypes =
            [
                typeof(ExceptionFlowArgumentMapper),
                typeof(ExceptionFlowCallContextFactProjector),
                typeof(ExceptionFlowConditionalWeakTableValueFactsProvider),
                typeof(ExceptionFlowDataFlowFactsProvider),
                typeof(ExceptionFlowDelegateTargetResolver),
                typeof(ExceptionFlowDereferenceFactDiscovery),
                typeof(ExceptionFlowEnumValueFactsProvider),
                typeof(ExceptionFlowGuardFactsProvider),
                typeof(ExceptionFlowImmutableMemberValueFactsProvider),
                typeof(ExceptionFlowKnownPropertyValueFactsProvider),
                typeof(ExceptionFlowLocalInitializerFactsProvider),
                typeof(ExceptionFlowNullabilityFactsProvider),
                typeof(ExceptionFlowPrimitiveValueFactsProvider),
                typeof(ExceptionFlowRuntimeDispatchClassifier),
                typeof(ExceptionFlowSequenceCollectionFactsProvider),
                typeof(ExceptionFlowSequenceContentPreservationFactsProvider),
                typeof(ExceptionFlowSourcePositionValueFactsProvider),
                typeof(ExceptionFlowStableMemberFacts),
                typeof(ExceptionFlowStableSourceMemberFactsProvider),
                typeof(ExceptionFlowSuccessfulSequenceValidationFactsProvider),
                typeof(ExceptionFlowSymbolUsageFacts)
            ];

            Assert.All(componentTypes, type => Assert.True(type.IsAbstract));
            Assert.All(componentTypes, type => Assert.True(type.IsSealed));
            Assert.All(componentTypes, type => Assert.Empty(type.GetInterfaces()));
        }

        /// <summary>
        /// Ensures the lower fact components cannot call back into the
        /// remaining analyzer partials and form a component cycle.
        /// </summary>
        [Fact]
        public void ExtractedComponents_DoNotDependOnAnalyzer()
        {
            string flowDirectory = GetFlowDirectory();
            string[] componentFiles =
            [
                "ExceptionFlowArgumentMapper.cs",
                "ExceptionFlowCallContextFactProjector.cs",
                "ExceptionFlowDataFlowFactsProvider.cs",
                "ExceptionFlowDelegateTargetResolver.cs",
                "ExceptionFlowDereferenceFactDiscovery.Callee.cs",
                "ExceptionFlowDereferenceFactDiscovery.cs",
                "ExceptionFlowEnumValueFactsProvider.cs",
                "ExceptionFlowGuardFactsProvider.cs",
                "ExceptionFlowImmutableMemberValueFactsProvider.cs",
                "ExceptionFlowKnownPropertyValueFactsProvider.cs",
                "ExceptionFlowLocalInitializerFactsProvider.cs",
                "ExceptionFlowNullabilityFactsProvider.cs",
                "ExceptionFlowPrimitiveValueFactsProvider.cs",
                "ExceptionFlowRuntimeDispatchClassifier.cs",
                "ExceptionFlowSequenceCollectionFactsProvider.cs",
                "ExceptionFlowSequenceContentPreservationFactsProvider.cs",
                "ExceptionFlowSourcePositionValueFactsProvider.cs",
                "ExceptionFlowStableMemberFacts.cs",
                "ExceptionFlowStableSourceMemberFactsProvider.cs",
                "ExceptionFlowSuccessfulSequenceValidationFactsProvider.cs",
                "ExceptionFlowSymbolUsageFacts.cs"
            ];

            Assert.All(
                componentFiles,
                file => Assert.DoesNotContain(
                    "ExceptionFlowAnalyzer",
                    File.ReadAllText(Path.Combine(flowDirectory, file)),
                    StringComparison.Ordinal));
        }

        /// <summary>
        /// Ensures the return-condition slice remains owned directly by the
        /// guard and nullability providers without Analyzer forwarding methods.
        /// </summary>
        [Fact]
        public void ReturnConditionHelpers_HaveDedicatedProviderOwners()
        {
            BindingFlags flags =
                BindingFlags.Static |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly;
            string[] conditionMethods =
            [
                "GetFactsProvenByDirectContainingReturnBranch",
                "TryGetDirectContainingIfBranch",
                "GetStringFactsProvenForStableExpressionByCondition",
                "AreEquivalentStableValueExpressions",
                "IsStableGuardValueExpression"
            ];
            string[] frameworkMethods =
            [
                "IsRoslynCompilationUnitRootMethod",
                "IsRoslynCSharpSyntaxTreeParseTextMethod",
                "IsSystemEnumToStringMethod"
            ];
            string[] analyzerMethods = typeof(ExceptionFlowAnalyzer)
                .GetMethods(flags)
                .Select(static method => method.Name)
                .ToArray();
            string[] guardMethods = typeof(ExceptionFlowGuardFactsProvider)
                .GetMethods(flags)
                .Select(static method => method.Name)
                .ToArray();
            string[] nullabilityMethods =
                typeof(ExceptionFlowNullabilityFactsProvider)
                    .GetMethods(flags)
                    .Select(static method => method.Name)
                    .ToArray();

            Assert.All(
                conditionMethods,
                method => Assert.Contains(method, guardMethods));
            Assert.All(
                frameworkMethods,
                method => Assert.Contains(method, nullabilityMethods));
            Assert.All(
                conditionMethods.Concat(frameworkMethods),
                method => Assert.DoesNotContain(method, analyzerMethods));
        }

        /// <summary>
        /// Ensures sequence shape, content-preservation, and call-context
        /// projection methods have cohesive owners without Analyzer facades.
        /// </summary>
        [Fact]
        public void SequenceHelpers_HaveDedicatedOwners()
        {
            BindingFlags flags =
                BindingFlags.Static |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly;
            string[] collectionMethods =
            [
                "IsFrameworkCollectionCountProperty",
                "TryGetElementPreservingSequenceSource",
                "IsElementPreservingSequenceMethod",
                "IsDictionaryValuesProperty",
                "IsKnownEmptyDictionaryCreation",
                "IsDictionaryType",
                "IsEqualityComparerType",
                "IsKnownMaterializedSequenceType"
            ];
            string[] preservationMethods =
            [
                "IsSourceHelperArgumentProvenToPreserveSequenceContents",
                "DoesSourceParameterPreserveSequenceContents",
                "IsInsideNestedCallable",
                "IsDirectForeachSequenceObservation",
                "IsSequenceParameterFactStillCurrent",
                "IsSequenceParameterFactStillCurrentAtUse",
                "DoesStatementPreserveSequenceParameterContents",
                "IsSupportedSequenceNullObservation",
                "IsLocalSequenceInitializerStillCurrent",
                "DoesContainingStatementEntryPreserveLocalSequenceContents",
                "DoesStatementPreserveLocalSequenceContents",
                "DoesSyntaxPreserveLocalSequenceContents",
                "IsSupportedReadOnlySequenceObservation"
            ];
            const string projectionMethod =
                "IsForeachIterationVariableProvenNonNullByCallContext";
            string[] analyzerMethods = typeof(ExceptionFlowAnalyzer)
                .GetMethods(flags)
                .Select(static method => method.Name)
                .ToArray();
            string[] collectionOwnerMethods =
                typeof(ExceptionFlowSequenceCollectionFactsProvider)
                    .GetMethods(flags)
                    .Select(static method => method.Name)
                    .ToArray();
            string[] preservationOwnerMethods =
                typeof(ExceptionFlowSequenceContentPreservationFactsProvider)
                    .GetMethods(flags)
                    .Select(static method => method.Name)
                    .ToArray();
            string[] projectorMethods =
                typeof(ExceptionFlowCallContextFactProjector)
                    .GetMethods(flags)
                    .Select(static method => method.Name)
                    .ToArray();

            Assert.All(
                collectionMethods,
                method => Assert.Contains(method, collectionOwnerMethods));
            Assert.All(
                preservationMethods,
                method => Assert.Contains(method, preservationOwnerMethods));
            Assert.Contains(projectionMethod, projectorMethods);
            Assert.All(
                collectionMethods
                    .Concat(preservationMethods)
                    .Append(projectionMethod),
                method => Assert.DoesNotContain(method, analyzerMethods));
        }

        /// <summary>
        /// Ensures dictionary value-fact projection, collection classification,
        /// and generic local-assignment discovery have their dedicated owners
        /// without Analyzer forwarding methods.
        /// </summary>
        [Fact]
        public void DictionaryValueFactHelpers_HaveDedicatedOwners()
        {
            BindingFlags flags =
                BindingFlags.Static |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly;
            string[] projectionMethods =
            [
                "GetDictionaryEntryValueFacts",
                "IsKeyValuePairValueProperty"
            ];
            const string collectionMethod =
                "IsReadOnlyDictionaryWrapperConstruction";
            const string symbolUsageMethod =
                "TryGetPrecedingSimpleLocalAssignment";
            string[] analyzerMethods = typeof(ExceptionFlowAnalyzer)
                .GetMethods(flags)
                .Select(static method => method.Name)
                .ToArray();
            string[] projectorMethods =
                typeof(ExceptionFlowCallContextFactProjector)
                    .GetMethods(flags)
                    .Select(static method => method.Name)
                    .ToArray();
            string[] collectionMethods =
                typeof(ExceptionFlowSequenceCollectionFactsProvider)
                    .GetMethods(flags)
                    .Select(static method => method.Name)
                    .ToArray();
            string[] symbolUsageMethods =
                typeof(ExceptionFlowSymbolUsageFacts)
                    .GetMethods(flags)
                    .Select(static method => method.Name)
                    .ToArray();

            Assert.All(
                projectionMethods,
                method => Assert.Contains(method, projectorMethods));
            Assert.Contains(collectionMethod, collectionMethods);
            Assert.Contains(symbolUsageMethod, symbolUsageMethods);
            Assert.All(
                projectionMethods
                    .Append(collectionMethod)
                    .Append(symbolUsageMethod),
                method => Assert.DoesNotContain(method, analyzerMethods));
        }

        /// <summary>
        /// Ensures sequence-range and dictionary-mutation facts have their
        /// cohesive existing owners without Analyzer forwarding methods.
        /// </summary>
        [Fact]
        public void SequenceRangeDictionaryMutationFacts_HaveDedicatedOwners()
        {
            BindingFlags flags =
                BindingFlags.Static |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly;
            string[] collectionMethods =
            [
                "TryGetDictionaryReceiverFromTryGetValue",
                "IsDictionaryOfListsType",
                "IsListAddRangeSourceArgument"
            ];
            string[] guardMethods =
            [
                "IsUseGuardedBySuccessfulTryGetValue",
                "ConditionRequiresInvocationTrue"
            ];
            string[] preservationMethods =
            [
                "DoesOutSequenceRemainUnchangedBeforeUse",
                "IsSupportedDictionarySequenceProperty"
            ];
            string[] symbolUsageMethods =
            [
                "GetOutArgumentSymbol",
                "AssignmentTargetsDictionaryProperty"
            ];
            string[] analyzerMethods = typeof(ExceptionFlowAnalyzer)
                .GetMethods(flags)
                .Select(static method => method.Name)
                .ToArray();
            string[] collectionOwnerMethods =
                typeof(ExceptionFlowSequenceCollectionFactsProvider)
                    .GetMethods(flags)
                    .Select(static method => method.Name)
                    .ToArray();
            string[] guardOwnerMethods =
                typeof(ExceptionFlowGuardFactsProvider)
                    .GetMethods(flags)
                    .Select(static method => method.Name)
                    .ToArray();
            string[] preservationOwnerMethods =
                typeof(ExceptionFlowSequenceContentPreservationFactsProvider)
                    .GetMethods(flags)
                    .Select(static method => method.Name)
                    .ToArray();
            string[] symbolUsageOwnerMethods =
                typeof(ExceptionFlowSymbolUsageFacts)
                    .GetMethods(flags)
                    .Select(static method => method.Name)
                    .ToArray();

            Assert.All(
                collectionMethods,
                method => Assert.Contains(method, collectionOwnerMethods));
            Assert.All(
                guardMethods,
                method => Assert.Contains(method, guardOwnerMethods));
            Assert.All(
                preservationMethods,
                method => Assert.Contains(method, preservationOwnerMethods));
            Assert.All(
                symbolUsageMethods,
                method => Assert.Contains(method, symbolUsageOwnerMethods));
            Assert.All(
                collectionMethods
                    .Concat(guardMethods)
                    .Concat(preservationMethods)
                    .Concat(symbolUsageMethods),
                method => Assert.DoesNotContain(method, analyzerMethods));
        }

        /// <summary>
        /// Ensures successful sequence-validation facts and the related
        /// content-preservation fact have their dedicated owners without an
        /// Analyzer facade or a component cycle.
        /// </summary>
        [Fact]
        public void SuccessfulSequenceValidationFacts_HaveDedicatedOwners()
        {
            BindingFlags flags =
                BindingFlags.Static |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly;
            string[] validationMethods =
            [
                "IsSequenceSymbolProvenToContainNonNullElementsBySuccessfulHelper",
                "StatementSuccessfulCompletionProvesSequenceElementsNonNull",
                "InvocationSuccessfulCompletionProvesSequenceElementsNonNull",
                "MethodSuccessfulCompletionProvesParameterElementsNonNull",
                "ForeachDirectlyEnumeratesParameter",
                "AllPrecedingReturnsAreVacuousSequenceGuards",
                "IsSingleVoidReturn",
                "ConditionTrueImpliesSequenceHasNoElements",
                "IsSequenceCountComparedEqualToZero",
                "ForeachBodyNecessarilyDereferencesEveryIteration",
                "ForeachBodyCanExitBeforeRemainingElementsAreValidated",
                "BreakTargetsForeach",
                "ContinueTargetsForeach"
            ];
            const string preservationMethod =
                "DoesStatementPreserveSequenceSymbolContents";
            string[] analyzerMethods = typeof(ExceptionFlowAnalyzer)
                .GetMethods(flags)
                .Select(static method => method.Name)
                .ToArray();
            string[] validationOwnerMethods =
                typeof(ExceptionFlowSuccessfulSequenceValidationFactsProvider)
                    .GetMethods(flags)
                    .Select(static method => method.Name)
                    .ToArray();
            string[] preservationOwnerMethods =
                typeof(ExceptionFlowSequenceContentPreservationFactsProvider)
                    .GetMethods(flags)
                    .Select(static method => method.Name)
                    .ToArray();
            string flowDirectory = GetFlowDirectory();
            string preservationSource = File.ReadAllText(
                Path.Combine(
                    flowDirectory,
                    "ExceptionFlowSequenceContentPreservationFactsProvider.cs"));

            Assert.All(
                validationMethods,
                method => Assert.Contains(method, validationOwnerMethods));
            Assert.Contains(
                preservationMethod,
                preservationOwnerMethods);
            Assert.All(
                validationMethods.Append(preservationMethod),
                method => Assert.DoesNotContain(method, analyzerMethods));
            Assert.DoesNotContain(
                nameof(ExceptionFlowSuccessfulSequenceValidationFactsProvider),
                preservationSource,
                StringComparison.Ordinal);
        }

        /// <summary>
        /// Ensures delegate-target resolution has one Analyzer-independent
        /// owner and that every production caller uses it directly.
        /// </summary>
        [Fact]
        public void DelegateTargetResolution_HasDedicatedOwnerAndDirectUsers()
        {
            BindingFlags flags =
                BindingFlags.Static |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly;
            string[] resolverMethods =
            [
                "TryResolveDelegateTarget",
                "UnwrapDelegateExpression",
                "TryResolveStableDelegateLocal",
                "HasDelegateLocalWrites"
            ];
            string[] analyzerMethods = typeof(ExceptionFlowAnalyzer)
                .GetMethods(flags)
                .Select(static method => method.Name)
                .ToArray();
            string[] ownerMethods = typeof(ExceptionFlowDelegateTargetResolver)
                .GetMethods(flags)
                .Select(static method => method.Name)
                .ToArray();
            string flowDirectory = GetFlowDirectory();
            string resolverSource = File.ReadAllText(
                Path.Combine(
                    flowDirectory,
                    "ExceptionFlowDelegateTargetResolver.cs"));
            string[] callerFiles =
            [
                "ExceptionFlowAnalyzer.SummaryGraphCalls.cs",
                "ExceptionFlowAnalyzer.ConditionalWeakTableValueFacts.cs",
                "ExceptionFlowLocalSourceAnalyzer.LocalCallables.cs"
            ];
            string[] dependencyFiles =
            [
                "ExceptionFlowCatchSemantics.cs",
                "ExceptionFlowSemanticScope.cs"
            ];

            Assert.All(
                resolverMethods,
                method => Assert.Contains(method, ownerMethods));
            Assert.Equal(5, ownerMethods.Length);
            Assert.Equal(
                2,
                ownerMethods.Count(
                    static method =>
                        method == "TryResolveDelegateTarget"));
            Assert.Empty(
                typeof(ExceptionFlowDelegateTargetResolver)
                    .GetFields(flags));
            Assert.All(
                resolverMethods,
                method => Assert.DoesNotContain(method, analyzerMethods));
            Assert.DoesNotContain(
                nameof(ExceptionFlowAnalyzer),
                resolverSource,
                StringComparison.Ordinal);
            Assert.DoesNotContain(
                "Func<",
                resolverSource,
                StringComparison.Ordinal);
            Assert.DoesNotContain(
                "Action<",
                resolverSource,
                StringComparison.Ordinal);
            Assert.All(
                callerFiles,
                file => Assert.Contains(
                    nameof(ExceptionFlowDelegateTargetResolver) +
                        ".TryResolveDelegateTarget(",
                    File.ReadAllText(Path.Combine(flowDirectory, file)),
                    StringComparison.Ordinal));
            Assert.All(
                dependencyFiles,
                file => Assert.DoesNotContain(
                    nameof(ExceptionFlowDelegateTargetResolver),
                    File.ReadAllText(Path.Combine(flowDirectory, file)),
                    StringComparison.Ordinal));
        }

        /// <summary>
        /// Ensures every partial declaration of the stateless table-fact
        /// provider remains free of Analyzer back references even though the
        /// cache owner remains in the same source file.
        /// </summary>
        [Fact]
        public void ConditionalWeakTableProvider_DoesNotDependOnAnalyzer()
        {
            string source = File.ReadAllText(
                Path.Combine(
                    GetFlowDirectory(),
                    "ExceptionFlowAnalyzer.ConditionalWeakTableValueFacts.cs"));
            CompilationUnitSyntax root =
                CSharpSyntaxTree.ParseText(source)
                    .GetCompilationUnitRoot();
            ClassDeclarationSyntax[] declarations =
                root.DescendantNodes()
                    .OfType<ClassDeclarationSyntax>()
                    .Where(
                        static declaration =>
                            declaration.Identifier.ValueText ==
                                nameof(ExceptionFlowConditionalWeakTableValueFactsProvider))
                    .ToArray();

            Assert.Equal(3, declarations.Length);
            Assert.All(
                declarations,
                declaration => Assert.DoesNotContain(
                    nameof(ExceptionFlowAnalyzer),
                    declaration.ToFullString(),
                    StringComparison.Ordinal));
        }

        /// <summary>
        /// Ensures dereference discovery owns the one successful-dereference
        /// cache and that the old analyzer owner no longer has a copy.
        /// </summary>
        [Fact]
        public void DereferenceDiscovery_IsSoleSuccessfulDereferenceCacheOwner()
        {
            const string fieldName = "successfulDereferenceCaches";
            BindingFlags flags =
                BindingFlags.Static |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly;

            Assert.NotNull(
                typeof(ExceptionFlowDereferenceFactDiscovery).GetField(
                    fieldName,
                    flags));
            Assert.Null(
                typeof(ExceptionFlowAnalyzer).GetField(
                    fieldName,
                    flags));
        }

        /// <summary>
        /// Ensures the extracted lower layer does not acquire summary,
        /// executable-entry, or historical semantic-environment dependencies.
        /// </summary>
        [Fact]
        public void ExtractedComponents_ExcludeForbiddenDependencies()
        {
            string flowDirectory = GetFlowDirectory();
            string source = string.Join(
                Environment.NewLine,
                Directory.EnumerateFiles(
                        flowDirectory,
                        "ExceptionFlow*Provider.cs",
                        SearchOption.TopDirectoryOnly)
                    .Append(
                        Path.Combine(
                            flowDirectory,
                            "ExceptionFlowStableMemberFacts.cs"))
                    .Append(
                        Path.Combine(
                            flowDirectory,
                            "ExceptionFlowSymbolUsageFacts.cs"))
                    .Append(
                        Path.Combine(
                            flowDirectory,
                            "ExceptionFlowArgumentMapper.cs"))
                    .Append(
                        Path.Combine(
                            flowDirectory,
                            "ExceptionFlowCallContextFactProjector.cs"))
                    .Append(
                        Path.Combine(
                            flowDirectory,
                            "ExceptionFlowDelegateTargetResolver.cs"))
                    .Append(
                        Path.Combine(
                            flowDirectory,
                            "ExceptionFlowRuntimeDispatchClassifier.cs"))
                    .Append(
                        Path.Combine(
                            flowDirectory,
                            "ExceptionFlowDereferenceFactDiscovery.cs"))
                    .Append(
                        Path.Combine(
                            flowDirectory,
                            "ExceptionFlowDereferenceFactDiscovery.Callee.cs"))
                    .Select(File.ReadAllText));

            string[] forbiddenNames =
            [
                "ExceptionFlowSummaryGraphEvaluator",
                "ProjectClosureSemanticContext",
                "SemanticCompilationScope",
                "SupportingSourceSymbolResolver",
                "CrossCompilationSymbolResolver",
                "Program.Main"
            ];

            Assert.All(
                forbiddenNames,
                name => Assert.DoesNotContain(
                    name,
                    source,
                    StringComparison.Ordinal));
        }

        /// <summary>
        /// Ensures compilation-local semantic-model resolution cannot acquire
        /// runtime dispatch or implementation interfaces.
        /// </summary>
        [Fact]
        public void SemanticModelResolution_IsConcreteAndNonvirtual()
        {
            BindingFlags flags =
                BindingFlags.Static |
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly;
            MethodInfo[] resolutionMethods =
                typeof(ExceptionFlowSemanticScope)
                    .GetMethods(flags)
                    .Where(
                        static method =>
                            method.Name is
                                "TryGetSemanticModel" or
                                "GetSemanticModelForSyntaxTree")
                    .ToArray();

            Assert.Equal(3, resolutionMethods.Length);
            Assert.All(
                resolutionMethods,
                method => Assert.False(method.IsVirtual));
            Assert.Empty(typeof(ExceptionFlowSemanticScope).GetInterfaces());
        }

        /// <summary>
        /// Locates the exception-flow source directory from the test process.
        /// </summary>
        /// <returns>The absolute exception-flow source directory.</returns>
        private static string GetFlowDirectory()
        {
            string[] startingDirectories =
            [
                Directory.GetCurrentDirectory(),
                AppContext.BaseDirectory
            ];

            foreach (string startingDirectory in startingDirectories)
            {
                DirectoryInfo? directory = new(startingDirectory);

                while (directory != null)
                {
                    string solutionPath = Path.Combine(
                        directory.FullName,
                        "XMLDocNormalizer.sln");

                    if (File.Exists(solutionPath))
                    {
                        return Path.Combine(
                            directory.FullName,
                            "src",
                            "XMLDocNormalizer",
                            "Checks",
                            "Infrastructure",
                            "Exception",
                            "Flow");
                    }

                    directory = directory.Parent;
                }
            }

            throw new InvalidOperationException(
                "Could not locate XMLDocNormalizer.sln from the current test execution directories.");
        }
    }
}
