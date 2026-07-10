using XMLDocNormalizer.Configuration;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Models.DTO;
using XMLDocNormalizer.Reporting.Sarif.Contract;
using XMLDocNormalizer.Utils;

namespace XMLDocNormalizer.Reporting.Sarif
{
    /// <summary>
    /// Builds a SARIF 2.1.0 log from tool findings.
    /// </summary>
    internal static class SarifLogBuilder
    {
        private const string SchemaUri = "https://json.schemastore.org/sarif-2.1.0.json";
        private const string SarifVersion = "2.1.0";

        /// <summary>
        /// Builds a SARIF log containing a single run with all findings.
        /// </summary>
        /// <param name="findings">All findings across files.</param>
        /// <returns>A SARIF log.</returns>
        public static SarifLog Build(IReadOnlyList<Finding> findings, RunResult result)
        {
            IReadOnlyList<SarifRule> rules = BuildRules(findings);

            SarifToolComponent driver = new(
                Name: ToolMetadata.Name,
                Version: ToolMetadata.Version,
                Rules: rules);

            SarifTool tool = new(driver);

            IReadOnlyList<SarifResult> results = findings.Select(BuildResult).ToList();

            RunMetricsDto metrics = RunMetricsCalculator.From(result);

            Dictionary<string, object> properties = new(StringComparer.Ordinal)
            {
                ["metrics"] = metrics
            };

            SarifRun run = new(tool, results, properties);

            return new SarifLog(
                Schema: SchemaUri,
                Version: SarifVersion,
                Runs: new[] { run });
        }

        /// <summary>
        /// Builds rule metadata based on unique smell ids.
        /// </summary>
        /// <param name="findings">The findings to extract smells from.</param>
        /// <returns>A list of SARIF rules.</returns>
        private static IReadOnlyList<SarifRule> BuildRules(IReadOnlyList<Finding> findings)
        {
            return findings
                .Select(finding => finding.Smell)
                .DistinctBy(smell => smell.ID, StringComparer.Ordinal)
                .OrderBy(smell => smell.ID, StringComparer.Ordinal)
                .Select(smell =>
                {
                    string level = SarifSeverityMapper.ToSarifLevel(smell.Severity);

                    return new SarifRule(
                        Id: smell.ID,
                        ShortDescription: new SarifMultiformatMessageString(smell.RuleTitle),
                        FullDescription: new SarifMultiformatMessageString(smell.RuleDescription),
                        DefaultConfiguration: new SarifReportingConfiguration(level));
                })
                .ToList();
        }

        /// <summary>
        /// Converts a single finding into a SARIF result.
        /// </summary>
        /// <param name="finding">The finding to convert.</param>
        /// <returns>A SARIF result.</returns>
        private static SarifResult BuildResult(Finding finding)
        {
            string level = SarifSeverityMapper.ToSarifLevel(finding.Smell.Severity);

            SarifArtifactLocation artifact = new(SarifPathNormalizer.Normalize(finding.FilePath));
            SarifSnippet? snippet = CreateSnippetOrNull(finding.Snippet);

            SarifRegion region = new(
                StartLine: EnsureOneBased(finding.Line),
                StartColumn: EnsureOneBased(finding.Column),
                Snippet: snippet);

            SarifPhysicalLocation physical = new(artifact, region);
            SarifLocation location = new(physical);

            return new SarifResult(
                RuleId: finding.Smell.ID,
                Level: level,
                Message: new SarifMessage(finding.Message),
                Locations: new[] { location },
                Properties: BuildResultProperties(finding));
        }

        /// <summary>
        /// Builds tool-specific SARIF properties for a finding.
        /// </summary>
        /// <param name="finding">The finding to convert.</param>
        /// <returns>
        /// A dictionary containing additional SARIF result metadata.
        /// </returns>
        private static IReadOnlyDictionary<string, object?> BuildResultProperties(Finding finding)
        {
            Dictionary<string, object?> properties = new(StringComparer.Ordinal)
            {
                ["tagName"] = finding.TagName,
                ["ownerKind"] = finding.Context.OwnerKind,
                ["subjectKind"] = finding.Context.SubjectKind,
                ["accessibility"] = finding.Context.Accessibility,
                ["symbolName"] = finding.Context.SymbolName,
                ["containingType"] = finding.Context.ContainingType,
                ["containingNamespace"] = finding.Context.ContainingNamespace
            };

            AddOptionalProperty(properties, "targetName", finding.Context.TargetName);
            AddOptionalProperty(properties, "projectName", finding.Context.ProjectName);
            AddOptionalProperty(properties, "isGenerated", finding.Context.IsGenerated);
            AddOptionalProperty(properties, "isTestFile", finding.Context.IsTestFile);

            return properties;
        }

        /// <summary>
        /// Adds a property to a dictionary if the value is not null.
        /// </summary>
        /// <param name="properties">The dictionary to update.</param>
        /// <param name="name">The property name.</param>
        /// <param name="value">The property value.</param>
        private static void AddOptionalProperty(
            Dictionary<string, object?> properties,
            string name,
            object? value)
        {
            if (value == null)
            {
                return;
            }

            properties[name] = value;
        }

        /// <summary>
        /// Ensures a region coordinate is 1-based as required by SARIF.
        /// </summary>
        /// <param name="value">Input value.</param>
        /// <returns>1-based value.</returns>
        private static int EnsureOneBased(int value)
        {
            return value < 1 ? 1 : value;
        }

        /// <summary>
        /// Creates a SARIF snippet instance if the input string is non-empty.
        /// </summary>
        /// <param name="snippetText">Snippet text.</param>
        /// <returns>A snippet or null.</returns>
        private static SarifSnippet? CreateSnippetOrNull(string snippetText)
        {
            if (string.IsNullOrWhiteSpace(snippetText))
            {
                return null;
            }

            return new SarifSnippet(snippetText);
        }
    }
}
