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
    public sealed class ConsoleFindingsReporterTests
    {
        /// <summary>
        /// Ensures that calling <see cref="ConsoleFindingsReporter.ReportFile"/>
        /// with a single finding writes the expected formatted output.
        /// </summary>
        [Fact]
        public void ReportFile_WithSingleFinding_WritesExpectedConsoleOutput()
        {
            // Arrange
            ConsoleFindingsReporter reporter = new ConsoleFindingsReporter();

            Finding finding = TestFindingFactory.Create(
                smellId: "DOC200",
                severity: Severity.Error,
                filePath: "Test.cs",
                tagName: "summary",
                line: 12,
                column: 5);

            List<Finding> findings = new() { finding };

            using StringWriter writer = new StringWriter();
            System.Console.SetOut(writer);

            // Act
            reporter.ReportFile("Test.cs", findings);

            // Assert
            string output = writer.ToString();

            Assert.Contains("Test.cs", output, StringComparison.Ordinal);
            Assert.Contains("[DOC200|Error]", output, StringComparison.Ordinal);
            Assert.Contains("[12,5]", output, StringComparison.Ordinal);
            Assert.Contains("<summary>", output, StringComparison.Ordinal);
            Assert.Contains("Snippet", output, StringComparison.Ordinal);
        }

        /// <summary>
        /// Ensures that calling <see cref="ConsoleFindingsReporter.ReportFile"/>
        /// with no findings produces no console output.
        /// </summary>
        [Fact]
        public void ReportFile_WithEmptyFindings_WritesNothing()
        {
            // Arrange
            ConsoleFindingsReporter reporter = new ConsoleFindingsReporter();

            using StringWriter writer = new StringWriter();
            System.Console.SetOut(writer);

            // Act
            reporter.ReportFile("Test.cs", new List<Finding>());

            // Assert
            string output = writer.ToString();
            Assert.True(string.IsNullOrWhiteSpace(output));
        }

        /// <summary>
        /// Ensures that calling <see cref="ConsoleFindingsReporter.Complete"/>
        /// does not throw and does not write additional output.
        /// </summary>
        [Fact]
        public void Complete_DoesNotThrow_AndProducesNoOutput()
        {
            // Arrange
            ConsoleFindingsReporter reporter = new ConsoleFindingsReporter();

            using StringWriter writer = new StringWriter();
            System.Console.SetOut(writer);

            // Act
            reporter.Complete();

            // Assert
            string output = writer.ToString();
            Assert.True(string.IsNullOrWhiteSpace(output));
        }
    }
}
