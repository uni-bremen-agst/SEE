using System.Text;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Reporting.Console;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Reporting.Console
{
    /// <summary>
    /// Tests for <see cref="ConsoleFindingsReporter"/>.
    /// </summary>
    /// <remarks>
    /// These tests verify that findings are written to standard output
    /// in the expected human-readable format.
    /// </remarks>
    [Collection("Console-dependent tests")]
    public sealed class ConsoleFindingsReporterTests
    {
        /// <summary>
        /// Ensures that calling <see cref="ConsoleFindingsReporter.ReportFile"/>
        /// with a single finding writes the expected formatted output.
        /// </summary>
        [Fact]
        public void ReportFile_WithSingleFinding_WritesExpectedConsoleOutput()
        {
            ConsoleFindingsReporter reporter = new ConsoleFindingsReporter();

            Finding finding = TestFindingFactory.Create(
                smellId: "DOC200",
                severity: Severity.Error,
                filePath: "Test.cs",
                tagName: "summary",
                line: 12,
                column: 5);

            List<Finding> findings = new() { finding };

            TextWriter originalOut = System.Console.Out;
            using StringWriter writer = new StringWriter();

            try
            {
                System.Console.SetOut(writer);

                reporter.ReportFile("Test.cs", findings);

                string output = writer.ToString();

                Assert.Contains("Test.cs", output, StringComparison.Ordinal);
                Assert.Contains("[DOC200|Error]", output, StringComparison.Ordinal);
                Assert.Contains("[12,5]", output, StringComparison.Ordinal);
                Assert.Contains("<summary>", output, StringComparison.Ordinal);
                Assert.Contains("Snippet", output, StringComparison.Ordinal);
            }
            finally
            {
                System.Console.SetOut(originalOut);
            }
        }

        /// <summary>
        /// Ensures that calling <see cref="ConsoleFindingsReporter.ReportFile"/>
        /// with no findings produces no console output.
        /// </summary>
        [Fact]
        public void ReportFile_WithEmptyFindings_WritesNothing()
        {
            ConsoleFindingsReporter reporter = new ConsoleFindingsReporter();

            TextWriter originalOut = System.Console.Out;
            using StringWriter writer = new StringWriter();

            try
            {
                System.Console.SetOut(writer);

                reporter.ReportFile("Test.cs", new List<Finding>());

                string output = writer.ToString();
                Assert.True(string.IsNullOrWhiteSpace(output));
            }
            finally
            {
                System.Console.SetOut(originalOut);
            }
        }

        /// <summary>
        /// Ensures that calling <see cref="ConsoleFindingsReporter.Complete"/>
        /// does not throw and does not write additional output.
        /// </summary>
        [Fact]
        public void Complete_DoesNotThrow_AndProducesNoOutput()
        {
            ConsoleFindingsReporter reporter = new ConsoleFindingsReporter();

            TextWriter originalOut = System.Console.Out;
            using StringWriter writer = new StringWriter();

            try
            {
                System.Console.SetOut(writer);

                reporter.Complete();

                string output = writer.ToString();
                Assert.True(string.IsNullOrWhiteSpace(output));
            }
            finally
            {
                System.Console.SetOut(originalOut);
            }
        }
    }
}
