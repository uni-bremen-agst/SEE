using System.Reflection;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.Canonical;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exceptions
{
    /// <summary>
    /// Tests Roslyn-independent canonical identity creation and exact rebinding.
    /// </summary>
    public sealed class CanonicalIdentityTests
    {
        /// <summary>
        /// Provides representative type and callable declarations.
        /// </summary>
        private const string IdentitySource =
            "namespace IdentityFixture\n" +
            "{\n" +
            "    public interface IService { void Execute(int value); }\n" +
            "    public sealed class Outer<T>\n" +
            "    {\n" +
            "        public sealed class Inner<U> { }\n" +
            "    }\n" +
            "    public sealed class Service : IService\n" +
            "    {\n" +
            "        public string NameField = string.Empty;\n" +
            "        public string Name { get; set; } = string.Empty;\n" +
            "        public Service() { }\n" +
            "        public void Execute(int value) { }\n" +
            "        void IService.Execute(int value) { }\n" +
            "        public void Overload(int value) { }\n" +
            "        public void Overload(string value) { }\n" +
            "        public T Echo<T>(T value) => value;\n" +
            "        public void ByRef(ref int value) { }\n" +
            "        public void ByOut(out string value) { value = string.Empty; }\n" +
            "        public void ByIn(in long value) { }\n" +
            "    }\n" +
            "}\n";

        /// <summary>
        /// Creates equal value identities for the same Roslyn assembly input.
        /// </summary>
        [Fact]
        public void AssemblyIdentity_SameInput_Equals()
        {
            CSharpCompilation compilation = CreateCompilation("IdentityAssembly", IdentitySource);

            CanonicalAssemblyIdentity first =
                RoslynCanonicalIdentityFactory.CreateAssemblyIdentity(compilation.Assembly);
            CanonicalAssemblyIdentity second =
                RoslynCanonicalIdentityFactory.CreateAssemblyIdentity(compilation.Assembly.Identity);

            Assert.Equal(first, second);
            Assert.Equal(first.GetHashCode(), second.GetHashCode());
        }

        /// <summary>
        /// Distinguishes assembly versions.
        /// </summary>
        [Fact]
        public void AssemblyIdentity_DifferentVersion_NotEqual()
        {
            CSharpCompilation firstCompilation = CreateCompilation(
                "VersionedAssembly",
                "using System.Reflection; [assembly: AssemblyVersion(\"1.0.0.0\")] class C { }");
            CSharpCompilation secondCompilation = CreateCompilation(
                "VersionedAssembly",
                "using System.Reflection; [assembly: AssemblyVersion(\"2.0.0.0\")] class C { }");

            CanonicalAssemblyIdentity first =
                RoslynCanonicalIdentityFactory.CreateAssemblyIdentity(firstCompilation.Assembly);
            CanonicalAssemblyIdentity second =
                RoslynCanonicalIdentityFactory.CreateAssemblyIdentity(secondCompilation.Assembly);

            Assert.NotEqual(first, second);
        }

        /// <summary>
        /// Distinguishes otherwise equal assembly identities with different keys.
        /// </summary>
        [Fact]
        public void AssemblyIdentity_DifferentKey_NotEqual()
        {
            CanonicalAssemblyIdentity first = CreateCanonicalAssembly("0011223344556677");
            CanonicalAssemblyIdentity second = CreateCanonicalAssembly("8899AABBCCDDEEFF");

            Assert.NotEqual(first, second);
        }

        /// <summary>
        /// Distinguishes module MVIDs while preserving other module fields.
        /// </summary>
        [Fact]
        public void ModuleIdentity_DifferentMvid_NotEqual()
        {
            CanonicalAssemblyIdentity assembly = CreateCanonicalAssembly(string.Empty);
            CanonicalModuleIdentity first = new(assembly, "Module.dll", 0, Guid.NewGuid());
            CanonicalModuleIdentity second = new(assembly, "Module.dll", 0, Guid.NewGuid());

            Assert.NotEqual(first, second);
        }

        /// <summary>
        /// Produces equal nested and generic identities in equivalent compilations.
        /// </summary>
        [Fact]
        public void TypeIdentity_SameAcrossEquivalentCompilations_Equals()
        {
            CSharpCompilation firstCompilation = CreateCompilation("Equivalent", IdentitySource);
            CSharpCompilation secondCompilation = CreateCompilation("Equivalent", IdentitySource);
            INamedTypeSymbol firstDefinition = GetRequiredType(
                firstCompilation,
                "IdentityFixture.Outer`1+Inner`1");
            INamedTypeSymbol secondDefinition = GetRequiredType(
                secondCompilation,
                "IdentityFixture.Outer`1+Inner`1");
            INamedTypeSymbol firstConstructed = firstDefinition.Construct(
                firstCompilation.GetSpecialType(SpecialType.System_String));
            INamedTypeSymbol secondConstructed = secondDefinition.Construct(
                secondCompilation.GetSpecialType(SpecialType.System_String));

            CanonicalTypeIdentity first =
                RoslynCanonicalIdentityFactory.CreateTypeIdentity(firstConstructed);
            CanonicalTypeIdentity second =
                RoslynCanonicalIdentityFactory.CreateTypeIdentity(secondConstructed);

            Assert.Equal(first, second);
            Assert.NotNull(first.ContainingType);
            Assert.Single(first.TypeArguments);
        }

        /// <summary>
        /// Distinguishes equal metadata type names from different assemblies.
        /// </summary>
        [Fact]
        public void TypeIdentity_SameNameDifferentAssembly_NotEqual()
        {
            CSharpCompilation firstCompilation = CreateCompilation(
                "FirstAssembly",
                "namespace Shared { public sealed class Value { } }");
            CSharpCompilation secondCompilation = CreateCompilation(
                "SecondAssembly",
                "namespace Shared { public sealed class Value { } }");

            CanonicalTypeIdentity first = RoslynCanonicalIdentityFactory.CreateTypeIdentity(
                GetRequiredType(firstCompilation, "Shared.Value"));
            CanonicalTypeIdentity second = RoslynCanonicalIdentityFactory.CreateTypeIdentity(
                GetRequiredType(secondCompilation, "Shared.Value"));

            Assert.NotEqual(first, second);
        }

        /// <summary>
        /// Preserves array rank, element identity, and type-parameter ownership.
        /// </summary>
        [Fact]
        public void TypeIdentity_ArrayAndTypeParameter_PreserveStructure()
        {
            CSharpCompilation compilation = CreateCompilation("Structures", IdentitySource);
            INamedTypeSymbol service = GetRequiredType(compilation, "IdentityFixture.Service");
            IMethodSymbol genericMethod = service.GetMembers("Echo").OfType<IMethodSymbol>().Single();
            IArrayTypeSymbol array = compilation.CreateArrayTypeSymbol(
                genericMethod.TypeParameters[0],
                rank: 2);

            CanonicalTypeIdentity identity =
                RoslynCanonicalIdentityFactory.CreateTypeIdentity(array);

            Assert.Equal(CanonicalTypeIdentityKind.Array, identity.Kind);
            Assert.Equal(2, identity.ArrayRank);
            Assert.NotNull(identity.ElementType);
            Assert.Equal(CanonicalTypeIdentityKind.TypeParameter, identity.ElementType!.Kind);
            Assert.Equal(CanonicalTypeParameterScope.Method, identity.ElementType.TypeParameterScope);
            Assert.Equal(0, identity.ElementType.TypeParameterOrdinal);
        }

        /// <summary>
        /// Distinguishes overload and managed-reference signatures.
        /// </summary>
        [Fact]
        public void CallableIdentity_OverloadsAndRefKinds_AreDistinct()
        {
            CSharpCompilation compilation = CreateCompilation("Callables", IdentitySource);
            INamedTypeSymbol service = GetRequiredType(compilation, "IdentityFixture.Service");
            IMethodSymbol[] overloads = service.GetMembers("Overload").OfType<IMethodSymbol>().ToArray();
            IMethodSymbol byRef = service.GetMembers("ByRef").OfType<IMethodSymbol>().Single();
            IMethodSymbol byOut = service.GetMembers("ByOut").OfType<IMethodSymbol>().Single();
            IMethodSymbol byIn = service.GetMembers("ByIn").OfType<IMethodSymbol>().Single();

            CanonicalCallableIdentity firstOverload =
                RoslynCanonicalIdentityFactory.CreateCallableIdentity(overloads[0]);
            CanonicalCallableIdentity secondOverload =
                RoslynCanonicalIdentityFactory.CreateCallableIdentity(overloads[1]);
            CanonicalCallableIdentity refIdentity =
                RoslynCanonicalIdentityFactory.CreateCallableIdentity(byRef);
            CanonicalCallableIdentity outIdentity =
                RoslynCanonicalIdentityFactory.CreateCallableIdentity(byOut);
            CanonicalCallableIdentity inIdentity =
                RoslynCanonicalIdentityFactory.CreateCallableIdentity(byIn);

            Assert.NotEqual(firstOverload, secondOverload);
            Assert.Equal(CanonicalRefKind.Ref, Assert.Single(refIdentity.Parameters).RefKind);
            Assert.Equal(CanonicalRefKind.Out, Assert.Single(outIdentity.Parameters).RefKind);
            Assert.Equal(CanonicalRefKind.In, Assert.Single(inIdentity.Parameters).RefKind);
        }

        /// <summary>
        /// Preserves constructors, property accessors, and generic substitutions.
        /// </summary>
        [Fact]
        public void CallableIdentity_ConstructorAccessorAndGenericMethod_PreserveKinds()
        {
            CSharpCompilation compilation = CreateCompilation("CallableKinds", IdentitySource);
            INamedTypeSymbol service = GetRequiredType(compilation, "IdentityFixture.Service");
            IMethodSymbol constructor = service.InstanceConstructors.Single(
                candidate => !candidate.IsImplicitlyDeclared);
            IPropertySymbol property = service.GetMembers("Name").OfType<IPropertySymbol>().Single();
            IMethodSymbol genericDefinition = service.GetMembers("Echo").OfType<IMethodSymbol>().Single();
            IMethodSymbol constructedMethod = genericDefinition.Construct(
                compilation.GetSpecialType(SpecialType.System_String));

            CanonicalCallableIdentity constructorIdentity =
                RoslynCanonicalIdentityFactory.CreateCallableIdentity(constructor);
            CanonicalCallableIdentity getterIdentity =
                RoslynCanonicalIdentityFactory.CreateCallableIdentity(property.GetMethod!);
            CanonicalCallableIdentity setterIdentity =
                RoslynCanonicalIdentityFactory.CreateCallableIdentity(property.SetMethod!);
            CanonicalCallableIdentity genericIdentity =
                RoslynCanonicalIdentityFactory.CreateCallableIdentity(constructedMethod);

            Assert.Equal(CanonicalCallableKind.Constructor, constructorIdentity.Kind);
            Assert.Equal(CanonicalCallableKind.PropertyGetter, getterIdentity.Kind);
            Assert.Equal(CanonicalCallableKind.PropertySetter, setterIdentity.Kind);
            Assert.Single(genericIdentity.TypeArguments);
        }

        /// <summary>
        /// Retains explicit interface implementation identity.
        /// </summary>
        [Fact]
        public void CallableIdentity_ExplicitInterface_PreservesImplementedCallable()
        {
            CSharpCompilation compilation = CreateCompilation("ExplicitInterface", IdentitySource);
            INamedTypeSymbol service = GetRequiredType(compilation, "IdentityFixture.Service");
            IMethodSymbol explicitMethod = service.GetMembers()
                .OfType<IMethodSymbol>()
                .Single(method => method.ExplicitInterfaceImplementations.Length == 1);

            CanonicalCallableIdentity identity =
                RoslynCanonicalIdentityFactory.CreateCallableIdentity(explicitMethod);

            Assert.Single(identity.ExplicitInterfaceImplementations);
        }

        /// <summary>
        /// Produces equal callable identities across equivalent compilations.
        /// </summary>
        [Fact]
        public void CallableIdentity_CrossCompilationEquivalent_Equals()
        {
            CSharpCompilation firstCompilation = CreateCompilation("EquivalentCalls", IdentitySource);
            CSharpCompilation secondCompilation = CreateCompilation("EquivalentCalls", IdentitySource);
            IMethodSymbol firstMethod = GetRequiredType(firstCompilation, "IdentityFixture.Service")
                .GetMembers("Echo")
                .OfType<IMethodSymbol>()
                .Single();
            IMethodSymbol secondMethod = GetRequiredType(secondCompilation, "IdentityFixture.Service")
                .GetMembers("Echo")
                .OfType<IMethodSymbol>()
                .Single();

            CanonicalCallableIdentity first =
                RoslynCanonicalIdentityFactory.CreateCallableIdentity(firstMethod);
            CanonicalCallableIdentity second =
                RoslynCanonicalIdentityFactory.CreateCallableIdentity(secondMethod);

            Assert.Equal(first, second);
        }

        /// <summary>
        /// Produces equal field and property identities across equivalent compilations.
        /// </summary>
        [Fact]
        public void StableMemberIdentity_CrossCompilationEquivalent_Equals()
        {
            CSharpCompilation firstCompilation = CreateCompilation("EquivalentMembers", IdentitySource);
            CSharpCompilation secondCompilation = CreateCompilation("EquivalentMembers", IdentitySource);
            INamedTypeSymbol firstType = GetRequiredType(firstCompilation, "IdentityFixture.Service");
            INamedTypeSymbol secondType = GetRequiredType(secondCompilation, "IdentityFixture.Service");

            foreach (string memberName in new[] { "Name", "NameField" })
            {
                CanonicalStableMemberIdentity first =
                    RoslynCanonicalIdentityFactory.CreateStableMemberIdentity(
                        firstType.GetMembers(memberName).Single());
                CanonicalStableMemberIdentity second =
                    RoslynCanonicalIdentityFactory.CreateStableMemberIdentity(
                        secondType.GetMembers(memberName).Single());

                Assert.Equal(first, second);
            }
        }

        /// <summary>
        /// Resolves types, callables, and stable members only through exact identities.
        /// </summary>
        [Fact]
        public void Resolver_ExactCanonicalIdentities_ReturnOwnedSymbols()
        {
            CSharpCompilation sourceCompilation = CreateCompilation("Resolver", IdentitySource);
            CSharpCompilation destinationCompilation = CreateCompilation("Resolver", IdentitySource);
            INamedTypeSymbol sourceType = GetRequiredType(sourceCompilation, "IdentityFixture.Service");
            IMethodSymbol sourceMethod = sourceType.GetMembers("Echo").OfType<IMethodSymbol>().Single();
            ISymbol sourceMember = sourceType.GetMembers("Name").Single();
            RoslynCanonicalIdentityResolver resolver = new(destinationCompilation);

            INamedTypeSymbol? resolvedType = resolver.ResolveType(
                RoslynCanonicalIdentityFactory.CreateTypeIdentity(sourceType)) as INamedTypeSymbol;
            IMethodSymbol? resolvedMethod = resolver.ResolveCallable(
                RoslynCanonicalIdentityFactory.CreateCallableIdentity(sourceMethod));
            ISymbol? resolvedMember = resolver.ResolveStableMember(
                RoslynCanonicalIdentityFactory.CreateStableMemberIdentity(sourceMember));

            Assert.NotNull(resolvedType);
            Assert.NotNull(resolvedMethod);
            Assert.NotNull(resolvedMember);
            Assert.True(SymbolEqualityComparer.Default.Equals(
                GetRequiredType(destinationCompilation, "IdentityFixture.Service"),
                resolvedType));
        }

        /// <summary>
        /// Fails closed when the destination assembly identity differs.
        /// </summary>
        [Fact]
        public void Resolver_DifferentAssembly_ReturnsNull()
        {
            CSharpCompilation sourceCompilation = CreateCompilation("SourceAssembly", IdentitySource);
            CSharpCompilation destinationCompilation = CreateCompilation(
                "DestinationAssembly",
                IdentitySource);
            INamedTypeSymbol sourceType = GetRequiredType(sourceCompilation, "IdentityFixture.Service");
            IMethodSymbol sourceMethod = sourceType.GetMembers("Execute")
                .OfType<IMethodSymbol>()
                .First(method => method.ExplicitInterfaceImplementations.IsEmpty);
            RoslynCanonicalIdentityResolver resolver = new(destinationCompilation);

            Assert.Null(resolver.ResolveType(
                RoslynCanonicalIdentityFactory.CreateTypeIdentity(sourceType)));
            Assert.Null(resolver.ResolveCallable(
                RoslynCanonicalIdentityFactory.CreateCallableIdentity(sourceMethod)));
        }

        /// <summary>
        /// Preserves and exactly resolves type declarations used as graph roots.
        /// </summary>
        [Fact]
        public void CallableIdentity_TypeDeclarationRoot_RoundtripsExactly()
        {
            CSharpCompilation compilation = CreateCompilation(
                "TypeRoot",
                "public sealed class RootType { }");
            INamedTypeSymbol type = GetRequiredType(compilation, "RootType");
            CanonicalCallableIdentity identity =
                RoslynCanonicalIdentityFactory.CreateCallableIdentity(type);

            Assert.Equal(CanonicalCallableKind.TypeDeclaration, identity.Kind);
            Assert.True(SymbolEqualityComparer.Default.Equals(
                type,
                new RoslynCanonicalIdentityResolver(compilation)
                    .ResolveCallableSymbol(identity)));
        }

        /// <summary>
        /// Preserves and exactly resolves fields used as graph roots.
        /// </summary>
        [Fact]
        public void CallableIdentity_FieldDeclarationRoot_RoundtripsExactly()
        {
            CSharpCompilation compilation = CreateCompilation(
                "FieldRoot",
                "public sealed class RootType { public string Value = string.Empty; }");
            INamedTypeSymbol type = GetRequiredType(compilation, "RootType");
            IFieldSymbol field = type.GetMembers("Value").OfType<IFieldSymbol>().Single();
            CanonicalCallableIdentity identity =
                RoslynCanonicalIdentityFactory.CreateCallableIdentity(field);

            Assert.Equal(CanonicalCallableKind.FieldDeclaration, identity.Kind);
            Assert.True(SymbolEqualityComparer.Default.Equals(
                field,
                new RoslynCanonicalIdentityResolver(compilation)
                    .ResolveCallableSymbol(identity)));
        }

        /// <summary>
        /// Preserves and exactly resolves namespaces used as graph roots.
        /// </summary>
        [Fact]
        public void CallableIdentity_NamespaceDeclarationRoot_RoundtripsExactly()
        {
            CSharpCompilation compilation = CreateCompilation(
                "NamespaceRoot",
                "namespace Outer.Inner { public sealed class Value { } }");
            INamespaceSymbol namespaceSymbol = compilation.Assembly.GlobalNamespace
                .GetNamespaceMembers().Single(candidate => candidate.Name == "Outer")
                .GetNamespaceMembers().Single(candidate => candidate.Name == "Inner");
            CanonicalCallableIdentity identity =
                RoslynCanonicalIdentityFactory.CreateCallableIdentity(namespaceSymbol);

            Assert.Equal(CanonicalCallableKind.NamespaceDeclaration, identity.Kind);
            Assert.True(SymbolEqualityComparer.Default.Equals(
                namespaceSymbol,
                new RoslynCanonicalIdentityResolver(compilation)
                    .ResolveCallableSymbol(identity)));
        }

        /// <summary>
        /// Preserves canonical identities through JSON serialization.
        /// </summary>
        [Fact]
        public void CanonicalIdentities_JsonRoundtrip_PreserveEquality()
        {
            CSharpCompilation compilation = CreateCompilation("Serialization", IdentitySource);
            INamedTypeSymbol service = GetRequiredType(compilation, "IdentityFixture.Service");
            IMethodSymbol method = service.GetMembers("Echo").OfType<IMethodSymbol>().Single();
            CanonicalCallableIdentity callable =
                RoslynCanonicalIdentityFactory.CreateCallableIdentity(method);
            CanonicalStableMemberIdentity member =
                RoslynCanonicalIdentityFactory.CreateStableMemberIdentity(
                    service.GetMembers("Name").Single());

            CanonicalCallableIdentity? callableRoundtrip = JsonSerializer.Deserialize<CanonicalCallableIdentity>(
                JsonSerializer.Serialize(callable));
            CanonicalStableMemberIdentity? memberRoundtrip =
                JsonSerializer.Deserialize<CanonicalStableMemberIdentity>(
                    JsonSerializer.Serialize(member));

            Assert.Equal(callable, callableRoundtrip);
            Assert.Equal(member, memberRoundtrip);
        }

        /// <summary>
        /// Creates one canonical assembly identity for value-semantics tests.
        /// </summary>
        private static CanonicalAssemblyIdentity CreateCanonicalAssembly(string key)
        {
            return new CanonicalAssemblyIdentity(
                "Canonical.Assembly",
                1,
                2,
                3,
                4,
                string.Empty,
                key,
                hasPublicKey: false,
                isRetargetable: false,
                CanonicalAssemblyContentType.Default);
        }

        /// <summary>
        /// Creates a source-backed test compilation.
        /// </summary>
        private static CSharpCompilation CreateCompilation(string assemblyName, string source)
        {
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(
                source,
                path: "CanonicalIdentityFixture.cs");

            return CSharpCompilation.Create(
                assemblyName,
                new[] { syntaxTree },
                MetadataReferences.Default,
                new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    nullableContextOptions: NullableContextOptions.Enable));
        }

        /// <summary>
        /// Gets a required named type.
        /// </summary>
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
