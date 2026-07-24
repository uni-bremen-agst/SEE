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
        /// Initializes a new exception-flow path.
        /// </summary>
        /// <param name="steps">The ordered path steps.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="steps"/> is
        /// <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="steps"/> contains no steps or a
        /// <see langword="null"/> step.
        /// </exception>
        public ExceptionFlowPath(
            IEnumerable<ExceptionFlowPathStep> steps)
        {
            ArgumentNullException.ThrowIfNull(steps);

            this.steps = steps.ToArray();

            if (this.steps.Length == 0)
            {
                throw new ArgumentException(
                    "An exception-flow path must contain at least one step.",
                    nameof(steps));
            }

            if (this.steps.Any(static step => step == null))
            {
                throw new ArgumentException(
                    "An exception-flow path must not contain null steps.",
                    nameof(steps));
            }

            DeduplicationKey =
                CreateDeduplicationKey(this.steps);
        }

        /// <summary>
        /// Gets the ordered steps of this exception-flow path.
        /// </summary>
        /// <value>The ordered path steps.</value>
        public IReadOnlyList<ExceptionFlowPathStep> Steps =>
            steps;

        /// <summary>
        /// Gets the stable key used to deduplicate equivalent paths.
        /// </summary>
        /// <value>The path deduplication key.</value>
        internal string DeduplicationKey { get; }

        /// <summary>
        /// Creates a new path with the specified step inserted at the
        /// beginning.
        /// </summary>
        /// <param name="step">The step to prepend.</param>
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
            ArgumentNullException.ThrowIfNull(step);

            ExceptionFlowPathStep[] prefixedSteps =
                new ExceptionFlowPathStep[steps.Length + 1];

            prefixedSteps[0] = step;

            Array.Copy(
                sourceArray: steps,
                sourceIndex: 0,
                destinationArray: prefixedSteps,
                destinationIndex: 1,
                length: steps.Length);

            return new ExceptionFlowPath(prefixedSteps);
        }

        /// <summary>
        /// Creates a stable key from the complete ordered path content.
        /// </summary>
        /// <param name="pathSteps">The ordered path steps.</param>
        /// <returns>The created deduplication key.</returns>
        private static string CreateDeduplicationKey(
            IReadOnlyList<ExceptionFlowPathStep> pathSteps)
        {
            StringBuilder builder = new();

            foreach (ExceptionFlowPathStep step in pathSteps)
            {
                AppendKeyPart(
                    builder,
                    ((int)step.Kind).ToString(
                        CultureInfo.InvariantCulture));

                AppendKeyPart(
                    builder,
                    step.SymbolName);

                AppendKeyPart(
                    builder,
                    step.FilePath ?? string.Empty);

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
            }

            return builder.ToString();
        }

        /// <summary>
        /// Appends one length-prefixed value to a path key.
        /// </summary>
        /// <param name="builder">The target key builder.</param>
        /// <param name="value">The value to append.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="builder"/> or
        /// <paramref name="value"/> is <see langword="null"/>.
        /// </exception>
        private static void AppendKeyPart(
            StringBuilder builder,
            string value)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(value);

            builder.Append(
                value.Length.ToString(
                    CultureInfo.InvariantCulture));

            builder.Append(':');
            builder.Append(value);
            builder.Append('|');
        }
    }
}
