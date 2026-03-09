using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Inheritdoc
{
    /// <summary>
    /// Tests DOC710 – invalid inheritdoc cref target.
    /// </summary>
    /// <remarks>
    /// DOC710 is raised only when the <c>cref</c> target cannot be resolved semantically.
    /// If the target resolves successfully but is not a valid inheritance source,
    /// the case belongs to DOC711 instead.
    /// </remarks>
    public sealed class DOC710_InvalidInheritdocCrefTests
    {
        /// <summary>
        /// Ensures that an unresolved inheritdoc cref target triggers DOC710.
        /// </summary>
        [Fact]
        public void InvalidInheritdocCref_IsDetected()
        {
            string member =
                "/// <inheritdoc cref=\"DoesNotExist\"/>\n" +
                "public void M() { }\n";

            List<Finding> findings =
                CheckAssert.FindSemanticInheritdocFindingsForMember(member);

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.InvalidInheritdocCref.ID, finding.Smell.ID);
            Assert.Equal("inheritdoc", finding.TagName);
        }

        /// <summary>
        /// Ensures that an unresolved fully qualified inheritdoc cref target triggers DOC710.
        /// </summary>
        [Fact]
        public void InvalidQualifiedInheritdocCref_IsDetected()
        {
            string member =
                "/// <inheritdoc cref=\"Unknown.Namespace.Type.Member\"/>\n" +
                "public void M() { }\n";

            List<Finding> findings =
                CheckAssert.FindSemanticInheritdocFindingsForMember(member);

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.InvalidInheritdocCref.ID, finding.Smell.ID);
            Assert.Equal("inheritdoc", finding.TagName);
        }

        /// <summary>
        /// Ensures that an unresolved inheritdoc cref target in another file still triggers DOC710.
        /// </summary>
        [Fact]
        public void InvalidInheritdocCref_InOtherFile_IsDetected()
        {
            List<Finding> findings = CheckAssert.FindSemanticInheritdocFindingsForSources(
                ("Base.cs",
                    "namespace Contracts;\n" +
                    "public class Base\n" +
                    "{\n" +
                    "    public void M() { }\n" +
                    "}"),
                ("Derived.cs",
                    "namespace Implementation;\n" +
                    "public class Derived\n" +
                    "{\n" +
                    "    /// <inheritdoc cref=\"Contracts.Base.DoesNotExist\"/>\n" +
                    "    public void N() { }\n" +
                    "}")
            );

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.InvalidInheritdocCref.ID, finding.Smell.ID);
            Assert.Equal("inheritdoc", finding.TagName);
        }

        /// <summary>
        /// Ensures that inheritdoc with cref does not additionally trigger DOC720,
        /// even when the cref target is invalid.
        /// </summary>
        [Fact]
        public void InvalidInheritdocCref_DoesNotTriggerNoSource()
        {
            string member =
                "/// <inheritdoc cref=\"DoesNotExist\"/>\n" +
                "public void M() { }\n";

            List<Finding> findings =
                CheckAssert.FindSemanticInheritdocFindingsForMember(member);

            Assert.DoesNotContain(findings, f => f.Smell.ID == XmlDocSmells.InheritdocNoSource.ID);
        }

        /// <summary>
        /// Ensures that an invalid inheritdoc cref target produces exactly one DOC710 finding.
        /// </summary>
        [Fact]
        public void InvalidInheritdocCref_ProducesExactlyOneFinding()
        {
            string member =
                "/// <inheritdoc cref=\"DoesNotExist\"/>\n" +
                "public void M() { }\n";

            List<Finding> findings =
                CheckAssert.FindSemanticInheritdocFindingsForMember(member);

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.InvalidInheritdocCref.ID, finding.Smell.ID);
        }
    }
}