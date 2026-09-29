using System.Diagnostics;

namespace SEE.Utils
{
    /// <summary>
    /// The assertions used by the layers of SEE that must not depend on Unity.
    ///
    /// Every call here is removed entirely -- its arguments included -- unless
    /// assertions are enabled for the assembly being compiled. Unity defines
    /// UNITY_ASSERTIONS in the editor and in development builds; a host outside
    /// Unity enables them by defining SEE_ASSERTIONS. The symbol is resolved
    /// where the call stands, not where these methods are declared, so a prebuilt
    /// assembly carries whatever was decided when it was compiled.
    ///
    /// The methods come in two families, and they differ in what a violated
    /// assumption does. This mirrors the split Unity itself makes, and each
    /// method replaces the Unity method of the same name:
    /// <list type="bullet">
    /// <item><description><see cref="Assert(bool)"/> logs, as
    /// <c>UnityEngine.Debug.Assert</c> does.</description></item>
    /// <item><description><see cref="IsTrue(bool)"/> and
    /// <see cref="IsNotNull{T}(T)"/> throw an <see cref="AssertionException"/>,
    /// as the like-named methods of <c>UnityEngine.Assertions.Assert</c> do
    /// while its <c>raiseExceptions</c> holds its default of true.</description></item>
    /// </list>
    /// Be aware that a throwing assertion which is compiled away stops guarding
    /// the code that follows it. That is true of Unity's assertions as well.
    /// </summary>
    /// <remarks>
    /// This is deliberately separate from <see cref="Assertions"/>, which depends
    /// on Unity and so cannot serve the layers below it.
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

        /// <summary>
        /// Throws an <see cref="AssertionException"/> if <paramref name="condition"/>
        /// is false.
        /// </summary>
        /// <param name="condition">The assumption that is expected to hold.</param>
        /// <exception cref="AssertionException">Thrown if <paramref name="condition"/>
        /// is false.</exception>
        [Conditional("UNITY_ASSERTIONS")]
        [Conditional("SEE_ASSERTIONS")]
        public static void IsTrue(bool condition)
        {
            IsTrue(condition, null);
        }

        /// <summary>
        /// Throws an <see cref="AssertionException"/> carrying
        /// <paramref name="message"/> if <paramref name="condition"/> is false.
        /// </summary>
        /// <param name="condition">The assumption that is expected to hold.</param>
        /// <param name="message">A message describing the assumption.</param>
        /// <exception cref="AssertionException">Thrown if <paramref name="condition"/>
        /// is false.</exception>
        [Conditional("UNITY_ASSERTIONS")]
        [Conditional("SEE_ASSERTIONS")]
        public static void IsTrue(bool condition, string message)
        {
            if (!condition)
            {
                throw new AssertionException(Describe("Value was false", message));
            }
        }

        /// <summary>
        /// Throws an <see cref="AssertionException"/> if <paramref name="value"/>
        /// is null.
        /// </summary>
        /// <typeparam name="T">The type of <paramref name="value"/>.</typeparam>
        /// <param name="value">The value expected not to be null.</param>
        /// <exception cref="AssertionException">Thrown if <paramref name="value"/>
        /// is null.</exception>
        [Conditional("UNITY_ASSERTIONS")]
        [Conditional("SEE_ASSERTIONS")]
        public static void IsNotNull<T>(T value) where T : class
        {
            IsNotNull(value, null);
        }

        /// <summary>
        /// Throws an <see cref="AssertionException"/> carrying
        /// <paramref name="message"/> if <paramref name="value"/> is null.
        /// </summary>
        /// <typeparam name="T">The type of <paramref name="value"/>.</typeparam>
        /// <param name="value">The value expected not to be null.</param>
        /// <param name="message">A message describing the assumption.</param>
        /// <exception cref="AssertionException">Thrown if <paramref name="value"/>
        /// is null.</exception>
        [Conditional("UNITY_ASSERTIONS")]
        [Conditional("SEE_ASSERTIONS")]
        public static void IsNotNull<T>(T value, string message) where T : class
        {
            if (value is null)
            {
                throw new AssertionException(Describe($"{typeof(T)} was null", message));
            }
        }

        /// <summary>
        /// Combines the caller's <paramref name="message"/>, if any, with the
        /// <paramref name="reason"/> the assumption was found to be violated.
        /// </summary>
        /// <param name="reason">How the assumption was violated.</param>
        /// <param name="message">The caller's description; may be null or empty.</param>
        /// <returns>The text of the exception to be thrown.</returns>
        private static string Describe(string reason, string message)
        {
            return string.IsNullOrEmpty(message) ? reason : $"{message}\n{reason}";
        }
    }
}
