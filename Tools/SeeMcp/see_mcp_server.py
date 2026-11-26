"""MCP server that lets Claude Code point at code in SEE's code cities ("Ask SEE").

Claude Code starts this script and talks MCP (JSON-RPC, one message per line) over
stdin/stdout. Each tool call is forwarded as an HTTP request to SEE's AgentCommandServer
(Assets/SEE/Tools/AI/AgentCommandServer.cs), which runs inside Unity in Play mode.

Only the Python standard library is used, so nothing needs to be installed.

Environment variables:
    SEE_AGENT_PORT  port of SEE's command server (default 8765)
"""

import json
import os
import sys
import urllib.error
import urllib.request

SERVER_NAME = "see"
SERVER_VERSION = "0.1.0"
SEE_URL = f"http://127.0.0.1:{int(os.environ.get('SEE_AGENT_PORT', '8765'))}/command"
SUPPORTED_PROTOCOLS = ("2024-11-05", "2025-03-26", "2025-06-18")

INSTRUCTIONS = """\
SEE is a 3D code city running in Unity: every class/file/package is a building.
The user watches the city while asking you questions about the code.

When the user asks where something is in the code ("where is X handled?"):
1. Find the answer in the source code with your normal tools (search and read files).
2. Call find_symbol with the class or file name to get the building's node ID.
   If several match, pick the one whose file fits your answer.
3. Call focus with that node ID, so SEE shows the building to the user.
4. Answer with file:line and a short explanation. Offer open_code if useful.

Only state dependencies (calls, uses) that the SEE tools report or that you saw in the source;
never add a step to a path yourself. If a tool reports no connection, say so.

To find the building for a file:line (e.g. from a stack trace or a log), call node_at
and use its focus_id with focus or show_path.

Use show_path for call chains, show_neighbors for "what uses / is used by X",
highlight for several related buildings, and reset when the user wants a clean city.
If a tool says SEE is not reachable, tell the user to start Play mode in Unity.
"""

TARGET = {"type": "string", "description": "Node ID (from find_symbol) or source name, e.g. 'ContextMenu'."}
TARGETS = {"type": "array", "items": {"type": "string"}, "description": "Node IDs or source names."}

TOOLS = [
    {
        "name": "status",
        "description": "Check that SEE is running and list the loaded code cities.",
        "inputSchema": {"type": "object", "properties": {}},
    },
    {
        "name": "find_symbol",
        "description": "Find buildings by class, file or package name (or part of it). "
                       "Returns node IDs with file and line. Use before focus/highlight.",
        "inputSchema": {
            "type": "object",
            "properties": {
                "query": {"type": "string", "description": "Name or part of it, e.g. 'ContextMenu' or 'ContextMenu.cs'."},
                "limit": {"type": "integer", "description": "Maximum number of matches (default 10)."},
            },
            "required": ["query"],
        },
    },
    {
        "name": "neighbors",
        "description": "List the incoming and outgoing dependencies of a node from the loaded graph "
                       "(e.g. callers and callees). Does not change the city.",
        "inputSchema": {
            "type": "object",
            "properties": {"target": TARGET, "limit": {"type": "integer", "description": "Maximum per direction (default 30)."}},
            "required": ["target"],
        },
    },
    {
        "name": "node_at",
        "description": "Find the node declared at a source location (file and line), e.g. the method "
                       "containing that line. Give file+line, or several locations at once. Returns the "
                       "node and focus_id, the building to use with focus or show_path (the node itself "
                       "or its nearest drawn ancestor). match 'range' is exact; 'nearest_declaration' "
                       "means the graph has no source ranges and the node declared last before the "
                       "line was taken, which may be wrong. Does not change the city.",
        "inputSchema": {
            "type": "object",
            "properties": {
                "file": {"type": "string", "description": "Absolute or relative path, e.g. 'Assets/SEE/Game/Foo.cs'."},
                "line": {"type": "integer", "description": "Line number (1-based)."},
                "locations": {
                    "type": "array",
                    "description": "Several locations instead of file+line.",
                    "items": {
                        "type": "object",
                        "properties": {"file": {"type": "string"}, "line": {"type": "integer"}},
                        "required": ["file", "line"],
                    },
                },
                "city": {"type": "string", "description": "Only search this city (name from status)."},
            },
        },
    },
    {
        "name": "focus",
        "description": "Show the user one building: dim the rest of the city, make it glow, put a beam "
                       "above it and fly the camera to it. Use this for the answer to 'where is ...'.",
        "inputSchema": {"type": "object", "properties": {"target": TARGET}, "required": ["target"]},
    },
    {
        "name": "focus_camera",
        "description": "Only fly the camera to a building, without other effects.",
        "inputSchema": {"type": "object", "properties": {"target": TARGET}, "required": ["target"]},
    },
    {
        "name": "highlight",
        "description": "Show several buildings at once (e.g. all classes matching a question): dims the rest of "
                       "the city, makes them pulse, and flies the camera over each one before showing all "
                       "together. Replaces earlier effects, so pass every building in one call.",
        "inputSchema": {"type": "object", "properties": {"targets": TARGETS}, "required": ["targets"]},
    },
    {
        "name": "show_path",
        "description": "Animate a path (e.g. a call chain) node by node, drawing the edges in between, "
                       "while the camera follows; ends with an overview of the whole path. Give at least two "
                       "nodes in order; usually just the start and the end. SEE finds the shortest connection "
                       "in the dependency data between nodes that are not directly connected, and the result "
                       "lists the complete path. Report that path; do not add steps of your own.",
        "inputSchema": {"type": "object", "properties": {"targets": TARGETS}, "required": ["targets"]},
    },
    {
        "name": "show_neighbors",
        "description": "Show a building with everything connected to it: incoming dependencies pulse orange, "
                       "outgoing ones green, with their edges drawn; the camera then pulls back to show them all.",
        "inputSchema": {"type": "object", "properties": {"target": TARGET}, "required": ["target"]},
    },
    {
        "name": "search_trail",
        "description": "Briefly flash the buildings you looked at while searching, in order, then focus the final one.",
        "inputSchema": {
            "type": "object",
            "properties": {
                "visited": TARGETS,
                "final": {"type": "string", "description": "Building to focus at the end (optional)."},
            },
            "required": ["visited"],
        },
    },
    {
        "name": "open_code",
        "description": "Open SEE's code window for a building, scrolled to a line. Needs the source files.",
        "inputSchema": {
            "type": "object",
            "properties": {"target": TARGET, "line": {"type": "integer", "description": "Line to show (optional)."}},
            "required": ["target"],
        },
    },
    {
        "name": "reset",
        "description": "Remove all effects (glow, dimming, beam, drawn edges). The camera stays where it is.",
        "inputSchema": {"type": "object", "properties": {}},
    },
]
TOOL_NAMES = {tool["name"] for tool in TOOLS}


