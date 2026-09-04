
using Michsky.UI.ModernUIPack;
using SEE.Utils;
using SEE.Controls;
using SEE.DataModel.DG;

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SEE;
using SEE.Game.City;
using SEE.Game;
using SEE.GO;

using TMPro;
using UnityEngine.UI;
using Cypher;

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
        private List<Cypher.QueryResult> allQueryResults = new();

        /// <summary>
        /// List that stores all graphs in the current scene. // Not currently
        /// ExecuteQuery() runs on all graphs in this List.
        /// </summary>
        private List<Graph> allLoadedGraphs = new List<Graph>(); // TODO just specific graphs per user request?

        /// <summary>
        /// List that contains all GraphElements in allQueryResults.
        /// Used for highlight and dehighlight.
        /// </summary>
        HashSet<GraphElement> foundGraphElementList = new();

        /// <summary>
        /// Pairs foundGraphElementList with References to GameObjects
        /// </summary>
        Dictionary<GraphElement, GraphElementRef> refs = new();

        String helpText =
            "Help:\n" +
            "Jede Abfrage braucht eine MATCH und RETURN Klausel.\n"+
            "-- MATCH Beispiele: --\n"+

            "MATCH (n)\n"+
            "hier werden alle Knoten ausgewählt und mit der Variable n vermerkt.\n\n"+

            "MATCH (n)-[r]-(m)\n"+
            "hier werden alle Kanten ausgewählt. Die Startknoten werden mit n, die Kante an sich mit r und der Endknoten mit m vermerkt.\n\n"+

            "MATCH (n:File)\n"+
            "Hier werden alle Knoten mit dem Typ File ausgegeben.\n\n"+
            "MATCH ()-[r:help]->()\n"+
            "Hier wird jede Kante vom Typ 'help' im Graphen ausgewählt.\n\n"+

            "-- WHERE Beispiel: --\n"+
            "MATCH (n)\n"+
            "WHERE n.Source.Name = 'tx.c' AND n.Metric.LOC > 0\n"+ // testen
            "Hiermit werden nur die Ausgaben aus MATCH berücksichtigt, welche die Bedingungen in WHERE erfüllen.\n\n"+

            "-- RETURN Beispiel: --\n"+
            "MATCH (n:Files)\n"+
            "RETURN n, n.Source.Name, n.Metric.LOC\n"+
            "Ausgegeben wird: Jeder Knoten mit dem Typ File, jeder Source.Name der Files, jede Metric.LOC der Files.";

        // Window Components ///////////////////////////////
        /// <summary>
        /// Project path of the prefab.
        /// </summary>
        private const string cypherQueryWindowPrefab = "Prefabs/UI/CypherQueryWindow/CypherQueryWindow";

        /// <summary>
        /// Project path of the Row prefab.
        /// </summary>
        private const string cypherQueryWindowRowPrefab = "Prefabs/UI/CypherQueryWindow/CypherQueryWindowRow";

        private const string resultTitlePrefab = "Prefabs/UI/CypherQueryWindow/CypherQueryWindowGraphTitle";

        /// <summary>
        /// User Input for the Cypher-Query.
        /// Currently only MATCH, WHERE and RETURN supported.
        /// </summary>
        private TMP_InputField queryInput;

        /// <summary>
        /// Output of the Cypher-Query in form of a table.
        /// // TODO is supposed to write Exceptions, Errors, Highlight Success and Highlight failure
        /// // TODO horizontal Scrollbar
        /// </summary>
        private TMP_Text outputText;
        private Transform resultContent;

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
            // OUTPUTEXT
            outputText = root.transform
                .Find("Content/ScrollView/Viewport/Content/OutputText")
                .GetComponent<TMP_Text>();
            outputText.text = helpText;

            // QUERY RESULT OUTPUT
            resultContent = root.transform
                .Find("Content/ScrollView/Viewport/Content/ResultContent");

            // EXECUTE BUTTON
            executeButton = root.transform
                .Find("Content/ExecuteButton")
                .GetComponent<Button>();

            executeButton.onClick.AddListener(ExecuteQuery);

            // CLEAR HIGHLIGHTS BUTTON
            clearButton = root.transform
                .Find("Content/ClearButton")
                .GetComponent<Button>();

            clearButton.onClick.AddListener(Clear);
        }

        /// <summary>
        /// Main Function for the Cypher-Query.
        /// Runs FullExecute() on all graphs in the scene.
        /// Constructs and outputs a string in form of a table.
        /// The table contains every QueryResult of FullExecute().
        /// Also dehighlights all Gameobjects and highlights GameObjects that are referenced in refs.
        /// </summary>
        private void ExecuteQuery()
        {
            outputText.text = "Fehler in der Abfrage";
            allLoadedGraphs = new List<Graph>();
            ClearResult();


            /*Working*/
            // copied from Network.cs InitializeGame()
            SEECity[] cities = UnityEngine.Object.FindObjectsByType<SEECity>(FindObjectsSortMode.None);
            refs = new();

            foreach (SEECity city in cities)
            {
                Graph graph = city.LoadedGraph;
                if (graph is null) {continue;}
                allLoadedGraphs.Add(graph);

                foreach (NodeRef nodeRef in city.GetComponentsInChildren<NodeRef>())
                {
                    if (nodeRef.Value is not null) {refs[nodeRef.Value] = nodeRef;}
                    nodeRef.gameObject.SetHighlight(false);
                }
                foreach (EdgeRef edgeRef in city.GetComponentsInChildren<EdgeRef>())
                {
                    if (edgeRef.Value is not null) {refs[edgeRef.Value] = edgeRef;}
                    edgeRef.gameObject.SetHighlight(false);
                }
            }
            //


            // GRAPH INPUT
            if (allLoadedGraphs.Count == 0) // No Input Graph found, use following test graph
            {
                allLoadedGraphs.Add(CreateTestGraph());
            }

            // ENGINE
            allQueryResults = new Cypher.QueryExecutor().FullExecute(allLoadedGraphs, queryInput.text);

            // RESULT OUTPUT
            foreach (Cypher.QueryResult qr in allQueryResults)
            {
                List<CypherQueryWindowRow> allGeneratedRows = new();
                CypherQueryWindowRow row;

                row = AddResultHeader(qr);
                allGeneratedRows.Add(row);

                foreach (List<object> queryRow in qr.Rows)
                {
                    row = AddResultRow(queryRow);
                    allGeneratedRows.Add(row);
                }
                List<float> maxWidths = getMaxWidths(allGeneratedRows);
                foreach (CypherQueryWindowRow generatedRow in allGeneratedRows)
                {
                    SetRowWidths(maxWidths, generatedRow);
                }

            }

            // HIGHLIGHT

            /*working*/
            // Gather GraphElement Lists

            foundGraphElementList = new();
            foreach (QueryResult qr in allQueryResults)
            {
                foundGraphElementList.UnionWith(qr.graphElementsInTable);
            }

            String result = "";
            // Highlight all GraphElements in List
            int highlight_counter = 0;
            foreach (GraphElement element in foundGraphElementList)
            {
                if (refs.TryGetValue(element, out GraphElementRef r))
                {
                    GameObject g = r.gameObject;
                    // highlight g

                    //g.SetHighlight(true);
                    g.EnableGlowOutline(); // working
                    highlight_counter += 1;
                }
                else
                {
                    //write No Unity object for highlighting found
                    result += $"{element.ToShortString()}: Failed to highlight.\n";
                }
            }
            result += $"{highlight_counter} Objects successfully highlighted.\n";
            //
            outputText.text = result;

        }

        private List<float> getMaxWidths(List<CypherQueryWindowRow> rows)
        {
            List<float> maxWidths = new();
            int countColumn = 0;
            while (countColumn < rows[0].cells.Count())
            {
                float maxWidth = 0;
                int countRow = 0;
                while (countRow < rows.Count())
                {
                    float pref = rows[countRow].cells[countColumn].PreferredWidth;
                    if (maxWidth < pref){maxWidth = pref;}
                    countRow += 1;
                }
                maxWidths.Add(maxWidth);
                countColumn += 1;
            }
            return maxWidths;
        }

        private void SetRowWidths(List<float> maxWidths, CypherQueryWindowRow row)
        {
            int i = 0;
            while (i < row.cells.Count())
            {
                row.cells[i].SetWidth(maxWidths[i] + 10);
                i += 1;
            }
        }


        /// <summary>
        /// Adds the header of a query result.
        /// </summary>
        private CypherQueryWindowRow AddResultHeader(Cypher.QueryResult qr)
        {
            GameObject titleObject = PrefabInstantiator.InstantiatePrefab(
                resultTitlePrefab,
                resultContent,
                false
            );

            TMP_Text title = titleObject.MustGetComponent<TMP_Text>();
            title.text = qr.graphName;

            return AddHeaderRow(qr.Columns);
        }

        private CypherQueryWindowRow AddHeaderRow(List<string> columns)
        {
            GameObject rowObject = PrefabInstantiator.InstantiatePrefab(
                cypherQueryWindowRowPrefab,
                resultContent,
                false
            );

            CypherQueryWindowRow row =
                rowObject.MustGetComponent<CypherQueryWindowRow>();

            foreach (string column in columns)
            {
                row.AddHeaderCell(column);
            }
            return row;
        }

        /// <summary>
        /// Adds a result row to the result table.
        /// </summary>
        private CypherQueryWindowRow AddResultRow(List<object> values)
        {
            GameObject rowObject = PrefabInstantiator.InstantiatePrefab(
                cypherQueryWindowRowPrefab,
                resultContent,
                false
            );

            CypherQueryWindowRow row = rowObject.MustGetComponent<CypherQueryWindowRow>();

            foreach (object value in values)
            {
                GraphElementRef reference = null;

                if (value is GraphElement element)
                {
                    refs.TryGetValue(element, out reference);
                }

                row.AddCell(value, reference);
            }
            return row;
        }

        private void ClearResult()
        {
            foreach (Transform child in resultContent)
            {
                Destroy(child.gameObject);
            }
        }

        private void Clear()
        {
            // Dehighlight everything
            SEECity[] cities = UnityEngine.Object.FindObjectsByType<SEECity>(FindObjectsSortMode.None);
            foreach (SEECity city in cities)
            {
                foreach (NodeRef nodeRef in city.GetComponentsInChildren<NodeRef>())
                {
                    nodeRef.gameObject.SetHighlight(false);
                }
                foreach (EdgeRef edgeRef in city.GetComponentsInChildren<EdgeRef>())
                {
                    edgeRef.gameObject.SetHighlight(false);
                }
            }
            // reset all variables
            allQueryResults = new();
            allLoadedGraphs = new List<Graph>();
            foundGraphElementList = new();
            refs = new();
            ClearResult();

            // reset Window Text
            queryInput.text = "";
            outputText.text = helpText;

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
