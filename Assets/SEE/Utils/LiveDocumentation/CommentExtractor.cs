using System.IO;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Newtonsoft.Json;
using SEE.Game.UI.LiveDocumantation;

namespace SEE.Utils
{
    public class CommentExtractor
    {
        public static void writeInBuffer(LiveDocumentationBuffer buffer, string fileName)
        {
            var input = File.ReadAllText(fileName);
            var lexer = new CSharpCommentsGrammarLexer(new AntlrInputStream(input));
            var tokens = new CommonTokenStream(lexer);
            tokens.Fill();
         

            var parser = new CSharpCommentsGrammarParser(tokens);
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
        }
    }
}