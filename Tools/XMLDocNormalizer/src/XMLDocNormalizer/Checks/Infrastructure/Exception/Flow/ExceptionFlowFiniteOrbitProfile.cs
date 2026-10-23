using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>Version-neutral nominal terms and finite-orbit admission.</summary>
    /// <remarks>
    /// This checker consumes a finite declaration inventory, never Roslyn
    /// symbols or a concrete-context queue. A slot is qualified by both body
    /// and formal binder; equal containing binders in different bodies are
    /// deliberately different vertices.
    /// </remarks>
    internal static class ExceptionFlowFiniteOrbitProfile
    {
        internal const string Version = "A9-FiniteOrbit-v1";

        internal sealed record Term(string? Variable, string? Owner, Term[] Arguments)
        {
            internal static Term Var(string binder) => new(binder, null, []);
            internal static Term Named(string owner, params Term[] arguments) => new(null, owner, arguments);
            internal string Key => JsonSerializer.Serialize(this);

            internal IEnumerable<(string Binder, int Weight)> Occurrences(int weight = 0)
            {
                if (Variable != null)
                {
                    yield return (Variable, weight);
                    yield break;
                }
                foreach (Term child in Arguments)
                foreach (var occurrence in child.Occurrences(weight + 1))
                    yield return occurrence;
            }

            internal bool IsGround => !Occurrences().Any();

            internal Term Apply(IReadOnlyDictionary<string, Term> environment)
                => Variable != null ? environment[Variable]
                    : Named(Owner!, Arguments.Select(argument => argument.Apply(environment)).ToArray());
        }

        internal sealed record Body(string Id, string[] Binders, Term[] Templates);
        internal sealed record Edge(string From, string To, Term[] Assignments, string Witness);
        internal sealed record Reason(string Category, string Witness);

        /// <summary>An internal outcome, not a new transport/result DTO.</summary>
        internal sealed record Certificate(
            string Profile, string Query, string Root, string ScopeDigest,
            string InventoryDigest, string InventoryWitness, bool Complete, Reason[] Reasons)
        {
            internal string InformationQuality => Complete ? "Exact" : "UncertifiedCandidates";
            internal bool MayMiss => !Complete;
            internal bool MayAdd => !Complete;
            internal bool NormalFindingAdmissionBlocked => !Complete;
            internal string[] Details => new[]
            {
                "Profile=" + Profile, "Query=" + Query, "Root=" + Root,
                "Scope=" + ScopeDigest, "Inventory=" + InventoryDigest,
                "Complete=" + Complete.ToString().ToLowerInvariant(),
                "Quality=" + InformationQuality,
                "MayMiss=" + MayMiss.ToString().ToLowerInvariant(),
                "MayAdd=" + MayAdd.ToString().ToLowerInvariant(),
                "NormalFindingAdmissionBlocked=" + NormalFindingAdmissionBlocked.ToString().ToLowerInvariant()
            }.Concat(Reasons.Select(reason => reason.Category + ": " + reason.Witness)).ToArray();
        }

        internal static string Digest(string text)
            => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));

        internal static Certificate Check(
            Body[] bodies, Edge[] edges, string root, Term[] seed,
            string scopeDigest, IEnumerable<Reason> coverageReasons)
        {
            List<Reason> reasons = [.. coverageReasons];
            foreach (Term initial in seed.Concat(bodies.SelectMany(body => body.Templates))
                         .Concat(edges.SelectMany(edge => edge.Assignments)))
            {
                Stack<Term> terms = new(); terms.Push(initial);
                while (terms.TryPop(out Term? term))
                {
                    if (term.Variable != null ? term.Owner != null || term.Arguments.Length != 0
                        : string.IsNullOrWhiteSpace(term.Owner))
                        reasons.Add(new("RepresentationOutsideGrammar", "Malformed nominal/variable term"));
                    foreach (Term child in term.Arguments) terms.Push(child);
                }
            }
            string Slot(string body, string binder) => JsonSerializer.Serialize(new[] { body, binder });
            Dictionary<string, Body> inventory = new(StringComparer.Ordinal);
            foreach (Body body in bodies)
            {
                if (!inventory.TryAdd(body.Id, body) || body.Binders.Distinct(StringComparer.Ordinal).Count() != body.Binders.Length)
                    reasons.Add(new("ScopeMapNotClosed", "Duplicate declaration/binder: " + body.Id));
                foreach (Term term in body.Templates)
                foreach (var occurrence in term.Occurrences())
                    if (!body.Binders.Contains(occurrence.Binder, StringComparer.Ordinal))
                        reasons.Add(new("ScopeMapNotClosed", "Foreign template binder: " + body.Id + " / " + occurrence.Binder));
            }
            if (!inventory.TryGetValue(root, out Body? rootBody)
                || rootBody.Binders.Length != seed.Length || seed.Any(term => !term.IsGround))
                reasons.Add(new("ScopeMapNotClosed", "Root environment is not total and closed: " + root));

            Dictionary<string, HashSet<string>> forward = new(StringComparer.Ordinal);
            Dictionary<string, HashSet<string>> reverse = new(StringComparer.Ordinal);
            foreach (Body body in bodies)
            foreach (string binder in body.Binders)
            {
                string key = Slot(body.Id, binder);
                forward.TryAdd(key, new(StringComparer.Ordinal));
                reverse.TryAdd(key, new(StringComparer.Ordinal));
            }
            List<(string From, string To, string Witness)> positive = [];
            foreach (Edge edge in edges)
            {
                if (!inventory.TryGetValue(edge.From, out Body? from)
                    || !inventory.TryGetValue(edge.To, out Body? to)
                    || to.Binders.Length != edge.Assignments.Length)
                {
                    reasons.Add(new("ScopeMapNotClosed", edge.Witness + " / non-total edge map"));
                    continue;
                }
                for (int coordinate = 0; coordinate < edge.Assignments.Length; coordinate++)
                foreach (var occurrence in edge.Assignments[coordinate].Occurrences())
                {
                    if (!from.Binders.Contains(occurrence.Binder, StringComparer.Ordinal))
                    {
                        reasons.Add(new("ScopeMapNotClosed", edge.Witness + " / foreign binder " + occurrence.Binder));
                        continue;
                    }
                    string source = Slot(from.Id, occurrence.Binder), target = Slot(to.Id, to.Binders[coordinate]);
                    forward[source].Add(target);
                    reverse[target].Add(source);
                    if (occurrence.Weight > 0)
                        positive.Add((source, target, edge.Witness + " / weight=" + occurrence.Weight + " / " + source + " -> " + target));
                }
            }

            // Iterative Kosaraju: no recursion limit, path budget, or exact
            // context unfolding is part of this declaration-slot check.
            HashSet<string> visited = new(StringComparer.Ordinal);
            List<string> finish = [];
            foreach (string start in forward.Keys.Order(StringComparer.Ordinal))
            {
                if (!visited.Add(start)) continue;
                Stack<(string Node, IEnumerator<string> Next)> stack = new();
                stack.Push((start, forward[start].Order(StringComparer.Ordinal).GetEnumerator()));
                while (stack.Count != 0)
                {
                    var frame = stack.Peek();
                    if (frame.Next.MoveNext())
                    {
                        string next = frame.Next.Current;
                        if (visited.Add(next)) stack.Push((next, forward[next].Order(StringComparer.Ordinal).GetEnumerator()));
                    }
                    else { frame.Next.Dispose(); finish.Add(frame.Node); stack.Pop(); }
                }
            }
            Dictionary<string, int> components = new(StringComparer.Ordinal);
            int component = 0;
            foreach (string start in finish.AsEnumerable().Reverse())
            {
                if (!components.TryAdd(start, component)) continue;
                Stack<string> pending = new(); pending.Push(start);
                while (pending.TryPop(out string? node))
                    foreach (string next in reverse[node])
                        if (components.TryAdd(next, component)) pending.Push(next);
                component++;
            }
            foreach (var edge in positive)
                if (components[edge.From] == components[edge.To])
                {
                    Dictionary<string, string?> predecessor = new(StringComparer.Ordinal) { [edge.To] = null };
                    Queue<string> search = new(); search.Enqueue(edge.To);
                    while (!predecessor.ContainsKey(edge.From) && search.TryDequeue(out string? next))
                        foreach (string successor in forward[next].Order(StringComparer.Ordinal))
                            if (predecessor.TryAdd(successor, next)) search.Enqueue(successor);
                    List<string> path = [];
                    for (string? step = edge.From; step != null; step = predecessor[step]) path.Add(step);
                    path.Reverse();
                    reasons.Add(new("PositiveConstructorCycle", edge.Witness + " / return=" + JsonSerializer.Serialize(path)));
                }

            string canonical = JsonSerializer.Serialize(new
            {
                Version, Scope = scopeDigest, Root = root, Seed = seed.Select(term => term.Key).ToArray(),
                Bodies = bodies.OrderBy(body => body.Id, StringComparer.Ordinal).ToArray(),
                Edges = edges.OrderBy(edge => JsonSerializer.Serialize(edge), StringComparer.Ordinal).ToArray()
            });
            Reason[] sorted = reasons.Distinct().OrderBy(reason => reason.Category, StringComparer.Ordinal)
                .ThenBy(reason => reason.Witness, StringComparer.Ordinal).ToArray();
            return new(Version, "CompleteSummaryProjection", root, scopeDigest, Digest(canonical), canonical, sorted.Length == 0, sorted);
        }
    }
}
