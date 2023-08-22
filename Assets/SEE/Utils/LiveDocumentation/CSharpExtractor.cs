using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using JetBrains.Annotations;
using SEE.Game.UI.LiveDocumentation.Buffer;

namespace SEE.Utils.LiveDocumentation
{
    /// <summary>
    ///     Extractor class for CSharp source code
    /// </summary>
    public class CSharpExtractor : IExtractor
    {
        /// <summary>
        /// The path of the file which should be parsed
        /// </summary>
        private readonly string filePath;

        /// <summary>
        /// The extracted tokens from the CSharp file
        /// </summary>
        private readonly CommonTokenStream commentTokens;

        /// <summary>
        /// The parser which is used to extract informations from the file
        /// </summary>
        private readonly CSharpParser parser;

        /// <summary>
        /// Constructs a new instance of a <see cref="CSharpExtractor"/>
        /// </summary>
        /// <param name="fileName">The name of the file which should be extracted</param>
        /// <exception cref="FileNotFoundException">When the source code file doesn't exist.</exception>
        public CSharpExtractor(string fileName)
        {
            filePath = fileName;
            if (!File.Exists(fileName))
            {
                throw new FileNotFoundException($"The file {fileName} doesn't exist");
            }

            string input = File.ReadAllText(fileName);
            CSharpFullLexer lexer = new CSharpFullLexer(new AntlrInputStream(input));
            CommonTokenStream tokens = new CommonTokenStream(lexer);
            tokens.Fill();
            parser = new CSharpParser(tokens);

            CSharpFullLexer lexer1 = new CSharpFullLexer(new AntlrInputStream(input));


            commentTokens = new CommonTokenStream(lexer1, 2);
            commentTokens.Fill();
        }


        /// <summary>
        ///     This method will extract the documentation of an given C# Class in a given file.
        /// </summary>
        /// <param name="className">The name of the class</param>
        /// <exception cref="ClassNotFoundException">Is thrown, when the class cant be found</exception>
        /// <returns>The documentation of the class</returns>
        public LiveDocumentationBuffer ExtractClassComments(string className)
        {
            parser.Reset();
            LiveDocumentationBuffer buffer = new();


            (CSharpParser.Type_declarationContext classContext, int upperLineBound) = GetClassByName(className);

            if (classContext != null)
            {
                // Combining the Type 2 Tokens (C# Class Documentation) between the start of the class and the
                // start of the namespace, if the class is the first in the file, or between the start of the class and the
                // end of the last type declaration
                string commentTokens = string.Join(Environment.NewLine, this.commentTokens.GetTokens()
                    .Where(x => x.Type == 2 && x.Line < classContext.Start.Line && x.Line > upperLineBound)
                    .Select(x => x.Text).ToList());

                // Parsing of the class documentation
                CSharpCommentsGrammarLexer lexer = new CSharpCommentsGrammarLexer(new AntlrInputStream(commentTokens));
                CommonTokenStream tokens = new CommonTokenStream(lexer);
                tokens.Fill();
                if (tokens.GetTokens().Count > 1)
                {
                    CSharpCommentsGrammarParser parser = new CSharpCommentsGrammarParser(tokens); ;
                    ProcessSummary(buffer, parser.docs().summary());
                    // Parse C# Doc line by line and write it in the Buffer
                }

                return buffer;
            }

            throw new ClassNotFoundException(className, filePath);
        }

