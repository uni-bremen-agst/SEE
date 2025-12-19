using Cysharp.Threading.Tasks;
using SEE.Utils.Paths;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Xml;
using System.Xml.XPath;
using UnityEngine;

/// <summary>
/// Contains types for parsing external tool reports and applying their metrics to SEE dependency graphs.
/// </summary>
namespace SEE.DataModel.DG.IO
{
    /// <summary>
    /// Generic XML parser that uses a <see cref="ParsingConfig"/> to translate report files
    /// into <see cref="MetricSchema"/> instances.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// <list type="bullet">
    /// <item><description>The provided configuration must not be null and must contain a valid <see cref="ParsingConfig.XPathMapping"/>.</description></item>
    /// <item><description>The input report must be compatible with the configured XPath expressions.</description></item>
    /// </list>
    /// </remarks>
    public sealed class XmlReportParser : IReportParser
    {
        /// <summary>
        /// Configuration that describes how XML reports are interpreted.
        /// </summary>
        /// <remarks>Preconditions: Must not be null.</remarks>
        private readonly ParsingConfig config;

        /// <summary>
        /// Reader settings reused for every parse operation.
        /// </summary>
        /// <remarks>Preconditions: Initialized by <see cref="Prepare"/> before being used.</remarks>
        private XmlReaderSettings readerSettings;

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlReportParser"/> class.
        /// </summary>
        /// <param name="config">Configuration that describes how reports should be parsed. Must not be null.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="config"/> is null.</exception>
        public XmlReportParser(ParsingConfig config)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Creates or recreates the XML reader settings so that <see cref="ParseAsync"/> can
        /// use consistent, secure defaults.
        /// </summary>
        public void Prepare()
        {
            readerSettings = new XmlReaderSettings
            {
                CloseInput = true,
                IgnoreWhitespace = true,
                IgnoreComments = true,
                Async = true,
                DtdProcessing = DtdProcessing.Ignore,
                XmlResolver = null
            };
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
        /// A <see cref="MetricSchema"/> that mirrors the parsed XML content.
        /// The returned schema is never null.
        /// </returns>
        public async UniTask<MetricSchema> ParseAsync(DataPath path, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();

            Prepare();
            using Stream stream = await path.LoadAsync();
            using XmlReader xmlReader = XmlReader.Create(stream, readerSettings);

            await UniTask.SwitchToThreadPool();

            MetricSchema metricSchema = ParseCore(xmlReader, config);

            return metricSchema;
        }

        /// <summary>
        /// Core parsing routine that evaluates all configured XPath expressions.
        /// </summary>
        /// <param name="xmlReader">Reader positioned at the start of the XML report. Must not be null.</param>
        /// <param name="parsingConfig">
        /// Parsing configuration that describes how to interpret nodes.
        /// Must not be null and must provide a non-null <see cref="ParsingConfig.XPathMapping"/>.
        /// </param>
        /// <returns>
        /// A schema filled with all findings emitted by the report.
        /// The returned schema is never null.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown if any argument is null.</exception>
        private static MetricSchema ParseCore(XmlReader xmlReader, ParsingConfig parsingConfig)
        {
            if (xmlReader == null)
            {
                throw new ArgumentNullException(nameof(xmlReader));
            }

            if (parsingConfig == null)
            {
                throw new ArgumentNullException(nameof(parsingConfig));
            }

            XPathMapping xpathMapping = parsingConfig.XPathMapping;
            if (xpathMapping == null)
            {
                throw new ArgumentNullException(nameof(parsingConfig.XPathMapping));
            }

            XPathDocument report = new XPathDocument(xmlReader);
            XPathNavigator navigator = report.CreateNavigator();

            // Optional: configure namespaces if provided by the mapping.
            XmlNamespaceManager? namespaceManager = null;
            if (xpathMapping.Namespaces?.Count > 0)
            {
                namespaceManager = new XmlNamespaceManager(navigator.NameTable);

                foreach (KeyValuePair<string, string> kv in xpathMapping.Namespaces)
                {
                    namespaceManager.AddNamespace(kv.Key, kv.Value);
                }
            }

            MetricSchema metricSchema = new MetricSchema
            {
                ToolId = parsingConfig.ToolId ?? string.Empty
            };

            // Select all nodes of interest (typically a union expression).
            XPathNodeIterator iterator = namespaceManager == null
                ? navigator.Select(xpathMapping.SearchedNodes)
                : navigator.Select(xpathMapping.SearchedNodes, namespaceManager);

            int nodeCount = 0;

            while (iterator.MoveNext())
            {
                XPathNavigator current = iterator.Current;

                if (current == null)
                {
                    continue;
                }

                nodeCount++;

                string context = current.LocalName;

                if (!xpathMapping.PathBuilders.TryGetValue(context, out string pathExpression))
                {
                    continue;
                }

                string fullPath = string.Empty;
                try
                {
                    object evaluationResult = current.Evaluate(pathExpression, namespaceManager);
                    fullPath = evaluationResult?.ToString() ?? string.Empty;
                }
                catch (XPathException ex)
                {
                    Debug.LogWarning(
                        $"[{nameof(XmlReportParser)}] XPath error in path builder '{pathExpression}': {ex.Message}.\n");
                }

                Finding? finding = CreateFinding(current, fullPath, xpathMapping, namespaceManager);

                if (finding != null)
                {
                    metricSchema.Findings.Add(finding);
                }
            }

            Debug.Log(
                $"[{nameof(XmlReportParser)}] Parsing finished. Nodes visited: {nodeCount}, Findings: {metricSchema.Findings.Count}.\n");

            return metricSchema;
        }

        /// <summary>
        /// Builds a single <see cref="Finding"/> from the current XPath position.
        /// </summary>
        /// <param name="current">Navigator pointing at the node that should become a finding. Must not be null.</param>
        /// <param name="fullPath">
        /// Normalized identifier produced by the path builder.
        /// May be an empty string if no path could be computed.
        /// </param>
        /// <param name="xpathMapping">Mapping that describes metrics, locations and context. Must not be null.</param>
        /// <param name="namespaceManager">
        /// Optional namespace manager used for XPath evaluation.
        /// May be null if the report does not use namespaces.
        /// </param>
        /// <returns>A populated finding or <c>null</c> if no metrics were produced for the current node.</returns>
        private static Finding? CreateFinding(
            XPathNavigator current,
            string fullPath,
            XPathMapping xpathMapping,
            XmlNamespaceManager? namespaceManager)
        {
            string context = current.LocalName;

            xpathMapping.FileName.TryGetValue(context, out string fileNameExpression);

            string fileName = string.IsNullOrEmpty(fileNameExpression)
                ? string.Empty
                : current.Evaluate(fileNameExpression, namespaceManager)?.ToString() ?? string.Empty;

            Finding finding = new Finding
            {
                FullPath = fullPath,
                FileName = fileName,
                Context = context,
                Metrics = new Dictionary<string, string>(),
                Location = ParseLocation(current, xpathMapping, namespaceManager)
            };

            foreach (KeyValuePair<string, string> kv in xpathMapping.MetricsByContext[context])
            {
                string value = string.Empty;
                try
                {
                    object result = current.Evaluate(kv.Value, namespaceManager);

                    if (result is double numericResult && double.IsNaN(numericResult))
                    {
                        // Skip NaN values.
                        continue;
                    }

                    value = result is double d
                        ? d.ToString(CultureInfo.InvariantCulture)
                        : result?.ToString() ?? string.Empty;
                }
                catch (XPathException ex)
                {
                    Debug.LogWarning($"[{nameof(XmlReportParser)}] XPath error in metric '{kv.Key}': {ex.Message}.\n");
                }

                if (!string.IsNullOrEmpty(value) && value != "NaN")
                {
                    finding.Metrics[kv.Key] = value;
                }
            }

            return finding.Metrics.Count > 0 ? finding : null;
        }

        /// <summary>
        /// Reads the configured location fields (start or end line and column) for the current node.
        /// </summary>
        /// <param name="current">Navigator pointing at the node whose location should be read. Must not be null.</param>
        /// <param name="xpathMapping">
        /// Mapping that specifies which location fields to extract.
        /// May provide an empty or null <see cref="XPathMapping.LocationMapping"/> if no location is defined.
        /// </param>
        /// <param name="namespaceManager">
        /// Optional namespace manager used for XPath evaluation.
        /// May be null if the report does not use namespaces.
        /// </param>
        /// <returns>A populated <see cref="MetricLocation"/> or <c>null</c> if no location information was found.</returns>
        private static MetricLocation? ParseLocation(
            XPathNavigator current,
            XPathMapping xpathMapping,
            XmlNamespaceManager? namespaceManager)
        {
            if (xpathMapping.LocationMapping == null || xpathMapping.LocationMapping.Count == 0)
            {
                return null;
            }

            MetricLocation location = new MetricLocation();

            foreach (KeyValuePair<string, string> kv in xpathMapping.LocationMapping)
            {
                if (string.IsNullOrEmpty(kv.Value))
                {
                    continue;
                }

                try
                {
                    string value = current.Evaluate(kv.Value, namespaceManager)?.ToString() ?? string.Empty;

                    if (int.TryParse(value, out int intValue))
                    {
                        switch (kv.Key.ToLower())
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
                catch (XPathException ex)
                {
                    Debug.LogWarning(
                        $"[{nameof(XmlReportParser)}] XPath error in location field '{kv.Key}': {ex.Message}.\n");
                }
            }

            return location.StartLine.HasValue ? location : null;
        }
    }
}
