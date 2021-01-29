using UnityEngine;
using SEE.DataModel.DG;
using SEE.Game;
using System;
using SEE.GO;
using System.Collections.Generic;
using System.Linq;
using SEE.Utils;

namespace SEE.Controls
{

    public class NewNodeAction : NodeAction
    {
        /// <summary>
        /// The Code City in wich the node should be placed
        /// </summary>
        private SEECity city = null;

        public SEECity City { get => city; set => city = value; }

        /// <summary>
        /// The New GameObject which contains the new Node
        /// </summary>
        private GameObject GONode = null;

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
        /// The median of the lossyscale of the graphs leafs  
        /// </summary>
        private Vector3 medianOfLeaf;

        /// <summary>
        /// The median of the lossyscale of the graphs inner nodes.
        /// </summary>
        private Vector3 medianOfInnerNode;

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

        /// <summary>
        /// True, if allNodesOfScene() is called, else false.
        /// </summary>
        private bool nodesLoaded = false;

        /// <summary>
        /// The current hovered city, which has to be colored while selecting a city-process for creating a new node.
        /// </summary>
        private SEECity cityToDye = null;

        public enum Progress
        {
            NoCitySelected,
            CityIsSelected,
            WaitingForValues,
            CanvasIsClosed,
            ValuesAreGiven,
            AddingIsCanceled
        }

        /// <summary>
        /// The specific state of the progress of adding a node to the implementation.
        /// </summary>
        private Progress progress = Progress.NoCitySelected;

        public Progress Progress1 { get => progress; set => progress = value; }

        public void Start()
        {
            listOfRoots = new List<GameObject>();
            InitialiseCanvasObject();
        }

        /// <summary>
        /// The update-method interacts in dependency of the progress-state. (sequencial series)
        /// NoCitySelected: Searching for a selected city, where a node can be added
        /// CityIsSelected: Opens a canvas-object where values for the node can be added
        /// WaitingForValues: This State is active while the canvas is open, but the button "AddNode" on it is not pushed
        /// CanvasIsClosed: Closes the canvas-object after extracting the values for the creation of a node. This state is reached by pushing the "AddNode"-Button
        /// ValuesAreGiven: Moves the node and waits for a mouseInput to place the node, if its inside of the previous chosen city
        /// AddingIsCanceled: Removes all attributes and states and resets the progress-state to noCitySelected
        /// 
        /// </summary>
        public void Update()
        {
            switch (Progress1)
            {
                case Progress.NoCitySelected:
                    SelectCity();
                    if (city != null)
                    {
                        Progress1 = Progress.CityIsSelected;

                    }
                    break;

                case Progress.CityIsSelected:

                    OpenDialog();
                    Progress1 = Progress.WaitingForValues;
                    break;

                case Progress.CanvasIsClosed:

                    //Removes the canvasObject and extracts the values from it to create a new node with these params.
                    CanvasGenerator c = canvasObject.GetComponent<CanvasGenerator>();
                    AddingNodeCanvasAction script = canvasObject.GetComponent<AddingNodeCanvasAction>();
                    script.GetNodeMetrics();
                    c.DestroyAddNodeCanvas();
                    Progress1 = Progress.ValuesAreGiven;
                    break;

                case Progress.ValuesAreGiven:

                    if (GONode == null)
                    {
                        NewNode();
                        nodesLoaded = false;
                        GameNodeMover.MoveTo(GONode);
                       // Tweens.Move(GONode, new Vector3(GONode.transform.position.x, GONode.transform.position.y + 0.4f, GONode.transform.position.z), 0.6f);
                    }
                    else
                    {
                        GameNodeMover.MoveTo(GONode);
                        if (Input.GetMouseButtonDown(0))
                        {
                            Place();

                            Progress1 = Progress.NoCitySelected;
                        }
                    }
                    break;

                case Progress.AddingIsCanceled:
                    c = canvasObject.GetComponent<CanvasGenerator>();
                    script = canvasObject.GetComponent<AddingNodeCanvasAction>();
                    script.GetNodeMetrics();
                    c.DestroyAddNodeCanvas();
                    city = null;
                    Progress1 = Progress.NoCitySelected;
                    break;
            }
        }

        /// <summary>
        /// Selects the City with hovering. Sets the City Object on Click on a GameObject.
        /// While there is no city selected by mousceclick, the rootList of the current hovered city will be colored in a different color
        /// to support the selection-process visually.
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
                if (!nodesLoaded)
                {
                    SceneQueries.GetCodeCity(hoveredObject.transform)?.gameObject.TryGetComponent<SEECity>(out cityToDye);
                    GetNodesOfScene();
                }

