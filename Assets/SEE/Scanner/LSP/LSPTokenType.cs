using System.Collections.Generic;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace SEE.Scanner.LSP
{
    /// <summary>
    /// Represents a kind of token in an LSP-supported programming language, with an associated color.
    /// For example, this may be a <see cref="Class"/> or an <see cref="Operator"/>.
    /// </summary>
    public record LSPTokenType : TokenType
    {
        private LSPTokenType(string name, string color) : base(name, color) { }

        /// <summary>
        /// For identifiers that declare or reference a namespace, module, or package.
        /// </summary>
        public static readonly LSPTokenType Namespace = new("namespace", "#FFCB6B");

        /// <summary>
        /// For identifiers that declare or reference a class type.
        /// </summary>
        public static readonly LSPTokenType Class = new("class", "#FFCB6B");

        /// <summary>
        /// For identifiers that declare or reference an enumeration type.
        /// </summary>
        public static readonly LSPTokenType Enum = new("enum", "#F78C6C");

        /// <summary>
        /// For identifiers that declare or reference an interface type.
        /// </summary>
        public static readonly LSPTokenType Interface = new("interface", "#C3E88D");

        /// <summary>
        /// For identifiers that declare or reference a struct type.
        /// </summary>
        public static readonly LSPTokenType Struct = new("struct", "#FFCB6B");

        /// <summary>
        /// For identifiers that declare or reference a type parameter.
        /// </summary>
        public static readonly LSPTokenType TypeParameter = new("typeParameter", "#C3E88D");

        /// <summary>
        /// For identifiers that declare or reference function or method parameter.
        /// </summary>
        public static readonly LSPTokenType Parameter = new("parameter", "#F78C6C");
        /// <summary>
        /// For identifiers that declare or reference a local or global variable.
        /// </summary>
        public static readonly LSPTokenType Variable = new("variable", "#EEFFE3");
        /// <summary>
        /// For identifiers that declare or reference a member property, member field, or member variable.
        /// </summary>
        public static readonly LSPTokenType Property = new("property", "#EEFFFF");

        /// <summary>
        /// For identifiers that declare or reference an enumeration property, constant, or member.
        /// </summary>
        public static readonly LSPTokenType EnumMember = new("enumMember", "#F78C6C");

        /// <summary>
        /// For identifiers that declare an event property.
        /// </summary>
        public static readonly LSPTokenType Event = new("event", "#EEFFE3");

        /// <summary>
        /// For identifiers that declare a function.
        /// </summary>
        public static readonly LSPTokenType Function = new("function", "#82AAFF");

        /// <summary>
        /// For identifiers that declare a member function or method.
        /// </summary>
        public static readonly LSPTokenType Method = new("method", "#82AAFF");

        /// <summary>
        /// For identifiers that declare a macro.
        /// </summary>
        public static readonly LSPTokenType Macro = new("macro", "#C792EA");

        /// <summary>
        /// For tokens that represent a language keyword.
        /// </summary>
        public static readonly LSPTokenType Keyword = new("keyword", "#C792EA");

        /// <summary>
        /// For tokens that represent a modifier.
        /// </summary>
        public static readonly LSPTokenType Modifier = new("modifier", "#C792EA");

        /// <summary>
        /// For tokens that represent a comment.
        /// </summary>
        public static readonly LSPTokenType Comment = new("comment", "#717CB4");

        /// <summary>
        /// For tokens that represent a string literal.
        /// </summary>
        public static readonly LSPTokenType String = new("string", "#C3E88D");

        /// <summary>
        /// For tokens that represent a number literal.
        /// </summary>
        public static readonly LSPTokenType Number = new("number", "#F78C6C");

        /// <summary>
        /// For tokens that represent a regular expression literal.
        /// </summary>
        public static readonly LSPTokenType Regexp = new("regexp", "#93E88D");

        /// <summary>
        /// For tokens that represent an operator.
        /// </summary>
        public static readonly LSPTokenType Operator = new("operator", "#89DDFF");

        /// <summary>
        /// For identifiers that declare or reference decorators and annotations.
        /// </summary>
        public static readonly LSPTokenType Decorator = new("decorator", "#FFCB6B");

        /// <summary>
        /// For identifiers that declare a label.
        /// </summary>
        public static readonly LSPTokenType Label = new("label", "#C3D3DE");

        /// <summary>
        /// Represents a generic type. Acts as a fallback for types which can't be mapped to one of the other types.
        /// </summary>
        public static readonly LSPTokenType Type = new("type", "#FFFFFF");

        /// <summary>
        /// A mapping of <see cref="SemanticTokenType"/> to <see cref="LSPTokenType"/>.
        /// </summary>
        private static readonly Dictionary<SemanticTokenType, LSPTokenType> semanticTokenMapping = new()
        {
            { SemanticTokenType.Comment, Comment },
            { SemanticTokenType.Keyword, Keyword },
            { SemanticTokenType.String, String },
            { SemanticTokenType.Number, Number },
            { SemanticTokenType.Regexp, Regexp },
            { SemanticTokenType.Operator, Operator },
            { SemanticTokenType.Namespace, Namespace },
            { SemanticTokenType.Type, Type },
            { SemanticTokenType.Struct, Struct },
            { SemanticTokenType.Class, Class },
            { SemanticTokenType.Interface, Interface },
            { SemanticTokenType.Enum, Enum },
            { SemanticTokenType.TypeParameter, TypeParameter },
            { SemanticTokenType.Function, Function },
            { SemanticTokenType.Method, Method },
            { SemanticTokenType.Property, Property },
            { SemanticTokenType.Macro, Macro },
            { SemanticTokenType.Variable, Variable },
            { SemanticTokenType.Parameter, Parameter },
            { SemanticTokenType.Label, Label },
            { SemanticTokenType.Modifier, Modifier },
            { SemanticTokenType.Event, Event },
            { SemanticTokenType.EnumMember, EnumMember },
            { SemanticTokenType.Decorator, Decorator }
        };

        /// <summary>
        /// Returns the <see cref="LSPTokenType"/> that corresponds to the given <see cref="SemanticTokenType"/>.
        /// </summary>
        /// <param name="semanticTokenType">The <see cref="SemanticTokenType"/> to map to an <see cref="LSPTokenType"/>.</param>
        /// <returns>The <see cref="LSPTokenType"/> that corresponds to the given <see cref="SemanticTokenType"/>.</returns>
        public static LSPTokenType FromSemanticTokenType(SemanticTokenType semanticTokenType) => semanticTokenMapping.GetValueOrDefault(semanticTokenType, Type);
    }
}
