using System;
using System.Collections.Generic;
using SEE.DataModel.DG;

namespace Cypher
{
    /// <summary>
    /// The abstract node type every concrete node.
    /// </summary>
    public abstract class ASTNode
    {

    }

    /// <summary>
    /// The root node consisting of a MATCH and MATCH object.
    /// </summary>
    public class ASTRoot : ASTNode
    {
        public MatchASTNode Match;
        public ReturnASTNode Return;
    }

    /// <summary>
    /// MATCH node containing patterns and the WHERE clause.
    /// </summary>
    public class MatchASTNode : ASTNode
    {
        public int NodeCount = 0;
        public int PathCount = 0;
        public Dictionary<string, string> Variables;
        public List<PatternASTNode> PatternList;
        public ExpressionASTNode Where {  get; set; }  

        public MatchASTNode()
        {
            Variables = new Dictionary<string, string>();
            PatternList = new List<PatternASTNode>();
        }

        public void SetPatternList(List<PatternASTNode> list)
        {
            foreach (PatternASTNode pattern in list)
            {
                foreach (var entry in pattern.Variables)
                {
                    if (Variables.TryGetValue(entry.Key, out string existingType) && entry.Value != existingType)
                    {
                        throw new NotSupportedException($"The same variable: {entry.Key} must not be used for nodes and edges simultaneously.");
                    }
                    else
                    {
                        Variables[entry.Key] = entry.Value;
                    }
                }
            }
            foreach (var type in Variables)
            {
                if (type.Value == "Node")
                {
                    NodeCount++;
                }
                else
                {
                    PathCount++;
                }
            }
        }
    }

    /// <summary>
    /// Only a transport node.
    /// </summary>
    public class PatternListASTNode : ASTNode
    {
        public List<PatternASTNode> PatternsList { get; set; }
        public PatternListASTNode()
        {
            PatternsList = new List<PatternASTNode>();
        }
    }

    /// <summary>
    /// Node to store a single pattern within MATCH.
    /// </summary>
    public class PatternASTNode : ASTNode
    {
        public List<AttributableASTNode> Pattern { get; }
        public Dictionary<string, string> Variables;

        public PatternASTNode()
        {
            Pattern = new List<AttributableASTNode>();
            Variables = new Dictionary<string, string>();
        }

        public void AddPattern(AttributableASTNode attObj)
        {
            Pattern.Add(attObj);
            if (attObj.Variable != null)
            {
                string type = attObj is NodeASTNode ? "Node" : "Edge";
                Variables.Add(attObj.Variable, type);
            }

        }
    }

    /// <summary>
    /// The superclass of Edge, Node and Relationship.
    /// </summary>
    public abstract class AttributableASTNode : ASTNode
    {
        public string Variable { get; set; }
        public string Label { get; set; }
        public Dictionary<string, ExpressionASTNode> Properties { get; set; }

        public void SetProperty(string key, ExpressionASTNode value)
        {
            if (Properties == null) Properties = new Dictionary<string, ExpressionASTNode>();
            Properties.Add(key, value);
        }
    }

    /// <summary>
    /// Node type attributable.
    /// </summary>
    public class NodeASTNode : AttributableASTNode
    {
        // intentionally empty
    }

    /// <summary>
    /// Edge type node.
    /// </summary>
    public class EdgeASTNode : AttributableASTNode
    {
        public NodeASTNode From { get; set; }
        public NodeASTNode To { get; set; }
        public bool Undirected = false;
    }

    /// <summary>
    /// Relationship type to construct an edge with knowledge of its context.
    /// </summary>
    public class RelationshipASTNode : EdgeASTNode
    {
        public bool Left { get; set; } = false;
        public bool Right { get; set; } = false;

        public EdgeASTNode GetEdge()
        {
            return new EdgeASTNode()
            {
                From = this.From,
                To = this.To,
                Variable = this.Variable,
                Label = this.Label,
                Properties = this.Properties,
                Undirected= this.Undirected
            };
        }

    }

    /// <summary>
    /// A transaport node to transmit label properties.
    /// </summary>
    public class LabelASTNode : ASTNode
    {
        public string Name;
        public bool NOT;
    }

    /// <summary>
    /// RETURN node to filter the results.
    /// </summary>
    public class ReturnASTNode : ASTNode
    {
        public bool DISTINCT = false;
        public bool ANYTHING = false;
        public List<ReturnItemASTNode> ReturnItems;
        public OrderASTNode Order;
        public int Limit;
    }

    /// <summary>
    /// Only a transport node.
    /// </summary>
    public class ReturnItemsASTNode : ASTNode
    {
        public List<ReturnItemASTNode> ReturnItems;
        public bool ANYTHING = false;

        public ReturnItemsASTNode()
        {
            ReturnItems = new List<ReturnItemASTNode>();
        }
    }

    /// <summary>
    /// Node to store the RETURN items of the RETURN node.
    /// </summary>
    public class ReturnItemASTNode : ASTNode
    {
        public string Alias = null;
        public ExpressionASTNode Expression;

        public ReturnItemASTNode(string alias, ExpressionASTNode exp)
        {
            Alias = alias;
            Expression = exp;
        }
    }

    /// <summary>
    /// Node to store the RETURN order of the RETURN node.
    /// </summary>
    public class OrderASTNode : ASTNode
    {
        public List<(ExpressionASTNode by, string order)> OrderList { get; set; }

        public OrderASTNode()
        {
            OrderList = new List<(ExpressionASTNode by, string order)>();
        }
    }

    /// <summary>
    /// String transport node, to be deleted later.a
    /// </summary>
    public class StringASTNode : ASTNode
    {
        public string Value { get; set; }
        public StringASTNode(string value)
        {
            Value = value;
        }
    }

    /// <summary>
    /// Node to store map entrys.
    /// </summary>
    public class MapASTNode : ASTNode
    {
        public Dictionary<string, ExpressionASTNode> Map { get; set; }

        public MapASTNode() 
        {
            Map = new Dictionary<string, ExpressionASTNode>();
        }
    }

    /// <summary>
    /// Node to store a nested Expression.
    /// </summary>
    public class ExpressionASTNode : ASTNode
    {
        public ExpressionASTNode leftNode {  get; set; }
        public ExpressionASTNode rightNode { get; set; }
        public string Operator { get; set; }
        public string Value { get; set; }
        public string Type { get; set; }
        public GraphElement CurrentGraphElement { get; set; }


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
        }

    }

}