using System.Reflection;
using System.Reflection.Emit;
using System.Text.Json;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Protects the audited contextual evaluator ownership and dependency boundary.
    /// </summary>
    public sealed class ExceptionFlowContextualFactEvaluatorDependencyTests
    {
        private const BindingFlags Declared = BindingFlags.Public | BindingFlags.NonPublic
            | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        /// <summary>
        /// Matches the complete, overload-sensitive A5A move set and forbids old-owner duplicates.
        /// </summary>
        [Fact]
        public void AuditedScc_HasExactlyTheNewOwnerAndFourInternalEntries()
        {
            using JsonDocument audit = JsonDocument.Parse(File.ReadAllText(Path.Combine(
                GetRoot(), "Evaluation", "P5O2A5A-contextual-evaluator-boundary-audit.json")));
            string[] expected = audit.RootElement.GetProperty("Scc").GetProperty("Members")
                .EnumerateArray().Select(member => member.GetProperty("Id").GetString()!
                    .Replace("ExceptionFlowAnalyzer", "ExceptionFlowContextualFactEvaluator", StringComparison.Ordinal)
                    .Replace("?", "", StringComparison.Ordinal)).Order(StringComparer.Ordinal).ToArray();
            Type evaluator = typeof(ExceptionFlowContextualFactEvaluator);
            MethodInfo[] methods = evaluator.GetMethods(Declared);
            MethodInfo[] seeds = methods.Where(method =>
                method.Name == "CreateCallContext" && method.GetParameters().Length == 4
                || method.Name == "IsDefinitelyNonNull" && method.GetParameters().Length == 3).ToArray();

            Assert.Equal(63, expected.Length);
            Assert.Equal(2, seeds.Length);
            Assert.All(seeds, method => Assert.True(method.IsAssembly));
            Assert.Equal(expected, methods.Except(seeds).Select(Signature).Order(StringComparer.Ordinal).ToArray());
            Assert.Equal(6, methods.Count(method => method.IsAssembly));
            Assert.All(methods, method => Assert.True(method.IsAssembly || method.IsPrivate));
            Assert.All(methods, method => Assert.True(method.IsStatic));
            Assert.True(evaluator.IsAbstract && evaluator.IsSealed && evaluator.IsNotPublic);
            Assert.Empty(evaluator.GetInterfaces());
            Assert.DoesNotContain(typeof(ExceptionFlowAnalyzer).GetMethods(Declared), method =>
                expected.Contains(Signature(method).Replace("ExceptionFlowAnalyzer",
                    "ExceptionFlowContextualFactEvaluator", StringComparison.Ordinal), StringComparer.Ordinal));
            Assert.DoesNotContain(typeof(ExceptionFlowAnalyzer).GetMethods(Declared), method =>
                seeds.Select(Signature).Contains(Signature(method).Replace("ExceptionFlowAnalyzer",
                    "ExceptionFlowContextualFactEvaluator", StringComparison.Ordinal), StringComparer.Ordinal));
        }

        /// <summary>
        /// Rejects return-only and guard-seed-only Analyzer proxies structurally,
        /// while retaining genuine transformations and fail-closed orchestration.
        /// </summary>
        [Fact]
        public void Composition_HasNoThinAnalyzerForwarderAndOwnsFreshGuardSeeds()
        {
            string directory = Path.Combine(GetRoot(), "src", "XMLDocNormalizer", "Checks",
                "Infrastructure", "Exception", "Flow");
            ClassDeclarationSyntax[] types = Directory.EnumerateFiles(directory, "*.cs")
                .SelectMany(file => CSharpSyntaxTree.ParseText(File.ReadAllText(file)).GetRoot()
                    .DescendantNodes().OfType<ClassDeclarationSyntax>()).ToArray();
            bool IsEvaluatorCall(ExpressionSyntax? expression) => expression is InvocationExpressionSyntax invocation
                && invocation.Expression is MemberAccessExpressionSyntax access
                && access.Expression.ToString() == nameof(ExceptionFlowContextualFactEvaluator);
            MethodDeclarationSyntax[] analyzerMethods = types
                .Where(type => type.Identifier.ValueText == nameof(ExceptionFlowAnalyzer))
                .SelectMany(type => type.Members.OfType<MethodDeclarationSyntax>()).ToArray();
            Assert.NotEmpty(analyzerMethods);
            Assert.DoesNotContain(types, type => type.Identifier.ValueText == nameof(ExceptionFlowAnalyzer)
                && type.Members.Count == 0);
            Assert.All(analyzerMethods, method =>
            {
                Assert.False(IsEvaluatorCall(method.ExpressionBody?.Expression));
                if (method.Body?.Statements is { Count: 1 } single && single[0] is ReturnStatementSyntax returned)
                {
                    Assert.False(IsEvaluatorCall(returned.Expression));
                }
                if (method.Body?.Statements is { Count: 2 } pair
                    && pair[0] is LocalDeclarationStatementSyntax local
                    && local.Declaration.Type.ToString() == "HashSet<ISymbol>"
                    && pair[1] is ReturnStatementSyntax seedReturn)
                {
                    Assert.False(IsEvaluatorCall(seedReturn.Expression));
                }
            });

            MethodDeclarationSyntax[] seeds = types
                .Where(type => type.Identifier.ValueText == nameof(ExceptionFlowContextualFactEvaluator))
                .SelectMany(type => type.Members.OfType<MethodDeclarationSyntax>())
                .Where(method => method.Identifier.ValueText == "CreateCallContext" && method.ParameterList.Parameters.Count == 4
                    || method.Identifier.ValueText == "IsDefinitelyNonNull" && method.ParameterList.Parameters.Count == 3).ToArray();
            Assert.Equal(2, seeds.Length);
            Assert.All(seeds, method =>
            {
                Assert.Equal(2, method.Body!.Statements.Count);
                LocalDeclarationStatementSyntax seed = Assert.IsType<LocalDeclarationStatementSyntax>(method.Body.Statements[0]);
                Assert.Equal("HashSet<ISymbol>", seed.Declaration.Type.ToString());
                ImplicitObjectCreationExpressionSyntax creation = Assert.IsType<ImplicitObjectCreationExpressionSyntax>(
                    Assert.Single(seed.Declaration.Variables).Initializer!.Value);
                Assert.Equal("SymbolEqualityComparer.Default", Assert.Single(creation.ArgumentList.Arguments).Expression.ToString());
                Assert.True(IsEvaluatorCall(Assert.IsType<ReturnStatementSyntax>(method.Body.Statements[1]).Expression));
            });
        }

        /// <summary>
        /// Protects the complete weak cache partition, its two helpers and three fields.
        /// </summary>
        [Fact]
        public void CachePartition_IsEvaluatorOwnedWithUnchangedStorageKinds()
        {
            Type evaluator = typeof(ExceptionFlowContextualFactEvaluator);
            FieldInfo cache = Assert.Single(evaluator.GetFields(Declared));
            Assert.Equal("conditionalWeakTableValueFactCaches", cache.Name);
            Assert.True(cache.IsPrivate && cache.IsStatic && cache.IsInitOnly);
            Assert.Equal(typeof(System.Runtime.CompilerServices.ConditionalWeakTable<,>),
                cache.FieldType.GetGenericTypeDefinition());
            Assert.Equal(typeof(Microsoft.CodeAnalysis.SemanticModel), cache.FieldType.GenericTypeArguments[0]);
            Type partition = Assert.Single(evaluator.GetNestedTypes(BindingFlags.NonPublic)
                .Where(type => type.Name == "ConditionalWeakTableValueFactCachePartition"));
            Assert.Equal(partition, cache.FieldType.GenericTypeArguments[1]);
            Assert.True(partition.IsNestedPrivate && partition.IsSealed);
            Assert.Equal(new[] { "Store", "TryGetValue" }, partition.GetMethods(Declared)
                .Select(method => method.Name).Order(StringComparer.Ordinal).ToArray());
            FieldInfo[] fields = partition.GetFields(Declared);
            Assert.Equal(new[] { "entries", "gate" }, fields.Select(field => field.Name)
                .Order(StringComparer.Ordinal).ToArray());
            Assert.All(fields, field => Assert.True(field.IsPrivate && field.IsInitOnly && !field.IsStatic));
            Assert.Equal(typeof(Dictionary<Microsoft.CodeAnalysis.ISymbol, bool>),
                fields.Single(field => field.Name == "entries").FieldType);
            Assert.Equal(typeof(object), fields.Single(field => field.Name == "gate").FieldType);
            Assert.DoesNotContain(typeof(ExceptionFlowAnalyzer).GetFields(Declared), field => field.Name == cache.Name);
            Assert.DoesNotContain(typeof(ExceptionFlowAnalyzer).GetNestedTypes(Declared), type => type.Name == partition.Name);
        }

        /// <summary>
        /// Includes calls, delegate targets, constructors, fields and type tokens in the
        /// reachable source-component graph; nested implementation types share their owner.
        /// </summary>
        [Fact]
        public void EvaluatorClosure_HasNoAnalyzerBacklinkOrInterComponentCycle()
        {
            Type evaluator = typeof(ExceptionFlowContextualFactEvaluator);
            Type analyzer = typeof(ExceptionFlowAnalyzer);
            Dictionary<short, OpCode> codes = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => field.FieldType == typeof(OpCode))
                .Select(field => (OpCode)field.GetValue(null)!).ToDictionary(code => code.Value);
            Dictionary<Type, HashSet<Type>> graph = [];
            foreach (Type type in evaluator.Assembly.GetTypes().Where(type => type.Namespace == evaluator.Namespace))
            {
                Type owner = Owner(type);
                HashSet<Type> targets = graph.TryGetValue(owner, out HashSet<Type>? existing)
                    ? existing : graph[owner] = [];
                IEnumerable<MethodBase> bodies = type.GetMethods(Declared).Cast<MethodBase>()
                    .Concat(type.GetConstructors(Declared));
                if (type.TypeInitializer != null)
                {
                    bodies = bodies.Append(type.TypeInitializer);
                }

                foreach (MethodBase method in bodies)
                {
                    byte[] bytes = method.GetMethodBody()?.GetILAsByteArray() ?? [];
                    for (int offset = 0; offset < bytes.Length;)
                    {
                        short value = bytes[offset++];
                        if (value == 0xfe)
                        {
                            value = (short)(0xfe00 | bytes[offset++]);
                        }

                        OpCode code = codes[value];
                        if (code.OperandType is OperandType.InlineMethod or OperandType.InlineField
                            or OperandType.InlineType or OperandType.InlineTok)
                        {
                            MemberInfo? member = method.Module.ResolveMember(BitConverter.ToInt32(bytes, offset),
                                type.IsGenericType ? type.GetGenericArguments() : null,
                                method.IsGenericMethod ? method.GetGenericArguments() : null);
                            Type? referenced = member as Type ?? member?.DeclaringType;
                            if (referenced != null && referenced.Assembly == evaluator.Assembly
                                && referenced.Namespace == evaluator.Namespace)
                            {
                                Type target = Owner(referenced);
                                if (target != owner)
                                {
                                    targets.Add(target);
                                }
                            }
                        }

                        offset += code.OperandType switch
                        {
                            OperandType.InlineNone => 0,
                            OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
                            OperandType.InlineVar => 2,
                            OperandType.InlineI8 or OperandType.InlineR => 8,
                            OperandType.InlineSwitch => 4 + 4 * BitConverter.ToInt32(bytes, offset),
                            _ => 4
                        };
                    }
                }
            }

            HashSet<Type> visited = [];
            HashSet<Type> active = [];
            void Visit(Type owner)
            {
                Assert.NotEqual(analyzer, owner);
                Assert.DoesNotContain(owner, active);
                if (!visited.Add(owner))
                {
                    return;
                }

                active.Add(owner);
                if (graph.TryGetValue(owner, out HashSet<Type>? targets))
                {
                    foreach (Type target in targets)
                    {
                        Visit(target);
                    }
                }

                active.Remove(owner);
            }

            Visit(evaluator);
            Assert.True(visited.Count > 20);
        }

        private static Type Owner(Type type)
        {
            while (type.DeclaringType != null)
            {
                type = type.DeclaringType;
            }

            return type.IsGenericType ? type.GetGenericTypeDefinition() : type;
        }

        private static string Signature(MethodInfo method) => method.DeclaringType!.FullName + "." + method.Name
            + "(" + string.Join(", ", method.GetParameters().Select(parameter =>
                (parameter.ParameterType.IsByRef ? parameter.IsOut ? "out " : "ref " : "")
                + TypeName(parameter.ParameterType.IsByRef ? parameter.ParameterType.GetElementType()! : parameter.ParameterType))) + ")";

        private static string TypeName(Type type)
        {
            if (type == typeof(bool))
            {
                return "bool";
            }

            if (type == typeof(int))
            {
                return "int";
            }

            return type.IsGenericType
                ? type.GetGenericTypeDefinition().FullName!.Split('`')[0] + "<"
                  + string.Join(", ", type.GetGenericArguments().Select(TypeName)) + ">"
                : type.FullName!;
        }

        private static string GetRoot()
        {
            for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
            {
                if (File.Exists(Path.Combine(directory.FullName, "XMLDocNormalizer.sln")))
                {
                    return directory.FullName;
                }
            }

            throw new InvalidOperationException("Could not find the solution root.");
        }
    }
}
