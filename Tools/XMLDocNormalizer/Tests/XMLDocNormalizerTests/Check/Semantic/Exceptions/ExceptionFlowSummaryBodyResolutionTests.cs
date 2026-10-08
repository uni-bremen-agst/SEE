using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Execution.Semantic;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Pins the exact moved target/body predicate and its fail-closed scope guard.
    /// </summary>
    public sealed class ExceptionFlowSummaryBodyResolutionTests
    {
        /// <summary>
        /// Preserves method/local-function block and expression bodies, without
        /// widening this invocation predicate to other callable syntax kinds.
        /// </summary>
        /// <param name="member">The member declaration inside a test class.</param>
        /// <param name="localFunction">Whether to select its nested local function.</param>
        /// <param name="expected">Whether the declaration has a supported body.</param>
        [Theory]
        [InlineData("void M() { }", false, true)]
        [InlineData("int M() => 1;", false, true)]
        [InlineData("void M() { void L() { } }", true, true)]
        [InlineData("void M() { int L() => 1; }", true, true)]
        [InlineData("abstract void M();", false, false)]
        [InlineData("extern void M();", false, false)]
        [InlineData("partial void M();", false, false)]
        [InlineData("Test() { }", false, false)]
        [InlineData("int P => 1;", false, false)]
        [InlineData("void M() { System.Action a = () => { }; }", false, true)]
        public void BodyShape_UsesTheOriginalExactPredicate(string member, bool localFunction, bool expected)
        {
            SyntaxNode root = CSharpSyntaxTree.ParseText("abstract partial class Test { " + member + " }").GetRoot();
            SyntaxNode declaration = localFunction
                ? root.DescendantNodes().OfType<LocalFunctionStatementSyntax>().Single()
                : root.DescendantNodes().OfType<ClassDeclarationSyntax>().Single().Members.Single();
            bool found = ExceptionFlowSummaryTargetRegistrar.TryGetSummaryInvocationBody(declaration, out SyntaxNode? body);
            Assert.Equal(expected, found);
            Assert.Equal(expected, body != null);
            if (body != null)
            {
                Assert.Same(declaration.SyntaxTree, body.SyntaxTree);
                Assert.True(declaration.Span.Contains(body.Span));
            }
        }

        /// <summary>
        /// Requires the exact declaration tree to belong to the environment;
        /// equal source text in another compilation must not grant access.
        /// </summary>
        [Fact]
        public void TargetBody_FailsClosedForForeignSemanticScope()
        {
            const string source = "class Test { void M() { } }";
            SyntaxTree owned = CSharpSyntaxTree.ParseText(source);
            SyntaxTree foreign = CSharpSyntaxTree.ParseText(source);
            CSharpCompilation compilation = CSharpCompilation.Create("Owned", [owned]);
            CSharpCompilation other = CSharpCompilation.Create("Foreign", [foreign]);
            ExceptionFlowSemanticEnvironment environment = new(
                ProjectClosureSemanticContext.CreateSingleCompilationContext(owned, compilation));
            IMethodSymbol ownedMethod = (IMethodSymbol)compilation.GetSemanticModel(owned).GetDeclaredSymbol(
                owned.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single())!;
            IMethodSymbol foreignMethod = (IMethodSymbol)other.GetSemanticModel(foreign).GetDeclaredSymbol(
                foreign.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single())!;
            Assert.True(ExceptionFlowSummaryTargetRegistrar.HasAnalyzableSummaryInvocationBody(ownedMethod, environment));
            Assert.False(ExceptionFlowSummaryTargetRegistrar.HasAnalyzableSummaryInvocationBody(foreignMethod, environment));
        }

        /// <summary>
        /// Bodyless source and metadata targets remain unavailable even when
        /// their declaring source compilation is in scope.
        /// </summary>
        [Fact]
        public void TargetBody_FailsClosedForBodylessAndMetadataMethods()
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText("abstract class Test { public abstract void M(); }");
            CSharpCompilation compilation = CSharpCompilation.Create("Test", [tree],
                [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);
            ExceptionFlowSemanticEnvironment environment = new(
                ProjectClosureSemanticContext.CreateSingleCompilationContext(tree, compilation));
            IMethodSymbol bodyless = (IMethodSymbol)compilation.GetSemanticModel(tree).GetDeclaredSymbol(
                tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single())!;
            IMethodSymbol metadata = compilation.GetSpecialType(SpecialType.System_Object)
                .GetMembers("ToString").OfType<IMethodSymbol>().Single();
            Assert.False(ExceptionFlowSummaryTargetRegistrar.HasAnalyzableSummaryInvocationBody(bodyless, environment));
            Assert.False(ExceptionFlowSummaryTargetRegistrar.HasAnalyzableSummaryInvocationBody(metadata, environment));
        }
    }
}
