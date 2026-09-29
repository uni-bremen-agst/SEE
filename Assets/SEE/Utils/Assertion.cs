using System.Diagnostics;

namespace SEE.Utils
{
    /// <summary>
    /// Assertions reported through <see cref="Logging.Logger"/>.
    ///
    /// These behave as <c>UnityEngine.Debug.Assert</c> did: a violated assumption
    /// is logged rather than thrown, and the call is removed entirely -- its
    /// arguments included -- unless assertions are enabled for the assembly being
    /// compiled. Unity defines UNITY_ASSERTIONS in the editor and in development
    /// builds; a host outside Unity enables them by defining SEE_ASSERTIONS.
    ///
    /// Note that the symbol is resolved where the call stands, not where these
    /// methods are declared, so a prebuilt assembly carries whatever was decided
    /// when it was compiled.
    /// </summary>
    /// <remarks>
    /// This is deliberately separate from <see cref="Assertions"/>, which throws
    /// and which depends on Unity, and so cannot serve the layers below it.
    /// </remarks>
    public static class Assertion
    {
        /// <summary>
        /// Logs a failed assertion if <paramref name="condition"/> is false.
        /// </summary>
        /// <param name="condition">The assumption that is expected to hold.</param>
        [Conditional("UNITY_ASSERTIONS")]
        [Conditional("SEE_ASSERTIONS")]
        public static void Assert(bool condition)
        {
            Assert(condition, "Assertion failed");
        }

        /// <summary>
        /// Logs <paramref name="message"/> if <paramref name="condition"/> is false.
        /// </summary>
        /// <param name="condition">The assumption that is expected to hold.</param>
        /// <param name="message">A message describing the assumption.</param>
        [Conditional("UNITY_ASSERTIONS")]
        [Conditional("SEE_ASSERTIONS")]
        public static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                Logging.Logger.LogAssertion(message);
            }
        }
    }
}
