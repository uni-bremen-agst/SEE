using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure.Value;

namespace XMLDocNormalizerTests.Check.Syntax.Value
{
    /// <summary>
    /// Tests value-documentation classification helpers.
    /// </summary>
    public sealed class ValueDocumentationClassifierTests
    {
        /// <summary>
        /// Ensures that readable properties and indexers are value-documentation targets.
        /// </summary>
        [Fact]
        public void IsValueDocumentationTarget_ReturnsTrueForReadablePropertyAndIndexer()
        {
            Assert.True(ValueDocumentationClassifier.IsValueDocumentationTarget(ValueTargetKind.ReadableProperty));
            Assert.True(ValueDocumentationClassifier.IsValueDocumentationTarget(ValueTargetKind.Indexer));
        }

        /// <summary>
        /// Ensures that write-only properties and invalid members are not value-documentation targets.
        /// </summary>
        [Fact]
        public void IsValueDocumentationTarget_ReturnsFalseForWriteOnlyPropertyAndInvalidMember()
        {
            Assert.False(ValueDocumentationClassifier.IsValueDocumentationTarget(ValueTargetKind.WriteOnlyProperty));
            Assert.False(ValueDocumentationClassifier.IsValueDocumentationTarget(ValueTargetKind.InvalidMember));
        }

        /// <summary>
        /// Ensures that DTO-like type names are recognized.
        /// </summary>
        [Fact]
        public void HasDtoLikeTypeName_ReturnsTrueForDtoResultAndReportNames()
        {
            Assert.True(ValueDocumentationClassifier.HasDtoLikeTypeName("RunDto"));
            Assert.True(ValueDocumentationClassifier.HasDtoLikeTypeName("RunDTO"));
            Assert.True(ValueDocumentationClassifier.HasDtoLikeTypeName("RunResult"));
            Assert.True(ValueDocumentationClassifier.HasDtoLikeTypeName("RunReport"));
        }

        /// <summary>
        /// Ensures that non-DTO type names are not recognized as DTO-like.
        /// </summary>
        [Fact]
        public void HasDtoLikeTypeName_ReturnsFalseForNonDtoNames()
        {
            Assert.False(ValueDocumentationClassifier.HasDtoLikeTypeName("ToolOptions"));
            Assert.False(ValueDocumentationClassifier.HasDtoLikeTypeName("XmlDocOptions"));
            Assert.False(ValueDocumentationClassifier.HasDtoLikeTypeName("TopLevelTagOrderStatistics"));
            Assert.False(ValueDocumentationClassifier.HasDtoLikeTypeName("NamespaceDocState"));
        }

        /// <summary>
        /// Ensures that DTO-like namespaces are recognized.
        /// </summary>
        [Fact]
        public void HasDtoLikeNamespace_ReturnsTrueForDtoNamespaceSegments()
        {
            Assert.True(ValueDocumentationClassifier.HasDtoLikeNamespace("Sample.Dto"));
            Assert.True(ValueDocumentationClassifier.HasDtoLikeNamespace("Sample.Dto.Models"));
            Assert.True(ValueDocumentationClassifier.HasDtoLikeNamespace("Sample.DTO"));
            Assert.True(ValueDocumentationClassifier.HasDtoLikeNamespace("Sample.DTO.Models"));
        }

        /// <summary>
        /// Ensures that non-DTO namespaces are not recognized as DTO-like.
        /// </summary>
        [Fact]
        public void HasDtoLikeNamespace_ReturnsFalseForNonDtoNamespaces()
        {
            Assert.False(ValueDocumentationClassifier.HasDtoLikeNamespace(null));
            Assert.False(ValueDocumentationClassifier.HasDtoLikeNamespace(string.Empty));
            Assert.False(ValueDocumentationClassifier.HasDtoLikeNamespace("Sample.Models"));
            Assert.False(ValueDocumentationClassifier.HasDtoLikeNamespace("Sample.Dot"));
            Assert.False(ValueDocumentationClassifier.HasDtoLikeNamespace("Sample.DtoLike"));
        }

        /// <summary>
        /// Ensures that members inside DTO namespaces are classified as DTO-like containers.
        /// </summary>
        [Fact]
        public void IsDtoLikeContainer_ReturnsTrueForMemberInDtoNamespace()
        {
            MemberDeclarationSyntax member = GetFirstProperty(
                "namespace Sample.Dto\n" +
                "{\n" +
                "    public sealed class RunData\n" +
                "    {\n" +
                "        public int Count { get; set; }\n" +
                "    }\n" +
                "}\n");

            Assert.True(ValueDocumentationClassifier.IsDtoLikeContainer(member));
        }

        /// <summary>
        /// Ensures that members inside DTO-like type names are classified as DTO-like containers.
        /// </summary>
        [Fact]
        public void IsDtoLikeContainer_ReturnsTrueForMemberInDtoLikeTypeName()
        {
            MemberDeclarationSyntax member = GetFirstProperty(
                "namespace Sample\n" +
                "{\n" +
                "    public sealed class RunResult\n" +
                "    {\n" +
                "        public int Count { get; set; }\n" +
                "    }\n" +
                "}\n");

            Assert.True(ValueDocumentationClassifier.IsDtoLikeContainer(member));
        }

        /// <summary>
        /// Ensures that members inside non-DTO types are not classified as DTO-like containers.
        /// </summary>
        [Fact]
        public void IsDtoLikeContainer_ReturnsFalseForMemberInNonDtoType()
        {
            MemberDeclarationSyntax member = GetFirstProperty(
                "namespace Sample\n" +
                "{\n" +
                "    public sealed class ToolOptions\n" +
                "    {\n" +
                "        public int Count { get; set; }\n" +
                "    }\n" +
                "}\n");

            Assert.False(ValueDocumentationClassifier.IsDtoLikeContainer(member));
        }

        /// <summary>
        /// Gets the first property declaration from source text.
        /// </summary>
        /// <param name="source">
        /// The source text to parse.
        /// </param>
        /// <returns>
        /// The first property declaration found in the source text.
        /// </returns>
        private static MemberDeclarationSyntax GetFirstProperty(string source)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(source);

            return tree
                .GetCompilationUnitRoot()
                .DescendantNodes()
                .OfType<PropertyDeclarationSyntax>()
                .First();
        }
    }
}
