using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Execution.Semantic
{
    /// <summary>
    /// Tests symbol resolution from emitted dependency metadata back into the
    /// corresponding source compilation.
    /// </summary>
    public sealed class CrossCompilationSymbolResolverTests
    {
        /// <summary>
        /// Provides members used by the non-generic resolver tests.
        /// </summary>
        private const string MemberSource =
            "using System;\n" +
            "namespace Dependency\n" +
            "{\n" +
            "    public sealed class Sample\n" +
            "    {\n" +
            "        public Sample(int value) { }\n" +
            "        public string NameField = string.Empty;\n" +
            "        public int Value { get; set; }\n" +
            "        public event Action Changed { add { } remove { } }\n" +
            "        public void Execute() { }\n" +
            "        public static Sample operator +(Sample left, Sample right) => left;\n" +
            "    }\n" +
            "}\n";

        /// <summary>
        /// Resolves an ordinary metadata method to its source declaration.
        /// </summary>
        [Fact]
        public void ResolveMethod_OrdinaryMethod_ReturnsSourceMethod()
        {
            (CSharpCompilation sourceCompilation, CSharpCompilation consumerCompilation) =
                CreateCompilationPair(MemberSource);
            INamedTypeSymbol metadataType = GetRequiredType(consumerCompilation, "Dependency.Sample");
            IMethodSymbol metadataMethod = metadataType.GetMembers("Execute").OfType<IMethodSymbol>().Single();
            INamedTypeSymbol sourceType = GetRequiredType(sourceCompilation, "Dependency.Sample");
            IMethodSymbol sourceMethod = sourceType.GetMembers("Execute").OfType<IMethodSymbol>().Single();

            IMethodSymbol? resolvedMethod =
                CrossCompilationSymbolResolver.ResolveMethod(metadataMethod, sourceCompilation);

            AssertSourceMethod(sourceMethod, resolvedMethod);
        }

        /// <summary>
        /// Resolves a metadata constructor to its source declaration.
        /// </summary>
        [Fact]
        public void ResolveMethod_Constructor_ReturnsSourceConstructor()
        {
            (CSharpCompilation sourceCompilation, CSharpCompilation consumerCompilation) =
                CreateCompilationPair(MemberSource);
            INamedTypeSymbol metadataType = GetRequiredType(consumerCompilation, "Dependency.Sample");
            IMethodSymbol metadataConstructor = metadataType.InstanceConstructors.Single(
                constructor => !constructor.IsImplicitlyDeclared);
            INamedTypeSymbol sourceType = GetRequiredType(sourceCompilation, "Dependency.Sample");
            IMethodSymbol sourceConstructor = sourceType.InstanceConstructors.Single(
                constructor => !constructor.IsImplicitlyDeclared);

            IMethodSymbol? resolvedConstructor =
                CrossCompilationSymbolResolver.ResolveMethod(metadataConstructor, sourceCompilation);

            AssertSourceMethod(sourceConstructor, resolvedConstructor);
        }

        /// <summary>
        /// Resolves a metadata operator to its source declaration.
        /// </summary>
        [Fact]
        public void ResolveMethod_Operator_ReturnsSourceOperator()
        {
            (CSharpCompilation sourceCompilation, CSharpCompilation consumerCompilation) =
                CreateCompilationPair(MemberSource);
            INamedTypeSymbol metadataType = GetRequiredType(consumerCompilation, "Dependency.Sample");
            IMethodSymbol metadataOperator = metadataType.GetMembers("op_Addition").OfType<IMethodSymbol>().Single();
            INamedTypeSymbol sourceType = GetRequiredType(sourceCompilation, "Dependency.Sample");
            IMethodSymbol sourceOperator = sourceType.GetMembers("op_Addition").OfType<IMethodSymbol>().Single();

            IMethodSymbol? resolvedOperator =
                CrossCompilationSymbolResolver.ResolveMethod(metadataOperator, sourceCompilation);

            AssertSourceMethod(sourceOperator, resolvedOperator);
        }

        /// <summary>
        /// Resolves metadata property accessors without exchanging getter and setter.
        /// </summary>
        [Fact]
        public void ResolveMethod_PropertyAccessors_ReturnMatchingSourceAccessors()
        {
            (CSharpCompilation sourceCompilation, CSharpCompilation consumerCompilation) =
                CreateCompilationPair(MemberSource);
            IPropertySymbol metadataProperty = GetRequiredType(consumerCompilation, "Dependency.Sample")
                .GetMembers("Value")
                .OfType<IPropertySymbol>()
                .Single();
            IPropertySymbol sourceProperty = GetRequiredType(sourceCompilation, "Dependency.Sample")
                .GetMembers("Value")
                .OfType<IPropertySymbol>()
                .Single();

            IMethodSymbol? resolvedGetter =
                CrossCompilationSymbolResolver.ResolveMethod(metadataProperty.GetMethod!, sourceCompilation);
            IMethodSymbol? resolvedSetter =
                CrossCompilationSymbolResolver.ResolveMethod(metadataProperty.SetMethod!, sourceCompilation);

            AssertSourceMethod(sourceProperty.GetMethod!, resolvedGetter);
            AssertSourceMethod(sourceProperty.SetMethod!, resolvedSetter);
            Assert.Equal(MethodKind.PropertyGet, resolvedGetter!.MethodKind);
            Assert.Equal(MethodKind.PropertySet, resolvedSetter!.MethodKind);
        }

        /// <summary>
        /// Resolves both metadata event accessors to their matching source accessors.
        /// </summary>
        [Fact]
        public void ResolveMethod_EventAccessors_ReturnMatchingSourceAccessors()
        {
            (CSharpCompilation sourceCompilation, CSharpCompilation consumerCompilation) =
                CreateCompilationPair(MemberSource);
            IEventSymbol metadataEvent = GetRequiredType(consumerCompilation, "Dependency.Sample")
                .GetMembers("Changed")
                .OfType<IEventSymbol>()
                .Single();
            IEventSymbol sourceEvent = GetRequiredType(sourceCompilation, "Dependency.Sample")
                .GetMembers("Changed")
                .OfType<IEventSymbol>()
                .Single();

            IMethodSymbol? resolvedAdder =
                CrossCompilationSymbolResolver.ResolveMethod(metadataEvent.AddMethod!, sourceCompilation);
            IMethodSymbol? resolvedRemover =
                CrossCompilationSymbolResolver.ResolveMethod(metadataEvent.RemoveMethod!, sourceCompilation);

            AssertSourceMethod(sourceEvent.AddMethod!, resolvedAdder);
            AssertSourceMethod(sourceEvent.RemoveMethod!, resolvedRemover);
            Assert.Equal(MethodKind.EventAdd, resolvedAdder!.MethodKind);
            Assert.Equal(MethodKind.EventRemove, resolvedRemover!.MethodKind);
        }

        /// <summary>
        /// Resolves a metadata named type to its source declaration.
        /// </summary>
        [Fact]
        public void ResolveNamedType_MetadataType_ReturnsSourceType()
        {
            (CSharpCompilation sourceCompilation, CSharpCompilation consumerCompilation) =
                CreateCompilationPair(MemberSource);
            INamedTypeSymbol metadataType = GetRequiredType(consumerCompilation, "Dependency.Sample");
            INamedTypeSymbol sourceType = GetRequiredType(sourceCompilation, "Dependency.Sample");

            INamedTypeSymbol? resolvedType =
                CrossCompilationSymbolResolver.ResolveNamedType(metadataType, sourceCompilation);

            Assert.NotNull(resolvedType);
            Assert.True(SymbolEqualityComparer.Default.Equals(sourceType, resolvedType));
            Assert.False(resolvedType.DeclaringSyntaxReferences.IsDefaultOrEmpty);
        }

        /// <summary>
        /// Preserves a constructed generic containing type when resolving its method.
        /// </summary>
        [Fact]
        public void ResolveMethod_ConstructedGenericContainingType_PreservesSubstitution()
        {
            const string source =
                "namespace Dependency\n" +
                "{\n" +
                "    public sealed class Container<T>\n" +
                "    {\n" +
                "        public T Execute(T value) => value;\n" +
                "    }\n" +
                "}\n";
            (CSharpCompilation sourceCompilation, CSharpCompilation consumerCompilation) =
                CreateCompilationPair(source);
            INamedTypeSymbol metadataDefinition =
                GetRequiredType(consumerCompilation, "Dependency.Container`1");
            INamedTypeSymbol constructedMetadataType = metadataDefinition.Construct(
                consumerCompilation.GetSpecialType(SpecialType.System_String));
            IMethodSymbol metadataMethod = constructedMetadataType
                .GetMembers("Execute")
                .OfType<IMethodSymbol>()
                .Single();

            IMethodSymbol? resolvedMethod =
                CrossCompilationSymbolResolver.ResolveMethod(metadataMethod, sourceCompilation);

            Assert.NotNull(resolvedMethod);
            Assert.False(resolvedMethod.DeclaringSyntaxReferences.IsDefaultOrEmpty);
            Assert.Equal(SpecialType.System_String, resolvedMethod.ReturnType.SpecialType);
            Assert.Equal(SpecialType.System_String, resolvedMethod.Parameters.Single().Type.SpecialType);
            Assert.Equal(
                SpecialType.System_String,
                resolvedMethod.ContainingType.TypeArguments.Single().SpecialType);
            Assert.False(SymbolEqualityComparer.Default.Equals(
                resolvedMethod,
                resolvedMethod.OriginalDefinition));
        }

        /// <summary>
        /// Rejects same-shaped symbols from a different assembly identity.
        /// </summary>
        [Fact]
        public void ResolveSymbols_DifferentAssemblyIdentity_ReturnNull()
        {
            (CSharpCompilation sourceCompilation, CSharpCompilation consumerCompilation) =
                CreateCompilationPair(MemberSource);
            INamedTypeSymbol metadataType = GetRequiredType(consumerCompilation, "Dependency.Sample");
            IMethodSymbol metadataMethod = metadataType.GetMembers("Execute").OfType<IMethodSymbol>().Single();
            string mismatchedSource =
                "using System;\n" +
                "using System.Reflection;\n" +
                "[assembly: AssemblyVersion(\"2.0.0.0\")]\n" +
                MemberSource.Substring("using System;\n".Length);
            CSharpCompilation mismatchedCompilation = CreateSourceCompilation(
                sourceCompilation.AssemblyName!,
                mismatchedSource);

            Assert.Equal(
                sourceCompilation.Assembly.Identity.Name,
                mismatchedCompilation.Assembly.Identity.Name);
            Assert.NotEqual(
                sourceCompilation.Assembly.Identity,
                mismatchedCompilation.Assembly.Identity);

            Assert.Null(CrossCompilationSymbolResolver.ResolveNamedType(
                metadataType,
                mismatchedCompilation));
            Assert.Null(CrossCompilationSymbolResolver.ResolveMethod(
                metadataMethod,
                mismatchedCompilation));
        }

        /// <summary>
        /// Returns null when the destination assembly contains no matching declaration.
        /// </summary>
        [Fact]
        public void ResolveSymbols_MissingDeclarations_ReturnNull()
        {
            (CSharpCompilation sourceCompilation, CSharpCompilation consumerCompilation) =
                CreateCompilationPair(MemberSource);
            INamedTypeSymbol metadataType = GetRequiredType(consumerCompilation, "Dependency.Sample");
            IMethodSymbol metadataMethod = metadataType.GetMembers("Execute").OfType<IMethodSymbol>().Single();
            CSharpCompilation incompleteCompilation = CreateSourceCompilation(
                sourceCompilation.AssemblyName!,
                "namespace Dependency { public sealed class Other { } }");

            Assert.Null(CrossCompilationSymbolResolver.ResolveNamedType(
                metadataType,
                incompleteCompilation));
            Assert.Null(CrossCompilationSymbolResolver.ResolveMethod(
                metadataMethod,
                incompleteCompilation));
        }

        /// <summary>
        /// Resolves stable metadata property and field symbols to their source
        /// declarations.
        /// </summary>
        [Fact]
        public void ResolveStableMember_PropertyAndField_ReturnSourceMembers()
        {
            (CSharpCompilation sourceCompilation, CSharpCompilation consumerCompilation) =
                CreateCompilationPair(MemberSource);
            INamedTypeSymbol metadataType = GetRequiredType(consumerCompilation, "Dependency.Sample");

            foreach (string memberName in new[] { "Value", "NameField" })
            {
                ISymbol metadataMember = metadataType.GetMembers(memberName).Single();
                ISymbol sourceMember = GetRequiredType(sourceCompilation, "Dependency.Sample")
                    .GetMembers(memberName)
                    .Single();
                ISymbol? resolvedMember = CrossCompilationSymbolResolver.ResolveStableMember(
                    metadataMember, sourceCompilation);

                Assert.NotNull(resolvedMember);
                Assert.True(SymbolEqualityComparer.Default.Equals(sourceMember, resolvedMember));
                Assert.False(resolvedMember.DeclaringSyntaxReferences.IsDefaultOrEmpty);
            }
        }

        /// <summary>
        /// Rejects parameter symbols because call-context member rebinding is
        /// restricted to stable property and field identities.
        /// </summary>
        [Fact]
        public void ResolveStableMember_Parameter_ReturnsNull()
        {
            (CSharpCompilation sourceCompilation, CSharpCompilation consumerCompilation) =
                CreateCompilationPair(MemberSource);
            IMethodSymbol metadataConstructor = GetRequiredType(consumerCompilation, "Dependency.Sample")
                .InstanceConstructors
                .Single(constructor => !constructor.IsImplicitlyDeclared);

            Assert.Null(CrossCompilationSymbolResolver.ResolveStableMember(
                metadataConstructor.Parameters.Single(), sourceCompilation));
        }

        /// <summary>
        /// Rebinds ordinal value facts and stable member facts to the source
        /// callable without relying on parameter names.
        /// </summary>
        [Fact]
        public void RebindCallable_ValueAndStableMemberFacts_UseSourceSymbols()
        {
            const string source =
                "namespace Dependency { " +
                "public sealed class Options { public object? Name { get; } } " +
                "public static class Service { public static void Execute(Options renamed) { } } }";
            (CSharpCompilation sourceCompilation, CSharpCompilation consumerCompilation) =
                CreateCompilationPair(source);
            IMethodSymbol metadataMethod = GetRequiredType(consumerCompilation, "Dependency.Service")
                .GetMembers("Execute")
                .OfType<IMethodSymbol>()
                .Single();
            IMethodSymbol sourceMethod = GetRequiredType(sourceCompilation, "Dependency.Service")
                .GetMembers("Execute")
                .OfType<IMethodSymbol>()
                .Single();
            IPropertySymbol metadataProperty = metadataMethod.Parameters[0].Type
                .GetMembers("Name")
                .OfType<IPropertySymbol>()
                .Single();
            IPropertySymbol sourceProperty = sourceMethod.Parameters[0].Type
                .GetMembers("Name")
                .OfType<IPropertySymbol>()
                .Single();
            ExceptionFlowCallContext metadataContext = new(
                metadataMethod,
                new[]
                {
                    new KeyValuePair<int, ExceptionFlowValueFacts>(
                        0,
                        ExceptionFlowValueFacts.NonNull | ExceptionFlowValueFacts.NonNullElements)
                },
                new[] { new KeyValuePair<int, ISymbol>(0, metadataProperty) });

            ExceptionFlowCallContext sourceContext = metadataContext.RebindCallable(
                sourceMethod,
                member => CrossCompilationSymbolResolver.ResolveStableMember(member, sourceCompilation));

            Assert.True(sourceContext.GetParameterFacts(sourceMethod.Parameters[0]).ContainsAll(
                ExceptionFlowValueFacts.NonNull | ExceptionFlowValueFacts.NonNullElements));
            Assert.True(sourceContext.IsParameterMemberKnownNonNull(
                sourceMethod.Parameters[0], sourceProperty));
            Assert.False(sourceContext.IsParameterMemberKnownNonNull(
                sourceMethod.Parameters[0], metadataProperty));
        }

        /// <summary>
        /// Creates a source compilation and a consumer compilation referencing
        /// its emitted metadata image.
        /// </summary>
        /// <param name="source">The dependency source.</param>
        /// <returns>The source and consumer compilations.</returns>
        private static (CSharpCompilation SourceCompilation, CSharpCompilation ConsumerCompilation) CreateCompilationPair(
            string source)
        {
            CSharpCompilation sourceCompilation = CreateSourceCompilation(
                "ResolverDependency",
                source);
            using MemoryStream imageStream = new();
            EmitResult emitResult = sourceCompilation.Emit(imageStream);

            Assert.True(
                emitResult.Success,
                string.Join(Environment.NewLine, emitResult.Diagnostics));

            MetadataReference dependencyReference = MetadataReference.CreateFromImage(
                ImmutableArray.CreateRange(imageStream.ToArray()));
            CSharpCompilation consumerCompilation = CSharpCompilation.Create(
                "ResolverConsumer",
                references: MetadataReferences.Default.Append(dependencyReference),
                options: CreateCompilationOptions());

            return (sourceCompilation, consumerCompilation);
        }

        /// <summary>
        /// Creates one source-backed dependency compilation.
        /// </summary>
        /// <param name="assemblyName">The assembly identity name.</param>
        /// <param name="source">The dependency source.</param>
        /// <returns>The created compilation.</returns>
        private static CSharpCompilation CreateSourceCompilation(string assemblyName, string source)
        {
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, path: "Dependency.cs");

            return CSharpCompilation.Create(
                assemblyName,
                new[] { syntaxTree },
                MetadataReferences.Default,
                CreateCompilationOptions());
        }

        /// <summary>
        /// Creates compilation options shared by source and consumer compilations.
        /// </summary>
        /// <returns>The library compilation options.</returns>
        private static CSharpCompilationOptions CreateCompilationOptions()
        {
            return new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable);
        }

        /// <summary>
        /// Gets a required named type from a compilation.
        /// </summary>
        /// <param name="compilation">The compilation to inspect.</param>
        /// <param name="metadataName">The fully qualified metadata name.</param>
        /// <returns>The resolved named type.</returns>
        private static INamedTypeSymbol GetRequiredType(
            Compilation compilation,
            string metadataName)
        {
            INamedTypeSymbol? typeSymbol = compilation.GetTypeByMetadataName(metadataName);
            Assert.NotNull(typeSymbol);
            return typeSymbol;
        }

        /// <summary>
        /// Verifies that a resolved method is the expected source method.
        /// </summary>
        /// <param name="expectedMethod">The expected source method.</param>
        /// <param name="actualMethod">The resolved method.</param>
        private static void AssertSourceMethod(
            IMethodSymbol expectedMethod,
            IMethodSymbol? actualMethod)
        {
            Assert.NotNull(actualMethod);
            Assert.True(SymbolEqualityComparer.Default.Equals(expectedMethod, actualMethod));
            Assert.False(actualMethod.DeclaringSyntaxReferences.IsDefaultOrEmpty);
        }
    }
}