                foreach (GameObject go in listOfRoots)
                {
                    if (go.GetComponent<NodeRef>().Value.ItsGraph == cityToDye.LoadedGraph)
                    {
                        ChangeColor(go, go.GetComponent<Renderer>().material.color);
                    }
                }

            }
            nodesLoaded = false;
            if (hoveredObject != null && Input.GetMouseButtonDown(0))
            {
                //Gets the SEECity from the hoverdObject
                SceneQueries.GetCodeCity(hoveredObject.transform)?.gameObject.TryGetComponent<SEECity>(out city);

                foreach (GameObject root in listOfRoots)
                {
                    if (root.GetComponent<NodeRef>().Value.ItsGraph == city.LoadedGraph)
                    {
                        ChangeColor(root, root.GetComponent<Renderer>().material.color);
                    }
                }
            }
        }

        /// <summary>
        /// Opens a dialog-canvas where the user can insert some node-metrics. Therefore, a CanvasGenerator-script-component 
        /// will be added to this gameObject which contains the canvas as a gameObject-instance of a prefab.
        /// </summary>
        public void OpenDialog()
        {
            CanvasGenerator c = canvasObject.GetComponent<CanvasGenerator>();
            c.InstantiateAddingNodeCanvas();
        }

        /// <summary>
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
        /// Dyes the hoveredObject either in the defaulthoverCitycolor or in the alternativeHoverCityColor in case
        /// the object is already dyed in that color.
        /// The colors are about to be set by the user itself in the inspector or via GUI.
        /// </summary>
        /// <param name="colorOfCity">the default-color of the city.</param>
        /// <param name="objectToDye"> the object which will get dyed</param>
        private void ChangeColor(GameObject objectToDye, Color colorOfCity)
        {
           Color newCityColor = defaultHoverCityColor;

        //    if (colorOfCity == defaultHoverCityColor)
        //    {
        //        newCityColor = alternativeHoverCityColor;
        //    }
        //    else
        //    {
        //        newCityColor = defaultHoverCityColor;
        //    }
            if (!(hoveredObjectList.Contains(objectToDye)))
            {
                hoveredObjectList.Add(objectToDye);
            }
            listOfColors.Add(objectToDye.gameObject.GetComponent<Renderer>().material.color);
            objectToDye.gameObject.GetComponent<Renderer>().material.color = newCityColor;
        }

        /// <summary>
        /// Adds a node to the loadedGraph and creates its ID. Repeats the process in case the generated ID is not unique.
        /// </summary>
        /// <param name="node">The node to add to the graph</param>
        private void AddNode(Node node)
        {
            try
            {
                city.LoadedGraph.AddNode(node);
            }
            catch (Exception)
            {
                node.ID = RandomStrings.Get();
                AddNode(node);
                return;
            }
        }

        /// <summary>
        /// Creates a new node. First, it is created with default-values which will be replaced by inputValues by the user if they are given.
        /// Sets this node in the hierachy of the graph. 
        /// </summary>
        private void NewNode()
        {
            GameObject gameNode;
            System.Random rnd = new System.Random();
            Node node = new Node
            {
                //Set the metrics of the new node
                ID = RandomStrings.Get(),
                SourceName = "Node" + rnd.Next(0, 999999999),
                Type = "Type" + rnd.Next(0, 999999999)
            };

            AddNode(node);

            //Redraw the node Graph
            city.LoadedGraph.FinalizeNodeHierarchy();

            GraphRenderer graphRenderer = city.Renderer;

            if (isInnerNode)
            {
                gameNode = graphRenderer.NewInnerNode(node);
                GONode = gameNode;
                GONode.transform.localScale = medianOfInnerNode;
                GONode.gameObject.GetComponent<Renderer>().material.color = innerNodeColor;
            }
            if (!isInnerNode)
            {
                gameNode = graphRenderer.NewLeafNode(node);
                GONode = gameNode;
                GONode.transform.localScale = medianOfLeaf;
                GONode.gameObject.GetComponent<Renderer>().material.color = leafColor;
            }

            
          
            GONode.transform.position = hoveredObjectList.ElementAt(hoveredObjectList.Count - 1).transform.position;
            GONode.gameObject.GetComponent<Collider>().enabled = false;
            
            GameNodeMover.MoveTo(GONode);
        }

        /// <summary>
        /// Places a node on call and checks if the city is the preselected one. Before this node will be placed, 
        /// the default-values will be replaced by the users input, if it is given and they are not only whitespaces. 
        /// </summary>
        private void Place()
        {
            Node node = GONode.GetComponent<NodeRef>().Value;

            if (nodename.Trim().Length != 0)
            {
                node.SourceName = nodename;
            }
            if (nodetype.Trim().Length != 0)
            {
                node.Type = nodetype;
            }

            SEECity cityTmp;

            ///checks if the currently hovered object is part of the preselected city
            GameObject tmp = SceneQueries.GetCodeCity(hoveredObject.transform)?.gameObject;
            try
            {
                tmp.TryGetComponent<SEECity>(out cityTmp);
            }
            catch (Exception)
            {
                Debug.Log("city not selected");
                return;
            }
            if (cityTmp != null && city.Equals(cityTmp))
            {
                GONode.gameObject.GetComponent<Collider>().enabled = true;
                GameNodeMover.FinalizePosition(GONode, GONode.transform.position);

                new NewNodeNetAction(hoveredObject.name, isInnerNode, node.ID, node.SourceName, node.Type, GONode.transform.position, GONode.transform.lossyScale, GONode.transform.parent.gameObject.name).Execute(null);
            }
            else
            {
                Destroy(GONode);
            }

            GONode = null;
            city = null;
            nodesLoaded = false;
            cityToDye = null;

        }

        /// <summary>
        /// Removes this Script.
        /// Places the new Node if it is not placed
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
        /// Gets all Nodes of the scene and saves them inside of Collections - one InnerNode and one Leaf.
        /// If there is just one Node, it is the root automatically. 
        /// Else, this method searchs for all roots of the scene, too. 
        /// Finally, the median of all nodes will calculated by the lists of Lossyscale of the gameObject-Nodes 
        /// for constructing the node with a default-size.
        /// </summary>
        public void GetNodesOfScene()
        {
            List<Vector3> leafSize;
            List<Vector3> innerNodeSize;

            ICollection<GameObject> allLeafsInScene = SceneQueries.AllGameNodesInScene(true, false);
            ICollection<GameObject> allInnerNodesInScene = SceneQueries.AllGameNodesInScene(false, true);

            List<Node> rootList = cityToDye.LoadedGraph.GetRoots();

            //It is nessecary to find the GameObjects, which are containing the specific root-nodes.
            listOfRoots = RootSearch(allInnerNodesInScene, allLeafsInScene, rootList);

            //Lists of the gameObject-sizes of the nodes
            leafSize = ListOfLossyscale(allLeafsInScene);
            innerNodeSize = ListOfLossyscale(allInnerNodesInScene);

            medianOfLeaf = MathFunctions.CalcMedian(leafSize);
            medianOfInnerNode = MathFunctions.CalcMedian(innerNodeSize);

            // In the special case there are no inner nodes, the median of the graphs only leaf is set 
            // as a default value for any inner node that might be created.
            if (innerNodeSize.Count == 0)
            {
                medianOfInnerNode = medianOfLeaf;
            }

            nodesLoaded = true;
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
                //to specify if the specific node belong to the specific graph. 
                if (go.GetComponent<NodeRef>().Value.ItsGraph == cityToDye.LoadedGraph)
                {
                    lossyScaleList.Add(go.transform.lossyScale);
                }
            }
            return lossyScaleList;
        }

        /// <summary>
        /// Search for all rootNode-GameObjects in the given lists of Gameobjects. 
        /// Special cases for the root-search: 
        /// There is no node in the scene -> root = null
        /// There is just one node in the scene -> root is this node
        /// There are multipleRoots in the graph.
        /// There is exactly one root in the graph
        /// </summary>
        /// <param name="listofLeafs">A collection of all leafs in a loaded scene</param>
        /// <param name="listOfInnerNodes">A collection of all InnerNodes in a loaded scene</param>
        /// <param name="pListofRoots">A list of all root-nodes in a loaded scene</param>
        /// <returns>A list with all root-GameObjects in the loaded scene</returns>
        private List<GameObject> RootSearch(ICollection<GameObject> listOfInnerNodes, ICollection<GameObject> listofLeafs, List<Node> pListofRoots)
        {
            List<GameObject> listsOfRoot = new List<GameObject>();

            if (listofLeafs.Count == 1 && listOfInnerNodes.Count == 0)
            {
                listsOfRoot.Add(listofLeafs.ElementAt(0));
            }

            if (listofLeafs.Count == 0 && listOfInnerNodes.Count == 0)
            {
                listsOfRoot = null;
            }

            foreach (Node root in pListofRoots)
            {
                Node rootOfCity = root;
                Node rootTmp;

                foreach (GameObject rootSearchItem in listOfInnerNodes)
                {
                    rootTmp = rootSearchItem.GetComponent<NodeRef>().Value;
                    if (rootTmp.IsRoot() && rootTmp == rootOfCity && !(rootTmp == null))
                    {
                        listsOfRoot.Add(rootSearchItem);
                    }
                }
                foreach (GameObject rootSearchItem in listofLeafs)
                {
                    rootTmp = rootSearchItem.GetComponent<NodeRef>().Value;
                    if (rootTmp.IsRoot() && rootTmp == rootOfCity && !(rootTmp == null))
                    {
                        listsOfRoot.Add(rootSearchItem);
                    }
                }
            }
            return listsOfRoot;
        }

        /// <summary>
        /// For Network Use Only, creates the new node on all other clients.
        /// </summary>
        /// <param name="position"> The position of the new node</param>
        /// <param name="parentID">The id of the new GameObject</param>
        /// <param name="scale">the size of the new GameObject</param>
        public void NetworkNewNode(Vector3 position, Vector3 scale, string parentID)
        {
            NewNode();
            GONode.SetScale(scale);
            GameNodeMover.NetworkFinalizeNodePosition(GONode, parentID, position);
            GONode = null;
            RemoveScript();
        }

        /// <summary>
        /// Setter for the Static isInnerNode-variable
        /// </summary>
        /// <param name="isInnerNode"></param>
        public void SetIsInnerNode(bool isInnerNode)
        {
            IsInnerNode = isInnerNode;
        }
    }
}

