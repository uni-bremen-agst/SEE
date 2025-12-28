using Cysharp.Threading.Tasks;
using SEE.Utils.Paths;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;

namespace SEE.DataModel.DG.IO
{
    /// <summary>
    /// Parser for line-oriented text reports that uses regular expressions to extract structured data.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// <list type="bullet">
    /// <item><description>The provided configuration must not be null and must contain valid <see cref="TextParsingConfig.LinePatterns"/>.</description></item>
    /// <item><description>All regex patterns must be valid and use named capture groups.</description></item>
    /// </list>
    /// </remarks>
    public sealed class TextReportParser : IReportParser
    {
        /// <summary>
        /// Configuration that describes how text reports are interpreted.
        /// </summary>
        /// <remarks>Preconditions: Must not be null.</remarks>
        private readonly TextParsingConfig config;

        /// <summary>
        /// Compiled regex patterns for each context, cached for performance.
        /// </summary>
        /// <remarks>Preconditions: Initialized by <see cref="Prepare"/> before being used.</remarks>
        private Dictionary<string, Regex> compiledPatterns;

        /// <summary>
        /// Optional compiled line filter pattern.
        /// </summary>
        private Regex? lineFilterPattern;

        /// <summary>
        /// Initializes a new instance of the <see cref="TextReportParser"/> class.
        /// </summary>
        /// <param name="config">Configuration that describes how reports should be parsed. Must not be null.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="config"/> is null.</exception>
        public TextReportParser(TextParsingConfig config)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Compiles all regex patterns from the configuration for efficient reuse.
        /// </summary>
        public void Prepare()
        {
            compiledPatterns = new Dictionary<string, Regex>();

            foreach (KeyValuePair<string, string> patternEntry in config.LinePatterns)
            {
                try
                {
                    compiledPatterns[patternEntry.Key] = new Regex(
                        patternEntry.Value,
                        config.RegexOptions | RegexOptions.Compiled);
                }
                catch (ArgumentException exception)
                {
                    Debug.LogError(
                        $"[{nameof(TextReportParser)}] Invalid regex pattern for context '{patternEntry.Key}': {exception.Message}");
                }
            }

            if (!string.IsNullOrEmpty(config.LineFilter))
            {
                try
                {
                    lineFilterPattern = new Regex(
                        config.LineFilter,
                        config.RegexOptions | RegexOptions.Compiled);
                }
                catch (ArgumentException exception)
                {
                    Debug.LogError(
                        $"[{nameof(TextReportParser)}] Invalid line filter pattern: {exception.Message}");
                }
            }
        }

        /// <summary>
        /// Parses the report described by <paramref name="path"/> and returns all captured findings.
        /// </summary>
        /// <param name="path">Location of the report (file, bundle, remote, and so on). Must not be null.</param>
        /// <param name="token">
        /// Optional cancellation token. If cancellation is requested before or during parsing,
        /// the operation will be canceled.
        /// </param>
        /// <returns>
        /// A <see cref="MetricSchema"/> that contains all parsed findings.
        /// The returned schema is never null.
        /// </returns>
        public async UniTask<MetricSchema> ParseAsync(DataPath path, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();

            Prepare();

            using Stream stream = await path.LoadAsync();
            using StreamReader reader = new StreamReader(stream);

            await UniTask.SwitchToThreadPool();

            MetricSchema metricSchema = ParseCore(reader, config, compiledPatterns, lineFilterPattern);

            return metricSchema;
        }

        /// <summary>
        /// Core parsing routine that processes the text file line by line.
        /// </summary>
        /// <param name="reader">Text reader positioned at the start of the report. Must not be null.</param>
        /// <param name="parsingConfig">Parsing configuration. Must not be null.</param>
        /// <param name="patterns">Pre-compiled regex patterns. Must not be null.</param>
        /// <param name="lineFilter">Optional line filter pattern.</param>
        /// <returns>
        /// A schema filled with all findings extracted from the report.
        /// The returned schema is never null.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown if any required argument is null.</exception>
        private static MetricSchema ParseCore(
            StreamReader reader,
            TextParsingConfig parsingConfig,
            Dictionary<string, Regex> patterns,
            Regex? lineFilter)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            if (parsingConfig == null)
            {
                throw new ArgumentNullException(nameof(parsingConfig));
            }

            if (patterns == null)
            {
                throw new ArgumentNullException(nameof(patterns));
            }

            MetricSchema metricSchema = new MetricSchema
            {
                ToolId = parsingConfig.ToolId ?? string.Empty
            };

            int lineCount = 0;
            int matchedLines = 0;

            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                lineCount++;

                // Skip lines that don't pass the filter
                if (lineFilter != null && !lineFilter.IsMatch(line))
                {
                    continue;
                }

