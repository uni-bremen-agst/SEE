using System.Reflection;
using System.Reflection.Emit;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Guards summary ownership through signatures, storage and executable IL,
    /// including compiler-generated closures and delegate method tokens.
    /// </summary>
    public sealed class ExceptionFlowSummaryOrchestrationDependencyTests
    {
        private const BindingFlags Declared = BindingFlags.Public | BindingFlags.NonPublic
            | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        private static readonly Lazy<Dictionary<Type, HashSet<Type>>> Dependencies = new(BuildDependencies);

        /// <summary>
        /// Rejects direct and transitive Builder callbacks, fields, parameters,
        /// factories and delegate targets that reach the Analyzer.
        /// </summary>
        [Fact]
        public void BuilderClosure_HasNoAnalyzerOrSessionBacklink()
        {
            HashSet<Type> reachable = Reachable(typeof(ExceptionFlowSummaryGraphBuilder));
            Assert.DoesNotContain(typeof(ExceptionFlowAnalyzer), reachable);
            Assert.DoesNotContain(typeof(ExceptionFlowSummaryAnalysisSession), reachable);
            FieldInfo field = Assert.Single(typeof(ExceptionFlowSummaryGraphBuilder).GetFields(Declared));
            Assert.Equal(typeof(ExceptionFlowSemanticEnvironment), field.FieldType);
            Assert.True(field.IsInitOnly && !field.IsStatic);
            Assert.Equal(new[] { "TryRegisterSummaryGraphRoot" },
                typeof(ExceptionFlowSummaryGraphBuilder).GetMethods(Declared).Select(method => method.Name));
        }

        /// <summary>
        /// Prevents executable-body queries from returning to Builder or being
        /// disguised as Session forwarding operations.
        /// </summary>
        [Fact]
        public void AnalyzerClosure_HasNoBuilderOrSessionQuery()
        {
            HashSet<Type> reachable = Reachable(typeof(ExceptionFlowAnalyzer));
            Assert.DoesNotContain(typeof(ExceptionFlowSummaryGraphBuilder), reachable);
            Assert.DoesNotContain(typeof(ExceptionFlowSummaryAnalysisSession), reachable);
            Assert.Contains(typeof(ExceptionFlowSummaryTargetRegistrar), reachable);
            Assert.DoesNotContain(typeof(ExceptionFlowAnalyzer).GetMethods(Declared), method =>
                method.Name is "CreateSummaryAnalysisSession" or "AnalyzeSolutionTransitivelyThrownExceptions");
        }

        /// <summary>
        /// Protects the complete upper component graph, not just one removed
        /// edge, while allowing unchanged cycles inside neutral domain types.
        /// </summary>
        [Fact]
        public void SummaryComponents_HaveNoInterComponentCycle()
        {
            Type[] components =
            [
                typeof(ExceptionFlowAnalyzer),
                typeof(ExceptionFlowSummaryAnalysisSession),
                typeof(ExceptionFlowSummaryGraphBuilder),
                typeof(ExceptionFlowSummaryGraphEvaluator),
                typeof(ExceptionFlowSummaryTargetRegistrar)
            ];
            foreach (Type source in components)
            {
                foreach (Type target in components.Where(target => target != source))
                {
                    Assert.False(Reachable(source).Contains(target) && Reachable(target).Contains(source),
                        $"Inter-component cycle: {source.Name} <-> {target.Name}");
                }
            }

            HashSet<Type> registrarClosure = Reachable(typeof(ExceptionFlowSummaryTargetRegistrar));
            Assert.DoesNotContain(typeof(ExceptionFlowAnalyzer), registrarClosure);
            Assert.DoesNotContain(typeof(ExceptionFlowSummaryAnalysisSession), registrarClosure);
            Assert.DoesNotContain(typeof(ExceptionFlowSummaryGraphBuilder), registrarClosure);
        }

        /// <summary>
        /// Keeps fact/evaluator gates at zero across the same expanded graph.
        /// </summary>
        [Fact]
        public void LowerFactResolverClosure_RemainsIndependentOfAnalyzer()
        {
            Type analyzer = typeof(ExceptionFlowAnalyzer);
            Type[] lower = analyzer.Assembly.GetTypes().Where(type => type.DeclaringType == null
                && (type.Namespace == analyzer.Namespace
                    || type.Namespace?.StartsWith(analyzer.Namespace + ".", StringComparison.Ordinal) == true))
                .Where(type =>
                type.Name.EndsWith("FactsProvider", StringComparison.Ordinal)
                    || type.Name.EndsWith("Facts", StringComparison.Ordinal)
                    || type.Name.EndsWith("Resolver", StringComparison.Ordinal)
                    || type.Name.EndsWith("FactProjector", StringComparison.Ordinal)
                    || type.Name.EndsWith("ArgumentMapper", StringComparison.Ordinal)
                    || type.Name.EndsWith("FactDiscovery", StringComparison.Ordinal)
                    || type.Name.EndsWith("FactEvaluator", StringComparison.Ordinal)
                    || type.Name.EndsWith("RuntimeDispatchClassifier", StringComparison.Ordinal)
                    || type.Name.EndsWith("CatchSemantics", StringComparison.Ordinal)
                    || type.Name.EndsWith("SemanticScope", StringComparison.Ordinal)).ToArray();
            Assert.Equal(27, lower.Length);
            Assert.All(lower, type => Assert.DoesNotContain(analyzer, Reachable(type)));
        }

        private static HashSet<Type> Reachable(Type root)
        {
            HashSet<Type> visited = [];
            Queue<Type> pending = new();
            pending.Enqueue(root);
            while (pending.TryDequeue(out Type? owner))
            {
                if (!visited.Add(owner))
                {
                    continue;
                }

                if (Dependencies.Value.TryGetValue(owner, out HashSet<Type>? targets))
                {
                    foreach (Type target in targets)
                    {
                        pending.Enqueue(target);
                    }
                }
            }

            return visited;
        }

        private static Dictionary<Type, HashSet<Type>> BuildDependencies()
        {
            Assembly assembly = typeof(ExceptionFlowAnalyzer).Assembly;
            Dictionary<short, OpCode> codes = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => field.FieldType == typeof(OpCode))
                .Select(field => (OpCode)field.GetValue(null)!).ToDictionary(code => code.Value);
            Dictionary<Type, HashSet<Type>> graph = [];
            foreach (Type type in assembly.GetTypes())
            {
                Type owner = Owner(type);
                HashSet<Type> targets = graph.TryGetValue(owner, out HashSet<Type>? existing)
                    ? existing : graph[owner] = [];
                void AddType(Type? dependency)
                {
                    if (dependency == null)
                    {
                        return;
                    }

                    if (dependency.HasElementType)
                    {
                        AddType(dependency.GetElementType());
                    }

                    foreach (Type argument in dependency.GetGenericArguments())
                    {
                        AddType(argument);
                    }

                    if (dependency.Assembly == assembly && Owner(dependency) != owner)
                    {
                        targets.Add(Owner(dependency));
                    }
                }

                AddType(type.BaseType);
                foreach (Type contract in type.GetInterfaces())
                {
                    AddType(contract);
                }

                foreach (FieldInfo field in type.GetFields(Declared))
                {
                    AddType(field.FieldType);
                }

                IEnumerable<MethodBase> bodies = type.GetMethods(Declared).Cast<MethodBase>()
                    .Concat(type.GetConstructors(Declared));
                if (type.TypeInitializer != null)
                {
                    bodies = bodies.Append(type.TypeInitializer);
                }

                foreach (MethodBase method in bodies)
                {
                    if (method is MethodInfo function)
                    {
                        AddType(function.ReturnType);
                    }

                    foreach (ParameterInfo parameter in method.GetParameters())
                    {
                        AddType(parameter.ParameterType);
                    }

                    MethodBody? body = method.GetMethodBody();
                    foreach (LocalVariableInfo local in body?.LocalVariables ?? [])
                    {
                        AddType(local.LocalType);
                    }

                    byte[] bytes = body?.GetILAsByteArray() ?? [];
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
                            AddType(member as Type ?? member?.DeclaringType);
                            if (member is FieldInfo field)
                            {
                                AddType(field.FieldType);
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

            return graph;
        }

        private static Type Owner(Type type)
        {
            while (type.DeclaringType != null)
            {
                type = type.DeclaringType;
            }

            return type.IsGenericType ? type.GetGenericTypeDefinition() : type;
        }
    }
}
