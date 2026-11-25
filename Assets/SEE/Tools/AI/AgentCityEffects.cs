using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HighlightPlus;
using SEE.Components.GameEdges;
using SEE.Controls.KeyActions;
using SEE.Controls.Players;
using SEE.Controls.ReversibleActions;
using SEE.Extensions;
using SEE.Factories;
using SEE.Game;
using SEE.Game.City;
using SEE.Game.Operator;
using SEE.GraphElementRefs;
using SEE.GraphProviders.VCS;
using SEE.Graphs;
using SEE.Utils;
using UnityEngine;

namespace SEE.Tools.AI
{
    /// <summary>
    /// Visual effects an AI agent uses to point at buildings in a code city.
    ///
    /// All public methods take node IDs or source names, so the same calls can
    /// later be made by the MCP server / SEE API without any change here.
    ///
    /// Built only from existing SEE functionality:
    /// <see cref="NodeOperator.FadeTo"/>, <see cref="GraphElementOperator.GlowIn"/>,
    /// <see cref="NodeOperator.HitEffect"/>, <see cref="NodeOperator.MoveYTo"/>
    /// and <see cref="MarkerFactory"/>.
    ///
    /// Debug keys (Editor and development builds only, Game view must have focus): the key of
    /// <see cref="KeyAction.AskSee"/> (default J, bound in <see cref="KeyBindings"/>) together with
    ///   Ctrl   play a search trail over a few buildings, then focus the test building
    ///          (<see cref="TestTarget"/>)
    ///   Alt    show a path that follows outgoing edges from the test building
    ///   Shift  reset all effects
    /// Without Ctrl, Alt or Shift, the key opens the Ask SEE box (<see cref="AskSeeWindow"/>).
    /// </summary>
    internal class AgentCityEffects : MonoBehaviour
    {
        #region Tunable settings

        /// <summary>Alpha of all non-focused buildings while an answer is shown.</summary>
        [Tooltip("Alpha of all non-focused buildings while an answer is shown."), Range(0f, 1f)]
        public float DimAlpha = 0.2f;

        /// <summary>Glow colour of the focused building. Contrasts with SEE's pink/red cities.</summary>
        [Tooltip("Glow colour of the focused building. Contrasts with SEE's pink/red cities.")]
        public Color FocusGlowColor = Color.cyan;

        /// <summary>Strength of the colour tint on highlighted buildings (0 = none, 1 = fully tinted).</summary>
        [Tooltip("Strength of the colour tint on highlighted buildings (0 = none, 1 = fully tinted)."), Range(0f, 1f)]
        public float OverlayIntensity = 0.9f;

        /// <summary>Speed of the pulsing tint; 0 makes it steady.</summary>
        [Tooltip("Speed of the pulsing tint; 0 makes it steady.")]
        public float PulseSpeed = 2.5f;

        /// <summary>Weakest tint while pulsing, relative to <see cref="OverlayIntensity"/>.</summary>
        [Tooltip("Weakest tint while pulsing, relative to OverlayIntensity."), Range(0f, 1f)]
        public float PulseMinIntensity = 0.15f;

        /// <summary>Width of the outline around highlighted buildings (SEE's default is 0.45).</summary>
        [Tooltip("Width of the outline around highlighted buildings (SEE's default is 0.45).")]
        public float OutlineWidth = 1.5f;

        /// <summary>Colour of the marker beam above the focused building.</summary>
        [Tooltip("Colour of the marker beam above the focused building.")]
        public Color BeamColor = Color.cyan;

        /// <summary>Beam height in world units (SEE's own Show in City uses 1).</summary>
        [Tooltip("Beam height in world units (SEE's own Show in City uses 1).")]
        public float BeamHeight = 0.8f;

        /// <summary>Beam width in world units (SEE's own Show in City uses 0.01, which is hard to see).</summary>
        [Tooltip("Beam width in world units (SEE's own Show in City uses 0.01, which is hard to see).")]
        public float BeamWidth = 0.03f;

        /// <summary>How far the building pops up, relative to its own height.</summary>
        [Tooltip("How far the building pops up, relative to its own height.")]
        public float PopHeightFactor = 0.3f;

        /// <summary>Animation speed factor passed to the operators (1 = city's base duration).</summary>
        [Tooltip("Animation speed factor passed to the operators (1 = city's base duration).")]
        public float AnimationFactor = 1f;

        /// <summary>Whether <see cref="Focus"/> also flies the camera to the building.</summary>
        [Tooltip("Whether Focus also flies the camera to the building.")]
        public bool MoveCameraOnFocus = true;

        /// <summary>Duration of the camera flight in seconds.</summary>
        [Tooltip("Duration of the camera flight in seconds.")]
        public float CameraFlightSeconds = 1.2f;

        /// <summary>
        /// How much room is left around the building: 1 makes the building just fill the
        /// camera's view, 2 makes it take up about half of it.
        /// </summary>
        [Tooltip("How much room is left around the building: 1 makes the building just fill the camera's view, 2 makes it take up about half of it."), Range(1f, 6f)]
        public float CameraFrameMargin = 3f;

        /// <summary>
        /// Smallest camera distance in world units, so the camera does not end up inside
        /// the city and the neighbouring buildings stay in view.
        /// </summary>
        [Tooltip("Smallest camera distance in world units, so the camera does not end up inside the city and the neighbouring buildings stay in view.")]
        public float MinCameraDistance = 0.8f;

        /// <summary>Largest camera distance in world units, so huge buildings do not send the camera far away.</summary>
        [Tooltip("Largest camera distance in world units, so huge buildings do not send the camera far away.")]
        public float MaxCameraDistance = 2.5f;

        /// <summary>
        /// Downward viewing angle of the camera at the end of the flight, in degrees.
        /// It gets steeper when the avatar would otherwise have to sink below the floor.
        /// </summary>
        [Tooltip("Downward viewing angle of the camera at the end of the flight, in degrees. It gets steeper when the avatar would otherwise have to sink below the floor."), Range(0f, 90f)]
        public float CameraPitch = 25f;

        /// <summary>Time per step of <see cref="ShowPath"/> (one node and the edge to the next), in seconds.</summary>
        [Tooltip("Time per step of ShowPath (one node and the edge to the next), in seconds.")]
        public float PathStepSeconds = 0.6f;

        /// <summary>Longest connection <see cref="ShowPath"/> searches between two given nodes, in steps.</summary>
        [Tooltip("Longest connection ShowPath searches between two given nodes, in steps.")]
        public int MaxPathSteps = 6;

        /// <summary>Most nodes the path search visits before giving up.</summary>
        [Tooltip("Most nodes the path search visits before giving up.")]
        public int MaxPathSearchNodes = 5000;

        /// <summary>
        /// Time the camera spends at each building of a tour (flight included), in seconds.
        /// Also the step time of <see cref="ShowPath"/> while the camera follows the path.
        /// </summary>
        [Tooltip("Time the camera spends at each building of a tour (flight included), in seconds. Also the step time of ShowPath while the camera follows the path.")]
        public float TourStopSeconds = 2.2f;

