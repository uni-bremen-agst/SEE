#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Newtonsoft.Json.Linq;
using UnityEngine;
using SEE.Controls.Players;
using SEE.Extensions;
using SEE.Game;
using SEE.Game.City;
using SEE.Graphs;
using SEE.UI.Notification;
using SEE.Utils;

namespace SEE.Tools.AI
{
    /// <summary>
    /// The "SEE API" of the Ask SEE plan: a small HTTP server inside SEE that lets an AI agent
    /// (through the Python MCP server in <c>Tools/SeeMcp</c>) query the loaded code cities and
    /// trigger the effects of <see cref="AgentCityEffects"/>.
    ///
    /// Protocol: <c>POST http://127.0.0.1:{Port}/command</c> with the header <c>X-SEE-Agent: 1</c>
    /// and a JSON body <c>{"tool": "focus", "args": {"target": "ContextMenu"}}</c>.
    /// The answer is JSON: <c>{"ok": true, "message": "...", ...}</c>.
    ///
    /// Commands are received on a background thread but executed on Unity's main thread,
    /// because Unity objects may only be touched there.
    ///
    /// Only compiled into the Editor and development builds, and only reachable from this
    /// computer. Requests need the custom header, so a web page in a browser cannot send them.
    /// </summary>
    internal class AgentCommandServer : MonoBehaviour
    {
        /// <summary>Default port; the MCP server uses the same default (env var SEE_AGENT_PORT).</summary>
        internal const int DefaultPort = 8765;

        /// <summary>Port the server listens on (127.0.0.1 only).</summary>
        [SerializeField, Tooltip("Port the server listens on (127.0.0.1 only).")]
        internal int Port = DefaultPort;

        /// <summary>Whether each command shows a short notification in SEE.</summary>
        [SerializeField, Tooltip("Whether each command shows a short notification in SEE.")]
        internal bool ShowNotifications = true;

        /// <summary>How long a request waits for the main thread, in milliseconds.</summary>
        private const int mainThreadTimeoutMs = 15000;

        /// <summary>The single instance, created automatically when the game starts.</summary>
        internal static AgentCommandServer Instance { get; private set; }

        /// <summary>The HTTP listener, or null if not running.</summary>
        private HttpListener listener;

        /// <summary>Background thread accepting requests.</summary>
        private Thread listenerThread;

        /// <summary>Commands waiting for the main thread.</summary>
        private readonly ConcurrentQueue<PendingCommand> queue = new();

        /// <summary>Maps source locations onto nodes for the <c>node_at</c> tool.</summary>
        private readonly SourceLocator sourceLocator = new();

        /// <summary>The debugger tools; created on first use.</summary>
        private AgentDebugger debugger;

        /// <summary>The debugger tools.</summary>
        private AgentDebugger Debugger => debugger ??= new(sourceLocator);

        /// <summary>A command received from the agent, waiting for its answer.</summary>
        private class PendingCommand
        {
            /// <summary>Value of <see cref="State"/>: the command waits for the main thread.</summary>
            internal const int Waiting = 0;

            /// <summary>Value of <see cref="State"/>: the main thread has started the command.</summary>
            internal const int Started = 1;

            /// <summary>
            /// Value of <see cref="State"/>: the request timed out before the command started;
            /// the main thread must skip it.
            /// </summary>
            internal const int Cancelled = 2;

            /// <summary>The parsed request body.</summary>
            internal JObject Request;

            /// <summary>The JSON answer, set by the main thread.</summary>
            internal JObject Response;

            /// <summary>Signalled when <see cref="Response"/> is set.</summary>
            internal readonly ManualResetEventSlim Done = new(false);

            /// <summary>
            /// <see cref="Waiting"/>, <see cref="Started"/> or <see cref="Cancelled"/>. Changed only
            /// with <see cref="Interlocked.CompareExchange(ref int, int, int)"/>, so the listener
            /// thread (on timeout) and the main thread cannot both claim the same command.
            /// </summary>
            internal int State = Waiting;
        }

        #region Startup and shutdown