        /// <summary>
        ///     Extracting the methods (and their documentation) from a given class
        /// </summary>
        /// <param name="className">The name of the class</param>
        /// <returns>The methods as a list of <see cref="LiveDocumentationClassMemberBuffer" /></returns>
        public List<LiveDocumentationClassMemberBuffer> ExtractMethods(string className)
        {
            List<LiveDocumentationClassMemberBuffer> buffers = new();
            parser.Reset();

            (CSharpParser.Type_declarationContext classContext, int upperLineBound) = GetClassByName(className);
            // Parse all namespaces and iterate over them

            int methodUpperLineBound = upperLineBound;

            foreach (CSharpParser.Class_member_declarationContext i in classContext.class_definition().class_body().class_member_declarations()
                         .class_member_declaration())
            {
                i.GetText();
                LiveDocumentationClassMemberBuffer methodBuffer = new LiveDocumentationClassMemberBuffer();
                methodBuffer.Documentation = new LiveDocumentationBuffer();
                methodBuffer.LineNumber = i.common_member_declaration().Start.Line;
                // Concatenate the Method signature together.
                // Starting by the modifiers of the method
                string signature = i.all_member_modifiers().all_member_modifier()
                    .Aggregate("", (test, modi) => test + modi.GetText() + " ");
                // If the current class member is a method

                // If the class member is a method
                if (i.common_member_declaration().method_declaration() is { } methodDeclarationContext)
                {
                    (CSharpCommentsGrammarParser parser, CommonTokenStream tokens) = CreateParserForMethod(methodUpperLineBound, i.Start.Line);
                    LiveDocumentationBuffer methodDoc = new LiveDocumentationBuffer();
                    if (tokens.GetTokens().Count > 1)
                    {
                        ProcessSummary(methodDoc, parser.docs().summary());
                        parser.Reset();
                    }

                    methodBuffer.Documentation = methodDoc;

                    methodBuffer.Parameters =
                        ProcessParametersDocumentation(methodDeclarationContext.formal_parameter_list(), parser);

                    // Add the name of the method to the buffer
                    signature +=
                        methodDeclarationContext
                            .method_member_name().GetText();
                    methodBuffer.Add(new LiveDocumentationBufferText(signature));

                    ProcessMethodParameters(methodBuffer,
                        i.common_member_declaration().method_declaration().formal_parameter_list());
                }
                // If the class member is a constructor
                else if (i.common_member_declaration().constructor_declaration() is
                         { } constructorDeclarationContext)
                {
                    (CSharpCommentsGrammarParser parser, CommonTokenStream tokens) = CreateParserForMethod(methodUpperLineBound, i.Start.Line);
                    LiveDocumentationBuffer methodDoc = new LiveDocumentationBuffer();
                    if (tokens.GetTokens().Count > 1)
                    {
                        ProcessSummary(methodDoc, parser.docs().summary());
                        parser.Reset();
                    }

                    methodBuffer.Documentation = methodDoc;

                    parser.Reset();
                    methodBuffer.Parameters =
                        ProcessParametersDocumentation(constructorDeclarationContext.formal_parameter_list(), parser);

                    signature += constructorDeclarationContext.identifier().GetText();
                    methodBuffer.Add(new LiveDocumentationBufferText(signature));

                    ProcessMethodParameters(methodBuffer,
                        i.common_member_declaration().constructor_declaration().formal_parameter_list());
                }
                else if (i.common_member_declaration().typed_member_declaration() is
                         { } typedMemberDeclaration)
                {
                    // Skip C# Properties for now
                    // TODO Maybe implement them later
                    if (typedMemberDeclaration.method_declaration() == null)
                    {
                        methodUpperLineBound = i.Stop.Line;
                        continue;
                    }

                    (CSharpCommentsGrammarParser parser, CommonTokenStream tokens) = CreateParserForMethod(methodUpperLineBound, i.Start.Line);
                    LiveDocumentationBuffer methodDoc = new LiveDocumentationBuffer();
                    if (tokens.GetTokens().Count > 1)
                    {
                        ProcessSummary(methodDoc, parser.docs().summary());
                        parser.Reset();
                    }

                    methodBuffer.Documentation = methodDoc;

                    parser.Reset();
                    methodBuffer.Parameters =
                        ProcessParametersDocumentation(
                            typedMemberDeclaration.method_declaration().formal_parameter_list(), parser);
                    string methodType = typedMemberDeclaration.type_().GetText();
                    methodBuffer.Add(new LiveDocumentationBufferText(signature));
                    methodBuffer.Add(new LiveDocumentationLink(methodType, methodType));
                    methodBuffer.Add(new LiveDocumentationBufferText(" "));
                    methodBuffer.Add(new LiveDocumentationBufferText(typedMemberDeclaration.method_declaration()
                        .method_member_name().GetText()));


                    ProcessMethodParameters(methodBuffer,
                        typedMemberDeclaration.method_declaration().formal_parameter_list());
                }
                else
                {
                    methodUpperLineBound = i.Stop.Line;
                    continue;
                }


                methodUpperLineBound = i.Stop.Line;
                buffers.Add(methodBuffer);
            }


            return buffers;
        }


        /// <summary>
        ///     Extracts all using directives from a given C# file specified in <paramref name="fileName" />.
        ///     It will accumulate all imported type names in an list and return it.
        ///     If the file doesn't have any using directives an empty list is returned.
        /// </summary>
        /// <param name="fileName">The name of the file in which the using directives should be extracted</param>
        /// <returns>A <see cref="List" /> of imported type names</returns>
        public List<string> ExtractImportedNamespaces()
        {
            List<string> ret = new();
            parser.Reset();
            // If the file doesn't have any using directives an empty list is returned.
            if (parser.compilation_unit().using_directives() is { } usingDirectivesContext)
            {
                foreach (CSharpParser.Using_directiveContext usingDirective in usingDirectivesContext.using_directive())
                {
                    if (usingDirective is CSharpParser.UsingNamespaceDirectiveContext namespaceDirectiveContext)
                    {
                        ret.Add(namespaceDirectiveContext.namespace_or_type_name().GetText());
                    }
                }
            }

            return ret;
        }


