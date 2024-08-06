namespace SEE.Scanner
{
    /// <summary>
    /// Represents a token from a source code file, including <see cref="Text"/>, a <see cref="TokenType"/>,
    /// and some <see cref="Modifiers"/>.
    /// </summary>
    /// <param name="Text">The text of the token.</param>
    /// <param name="TokenType">The type of the token (e.g., <c>class</c>).</param>
    /// <param name="Language">The language of the token.</param>
    /// <param name="Modifiers">The modifiers of the token (e.g., <c>static</c>.</param>
    public abstract record SEEToken(string Text, TokenType TokenType, TokenLanguage Language, TokenModifiers Modifiers = TokenModifiers.None);
}
