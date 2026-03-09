using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Inheritdoc
{
    /// <summary>
    /// Tests DOC720 – inheritdoc without a valid implicit source.
    /// </summary>
    public sealed class DOC720_InheritdocNoSourceTests
    {
        /// <summary>
        /// Ensures that inheritdoc on a member without inheritance source
        /// triggers DOC720.
        /// </summary>
        [Fact]
        public void InheritdocWithoutImplicitSource_IsDetected()
        {
            string member =
                "/// <inheritdoc/>\n" +
                "public void M() { }\n";

            List<Finding> findings =
                CheckAssert.FindSemanticInheritdocFindingsForMember(member);

            Finding finding = Assert.Single(findings);

            Assert.Equal(XmlDocSmells.InheritdocNoSource.ID, finding.Smell.ID);
            Assert.Equal("inheritdoc", finding.TagName);
        }

        /// <summary>
        /// Ensures that inheritdoc on an overridden property does not trigger DOC720.
        /// </summary>
        [Fact]
        public void InheritdocOnOverriddenProperty_DoesNotTriggerFinding()
        {
            string source =
                "public class Base\n" +
                "{\n" +
                "    public virtual int P => 0;\n" +
                "}\n" +
                "\n" +
                "public class Derived : Base\n" +
                "{\n" +
                "    /// <inheritdoc/>\n" +
                "    public override int P => 1;\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticInheritdocFindingsForSource(source);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that inheritdoc on an implicitly implemented interface property
        /// does not trigger DOC720.
        /// </summary>
        [Fact]
        public void InheritdocOnImplicitInterfacePropertyImplementation_DoesNotTriggerFinding()
        {
            string source =
                "public interface ITest\n" +
                "{\n" +
                "    int P { get; }\n" +
                "}\n" +
                "\n" +
                "public class Test : ITest\n" +
                "{\n" +
                "    /// <inheritdoc/>\n" +
                "    public int P => 1;\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticInheritdocFindingsForSource(source);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that inheritdoc on an explicitly implemented interface property
        /// does not trigger DOC720.
        /// </summary>
        [Fact]
        public void InheritdocOnExplicitInterfacePropertyImplementation_DoesNotTriggerFinding()
        {
            string source =
                "public interface ITest\n" +
                "{\n" +
                "    int P { get; }\n" +
                "}\n" +
                "\n" +
                "public class Test : ITest\n" +
                "{\n" +
                "    /// <inheritdoc/>\n" +
                "    int ITest.P => 1;\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticInheritdocFindingsForSource(source);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that inheritdoc on an overridden event does not trigger DOC720.
        /// </summary>
        [Fact]
        public void InheritdocOnOverriddenEvent_DoesNotTriggerFinding()
        {
            string source =
                "using System;\n" +
                "\n" +
                "public class Base\n" +
                "{\n" +
                "    public virtual event EventHandler? Changed;\n" +
                "}\n" +
                "\n" +
                "public class Derived : Base\n" +
                "{\n" +
                "    /// <inheritdoc/>\n" +
                "    public override event EventHandler? Changed\n" +
                "    {\n" +
                "        add { }\n" +
                "        remove { }\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticInheritdocFindingsForSource(source);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that inheritdoc on an implicitly implemented interface event
        /// does not trigger DOC720.
        /// </summary>
        [Fact]
        public void InheritdocOnImplicitInterfaceEventImplementation_DoesNotTriggerFinding()
        {
            string source =
                "using System;\n" +
                "\n" +
                "public interface ITest\n" +
                "{\n" +
                "    event EventHandler? Changed;\n" +
                "}\n" +
                "\n" +
                "public class Test : ITest\n" +
                "{\n" +
                "    /// <inheritdoc/>\n" +
                "    public event EventHandler? Changed;\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticInheritdocFindingsForSource(source);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that inheritdoc on an explicitly implemented interface event
        /// does not trigger DOC720.
        /// </summary>
        [Fact]
        public void InheritdocOnExplicitInterfaceEventImplementation_DoesNotTriggerFinding()
        {
            string source =
                "using System;\n" +
                "\n" +
                "public interface ITest\n" +
                "{\n" +
                "    event EventHandler? Changed;\n" +
                "}\n" +
                "\n" +
                "public class Test : ITest\n" +
                "{\n" +
                "    /// <inheritdoc/>\n" +
                "    event EventHandler? ITest.Changed\n" +
                "    {\n" +
                "        add { }\n" +
                "        remove { }\n" +
                "    }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticInheritdocFindingsForSource(source);

            Assert.Empty(findings);
        }
    }
}
