#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;
using SEE.Controls.KeyActions;
using SEE.Game.City;
using SEE.Utils;

namespace SEE.Tools.AI
{
    /// <summary>
    /// The "Ask SEE" box: press the key of <see cref="KeyAction.AskSee"/> (default J) in SEE,
    /// type a question about the
    /// code and press Enter. The question goes to Claude Code, which runs in the background
    /// (headless, <c>claude -p</c>) in the repository folder with the <c>see</c> MCP server
    /// from <c>.mcp.json</c>. Claude searches the code and calls the SEE tools, so the
    /// buildings light up while it works; its answer then appears in the box.
    ///
    /// Claude may only read code (Read, Grep, Glob) and use the SEE tools; it cannot edit
    /// files or run commands itself. The SEE tool <c>probe_run</c> builds and runs the loaded
    /// project, but only in a disposable copy. Claude may read the repository and the source
    /// code directories of the loaded cities. Follow-up questions continue the same
    /// conversation until "New conversation" is pressed.
    ///
    /// Requires the Claude Code command-line tool, signed in once with <c>claude</c> → <c>/login</c>.
    /// Only compiled into the Editor and development builds, like <see cref="AgentCommandServer"/>.
    /// </summary>
    internal class AskSeeWindow : MonoBehaviour
    {
        #region Settings

        /// <summary>
        /// The Claude Code executable: "claude" (searched on PATH and in the usual install
        /// folders) or a full path.
        /// </summary>
        [SerializeField, Tooltip("The Claude Code executable: \"claude\" (searched on PATH and in the usual install folders) or a full path.")]
        internal string ClaudeExecutable = "claude";

        /// <summary>Model passed to Claude Code; empty uses Claude Code's default.</summary>
        [SerializeField, Tooltip("Model passed to Claude Code; empty uses Claude Code's default.")]
        internal string Model = "";

        /// <summary>A question is stopped after this many seconds.</summary>
        [SerializeField, Tooltip("A question is stopped after this many seconds.")]
        internal int TimeoutSeconds = 240;

        /// <summary>Font size of the box.</summary>
        [SerializeField, Tooltip("Font size of the box.")]
        internal int FontSize = 15;

        #endregion

        /// <summary>The single instance, created automatically when the game starts.</summary>
        internal static AskSeeWindow Instance { get; private set; }

        /// <summary>Extra instructions for Claude, on top of those of the see MCP server.</summary>
        private const string systemPrompt =
            "You are answering a question typed into SEE, a 3D code city in Unity. The user is "
            + "looking at the city, not at your tool calls. Find the answer in the source code with "
            + "Read, Grep and Glob, then show it in the city with the see tools: find_symbol, then "
            + "focus (or show_path, show_neighbors or highlight when that fits better). The city may "
            + "be built from an older version of the code; if no building matches, say so and focus "
            + "the closest match. For a path, give show_path the start and the end and report the "
            + "path it returns; never invent a dependency or a step. For questions about what happens "
            + "at runtime (which code runs, in which order, with which values), use probe_run as the "
            + "see instructions describe; if the user debugs in SEE, use the debug tools. "
            + "Match the length of your reply "
            + "to the question: for 'where is' questions, at most four short sentences that name the file and line; for 'how', "
            + "'why' or 'explain' questions, explain clearly in up to about fifteen sentences or a "
            + "short list (lines starting with '- '), citing files and lines. Use plain text: no "
            + "Markdown headings, bold, tables or code blocks, because the reply is shown as plain text.";

        /// <summary>Name of the question text field, for keyboard focus.</summary>
        private const string fieldName = "AskSeeQuestion";

        /// <summary>Whether the box is shown.</summary>
        private bool open;

        /// <summary>Frame in which the box was opened; that frame's key press is not typed into the field.</summary>
        private int openedFrame = -1;

        /// <summary>The question being typed.</summary>
        private string question = "";

        /// <summary>One question of the conversation, what Claude did for it, and its answer.</summary>
        private class Exchange
        {
            /// <summary>The question as typed.</summary>
            internal string Question;

            /// <summary>What Claude did, one line per step.</summary>
            internal readonly List<string> Steps = new();

            /// <summary>Claude's answer or an error message; empty while waiting.</summary>
            internal string Answer = "";

            /// <summary>Whether <see cref="Answer"/> is an error message.</summary>
            internal bool IsError;
        }

        /// <summary>The current conversation, oldest question first.</summary>
        private readonly List<Exchange> conversation = new();

        /// <summary>The newest exchange, or null if nothing was asked yet.</summary>
        private Exchange Current => conversation.Count > 0 ? conversation[^1] : null;

        /// <summary>One-line status under the buttons.</summary>
        private string status = "Ask anything about the code.";

        /// <summary>Whether the box is shown in its large size.</summary>
        private bool large;

        /// <summary>Whether the question field should get the keyboard focus at the next repaint.</summary>
        private bool focusQuestion;

        /// <summary>Claude Code's session, for follow-up questions; null starts a new conversation.</summary>
        private string sessionId;

        /// <summary>The running Claude Code process, or null.</summary>
        private Process process;

        /// <summary>Whether the running process has sent its final result.</summary>
        private bool gotResult;

