using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Execution.Semantic
{
    /// <summary>
    /// Tests semantic compilation roles and supporting source registration.
    /// </summary>
    public sealed class SemanticCompilationScopeTests
    {
        /// <summary>
        /// Verifies that solution-transitive Workspace projects retain their
        /// real identities and receive distinct reporting roles.
        /// </summary>
        [Fact]
        public async Task SolutionTransitiveProjects_AreClassifiedByReportingRole()
        {
            string reportingSource =
                "public sealed class ReportingType\n" +
                "{\n" +
                "}\n";
            string referencedSource =
                "public sealed class ReferencedException : System.Exception\n" +
                "{\n" +
                "}\n";

            (Solution solution, Project reportingProject, Document reportingDocument) =
                SolutionTestBuilder.CreateTwoProjectSolution(reportingSource, referencedSource);
            Project referencedProject = solution.Projects.Single(
                project => project.Id != reportingProject.Id);
            Document referencedDocument = referencedProject.Documents.Single();
            SyntaxTree reportingTree = (await reportingDocument.GetSyntaxTreeAsync())!;
            SyntaxTree referencedTree = (await referencedDocument.GetSyntaxTreeAsync())!;

            ProjectClosureSemanticContext context = ProjectClosureSemanticContextBuilder.Build(
                new[] { reportingProject },
                ExceptionAnalysisMode.SolutionTransitive);

            SemanticCompilationScope reportingScope = context.GetAnalysisCompilationScopes().Single(
                scope => scope.ProjectId == reportingProject.Id);
            SemanticCompilationScope referencedScope = context.GetAnalysisCompilationScopes().Single(
                scope => scope.ProjectId == referencedProject.Id);

            Assert.Equal(SemanticCompilationScopeKind.AnalysisTarget, reportingScope.Kind);
            Assert.Equal(SemanticCompilationScopeKind.ReferencedProject, referencedScope.Kind);
            Assert.True(context.IsInReportingScope(reportingTree));
            Assert.True(context.IsInAnalysisScope(reportingTree));
            Assert.False(context.IsInReportingScope(referencedTree));
            Assert.True(context.IsInAnalysisScope(referencedTree));
            Assert.True(context.TryGetOwningProjectId(reportingTree, out ProjectId reportingProjectId));
            Assert.Equal(reportingProject.Id, reportingProjectId);
            Assert.True(context.TryGetOwningProjectId(referencedTree, out ProjectId referencedProjectId));
            Assert.Equal(referencedProject.Id, referencedProjectId);
            Assert.False(context.HasDeclaredExceptionTypesInReportingScope());
        }

        /// <summary>
        /// Verifies supporting-source visibility, semantic-model lookup, cache
        /// invalidation, duplicate registration, and reporting isolation.
        /// </summary>
        [Fact]
        public void SupportingSource_IsAnalysisOnlyAndInvalidatesCombinedScopeCacheOnce()
        {
            SyntaxTree reportingTree = CSharpSyntaxTree.ParseText(
                "namespace Reporting { public sealed class Target { } }");
            CSharpCompilation reportingCompilation = CreateCompilation(
                "ReportingAssembly",
                reportingTree);
            ProjectClosureSemanticContext context =
                ProjectClosureSemanticContext.CreateSingleCompilationContext(
                    reportingTree,
                    reportingCompilation);

            IReadOnlyList<SemanticCompilationScope> scopesBefore =
                context.GetAnalysisCompilationScopes();
            SemanticCompilationScope projectScope = Assert.Single(scopesBefore);
            IReadOnlyList<INamedTypeSymbol> projectSourceTypes = projectScope.SourceTypes;

            SyntaxTree supportingTree = CSharpSyntaxTree.ParseText(
                "namespace Z { public sealed class Last { } }\n" +
                "namespace A\n" +
                "{\n" +
                "    public sealed class ExternalException : System.Exception { }\n" +
                "    public sealed class Outer { public sealed class Inner { } }\n" +
                "}\n");
            CSharpCompilation supportingCompilation = CreateCompilation(
                "SupportingAssembly",
                supportingTree);
            SemanticCompilationScope supportingScope =
                context.RegisterSupportingSource(supportingCompilation);
            IReadOnlyList<SemanticCompilationScope> scopesAfterRegistration =
                context.GetAnalysisCompilationScopes();
            SemanticCompilationScope duplicateScope =
                context.RegisterSupportingSource(supportingCompilation);

            Assert.Equal(SemanticCompilationScopeKind.SupportingSourceDependency, supportingScope.Kind);
            Assert.Null(supportingScope.ProjectId);
            Assert.Same(supportingScope, duplicateScope);
            Assert.True(context.IsInAnalysisScope(supportingTree));
            Assert.False(context.IsInReportingScope(supportingTree));
            Assert.False(context.TryGetOwningProjectId(supportingTree, out _));
            Assert.True(context.TryGetSemanticModel(supportingTree, out SemanticModel semanticModel));
            Assert.Same(supportingCompilation, semanticModel.Compilation);

            IReadOnlyList<SemanticCompilationScope> scopesAfter =
                context.GetAnalysisCompilationScopes();
            Assert.Equal(2, scopesAfter.Count);
            Assert.Same(scopesAfterRegistration, scopesAfter);
            Assert.Single(scopesAfter, scope => ReferenceEquals(scope, supportingScope));
            SemanticCompilationScope retainedProjectScope = scopesAfter.Single(
                scope => scope.Kind == SemanticCompilationScopeKind.AnalysisTarget);
            Assert.Same(projectScope, retainedProjectScope);
            Assert.Same(projectSourceTypes, projectScope.SourceTypes);
            Assert.False(context.HasDeclaredExceptionTypesInReportingScope());

            string[] sourceTypeNames = supportingScope.SourceTypes
                .Select(type => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                .ToArray();
            Assert.Equal(
                new[]
                {
                    "global::A.ExternalException",
                    "global::A.Outer",
                    "global::A.Outer.Inner",
                    "global::Z.Last"
                },
                sourceTypeNames);

            SyntaxTree unrelatedTree = CSharpSyntaxTree.ParseText("public sealed class Unrelated { }");
            Assert.False(context.IsInAnalysisScope(unrelatedTree));
            Assert.False(context.IsInReportingScope(unrelatedTree));
            Assert.False(context.TryGetSemanticModel(unrelatedTree, out _));
        }

        /// <summary>
        /// Verifies that distinct supporting compilations cannot claim the same
        /// syntax-tree object and that a failed registration is atomic.
        /// </summary>
        [Fact]
        public void SupportingCompilationsSharingSyntaxTree_AreRejectedWithoutMutation()
        {
            SyntaxTree sharedTree = CSharpSyntaxTree.ParseText("public sealed class Shared { }");
            SyntaxTree uniqueTree = CSharpSyntaxTree.ParseText("public sealed class Unique { }");
            CSharpCompilation firstCompilation = CreateCompilation("FirstSupporting", sharedTree);
            CSharpCompilation secondCompilation = CreateCompilation(
                "SecondSupporting",
                uniqueTree,
                sharedTree);
            SupportingSourceCatalog catalog = new();

            SemanticCompilationScope firstScope = catalog.Register(firstCompilation);
            long versionAfterFirstRegistration = catalog.Version;
            SemanticCompilationScope duplicateScope = catalog.Register(firstCompilation);

            Assert.Same(firstScope, duplicateScope);
            Assert.Equal(versionAfterFirstRegistration, catalog.Version);
            Assert.Throws<InvalidOperationException>(() => catalog.Register(secondCompilation));
            Assert.Equal(versionAfterFirstRegistration, catalog.Version);
            Assert.Same(firstScope, Assert.Single(catalog.GetScopes()));
            Assert.True(catalog.TryGetScope(sharedTree, out SemanticCompilationScope retainedScope));
            Assert.Same(firstScope, retainedScope);
            Assert.False(catalog.TryGetScope(uniqueTree, out _));
            Assert.Throws<InvalidOperationException>(() => catalog.Register(secondCompilation));
            Assert.Equal(versionAfterFirstRegistration, catalog.Version);
        }

        /// <summary>
        /// Verifies that a supporting compilation cannot claim a syntax tree
        /// already owned by an analysis target.
        /// </summary>
        [Fact]
        public void SupportingCompilationSharingProjectTree_IsRejectedWithoutMutation()
        {
            SyntaxTree reportingTree = CSharpSyntaxTree.ParseText(
                "namespace Reporting { public sealed class Target { } }");
            CSharpCompilation reportingCompilation = CreateCompilation(
                "ReportingAssembly",
                reportingTree);
            ProjectClosureSemanticContext context =
                ProjectClosureSemanticContext.CreateSingleCompilationContext(
                    reportingTree,
                    reportingCompilation);
            IReadOnlyList<SemanticCompilationScope> scopesBefore =
                context.GetAnalysisCompilationScopes();
            SemanticCompilationScope reportingScope = Assert.Single(scopesBefore);
            CSharpCompilation supportingCompilation = CreateCompilation(
                "ConflictingSupportingAssembly",
                reportingTree);

            Assert.Throws<InvalidOperationException>(
                () => context.RegisterSupportingSource(supportingCompilation));

            IReadOnlyList<SemanticCompilationScope> scopesAfter =
                context.GetAnalysisCompilationScopes();
            Assert.Same(scopesBefore, scopesAfter);
            Assert.Same(reportingScope, Assert.Single(scopesAfter));
            Assert.Equal(SemanticCompilationScopeKind.AnalysisTarget, reportingScope.Kind);
            Assert.True(context.IsInAnalysisScope(reportingTree));
            Assert.True(context.IsInReportingScope(reportingTree));
            Assert.True(context.TryGetSemanticModel(reportingTree, out SemanticModel semanticModel));
            Assert.Same(reportingCompilation, semanticModel.Compilation);
        }

        /// <summary>
        /// Verifies that distinct supporting compilations cannot share one
        /// exact assembly identity and that rejection is atomic.
        /// </summary>
        [Fact]
        public void SupportingCompilationsSharingExactAssemblyIdentity_AreRejectedWithoutMutation()
        {
            CSharpCompilation firstCompilation = CreateCompilation(
                "SupportingAssembly",
                CSharpSyntaxTree.ParseText("public sealed class First { }"));
            CSharpCompilation secondCompilation = CreateCompilation(
                "SupportingAssembly",
                CSharpSyntaxTree.ParseText("public sealed class Second { }"));
            SupportingSourceCatalog catalog = new();

            SemanticCompilationScope firstScope = catalog.Register(firstCompilation);
            long retainedVersion = catalog.Version;

            Assert.Throws<InvalidOperationException>(() => catalog.Register(secondCompilation));
            Assert.Equal(retainedVersion, catalog.Version);
            Assert.Same(firstScope, Assert.Single(catalog.GetScopes()));
            Assert.True(catalog.TryGetScope(
                firstCompilation.Assembly.Identity,
                out SemanticCompilationScope retainedScope));
            Assert.Same(firstScope, retainedScope);
        }

        /// <summary>
        /// Verifies that the same simple assembly name remains valid when the
        /// full assembly identities differ.
        /// </summary>
        [Fact]
        public void SupportingCompilationsSharingSimpleNameWithDifferentVersions_AreAccepted()
        {
            SyntaxTree versionOneTree = CSharpSyntaxTree.ParseText(
                "[assembly: System.Reflection.AssemblyVersion(\"1.0.0.0\")] " +
                "public sealed class First { }");
            SyntaxTree versionTwoTree = CSharpSyntaxTree.ParseText(
                "[assembly: System.Reflection.AssemblyVersion(\"2.0.0.0\")] " +
                "public sealed class Second { }");
            CSharpCompilation versionOne = CreateCompilation("SupportingAssembly", versionOneTree);
            CSharpCompilation versionTwo = CreateCompilation("SupportingAssembly", versionTwoTree);
            SupportingSourceCatalog catalog = new();

            SemanticCompilationScope versionOneScope = catalog.Register(versionOne);
            SemanticCompilationScope versionTwoScope = catalog.Register(versionTwo);

            Assert.Equal(2, catalog.GetScopes().Count);
            Assert.True(catalog.TryGetScope(versionOne.Assembly.Identity, out SemanticCompilationScope foundOne));
            Assert.True(catalog.TryGetScope(versionTwo.Assembly.Identity, out SemanticCompilationScope foundTwo));
            Assert.Same(versionOneScope, foundOne);
            Assert.Same(versionTwoScope, foundTwo);
        }

        /// <summary>
        /// Verifies that supporting source cannot duplicate a project scope's
        /// exact assembly identity even when syntax trees differ.
        /// </summary>
        [Fact]
        public void SupportingCompilationSharingProjectAssemblyIdentity_IsRejectedWithoutMutation()
        {
            SyntaxTree reportingTree = CSharpSyntaxTree.ParseText("public sealed class Reporting { }");
            CSharpCompilation reportingCompilation = CreateCompilation("SharedAssembly", reportingTree);
            ProjectClosureSemanticContext context =
                ProjectClosureSemanticContext.CreateSingleCompilationContext(
                    reportingTree, reportingCompilation);
            IReadOnlyList<SemanticCompilationScope> scopesBefore = context.GetAnalysisCompilationScopes();
            CSharpCompilation supportingCompilation = CreateCompilation(
                "SharedAssembly",
                CSharpSyntaxTree.ParseText("public sealed class Supporting { }"));

            Assert.Throws<InvalidOperationException>(
                () => context.RegisterSupportingSource(supportingCompilation));
            Assert.Same(scopesBefore, context.GetAnalysisCompilationScopes());
            Assert.Single(context.GetAnalysisCompilationScopes());
        }

        /// <summary>
        /// Creates a source-backed C# library compilation.
        /// </summary>
        /// <param name="assemblyName">The compilation assembly name.</param>
        /// <param name="syntaxTrees">The source trees to compile.</param>
        /// <returns>The created library compilation.</returns>
        private static CSharpCompilation CreateCompilation(
            string assemblyName,
            params SyntaxTree[] syntaxTrees)
        {
            return CSharpCompilation.Create(
                assemblyName,
                syntaxTrees,
                MetadataReferences.Default,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }
    }
}
