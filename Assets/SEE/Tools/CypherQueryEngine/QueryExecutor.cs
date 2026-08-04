using System.Collections.Generic;
using System.Linq;
using System;

namespace Cypher
{
    public class QueryExecutor
    {
        // MATCH //////////////////////////////////////////////////////////
        List<MatchResult> FindPattern(Graph graph, Pattern pattern)
        {
            if (pattern.Goal == null)
            {
                return FindNodes(graph, pattern);
            }
            List<MatchResult> found = new();
            foreach (GraphEdge edge in graph.Edges)
            {
                /*
                // TEst
                Console.WriteLine($"From: {edge.From.Type}");
                Console.WriteLine($"Wanted: {pattern.Start.Type}");

                Console.WriteLine($"Graph:   {edge.Relation}");
                Console.WriteLine($"Pattern: {pattern.Relation!.Type}");

                Console.WriteLine($"Target: {edge.To.Type}");
                Console.WriteLine($"Wanted: {pattern.Goal.Type}");
                //
                */

                // Nur gültige Edges zulassen
                if (!string.IsNullOrEmpty(pattern.Start.Type))
                {
                    if (pattern.Start.Type != edge.From.Type)
                    {continue;}}

                if (!string.IsNullOrEmpty(pattern.Relation?.Type))
                {
                    if (pattern.Relation.Type != edge.Relation)
                    {continue;}}

                if (!string.IsNullOrEmpty(pattern.Goal.Type))
                {
                    if (pattern.Goal.Type != edge.To.Type)
                    {continue;}}

                // Variablenzuordnung
                Dictionary<string, GraphElement> variables = new();
                if (!string.IsNullOrEmpty(pattern.Start.Variable))
                {
                    variables.Add(pattern.Start.Variable, edge.From);
                }
                if (!string.IsNullOrEmpty(pattern.Relation?.Variable))
                {
                    variables.Add(pattern.Relation.Variable, edge);
                }
                if (!string.IsNullOrEmpty(pattern.Goal.Variable))
                {
                    variables.Add(pattern.Goal.Variable, edge.To);
                }

                found.Add(new MatchResult(variables, edge));

            }
            return found;
        }

        List<MatchResult> FindNodes(Graph graph, Pattern pattern)
        {
            List<MatchResult> found = new();
            foreach (GraphNode node in graph.Nodes)
            {
                if (!string.IsNullOrEmpty(pattern.Start.Type))
                {
                    if (pattern.Start.Type != node.Type)
                    {continue;}
                }

                Dictionary<string, GraphElement> variables = new();
                if (!string.IsNullOrEmpty(pattern.Start.Variable))
                {
                    variables.Add(pattern.Start.Variable, node);
                }

                found.Add(new MatchResult(variables, null, node));
            }
            return found;
        }
        // WHERE ///////////////////////////////////////////////////////
        List<MatchResult> FilterPatternsWhereCondition(List<MatchResult> found, Condition c)
        {
            List<MatchResult> filtered = new();
            foreach (Cypher.MatchResult match in found)
            {
                if (c.CheckCondition(c, match))
                {
                    filtered.Add(match);
                }
            }
            return filtered;
        }
        // RETURN ///////////////////////////////////////////////////////
        QueryResult ExecuteReturn(List<MatchResult> matches, ReturnRequest returns)
        {
            // Build Table for n:m
            // Build Columns
            List<string> columns = new();
            foreach (ReturnColumn rs in returns.Requests)
            {
                columns.Add($"{rs.Variable}{rs.Property}");
            }

            // Build Rows
            List<List<object>> rows = new();
            foreach (MatchResult match in matches)
            {
                List<object> row = new();
                foreach (ReturnColumn rs in returns.Requests)
                {
                    object cell = new();
                    if (string.IsNullOrEmpty(rs.Property))
                    {
                        cell = match.Variables[rs.Variable];
                    }
                    else if (!match.Variables[rs.Variable].Properties.ContainsKey(rs.Property))
                    {
                        cell = "null";
                    }
                    else
                    {
                        cell = match.Variables[rs.Variable].Properties[rs.Property];
                    }

                    row.Add(cell);
                }
                rows.Add(row);
            }
            return new QueryResult(columns, rows);
        }

        void HighlightGraphElement(QueryResult qr)
        {
            foreach (List<object> row in qr.Rows)
            {
                foreach (object obj in row)
                {
                    if (obj is GraphElement)
                    {
                        // Highlight Methode
                        // highlight(findUnityObject());
                    }
                }
            }

        }

