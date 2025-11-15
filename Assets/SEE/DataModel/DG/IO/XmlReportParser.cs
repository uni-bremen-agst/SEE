using Cysharp.Threading.Tasks;
using SEE.Utils.Paths;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Xml;
using System.Xml.XPath;
using UnityEngine;

namespace SEE.DataModel.DG.IO
{
    /// <summary>
    /// Generic XML parser that uses a <see cref="ParsingConfig"/> to translate report files
    /// into <see cref="MetricSchema"/> instances.
    /// </summary>
    public sealed class XmlReportParser : IReportParser
    {
        private readonly ParsingConfig _config;

        /// <summary>
        /// Stores the configuration that drives the XPath traversal.
        /// </summary>
        public XmlReportParser(ParsingConfig config) => _config = config;


        /// <summary>
        /// Reader settings reused for every parse operation.
        /// </summary>
        protected XmlReaderSettings settings;

        /// <summary>
        /// Creates (or recreates) the XML reader settings so that <see cref="ParseAsync"/> can
        /// use consistent, secure defaults.
        /// </summary>
        public void Prepare() {

            settings = new()
            {
                CloseInput = true,
                IgnoreWhitespace = true,
                IgnoreComments = true,
                Async = true,
                DtdProcessing = DtdProcessing.Parse
            };

        }

        /// <summary>
        /// Parses the report described by <paramref name="path"/> and returns all captured findings.
        /// </summary>
        /// <param name="path">location of the report (file, bundle, remote, ...)</param>
        /// <param name="token">optional cancellation token (currently unused)</param>
        /// <returns>a <see cref="MetricSchema"/> that mirrors the parsed XML</returns>
        public async UniTask<MetricSchema> ParseAsync(DataPath path, CancellationToken token = default)
        {

            Prepare();
            using Stream stream = await path.LoadAsync();
            using XmlReader xmlReader = XmlReader.Create(stream, settings);
            await UniTask.SwitchToThreadPool();
            MetricSchema metricSchema = ParseCore(xmlReader, _config);
            return metricSchema;
        }

        /// <summary>
        /// Core parsing routine that evaluates all configured XPath expressions.
        /// </summary>
        /// <param name="xmlReader">reader positioned at the start of the XML report</param>
        /// <param name="config">parsing configuration that describes how to interpret nodes</param>
        /// <returns>a schema filled with all findings emitted by the report</returns>
        private static MetricSchema ParseCore(XmlReader xmlReader, ParsingConfig config)
        {
            XPathMapping xPathMapping = config.XPathMapping;
            var report = new XPathDocument(xmlReader);
            var nav = report.CreateNavigator();

            // Optional: Namespaces unterstützen (falls du sie später brauchst)
            XmlNamespaceManager? nsmgr = null;
            if (xPathMapping.Namespaces?.Count > 0)
            {
                nsmgr = new XmlNamespaceManager(nav.NameTable);
                foreach (var kv in xPathMapping.Namespaces)
                {
                    nsmgr.AddNamespace(kv.Key, kv.Value);
                }
            }

            MetricSchema metricSchema = new MetricSchema { ToolId = config.ToolId ?? "" };

            // Alle gesuchten Nodes holen (Union via "|")
            var iterator = nsmgr is null
                ? nav.Select(xPathMapping.SearchedNodes)
                : nav.Select(xPathMapping.SearchedNodes, nsmgr);

            int nodeCount = 0;

            while (iterator.MoveNext())
            {
                XPathNavigator current = iterator.Current;

                if (current is null)
                {
                    continue;
                }

                nodeCount++;

                string tagName = current.LocalName;

                if (!xPathMapping.PathBuilders.TryGetValue(tagName, out string pathExpression))
                {
                    continue;
                }

                string fullPath = "";
                try
                {
                    fullPath = current.Evaluate(pathExpression, nsmgr)?.ToString() ?? "";

                }
                catch (XPathException ex)
                {
                    Debug.LogWarning($"[Parser] XPath-Fehler bei PathBuilder '{pathExpression}': {ex.Message}");
                }

                Finding finding = CreateFinding(current, fullPath, xPathMapping, nsmgr);

                if(finding != null)
                {
                    metricSchema.findings.Add(finding);
                }
            }

            // Am Ende von ParseCore:
            Debug.Log($"[Parser] Parsing beendet. Gefundene Nodes: {nodeCount}, Findings: {metricSchema.findings.Count}");


            return metricSchema;
        }

        /// <summary>
        /// Builds a single <see cref="Finding"/> from the current XPath position.
        /// </summary>
        /// <param name="current">navigator pointing at the node that should become a finding</param>
        /// <param name="fullPath">normalized identifier produced by the path builder</param>
        /// <param name="xPathMapping">mapping that describes metrics and locations</param>
        /// <param name="nsmgr">optional namespace manager used for XPath evaluation</param>
        /// <returns>a populated finding or <c>null</c> if no metrics were produced</returns>
        private static Finding CreateFinding(XPathNavigator current, string fullPath, XPathMapping xPathMapping, XmlNamespaceManager nsmgr )
        {
            string context = xPathMapping.MapContext[current.LocalName];

            xPathMapping.FileName.TryGetValue(context, out string fileNameExpression);

            string fileName = string.IsNullOrEmpty(fileNameExpression) ? "" :
                current.Evaluate(fileNameExpression, nsmgr).ToString() ?? "";

            Finding finding = new Finding
            {
                FullPath = fullPath,
                FileName = fileName,
                Context = context,
                Metrics = new Dictionary<string, string>(),
                Location = ParseLocation(current, xPathMapping, nsmgr)
            };

            foreach (var kv in xPathMapping.Metrics)
            {
                string val = "";
                try
                {
                    object result = current.Evaluate(kv.Value, nsmgr);


                    if (result is double d && double.IsNaN(d))
                    {
                        continue; // Skip NaN-Values
                    }

                    val = result.ToString();
                }
                catch (XPathException ex)
                {
                    Debug.LogWarning($"[Parser] XPath-Error in Metric '{kv.Key}': {ex.Message}");
                }

                if (!string.IsNullOrEmpty(val) && val != "NaN")
                {
                    finding.Metrics[kv.Key] = val;
                }
            }
            return finding.Metrics.Count > 0 ? finding : null;

        }

        /// <summary>
        /// Reads the configured location fields (start/end line/column) for the current node.
        /// </summary>
        /// <param name="current">navigator pointing at the node whose location should be read</param>
        /// <param name="xPathMapping">mapping that specifies which fields to extract</param>
        /// <param name="nsmgr">optional namespace manager for XPath</param>
        /// <returns>a populated <see cref="MetricLocation"/> or <c>null</c> if nothing was found</returns>
        private static MetricLocation ParseLocation(XPathNavigator current, XPathMapping xPathMapping, XmlNamespaceManager nsmgr)
        {
            if (xPathMapping.LocationMapping == null || xPathMapping.LocationMapping.Count == 0)
                return null;

            var location = new MetricLocation();

            foreach (var kv in xPathMapping.LocationMapping)
            {
                if (string.IsNullOrEmpty(kv.Value))
                    continue;

                try
                {
                    string val = current.Evaluate(kv.Value, nsmgr).ToString() ?? "";
                    if (int.TryParse(val, out int intValue))
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
                    Debug.LogWarning($"[Parser] XPath-Fehler bei Location '{kv.Key}': {ex.Message}");
                }
            }

            return location.StartLine.HasValue ? location : null; 
        }

    }
}
