using Assets.SEE.DataModel;
using OdinSerializer;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Game.Runtime
{
    public class JLGVisualizer : SerializedMonoBehaviour
    {
        /// <summary>
        /// A ParsedJLG object that contains a parsed JLG file. This object contains all information needed for the visualization of a debugging process.
        /// </summary>
        public ParsedJLG parsedJLG;

        /// <summary>
        /// Int value the represents the index of the current active statement. All indices can be found in this.parsedJLG.allStatements.
        /// The total number of indices is this.parsedJLG.allStatements.Count.
        /// </summary>
        private int statementCount = 0;

        /// <summary>
        /// Time value in seconds. At this point in time(running time) the next or previous statement will be visualized, depending on the playing direction.
        /// </summary>
        private float nextUpdateTime = 1.0f;

        /// <summary>
        /// Seconds per statement.
        /// </summary>
        private float updateIntervall = 1.0f;

        /// <summary>
        /// true, when visualisation is running. false, when its paused. (Pause visualization by pressing 'p')
        /// </summary>
        private Boolean running = true;

        /// <summary>
        /// Describes the direction, in which the visualisation is running. True for forward, false for backwards.
        /// </summary>
        private Boolean playDirection = true;

        /// <summary>
        /// The GameObject that represents the class, to which the current statement belongs to. 
        /// </summary>
        public GameObject currentGO;

        /// <summary>
        /// A list of all active GameObjects that are tagged with 'Node'.
        /// </summary>
        private GameObject[] nodesGOs;

        /// Start is called before the first frame update
        void Start()
        {
            if (parsedJLG == null) {
                throw new Exception("Parsed JLG is null!");
            }
            nodesGOs = GameObject.FindGameObjectsWithTag("Node");
            ///Sets the currentGO to be the node representing the Class of the first Statement in preperation.
            if(nodesGOs == null)
            {
                throw new Exception("There are no Nodes");
            }
            currentGO = GetNodeForStatement(statementCount);
        }

        /// Update is called once per frame
        void Update()
        {            
            if (Time.time >= nextUpdateTime)
            {
               if (running)
               {
                    if (!NodeRepresentsStatementLocation(statementCount, currentGO))
                    {
                        currentGO = GetNodeForStatement(statementCount);
                        //Hier Textfeld generator einsetzen
                    }
                    if (playDirection)
                    {
                        NextStatement();
                    }
                    else
                    {                        
                        PreviousStatement();
                    }
               }
               nextUpdateTime += updateIntervall;               
            }            

            if (Input.GetKeyDown(KeyCode.P))
            {
                running = !running;
            }
            if (Input.GetKeyDown(KeyCode.O))
            {
                updateIntervall = 1;
                playDirection = !playDirection;
            }
            if (running)
            {
                if (Input.GetKeyDown(KeyCode.L))
                {
                    SpeedUp();
                }
                if (Input.GetKeyDown(KeyCode.K))
                {
                    SlowDown();
                }
            }
        }

        /// <summary>
        /// Gets a GameObject tagged with Node from this objects list of Node GameObjects that matches the location of the Statement represented by i.
        /// </summary>
        /// <param name="i">the index of the statement in this parsedJLG.allstatements</param>
        /// <returns></returns>
        private GameObject GetNodeForStatement(int i) {
            foreach (GameObject go in nodesGOs)
            {
                if (NodeRepresentsStatementLocation(i, go))
                {
                    return go;
                }
            }
            return null;
        }

        /// <summary>
        /// Tests if a GameObject is a node and the class, it is representing, is the class of the given statement.
        /// </summary>
        /// <param name="i">index of the javastatement in parsedJLGs JavaStatement list</param>
        /// <param name="go">the GameObject</param>
        /// <returns></returns>
        private Boolean NodeRepresentsStatementLocation(int i, GameObject go)
        {
            return parsedJLG.GetStatementLocationString(i).StartsWith(go.name)&&go.tag.Equals("Node");
        }

        /// <summary>
        /// Visualizes the current statement and then increases statementCount by 1.
        /// </summary>
        private void NextStatement() {
            Debug.Log(parsedJLG.AllStatements[statementCount].Line + " " + parsedJLG.GetStatementLocationString(statementCount)+" CurrentGo:"+ currentGO.name);
            if (statementCount < parsedJLG.AllStatements.Count-1)
            {
                statementCount++;
            }
            else
            {
                Debug.Log("End of JLG reached. Press 'P' to start playing backwards.");
                running = false;
                playDirection = false;
            }
        }

        /// <summary>
        /// Visualizes the current statement and then decreases the statementCount by 1.
        /// </summary>
        private void PreviousStatement() {
            Debug.Log(parsedJLG.AllStatements[statementCount].Line+ " "+parsedJLG.GetStatementLocationString(statementCount) + " CurrentGo:" + currentGO.name);
            if (statementCount > 0)
            {
                statementCount--;
            }
            else
            {
                Debug.Log("Start of JLG reached. Press 'P' to start.");
                running = false;
                playDirection = true;
            }
        }

        /// <summary>
        /// Speeds up the playing speed of the visualization by x2 until a max of updating every 0.03125 seconds or 32 statements per second.
        /// </summary>
        private void SpeedUp()
        {
            if (updateIntervall > 0.03125) {
                nextUpdateTime = nextUpdateTime - updateIntervall + (updateIntervall / 2);
                updateIntervall = updateIntervall / 2;
            }
        }

        /// <summary>
        /// Slows down the playing speed of the visualization by x2 until a minimum of updating every 8 seconds or 1 statement every 8 seconds.
        /// </summary>
        private void SlowDown()
        {
            if (updateIntervall < 8) {
                nextUpdateTime = nextUpdateTime - updateIntervall + (updateIntervall * 2);
                updateIntervall = updateIntervall * 2;
            }
        }
    }
}