        /// <summary>When the running question was sent (realtime seconds).</summary>
        private float startedAt;

        /// <summary>Lines from Claude Code's standard output, filled by a background thread.</summary>
        private readonly ConcurrentQueue<string> outputLines = new();

        /// <summary>Lines from Claude Code's standard error, filled by a background thread.</summary>
        private readonly ConcurrentQueue<string> errorLines = new();

        /// <summary>The last few lines of standard error, shown if Claude Code fails.</summary>
        private readonly List<string> recentErrors = new();

        /// <summary>Position and size of the box.</summary>
        private Rect windowRect = new(20, 20, 620, 200);

        /// <summary>Scroll position of the answer area.</summary>
        private Vector2 scroll;

        // The styles below are created on first use inside OnGUI (see CreateStyles).

        /// <summary>Style of the box itself, including its title.</summary>
        private GUIStyle windowStyle;

        /// <summary>Style of the explanation above the question field.</summary>
        private GUIStyle labelStyle;

        /// <summary>Style of the question field.</summary>
        private GUIStyle fieldStyle;

        /// <summary>Style of the buttons.</summary>
        private GUIStyle buttonStyle;

        /// <summary>Style of the steps Claude took for the newest question.</summary>
        private GUIStyle stepStyle;

        /// <summary>Style of the status line.</summary>
        private GUIStyle statusStyle;

        /// <summary>Style of the warning shown when the command server is not listening.</summary>
        private GUIStyle warningStyle;

        /// <summary>Style of a question in the conversation.</summary>
        private GUIStyle questionStyle;

        /// <summary>Style of an answer in the conversation.</summary>
        private GUIStyle answerStyle;

        /// <summary>Style of an error message in the conversation.</summary>
        private GUIStyle errorAnswerStyle;

        /// <summary>Style of the grey hint inside the empty question field.</summary>
        private GUIStyle hintStyle;

        /// <summary>Style of the line saying that SEE's keys are paused while typing.</summary>
        private GUIStyle typingStyle;

        /// <summary>Style of the line saying that the user is not typing.</summary>
        private GUIStyle notTypingStyle;

        /// <summary>Style of the small Copy buttons below the answers.</summary>
        private GUIStyle smallButtonStyle;

        /// <summary>Solid-colour textures for the styles; Unity's default skin is see-through.</summary>
        private readonly List<Texture2D> textures = new();

        /// <summary>Whether a question is running.</summary>
        private bool Running => process != null;

        /// <summary>Whether SEE's command server can receive the agent's commands.</summary>
        private static bool CommandServerRunning => AgentCommandServer.Instance != null && AgentCommandServer.Instance.IsListening;

        #region Startup and shutdown

        /// <summary>Creates the box automatically, so no scene has to be edited.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateInstance()
        {
            if (Instance != null)
            {
                return;
            }
            GameObject holder = new(nameof(AskSeeWindow));
            DontDestroyOnLoad(holder);
            Instance = holder.AddComponent<AskSeeWindow>();
        }

        /// <summary>Tells SEE that the mouse is over its UI while it is over the box.</summary>
        private void OnEnable()
        {
            Raycasting.MouseOverImmediateModeGUI += IsMouseOverBox;
        }

        /// <summary>Stops telling SEE about the box.</summary>
        private void OnDisable()
        {
            Raycasting.MouseOverImmediateModeGUI -= IsMouseOverBox;
        }

        /// <summary>
        /// Whether the box is open and the mouse is over it. SEE then treats the mouse as being
        /// over its UI, so a click on the box does not also select a building behind it, and the
        /// mouse wheel scrolls the answers instead of zooming the city.
        /// </summary>
        /// <returns>True if the mouse is over the open box.</returns>
        private bool IsMouseOverBox()
        {
            if (!open)
            {
                return false;
            }
            // Input uses a bottom-left origin, OnGUI a top-left one.
            Vector2 mouse = Input.mousePosition;
            return windowRect.Contains(new Vector2(mouse.x, Screen.height - mouse.y));
        }

        /// <summary>Stops a running question and gives the keyboard back to SEE.</summary>
        private void OnDestroy()
        {
            StopProcess();
            foreach (Texture2D texture in textures)
            {
                Destroyer.Destroy(texture);
            }
            if (open)
            {
                SEEInput.KeyboardShortcutsEnabled = true;
            }
            if (Instance == this)
            {
                Instance = null;
            }
        }

        #endregion

        #region Opening and closing

        /// <summary>Opens the box and stops SEE from reacting to the keys typed into it.</summary>
        internal void Open()
        {
            open = true;
            openedFrame = Time.frameCount;
            focusQuestion = true;
            SEEInput.KeyboardShortcutsEnabled = false;
        }

        /// <summary>Closes the box; a running question keeps running and its answer is kept.</summary>
        internal void Close()
        {
            open = false;
            SEEInput.KeyboardShortcutsEnabled = true;
            GUI.FocusControl(null);
        }

        #endregion

        #region Main loop