        /// <summary>With more results than this, the camera shows them together instead of visiting each.</summary>
        [Tooltip("With more results than this, the camera shows them together instead of visiting each.")]
        public int MaxTourStops = 6;

        /// <summary>Downward viewing angle of the overview of several buildings, in degrees.</summary>
        [Tooltip("Downward viewing angle of the overview of several buildings, in degrees."), Range(0f, 90f)]
        public float OverviewPitch = 50f;

        /// <summary>Room left around the buildings in the overview (1 = just fits).</summary>
        [Tooltip("Room left around the buildings in the overview (1 = just fits)."), Range(1f, 4f)]
        public float OverviewFrameMargin = 1.4f;

        /// <summary>Largest camera distance of the overview, in world units.</summary>
        [Tooltip("Largest camera distance of the overview, in world units.")]
        public float MaxOverviewDistance = 4f;

        /// <summary>Glow colour of nodes that depend on the shown node (incoming edges) in <see cref="ShowNeighbors"/>.</summary>
        [Tooltip("Glow colour of nodes that depend on the shown node (incoming edges) in ShowNeighbors.")]
        public Color IncomingColor = new(1f, 0.55f, 0f);

        /// <summary>Glow colour of nodes the shown node depends on (outgoing edges) in <see cref="ShowNeighbors"/>.</summary>
        [Tooltip("Glow colour of nodes the shown node depends on (outgoing edges) in ShowNeighbors.")]
        public Color OutgoingColor = Color.green;

        /// <summary>At most this many incoming and this many outgoing neighbours are shown.</summary>
        [Tooltip("At most this many incoming and this many outgoing neighbours are shown.")]
        public int MaxNeighborsPerSide = 25;

        /// <summary>Delay between buildings in the search trail, in seconds.</summary>
        [Tooltip("Delay between buildings in the search trail, in seconds.")]
        public float SearchTrailStepSeconds = 0.15f;

        /// <summary>Source name (or node ID) used by the debug key.</summary>
        [Tooltip("Source name (or node ID) used by the debug key.")]
        public string TestTarget = "RuntimeTabMenu";

        #endregion

        /// <summary>The single instance, created automatically when the game starts.</summary>
        public static AgentCityEffects Instance { get; private set; }

        /// <summary>The building currently in focus, or null.</summary>
        private GameObject focused;

        /// <summary>All buildings dimmed by the last <see cref="Focus"/>.</summary>
        private readonly List<GameObject> dimmed = new();

        /// <summary>All buildings glowing because of <see cref="Highlight"/>.</summary>
        private readonly List<GameObject> highlighted = new();

        /// <summary>
        /// Local Y position each building had before <see cref="Pop"/> lifted it for the first time.
        /// Used to put the building back exactly, even if a pop was interrupted or
        /// <see cref="Focus"/> was called again while the building was still in the air.
        /// Local (not world) coordinates are stored so the value stays valid if the city
        /// is moved or scaled (SEE zooms by scaling the city) in between.
        /// Entries are kept for the whole session (see <see cref="LandAt"/>). Known limitation:
        /// if a building's height is changed by something else (e.g. a re-layout), a later
        /// reset puts it back to the old height.
        /// </summary>
        private readonly Dictionary<GameObject, float> restingLocalY = new();

        /// <summary>
        /// Edges drawn by <see cref="ShowPath"/> or <see cref="ShowNeighbors"/>, and whether each
        /// was visible before, so <see cref="ResetEffects"/> hides again only the ones that were hidden.
        /// </summary>
        private readonly List<(EdgeOperator edge, bool wasVisible)> revealedEdges = new();

        /// <summary>SEE's highlight settings of each pulsing building or edge, restored on reset.</summary>
        private readonly Dictionary<GameObject, SavedHighlight> savedHighlights = new();

        /// <summary>Factory holding the beam above the focused building.</summary>
        private MarkerFactory markerFactory;

        /// <summary>Glow colour SEE uses by default (see <c>Highlighter.GetHighlightEffect</c>).</summary>
        private static readonly Color defaultGlowColor = Color.yellow;

        #region Result type (shape of a future tool response)

        /// <summary>
        /// Result of an effect call. This is the shape the SEE API / MCP tool can return to Claude.
        /// </summary>
        public readonly struct EffectResult
        {
            /// <summary>Whether the call did what was asked.</summary>
            public bool CallSucceeded { get; }

            /// <summary>Human-readable explanation, meant to be passed back to Claude.</summary>
            public string Message { get; }

            /// <summary>
            /// The node IDs the call acted on. On failure, the candidates that were found
            /// (if any), so the caller can choose between them.
            /// </summary>
            public IReadOnlyList<string> NodeIds { get; }

            /// <summary>Creates a result; use <see cref="Success"/> or <see cref="Failure"/>.</summary>
            /// <param name="callSucceeded">Whether the call succeeded.</param>
            /// <param name="message">Explanation of the outcome.</param>
            /// <param name="nodeIds">Node IDs involved; null means none.</param>
            private EffectResult(bool callSucceeded, string message, IReadOnlyList<string> nodeIds)
            {
                CallSucceeded = callSucceeded;
                Message = message;
                NodeIds = nodeIds ?? new List<string>();
            }

            /// <summary>Returns a successful result.</summary>
            /// <param name="message">Explanation of what was done.</param>
            /// <param name="ids">Node IDs the call acted on.</param>
            /// <returns>A result with <see cref="CallSucceeded"/> set to true.</returns>
            public static EffectResult Success(string message, params string[] ids) => new(true, message, ids);

            /// <summary>Returns a failed result.</summary>
            /// <param name="message">Explanation of what went wrong.</param>
            /// <param name="candidates">Node IDs that were found anyway, if any.</param>
            /// <returns>A result with <see cref="CallSucceeded"/> set to false.</returns>
            public static EffectResult Failure(string message, IReadOnlyList<string> candidates = null) => new(false, message, candidates);

            /// <summary>One-line summary for the console.</summary>
            /// <returns>"OK: ..." or "FAILED: ..." followed by the node IDs.</returns>
            public override string ToString() =>
                $"{(CallSucceeded ? "OK" : "FAILED")}: {Message}" + (NodeIds.Count > 0 ? $" [{string.Join(", ", NodeIds)}]" : "");
        }

        #endregion

        #region Startup

        /// <summary>
        /// Creates the effects object automatically, so no scene has to be edited.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateInstance()
        {
            if (Instance != null)
            {
                return;
            }
            GameObject holder = new(nameof(AgentCityEffects));
            DontDestroyOnLoad(holder);
            Instance = holder.AddComponent<AgentCityEffects>();
        }

        /// <summary>
        /// Clears <see cref="Instance"/> when this object is destroyed (e.g. when Play mode stops).
        /// </summary>
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        #endregion

        #region Public API (to be called by the SEE API / MCP tools later)

