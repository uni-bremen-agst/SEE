using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

// Standalone contract prototype over neutral finite inputs. Not linked to an analyzer,
// not a C# extractor, not a replacement SCC evaluator, and not a general family solver.
internal static class ProfileProof
{
    private sealed record Owner(string Assembly, string Module, string Declaration, int Arity, string? Parent = null)
    {
        public string Key => JsonSerializer.Serialize(new { Assembly, Module, Declaration, Arity, Parent });
    }
    private sealed record Binder(string Owner, string Scope, int Ordinal)
    {
        public string Key => JsonSerializer.Serialize(new { Owner, Scope, Ordinal });
    }
    private sealed record Term(Owner? Head, Binder? Variable, Term? Containing, Term[] Arguments)
    {
        public string Key => Variable != null ? "V" + Variable.Key
            : "C" + Head!.Key + JsonSerializer.Serialize(new[] { Containing?.Key }.Concat(Arguments.Select(a => a.Key)));
        public IEnumerable<(Binder Binder, int Depth)> Occurrences(int depth = 0)
        {
            if (Variable != null) { yield return (Variable, depth); yield break; }
            if (Containing != null) foreach (var item in Containing.Occurrences(depth + 1)) yield return item;
            foreach (var argument in Arguments) foreach (var item in argument.Occurrences(depth + 1)) yield return item;
        }
        public Term Apply(IReadOnlyDictionary<string, Term> environment)
        {
            if (Variable != null) return environment[Variable.Key]; // simultaneous: do NOT recurse into replacement
            return new(Head, null, Containing?.Apply(environment), Arguments.Select(a => a.Apply(environment)).ToArray());
        }
    }
    private sealed record Body(string Id, Binder[] Binders, Term? Source = null, Term[]? Catches = null);
    private sealed record Edge(string From, string To, Term[] Assignments);
    private sealed record Schema(Body[] Bodies, Edge[] Edges, Owner[] Manifest, string Root, Term[] Seed, string[] Blockers);
    private sealed record Verdict(bool Exact, string[] Reasons, string[] PositiveCycleEdges, string Digest,
        int SlotsVisited, int ConcreteContextsMaterialized);
    private sealed record BaseRule(Owner Head, Binder[] Binders, Term? Base);
    private static readonly List<object> Checks = [];
    private static Term V(Binder binder) => new(null, binder, null, []);
    private static Term C(Owner owner, params Term[] arguments) => new(owner, null, null, arguments);
    private static Term Nested(Owner owner, Term containing, params Term[] arguments) => new(owner, null, containing, arguments);
    private static string Digest(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static void Check(string name, bool value, object? detail = null)
    {
        if (!value) throw new InvalidOperationException(name);
        Checks.Add(new { Name = name, Passed = true, Detail = detail });
    }
    private static string Canonical(Schema schema)
    {
        // Definition IDs and binder IDs are scoped semantic IDs; display names and object identities are absent.
        var bodies = schema.Bodies.OrderBy(b => b.Id, StringComparer.Ordinal).Select(b => new
        {
            b.Id,
            Binders = b.Binders.Select(x => x.Key).Order(StringComparer.Ordinal).ToArray(),
            Source = b.Source?.Key,
            Catches = (b.Catches ?? []).Select(t => t.Key).Order(StringComparer.Ordinal).ToArray()
        }).ToArray();
        var edges = schema.Edges.Select(e => JsonSerializer.Serialize(new
        {
            e.From,
            e.To,
            Map = e.Assignments.Select((t, i) => new { Binder = schema.Bodies.Single(b => b.Id == e.To).Binders[i].Key, Term = t.Key })
                .OrderBy(x => x.Binder, StringComparer.Ordinal).ToArray()
        })).Order(StringComparer.Ordinal).ToArray();
        var root = schema.Bodies.Single(b => b.Id == schema.Root);
        var seed = schema.Seed.Select((t, i) => new { Binder = root.Binders[i].Key, Term = t.Key }).OrderBy(x => x.Binder, StringComparer.Ordinal);
        return JsonSerializer.Serialize(new
        {
            Profile = "A9-FiniteOrbit-v1",
            Bodies = bodies,
            Edges = edges,
            Manifest = schema.Manifest.Select(x => x.Key).Order(StringComparer.Ordinal).ToArray(),
            Root = schema.Root,
            Seed = seed
        });
    }
    private static Verdict Classify(Schema schema)
    {
        var reasons = new SortedSet<string>(schema.Blockers, StringComparer.Ordinal);
        if (schema.Manifest.Any(o => o.Arity < 0 || string.IsNullOrEmpty(o.Assembly) || string.IsNullOrEmpty(o.Module) || string.IsNullOrEmpty(o.Declaration)))
            reasons.Add("InvalidOwnerIdentity");
        if (schema.Bodies.Select(b => b.Id).Distinct(StringComparer.Ordinal).Count() != schema.Bodies.Length)
            return new(false, ["DuplicateBodyIdentity"], [], Digest(JsonSerializer.Serialize(schema)), 0, 0);
        var bodies = schema.Bodies.ToDictionary(b => b.Id, StringComparer.Ordinal);
        bool Valid(Term term, HashSet<string> binders)
        {
            if (term.Variable != null) return term.Head == null && term.Containing == null && term.Arguments.Length == 0 && binders.Contains(term.Variable.Key);
            if (term.Head == null || term.Arguments.Length != term.Head.Arity) return false;
            int owners = schema.Manifest.Count(o => o.Key == term.Head.Key);
            if (owners != 1) { reasons.Add(owners == 0 ? "OwnerBindingMissing" : "OwnerBindingAmbiguous"); return false; }
            if ((term.Head.Parent == null) != (term.Containing == null)) return false;
            var parent = term.Containing?.Head;
            if (term.Containing != null && (parent == null || parent.Declaration != term.Head.Parent
                || parent.Assembly != term.Head.Assembly || parent.Module != term.Head.Module)) return false;
            return (term.Containing == null || Valid(term.Containing, binders)) && term.Arguments.All(t => Valid(t, binders));
        }
        foreach (var body in schema.Bodies)
        {
            var allowed = body.Binders.Select(b => b.Key).ToHashSet(StringComparer.Ordinal);
            if (allowed.Count != body.Binders.Length || body.Binders.Any(b => string.IsNullOrEmpty(b.Owner) || b.Ordinal < 0 || b.Scope is not ("Method" or "Type"))) reasons.Add("InvalidBinderScope");
            Term[] observations = body.Catches ?? [];
            if (body.Source is { } source) observations = [source, .. observations];
            foreach (var term in observations)
                if (!Valid(term, allowed)) reasons.Add("TermOutsideGrammar");
        }
        if (!bodies.TryGetValue(schema.Root, out var root) || root.Binders.Length != schema.Seed.Length) reasons.Add("RootEnvironmentNotClosed");
        if (schema.Seed.Any(t => !Valid(t, []))) reasons.Add("RootEnvironmentNotClosed");
        foreach (var edge in schema.Edges)
        {
            if (!bodies.TryGetValue(edge.From, out var from) || !bodies.TryGetValue(edge.To, out var to) || to.Binders.Length != edge.Assignments.Length)
            { reasons.Add("SubstitutionMapNotTotal"); continue; }
            var allowed = from.Binders.Select(b => b.Key).ToHashSet(StringComparer.Ordinal);
            if (edge.Assignments.Any(t => !Valid(t, allowed))) reasons.Add("SubstitutionMapNotClosed");
        }
        if (reasons.Count != 0) return new(false, reasons.ToArray(), [], Digest(JsonSerializer.Serialize(schema)), 0, 0);
        var slots = schema.Bodies.SelectMany(b => b.Binders.Select(x => (Body: b.Id, Binder: x.Key))).ToArray();
        var index = slots.Select((s, i) => (s, i)).ToDictionary(x => x.s, x => x.i);
        var dependencies = new List<(int From, int To, int Weight)>();
        foreach (var edge in schema.Edges)
        {
            var target = bodies[edge.To];
            for (int i = 0; i < edge.Assignments.Length; i++)
                foreach (var occurrence in edge.Assignments[i].Occurrences())
                    dependencies.Add((index[(edge.From, occurrence.Binder.Key)], index[(edge.To, target.Binders[i].Key)], occurrence.Depth));
        }
        // Finite transitive closure is a deliberately simple proof implementation.
        // Production may use SCCs in O(V+E); membership NEVER constructs a ground context.
        int n = slots.Length;
        var reachable = new bool[n, n];
        for (int i = 0; i < n; i++) reachable[i, i] = true;
        foreach (var edge in dependencies) reachable[edge.From, edge.To] = true;
        for (int k = 0; k < n; k++) for (int i = 0; i < n; i++) for (int j = 0; j < n; j++)
                    reachable[i, j] |= reachable[i, k] && reachable[k, j];
        var positive = dependencies.Where(e => e.Weight > 0 && reachable[e.To, e.From])
            .Select(e => JsonSerializer.Serialize(new
            {
                Source = new { slots[e.From].Body, slots[e.From].Binder },
                Target = new { slots[e.To].Body, slots[e.To].Binder },
                e.Weight
            }))
            .Distinct().Order(StringComparer.Ordinal).ToArray();
        if (positive.Length != 0)
        {
            reasons.Add("PositiveConstructorCycle");
            if (schema.Bodies.Any(b => b.Source?.Occurrences().Select(x => x.Binder.Key).Distinct().Count() > 1
                && (b.Catches ?? []).Any(t => t.Occurrences().GroupBy(x => x.Binder.Key).Any(g => g.Count() > 1))))
                reasons.Add("CorrelatedPredicateOutsideFiniteOrbit");
        }
        return new(reasons.Count == 0, reasons.ToArray(), positive, Digest(Canonical(schema)), n, 0);
    }
    private static List<(string Body, Term[] Environment)> ExpandFinite(Schema schema)
    {
        if (!Classify(schema).Exact) throw new InvalidOperationException("No expansion without admission.");
        var result = new List<(string Body, Term[] Environment)>();
        var queue = new Queue<(string Body, Term[] Environment)>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        queue.Enqueue((schema.Root, schema.Seed));
        while (queue.TryDequeue(out var state))
        {
            string key = JsonSerializer.Serialize(new { state.Body, Arguments = state.Environment.Select(t => t.Key).ToArray() });
            if (!seen.Add(key)) continue;
            result.Add(state);
            var body = schema.Bodies.Single(b => b.Id == state.Body);
            var environment = body.Binders.Select((b, i) => (b.Key, Term: state.Environment[i])).ToDictionary(x => x.Key, x => x.Term);
            foreach (var edge in schema.Edges.Where(e => e.From == state.Body))
                queue.Enqueue((edge.To, edge.Assignments.Select(t => t.Apply(environment)).ToArray()));
        }
        return result;
    }
    private static bool Catch(Term thrown, Term caught, BaseRule[] rules)
    {
        if (thrown.Occurrences().Any() || caught.Occurrences().Any()) throw new InvalidOperationException("Only closed catch queries.");
        var seen = new HashSet<string>(StringComparer.Ordinal);
        Term? current = thrown;
        while (current != null)
        {
            if (current.Key == caught.Key) return true;
            var head = current.Head ?? throw new InvalidOperationException("Ground nominal type expected.");
            if (!seen.Add(head.Key)) throw new InvalidOperationException("Invalid cyclic base declarations.");
            var rule = rules.SingleOrDefault(r => r.Head.Key == head.Key);
            if (rule?.Base == null) return false;
            var environment = rule.Binders.Select((b, i) => (b.Key, Type: current.Arguments[i])).ToDictionary(x => x.Key, x => x.Type);
            current = rule.Base.Apply(environment);
        }
        return false;
    }
    public static void Main(string[] args)
    {
        var number = new Owner("System.Private.CoreLib,neutral", "core", "System.Int32", 0);
        var text = new Owner("System.Private.CoreLib,neutral", "core", "System.String", 0);
        var exception = new Owner("System.Private.CoreLib,neutral", "core", "System.Exception", 0);
        var list = new Owner("System.Private.CoreLib,neutral", "core", "List`1", 1);
        var dictionary = new Owner("System.Private.CoreLib,neutral", "core", "Dictionary`2", 2);
        var pair = new Owner("AssemblyA", "moduleA", "Pair`2", 2);
        var fault = new Owner("AssemblyA", "moduleA", "Fault`1", 1);
        var fault2 = new Owner("AssemblyA", "moduleA", "Fault`2", 2);
        var derived = new Owner("AssemblyA", "moduleA", "Derived`1", 1);
        var foreign = fault with { Assembly = "AssemblyB", Module = "moduleB" };
        var outer = new Owner("AssemblyA", "moduleA", "Outer`1", 1);
        var leaf = new Owner("AssemblyA", "moduleA", "Outer`1.Leaf`1", 1, "Outer`1");
        Owner[] manifest = [number, text, exception, list, dictionary, pair, fault, fault2, derived, foreign, outer, leaf];
        var t = new Binder("AssemblyA/moduleA/M", "Method", 0);
        var u = new Binder("AssemblyA/moduleA/M", "Method", 1);
        var containing = new Binder("AssemblyA/moduleA/Outer`1", "Type", 0);
        var body = new Body("M", [t], C(fault, V(t)));
        Schema One(Term map) => new([body], [new("M", "M", [map])], manifest, "M", [C(number)], []);
        var identity = One(V(t)); var closing = One(C(text)); var growing = One(C(list, V(t)));
        Check("obvious-identity-exact", Classify(identity).Exact);
        Check("identity-finite-one-context", ExpandFinite(identity).Count == 1);
        Check("constant-closing-exact", Classify(closing).Exact && ExpandFinite(closing).Count == 2);
        Check("growing-projection-outside", !Classify(growing).Exact && Classify(growing).Reasons.Contains("PositiveConstructorCycle"));
        Check("membership-before-concrete-expansion", Classify(growing).ConcreteContextsMaterialized == 0);
        var swappedBody = new Body("M", [t, u], C(fault2, V(t), V(u)));
        var swap = new Schema([swappedBody], [new("M", "M", [V(u), V(t)])], manifest, "M", [C(number), C(text)], []);
        Check("zero-weight-mutual-parameter-cycle-exact", Classify(swap).Exact && ExpandFinite(swap).Count == 2);
        var acyclicGrowth = swap with { Edges = [new("M", "M", [C(list, V(u)), C(number)])] };
        Check("acyclic-variable-growth-in-call-cycle-exact", Classify(acyclicGrowth).Exact && ExpandFinite(acyclicGrowth).Count == 3);
        var reset = new Schema([body, new("N", [u])], [new("M", "N", [C(list, V(t))]), new("N", "M", [C(number)])], manifest, "M", [C(text)], []);
        Check("growth-then-reset-finite", Classify(reset).Exact && ExpandFinite(reset).Count == 4);
        var mutual = reset with { Edges = [new("M", "N", [C(list, V(t))]), new("N", "M", [V(u)])] };
        Check("scoped-mutual-growing-cycle-outside", !Classify(mutual).Exact);
        Check("duplicating-growing-cycle-outside", !Classify(One(C(pair, V(t), C(list, V(t))))).Exact);
        var source = C(fault, C(list, V(t)));
        var environment = new Dictionary<string, Term> { [t.Key] = C(number), [u.Key] = C(text) };
        Check("finite-wrapped-source-projection", source.Apply(environment).Key == C(fault, C(list, C(number))).Key);
        var inner = C(dictionary, C(text), C(list, V(t)));
        var substituted = inner.Apply(environment);
        Check("finite-substitution-composition", substituted.Key == C(dictionary, C(text), C(list, C(number))).Key);
        var first = new Dictionary<string, Term> { [u.Key] = C(list, V(t)) };
        var composed = new Dictionary<string, Term> { [u.Key] = first[u.Key].Apply(environment) };
        Check("composition-two-scopes", C(fault, V(u)).Apply(first).Apply(environment).Key == C(fault, V(u)).Apply(composed).Key);
        var simultaneous = new Dictionary<string, Term> { [t.Key] = V(u), [u.Key] = C(number) };
        Check("simultaneous-not-recursive-text-replacement", V(t).Apply(simultaneous).Key == V(u).Key);
        var faultParameter = new Binder("AssemblyA/moduleA/Fault`1", "Type", 0);
        var derivedParameter = new Binder("AssemblyA/moduleA/Derived`1", "Type", 0);
        BaseRule[] rules = [new(fault, [faultParameter], C(exception)),
            new(derived, [derivedParameter], C(fault, V(derivedParameter))), new(exception, [], null)];
        Check("concrete-catch-exact", Catch(C(fault, C(number)), C(fault, C(number)), rules));
        Check("different-generic-argument-not-caught", !Catch(C(fault, C(text)), C(fault, C(number)), rules));
        Check("inheritance-catch-exact", Catch(C(derived, C(number)), C(fault, C(number)), rules));
        Check("base-exception-catch-exact", Catch(C(derived, C(text)), C(exception), rules));
        Check("assembly-sensitive-catch", !Catch(C(foreign, C(number)), C(fault, C(number)), rules));
        var nestedInt = Nested(leaf, C(outer, C(number)), C(text));
        var nestedText = Nested(leaf, C(outer, C(text)), C(text));
        Check("nested-containing-identity-preserved", !Catch(nestedInt, nestedText, []) && Catch(nestedInt, nestedInt, []));
        var finiteUnion = new[] { C(fault, C(number)), C(fault, C(text)) };
        var caught = finiteUnion.Where(x => Catch(x, finiteUnion[0], rules)).ToArray();
        var remainder = finiteUnion.Where(x => !Catch(x, finiteUnion[0], rules)).ToArray();
        Check("finite-union-catch-remainder-partition", caught.Length == 1 && remainder.Length == 1 && caught.Concat(remainder).Select(x => x.Key).ToHashSet().SetEquals(finiteUnion.Select(x => x.Key)));
        var hard = new Schema([swappedBody with { Catches = [C(fault2, V(t), V(t))] }],
            [new("M", "M", [C(list, V(t)), C(list, C(list, V(u)))]), new("M", "M", [C(list, C(list, V(t))), C(list, V(u))])],
            manifest, "M", [C(number), C(number)], []);
        Check("a8-pcp-shape-outside-before-expansion", !Classify(hard).Exact && Classify(hard).Reasons.Contains("CorrelatedPredicateOutsideFiniteOrbit") && Classify(hard).ConcreteContextsMaterialized == 0);
        Check("finite-correlation-is-not-rejected", Classify(swap with { Bodies = hard.Bodies }).Exact);
        Check("repeated-query-same-classification", JsonSerializer.Serialize(Classify(hard)) == JsonSerializer.Serialize(Classify(hard)));
        var reordered = reset with { Bodies = reset.Bodies.Reverse().ToArray(), Edges = reset.Edges.Reverse().ToArray(), Manifest = manifest.Reverse().ToArray() };
        Check("canonical-equivalent-order-same-classification", JsonSerializer.Serialize(Classify(reset)) == JsonSerializer.Serialize(Classify(reordered)));
        Check("new-canonical-objects-same-identity", C(fault with { }, C(number with { })).Key == C(fault, C(number)).Key);
        Check("owner-sensitive-exact-projections-distinct", C(foreign, C(number)).Key != C(fault, C(number)).Key);
        Check("method-and-containing-binders-distinct", V(t).Key != V(containing).Key);
        var nestedTemplate = Nested(leaf, C(outer, V(containing)), V(t));
        var nestedEnvironment = new Dictionary<string, Term> { [containing.Key] = C(number), [t.Key] = C(text) };
        Check("nested-projection-two-scopes", nestedTemplate.Apply(nestedEnvironment).Key == nestedInt.Key);
        Check("ambiguous-owner-certificate", Classify(identity with { Manifest = manifest.Append(fault).ToArray() }).Reasons.Contains("OwnerBindingAmbiguous"));
        Check("missing-owner-certificate", Classify(identity with { Manifest = manifest.Where(x => x != fault).ToArray() }).Reasons.Contains("OwnerBindingMissing"));
        Check("cross-scope-map-certificate", Classify(One(V(containing))).Reasons.Contains("SubstitutionMapNotClosed"));
        Check("open-root-certificate", !Classify(identity with { Seed = [V(t)] }).Exact);
        Check("invalid-arity-representation-certificate", !Classify(One(C(list))).Exact);
        Check("incomplete-skeleton-certificate", !Classify(identity with { Blockers = ["TargetClosureIncomplete"] }).Exact);
        Check("no-normal-findings-from-certificate", !Classify(growing).Exact);
        // Exhaustive finite schemas: two binders, maps from {T,U,int,List<T>,List<U>}.
        // On this deliberately finite alphabet the checker runs for EVERY candidate;
        // only admitted candidates are expanded, with no iteration/depth budget.
        Term[] choices = [V(t), V(u), C(number), C(list, V(t)), C(list, V(u))];
        int accepted = 0, rejected = 0, largest = 0;
        foreach (var a in choices) foreach (var b in choices)
            {
                var candidate = swap with { Edges = [new("M", "M", [a, b])] };
                var verdict = Classify(candidate);
                if (verdict.Exact) { accepted++; largest = Math.Max(largest, ExpandFinite(candidate).Count); }
                else rejected++;
            }
        Check("membership-finite-schema-enumeration-terminates", accepted > 0 && rejected > 0 && accepted + rejected == 25,
            new { Accepted = accepted, Rejected = rejected, LargestFiniteOrbit = largest, NoDepthOrIterationLimit = true });
        var output = new
        {
            Package = "P5O2B25A9",
            ContractProofOnly = true,
            NoProductDependency = true,
            ProductAnalyzerExecuted = false,
            ExistingTestsChanged = false,
            Profile = "A9-FiniteOrbit-v1",
            Checks,
            RequiredGroups = 12,
            TerminationProofByFiniteEnumeration = false,
            MathematicalProof = "Report: nonnegative constructor weights, no positive edge on a dependency cycle implies bounded term height over finite signature",
            HardPattern = Classify(hard),
            Growth = Classify(growing),
            Reset = Classify(reset),
            EnumeratedSchemas = 25
        };
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[0]))!);
        File.WriteAllText(args[0], JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"PASS: {Checks.Count} A9 standalone finite contract checks; no product/analyzer dependency or execution.");
    }
}