        /// <summary>Opens the box on the key of <see cref="KeyAction.AskSee"/> and processes Claude Code's output.</summary>
        private void Update()
        {
            if (!open && SEEInput.OpenAskSee())
            {
                Open();
            }
            if (open)
            {
                // Something else in SEE may turn the shortcuts back on (e.g. when one of its own
                // input fields loses focus); typing "w" into the box must not walk the player.
                SEEInput.KeyboardShortcutsEnabled = false;
            }

            while (outputLines.TryDequeue(out string line))
            {
                HandleOutputLine(line);
            }
            while (errorLines.TryDequeue(out string line))
            {
                recentErrors.Add(line);
                if (recentErrors.Count > 8)
                {
                    recentErrors.RemoveAt(0);
                }
            }

            if (Running)
            {
                float elapsed = Time.realtimeSinceStartup - startedAt;
                if (process.HasExited && outputLines.IsEmpty)
                {
                    if (!gotResult)
                    {
                        string details = string.Join("\n", recentErrors.Where(l => !string.IsNullOrWhiteSpace(l)));
                        Finish($"Claude Code stopped without an answer (exit code {process.ExitCode})."
                               + (details.Length > 0 ? "\n" + details : ""), error: true);
                    }
                    else
                    {
                        DisposeProcess();
                    }
                }
                else if (elapsed > TimeoutSeconds)
                {
                    StopProcess();
                    Finish($"Stopped after {TimeoutSeconds} s without an answer.", error: true);
                }
                else if (!gotResult)
                {
                    status = $"Claude is working… {elapsed:F0} s";
                }
            }
        }

        #endregion

        /// <summary>The name of the key that opens the box, for the title.</summary>
        /// <returns>The key bound to <see cref="KeyAction.AskSee"/>, e.g. "J".</returns>
        private static string OpenKeyName() =>
            KeyBindings.TryGetKeyCode(KeyAction.AskSee, out KeyCode key) ? key.ToString() : "J";

        #region Asking Claude Code

