using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FuzzySharp;
using SEE.Controls;
using SEE.DataModel.DG;
using SEE.Game;
using SEE.Game.City.LiveDocumentation;
using SEE.Game.Evolution;
using SEE.Game.UI.Menu;
using SEE.Game.UI.Notification;
using SEE.Game.UI.PropertyDialog;
using SEE.Utils;
using UnityEngine;

namespace SEE.GO.Menu
{
    /// <summary>
    /// A menu which allows its user to fuzzy search for nodes by entering the
    /// source name of a node.
    /// </summary>
    public class DocumentationMenu: MonoBehaviour
    {

        /// <summary>
        /// The node that is currently selected.
        /// </summary>
        private readonly Node selectedNode;
        
        /// <summary>
        /// The dialog in which the search query can be entered.
        /// </summary>
        private PropertyDialog testDialog;

        private List<Extractor> extractors = new List<Extractor>();

        public DocumentationMenu()
        {
            extractors.Add(new CommentExtractor());
        }

        private void Start()
        {
            foreach (var extractor in extractors)
            {
                extractor.PerformExtraction(selectedNode.SourceFile);
            }
        }

        /// <summary>
        /// Checks whether the <see cref="searchDialog"/> shall be opened.
        /// </summary>
        private void Update()
        {
            if (SEEInput.ToggleDocumentationMenu())
            {
                SEEInput.KeyboardShortcutsEnabled = false;
                testDialog.DialogShouldBeShown = true;
                SEEInput.KeyboardShortcutsEnabled = false;
            }
        }
    }
}