                // Try to match against all context patterns
                foreach (KeyValuePair<string, Regex> patternEntry in patterns)
                {
                    Match match = patternEntry.Value.Match(line);

                    if (!match.Success)
                    {
                        continue;
                    }

                    matchedLines++;

                    Finding? finding = CreateFinding(
                        match,
                        patternEntry.Key,
                        parsingConfig);

                    if (finding != null)
                    {
                        metricSchema.Findings.Add(finding);
                    }

                    // Stop after first match to avoid double-processing
                    break;
                }
            }

            Debug.Log(
                $"[{nameof(TextReportParser)}] Parsing finished. Lines processed: {lineCount}, " +
                $"Matched lines: {matchedLines}, Findings: {metricSchema.Findings.Count}.\n");

            return metricSchema;
        }

        /// <summary>
        /// Builds a single <see cref="Finding"/> from a successful regex match.
        /// </summary>
        /// <param name="match">Successful regex match containing named capture groups. Must not be null.</param>
        /// <param name="context">Context name that matched. Must not be null.</param>
        /// <param name="parsingConfig">Parsing configuration. Must not be null.</param>
        /// <returns>A populated finding or <c>null</c> if no metrics were produced.</returns>
        private static Finding? CreateFinding(
            Match match,
            string context,
            TextParsingConfig parsingConfig)
        {
            // Build full path using template substitution
            string fullPath = string.Empty;
            if (parsingConfig.PathBuilders.TryGetValue(context, out string pathTemplate))
            {
                fullPath = SubstituteTemplate(pathTemplate, match);
            }

            // Build file name using template substitution
            string fileName = string.Empty;
            if (parsingConfig.FileNameTemplates.TryGetValue(context, out string fileTemplate))
            {
                fileName = SubstituteTemplate(fileTemplate, match);
            }

            Finding finding = new Finding
            {
                FullPath = fullPath,
                FileName = fileName,
                Context = context,
                Metrics = new Dictionary<string, string>(),
                Location = ParseLocation(match, parsingConfig)
            };

            // Extract metrics using template substitution
            if (parsingConfig.MetricsByContext.TryGetValue(context, out Dictionary<string, string> metrics))
            {
                foreach (KeyValuePair<string, string> metricEntry in metrics)
                {
                    string value = SubstituteTemplate(metricEntry.Value, match);

                    if (!string.IsNullOrEmpty(value))
                    {
                        finding.Metrics[metricEntry.Key] = value;
                    }
                }
            }

            return finding.Metrics.Count > 0 ? finding : null;
        }

        /// <summary>
        /// Substitutes ${groupName} placeholders in a template with values from the regex match.
        /// </summary>
        /// <param name="template">Template string containing ${groupName} placeholders.</param>
        /// <param name="match">Regex match containing named capture groups.</param>
        /// <returns>The template with all placeholders replaced by their captured values.</returns>
        private static string SubstituteTemplate(string template, Match match)
        {
            if (string.IsNullOrEmpty(template))
            {
                return string.Empty;
            }

            // Replace ${groupName} with the captured value
            return Regex.Replace(
                template,
                @"\$\{([^}]+)\}",
                matchExpression =>
                {
                    string groupName = matchExpression.Groups[1].Value;
                    Group? group = match.Groups[groupName];

                    return group?.Success == true ? group.Value : string.Empty;
                });
        }

        /// <summary>
        /// Reads location information from the regex match using the configured location mapping.
        /// </summary>
        /// <param name="match">Regex match containing named capture groups. Must not be null.</param>
        /// <param name="parsingConfig">
        /// Parsing configuration that specifies which location fields to extract.
        /// May provide an empty or null <see cref="TextParsingConfig.LocationMapping"/> if no location is defined.
        /// </param>
        /// <returns>A populated <see cref="MetricLocation"/> or <c>null</c> if no location information was found.</returns>
        private static MetricLocation? ParseLocation(
            Match match,
            TextParsingConfig parsingConfig)
        {
            if (parsingConfig.LocationMapping == null || parsingConfig.LocationMapping.Count == 0)
            {
                return null;
            }

            MetricLocation location = new MetricLocation();

            foreach (KeyValuePair<string, string> mappingEntry in parsingConfig.LocationMapping)
            {
                if (string.IsNullOrEmpty(mappingEntry.Value))
                {
                    continue;
                }

                Group? group = match.Groups[mappingEntry.Value];
                if (group?.Success != true)
                {
                    continue;
                }

                if (int.TryParse(group.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intValue))
                {
                    switch (mappingEntry.Key.ToLower())
                    {
                        case "startline":
                            location.StartLine = intValue;
                            break;
                        case "endline":
                            location.EndLine = intValue;
                            break;
                        case "startcolumn":
                            location.StartColumn = intValue;
                            break;
                        case "endcolumn":
                            location.EndColumn = intValue;
                            break;
                    }
                }
            }

            return location.StartLine.HasValue ? location : null;
        }
    }
}