        /// <summary>Sends <see cref="question"/> to Claude Code.</summary>
        private void Ask()
        {
            string text = question.Trim();
            if (text.Length == 0 || Running)
            {
                return;
            }

            conversation.Add(new() { Question = text });
            question = "";
            recentErrors.Clear();
            gotResult = false;
            startedAt = Time.realtimeSinceStartup;
            ScrollToEnd();

            string executable = FindClaude();
            if (executable == null)
            {
                Finish("Claude Code was not found. Install its command-line tool "
                       + "(https://docs.claude.com/en/docs/claude-code/setup), or set ClaudeExecutable on the "
                       + $"{nameof(AskSeeWindow)} object to its full path.", error: true);
                return;
            }

            string repository = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string mcpConfig = WriteMcpConfig(repository, out string mcpProblem);
            if (mcpConfig == null)
            {
                Finish(mcpProblem, error: true);
                return;
            }
            List<string> arguments = new()
            {
                "-p", "--output-format", "stream-json", "--verbose",
                "--mcp-config", Quote(mcpConfig), "--strict-mcp-config",
                // The wildcard allows every tool of the see server, including tools added later
                // (node_at, probe_run, the debugger tools, ...).
                "--allowedTools", Quote("mcp__see__*"), "Read", "Grep", "Glob",
                "--disallowedTools", "Bash", "Edit", "Write", "NotebookEdit", "WebFetch", "WebSearch",
                "--append-system-prompt", Quote(systemPrompt)
            };
            // The loaded project may lie outside the repository (e.g. a city of another program);
            // without this, Claude could not read its files and build scripts.
            foreach (string directory in LoadedSourceDirectories(repository))
            {
                arguments.Add("--add-dir");
                arguments.Add(Quote(directory));
            }
            if (!string.IsNullOrEmpty(sessionId))
            {
                arguments.Add("--resume");
                arguments.Add(Quote(sessionId));
            }
            if (!string.IsNullOrWhiteSpace(Model))
            {
                arguments.Add("--model");
                arguments.Add(Quote(Model.Trim()));
            }

            (string fileName, string commandLine) = ExecutableLocator.CommandLineFor(executable, string.Join(" ", arguments));
            ProcessStartInfo startInfo = new(fileName, commandLine)
            {
                WorkingDirectory = repository,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
            // Unity may run with a minimal PATH (e.g. when started from the Hub on macOS);
            // Claude Code and the tools it starts (node, python) must still be found.
            startInfo.EnvironmentVariables["PATH"] = ExecutableLocator.ChildPath(executable);

            status = "Starting Claude Code…";

            try
            {
                process = new() { StartInfo = startInfo };
                process.OutputDataReceived += (_, e) => { if (e.Data != null) { outputLines.Enqueue(e.Data); } };
                process.ErrorDataReceived += (_, e) => { if (e.Data != null) { errorLines.Enqueue(e.Data); } };
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // The question goes through standard input, so no quoting problems.
                byte[] bytes = new UTF8Encoding(false).GetBytes(text);
                process.StandardInput.BaseStream.Write(bytes, 0, bytes.Length);
                process.StandardInput.Close();
            }
            catch (Exception e)
            {
                DisposeProcess();
                Finish($"Could not start Claude Code ({executable}): {e.Message}", error: true);
                return;
            }
            Debug.Log($"[{nameof(AskSeeWindow)}] Asked: {text}\n");
        }

        /// <summary>Handles one JSON line of Claude Code's stream-json output.</summary>
        /// <param name="line">One output line.</param>
        private void HandleOutputLine(string line)
        {
            JObject message;
            try
            {
                message = JObject.Parse(line);
            }
            catch (Exception)
            {
                return; // not JSON; ignore
            }

            switch ((string)message["type"])
            {
                case "system" when (string)message["subtype"] == "init":
                    sessionId = (string)message["session_id"] ?? sessionId;
                    JToken see = message["mcp_servers"]?.FirstOrDefault(s => (string)s["name"] == "see");
                    if (see == null || (string)see["status"] != "connected")
                    {
                        AddStep($"! SEE tools are not connected ({(string)see?["status"] ?? "missing"}).");
                    }
                    break;

                case "assistant":
                    foreach (JToken content in message["message"]?["content"] ?? new JArray())
                    {
                        if ((string)content["type"] == "tool_use")
                        {
                            AddStep("→ " + DescribeToolUse((string)content["name"], content["input"] as JObject));
                        }
                    }
                    break;

                case "user":
                    foreach (JToken content in message["message"]?["content"] as JArray ?? new JArray())
                    {
                        if ((string)content["type"] == "tool_result" && (bool?)content["is_error"] == true)
                        {
                            AddStep("   ✗ " + Shorten(ToolResultText(content["content"]), 140));
                        }
                    }
                    break;

                case "result":
                    gotResult = true;
                    sessionId = (string)message["session_id"] ?? sessionId;
                    string result = (string)message["result"] ?? "";
                    bool isError = (bool?)message["is_error"] ?? false;
                    if (result.Contains("Not logged in"))
                    {
                        sessionId = null;
                        result = "Claude Code is not signed in on this computer. Open a terminal, run "
                                 + "'claude', type /login and sign in once. Then ask again.";
                        isError = true;
                    }
                    Finish(result, isError);
                    break;
            }
        }

        /// <summary>Shows the answer and ends the running question.</summary>
        /// <param name="text">Answer or error message.</param>
        /// <param name="error">Whether it is an error.</param>
        private void Finish(string text, bool error)
        {
            if (Current == null)
            {
                conversation.Add(new() { Question = "" });
            }
            Current.Answer = text;
            Current.IsError = error;
            ScrollToEnd();
            focusQuestion = true;
            float elapsed = Time.realtimeSinceStartup - startedAt;
            status = error ? "Something went wrong." : $"Answered in {elapsed:F0} s. Ask a follow-up, or start a new conversation.";
            if (process != null && process.HasExited)
            {
                DisposeProcess();
            }
            Debug.Log($"[{nameof(AskSeeWindow)}] {(error ? "Error" : "Answer")}: {text}\n");
        }

        /// <summary>Adds a line to the steps of the current question and scrolls to it.</summary>
        /// <param name="step">What Claude did.</param>
        private void AddStep(string step)
        {
            Current?.Steps.Add(step);
            ScrollToEnd();
        }

        /// <summary>Scrolls the conversation to its end at the next repaint.</summary>
        private void ScrollToEnd()
        {
            scroll.y = float.MaxValue;
        }

        /// <summary>Kills the running Claude Code process, if any.</summary>
        private void StopProcess()
        {
            try
            {
                if (process is { HasExited: false })
                {
                    process.Kill();
                }
            }
            catch (Exception)
            {
                // already gone
            }
            DisposeProcess();
        }

        /// <summary>Releases the process object.</summary>
        private void DisposeProcess()
        {
            process?.Dispose();
            process = null;
        }

        /// <summary>
        /// The Claude Code executable: <see cref="ClaudeExecutable"/> if it is a full path,
        /// otherwise searched on PATH and in the folders the installers use on Windows, macOS
        /// and Linux (see <see cref="ExecutableLocator"/>). Unity may have been started before
        /// Claude Code was installed, or with a minimal PATH, so PATH alone is not enough.
        /// </summary>
        /// <returns>Full path, or null if not found.</returns>
        private string FindClaude() => ExecutableLocator.FindClaude(ClaudeExecutable);

        /// <summary>
        /// The source code directories of the cities that have a loaded graph, if they exist
        /// and lie outside <paramref name="repository"/>. Cities without a graph are skipped:
        /// their directory is only a default from the scene (possibly of another computer).
        /// </summary>
        /// <param name="repository">The folder Claude Code runs in; it can be read anyway.</param>
        /// <returns>Absolute paths without a trailing separator (which would escape the closing
        /// quote on the command line), without duplicates.</returns>
        private static IEnumerable<string> LoadedSourceDirectories(string repository)
        {
            string repositoryPrefix = Path.GetFullPath(repository).TrimEnd('\\', '/') + Path.DirectorySeparatorChar;
            HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
            foreach (AbstractSEECity city in FindObjectsByType<AbstractSEECity>(FindObjectsSortMode.None))
            {
                string directory = city.LoadedGraph != null ? SourceLocator.SourceDirectoryOf(city) : null;
                if (directory == null || !Directory.Exists(directory))
                {
                    continue;
                }
                directory = Path.GetFullPath(directory).TrimEnd('\\', '/');
                bool insideRepository = (directory + Path.DirectorySeparatorChar)
                    .StartsWith(repositoryPrefix, StringComparison.OrdinalIgnoreCase);
                if (directory.Length > 0 && !insideRepository && seen.Add(directory))
                {
                    yield return directory;
                }
            }
        }

        /// <summary>
        /// Writes the MCP configuration for Claude Code into Unity's temporary cache folder and
        /// returns its path. Unlike the repository's <c>.mcp.json</c>, which runs <c>python</c>,
        /// it names the Python interpreter found on this computer (<c>python3</c> on most macOS
        /// and Linux systems) and the absolute path of the MCP server, and passes the port of
        /// <see cref="AgentCommandServer"/>.
        /// </summary>
        /// <param name="repository">The root folder of the SEE repository.</param>
        /// <param name="problem">Why no configuration could be written; null on success.</param>
        /// <returns>Path of the configuration file, or null on failure.</returns>
        private static string WriteMcpConfig(string repository, out string problem)
        {
            string server = Path.Combine(repository, "Tools", "SeeMcp", "see_mcp_server.py");
            if (!File.Exists(server))
            {
                problem = $"The SEE MCP server was not found at {server}.";
                return null;
            }
            string python = ExecutableLocator.FindPython();
            if (python == null)
            {
                problem = "Python 3 was not found, but the SEE tools need it. Install Python 3 "
                          + "(python3 on macOS and Linux, python or py on Windows) and ask again.";
                return null;
            }
            int port = AgentCommandServer.Instance != null ? AgentCommandServer.Instance.Port
                                                           : AgentCommandServer.DefaultPort;
            JObject config = new()
            {
                ["mcpServers"] = new JObject
                {
                    ["see"] = new JObject
                    {
                        ["command"] = python,
                        ["args"] = new JArray(server),
                        ["env"] = new JObject { ["SEE_AGENT_PORT"] = port.ToString() }
                    }
                }
            };
            try
            {
                string path = Path.Combine(Application.temporaryCachePath, "ask-see-mcp.json");
                File.WriteAllText(path, config.ToString(), new UTF8Encoding(false));
                problem = null;
                return path;
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                problem = $"Could not write the MCP configuration: {e.Message}";
                return null;
            }
        }

        #endregion

        #region Drawing

        /// <summary>Draws the box.</summary>
        private void OnGUI()
        {
            if (!open)
            {
                return;
            }
            CreateStyles();
            windowRect.width = Mathf.Min(large ? 1000 : 620, Screen.width - 40);
            // GUILayout.Window only grows to fit its content; starting small every frame lets it
            // also shrink, e.g. after switching back from the large size.
            windowRect.height = 50;
            windowRect = GUILayout.Window(GetInstanceID(), windowRect, DrawWindowWithCursor,
                                          $"Ask SEE   ({OpenKeyName()} opens · Enter asks · Esc closes)", windowStyle);
        }

        /// <summary>
        /// Draws the box with a black, blinking text cursor. The cursor is white by default,
        /// which is invisible in the white text field. It has to be set here: Unity calls window
        /// functions after OnGUI has returned, so a setting made around GUILayout.Window would
        /// already be undone.
        /// </summary>
        /// <param name="_">Window ID (unused).</param>
        private void DrawWindowWithCursor(int _)
        {
            Color cursorColor = GUI.skin.settings.cursorColor;
            float cursorFlashSpeed = GUI.skin.settings.cursorFlashSpeed;
            GUI.skin.settings.cursorColor = Color.black;
            GUI.skin.settings.cursorFlashSpeed = 1.5f; // blinks per second, like a normal text field
            try
            {
                DrawWindow();
            }
            finally
            {
                GUI.skin.settings.cursorColor = cursorColor;
                GUI.skin.settings.cursorFlashSpeed = cursorFlashSpeed;
            }
        }

        /// <summary>Draws the content of the box.</summary>
        private void DrawWindow()
        {
            Event current = Event.current;
            if (current.type is EventType.ValidateCommand or EventType.ExecuteCommand)
            {
                HandleEditCommand(current);
            }
            if (current.type == EventType.KeyDown)
            {
                if (Time.frameCount == openedFrame)
                {
                    current.Use(); // the key that opened the box must not be typed into it
                }
                else if (current.keyCode == KeyCode.Escape)
                {
                    current.Use();
                    Close();
                    return;
                }
                else if ((current.keyCode == KeyCode.Return || current.keyCode == KeyCode.KeypadEnter)
                         && GUI.GetNameOfFocusedControl() == fieldName)
                {
                    current.Use();
                    Ask();
                }
            }

            GUILayout.Label("Ask anything about the code: where something is, how it works, what uses it. "
                            + "Claude reads the code and shows the answer in the city.", labelStyle);
            GUI.SetNextControlName(fieldName);
            question = GUILayout.TextField(question, 1000, fieldStyle, GUILayout.ExpandWidth(true));
            Rect fieldRect = GUILayoutUtility.GetLastRect();
            bool typing = GUI.GetNameOfFocusedControl() == fieldName;
            if (question.Length == 0 && current.type == EventType.Repaint)
            {
                // Grey hint inside the empty field, like a search box.
                Rect hintRect = new(fieldRect.x + fieldStyle.padding.left + 2, fieldRect.y + fieldStyle.padding.top,
                                    fieldRect.width - fieldStyle.padding.horizontal, fieldRect.height - fieldStyle.padding.vertical);
                GUI.Label(hintRect, typing ? "Type your question here and press Enter" : "Click here to type a question",
                          hintStyle);
            }
            // Focus the question field when the box opens and after each answer, but not every
            // frame: the user must be able to click into an answer to select and copy it.
            if (focusQuestion && Time.frameCount > openedFrame && current.type == EventType.Repaint)
            {
                GUI.FocusControl(fieldName);
                focusQuestion = false;
            }
            GUILayout.Label(typing
                                ? "● Typing: SEE's keys are paused until you close this box (Esc)."
                                : "○ Not typing: click the field above to type.",
                            typing ? typingStyle : notTypingStyle);

            GUILayout.BeginHorizontal();
            GUI.enabled = !Running && question.Trim().Length > 0;
            if (GUILayout.Button("Ask", buttonStyle))
            {
                Ask();
            }
            GUI.enabled = Running;
            if (GUILayout.Button("Stop", buttonStyle))
            {
                StopProcess();
                Finish("Stopped.", error: true);
            }
            GUI.enabled = !Running && sessionId != null;
            if (GUILayout.Button("New conversation", buttonStyle))
            {
                sessionId = null;
                conversation.Clear();
                status = "New conversation. Ask anything about the code.";
                focusQuestion = true;
            }
            GUI.enabled = AgentCityEffects.Instance != null;
            if (GUILayout.Button("Reset city", buttonStyle))
            {
                AgentCityEffects.Instance.ResetEffects();
            }
            GUI.enabled = true;
            if (GUILayout.Button("Close", buttonStyle))
            {
                // End the group opened above; returning without it unbalances IMGUI's layout stack.
                GUILayout.EndHorizontal();
                Close();
                return;
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(status, statusStyle, GUILayout.ExpandWidth(true));
            string lastAnswer = conversation.LastOrDefault(e => e.Answer.Length > 0 && !e.IsError)?.Answer;
            GUI.enabled = lastAnswer != null;
            if (GUILayout.Button("Copy last answer", buttonStyle, GUILayout.ExpandWidth(false)))
            {
                CopyToClipboard(lastAnswer);
            }
            GUI.enabled = true;
            if (GUILayout.Button(large ? "Smaller" : "Larger", buttonStyle, GUILayout.ExpandWidth(false)))
            {
                large = !large;
            }
            GUILayout.EndHorizontal();

            if (!CommandServerRunning)
            {
                GUILayout.Label($"! SEE's command server is not listening (port {AgentCommandServer.DefaultPort}), "
                                + "so Claude can answer but cannot highlight buildings. It retries every 2 s; "
                                + "see the Console for the reason.", warningStyle);
            }

            if (conversation.Count > 0)
            {
                float maxHeight = Screen.height * (large ? 0.75f : 0.45f);
                scroll = GUILayout.BeginScrollView(scroll, GUILayout.MinHeight(large ? maxHeight : 80),
                                                   GUILayout.MaxHeight(maxHeight));
                foreach (Exchange exchange in conversation)
                {
                    if (exchange.Question.Length > 0)
                    {
                        GUILayout.Label("Q: " + exchange.Question, questionStyle);
                    }
                    // Only the newest question shows what Claude did; older ones keep just the answer.
                    if (exchange == Current)
                    {
                        foreach (string step in exchange.Steps)
                        {
                            GUILayout.Label(step, stepStyle);
                        }
                    }
                    else if (exchange.Steps.Count > 0)
                    {
                        GUILayout.Label($"({exchange.Steps.Count} step(s))", stepStyle);
                    }
                    if (exchange.Answer.Length > 0)
                    {
                        GUILayout.Space(2);
                        // A text area instead of a label, so the answer can be selected and copied
                        // (Ctrl+C). Changes typed into it are thrown away.
                        GUILayout.TextArea(exchange.Answer, exchange.IsError ? errorAnswerStyle : answerStyle);
                        GUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Copy", smallButtonStyle, GUILayout.ExpandWidth(false)))
                        {
                            CopyToClipboard(exchange.Answer);
                        }
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.Space(10);
                }
                GUILayout.EndScrollView();
            }

            GUI.DragWindow();
        }

        /// <summary>
        /// Handles Copy, Cut, Paste and Select All for the question field and the answers.
        /// In the Unity Editor, Ctrl+C/X/V/A are taken by the Editor's Edit menu, which sends
        /// them to the Game view as commands instead of key presses. Unity's runtime text fields
        /// only react to key presses, so without this, copying does nothing in the Editor.
        /// (A built SEE gets the key presses, which the text fields handle themselves.)
        /// </summary>
        /// <param name="current">A ValidateCommand or ExecuteCommand event.</param>
        private void HandleEditCommand(Event current)
        {
            string command = current.commandName;
            if (command is not ("Copy" or "Cut" or "Paste" or "SelectAll") || GUIUtility.keyboardControl == 0)
            {
                return;
            }
            if (GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl) is not TextEditor editor)
            {
                return;
            }
            // Answers are read-only: only the question field may be changed.
            bool inQuestion = GUI.GetNameOfFocusedControl() == fieldName;
            if (command is "Cut" or "Paste" && !inQuestion)
            {
                return;
            }
            if (current.type == EventType.ValidateCommand)
            {
                current.Use(); // tells the Editor that the box handles this command
                return;
            }

            switch (command)
            {
                case "Copy":
                    CopyToClipboard(editor.hasSelection ? editor.SelectedText : editor.text);
                    break;
                case "Cut":
                    if (editor.hasSelection)
                    {
                        CopyToClipboard(editor.SelectedText);
                        editor.DeleteSelection();
                        question = editor.text;
                    }
                    break;
                case "Paste":
                    // Questions are one line; pasted line breaks become spaces.
                    string pasted = (GUIUtility.systemCopyBuffer ?? "").Replace("\r\n", " ").Replace('\n', ' ');
                    editor.ReplaceSelection(pasted);
                    question = editor.text;
                    break;
                case "SelectAll":
                    editor.SelectAll();
                    break;
            }
            current.Use();
        }

        /// <summary>Puts <paramref name="text"/> on the clipboard and says so in the status line and Console.</summary>
        /// <param name="text">Text to copy.</param>
        private void CopyToClipboard(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }
            GUIUtility.systemCopyBuffer = text;
            status = $"Copied {text.Length} characters.";
            Debug.Log($"[{nameof(AskSeeWindow)}] Copied {text.Length} characters to the clipboard.\n");
        }

        /// <summary>Creates the styles once (GUI.skin is only available inside OnGUI).</summary>
        private void CreateStyles()
        {
            if (labelStyle != null && labelStyle.fontSize == FontSize)
            {
                return;
            }
            foreach (Texture2D texture in textures)
            {
                Destroyer.Destroy(texture);
            }
            textures.Clear();

            Color text = new(0.95f, 0.95f, 0.95f);

            windowStyle = new(GUI.skin.window)
            {
                fontSize = FontSize,
                fontStyle = FontStyle.Bold,
                padding = new(12, 12, FontSize + 16, 12),
                border = new(2, 2, 2, 2)
            };
            SetBackground(windowStyle, new(0.11f, 0.12f, 0.15f, 0.97f), text);

            labelStyle = new(GUI.skin.label) { fontSize = FontSize, wordWrap = true, richText = false };
            labelStyle.normal.textColor = text;

            fieldStyle = new(GUI.skin.textField)
            {
                fontSize = FontSize + 1,
                padding = new(8, 8, 6, 6),
                border = new(1, 1, 1, 1)
            };
            SetBackground(fieldStyle, Color.white, Color.black);
            fieldStyle.focused.background = Solid(new(1f, 1f, 0.92f));
            fieldStyle.focused.textColor = Color.black;

            buttonStyle = new(GUI.skin.button) { fontSize = FontSize, padding = new(10, 10, 5, 5) };
            SetBackground(buttonStyle, new(0.24f, 0.27f, 0.33f), text);
            buttonStyle.hover.background = Solid(new(0.30f, 0.40f, 0.52f));
            buttonStyle.hover.textColor = Color.white;
            buttonStyle.active.background = Solid(new(0.15f, 0.55f, 0.75f));
            buttonStyle.active.textColor = Color.white;

            smallButtonStyle = new(buttonStyle) { fontSize = FontSize - 3, padding = new(8, 8, 2, 2) };

            stepStyle = new(labelStyle) { fontSize = FontSize - 2 };
            stepStyle.normal.textColor = new(0.55f, 0.85f, 1f);

            statusStyle = new(labelStyle) { fontStyle = FontStyle.Italic, fontSize = FontSize - 1 };
            statusStyle.normal.textColor = new(0.75f, 0.78f, 0.82f);

            warningStyle = new(labelStyle) { fontSize = FontSize - 1, fontStyle = FontStyle.Bold };
            warningStyle.normal.textColor = new(1f, 0.7f, 0.3f);

            hintStyle = new(GUI.skin.label) { fontSize = FontSize + 1, fontStyle = FontStyle.Italic, wordWrap = false };
            hintStyle.normal.textColor = new(0.55f, 0.55f, 0.55f);

            typingStyle = new(labelStyle) { fontSize = FontSize - 2, fontStyle = FontStyle.Bold };
            typingStyle.normal.textColor = new(0.45f, 0.95f, 0.5f);

            notTypingStyle = new(labelStyle) { fontSize = FontSize - 2 };
            notTypingStyle.normal.textColor = new(0.7f, 0.7f, 0.7f);

            questionStyle = new(labelStyle) { fontStyle = FontStyle.Bold };
            questionStyle.normal.textColor = new(1f, 0.86f, 0.45f);

            // A text area that looks like a label: no box, wrapped, selectable.
            answerStyle = new(GUI.skin.textArea)
            {
                fontSize = FontSize,
                wordWrap = true,
                richText = false,
                padding = new(2, 2, 2, 2),
                margin = new(4, 4, 2, 2)
            };
            SetBackground(answerStyle, new(0.15f, 0.16f, 0.20f), text);

            errorAnswerStyle = new(answerStyle);
            SetBackground(errorAnswerStyle, new(0.25f, 0.14f, 0.12f), new(1f, 0.75f, 0.6f));

            /// <summary>
            /// Gives all states of <paramref name="style"/> the same background and text colour,
            /// so it looks the same whether or not it has keyboard focus or is "on".
            /// </summary>
            /// <param name="style">The style to change.</param>
            /// <param name="background">Background colour.</param>
            /// <param name="textColor">Text colour.</param>
            void SetBackground(GUIStyle style, Color background, Color textColor)
            {
                Texture2D texture = Solid(background);
                foreach (GUIStyleState state in new[] { style.normal, style.onNormal, style.focused, style.onFocused,
                                                       style.hover, style.onHover, style.active, style.onActive })
                {
                    state.background = texture;
                    state.textColor = textColor;
                }
            }

            /// <summary>
            /// Creates a texture of one pixel in <paramref name="color"/> and remembers it in
            /// <see cref="textures"/>, so that it is destroyed with the box.
            /// </summary>
            /// <param name="color">Colour of the texture.</param>
            /// <returns>The new texture.</returns>
            Texture2D Solid(Color color)
            {
                Texture2D texture = new(1, 1) { hideFlags = HideFlags.HideAndDontSave };
                texture.SetPixel(0, 0, color);
                texture.Apply();
                textures.Add(texture);
                return texture;
            }
        }

        #endregion

        #region Helpers

        /// <summary>A short, readable description of a tool call, for the steps list.</summary>
        /// <param name="name">Tool name, e.g. "mcp__see__focus" or "Grep".</param>
        /// <param name="input">The tool's arguments.</param>
        /// <returns>E.g. "SEE: focus ContextMenu" or "Searching code for 'OpenContextMenu'".</returns>
        private static string DescribeToolUse(string name, JObject input)
        {
            input ??= new();
            /// <summary>
            /// The shortened text of an argument of the tool call; the elements of a list are
            /// separated by commas.
            /// </summary>
            /// <param name="key">Name of the argument.</param>
            /// <returns>The text, or an empty string if the argument is missing.</returns>
            string Argument(string key) => input[key] is JArray array
                ? string.Join(", ", array.Select(t => ShortId(t.ToString())))
                : ShortId(input[key]?.ToString() ?? "");

            /// <summary>The file name and line of a location argument, e.g. "Program.cs:25".</summary>
            /// <param name="location">An object with "file" and "line".</param>
            /// <returns>The text.</returns>
            static string Location(JObject location) =>
                $"{Path.GetFileName(location["file"]?.ToString() ?? "")}:{location["line"]}";

            if (name != null && name.StartsWith("mcp__see__"))
            {
                string tool = name.Substring("mcp__see__".Length);
                string detail = Argument("target") + Argument("targets") + Argument("query") + Argument("visited")
                                + Argument("kind");
                if (input["probes"] is JArray probes)
                {
                    // probe_run: the probed locations.
                    detail += string.Join(", ", probes.OfType<JObject>().Select(Location));
                }
                else if (input["file"] != null)
                {
                    // node_at, set_breakpoint and remove_breakpoint.
                    detail += Location(input);
                }
                return $"SEE: {tool} {detail}".TrimEnd();
            }
            return name switch
            {
                "Grep" => $"Searching code for '{Shorten(Argument("pattern"), 60)}'",
                "Glob" => $"Looking for files {Shorten(Argument("pattern"), 60)}",
                "Read" => $"Reading {Path.GetFileName(Argument("file_path"))}",
                _ => name
            };
        }

        /// <summary>
        /// Node IDs from C# graphs look like "T:SEE.UI.ContextMenu@Temp/bin/Debug/SEE.dll";
        /// this keeps "SEE.UI.ContextMenu".
        /// </summary>
        /// <param name="id">Node ID or name.</param>
        /// <returns>The shortened ID.</returns>
        private static string ShortId(string id)
        {
            int at = id.IndexOf('@');
            if (at > 0)
            {
                id = id.Substring(0, at);
            }
            return id.Length > 2 && id[1] == ':' && char.IsUpper(id[0]) ? id.Substring(2) : id;
        }

        /// <summary>Text of a tool result, which is either a string or a list of content blocks.</summary>
        /// <param name="content">The tool result's content.</param>
        /// <returns>Its text.</returns>
        private static string ToolResultText(JToken content) => content switch
        {
            JArray blocks => string.Join(" ", blocks.Select(b => (string)b["text"] ?? "")),
            null => "",
            _ => content.ToString()
        };

        /// <summary>Cuts <paramref name="text"/> to <paramref name="max"/> characters.</summary>
        /// <param name="text">The text.</param>
        /// <param name="max">Maximum length.</param>
        /// <returns>The text, with "…" if it was cut.</returns>
        private static string Shorten(string text, int max) =>
            text.Length <= max ? text : text.Substring(0, max - 1) + "…";

        /// <summary>Quotes a command-line argument for Windows.</summary>
        /// <param name="argument">The argument.</param>
        /// <returns>The quoted argument.</returns>
        private static string Quote(string argument) => "\"" + argument.Replace("\"", "\\\"") + "\"";

        #endregion
    }
}
#endif
