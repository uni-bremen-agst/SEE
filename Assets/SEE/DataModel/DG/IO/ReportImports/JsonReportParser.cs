using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SEE.Utils.Paths;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using UnityEngine;

namespace SEE.DataModel.DG.IO
{
    /// <summary>
    /// Generic JSON parser that uses a <see cref="JsonParsingConfig"/> to translate report files
    /// into <see cref="MetricSchema"/> instances.
    /// Supports multiple contexts (e.g. "class", "assembly") similar to the XML parser.
    /// </summary>
    public sealed class JsonReportParser : IReportParser
    {
        /// <summary>
        /// The configuration used to parse the JSON report.
        /// </summary>
        private readonly JsonParsingConfig config;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonReportParser"/> class.
        /// </summary>
        /// <param name="config">The configuration defining how to map JSON paths to metrics.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="config"/> is null.</exception>
        public JsonReportParser(JsonParsingConfig config)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Validates the configuration to ensure essential mapping fields are present.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the configuration is invalid or incomplete.</exception>
        public void Prepare()
        {
            if (config.JsonMapping == null)
            {
                throw new InvalidOperationException($"[{nameof(JsonReportParser)}] {nameof(JsonPathMapping)} configuration is missing.");
            }

            if (config.JsonMapping.SelectElements == null || config.JsonMapping.SelectElements.Count == 0)
            {
                throw new InvalidOperationException($"[{nameof(JsonReportParser)}] No '{nameof(JsonPathMapping.SelectElements)} "
                    + "defined in JsonMapping. The parser needs at least one context selector.");
            }

            if (config.JsonMapping.PathBuilders == null)
            {
                throw new InvalidOperationException($"[{nameof(JsonReportParser)}] {nameof(JsonPathMapping.PathBuilders)} dictionary is missing.");
            }
        }

        /// <summary>
        /// Parses the report file asynchronously.
        /// </summary>
        /// <param name="path">The path to the report file.</param>
        /// <param name="token">Token to cancel the operation.</param>
        /// <returns>The parsed metrics schema.</returns>
        public async UniTask<MetricSchema> ParseAsync(DataPath path, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();

            // Validate config before touching the file system
            Prepare();

            using Stream stream = await path.LoadAsync();
            using StreamReader reader = new StreamReader(stream);
            string jsonContent = await reader.ReadToEndAsync();

            await UniTask.SwitchToThreadPool();

            return ParseCore(jsonContent, config);
        }

        /// <summary>
        /// Core parsing logic that traverses the JSON content based on the configuration.
        /// </summary>
        /// <param name="jsonContent">The raw JSON string.</param>
        /// <param name="config">The parsing configuration.</param>
        /// <returns>A schema containing all found metrics.</returns>
        private static MetricSchema ParseCore(string jsonContent, JsonParsingConfig config)
        {
            MetricSchema schema = new MetricSchema { ToolId = config.ToolId };
            JObject root;

            try
            {
                root = JObject.Parse(jsonContent);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{nameof(JsonReportParser)}] Failed to parse JSON content: {ex.Message}\n");
                return schema;
            }

            JsonPathMapping mapping = config.JsonMapping;
            int findingsCount = 0;

            // Iterate through all defined contexts (e.g., "class", "method", "assembly")
            foreach (KeyValuePair<string, string> selection in mapping.SelectElements)
            {
                string context = selection.Key;
                string selectorPath = selection.Value;

                // Ensure we have a way to build an ID for this context
                if (!mapping.PathBuilders.TryGetValue(context, out string idPath))
                {
                    Debug.LogWarning($"[{nameof(JsonReportParser)}] No PathBuilder defined for context '{context}'. Skipping.\n");
                    continue;
                }

                // Select tokens based on the JSONPath
                IEnumerable<JToken> tokens = root.SelectTokens(selectorPath);

                if (tokens == null)
                {
                    continue;
                }

                foreach (JToken token in tokens)
                {
                    // 1. Extract ID (Finding.FullPath)
                    JToken idToken = token.SelectToken(idPath);
                    string id = idToken?.ToString();

                    if (string.IsNullOrWhiteSpace(id))
                    {
                        continue;
                    }

                    // 2. Extract Filename (optional)
                    string fileName = string.Empty;
                    if (mapping.FileName.TryGetValue(context, out string fileNamePath))
                    {
                        fileName = token.SelectToken(fileNamePath)?.ToString() ?? string.Empty;
                    }

                    // 3. Create Finding
                    Finding finding = new Finding
                    {
                        FullPath = id,
                        FileName = fileName,
                        Context = context,
                        Metrics = new Dictionary<string, string>(),
                        Location = ParseLocation(token, mapping)
                    };

                    // 4. Extract Metrics for this context
                    if (mapping.MetricsByContext.TryGetValue(context, out Dictionary<string, string> metricDefs))
                    {
                        foreach (KeyValuePair<string, string> metricDef in metricDefs)
                        {
                            string metricName = metricDef.Key;
                            string metricPath = metricDef.Value;

                            JToken metricToken = token.SelectToken(metricPath);

                            // Only add if value exists and is valid (not null)
                            if (metricToken != null && metricToken.Type != JTokenType.Null)
                            {
                                if (metricToken is JValue jValue)
                                {
                                    finding.Metrics[metricName] = jValue.ToString(CultureInfo.InvariantCulture);
                                }
                                else
                                {
                                    finding.Metrics[metricName] = metricToken.ToString();
                                }
                            }
                        }
                    }

                    // Only add the finding if it actually contains metrics
                    if (finding.Metrics.Count > 0)
                    {
                        schema.Findings.Add(finding);
                        findingsCount++;
                    }
                }
            }

            Debug.Log($"[{nameof(JsonReportParser)}] Parsing finished. Found {findingsCount} findings in {mapping.SelectElements.Count} contexts.\n");

            return schema;
        }

        /// <summary>
        /// Extracts location information (Line, Column) from a token if mappings exist.
        /// </summary>
        /// <param name="token">The JSON token representing the current element.</param>
        /// <param name="mapping">The mapping configuration.</param>
        /// <returns>A <see cref="MetricLocation"/> if location data is found, otherwise null.</returns>
        private static MetricLocation? ParseLocation(JToken token, JsonPathMapping mapping)
        {
            if (mapping.LocationMapping == null || mapping.LocationMapping.Count == 0)
            {
                return null;
            }

            MetricLocation location = new MetricLocation();
            bool hasAnyLocation = false;

            foreach (KeyValuePair<string, string> mapEntry in mapping.LocationMapping)
            {
                JToken val = token.SelectToken(mapEntry.Value);
                if (val != null && int.TryParse(val.ToString(), out int intValue))
                {
                    switch (mapEntry.Key.ToLower())
                    {
                        case "startline":
                            location.StartLine = intValue;
                            hasAnyLocation = true;
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

            return hasAnyLocation ? location : null;
        }
    }
}
