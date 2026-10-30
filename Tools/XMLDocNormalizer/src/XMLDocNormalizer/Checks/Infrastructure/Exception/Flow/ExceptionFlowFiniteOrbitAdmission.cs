using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.Canonical;
using static XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowFiniteOrbitProfile;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Discovers declaration schemas through a target-only sink and seals a
    /// finite-orbit inventory before any concrete production registration.
    /// </summary>
    internal sealed class ExceptionFlowFiniteOrbitAdmission : IExceptionFlowSummaryTargetSink
    {
        private readonly ExceptionFlowSemanticEnvironment environment;
        private readonly Dictionary<string, ISymbol> declarations = new(StringComparer.Ordinal);
        private readonly Queue<string> pendingDefinitions = new();
        private readonly Dictionary<string, List<Term>> templates = new(StringComparer.Ordinal);
        private readonly List<Edge> edges = [];
        private readonly HashSet<string> edgeTemplates = new(StringComparer.Ordinal);
        private readonly Dictionary<ExceptionFlowCallableKey, int> edgeWitnesses = new(ReferenceEqualityComparer.Instance);
        private readonly HashSet<int> locatedEdges = [];
        private readonly List<Reason> reasons = [];
        private readonly HashSet<string> knownAssemblies = new(StringComparer.Ordinal);
        private readonly Dictionary<string, (ExceptionFlowSummaryFragment Fragment, bool HasBody)> diagnosticDefinitions = new(StringComparer.Ordinal);
        private string? currentDefinition;
        private Certificate? certificate;
        private ExceptionFlowFiniteOrbitSeal? seal;

        private ExceptionFlowFiniteOrbitAdmission(ExceptionFlowSemanticEnvironment environment)
        {
            this.environment = environment;
            foreach (var scope in environment.GetAnalysisScopes())
            foreach (IAssemblySymbol assembly in new[] { scope.Compilation.Assembly }
                         .Concat(scope.Compilation.SourceModule.ReferencedAssemblySymbols))
                knownAssemblies.Add(AssemblyKey(assembly));
        }

        /// <summary>Gets the sealed classified outcome and its complete finite witness.</summary>
        internal Certificate Outcome => certificate ?? throw new InvalidOperationException("Inventory is not sealed.");

        /// <summary>Gets the number of inspected declarations, not concrete contexts.</summary>
        internal int DefinitionCount => declarations.Count;

        /// <summary>Gets immutable proof data only after successful fresh discovery.</summary>
        internal ExceptionFlowFiniteOrbitSeal Seal => seal
            ?? throw new InvalidOperationException("Outside inventory cannot authorize graph construction.");

        private static string AssemblyKey(IAssemblySymbol assembly)
            => JsonSerializer.Serialize(RoslynCanonicalIdentityFactory.CreateAssemblyIdentity(assembly));

        internal static string BodyId(ISymbol symbol)
            => CanonicalIdentityKeyWriter.Write(RoslynCanonicalIdentityFactory.CreateCallableIdentity(symbol, true));

        private static string InvocationId(ISymbol symbol)
            => CanonicalIdentityKeyWriter.Write(RoslynCanonicalIdentityFactory.CreateCallableIdentity(symbol, false));

        private static string Transition(string caller, string target, ISymbol invocation)
            => JsonSerializer.Serialize(new[] { caller, target, InvocationId(invocation) });

        private string AddDefinition(ISymbol symbol)
        {
            foreach (var scope in environment.GetAnalysisScopes())
            foreach (IAssemblySymbol assembly in new[] { scope.Compilation.Assembly }
                         .Concat(scope.Compilation.SourceModule.ReferencedAssemblySymbols))
                knownAssemblies.Add(AssemblyKey(assembly));
            ISymbol definition = symbol.OriginalDefinition;
            string id = BodyId(definition);
            if (declarations.TryAdd(id, definition))
            {
                templates.Add(id, []);
                pendingDefinitions.Enqueue(id);
            }
            else if (!SymbolEqualityComparer.Default.Equals(declarations[id], definition))
            {
                reasons.Add(new("OwnerBindingFailure", "Multiple source declarations for " + id));
            }
            return id;
        }

        /// <summary>Records only a declaration/map; this sink cannot call GetOrAdd or enqueue a concrete context.</summary>
        /// <param name="symbol">The resolved source body handle.</param>
        /// <param name="context">The original symbolic call-site construction.</param>
        /// <returns>A local diagnostic edge key, never registered in a graph.</returns>
        public ExceptionFlowCallableKey RegisterTarget(ISymbol symbol, ExceptionFlowCallContext context)
        {
            string target = AddDefinition(symbol);
            ISymbol invocation = context.InvocationSymbol ?? symbol;
            if (currentDefinition == null) throw new InvalidOperationException("No discovery body frame.");
            if (BodyId(invocation) != target)
                reasons.Add(new("OwnerBindingFailure", "Source/metadata declaration mismatch: " + BodyId(invocation) + " -> " + target));
            Term[] assignments = ExceptionFlowInvocationSubstitution.Bindings(invocation)
                .Select(pair => ToTerm(pair.Actual)).ToArray();
            string transition = Transition(currentDefinition, target, invocation);
            edges.Add(new(currentDefinition, target, assignments, transition));
            edgeTemplates.Add(transition);
            // No declaration-body analysis or concrete successor materialization
            // occurs here. The independent queue contains definition IDs only.
            ExceptionFlowCallableKey key = new(symbol, context);
            edgeWitnesses.Add(key, edges.Count - 1);
            return key;
        }

        /// <summary>Retains pre-catch coverage and source terms from one definition body.</summary>
        /// <param name="fragment">The not-yet-suppressed local fragment.</param>
        public void ObserveLocalFragment(ExceptionFlowSummaryFragment fragment)
        {
            if (currentDefinition == null) throw new InvalidOperationException("No discovery body frame.");
            foreach (var source in fragment.Sources) templates[currentDefinition].Add(ToTerm(source.ExceptionType));
            foreach (var edge in fragment.CallEdges)
                if (edgeWitnesses.TryGetValue(edge.Target, out int index) && locatedEdges.Add(index))
                    edges[index] = edges[index] with { Witness = edges[index].Witness + " / site=" + JsonSerializer.Serialize(new
                    {
                        edge.CallSiteStep.Kind, edge.CallSiteStep.FilePath, edge.CallSiteStep.Line, edge.CallSiteStep.Column
                    }) };
            if (fragment.UncertainTargets.Count != 0)
                reasons.Add(new("TargetOrAnalysisCoverageIncomplete", currentDefinition + " / local analysis coverage is incomplete"));
        }

        private Term ToTerm(ITypeSymbol type)
        {
            if (type is ITypeParameterSymbol parameter)
                return Term.Var(ExceptionFlowInvocationSubstitution.Binder(parameter));
            if (type.TypeKind == TypeKind.Dynamic)
            {
                IAssemblySymbol? core = environment.GetAnalysisScopes().Select(scope => scope.Compilation.ObjectType.ContainingAssembly).FirstOrDefault();
                if (core?.GetTypeByMetadataName("System.Object") is INamedTypeSymbol objectType) type = objectType;
            }
            if (type is not INamedTypeSymbol named || named.TypeKind == TypeKind.Error || named.IsUnboundGenericType)
            {
                reasons.Add(new("RepresentationOutsideGrammar", currentDefinition + " / unsupported runtime type kind " + type.TypeKind));
                return Term.Named("outside:" + type.TypeKind);
            }
            if (named.IsTupleType && named.TupleUnderlyingType != null) named = named.TupleUnderlyingType;
            if (named.IsNativeIntegerType)
                named = (INamedTypeSymbol)environment.GetAnalysisScopes().First().Compilation.GetSpecialType(named.SpecialType);
            if (!knownAssemblies.Contains(AssemblyKey(named.ContainingAssembly)))
                reasons.Add(new("OwnerBindingFailure", currentDefinition + " / type owner outside registered scope: " + AssemblyKey(named.ContainingAssembly)));
            List<Term> arguments = [];
            if (named.ContainingType != null) arguments.Add(ToTerm(named.ContainingType));
            arguments.AddRange(named.TypeArguments.Select(ToTerm));
            return Term.Named(CanonicalIdentityKeyWriter.Write(
                RoslynCanonicalIdentityFactory.CreateTypeIdentity(named.OriginalDefinition)), arguments.ToArray());
        }

        private void InspectDeclaration(ISymbol definition)
        {
            foreach (SyntaxReference syntax in definition.DeclaringSyntaxReferences)
            {
                SyntaxNode declaration = syntax.GetSyntax();
                if (!environment.TryGetSemanticModel(declaration.SyntaxTree, out SemanticModel model))
                {
                    reasons.Add(new("TargetOrAnalysisCoverageIncomplete", currentDefinition + " / semantic model missing"));
                    continue;
                }
                // This query analyzes source members, not executable packaging.
                // A Workspace compilation may have no Main while every member
                // is fully bound. CS5001 cannot change a type/map/transfer rule;
                // all binding, syntax, reference and constraint errors remain
                // rejection inputs. Build validation is a separate unchanged gate.
                if (environment.HasBlockingCompilationErrors(model.Compilation))
                    reasons.Add(new("RepresentationOutsideGrammar", currentDefinition + " / source compilation has errors"));
                foreach (SyntaxNode node in declaration.DescendantNodesAndSelf(child =>
                             ReferenceEquals(child, declaration) || child is not MemberDeclarationSyntax
                                 && child is not LocalFunctionStatementSyntax && child is not AnonymousFunctionExpressionSyntax))
                {
                    if (node is AwaitExpressionSyntax)
                        reasons.Add(new("TargetOrAnalysisCoverageIncomplete", currentDefinition + " / await requires unavailable shared RuntimeAwait capability"));
                    if (node is CatchDeclarationSyntax caught && model.GetTypeInfo(caught.Type).Type is ITypeSymbol caughtType)
                        templates[currentDefinition!].Add(ToTerm(caughtType));
                }
            }
            ExceptionFlowSummaryFragment fragment = new();
            bool hasBody = ExceptionFlowAnalyzer.AnalyzeSummarySymbolDeclarations(definition, environment,
                    this, fragment, new ExceptionFlowCallContext(definition));
            if (!hasBody)
                reasons.Add(new("TargetOrAnalysisCoverageIncomplete", currentDefinition + " / executable body unavailable"));
            ObserveLocalFragment(fragment);
            diagnosticDefinitions.Add(currentDefinition!, (fragment, hasBody));
        }

        /// <summary>Performs the finite definition prepass without a production graph.</summary>
        /// <param name="member">The source-owned production root.</param>
        /// <param name="environment">The existing validated semantic environment.</param>
        /// <returns>The sealed inventory and classified outcome.</returns>
        internal static ExceptionFlowFiniteOrbitAdmission Discover(
            MemberDeclarationSyntax member, ExceptionFlowSemanticEnvironment environment)
        {
            ExceptionFlowFiniteOrbitAdmission admission = new(environment);
            string root = "unresolved:" + member.SyntaxTree.FilePath + ":" + member.SpanStart;
            Term[] seed = [];
            if (!environment.TryGetSemanticModel(member.SyntaxTree, out SemanticModel model)
                || model.GetDeclaredSymbol(member) is not ISymbol rootSymbol)
                admission.reasons.Add(new("OwnerBindingFailure", root));
            else if (member.ContainsDiagnostics || string.IsNullOrWhiteSpace(rootSymbol.Name))
                admission.reasons.Add(new("RepresentationOutsideGrammar", root + ":" + member.Span.Length
                    + ":" + member.Kind() + " / incomplete root"));
            else
            {
                root = admission.AddDefinition(rootSymbol);
                admission.currentDefinition = root;
                seed = ExceptionFlowInvocationSubstitution.Bindings(rootSymbol).Select(pair => admission.ToTerm(pair.Actual)).ToArray();
                while (admission.pendingDefinitions.TryDequeue(out string? body))
                {
                    admission.currentDefinition = body;
                    admission.InspectDeclaration(admission.declarations[body]);
                }
            }
            admission.currentDefinition = null;
            Body[] bodies = admission.declarations.Select(pair => new Body(pair.Key,
                ExceptionFlowInvocationSubstitution.Bindings(pair.Value).Select(binding => ExceptionFlowInvocationSubstitution.Binder(binding.Formal)).ToArray(),
                admission.templates[pair.Key].DistinctBy(term => term.Key).OrderBy(term => term.Key, StringComparer.Ordinal).ToArray())).ToArray();
            string scope = ScopeDigest(environment);
            foreach (var owners in environment.GetAnalysisScopes().GroupBy(item => AssemblyKey(item.Compilation.Assembly), StringComparer.Ordinal))
                if (owners.Select(item => item.Compilation).Distinct(ReferenceEqualityComparer.Instance).Count() != 1)
                    admission.reasons.Add(new("OwnerBindingFailure", "Conflicting registered source owners: " + owners.Key));
            foreach (var owners in environment.GetAnalysisScopes().SelectMany(item => item.Compilation.References
                         .OfType<PortableExecutableReference>().Select(reference => new
                         {
                             Owner = item.Compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assembly
                                 ? AssemblyKey(assembly) : "module:" + reference.Display,
                             Modules = MetadataModules(reference)
                         })).GroupBy(item => item.Owner, StringComparer.Ordinal))
                if (owners.Select(item => item.Modules).Distinct(StringComparer.Ordinal).Count() != 1)
                    admission.reasons.Add(new("OwnerBindingFailure", "Conflicting metadata module worlds: " + owners.Key));
            admission.certificate = Check(bodies, admission.edges.Distinct().ToArray(), root, seed, scope, admission.reasons);
            if (admission.Outcome.Complete)
                admission.seal = new(admission.Outcome, admission.declarations,
                    admission.edgeTemplates, admission.knownAssemblies,
                    environment.GetAnalysisScopes().Select(item => item.Compilation));
            return admission;
        }

        /// <summary>Binds all registered source/assembly inputs, independently of compiler object identity.</summary>
        /// <param name="environment">The registered source scopes.</param>
        /// <returns>The deterministic source/reference fingerprint.</returns>
        internal static string ScopeDigest(ExceptionFlowSemanticEnvironment environment)
            => Digest(JsonSerializer.Serialize(environment.GetAnalysisScopes().Select(scope => new
            {
                Assembly = AssemblyKey(scope.Compilation.Assembly),
                Sources = scope.Compilation.SyntaxTrees.Select(tree => new
                    {
                        Path = tree.FilePath, Hash = Digest(tree.GetText().ToString()),
                        Parse = tree.Options is CSharpParseOptions parse ? JsonSerializer.Serialize(new
                        {
                            Language = parse.LanguageVersion.ToDisplayString(), Specified = parse.SpecifiedLanguageVersion.ToDisplayString(),
                            parse.Kind, parse.DocumentationMode, Defines = parse.PreprocessorSymbolNames.Order(StringComparer.Ordinal).ToArray(),
                            Features = parse.Features.OrderBy(pair => pair.Key, StringComparer.Ordinal).ToArray()
                        }) : tree.Options.ToString()
                    })
                    .OrderBy(source => source.Path, StringComparer.Ordinal).ToArray(),
                Options = new
                {
                    scope.Compilation.Options.OutputKind, scope.Compilation.Options.Platform,
                    scope.Compilation.Options.CheckOverflow, scope.Compilation.Options.OptimizationLevel,
                    Nullable = (scope.Compilation.Options as CSharpCompilationOptions)?.NullableContextOptions,
                    Unsafe = (scope.Compilation.Options as CSharpCompilationOptions)?.AllowUnsafe
                },
                References = scope.Compilation.References.Select(reference => JsonSerializer.Serialize(new
                {
                    Owner = scope.Compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assembly
                        ? AssemblyKey(assembly) : "module:" + reference.Display,
                    Aliases = reference.Properties.Aliases.Order(StringComparer.Ordinal).ToArray(),
                    reference.Properties.EmbedInteropTypes, reference.Properties.Kind,
                    Modules = reference is PortableExecutableReference pe ? MetadataModules(pe) : null,
                    Source = reference is CompilationReference source ? Digest(string.Join("\n", source.Compilation.SyntaxTrees
                        .Select(tree => tree.FilePath + ":" + Digest(tree.GetText().ToString())).Order(StringComparer.Ordinal))) : null
                })).Order(StringComparer.Ordinal).ToArray()
            }).OrderBy(scope => scope.Assembly, StringComparer.Ordinal).ToArray()));

        private static string MetadataModules(PortableExecutableReference reference)
        {
            // Read the metadata already held by the immutable Roslyn reference,
            // not a possibly replaced file at reference.FilePath. This binds
            // aliases and module worlds without inventing a source/PE alias.
            Metadata metadata = reference.GetMetadata();
            IEnumerable<ModuleMetadata> modules = metadata is AssemblyMetadata assembly
                ? assembly.GetModules() : new[] { (ModuleMetadata)metadata };
            return JsonSerializer.Serialize(modules.Select(module =>
            {
                var reader = module.GetMetadataReader();
                var definition = reader.GetModuleDefinition();
                return new { Name = reader.GetString(definition.Name), Mvid = reader.GetGuid(definition.Mvid).ToString("D") };
            }).OrderBy(module => module.Name, StringComparer.Ordinal).ToArray());
        }

        /// <summary>
        /// Solves only finite ground, parameterless definition equations for
        /// independent diagnostic facts. No generic environment is unfolded.
        /// A body outside that subquery is unknown, not folded or assumed empty.
        /// </summary>
        /// <returns>Proven lower witnesses and an explicit unknown remainder.</returns>
        internal ExceptionFlowProfileDiagnostics DiagnoseGroundFacts()
        {
            string root = Outcome.Root;
            if (!declarations.ContainsKey(root) || Outcome.Reasons.Any(reason =>
                    reason.Category is "OwnerBindingFailure" or "RepresentationOutsideGrammar" or "ScopeMapNotClosed"))
                return new([], [], true, ["No ground diagnostic claim: binding/representation is not established."]);
            bool Eligible(string body) => !ExceptionFlowInvocationSubstitution.Bindings(declarations[body]).Any()
                && declarations[body] is IMethodSymbol { Parameters.Length: 0 };
            Dictionary<string, Dictionary<string, INamedTypeSymbol>> known = new(StringComparer.Ordinal);
            Dictionary<string, Dictionary<string, INamedTypeSymbol>> external = new(StringComparer.Ordinal);
            Dictionary<string, bool> unknown = new(StringComparer.Ordinal);
            string TypeKey(INamedTypeSymbol type) => CanonicalIdentityKeyWriter.Write(RoslynCanonicalIdentityFactory.CreateTypeIdentity(type));
            foreach (string body in declarations.Keys)
            {
                known.Add(body, new(StringComparer.Ordinal)); external.Add(body, new(StringComparer.Ordinal));
                var snapshot = diagnosticDefinitions[body];
                unknown.Add(body, !Eligible(body) || !snapshot.HasBody || snapshot.Fragment.UncertainTargets.Count != 0);
                if (!Eligible(body)) continue;
                foreach (var source in snapshot.Fragment.Sources)
                {
                    if (!ToTerm(source.ExceptionType).IsGround) { unknown[body] = true; continue; }
                    (source.Kind == Models.ExceptionFlowSourceKind.ProvenException ? known[body] : external[body])
                        .TryAdd(TypeKey(source.ExceptionType), source.ExceptionType);
                }
            }
            // A finite monotone lattice over the closed source types present in
            // the finite input, not a depth/iteration bound or a generic solver.
            bool changed;
            do
            {
                changed = false;
                foreach (string body in declarations.Keys)
                {
                    if (!Eligible(body)) continue;
                    foreach (var edge in diagnosticDefinitions[body].Fragment.CallEdges)
                    {
                        string target = BodyId(edge.Target.Symbol);
                        if (!unknown.ContainsKey(target)) { if (!unknown[body]) { unknown[body] = true; changed = true; } continue; }
                        if (unknown[target] && !unknown[body]) { unknown[body] = true; changed = true; }
                        foreach (var pair in known[target])
                            if (!edge.CaughtExceptionTypes.Any(caught => ExactGroundCatch(pair.Value, caught))
                                && known[body].TryAdd(pair.Key, pair.Value)) changed = true;
                        foreach (var pair in external[target])
                            if (!edge.CaughtExceptionTypes.Any(caught => ExactGroundCatch(pair.Value, caught))
                                && external[body].TryAdd(pair.Key, pair.Value)) changed = true;
                    }
                }
            } while (changed);
            return new(known[root].OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => pair.Value).ToArray(),
                external[root].OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => pair.Value).ToArray(), unknown[root],
                ["Ground parameterless declaration equations; whole-result admission remains blocked.", "Inventory=" + Outcome.InventoryDigest]);
        }

        private static bool ExactGroundCatch(INamedTypeSymbol thrown, INamedTypeSymbol caught)
        {
            var expected = RoslynCanonicalIdentityFactory.CreateTypeIdentity(caught);
            HashSet<string> visited = new(StringComparer.Ordinal);
            for (INamedTypeSymbol? current = thrown; current != null; current = current.BaseType)
            {
                var identity = RoslynCanonicalIdentityFactory.CreateTypeIdentity(current);
                if (!visited.Add(CanonicalIdentityKeyWriter.Write(identity))) return false;
                if (identity.Equals(expected)) return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Immutable discovery output. Validation uses only copied inventory data
    /// and immutable Roslyn handles, never discovery, Analyzer or a callback.
    /// Session obtains this seal from fresh discovery, not from a caller.
    /// </summary>
    internal sealed class ExceptionFlowFiniteOrbitSeal
    {
        private readonly Dictionary<string, int> declarations;
        private readonly HashSet<string> transitions;
        private readonly HashSet<string> assemblies;
        private readonly HashSet<Compilation> scopes;

        internal string ScopeDigest { get; }

        internal ExceptionFlowFiniteOrbitSeal(Certificate certificate,
            IReadOnlyDictionary<string, ISymbol> declarations, IEnumerable<string> transitions,
            IEnumerable<string> assemblies, IEnumerable<Compilation> scopes)
        {
            if (!certificate.Complete) throw new InvalidOperationException("Outside inventory cannot issue a seal.");
            ScopeDigest = certificate.ScopeDigest;
            this.declarations = declarations.ToDictionary(pair => pair.Key,
                pair => ExceptionFlowInvocationSubstitution.Bindings(pair.Value).Count(), StringComparer.Ordinal);
            this.transitions = new(transitions, StringComparer.Ordinal);
            this.assemblies = new(assemblies, StringComparer.Ordinal);
            this.scopes = new(scopes, ReferenceEqualityComparer.Instance);
        }

        internal static string BodyId(ISymbol symbol)
            => CanonicalIdentityKeyWriter.Write(RoslynCanonicalIdentityFactory.CreateCallableIdentity(symbol, true));

        internal bool ScopeUnchanged(IEnumerable<Compilation> current) => scopes.SetEquals(current);

        internal bool ContainsTransition(ExceptionFlowCallableKey caller, ISymbol target, ISymbol invocation)
            => transitions.Contains(JsonSerializer.Serialize(new[] { BodyId(caller.Symbol), BodyId(target),
                CanonicalIdentityKeyWriter.Write(RoslynCanonicalIdentityFactory.CreateCallableIdentity(invocation, false)) }));

        internal bool ContainsContext(ISymbol symbol, ExceptionFlowCallContext? context)
        {
            if (context?.InvocationSymbol == null || context.CanonicalContext == null) return false;
            var bindings = ExceptionFlowInvocationSubstitution.Bindings(context.InvocationSymbol).ToArray();
            return declarations.TryGetValue(BodyId(symbol), out int arity) && bindings.Length == arity
                && bindings.All(binding => IsGroundNominal(binding.Actual));
        }

        private bool IsGroundNominal(ITypeSymbol type)
        {
            if (type.TypeKind == TypeKind.Dynamic) return true;
            if (type is not INamedTypeSymbol named || named.TypeKind == TypeKind.Error || named.IsUnboundGenericType)
                return false;
            if (named.IsTupleType && named.TupleUnderlyingType != null) named = named.TupleUnderlyingType;
            return assemblies.Contains(JsonSerializer.Serialize(RoslynCanonicalIdentityFactory.CreateAssemblyIdentity(named.ContainingAssembly)))
                && (named.ContainingType == null || IsGroundNominal(named.ContainingType))
                && named.TypeArguments.All(IsGroundNominal);
        }
    }

    /// <summary>Independent diagnostic evidence, never an admitted summary or normal Finding permission.</summary>
    internal sealed record ExceptionFlowProfileDiagnostics(INamedTypeSymbol[] ProvenLowerWitnesses,
        INamedTypeSymbol[] ExternalDocumentation, bool HasEscapingUncertainty, string[] Proof)
    {
        internal bool NormalFindingAdmissionBlocked => true;
    }

    /// <summary>A typed invalidation of a production inventory or exact binding.</summary>
    internal sealed class ExceptionFlowProfileViolation : System.Exception
    {
        /// <summary>Gets the neutral classified reason.</summary>
        internal Reason Reason { get; }

        /// <summary>Creates a failed admission witness, never an exact fallback.</summary>
        /// <param name="category">The A9 reason category.</param>
        /// <param name="witness">The finite offending input/map witness.</param>
        internal ExceptionFlowProfileViolation(string category, string witness) : base(witness)
        { Reason = new(category, witness); }
    }
}
