using System;
using System.Collections.Generic;
using System.Globalization;
using SEE.DataModel.DG.IO;
using SEE.GraphProviders.NodeCounting;

namespace SEE.GraphProviders
{
    /// <summary>
    /// Tests for JaCoCo XML coverage report (JaCoCo core) with concrete expected metrics.
    /// </summary>
    internal class TestJaCoCoReport : TestReportGraphProviderBase
    {
        /// <inheritdoc/>
        protected override string GetRelativeReportPath()
        {
            // Adjust this to wherever you place the uploaded jacoco XML in your repo.
            return "/jacoco/jacoco-2-f5c5b1f831903c9c2f771e467916ce9664aedb1b.xml";
        }
        
        /// <inheritdoc/>
        protected override string GetRelativeGlxPath()
        {
            // Adjust this to wherever you place the uploaded jacoco XML in your repo.
            return "/jacoco/jacoco-2-f5c5b1f831903c9c2f771e467916ce9664aedb1b.gxl.xz";
        }

        /// <inheritdoc/>
        protected override ParsingConfig GetParsingConfig()
        {
            return new JaCoCoParsingConfig();
        }

        /// <inheritdoc/>
        protected override ICountReportNodes GetNodeCounter()
        {
            return new XmlNodeCounter();
        }

        /// <summary>
        /// All JaCoCo node types that this test verifies.
        /// </summary>
        private static readonly string[] parsedNodes = { "package", "class", "method" };

        /// <inheritdoc/>
        protected override string[] GetNodesToParse()
        {
            return parsedNodes;
        }

        /// <inheritdoc/>
        protected override Dictionary<string, Finding> GetTestFindings()
        {
            return CreateTestFindings();
        }
        public record MetricValue(int? Missed, int? Covered);


        /// <summary>
        /// Builds a metric dictionary for a JaCoCo node.
        /// Für jede Metrik werden *_missed, *_covered und *_percentage eingetragen.
        /// Preconditions: All metric values must be greater than or equal to zero.
        /// </summary>
        /// <param name="instructionMissed">Number of missed instructions.</param>
        /// <param name="instructionCovered">Number of covered instructions.</param>
        /// <param name="branchMissed">Number of missed branches.</param>
        /// <param name="branchCovered">Number of covered branches.</param>
        /// <param name="lineMissed">Number of missed lines.</param>
        /// <param name="lineCovered">Number of covered lines.</param>
        /// <param name="complexityMissed">Number of missed complexity points.</param>
        /// <param name="complexityCovered">Number of covered complexity points.</param>
        /// <param name="methodMissed">Number of missed methods.</param>
        /// <param name="methodCovered">Number of covered methods.</param>
        /// <param name="classMissed">Optional number of missed classes.</param>
        /// <param name="classCovered">Optional number of covered classes.</param>
        /// <returns>
        /// Dictionary that contains for each metric name entries for missed, covered and percentage.
        /// </returns>
        private static Dictionary<string, string> BuildMetrics(
            int? instructionMissed, int? instructionCovered,
            int? branchMissed, int? branchCovered,
            int? lineMissed, int? lineCovered,
            int? complexityMissed, int? complexityCovered,
            int? methodMissed, int? methodCovered,
            int? classMissed = null, int? classCovered = null)
        {
            Dictionary<string, string> metrics = new Dictionary<string, string>();

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
            Dictionary<string, MetricValue> jaCoCoMetrics = new Dictionary<string, MetricValue>()
            {
                ["INSTRUCTION"] = new MetricValue(instructionMissed, instructionCovered),
                ["BRANCH"] = new MetricValue(branchMissed, branchCovered),
                ["LINE"] = new MetricValue(lineMissed, lineCovered),
                ["COMPLEXITY"] = new MetricValue(complexityMissed, complexityCovered),
                ["METHOD"] = new MetricValue(methodMissed, methodCovered),
                ["CLASS"] = new MetricValue(classMissed, classCovered)
            };


            foreach (KeyValuePair<string,MetricValue> metricValue in jaCoCoMetrics)
            {
                if (metricValue.Value.Missed is int missed && metricValue.Value.Covered is int covered)
                {
                    AddMetric(metricValue.Key, missed, covered);
                }
            }

            return metrics;
        }

        