        /// <summary>
        ///     Uses the <see cref="CSharpParser" /> to parse a source file to find a speciffic class.
        /// </summary>
        /// <param name="parser">The parses which should be used for that</param>
        /// <param name="className">The name of the class which should be found</param>
        /// <returns>
        ///     The declaration_context of the class and the upper bound of line numbers. If the class cant by found under that
        ///     name null is returned.
        ///     (Context, LineNumberBound)
        /// </returns>
        private (CSharpParser.Type_declarationContext, int) GetClassByName(string className)
        {
            parser.Reset();
            CSharpParser.Namespace_member_declarationContext[] namespaces = parser
                .compilation_unit()
                .namespace_member_declarations()
                .namespace_member_declaration();
            foreach (CSharpParser.Namespace_member_declarationContext namspace in namespaces)
            {
                int namespaceDefLine = namspace.namespace_declaration().Start.Line;
                List<CSharpParser.Namespace_member_declarationContext> types = namspace
                    .namespace_declaration()
                    .namespace_body()
                    .namespace_member_declarations()
                    .namespace_member_declaration()
                    .ToList();
                foreach (CSharpParser.Namespace_member_declarationContext type in types)
                    // Extract the name of the class or struct and compare it with the passed className
                {
                    if ((type.type_declaration().class_definition() is { } c &&
                         c.identifier().GetText().Equals(className)) ||
                        (type.type_declaration().struct_definition() is { } s &&
                         s.identifier().GetText().Equals(className)))
                    {
                        int classIndex = types.IndexOf(type);
                        int classDocMinStartLine = namespaceDefLine;
                        // Get The end line of the last class
                        if (classIndex > 0)
                        {
                            classDocMinStartLine = types[classIndex - 1].Stop.Line;
                        }

                        return (type.type_declaration(), classDocMinStartLine);
                    }
                }
            }

            return (null, -1);
        }

        /// <summary>
        ///     Processes the parameters of a method signature
        /// </summary>
        /// <param name="buff">
        ///     The methods signature (<see cref="LiveDocumentationBuffer" />) where the parameters should be
        ///     appended to
        /// </param>
        /// <param name="parameterList">The parameter list of the method</param>
        private void ProcessMethodParameters(LiveDocumentationBuffer buff,
            [CanBeNull] CSharpParser.Formal_parameter_listContext parameterList)
        {
            buff.Add(new LiveDocumentationBufferText("("));
            if (parameterList != null)
            {
                CSharpParser.Fixed_parameterContext[] parameters = parameterList
                    .fixed_parameters()
                    .fixed_parameter();
                foreach (CSharpParser.Fixed_parameterContext parameter in parameters)
                {
                    string pType = parameter.arg_declaration().type_().GetText() + " ";
                    buff.Add(new LiveDocumentationLink(pType, pType));
                    buff.Add(
                        new LiveDocumentationBufferText(parameter.arg_declaration().identifier()
                            .GetText()));
                    // If this wasn't the last parameter append a comma
                    if (!parameter.Equals(parameters.Last()))
                    {
                        buff.Add(new LiveDocumentationBufferText(", "));
                    }
                }
            }

            buff.Add(new LiveDocumentationBufferText(")"));
        }

        /// <summary>
        ///     Processes the summary tag of csharp documentation and appending the content to <paramref name="buffer" />
        /// </summary>
        /// <param name="buffer">The buffer where the summary tag should be appended to</param>
        /// <param name="summaryContext">The summary which should be parsed</param>
        private void ProcessSummary(LiveDocumentationBuffer buffer,
            [CanBeNull] CSharpCommentsGrammarParser.SummaryContext summaryContext)
        {
            if (summaryContext != null)
            {
                foreach (CSharpCommentsGrammarParser.CommentsContext commentLine in summaryContext.comments())
                {
                    ProcessComment(buffer, commentLine);

                    buffer.Add(new LiveDocumentationBufferText("\n"));
                }
            }
        }

