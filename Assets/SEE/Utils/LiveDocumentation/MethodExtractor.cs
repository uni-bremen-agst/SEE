using System.Collections.Generic;
using System.IO;
using System.Linq;
using Antlr4.Runtime;
using SEE.Game.UI.LiveDocumantation;

namespace SEE.Utils
{
    public class MethodExtractor
    {
        public static void FillMethods(List<LiveDocumentationBuffer> buffers, string fileName)
        {
            var input = File.ReadAllText(fileName);
            var lexer = new CSharpCommentsGrammarLexer(new AntlrInputStream(input));
            var tokens = new CommonTokenStream(lexer);
            tokens.Fill();
            var parser = new CSharpCommentsGrammarParser(tokens);

            var methods = parser.start().namespaceDeclaration(0).classDefinition(0).classContent().methodDeclaration();
            foreach (var i in methods)
            {
                LiveDocumentationBuffer methodBuffer = new LiveDocumentationBuffer();
                var signature = i.methodSignature();
                
               // methodBuffer.Add(new LiveDocumentationBufferText(signature.accesModifier.Text+ " ")); 
                signature.children.Select((x) => x.GetText()).Select(x => new LiveDocumentationBufferText(x+" "))
                    .ToList().ForEach(x => methodBuffer.Add(x));
                buffers.Add(methodBuffer);
            }
        }
    }
}