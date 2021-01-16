using UnityEngine;
using SEE.DataModel.DG;
using SEE.Game;
using System;
using SEE.GO;
using System.Collections.Generic;
using System.Linq;

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
        private static bool isInnerNode;

        /// <summary>
        /// The Meta infos from the new node, set by the GUI
        /// 1. ID, 2. SourceName, 3. Type
        /// </summary>
        Tuple<String, String, String> nodeMetrics = null;

        /// <summary>
        /// The Object that the Cursor hovers over
        /// </summary>
        public GameObject hoveredObject = null;

        /// <summary>
        /// The script which instantiates the adding-node-canvas
        /// </summary>
        public CanvasGenerator canvasGenerator;

        /// <summary>
        /// true, if the addingnode-canvas is closed, else false.
        /// </summary>
        private static bool canvasIsActive = true;

        /// <summary>
        /// True, if the adding-node-canvas was opened, node-values was given and saved and the canvas was closed again, else false.
        /// </summary>
        private static bool valuesAreGiven = false;

        /// <summary>
        /// The name of a node given in the input-Field.
        /// </summary>
        private static string nodename;

        /// <summary>
        /// The type of a node given in the input-Field.
        /// </summary>
        private static string nodetype;

        /// <summary>
        /// Colour the hovered city is changed to when hovered or selected. 
        /// </summary>
        private Color BLACK = Color.black;

        /// <summary>
        /// Colour the hovered city is changed to when hovered or selected. 
        /// </summary>
        private Color GREEN = Color.green;

        /// <summary>
        /// A list the hovered GameObjects are stored in.
        /// </summary>
        private List<GameObject> hoveredObjectList = null;

        /// <summary>
        /// A list the colors of the hovered GameObjects are stored in.
        /// </summary>
        private List<Color> listOfColors = null;

        /// <summary>
        /// The GameObject Node
        /// </summary>
        private GameObject canvasObject;

        public void Start()
        {
            hoveredObjectList = new List<GameObject>();
            listOfColors =  new List<Color>();
            canvasObject = GameObject.Find("CanvasObject");
        }

        public void Update()
        {

            if (city == null)
            {
               //City Selection
                SelectCity();

            }
            else
            {
                //Gets the Metrics for the new Node if no
                if (nodeMetrics == null)
                {
                    OpenDialog();
                    GetMetrics();
                }
                else
                {
                    //Creates the new node, important check if the metrics have been set before!
                    if (valuesAreGiven)
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

            if (!canvasIsActive)
            {
                CanvasGenerator c = canvasObject.GetComponent<CanvasGenerator>();
                c.GetNodeMetrics();
                c.DestroyAddNodeCanvas();
                canvasIsActive = true;
                valuesAreGiven = true;
            }

        }

        /// <summary>
        /// opens a dialog-canvas where the user can insert some node-metrics. Therefore,a CanvasGenerator-script-component 
        /// will be added to this gameObject which contains the canvas.
        /// </summary>
        public void OpenDialog()
        {
            CanvasGenerator c = canvasObject.GetComponent<CanvasGenerator>();
            c.InstantiateAddingNodeCanvas();
        }

        /// <summary>
        /// Selects the City with hovering. Sets the City Object on Click on a GameObject
        /// </summary>
        private void SelectCity()
        {
            
            // The case the user hovers an object and has hovered objects before. The former colors of the specific objects are set again.
            if(hoveredObject != null && hoveredObjectList.Count >0 && hoveredObject.gameObject.GetComponent<Renderer>().material.color != GREEN || hoveredObject == null && hoveredObjectList.Count > 0)
            {
                int ct = 0; 
                foreach(GameObject GO in hoveredObjectList)
                {
                    GO.gameObject.GetComponent<Renderer>().material.color = listOfColors.ElementAt(ct);
                    ct++;
                }
                listOfColors.Clear();
                hoveredObjectList.Clear();
            }

            if (hoveredObject != null)
            {
                // The case the user hovers over an object which is not stored yet in the datastructure.
                // The object is either dyed green in case it is not already green, else black.
                if (!(hoveredObjectList.Contains(hoveredObject)))
                {
                    ChangeColor(hoveredObject, hoveredObject.gameObject.GetComponent<Renderer>().material.color);
                }
            }

                if (hoveredObject != null && Input.GetMouseButtonDown(0))
            {
               
                //Gets the SEECity from the hoverdObject
                SceneQueries.GetCodeCity(hoveredObject.transform)?.gameObject.TryGetComponent<SEECity>(out city);

            }
        }

        /// <summary>
        /// Sets the Metrics from the GUI
        /// </summary>
        private void GetMetrics()
        { 
            string randomID = RandomizeString();
            System.Random rnd = new System.Random();
            //YOU CANT MODIFY THE VALUES OF A TUPLE, SO YOU NEED TO CREATE A NEW ONE IF YOU WANT TO MODIFY
            nodeMetrics = new Tuple<string, string, string>(randomID, "NODE" + rnd.Next(0, 999999999), "FILE");
        }

        /// <summary>
        /// Dyes the hoveredObject either green in case it is not already green, else black.
        /// </summary>
        private void ChangeColor(GameObject hoveredbject, Color colorOfCity)
        {
            Color newCityColor = new Color();
            if (colorOfCity == GREEN)
            { newCityColor = BLACK;
            }
            else 
            {
                newCityColor = GREEN;
            }
            hoveredObjectList.Add(hoveredObject);
            listOfColors.Add(hoveredObject.gameObject.GetComponent<Renderer>().material.color);
            hoveredObject.gameObject.GetComponent<Renderer>().material.color = newCityColor;
        }
        /// <summary>
        /// creates a randomized string for the id for the created node
        /// </summary>
        private static string RandomizeString()
        {
            return Utils.RandomStrings.Get();

        }

        /// <summary>
        /// Adds a node to the loadedGraph and repeats the process in case the generated ID is not unique.
        /// </summary>
        private void AddNode(Node node)
        {
            try 
            {
                city.LoadedGraph.AddNode(node);
            }
            catch(Exception)
            {
                node.ID = RandomizeString();
                AddNode(node);
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

            AddNode(node);
            //Redraw the node Graph
            city.LoadedGraph.FinalizeNodeHierarchy();

            //gets the renderer
            GraphRenderer graphRenderer = city.Renderer;

            if (isInnerNode)
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
        }

        /// <summary>
        /// Places a node on call and checks if the city is the preselected one
        /// </summary>
        private void Place()
        {
            if (valuesAreGiven)
            {
                Node node = GONode.GetComponent<NodeRef>().node;
                //this will reached only if the nodename is not set in the inputField
                if (nodename != null && !nodename.Equals(""))
                {
                    node.SourceName = nodename;
                }

                if (nodetype != null && !nodetype.Equals(""))
                {
                    node.Type = nodetype;
                }

                SEECity cityTmp;
  
                    //checks if the currently hovered object is part of the preselected city
                    GameObject tmp = SceneQueries.GetCodeCity(hoveredObject.transform)?.gameObject;
                    try
                    {
                        tmp.TryGetComponent<SEECity>(out cityTmp);
                    }
                    catch (Exception)
                    {
                        Debug.Log("city not selected"); // FIXME
                        return;
                    }
                    if (cityTmp != null && city.Equals(cityTmp))
                    {
                        GameNodeMover.FinalizePosition(GONode, GONode.transform.position);
                        valuesAreGiven = false;
                    }
                else
                {
                    //FIXME: DO WE NEED TO DESTROY THE NODE TOO?
                    Destroy(GONode);
                    valuesAreGiven = false;

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
        public void RemoveScript()
        {
            if (GONode != null)
            {
                Place();
            }

            Destroy(this);
        }

        /// <summary>
        /// A setter-method for the canvasIsClosed-Attribute.
        /// </summary>
        /// <param name="isactive">the value for the canvasIsClosed-attribute. </param>
        public static void  SetValuesAreGiven(bool isactive)
        {
            valuesAreGiven = isactive;
        }

        /// <summary>
        /// Setter-method for the nodeName-attribute
        /// </summary>
        /// <param name="newNodename">new value for the nodeName-attribute</param>
        public static void SetNodeName(string newNodename)
        {
            nodename = newNodename;
        }

        /// <summary>
        /// Setter-method fpr the nodetype-attribute
        /// </summary>
        /// <param name="newNodeType">new value for the nodeType-attribute</param>
        public static void SetNodeType(string newNodeType)
        {
            nodetype = newNodeType;
        }

        public static void SetCanvasIsActive(bool newCanvasIsActive)
        {
            canvasIsActive = newCanvasIsActive;
        }
            
        public static void SetIsInnerNode(bool newIsInnerNode)
        {
            isInnerNode = newIsInnerNode;
        }
    }

}
