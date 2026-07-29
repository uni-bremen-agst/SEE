using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests implicit synchronous and asynchronous disposal calls represented
    /// by using statements and using declarations.
    /// </summary>
    public sealed class ExceptionFlowSummaryGraphDisposalTests
    {
        /// <summary>
        /// Ensures that declaration-form using statements create a
        /// synchronous disposal edge.
        /// </summary>
        [Fact]
        public void UsingStatementDeclaration_CreatesDisposeEdge()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(Resource resource)
                    {
                        using (Resource alias = resource)
                        {
                        }
                    }
                }

                public sealed class Resource : IDisposable
                {
                    public void Dispose()
                    {
                        throw new InvalidOperationException();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge disposalEdge =
                Assert.Single(
                    GetDisposalEdges(run));

            Assert.Equal(
                ExceptionFlowPathStepKind.DisposeCall,
                disposalEdge.CallSiteStep.Kind);

            Assert.Equal(
                "Dispose",
                disposalEdge.Target.Symbol.Name);

            ExceptionFlowSummary disposalSummary =
                run.GetRequiredSummary(
                    disposalEdge.Target);

            Assert.Equal(
                "InvalidOperationException",
                Assert.Single(
                    disposalSummary.Sources)
                    .ExceptionType
                    .Name);
        }

        /// <summary>
        /// Ensures that expression-form using statements create a
        /// synchronous disposal edge.
        /// </summary>
        [Fact]
        public void UsingStatementExpression_CreatesDisposeEdge()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(Resource resource)
                    {
                        using (resource)
                        {
                        }
                    }
                }

                public sealed class Resource : IDisposable
                {
                    public void Dispose()
                    {
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            Assert.Equal(
                ExceptionFlowPathStepKind.DisposeCall,
                Assert.Single(
                    GetDisposalEdges(run))
                    .CallSiteStep
                    .Kind);
        }

        /// <summary>
        /// Ensures that using declarations create a synchronous disposal
        /// edge at the end of their containing scope.
        /// </summary>
        [Fact]
        public void UsingDeclaration_CreatesDisposeEdge()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(Resource resource)
                    {
                        using Resource alias = resource;
                    }
                }

                public sealed class Resource : IDisposable
                {
                    public void Dispose()
                    {
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            Assert.Equal(
                ExceptionFlowPathStepKind.DisposeCall,
                Assert.Single(
                    GetDisposalEdges(run))
                    .CallSiteStep
                    .Kind);
        }

        /// <summary>
        /// Ensures that resources from successive using declarations are
        /// recorded in reverse disposal order.
        /// </summary>
        [Fact]
        public void MultipleUsingDeclarations_AreRecordedInReverseOrder()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(
                        First first,
                        Second second)
                    {
                        using First firstAlias = first;
                        using Second secondAlias = second;
                    }
                }

                public sealed class First : IDisposable
                {
                    public void Dispose()
                    {
                    }
                }

                public sealed class Second : IDisposable
                {
                    public void Dispose()
                    {
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            string[] containingTypeNames =
                GetDisposalEdges(run)
                    .Select(
                        edge =>
                            edge.Target.Symbol.ContainingType?.Name ??
                            string.Empty)
                    .ToArray();

            Assert.Equal(
                new[]
                {
                    "Second",
                    "First"
                },
                containingTypeNames);
        }

        /// <summary>
        /// Ensures that variables declared together in one using statement
        /// are recorded in reverse declaration order.
        /// </summary>
        [Fact]
        public void MultipleResources_AreRecordedInReverseOrder()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(
                        Resource first,
                        Resource second)
                    {
                        using (
                            Resource firstAlias = first,
                                     secondAlias = second)
                        {
                        }
                    }
                }

                public sealed class Resource : IDisposable
                {
                    public void Dispose()
                    {
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge[] disposalEdges =
                GetDisposalEdges(run);

            Assert.Equal(
                2,
                disposalEdges.Length);

            int firstRecordedLine =
                Assert.IsType<int>(
                    disposalEdges[0].CallSiteStep.Line);

            int secondRecordedLine =
                Assert.IsType<int>(
                    disposalEdges[1].CallSiteStep.Line);

            Assert.True(
                firstRecordedLine > secondRecordedLine);
        }

        /// <summary>
        /// Ensures that nested using statements are recorded from the
        /// innermost resource to the outermost resource.
        /// </summary>
        [Fact]
        public void NestedUsingStatements_AreRecordedInsideOut()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(
                        First first,
                        Second second)
                    {
                        using (First firstAlias = first)
                        {
                            using (Second secondAlias = second)
                            {
                            }
                        }
                    }
                }

                public sealed class First : IDisposable
                {
                    public void Dispose()
                    {
                    }
                }

                public sealed class Second : IDisposable
                {
                    public void Dispose()
                    {
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            string[] containingTypeNames =
                GetDisposalEdges(run)
                    .Select(
                        edge =>
                            edge.Target.Symbol.ContainingType?.Name ??
                            string.Empty)
                    .ToArray();

            Assert.Equal(
                new[]
                {
                    "Second",
                    "First"
                },
                containingTypeNames);
        }

        /// <summary>
        /// Ensures that a resource proven to be null does not create a
        /// disposal edge.
        /// </summary>
        [Fact]
        public void KnownNullResource_DoesNotCreateDisposalEdge()
        {
            const string source =
                """
                #nullable enable
                using System;

                public static class EntryPoint
                {
                    public static void M()
                    {
                        using (Resource? resource = null)
                        {
                        }
                    }
                }

                public sealed class Resource : IDisposable
                {
                    public void Dispose()
                    {
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            Assert.Empty(
                GetDisposalEdges(run));
        }

        /// <summary>
        /// Ensures that a null value hidden behind a built-in reference
        /// conversion does not create a disposal edge.
        /// </summary>
        [Fact]
        public void ConvertedNullResource_DoesNotCreateDisposalEdge()
        {
            const string source =
                """
                #nullable enable
                using System;

                public static class EntryPoint
                {
                    public static void M()
                    {
                        using (null as Resource)
                        {
                        }
                    }
                }

                public sealed class Resource : IDisposable
                {
                    public void Dispose()
                    {
                        throw new InvalidOperationException();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            Assert.Empty(
                GetDisposalEdges(run));
        }

        /// <summary>
        /// Ensures that a default reference-type resource does not create a
        /// disposal edge.
        /// </summary>
        [Fact]
        public void DefaultReferenceResource_DoesNotCreateDisposalEdge()
        {
            const string source =
                """
                #nullable enable
                using System;

                public static class EntryPoint
                {
                    public static void M()
                    {
                        using (default(Resource))
                        {
                        }
                    }
                }

                public sealed class Resource : IDisposable
                {
                    public void Dispose()
                    {
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            Assert.Empty(
                GetDisposalEdges(run));
        }

        /// <summary>
        /// Ensures that a nullable disposable value with an unknown value
        /// creates a possible disposal edge targeting the underlying type.
        /// </summary>
        [Fact]
        public void NullableValueResource_UsesUnderlyingDisposeMethod()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(Resource? resource)
                    {
                        using (resource)
                        {
                        }
                    }
                }

                public struct Resource : IDisposable
                {
                    public void Dispose()
                    {
                        throw new InvalidOperationException();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge disposalEdge =
                Assert.Single(
                    GetDisposalEdges(run));

            Assert.Equal(
                "Resource",
                disposalEdge.Target.Symbol.ContainingType?.Name);

            Assert.Equal(
                "InvalidOperationException",
                Assert.Single(
                    run.GetRequiredSummary(
                            disposalEdge.Target)
                        .Sources)
                    .ExceptionType
                    .Name);
        }

        /// <summary>
        /// Ensures that a nullable disposable value proven to have no value
        /// does not create a disposal edge.
        /// </summary>
        [Fact]
        public void DefaultNullableValueResource_DoesNotCreateDisposalEdge()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M()
                    {
                        using (Resource? resource = default(Resource?))
                        {
                        }
                    }
                }

                public struct Resource : IDisposable
                {
                    public void Dispose()
                    {
                        throw new InvalidOperationException();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            Assert.Empty(
                GetDisposalEdges(run));
        }

        /// <summary>
        /// Ensures that a maybe-null resource still creates a disposal edge
        /// because the non-null runtime path remains possible.
        /// </summary>
        [Fact]
        public void MaybeNullResource_StillCreatesDisposalEdge()
        {
            const string source =
                """
                #nullable enable
                using System;

                public static class EntryPoint
                {
                    public static void M(Resource? resource)
                    {
                        using (resource)
                        {
                        }
                    }
                }

                public sealed class Resource : IDisposable
                {
                    public void Dispose()
                    {
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            Assert.Single(
                GetDisposalEdges(run));
        }

        /// <summary>
        /// Ensures that a using declaration remains associated with disposal
        /// when control leaves its scope through a return statement.
        /// </summary>
        [Fact]
        public void UsingDeclarationBeforeReturn_StillCreatesDisposalEdge()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static int M(Resource resource)
                    {
                        using Resource alias = resource;
                        return 1;
                    }
                }

                public sealed class Resource : IDisposable
                {
                    public void Dispose()
                    {
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            Assert.Single(
                GetDisposalEdges(run));
        }

        /// <summary>
        /// Ensures that the accessible synchronous dispose pattern of a ref
        /// struct creates a disposal edge without requiring
        /// <see cref="IDisposable"/>.
        /// </summary>
        [Fact]
        public void RefStructDisposePattern_CreatesDisposeEdge()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(Resource resource)
                    {
                        using (resource)
                        {
                        }
                    }
                }

                public ref struct Resource
                {
                    internal void Dispose()
                    {
                        throw new InvalidOperationException();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge disposalEdge =
                Assert.Single(
                    GetDisposalEdges(run));

            Assert.Equal(
                "Resource",
                disposalEdge.Target.Symbol.ContainingType?.Name);

            Assert.Equal(
                "InvalidOperationException",
                Assert.Single(
                    run.GetRequiredSummary(
                            disposalEdge.Target)
                        .Sources)
                    .ExceptionType
                    .Name);
        }

        /// <summary>
        /// Ensures that a ref-struct disposal pattern may use optional and
        /// <c>params</c> parameters while remaining callable without explicit
        /// arguments.
        /// </summary>
        [Fact]
        public void RefStructDisposePatternWithOptionalParameters_CreatesEdge()
        {
            const string source =
                """
        #nullable enable
        using System;

        public static class EntryPoint
        {
            public static void M(Resource resource)
            {
                using (resource)
                {
                }
            }
        }

        public ref struct Resource
        {
            internal void Dispose(
                string? value = "known",
                params object[] arguments)
            {
                ArgumentNullException.ThrowIfNull(value);
                ArgumentNullException.ThrowIfNull(arguments);
            }
        }
        """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge disposalEdge =
                Assert.Single(
                    GetDisposalEdges(run));

            IMethodSymbol disposalMethod =
                Assert.IsAssignableFrom<IMethodSymbol>(
                    disposalEdge.Target.Symbol);

            Assert.Equal(
                2,
                disposalMethod.Parameters.Length);

            Assert.True(
                disposalMethod.Parameters[0].HasExplicitDefaultValue);

            Assert.Equal(
                "known",
                disposalMethod.Parameters[0].ExplicitDefaultValue);

            Assert.True(
                disposalMethod.Parameters[1].IsParams);

            ExceptionFlowSummary disposalSummary =
                run.GetRequiredSummary(
                    disposalEdge.Target);

            Assert.Empty(
                disposalSummary.Sources);
        }

        /// <summary>
        /// Ensures that an explicit <see cref="IDisposable"/>
        /// implementation is resolved to its source method.
        /// </summary>
        [Fact]
        public void ExplicitIDisposableImplementation_IsResolved()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(Resource resource)
                    {
                        using (resource)
                        {
                        }
                    }
                }

                public sealed class Resource : IDisposable
                {
                    void IDisposable.Dispose()
                    {
                        throw new InvalidOperationException();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge disposalEdge =
                Assert.Single(
                    GetDisposalEdges(run));

            IMethodSymbol disposalMethod =
                Assert.IsAssignableFrom<IMethodSymbol>(
                    disposalEdge.Target.Symbol);

            Assert.Single(
                disposalMethod.ExplicitInterfaceImplementations);

            Assert.Equal(
                "InvalidOperationException",
                Assert.Single(
                    run.GetRequiredSummary(
                            disposalEdge.Target)
                        .Sources)
                    .ExceptionType
                    .Name);
        }

        /// <summary>
        /// Ensures that an interface-typed resource retains the interface
        /// disposal target until dispatch expansion is implemented.
        /// </summary>
        [Fact]
        public void InterfaceTypedResource_RetainsInterfaceTarget()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(IDisposable resource)
                    {
                        using (resource)
                        {
                        }
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            IMethodSymbol disposalMethod =
                Assert.IsAssignableFrom<IMethodSymbol>(
                    Assert.Single(
                        GetDisposalEdges(run))
                        .Target
                        .Symbol);

            Assert.Equal(
                "IDisposable",
                disposalMethod.ContainingType.Name);
        }

        /// <summary>
        /// Ensures that a type parameter constrained to
        /// <see cref="IDisposable"/> retains the interface target until
        /// runtime dispatch is expanded.
        /// </summary>
        [Fact]
        public void IDisposableTypeParameter_RetainsInterfaceTarget()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M<TResource>(TResource resource)
                        where TResource : IDisposable
                    {
                        using (resource)
                        {
                        }
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            IMethodSymbol disposalMethod =
                Assert.IsAssignableFrom<IMethodSymbol>(
                    Assert.Single(
                        GetDisposalEdges(run))
                        .Target
                        .Symbol);

            Assert.Equal(
                "IDisposable",
                disposalMethod.ContainingType.Name);
        }

        /// <summary>
        /// Ensures that await-using statements create asynchronous disposal
        /// edges.
        /// </summary>
        [Fact]
        public void AwaitUsingStatement_CreatesDisposeAsyncEdge()
        {
            const string source =
                """
                using System;
                using System.Threading.Tasks;

                public static class EntryPoint
                {
                    public static async Task M(Resource resource)
                    {
                        await using (Resource alias = resource)
                        {
                        }
                    }
                }

                public sealed class Resource : IAsyncDisposable
                {
                    public ValueTask DisposeAsync()
                    {
                        throw new InvalidOperationException();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge disposalEdge =
                Assert.Single(
                    GetDisposalEdges(run));

            Assert.Equal(
                ExceptionFlowPathStepKind.DisposeAsyncCall,
                disposalEdge.CallSiteStep.Kind);

            Assert.Equal(
                "InvalidOperationException",
                Assert.Single(
                    run.GetRequiredSummary(
                            disposalEdge.Target)
                        .Sources)
                    .ExceptionType
                    .Name);
        }

        /// <summary>
        /// Ensures that await-using declarations create asynchronous disposal
        /// edges.
        /// </summary>
        [Fact]
        public void AwaitUsingDeclaration_CreatesDisposeAsyncEdge()
        {
            const string source =
                """
                using System;
                using System.Threading.Tasks;

                public static class EntryPoint
                {
                    public static async Task M(Resource resource)
                    {
                        await using Resource alias = resource;
                    }
                }

                public sealed class Resource : IAsyncDisposable
                {
                    public ValueTask DisposeAsync()
                    {
                        return ValueTask.CompletedTask;
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            Assert.Equal(
                ExceptionFlowPathStepKind.DisposeAsyncCall,
                Assert.Single(
                    GetDisposalEdges(run))
                    .CallSiteStep
                    .Kind);
        }

        /// <summary>
        /// Ensures that a maybe-null asynchronous reference resource still
        /// creates a disposal edge because its non-null runtime path remains
        /// possible.
        /// </summary>
        [Fact]
        public void MaybeNullAsyncReferenceResource_StillCreatesDisposeAsyncEdge()
        {
            const string source =
                """
        #nullable enable
        using System;
        using System.Threading.Tasks;

        public static class EntryPoint
        {
            public static async Task M(
                Resource? resource)
            {
                await using (resource)
                {
                }
            }
        }

        public sealed class Resource : IAsyncDisposable
        {
            public ValueTask DisposeAsync()
            {
                throw new InvalidOperationException();
            }
        }
        """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge disposalEdge =
                Assert.Single(
                    GetDisposalEdges(run));

            Assert.Equal(
                ExceptionFlowPathStepKind.DisposeAsyncCall,
                disposalEdge.CallSiteStep.Kind);

            Assert.Equal(
                "Resource",
                disposalEdge.Target.Symbol.ContainingType?.Name);

            ExceptionFlowSummary disposalSummary =
                run.GetRequiredSummary(
                    disposalEdge.Target);

            Assert.Equal(
                "InvalidOperationException",
                Assert.Single(
                    disposalSummary.Sources)
                    .ExceptionType
                    .Name);
        }

        /// <summary>
        /// Ensures that a suitable pattern method enables asynchronous
        /// disposal without implementing <see cref="IAsyncDisposable"/>.
        /// </summary>
        [Fact]
        public void AsyncDisposePatternWithoutInterface_CreatesEdge()
        {
            const string source =
                """
                using System;
                using System.Threading.Tasks;

                public static class EntryPoint
                {
                    public static async Task M(Resource resource)
                    {
                        await using (resource)
                        {
                        }
                    }
                }

                public sealed class Resource
                {
                    public ValueTask DisposeAsync()
                    {
                        throw new InvalidOperationException();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge disposalEdge =
                Assert.Single(
                    GetDisposalEdges(run));

            Assert.Equal(
                "Resource",
                disposalEdge.Target.Symbol.ContainingType?.Name);

            Assert.Equal(
                "InvalidOperationException",
                Assert.Single(
                    run.GetRequiredSummary(
                            disposalEdge.Target)
                        .Sources)
                    .ExceptionType
                    .Name);
        }

        /// <summary>
        /// Ensures that the current compiler preference for a directly
        /// callable pattern method is retained when the type also has an
        /// explicit <see cref="IAsyncDisposable"/> implementation.
        /// </summary>
        [Fact]
        public void AsyncDisposePattern_IsPreferredOverExplicitInterfaceMethod()
        {
            const string source =
                """
                using System;
                using System.Threading.Tasks;

                public static class EntryPoint
                {
                    public static async Task M(Resource resource)
                    {
                        await using (resource)
                        {
                        }
                    }
                }

                public sealed class Resource : IAsyncDisposable
                {
                    public ValueTask DisposeAsync()
                    {
                        throw new InvalidOperationException();
                    }

                    ValueTask IAsyncDisposable.DisposeAsync()
                    {
                        throw new ArgumentException();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge disposalEdge =
                Assert.Single(
                    GetDisposalEdges(run));

            IMethodSymbol disposalMethod =
                Assert.IsAssignableFrom<IMethodSymbol>(
                    disposalEdge.Target.Symbol);

            Assert.Empty(
                disposalMethod.ExplicitInterfaceImplementations);

            Assert.Equal(
                "InvalidOperationException",
                Assert.Single(
                    run.GetRequiredSummary(
                            disposalEdge.Target)
                        .Sources)
                    .ExceptionType
                    .Name);
        }

        /// <summary>
        /// Ensures that an asynchronous disposal pattern may use optional and
        /// <c>params</c> parameters while remaining callable without explicit
        /// arguments.
        /// </summary>
        [Fact]
        public void AsyncDisposePatternWithOptionalParameters_CreatesEdge()
        {
            const string source =
                """
                using System;
                using System.Threading.Tasks;

                public static class EntryPoint
                {
                    public static async Task M(Resource resource)
                    {
                        await using (resource)
                        {
                        }
                    }
                }

                public sealed class Resource
                {
                    public ValueTask DisposeAsync(
                        string? value = "known",
                        params object[] arguments)
                    {
                        ArgumentNullException.ThrowIfNull(value);
                        ArgumentNullException.ThrowIfNull(arguments);
                        return ValueTask.CompletedTask;
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            IMethodSymbol disposalMethod =
                Assert.IsAssignableFrom<IMethodSymbol>(
                    Assert.Single(
                        GetDisposalEdges(run))
                        .Target
                        .Symbol);

            Assert.Equal(
                2,
                disposalMethod.Parameters.Length);

            ExceptionFlowSummaryCallEdge disposalEdge =
                Assert.Single(
                    GetDisposalEdges(run));

            Assert.Empty(
                run.GetRequiredSummary(
                        disposalEdge.Target)
                    .Sources);
        }

        /// <summary>
        /// Ensures that an explicit <see cref="IAsyncDisposable"/>
        /// implementation is resolved to its source method.
        /// </summary>
        [Fact]
        public void ExplicitIAsyncDisposableImplementation_IsResolved()
        {
            const string source =
                """
                using System;
                using System.Threading.Tasks;

                public static class EntryPoint
                {
                    public static async Task M(Resource resource)
                    {
                        await using (resource)
                        {
                        }
                    }
                }

                public sealed class Resource : IAsyncDisposable
                {
                    ValueTask IAsyncDisposable.DisposeAsync()
                    {
                        throw new InvalidOperationException();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge disposalEdge =
                Assert.Single(
                    GetDisposalEdges(run));

            IMethodSymbol disposalMethod =
                Assert.IsAssignableFrom<IMethodSymbol>(
                    disposalEdge.Target.Symbol);

            Assert.Single(
                disposalMethod.ExplicitInterfaceImplementations);

            Assert.Equal(
                "InvalidOperationException",
                Assert.Single(
                    run.GetRequiredSummary(
                            disposalEdge.Target)
                        .Sources)
                    .ExceptionType
                    .Name);
        }

        /// <summary>
        /// Ensures that an asynchronously disposable interface resource
        /// retains its interface target until dispatch expansion.
        /// </summary>
        [Fact]
        public void AsyncInterfaceTypedResource_RetainsInterfaceTarget()
        {
            const string source =
                """
                using System;
                using System.Threading.Tasks;

                public static class EntryPoint
                {
                    public static async Task M(IAsyncDisposable resource)
                    {
                        await using (resource)
                        {
                        }
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            IMethodSymbol disposalMethod =
                Assert.IsAssignableFrom<IMethodSymbol>(
                    Assert.Single(
                        GetDisposalEdges(run))
                        .Target
                        .Symbol);

            Assert.Equal(
                "IAsyncDisposable",
                disposalMethod.ContainingType.Name);
        }

        /// <summary>
        /// Ensures that a type parameter constrained to
        /// <see cref="IAsyncDisposable"/> retains the interface target until
        /// runtime dispatch is expanded.
        /// </summary>
        [Fact]
        public void IAsyncDisposableTypeParameter_RetainsInterfaceTarget()
        {
            const string source =
                """
                using System;
                using System.Threading.Tasks;

                public static class EntryPoint
                {
                    public static async Task M<TResource>(TResource resource)
                        where TResource : IAsyncDisposable
                    {
                        await using (resource)
                        {
                        }
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            IMethodSymbol disposalMethod =
                Assert.IsAssignableFrom<IMethodSymbol>(
                    Assert.Single(
                        GetDisposalEdges(run))
                        .Target
                        .Symbol);

            Assert.Equal(
                "IAsyncDisposable",
                disposalMethod.ContainingType.Name);
        }

        /// <summary>
        /// Ensures that a type parameter may obtain its asynchronous disposal
        /// pattern from a non-framework interface constraint.
        /// </summary>
        [Fact]
        public void AsyncPatternTypeParameter_ResolvesConstraintMethod()
        {
            const string source =
                """
                using System;
                using System.Threading.Tasks;

                public interface IAsyncResource
                {
                    ValueTask DisposeAsync(int value = 0);
                }

                public static class EntryPoint
                {
                    public static async Task M<TResource>(TResource resource)
                        where TResource : IAsyncResource
                    {
                        await using (resource)
                        {
                        }
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            IMethodSymbol disposalMethod =
                Assert.IsAssignableFrom<IMethodSymbol>(
                    Assert.Single(
                        GetDisposalEdges(run))
                        .Target
                        .Symbol);

            Assert.Equal(
                "IAsyncResource",
                disposalMethod.ContainingType.Name);
        }

        /// <summary>
        /// Ensures that a catch surrounding the complete using construct
        /// filters exceptions produced by disposal.
        /// </summary>
        [Fact]
        public void CatchAroundUsing_FiltersDisposeException()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(Resource resource)
                    {
                        try
                        {
                            using (resource)
                            {
                            }
                        }
                        catch (InvalidOperationException)
                        {
                        }
                    }
                }

                public sealed class Resource : IDisposable
                {
                    public void Dispose()
                    {
                        throw new InvalidOperationException();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge disposalEdge =
                Assert.Single(
                    GetDisposalEdges(run));

            Assert.True(
                disposalEdge.Suppresses(
                    run.GetRequiredType(
                        "System.InvalidOperationException")));
        }

        /// <summary>
        /// Ensures that a catch inside the using body does not filter an
        /// exception produced later by disposal.
        /// </summary>
        [Fact]
        public void CatchInsideUsingBody_DoesNotFilterDisposeException()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(Resource resource)
                    {
                        using (resource)
                        {
                            try
                            {
                                throw new InvalidOperationException();
                            }
                            catch (InvalidOperationException)
                            {
                            }
                        }
                    }
                }

                public sealed class Resource : IDisposable
                {
                    public void Dispose()
                    {
                        throw new InvalidOperationException();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummaryCallEdge disposalEdge =
                Assert.Single(
                    GetDisposalEdges(run));

            Assert.False(
                disposalEdge.Suppresses(
                    run.GetRequiredType(
                        "System.InvalidOperationException")));

            Assert.Empty(
                run.RootSummary.Sources);
        }

        /// <summary>
        /// Ensures that a using construct inside an uncalled lambda remains
        /// outside the containing method summary.
        /// </summary>
        [Fact]
        public void UsingInsideUncalledLambda_IsExcluded()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(Resource resource)
                    {
                        Action action =
                            () =>
                            {
                                using (resource)
                                {
                                }
                            };
                    }
                }

                public sealed class Resource : IDisposable
                {
                    public void Dispose()
                    {
                        throw new InvalidOperationException();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            Assert.Empty(
                GetDisposalEdges(run));

            Assert.Empty(
                run.RootSummary.Sources);
        }

        /// <summary>
        /// Gets all synchronous and asynchronous disposal edges from one root
        /// summary.
        /// </summary>
        /// <param name="run">
        /// The completed summary-graph test run.
        /// </param>
        /// <returns>The disposal edges in recorded execution order.</returns>
        private static ExceptionFlowSummaryCallEdge[] GetDisposalEdges(
            ExceptionFlowSummaryGraphTestRun run)
        {
            return run.RootSummary.CallEdges
                .Where(
                    edge =>
                        edge.CallSiteStep.Kind ==
                            ExceptionFlowPathStepKind.DisposeCall ||
                        edge.CallSiteStep.Kind ==
                            ExceptionFlowPathStepKind.DisposeAsyncCall)
                .ToArray();
        }
    }
}