        /// <summary>Creates the server automatically, so no scene has to be edited.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateInstance()
        {
            if (Instance != null)
            {
                return;
            }
            GameObject holder = new(nameof(AgentCommandServer));
            DontDestroyOnLoad(holder);
            Instance = holder.AddComponent<AgentCommandServer>();
        }

        /// <summary>Seconds between attempts to listen while the port is busy.</summary>
        private const float retrySeconds = 2f;

        /// <summary>When to try again to listen (realtime seconds), while <see cref="listener"/> is null.</summary>
        private float nextAttempt;

        /// <summary>Whether the failure to listen has been logged already (it is logged only once).</summary>
        private bool failureLogged;

        /// <summary>Whether the server is listening for commands.</summary>
        internal bool IsListening => listener is { IsListening: true };

        /// <summary>Starts listening and makes sure the port is released whenever SEE stops.</summary>
        private void Start()
        {
            // The user types the question in another window (Claude Code), so SEE is in the
            // background exactly when the commands arrive. Without this, Update would not run.
            Application.runInBackground = true;

            // OnDestroy is not called when Unity recompiles during Play mode; the old listener
            // would keep the port, and the next Play session could not listen.
#if UNITY_EDITOR
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += StopListening;
#endif
            Application.quitting += StopListening;

            TryStartListening();
        }

        /// <summary>
        /// Tries to listen on <see cref="Port"/>. If the port is still busy (e.g. held by the
        /// previous Play session for a moment), <see cref="Update"/> tries again every
        /// <see cref="retrySeconds"/> seconds.
        /// </summary>
        private void TryStartListening()
        {
            HttpListener newListener = new();
            try
            {
                newListener.Prefixes.Add($"http://127.0.0.1:{Port}/");
                newListener.Start();
            }
            catch (Exception e)
            {
                try
                {
                    newListener.Close();
                }
                catch (Exception)
                {
                    // nothing to release
                }
                if (!failureLogged)
                {
                    Debug.LogWarning($"[{nameof(AgentCommandServer)}] Could not listen on port {Port}: {e.Message} "
                                     + $"Trying again every {retrySeconds:F0} s.\n");
                    failureLogged = true;
                }
                nextAttempt = Time.realtimeSinceStartup + retrySeconds;
                return;
            }

            listener = newListener;
            // The thread gets its own listener, so it ends with that listener even if a new
            // one is started later.
            listenerThread = new(() => Listen(newListener)) { IsBackground = true, Name = nameof(AgentCommandServer) };
            listenerThread.Start();
            failureLogged = false;
            Debug.Log($"[{nameof(AgentCommandServer)}] Listening on http://127.0.0.1:{Port}/command\n");
        }

        /// <summary>Stops listening, releases the port and answers waiting requests.</summary>
        private void StopListening()
        {
            HttpListener old = listener;
            listener = null;
            try
            {
                old?.Stop();
                old?.Close();
            }
            catch (ObjectDisposedException)
            {
                // already closed
            }
            while (queue.TryDequeue(out PendingCommand pending))
            {
                pending.Response = Error("SEE is shutting down.");
                pending.Done.Set();
            }
        }

        /// <summary>Stops listening.</summary>
        private void OnDestroy()
        {
#if UNITY_EDITOR
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload -= StopListening;
#endif
            Application.quitting -= StopListening;
            StopListening();
            if (Instance == this)
            {
                Instance = null;
            }
        }

        #endregion

        #region Background thread

        /// <summary>Accepts requests until <paramref name="httpListener"/> is stopped.</summary>
        /// <param name="httpListener">The listener this thread serves.</param>
        private void Listen(HttpListener httpListener)
        {
            while (httpListener.IsListening)
            {
                HttpListenerContext context;
                try
                {
                    context = httpListener.GetContext();
                }
                catch (Exception)
                {
                    break; // listener stopped
                }
                try
                {
                    Handle(context);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[{nameof(AgentCommandServer)}] {e.Message}\n");
                }
            }
        }