        /// <summary>
        /// Points at one building: dims the rest of the city, makes the building glow
        /// in a contrasting colour, puts a beam above it and lets it pop up once.
        /// Any previous focus is reset first.
        /// </summary>
        /// <param name="idOrName">Node ID or source name (e.g. "ContextMenu").</param>
        /// <returns>The result, with the focused node ID on success.</returns>
        public EffectResult Focus(string idOrName)
        {
            EffectResult resolved = Resolve(idOrName, out GameObject gameNode);
            if (!resolved.CallSucceeded)
            {
                return resolved;
            }

            ResetEffects();
            focused = gameNode;

            DimCityExcept(gameNode);
            GlowStrong(gameNode);
            PutBeamAbove(gameNode);
            Pop(gameNode);

            string cameraNote = "";
            if (MoveCameraOnFocus)
            {
                EffectResult flown = FlyCameraTo(gameNode);
                cameraNote = flown.CallSucceeded ? " Camera moved." : $" Camera not moved: {flown.Message}";
            }

            return EffectResult.Success($"Focused '{Describe(gameNode)}'.{cameraNote}", gameNode.name);
        }

        /// <summary>
        /// Flies the camera to one building without changing any other effect
        /// (the <c>focus_camera</c> tool of the plan).
        /// </summary>
        /// <param name="idOrName">Node ID or source name.</param>
        /// <returns>The result, with the node ID on success.</returns>
        public EffectResult FocusCamera(string idOrName)
        {
            EffectResult resolved = Resolve(idOrName, out GameObject gameNode);
            if (!resolved.CallSucceeded)
            {
                return resolved;
            }
            EffectResult flown = FlyCameraTo(gameNode);
            return flown.CallSucceeded
                ? EffectResult.Success($"Camera moved to '{Describe(gameNode)}'.", gameNode.name)
                : EffectResult.Failure(flown.Message, new[] { gameNode.name });
        }

        /// <summary>
        /// Highlights several buildings at once (e.g. all callers of a method): dims the rest
        /// of the city, makes them pulse in <see cref="FocusGlowColor"/>, and flies the camera
        /// over them one by one (<see cref="CameraTour"/>) before showing all of them together.
        /// Any previous effect is reset first. Buildings that are not found are skipped.
        /// </summary>
        /// <param name="idsOrNames">Node IDs or source names.</param>
        /// <returns>Success if all were found, otherwise a failure listing the missing ones
        /// (the found ones are highlighted anyway).</returns>
        public EffectResult Highlight(IEnumerable<string> idsOrNames)
        {
            List<GameObject> gameNodes = new();
            List<string> missing = new();
            foreach (string idOrName in idsOrNames)
            {
                if (Resolve(idOrName, out GameObject gameNode).CallSucceeded)
                {
                    if (!gameNodes.Contains(gameNode))
                    {
                        gameNodes.Add(gameNode);
                    }
                }
                else
                {
                    missing.Add(idOrName);
                }
            }
            if (gameNodes.Count == 0)
            {
                return EffectResult.Failure($"Not found: {string.Join(", ", missing)}");
            }

            ResetEffects();
            DimCityExcept(gameNodes);
            foreach (GameObject gameNode in gameNodes)
            {
                GlowStrong(gameNode);
                highlighted.Add(gameNode);
            }
            string cameraNote = StartCameraTour(gameNodes, gameNodes);

            string[] found = gameNodes.Select(b => b.name).ToArray();
            return missing.Count == 0
                ? EffectResult.Success($"Highlighted {found.Length} building(s).{cameraNote}", found)
                : EffectResult.Failure($"Highlighted {found.Length}; not found: {string.Join(", ", missing)}.{cameraNote}", found);
        }

        /// <summary>
        /// Briefly flashes each building Claude looked at, one after the other,
        /// then focuses <paramref name="finalIdOrName"/> if given.
        /// </summary>
        /// <param name="visitedIdsOrNames">Node IDs or source names, in the order they were visited.</param>
        /// <param name="finalIdOrName">Building to focus at the end, or null.</param>
        /// <returns>The result, with the node IDs that will be flashed.</returns>
        public EffectResult ShowSearchTrail(IEnumerable<string> visitedIdsOrNames, string finalIdOrName = null)
        {
            List<GameObject> gameNodes = visitedIdsOrNames.Select(ResolveOrNull)
                                                          .Where(gameNode => gameNode != null)
                                                          .ToList();
            StartCoroutine(SearchTrail(gameNodes, finalIdOrName));
            return EffectResult.Success($"Search trail over {gameNodes.Count} building(s).",
                                        gameNodes.Select(b => b.name).ToArray());
        }

        /// <summary>
        /// Shows a path through the city (the <c>show_path</c> tool of the plan), e.g. a call
        /// chain: dims the rest of the city, then step by step makes each node glow and draws
        /// the edge from it to the next node, while the camera follows.
        /// Any previous effect is reset first.
        ///
        /// The caller may give just the start and the end. Wherever two consecutive nodes have
        /// no drawn edge between them, the shortest connection in the dependency graph is
        /// searched (<see cref="ShortestDrawnRoute"/>) and its nodes are inserted, so the path
        /// shows only dependencies that really exist. If none is found within
        /// <see cref="MaxPathSteps"/> steps, the path jumps there and the message says so.
        /// The message lists the complete path, so the agent can report exactly that.
        /// </summary>
        /// <param name="idsOrNames">Node IDs or source names, in path order; at least two.</param>
        /// <returns>Success with the node IDs of the complete path, or a failure naming the nodes not found.</returns>
        public EffectResult ShowPath(IEnumerable<string> idsOrNames)
        {
            List<GameObject> nodes = new();
            List<string> missing = new();
            foreach (string idOrName in idsOrNames)
            {
                if (Resolve(idOrName, out GameObject gameNode).CallSucceeded)
                {
                    nodes.Add(gameNode);
                }
                else
                {
                    missing.Add(idOrName);
                }
            }
            if (missing.Count > 0)
            {
                // A path with silently dropped nodes would show something Claude did not mean.
                return EffectResult.Failure($"Not found: {string.Join(", ", missing)}", nodes.Select(n => n.name).ToList());
            }
            if (nodes.Count < 2)
            {
                return EffectResult.Failure("A path needs at least two nodes.", nodes.Select(n => n.name).ToList());
            }

            // The complete path: the given nodes plus whatever the search inserts between them.
            // edges[i] connects path[i] and path[i + 1]; null marks a gap.
            List<GameObject> path = new() { nodes[0] };
            List<EdgeOperator> edges = new();
            List<string> searched = new();
            List<string> gaps = new();
            bool againstDirection = false;
            for (int i = 0; i + 1 < nodes.Count; i++)
            {
                GameObject from = nodes[i];
                GameObject to = nodes[i + 1];
                EdgeOperator direct = DrawnEdgeBetween(from, to);
                if (direct != null)
                {
                    edges.Add(direct);
                    path.Add(to);
                    continue;
                }
                List<(GameObject node, EdgeOperator edge)> route = ShortestDrawnRoute(from, to, out bool reversed);
                if (route != null)
                {
                    foreach ((GameObject node, EdgeOperator edge) in route)
                    {
                        edges.Add(edge);
                        path.Add(node);
                    }
                    searched.Add($"{Describe(from)} -> {Describe(to)}");
                    againstDirection |= reversed;
                }
                else
                {
                    edges.Add(null);
                    path.Add(to);
                    gaps.Add($"{Describe(from)} -> {Describe(to)}");
                }
            }

            ResetEffects();
            DimCityExcept(path);
            StartCoroutine(PathAnimation(path, edges));

            string message = $"Path: {string.Join(" -> ", path.Select(Describe))}.";
            if (searched.Count > 0)
            {
                message += $" The steps between {string.Join("; ", searched)} were found in the dependency data"
                           + (againstDirection ? " (some steps go against the dependency direction)." : ".");
            }
            if (gaps.Count > 0)
            {
                message += $" No connection within {MaxPathSteps} steps for: {string.Join("; ", gaps)}; "
                           + "the path jumps there. Do not claim a dependency for these.";
            }
            if (MoveCameraOnFocus && DesktopPlayerMovement.Local == null)
            {
                message += " Camera not moved: no desktop player.";
            }
            return EffectResult.Success(message, path.Select(n => n.name).ToArray());
        }

