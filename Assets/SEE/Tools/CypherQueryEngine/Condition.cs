using System.Security.AccessControl;
using System.Xml;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Cypher
{
    public class Condition
    {
        /*
        public string Variable { get; set; }
        public string Property { get; set; }
        public string Operator { get; set; }
        public object Value { get; set; }
        */
        /* aus ASTNode.cs ExpressionASTNode
        public ExpressionASTNode leftNode {  get; set; }
        public ExpressionASTNode rightNode { get; set; }
        public string Operator { get; set; }
        public string Value { get; set; }
        public string Type { get; set; }
        public GraphElement CurrentGraphElement { get; set; }
        */

        // Knoten
        public string? Operator { get; set; }
        public Condition? Left {  get; set; }
        public Condition? Right {  get; set; }

        // Blatt
        public string? Variable { get; set; }
        public string? Property { get; set; }

        // Wert
        public object? Value { get; set; }

        // Knoten
        public Condition(Condition l, Condition r, string op)
        {
            Left = l;
            Right = r;
            Operator = op;
        }
        // Blatt
        public Condition(string v, string property)
        {
            Variable = v;
            Property = property;
        }
        // Wert
        public Condition(object value)
        {
            Value = value;
        }

        /*
        public bool CheckCondition(GraphElement element)
        {
            if (!element.Properties.TryGetValue(this.Property, out object? value))
            {
                return false;
            }

            switch(this.Operator)
            {
                case "==":
                    return value.Equals(this.Value);

                case "!=":
                    return !value.Equals(this.Value);

                case ">":
                    return Convert.ToInt32(value) > Convert.ToInt32(this.Value);

                case "<":
                    return Convert.ToInt32(value) < Convert.ToInt32(this.Value);

                case ">=":
                    return Convert.ToInt32(value) >= Convert.ToInt32(this.Value);

                case "<=":
                    return Convert.ToInt32(value) <= Convert.ToInt32(this.Value);

                default:
                    throw new Exception("Unknown operator");
            }

        }*/

        public bool CheckCondition(Condition c, MatchResult match)
        {
            if (c.Operator == null)
            {
                throw new Exception("WHERE is not a BOOLEAN Term");
            }
            object? left;
            object? right;
            switch (c.Operator)
            {
                case "AND":
                    return CheckCondition(c.Left!, match) &&
                           CheckCondition(c.Right!, match);

                case "OR":
                    return CheckCondition(c.Left!, match) ||
                           CheckCondition(c.Right!, match);

                case "NOT":
                    return !CheckCondition(c.Left!, match);

                // TODO fälle für unterschiedliche object von GetValue()
                // z.B. WHERE n > n.Lines
                // es bracht Fehlermeldung
                case "=":
                    left = GetValue(c.Left!, match);
                    right = GetValue(c.Right!, match);
                    if (left == null || right == null) {return false;}

                    return Equals(left, right);

                case ">":
                    left = GetValue(c.Left!, match);
                    right = GetValue(c.Right!, match);
                    if (left == null || right == null) {return false;}

                    return Convert.ToInt32(left)
                         > Convert.ToInt32(right);

                case "<":
                    left = GetValue(c.Left!, match);
                    right = GetValue(c.Right!, match);
                    if (left == null || right == null) {return false;}

                    return Convert.ToInt32(left)
                         < Convert.ToInt32(right);
                case ">=":
                    left = GetValue(c.Left!, match);
                    right = GetValue(c.Right!, match);
                    if (left == null || right == null) {return false;}

                    return Convert.ToInt32(left)
                         >= Convert.ToInt32(right);
                case "<=":
                    left = GetValue(c.Left!, match);
                    right = GetValue(c.Right!, match);
                    if (left == null || right == null) {return false;}

                    return Convert.ToInt32(left)
                         <= Convert.ToInt32(right);

                default:
                    throw new Exception("Not supported Operator in WHERE");
            }
        }
        public object? GetValue(Condition c, MatchResult match)
        {
            // Wert Blatt
            if (c.Value != null)
            {
                return c.Value;
            }
            // Form: v.Property oder v
            if (c.Variable != null)
            {
                // falls variable von c nicht in match gefunden wird
                if (!match.Variables.TryGetValue(c.Variable, out GraphElement? element))
                {
                    return null;
                }
                if (element is null) {return null;}
                // falls es eine einzige variable ist
                if (c.Property == null)
                {
                    return element;
                }
                // falls
                if (!element.Properties.TryGetValue(c.Property, out object? value))
                {
                    return null;
                }
                if (value is null) {return null;}
                return value;
            }
            throw new Exception("Faulty WHERE Condition");
        }

    /*

          AND
        /     \
       >       ==
      / \     /  \
   age 18  type Person

    */
    /*
        public object Evaluate ()
        {
            var l = leftNode.Evaluate();
            var r = rightNode.Evaluate();

            switch (Operator.ToUpper())
            {
                case "OR":                  // Expression (TOP)
                    return (bool)l || (bool)r;
                case "XOR":                 // Expression11
                    return (bool)l ^ (bool)r;
                case "AND":                 // Expression10
                    return (bool)l && (bool)r;
                case "NOT":                 // Expression9
                    return !((bool)l);
                case "==":                  // Expression8 (BEGIN)
                    return l == r;
                case "!=":
                    return l != r;
                case "<>":
                    return l != r;
                case "<=":
                    return (decimal)l <= (decimal)r;
                case ">=":
                    return (decimal)l >= (decimal)r;
                case "<":
                    return (decimal)l < (decimal)r;
                case ">":
                    return (decimal)l > (decimal)r;
                case "IS NULL":             // Expression7
                    return l is null;
                case "IS NOT NULL":
                        return l is null;
                case ":":
                    if (l is Node node1)
                    {
                        return node1.Type == Value;
                    }
                    else if (l is Edge edge1)
                    {
                        return edge1.Type == Value;
                    }
                    throw new NotSupportedException($"Das Element {l.ToString()} konnte nicht auf Typgleichheit geprüft werden.");
                case "IS":
                    if (l is Node node2)
                    {
                        return node2.Type == Value;
                    }
                    else if (l is Edge edge2)
                    {
                        return edge2.Type == Value;
                    }
                    throw new NotSupportedException($"Das Element {l.ToString()} konnte nicht auf Typgleichheit geprüft werden.");
                case "SIGN":                // Expression3 (Exp6-Exp4 are not supported)
                    var neg = -(decimal)l;
                    l = Convert.ChangeType(neg, l.GetType());
                    return l;
                case "PROPERTYACCESS":      // Expression2
                    if (l is Node node3)
                    {
                        if (node3.TryGetNumeric(Value, out float numeric)) return numeric;
                        if (node3.TryGetString(Value, out string property)) return property;
                        return null;
                    }
                    else if (l is Edge edge3)
                    {
                        if (edge3.TryGetNumeric(Value, out float numeric)) return numeric;
                        if (edge3.TryGetString(Value, out string property)) return property;
                        return null;
                    }
                    throw new NotSupportedException($"Das Element {l.ToString()} konnte nicht auf Typgleichheit geprüft werden.");
                case "BOOLEAN":             // Expression1
                    return ((ExpressionASTNode)l).Value.ToUpper() == "TRUE" ? true : false;
                case "KEYWORD":
                    return null;
                case "NUMERIC":
                    var numericValue = ((ExpressionASTNode)l).Value;
                    return numericValue.Contains(".") ? double.Parse(numericValue) : int.Parse(numericValue);
                case "VARIABLE":
                    if (Type == "Node")
                    {
                        return (Node) CurrentGraphElement;
                    }
                    else if (Type == "Edge")
                    {
                        return (Edge) CurrentGraphElement;
                    }
                    return null;
                case "COUNT*":
                    return "COUNT*";
                default:
                    throw new NotSupportedException($"Operator {Operator} nicht unterstützt");
            }
        }*/
    }
}
