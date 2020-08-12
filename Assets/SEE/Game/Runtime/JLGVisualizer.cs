using Assets.SEE.DataModel;
using OdinSerializer;
using SEE.DataModel;
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

        private float currentTimeInLoop = 0.0f;

        private Stack<FunctionCallSimulator> functionCalls = new Stack<FunctionCallSimulator>();

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
                        if (playDirection && statementCounter > 0 && parsedJLG.AllStatements[statementCounter].StatementType.Equals("entry"))
                        {
                            CreateFunctionCall(currentGO, GetNodeForStatement(statementCounter));
                        }

                        currentGO = GetNodeForStatement(statementCounter);

                        if (! textWindowForNodeExists(currentGO))
                        {
                            GenerateScrollableTextWindow();
                        }
                        ToggleTextWindows();
                    }

                    if (playDirection)
                    {
                        ///If previous statement exitted a class, metaphorically remove the class from the callstack by destroying its textwindow and its functionCallSimulator.
                        if (statementCounter > 0 
                            && parsedJLG.AllStatements[statementCounter - 1].StatementType.Equals("exit") 
                            && currentGO != GetNodeForStatement(statementCounter - 1))
                        {
                            functionCalls.Pop().Shutdown();
                            GameObject.Destroy(textWindows.Pop());
                        }
                        NextStatement();
                    }
                    else
                    {
                        ///If previous statement entered a class, metaphorically remove the class from the callstack by destroying its textwindow and its FunctionCallSimulator.
                        ///Looking for an "entrystatement", because the visualisation is running backwards.
                        if (statementCounter < parsedJLG.AllStatements.Count-1
                            && parsedJLG.AllStatements[statementCounter + 1].StatementType.Equals("entry")
                            && currentGO != GetNodeForStatement(statementCounter + 1))
                        {
                            if (functionCalls.Count > 0 && 
                                functionCalls.Peek().name.Equals("FunctionCall: " + currentGO.name + " call " + GetNodeForStatement(statementCounter + 1).name))
                            {
                                functionCalls.Pop().Shutdown();
                            }
                            ///Also Check, if textWindow for the statement+1 exists. (Special Case: Direction changes at statement and statement+1 is entry but was never executed =>
                            /// => textwindow for statement+1 doesn't exist yet and the wrong textwindow is popped.)
                            if (textWindows.Peek().name.StartsWith(GetNodeForStatement(statementCounter + 1).name))
                            {
                                GameObject.Destroy(textWindows.Pop());
                            }
                        }
                        PreviousStatement();
                    }
               }
               nextUpdateTime += updateIntervall;               
            }            

            ///Controls
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

            //functionCalls.ForEach(e => e.UpdateSimulation(currentTimeInLoop / 1.0f));
            foreach (FunctionCallSimulator f in functionCalls) {
                f.UpdateSpheres();
            }
            currentTimeInLoop = (currentTimeInLoop + Time.deltaTime) % 1.0f;
        }

        private void CreateFunctionCall(GameObject currentGO, GameObject destination)
        {
            GameObject fCGO = new GameObject("FunctionCall: " + currentGO.name +" call "+ destination.name, typeof(FunctionCallSimulator))
            {
                tag = Tags.FunctionCall
            };
            FunctionCallSimulator sim = fCGO.GetComponent<FunctionCallSimulator>();
            sim.Initialize(currentGO, destination, currentTimeInLoop);
            functionCalls.Push(sim);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
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
                    int i = 1;
                    string output = "";
                    foreach (string line in File.ReadLines(p))
                    {
                        output = output + i + ". " + line + Environment.NewLine;
                        i++;
                    }
                    return output;
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

            HighlightCurrentLineFadePreviousReverse();

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
        /// Highlights the currentline and fades the previous five lines bit by bit back to white.
        /// This is calle when playdirection is forwards/true.
        /// </summary>
        private void HighlightCurrentLineFadePrevious()
        {
            GameObject currentFileContentGO = textWindows.Peek();
            string fileContent = currentFileContentGO.transform.GetChild(0).GetChild(0).GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text;
            string[] lines = fileContent.Split(new string[]{ Environment.NewLine }, StringSplitOptions.None);

            lines = FadePreviousLines(lines);

            string currentLineString = lines[parsedJLG.AllStatements[statementCounter].LineAsInt() - 1];

            ///strip currentline of previous highlighting, if it has it
            if (currentLineString.StartsWith("<color=#"))
            {
                ///remove color tag at start
                currentLineString = currentLineString.Substring(currentLineString.IndexOf('>')+1);
                ///remove color tag at end
                currentLineString = currentLineString.Remove(currentLineString.LastIndexOf('<'));
            }
            ///highlight currentline, LineAsInt -1, because lines array starts counting at 0 and Classlines start at 1.
            lines[parsedJLG.AllStatements[statementCounter].LineAsInt()-1] = "<color=#5ACD5A>" + currentLineString + "</color>";

            ///return lines array back to a single string and then save the new highlighted string in the GameObject.
            fileContent = string.Join(Environment.NewLine, lines);
            currentFileContentGO.transform.GetChild(0).GetChild(0).GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text = fileContent;

            ///Scroll the Scroll rect so the current line is in the middle.
            currentFileContentGO.transform.GetChild(0).GetComponent<ScrollRect>().verticalNormalizedPosition = 1 - ((float)parsedJLG.AllStatements[statementCounter].LineAsInt() / (float)lines.Length);
        }

        /// <summary>
        /// Highlights the currentline and unfades previously faded lines back to white. Can only actively highlight the current line.
        /// This is called when playdirection is backwards/false.
        /// </summary>
        private void HighlightCurrentLineFadePreviousReverse()
        {
            GameObject currentFileContentGO = textWindows.Peek();
            string fileContent = currentFileContentGO.transform.GetChild(0).GetChild(0).GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text;
            string[] lines = fileContent.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

            lines = UnfadePreviousLines(lines);

            lines[parsedJLG.AllStatements[statementCounter].LineAsInt() - 1] = "<color=#5ACD5A>" + lines[parsedJLG.AllStatements[statementCounter].LineAsInt() - 1] + "</color>";

            fileContent = string.Join(Environment.NewLine, lines);
            currentFileContentGO.transform.GetChild(0).GetChild(0).GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text = fileContent;

            currentFileContentGO.transform.GetChild(0).GetComponent<ScrollRect>().verticalNormalizedPosition = 1 - ((float)parsedJLG.AllStatements[statementCounter].LineAsInt() / (float)lines.Length);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        private string[] UnfadePreviousLines(string[] lines)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("<color="))
                {
                    if (lines[i].StartsWith("<color=#5ACD5A>"))
                    {
                        lines[i] = lines[i].Replace("<color=#5ACD5A>", "");
                        lines[i] = lines[i].Replace("</color>", "");
                    }
                    else if (lines[i].StartsWith("<color=#A5CDA5>"))
                    {
                        lines[i] = lines[i].Replace("<color=#A5CDA5>", "");
                        lines[i] = lines[i].Replace("</color>", "");
                    }
                    else if (lines[i].StartsWith("<color=#78CD78>"))
                    {
                        lines[i] = lines[i].Replace("<color=#78CD78>", "");
                        lines[i] = lines[i].Replace("</color>", "");
                    }
                    else if (lines[i].StartsWith("<color=#96CD96>"))
                    {
                        lines[i] = lines[i].Replace("<color=#96CD96>", "");
                        lines[i] = lines[i].Replace("</color>", "");
                    }
                    else if (lines[i].StartsWith("<color=#B4CDB4>"))
                    {
                        lines[i] = lines[i].Replace("<color=#B4CDB4>", "");
                        lines[i] = lines[i].Replace("</color>", "");
                    }
                }
            }
            return lines;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        private string[] FadePreviousLines(string[] lines)
        {
            for(int i = 0; i<lines.Length;i++)
            {
                if (lines[i].StartsWith("<color="))
                {                  
                    if (lines[i].StartsWith("<color=#5ACD5A>"))
                    {
                        lines[i] = lines[i].Replace("<color=#5ACD5A>", "<color=#78CD78>");
                    }
                    else if (lines[i].StartsWith("<color=#78CD78>"))
                    {
                        lines[i] = lines[i].Replace("<color=#78CD78>", "<color=#96CD96>");
                    }
                    else if (lines[i].StartsWith("<color=#96CD96>"))
                    {
                        lines[i] = lines[i].Replace("<color=#96CD96>", "<color=#A5CDA5>");
                    }
                    else if (lines[i].StartsWith("<color=#A5CDA5>"))
                    {
                       lines[i] = lines[i].Replace("<color=#A5CDA5>", "<color=#B4CDB4>");
                    }
                    else
                    {
                        lines[i] = lines[i].Replace("<color=#B4CDB4>", "");
                        lines[i] = lines[i].Replace("</color>", "");
                    }
                }
            }
            return lines;
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
