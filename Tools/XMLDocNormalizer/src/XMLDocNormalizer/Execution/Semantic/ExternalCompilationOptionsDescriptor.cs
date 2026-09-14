using System.Collections.Immutable;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Describes the compiler options explicitly serialized in a Portable PDB.
    /// </summary>
    internal sealed class ExternalCompilationOptionsDescriptor
    {
        /// <summary>
        /// Provides case-sensitive lookup without assigning semantics to the
        /// serialized option order.
        /// </summary>
        private readonly ImmutableDictionary<string, string> valuesByKey;

        /// <summary>
        /// Initializes compilation-option provenance from validated entries.
        /// </summary>
        /// <param name="options">The entries in their original blob order.</param>
        internal ExternalCompilationOptionsDescriptor(
            ImmutableArray<ExternalCompilationOption> options)
        {
            Options = options.IsDefault
                ? ImmutableArray<ExternalCompilationOption>.Empty
                : options;
            ImmutableDictionary<string, string>.Builder values =
                ImmutableDictionary.CreateBuilder<string, string>(StringComparer.Ordinal);

            foreach (ExternalCompilationOption option in Options)
            {
                values[option.Key] = option.Value;
            }

            valuesByKey = values.ToImmutable();
        }

        /// <summary>
        /// Gets the options in their original Portable PDB blob order.
        /// </summary>
        /// <value>The exact serialized option entries.</value>
        public ImmutableArray<ExternalCompilationOption> Options { get; }

        /// <summary>
        /// Tries to get an option value by its exact case-sensitive key.
        /// </summary>
        /// <param name="key">The exact option key.</param>
        /// <param name="value">The serialized value when present.</param>
        /// <returns>
        /// <see langword="true"/> when the key was serialized; otherwise
        /// <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="key"/> is <see langword="null"/>.
        /// </exception>
        public bool TryGetValue(string key, out string value)
        {
            ArgumentNullException.ThrowIfNull(key);
            return valuesByKey.TryGetValue(key, out value!);
        }
    }
}
