// Read-only syntax inspection. Run with installed VS csi.exe; no project build,
// test execution, source evaluation, file output, or generated obj artifacts.
#r "Microsoft.CodeAnalysis.dll"
#r "Microsoft.CodeAnalysis.CSharp.dll"
#r "System.Web.Extensions"
using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections;
using System.Web.Script.Serialization;
var serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue, RecursionLimit = 200 };
var catalog = serializer.Deserialize<Dictionary<string, object>>(File.ReadAllText("Evaluation/P5O2B25A10Proof/Complete-fixture-preflight.json"));
var targets = new HashSet<string>();
foreach (Dictionary<string, object> row in (IEnumerable)catalog["WorkerMethods"])
    if (((IEnumerable)row["Cases"]).Cast<Dictionary<string, object>>().Any(c => ((IEnumerable)c["Assertions"]).Cast<Dictionary<string, object>>().Any(a => (string)a["Classification"] == "AMBIGUOUS"))) targets.Add((string)row["Test"]);
bool targetsOnly = Args.Contains("--targets-only");
bool bindingsOnly = Args.Contains("--bindings");
foreach (var path in Args.Where(a => !a.StartsWith("--")))
{
    Console.WriteLine("FILE " + path);
    var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(path));
    if (bindingsOnly)
    {
        foreach (Dictionary<string, object> row in (IEnumerable)catalog["WorkerMethods"])
        {
            if (!targets.Contains((string)row["Test"]) || ((string)row["File"]).Replace('\\','/') != path.Replace('\\','/')) continue;
            Console.WriteLine((string)row["Test"]);
            var observations = ((IEnumerable)row["Cases"]).Cast<Dictionary<string, object>>().SelectMany(c => ((IEnumerable)c["Assertions"]).Cast<Dictionary<string, object>>()).Where(a => (string)a["Classification"] == "AMBIGUOUS").GroupBy(a => a["File"] + ":" + a["Line"] + ":" + a["Current"]).Select(g => g.First());
            foreach (var a in observations) Console.WriteLine(a["File"] + ":" + a["Line"] + " " + a["Current"]);
        }
        continue;
    }
    foreach (var method in tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>())
    {
        var owner = method.Ancestors().OfType<ClassDeclarationSyntax>().First().Identifier.ValueText;
        if (!targets.Contains(owner + "." + method.Identifier.ValueText) && method.AttributeLists.Count != 0) continue;
        if (targetsOnly && !targets.Contains(owner + "." + method.Identifier.ValueText)) continue;
        Console.WriteLine("LINE " + (tree.GetLineSpan(method.Span).StartLinePosition.Line + 1));
        Console.WriteLine(method.ToString());
    }
}
