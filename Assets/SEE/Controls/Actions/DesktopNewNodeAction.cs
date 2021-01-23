using UnityEngine;
using SEE.DataModel.DG;
using SEE.Game;
using System;
using SEE.GO;
using System.Collections.Generic;
using System.Linq;

namespace SEE.Controls
{

    public class DesktopNewNodeAction : DesktopNodeAction
    {
        /// <summary>
        /// The Code City in wich the node should be placed
        /// </summary>
        private SEECity city = null;

        /// <summary>
        /// The New GameObject
        /// </summary>
        private GameObject GONode = null;

        /// <summary>
        /// The Meta infos from the new node, set by the GUI
        /// 1. ID, 2. SourceName, 3. Type
        /// </summary>
        private Tuple<String, String, String> nodeMetrics = null;

        /// <summary>
        /// The script which instantiates the adding-node-canvas
        /// </summary>
        public CanvasGenerator canvasGenerator;

        /// <summary>
        /// true, if the addingnode-canvas is closed, else false.
        /// </summary>
        private static bool canvasIsActive = true;

        public static bool CanvasIsActive { get => canvasIsActive; set => canvasIsActive = value; }

        /// <summary>
        /// True, if the adding-node-canvas was opened, node-values was given and saved and the canvas was closed again, else false.
        /// </summary>
        private static bool valuesAreGiven = false;

        public static bool ValuesAreGiven { get => valuesAreGiven; set => valuesAreGiven = value; }

        /// <summary>
        /// True, if the node which will be created is an innerNode, else false
        /// </summary>
        private static bool isInnerNode = false;

        public static bool IsInnerNode { get => isInnerNode; set => isInnerNode = value; }

        /// <summary>
        /// The name of a node given in the input-Field.
        /// </summary>
        private static string nodename;

        public static string Nodename { get => nodename; set => nodename = value; }

        /// <summary>
        /// The type of a node given in the input-Field.
        /// </summary>
        private static string nodetype;

        public static string Nodetype { get => nodetype; set => nodetype = value; }

        /// <summary>
        /// Colour the hovered city is dyed when hovered or selected , set green by default 
        /// </summary>
        private Color defaultHoverCityColor = Color.green;

        public Color DefaultHoverCityColor { get => defaultHoverCityColor; set => defaultHoverCityColor = value; }

        /// <summary>
        /// Colour the hovered city is dyed to when hovered or selected in case the default colour is occupied , set black by default
        /// </summary>
        private Color alternativeHoverCityColor = Color.black;

        public Color AlternativeHoverCityColor { get => alternativeHoverCityColor; set => alternativeHoverCityColor = value; }

        /// <summary>
        /// A list the hovered GameObjects are stored in.
        /// </summary>
        private List<GameObject> hoveredObjectList = new List<GameObject>();

        /// <summary>
        /// A list the colors of the hovered GameObjects are stored in.
        /// </summary>
        private List<Color> listOfColors = new List<Color>();

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
        /// The colour the graphs leafs graphical representation is dyed, set red by default . // To be changed when the metrics are considered
        /// </summary>
        private Color leafColor = Color.red;

        /// <summary>
        /// The colour the graphs inner nodes graphical representation is dyed, set white by default . // To be changed when the metrics are considered
        /// </summary>
        private Color innerNodeColor = Color.white;

        /// <summary>
        /// A list of roots stores at least the single root of a graph, more in the special case the graph has more than one root.
        /// </summary>
        private List<GameObject> listOfRoots = null;


