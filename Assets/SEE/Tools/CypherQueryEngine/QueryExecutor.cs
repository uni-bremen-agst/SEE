using System.Collections.Generic;
using System.Linq;
using System;
using SEE.DataModel.DG;
using UnityEngine;
using System.Diagnostics;
//using System.Diagnostics;


namespace Cypher
{
    /// <summary>
    /// Main Class for the Engine
    /// Basically we take a List of all Edges or Nodes of a Graph and filter it with MATCH, WHERE and RETURN Functions.
    ///
    /// </summary>
    public class QueryExecutor
    {
        /// <summary>
        /// MATCH Pattern used in ExecuteQuery()
        /// </summary>
        Pattern pattern;

        /// <summary>
        /// WHERE Condition used in ExecuteQuery()
        /// </summary>
        Condition? condition = null;

        /// <summary>
        /// RETURN Requests used in ExecuteQuery()
        /// </summary>
        ReturnRequest returns;

        /// <summary>
        /// Simple check if WHERE Condition is not null
        /// </summary>
        Boolean whereExists = false;


        #region MATCH
        // MATCH //////////////////////////////////////////////////////////
        /// <summary>
        /// Takes either the nodes or edges List from graph.
        /// May filter List according to Type and/or Pattern form.
        /// </summary>
        /// <param name="graph">Graph to search in</param>
        /// <param name="pattern">Uses Pattern to search</param>
        /// <returns>List<MatchResult> to gather found pairings for each node/edge in graph</returns>
        List<MatchResult> FindPattern(Graph graph, Pattern pattern)
        {
            if (pattern.Goal == null)
            {
                return FindNodes(graph, pattern);
            }
            List<MatchResult> found = new();
            foreach (Edge edge in graph.Edges())
            {
                // Nur gültige Edges zulassen
                if (!string.IsNullOrEmpty(pattern.Start.Type))
                {
                    if (pattern.Start.Type != edge.Source.Type)
                    {continue;}}

                if (!string.IsNullOrEmpty(pattern.Relation?.Type))
                {
                    if (pattern.Relation.Type != edge.Type)
                    {continue;}}

                if (!string.IsNullOrEmpty(pattern.Goal.Type))
                {
                    if (pattern.Goal.Type != edge.Target.Type)
                    {continue;}}

                // Variablenzuordnung
                Dictionary<string, GraphElement> variables = new();
                if (!string.IsNullOrEmpty(pattern.Start.Variable))
                {
                    variables.Add(pattern.Start.Variable, edge.Source);
                }
                if (!string.IsNullOrEmpty(pattern.Relation?.Variable))
                {
                    variables.Add(pattern.Relation.Variable, edge);
                }
                if (!string.IsNullOrEmpty(pattern.Goal.Variable))
                {
                    variables.Add(pattern.Goal.Variable, edge.Target);
                }

                found.Add(new MatchResult(variables));

            }
            return found;
        }

