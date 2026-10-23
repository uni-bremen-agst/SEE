using System.Collections;
using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>Audit-only fixture expression interpreter. Never edits or executes test methods.</summary>
internal sealed class FixturePreflight
{
    private sealed record Unknown(string Reason);
    private sealed record Captured(string Kind);
    private sealed record Mode(string Name);
    private readonly string tool;
    private readonly Assembly tests;
    private readonly MetadataReference[] expressionReferences;
    private readonly Dictionary<string, ClassDeclarationSyntax> classes = new(StringComparer.Ordinal);
    private readonly Dictionary<SyntaxTree, SemanticModel> models = new();
    private readonly List<object> cases = new();
    private readonly List<object> captures = new();
    private readonly List<object> errors = new();
    private readonly List<object> assertions = new();
    private readonly Dictionary<string, object?> globals = new(StringComparer.Ordinal);
    private readonly Dictionary<MetadataReference, string> referenceSources = new();
    private string caseId = "";
    private string testId = "";
    private ClassDeclarationSyntax owner = null!;
    private int captureStart;
    private static readonly object? NoReturn = new Captured("no return");

    private FixturePreflight(string tool, string bin)
    {
        this.tool = tool;
        AssemblyLoadContext.Default.Resolving += (_, name) => File.Exists(Path.Combine(bin, name.Name + ".dll"))
            ? AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(bin, name.Name + ".dll")) : null;
        tests = Assembly.LoadFrom(Path.Combine(bin, "XMLDocNormalizerTests.dll"));
        expressionReferences = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Select(p => MetadataReference.CreateFromFile(p)).ToArray();
        string root = Path.Combine(tool, "Tests/XMLDocNormalizerTests");
        foreach (string file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("/obj/", StringComparison.OrdinalIgnoreCase) && !f.Contains("/bin/", StringComparison.OrdinalIgnoreCase)
                && !f.Contains("\\obj\\", StringComparison.OrdinalIgnoreCase) && !f.Contains("\\bin\\", StringComparison.OrdinalIgnoreCase)))
        {
            var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(file), path: file);
            foreach (var type in tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                if (!type.Ancestors().OfType<ClassDeclarationSyntax>().Any()) classes.TryAdd(type.Identifier.ValueText, type);
            }
        }
    }

    internal static void Run(string tool, string export, string bin, bool all, bool expanded = false)
    {
        string result = Path.Combine(tool, expanded ? "artifacts/p5o2b25a10/expanded-semantic-fixtures-r3.json" : all ? "artifacts/p5o2b25a10/all-finding-fixtures-r1.json" : "artifacts/p5o2b25a10/full-fixture-preflight-r3.json");
        if (File.Exists(result)) throw new InvalidOperationException("Preserve prior fixture extraction.");
        var audit = new FixturePreflight(tool, bin);
        using var old = JsonDocument.Parse(File.ReadAllText(Path.Combine(tool, "artifacts/p5o2b25a10/production-finding-probe.json")));
        string[] targets = all ? audit.classes.Values.Where(c => c.SyntaxTree.FilePath.Contains("Check" + Path.DirectorySeparatorChar + "Semantic" + Path.DirectorySeparatorChar + "Exceptions", StringComparison.Ordinal)
                && c.SyntaxTree.GetText().ToString().Contains("FindSemanticExceptionFindingsForSource", StringComparison.Ordinal))
            .SelectMany(c => c.Members.OfType<MethodDeclarationSyntax>().Where(m => m.Modifiers.Any(SyntaxKind.PublicKeyword) && m.AttributeLists.Count > 0)
                .Select(m => c.Identifier.ValueText + "." + m.Identifier.ValueText)).Order(StringComparer.Ordinal).ToArray()
            : old.RootElement.GetProperty("SkippedFixtures").EnumerateArray().Select(e => e.GetProperty("Test").GetString()!).ToArray();
        if (expanded)
        {
            using var index = JsonDocument.Parse(File.ReadAllText(Path.Combine(tool, "artifacts/p5o2b25a10/test-path-inventory-r1.json")));
            targets = index.RootElement.GetProperty("Rows").EnumerateArray().Where(r =>
                (r.GetProperty("File").GetString()!.Contains("Check\\Semantic\\Exceptions", StringComparison.Ordinal)
                 || r.GetProperty("Test").GetString()!.StartsWith("FindingContextForSemanticExceptionDetectorTests.", StringComparison.Ordinal))
                && r.GetProperty("Entries").EnumerateArray().Any(e => e.GetString()!.Contains("FindExceptionSmells", StringComparison.Ordinal)
                    || e.GetString()!.Contains("AnalyzeSolutionTransitivelyThrownExceptions", StringComparison.Ordinal)))
                .Select(r => r.GetProperty("Test").GetString()!).Order(StringComparer.Ordinal).ToArray();
        }
        foreach (string target in targets) audit.Extract(target);
        File.WriteAllText(result, JsonSerializer.Serialize(new {
            Kind = "Read-only current-source fixture extraction; no test methods or input programs executed",
            RequestedMethods = targets.Length, Cases = audit.cases, Captures = audit.captures,
            Assertions = audit.assertions, Unresolved = audit.errors,
            ProductionOrTestFilesChanged = false, CompletedBehavioralClassification = false
        }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"Fixture extraction: {targets.Length} requested methods, {audit.cases.Count} cases, {audit.captures.Count} captured calls, {audit.errors.Count} unresolved expressions. {result}");
    }

    private void Extract(string target)
    {
        testId = target;
        int split = target.LastIndexOf('.');
        owner = classes[target[..split]];
        var method = owner.Members.OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.ValueText == target[(split + 1)..]);
        var inline = method.AttributeLists.SelectMany(a => a.Attributes).Where(a => a.Name.ToString() is "InlineData" or "InlineDataAttribute").ToArray();
        var parameterCases = inline.Length == 0 ? new[] { Array.Empty<object?>() }
            : inline.Select(a => a.ArgumentList!.Arguments.Select(v => Eval(v.Expression, new())).ToArray()).ToArray();
        for (int i = 0; i < parameterCases.Length; i++)
        {
            caseId = target + "#" + i;
            captureStart = captures.Count;
            globals.Clear();
            referenceSources.Clear();
            foreach (var field in owner.Members.OfType<FieldDeclarationSyntax>())
                foreach (var variable in field.Declaration.Variables)
                    if (variable.Initializer != null) globals[variable.Identifier.ValueText] = Eval(variable.Initializer.Value, globals);
            var env = new Dictionary<string, object?>(globals, StringComparer.Ordinal);
            int previousErrors = errors.Count;
            for (int p = 0; p < method.ParameterList.Parameters.Count; p++)
                env[method.ParameterList.Parameters[p].Identifier.ValueText] = p < parameterCases[i].Length ? parameterCases[i][p] : new Unknown("Missing parameter case");
            Execute(method, env, new HashSet<string>(StringComparer.Ordinal));
            if (captures.Count == captureStart && target.EndsWith("KnownFrameworkReturnClassifiers_RequireExactSupportedSignatures", StringComparison.Ordinal))
                captures.Add(new { Case = caseId, Test = testId, Entry = "Finite framework signature classifiers (no Session.Analyze)",
                    Mode = "PrimitiveDiagnostic", Source = env["source"], AdditionalReferenceObjects = Flatten(new[] { env.GetValueOrDefault("references") }).OfType<MetadataReference>().Count(),
                    Classification = "diagnostic / low-level", Reason = "Six framework signature predicate assertions, not a full production analysis" });
            cases.Add(new { Id = caseId, Test = target, Parameters = parameterCases[i].Select(Printable).ToArray(),
                File = Path.GetRelativePath(tool, owner.SyntaxTree.FilePath), Line = Line(method), CapturedCalls = captures.Count - captureStart,
                UnresolvedExpressions = errors.Count - previousErrors });
        }
    }

    private object? Execute(MethodDeclarationSyntax method, Dictionary<string, object?> env, HashSet<string> stack)
    {
        if (!stack.Add(method.Identifier.ValueText)) return new Unknown("Recursive fixture helper: " + method.Identifier.ValueText);
        try
        {
            if (method.ExpressionBody != null) return Eval(method.ExpressionBody.Expression, env, stack);
            foreach (var statement in method.Body?.Statements ?? default)
            {
                object? value = Statement(statement, env, stack);
                if (!ReferenceEquals(value, NoReturn)) return value;
            }
            return NoReturn;
        }
        finally { stack.Remove(method.Identifier.ValueText); }
    }

    private object? Statement(StatementSyntax statement, Dictionary<string, object?> env, HashSet<string> stack)
    {
        switch (statement)
        {
            case LocalDeclarationStatementSyntax local:
                foreach (var v in local.Declaration.Variables)
                    env[v.Identifier.ValueText] = v.Initializer == null ? new Unknown("uninitialized") : Eval(v.Initializer.Value, env, stack);
                return NoReturn;
            case ExpressionStatementSyntax expression:
                Eval(expression.Expression, env, stack); return NoReturn;
            case ReturnStatementSyntax ret:
                return ret.Expression == null ? null : Eval(ret.Expression, env, stack);
            case BlockSyntax block:
                foreach (var child in block.Statements) { var value = Statement(child, env, stack); if (!ReferenceEquals(value, NoReturn)) return value; }
                return NoReturn;
            case IfStatementSyntax conditional:
                object? condition = Eval(conditional.Condition, env, stack);
                if (condition is bool known) return known ? Statement(conditional.Statement, env, stack)
                    : conditional.Else == null ? NoReturn : Statement(conditional.Else.Statement, env, stack);
                errors.Add(new { Case = caseId, Kind = "Unresolved fixture control flow", Line = Line(conditional), Code = conditional.Condition.ToString() });
                return NoReturn;
            case ForEachStatementSyntax loop:
                if (Eval(loop.Expression, env, stack) is IEnumerable values)
                    foreach (object? value in values) { env[loop.Identifier.ValueText] = value; Statement(loop.Statement, env, stack); }
                else errors.Add(new { Case = caseId, Kind = "Unresolved fixture enumeration", Line = Line(loop), Code = loop.Expression.ToString() });
                return NoReturn;
            case ForStatementSyntax loop when loop.Declaration != null:
                foreach (var v in loop.Declaration.Variables) env[v.Identifier.ValueText] = Eval(v.Initializer!.Value, env, stack);
                while (Eval(loop.Condition!, env, stack) is true)
                {
                    Statement(loop.Statement, env, stack);
                    foreach (var increment in loop.Incrementors) Eval(increment, env, stack);
                }
                return NoReturn;
            default:
                errors.Add(new { Case = caseId, Kind = "Uninterpreted fixture statement", Line = Line(statement), Code = statement.ToString() });
                return NoReturn;
        }
    }

    private object? Eval(ExpressionSyntax expression, Dictionary<string, object?> env, HashSet<string>? stack = null)
    {
        stack ??= new(StringComparer.Ordinal);
        switch (expression)
        {
            case LiteralExpressionSyntax literal: return literal.Token.Value;
            case IdentifierNameSyntax name:
                return env.TryGetValue(name.Identifier.ValueText, out var found) ? found : new Unknown("Identifier " + name.Identifier.ValueText);
            case ParenthesizedExpressionSyntax paren: return Eval(paren.Expression, env, stack);
            case BinaryExpressionSyntax binary:
                var left = Eval(binary.Left, env, stack); var right = Eval(binary.Right, env, stack);
                if (left is int li && right is int ri)
                {
                    if (binary.IsKind(SyntaxKind.AddExpression)) return li + ri;
                    if (binary.IsKind(SyntaxKind.LessThanExpression)) return li < ri;
                }
                if (binary.IsKind(SyntaxKind.AddExpression) && left is not Unknown && right is not Unknown)
                    return Convert.ToString(left, System.Globalization.CultureInfo.InvariantCulture) + Convert.ToString(right, System.Globalization.CultureInfo.InvariantCulture);
                if (binary.IsKind(SyntaxKind.EqualsExpression)) return Equals(left, right);
                if (binary.IsKind(SyntaxKind.NotEqualsExpression)) return !Equals(left, right);
                break;
            case ConditionalExpressionSyntax conditional:
                if (Eval(conditional.Condition, env, stack) is bool condition) return Eval(condition ? conditional.WhenTrue : conditional.WhenFalse, env, stack);
                break;
            case InterpolatedStringExpressionSyntax interpolation:
                var pieces = new List<string>();
                foreach (var content in interpolation.Contents)
                {
                    if (content is InterpolatedStringTextSyntax text) pieces.Add(text.TextToken.ValueText);
                    else if (content is InterpolationSyntax hole)
                    {
                        object? value = Eval(hole.Expression, env, stack);
                        if (value is Unknown) return value;
                        pieces.Add(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? "");
                    }
                }
                return string.Concat(pieces);
            case MemberAccessExpressionSyntax member:
                if (member.Expression.ToString() == "ExceptionAnalysisMode") return new Mode(member.Name.Identifier.ValueText);
                if (member.Expression.ToString() is "string" or "String" && member.Name.Identifier.ValueText == "Empty") return "";
                if (member.Name.Identifier.ValueText == "Location" && Eval(member.Expression, env, stack) is Assembly assembly) return assembly.Location;
                if (member.Name.Identifier.ValueText == "Assembly" && Eval(member.Expression, env, stack) is Type type) return type.Assembly;
                break;
            case TypeOfExpressionSyntax typeOf:
                string simple = typeOf.Type.ToString();
                return typeof(Compilation).Assembly.GetTypes().Concat(typeof(CSharpCompilation).Assembly.GetTypes())
                    .FirstOrDefault(t => t.Name == simple || t.FullName == simple) ?? (object)new Unknown("typeof " + simple);
            case ImplicitArrayCreationExpressionSyntax array:
                return array.Initializer.Expressions.Select(e => Eval(e, env, stack)).ToArray();
            case CollectionExpressionSyntax collection:
                return collection.Elements.OfType<ExpressionElementSyntax>().Select(e => Eval(e.Expression, env, stack)).ToArray();
            case ArrayCreationExpressionSyntax array when array.Initializer != null:
                return array.Initializer.Expressions.Select(e => Eval(e, env, stack)).ToArray();
            case TupleExpressionSyntax tuple:
                return tuple.Arguments.Select(a => Eval(a.Expression, env, stack)).ToArray();
            case AwaitExpressionSyntax awaited: return Eval(awaited.Expression, env, stack);
            case ImplicitObjectCreationExpressionSyntax created when created.Ancestors().OfType<VariableDeclarationSyntax>().FirstOrDefault()?.Type.ToString() == "StringBuilder":
                return new System.Text.StringBuilder();
            case ObjectCreationExpressionSyntax created when created.Type.ToString() == "StringBuilder":
                return new System.Text.StringBuilder();
            case PostfixUnaryExpressionSyntax postfix when postfix.IsKind(SyntaxKind.PostIncrementExpression) && postfix.Operand is IdentifierNameSyntax variable:
                int previous = (int)env[variable.Identifier.ValueText]!; env[variable.Identifier.ValueText] = previous + 1; return previous;
            case InvocationExpressionSyntax invocation: return Invoke(invocation, env, stack);
            case AssignmentExpressionSyntax assign when assign.Left is IdentifierNameSyntax variable:
                return env[variable.Identifier.ValueText] = Eval(assign.Right, env, stack);
        }
        var model = Model(expression.SyntaxTree);
        Optional<object?> constant = model.GetConstantValue(expression);
        return constant.HasValue ? constant.Value : new Unknown(expression.Kind() + ": " + expression.ToString());
    }

    private object? Invoke(InvocationExpressionSyntax call, Dictionary<string, object?> env, HashSet<string> stack)
    {
        string name = call.Expression switch { IdentifierNameSyntax simple => simple.Identifier.ValueText, MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText, _ => call.Expression.ToString() };
        if (call.Expression is MemberAccessExpressionSyntax assert && assert.Expression.ToString() == "Assert")
        {
            assertions.Add(new { Case = caseId, Test = testId, File = Path.GetRelativePath(tool, call.SyntaxTree.FilePath), Line = Line(call), Assertion = call.ToString() });
            foreach (var argument in call.ArgumentList.Arguments) Eval(argument.Expression, env, stack);
            return new Captured("assertion observed, not executed");
        }
        var args = call.ArgumentList.Arguments.Select(a => Eval(a.Expression, env, stack)).ToArray();
        if (call.Expression is MemberAccessExpressionSyntax builderCall && Eval(builderCall.Expression, env, stack) is System.Text.StringBuilder builder)
        {
            if (name == "AppendLine" && args.FirstOrDefault() is string text) { builder.AppendLine(text); return builder; }
            if (name == "ToString" && args.Length == 0) return builder.ToString();
        }
        if (call.Expression.ToString() == "Enum.Parse<ExceptionAnalysisMode>" && args[0] is string parsedMode) return new Mode(parsedMode);
        var helper = owner.Members.OfType<MethodDeclarationSyntax>().FirstOrDefault(m => m.Identifier.ValueText == name
            && m.ParameterList.Parameters.Count >= args.Length && m.ParameterList.Parameters.Skip(args.Length).All(p => p.Default != null));
        if (helper != null && call.Expression is IdentifierNameSyntax)
        {
            var scope = new Dictionary<string, object?>(globals, StringComparer.Ordinal);
            for (int i = 0; i < helper.ParameterList.Parameters.Count; i++) scope[helper.ParameterList.Parameters[i].Identifier.ValueText] =
                i < args.Length ? args[i] : Eval(helper.ParameterList.Parameters[i].Default!.Value, scope, stack);
            return Execute(helper, scope, stack);
        }
        if (call.Expression is MemberAccessExpressionSyntax memberCall && memberCall.Expression.ToString() == "Wrapper" && name == "WrapInClass")
            return args[0] is string snippet ? Wrap(snippet) : new Unknown("Wrapper argument");
        if (call.Expression is MemberAccessExpressionSyntax check && check.Expression.ToString() == "CheckAssert" && name.StartsWith("FindSemanticExceptionFindingsFor", StringComparison.Ordinal))
        {
            bool multiple = name.EndsWith("ForSources", StringComparison.Ordinal);
            var sources = multiple ? args.Skip(1).OfType<object?[]>().Select(t => new { Path = t[0] as string, Text = t[1] as string }).ToArray() : null;
            string? source = multiple ? sources?.FirstOrDefault()?.Text : args.FirstOrDefault() as string;
            if (name.EndsWith("ForMember", StringComparison.Ordinal) && source != null) source = Wrap(source);
            string mode = args.OfType<Mode>().FirstOrDefault()?.Name ?? "ProjectTransitiveDeclaredExceptions";
            var extras = Flatten(args.Skip(1)).OfType<MetadataReference>().ToArray();
            if (source == null)
            {
                errors.Add(new { Case = caseId, Kind = "Unresolved productive fixture", Line = Line(call), Code = call.ToString(), Arguments = args.Select(Printable).ToArray() });
            }
            else captures.Add(new { Case = caseId, Test = testId, File = Path.GetRelativePath(tool, call.SyntaxTree.FilePath), Line = Line(call), Entry = name,
                Mode = mode, Source = source, AdditionalReferences = extras.Select(r => r.Display).ToArray(),
                AdditionalReferenceObjects = extras.Length, SourceSha256 = HashText(source), SourceFiles = sources,
                ReferenceDescriptors = extras.Select(r => new { Path = r.Display, Source = referenceSources.GetValueOrDefault(r) }).ToArray(),
                Trace = stack.Order(StringComparer.Ordinal).ToArray() });
            return new Captured("Findings");
        }
        if (call.Expression is MemberAccessExpressionSyntax native && native.Expression.ToString() == "ExceptionFlowAnalyzerTestHelper"
            && name.StartsWith("Analyze", StringComparison.Ordinal) && args[0] is string nativeSource)
        {
            captures.Add(new { Case = caseId, Test = testId, File = Path.GetRelativePath(tool, call.SyntaxTree.FilePath), Line = Line(call), Entry = name,
                Mode = name == "AnalyzeSolutionTransitively" ? "SolutionTransitiveNative" : "DiagnosticNative", Source = nativeSource,
                RootMethod = args.ElementAtOrDefault(1), AdditionalReferences = Flatten(args.Skip(2)).OfType<MetadataReference>().Select(r => r.Display).ToArray(),
                ReferenceDescriptors = Flatten(args.Skip(2)).OfType<MetadataReference>().Select(r => new { Path = r.Display, Source = referenceSources.GetValueOrDefault(r) }).ToArray(),
                AdditionalReferenceObjects = Flatten(args.Skip(2)).OfType<MetadataReference>().Count() });
            return new Captured("Native diagnostic result");
        }
        if (call.Expression is MemberAccessExpressionSyntax detector && detector.Expression.ToString() == "XmlDocExceptionSemanticDetector"
            && name == "FindExceptionSmells" && env.GetValueOrDefault("reportingSource") is string reporting
            && (env.GetValueOrDefault("dependencySource") ?? env.GetValueOrDefault("referencedSource")) is string dependency)
        {
            string actualMode = owner.Members.OfType<MethodDeclarationSyntax>().Single(m => testId.EndsWith("." + m.Identifier.ValueText, StringComparison.Ordinal))
                .DescendantNodes().OfType<AssignmentExpressionSyntax>().Where(a => a.Left.ToString() == "ExceptionAnalysisMode")
                .Select(a => a.Right).OfType<MemberAccessExpressionSyntax>().Select(m => m.Name.Identifier.ValueText).FirstOrDefault() ?? "SolutionTransitive";
            captures.Add(new { Case = caseId, Test = testId, File = Path.GetRelativePath(tool, call.SyntaxTree.FilePath), Line = Line(call), Entry = name,
                Mode = actualMode, Source = reporting, SupportingProjectSource = dependency,
                Fixture = "SolutionTestBuilder.CreateTwoProjectSolution(reportingSource,dependencySource), reporting project references dependency" });
            return new Captured("Cross-project findings");
        }
        // Only known pure metadata-reference fixture helpers may be invoked.
        if (name is "GetRoslynMetadataReferences" or "GetMetadataReferences")
        {
            Type? compiledOwner = tests.GetType("XMLDocNormalizerTests.Check.Semantic.Exception." + owner.Identifier.ValueText);
            MethodInfo? referenceFactory = compiledOwner?.GetMethod(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (referenceFactory != null && referenceFactory.GetParameters().Length == args.Length)
                return referenceFactory.Invoke(null, args);
        }
        if (call.Expression is MemberAccessExpressionSyntax reference && reference.Expression.ToString() == "ExternalDocumentationReferenceTestHelper" && name == "Create")
        {
            Type factory = tests.GetType("XMLDocNormalizerTests.Helpers.ExternalDocumentationReferenceTestHelper", true)!;
            var created = (MetadataReference)factory.GetMethod("Create", BindingFlags.Public | BindingFlags.Static)!.Invoke(null, args)!;
            referenceSources.Add(created, (string)args[0]!);
            return created;
        }
        if (call.Expression is MemberAccessExpressionSyntax metadata && metadata.Expression.ToString() == "MetadataReference" && name == "CreateFromFile" && args[0] is string path)
            return MetadataReference.CreateFromFile(path);
        if (call.Expression is MemberAccessExpressionSyntax replace && name == "Replace" && Eval(replace.Expression, env, stack) is string input
            && args.Length == 2 && args[0] is string from && args[1] is string to) return input.Replace(from, to, StringComparison.Ordinal);
        return new Unknown("Call " + call.Expression);
    }

    private SemanticModel Model(SyntaxTree tree)
    {
        if (!models.TryGetValue(tree, out var model)) models.Add(tree, model = CSharpCompilation.Create("FixtureExpressionAudit", [tree], expressionReferences).GetSemanticModel(tree));
        return model;
    }
    private static IEnumerable<object?> Flatten(IEnumerable<object?> values)
    {
        foreach (object? value in values)
            if (value is IEnumerable sequence && value is not string) foreach (object? child in sequence) yield return child;
            else yield return value;
    }
    private static object? Printable(object? value) => value switch { null => null, string or bool or int or long => value, Mode mode => mode.Name, Unknown unknown => "UNRESOLVED " + unknown.Reason, _ => value.GetType().Name };
    private static int Line(SyntaxNode node) => node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
    private static string HashText(string value) => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value)));
    private static string Wrap(string source) => "/// <summary>\n/// This is a test class.\n/// </summary>\nclass C\n{\n" + source + "\n}\n";
}