        /// <summary>
        /// Creates one expected finding per context with concrete metrics from the uploaded jacoco-2 report.
        /// FullPath strings assume:
        /// - report: "JaCoCo" (report name),
        /// - package: "org/jacoco/core/tools",
        /// - class:   "org/jacoco/core/tools/ExecFileLoader",
        /// - method:  "org/jacoco/core/tools/ExecFileLoader#save(Ljava/io/File;Z)V".
        /// </summary>
        /// <returns>Dictionary of expected findings keyed by their full path.</returns>
        private static Dictionary<string, Finding> CreateTestFindings()
        {

            // Package "org/jacoco/core/tools":
            // INSTRUCTION {missed=7,  covered=210}
            // BRANCH      {missed=1,  covered=5}
            // LINE        {missed=3,  covered=73}
            // COMPLEXITY  {missed=3,  covered=18}
            // METHOD      {missed=2,  covered=16}
            // CLASS       {missed=0,  covered=2}
            Finding packageFinding = new Finding
            {
                FullPath = "org/jacoco/core/tools",
                FileName = string.Empty,
                Context = "package",
                Metrics = BuildMetrics(
                    instructionMissed: 7, instructionCovered: 210,
                    branchMissed: 1, branchCovered: 5,
                    lineMissed: 3, lineCovered: 73,
                    complexityMissed: 3, complexityCovered: 18,
                    methodMissed: 2, methodCovered: 16,
                    classMissed: 0, classCovered: 2)
            };

            // Class "org/jacoco/core/tools/ExecFileLoader":
            // INSTRUCTION {missed=0, covered=95}
            // BRANCH      {missed=1, covered=1}
            // LINE        {missed=0, covered=32}
            // COMPLEXITY  {missed=1, covered=7}
            // METHOD      {missed=0, covered=7}
            // CLASS       {missed=0, covered=1}
            Finding classFinding = new Finding
            {
                FullPath = "org/jacoco/core/tools/ExecFileLoader",
                FileName = "ExecFileLoader.java",
                Context = "class",
                Metrics = BuildMetrics(
                    instructionMissed: 0, instructionCovered: 95,
                    branchMissed: 1, branchCovered: 1,
                    lineMissed: 0, lineCovered: 32,
                    complexityMissed: 1, complexityCovered: 7,
                    methodMissed: 0, methodCovered: 7,
                    classMissed: 0, classCovered: 1)
            };

            // Method "save" in dieser Klasse:
            // INSTRUCTION {missed=0, covered=30}
            // BRANCH      {missed=1, covered=1}
            // LINE        {missed=0, covered=11}
            // COMPLEXITY  {missed=1, covered=1}
            // METHOD      {missed=0, covered=1}
            // (kein CLASS-Counter auf Methodenebene)
            Finding methodFinding = new Finding
            {
                FullPath = "org/jacoco/core/tools/ExecFileLoader#save",
                FileName = "ExecFileLoader.java",
                Context = "method",
                Location = new MetricLocation { StartLine = 108 },
                Metrics = BuildMetrics(
                    instructionMissed: 0, instructionCovered: 30,
                    branchMissed: 1, branchCovered: 1,
                    lineMissed: 0, lineCovered: 11,
                    complexityMissed: 1, complexityCovered: 1,
                    methodMissed: 0, methodCovered: 1)
            };

            Finding execDumpClientClassFinding = new Finding
            {
                FullPath = "org/jacoco/core/tools/ExecDumpClient",
                FileName = "ExecDumpClient.java",
                Context = "class",
                Metrics = BuildMetrics(
            instructionMissed: 7, instructionCovered: 115,
            branchMissed: 0, branchCovered: 4,
            lineMissed: 3, lineCovered: 41,
            complexityMissed: 2, complexityCovered: 11,
            methodMissed: 2, methodCovered: 9,
            classMissed: 0, classCovered: 1)
            };


            Finding execDumpClientSleepMethodFinding = new Finding
            {
                FullPath = "org/jacoco/core/tools/ExecDumpClient#sleep",
                FileName = "ExecDumpClient.java",
                Context = "method",
                Location = new MetricLocation { StartLine = 157 },
                Metrics = BuildMetrics(
           instructionMissed: 5, instructionCovered: 5,
           branchMissed: null, branchCovered: null, // no BRANCH-Counter
           lineMissed: 1, lineCovered: 3,
           complexityMissed: 0, complexityCovered: 1,
           methodMissed: 0, methodCovered: 1)
            };

            return new Dictionary<string, Finding>
            {
                { packageFinding.FullPath, packageFinding },
                { classFinding.FullPath,   classFinding },
                { methodFinding.FullPath,  methodFinding },
                { execDumpClientClassFinding.FullPath,  execDumpClientClassFinding },
                { execDumpClientSleepMethodFinding.FullPath, execDumpClientSleepMethodFinding }
            };
        }
    }
}
