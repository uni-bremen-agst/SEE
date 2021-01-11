using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SEE.DataModel.DG;
using SEE.Game;
using System;
using SEE.Controls.Actions;
using SEE.Utils;
using UnityEngine.UI;
using SEE.GO;

namespace SEE.Controls
{

    public class DesktopNewNodeAction : MonoBehaviour
    {
        /// <summary>
        /// The Code City in wich the node should be placed
        /// </summary>
        SEECity city = null;

        /// <summary>
        /// The New GameObject
        /// </summary>
        GameObject GONode = null;

        /// <summary>
        /// Set by the GUI if a Inner Node should be created
        /// </summary>
        public static bool is_innerNode;

        /// <summary>
        /// The Meta infos from the new node, set by the GUI
        /// 1. ID, 2. SourceName, 3. Type
        /// </summary>
        Tuple<String, String, String> nodeMetrics = null;

        /// <summary>
        /// The Object that the Cursor hovers over
        /// </summary>
        public GameObject hoveredObject = null;

        public CanvasGenerator canvasGenerator;

        public static bool canvasIsClosed = true;

        public static bool anotherBool = false;

        /// <summary>
        /// The name of a node given in the input-Field.
        /// </summary>
        public static string nodename;

        /// <summary>
        /// The type of a node given in the input-Field.
        /// </summary>
        public static string nodetype;

        public void Start()
        {

        }

        public void Update()
        {

            if (city == null)
            {
                //City Selection
                selectCity();
            }
            else
            {
                //Gets the Metrics for the new Node if no
                if (nodeMetrics == null)
                {
                    OpenDialog();
                    getMetrics();
                }
                else
                {
                    //Creates the new node, important check if the metrics have been set before!
                    if (anotherBool)
                    {
                        if (GONode == null)
                        {
                            NewNode();
                            GameNodeMover.MoveTo(GONode);
                        }
                        else
                        {
                            if (Input.GetMouseButtonDown(0))
                            {

                                Place();
                            }
                            else
                            {
                                GameNodeMover.MoveTo(GONode);
                            }
                        }
                    }
                }
            }
            
                if(!canvasIsClosed)
                {
                    GameObject canvasObject = GameObject.Find("AddingNodeCanvas");
                    CanvasGenerator c = (CanvasGenerator)canvasObject.GetComponent("CanvasGenerator");
                    c.DestroyGameObject();
                anotherBool = true;
                canvasIsClosed = true;
                }

        }

        public void OpenDialog()
        {
            GameObject canvasObject = GameObject.Find("AddingNodeCanvas");
            CanvasGenerator c = (CanvasGenerator)canvasObject.GetComponent("CanvasGenerator");
            GameObject canvas = c.InstantiateGameObject();
            Debug.Log(canvas);
            
        }

        /// <summary>
        /// Selects the City with hovering. Sets the City Object on Click on a GameObject
        /// </summary>
        private void selectCity()
        {

            if (hoveredObject != null && Input.GetMouseButtonDown(0))
            {
                //Gets the SEECity from the hoverdObject
                SceneQueries.GetCodeCity(hoveredObject.transform)?.gameObject.TryGetComponent<SEECity>(out city);

            }
        }

        /// <summary>
        /// Sets the Metrics from the GUI
        /// </summary>
        private void getMetrics()
        { 
            string randomID = randomizeString();
            System.Random rnd = new System.Random();
            //YOU CANT MODIFY THE VALUES OF A TUPLE, SO YOU NEED TO CREATE A NEW ONE IF YOU WANT TO MODIFY
            nodeMetrics = new Tuple<string, string, string>(randomID, "TEST-NODE" + rnd.Next(0, 999999999), "TEST NODE");
        }

        private static string randomizeString()
        {
            return SEE.Utils.RandomStrings.Get();

        }

        /// <summary>
        /// Adds a node to the loadedGraph and repeats the process in case the generated ID is not unique.
        /// </summary>
        private void addNode(Node node)
        {
            try 
            {
                city.LoadedGraph.AddNode(node);
            }
            catch(Exception e)
            {
                node.ID = randomizeString();
                addNode(node);
                return; 
            }
        }
        /// <summary>
        /// Creates a new node
        /// </summary>
        /// <returns>New Node as GameObject</returns>
        private void NewNode()
        {
            GameObject gameNode;
            Node node = new Node();

            //Set the metrics of the new node
            node.ID = nodeMetrics.Item1;
            node.SourceName = nodeMetrics.Item2;
            node.Type = nodeMetrics.Item3;


            //Adds the new Node to the City Graph

            addNode(node);
            //Redraw the node Graph
            city.LoadedGraph.FinalizeNodeHierarchy();

            //gets the renderer
            GraphRenderer graphRenderer = city.Renderer;

            if (is_innerNode)
            {
                gameNode = graphRenderer.NewInnerNode(node);

            }
            else
            {
                gameNode = graphRenderer.NewLeafNode(node);
            }

            //Sets the The GONode so the main work can continue;
            GONode = gameNode;
            GameNodeMover.MoveTo(GONode);
            Debug.Log("Endgültiger Nodename: " + node.SourceName);
            Debug.Log("Endgültiger Nodetype: " + node.Type);
        }



        /// <summary>
        /// Places a node on call and checks if the city is the preselected one
        /// </summary>

        private void Place()
        {
            if (anotherBool)
            {
                Node node = GONode.GetComponent<NodeRef>().node;

                if (nodename != null || !nodename.Equals(" ")) ;
                {
                    node.SourceName = nodename;
                }

                if (nodetype != null || !nodetype.Equals(" "))
                {
                    node.Type = nodetype;
                }

                Debug.Log(node.SourceName);
                Debug.Log(node.Type);

                SEECity cityTmp = null;
                if (hoveredObject != null)
                {

                    //checks if the currently hovered object is part of the preselected city
                    GameObject tmp = SceneQueries.GetCodeCity(hoveredObject.transform)?.gameObject;
                    try
                    {
                        tmp.TryGetComponent<SEECity>(out cityTmp);
                    }
                    catch (Exception np)
                    {
                        Debug.Log("city not selected"); // FIXME
                        return;
                    }
                    if (city.Equals(cityTmp))
                    {
                        GameNodeMover.FinalizePosition(GONode, GONode.transform.position);
                        anotherBool = false;
                    }

                }
                else
                {
                    //FIXME: DO WE NEED TO DESTROY THE NODE TO?
                    Destroy(GONode);
                }
                GONode = null;
                city = null;
                nodeMetrics = null;
            }
        }

        /// <summary>
        /// Removes The Script
        /// Places the new Node if not placed
        /// </summary>
        public void removeScript()
        {
            if (GONode != null)
            {
                Place();
            }

            Destroy(this);
        }

        public static void  SetBool(bool isactive)
        {
            canvasIsClosed = isactive;
        }

    }
}
