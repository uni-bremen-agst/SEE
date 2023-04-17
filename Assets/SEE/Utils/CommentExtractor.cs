using System.IO;
using System.Linq;
using Antlr4.Runtime;
using Newtonsoft.Json;
using SEE.Game.UI.LiveDocumantation;

namespace SEE.Utils
{
    public class CommentExtractor
    {
        public static void writeInBuffer(LiveDocumentationBuffer buffer, string fileName)
        {
            //TODO Here you have to integrate your code
            var input = File.ReadAllText(fileName);
            var lexer = new CSharpCommentsGrammarLexer(new AntlrInputStream(input));
            var tokens = new CommonTokenStream(lexer);
            tokens.Fill();
            var comments = tokens.GetTokens().Where(x => x.Type == CSharpCommentsGrammarLexer.Comment);
            foreach (var i in tokens.GetTokens())
            {
            }

            var parser = new CSharpCommentsGrammarParser(tokens);
            // var t = parser.start().children;
            var claasComments = parser.start().claasDefinition(0).summary().comment();
            foreach (var i in claasComments)
            {
                foreach (var j in i.children)
                {
                    buffer.Add(new LiveDocumentationBufferText(j.GetText()));
                }
            }
        }
    }
}