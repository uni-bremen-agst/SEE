using XMLDocNormalizer.Configuration;
using XMLDocNormalizer.Models;

namespace XMLDocNormalizer.Cli
{
    /// <summary>
    /// Parses and validates command-line arguments for the tool.
    /// </summary>
    internal static class ArgParsing
    {
        /// <summary>
        /// Contains all options that expect a value token, for example "--format json".
        /// </summary>
        private static readonly HashSet<string> optionsWithValue =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "--project",
                "--format",
                "--output",
                "--exception-analysis-mode",
                "--exception-analysis-comparison-runs",
                "--exception-analysis-comparison-warmup-runs",
                "--statistics-output",
                "--value-documentation-mode"
            };

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
            options = null;

            if (args == null || args.Length == 0)
            {
                PrintUsage("Arguments must not be null.");
                return false;
            }

            if (HasFlag(args, "--help") || HasFlag(args, "-h"))
            {
                PrintUsage(null);
                return false;
            }

            bool checkOnly = HasFlag(args, "--check");
            bool fix = HasFlag(args, "--fix");
            bool cleanBackups = HasFlag(args, "--clean-backups");
            bool useTest = HasFlag(args, "--test");
            bool verbose = HasFlag(args, "--verbose") || HasFlag(args, "-v");
            bool fullAnalysis = HasFlag(args, "--full");
            bool includeGenerated = HasFlag(args, "--include-generated");
            bool includeTests = HasFlag(args, "--include-tests");
            bool compareExceptionAnalysisModes = HasFlag(args, "--compare-exception-analysis-modes");
            bool enableStatistics = HasFlag(args, "--enable-statistics");

            XmlDocOptions xmlDocOptions = ParseXmlDocOptions(args);

            string? projectName = GetOptionValue(args, "--project");
            OutputFormat outputFormat = ParseOutputFormat(args);
            string? outputPath = GetOptionValue(args, "--output");
            string? statisticsOutputPath = GetOptionValue(args, "--statistics-output");

            if (!TryParseExceptionAnalysisComparisonRuns(
                args,
                out int exceptionAnalysisComparisonRuns))
            {
                return false;
            }

            if (!TryParseExceptionAnalysisComparisonWarmupRuns(
                args,
                out int exceptionAnalysisComparisonWarmupRuns))
            {
                return false;
            }

            if (fullAnalysis && projectName != null)
            {
                PrintUsage("Options --full and --project cannot be used together.");
                return false;
            }

            if (!checkOnly && !fix)
            {
                PrintUsage("Either --check or --fix must be specified.");
                return false;
            }

            if (checkOnly && fix)
            {
                PrintUsage("Please specify either --check or --fix, not both.");
                return false;
            }

            if (compareExceptionAnalysisModes && !checkOnly)
            {
                PrintUsage("Option --compare-exception-analysis-modes requires --check.");
                return false;
            }

            if (HasFlag(args, "--exception-analysis-comparison-runs") &&
                !compareExceptionAnalysisModes)
            {
                PrintUsage("Option --exception-analysis-comparison-runs requires --compare-exception-analysis-modes.");
                return false;
            }

            if (HasFlag(args, "--exception-analysis-comparison-warmup-runs") &&
                !compareExceptionAnalysisModes)
            {
                PrintUsage("Option --exception-analysis-comparison-warmup-runs requires --compare-exception-analysis-modes.");
                return false;
            }

            if (enableStatistics && !checkOnly)
            {
                PrintUsage("Option --enable-statistics requires --check.");
                return false;
            }

            string targetPath = GetTargetPathOrDefault(args);
            if (!Directory.Exists(targetPath) && !File.Exists(targetPath))
            {
                PrintUsage($"Target path does not exist: {targetPath}");
                return false;
            }

            options = new ToolOptions(
                targetPath: targetPath,
                checkOnly: checkOnly,
                cleanBackups: cleanBackups,
                useTest: useTest,
                xmlDocOptions: xmlDocOptions,
                outputFormat: outputFormat,
                outputPath: outputPath,
                verbose: verbose,
                fullAnalysis: fullAnalysis,
                projectName: projectName,
                includeGenerated: includeGenerated,
                includeTests: includeTests,
                compareExceptionAnalysisModes: compareExceptionAnalysisModes,
                exceptionAnalysisComparisonRuns: exceptionAnalysisComparisonRuns,
                exceptionAnalysisComparisonWarmupRuns: exceptionAnalysisComparisonWarmupRuns,
                enableStatistics: enableStatistics,
                statisticsOutputPath: statisticsOutputPath);

