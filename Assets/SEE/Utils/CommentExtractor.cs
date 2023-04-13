using System.IO;
using Antlr4.Runtime;
using Newtonsoft.Json;

namespace SEE.Utils
{
    public class CommentExtractor
    {
        public void writeInBuffer()
        {
            //TODO Here you have to integrate your code
            var input = File.ReadAllText("MyCSharpFile.cs");
            var lexer = new CSharpCommentsGrammarLexer(new AntlrInputStream(input));
            var tokens = new CommonTokenStream(lexer);
            var parser = new CSharpCommentsGrammarParser(tokens);
            var commentsVisitor = new CSharpCommentsGrammarBaseVisitor();
            var comments = commentsVisitor.Visit(parser.start());
            File.WriteAllText("MyComments.json", JsonConvert.SerializeObject(comments));
        }
    }
}