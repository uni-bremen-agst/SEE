using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests canonical summary-graph binding from dependency metadata to
    /// registered supporting source declarations.
    /// </summary>
    public sealed class ExceptionFlowSupportingSourceBindingTests
    {
        /// <summary>
        /// Binds an ordinary metadata invocation to its supporting source body.
        /// </summary>
        [Fact]
        public void Invocation_RegisteredSupportingSource_UsesSourceMethodAndBody()
        {
            const string dependencySource =
                "using System; namespace Dependency { public static class Service { " +
                "public static void Execute() { throw new InvalidOperationException(); } } }";
            const string consumerSource =
                "public static class Consumer { public static void M() { Dependency.Service.Execute(); } }";

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphProjectTestHelper.BuildWithSupportingSource(
                    dependencySource, consumerSource, "M");
            ExceptionFlowSummaryCallEdge edge = Assert.Single(
                run.RootSummary.CallEdges,
                callEdge => callEdge.Target.Symbol.ContainingType.Name == "Service");
            IMethodSymbol targetMethod = Assert.IsAssignableFrom<IMethodSymbol>(edge.Target.Symbol);
            ExceptionFlowSummary targetSummary = run.GetRequiredSummary(edge.Target);

            Assert.False(targetMethod.DeclaringSyntaxReferences.IsDefaultOrEmpty);
            Assert.True(targetSummary.HasExecutableBody);
            Assert.Contains(
                targetSummary.Sources,
                source => source.ExceptionType.Name == "InvalidOperationException");
        }

        /// <summary>
        /// Retains the metadata target when the dependency is not registered
        /// as supporting source.
        /// </summary>
        [Fact]
        public void Invocation_UnregisteredSupportingSource_RetainsMetadataTarget()
        {
            const string dependencySource =
                "namespace Dependency { public static class Service { public static void Execute() { } } }";
            const string consumerSource =
                "public static class Consumer { public static void M() { Dependency.Service.Execute(); } }";

            ExceptionFlowSummaryGraphTestRun run = ExceptionFlowSummaryGraphProjectTestHelper.Build(
                dependencySource, consumerSource, "M");
            IMethodSymbol targetMethod = Assert.IsAssignableFrom<IMethodSymbol>(
                Assert.Single(run.RootSummary.CallEdges).Target.Symbol);
            ExceptionFlowSummary targetSummary = run.GetRequiredSummary(
                Assert.Single(run.RootSummary.CallEdges).Target);

            Assert.True(targetMethod.DeclaringSyntaxReferences.IsDefaultOrEmpty);
            Assert.False(targetSummary.HasExecutableBody);
        }

        /// <summary>
        /// Preserves value facts by parameter ordinal while rebinding the
        /// callable from metadata to source.
        /// </summary>
        [Fact]
        public void Invocation_ParameterFacts_AreReboundByOrdinal()
        {
            const string dependencySource =
                "using System; namespace Dependency { public static class Service { " +
                "public static void Execute(string value) { ArgumentNullException.ThrowIfNull(value); } } }";
            const string consumerSource =
                "public static class Consumer { public static void M() { Dependency.Service.Execute(\"known\"); } }";

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphProjectTestHelper.BuildWithSupportingSource(
                    dependencySource, consumerSource, "M");
            ExceptionFlowSummaryCallEdge edge = Assert.Single(
                run.RootSummary.CallEdges,
                callEdge => callEdge.Target.Symbol.ContainingType.Name == "Service");
            IMethodSymbol sourceMethod = Assert.IsAssignableFrom<IMethodSymbol>(edge.Target.Symbol);

            Assert.True(run.Graph.TryGetCallContext(
                edge.Target, out ExceptionFlowCallContext? targetContext));
            Assert.NotNull(targetContext);
            Assert.True(targetContext.IsParameterKnownNonNull(sourceMethod.Parameters[0]));
            Assert.Empty(run.GetRequiredSummary(edge.Target).Sources);
        }

        /// <summary>
        /// Maps a metadata reduced extension invocation to its unreduced
        /// supporting source declaration and transfers the receiver fact.
        /// </summary>
        [Fact]
        public void ReducedExtension_RegisteredSupportingSource_UsesUnreducedSourceAndReceiverFact()
        {
            const string dependencySource =
                "using System; namespace Dependency { public static class Extensions { " +
                "public static void Validate(this object? value) { " +
                "ArgumentNullException.ThrowIfNull(value); } } }";
            const string consumerSource =
                "using Dependency; public static class Consumer { " +
                "public static void M() { new object().Validate(); } }";

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphProjectTestHelper.BuildWithSupportingSource(
                    dependencySource, consumerSource, "M");
            ExceptionFlowSummaryCallEdge edge = Assert.Single(
                run.RootSummary.CallEdges,
                callEdge => callEdge.Target.Symbol.ContainingType.Name == "Extensions");
            IMethodSymbol sourceMethod = Assert.IsAssignableFrom<IMethodSymbol>(edge.Target.Symbol);
            ExceptionFlowSummary sourceSummary = run.GetRequiredSummary(edge.Target);

            Assert.Equal(MethodKind.Ordinary, sourceMethod.MethodKind);
            Assert.True(sourceMethod.IsExtensionMethod);
            Assert.Null(sourceMethod.ReducedFrom);
            Assert.Single(sourceMethod.Parameters);
            Assert.Equal(0, sourceMethod.Parameters[0].Ordinal);
            Assert.True(run.Graph.TryGetCallContext(
                edge.Target, out ExceptionFlowCallContext? targetContext));
            Assert.NotNull(targetContext);
            Assert.True(targetContext.IsParameterKnownNonNull(sourceMethod.Parameters[0]));
            Assert.Empty(sourceSummary.Sources);
            Assert.Equal(3, run.Graph.Count);

            ExceptionFlowSummaryGraphTestRun metadataRun =
                ExceptionFlowSummaryGraphProjectTestHelper.Build(
                    dependencySource, consumerSource, "M");
            ExceptionFlowSummaryCallEdge metadataEdge = Assert.Single(
                metadataRun.RootSummary.CallEdges,
                callEdge => callEdge.Target.Symbol.ContainingType.Name == "Extensions");
            IMethodSymbol metadataMethod = Assert.IsAssignableFrom<IMethodSymbol>(
                metadataEdge.Target.Symbol);

            Assert.Equal(MethodKind.ReducedExtension, metadataMethod.MethodKind);
            Assert.NotNull(metadataMethod.ReducedFrom);
            Assert.True(metadataMethod.DeclaringSyntaxReferences.IsDefaultOrEmpty);
            Assert.False(metadataRun.GetRequiredSummary(metadataEdge.Target).HasExecutableBody);
        }

        /// <summary>
        /// Maps an implicit metadata constructor to its supporting source
        /// symbol and analyzes the containing type's instance initializer.
        /// </summary>
        [Fact]
        public void ImplicitConstructor_RegisteredSupportingSource_AnalyzesInstanceInitializer()
        {
            const string dependencySource =
                "using System; namespace Dependency { public sealed class Value { " +
                "private readonly object value = CreateValue(); " +
                "private static object CreateValue() { throw new InvalidOperationException(); } } }";
            const string consumerSource =
                "public static class Consumer { public static void M() { _ = new Dependency.Value(); } }";

            ExceptionFlowSummaryGraphTestRun sourceRun =
                ExceptionFlowSummaryGraphProjectTestHelper.BuildWithSupportingSource(
                    dependencySource, consumerSource, "M");
            ExceptionFlowSummaryCallEdge constructorEdge = Assert.Single(sourceRun.RootSummary.CallEdges);
            IMethodSymbol sourceConstructor = Assert.IsAssignableFrom<IMethodSymbol>(
                constructorEdge.Target.Symbol);
            ExceptionFlowSummary constructorSummary = sourceRun.GetRequiredSummary(
                constructorEdge.Target);
            ExceptionFlowSummaryCallEdge initializerEdge = Assert.Single(
                constructorSummary.CallEdges,
                callEdge => callEdge.Target.Symbol.Name == "CreateValue");

            Assert.Equal(MethodKind.Constructor, sourceConstructor.MethodKind);
            Assert.True(sourceConstructor.IsImplicitlyDeclared);
            Assert.True(sourceConstructor.DeclaringSyntaxReferences.IsDefaultOrEmpty);
            Assert.True(constructorSummary.HasExecutableBody);
            Assert.Contains(
                sourceRun.GetRequiredSummary(initializerEdge.Target).Sources,
                source => source.ExceptionType.Name == "InvalidOperationException");

            ExceptionFlowSummaryGraphTestRun metadataRun =
                ExceptionFlowSummaryGraphProjectTestHelper.Build(
                    dependencySource, consumerSource, "M");
            ExceptionFlowSummaryCallEdge metadataConstructorEdge = Assert.Single(
                metadataRun.RootSummary.CallEdges);
            IMethodSymbol metadataConstructor = Assert.IsAssignableFrom<IMethodSymbol>(
                metadataConstructorEdge.Target.Symbol);

            Assert.False(metadataConstructor.IsImplicitlyDeclared);
            Assert.True(metadataConstructor.DeclaringSyntaxReferences.IsDefaultOrEmpty);
            Assert.False(metadataRun.GetRequiredSummary(
                metadataConstructorEdge.Target).HasExecutableBody);
        }

        /// <summary>
        /// Uses one canonical source key for a supporting method reached first
        /// through metadata and then recursively through source.
        /// </summary>
        [Fact]
        public void RecursiveInvocation_MetadataAndSourcePaths_DoNotCreateDuplicateNodes()
        {
            const string dependencySource =
                "namespace Dependency { public static class Service { " +
                "public static void Execute(object value) { if (value != null) { Execute(value); } } } }";
            const string consumerSource =
                "public static class Consumer { public static void M() { Dependency.Service.Execute(\"known\"); } }";

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphProjectTestHelper.BuildWithSupportingSource(
                    dependencySource, consumerSource, "M");
            ExceptionFlowSummaryCallEdge entryEdge = Assert.Single(run.RootSummary.CallEdges);
            ExceptionFlowSummary sourceSummary = run.GetRequiredSummary(entryEdge.Target);
            ExceptionFlowSummaryCallEdge recursiveEdge = Assert.Single(sourceSummary.CallEdges);

            Assert.Equal(2, run.Graph.Count);
            Assert.Equal(entryEdge.Target, recursiveEdge.Target);
            Assert.False(entryEdge.Target.Symbol.DeclaringSyntaxReferences.IsDefaultOrEmpty);
        }

        /// <summary>
        /// Canonicalizes constructor, property getter, and operator targets.
        /// </summary>
        /// <param name="methodName">The consumer root method.</param>
        /// <param name="expectedKind">The expected source method kind.</param>
        [Theory]
        [InlineData("Create", MethodKind.Constructor)]
        [InlineData("Read", MethodKind.PropertyGet)]
        [InlineData("Add", MethodKind.UserDefinedOperator)]
        public void CallableKinds_RegisteredSupportingSource_UseSourceMethods(
            string methodName,
            MethodKind expectedKind)
        {
            const string dependencySource =
                "namespace Dependency { public sealed class Value { " +
                "public Value() { } public int Number { get { return 1; } } " +
                "public static Value operator +(Value left, Value right) { return left; } } }";
            const string consumerSource =
                "public static class Consumer { " +
                "public static void Create() { _ = new Dependency.Value(); } " +
                "public static int Read(Dependency.Value value) { return value.Number; } " +
                "public static void Add(Dependency.Value left, Dependency.Value right) { _ = left + right; } }";

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphProjectTestHelper.BuildWithSupportingSource(
                    dependencySource, consumerSource, methodName);
            IMethodSymbol targetMethod = Assert.IsAssignableFrom<IMethodSymbol>(
                Assert.Single(run.RootSummary.CallEdges).Target.Symbol);

            Assert.Equal(expectedKind, targetMethod.MethodKind);
            Assert.False(targetMethod.DeclaringSyntaxReferences.IsDefaultOrEmpty);
            Assert.True(run.GetRequiredSummary(
                Assert.Single(run.RootSummary.CallEdges).Target).HasExecutableBody);
        }

        /// <summary>
        /// Continues canonical source binding when one registered supporting
        /// method calls another registered supporting method.
        /// </summary>
        [Fact]
        public void SupportingSourceMethodCallingSupportingSourceMethod_UsesSourceTargets()
        {
            const string targetDependencySource =
                "using System; namespace TargetDependency { public static class Second { " +
                "public static void Execute() { throw new InvalidOperationException(); } } }";
            const string callingDependencySource =
                "namespace CallingDependency { public static class First { " +
                "public static void Execute() { TargetDependency.Second.Execute(); } } }";
            const string consumerSource =
                "public static class Consumer { public static void M() { " +
                "CallingDependency.First.Execute(); } }";

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphProjectTestHelper.BuildWithSupportingSourceChain(
                    targetDependencySource, callingDependencySource, consumerSource, "M");
            ExceptionFlowSummaryCallEdge firstEdge = Assert.Single(run.RootSummary.CallEdges);
            ExceptionFlowSummaryCallEdge secondEdge = Assert.Single(
                run.GetRequiredSummary(firstEdge.Target).CallEdges);

            Assert.False(firstEdge.Target.Symbol.DeclaringSyntaxReferences.IsDefaultOrEmpty);
            Assert.False(secondEdge.Target.Symbol.DeclaringSyntaxReferences.IsDefaultOrEmpty);
            Assert.Equal("Second", secondEdge.Target.Symbol.ContainingType.Name);
            Assert.Equal(3, run.Graph.Count);
            Assert.Contains(
                run.GetRequiredSummary(secondEdge.Target).Sources,
                source => source.ExceptionType.Name == "InvalidOperationException");
        }
    }
}
