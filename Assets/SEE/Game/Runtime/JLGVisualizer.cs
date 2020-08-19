using Assets.SEE.DataModel;
using OdinSerializer;
using SEE.DataModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        private string labelText = "";

        /// <summary>
        /// Time value in seconds. At this point in time(running time) the next or previous statement will be visualized, depending on the playing direction.
        /// </summary>
        private float nextUpdateTime = 1.0f;

        private float showLabelUntil = 0f;

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
        private Boolean windowsToggled = false;  

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

        private Stack<GameObject> functionCalls = new Stack<GameObject>();

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
            if (GetNodeForStatement(statementCounter) == null) {
                throw new Exception("Node is missing. Check, if the correct gxl is loaded.");
            }
            currentGO = GetNodeForStatement(statementCounter);
            currentGO.GetComponentInChildren<MeshRenderer>().material.color = new Color(0.219f, 0.329f, 0.556f, 1f);
            GenerateScrollableTextWindow();
        }

        /// Update is called once per frame
        void Update()
        {   
            ///this is true every updateintervall time.
            if (Time.time >= nextUpdateTime)
            {
               if (running)
               {
                    ///Check if currentGo is not GO of current Statement. If true change currentGO and generate FunctionCall to new class and new TextWindow.
                    if (!NodeRepresentsStatementLocation(statementCounter, currentGO))
                    {
                        if (playDirection && statementCounter > 0 && parsedJLG.AllStatements[statementCounter].StatementType.Equals("entry"))
                        {
                            CreateFunctionCall(currentGO, GetNodeForStatement(statementCounter));
                        }
                        currentGO.GetComponentInChildren<MeshRenderer>().material.color = new Color(0.530f, 0.637f, 0.858f, 1f);
                        currentGO = GetNodeForStatement(statementCounter);
                        currentGO.GetComponentInChildren<MeshRenderer>().material.color = new Color(0.219f, 0.329f, 0.556f, 1f);

                        if (! textWindowForNodeExists(currentGO))
                        {
                            GenerateScrollableTextWindow();
                        }
                        ToggleTextWindows();
                    }

                    updateStacks();

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

            ///Controls
            if (Input.GetKeyDown(KeyCode.P))
            {
                running = !running;
                showLabelUntil = Time.time + 1f;
                if (running)
                {
                    labelText = "Play";
                    ToggleTextWindows();
                }
                else
                {
                    labelText = "Pause";
                }
            }
            if (Input.GetMouseButtonDown(0))
            {
                GameObject clickedGO = MouseClickHitActiveNode();
                if (clickedGO != null)
                {
                    if (running)
                    {
                        running = false;
                        showLabelUntil = Time.time + 1f;
                        labelText = "Paused";
                    }
                    ActivateNodeTextWindow(clickedGO);
                    Debug.Log("Hit Detected :" + clickedGO);
                }
            }
            if (Input.GetKeyDown(KeyCode.O))
            {
                updateIntervall = 1;
                playDirection = !playDirection;
                showLabelUntil = Time.time + 1f;
                if (playDirection)
                {
                    labelText = "Forward";
                }
                else
                {
                    labelText = "Rewind";
                }
            }

            ///not needed in current visualisation
            if (Input.GetKeyDown(KeyCode.J))
            {
                //windowsToggled = !windowsToggled;
                //ToggleTextWindows();
            }
            if (running)
            {
                if (Input.GetKeyDown(KeyCode.L))
                {
                    SpeedUp();
                    showLabelUntil = Time.time + 1f;
                    labelText = "Speed x" + 1f / updateIntervall;
                }
                if (Input.GetKeyDown(KeyCode.K))
                {
                    SlowDown();
                    showLabelUntil = Time.time + 1f;
                    labelText = "Speed x" + 1f / updateIntervall;
                }
            }

            ///Only Update the Spheres cause this visualization uses its own highlighting for the buildings.
            foreach (GameObject f in functionCalls) {
                f.GetComponent<FunctionCallSimulator>().UpdateSpheres();
            }
        }

        /// <summary>
        /// Responsible for displaying small log messages, that indicate what button was pressed and its effect.
        /// </summary>
        void OnGUI()
        {
            if (Time.time < showLabelUntil)
            {
                GUI.Label(new Rect(Screen.width / 96, Screen.height / 96, Screen.width / 24, Screen.height / 24), labelText);
            }
        }

        private void ActivateNodeTextWindow(GameObject gameObject)
        {
            if (textWindows.Count != 0) {
                foreach (GameObject go in textWindows) {
                    if (go.name == gameObject.name+"FileContent")
                    {
                        go.SetActive(true);
                    }
                    else {
                        go.SetActive(false);
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private GameObject MouseClickHitActiveNode()
        {
            RaycastHit hit;
            Ray camerMouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            if(Physics.Raycast(camerMouseRay, out hit))
            {
                if (hit.transform && textWindowForNodeExists(hit.transform.gameObject)) {
                    return hit.transform.gameObject;
                }
            }
            return null;
        }

        /// <summary>
        /// Using the GenerateFunctionCalls method from Runtime.cs but with some adjustments so it better works in this Visualization. Only using the spheres 
        /// and saving the complete FunctionCall Gameobject instead of just the actual FunctionCallSimulator component.
        /// </summary>
        /// <param name="currentGO"></param>
        /// <param name="destination"></param>
        private void CreateFunctionCall(GameObject currentGO, GameObject destination)
        {
            GameObject fCGO = new GameObject("FunctionCall: " + currentGO.name +" call "+ destination.name, typeof(FunctionCallSimulator))
            {
                tag = Tags.FunctionCall
            };
            FunctionCallSimulator sim = fCGO.GetComponent<FunctionCallSimulator>();
            sim.Initialize(currentGO, destination, 1f);
            functionCalls.Push(fCGO);
        }

        /// <summary>
        /// Checks, if there is an exisiting scrollable Textwindow for a given GameObject.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private bool textWindowForNodeExists(GameObject node)
        {
            bool exists = false;
            if (textWindows.Count != 0)
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
        /// Returns the first active GameObject in a stack. Null if there is none.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private GameObject findFirstActiveGOinStack(Stack<GameObject> s)
        {
            var clonedStack = new Stack<GameObject>(s.Reverse());
            foreach(GameObject g in clonedStack)
            {
                if (g.activeSelf) {
                    return g;
                }
            }
            return null;
        }

        /// <summary>
        /// This Method generates a new ScrollableTextMeshProWindow above the middle of the currentGO Node. Also it fills the Textfield with the code saved in the file,
        /// that belongs to this node aka the "FileContent". Also creates a line between the Textwindow and the node Gameobject.
        /// </summary>
        private void GenerateScrollableTextWindow()
        {
            ///spawn textwindow in middle of map
            Vector3 v = gameObject.transform.parent.position;
            v.y = v.y + 2f*gameObject.transform.parent.localScale.y;
            GameObject go = Instantiate((GameObject)Resources.Load("ScrollableTextWindow"),v,currentGO.transform.rotation,this.gameObject.transform.parent);
            go.name = currentGO.name + "FileContent";
            ///set canvas order in layer to textwindows.count damit die fenster voreinander gerendered werden
            go.transform.GetChild(0).GetChild(0).GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text = GetFileContentForNode(currentGO);
            textWindows.Push(go);

            ///add line between Class and FileContentWindow
            LineRenderer line = new GameObject(currentGO.name + "FileContentConnector").AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.material= new Material(Shader.Find("Sprites/Default"));
            line.material.color = new Color(0.219f, 0.329f, 0.556f, 1f);
            line.startWidth = 0.2f;
            line.endWidth = 0.2f;
            line.useWorldSpace = true;
            float heightOfTextObject = go.GetComponent<RectTransform>().rect.height * go.transform.parent.localScale.y;
            Debug.Log("height:" + heightOfTextObject);
            Vector3 goPoint = go.transform.position;
            goPoint.y = goPoint.y - heightOfTextObject / 2;
            line.SetPosition(0, goPoint);
            line.SetPosition(1, currentGO.transform.position);
            line.gameObject.transform.parent = go.transform;
        }

        /// <summary>
        /// This Method finds the path to the file of a given node and returns the content of the file.
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
            throw new Exception("File could not be loaded.");
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

        private void updateStacks() {
            if (playDirection)
            {
                ///If previous statement exitted a class, metaphorically remove the statements class from the callstack 
                ///by destroying its textwindow, its functionCallSimulator and coloring it back to normal. 
                if (statementCounter > 0
                        && parsedJLG.AllStatements[statementCounter - 1].StatementType.Equals("exit")
                        && currentGO != GetNodeForStatement(statementCounter - 1))
                {
                    GetNodeForStatement(statementCounter - 1).GetComponentInChildren<MeshRenderer>().material.color = new Color(1f, 0f, 0f, 1f);
                    if (findFirstActiveGOinStack(functionCalls) != null)
                    {
                        findFirstActiveGOinStack(functionCalls).SetActive(false); // hier nur disablen
                    }
                    GameObject.Destroy(textWindows.Pop());
                }
            }
            else
            {
                if (parsedJLG.AllStatements[statementCounter].StatementType.Equals("exit") && currentGO != GetNodeForStatement(statementCounter+1)
                    && findFunctionCallForGameObjects(currentGO, GetNodeForStatement(statementCounter + 1))!= null)//Check for null because sometimes, this can break if the playdirection is changed a lot.
                {
                    findFunctionCallForGameObjects(currentGO, GetNodeForStatement(statementCounter + 1)).SetActive(true);
                }
                ///If previous statement entered a class, metaphorically remove the class from the callstack by destroying its textwindow and its FunctionCallSimulator.
                ///Looking for an "entrystatement", because the visualisation is running backwards.
                else if (statementCounter < parsedJLG.AllStatements.Count - 1
                    && parsedJLG.AllStatements[statementCounter + 1].StatementType.Equals("entry")
                    && currentGO != GetNodeForStatement(statementCounter + 1))
                {
                    GetNodeForStatement(statementCounter + 1).GetComponentInChildren<MeshRenderer>().material.color = new Color(1f, 0f, 0f, 1f);
                    if (functionCalls.Count > 0 &&
                        functionCalls.Peek().name.Equals("FunctionCall: " + currentGO.name + " call " + GetNodeForStatement(statementCounter + 1).name))
                    {
                        GameObject.Destroy(functionCalls.Pop());
                    }
                    ///Also Check, if textWindow for the statement+1 exists. (Special Case: Direction changes at statement and statement+1 is entry but was never executed =>
                    /// => textwindow for statement+1 doesn't exist yet and the wrong textwindow is popped.)
                    if (textWindows.Peek().name.StartsWith(GetNodeForStatement(statementCounter + 1).name))
                    {
                        GameObject.Destroy(textWindows.Pop());
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="currentGO"></param>
        /// <param name="gameObject"></param>
        /// <returns></returns>
        private GameObject findFunctionCallForGameObjects(GameObject currentGO, GameObject gameObject)
        {
            var clonedStack = new Stack<GameObject>(functionCalls.Reverse());
            foreach (GameObject go in clonedStack) {
                if (!go.activeSelf && go.GetComponent<FunctionCallSimulator>().src == gameObject
                    && go.GetComponent<FunctionCallSimulator>().dst == currentGO) {
                    return go;
                }
            }
            return null;
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


            ///For some Reason this is sometimes false and it would throw an error.
            if (lines.Length - 1 > parsedJLG.AllStatements[statementCounter].LineAsInt())
            {
                string currentLineString = lines[parsedJLG.AllStatements[statementCounter].LineAsInt() - 1];
                ///strip currentline of previous highlighting, if it has it
                if (currentLineString.StartsWith("<color=#"))
                {
                    ///remove color tag at start
                    currentLineString = currentLineString.Substring(currentLineString.IndexOf('>') + 1);
                    ///remove color tag at end
                    currentLineString = currentLineString.Remove(currentLineString.LastIndexOf('<'));
                }
                ///highlight currentline, LineAsInt -1, because lines array starts counting at 0 and Classlines start at 1.
                lines[parsedJLG.AllStatements[statementCounter].LineAsInt() - 1] = "<color=#5ACD5A>" + currentLineString + "</color>";

                ///return lines array back to a single string and then save the new highlighted string in the GameObject.
                fileContent = string.Join(Environment.NewLine, lines);
                currentFileContentGO.transform.GetChild(0).GetChild(0).GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text = fileContent;

                ///Scroll the Scroll rect so the current line is in the middle.
                currentFileContentGO.transform.GetChild(0).GetComponent<ScrollRect>().verticalNormalizedPosition = 1 - ((float)parsedJLG.AllStatements[statementCounter].LineAsInt() / (float)lines.Length);
            }
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

            ///For some Reason this is sometimes false and it would throw an error.
            if (lines.Length - 1 > parsedJLG.AllStatements[statementCounter].LineAsInt())
            {
                lines[parsedJLG.AllStatements[statementCounter].LineAsInt() - 1] = "<color=#5ACD5A>" + lines[parsedJLG.AllStatements[statementCounter].LineAsInt() - 1] + "</color>";

                fileContent = string.Join(Environment.NewLine, lines);
                currentFileContentGO.transform.GetChild(0).GetChild(0).GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text = fileContent;

                currentFileContentGO.transform.GetChild(0).GetComponent<ScrollRect>().verticalNormalizedPosition = 1 - ((float)parsedJLG.AllStatements[statementCounter].LineAsInt() / (float)lines.Length);
            }
        }

        /// <summary>
        /// This Method reverses the highlighting of lines by coloring them all white again.
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
        /// This Method fades highlighted lines in 5 steps back to white.
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
