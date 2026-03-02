using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using XMLDocNormalizer.Checks.Infrastructure.Namespace;

namespace XMLDocNormalizerTests.Check.Infrastructure.Namespace
{
    /// <summary>
    /// Tests for <see cref="NamespaceDocumentationAggregator"/> focusing on unique namespace key counting.
    /// </summary>
    public sealed class NamespaceDocumentationAggregatorTests
    {
        /// <summary>
        /// Ensures that multiple occurrences of the same namespace in the same directory are counted only once.
        /// </summary>
        [Fact]
        public void UniqueNamespaceKeyCount_SameDirectorySameNamespace_IsCountedOnce()
        {
            NamespaceDocumentationAggregator aggregator = new NamespaceDocumentationAggregator(enabled: true);

            SyntaxTree tree1 = CSharpSyntaxTree.ParseText("namespace N { }", path: "InMemory1.cs");
            SyntaxTree tree2 = CSharpSyntaxTree.ParseText("namespace N { }", path: "InMemory2.cs");

            aggregator.RegisterMissingNamespaceDocumentation(
                tree1,
                filePath: @"C:\Repo\Game\A.cs",
                namespaceName: "SEE.Game",
                anchorPosition: 0);

            aggregator.RegisterMissingNamespaceDocumentation(
                tree2,
                filePath: @"C:\Repo\Game\B.cs",
                namespaceName: "SEE.Game",
                anchorPosition: 0);

            Assert.Equal(1, aggregator.UniqueNamespaceKeyCount);
        }

        /// <summary>
        /// Ensures that the same namespace name in different directories is counted separately,
        /// because DOC101 aggregation is per (directory, fully-qualified namespace).
        /// </summary>
        [Fact]
        public void UniqueNamespaceKeyCount_DifferentDirectorySameNamespace_IsCountedSeparately()
        {
            NamespaceDocumentationAggregator aggregator = new NamespaceDocumentationAggregator(enabled: true);

            SyntaxTree tree1 = CSharpSyntaxTree.ParseText("namespace N { }", path: "InMemory1.cs");
            SyntaxTree tree2 = CSharpSyntaxTree.ParseText("namespace N { }", path: "InMemory2.cs");

            aggregator.RegisterMissingNamespaceDocumentation(
                tree1,
                filePath: @"C:\Repo\Game\Drawable.cs",
                namespaceName: "SEE.Drawable",
                anchorPosition: 0);

            aggregator.RegisterMissingNamespaceDocumentation(
                tree2,
                filePath: @"C:\Repo\UI\Drawable.cs",
                namespaceName: "SEE.Drawable",
                anchorPosition: 0);

            Assert.Equal(2, aggregator.UniqueNamespaceKeyCount);
        }

        /// <summary>
        /// Ensures that different namespaces in the same directory are counted separately.
        /// </summary>
        [Fact]
        public void UniqueNamespaceKeyCount_SameDirectoryDifferentNamespace_IsCountedSeparately()
        {
            NamespaceDocumentationAggregator aggregator = new NamespaceDocumentationAggregator(enabled: true);

            SyntaxTree tree1 = CSharpSyntaxTree.ParseText("namespace N { }", path: "InMemory1.cs");
            SyntaxTree tree2 = CSharpSyntaxTree.ParseText("namespace N { }", path: "InMemory2.cs");

            aggregator.RegisterMissingNamespaceDocumentation(
                tree1,
                filePath: @"C:\Repo\Game\A.cs",
                namespaceName: "SEE.Game",
                anchorPosition: 0);

            aggregator.RegisterMissingNamespaceDocumentation(
                tree2,
                filePath: @"C:\Repo\Game\B.cs",
                namespaceName: "SEE.Game.UI",
                anchorPosition: 0);

            Assert.Equal(2, aggregator.UniqueNamespaceKeyCount);
        }

        /// <summary>
        /// Ensures that the aggregator does not count anything when it is disabled.
        /// </summary>
        [Fact]
        public void UniqueNamespaceKeyCount_DisabledAggregator_IsZero()
        {
            NamespaceDocumentationAggregator aggregator = new NamespaceDocumentationAggregator(enabled: false);

            SyntaxTree tree = CSharpSyntaxTree.ParseText("namespace N { }", path: "InMemory.cs");

            aggregator.RegisterMissingNamespaceDocumentation(
                tree,
                filePath: @"C:\Repo\Game\A.cs",
                namespaceName: "SEE.Game",
                anchorPosition: 0);

            Assert.Equal(0, aggregator.UniqueNamespaceKeyCount);
        }
    }
}