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

        /// <summary>
        /// A list the gamenodes are stored in.
        /// </summary>
        public ICollection<GameNode> gameNodes = null;

        /// <summary>
        /// The median of the lossyscale of the graphs leafs  
        /// </summary>
        private Vector3 medianOfLeaf = new Vector3();

        /// <summary>
        /// The median of the lossyscale of the graphs inner nodes.
        /// </summary>
        private Vector3 medianOfInnerNode = new Vector3();

        /// <summary>
        /// A gamobject which stores the graphs root. 
        /// </summary>
        private GameObject dir_Root = null;

        /// <summary>
        /// The colour the graphs leafs graphical representation is dyed . 
        /// </summary>
        private Color leafColor = Color.red;

        /// <summary>
        /// The colour the graphs inner nodes graphical representation is dyed . 
        /// </summary>
        private Color innerNodeColor = Color.white;

        Vector3 zeroVector = new Vector3(0, 0, 0);


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
        /// opens a dialog-canvas where the user can insert some node-metrics. Therefore, a CanvasGenerator-script-component 
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

            // The case the user hovers an object and has hovered objects before. The former colours of the specific objects are set again.
            if (hoveredObject != null && hoveredObjectList.Count > 0 && hoveredObject.gameObject.GetComponent<Renderer>().material.color != GREEN || hoveredObject == null && hoveredObjectList.Count > 0)
            {
                Undye();
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
                Undye();
                GetNodesOfScene();
                //Gets the SEECity from the hoverdObject
                SceneQueries.GetCodeCity(hoveredObject.transform)?.gameObject.TryGetComponent<SEECity>(out city);
                if (dir_Root != null)
                {
                    dir_Root.GetComponent<Renderer>().material.color = Color.white;
                }
            }
        }


        // <summary>
        /// Undyes the the current colour of the object, i.e. changes the color to its original color.
        /// </summary>
        public void Undye()
        {
            int count = 0;
            foreach (GameObject GO in hoveredObjectList)
            {
                GO.gameObject.GetComponent<Renderer>().material.color = listOfColors.ElementAt(count);
                count++;
            }
            listOfColors.Clear();
            hoveredObjectList.Clear();
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
        /// Dyes the hoveredObject either green in case it is not already green, else black by default.
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
        /// Adds a node to the loadedGraph and creats its ID. Repeats the process in case the generated ID is not unique.
        /// </summary>
        /// /// <param name="newNodeType">The node to add to the graph</param>
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
            if (isInnerNode)
            {

                GONode.transform.localScale = medianOfInnerNode;
                GONode.gameObject.GetComponent<Renderer>().material.color = innerNodeColor;

            }
            else
            {

                GONode.transform.localScale = medianOfLeaf;
                GONode.gameObject.GetComponent<Renderer>().material.color = leafColor;

            }
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
        /// Setter-method for the nodetype-attribute
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



        /// <summary>
        /// 
        /// </summary>
        public void GetNodesOfScene()
        {

            //Prepares a list of all leafs lossyscale which is stored in the list innerNodeSize.
            List<Vector3> leafSize = new List<Vector3>();

            //Prepares a list of all innernodes lossyscale which is stored in the list innerNodeSize.
            List<Vector3> innerNodeSize = new List<Vector3>();

            // Query to obtain all the leafnodes of the specific scene.
            ICollection<GameObject> allLeafsInScene = SceneQueries.AllGameNodesInScene(true, false);


            //Query to obtain all the inner nodes of the specific scene.
            ICollection<GameObject> allInnerNodesInScene = SceneQueries.AllGameNodesInScene(false, true);


            // In the special case the graph only consists of one leaf, we will have to check in the list of all leafs, which has the count of one in that case,
            // if there is the root node.
            if (allLeafsInScene.Count == 1 && allInnerNodesInScene.Count == 0)
            {
                dir_Root = rootSearch(allLeafsInScene);

            }


            // Search for the graphs root in the set of all inner nodes.
            dir_Root = rootSearch(allInnerNodesInScene);


            // Fill the lists with the specific lossyscales of all the nodes, either leafs or innernodes.
            leafSize = listOfLossyscale(allLeafsInScene);
            innerNodeSize = listOfLossyscale(allInnerNodesInScene);
            
            // Calculate the median of the specific sets, either leafs or innernodes. 
            medianOfLeaf = CalcMedianOfaVector(leafSize);
            medianOfInnerNode = CalcMedianOfaVector(innerNodeSize);
            
            // In the special case there are no inner nodes, the median of the graphs only leaf is set 
            // as a default value for any inner node that might be created.
            if (innerNodeSize.Count == 0)
            {
                medianOfInnerNode = medianOfLeaf;
            }
        }

        /// <summary>
        /// Returns a vector list filled with the lossyscale of the param pListOfGameObject. 
        /// </summary>
        /// <param name="pListOfGameObjects">A List of GameObjects</param>
        /// <returns></returns>
        private List<Vector3> listOfLossyscale(ICollection<GameObject> pListOfGameObjects)
        {
            if (pListOfGameObjects == null | pListOfGameObjects.Count == 0)
            {
                return null;
            }
            List<Vector3> lossyScaleList = new List<Vector3>();
            foreach (GameObject go in pListOfGameObjects)
            {
                    lossyScaleList.Add(go.transform.lossyScale);
            }

            return lossyScaleList;


        }

        private GameObject rootSearch(ICollection<GameObject> pListOfGameNodes)
        {
            GameObject root = new GameObject();
            Node rootTmp = new Node();
            foreach (GameObject rootSearchItem in pListOfGameNodes)
            {
                rootTmp = rootSearchItem.GetComponent<NodeRef>().node;
                if (rootTmp.IsRoot())
                {
                    root = rootSearchItem;
                    return root;
                }
            }
            return null; 
        }


        /// <summary>
        /// Calculates the median of a list of vectors.
        /// </summary>
        /// /// <param name="vectors">List of vectors-attribute</param>
        private Vector3 CalcMedianOfaVector(List<Vector3> vectors)
        {
            if (vectors.Count == 0 || vectors == null)
            {
                return new Vector3(0,0,0);
            }

            Vector3 result = new Vector3();
            List<float> xAxis = new List<float>();
            List<float> yAxis = new List<float>();
            List<float> zAxis = new List<float>();

            foreach (Vector3 vect in vectors)
            {
                xAxis.Add(vect.x);
                yAxis.Add(vect.y);
                zAxis.Add(vect.z);
            }

            xAxis.Sort();
            yAxis.Sort();
            zAxis.Sort();
   
            result.x = CalcMedianElementFromFloats(xAxis);
            result.y = CalcMedianElementFromFloats(yAxis);
            result.z = CalcMedianElementFromFloats(zAxis);


            
            if(!(vectors.Count %2 == 0))
            {
                return result; 
            }

            int indexSecondMedian = (xAxis.Count +1) /2;
            float SecondXCoordinate = CalcMedianElementFromFloats(xAxis);
            float SecondYCoordinate = CalcMedianElementFromFloats(yAxis);
            float SecondZCoordinate = CalcMedianElementFromFloats(zAxis);

            result.x = (result.x + SecondXCoordinate) / 2;
            result.y = (result.y + SecondYCoordinate) / 2;
            result.z = (result.z + SecondZCoordinate) / 2;

            return result;
        }

        /// <summary>
        /// Calculates the median of a list of floats.
        /// </summary>
        /// /// <param name="newNodeType">List of floats of which the median is calculated</param>
        private static float CalcMedianElementFromFloats(List<float> floatList)
        {
           float median = 0;
            if(floatList.Count == 0 | floatList == null)
            {
                return median; 
            }
            int indexOfMid = floatList.Count;
            indexOfMid = indexOfMid/2; 
            median = floatList.ElementAt(indexOfMid);

            // If the amount of the list is impair, we will return the element which is located at the middle of the list,
            // e.g. the amount = 13 , i.e the element at the index 6.
            if (!(floatList.Count % 2 == 0))
            {
                return median;
            }

            // If the amount is impair, we have to interpolate linearly between the value at the index at the half of the lists size,
            // and the value of the following index. E.g. size = 13 -> index 6 and 7 .
            int indexSecondMedianValue  = indexOfMid+1;
            float SecondCoordinate = floatList.ElementAt(indexSecondMedianValue);
            median += SecondCoordinate;
            median = median/2; 
            return median ; 
        }
    }

}