        /// <summary>Checks and parses one request, hands it to the main thread and writes the answer.</summary>
        /// <param name="context">The HTTP request and response.</param>
        private void Handle(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;
            if (request.HttpMethod != "POST" || request.Url.AbsolutePath != "/command")
            {
                Respond(context, 404, Error("Use POST /command."));
                return;
            }
            if (request.Headers["X-SEE-Agent"] != "1")
            {
                Respond(context, 403, Error("Missing header X-SEE-Agent: 1."));
                return;
            }

            JObject body;
            try
            {
                using StreamReader reader = new(request.InputStream, Encoding.UTF8);
                body = JObject.Parse(reader.ReadToEnd());
            }
            catch (Exception e)
            {
                Respond(context, 400, Error($"Body is not a JSON object: {e.Message}"));
                return;
            }

            PendingCommand pending = new() { Request = body };
            queue.Enqueue(pending);
            if (!pending.Done.Wait(mainThreadTimeoutMs))
            {
                if (Interlocked.CompareExchange(ref pending.State, PendingCommand.Cancelled, PendingCommand.Waiting)
                    == PendingCommand.Waiting)
                {
                    // Not started yet: it never will be, so the agent's failure report stays true.
                    Respond(context, 504, Error("SEE did not answer in time, so the command was cancelled. "
                                                + "Is Play mode paused?"));
                    return;
                }
                // The main thread started the command just now; it finishes within this frame.
                pending.Done.Wait();
            }
            Respond(context, 200, pending.Response);
        }

        /// <summary>Writes <paramref name="json"/> as the HTTP response.</summary>
        /// <param name="context">The HTTP request and response.</param>
        /// <param name="status">HTTP status code.</param>
        /// <param name="json">The answer.</param>
        private static void Respond(HttpListenerContext context, int status, JObject json)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(json.ToString(Newtonsoft.Json.Formatting.None));
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/json; charset=utf-8";
            context.Response.ContentLength64 = bytes.Length;
            context.Response.OutputStream.Write(bytes, 0, bytes.Length);
            context.Response.OutputStream.Close();
        }

        #endregion

        #region Main thread

        /// <summary>Retries listening if needed and executes the waiting commands.</summary>
        private void Update()
        {
            if (listener == null && Time.realtimeSinceStartup >= nextAttempt)
            {
                TryStartListening();
            }

            while (queue.TryDequeue(out PendingCommand pending))
            {
                if (Interlocked.CompareExchange(ref pending.State, PendingCommand.Started, PendingCommand.Waiting)
                    != PendingCommand.Waiting)
                {
                    // The request timed out and was answered with a failure; do not run it now.
                    continue;
                }
                try
                {
                    pending.Response = Execute(pending.Request);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    pending.Response = Error($"{e.GetType().Name}: {e.Message}");
                }
                finally
                {
                    pending.Done.Set();
                }
            }
        }