        /// <summary>
        ///     Processes a single comment line and appends the contents to <paramref name="buffer" />.
        /// </summary>
        /// <param name="buffer">The buffer the comment line should be appended to.</param>
        /// <param name="commentsContext">The comment line which should be parsed</param>
        private void ProcessComment(LiveDocumentationBuffer buffer,
            CSharpCommentsGrammarParser.CommentsContext commentsContext)
        {
            foreach (IParseTree i in commentsContext.children)
            {
                if (i is CSharpCommentsGrammarParser.SomeTextContext someTextContext)
                {
                    buffer.Add(new LiveDocumentationBufferText(someTextContext.TEXT()
                        .Aggregate("", (text, word) => text += word.GetText() + " ").TrimEnd()));
                }
                else if (i is CSharpCommentsGrammarParser.ClassLinkContext classLinkContext)
                {
                    buffer.Add(new LiveDocumentationLink(classLinkContext.linkID.Text,
                        classLinkContext.linkID.Text));
                }
            }
        }

        /// <summary>
        ///     Process the contents of a XML tag and appends the contents to <paramref name="buffer" />
        /// </summary>
        /// <param name="buffer">The buffer where the documentation should be appended</param>
        /// <param name="tagContentContext">The XML tag which should be parsed</param>
        private void ProcessTagContent(LiveDocumentationBuffer buffer,
            CSharpCommentsGrammarParser.TagContentContext tagContentContext)
        {
            foreach (IParseTree i in tagContentContext.children)
            {
                if (i is CSharpCommentsGrammarParser.SomeTextContext someTextContext)
                {
                    buffer.Add(new LiveDocumentationBufferText(someTextContext.TEXT()
                        .Aggregate("", (text, word) => text += word.GetText() + " ").TrimEnd()));
                }
                else if (i is CSharpCommentsGrammarParser.CommentsContext commentsContext)
                {
                    ProcessComment(buffer, commentsContext);
                }
            }
        }

        /// <summary>
        ///     Process the documentation of a methods parameters
        /// </summary>
        /// <param name="parameterListContext">The parameters of the method which should be parsed</param>
        /// <param name="parser">The parser which should be used for parsing</param>
        /// <returns>The documentation of all parameters as a list</returns>
        private List<LiveDocumentationBuffer> ProcessParametersDocumentation(
            [CanBeNull] CSharpParser.Formal_parameter_listContext parameterListContext,
            CSharpCommentsGrammarParser parser)
        {
            List<LiveDocumentationBuffer> bufferList = new List<LiveDocumentationBuffer>();

            if (parameterListContext != null)
            {
                foreach (CSharpParser.Fixed_parameterContext parameter in parameterListContext.fixed_parameters().fixed_parameter())
                {
                    parser.Reset();
                    LiveDocumentationBuffer buffer = new LiveDocumentationBuffer();
                    string parameterName = parameter.arg_declaration().identifier().GetText();
                    string parameterType = parameter.arg_declaration().type_().GetText();
                    buffer.Add(new LiveDocumentationLink(parameterType, parameterType));
                    buffer.Add(new LiveDocumentationBufferText(" " + parameterName));
                    buffer.Add(new LiveDocumentationBufferText(" "));

                    CSharpCommentsGrammarParser.ParameterContext matchingParamDoc = parser.docs().parameters()?.parameter()
                        .FirstOrDefault(x => x.paramName.Text == parameterName);
                    if (matchingParamDoc != null)
                    {
                        ProcessTagContent(buffer, matchingParamDoc.parameterDescription);
                    }

                    bufferList.Add(buffer);
                }
            }

            return bufferList;
        }

        /// <summary>
        ///     Creates a documentation parser for a method.
        ///     This method will search between the lines <paramref name="methodUpperLineBound" /> and
        ///     <paramref name="methodLowerLineBound" /> in the source code file for any CSharp documentation and will create a new
        ///     parser with the contents.
        /// </summary>
        /// <param name="methodUpperLineBound">The upper most position where the CSharp doc can be</param>
        /// <param name="methodLowerLineBound">The lower most position where the CSharp doc can be</param>
        /// <returns>The new created parser and a token stream as a tuple</returns>
        private (CSharpCommentsGrammarParser, CommonTokenStream) CreateParserForMethod(int methodUpperLineBound,
            int methodLowerLineBound)
        {
            string commentTokens = string.Join(Environment.NewLine, this.commentTokens.GetTokens()
                .Where(x => x.Type == 2 && x.Line < methodLowerLineBound && x.Line > methodUpperLineBound)
                .Select(x => x.Text).ToList());

            CSharpCommentsGrammarLexer lexer = new CSharpCommentsGrammarLexer(new AntlrInputStream(commentTokens));
            CommonTokenStream tokens = new CommonTokenStream(lexer);
            tokens.Fill();
            return (new CSharpCommentsGrammarParser(tokens), tokens);
        }
    }
}