        void WriteQueryResultTable(QueryResult qr)
        {
            Console.WriteLine("------------------------");
            string chainedString = "|";
            foreach (string s in qr.Columns)
            {
                chainedString += $" {s} |";
            }
            Console.WriteLine(chainedString);
            foreach (List<object> row in qr.Rows)
            {
                chainedString = "|";
                foreach (object obj in row)
                {
                    chainedString += $" {obj.ToString()} |";
                }
                Console.WriteLine(chainedString);
            }
            Console.WriteLine("------------------------");
        }

        // Throw Exceptions ///////////////////////////////////////////////////////
        public void CompareReturnVariableWithPattern(Pattern pattern, ReturnRequest returns)
        {
            foreach (ReturnColumn column in returns.Requests)
            {
                if (column.Variable != pattern.Start.Variable && column.Variable != pattern.Goal?.Variable && column.Variable != pattern.Relation?.Variable)
                {
                    throw new Exception(
                        $"RETURN Variable '{column.Variable}' NOT in MATCH");
                }
            }
        }
        /* veraltet
        public void CompareConditionVariableWithPattern(Pattern pattern, Condition condition)
        {
            if (condition.Variable != pattern.Start.Variable && condition.Variable != pattern.Goal?.Variable && condition.Variable != pattern.Relation?.Variable)
            {
                throw new Exception(
                    $"WHERE Variable '{condition.Variable}' NOT in MATCH");
            }
        }
        */
        public void CompareVariablesInPattern(Pattern pattern)
        {
            // check if a variable in pattern is used more than once
            if ((pattern.Start.Variable == pattern.Goal?.Variable && !string.IsNullOrEmpty(pattern.Goal?.Variable)) ||
                (pattern.Start.Variable == pattern.Relation?.Variable && !string.IsNullOrEmpty(pattern.Relation?.Variable)) ||
                (pattern.Goal?.Variable == pattern.Relation?.Variable && !string.IsNullOrEmpty(pattern.Goal?.Variable)))
            {
                throw new Exception(
                    "MATCH Variable defined more than once");
            }
            // check if patternform is not accepted
            if ((pattern.Goal == null && pattern.Relation != null) ||
                (pattern.Goal != null && pattern.Relation == null))
            {
                throw new Exception(
                    "Not supported MATCH Form");
            }
        }
        /////////////////////////////////////////////////////////////////////////////
        public QueryResult ExecuteQuery(Graph graph, string query)
        {
            //Console.WriteLine(query); // für tests
            Cypher.ParseTree pt = new Cypher.ParseTree(query);
            Cypher.ASTRoot root = pt.GetTypedTree();
            // ASTRoot Evaluation ////////////////////////////////
            Cypher.MatchASTNode parsePattern = root.Match;
            Cypher.ExpressionASTNode? parseCondition = root.Match.Where;
            Cypher.ReturnASTNode parseReturns = root.Return;
            // check if where exists
            Boolean whereExits = parseCondition is not null;
            /*
            // Test
            Console.WriteLine($"parseCondition found: {parseCondition != null}");
            //
            */
            //Console.WriteLine("ASTRoot Converted");
            // Convert from Parser ///////////////////////////////
            Pattern pattern = ConvertAST.ConvertPattern(parsePattern);
            //Console.WriteLine("Pattern Converted");
            Condition? condition = null;
            if (whereExits)
            {
                condition = ConvertAST.ConvertCondition(parseCondition!);
                /*
                // Test
                Console.WriteLine($"Condition.Variable = '{condition.Variable}'");
                Console.WriteLine($"Condition.Property = '{condition.Property}'");
                Console.WriteLine($"Condition.Operator = '{condition.Operator}'");
                //
                */
                //Console.WriteLine("Condition Converted");
            }
            ReturnRequest returns = ConvertAST.ConvertReturn(parseReturns);
            //Console.WriteLine("Parser Converted");
            // Throw Exceptions //////////////////////////////////
            // check if all variables in returns exist in pattern
            CompareReturnVariableWithPattern(pattern, returns);
            /* funktioniert gerade nicht TODO
            // check if the condition variable exist in pattern
            if (whereExits)
            {
                CompareConditionVariableWithPattern(pattern, condition!);
            }
            */
            // check if a variable in pattern is used more than once
            CompareVariablesInPattern(pattern);
            //Console.WriteLine("Exceptions passed");
            ///////////////////////////////////////////////////////
            // MATCH
            List<MatchResult> foundPatterns = FindPattern(graph, pattern);
            //Console.WriteLine("MATCH worked");
            // WHERE
            if (whereExits)
            {
                foundPatterns = FilterPatternsWhereCondition(foundPatterns, condition!);
            }
            //Console.WriteLine("WHERE worked");
            // RETURN
            QueryResult result = ExecuteReturn(foundPatterns, returns);
            //WriteQueryResultTable(result);
            //HighlightGraphElement(result);
            //Console.WriteLine("RETURN worked");
            return result;
        }
    }
}