        /// <summary>Runs one command.</summary>
        /// <param name="request">The request body: <c>{"tool": ..., "args": {...}}</c>.</param>
        /// <returns>The JSON answer.</returns>
        private JObject Execute(JObject request)
        {
            string tool = (string)request["tool"];
            JObject args = request["args"] as JObject ?? new();
            AgentCityEffects effects = AgentCityEffects.Instance;
            if (effects == null && tool != "status")
            {
                return Error("AgentCityEffects is not running.");
            }

            JObject response = tool switch
            {
                "status" => Status(),
                "find_symbol" => FindSymbol(StringArgument(args, "query"), IntArgument(args, "limit") ?? 10),
                "neighbors" => Neighbors(StringArgument(args, "target"), IntArgument(args, "limit") ?? 30),
                "node_at" => NodeAt(args),
                "focus" => ToJson(effects.Focus(StringArgument(args, "target"))),
                "focus_camera" => ToJson(effects.FocusCamera(StringArgument(args, "target"))),
                "highlight" => ToJson(effects.Highlight(StringListArgument(args, "targets"))),
                "show_path" => ToJson(effects.ShowPath(StringListArgument(args, "targets"))),
                "show_neighbors" => ToJson(effects.ShowNeighbors(StringArgument(args, "target"))),
                "search_trail" => ToJson(effects.ShowSearchTrail(StringListArgument(args, "visited"),
                                                                 StringArgument(args, "final"))),
                "open_code" => ToJson(effects.OpenCode(StringArgument(args, "target"), IntArgument(args, "line"))),
                "reset" => ToJson(effects.ResetEffects()),
                "debug_status" => Debugger.Status(BoolArgument(args, "focus") ?? false),
                "set_breakpoint" => Debugger.SetBreakpoint(StringArgument(args, "file"), IntArgument(args, "line"), add: true),
                "remove_breakpoint" => Debugger.SetBreakpoint(StringArgument(args, "file"), IntArgument(args, "line"), add: false),
                "debug_continue" => Debugger.Continue(),
                "debug_step" => Debugger.Step(StringArgument(args, "kind")),
                "debug_stack" => Debugger.Stack(IntArgument(args, "limit") ?? 20),
                "debug_variables" => Debugger.Variables(IntArgument(args, "frame") ?? 0, IntArgument(args, "limit") ?? 50),
                _ => Error($"Unknown tool '{tool}'.")
            };

            bool callSucceeded = (bool?)response["ok"] ?? false;
            string message = (string)response["message"] ?? "";
            Debug.Log($"[{nameof(AgentCommandServer)}] {tool} {args.ToString(Newtonsoft.Json.Formatting.None)} -> "
                      + $"{(callSucceeded ? "OK" : "FAILED")}: {message}\n");
            if (ShowNotifications && tool is not ("status" or "find_symbol" or "neighbors" or "node_at"
                                                  or "debug_status" or "debug_stack" or "debug_variables"))
            {
                if (callSucceeded)
                {
                    ShowNotification.Info($"Agent: {tool}", message, log: false);
                }
                else
                {
                    ShowNotification.Warn($"Agent: {tool} failed", message, log: false);
                }
            }
            return response;
        }

        #endregion

        #region Queries

        /// <summary>
        /// What is loaded: cities with their number of drawn nodes and the folder of their source
        /// code, and whether effects can run.
        /// </summary>
        /// <returns>The JSON answer.</returns>
        private static JObject Status()
        {
            JArray cities = new();
            foreach (AbstractSEECity city in FindObjectsByType<AbstractSEECity>(FindObjectsSortMode.None))
            {
                int nodes = city.gameObject.FindAllDescendantsWithTag(Tags.Node, includeInactive: false).Count;
                cities.Add(new JObject
                {
                    ["name"] = city.name,
                    ["type"] = city.GetType().Name,
                    ["drawn_nodes"] = nodes,
                    ["source_directory"] = SourceLocator.SourceDirectoryOf(city)
                });
            }
            return new()
            {
                ["ok"] = true,
                ["message"] = $"SEE is running with {cities.Count} city/cities.",
                ["cities"] = cities,
                ["effects"] = AgentCityEffects.Instance != null,
                ["desktop_player"] = DesktopPlayerMovement.Local != null
            };
        }

