using System.IO;
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
            foreach (var i in tokens.GetTokens())
            {
                
            }
            var parser = new CSharpCommentsGrammarParser(tokens);
        }
    }
}