using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Syntax.Inheritdoc
{
    /// <summary>
    /// Tests DOC710 – invalid inheritdoc cref target.
    /// </summary>
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

            List<Finding> findings = CheckAssert.FindSemanticInheritdocFindingsForMember(member);

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.InvalidInheritdocCref.ID, finding.Smell.ID);
            Assert.Equal("inheritdoc", finding.TagName);
        }

        /// <summary>
        /// Ensures that a resolvable inheritdoc cref target does not trigger DOC710.
        /// </summary>
        [Fact]
        public void ValidInheritdocCref_DoesNotTriggerFinding()
        {
            string source =
                "public class Base\n" +
                "{\n" +
                "    /// <summary>Base documentation.</summary>\n" +
                "    public void M() { }\n" +
                "}\n" +
                "\n" +
                "public class Derived\n" +
                "{\n" +
                "    /// <inheritdoc cref=\"Base.M\"/>\n" +
                "    public void N() { }\n" +
                "}\n";

            List<Finding> findings = CheckAssert.FindSemanticInheritdocFindingsForSource(source);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that an inheritdoc cref target can be resolved across multiple files
        /// within the same namespace.
        /// </summary>
        [Fact]
        public void ValidInheritdocCref_InOtherFileSameNamespace_DoesNotTriggerFinding()
        {
            List<Finding> findings = CheckAssert.FindSemanticInheritdocFindingsForSources(
                ("Base.cs",
                    "namespace Demo;\n" +
                    "public class Base\n" +
                    "{\n" +
                    "    /// <summary>Base documentation.</summary>\n" +
                    "    public void M() { }\n" +
                    "}"),
                ("Derived.cs",
                    "namespace Demo;\n" +
                    "public class Derived\n" +
                    "{\n" +
                    "    /// <inheritdoc cref=\"Base.M\"/>\n" +
                    "    public void N() { }\n" +
                    "}")
            );

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that an inheritdoc cref target can be resolved across multiple files
        /// and across namespaces when a fully qualified cref is used.
        /// </summary>
        [Fact]
        public void ValidInheritdocCref_InOtherFileDifferentNamespace_DoesNotTriggerFinding()
        {
            List<Finding> findings = CheckAssert.FindSemanticInheritdocFindingsForSources(
                ("Base.cs",
                    "namespace Contracts;\n" +
                    "public class Base\n" +
                    "{\n" +
                    "    /// <summary>Base documentation.</summary>\n" +
                    "    public void M() { }\n" +
                    "}"),
                ("Derived.cs",
                    "namespace Implementation;\n" +
                    "public class Derived\n" +
                    "{\n" +
                    "    /// <inheritdoc cref=\"Contracts.Base.M\"/>\n" +
                    "    public void N() { }\n" +
                    "}")
            );

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that an unresolved inheritdoc cref target in another namespace
        /// still triggers DOC710.
        /// </summary>
        [Fact]
        public void InvalidInheritdocCref_InOtherFileDifferentNamespace_IsDetected()
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
        }
    }
}