        public void Start()
        {
            listOfRoots = new List<GameObject>();
            InitialiseCanvasObject();
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
                AddingNodeCanvasScript script = canvasObject.GetComponent<AddingNodeCanvasScript>();
                script.GetNodeMetrics();
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
            if (hoveredObject != null && hoveredObjectList.Count > 0 && hoveredObject.gameObject.GetComponent<Renderer>().material.color != defaultHoverCityColor || hoveredObject == null && hoveredObjectList.Count > 0)
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

                //Gets the SEECity from the hoverdObject
                SceneQueries.GetCodeCity(hoveredObject.transform)?.gameObject.TryGetComponent<SEECity>(out city);
                GetNodesOfScene();
                if (dir_Root != null)
                {
                    foreach (GameObject root in listOfRoots)
                    {
                        if (CheckNodeAndGraph(root, city.LoadedGraph))
                        {
                            ChangeColor(root, Color.white);
                        }
                    }
                }
            }
        }

        // <summary>
        /// Undyes the the current colour of the object, i.e. changes the color of to its original color.
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
        /// Dyes the hoveredObject either in the defaulthoverCitycolor or in the alternativeHoverCityColor in case
        /// the object is already dyed in that color.
        /// The colors are about to be set by the user itself in the inspector or via GUI.
        /// </summary>
        private void ChangeColor(GameObject objectToDye, Color colorOfCity)
        {
            Color newCityColor = new Color();
            if (colorOfCity == defaultHoverCityColor)
            {
                newCityColor = alternativeHoverCityColor;
            }
            else
            {
                newCityColor = defaultHoverCityColor;
            }
            if (!(hoveredObjectList.Contains(objectToDye)))
            {
                hoveredObjectList.Add(objectToDye);
            }
            listOfColors.Add(objectToDye.gameObject.GetComponent<Renderer>().material.color);
            objectToDye.gameObject.GetComponent<Renderer>().material.color = newCityColor;
        }

        /// <summary>
        /// Creates a randomized string, i.e. the id for the created node
        /// </summary>
        private static string RandomizeString()
        {
            return Utils.RandomStrings.Get();
        }

        /// <summary>
        /// Adds a node to the loadedGraph and creats its ID. Repeats the process in case the generated ID is not unique.
        /// Precondition: The parameter may be null, it is checked again before beeing added to the loadedGraph.
        /// </summary>
        /// /// <param name="newNodeType">The node to add to the graph</param>
        private void AddNode(Node node)
        {
            if (node == null)
            {
                return;
            }
            try
            {
                city.LoadedGraph.AddNode(node);
            }
            catch (Exception)
            {
                node.ID = RandomizeString();
                AddNode(node);
                return;
            }
        }

