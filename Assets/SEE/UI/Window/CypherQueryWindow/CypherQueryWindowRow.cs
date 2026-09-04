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
    /// Represents one row of a Cypher query result table.
    /// </summary>
    public class CypherQueryWindowRow : MonoBehaviour
    {
        public List<CypherQueryWindowCell> cells {get;set;}= new();

        /// <summary>
        /// The project path to the cell prefab.
        /// </summary>
        private const string cypherQueryWindowCellPrefab = "Prefabs/UI/CypherQueryWindow/CypherQueryWindowCell";

        /// <summary>
        /// Adds a cell to this row and initializes it with the given value.
        /// </summary>
        /// <param name="value">The value displayed by the cell.</param>
        /// <param name="reference">
        /// The graph element reference, if the value represents a graph element.
        /// </param>
        public void AddCell(object value, GraphElementRef reference = null)
        {
            GameObject cellObject = PrefabInstantiator.InstantiatePrefab(
                cypherQueryWindowCellPrefab,
                transform,
                false
            );

            CypherQueryWindowCell cell = cellObject.MustGetComponent<CypherQueryWindowCell>();

            cell.Initialize(value, reference);
            cells.Add(cell);
        }

        public void AddHeaderCell(string str)
        {
            GameObject cellObject = PrefabInstantiator.InstantiatePrefab(
                cypherQueryWindowCellPrefab,
                transform,
                false
            );
            CypherQueryWindowCell cell = cellObject.MustGetComponent<CypherQueryWindowCell>();

            cell.InitializeHeader(str);
            cells.Add(cell);
        }
    }
}
