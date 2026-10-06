using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

/// <summary>
/// Rebinds committed production sources in memory without checking out or modifying WIP.
/// Generated trees and the active compilation's references/options remain unchanged.
/// </summary>
internal static class CompositionBaseline
{
    internal static Compilation Load(Compilation compilation, string root, string reference)
    {
        string Git(params string[] arguments)
        {
            ProcessStartInfo start = new("git")
            {
                WorkingDirectory = root,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            foreach (string argument in arguments)
            {
                start.ArgumentList.Add(argument);
            }
            using Process process = Process.Start(start)!;
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(error);
            }
            return output;
        }

        string repository = Git("rev-parse", "--show-toplevel").Trim();
        string prefix = Path.GetRelativePath(repository, root).Replace('\\', '/') + "/src/XMLDocNormalizer/";
        SyntaxTree[] production = compilation.SyntaxTrees.Where(tree =>
            tree.FilePath.StartsWith(Path.Combine(root, "src", "XMLDocNormalizer") + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase)
            && !tree.FilePath.Replace('\\', '/').Contains("/obj/", StringComparison.Ordinal)).ToArray();
        CSharpParseOptions options = (CSharpParseOptions)production.First().Options;
        compilation = compilation.RemoveSyntaxTrees(production);
        foreach (string file in Git("-C", repository, "ls-tree", "-r", "--name-only", reference, "--", prefix)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(file => file.TrimEnd('\r'))
            .Where(file => file.EndsWith(".cs", StringComparison.Ordinal)
                && !file.Contains("/obj/", StringComparison.Ordinal) && !file.Contains("/bin/", StringComparison.Ordinal)))
        {
            compilation = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(Git("show", reference + ":" + file),
                options, Path.Combine(repository, file.Replace('/', Path.DirectorySeparatorChar)), Encoding.UTF8));
        }
        return compilation;
    }
}
