using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Characterizes the method-shape decision used before runtime target
    /// expansion.
    /// </summary>
    public sealed class ExceptionFlowRuntimeDispatchClassifierTests
    {
        /// <summary>
        /// Distinguishes direct, interface, virtual, overridden, and sealed
        /// method dispatch without consulting compilation state.
        /// </summary>
        [Fact]
        public void RequiresRuntimeDispatch_ClassifiesMethodShapes()
        {
            const string source =
                "interface IService { void InterfaceMethod(); }\n" +
                "abstract class Base\n" +
                "{\n" +
                "    public void Direct() { }\n" +
                "    public static void Static() { }\n" +
                "    public abstract void Abstract();\n" +
                "    public virtual void Virtual() { }\n" +
                "}\n" +
                "class Derived : Base\n" +
                "{\n" +
                "    public override void Abstract() { }\n" +
                "    public override void Virtual() { }\n" +
                "}\n" +
                "class Final : Derived\n" +
                "{\n" +
                "    public sealed override void Virtual() { }\n" +
                "}\n";

            CSharpCompilation compilation = CSharpCompilation.Create(
                "RuntimeDispatchClassification",
                new[] { CSharpSyntaxTree.ParseText(source) },
                MetadataReferences.Default,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            INamedTypeSymbol service = GetRequiredType(compilation, "IService");
            INamedTypeSymbol baseType = GetRequiredType(compilation, "Base");
            INamedTypeSymbol derived = GetRequiredType(compilation, "Derived");
            INamedTypeSymbol finalType = GetRequiredType(compilation, "Final");

            Assert.False(RequiresDispatch(baseType, "Direct"));
            Assert.False(RequiresDispatch(baseType, "Static"));
            Assert.True(RequiresDispatch(service, "InterfaceMethod"));
            Assert.True(RequiresDispatch(baseType, "Abstract"));
            Assert.True(RequiresDispatch(baseType, "Virtual"));
            Assert.True(RequiresDispatch(derived, "Abstract"));
            Assert.True(RequiresDispatch(derived, "Virtual"));
            Assert.False(RequiresDispatch(finalType, "Virtual"));
        }

        private static bool RequiresDispatch(
            INamedTypeSymbol type,
            string methodName)
        {
            IMethodSymbol method = type.GetMembers(methodName)
                .OfType<IMethodSymbol>()
                .Single();

            return ExceptionFlowRuntimeDispatchClassifier.RequiresRuntimeDispatch(
                method);
        }

        private static INamedTypeSymbol GetRequiredType(
            Compilation compilation,
            string metadataName)
        {
            INamedTypeSymbol? type = compilation.GetTypeByMetadataName(metadataName);
            Assert.NotNull(type);
            return type;
        }
    }
}
