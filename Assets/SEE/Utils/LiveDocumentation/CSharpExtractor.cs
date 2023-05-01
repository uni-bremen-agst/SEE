using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Parser;
using SEE.Game.UI.LiveDocumantation;

namespace SEE.Utils.LiveDocumentation
{
    public class CSharpExtractor : Extractor
    {
        public static void ExtractClassComment(LiveDocumentationBuffer buffer, string fileName, string className)
        {
            var input = File.ReadAllText(fileName);
            var clex = new CSharpFullLexer(new AntlrInputStream(input));
            var clex2 = new CSharpFullLexer(new AntlrInputStream(input));
            var ctoken = new CommonTokenStream(clex2);
            ctoken.Fill();
            var tokenss = new CommonTokenStream(clex, 2);
            tokenss.Fill();
            var cpars = new CSharpParser(ctoken);
            var cpars2 = new CSharpParser(ctoken);


            var namespaces = cpars.compilation_unit().namespace_member_declarations().namespace_member_declaration();

            foreach (var namspace in namespaces)
            {
                var namespaceDefLine = namspace.namespace_declaration().Start.Line;
                var classes = namspace.namespace_declaration().namespace_body().namespace_member_declarations()
                    .namespace_member_declaration().ToList();
                foreach (var clss in classes)
                {
                    if (clss.type_declaration().class_definition().identifier().GetText().Equals(className))
                    {
                        var classIndex = classes.IndexOf(clss);
                        // Define the minimal line number for Documentation code to begin
                        var classDocMinStartLine = namespaceDefLine;
                        // Get The end line of the last class
                        if (classIndex > 0)
                        {
                            classDocMinStartLine = classes[classIndex - 1].Stop.Line;
                        }

                        var classDefLine = clss.type_declaration().Start.Line;

                        var commentTokens = String.Join(Environment.NewLine, tokenss.GetTokens()
                            .Where(x => x.Type == 2 && x.Line < classDefLine && x.Line > classDocMinStartLine)
                            .Select(x => x.Text).ToList());

                        var lexer = new CSharpCommentsGrammarLexer(new AntlrInputStream(commentTokens));
                        var tokens = new CommonTokenStream(lexer);
                        tokens.Fill();

                        var parser = new CSharpCommentsGrammarParser(tokens);
                        // var t = parser.start().children;
                        var classComments = parser.docs().summary().comments().comment();
                        // Write class documentation in Buffer
                        foreach (var i in classComments)
                        {
                            if (i is CSharpCommentsGrammarParser.CommentContext comment)
                            {
                                foreach (var j in comment.children)
                                {
                                    if (j is ITerminalNode)
                                    {
                                        var commentString = j.GetText();
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
            }
        }
        
        

        public static void ExtractMethods(List<LiveDocumentationBuffer> buffers, string fileName, string className)
        {
            var input = File.ReadAllText(fileName);
            var lexer = new CSharpFullLexer(new AntlrInputStream(input));
            var tokens = new CommonTokenStream(lexer);
            tokens.Fill();
            //var parser = new CSharpCommentsGrammarParser(tokens);


            var parser = new CSharpParser(tokens);
            var namespaces = parser.compilation_unit().namespace_member_declarations().namespace_member_declaration();

            foreach (var namspace in namespaces)
            {
                var classes = namspace.namespace_declaration().namespace_body().namespace_member_declarations()
                    .namespace_member_declaration().ToList();
                foreach (var clss in classes)
                {
                    if (clss.type_declaration().class_definition().identifier().GetText().Equals(className))
                    {
                        var classOfMethods = clss.type_declaration().class_definition().class_body();
                        var methods = classOfMethods.class_member_declarations().class_member_declaration()
                            .Where(x => x.common_member_declaration().method_declaration() != null).ToList();
                        foreach (var i in methods)
                        {
                            i.GetText();
                            LiveDocumentationBuffer methodBuffer = new LiveDocumentationBuffer();
                            var signature = i.all_member_modifiers().GetText() +" "+
                                            i.common_member_declaration().method_declaration()
                                                .method_member_name().GetText();
                            signature += "(";
                            methodBuffer.Add(new LiveDocumentationBufferText(signature));
                            if (i.common_member_declaration().method_declaration().formal_parameter_list() != null)
                            {
                                var parameters = i.common_member_declaration().method_declaration()
                                    .formal_parameter_list().fixed_parameters().fixed_parameter();
                                foreach (var parameter in parameters)
                                {
                                    
                                    var pType = parameter.arg_declaration().type_().GetText() +" ";
                                    methodBuffer.Add(new LiveDocumentationLink(pType,pType));
                                    //signature = signature + parameter.arg_declaration().type_().GetText() + " ";
                                  //  signature = signature + parameter.arg_declaration().identifier().GetText();
                                    methodBuffer.Add(new LiveDocumentationBufferText(parameter.arg_declaration().identifier().GetText()));
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
        }

        public LiveDocumentationBuffer ExtractComments(string fileName, string className)
        {
            var input = File.ReadAllText(fileName);
            var lexer = new CSharpCommentsGrammarLexer(new AntlrInputStream(input));
            var tokens = new CommonTokenStream(lexer);
            tokens.Fill();


            var parser = new CSharpCommentsGrammarParser(tokens);

            LiveDocumentationBuffer buffer = new();

            // var t = parser.start().children;
            var classComments = parser.start().namespaceDeclaration(0).classDefinition(0).summary().comments().children;
            // Write class documentation in Buffer
            foreach (var i in classComments)
            {
                if (i is CSharpCommentsGrammarParser.CommentContext comment)
                {
                    foreach (var j in comment.children)
                    {
                        if (j is ITerminalNode)
                        {
                            var commentString = j.GetText();
                            buffer.Add(new LiveDocumentationBufferText(commentString.Substring(3).Trim()));
                        }
                        else if (j is CSharpCommentsGrammarParser.ClassLinkContext classLink)
                        {
                            buffer.Add(new LiveDocumentationLink(classLink.linkID.Text, classLink.linkID.Text));
                        }
                    }
                }

                buffer.Add(new LiveDocumentationBufferText("\n"));
            }

            return buffer;
        }

        public List<LiveDocumentationBuffer> ExtractMethods(string fileName, string className)
        {
            throw new System.NotImplementedException();
        }
    }
}