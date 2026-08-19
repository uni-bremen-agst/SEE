using System.Globalization;
using System.Text;

namespace XMLDocNormalizer.Models
{
    /// <summary>
    /// Represents one distinct source-level path from an analyzed member
    /// to an exception source.
    /// </summary>
    internal sealed class ExceptionFlowPath
    {
        /// <summary>
        /// Stores the path steps in traversal order.
        /// </summary>
        private readonly ExceptionFlowPathStep[] steps;

        /// <summary>
        /// Initializes a single-step exception-flow path.
        /// </summary>
        /// <param name="terminalStep">
        /// The terminal exception-source step.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="terminalStep"/> is
        /// <see langword="null"/>.
        /// </exception>
        public ExceptionFlowPath(
            ExceptionFlowPathStep terminalStep)
        {
            ArgumentNullException.ThrowIfNull(
                terminalStep);

            steps =
                [terminalStep];

            DeduplicationKey =
                CreateStepDeduplicationKey(
                    terminalStep);
        }

        /// <summary>
        /// Initializes a path by prepending one step to an existing valid
        /// exception-flow path.
        /// </summary>
        /// <param name="prefix">
        /// The step to prepend.
        /// </param>
        /// <param name="suffix">
        /// The existing path to append after the prefix.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="prefix"/> or
        /// <paramref name="suffix"/> is <see langword="null"/>.
        /// </exception>
        private ExceptionFlowPath(
            ExceptionFlowPathStep prefix,
            ExceptionFlowPath suffix)
        {
            ArgumentNullException.ThrowIfNull(
                prefix);

            ArgumentNullException.ThrowIfNull(
                suffix);

            steps =
                new ExceptionFlowPathStep[
                    suffix.steps.Length + 1];

            steps[0] =
                prefix;

            Array.Copy(
                sourceArray: suffix.steps,
                sourceIndex: 0,
                destinationArray: steps,
                destinationIndex: 1,
                length: suffix.steps.Length);

            DeduplicationKey =
                string.Concat(
                    CreateStepDeduplicationKey(
                        prefix),
                    suffix.DeduplicationKey);
        }

        /// <summary>
        /// Gets the ordered steps of this exception-flow path.
        /// </summary>
        /// <value>
        /// The ordered path steps.
        /// </value>
        public IReadOnlyList<ExceptionFlowPathStep> Steps =>
            steps;

        /// <summary>
        /// Gets the stable key used to deduplicate equivalent paths.
        /// </summary>
        /// <value>
        /// The path deduplication key.
        /// </value>
        internal string DeduplicationKey { get; }

        /// <summary>
        /// Creates a new path with the specified step inserted at the
        /// beginning.
        /// </summary>
        /// <param name="step">
        /// The step to prepend.
        /// </param>
        /// <returns>
        /// A new path beginning with <paramref name="step"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="step"/> is
        /// <see langword="null"/>.
        /// </exception>
        public ExceptionFlowPath Prepend(
            ExceptionFlowPathStep step)
        {
            ArgumentNullException.ThrowIfNull(
                step);

            return new ExceptionFlowPath(
                step,
                this);
        }

        /// <summary>
        /// Creates the stable deduplication-key fragment for one path step.
        /// </summary>
        /// <param name="step">
        /// The path step to serialize.
        /// </param>
        /// <returns>
        /// The serialized key fragment for the path step.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="step"/> is
        /// <see langword="null"/>.
        /// </exception>
        private static string CreateStepDeduplicationKey(
            ExceptionFlowPathStep step)
        {
            ArgumentNullException.ThrowIfNull(
                step);

            StringBuilder builder =
                new();

            AppendKeyPart(
                builder,
                ((int)step.Kind).ToString(
                    CultureInfo.InvariantCulture) ??
                string.Empty);

            AppendKeyPart(
                builder,
                step.SymbolName ??
                string.Empty);

            AppendKeyPart(
                builder,
                step.FilePath ??
                string.Empty);

            AppendKeyPart(
                builder,
                step.Line?.ToString(
                    CultureInfo.InvariantCulture) ??
                string.Empty);

            AppendKeyPart(
                builder,
                step.Column?.ToString(
                    CultureInfo.InvariantCulture) ??
                string.Empty);

            return builder.ToString();
        }

        /// <summary>
        /// Appends one length-prefixed value to a path-key fragment.
        /// </summary>
        /// <param name="builder">
        /// The target key builder.
        /// </param>
        /// <param name="value">
        /// The value to append.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="builder"/> or
        /// <paramref name="value"/> is <see langword="null"/>.
        /// </exception>
        private static void AppendKeyPart(
            StringBuilder builder,
            string value)
        {
            ArgumentNullException.ThrowIfNull(
                builder);

            ArgumentNullException.ThrowIfNull(
                value);

            builder.Append(
                value.Length.ToString(
                    CultureInfo.InvariantCulture));

            builder.Append(':');
            builder.Append(value);
            builder.Append('|');
        }
    }
}
