using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using System;
using System.Diagnostics;
using static Antlr4.Runtime.Atn.SemanticContext;

namespace Cypher
{
    public class ParseTree
    {
        public string CurrentQuery;

        public ICharStream input;

        public CypherLexer lexer;
        public CommonTokenStream tokens;
        public CypherParser parser;
        public CypherVisitor visitor;
        public IParseTree tree;

        public ParseTree(string query)
        {
            CurrentQuery = "MATCH(n) " +
                            "WHERE n.`METRIC` IS NOT NULL " +
                            "AND n.`Source.Name` = 'PLACE' " +
                            "RETURN n.`METRIC` AS Metric, " +
                            "       n.`Source.Name` AS Source_Name, " +
                            "       n.id AS id " +
                            "ORDER BY Metric DESC " +
                            "LIMIT 1";

            if (!string.IsNullOrEmpty(query))
            {
                CurrentQuery = query;
            }

            input = CharStreams.fromString(CurrentQuery);

            lexer = new CypherLexer(input);
            tokens = new CommonTokenStream(lexer);
            parser = new CypherParser(tokens);


            tree = parser.statements();

            if (tree == null)
            {
                throw new ArgumentNullException("Parsed tree was null!");
            }

            CypherVisitor visitor = new CypherVisitor();

        }

        public ASTRoot GetTypedTree()
        {
            return (ASTRoot)visitor.Visit(tree);
        }

    }
}
