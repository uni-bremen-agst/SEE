using System;
using System.Collections.Generic;
using System.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using SEE.Utils;

namespace SEE.Scanner
{
    /// <summary>
    /// Modifiers that can be applied to a semantic token
    /// (for example, <see cref="Static"/> or <see cref="Deprecated"/>).
    /// </summary>
    [Flags]
    public enum TokenModifiers
    {
        /// <summary>
        /// No modifiers.
        /// </summary>
        None = 0,

        /// <summary>
        /// For declarations of symbols.
        /// </summary>
        Declaration = 1 << 0,

        /// <summary>
        /// For definitions of symbols, for example, in header files.
        /// </summary>
        Definition = 1 << 1,

        /// <summary>
        /// For readonly variables and member fields, as well as constants.
        /// </summary>
        Readonly = 1 << 2,

        /// <summary>
        /// For class members that are static.
        /// </summary>
        Static = 1 << 3,

        /// <summary>
        /// For symbols that should no longer be used.
        /// </summary>
        Deprecated = 1 << 4,

        /// <summary>
        /// For types and member functions that are abstract.
        /// </summary>
        Abstract = 1 << 5,

        /// <summary>
        /// For functions that are marked as asynchronous.
        /// </summary>
        Async = 1 << 6,

        /// <summary>
        /// For variable references where the variable is reassigned.
        /// </summary>
        Modification = 1 << 7,

        /// <summary>
        /// For occurrences of symbols in documentation.
        /// </summary>
        Documentation = 1 << 8,

        /// <summary>
        /// For symbols that are part of the standard library.
        /// </summary>
        DefaultLibrary = 1 << 9
    }

    /// <summary>
    /// Extension methods for <see cref="TokenModifiers"/>.
    /// </summary>
    public static class TokenModifiersExtensions
    {
        /// <summary>
        /// Mapping from <see cref="SemanticTokenModifier"/> to <see cref="TokenModifiers"/>.
        /// </summary>
        private static readonly IDictionary<SemanticTokenModifier, TokenModifiers> tokenModifierMapping = new Dictionary<SemanticTokenModifier, TokenModifiers>
        {
            { SemanticTokenModifier.Declaration, TokenModifiers.Declaration },
            { SemanticTokenModifier.Definition, TokenModifiers.Definition },
            { SemanticTokenModifier.Readonly, TokenModifiers.Readonly },
            { SemanticTokenModifier.Static, TokenModifiers.Static },
            { SemanticTokenModifier.Deprecated, TokenModifiers.Deprecated },
            { SemanticTokenModifier.Abstract, TokenModifiers.Abstract },
            { SemanticTokenModifier.Async, TokenModifiers.Async },
            { SemanticTokenModifier.Modification, TokenModifiers.Modification },
            { SemanticTokenModifier.Documentation, TokenModifiers.Documentation },
            { SemanticTokenModifier.DefaultLibrary, TokenModifiers.DefaultLibrary }
        };

        /// <summary>
        /// Converts a <see cref="SemanticTokenModifier"/> to a <see cref="TokenModifiers"/>.
        /// </summary>
        /// <param name="modifier">The <see cref="SemanticTokenModifier"/> to convert.</param>
        /// <returns>The <see cref="TokenModifiers"/> that corresponds to the given <see cref="SemanticTokenModifier"/>.</returns>
        public static TokenModifiers FromLspTokenModifier(this SemanticTokenModifier modifier)
        {
            return tokenModifierMapping.GetValueOrDefault(modifier);
        }

        /// <summary>
        /// Converts a <see cref="TokenModifiers"/> to the name of a tag that can be used in
        /// TextMeshPro's rich text markup.
        /// </summary>
        /// <param name="modifiers">The <see cref="TokenModifiers"/> to convert. Should be a single flagUp.</param>
        /// <returns>The name of a tag that can be used in TextMeshPro's rich text markup.</returns>
        public static string ToRichTextTag(this TokenModifiers modifiers)
        {
            return modifiers switch
            {
                TokenModifiers.Static => "i",
                TokenModifiers.Deprecated => "strikethrough",
                TokenModifiers.Modification => "u",
                TokenModifiers.Documentation => "i",
                _ => string.Empty
            };
        }

        /// <summary>
        /// Returns a stream of <see cref="TokenModifiers"/> that are set in the given <see cref="TokenModifiers"/>.
        /// </summary>
        /// <param name="modifiers">The <see cref="TokenModifiers"/> to get the set modifiers from.</param>
        /// <returns>An enumerable of <see cref="TokenModifiers"/> that are set in the given <see cref="TokenModifiers"/>.</returns>
        public static IEnumerable<TokenModifiers> AsEnumerable(this TokenModifiers modifiers)
        {
            return Enum.GetValues(typeof(TokenModifiers)).Cast<TokenModifiers>().Where(modifier => modifiers.HasFlag(modifier));
        }
    }
}