def log(message):
    """Logs to stderr; stdout is reserved for MCP messages."""
    print(f"[see-mcp] {message}", file=sys.stderr, flush=True)


def call_see(tool, args):
    """Sends one command to SEE and returns (is_error, text for Claude)."""
    body = json.dumps({"tool": tool, "args": args}).encode("utf-8")
    request = urllib.request.Request(
        SEE_URL, data=body, method="POST",
        headers={"Content-Type": "application/json", "X-SEE-Agent": "1"},
    )
    try:
        with urllib.request.urlopen(request, timeout=20) as response:
            payload = response.read().decode("utf-8")
    except urllib.error.HTTPError as error:
        payload = error.read().decode("utf-8", errors="replace")
    except (urllib.error.URLError, TimeoutError, ConnectionError) as error:
        return True, (f"SEE is not reachable at {SEE_URL} ({getattr(error, 'reason', error)}). "
                      "Ask the user to start Play mode in Unity (Editor or a development build).")
    try:
        result = json.loads(payload)
    except json.JSONDecodeError:
        return True, f"SEE sent an invalid answer: {payload[:500]}"
    return not result.get("ok", False), json.dumps(result, indent=1, ensure_ascii=False)


def handle(message):
    """Returns the JSON-RPC response for one message, or None for notifications."""
    method = message.get("method")
    msg_id = message.get("id")
    params = message.get("params") or {}

    if msg_id is None:
        return None  # notification, e.g. notifications/initialized

    if method == "initialize":
        requested = params.get("protocolVersion")
        version = requested if requested in SUPPORTED_PROTOCOLS else SUPPORTED_PROTOCOLS[-1]
        result = {
            "protocolVersion": version,
            "capabilities": {"tools": {}},
            "serverInfo": {"name": SERVER_NAME, "version": SERVER_VERSION},
            "instructions": INSTRUCTIONS,
        }
    elif method == "ping":
        result = {}
    elif method == "tools/list":
        result = {"tools": TOOLS}
    elif method == "tools/call":
        name = params.get("name")
        if name not in TOOL_NAMES:
            return error_response(msg_id, -32602, f"Unknown tool: {name}")
        is_error, text = call_see(name, params.get("arguments") or {})
        result = {"content": [{"type": "text", "text": text}], "isError": is_error}
    else:
        return error_response(msg_id, -32601, f"Method not found: {method}")
    return {"jsonrpc": "2.0", "id": msg_id, "result": result}


def error_response(msg_id, code, text):
    """A JSON-RPC error."""
    return {"jsonrpc": "2.0", "id": msg_id, "error": {"code": code, "message": text}}


def main():
    """Reads one JSON-RPC message per line from stdin and answers on stdout."""
    sys.stdin.reconfigure(encoding="utf-8")
    sys.stdout.reconfigure(encoding="utf-8", newline="\n")
    log(f"started, forwarding to {SEE_URL}")
    for line in sys.stdin:
        line = line.strip()
        if not line:
            continue
        try:
            message = json.loads(line)
        except json.JSONDecodeError:
            response = error_response(None, -32700, "Parse error")
        else:
            try:
                response = handle(message)
            except Exception as error:  # keep the server alive whatever happens
                log(f"error: {error!r}")
                response = error_response(message.get("id"), -32603, str(error))
        if response is not None:
            sys.stdout.write(json.dumps(response, ensure_ascii=False) + "\n")
            sys.stdout.flush()


if __name__ == "__main__":
    main()
