using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SEE.DataModel.DG;
using SEE.Game;
using UnityEngine.TestTools;

namespace SEE.Controls {
    public class DesktopNewNodeAction : MonoBehaviour
    {
        // Start is called before the first frame update
        //temp save the new node
        //maybe i need graphrenderer.draw(gameObject)
        Node node;
        GameObject nodeRepresentation;
        SEECity city;
        GraphRenderer graphRenderer;
        //Represents which kin of node should be created
        bool is_innerNode = false;
        void Start()
        {
            //gameObject.AddComponent(SEECity());
           
           gameObject.TryGetComponent<SEECity>(out city);
           graphRenderer = new GraphRenderer(city, city.LoadedGraph);
        }

        // Update is called once per frame
        void Update()
        {
            
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
                //graphRenderer.ToString(); //test output if graphrenderer works
                nodeRepresentation = graphRenderer.NewLeafNode(node);
                Debug.Log("NodeEditMODE TRUE\n");
            }


            if (node != null && nodeRepresentation != null)
            {
                Vector3 mp = Input.mousePosition;
                mp.z = 1;
                mp = Camera.main.ScreenToWorldPoint(mp);
                nodeRepresentation.transform.position = new Vector3(mp.x, 1, mp.z);

                //iF b is pressed change the node kind, later this will be removed and replaced with the menu action where you can choose wich node you want to add
                if (Input.GetKeyDown(KeyCode.B))

                {
                    if (is_innerNode)
                    {
                        is_innerNode = false;
                        nodeRepresentation = graphRenderer.NewLeafNode(node);

                    }
                    else
                    {
                        is_innerNode = true;
                        nodeRepresentation = graphRenderer.NewInnerNode(node);
                    }
                }
                if (Input.GetButtonDown("Fire1")) //Looks for a Left Klick
                {
                    Debug.Log("NodeEditModeLeftKlick\n");
                    //gets the mouse podsition
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
            if (Input.GetKeyDown(KeyCode.Y) && node != null)
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
