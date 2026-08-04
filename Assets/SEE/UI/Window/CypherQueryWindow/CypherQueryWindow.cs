
using Michsky.UI.ModernUIPack;
using SEE.Utils;
using SEE.Controls;

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SEE;

using TMPro;
using UnityEngine.UI;

namespace SEE.UI.Window.CypherQueryWindow
{
    public class CypherQueryWindow : BaseWindow
    {
        private const string cypherQueryWindowPrefab = "Prefabs/UI/CypherQueryWindow/CypherQueryWindow";
        private TMP_InputField queryInput;

        private TMP_Text outputText;

        private Button executeButton;

        private Button clearButton;

        /// <summary>
        /// Is called when the component is mounted and initializes it.
        /// </summary>
        protected override void Start()
        {
            Title = "Cypher Query";
            base.Start();
        }

        protected override void StartDesktop()
        {
            base.StartDesktop();

            GameObject content = PrefabInstantiator.InstantiatePrefab(cypherQueryWindowPrefab, Window.transform.Find("Content"), false);
            content.name = "Cypher Query Window";

            // QUERY INPUT
            queryInput = content.transform
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
            outputText = content.transform
                .Find("Content/OutputText")
                .GetComponent<TMP_Text>();

            // EXECUTE BUTTON
            executeButton = content.transform
                .Find("Content/ExecuteButton")
                .GetComponent<Button>();

            executeButton.onClick.AddListener(ExecuteQuery);

            // CLEAR HIGHLIGHTS BUTTON
            clearButton = content.transform
                .Find("Content/ClearButton")
                .GetComponent<Button>();


            clearButton.onClick.AddListener(() =>
            {
                queryInput.text = "";
                outputText.text = "";
            });
        }

        private void ExecuteQuery()
        {
            // GRAPH INPUT
            Cypher.Graph graph = Cypher.TestGraph.Create();
            // ENGINE
            Cypher.QueryResult qr = new Cypher.QueryExecutor().ExecuteQuery(graph, queryInput.text);

            // TABLE OUTPUT
            string result = "------------------------\n";
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
                    chainedString += $" {obj.ToString()} |";
                }
                result += chainedString + "\n";
            }
            result += "------------------------\n";

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
    }
}
