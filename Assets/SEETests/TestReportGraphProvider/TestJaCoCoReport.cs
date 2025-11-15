using System;
using System.Collections.Generic;
using System.Globalization;
using SEE.DataModel.DG.IO;
using SEE.GraphProviders.NodeCounting;

namespace SEE.GraphProviders
{
    /// <summary>
    /// Tests for JaCoCo XML coverage reports with concrete expected metrics.
    /// </summary>
    public class TestJaCoCoReport : TestReportGraphProviderBase
    {
        /// <inheritdoc/>
        protected override string GetRelativeReportPath()
            => "/JLGExample/jacoco.xml";

        /// <inheritdoc/>
        protected override ParsingConfig GetParsingConfig()
            => new JaCoCoParsingConfig();

        /// <inheritdoc/>
        protected override ICountReportNodes GetNodeCounter()
            => new XmlNodeCounter();

        /// <summary>
        /// All JaCoCo node types that this test verifies.
        /// </summary>
        private static readonly string[] ParsedNodes = { "report", "package", "class", "method" };

        /// <inheritdoc/>
        protected override string[] GetNodesToParse() => ParsedNodes;

        /// <inheritdoc/>
        protected override Dictionary<string, Finding> GetTestFindings()
            => CreateTestFindings();

        /// <summary>
        /// Helper to build the same metric set for any node.
        /// Für jede Metrik werden *_missed, *_covered und *_percentage eingetragen.
        /// </summary>
        private static Dictionary<string, string> BuildMetrics(
            int instructionMissed, int instructionCovered,
            int branchMissed, int branchCovered,
            int lineMissed, int lineCovered,
            int complexityMissed, int complexityCovered,
            int methodMissed, int methodCovered,
            int? classMissed = null, int? classCovered = null)
        {
            var metrics = new Dictionary<string, string>();

            void AddMetric(string name, int missed, int covered)
            {
                metrics[$"{name}_missed"] = missed.ToString(CultureInfo.InvariantCulture);
                metrics[$"{name}_covered"] = covered.ToString(CultureInfo.InvariantCulture);

                int total = missed + covered;
                string percentage = total > 0
                    ? Math.Round(100d * covered / total).ToString(CultureInfo.InvariantCulture)
                    : "0";

                metrics[$"{name}_percentage"] = percentage;
            }

            AddMetric("INSTRUCTION", instructionMissed, instructionCovered);
            AddMetric("BRANCH", branchMissed, branchCovered);
            AddMetric("LINE", lineMissed, lineCovered);
            AddMetric("COMPLEXITY", complexityMissed, complexityCovered);
            AddMetric("METHOD", methodMissed, methodCovered);

            // CLASS gibt es nicht auf allen Ebenen (z.B. nicht auf Method-Ebene)
            if (classMissed.HasValue && classCovered.HasValue)
            {
                AddMetric("CLASS", classMissed.Value, classCovered.Value);
            }

            return metrics;
        }

        /// <summary>
        /// One expected Finding per context with concrete metrics from jacoco.xml.
        /// FullPath strings assume:
        /// - report: report name
        /// - package: "counter"
        /// - class:   "counter/CountVowels"
        /// - method:  "counter/CountVowels#countVowels"
        /// </summary>
        private static Dictionary<string, Finding> CreateTestFindings()
        {
            // Werte aus <report> ... </report> (ganz unten in jacoco.xml):
            // INSTRUCTION {missed=1313, covered=441}
            // BRANCH      {missed=101,  covered=27}
            // LINE        {missed=330,  covered=83}
            // COMPLEXITY  {missed=107,  covered=20}
            // METHOD      {missed=55,   covered=8}
            // CLASS       {missed=6,    covered=4}
            var reportFinding = new Finding
            {
                FullPath = "JaCoCo Coverage Report",
                FileName = "",
                Context = "root",
                Metrics = BuildMetrics(
                    instructionMissed: 1313, instructionCovered: 441,
                    branchMissed: 101, branchCovered: 27,
                    lineMissed: 330, lineCovered: 83,
                    complexityMissed: 107, complexityCovered: 20,
                    methodMissed: 55, methodCovered: 8,
                    classMissed: 6, classCovered: 4)
            };

            // Package "counter":
            // INSTRUCTION {missed=31,  covered=313}
            // BRANCH      {missed=1,   covered=17}
            // LINE        {missed=13,  covered=45}
            // COMPLEXITY  {missed=9,   covered=14}
            // METHOD      {missed=8,   covered=6}
            // CLASS       {missed=0,   covered=3}
            var packageFinding = new Finding
            {
                FullPath = "counter",
                FileName = "",
                Context = "package",
                Metrics = BuildMetrics(
                    instructionMissed: 31, instructionCovered: 313,
                    branchMissed: 1, branchCovered: 17,
                    lineMissed: 13, lineCovered: 45,
                    complexityMissed: 9, complexityCovered: 14,
                    methodMissed: 8, methodCovered: 6,
                    classMissed: 0, classCovered: 3)
            };

            // Class "counter/CountVowels":
            // INSTRUCTION {missed=7, covered=66}
            // BRANCH      {missed=0, covered=6}
            // LINE        {missed=3, covered=11}
            // COMPLEXITY  {missed=2, covered=5}
            // METHOD      {missed=2, covered=2}
            // CLASS       {missed=0, covered=1}
            var classFinding = new Finding
            {
                FullPath = "counter/CountVowels",
                FileName = "CountVowels.java",
                Context = "class",
                Metrics = BuildMetrics(
                    instructionMissed: 7, instructionCovered: 66,
                    branchMissed: 0, branchCovered: 6,
                    lineMissed: 3, lineCovered: 11,
                    complexityMissed: 2, complexityCovered: 5,
                    methodMissed: 2, methodCovered: 2,
                    classMissed: 0, classCovered: 1)
            };

            // Method "countVowels" in dieser Klasse:
            // INSTRUCTION {missed=0, covered=39}
            // BRANCH      {missed=0, covered=6}
            // LINE        {missed=0, covered=8}
            // COMPLEXITY  {missed=0, covered=4}
            // METHOD      {missed=0, covered=1}
            // (kein CLASS-Counter auf Methodenebene)
            var methodFinding = new Finding
            {
                FullPath = "counter/CountVowels#countVowels",
                FileName = "CountVowels.java",
                Context = "method",
                Location = new MetricLocation { StartLine = 11 },
                Metrics = BuildMetrics(
                    instructionMissed: 0, instructionCovered: 39,
                    branchMissed: 0, branchCovered: 6,
                    lineMissed: 0, lineCovered: 8,
                    complexityMissed: 0, complexityCovered: 4,
                    methodMissed: 0, methodCovered: 1)
            };

            return new Dictionary<string, Finding>
            {
                { reportFinding.FullPath,  reportFinding  },
                { packageFinding.FullPath, packageFinding },
                { classFinding.FullPath,   classFinding   },
                { methodFinding.FullPath,  methodFinding  }
            };
        }
    }
}