            return true;
        }

        /// <summary>
        /// Parses the measured exception analysis comparison run count.
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        /// <param name="runCount">The parsed run count.</param>
        /// <returns>True if parsing succeeded; otherwise false.</returns>
        private static bool TryParseExceptionAnalysisComparisonRuns(
            string[] args,
            out int runCount)
        {
            runCount = 1;

            if (!HasFlag(args, "--exception-analysis-comparison-runs"))
            {
                return true;
            }

            string? value = GetOptionValue(args, "--exception-analysis-comparison-runs");

            if (string.IsNullOrWhiteSpace(value))
            {
                PrintUsage("Option --exception-analysis-comparison-runs requires a positive integer value.");
                return false;
            }

            if (!int.TryParse(value, out int parsedRunCount) ||
                parsedRunCount <= 0)
            {
                PrintUsage(
                    $"Invalid value for --exception-analysis-comparison-runs: '{value}'. " +
                    "Expected a positive integer.");
                return false;
            }

            runCount = parsedRunCount;
            return true;
        }

        /// <summary>
        /// Parses the warmup exception analysis comparison run count.
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        /// <param name="warmupRunCount">The parsed warmup run count.</param>
        /// <returns>True if parsing succeeded; otherwise false.</returns>
        private static bool TryParseExceptionAnalysisComparisonWarmupRuns(
            string[] args,
            out int warmupRunCount)
        {
            warmupRunCount = 0;

            if (!HasFlag(args, "--exception-analysis-comparison-warmup-runs"))
            {
                return true;
            }

            string? value = GetOptionValue(args, "--exception-analysis-comparison-warmup-runs");

            if (string.IsNullOrWhiteSpace(value))
            {
                PrintUsage("Option --exception-analysis-comparison-warmup-runs requires a non-negative integer value.");
                return false;
            }

            if (!int.TryParse(value, out int parsedWarmupRunCount) ||
                parsedWarmupRunCount < 0)
            {
                PrintUsage(
                    $"Invalid value for --exception-analysis-comparison-warmup-runs: '{value}'. " +
                    "Expected a non-negative integer.");
                return false;
            }

            warmupRunCount = parsedWarmupRunCount;
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

            xmlDocOptions.ExceptionAnalysisMode = ParseExceptionAnalysisMode(args);
            xmlDocOptions.ValueDocumentationMode = ParseValueDocumentationMode(args);

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
        /// Parses the exception analysis mode from the command line.
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        /// <returns>The selected exception analysis mode.</returns>
        private static ExceptionAnalysisMode ParseExceptionAnalysisMode(string[] args)
        {
            string? value = GetOptionValue(args, "--exception-analysis-mode");

            if (string.IsNullOrWhiteSpace(value))
            {
                return XmlDocOptions.DefaultExceptionAnalysisMode;
            }

            return value.Trim().ToLowerInvariant() switch
            {
                "direct" or
                "d" =>
                    ExceptionAnalysisMode.Direct,

                "project-transitive-declared-exceptions" or
                "project-transitive-declared" or
                "project-declared" or
                "declared" or
                "ptd" =>
                    ExceptionAnalysisMode.ProjectTransitiveDeclaredExceptions,

                "project-transitive" or
                "project" or
                "pt" =>
                    ExceptionAnalysisMode.ProjectTransitive,

                "solution-transitive" or
                "solution" or
                "st" =>
                    ExceptionAnalysisMode.SolutionTransitive,

                _ => ThrowInvalidExceptionAnalysisMode(value)
            };
        }

        /// <summary>
        /// Throws an exception for invalid exception analysis modes in a single expression-friendly way.
        /// </summary>
        /// <param name="value">The invalid mode value.</param>
        /// <returns>Never returns.</returns>
        private static ExceptionAnalysisMode ThrowInvalidExceptionAnalysisMode(string value)
        {
            PrintUsage(
                $"Invalid value for --exception-analysis-mode: '{value}'. " +
                "Expected direct, project-transitive-declared-exceptions, " +
                "project-transitive or solution-transitive. " +
                "Supported aliases are d, ptd, declared, project-declared, " +
                "project-transitive-declared, pt, project, st and solution.");
            throw new ArgumentException("Invalid exception analysis mode.", nameof(value));
        }

        /// <summary>
        /// Parses the value-documentation mode from the command line.
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        /// <returns>The selected value-documentation mode.</returns>        /
        // <exception cref="ArgumentException">
        /// Thrown when the configured value-documentation mode is invalid.
        /// </exception>
        private static ValueDocumentationMode ParseValueDocumentationMode(string[] args)
        {
            string? value = GetOptionValue(args, "--value-documentation-mode");

            if (string.IsNullOrWhiteSpace(value))
            {
                return XmlDocOptions.DefaultValueDocumentationMode;
            }

            return value.Trim().ToLowerInvariant() switch
            {
                "disabled" or
                "off" or
                "none" =>
                    ValueDocumentationMode.None,

                "all-readable-properties" or
                "all" or
                "readable-properties" or
                "strict" =>
                    ValueDocumentationMode.AllReadableProperties,

                "exclude-dto-like-types" or
                "exclude-dto-like" or
                "exclude-dto" or
                "non-dto" or
                "non-dto-like" =>
                    ValueDocumentationMode.ExcludeDtoLikeTypes,

                "indexers-only" or
                "indexer-only" or
                "indexers" =>
                    ValueDocumentationMode.IndexersOnly,

                _ => ThrowInvalidValueDocumentationMode(value)
            };
        }

        /// <summary>
        /// Throws an exception for invalid value-documentation modes in a single expression-friendly way.
        /// </summary>
        /// <param name="value">The invalid mode value.</param>
        /// <returns>Never returns.</returns>        
        /// <exception cref="ArgumentException">
        /// Always thrown to indicate an invalid value-documentation mode.
        /// </exception>
        private static ValueDocumentationMode ThrowInvalidValueDocumentationMode(string value)
        {
            PrintUsage(
                $"Invalid value for --value-documentation-mode: '{value}'. " +
                "Expected disabled, all-readable-properties, exclude-dto-like-types or indexers-only. " +
                "Supported aliases are off, none, all, strict, readable-properties, " +
                "exclude-dto-like, exclude-dto, non-dto, non-dto-like, indexer-only and indexers.");

            throw new ArgumentException("Invalid value documentation mode.", nameof(value));
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
        /// <returns>
        /// The last positional argument that is not a flag and not an option value.
        /// If no positional argument is present, returns the current directory.
        /// </returns>
        /// <remarks>
        /// A positional argument is a token that does not start with "-".
        /// Values of options with values are skipped and never treated as target paths.
        /// The target path is expected as the last positional argument.
        /// </remarks>
        private static string GetTargetPathOrDefault(string[] args)
        {
            string? candidate = null;

            for (int i = 0; i < args.Length; i++)
            {
                string current = args[i];

                if (IsFlagToken(current))
                {
                    if (optionsWithValue.Contains(current))
                    {
                        i++;
                    }

                    continue;
                }

                candidate = current;
            }

            return candidate ?? Directory.GetCurrentDirectory();
        }

        /// <summary>
        /// Determines whether the token is a flag or option token.
        /// </summary>
        /// <param name="token">The argument token.</param>
        /// <returns>True if it is a flag token; otherwise false.</returns>
        private static bool IsFlagToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            return token.StartsWith("-", StringComparison.Ordinal);
        }

        /// <summary>
        /// Determines whether the token is a negative integer value token rather than an option flag.
        /// </summary>
        /// <param name="token">The argument token.</param>
        /// <returns>True if the token is a negative integer value; otherwise false.</returns>
        private static bool IsNegativeIntegerValueToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            if (!token.StartsWith("-", StringComparison.Ordinal))
            {
                return false;
            }

            return int.TryParse(token, out _);
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
        /// <param name="optionName">Option name, for example "--format".</param>
        /// <returns>The option value or null.</returns>
        /// <remarks>
        /// Only options contained in the option-value set are treated as value-based options.
        /// If the value is missing or the next token is another flag, null is returned.
        /// Negative integer tokens are treated as values so numeric validation can report
        /// the actual invalid value.
        /// </remarks>
        private static string? GetOptionValue(string[] args, string optionName)
        {
            if (args == null || args.Length == 0)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(optionName))
            {
                return null;
            }

            if (!optionsWithValue.Contains(optionName))
            {
                return null;
            }

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

                if (IsFlagToken(value) && !IsNegativeIntegerValueToken(value))
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
        private static void PrintUsage(string? error)
        {
            if (!string.IsNullOrWhiteSpace(error))
            {
                Console.WriteLine($"Error: {error}");
                Console.WriteLine();
            }

            Console.WriteLine("XMLDocNormalizer - Checks and fixes C# XML documentation.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  XMLDocNormalizer (--check | --fix) [--full] [--project projectName] [--test] [--clean-backups] [--verbose]");
            Console.WriteLine("                   [--format console|json|sarif] [--output path] [--exception-analysis-mode mode]");
            Console.WriteLine("                   [--compare-exception-analysis-modes]");
            Console.WriteLine("                   [--exception-analysis-comparison-runs n] [--exception-analysis-comparison-warmup-runs n] [path]");
            Console.WriteLine("                   [--value-documentation-mode mode]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --check               Run in check-only mode (no changes).");
            Console.WriteLine("  --fix                 Run in fix mode (modifies files).");
            Console.WriteLine("  --full                Analyze all projects in a solution.");
            Console.WriteLine("  --project <name>      Analyze only the specified project in a solution.");
            Console.WriteLine("  --test                Use test mode (creates backups).");
            Console.WriteLine("  --clean-backups       Remove any backup files after run.");
            Console.WriteLine("  --verbose, -v         Enable verbose logging.");
            Console.WriteLine("  --format <console|json|sarif>  Output format.");
            Console.WriteLine("  --output <path>       File path for JSON/SARIF output.");
            Console.WriteLine("  --exception-analysis-mode <mode>");
            Console.WriteLine("                       Controls how exception documentation is analyzed.");
            Console.WriteLine("                       Values:");
            Console.WriteLine("                         direct");
            Console.WriteLine("                           Reports only directly thrown exceptions. Alias: d.");
            Console.WriteLine("                         project-transitive-declared-exceptions");
            Console.WriteLine("                           Follows calls within the reporting scope and reports only exception types declared in that scope.");
            Console.WriteLine("                           Aliases: ptd, declared, project-declared, project-transitive-declared.");
            Console.WriteLine("                         project-transitive");
            Console.WriteLine("                           Follows calls within the reporting scope. Aliases: pt, project.");
            Console.WriteLine("                         solution-transitive");
            Console.WriteLine("                           Follows calls across the loaded solution project-reference closure.");
            Console.WriteLine("                           Aliases: st, solution.");
            Console.WriteLine("                       Default: solution-transitive.");
            Console.WriteLine("  --compare-exception-analysis-modes");
            Console.WriteLine("                       Executes all four exception analysis modes in isolated child processes and writes a comparison report.");
            Console.WriteLine("  --value-documentation-mode <mode>");
            Console.WriteLine("                       Controls when missing <value> documentation is reported.");
            Console.WriteLine("                       Values: disabled, all-readable-properties, exclude-dto-like-types, indexers-only.");
            Console.WriteLine("                       Aliases: off, none, all, strict, readable-properties, exclude-dto-like, exclude-dto, non-dto, non-dto-like, indexer-only, indexers.");
            Console.WriteLine("                       Default: all-readable-properties.");
            Console.WriteLine("  --exception-analysis-comparison-runs <n>");
            Console.WriteLine("                       Executes each exception analysis mode n measured times.");
            Console.WriteLine("                       Runs greater than 1 use rotating mode order and report median, mean, min, max and standard deviation.");
            Console.WriteLine("                       Default: 1.");
            Console.WriteLine("  --exception-analysis-comparison-warmup-runs <n>");
            Console.WriteLine("                       Executes each exception analysis mode n warmup times before measured runs.");
            Console.WriteLine("                       Warmup runs are excluded from timing statistics.");
            Console.WriteLine("                       Default: 0.");
            Console.WriteLine("  --enable-statistics           Generate statistics output for study/evaluation.");
            Console.WriteLine("  --statistics-output <path>    Write statistics JSON to the specified path.");
            Console.WriteLine("                       defaults to <output>_statistics.json when --output is set.");
            Console.WriteLine("  --include-generated   Includes generated files in analysis and metrics.");
            Console.WriteLine("  --include-tests       Includes test source files in analysis and metrics.");
            Console.WriteLine("  --help, -h            Show this help message.");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  XMLDocNormalizer --check --format console src/");
            Console.WriteLine("  XMLDocNormalizer --fix --test src/");
            Console.WriteLine("  XMLDocNormalizer --check --full MySolution.sln");
            Console.WriteLine("  XMLDocNormalizer --check --project MyProject MySolution.sln");
            Console.WriteLine("  XMLDocNormalizer --check --compare-exception-analysis-modes --exception-analysis-comparison-runs 5 MySolution.sln");
            Console.WriteLine("  XMLDocNormalizer --check --compare-exception-analysis-modes --exception-analysis-comparison-warmup-runs 1 --exception-analysis-comparison-runs 5 MySolution.sln");
        }
    }
}
