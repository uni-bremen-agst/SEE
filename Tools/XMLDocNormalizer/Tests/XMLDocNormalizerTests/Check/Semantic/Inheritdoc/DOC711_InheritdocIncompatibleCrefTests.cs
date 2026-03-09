using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Inheritdoc
{
    /// <summary>
    /// Tests DOC711 – inheritdoc cref resolves successfully but does not refer
    /// to a valid inheritance source for the documented declaration.
    /// </summary>
    public sealed class DOC711_InheritdocIncompatibleCrefTests
    {
        /// <summary>
        /// Ensures that inheritdoc cref to an unrelated method triggers DOC711.
        /// </summary>
        [Fact]
        public void MethodReferencingUnrelatedMethod_IsDetected()
        {
            string source =
                "public class Source\n" +
                "{\n" +
                "    /// <summary>Source method documentation.</summary>\n" +
                "    public void M() { }\n" +
                "}\n" +
                "\n" +
                "public class Target\n" +
                "{\n" +
                "    /// <inheritdoc cref=\"Source.M\"/>\n" +
                "    public void N() { }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticInheritdocFindingsForSource(source);

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.InheritdocIncompatibleCref.ID, finding.Smell.ID);
            Assert.Equal("inheritdoc", finding.TagName);
        }

        /// <summary>
        /// Ensures that inheritdoc cref to the overridden base method does not trigger DOC711.
        /// </summary>
        [Fact]
        public void MethodReferencingOverriddenBaseMethod_DoesNotTriggerFinding()
        {
            string source =
                "public class Base\n" +
                "{\n" +
                "    /// <summary>Base method documentation.</summary>\n" +
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
        /// Ensures that inheritdoc cref to an implicitly implemented interface method
        /// does not trigger DOC711.
        /// </summary>
        [Fact]
        public void MethodReferencingImplicitlyImplementedInterfaceMethod_DoesNotTriggerFinding()
        {
            string source =
                "public interface ITest\n" +
                "{\n" +
                "    /// <summary>Interface method documentation.</summary>\n" +
                "    void M();\n" +
                "}\n" +
                "\n" +
                "public class Test : ITest\n" +
                "{\n" +
                "    /// <inheritdoc cref=\"ITest.M\"/>\n" +
                "    public void M() { }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticInheritdocFindingsForSource(source);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that inheritdoc cref to an explicitly implemented interface method
        /// does not trigger DOC711.
        /// </summary>
        [Fact]
        public void MethodReferencingExplicitlyImplementedInterfaceMethod_DoesNotTriggerFinding()
        {
            string source =
                "public interface ITest\n" +
                "{\n" +
                "    /// <summary>Interface method documentation.</summary>\n" +
                "    void M();\n" +
                "}\n" +
                "\n" +
                "public class Test : ITest\n" +
                "{\n" +
                "    /// <inheritdoc cref=\"ITest.M\"/>\n" +
                "    void ITest.M() { }\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticInheritdocFindingsForSource(source);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that inheritdoc cref to an unrelated property triggers DOC711.
        /// </summary>
        [Fact]
        public void PropertyReferencingUnrelatedProperty_IsDetected()
        {
            string source =
                "public class Source\n" +
                "{\n" +
                "    /// <summary>Source property documentation.</summary>\n" +
                "    public int P => 1;\n" +
                "}\n" +
                "\n" +
                "public class Target\n" +
                "{\n" +
                "    /// <inheritdoc cref=\"Source.P\"/>\n" +
                "    public int Q => 2;\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticInheritdocFindingsForSource(source);

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.InheritdocIncompatibleCref.ID, finding.Smell.ID);
            Assert.Equal("inheritdoc", finding.TagName);
        }

        /// <summary>
        /// Ensures that inheritdoc cref to the overridden base property does not trigger DOC711.
        /// </summary>
        [Fact]
        public void PropertyReferencingOverriddenBaseProperty_DoesNotTriggerFinding()
        {
            string source =
                "public class Base\n" +
                "{\n" +
                "    /// <summary>Base property documentation.</summary>\n" +
                "    public virtual int P => 1;\n" +
                "}\n" +
                "\n" +
                "public class Derived : Base\n" +
                "{\n" +
                "    /// <inheritdoc cref=\"Base.P\"/>\n" +
                "    public override int P => 2;\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticInheritdocFindingsForSource(source);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that inheritdoc cref to an implicitly implemented interface property
        /// does not trigger DOC711.
        /// </summary>
        [Fact]
        public void PropertyReferencingImplicitlyImplementedInterfaceProperty_DoesNotTriggerFinding()
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
                "    /// <inheritdoc cref=\"ITest.P\"/>\n" +
                "    public int P => 1;\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticInheritdocFindingsForSource(source);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that inheritdoc cref to an explicitly implemented interface property
        /// does not trigger DOC711.
        /// </summary>
        [Fact]
        public void PropertyReferencingExplicitlyImplementedInterfaceProperty_DoesNotTriggerFinding()
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
                "    /// <inheritdoc cref=\"ITest.P\"/>\n" +
                "    int ITest.P => 1;\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticInheritdocFindingsForSource(source);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that inheritdoc cref to an unrelated event triggers DOC711.
        /// </summary>
        [Fact]
        public void EventReferencingUnrelatedEvent_IsDetected()
        {
            string source =
                "using System;\n" +
                "\n" +
                "public class Source\n" +
                "{\n" +
                "    /// <summary>Source event documentation.</summary>\n" +
                "    public event EventHandler Changed;\n" +
                "}\n" +
                "\n" +
                "public class Target\n" +
                "{\n" +
                "    /// <inheritdoc cref=\"Source.Changed\"/>\n" +
                "    public event EventHandler Updated;\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticInheritdocFindingsForSource(source);

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.InheritdocIncompatibleCref.ID, finding.Smell.ID);
            Assert.Equal("inheritdoc", finding.TagName);
        }

        /// <summary>
        /// Ensures that inheritdoc cref to the overridden base event does not trigger DOC711.
        /// </summary>
        [Fact]
        public void EventReferencingOverriddenBaseEvent_DoesNotTriggerFinding()
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
                "    /// <inheritdoc cref=\"Base.Changed\"/>\n" +
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
        /// Ensures that inheritdoc cref to an implicitly implemented interface event
        /// does not trigger DOC711.
        /// </summary>
        [Fact]
        public void EventReferencingImplicitlyImplementedInterfaceEvent_DoesNotTriggerFinding()
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
                "    /// <inheritdoc cref=\"ITest.Changed\"/>\n" +
                "    public event EventHandler Changed;\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticInheritdocFindingsForSource(source);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that inheritdoc cref to an explicitly implemented interface event
        /// does not trigger DOC711.
        /// </summary>
        [Fact]
        public void EventReferencingExplicitlyImplementedInterfaceEvent_DoesNotTriggerFinding()
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
                "    /// <inheritdoc cref=\"ITest.Changed\"/>\n" +
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

        /// <summary>
        /// Ensures that inheritdoc cref to the direct base type does not trigger DOC711.
        /// </summary>
        [Fact]
        public void TypeReferencingBaseType_DoesNotTriggerFinding()
        {
            string source =
                "/// <summary>Base type documentation.</summary>\n" +
                "public class Base\n" +
                "{\n" +
                "}\n" +
                "\n" +
                "/// <inheritdoc cref=\"Base\"/>\n" +
                "public class Derived : Base\n" +
                "{\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticInheritdocFindingsForSource(source);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that inheritdoc cref to an unrelated type triggers DOC711.
        /// </summary>
        [Fact]
        public void TypeReferencingUnrelatedType_IsDetected()
        {
            string source =
                "/// <summary>Base type documentation.</summary>\n" +
                "public class Base\n" +
                "{\n" +
                "}\n" +
                "\n" +
                "/// <summary>Other type documentation.</summary>\n" +
                "public class Other\n" +
                "{\n" +
                "}\n" +
                "\n" +
                "/// <inheritdoc cref=\"Other\"/>\n" +
                "public class Derived : Base\n" +
                "{\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticInheritdocFindingsForSource(source);

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.InheritdocIncompatibleCref.ID, finding.Smell.ID);
            Assert.Equal("inheritdoc", finding.TagName);
        }

        /// <summary>
        /// Ensures that inheritdoc cref to a direct base interface does not trigger DOC711.
        /// </summary>
        [Fact]
        public void InterfaceReferencingBaseInterface_DoesNotTriggerFinding()
        {
            string source =
                "/// <summary>Base interface documentation.</summary>\n" +
                "public interface IBase\n" +
                "{\n" +
                "}\n" +
                "\n" +
                "/// <inheritdoc cref=\"IBase\"/>\n" +
                "public interface IDerived : IBase\n" +
                "{\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticInheritdocFindingsForSource(source);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that inheritdoc cref to an unrelated interface triggers DOC711.
        /// </summary>
        [Fact]
        public void InterfaceReferencingUnrelatedInterface_IsDetected()
        {
            string source =
                "/// <summary>Base interface documentation.</summary>\n" +
                "public interface IBase\n" +
                "{\n" +
                "}\n" +
                "\n" +
                "/// <summary>Other interface documentation.</summary>\n" +
                "public interface IOther\n" +
                "{\n" +
                "}\n" +
                "\n" +
                "/// <inheritdoc cref=\"IOther\"/>\n" +
                "public interface IDerived : IBase\n" +
                "{\n" +
                "}\n";

            List<Finding> findings =
                CheckAssert.FindSemanticInheritdocFindingsForSource(source);

            Finding finding = Assert.Single(findings);
            Assert.Equal(XmlDocSmells.InheritdocIncompatibleCref.ID, finding.Smell.ID);
            Assert.Equal("inheritdoc", finding.TagName);
        }
    }
}
