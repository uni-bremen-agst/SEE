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
        /// <param name="args">Command-line arguments.</param>
        /// <param name="options">
        /// When this method returns, contains the parsed <see cref="ToolOptions"/> if parsing succeeded;
        /// otherwise, contains null.
        /// </param>
        /// <returns>True if parsing succeeded; otherwise false.</returns>
        public static bool TryParseOptions(string[] args, out ToolOptions? options)
        {
            if (args == null)
            {
                PrintUsage("Arguments must not be null.");
                options = null;
                return false;
            }

            bool checkOnly = HasFlag(args, "--check");
            bool fix = HasFlag(args, "--fix");
            bool cleanBackups = HasFlag(args, "--clean-backups");
            bool useTest = HasFlag(args, "--test");
            bool verbose = HasFlag(args, "--verbose");

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

            string targetPath = GetTargetPathOrDefault(args);

            if (!Directory.Exists(targetPath) && !File.Exists(targetPath))
            {
                PrintUsage($"Target path does not exist: {targetPath}");
                options = null;
                return false;
            }

            XmlDocOptions xmlDocOptions = ParseXmlDocOptions(args);

            OutputFormat outputFormat = ParseOutputFormat(args);
            string? outputPath = GetOptionValue(args, "--output");

            options = new ToolOptions(
                targetPath: targetPath,
                checkOnly: checkOnly,
                cleanBackups: cleanBackups,
                useTest: useTest,
                xmlDocOptions: xmlDocOptions,
                outputFormat: outputFormat,
                outputPath: outputPath,
                verbose: verbose);

            return true;
        }

        /// <summary>
        /// Parses XML documentation-related options from CLI flags.
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        /// <returns>A configured <see cref="XmlDocOptions"/> instance.</returns>
        private static XmlDocOptions ParseXmlDocOptions(string[] args)
        {
            XmlDocOptions xmlDocOptions = new();

            if (HasFlag(args, "--no-check-enum-members"))
            {
                xmlDocOptions.CheckEnumMembers = false;
            }

            if (HasFlag(args, "--check-enum-members"))
            {
                xmlDocOptions.CheckEnumMembers = true;
            }

            if (HasFlag(args, "--no-require-field-summary"))
            {
                xmlDocOptions.RequireSummaryForFields = false;
            }

            if (HasFlag(args, "--require-field-summary"))
            {
                xmlDocOptions.RequireSummaryForFields = true;
            }

            return xmlDocOptions;
        }

        /// <summary>
        /// Parses the output format from the command line.
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        /// <returns>The selected <see cref="OutputFormat"/>.</returns>
        private static OutputFormat ParseOutputFormat(string[] args)
        {
            string? value = GetOptionValue(args, "--format");
            if (string.IsNullOrWhiteSpace(value))
            {
                return OutputFormat.Console;
            }

            return value.Trim().ToLowerInvariant() switch
            {
                "console" => OutputFormat.Console,
                "json" => OutputFormat.Json,
                "sarif" => OutputFormat.Sarif,
                _ => ThrowInvalidFormat(value)
            };
        }

        /// <summary>
        /// Throws an exception for invalid output formats in a single expression-friendly way.
        /// </summary>
        /// <param name="value">The invalid format value.</param>
        /// <returns>Never returns.</returns>
        private static OutputFormat ThrowInvalidFormat(string value)
        {
            PrintUsage($"Invalid value for --format: '{value}'. Expected console|json|sarif.");
            throw new ArgumentException("Invalid output format.", nameof(value));
        }

        /// <summary>
        /// Extracts the target path argument or returns the current directory as default.
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        /// <returns>Target path.</returns>
        private static string GetTargetPathOrDefault(string[] args)
        {
            string? pathArg = args.FirstOrDefault(a => !a.StartsWith("--", StringComparison.OrdinalIgnoreCase));
            return pathArg ?? Directory.GetCurrentDirectory();
        }

        /// <summary>
        /// Checks whether a CLI flag exists.
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        /// <param name="flag">The flag to search for.</param>
        /// <returns>True if present; otherwise false.</returns>
        private static bool HasFlag(string[] args, string flag)
        {
            return args.Any(a => a.Equals(flag, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets the value of an option of the form "--name value".
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        /// <param name="optionName">Option name, e.g. "--format".</param>
        /// <returns>The option value or null.</returns>
        private static string? GetOptionValue(string[] args, string optionName)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (!args[i].Equals(optionName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                int valueIndex = i + 1;
                if (valueIndex >= args.Length)
                {
                    return null;
                }

                string value = args[valueIndex];
                if (value.StartsWith("--", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                return value;
            }

            return null;
        }

        /// <summary>
        /// Prints a usage message including an error description.
        /// </summary>
        /// <param name="error">Validation error to display.</param>
        private static void PrintUsage(string error)
        {
            Console.WriteLine(error);
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  XMLDocNormalizer (--check | --fix) [--test] [--clean-backups] [--format console|json|sarif] [--output path] [path]");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  XMLDocNormalizer --check --format console");
            Console.WriteLine("  XMLDocNormalizer --check --format sarif --output artifacts/findings.sarif");
            Console.WriteLine("  XMLDocNormalizer --fix --test src/");
        }
    }
}
