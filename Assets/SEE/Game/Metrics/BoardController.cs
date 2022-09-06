using System;
using SEE.DataModel;
using SEE.GO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.Game.MetricsVisualization
{
    public class BoardController : MonoBehaviour
    {
        public GameObject prefab;
        private bool _initiated;
    
        // TODO: Update this automatically when a new code de city is loaded
        private void Update()
        {
            if (! _initiated && Input.GetKeyDown(KeyCode.M))
            {
                _initiated = true;
                GameObject[] nodes = GameObject.FindGameObjectsWithTag(Tags.Node);
                GameObject[] edges = GameObject.FindGameObjectsWithTag(Tags.Edge);
                int numberOfNodes = 0;
                float sum = 0.0f;
                foreach (GameObject node in nodes)
                {

                    DataModel.DG.Node graphNode = node.GetComponent<NodeRef>().Value;
                    if (graphNode != null)
                    {
                        try
                        {
                            sum += graphNode.GetNumeric("Metric.Lines.LOC");
                            numberOfNodes++;
                        }
                        catch(Exception e)
                        {
                            
                        }
                    }
                }
                float avg = sum / numberOfNodes;
            
                DrawLinesOfCodeMetric((int) avg);
            }
        }

        private void DrawLinesOfCodeMetric(int lineCount)
        {
            Vector2 boardSize = GetComponent<SpriteRenderer>().size;
            Vector2 prefabSize = prefab.GetComponent<SpriteRenderer>().size;
            float needleRotationZ;
            if (lineCount < 350)
                needleRotationZ = lineCount < 100 ? 130.0f : 90.0f;
            else
                needleRotationZ = lineCount < 400 ? -90.0f : -120.0f;
            Quaternion needleRotation = Quaternion.Euler(0.0f, 0.0f, needleRotationZ);
            Transform anchor = transform.Find("TopOne");
            GameObject prefabInstance = Instantiate(prefab, anchor.position, anchor.rotation, anchor);
            prefabInstance.transform.Find("NeedleAnchor").localRotation = needleRotation;
            TextMeshProUGUI text = prefabInstance.transform.Find("Canvas").Find("Text").GetComponent<TextMeshProUGUI>();
            text.text = "Lines of code";
        }
    }
}