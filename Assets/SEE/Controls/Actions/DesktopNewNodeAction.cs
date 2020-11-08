using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SEE.DataModel.DG;
using SEE.Game;

namespace SEE.Controls {
    public class DesktopNewNodeAction : MonoBehaviour
    {
        // Start is called before the first frame update
        //temp save the new node
        Node newNode;
        GameObject nodeRepresentation;
        static SEECity city = new SEECity();
        GraphRenderer graphRenderer = new GraphRenderer(city, city.LoadedGraph);
        private const int LeftMouseButton = 1;
        //Represents which kin of node should be created
        bool is_innerNode = false;
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            //Eventuell hilft die Mehtode GetNode from ObjectManager
            //Later replace getKeyDown with menu action
            //Test for insert a new node

            // Do i need addGraph from Graph.cs ?  but than i need to set an ID for the node


            // NodeFactory -> NewBlock did this represents a new Grphic node?

            if (Input.GetKeyDown(KeyCode.N) && newNode == null)
            { //Eventuell statt key eine Box mit Bausteinen neben dem Tisch?
              //create new Node and let him stick to the cursor
                newNode = new Node();
                graphRenderer.NewLeafNode(newNode);
                Debug.Log("NodeEditMODE TRUE\n");
            }
            if (newNode != null)
            {
                if (Input.GetKeyDown(KeyCode.B))

                {
                    if (is_innerNode)
                    {
                        is_innerNode = false;


                    }


                    //Change node type to the next in the list
                }
                if (Input.GetMouseButton(LeftMouseButton))
                {
                    Debug.Log("NodeEditModeLeftKlick\n");
                    //Place node and set newNode to Null
                    if (is_innerNode)
                    {
                        graphRenderer.NewInnerNode(newNode);

                    }
                    else
                    {
                        graphRenderer.NewLeafNode(newNode);
                    }
                }
            }
            if (Input.GetKeyDown(KeyCode.N) && newNode != null)
            {
                //exit node adding
                //remove node from cursor
                newNode = null;
                //DesktopInput.NodeCreationRequested()
            }
        }
    }
}
