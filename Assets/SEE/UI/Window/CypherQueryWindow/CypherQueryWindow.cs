
using Michsky.UI.ModernUIPack;
using SEE.Utils;
using SEE.Controls;
using SEE.DataModel.DG;

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SEE;

using TMPro;
using UnityEngine.UI;

namespace SEE.UI.Window.CypherQueryWindow
{
    /// <summary>
    /// This window will be used to run Cypher-Queries on graphs in the current scene.
    /// </summary>
    public class CypherQueryWindow : BaseWindow
    {
        // Variables //////////////////////////////////////
        /// <summary>
        /// List that stores all QueryResults of ExecuteQuery().
        /// Is needed for the output and its visualisation.
        /// </summary>
        private List<Cypher.QueryResult> allQueryResults;

        /// <summary>
        /// List that stores all graphs in the current scene. // Not currently
        /// ExecuteQuery() runs on all graphs in this List.
        /// </summary>
        private List<Graph> allLoadedGraphs = new List<Graph>(); // TODO just specific graphs per user request?

        // Window Components ///////////////////////////////
        /// <summary>
        /// Project path of the prefab.
        /// </summary>
        private const string cypherQueryWindowPrefab = "Prefabs/UI/CypherQueryWindow/CypherQueryWindow";

        /// <summary>
        /// User Input for the Cypher-Query.
        /// Currently only MATCH, WHERE and RETURN supported.
        /// </summary>
        private TMP_InputField queryInput;

        /// <summary>
        /// Output of the Cypher-Query in form of a table.
        /// </summary>
        private TMP_Text outputText;

        /// <summary>
        /// Button starts ExecuteQuery().
        /// </summary>
        private Button executeButton;

        /// <summary>
        /// Button clears the InputField and Output.
        /// Also dehighlights all highlighted nodes and edges from ExecuteQuery().
        /// </summary>
        private Button clearButton;

        /// <summary>
        /// Is called when the component is mounted and initializes it.
        /// </summary>
        protected override void Start()
        {
            Title = "Cypher Query";
            base.Start();
        }

        /// <summary>
        /// Initializes the component in a desktop environment.
        /// </summary>
        protected override void StartDesktop()
        {
            base.StartDesktop();

            GameObject root = PrefabInstantiator.InstantiatePrefab(cypherQueryWindowPrefab, Window.transform.Find("Content"), false);
            root.name = "Cypher Query Window";

            // QUERY INPUT
            queryInput = root.transform
                .Find("Content/QueryInput")
                .GetComponent<TMP_InputField>();

            queryInput.onSelect.AddListener(_ =>
            {
                SEEInput.KeyboardShortcutsEnabled = false;
            });

            queryInput.onDeselect.AddListener(_ =>
            {
                SEEInput.KeyboardShortcutsEnabled = true;
            });

            // QUERY OUTPUT
            outputText = root.transform
                .Find("Content/ScrollView/Viewport/Content/OutputText")
                .GetComponent<TMP_Text>();

            // EXECUTE BUTTON
            executeButton = root.transform
                .Find("Content/ExecuteButton")
                .GetComponent<Button>();

            executeButton.onClick.AddListener(ExecuteQuery);

            // CLEAR HIGHLIGHTS BUTTON
            clearButton = root.transform
                .Find("Content/ClearButton")
                .GetComponent<Button>();


            clearButton.onClick.AddListener(() =>
            {
                queryInput.text = "";
                outputText.text = "";
            });
        }

        /// <summary>
        /// Main Function for the Cypher-Query.
        /// Runs FullExecute() on all graphs in the scene.
        /// Constructs and outputs a string in form of a table.
        /// The table contains every QueryResult of FullExecute().
        /// </summary>
        private void ExecuteQuery()
        {
            // GRAPH INPUT
            if (allLoadedGraphs.Count == 0) // No Input Graph found, use following test graph
            {
                allLoadedGraphs.Add(CreateTestGraph());
            }

            // ENGINE
            allQueryResults = new Cypher.QueryExecutor().FullExecute(allLoadedGraphs, queryInput.text);

            // TABLE OUTPUT
            string result = "";
            foreach (Cypher.QueryResult qr in allQueryResults)
            {
                result += "------------------------\n";
                result += $"Searched Graph: {qr.graphName}\n";
                string chainedString = "|";
                foreach (string s in qr.Columns)
                {
                    chainedString += $" {s} |";
                }
                result += chainedString + "\n";
                result += "------------------------\n";
                foreach (List<object> row in qr.Rows)
                {
                    chainedString = "|";
                    foreach (object obj in row)
                    {
                        if (obj is GraphElement graphElement)
                        {
                            chainedString += $" {graphElement.ToShortString()} |";
                        }
                        else
                        {
                            chainedString += $" {obj.ToString()} |";
                        }
                    }
                    result += chainedString + "\n";
                }
                result += "------------------------\n";
            }
            outputText.text = result;

            // Highlight from QueryResult

        }

        #region BaseWindow
        public override void RebuildLayout()
        {
            // Nothing needs to be done.
        }

        public override WindowValues ToValueObject()
        {
            return new WindowValues(Title, gameObject.name);
        }

        public override void UpdateFromNetworkValueObject(WindowValues valueObject)
        {
            Title = valueObject.Title;
        }

        protected override void InitializeFromValueObject(WindowValues valueObject)
        {
            Title = valueObject.Title;
        }

        #endregion

        #region TestGraph
        /// <summary>
        /// Constructs a Graph that is mainly used for testing.
        /// Graph supports Attributes.
        /// </summary>
        /// <returns>Test Graph</returns>
        private Graph CreateTestGraph()
        {
            Graph graph = new("TESTPATH", "Test Graph");

            Node n1 = new()
            {
                ID = "n1",
                SourceName = "n1",
                Type = "Type1"
            };

            Node n2 = new()
            {
                ID = "n2",
                SourceName = "n2",
                Type = "Type1"
            };

            Node n3 = new()
            {
                ID = "n3",
                SourceName = "n3",
                Type = "Type2"
            };

            // Attribute
            n1.SetString("Name", "FunctionA");
            n1.SetInt("TestInt", 10);
            n1.SetFloat("Complexity", 2.5f);
            n1.SetToggle("Linkage.Is_Definition");

            n2.SetString("Name", "FunctionB");
            n2.SetInt("TestInt", 20);
            n2.SetFloat("Complexity", 4.2f);

            n3.SetString("Name", "FunctionC");
            n3.SetInt("TestInt", 30);
            n3.SetFloat("Complexity", 1.7f);

            // Add Nodes to Graph
            graph.AddNode(n1);
            graph.AddNode(n2);
            graph.AddNode(n3);

            // Add Edges to Graph
            graph.AddEdge(n1, n2, "call");
            graph.AddEdge(n1, n2, "use");

            graph.AddEdge(n2, n3, "call");
            graph.AddEdge(n2, n3, "use");

            graph.AddEdge(n3, n1, "call");

            return graph;
        }
        #endregion
    }
}
