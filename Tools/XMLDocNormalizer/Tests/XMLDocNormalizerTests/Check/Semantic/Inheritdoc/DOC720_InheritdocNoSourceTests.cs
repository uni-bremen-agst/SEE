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
        /// Ensures that inheritdoc on an override does not trigger DOC720.
        /// </summary>
        [Fact]
        public void InheritdocOnOverride_DoesNotTriggerFinding()
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
        /// Ensures that inheritdoc on an implicitly implemented interface method
        /// does not trigger DOC720.
        /// </summary>
        [Fact]
        public void InheritdocOnImplicitInterfaceImplementation_DoesNotTriggerFinding()
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
        /// Ensures that inheritdoc on an explicitly implemented interface method
        /// does not trigger DOC720.
        /// </summary>
        [Fact]
        public void InheritdocOnExplicitInterfaceImplementation_DoesNotTriggerFinding()
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
                "    void ITest.M() { }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticInheritdocFindingsForSource(source);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that inheritdoc on a derived class does not trigger DOC720.
        /// </summary>
        [Fact]
        public void InheritdocOnDerivedClass_DoesNotTriggerFinding()
        {
            string source =
                "/// <summary>Base type documentation.</summary>\n" +
                "public class Base { }\n" +
                "\n" +
                "/// <inheritdoc/>\n" +
                "public class Derived : Base { }\n";

            List<Finding> findings =
                CheckAssert.FindSemanticInheritdocFindingsForSource(source);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that inheritdoc on a derived interface does not trigger DOC720.
        /// </summary>
        [Fact]
        public void InheritdocOnDerivedInterface_DoesNotTriggerFinding()
        {
            string source =
                "public interface IBase\n" +
                "{\n" +
                "    /// <summary>Base interface documentation.</summary>\n" +
                "    void M();\n" +
                "}\n" +
                "\n" +
                "public interface IDerived : IBase\n" +
                "{\n" +
                "    /// <inheritdoc/>\n" +
                "    new void M();\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticInheritdocFindingsForSource(source);

            Assert.Empty(findings);
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
                "    /// <summary>Base property documentation.</summary>\n" +
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
                "    /// <summary>Interface property documentation.</summary>\n" +
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
                "    /// <summary>Interface property documentation.</summary>\n" +
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
                "    /// <summary>Base event documentation.</summary>\n" +
                "    public virtual event EventHandler Changed;\n" +
                "}\n" +
                "\n" +
                "public class Derived : Base\n" +
                "{\n" +
                "    /// <inheritdoc/>\n" +
                "    public override event EventHandler Changed\n" +
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
                "    /// <summary>Interface event documentation.</summary>\n" +
                "    event EventHandler Changed;\n" +
                "}\n" +
                "\n" +
                "public class Test : ITest\n" +
                "{\n" +
                "    /// <inheritdoc/>\n" +
                "    public event EventHandler Changed;\n" +
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
                "    /// <summary>Interface event documentation.</summary>\n" +
                "    event EventHandler Changed;\n" +
                "}\n" +
                "\n" +
                "public class Test : ITest\n" +
                "{\n" +
                "    /// <inheritdoc/>\n" +
                "    event EventHandler ITest.Changed\n" +
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
