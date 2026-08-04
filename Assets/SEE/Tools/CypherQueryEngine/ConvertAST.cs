using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Cypher
{
    public static class ConvertAST
    {
        public static PatternNode ConvertNode(Cypher.NodeASTNode match)
        {
            string? v = match.Variable;
            string? l = match.Label;
            return new PatternNode(v, l);
        }
        public static PatternEdge ConvertEdge(Cypher.EdgeASTNode match)
        {
            string? v = match.Variable;
            string? l = match.Label;
            return new PatternEdge(v, l);
        }
        public static Pattern ConvertPattern(Cypher.MatchASTNode match)
        {
            // Console.WriteLine($"Pattern count = {match.PatternList.Count}"); // for tests
            if (match.PatternList.Count != 1)
            {
                throw new NotSupportedException(
                    "Only one MATCH pattern is currently supported.");
            }
            Cypher.PatternASTNode p = match.PatternList[0];

            PatternNode? start = null;
            PatternNode? goal = null;
            PatternEdge? relation = null;

            /*
            // Test
            Console.WriteLine($"Pattern elements: {p.Pattern.Count}");
            foreach (var element in p.Pattern)
            {
                Console.WriteLine(element.GetType().Name);
            }
            //
            */

            // The Parser returns the nodes in this order:
            // Startnode
            // Goalnode
            // Edgenode
            foreach (Cypher.AttributableASTNode node in p.Pattern)
            {
                switch (node)
                {
                    case Cypher.NodeASTNode n when start == null:
                        start = ConvertNode(n);
                        break;

                    case Cypher.NodeASTNode n when goal == null:
                        goal = ConvertNode(n);
                        break;

                    case Cypher.EdgeASTNode e when relation == null:
                        relation = ConvertEdge(e);
                        break;


                }
            } // TODO check if more than three patternsNodes are in MatchNode
            if (start == null)
            {
                throw new NotSupportedException(
                    "Missing Start Node");
            }

            /*
            // Test
            Console.WriteLine(start.Type);
            Console.WriteLine(goal.Type);
            Console.WriteLine(relation.Type);
            //
            */

            return new Pattern(start, relation, goal);
        }

        public static Condition ConvertCondition(Cypher.ExpressionASTNode expr)
        {
            /*
            // Test
            Console.WriteLine("-----");
            Console.WriteLine($"Operator: {expr.Operator}");
            Console.WriteLine($"Value:    {expr.Value}");
            Console.WriteLine($"Type:     {expr.Type}");

            Console.WriteLine($"Left : {(expr.leftNode == null ? "null" : expr.leftNode.Operator + " / " + expr.leftNode.Value)}");
            Console.WriteLine($"Right: {(expr.rightNode == null ? "null" : expr.rightNode.Operator + " / " + expr.rightNode.Value)}");
            //
            */

            // Vergleich oder logischer Operator
            if (expr.Operator != null)
            {
                switch (expr.Operator.ToUpper())
                {
                    case "AND":
                    case "OR":
                    case "NOT": // anders machen
                    case "=":
                    case "<":
                    case ">":
                            string op = expr.Operator;
                            Condition l = ConvertCondition(expr.leftNode!);
                            Condition r = ConvertCondition(expr.rightNode!);
                            return new Condition(l, r, op);

                    case "PROPERTYACCESS":

                        /*
                        // Test
                        Console.WriteLine($"PROPERTYACCESS");
                        Console.WriteLine($"left.Type  = {expr.leftNode?.Type}");
                        Console.WriteLine($"left.Value = '{expr.leftNode?.Value}'");
                        Console.WriteLine($"property   = '{expr.Value}'");
                        //
                        */

                        if (expr.leftNode == null || expr.leftNode.Type != "Variable")
                        {
                            throw new InvalidOperationException(
                                "Property access must have a variable on the left side.");
                        }
                        string variable = expr.leftNode.Value;
                        string property = expr.Value;
                        return new Condition(variable, property);
                }
            }
            // Wert
            object v = expr.Value;
            return new Condition(v);
        }

        public static ReturnRequest ConvertReturn(Cypher.ReturnASTNode ret)
        {
            List<ReturnColumn> columns = new();
            foreach (Cypher.ReturnItemASTNode item in ret.ReturnItems)
            {
                columns.Add(ConvertReturnColumn(item));
            }
            return new ReturnRequest(columns);
        }
        public static ReturnColumn ConvertReturnColumn(Cypher.ReturnItemASTNode item)
        {
            Cypher.ExpressionASTNode expr = item.Expression;
            if (expr.Operator == null)
            {
                // RETURN variable
                if (expr.Type == "Variable")
                {
                    return new ReturnColumn(expr.Value, null);
                }
                throw new NotSupportedException(
                    $"RETURN expression type '{expr.Type}' is not supported.");
            }
            switch (expr.Operator!.ToUpper())
            {
                case "PROPERTYACCESS":
                {
                    if (expr.leftNode == null ||
                        expr.leftNode.Type != "Variable")
                    {
                        throw new InvalidOperationException(
                            "RETURN property access requires a variable.");
                    }

                    return new ReturnColumn(expr.leftNode.Value, expr.Value);
                }

                default:
                {
                    throw new NotSupportedException(
                        $"RETURN expression '{expr.Operator}' is not supported.");
                }
            }
        }
    }
}
