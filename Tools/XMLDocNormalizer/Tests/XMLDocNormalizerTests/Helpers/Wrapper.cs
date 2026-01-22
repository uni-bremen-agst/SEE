namespace XMLDocNormalizerTests.Helpers
{
    /// <summary>
    /// 
    /// </summary>
    internal static class Wrapper
    {
        /// <summary>
        /// Wraps a member snippet into a minimal class so it becomes valid C# source code.
        /// </summary>
        /// <param name="memberCode">A member declaration such as a method, property, or field.</param>
        /// <returns>Valid C# source containing the member.</returns>
        public static string WrapInClass(string memberCode)
        {
            return
                "/// <summary>\n" +
                "/// This is a test class.\n" +
                "/// </summary>\n" +
                "class C\n" +
                "{\n" +
                memberCode +
                "\n}\n";
        }
    }
}