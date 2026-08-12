using System.Security.AccessControl;
using System.Xml;
using System.Collections.Generic;
using System.Linq;
using System;
using SEE.DataModel.DG;
using UnityEngine;

namespace Cypher
{
    /// <summary>
    /// WHERE Condition
    /// Is a nested tree. Example:
    ///       AND
    ///     /     \
    ///    >       ==
    ///   / \     /  \
    /// age 30  Name John
    /// </summary>
    public class Condition
    {
        /// <summary>
        /// Tree Nodes compares Left and Right Child, depending of the operator.
        /// </summary>
        // Tree Node
        public string? Operator { get; set; }
        public Condition? Left {  get; set; }
        public Condition? Right {  get; set; }

        /// <summary>
        /// Tree Leafes only exist to transform into a Value based on their Variable and Property in GetValue().
        /// </summary>
        // Tree Leaf
        public string? Variable { get; set; }
        public string? Property { get; set; }

        /// <summary>
        /// Values are Leafes in the Tree and are compared to each other based on their parent operator.
        /// </summary>
        // Value
        public object? Value { get; set; }

        // Constructors
        // Tree Node
        public Condition(Condition l, Condition r, string op)
        {
            Left = l;
            Right = r;
            Operator = op;
        }
        // Tree Leaf
        public Condition(string v, string property)
        {
            Variable = v;
            Property = property;
        }
        // Value
        public Condition(object value)
        {
            Value = value;
        }

        /// <summary>
        /// Function that applies the WHERE Condition on a specific MatchResult.
        /// Recursive Function for logical operators
        /// Uses GetValue for comparative operators
        /// </summary>
        /// <param name="c">Recursive Condition</param>
        /// <param name="match">Tree Leafs search in match for a Value</param>
        /// <returns>True if the MatchResult fulfills the WHERE Condition</returns>
        /// <exception cref="Exception"></exception>
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
        /// <summary>
        /// Used to turn the leafes of the nested tree into comparable values
        /// </summary>
        /// <param name="c">Recursive Condition</param>
        /// <param name="match">Tree Leafs search in match for a Value</param>
        /// <returns>Usable Value for CheckCondition</returns>
        /// <exception cref="Exception"></exception>
        private object? GetValue(Condition c, MatchResult match)
        {
            // Value Leaf
            if (c.Value != null)
            {
                return c.Value;
            }
            // Tree Leaf
            if (c.Variable != null)
            {
                // if there is no variable, then null
                if (!match.Variables.TryGetValue(c.Variable, out GraphElement? element))
                {return null;}
                // if found GraphElement is null, then return null
                if (element is null)
                {return null;}
                // if there is only a variable, then return the element. Needed for example: WHERE n = m
                if (c.Property == null)
                {return element;}
                // if the property is not found in the GraphElement, then return null
                if (!element.TryGetAny(c.Property, out object? propertyValue))
                {return null;}
                return propertyValue;
            }
            throw new Exception("Faulty WHERE Condition");
        }

    /*



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