        /// <summary>
        /// Finds drawn nodes whose source name, ID or file matches <paramref name="query"/>,
        /// best matches first: exact name, name prefix, file name, then any substring.
        /// </summary>
        /// <param name="query">A class, method, file or package name, or part of it.</param>
        /// <param name="limit">Maximum number of matches returned.</param>
        /// <returns>The JSON answer with a "matches" array.</returns>
        private static JObject FindSymbol(string query, int limit)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Error("No query given.");
            }
            query = query.Trim();
            List<(int score, Node node)> scored = new();
            foreach (GameObject gameNode in GameObject.FindGameObjectsWithTag(Tags.Node))
            {
                if (!gameNode.activeInHierarchy || !gameNode.TryGetNode(out Node node))
                {
                    continue;
                }
                int score = Score(node, query);
                if (score > 0)
                {
                    scored.Add((score, node));
                }
            }

            JArray matches = new();
            foreach ((int _, Node node) in scored.OrderByDescending(s => s.score)
                                                 .ThenBy(s => s.node.ID.Length)
                                                 .Take(Math.Max(1, limit)))
            {
                matches.Add(Describe(node));
            }
            return new()
            {
                ["ok"] = matches.Count > 0,
                ["message"] = matches.Count > 0
                    ? $"{scored.Count} match(es) for '{query}', showing {matches.Count}."
                    : $"No drawn node matches '{query}'.",
                ["matches"] = matches
            };

            /// <summary>
            /// How well <paramref name="node"/> matches <paramref name="query"/>, ignoring case.
            /// </summary>
            /// <param name="node">The node to rate.</param>
            /// <param name="query">The trimmed query.</param>
            /// <returns>4 for an exact name or ID, 3 for the file name, 2 for a name prefix,
            /// 1 for any substring, and 0 for no match.</returns>
            static int Score(Node node, string query)
            {
                const StringComparison ignoreCase = StringComparison.OrdinalIgnoreCase;
                string name = node.SourceName ?? "";
                string file = node.Filename ?? "";
                if (name.Equals(query, ignoreCase) || node.ID.Equals(query, ignoreCase))
                {
                    return 4;
                }
                if (Path.GetFileNameWithoutExtension(file).Equals(query, ignoreCase) || file.Equals(query, ignoreCase))
                {
                    return 3;
                }
                if (name.StartsWith(query, ignoreCase))
                {
                    return 2;
                }
                return name.Contains(query, ignoreCase) || node.ID.Contains(query, ignoreCase)
                       || file.Contains(query, ignoreCase) ? 1 : 0;
            }
        }

        /// <summary>Incoming and outgoing edges of a node, with the node at the other end.</summary>
        /// <param name="target">Node ID or source name.</param>
        /// <param name="limit">Maximum number of edges per direction.</param>
        /// <returns>The JSON answer with "incoming" and "outgoing" arrays.</returns>
        private static JObject Neighbors(string target, int limit)
        {
            AgentCityEffects.EffectResult resolved = AgentCityEffects.Instance.Resolve(target, out GameObject gameNode);
            if (!resolved.CallSucceeded)
            {
                return ToJson(resolved);
            }
            if (!gameNode.TryGetNode(out Node node))
            {
                return Error($"'{gameNode.name}' is not a graph node.");
            }
            return new()
            {
                ["ok"] = true,
                ["message"] = $"{node.Incomings.Count} incoming, {node.Outgoings.Count} outgoing edge(s).",
                ["node"] = Describe(node),
                ["incoming"] = Edges(node.Incomings, e => e.Source),
                ["outgoing"] = Edges(node.Outgoings, e => e.Target)
            };

            /// <summary>
            /// Describes at most <c>limit</c> of <paramref name="edges"/> by the node at their other end.
            /// </summary>
            /// <param name="edges">The edges to describe.</param>
            /// <param name="otherEnd">Yields the node at the other end of an edge.</param>
            /// <returns>One JSON object per edge, with the other node and the edge type.</returns>
            JArray Edges(IEnumerable<Edge> edges, Func<Edge, Node> otherEnd)
            {
                JArray result = new();
                foreach (Edge edge in edges.Take(Math.Max(1, limit)))
                {
                    JObject other = Describe(otherEnd(edge));
                    other["edge_type"] = edge.Type;
                    result.Add(other);
                }
                return result;
            }
        }

        /// <summary>
        /// The nodes declared at source locations: either the single location in the arguments
        /// "file" and "line", or each object {"file", "line"} of the argument "locations".
        /// The optional argument "city" restricts the search to the city with that name.
        /// Each location found also names its city and that city's source code directory.
        /// </summary>
        /// <param name="args">The arguments of the request.</param>
        /// <returns>The JSON answer with one entry per location in "locations"; ok if at
        /// least one location was found.</returns>
        private JObject NodeAt(JObject args)
        {
            IEnumerable<JToken> requested = args["locations"] is JArray array
                ? array
                : new JToken[] { new JObject { ["file"] = args["file"], ["line"] = args["line"] } };
            string cityName = StringArgument(args, "city");

            JArray results = new();
            int found = 0;
            foreach (JToken token in requested)
            {
                JObject location = token as JObject ?? new();
                string file = StringArgument(location, "file");
                int line = IntArgument(location, "line") ?? 0;
                JObject result = new() { ["file"] = file, ["line"] = line };
                if (sourceLocator.TryLocate(file, line, cityName, out Node node, out SourceLocator.MatchKind match,
                                            out AbstractSEECity city, out string problem))
                {
                    found++;
                    Node drawn = SourceLocator.DrawnNodeOrAncestor(node);
                    result["ok"] = true;
                    result["match"] = match == SourceLocator.MatchKind.Range ? "range" : "nearest_declaration";
                    result["node"] = Describe(node);
                    // The building to pass to focus or show_path; null if nothing of it is drawn.
                    result["focus_id"] = drawn?.ID;
                    // The city of the graph, also if the node is not drawn; probe_run takes its
                    // source code directory as the project folder.
                    result["city"] = city != null ? city.name : null;
                    result["source_directory"] = SourceLocator.SourceDirectoryOf(city);
                }
                else
                {
                    result["ok"] = false;
                    result["message"] = problem;
                }
                results.Add(result);
            }
            return new()
            {
                ["ok"] = found > 0,
                ["message"] = $"{found} of {results.Count} location(s) found.",
                ["locations"] = results
            };
        }

        /// <summary>The fields of a node the agent needs to choose and cite it.</summary>
        /// <param name="node">The node.</param>
        /// <returns>JSON with id, name, type, file, line, city and whether it is drawn.</returns>
        internal static JObject Describe(Node node)
        {
            GameObject gameNode = node.GameObject(false);
            AbstractSEECity city = gameNode != null ? gameNode.ContainingCity() : null;
            return new()
            {
                ["id"] = node.ID,
                ["name"] = node.SourceName,
                ["type"] = node.Type,
                ["file"] = node.Path(),
                ["line"] = node.SourceLine,
                ["city"] = city != null ? city.name : null,
                ["drawn"] = gameNode != null && gameNode.activeInHierarchy
            };
        }

        #endregion

        #region JSON helpers

        /// <summary>Converts an effect result to the JSON answer.</summary>
        /// <param name="result">The result.</param>
        /// <returns>JSON with ok, message and node_ids.</returns>
        private static JObject ToJson(AgentCityEffects.EffectResult result) => new()
        {
            ["ok"] = result.CallSucceeded,
            ["message"] = result.Message,
            ["node_ids"] = new JArray(result.NodeIds)
        };

        /// <summary>A failed answer.</summary>
        /// <param name="message">What went wrong.</param>
        /// <returns>JSON with ok = false.</returns>
        internal static JObject Error(string message) => new() { ["ok"] = false, ["message"] = message };

        /// <summary>A string argument; any other JSON value is converted to its text.</summary>
        /// <param name="args">The arguments of the request.</param>
        /// <param name="name">Name of the argument.</param>
        /// <returns>The argument as a string, or null if it is missing.</returns>
        private static string StringArgument(JObject args, string name) =>
            args[name]?.Type == JTokenType.String ? (string)args[name] : args[name]?.ToString();

        /// <summary>An integer argument; a fractional number is truncated.</summary>
        /// <param name="args">The arguments of the request.</param>
        /// <param name="name">Name of the argument.</param>
        /// <returns>The argument as an integer, or null if it is missing or not a number.</returns>
        private static int? IntArgument(JObject args, string name) =>
            args[name] is JValue { Type: JTokenType.Integer or JTokenType.Float } value ? (int)(double)value : null;

        /// <summary>A boolean argument.</summary>
        /// <param name="args">The arguments of the request.</param>
        /// <param name="name">Name of the argument.</param>
        /// <returns>The argument, or null if it is missing or not a boolean.</returns>
        private static bool? BoolArgument(JObject args, string name) =>
            args[name] is JValue { Type: JTokenType.Boolean } value ? (bool)value : null;

        /// <summary>A list-of-strings argument; a single string counts as a list of one.</summary>
        /// <param name="args">The arguments of the request.</param>
        /// <param name="name">Name of the argument.</param>
        /// <returns>The strings of the argument; empty if it is missing or of another type.</returns>
        private static List<string> StringListArgument(JObject args, string name) => args[name] switch
        {
            JArray array => array.Select(t => t.ToString()).ToList(),
            JValue { Type: JTokenType.String } single => new List<string> { (string)single },
            _ => new List<string>()
        };

        #endregion
    }
}
#endif
