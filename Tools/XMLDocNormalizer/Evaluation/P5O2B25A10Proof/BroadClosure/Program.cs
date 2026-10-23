using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

try
{
string tool = args[0];
using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(tool, "Evaluation/P5O2B25A10Proof/Complete-fixture-preflight.json")));
if (args.Length > 1 && args[1] == "remaining")
{
    List<object> remainingPlans = [];
    foreach (var row in document.RootElement.GetProperty("Changes").EnumerateArray().Where(row => row.GetProperty("Group").GetString() == "R10"
        || row.GetProperty("File").GetString()!.EndsWith("DOC611_OneBasedSourcePositionFactsTests.cs")
        || row.GetProperty("File").GetString()!.EndsWith("DOC611_ConditionalWeakTableReturnFactsTests.cs")))
    {
        string file = row.GetProperty("File").GetString()!.Replace('\\', '/');
        string name = row.GetProperty("Case").GetString()!.Split('.')[1].Split('#', ' ')[0];
        var root = CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(tool, file))).GetRoot();
        var method = root.DescendantNodes().OfType<MethodDeclarationSyntax>().Single(method => method.Identifier.ValueText == name);
        string before = method.ToString(), after = before;
        if (row.GetProperty("Group").GetString() == "R10")
        {
            string current = row.GetProperty("Assertions")[0].GetProperty("Current").GetString()!;
            string pattern = string.Join(@"\s*", Regex.Split(current, @"\s+").Where(part => part.Length != 0).Select(Regex.Escape));
            var matches = Regex.Matches(before, pattern);
            if (matches.Count != 1) throw new InvalidOperationException("Ambiguous R10 assertion: " + name);
            var match = matches[0];
            after = before.Remove(match.Index, match.Length).Insert(match.Index, match.Value.Replace("AnalysisFailure", "UnsupportedContext").Replace("analysisFailure", "unsupportedContext"));
        }
        else if (file.EndsWith("DOC611_OneBasedSourcePositionFactsTests.cs"))
        {
            var assertion = method.DescendantNodes().OfType<InvocationExpressionSyntax>().Single(call => call.Expression.ToString() == "Assert.Contains");
            after = before.Remove(assertion.SpanStart - method.SpanStart, assertion.Span.Length).Insert(assertion.SpanStart - method.SpanStart,
                "ExceptionFlowFiniteOrbitAssertions.CoverageOutside(source, findings, XMLDocNormalizer.Models.ExceptionAnalysisMode.ProjectTransitive, MetadataReference.CreateFromFile(typeof(SyntaxTree).Assembly.Location))");
        }
        else
        {
            var call = method.DescendantNodes().OfType<InvocationExpressionSyntax>().Single(call => call.Expression.ToString() == "AssertDownstreamArgumentNullExceptionPresentInBothTransitiveModes");
            after = before.Remove(call.SpanStart - method.SpanStart, call.Span.Length).Insert(call.SpanStart - method.SpanStart,
                "AssertDownstreamArgumentNullExceptionPresent(ExceptionFlowAnalyzerTestHelper.AnalyzeTransitively(source, \"M\"));\n            ExceptionFlowFiniteOrbitAssertions.CoverageOutside(ExceptionFlowAnalyzerTestHelper.AnalyzeSolutionTransitively(source, \"M\"))");
        }
        remainingPlans.Add(new { File = file, Edits = new[] { new { Before = before, After = after } }, Case = row.GetProperty("Case").GetString() });
    }
    Console.WriteLine(JsonSerializer.Serialize(remainingPlans));
    return 0;
}
var rows = document.RootElement.GetProperty("Changes").EnumerateArray().Where(row => row.GetProperty("Group").GetString() is "R7" or "R8" or "R9").ToArray();
List<object> plans = [];
string Compact(string value) => Regex.Replace(value, @"\s+", "");
foreach (var group in rows.GroupBy(row => row.GetProperty("File").GetString()!.Replace('\\', '/')))
{
    if (args.Length > 1 && !group.Key.EndsWith(args[1], StringComparison.Ordinal)) continue;
    string source = File.ReadAllText(Path.Combine(tool, group.Key));
    var root = CSharpSyntaxTree.ParseText(source).GetRoot();
    var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>().ToDictionary(method => method.Identifier.ValueText);
    HashSet<string> facts = group.Select(row => row.GetProperty("Case").GetString()!.Split('.')[1].Split('#')[0]).ToHashSet();
    var replacements = new Dictionary<string, string>();
    var grants = group.SelectMany(row => row.GetProperty("Assertions").EnumerateArray()).Select(row => Compact(row.GetProperty("Current").GetString()!).TrimEnd(';')).ToHashSet();
    bool HasGrantedAssertion(MethodDeclarationSyntax method, HashSet<string> visited)
    {
        if (!visited.Add(method.Identifier.ValueText)) return false;
        return method.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(call => grants.Contains(Compact(call.ToString()))
            || (call.Expression is IdentifierNameSyntax name && methods.TryGetValue(name.Identifier.ValueText, out var next) && HasGrantedAssertion(next, visited)));
    }
    string? Transform(string name)
    {
        if (replacements.ContainsKey(name)) return facts.Contains(name) ? name : name + "A10Outside";
        var method = methods[name];
        var calls = method.DescendantNodes().OfType<InvocationExpressionSyntax>().ToArray();
        var productive = calls.Where(call => call.Expression is MemberAccessExpressionSyntax access
            && access.Name.Identifier.ValueText is "FindSemanticExceptionFindingsForSource" or "AnalyzeSolutionTransitively")
            .Where(call => call.Ancestors().OfType<VariableDeclaratorSyntax>().Any()).ToArray();
        string text = method.ToString();
        List<(int Start, int Length, string Text)> edits = [];
        if (productive.Length != 0)
        {
            var variables = productive.Select(call => (Call: call, Variable: call.Ancestors().OfType<VariableDeclaratorSyntax>().First().Identifier.ValueText)).ToArray();
            HashSet<string> dependent = variables.Select(pair => pair.Variable).ToHashSet();
            List<StatementSyntax> remove = [];
            foreach (var statement in method.Body!.Statements)
            {
                if (productive.Any(call => statement.Span.Contains(call.Span))) continue;
                bool references = statement.DescendantNodes().OfType<IdentifierNameSyntax>().Any(id => dependent.Contains(id.Identifier.ValueText));
                bool granted = statement.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(call => grants.Contains(Compact(call.ToString()))
                    || call.Expression is IdentifierNameSyntax local && methods.TryGetValue(local.Identifier.ValueText, out var next) && HasGrantedAssertion(next, []));
                if (!references || !granted) continue;
                remove.Add(statement);
                foreach (var variable in statement.DescendantNodes().OfType<VariableDeclaratorSyntax>()) dependent.Add(variable.Identifier.ValueText);
            }
            if (remove.Count == 0) throw new InvalidOperationException("No exact granted statements: " + group.Key + ":" + name);
            string replacement = string.Join("\n            ", variables.Select(pair => pair.Call.Expression is MemberAccessExpressionSyntax access
                && access.Name.Identifier.ValueText == "AnalyzeSolutionTransitively"
                ? $"ExceptionFlowFiniteOrbitAssertions.CoverageOutside({pair.Variable});"
                : $"ExceptionFlowFiniteOrbitAssertions.CoverageOutside({pair.Call.ArgumentList.Arguments[0]}, {pair.Variable}, {pair.Call.ArgumentList.Arguments[1]});"));
            for (int index = 0; index < remove.Count; index++)
                edits.Add((remove[index].SpanStart - method.SpanStart, remove[index].Span.Length, index == 0 ? replacement : ""));
        }
        else
        {
            foreach (var call in calls.Where(call => call.Expression is IdentifierNameSyntax))
            {
                string target = ((IdentifierNameSyntax)call.Expression).Identifier.ValueText;
                if (!methods.TryGetValue(target, out var next) || !HasGrantedAssertion(next, [])) continue;
                string? renamed = Transform(target);
                if (renamed != null && renamed != target)
                    edits.Add((call.Expression.SpanStart - method.SpanStart, call.Expression.Span.Length, renamed));
            }
        }
        if (edits.Count == 0) return null;
        foreach (var edit in edits.OrderByDescending(edit => edit.Start)) text = text.Remove(edit.Start, edit.Length).Insert(edit.Start, edit.Text);
        if (!facts.Contains(name))
            text = text.Remove(method.Identifier.SpanStart - method.SpanStart, method.Identifier.Span.Length)
                .Insert(method.Identifier.SpanStart - method.SpanStart, name + "A10Outside");
        replacements.Add(name, text);
        return facts.Contains(name) ? name : name + "A10Outside";
    }
    foreach (string name in facts) Transform(name);
    string result = source;
    foreach (var pair in replacements.OrderByDescending(pair => methods[pair.Key].SpanStart))
    {
        var method = methods[pair.Key];
        result = facts.Contains(pair.Key)
            ? result.Remove(method.SpanStart, method.Span.Length).Insert(method.SpanStart, pair.Value)
            : result.Insert(method.Span.End, "\n\n        // A10: only explicitly approved outside-profile callers use this variant.\n        " + pair.Value);
    }
    plans.Add(new { File = group.Key, Edits = replacements.Select(pair => new { Before = methods[pair.Key].ToString(),
        After = facts.Contains(pair.Key) ? pair.Value : methods[pair.Key].ToString() + "\n\n        // A10: only explicitly approved outside-profile callers use this variant.\n        " + pair.Value }).ToArray(), Facts = facts.Order().ToArray() });
}
Console.WriteLine(JsonSerializer.Serialize(plans));
return 0;
}
catch (Exception error) { Console.Error.WriteLine(error); return 1; }
