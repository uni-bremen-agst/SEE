using Assets.SEE.DataModel;
using OdinSerializer;
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
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
        private int statementCounter = 0;

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
        private Boolean running = false;

        /// <summary>
        /// Describes the direction, in which the visualisation is running. True for forward, false for backwards.
        /// </summary>
        private Boolean playDirection = true;

        /// <summary>
        /// 
        /// </summary>
        private Boolean windowsToggled = true;  

        /// <summary>
        /// The GameObject that represents the class, to which the current statement belongs to. 
        /// </summary>
        private GameObject currentGO;

        /// <summary>
        /// A list of all active GameObjects that are tagged with 'Node'.
        /// </summary>
        private GameObject[] nodesGOs;

        /// <summary>
        /// 
        /// </summary>
        private Stack<GameObject> textWindows = new Stack<GameObject>();

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
            currentGO = GetNodeForStatement(statementCounter);
            GenerateScrollableTextWindow();
        }

        /// Update is called once per frame
        void Update()
        {            
            if (Time.time >= nextUpdateTime)
            {
               if (running)
               {
                    ///Check if currentGo is not GO of current Statement. If true change currentGO and generate TextWindow.
                    if (!NodeRepresentsStatementLocation(statementCounter, currentGO))
                    {
                        currentGO = GetNodeForStatement(statementCounter);
                        if (! textWindowForNodeExists(currentGO))
                        {
                            GenerateScrollableTextWindow();
                        }
                        ToggleTextWindows();
                    }
                    if (playDirection)
                    {
                        ///If previous statement exitted a class, metaphorically remove the class from the callstack by destroying its textwindow.
                        if (statementCounter > 0 
                            && parsedJLG.AllStatements[statementCounter - 1].StatementType.Equals("exit") 
                            && currentGO != GetNodeForStatement(statementCounter - 1))
                        {
                            GameObject.Destroy(textWindows.Pop());
                        }
                        NextStatement();
                    }
                    else
                    {
                        ///If previous statement exitted a class, metaphorically remove the class from the callstack by destroying its textwindow.
                        ///Looking for an "entrystatement", because the visualisation is running backwards.
                        if (statementCounter < parsedJLG.AllStatements.Count
                            && parsedJLG.AllStatements[statementCounter + 1].StatementType.Equals("entry")
                            && currentGO != GetNodeForStatement(statementCounter + 1))
                        {
                            GameObject.Destroy(textWindows.Pop());
                        }
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
            if (Input.GetKeyDown(KeyCode.J))
            {
                windowsToggled = !windowsToggled;
                ToggleTextWindows();
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

        private bool textWindowForNodeExists(GameObject node)
        {
            bool exists = false;
            if (! (textWindows.Count == 0))
            {
                foreach (GameObject go in textWindows)
                {
                    if (go.name.Equals(node.name + "FileContent"))
                    {
                        exists = true;
                        break;
                    }
                }
            }
            return exists;
        }

        /// <summary>
        /// This Method generates a new ScrollableTextMeshProWindow above the middle of the currentGO Node. Also it fills the Textfield with the code saved in the classfile,
        /// that belongs to this node.
        /// </summary>
        private void GenerateScrollableTextWindow()
        {
            Vector3 v = currentGO.transform.position;
            GameObject go = Instantiate((GameObject)Resources.Load("ScrollableTextWindow"),v,currentGO.transform.rotation,this.gameObject.transform.parent);
            go.name = currentGO.name + "FileContent";
            ///set canvas order in layer to textwindows.count damit die fenster voreinander gerendered werden
            go.transform.GetChild(0).GetChild(0).GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text = GetFileContentForNode(currentGO);
            //go.transform.Find("ScrollContainer").Find("TextContainer").Find("Text(TMP)").GetComponent<TextMeshProUGUI>().text = GetFileContentForNode(currentGO);
            textWindows.Push(go);            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="go"></param>
        /// <returns></returns>
        private string GetFileContentForNode(GameObject go)
        {
            string classname = go.name;
            if (classname.Contains(".")) {
                classname = classname.Substring(classname.LastIndexOf(".") + 1);
            }
            classname = classname + ".java";
            foreach (string p in parsedJLG.FilesOfProject)
            {
                if (p.EndsWith(classname))
                {
                    return File.ReadAllText(p);
                }
            }
            return null;
        }

        /// <summary>
        /// Gets a GameObject tagged with Node from this objects list of GameObjects that matches the location of the Statement represented by i.
        /// A Node matches the location of a statement, when the classname of the location equals the nodes name.
        /// </summary>
        /// <param name="i">the index of the statement in this parsedJLG.allstatements</param>
        /// <returns>Node for Statement, if exists, else null.</returns>
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
        /// Visualizes the current statement and then increases statementCounter by 1.
        /// </summary>
        private void NextStatement() {
            Debug.Log(parsedJLG.AllStatements[statementCounter].Line + " " + parsedJLG.GetStatementLocationString(statementCounter)+" CurrentGo:"+ currentGO.name);

            HighlightCurrentLineFadePrevious();

            GameObject fileContent = GameObject.Find(currentGO.name + "FileContent");
            fileContent.transform.GetChild(1).GetChild(0).GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text = parsedJLG.CreateStatementInfoString(statementCounter);

            if (statementCounter < parsedJLG.AllStatements.Count-1)
            {
                statementCounter++;
            }
            else
            {
                Debug.Log("End of JLG reached. Press 'P' to start playing backwards.");
                running = false;
                playDirection = false;
            }
        }

        /// <summary>
        /// Visualizes the current statement and then decreases the statementCounter by 1.
        /// </summary>
        private void PreviousStatement() {
            Debug.Log(parsedJLG.AllStatements[statementCounter].Line+ " "+parsedJLG.GetStatementLocationString(statementCounter) + " CurrentGo:" + currentGO.name);
            GameObject fileContent = GameObject.Find(currentGO.name + "FileContent");
            fileContent.transform.GetChild(1).GetChild(0).GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text = parsedJLG.CreateStatementInfoString(statementCounter);
            if (statementCounter > 0)
            {
                statementCounter--;
            }
            else
            {
                Debug.Log("Start of JLG reached. Press 'P' to start.");
                running = false;
                playDirection = true;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void ToggleTextWindows() {
            foreach (GameObject go in textWindows) {
                ///Textwindow of currentGO is always active
                if (go.name == currentGO.name+"FileContent") {
                    go.SetActive(true);
                }
                else
                {
                    go.SetActive(windowsToggled);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void HighlightCurrentLineFadePrevious()
        {
            GameObject currentFileContentGO = textWindows.Peek();
            string fileContent = currentFileContentGO.transform.GetChild(0).GetChild(0).GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text;
            string[] lines = fileContent.Split(new string[]{ Environment.NewLine }, StringSplitOptions.None);

            ///highlight currentline, LineAsInt -1, because lines array starts counting at 0 and Classlines start at 1.
            lines[parsedJLG.AllStatements[statementCounter].LineAsInt()-1] = "<color=\"red\">" + lines[parsedJLG.AllStatements[statementCounter].LineAsInt()-1] + "</color>";

            ///return lines array back to a single string and then save the new highlighted string in the GameObject.
            fileContent = string.Join(Environment.NewLine, lines);
            currentFileContentGO.transform.GetChild(0).GetChild(0).GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text = fileContent;
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