        /// <summary>
        /// Creates a new node
        /// </summary>
        private void NewNode()
        {
            GameObject gameNode;
            Node node = new Node
            {
                //Set the metrics of the new node
                ID = nodeMetrics.Item1,
                SourceName = nodeMetrics.Item2,
                Type = nodeMetrics.Item3
            };

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
                    new NewNodeNetAction(city, isInnerNode,nodeMetrics.Item1, nodeMetrics.Item2, nodeMetrics.Item3, GONode.transform.position).Execute(null);
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
        public override void RemoveScript()
        {
            if (GONode != null)
            {
                Place();
            }

            base.RemoveScript();
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

            //List with the graphs roots or most likely only a single root of the specific city.
            List<Node> rootList = city.LoadedGraph.GetRoots();

            // In the special case the graph only consists of one leaf, we will have to check in the list of all leafs, which has the count of one in that case,
            // if there is the root node.
            if (allLeafsInScene.Count == 1 && allInnerNodesInScene.Count == 0)
            {
                dir_Root = RootSearch(allLeafsInScene, rootList);

            }

            // Search for the graphs root in the set of all inner nodes.
            dir_Root = RootSearch(allInnerNodesInScene, rootList);


            // Fill the lists with the specific lossyscales of all the nodes, either leafs or innernodes.
            leafSize = ListOfLossyscale(allLeafsInScene);
            innerNodeSize = ListOfLossyscale(allInnerNodesInScene);

            // Calculate the median of the specific sets, either leafs or innernodes. 
            medianOfLeaf = MathFunctions.CalcMedian(leafSize);
            medianOfInnerNode = MathFunctions.CalcMedian(innerNodeSize);

            // In the special case there are no inner nodes, the median of the graphs only leaf is set 
            // as a default value for any inner node that might be created.
            if (innerNodeSize.Count == 0)
            {
                medianOfInnerNode = medianOfLeaf;
            }
        }

        /// <summary>
        /// Iterates the list of gameobjects and adds the lossyscale of the given objects to a list. 
        /// </summary>
        /// <param name="pListOfGameObjects">A List of GameObjects</param>
        /// <returns> Returns a  vector list filled with the lossyscale of the param pListOfGameObject or null in case the list is empty or
        ///  the given object is null</returns>
        private List<Vector3> ListOfLossyscale(ICollection<GameObject> pListOfGameObjects)
        {
            if (pListOfGameObjects == null | pListOfGameObjects.Count == 0)
            {
                return null;
            }
            List<Vector3> lossyScaleList = new List<Vector3>();
            foreach (GameObject go in pListOfGameObjects)
            {
                //to specify if the specific node belongt to the specific graph.
                if (CheckNodeAndGraph(go, city.LoadedGraph))
                {
                    lossyScaleList.Add(go.transform.lossyScale);
                }
            }
            return lossyScaleList;
        }

        /// <summary>
        /// Specifies whether the node representation of a given gameObject belongs to the certain graph.
        /// </summary>
        /// <param name="pGameObject"></param>
        /// <param name="g"></param>
        /// <returns true, if graph belongs to the gameObject represented as a node, else false </returns>
        public static bool CheckNodeAndGraph(GameObject pGameObject, Graph g)
        {
            if (pGameObject == null || g == null)
            {
                return false;
            }

            return pGameObject.GetComponent<NodeRef>().node.ItsGraph == g;
        }

        /// <summary>
        /// Search for a rootNode in a given list of Gameobjects. 
        /// </summary>
        /// <param name="pListOfGameNodes"></param>
        /// <returns> The rootnode as gameObject in case the root is found, else dir_root (which can be null).</returns>
        private GameObject RootSearch(ICollection<GameObject> pListOfGameNodes, List<Node> pListofRoots)
        {
            Node rootTmp;

            ///In the special case a graph has not only a single root, we would have to iterate the list of Roots in order to 
            ///compare the GameObject and search. 
            foreach (Node root in pListofRoots)
            {
                Node rootOfCity = root;
                foreach (GameObject rootSearchItem in pListOfGameNodes)
                {
                    rootTmp = rootSearchItem.GetComponent<NodeRef>().node;
                    if (rootTmp.IsRoot() && rootTmp == rootOfCity && !(rootTmp == null))
                    {
                        listOfRoots.Add(rootSearchItem);
                        dir_Root = rootSearchItem;
                    }
                }
            }

            return dir_Root;
        }



        /// <summary>
        /// Sets the param IsInnerNode for Network Use
        /// </summary>
        /// <param name="IsInnerNode">Should the new node be a inner or not</param>
        public void SetIsInnerNode(bool IsInnerNode)
        {
            isInnerNode = IsInnerNode;
        }

        /// <summary>
        /// Set the City for Network Use
        /// </summary>
        /// <param name="cit">the city</param>
        public void SetCity(SEECity City)
        {
            city = City;
        }

        /// <summary>
        /// Sets the Node Metrics for NEtwork Use
        /// </summary>
        /// <param name="NodeMetrics">the node metrics</param>
        public void SetNodeMetrics(string NodeMetrics1, string NodeMetrics2, string NodeMetrics3)
        {
            nodeMetrics = new Tuple<string, string, string> ( NodeMetrics1,NodeMetrics2,NodeMetrics3);
        }

        /// <summary>
        /// For Network Use Only, creates the new node on the Clients
        /// </summary>
        /// <param name="position">The position of the new node</param>
        public void NetworkNewNode(Vector3 position)
        {
            valuesAreGiven = true;
            NewNode();
           // GameNodeMover.FinalizePosition(GONode, position);
           // GONode = null;
           // RemoveScript();
        }
    }

}