        /// <summary>
        /// Extension for FindPattern() // TODO combine them into one
        /// </summary>
        List<MatchResult> FindNodes(Graph graph, Pattern pattern)
        {
            List<MatchResult> found = new();
            foreach (Node node in graph.Nodes())
            {
                if (!string.IsNullOrEmpty(pattern.Start.Type))
                {
                    //Debug.Log($"PatternType: {pattern.Start.Type}");
                    //Debug.Log($"NodeType{node.Type}");
                    if (pattern.Start.Type != node.Type)
                    {continue;}
                }

                Dictionary<string, GraphElement> variables = new();
                if (!string.IsNullOrEmpty(pattern.Start.Variable))
                {
                    variables.Add(pattern.Start.Variable, node);
                }

                found.Add(new MatchResult(variables));
            }
            return found;
        }
        #endregion
        #region WHERE
        // WHERE ///////////////////////////////////////////////////////
        /// <summary>
        /// Filters a list of found MatchResults based on WHERE Conditon
        /// </summary>
        /// <param name="found">Found Matches in graph</param>
        /// <param name="c">Filter Condition</param>
        /// <returns>Filtered List</returns>
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
        #endregion
        #region RETURN
        // RETURN ///////////////////////////////////////////////////////
        /// <summary>
        /// Constructs a table based on parameters.
        /// </summary>
        /// <param name="matches">Determines the Rows of the table</param>
        /// <param name="returns">Determines the Columns of the table</param>
        /// <returns>QueryResult table</returns>
        QueryResult ExecuteReturn(List<MatchResult> matches, ReturnRequest returns)
        {
            // Build Table for n:m
            // Build Columns
            List<string> columns = new();
            foreach (ReturnColumn rs in returns.Requests)
            {
                columns.Add($"{rs.Variable}{rs.Property}");
            }

            // Build Rows // Build for multiple Graphs
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
                    else if (!match.Variables[rs.Variable].TryGetAny(rs.Property, out object propertyValue))
                    {
                        cell = "null";
                    }
                    else
                    {
                        cell = propertyValue;
                    }

                    row.Add(cell);
                }
                rows.Add(row);
            }
            return new QueryResult(columns, rows);
        }
        #endregion
        /*
        void HighlightGraphElements(QueryResult qr)
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
        */
        #region Exceptions
        // Throw Exceptions /////////////////////////////////////////////////////// // TODO need to be integrated into Window
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
        #endregion
        #region Execute
        /////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Contains Engine steps before graph is needed.
        /// InputQuery -> Parser -> ASTTree -> ConvertAST -> ThrowExceptions
        /// </summary>
        /// <param name="query">User Input</param>
        public void PrepareExecute(string query)
        {
            Cypher.ParseTree pt = new Cypher.ParseTree(query);
            Cypher.ASTRoot root = pt.GetTypedTree();
            // ASTRoot Evaluation ////////////////////////////////
            Cypher.MatchASTNode parsePattern = root.Match;
            Cypher.ExpressionASTNode? parseCondition = root.Match.Where;
            Cypher.ReturnASTNode parseReturns = root.Return;
            // check if where exists
            whereExists = parseCondition is not null;
            // Convert from Parser ///////////////////////////////
            pattern = ConvertAST.ConvertPattern(parsePattern);
            if (whereExists)
            {
                condition = ConvertAST.ConvertCondition(parseCondition!);
            }
            returns = ConvertAST.ConvertReturn(parseReturns);
            // Throw Exceptions //////////////////////////////////
            // check if all variables in returns exist in pattern
            CompareReturnVariableWithPattern(pattern, returns);
            /* funktioniert gerade nicht TODO
            // check if the condition variable exist in pattern
            if (whereExists)
            {
                CompareConditionVariableWithPattern(pattern, condition!);
            }
            */
            // check if a variable in pattern is used more than once
            CompareVariablesInPattern(pattern);
        }
        /// <summary>
        /// Gathers QueryResult tables for each Graph in graphs
        /// Applies MATCH, WHERE and RETURN functions.
        /// </summary>
        /// <param name="graphs">Graphs to search in</param>
        /// <returns>QueryResults for each searched graph</returns>
        public List<QueryResult> ExecuteQuery(List<Graph> graphs)
        {
            List<QueryResult> result = new List<QueryResult>();
            QueryResult qr;
            foreach (Graph graph in graphs)
            {
                // MATCH
                List<MatchResult> foundPatterns = FindPattern(graph, pattern);
                // WHERE
                if (whereExists)
                {
                    foundPatterns = FilterPatternsWhereCondition(foundPatterns, condition!);
                }
                // RETURN
                qr = ExecuteReturn(foundPatterns, returns);
                qr.graphName = graph.Name;
                result.Add(qr);
            }
            //WriteQueryResultTable(result); // For Tests
            //HighlightGraphElement(result); // TODO
            return result;
        }
        /// <summary>
        /// Combines PrepareExecute() and ExecuteQuery() in one function.
        /// </summary>
        public List<QueryResult> FullExecute(List<Graph> graphs, string query)
        {
            PrepareExecute(query);
            return ExecuteQuery(graphs);
        }
        #endregion
    }
}
