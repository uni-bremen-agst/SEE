using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using JetBrains.Annotations;
using SEE.Game.UI.LiveDocumantation;

namespace SEE.Utils.LiveDocumentation
{
    public class CSharpExtractor : Extractor
    {
        /// <summary>
        /// Uses the <see cref="CSharpParser"/> to parse a source file to find a speciffic class.
        /// </summary>
        /// <param name="parser">The parses which should be used for that</param>
        /// <param name="className">The name of the class which should be found</param>
        /// <returns>The declaration_context of the class and the upper bound of line numbers. If the class cant by found under that name null is returned.
        /// (Context, LineNumberBound)
        /// </returns>
        [CanBeNull]
        private static (CSharpParser.Type_declarationContext, int) GetClassByName(CSharpParser parser, string className)
        {
            var namespaces = parser
                .compilation_unit()
                .namespace_member_declarations()
                .namespace_member_declaration();
            foreach (var namspace in namespaces)
            {
                var namespaceDefLine = namspace.namespace_declaration().Start.Line;
                var types = namspace
                    .namespace_declaration()
                    .namespace_body()
                    .namespace_member_declarations()
                    .namespace_member_declaration()
                    .ToList();
                foreach (var type in types)
                {
                    // Extract the name of the class or struct and compare it with the passed className
                    if ((type.type_declaration().class_definition() is { } c &&
                         c.identifier().GetText().Equals(className)) ||
                        (type.type_declaration().struct_definition() is { } s &&
                         s.identifier().GetText().Equals(className)))
                    {
                        var classIndex = types.IndexOf(type);
                        var classDocMinStartLine = namespaceDefLine;
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

        public LiveDocumentationBuffer ExtractComments(string fileName, string className)
        {
            LiveDocumentationBuffer buffer = new();
            var input = File.ReadAllText(fileName);
            // Lexer for getting the comments
            var lexer1 = new CSharpFullLexer(new AntlrInputStream(input));
            // Lexer for parsing of the class stucture
            var lexer2 = new CSharpFullLexer(new AntlrInputStream(input));

            var cstoken = new CommonTokenStream(lexer2);
            cstoken.Fill();

            var docTokens = new CommonTokenStream(lexer1, 2);
            docTokens.Fill();


            var csFileParser = new CSharpParser(cstoken);


            var (classContext, upperLineBound) = GetClassByName(csFileParser, className);

            if (classContext != null)
            {
                // Combining the Type 2 Tokens (C# Class Documentation) between the start of the class and the
                // start of the namespace, if the class is the first in the file, or between the start of the class and the
                // end of the last type declaration
                var commentTokens = String.Join(Environment.NewLine, docTokens.GetTokens()
                    .Where(x => x.Type == 2 && x.Line < classContext.Start.Line && x.Line > upperLineBound)
                    .Select(x => x.Text).ToList());

                // Parsing of the class documentation
                var lexer = new CSharpCommentsGrammarLexer(new AntlrInputStream(commentTokens));
                var tokens = new CommonTokenStream(lexer);
                tokens.Fill();
                if (tokens.GetTokens().Count > 1)
                {
                    var parser = new CSharpCommentsGrammarParser(tokens);
                    // var t = parser.start().children;
                    var classComments = parser.docs().summary().comments().comment();
                    // Parse C# Doc line by line and write it in the Buffer
                    foreach (var commentLine in classComments)
                    {
                        if (commentLine is CSharpCommentsGrammarParser.CommentContext comment)
                        {
                            foreach (var j in comment.children)
                            {
                                if (j is ITerminalNode)
                                {
                                    var text = j.GetText();
                                    // if necessary remove the three slashes at the beginning of the comment.  
                                    var commentString = text.StartsWith("///") ? text[3..].Trim() : text;
                                    buffer.Add(new LiveDocumentationBufferText(commentString.Substring(3).Trim()));
                                }
                                else if (j is CSharpCommentsGrammarParser.ClassLinkContext classLink)
                                {
                                    buffer.Add(new LiveDocumentationLink(classLink.linkID.Text,
                                        classLink.linkID.Text));
                                }
                            }
                        }

                        buffer.Add(new LiveDocumentationBufferText("\n"));
                    }
                }
            }

            return buffer;
        }

        public List<LiveDocumentationBuffer> ExtractMethods(string fileName, string className)
        {
            List<LiveDocumentationBuffer> buffers = new();
            var input = File.ReadAllText(fileName);
            var lexer = new CSharpFullLexer(new AntlrInputStream(input));
            var tokens = new CommonTokenStream(lexer);
            tokens.Fill();
            //var parser = new CSharpCommentsGrammarParser(tokens);


            var parser = new CSharpParser(tokens);
            // Parse all namespaces and iterate over them
            var namespaces = parser
                .compilation_unit()
                .namespace_member_declarations()
                .namespace_member_declaration();
            foreach (var namspace in namespaces)
            {
                // Parse all classes in the namespace and iterate over them in order to find the class with the className.
                var classes = namspace
                    .namespace_declaration()
                    .namespace_body()
                    .namespace_member_declarations()
                    .namespace_member_declaration()
                    .ToList();
                foreach (var clss in classes)
                {
                    // Check the class name
                    if (clss.type_declaration()
                        .class_definition()
                        .identifier()
                        .GetText()
                        .Equals(className))
                    {
                        var classBody = clss
                            .type_declaration()
                            .class_definition()
                            .class_body();
                        var methods = classBody
                            .class_member_declarations()
                            .class_member_declaration()
                            .Where(x => x.common_member_declaration().method_declaration() != null).ToList();
                        foreach (var i in methods)
                        {
                            i.GetText();
                            LiveDocumentationClassMemberBuffer methodBuffer = new LiveDocumentationClassMemberBuffer();

                   
                            methodBuffer.LineNumber = i.common_member_declaration().method_declaration()
                                .method_member_name().Start.Line;
                            
                            var signature = i.all_member_modifiers().GetText() + " " +
                                            i.common_member_declaration().method_declaration()
                                                .method_member_name().GetText();
                            signature += "(";
                            methodBuffer.Add(new LiveDocumentationBufferText(signature));
                            if (i.common_member_declaration()
                                    .method_declaration()
                                    .formal_parameter_list() != null)
                            {
                                var parameters = i.common_member_declaration()
                                    .method_declaration()
                                    .formal_parameter_list()
                                    .fixed_parameters()
                                    .fixed_parameter();
                                foreach (var parameter in parameters)
                                {
                                    var pType = parameter.arg_declaration().type_().GetText() + " ";
                                    methodBuffer.Add(new LiveDocumentationLink(pType, pType));
                                    //signature = signature + parameter.arg_declaration().type_().GetText() + " ";
                                    //  signature = signature + parameter.arg_declaration().identifier().GetText();
                                    methodBuffer.Add(
                                        new LiveDocumentationBufferText(parameter.arg_declaration().identifier()
                                            .GetText()));
                                    if (!parameter.Equals(parameters.Last()))
                                    {
                                        methodBuffer.Add(new LiveDocumentationBufferText(", "));
                                    }
                                }


                                //   signature = signature + "(" + i.common_member_declaration().method_declaration()
                                //     .formal_parameter_list().GetText() + ")";
                            }

                            methodBuffer.Add(new LiveDocumentationBufferText(")"));


                            //  methodBuffer.Add(new LiveDocumentationBufferText(signature));
                            //signature.children.Select((x) => x.GetText())
                            //    .Select(x => new LiveDocumentationBufferText(x + " "))
                            //  .ToList().ForEach(x => methodBuffer.Add(x));
                            buffers.Add(methodBuffer);
                        }
                    }
                }

                // var methods = parser.start().namespaceDeclaration(0).classDefinition(0).classContent().methodDeclaration();
                // foreach (var i in methods)
                //  {
                //      LiveDocumentationBuffer methodBuffer = new LiveDocumentationBuffer();
                //      var signature = i.methodSignature();

                // methodBuffer.Add(new LiveDocumentationBufferText(signature.accesModifier.Text+ " ")); 
                //         signature.children.Select((x) => x.GetText()).Select(x => new LiveDocumentationBufferText(x + " "))
                //            .ToList().ForEach(x => methodBuffer.Add(x));
                //        buffers.Add(methodBuffer);
            }

            return buffers;
        }

        public List<string> ExtractImportedNamespaces(string fileName)
        {
            throw new NotImplementedException();
        }
    }
}