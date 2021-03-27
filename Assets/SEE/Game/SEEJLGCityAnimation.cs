using SEE.Controls;
using SEE.DataModel;
using SEE.DataModel.IO;
using SEE.Game.Runtime;
using SEE.GO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.Game
{
    /// <summary>
    /// Configuration of a code city for the visualization of dynamic data in
    /// traced at the level of statements.
    /// 
    /// This part contains the animation code.
    /// </summary>
    public partial class SEEJlgCity : SEECity
    {
        //------------------------------------
        // Public configuration attributes
        //------------------------------------

        /// <summary>
        /// The class the breakpoint is contained in.
        /// </summary>
        public string BreakpointClass = "classname";

        /// <summary>
        /// The source line of the breakpoint.
        /// </summary>
        public int BreakpointLine = 0;

        /// <summary>
        /// The distance between the code-city object and the source-code window
        /// in Unity units.
        /// </summary>
        public float DistanceAboveCity = 0.01f;

        /// <summary>
        /// The distance between the back edge of the code-city object and the source-code window
        /// in Unity units.
        /// </summary>
        public float DistanceBehindCity = 0.3f;

        /// <summary>
        /// The width of the line connecting the source-code window and the game objects
        /// whose source code is currently shown.
        /// </summary>
        public float LineWidth = 0.01f;

        //-------------------------------------------------------
        // Private attributes not saved in the configuration file
        //-------------------------------------------------------

        /// <summary>
        /// This name will be added at the end of every game object representing a 
        /// source-code viewer for an executed node.
        /// </summary>
        private const string FileContentNamePostfix = "FileContent";

        /// <summary>
        /// A ParsedJLG object that contains a parsed JLG file. This object contains all 
        /// information needed for the visualization of a debugging process.
        /// </summary>
        //[NonSerialized, OdinSerialize]
        private ParsedJLG parsedJLG;

        /// <summary>
        /// Int value the represents the index of the current active statement. 
        /// All indices can be found in this.parsedJLG.allStatements.
        /// The total number of indices is this.parsedJLG.allStatements.Count.
        /// Must always stay in the range 0 <= statementCounter <= parsedJLG.AllStatements.Count - 1
        /// </summary>
        private int statementCounter = 0;

        /// <summary>
        /// This Field saves the text, that is displayed when a button is hit is saved.
        /// </summary>
        private string labelText = "";

        /// <summary>
        /// Time value in seconds. At this point in time (running time) the next or 
        /// previous statement will be visualized, depending on the playing direction.
        /// </summary>
        private float nextUpdateTime = 1.0f;

        /// <summary>
        /// How long the currently a GUI message telling which button was pressed should
        /// be shown.
        /// </summary>
        private float showLabelUntil = 0f;

        /// <summary>
        /// Seconds per statement.
        /// </summary>
        private float updateIntervall = 1.0f;

        /// <summary>
        /// True is visualisation is running. false if paused.
        /// </summary>
        private Boolean running = false;

        /// <summary>
        /// Describes the direction, in which the visualisation is running. True for forward, false for backwards.
        /// </summary>
        private Boolean playingForward = true;

        /// <summary>
        /// This saves the direction in which the last Statement was visualized.
        /// </summary>
        private Boolean lastDirection = true;

        /// <summary>
        /// The GameObject that represents the class the current statement belongs to. 
        /// </summary>
        private GameObject currentGO;

        /// <summary>
        /// A list of all active GameObjects that are tagged with 'Node'.
        /// </summary>
        private Dictionary<string, GameObject> nodesGOs;

        /// <summary>
        /// Stack of all existing FileContent Textwindows.
        /// </summary>
        private Stack<GameObject> textWindows = new Stack<GameObject>();

        /// <summary>
        /// Stack of all existing function calls.
        /// </summary>
        private Stack<GameObject> functionCalls = new Stack<GameObject>();

        /// Start is called before the first frame update
        void Start()
        {
            JLGParser jlgParser = new JLGParser(JLGPath.Path);
            parsedJLG = jlgParser.Parse();

            if (parsedJLG == null)
            {
                enabled = false;
                Debug.LogError("Parsed JLG is null!");
            }
            else
            {
                nodesGOs = GetNodes();
                Debug.Log($"[JLG] Number of nodes contained in {gameObject.name} is {nodesGOs.Count}\n");
                // Sets the currentGO to be the node representing the Class of the first Statement in preperation.
                if (nodesGOs.Count == 0)
                {
                    enabled = false;
                    Debug.LogError("[JLG] There are no nodes.\n");
                }
                else if (GetNodeForStatement(statementCounter) == null)
                {
                    enabled = false;
                    Debug.LogError($"[JLG] Node for statement counter {statementCounter} is missing. Check whether the correct GXL is loaded.\n");
                }
                else
                {
                    currentGO = GetNodeForStatement(statementCounter);
                    currentGO.GetComponentInChildren<MeshRenderer>().material.color = new Color(0.219f, 0.329f, 0.556f, 1f);
                    GenerateScrollableTextWindow();
                    Debug.Log("[JLG] Started.");
                }
            }
        }

        /// <summary>
        /// Returns all transitive children of <see cref="gameObject"/> tagged by <see cref="Tags.Node"/>.
        /// </summary>
        /// <returns>all ancestors representing nodes</returns>
        private Dictionary<string, GameObject> GetNodes()
        {
            Dictionary<string, GameObject> result = new Dictionary<string, GameObject>();
            foreach(GameObject node in gameObject.AllAncestors(Tags.Node))
            {
                result[node.name] = node;
            }
            return result;
        }

        /// Update is called once per frame
        void Update()
        {
            // Update Visualisation every 'interval' seconds.
            if (Time.time >= nextUpdateTime)
            {
                if (running)
                {
                    UpdateVisualization();
                }
                nextUpdateTime += updateIntervall;
            }

            // Controls
            if (Input.GetKeyDown(KeyBindings.ToggleAutomaticManualMode))
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
            if (Input.GetKeyDown(KeyBindings.ToggleExecutionOrder))
            {
                updateIntervall = 1;
                playingForward = !playingForward;

                showLabelUntil = Time.time + 1f;
                if (playingForward)
                {
                    lastDirection = true;
                    statementCounter = statementCounter + 2; //These need to be called because the statementcounter was increased/decreased in the last UpdateVisualization call already. This prevents bugs.
                    labelText = "Forward";
                }
                else
                {
                    lastDirection = false;
                    statementCounter = statementCounter - 2;
                    labelText = "Rewind";
                }
            }
            if (running)
            {
                if (Input.GetKeyDown(KeyBindings.IncreaseAnimationSpeed))
                {
                    SpeedUp();
                    showLabelUntil = Time.time + 1f;
                    labelText = "Speed x" + 1f / updateIntervall;
                }
                if (Input.GetKeyDown(KeyBindings.DecreaseAnimationSpeed))
                {
                    SlowDown();
                    showLabelUntil = Time.time + 1f;
                    labelText = "Speed x" + 1f / updateIntervall;
                }
            }
            else
            {
                if (Input.GetKeyDown(KeyBindings.ExecuteToBreakpoint))
                {
                    showLabelUntil = Time.time + 1f;
                    labelText = "Jumping to Breakpoint...";
                    if (JumpToNextBreakpointHit())
                    {
                        showLabelUntil = Time.time + 1f;
                        labelText = "Jumped to Breakpoint";
                    }
                    else
                    {
                        showLabelUntil = Time.time + 1f;
                        labelText = "Could not find Breakpoint";
                    }
                }
                if (Input.GetKeyDown(KeyBindings.PreviousStatement))
                {
                    OneStep(true);
                    lastDirection = true;
                }
                if (Input.GetKeyDown(KeyBindings.NextStatement))
                {
                    OneStep(false);
                    lastDirection = false;
                }
                if (Input.GetKeyDown(KeyBindings.FirstStatement))
                {
                    ResetComplete();
                }
            }

            // Only update the Spheres cause this visualization uses its own highlighting for the buildings.
            foreach (GameObject f in functionCalls)
            {
                f.GetComponent<FunctionCallSimulator>().UpdateSpheres();
            }
        }

        /// <summary>
        /// Responsible for displaying small log messages that indicate what button 
        /// was pressed and its effect.
        /// </summary>
        void OnGUI()
        {
            // FIXME: Is this the right condition? Time.time is the time since the start of the game.
            if (Time.time < showLabelUntil)
            {
                GUI.Label(new Rect(Screen.width / 96, Screen.height / 96, Screen.width / 3, Screen.height / 16), labelText);
            }
        }

        /// <summary>
        /// This Method updates the visualization for one Step.
        /// </summary>
        private void UpdateVisualization()
        {
            //Debug.Log($"[JLG] Current statement: {statementCounter}\n");

            try
            {
                CheckCurrentGO();

                if (!TextWindowForNodeExists(currentGO))
                {
                    GenerateScrollableTextWindow();
                }
                ToggleTextWindows();

                UpdateStacks();

                if (playingForward)
                {
                    NextStatement();
                }
                else
                {
                    PreviousStatement();
                }
            }
            catch (MalformedStatement e)
            {
                Debug.LogError($"[JLG] Statement {statementCounter} is malformed and will be skipped: {e.Message}.\n");
                if (playingForward)
                {
                    statementCounter = Mathf.Min(statementCounter + 1, parsedJLG.AllStatements.Count - 1);
                }
                else
                {
                    statementCounter = Mathf.Max(0, statementCounter - 1);
                }
            }
        }

        /// <summary>
        /// Check if currentGo is GO of current Statement. If not, change currentGO.
        /// </summary>
        private void CheckCurrentGO()
        {
            if (!NodeRepresentsStatementLocation(statementCounter, currentGO))
            {
                GameObject nodeForStatement = GetNodeForStatement(statementCounter);
                if (playingForward && statementCounter > 0 && parsedJLG.AllStatements[statementCounter].StatementType.Equals("entry"))
                {
                    CreateFunctionCall(currentGO, nodeForStatement);
                }
                currentGO.GetComponentInChildren<MeshRenderer>().material.color = new Color(0.530f, 0.637f, 0.858f, 1f);
                currentGO = nodeForStatement;
                currentGO.GetComponentInChildren<MeshRenderer>().material.color = new Color(0.219f, 0.329f, 0.556f, 1f);
            }
        }

        /// <summary>
        /// Activates the FileContent Gameobject of a given node and disables all others.
        /// </summary>
        /// <param name="gameObject"></param>
        private void ActivateNodeTextWindow(GameObject gameObject)
        {
            if (textWindows.Count != 0)
            {
                foreach (GameObject go in textWindows)
                {
                    if (go.name == gameObject.name + FileContentNamePostfix)
                    {
                        go.SetActive(true);
                    }
                    else
                    {
                        go.SetActive(false);
                    }
                }
            }
        }

        /// <summary>
        /// Checks if a mouse click hit a Node Gameobject. Returns GameObject or Null if no object was hit.
        /// </summary>
        /// <returns>hit gameobject</returns>
        private GameObject MouseClickHitActiveNode()
        {
            RaycastHit hit;
            Ray camerMouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(camerMouseRay, out hit))
            {
                if (hit.transform && TextWindowForNodeExists(hit.transform.gameObject))
                {
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
            GameObject fCGO = new GameObject("FunctionCall: " + currentGO.name + " call " + destination.name, typeof(FunctionCallSimulator))
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
        private bool TextWindowForNodeExists(GameObject node)
        {
            bool exists = false;
            if (textWindows.Count != 0)
            {
                foreach (GameObject go in textWindows)
                {
                    if (go.name.Equals(node.name + FileContentNamePostfix))
                    {
                        exists = true;
                        break;
                    }
                }
            }
            return exists;
        }

        /// <summary>
        /// This method generates a new ScrollableTextMeshProWindow above the code city. 
        /// This method assumes that the game object this JLGVisualizer is attached to is 
        /// an immediate child of the code-city object. Hence, the parent of 
        /// <see cref="gameObject"/> identifies the code-city object. Also it fills the 
        /// Textfield with the written code saved in the file, that belongs to this node 
        /// aka the "FileContent". Lastly it creates a visual line between the 
        /// TextWindow and the <see cref="gameObject"/>.
        /// </summary>
        private void GenerateScrollableTextWindow()
        {
            Debug.LogFormat("GenerateScrollableTextWindow: windowRotation {0} \n", (Quaternion)currentGO.transform.rotation);
            GameObject textWindow = Instantiate((GameObject)Resources.Load("ScrollableTextWindow"), Vector3.zero, rotation: currentGO.transform.rotation);
            textWindow.name = currentGO.name + FileContentNamePostfix;

            float textWindowHeight = GetWindowHeight(textWindow);

            {
                // Sets the position of the textWindow.
                Transform referencePlane = GetReferencePlane();
                Vector3 cityExtent = referencePlane.transform.lossyScale / 2.0f;

                Vector3 windowPosition = referencePlane.position;
                // the position of the textWindow rectangle is the 
                windowPosition.y += DistanceAboveCity - textWindowHeight / 2;
                windowPosition.z += cityExtent.z + DistanceBehindCity; // at the back edge of the city
                textWindow.transform.position = windowPosition;
            }

            ///set canvas order in layer to textwindows.count so that the text windows can be renderer in front of each other
            textWindow.transform.GetChild(0).GetChild(0).GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text = GetFileContentForNode(currentGO);
            textWindows.Push(textWindow);

            ///add line between Class and FileContentWindow
            LineRenderer line = new GameObject(currentGO.name + "FileContentConnector").AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.material.color = new Color(0.219f, 0.329f, 0.556f, 1f);
            line.startWidth = LineWidth;
            line.endWidth = LineWidth;
            line.useWorldSpace = true;
            Vector3 startPosition = textWindow.transform.position;
            // FIXME: The beam just above the lower edge of the text window. It seems as if the
            // textWindow is resized. At least we are not getting its correct height.
            startPosition.y -= textWindowHeight;
            line.SetPosition(0, startPosition);
            line.SetPosition(1, currentGO.transform.position);
            line.gameObject.transform.parent = textWindow.transform;
        }

        private float GetWindowHeight(GameObject textWindow)
        {
            if (textWindow.TryGetComponent<RectTransform>(out RectTransform rectTransform))
            {
                // Each corner provides its world space value. The returned array of 4 vertices 
                // is clockwise. It starts bottom left and rotates to top left, then top right,
                // and finally bottom right. Note that bottom left, for example, is an (x, y, z) 
                // vector with x being left and y being bottom.
                Vector3[] corners = new Vector3[4];
                rectTransform.GetWorldCorners(corners);
                return corners[1].y - corners[0].y;
            }
            else
            {
                throw new Exception("Text window " + textWindow.name + "does not have a RectTransform.");
            }
        }

        /// <summary>
        /// Returns the transform of the plane underlying the game objects forming the code city
        /// if it exists; otherwise the tranform of the code-city object is returned.
        /// 
        /// Assumption: The plane is an immediate child of the code-city object, named "Plane"
        /// and tagged by Tags.Decoration.
        /// </summary>
        /// <returns>transform of plane or code-city</returns>
        private Transform GetReferencePlane()
        {
            foreach (Transform child in gameObject.transform)
            {
                if (child.CompareTag(Tags.Decoration) && child.name == "Plane")
                {
                    return child.transform;
                }
            }
            return gameObject.transform.parent;
        }

        /// <summary>
        /// This method finds the path to the file of a given node and returns the content of the file.
        /// If the source code cannot be found, the single line "1. // empty" will be returned.
        /// Precondition: <paramref name="go"/> represents a Java class.
        /// </summary>
        /// <param name="go">the game object representing the class whose source code is to be returned</param>
        /// <returns>the file content of the corresponding source-code file (line numbers are appended)</returns>
        private string GetFileContentForNode(GameObject go)
        {
            string classname = go.name;
            if (classname.Contains("."))
            {
                classname = classname.Substring(classname.LastIndexOf(".") + 1);
            }
            classname += ".java";
            foreach (string p in parsedJLG.FilesOfProject)
            {
                if (p.EndsWith(classname))
                {
                    if (File.Exists(p))
                    {
                        int lineNumber = 1;
                        string output = "";
                        foreach (string lineContent in File.ReadLines(p))
                        {
                            output += lineNumber.ToString() + ". " + lineContent + Environment.NewLine;
                            lineNumber++;
                        }
                        return output;
                    }
                    else
                    {
                        Debug.LogErrorFormat("Source code file {0} not found for game object {1}.\n", p, go.name);
                        return "1. // empty" + Environment.NewLine;
                    }
                }
            }
            throw new Exception("File could not be loaded for " + classname + ".");
        }

        /// <summary>
        /// Gets a GameObject tagged with Node from this objects list of GameObjects (nodesGos) that matches 
        /// the location of the statement represented by <paramref name="index"/>.
        /// A node matches the location of a statement if the class name of the location equals 
        /// the node's name.
        /// </summary>
        /// <param name="index">the index of the statement in this <see cref="parsedJLG.allstatements"/></param>
        /// <returns>Node for Statement, if exists, else null.</returns>
        private GameObject GetNodeForStatement(int index)
        {
            string searchedName = parsedJLG.GetStatementLocationString(index);
            if (nodesGOs.TryGetValue(searchedName, out GameObject result))
            {
                return result;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the FileContent GameObject of a given Node, if it exists in textwindows. Otherwise returns null.
        /// </summary>
        /// <param name="classGO">given node</param>
        /// <returns>FileContent for node</returns>
        private GameObject GetFileContentGOForNode(GameObject classGO)
        {
            foreach (GameObject fc in textWindows)
            {
                if (fc.name.Equals(classGO.name + FileContentNamePostfix))
                {
                    return fc;
                }
            }
            return null;
        }

        /// <summary>
        /// Returns true if <see cref="go"/> is a node and the class it is representing is the class of 
        /// the given statement.
        /// </summary>
        /// <param name="stmtIndex">index of the Java statement in parsedJLGs JavaStatement list</param>
        /// <param name="go">the GameObject</param>
        /// <returns>whether <paramref name="go"/> represent the class at given <paramref name="stmtIndex"/></returns>
        private Boolean NodeRepresentsStatementLocation(int stmtIndex, GameObject go)
        {
            return parsedJLG.GetStatementLocationString(stmtIndex) == go.name && go.CompareTag(Tags.Node);
        }

        /// <summary>
        /// Visualizes the current statement and then increases statementCounter by 1.
        /// </summary>
        private void NextStatement()
        {
            HighlightCurrentLineFadePrevious();

            // Generate the info text in the smaller textwindow for the currentstatement. The info text is built
            // in the parsedJLG object and then returned to the TMPro text component.
            GameObject fileContent = GetFileContentGOForNode(currentGO);
            fileContent.transform.GetChild(1).GetChild(0).GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text
                = parsedJLG.CreateStatementInfoString(statementCounter, true);

            if (statementCounter < parsedJLG.AllStatements.Count - 1)
            {
                statementCounter++;
            }
            else
            {
                Debug.Log($"End of JLG reached. Press {KeyBindings.PreviousStatement} to start playing backwards.\n");
                running = false;
                playingForward = false;
            }
        }

        /// <summary>
        /// Visualizes the current statement and then decreases the statementCounter by 1.
        /// </summary>
        private void PreviousStatement()
        {
            HighlightCurrentLineFadePreviousReverse();

            //Generate the info text in the smaller textwindow for the currentstatement. The info text is build in the parsedJLG object and then returned to the TMPro text component.
            GameObject fileContent = GetFileContentGOForNode(currentGO);
            fileContent.transform.GetChild(1).GetChild(0).GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text = parsedJLG.CreateStatementInfoString(statementCounter, false);
            if (statementCounter > 0)
            {
                statementCounter--;
            }
            else
            {
                Debug.Log("Start of JLG reached. Press 'P' to start.");
                running = false;
                playingForward = true;
            }
        }

        /// <summary>
        /// Activates the Textwindow of the currentGO and disables all other.
        /// </summary>
        private void ToggleTextWindows()
        {
            foreach (GameObject go in textWindows)
            {
                // Textwindow of currentGO is always active
                if (go.name == currentGO.name + FileContentNamePostfix)
                {
                    go.SetActive(true);
                }
                else
                {
                    go.SetActive(false);
                }
            }
        }

        /// <summary>
        /// This method updates all stacks that are need for the visualization, depending on the playdirection.
        /// The stacks modified here are: functionCalls and parsedJLG.ReturnValues.
        /// The returnValues stack is filled inside the parsedJLG Object when playdirection is true/Forward.
        /// </summary>
        private void UpdateStacks()
        {
            if (playingForward)
            {
                // Executing forward.

                // If previous statement exited a class, metaphorically remove the statements class from the callstack 
                // by disabling its functionCall and coloring it back to normal. 
                if (statementCounter > 0 // not at the first statement
                        && parsedJLG.AllStatements[statementCounter - 1].StatementType.Equals("exit"))
                {
                    GameObject nodeForPreviousStatement = GetNodeForStatement(statementCounter - 1);

                    if (currentGO != nodeForPreviousStatement)
                    {
                        GameObject functionCall = FindFunctionCallForGameObjects(nodeForPreviousStatement, currentGO, true);
                        if (functionCall != null)
                        {
                            functionCall.SetActive(false); // only disable, so it can be enabled when the visualization is running backwards
                            nodeForPreviousStatement.GetComponentInChildren<MeshRenderer>().material.color = new Color(1f, 0f, 0f, 1f);
                        }
                    }
                }
            }
            else
            {
                // Executing backward.

                bool exitingCalledFunction = parsedJLG.AllStatements[statementCounter].StatementType.Equals("exit");
                if (exitingCalledFunction)
                {
                    parsedJLG.ReturnValues.Pop(); // remove return value from stack, that is returned in the exit statement.
                }

                GameObject nodeForNextStatement = statementCounter < parsedJLG.AllStatements.Count - 1 ? GetNodeForStatement(statementCounter + 1) : null;
                if (exitingCalledFunction && currentGO != nodeForNextStatement && nodeForNextStatement != null)
                {
                    GameObject functionCall = FindFunctionCallForGameObjects(currentGO, nodeForNextStatement, false);
                    //Check for null because sometimes, this can break if the playdirection is changed a lot in a short time.
                    if (functionCall != null)
                    {
                        functionCall.SetActive(true);
                    }
                }
                // If previous statement entered a class, metaphorically remove the class from the callstack by destroying its textwindow and its FunctionCallSimulator.
                // Looking for an at statementCounter+1 "entrystatement", because the visualisation is running backwards.
                else if (statementCounter < parsedJLG.AllStatements.Count - 1
                    && parsedJLG.AllStatements[statementCounter + 1].StatementType.Equals("entry")
                    && currentGO != nodeForNextStatement)
                {
                    nodeForNextStatement.GetComponentInChildren<MeshRenderer>().material.color = new Color(1f, 0f, 0f, 1f);
                    if (functionCalls.Count > 0 &&
                        functionCalls.Peek().name.Equals("FunctionCall: " + currentGO.name + " call " + nodeForNextStatement.name))
                    {
                        GameObject.Destroy(functionCalls.Pop());
                    }
                }
            }
        }

        /// <summary>
        /// Returns the latest <paramref name="active"/> FunctionCall that matches the two given GameObjects.
        /// </summary>
        /// <param name="dstGO">destination of the call</param>
        /// <param name="srcGO">source of the call</param>
        /// <param name="active">whether the returned function should be active</param>
        /// <returns>function call</returns>
        private GameObject FindFunctionCallForGameObjects(GameObject dstGO, GameObject srcGO, bool active)
        {
            // The enumerator of a stack iterates from the top element to the very first element added to the stack.
            foreach (GameObject go in functionCalls)
            {
                if (go.activeSelf == active && go.GetComponent<FunctionCallSimulator>().src == srcGO
                    && go.GetComponent<FunctionCallSimulator>().dst == dstGO)
                {
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
            GameObject currentFileContentGO = GetFileContentGOForNode(currentGO);

            //cancel the method if there was no FileContent GameObject found. This should never happen though.
            if (currentFileContentGO == null)
            {
                return;
            }

            string fileContent = currentFileContentGO.transform.GetChild(0).GetChild(0).GetChild(0).gameObject.GetComponent<TextMeshProUGUI>().text;
            string[] lines = fileContent.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

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

                ///Scroll the Scroll rect so the current line is visible. This is not optimal yet, since sometimes it shows the current line right at the top of the Textwindow and not in the middle.
                currentFileContentGO.transform.GetChild(0).GetComponent<ScrollRect>().verticalNormalizedPosition = 1 - ((float)parsedJLG.AllStatements[statementCounter].LineAsInt() / (float)lines.Length);
            }
        }

        /// <summary>
        /// Highlights the currentline and unfades previously faded lines back to white. Can only actively highlight the current line.
        /// This is called when playdirection is backwards/false.
        /// </summary>
        private void HighlightCurrentLineFadePreviousReverse()
        {
            GameObject currentFileContentGO = GetFileContentGOForNode(currentGO);

            //cancel the method if there was no FileContent GameObject found. This should never happen though.
            if (currentFileContentGO == null)
            {
                return;
            }

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
            for (int i = 0; i < lines.Length; i++)
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
        /// This Method jumps to the next Breakpoint. If the Breakpoint is more than 200 Statements ahead, it only visualizes the last 150 steps.
        /// </summary>
        /// <returns></returns>
        private Boolean JumpToNextBreakpointHit()
        {
            playingForward = true;
            if (BreakpointLine <= 0)
            {
                return false;
            }
            if (BreakpointClass == "" || BreakpointClass == null)
            {
                return false;
            }
            JavaStatement js = parsedJLG.AllStatements[statementCounter];
            int count = statementCounter;
            while (!((js.LineAsInt() == BreakpointLine) && parsedJLG.GetStatementLocationString(count).Contains(BreakpointClass)))
            {
                count++;
                Debug.Log(count);
                if (count < parsedJLG.AllStatements.Count)
                {
                    js = parsedJLG.AllStatements[count];
                }
                else
                {
                    return false;
                }
            }
            if (count <= 300 || (count - statementCounter) < 200)
            {
                while (statementCounter <= count)
                {
                    UpdateVisualization();
                }
            }
            else if ((count - statementCounter) > 200)
            {

                statementCounter = count - 150;
                parsedJLG.AllStatements.RemoveRange(0, statementCounter - 1);
                ResetVisualization();
                while (statementCounter <= 151)
                {
                    UpdateVisualization();
                }
            }
            return true;
        }

        private void OneStep(Boolean direction)
        {
            Boolean saveDirection = playingForward;
            playingForward = direction;
            if (!playingForward == lastDirection)
            {
                if (direction)
                {
                    statementCounter += 2;
                }
                else
                {
                    statementCounter -= 2;
                }
            }
            UpdateVisualization();
            playingForward = saveDirection;
        }

        /// <summary>
        /// Speeds up the playing speed of the visualization by x2 until a max of updating every 0.03125 seconds or 32 statements per second.
        /// </summary>
        private void SpeedUp()
        {
            if (updateIntervall > 0.03125)
            {
                nextUpdateTime = nextUpdateTime - updateIntervall + (updateIntervall / 2);
                updateIntervall = updateIntervall / 2;
            }
        }

        /// <summary>
        /// Slows down the playing speed of the visualization by x2 until a minimum of updating every 8 seconds or 1 statement every 8 seconds.
        /// </summary>
        private void SlowDown()
        {
            if (updateIntervall < 8)
            {
                nextUpdateTime = nextUpdateTime - updateIntervall + (updateIntervall * 2);
                updateIntervall = updateIntervall * 2;
            }
        }

        /// <summary>
        /// This method resets the visualization by destroying all Textwindows and functionCalls and setting the StatementCounter to 0.
        /// </summary>
        private void ResetVisualization()
        {
            foreach (GameObject go in textWindows)
            {
                GameObject.Destroy(go);
            }
            textWindows = new Stack<GameObject>();
            foreach (GameObject go in functionCalls)
            {
                GameObject.Destroy(go);
            }
            functionCalls = new Stack<GameObject>();
            statementCounter = 0;
        }

        /// <summary>
        /// This Method does a complete reset by calling ResetVisualization() and reparsing the JLG-File by calling Start().
        /// </summary>
        private void ResetComplete()
        {
            ResetVisualization();
            Start();
            foreach (GameObject go in nodesGOs.Values)
            {
                go.GetComponentInChildren<MeshRenderer>().material.color = new Color(1f, 0f, 0f, 1f);
            }
        }
    }
}
