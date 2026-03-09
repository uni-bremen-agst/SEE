using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Inheritdoc
{
    /// <summary>
    /// Tests DOC730 – redundant inheritdoc because the resolved source has no useful documentation.
    /// </summary>
    public sealed class DOC730_RedundantInheritdocTests
    {
        /// <summary>
        /// Ensures that inheritdoc on an override without base documentation triggers DOC730.
        /// </summary>
        [Fact]
        public void OverrideWithoutBaseDocumentation_IsDetected()
        {
            string source =
                "public class Base\n" +
                "{\n" +
                "    public virtual void M() { }\n" +
                "}\n" +
                "\n" +
                "public class Derived : Base\n" +
                "{\n" +
                "    /// <inheritdoc/>\n" +
                "    public override void M() { }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticInheritdocFindingsForSource(source);

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.RedundantInheritdoc.ID, finding.Smell.ID);
            Assert.Equal("inheritdoc", finding.TagName);
        }

        /// <summary>
        /// Ensures that inheritdoc on an override with base documentation does not trigger DOC730.
        /// </summary>
        [Fact]
        public void OverrideWithBaseDocumentation_DoesNotTriggerFinding()
        {
            string source =
                "public class Base\n" +
                "{\n" +
                "    /// <summary>Base documentation.</summary>\n" +
                "    public virtual void M() { }\n" +
                "}\n" +
                "\n" +
                "public class Derived : Base\n" +
                "{\n" +
                "    /// <inheritdoc/>\n" +
                "    public override void M() { }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticInheritdocFindingsForSource(source);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that inheritdoc cref to a valid base member without documentation triggers DOC730.
        /// </summary>
        [Fact]
        public void ExplicitInheritdocToUndocumentedBaseMember_IsDetected()
        {
            string source =
                "public class Base\n" +
                "{\n" +
                "    public virtual void M() { }\n" +
                "}\n" +
                "\n" +
                "public class Derived : Base\n" +
                "{\n" +
                "    /// <inheritdoc cref=\"Base.M\"/>\n" +
                "    public override void M() { }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticInheritdocFindingsForSource(source);

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.RedundantInheritdoc.ID, finding.Smell.ID);
            Assert.Equal("inheritdoc", finding.TagName);
        }

        /// <summary>
        /// Ensures that inheritdoc cref to a valid documented base member does not trigger DOC730.
        /// </summary>
        [Fact]
        public void ExplicitInheritdocToDocumentedBaseMember_DoesNotTriggerFinding()
        {
            string source =
                "public class Base\n" +
                "{\n" +
                "    /// <summary>Base documentation.</summary>\n" +
                "    public virtual void M() { }\n" +
                "}\n" +
                "\n" +
                "public class Derived : Base\n" +
                "{\n" +
                "    /// <inheritdoc cref=\"Base.M\"/>\n" +
                "    public override void M() { }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticInheritdocFindingsForSource(source);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that inheritdoc on an interface implementation without source documentation
        /// triggers DOC730.
        /// </summary>
        [Fact]
        public void InterfaceImplementationWithoutSourceDocumentation_IsDetected()
        {
            string source =
                "public interface ITest\n" +
                "{\n" +
                "    void M();\n" +
                "}\n" +
                "\n" +
                "public class Test : ITest\n" +
                "{\n" +
                "    /// <inheritdoc/>\n" +
                "    public void M() { }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticInheritdocFindingsForSource(source);

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.RedundantInheritdoc.ID, finding.Smell.ID);
            Assert.Equal("inheritdoc", finding.TagName);
        }

        /// <summary>
        /// Ensures that inheritdoc on an interface implementation with source documentation
        /// does not trigger DOC730.
        /// </summary>
        [Fact]
        public void InterfaceImplementationWithSourceDocumentation_DoesNotTriggerFinding()
        {
            string source =
                "public interface ITest\n" +
                "{\n" +
                "    /// <summary>Interface documentation.</summary>\n" +
                "    void M();\n" +
                "}\n" +
                "\n" +
                "public class Test : ITest\n" +
                "{\n" +
                "    /// <inheritdoc/>\n" +
                "    public void M() { }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticInheritdocFindingsForSource(source);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that inheritdoc on a derived type without base type documentation triggers DOC730.
        /// </summary>
        [Fact]
        public void DerivedTypeWithoutBaseDocumentation_IsDetected()
        {
            string source =
                "public class Base\n" +
                "{\n" +
                "}\n" +
                "\n" +
                "/// <inheritdoc/>\n" +
                "public class Derived : Base\n" +
                "{\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticInheritdocFindingsForSource(source);

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.RedundantInheritdoc.ID, finding.Smell.ID);
            Assert.Equal("inheritdoc", finding.TagName);
        }

        /// <summary>
        /// Ensures that inheritdoc on a derived type with base type documentation does not trigger DOC730.
        /// </summary>
        [Fact]
        public void DerivedTypeWithBaseDocumentation_DoesNotTriggerFinding()
        {
            string source =
                "/// <summary>Base type documentation.</summary>\n" +
                "public class Base\n" +
                "{\n" +
                "}\n" +
                "\n" +
                "/// <inheritdoc/>\n" +
                "public class Derived : Base\n" +
                "{\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticInheritdocFindingsForSource(source);

            Assert.Empty(findings);
        }
    }
}