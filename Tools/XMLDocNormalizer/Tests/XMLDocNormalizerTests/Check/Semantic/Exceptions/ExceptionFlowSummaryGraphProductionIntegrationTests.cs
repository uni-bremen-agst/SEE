using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Checks;
using XMLDocNormalizer.Configuration;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests productive use of summary-graph results by
    /// solution-transitive semantic exception detection.
    /// </summary>
    [Collection(SemanticExceptionTestCollection.Name)]
    public sealed class
        ExceptionFlowSummaryGraphProductionIntegrationTests
    {
        /// <summary>
        /// Ensures that solution-transitive mode reports an exception from a
        /// known virtual override while project-transitive mode retains its
        /// existing statically selected behavior.
        /// </summary>
        [Fact]
        public void SolutionTransitive_UsesVirtualDispatchTargets()
        {
            const string source =
                """
                using System;

                public class BaseService
                {
                    public virtual void Execute()
                    {
                    }
                }

                public sealed class DerivedService : BaseService
                {
                    public override void Execute()
                    {
                        throw new InvalidOperationException();
                    }
                }

                public static class EntryPoint
                {
                    /// <summary>Executes the service.</summary>
                    public static void M(BaseService service)
                    {
                        service.Execute();
                    }
                }
                """;

            List<Finding> projectFindings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.ProjectTransitive);

            Assert.DoesNotContain(
                projectFindings,
                finding =>
                    finding.Smell.ID ==
                    XmlDocSmells
                        .MissingTransitiveExceptionDocumentation
                        .ID);

            List<Finding> solutionFindings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.SolutionTransitive);

            Finding finding =
                Assert.Single(
                    solutionFindings.Where(
                        candidate =>
                            candidate.Smell.ID ==
                            XmlDocSmells
                                .MissingTransitiveExceptionDocumentation
                                .ID));

            Assert.Contains(
                "System.InvalidOperationException",
                finding.Message,
                StringComparison.Ordinal);
        }

        /// <summary>
        /// Ensures that incomplete public interface dispatch produces DOC631
        /// instead of an incorrect DOC632 conclusion.
        /// </summary>
        [Fact]
        public void OpenInterfaceDispatch_ProducesDoc631()
        {
            const string source =
                """
                using System;

                public interface IService
                {
                    void Execute();
                }

                public sealed class Service : IService
                {
                    public void Execute()
                    {
                    }
                }

                public static class EntryPoint
                {
                    /// <summary>Executes the service.</summary>
                    /// <exception cref="InvalidOperationException">
                    /// Thrown by an external implementation.
                    /// </exception>
                    public static void M(IService service)
                    {
                        service.Execute();
                    }
                }
                """;

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.SolutionTransitive);

            Finding finding =
                Assert.Single(
                    findings.Where(
                        candidate =>
                            candidate.Smell.ID ==
                            XmlDocSmells
                                .ExceptionFlowNotDecidable
                                .ID));

            Assert.Equal(
                "exception",
                finding.TagName);

            Assert.DoesNotContain(
                findings,
                candidate =>
                    candidate.Smell.ID ==
                    XmlDocSmells
                        .ExceptionTagWithoutTransitiveThrow
                        .ID);
        }

        /// <summary>
        /// Ensures that an exact object-creation receiver excludes base and
        /// more-derived override exceptions from productive findings.
        /// </summary>
        [Fact]
        public void ExactReceiver_ReportsOnlyExactRuntimeTarget()
        {
            const string source =
                """
                using System;

                public class BaseService
                {
                    public virtual void Execute()
                    {
                        throw new ArgumentException();
                    }
                }

                public class DerivedService : BaseService
                {
                    public override void Execute()
                    {
                        throw new InvalidOperationException();
                    }
                }

                public sealed class MoreDerivedService : DerivedService
                {
                    public override void Execute()
                    {
                        throw new FormatException();
                    }
                }

                public static class EntryPoint
                {
                    /// <summary>Executes one exact service.</summary>
                    public static void M()
                    {
                        new DerivedService().Execute();
                    }
                }
                """;

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.SolutionTransitive);

            Finding finding =
                Assert.Single(
                    findings.Where(
                        candidate =>
                            candidate.Smell.ID ==
                            XmlDocSmells
                                .MissingTransitiveExceptionDocumentation
                                .ID));

            Assert.Contains(
                "System.InvalidOperationException",
                finding.Message,
                StringComparison.Ordinal);

            Assert.DoesNotContain(
                findings,
                candidate =>
                    candidate.Message.Contains(
                        "System.ArgumentException",
                        StringComparison.Ordinal));

            Assert.DoesNotContain(
                findings,
                candidate =>
                    candidate.Message.Contains(
                        "System.FormatException",
                        StringComparison.Ordinal));
        }

        /// <summary>
        /// Ensures that a typed catch around virtual dispatch suppresses the
        /// matching override exception in productive analysis.
        /// </summary>
        [Fact]
        public void TypedCatch_SuppressesVirtualOverrideException()
        {
            const string source =
                """
                using System;

                public class BaseService
                {
                    public virtual void Execute()
                    {
                    }
                }

                public sealed class DerivedService : BaseService
                {
                    public override void Execute()
                    {
                        throw new InvalidOperationException();
                    }
                }

                public static class EntryPoint
                {
                    /// <summary>Executes the service.</summary>
                    public static void M(BaseService service)
                    {
                        try
                        {
                            service.Execute();
                        }
                        catch (InvalidOperationException)
                        {
                        }
                    }
                }
                """;

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.SolutionTransitive);

            Assert.DoesNotContain(
                findings,
                finding =>
                    finding.Message.Contains(
                        "System.InvalidOperationException",
                        StringComparison.Ordinal));
        }

        /// <summary>
        /// Ensures that multiple documented roots analyzed through one shared
        /// detector session retain independent productive results.
        /// </summary>
        [Fact]
        public void MultipleDocumentedRoots_ProduceIndependentFindings()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    /// <summary>Runs the first operation.</summary>
                    public static void First()
                    {
                        ThrowArgument();
                    }

                    /// <summary>Runs the second operation.</summary>
                    public static void Second()
                    {
                        ThrowInvalidOperation();
                    }

                    private static void ThrowArgument()
                    {
                        throw new ArgumentException();
                    }

                    private static void ThrowInvalidOperation()
                    {
                        throw new InvalidOperationException();
                    }
                }
                """;

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.SolutionTransitive);

            Finding[] transitiveFindings =
                findings.Where(
                        finding =>
                            finding.Smell.ID ==
                            XmlDocSmells
                                .MissingTransitiveExceptionDocumentation
                                .ID)
                    .ToArray();

            Assert.Equal(
                2,
                transitiveFindings.Length);

            Assert.Contains(
                transitiveFindings,
                finding =>
                    finding.Message.Contains(
                        "System.ArgumentException",
                        StringComparison.Ordinal));

            Assert.Contains(
                transitiveFindings,
                finding =>
                    finding.Message.Contains(
                        "System.InvalidOperationException",
                        StringComparison.Ordinal));
        }

        /// <summary>
        /// Ensures that a custom exception originating in another analyzed
        /// project is normalized into the reporting compilation and covers its
        /// documentation tag.
        /// </summary>
        [Fact]
        public async Task CrossProjectCustomException_IsNormalized()
        {
            const string dependencySource =
                """
                using System;

                namespace Dependency
                {
                    public sealed class DependencyException : Exception
                    {
                    }

                    public static class Service
                    {
                        public static void Execute()
                        {
                            throw new DependencyException();
                        }
                    }
                }
                """;

            const string reportingSource =
                """
                using Dependency;

                public static class EntryPoint
                {
                    /// <summary>Executes the dependency service.</summary>
                    /// <exception cref="DependencyException">
                    /// Thrown by the dependency.
                    /// </exception>
                    public static void M()
                    {
                        Service.Execute();
                    }
                }
                """;

            (
                Solution solution,
                Project reportingProject,
                Document reportingDocument) =
                    SolutionTestBuilder.CreateTwoProjectSolution(
                        reportingSource,
                        dependencySource);

            ProjectClosureSemanticContext semanticContext =
                ProjectClosureSemanticContextBuilder.Build(
                    [reportingProject],
                    ExceptionAnalysisMode.SolutionTransitive);

            SyntaxTree tree =
                (await reportingDocument.GetSyntaxTreeAsync())!;

            Compilation compilation =
                (await reportingProject.GetCompilationAsync())!;

            SemanticModel semanticModel =
                compilation.GetSemanticModel(
                    tree);

            XmlDocOptions options =
                new()
                {
                    ExceptionAnalysisMode =
                        ExceptionAnalysisMode.SolutionTransitive
                };

            List<Finding> findings =
                XmlDocExceptionSemanticDetector.FindExceptionSmells(
                    tree,
                    "Reporting.cs",
                    semanticModel,
                    semanticContext,
                    options);

            Assert.DoesNotContain(
                findings,
                finding =>
                    finding.Smell.ID ==
                    XmlDocSmells
                        .MissingTransitiveExceptionDocumentation
                        .ID);

            Assert.DoesNotContain(
                findings,
                finding =>
                    finding.Smell.ID ==
                    XmlDocSmells
                        .ExceptionTagWithoutTransitiveThrow
                        .ID);

            Assert.DoesNotContain(
                findings,
                finding =>
                    finding.Smell.ID ==
                    XmlDocSmells
                        .ExceptionFlowNotDecidable
                        .ID);
        }
    }
}
