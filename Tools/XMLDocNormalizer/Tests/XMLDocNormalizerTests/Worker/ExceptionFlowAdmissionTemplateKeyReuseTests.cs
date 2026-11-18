using System.Reflection;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizerTests.Helpers;
using Xunit.Abstractions;
using static XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowFiniteOrbitProfile;

namespace XMLDocNormalizerTests.Worker;

/// <summary>Exercises the actual sealing query and compares complete discovery products with the old query.</summary>
public sealed class ExceptionFlowAdmissionTemplateKeyReuseTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData("empty")]
    [InlineData("single")]
    [InlineData("distinct")]
    [InlineData("duplicates")]
    [InlineData("same-reference")]
    [InlineData("equal-keys-different-arrays")]
    [InlineData("reversed-equal-keys")]
    [InlineData("ordinal-and-escaping")]
    [InlineData("generic-bindings")]
    [InlineData("nested")]
    public void ActualSealingQueryPreservesFullTermsAndFirstRepresentatives(string shape)
    {
        Term first = Term.Named("Owner", Term.Var("T"));
        Term equal = Term.Named("Owner", Term.Var("T"));
        Assert.NotEqual(first, equal);
        Assert.Equal(first.Key, equal.Key);
        Term[] terms = shape switch
        {
            "empty" => [],
            "single" => [first],
            "distinct" => [Term.Named("Z"), Term.Named("A"), Term.Var("T")],
            "duplicates" => [first, equal, Term.Named("Other"), Term.Named("Other"), first],
            "same-reference" => [first, first, first],
            "equal-keys-different-arrays" => [first, equal],
            "reversed-equal-keys" => [equal, first],
            "ordinal-and-escaping" => [Term.Named("a"), Term.Named("Z"), Term.Named("ä"), Term.Named("A"), Term.Named("\"\\\nλ")],
            "generic-bindings" => [Term.Named("Fault", Term.Var("T")), Term.Named("Fault", Term.Var("U")), Term.Named("Fault", Term.Named("Int32")), Term.Named("Fault", Term.Named("String"))],
            "nested" => [Term.Named("Outer", Term.Named("Inner", Term.Var("T"))), Term.Named("Outer", Term.Named("Inner", Term.Var("T"))), Term.Named("Outer", Term.Named("Inner", Term.Var("U")))],
            _ => throw new ArgumentOutOfRangeException(nameof(shape))
        };
        Func<CountedTemplate[], CountedTemplate[]> query = ActualQuery();
        CountedTemplate[] source = Wrap(terms);
        CountedTemplate[] expected = Legacy(source);
        CountedTemplate[] actual = query(source);
        Assert.Equal(Canonical(expected), Canonical(actual));
        Assert.Equal(expected.Length, actual.Length);
        for (int index = 0; index < expected.Length; index++)
        {
            Assert.Same(expected[index], actual[index]);
            Assert.Same(expected[index].Value, actual[index].Value);
        }
        Assert.Equal(terms.Select(term => term.Key).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal),
            actual.Select(item => ((Term)item.Value).Key));
    }

    [Fact]
    public void ChangedArgumentArrayIsObservedByTheNextSealingActivation()
    {
        Term[] arguments = [Term.Named("Before")];
        Term mutable = Term.Named("Owner", arguments);
        Func<CountedTemplate[], CountedTemplate[]> query = ActualQuery();
        string before = Canonical(query(Wrap([mutable, Term.Named("Owner", Term.Named("Before"))])));
        arguments[0] = Term.Named("After");
        CountedTemplate[] source = Wrap([mutable, Term.Named("Owner", Term.Named("Before"))]);
        string after = Canonical(query(source));
        Assert.NotEqual(before, after);
        Assert.Equal(Canonical(Legacy(source)), after);
        Assert.Equal(2, query(Wrap([mutable, Term.Named("Owner", Term.Named("Before"))])).Length);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnsupportedNullOrCyclicTermsKeepTheirDeterministicFailure(bool cyclic)
    {
        Term[] terms;
        if (cyclic)
        {
            Term[] arguments = new Term[1];
            Term cycle = Term.Named("Cycle", arguments);
            arguments[0] = cycle;
            terms = [Term.Named("Valid"), cycle];
        }
        else
        {
            terms = [Term.Named("Valid"), null!];
        }
        Func<CountedTemplate[], CountedTemplate[]> query = ActualQuery();
        Exception expected = Assert.ThrowsAny<Exception>(() => Legacy(Wrap(terms)));
        Exception actual = Assert.ThrowsAny<Exception>(() => query(Wrap(terms)));
        Assert.Equal(expected.GetType(), actual.GetType());
    }

    [Fact]
    public void SealingQueryComputesEveryTemplateKeyOnce()
    {
        Func<CountedTemplate[], CountedTemplate[]> query = ActualQuery();
        foreach ((int count, int unique) in new[] { (8, 8), (16, 4), (32, 8) })
        {
            Term[] terms = Enumerable.Range(0, count).Select(index => Term.Named("Owner", Term.Named("Type" + index % unique))).ToArray();
            CountedTemplate[] oldInput = Wrap(terms);
            CountedTemplate[] newInput = Wrap(terms);
            CountedTemplate[] expected = Legacy(oldInput);
            CountedTemplate[] actual = query(newInput);
            Assert.Equal(Canonical(expected), Canonical(actual));
            Assert.Equal(count + unique, oldInput.Sum(item => item.Calls));
            Assert.Equal(count, newInput.Sum(item => item.Calls));
            Assert.All(newInput, item => Assert.Equal(1, item.Calls));
            output.WriteLine("PERF09A_KEYS " + JsonSerializer.Serialize(new
            {
                templates = count, unique, before = oldInput.Sum(item => item.Calls), after = newInput.Sum(item => item.Calls),
                canonical = Canonical(actual), proof = "Executed query extracted from the actual Discover Body argument; only the input collection/type is adapted"
            }));
        }
    }

    [Theory]
    [InlineData("empty")]
    [InlineData("throw")]
    [InlineData("duplicate-catches")]
    [InlineData("generic-bindings")]
    [InlineData("recursive-swap")]
    [InlineData("recursive-growth")]
    [InlineData("overlap")]
    [InlineData("unavailable")]
    public void DiscoveryRetainsCompleteCanonicalInventoryAndCertificate(string shape)
    {
        string members = shape switch
        {
            "empty" => "static void Root() {}",
            "throw" => "static void Root() { throw new System.ArgumentException(); }",
            "duplicate-catches" => "static void Root() { try { Leaf(); } catch (System.ArgumentException) {} try { Leaf(); } catch (System.ArgumentException) {} } static void Leaf() {}",
            "generic-bindings" => "static void Root() { Leaf<int>(); Leaf<string>(); } static void Leaf<T>() { throw new Fault<T>(); }",
            "recursive-swap" => "static void Root() { Recur<int, string>(); } static void Recur<T, U>() { Recur<U, T>(); }",
            "recursive-growth" => "static void Root() { Recur<int>(); } static void Recur<T>() { Recur<System.Collections.Generic.List<T>>(); }",
            "overlap" => "static void Root() { Left<int>(); Right<int>(); } static void Left<T>() { Leaf<T>(); } static void Right<T>() { Leaf<T>(); } static void Leaf<T>() { throw new Fault<T>(); }",
            "unavailable" => "static void Root() { Missing(); } static extern void Missing();",
            _ => throw new ArgumentOutOfRangeException(nameof(shape))
        };
        AdmissionCharacterization.Fixture fixture = new([("/PERF09A/Templates.cs", "class Fault<T> : System.Exception {} class Entry { " + members + " }")]);
        ExceptionFlowSemanticEnvironment environment = fixture.Session().Environment;
        ExceptionFlowFiniteOrbitAdmission admission = ExceptionFlowFiniteOrbitAdmission.Discover(fixture.Root("Root"), environment);
        Assert.Empty(ExceptionFlowInvocationSubstitution.Bindings(fixture.Symbol("Root")));
        Dictionary<string, ISymbol> declarations = AdmissionCharacterization.Field<Dictionary<string, ISymbol>>(admission, "declarations");
        Dictionary<string, List<Term>> templates = AdmissionCharacterization.Field<Dictionary<string, List<Term>>>(admission, "templates");
        List<Edge> edges = AdmissionCharacterization.Field<List<Edge>>(admission, "edges");
        List<Reason> reasons = AdmissionCharacterization.Field<List<Reason>>(admission, "reasons");
        Body[] oldBodies = declarations.Select(pair => new Body(pair.Key,
            ExceptionFlowInvocationSubstitution.Bindings(pair.Value).Select(binding => ExceptionFlowInvocationSubstitution.Binder(binding.Formal)).ToArray(),
            templates[pair.Key].DistinctBy(term => term.Key).OrderBy(term => term.Key, StringComparer.Ordinal).ToArray())).ToArray();
        Certificate actual = admission.Outcome;
        Certificate expected = ExceptionFlowFiniteOrbitProfile.Check(oldBodies, edges.Distinct().ToArray(), actual.Root, [], actual.ScopeDigest, reasons);
        Assert.Equal(JsonSerializer.Serialize(expected), JsonSerializer.Serialize(actual));
        Assert.Equal(expected.InventoryWitness, actual.InventoryWitness);
        Assert.Equal(expected.InventoryDigest, actual.InventoryDigest);
        Assert.Equal(Digest(actual.InventoryWitness), actual.InventoryDigest);
        Assert.Equal(expected.Reasons, actual.Reasons);
        Assert.Equal(expected.Complete, actual.Complete);
        using JsonDocument inventory = JsonDocument.Parse(actual.InventoryWitness);
        Assert.Equal(JsonSerializer.Serialize(oldBodies.OrderBy(body => body.Id, StringComparer.Ordinal).ToArray()),
            inventory.RootElement.GetProperty("Bodies").GetRawText());
        if (shape == "duplicate-catches")
        {
            Assert.Contains(templates.Values, terms => terms.Count > terms.Select(term => term.Key).Distinct(StringComparer.Ordinal).Count());
        }
        if (shape == "recursive-growth")
        {
            Assert.Contains(actual.Reasons, reason => reason.Category == "PositiveConstructorCycle");
        }
        output.WriteLine("PERF09A_DISCOVERY " + JsonSerializer.Serialize(new
        {
            shape, actual.InventoryDigest, actual.Complete, reasons = actual.Reasons.Select(reason => reason.Category).ToArray(),
            rawTemplates = templates.Values.Sum(terms => terms.Count), uniqueTemplates = oldBodies.Sum(body => body.Templates.Length),
            keyLengths = templates.Values.SelectMany(terms => terms).Select(term => term.Key.Length).ToArray()
        }));
    }

    // The public adapter is visible to the tiny in-memory compilation. Its getter
    // counts calls while delegating to the real Term.Key; no product counter exists.
    public sealed class CountedTemplate(object value, Func<string> key)
    {
        public object Value { get; } = value;
        public int Calls { get; private set; }
        public string Key
        {
            get
            {
                Calls++;
                return key();
            }
        }
    }

    private static CountedTemplate[] Wrap(Term[] terms)
        => terms.Select(term => new CountedTemplate(term, () => term.Key)).ToArray();

    private static CountedTemplate[] Legacy(CountedTemplate[] source)
        => source.DistinctBy(term => term.Key).OrderBy(term => term.Key, StringComparer.Ordinal).ToArray();

    private static string Canonical(CountedTemplate[] source)
        => JsonSerializer.Serialize(source.Select(item => (Term)item.Value).ToArray());

    private static Func<CountedTemplate[], CountedTemplate[]> ActualQuery()
    {
        string directory = AppContext.BaseDirectory;
        while (!File.Exists(Path.Combine(directory, "XMLDocNormalizer.sln")))
        {
            directory = Directory.GetParent(directory)?.FullName ?? throw new InvalidOperationException("Tool root missing.");
        }
        string path = Path.Combine(directory, "src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowFiniteOrbitAdmission.cs");
        CompilationUnitSyntax source = CSharpSyntaxTree.ParseText(File.ReadAllText(path)).GetCompilationUnitRoot();
        MethodDeclarationSyntax discover = source.DescendantNodes().OfType<MethodDeclarationSyntax>().Single(method => method.Identifier.ValueText == "Discover");
        ObjectCreationExpressionSyntax body = discover.DescendantNodes().OfType<ObjectCreationExpressionSyntax>().Single(creation => creation.Type.ToString() == "Body");
        ExpressionSyntax query = body.ArgumentList!.Arguments[2].Expression;
        ElementAccessExpressionSyntax input = query.DescendantNodesAndSelf().OfType<ElementAccessExpressionSyntax>().Single();
        Assert.Equal("admission.templates[pair.Key]", input.ToString());
        query = query.ReplaceNode(input, SyntaxFactory.IdentifierName("source"));
        string adapter = "global::XMLDocNormalizerTests.Worker.ExceptionFlowAdmissionTemplateKeyReuseTests.CountedTemplate";
        string text = "using System; using System.Linq; public static class ActualQuery { public static " + adapter + "[] Run(" + adapter + "[] source) => " + query + "; }";
        MetadataReference[] extra = [MetadataReference.CreateFromFile(typeof(CountedTemplate).Assembly.Location), MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location)];
        CSharpCompilation compilation = CSharpCompilation.Create("PERF09A.Query." + Guid.NewGuid().ToString("N"),
            [CSharpSyntaxTree.ParseText(text, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp12))],
            MetadataReferences.Default.Concat(extra).DistinctBy(reference => reference.Display, StringComparer.Ordinal),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using MemoryStream image = new();
        Microsoft.CodeAnalysis.Emit.EmitResult emitted = compilation.Emit(image);
        Assert.True(emitted.Success, string.Join(Environment.NewLine, emitted.Diagnostics));
        MethodInfo method = Assembly.Load(image.ToArray()).GetType("ActualQuery")!.GetMethod("Run")!;
        return values =>
        {
            try
            {
                return (CountedTemplate[])method.Invoke(null, [values])!;
            }
            catch (TargetInvocationException exception) when (exception.InnerException != null)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                throw;
            }
        };
    }
}
