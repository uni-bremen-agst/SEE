using XMLDocNormalizer.Configuration;

namespace XMLDocNormalizer.Cli
{
    /// <summary>
    /// Parses and validates command-line arguments for the tool.
    /// </summary>
    internal static class ArgParsing
    {
        /// <summary>
        /// Parses and validates command-line arguments.
        /// </summary>
        /// <param name="args">The command-line arguments.</param>
        /// <param name="options">
        /// When this method returns, contains the parsed <see cref="ToolOptions"/> if parsing succeeded;
        /// otherwise, contains null.
        /// </param>
        /// <returns>
        /// True if the arguments are valid and <paramref name="options"/> is populated; otherwise, false.
        /// </returns>
        public static bool TryParseOptions(string[] args, out ToolOptions? options)
        {
            bool checkOnly = args.Any(a => a.Equals("--check", StringComparison.OrdinalIgnoreCase));
            bool fix = args.Any(a => a.Equals("--fix", StringComparison.OrdinalIgnoreCase));
            bool cleanBackups = args.Any(a => a.Equals("--clean-backups", StringComparison.OrdinalIgnoreCase));
            bool useTest = args.Any(a => a.Equals("--test", StringComparison.OrdinalIgnoreCase));

            // The first non-flag argument is treated as the target path.
            string? pathArg =
                args.FirstOrDefault(a => !a.StartsWith("--", StringComparison.OrdinalIgnoreCase));

            string targetPath = pathArg ?? Directory.GetCurrentDirectory();

            if (!checkOnly && !fix)
            {
                PrintUsage("Either --check or --fix must be specified.");
                options = null;
                return false;
            }

            if (checkOnly && fix)
            {
                PrintUsage("Please specify either --check or --fix, not both.");
                options = null;
                return false;
            }

            if (!Directory.Exists(targetPath) && !File.Exists(targetPath))
            {
                PrintUsage($"Target path does not exist: {targetPath}");
                options = null;
                return false;
            }

            XmlDocOptions xmlDocOptions = new();

            if (args.Any(a => a.Equals("--no-check-enum-members", StringComparison.OrdinalIgnoreCase)))
            {
                xmlDocOptions.CheckEnumMembers = false;
            }

            if (args.Any(a => a.Equals("--check-enum-members", StringComparison.OrdinalIgnoreCase)))
            {
                xmlDocOptions.CheckEnumMembers = true;
            }

            if (args.Any(a => a.Equals("--no-require-field-summary", StringComparison.OrdinalIgnoreCase)))
            {
                xmlDocOptions.RequireSummaryForFields = false;
            }

            if (args.Any(a => a.Equals("--require-field-summary", StringComparison.OrdinalIgnoreCase)))
            {
                xmlDocOptions.RequireSummaryForFields = true;
            }

            OutputFormat outputFormat = OutputFormat.Console;
            string? outputPath = null;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Equals("--format", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 >= args.Length)
                    {
                        PrintUsage("Missing value after --format.");
                        options = null;
                        return false;
                    }

                    string value = args[i + 1];

                    if (value.Equals("console", StringComparison.OrdinalIgnoreCase))
                    {
                        outputFormat = OutputFormat.Console;
                    }
                    else if (value.Equals("json", StringComparison.OrdinalIgnoreCase))
                    {
                        outputFormat = OutputFormat.Json;
                    }
                    else
                    {
                        PrintUsage($"Unknown --format value: {value}. Supported: console|json.");
                        options = null;
                        return false;
                    }
                }

                if (args[i].Equals("--output", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 >= args.Length)
                    {
                        PrintUsage("Missing value after --output.");
                        options = null;
                        return false;
                    }

                    outputPath = args[i + 1];
                }
            }

            options = new ToolOptions(
                targetPath,
                checkOnly,
                cleanBackups,
                useTest,
                xmlDocOptions,
                outputFormat,
                outputPath);

            return true;
        }

        /// <summary>
        /// Prints a usage message including an error description.
        /// </summary>
        /// <param name="error">The validation error to display.</param>
        private static void PrintUsage(string error)
        {
            Console.WriteLine(error);
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  XMLDocNormalizer (--check | --fix) [--test] [--clean-backups] [path]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --check          Check XML documentation only (no changes).");
            Console.WriteLine("  --fix            Normalize / rewrite XML documentation.");
            Console.WriteLine("  --test           In fix mode, rewrite .bak copies instead of original files.");
            Console.WriteLine("  --clean-backups  Remove .bak files created by this tool.");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  XMLDocNormalizer --check");
            Console.WriteLine("  XMLDocNormalizer --check MyFile.cs");
            Console.WriteLine("  XMLDocNormalizer --fix src/");
            Console.WriteLine("  XMLDocNormalizer --fix --test Test/");
        }
    }
}