        /// <summary>
        /// The shortest connection from <paramref name="from"/> to <paramref name="to"/> over
        /// edges that are drawn in the city (breadth-first search). Edges are first followed
        /// only in their direction (e.g. caller to callee); if that finds nothing, in both.
        /// At most <see cref="MaxPathSteps"/> steps and <see cref="MaxPathSearchNodes"/>
        /// visited nodes, so a huge graph cannot stall the frame.
        /// </summary>
        /// <param name="from">Start building.</param>
        /// <param name="to">End building.</param>
        /// <param name="reversed">Whether the route uses edges against their direction.</param>
        /// <returns>The steps after <paramref name="from"/>, each with the edge leading to it,
        /// ending with <paramref name="to"/>; or null if there is no connection.</returns>
        private List<(GameObject node, EdgeOperator edge)> ShortestDrawnRoute(GameObject from, GameObject to, out bool reversed)
        {
            reversed = false;
            if (!from.TryGetNode(out Node start) || !to.TryGetNode(out Node goal))
            {
                return null;
            }
            foreach (bool directed in new[] { true, false })
            {
                Dictionary<Node, (Node previous, EdgeOperator edge)> reached = new() { [start] = (null, null) };
                Queue<(Node node, int depth)> queue = new();
                queue.Enqueue((start, 0));
                while (queue.Count > 0 && reached.Count < MaxPathSearchNodes)
                {
                    (Node node, int depth) = queue.Dequeue();
                    if (depth >= MaxPathSteps)
                    {
                        continue;
                    }
                    IEnumerable<(Edge edge, Node other)> next = node.Outgoings.Select(e => (e, e.Target));
                    if (!directed)
                    {
                        next = next.Concat(node.Incomings.Select(e => (e, e.Source)));
                    }
                    foreach ((Edge edge, Node other) in next)
                    {
                        if (other == null || reached.ContainsKey(other))
                        {
                            continue;
                        }
                        GameObject otherObject = other.GameObject(false);
                        EdgeOperator edgeOperator = edge.Operator(mustFind: false);
                        if (otherObject == null || !otherObject.activeInHierarchy || edgeOperator == null)
                        {
                            continue;
                        }
                        reached[other] = (node, edgeOperator);
                        if (other == goal)
                        {
                            reversed = !directed;
                            List<(GameObject, EdgeOperator)> route = new();
                            for (Node step = goal; step != start; step = reached[step].previous)
                            {
                                route.Add((step.GameObject(false), reached[step].edge));
                            }
                            route.Reverse();
                            return route;
                        }
                        queue.Enqueue((other, depth + 1));
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Shows what a node is connected to (the <c>neighbors</c> tool, made visible):
        /// dims the rest of the city, makes the node glow in <see cref="FocusGlowColor"/>,
        /// nodes with an edge to it (e.g. callers) in <see cref="IncomingColor"/> and nodes it
        /// has an edge to (e.g. callees) in <see cref="OutgoingColor"/>, draws those edges
        /// and flies the camera to the node. Any previous effect is reset first.
        /// Only neighbours that are drawn in the city count; at most
        /// <see cref="MaxNeighborsPerSide"/> per direction.
        /// </summary>
        /// <param name="idOrName">Node ID or source name.</param>
        /// <returns>The result, with the node ID first and then the neighbours' IDs.</returns>
        public EffectResult ShowNeighbors(string idOrName)
        {
            EffectResult resolved = Resolve(idOrName, out GameObject gameNode);
            if (!resolved.CallSucceeded)
            {
                return resolved;
            }
            if (!gameNode.TryGetNode(out Node node))
            {
                return EffectResult.Failure($"'{gameNode.name}' is not a graph node.");
            }

            List<(GameObject other, EdgeOperator edge)> incoming = DrawnNeighbors(node.Incomings, e => e.Source);
            List<(GameObject other, EdgeOperator edge)> outgoing = DrawnNeighbors(node.Outgoings, e => e.Target);

            ResetEffects();
            List<GameObject> keep = new() { gameNode };
            keep.AddRange(incoming.Select(n => n.other));
            keep.AddRange(outgoing.Select(n => n.other));
            DimCityExcept(keep);

            GlowStrong(gameNode);
            highlighted.Add(gameNode);
            gameNode.NodeOperator().HitEffect();
            foreach ((GameObject other, EdgeOperator edge) in incoming)
            {
                GlowStrong(other, IncomingColor);
                highlighted.Add(other);
                RevealEdge(edge, IncomingColor);
            }
            foreach ((GameObject other, EdgeOperator edge) in outgoing)
            {
                // A node that is both caller and callee keeps the outgoing colour.
                GlowStrong(other, OutgoingColor);
                highlighted.Add(other);
                RevealEdge(edge, OutgoingColor);
            }

            // Visit the node itself, then pull back to show it with all its neighbours.
            string cameraNote = StartCameraTour(new List<GameObject> { gameNode }, keep);

            List<string> ids = new() { gameNode.name };
            ids.AddRange(incoming.Select(n => n.other.name));
            ids.AddRange(outgoing.Select(n => n.other.name));
            return EffectResult.Success($"'{Describe(gameNode)}': {incoming.Count} incoming (orange), "
                                        + $"{outgoing.Count} outgoing (green).{cameraNote}",
                                        ids.Distinct().ToArray());

            // Neighbours over the given edges that have a game edge and a drawn building.
            List<(GameObject, EdgeOperator)> DrawnNeighbors(IEnumerable<Edge> edges, System.Func<Edge, Node> otherEnd)
            {
                List<(GameObject, EdgeOperator)> result = new();
                foreach (Edge edge in edges)
                {
                    if (result.Count >= MaxNeighborsPerSide)
                    {
                        break;
                    }
                    Node other = otherEnd(edge);
                    GameObject otherObject = other?.GameObject(false);
                    EdgeOperator edgeOperator = edge.Operator(mustFind: false);
                    if (other != node && otherObject != null && otherObject.activeInHierarchy && edgeOperator != null)
                    {
                        result.Add((otherObject, edgeOperator));
                    }
                }
                return result;
            }
        }

        /// <summary>
        /// Opens SEE's code window for a node (the <c>open_code</c> tool of the plan), scrolled
        /// to <paramref name="line"/> or, if none is given, to the node's own source line.
        /// Does not change any other effect.
        /// </summary>
        /// <param name="idOrName">Node ID or source name.</param>
        /// <param name="line">Line to scroll to (1-based), or null for the node's source line.</param>
        /// <returns>The result, or a failure if the source file is unknown or not found.</returns>
        public EffectResult OpenCode(string idOrName, int? line = null)
        {
            EffectResult resolved = Resolve(idOrName, out GameObject gameNode);
            if (!resolved.CallSucceeded)
            {
                return resolved;
            }
            if (!gameNode.TryGetComponent(out GraphElementRef elementRef) || elementRef.Elem == null)
            {
                return EffectResult.Failure($"'{gameNode.name}' has no graph element.");
            }

            // ShowCodeAction reports these problems only as notifications from inside an
            // async method, so they are checked here to give the agent a usable answer.
            GraphElement element = elementRef.Elem;
            if (string.IsNullOrEmpty(element.Filename))
            {
                return EffectResult.Failure($"'{Describe(gameNode)}' has no source file in the graph "
                                            + "(e.g. a GXL graph without source locations).", new[] { gameNode.name });
            }
            if (!element.TryGetCommitID(out string _))
            {
                string path = element.AbsolutePlatformPath();
                if (!System.IO.File.Exists(path))
                {
                    return EffectResult.Failure($"Source file of '{Describe(gameNode)}' not found at '{path}'. "
                                                + "The city's source code folder may not point to the code.",
                                                new[] { gameNode.name });
                }
            }

            int? scrollTo = line is > 0 ? line : null;
            UI.Window.CodeWindow.CodeWindow window = ShowCodeAction.ShowCode(elementRef, codeWindow =>
            {
                if (scrollTo.HasValue)
                {
                    codeWindow.ScrolledVisibleLine = scrollTo.Value;
                }
            });
            GameWindowManager.ActivateWindow(window);

            int? shownLine = scrollTo ?? element.SourceLine;
            return EffectResult.Success($"Opened {element.Filename}" + (shownLine.HasValue ? $" at line {shownLine}." : "."),
                                        gameNode.name);
        }

        /// <summary>
        /// Undoes all effects: restores transparency and height, removes glow and beam,
        /// and hides drawn edges that were hidden before.
        /// </summary>
        /// <returns>Always a successful result.</returns>
        public EffectResult ResetEffects()
        {
            StopAllCoroutines();

            foreach (GameObject gameNode in dimmed.Where(b => b != null))
            {
                gameNode.NodeOperator().FadeTo(1f, AnimationFactor);
            }
            dimmed.Clear();

            foreach (GameObject gameNode in highlighted.Where(b => b != null))
            {
                RemoveStrongGlow(gameNode);
            }
            highlighted.Clear();

            foreach ((EdgeOperator edge, bool wasVisible) in revealedEdges.Where(e => e.edge != null))
            {
                RemoveStrongGlow(edge.gameObject);
                edge.AnimateDataFlow(false);
                if (!wasVisible)
                {
                    edge.Destruct(AnimationFactor);
                }
            }
            revealedEdges.Clear();

            if (focused != null)
            {
                RemoveStrongGlow(focused);
                focused = null;
            }

            markerFactory?.Clear();
            markerFactory = null;

            // Put popped buildings back where they were, even if a pop was interrupted.
            // The remembered heights are kept (see LandAt); only destroyed buildings are dropped.
            foreach (GameObject gameNode in restingLocalY.Keys.ToList())
            {
                if (gameNode == null)
                {
                    restingLocalY.Remove(gameNode);
                }
                else
                {
                    LandAt(gameNode);
                }
            }

            return EffectResult.Success("Effects reset.");
        }

        #endregion

        #region Effects

        /// <summary>Fades all other buildings of the same city to <see cref="DimAlpha"/>.</summary>
        /// <param name="gameNode">The building that stays fully visible.</param>
        private void DimCityExcept(GameObject gameNode) => DimCityExcept(new List<GameObject> { gameNode });

        /// <summary>
        /// Fades all buildings of the cities containing <paramref name="keep"/> to
        /// <see cref="DimAlpha"/>, except the buildings in <paramref name="keep"/>.
        /// </summary>
        /// <param name="keep">The buildings that stay fully visible.</param>
        private void DimCityExcept(ICollection<GameObject> keep)
        {
            HashSet<GameObject> kept = new(keep);
            HashSet<AbstractSEECity> cities = new(keep.Select(gameNode => gameNode.ContainingCity())
                                                      .Where(city => city != null));
            foreach (AbstractSEECity city in cities)
            {
                IEnumerable<GameObject> others = city.gameObject
                                                     .FindAllDescendantsWithTag(Tags.Node, includeInactive: false)
                                                     .Where(gameNode => !kept.Contains(gameNode));
                foreach (GameObject other in others)
                {
                    other.NodeOperator().FadeTo(DimAlpha, AnimationFactor);
                    dimmed.Add(other);
                }
            }
        }

        /// <summary>Glow in <see cref="FocusGlowColor"/> (or <paramref name="color"/>) instead of SEE's default yellow.</summary>
        /// <param name="gameNode">The building to make glow.</param>
        /// <param name="color">Glow colour; null means <see cref="FocusGlowColor"/>.</param>
        private void GlowStrong(GameObject gameNode, Color? color = null)
        {
            // NodeOperator, not Operator(): FadeTo and HitEffect only exist on the generic
            // GraphElementOperator<Color> that NodeOperator derives from.
            NodeOperator op = gameNode.NodeOperator();
            op.FadeTo(1f, AnimationFactor); // make sure it is fully visible
            op.GlowIn(AnimationFactor);
            // The operator creates the HighlightEffect in its OnEnable, so it exists by now.
            if (gameNode.TryGetComponent(out HighlightEffect effect))
            {
                effect.SetGlowColor(color ?? FocusGlowColor);
                ApplyPulse(gameNode, effect, color ?? FocusGlowColor);
                effect.UpdateMaterialProperties();
            }
        }

        /// <summary>Glow in <see cref="FocusGlowColor"/> (or <paramref name="color"/>) on an edge.</summary>
        /// <param name="edge">The edge to make glow.</param>
        /// <param name="color">Glow colour; null means <see cref="FocusGlowColor"/>.</param>
        private void GlowStrong(EdgeOperator edge, Color? color = null)
        {
            edge.GlowIn(AnimationFactor);
            if (edge.TryGetComponent(out HighlightEffect effect))
            {
                effect.SetGlowColor(color ?? FocusGlowColor);
                ApplyPulse(edge.gameObject, effect, color ?? FocusGlowColor);
                effect.UpdateMaterialProperties();
            }
        }

        /// <summary>
        /// Draws <paramref name="edge"/> from scratch so it visibly grows (even if it was shown
        /// already), makes it glow and shows its data flow. Remembers whether it was visible
        /// before, for <see cref="ResetEffects"/>. SEE builds edges from source to target.
        /// </summary>
        /// <param name="edge">The edge to reveal.</param>
        /// <param name="color">Glow colour; null means <see cref="FocusGlowColor"/>.</param>
        private void RevealEdge(EdgeOperator edge, Color? color = null)
        {
            revealedEdges.Add((edge, IsVisible(edge)));
            edge.Destruct(0);
            edge.Construct(AnimationFactor);
            GlowStrong(edge, color);
            edge.AnimateDataFlow(true);
        }

        /// <summary>
        /// The drawn edge leading from <paramref name="from"/> to <paramref name="to"/>, or null
        /// if there is none. Only edges in the direction of the path count: an edge from
        /// <paramref name="to"/> back to <paramref name="from"/> is left to
        /// <see cref="ShortestDrawnRoute"/>, which first looks for a route in the dependency
        /// direction and otherwise reports that the path goes against it.
        /// </summary>
        /// <param name="from">Game node the path comes from.</param>
        /// <param name="to">Game node the path goes to.</param>
        /// <returns>The operator of the game edge, or null.</returns>
        private static EdgeOperator DrawnEdgeBetween(GameObject from, GameObject to)
        {
            if (!from.TryGetNode(out Node source) || !to.TryGetNode(out Node target))
            {
                return null;
            }
            // Edges can exist in the graph without a game edge; Operator yields null then.
            return source.Outgoings.Where(edge => edge.Target == target)
                                   .Select(edge => edge.Operator(mustFind: false))
                                   .FirstOrDefault(edgeOperator => edgeOperator != null);
        }

        /// <summary>
        /// Whether a game edge can currently be seen: not faded out and not destructed
        /// (the two ways SEE hides edges, see <see cref="EdgeAnimationKind"/>).
        /// </summary>
        /// <param name="edge">The edge to check.</param>
        /// <returns>True if the edge is visible.</returns>
        private static bool IsVisible(EdgeOperator edge)
        {
            return edge.TargetColor.start.a > 0.01f
                && (!edge.TryGetComponent(out SEESpline spline) || (spline.IsSelectable && spline.VisibleSegmentEnd > 0.99f));
        }

        /// <summary>
        /// Walks the path: each node pulses and gets a hit effect, then the edge to the next
        /// node is drawn from scratch, glows and shows its data flow. The camera follows the
        /// path node by node and ends with an overview of the whole path; if the user moves
        /// the camera meanwhile, it stops following.
        /// </summary>
        /// <param name="nodes">Buildings of the path, in order.</param>
        /// <param name="edges">Edge between nodes[i] and nodes[i + 1], or null for a gap.</param>
        /// <returns>Coroutine enumerator.</returns>
        private IEnumerator PathAnimation(List<GameObject> nodes, List<EdgeOperator> edges)
        {
            bool followCamera = CanFlyCamera;
            float startedAt = Time.realtimeSinceStartup;
            for (int i = 0; i < nodes.Count; i++)
            {
                GameObject node = nodes[i];
                if (node == null)
                {
                    continue;
                }
                GlowStrong(node);
                highlighted.Add(node);
                node.NodeOperator().HitEffect();

                if (i < edges.Count && edges[i] != null)
                {
                    // An edge followed backwards grows against the path direction.
                    RevealEdge(edges[i]);
                }

                followCamera = followCamera && !UserMovedCameraSince(startedAt);
                if (followCamera)
                {
                    FlyCameraTo(node);
                }
                yield return new WaitForSeconds(followCamera ? Mathf.Max(PathStepSeconds, TourStopSeconds) : PathStepSeconds);
            }

            if (followCamera && !UserMovedCameraSince(startedAt))
            {
                FlyCameraOver(nodes.Where(n => n != null).ToList());
            }
        }

        /// <summary>
        /// Flies the camera to each of <paramref name="stops"/> in turn (at most
        /// <see cref="MaxTourStops"/>), giving each a hit effect when the camera arrives, then
        /// pulls back to show all of <paramref name="overview"/> together. Stops as soon as the
        /// user moves or turns the camera.
        /// </summary>
        /// <param name="stops">Buildings to visit, in order.</param>
        /// <param name="overview">Buildings to show together at the end.</param>
        /// <returns>Coroutine enumerator.</returns>
        private IEnumerator CameraTour(List<GameObject> stops, List<GameObject> overview)
        {
            float startedAt = Time.realtimeSinceStartup;
            // With many results, visiting each one would take too long; show them together.
            if (stops.Count <= MaxTourStops)
            {
                foreach (GameObject stop in stops.Where(s => s != null))
                {
                    if (UserMovedCameraSince(startedAt))
                    {
                        yield break;
                    }
                    FlyCameraTo(stop);
                    yield return new WaitForSeconds(CameraFlightSeconds);
                    if (stop != null)
                    {
                        stop.NodeOperator().HitEffect();
                    }
                    yield return new WaitForSeconds(Mathf.Max(0f, TourStopSeconds - CameraFlightSeconds));
                }
            }
            List<GameObject> all = overview.Where(o => o != null).ToList();
            // A single building was just visited; there is nothing more to show.
            if (all.Count > 1 && !UserMovedCameraSince(startedAt))
            {
                FlyCameraOver(all);
            }
        }

        /// <summary>
        /// Starts <see cref="CameraTour"/> if camera flights are possible and wanted.
        /// </summary>
        /// <param name="stops">Buildings to visit, in order.</param>
        /// <param name="overview">Buildings to show together at the end.</param>
        /// <returns>Text to append to a result message: empty, or why the camera did not move.</returns>
        private string StartCameraTour(List<GameObject> stops, List<GameObject> overview)
        {
            if (!MoveCameraOnFocus)
            {
                return "";
            }
            if (!CanFlyCamera)
            {
                return " Camera not moved: no desktop player.";
            }
            StartCoroutine(CameraTour(stops, overview));
            return stops.Count > MaxTourStops || overview.Count > 1 ? " Camera is touring the results." : "";
        }

        /// <summary>Whether the camera can fly: wanted and a desktop player exists.</summary>
        private bool CanFlyCamera => MoveCameraOnFocus && DesktopPlayerMovement.Local != null;

        /// <summary>Whether the user moved or turned the camera since <paramref name="since"/> (realtime seconds).</summary>
        /// <param name="since">Point in time to compare with.</param>
        /// <returns>True if the user took over the camera.</returns>
        private static bool UserMovedCameraSince(float since) =>
            DesktopPlayerMovement.Local != null && DesktopPlayerMovement.Local.LastUserInputTime > since;

        /// <summary>
        /// Gives <paramref name="target"/> a strong, pulsing highlight in <paramref name="color"/>:
        /// a colour tint over the whole object that pulses (<see cref="PulseSpeed"/>) and a thick
        /// outline. The previous settings are saved once, for <see cref="RemoveStrongGlow"/>.
        /// </summary>
        /// <param name="target">Building or edge.</param>
        /// <param name="effect">Its highlight effect.</param>
        /// <param name="color">Highlight colour.</param>
        private void ApplyPulse(GameObject target, HighlightEffect effect, Color color)
        {
            if (!savedHighlights.ContainsKey(target))
            {
                savedHighlights[target] = new SavedHighlight(effect);
            }
            effect.highlighted = true;
            effect.overlay = OverlayIntensity;
            effect.overlayColor = color;
            effect.overlayAnimationSpeed = PulseSpeed;
            effect.overlayMinIntensity = PulseMinIntensity;
            effect.overlayBlending = 1f;
            effect.outline = 1f;
            effect.outlineColor = color;
            effect.outlineWidth = OutlineWidth;
        }

        /// <summary>Turns glow and pulse off and restores SEE's highlight settings.</summary>
        /// <param name="gameNode">The building or edge whose highlight is removed.</param>
        private void RemoveStrongGlow(GameObject gameNode)
        {
            gameNode.Operator().GlowOut(AnimationFactor);
            if (gameNode.TryGetComponent(out HighlightEffect effect))
            {
                if (savedHighlights.TryGetValue(gameNode, out SavedHighlight saved))
                {
                    saved.Restore(effect);
                    savedHighlights.Remove(gameNode);
                }
                effect.SetGlowColor(defaultGlowColor);
                effect.UpdateMaterialProperties();
            }
        }

        /// <summary>The highlight settings <see cref="ApplyPulse"/> changes, to restore them later.</summary>
        private readonly struct SavedHighlight
        {
            private readonly float overlay, overlayAnimationSpeed, overlayMinIntensity, overlayBlending, outline, outlineWidth;
            private readonly Color overlayColor, outlineColor;

            /// <summary>Saves the settings of <paramref name="effect"/>.</summary>
            /// <param name="effect">The highlight effect.</param>
            internal SavedHighlight(HighlightEffect effect)
            {
                overlay = effect.overlay;
                overlayColor = effect.overlayColor;
                overlayAnimationSpeed = effect.overlayAnimationSpeed;
                overlayMinIntensity = effect.overlayMinIntensity;
                overlayBlending = effect.overlayBlending;
                outline = effect.outline;
                outlineColor = effect.outlineColor;
                outlineWidth = effect.outlineWidth;
            }

            /// <summary>Writes the saved settings back to <paramref name="effect"/>.</summary>
            /// <param name="effect">The highlight effect.</param>
            internal void Restore(HighlightEffect effect)
            {
                effect.overlay = overlay;
                effect.overlayColor = overlayColor;
                effect.overlayAnimationSpeed = overlayAnimationSpeed;
                effect.overlayMinIntensity = overlayMinIntensity;
                effect.overlayBlending = overlayBlending;
                effect.outline = outline;
                effect.outlineColor = outlineColor;
                effect.outlineWidth = outlineWidth;
            }
        }

        /// <summary>A thicker, contrasting version of the beam SEE's Show in City uses.</summary>
        /// <param name="gameNode">The building to put the beam on.</param>
        private void PutBeamAbove(GameObject gameNode)
        {
            markerFactory = new MarkerFactory(new MarkerAttributes(BeamHeight, BeamWidth, BeamColor, BeamColor, BeamColor));
            markerFactory.MarkBorn(gameNode);
        }

        /// <summary>
        /// Flies the local desktop player's camera so it looks down at the centre of
        /// <paramref name="gameNode"/>, from just far enough away that the whole building
        /// fits into the camera's view (plus <see cref="CameraFrameMargin"/>).
        /// </summary>
        /// <param name="gameNode">The building to look at.</param>
        /// <returns>Success, or a failure if there is no desktop player (e.g. in VR).</returns>
        private EffectResult FlyCameraTo(GameObject gameNode)
        {
            DesktopPlayerMovement player = DesktopPlayerMovement.Local;
            if (player == null)
            {
                return EffectResult.Failure("No desktop player (camera flights are not supported in VR yet).");
            }

            Bounds bounds = BoundsOf(gameNode);
            float distance = Mathf.Clamp(FittingDistance(bounds) * CameraFrameMargin,
                                         MinCameraDistance, Mathf.Max(MinCameraDistance, MaxCameraDistance));
            player.FlyToLookAt(bounds.center, distance, CameraPitch, CameraFlightSeconds);
            return EffectResult.Success("Camera flight started.", gameNode.name);
        }

        /// <summary>
        /// Flies the camera up and back so that all <paramref name="gameNodes"/> are in view at once,
        /// looking down more steeply (<see cref="OverviewPitch"/>) than for a single building.
        /// </summary>
        /// <param name="gameNodes">The buildings to show; must not be empty.</param>
        private void FlyCameraOver(List<GameObject> gameNodes)
        {
            DesktopPlayerMovement player = DesktopPlayerMovement.Local;
            if (player == null || gameNodes.Count == 0)
            {
                return;
            }
            Bounds all = BoundsOf(gameNodes[0]);
            foreach (GameObject gameNode in gameNodes.Skip(1))
            {
                all.Encapsulate(BoundsOf(gameNode));
            }
            float distance = Mathf.Clamp(FittingDistance(all) * OverviewFrameMargin,
                                         MinCameraDistance, Mathf.Max(MinCameraDistance, MaxOverviewDistance));
            player.FlyToLookAt(all.center, distance, OverviewPitch, CameraFlightSeconds);
        }

        /// <summary>World-space bounds of a building (its collider, or its transform as a fallback).</summary>
        /// <param name="gameNode">The building.</param>
        /// <returns>Its bounds.</returns>
        private static Bounds BoundsOf(GameObject gameNode) =>
            gameNode.TryGetComponent(out Collider collider)
                ? collider.bounds
                : new Bounds(gameNode.transform.position, gameNode.transform.lossyScale);

        /// <summary>
        /// Distance at which a sphere around <paramref name="bounds"/> just fits into the narrower
        /// of the camera's two fields of view. A tall building does not push the camera up by its
        /// height times a factor; it only needs enough distance to be seen whole.
        /// </summary>
        /// <param name="bounds">What has to be in view.</param>
        /// <returns>Distance in world units, without any margin.</returns>
        private static float FittingDistance(Bounds bounds)
        {
            Camera camera = Camera.main;
            float verticalFov = camera != null ? camera.fieldOfView : 60f;
            float aspect = camera != null ? camera.aspect : 16f / 9f;
            float horizontalFov = Camera.VerticalToHorizontalFieldOfView(verticalFov, aspect);
            float halfFov = 0.5f * Mathf.Min(verticalFov, horizontalFov) * Mathf.Deg2Rad;
            return bounds.extents.magnitude / Mathf.Sin(halfFov);
        }

        /// <summary>Lifts the building a little and drops it back, so the eye catches the moment.</summary>
        /// <param name="gameNode">The building to pop.</param>
        private void Pop(GameObject gameNode)
        {
            // Remember the resting height only the first time. If the building is still in the
            // air from an earlier pop, its current position is NOT its resting position.
            if (!restingLocalY.ContainsKey(gameNode))
            {
                restingLocalY[gameNode] = gameNode.transform.localPosition.y;
            }

            float restingY = RestingWorldY(gameNode);
            float lift = Mathf.Max(0.01f, gameNode.transform.lossyScale.y * PopHeightFactor);
            // If ResetEffects or another Focus moves the building before the lift ends,
            // this tween is killed and OnComplete does not run; whoever moved it lands it.
            gameNode.NodeOperator().MoveYTo(restingY + lift, AnimationFactor * 0.5f)
                    .OnComplete(() => LandAt(gameNode));
        }

        /// <summary>
        /// Moves <paramref name="gameNode"/> back to its remembered resting height.
        /// The height is deliberately never forgotten while the building exists: SEE's operator
        /// reports "done" immediately when asked to move to a target it is already heading for,
        /// so "forget once landed" cannot be detected reliably, and forgetting too early makes
        /// the next <see cref="Pop"/> record a mid-air height as the resting height.
        /// </summary>
        /// <param name="gameNode">A building that may be in <see cref="restingLocalY"/>.</param>
        private void LandAt(GameObject gameNode)
        {
            if (gameNode != null && restingLocalY.ContainsKey(gameNode))
            {
                gameNode.NodeOperator().MoveYTo(RestingWorldY(gameNode), AnimationFactor * 0.5f);
            }
        }

        /// <summary>
        /// The world Y coordinate of <paramref name="gameNode"/> at its remembered resting height
        /// (<see cref="NodeOperator.MoveYTo"/> expects world coordinates).
        /// </summary>
        /// <param name="gameNode">A building that is in <see cref="restingLocalY"/>.</param>
        /// <returns>World Y coordinate of the resting position.</returns>
        private float RestingWorldY(GameObject gameNode)
        {
            Vector3 local = gameNode.transform.localPosition;
            local.y = restingLocalY[gameNode];
            Transform parent = gameNode.transform.parent;
            return parent != null ? parent.TransformPoint(local).y : local.y;
        }

        /// <summary>
        /// Flashes each building in turn, then focuses <paramref name="finalIdOrName"/>.
        /// </summary>
        /// <param name="gameNodes">Buildings to flash, in order.</param>
        /// <param name="finalIdOrName">Building to focus at the end, or null.</param>
        /// <returns>Coroutine enumerator.</returns>
        private IEnumerator SearchTrail(List<GameObject> gameNodes, string finalIdOrName)
        {
            foreach (GameObject gameNode in gameNodes)
            {
                if (gameNode != null)
                {
                    gameNode.NodeOperator().HitEffect();
                }
                yield return new WaitForSeconds(SearchTrailStepSeconds);
            }
            if (!string.IsNullOrEmpty(finalIdOrName))
            {
                Debug.Log($"[{nameof(AgentCityEffects)}] {Focus(finalIdOrName)}\n");
            }
        }

        #endregion

        #region Resolving node IDs and names

        /// <summary>
        /// Finds the building for <paramref name="idOrName"/>: first as exact node ID,
        /// then as source name (e.g. a class name) among all drawn buildings.
        /// If several buildings share the name, the first one is used and the others
        /// are reported in the message (a later tool can ask Claude to choose).
        /// </summary>
        /// <param name="idOrName">Node ID or source name.</param>
        /// <param name="gameNode">The building found, or null.</param>
        /// <returns>Success with the matching node ID(s), or a failure.</returns>
        public EffectResult Resolve(string idOrName, out GameObject gameNode)
        {
            gameNode = null;
            if (string.IsNullOrWhiteSpace(idOrName))
            {
                return EffectResult.Failure("No node ID or name given.");
            }

            if (GraphElementIDMap.TryGetValue(idOrName, out GameObject byId)
                && byId != null && byId.activeInHierarchy && byId.IsNode())
            {
                gameNode = byId;
                return EffectResult.Success("Found by ID.", byId.name);
            }

            List<GameObject> matches = new();
            foreach (GameObject candidate in GameObject.FindGameObjectsWithTag(Tags.Node))
            {
                if (candidate.TryGetNode(out Node node)
                    && string.Equals(node.SourceName, idOrName, System.StringComparison.OrdinalIgnoreCase))
                {
                    matches.Add(candidate);
                }
            }

            if (matches.Count == 0)
            {
                return EffectResult.Failure($"No building named or with ID '{idOrName}' in the drawn cities.");
            }

            gameNode = matches[0];
            return matches.Count == 1
                ? EffectResult.Success("Found by name.", gameNode.name)
                : EffectResult.Success($"{matches.Count} buildings named '{idOrName}', using the first.",
                                       matches.Select(m => m.name).ToArray());
        }

        /// <summary>
        /// The game node for <paramref name="idOrName"/> (see <see cref="Resolve"/>), or null
        /// if there is none.
        /// </summary>
        /// <param name="idOrName">Node ID or source name.</param>
        /// <returns>The game node, or null.</returns>
        private GameObject ResolveOrNull(string idOrName) =>
            Resolve(idOrName, out GameObject gameNode).CallSucceeded ? gameNode : null;

        /// <summary>
        /// Readable name for messages: the source name (e.g. "Graph"), or the node ID if there
        /// is none. Node IDs can be very long (e.g. with the path of the analysed assembly);
        /// they are returned separately in <see cref="EffectResult.NodeIds"/>.
        /// </summary>
        /// <param name="gameNode">The building to describe.</param>
        /// <returns>Source name, or the node ID.</returns>
        private static string Describe(GameObject gameNode) =>
            gameNode.TryGetNode(out Node node) && !string.IsNullOrEmpty(node.SourceName) ? node.SourceName : gameNode.name;

        #endregion

        #region Debug keys

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// Handles the debug keys (see the class summary). Only compiled into the Editor and
        /// development builds.
        /// </summary>
        private void Update()
        {
            if (!SEEInput.AgentEffectsDebugKey())
            {
                return;
            }

            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            bool alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

            EffectResult result;
            if (shift)
            {
                result = ResetEffects();
            }
            else if (alt)
            {
                result = ShowPath(DebugPathFrom(TestTarget, maxSteps: 4));
            }
            else if (ctrl)
            {
                // Pick a few random buildings of the city as "files Claude looked at".
                IEnumerable<string> visited = GameObject.FindGameObjectsWithTag(Tags.Node)
                                                        .OrderBy(_ => Random.value)
                                                        .Take(8)
                                                        .Select(g => g.name);
                result = ShowSearchTrail(visited, TestTarget);
            }
            else
            {
                // Without a modifier, the key opens the Ask SEE box (see AskSeeWindow).
                return;
            }
            Debug.Log($"[{nameof(AgentCityEffects)}] {result}\n");
        }

        /// <summary>
        /// A real path for testing <see cref="ShowPath"/>: starts at <paramref name="start"/> and
        /// follows outgoing edges that have a game edge, never visiting a node twice.
        /// </summary>
        /// <param name="start">Node ID or source name of the first node.</param>
        /// <param name="maxSteps">Maximum number of edges to follow.</param>
        /// <returns>Node IDs of the path; just the start if it has no drawn outgoing edge.</returns>
        private List<string> DebugPathFrom(string start, int maxSteps)
        {
            List<string> path = new();
            if (!Resolve(start, out GameObject gameNode).CallSucceeded || !gameNode.TryGetNode(out Node node))
            {
                path.Add(start); // ShowPath reports it as not found
                return path;
            }
            path.Add(node.ID);
            HashSet<Node> visited = new() { node };
            for (int i = 0; i < maxSteps; i++)
            {
                Edge next = node.Outgoings.FirstOrDefault(e => !visited.Contains(e.Target)
                                                               && e.Operator(mustFind: false) != null
                                                               && e.Target.GameObject(false) != null);
                if (next == null)
                {
                    break;
                }
                node = next.Target;
                visited.Add(node);
                path.Add(node.ID);
            }
            return path;
        }
#endif

        #endregion
    }
}
