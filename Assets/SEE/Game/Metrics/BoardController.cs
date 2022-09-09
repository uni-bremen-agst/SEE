using System;
using System.Threading.Tasks;
using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.GO;
using TMPro;
using UnityEngine;

namespace SEE.Game.Metrics
{
    public class BoardController : MonoBehaviour
    {
        private bool _setUp;
        
        // All the metrics
        private float _averageLinesOfCode;
        
        // References to prefabs to be bound from the unity editor.
        public GameObject tachometerPrefab;
        
        // References to visualization GameObjects
        private GameObject _linesOfCodeVisualization;

        private void Start()
        {
            // Draw all the selected visualizations on the board
            // TODO: Dynamically decide which metrics to draw based on the user selection.
            DrawLinesOfCodeMetric();
            
            _setUp = true;
        }

        public async void OnGraphLoad()
        {
            while (!_setUp)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
            }

            // (Re-)Calculate all the metrics
            CalculateMetrics();

            // Adapt all the visualizations (e.g. change needle angle for tachometer)
            UpdateLinesOfCodeMetric();
        }

        /// <summary>
        /// This method calculates all the metrics for the currently loaded Graph(s). The results are saved in fields
        /// of this class. We try to calculate as many metrics with as few iterations of loops as possible.
        /// </summary>
        private void CalculateMetrics()
        {
            GameObject[] nodes = GameObject.FindGameObjectsWithTag(Tags.Node);
            //GameObject[] edges = GameObject.FindGameObjectsWithTag(Tags.Edge);
            int totalNodes = 0;
            float totalLines = 0.0f;

            foreach (GameObject node in nodes)
            {
                Node graphNode = node.GetComponent<NodeRef>().Value;
                if (graphNode != null && graphNode.TryGetNumeric("Metric.Lines.LOC", out var lines))
                {
                    totalLines += lines;
                    totalNodes++;
                }
            }

            if (totalNodes != 0)
            {
                _averageLinesOfCode = totalLines / totalNodes;    
            }
        }
        
        /// <summary>
        /// Initially draws the lines of code metric with a default value.
        /// This method is called once when the scene starts.
        /// </summary>
        private void DrawLinesOfCodeMetric()
        {
            Transform anchor = transform.Find("TopOne");  // TODO: Find out if getting by id is better in terms of run time
            _linesOfCodeVisualization = Instantiate(tachometerPrefab, anchor.position, anchor.rotation, anchor);
            TextMeshPro text = _linesOfCodeVisualization.transform.Find("TextAnchor").GetComponent<TextMeshPro>();
            text.text = "Avg. lines of code";
        }

        /// <summary>
        /// This method implements a mapping from a number of code lines to a number of degrees that determine how far a
        /// tachometer needle in Unity will be rotated. The value range for the number of lines is from 0 to 1000, the
        /// value range for the number of degrees is from 140 to -140. 0 lines of code will be mapped to 140 degrees, 
        /// 1000 lines of code will be mapped to -140 degrees. This is a linear function and everything in between will 
        /// be mapped accordingly. If the <paramref name="lines"/> lie outside the value range, they will be set to the
        /// nearest value in the range.
        /// </summary>
        /// <param name="lines">The number of code lines</param>
        /// <returns>The amount of degrees the needle should have</returns>
        private static float MapLinesToDegrees(float lines)
        {
            if (lines > 1000)
                lines = 1000;
            else if (lines < 0)
                lines = 0;
            return (lines - 500) * -0.28f;
        }
        
        private void UpdateLinesOfCodeMetric()
        {
            float needleRotationZ = MapLinesToDegrees(_averageLinesOfCode);
            Quaternion needleRotation = Quaternion.Euler(0.0f, 0.0f, needleRotationZ);
            _linesOfCodeVisualization.transform.Find("NeedleAnchor").localRotation = needleRotation;
            Debug.LogFormat("Set needle angle to {0}, average lines of code were {1}", needleRotationZ, _averageLinesOfCode);
        }
    }
}