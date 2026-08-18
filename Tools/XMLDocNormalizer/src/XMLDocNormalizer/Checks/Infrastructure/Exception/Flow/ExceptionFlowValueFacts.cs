namespace XMLDocNormalizer.Checks.Infrastructure.Exception.Flow
{
    /// <summary>
    /// Describes value properties that have been proven for an expression or
    /// parameter.
    /// </summary>
    [Flags]
    internal enum ExceptionFlowValueFacts
    {
        /// <summary>
        /// No value property has been proven.
        /// </summary>
        None = 0,

        /// <summary>
        /// The value is proven not to be <see langword="null"/>.
        /// </summary>
        NonNull = 1 << 0,

        /// <summary>
        /// The value is proven to be a non-null, non-empty string.
        /// </summary>
        NonEmptyString = 1 << 1,

        /// <summary>
        /// The value is proven to be a non-null string containing at least one
        /// non-whitespace character.
        /// </summary>
        NonWhiteSpaceString = 1 << 2,

        /// <summary>
        /// The value is a sequence whose produced elements are proven not to
        /// be <see langword="null"/>.
        /// </summary>
        NonNullElements = 1 << 3
    }

    /// <summary>
    /// Provides operations for normalized exception-flow value facts.
    /// </summary>
    internal static class ExceptionFlowValueFactsExtensions
    {
        /// <summary>
        /// Normalizes implied facts so stronger string facts also contain their
        /// weaker prerequisites.
        /// </summary>
        /// <param name="facts">The facts to normalize.</param>
        /// <returns>The normalized facts.</returns>
        public static ExceptionFlowValueFacts Normalize(
            this ExceptionFlowValueFacts facts)
        {
            if ((facts &
                 ExceptionFlowValueFacts.NonWhiteSpaceString) != 0)
            {
                facts |=
                    ExceptionFlowValueFacts.NonEmptyString;
            }

            if ((facts &
                 ExceptionFlowValueFacts.NonEmptyString) != 0)
            {
                facts |=
                    ExceptionFlowValueFacts.NonNull;
            }

            return facts;
        }

        /// <summary>
        /// Determines whether all required facts are present.
        /// </summary>
        /// <param name="facts">The available facts.</param>
        /// <param name="requiredFacts">The required facts.</param>
        /// <returns>
        /// <see langword="true"/> if all required facts are present; otherwise
        /// <see langword="false"/>.
        /// </returns>
        public static bool ContainsAll(
            this ExceptionFlowValueFacts facts,
            ExceptionFlowValueFacts requiredFacts)
        {
            return (facts & requiredFacts) == requiredFacts;
        }
    }
}
