using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SEE.DataModel.DG;
using SEE.Game;
using UnityEngine.TestTools;
using SEE.Controls.Actions;

namespace SEE.Controls {

    public class DesktopNewNodeAction : MonoBehaviour
    {
        
        private bool is_innerNode = false;

        DesktopNewNodeAction(bool is_innerNode)
        {
            this.is_innerNode = is_innerNode;
        }
        // Start is called before the first frame update
        //temp save the new node
        //maybe i need graphrenderer.draw(gameObject)
        private Node node;
        private GameObject nodeRepresentation;
        private  SEECity city;
        private GraphRenderer graphRenderer;
        private bool is_selected = false;
        GameObject codeCityObject;
        NewNodeHoverAction newNodeHoverAction ;
        //Represents which kin of node should be created
        void Start()
        {
            
            //SceneQueries.GetCodeCity();
           gameObject.TryGetComponent<SEECity>(out city);
           graphRenderer = city.Renderer;
            newNodeHoverAction = gameObject.AddComponent<NewNodeHoverAction>();
        }

        // Update is called once per frame
        void Update()
        {
            if (!is_selected)
            {
                


                //Code to select the city
                //if you hover over a city with the mouse then highlight it yellow 
                
                if (Input.GetButtonDown("Fire1"))
                {
                    GameObject hovers = newNodeHoverAction.selectGameObject();
                    codeCityObject = SceneQueries.GetCodeCity(hovers.transform)?.gameObject;

                    
                    //on click save the GameObject of it to use the graph
                    is_selected = true;
                }
            }

            
            //Currently i can only add one new node because we need to persist the added node 
            //Eventuell hilft die Mehtode GetNode from ObjectManager
            //Later replace getKeyDown with menu action
            //Test for insert a new node

            // Do i need addGraph from Graph.cs ?  but than i need to set an ID for the node


            // NodeFactory -> NewBlock did this represents a new Grphic node?

            if (Input.GetKeyDown(KeyCode.N) && node == null)
            { //Eventuell statt key eine Box mit Bausteinen neben dem Tisch?
              //create new Node and let him stick to the cursor
                node = new Node();
                node.SourceName= "NEW-NODE";
                node.ItsGraph = city.LoadedGraph;
               //?? Muss ich das setzten? --> node.Type
                //graphRenderer.ToString(); //test output if graphrenderer works
                if (is_innerNode)
                {
                    nodeRepresentation = graphRenderer.NewInnerNode(node);


                }
                else
                {
                    nodeRepresentation = graphRenderer.NewLeafNode(node);
                }
                
                Debug.Log("NodeEditMODE TRUE\n");
            }


            if (node != null && nodeRepresentation != null)
            {
                Vector3 mp = Input.mousePosition;
                mp.z = 1;
                mp = Camera.main.ScreenToWorldPoint(mp);
                nodeRepresentation.transform.position = new Vector3(mp.x, 1, mp.z);

                if (Input.GetButtonDown("Fire1")) //Looks for a Left Klick
                {

                    GameObject hovers = newNodeHoverAction.selectGameObject();
                    Node tempNode;
                    //node.Parent = hovers.TryGetComponent<Node>(out tempNode);
                    Debug.Log("NodeEditModeLeftKlick\n");
                    //gets the mouse position
                    Debug.Log(Input.mousePosition);
                    //Place node and set newNode to Null
                    if (is_innerNode)
                    {
                        //todo enter name of node
                        
                    }
                    else
                    {
                       //enter name of node 
                    }
                    node = null;
                }

            }
            if (Input.GetKeyDown(KeyCode.Y) && node != null) //vlt später durch eine kill funciton ersetzten die über das menu dann aufgerufen wird
            {
                //exit node adding
                //remove node from cursor
                node = null;
                Debug.Log("NodeEditMODE FALSE\n");
                //DesktopInput.NodeCreationRequested()
            }
        }
    }
}
