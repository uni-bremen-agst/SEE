using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Cypher
{
    /// <summary>
    /// Converts the Output of the Parser (ASTTree) into usable input for the Main Engine (QueryExecutor).
    /// </summary>
    public static class ConvertAST
    {
        /// <summary>
        /// Converts from Parser NodeASTNode to PatternNode.
        /// </summary>
        public static PatternNode ConvertNode(Cypher.NodeASTNode node)
        {
            string? v = node.Variable;
            string? l = node.Label;
            if (l is not null) {l = l.TrimStart(':');}
            return new PatternNode(v, l);
        }
        /// <summary>
        /// Converts from Parser EdgeASTNode to PatternEdge.
        /// </summary>
        public static PatternEdge ConvertEdge(Cypher.EdgeASTNode edge)
        {
            string? v = edge.Variable;
            string? l = edge.Label;
            if (l is not null) {l = l.TrimStart(':');}
            return new PatternEdge(v, l);
        }

        /// <summary>
        /// Converts from Parser MatchASTNode to a usable MATCH Pattern.
        /// </summary>
        /// <exception cref="NotSupportedException"></exception>
        public static Pattern ConvertPattern(Cypher.MatchASTNode match)
        {
            if (match.PatternList.Count != 1)
            {
                throw new NotSupportedException(
                    "Only one MATCH pattern is currently supported.");
            }
            Cypher.PatternASTNode p = match.PatternList[0];

            PatternNode? start = null;
            PatternNode? goal = null;
            PatternEdge? relation = null;

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

            return new Pattern(start, relation, goal);
        }

        /// <summary>
        /// Converts from Parser ExpressionASTNode to a usable WHERE Condition.
        /// ExpressionASTNode and Condition are both nested trees.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public static Condition ConvertCondition(Cypher.ExpressionASTNode expr)
        {
            // Vergleich oder logischer Operator
            if (expr.Operator != null)
            {
                switch (expr.Operator.ToUpper())
                {
                    case "AND":
                    case "OR":
                    case "NOT": // anders machen // TODO
                    case "=":
                    case "<":
                    case ">":
                            string op = expr.Operator;
                            Condition l = ConvertCondition(expr.leftNode!);
                            Condition r = ConvertCondition(expr.rightNode!);
                            return new Condition(l, r, op);

                    case "PROPERTYACCESS":
                        if (expr.leftNode == null || expr.leftNode.Type != "Variable")
                        {
                            throw new InvalidOperationException(
                                "Property access must have a variable on the left side.");
                        }
                        string variable = expr.leftNode.Value;
                        string property = expr.Value.TrimStart('.');
                        return new Condition(variable, property);
                }
            }
            // Single Value
            object v = expr.Value;
            return new Condition(v);
        }

        /// <summary>
        /// Converts from Parser ReturnASTNode to a usable RETURN ReturnRequest.
        /// </summary>
        public static ReturnRequest ConvertReturn(Cypher.ReturnASTNode ret)
        {
            List<ReturnColumn> columns = new();
            foreach (Cypher.ReturnItemASTNode item in ret.ReturnItems)
            {
                columns.Add(ConvertReturnColumn(item));
            }
            return new ReturnRequest(columns);
        }
        /// <summary>
        /// For ConvertReturn.
        /// Converts from Parser ReturnASTItemNode to a ReturnColumn.
        /// </summary>
        /// <exception cref="NotSupportedException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
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

                    return new ReturnColumn(expr.leftNode.Value, expr.Value.TrimStart('.'));
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
