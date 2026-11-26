"""Temporary logs ("probes") that answer runtime questions: the probe_run tool of the SEE MCP server.

A probe is a source location plus expressions: "log sum and i before line 25 of Program.cs".
probe_run never touches the user's files. It works in a disposable copy of the project:

1. Copy: a git worktree if the project is in a git repository (plus the uncommitted and
   untracked changes, so the run uses the code the user sees), otherwise a plain directory copy.
2. Instrument: insert one log statement before each probed line, writing a marker line to stderr.
3. Run the user's command in the copy (with a timeout) and collect the marker lines in order.
4. Map each probed location to a SEE node with node_at and show the execution path with show_path.
5. Delete the copy.

This is source-level instrumentation; it needs no debugger, so it works for every MCP client.
Only the Python standard library is used.
"""

import json
import os
import shutil
import signal
import stat
import subprocess
import sys
import tempfile

MARKER = "@@SEE-PROBE@@"
"""Starts every line a probe writes; followed by tab-separated fields: probe id, then one value per expression."""

DEFAULT_TIMEOUT_S = 120
DEFAULT_MAX_HITS = 200
MAX_VALUE_LENGTH = 200
OUTPUT_TAIL_LINES = 40
MAX_PATH_STEPS = 20
"""At most this many steps are passed to show_path; longer paths are cut."""

SKIPPED_DIRECTORIES = (".git", ".vs", ".idea", "bin", "obj", "Library", "Temp", "Logs", "node_modules", "__pycache__")
"""Not copied when the project is not in a git repository (build output and caches)."""

LANGUAGES = {".cs": "csharp", ".py": "python"}
"""Supported file extensions and the language of the inserted statement."""


class ProbeError(Exception):
    """A problem that prevents the probes from running; its message is meant for the agent."""


# ---------------------------------------------------------------------------
# Instrumentation
# ---------------------------------------------------------------------------

def statement(language, probe_id, expressions):
    """The statement that logs the values of expressions for the probe with the given id."""
    if language == "csharp":
        parts = [f'"{MARKER}\\t{probe_id}"']
        for expression in expressions:
            # Invariant culture: 64.6 must not become "64,6" on a German system.
            # Tabs and line breaks would break the marker line.
            value = (f"(System.Convert.ToString({expression}, System.Globalization.CultureInfo.InvariantCulture)"
                     f' ?? "null").Replace(\'\\t\', \' \').Replace(\'\\r\', \' \').Replace(\'\\n\', \' \')')
            parts.append(f'"\\t" + {value}')
        return f"System.Console.Error.WriteLine({' + '.join(parts)});"
    if language == "python":
        values = "".join(f' + "\\t" + repr({expression}).replace("\\t", " ")' for expression in expressions)
        return f'print("{MARKER}\\t{probe_id}"{values}, file=__import__("sys").stderr, flush=True)'
    raise ProbeError(f"Unsupported language '{language}'.")


def instrument(text, insertions, language):
    """Inserts log statements into the source text of one file.

    insertions: list of (line, probe_id, expressions); the statement goes before that line
    (1-based) with the indentation of that line, or of the closest non-blank line above it
    if that line is blank. Insertion runs bottom-up, so the given lines refer to the original file.
    The line ending style of the file is kept.
    """
    newline = "\r\n" if "\r\n" in text else "\n"
    lines = text.splitlines(keepends=True)
    for line, probe_id, expressions in sorted(insertions, key=lambda i: i[0], reverse=True):
        if not 1 <= line <= len(lines):
            raise ProbeError(f"Line {line} is outside the file (1..{len(lines)}).")
        indentation = ""
        for candidate in range(line - 1, -1, -1):
            if lines[candidate].strip():
                content = lines[candidate]
                indentation = content[:len(content) - len(content.lstrip())]
                break
        lines.insert(line - 1, indentation + statement(language, probe_id, expressions) + newline)
    return "".join(lines)


# ---------------------------------------------------------------------------
# Disposable copy
# ---------------------------------------------------------------------------

def git(directory, *args, input_bytes=None, check=True):
    """Runs git in directory and returns its completed process (stdout as bytes)."""
    env = dict(os.environ, GIT_LFS_SKIP_SMUDGE="1")
    result = subprocess.run(["git", "-C", directory, *args], input=input_bytes, capture_output=True, env=env)
    if check and result.returncode != 0:
        raise ProbeError(f"git {' '.join(args)} failed: {result.stderr.decode('utf-8', 'replace').strip()}")
    return result


def remove_tree(path):
    """Deletes the folder path; never raises.

    On Windows, git's object files are read-only, which makes shutil.rmtree fail;
    the read-only flag is cleared and the deletion retried.
    """
    def retry_writable(function, failed_path, _):
        try:
            os.chmod(failed_path, stat.S_IWRITE)
            function(failed_path)
        except OSError:
            pass
    if sys.version_info >= (3, 12):
        shutil.rmtree(path, onexc=retry_writable)
    else:
        shutil.rmtree(path, onerror=retry_writable)


def repository_top(directory):
    """The top folder of the git repository containing directory, or None if there is none."""
    try:
        result = git(directory, "rev-parse", "--show-toplevel", check=False)
    except OSError:
        return None  # git is not installed
    if result.returncode != 0:
        return None
    return os.path.normpath(result.stdout.decode("utf-8").strip())


class DisposableCopy:
    """A temporary copy of a project folder, deleted by close().

    root is the copied project folder inside the copy; kind is "git worktree" or "directory copy".
    """

    def __init__(self, project_root):
        self.project_root = os.path.normpath(os.path.abspath(project_root))
        self.temp = tempfile.mkdtemp(prefix="see-probe-")
        self.top = repository_top(self.project_root)
        try:
            if self.top:
                self.kind = "git worktree"
                self._make_worktree()
            else:
                self.kind = "directory copy"
                self.root = os.path.join(self.temp, "copy")
                shutil.copytree(self.project_root, self.root, ignore=shutil.ignore_patterns(*SKIPPED_DIRECTORIES))
        except BaseException:
            self.close()
            raise

    def _make_worktree(self):
        """Creates a detached worktree of HEAD and copies the uncommitted and untracked changes into it."""
        git(self.top, "worktree", "prune", check=False)  # left over from runs that crashed
        self.worktree = os.path.join(self.temp, "worktree")
        git(self.top, "worktree", "add", "--detach", self.worktree, "HEAD")
        self.root = os.path.join(self.worktree, os.path.relpath(self.project_root, self.top))

        changes = git(self.top, "diff", "HEAD", "--binary").stdout
        if changes.strip():
            git(self.worktree, "apply", "--whitespace=nowarn", input_bytes=changes)
        untracked = git(self.top, "ls-files", "--others", "--exclude-standard", "-z").stdout
        for name in filter(None, untracked.decode("utf-8").split("\0")):
            target = os.path.join(self.worktree, name)
            os.makedirs(os.path.dirname(target), exist_ok=True)
            shutil.copy2(os.path.join(self.top, name), target)

    def path_in_copy(self, file):
        """The copy of file, which is absolute or relative to the project folder and must lie inside it."""
        original = os.path.normpath(os.path.join(self.project_root, file))
        relative = os.path.relpath(original, self.project_root)
        if relative == os.pardir or relative.startswith(os.pardir + os.sep) or os.path.isabs(relative):
            raise ProbeError(f"'{file}' is not inside the project folder '{self.project_root}'.")
        return os.path.join(self.root, relative)

    def close(self):
        """Deletes the copy; never raises."""
        if getattr(self, "worktree", None) and self.top:
            try:
                git(self.top, "worktree", "remove", "--force", self.worktree, check=False)
            except OSError:
                pass
        try:
            remove_tree(self.temp)
        except OSError:
            pass  # e.g. a file still opened by a process that outlived the timeout


# ---------------------------------------------------------------------------
# Running and parsing
# ---------------------------------------------------------------------------

def kill_tree(process):
    """Kills process and all processes it started (e.g. a build started by a shell)."""
    try:
        if os.name == "nt":
            subprocess.run(["taskkill", "/PID", str(process.pid), "/T", "/F"], capture_output=True)
        else:
            os.killpg(process.pid, signal.SIGKILL)
    except OSError:
        process.kill()


def run_command(command, directory, timeout_s):
    """Runs command in the system shell in directory; returns (exit code or None, output, timed out).

    stdout and stderr are merged, so the marker lines keep their order relative to the program output.
    """
    process = subprocess.Popen(
        command, shell=True, cwd=directory,
        stdin=subprocess.DEVNULL, stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
        env=dict(os.environ, GIT_LFS_SKIP_SMUDGE="1"),
        start_new_session=os.name != "nt",
    )
    try:
        output, _ = process.communicate(timeout=timeout_s)
        timed_out = False
    except subprocess.TimeoutExpired:
        kill_tree(process)
        output, _ = process.communicate()
        timed_out = True
    text = output.decode("utf-8", errors="replace")
    return (None if timed_out else process.returncode), text, timed_out


def parse_hits(output, probes, max_hits):
    """The marker lines in output as hits, in order.

    probes: the normalized probes (dicts with file, line, expressions), indexed by probe id.
    Only the first max_hits hits are returned; values are cut to MAX_VALUE_LENGTH characters.
    Returns (hits, total number of hits, number of hits per probe id).
    """
    hits = []
    total = 0
    counts = [0] * len(probes)
    for raw in output.splitlines():
        start = raw.find(MARKER)
        if start < 0:
            continue
        fields = raw[start + len(MARKER):].split("\t")[1:]
        if not fields or not fields[0].isdigit() or int(fields[0]) >= len(probes):
            continue
        probe = probes[int(fields[0])]
        total += 1
        counts[int(fields[0])] += 1
        if len(hits) < max_hits:
            values = {expression: shorten(value)
                      for expression, value in zip(probe["expressions"], fields[1:])}
            hits.append({"seq": total, "file": probe["file"], "line": probe["line"], "values": values})
    return hits, total, counts


def shorten(value):
    """value cut to MAX_VALUE_LENGTH characters."""
    return value if len(value) <= MAX_VALUE_LENGTH else value[:MAX_VALUE_LENGTH] + "…"


def collapse(sequence):
    """sequence without consecutive repetitions and without None."""
    result = []
    for item in sequence:
        if item is not None and (not result or result[-1] != item):
            result.append(item)
    return result


# ---------------------------------------------------------------------------
# The tool
# ---------------------------------------------------------------------------

def normalize_probes(raw_probes):
    """Checks the probes argument and returns a list of dicts with file, line and expressions."""
    if not isinstance(raw_probes, list) or not raw_probes:
        raise ProbeError("probes must be a non-empty list of {file, line, expressions}.")
    probes = []
    for raw in raw_probes:
        if not isinstance(raw, dict) or not raw.get("file") or not isinstance(raw.get("line"), int):
            raise ProbeError(f"Each probe needs file (string) and line (integer), got {json.dumps(raw)}.")
        expressions = raw.get("expressions") or []
        if not isinstance(expressions, list) or not all(isinstance(e, str) and e.strip() for e in expressions):
            raise ProbeError(f"expressions of the probe at {raw['file']}:{raw['line']} must be a list of strings.")
        language = LANGUAGES.get(os.path.splitext(raw["file"])[1].lower())
        if language is None:
            raise ProbeError(f"Cannot probe '{raw['file']}': only {', '.join(sorted(LANGUAGES))} files are supported.")
        probes.append({"file": raw["file"], "line": raw["line"], "expressions": expressions, "language": language})
    return probes


def instrument_copy(copy, probes):
    """Inserts the statements of all probes into the files of copy."""
    by_file = {}
    for probe_id, probe in enumerate(probes):
        by_file.setdefault(copy.path_in_copy(probe["file"]), []).append((probe_id, probe))
    for path, entries in by_file.items():
        if not os.path.isfile(path):
            raise ProbeError(f"File not found in the project: {entries[0][1]['file']}")
        with open(path, "rb") as stream:
            data = stream.read()
        bom = data.startswith(b"\xef\xbb\xbf")
        text = data.decode("utf-8-sig")
        try:
            text = instrument(text, [(p["line"], i, p["expressions"]) for i, p in entries], entries[0][1]["language"])
        except ProbeError as error:
            raise ProbeError(f"{entries[0][1]['file']}: {error}") from None
        with open(path, "wb") as stream:
            stream.write((b"\xef\xbb\xbf" if bom else b"") + text.encode("utf-8"))


def show_in_see(see, project_root, probes, hits, show_path):
    """Maps the probed locations to SEE nodes and, if wanted, shows the path of the hits.

    Adds node_id/node_name/match to each probe; returns (path of node ids, message about SEE).
    """
    locations = [{"file": os.path.join(project_root, p["file"]), "line": p["line"]} for p in probes]
    is_error, text = see("node_at", {"locations": locations})
    try:
        answer = json.loads(text)
    except json.JSONDecodeError:
        return [], f"SEE not used: {text}"
    for probe, located in zip(probes, answer.get("locations", [])):
        if located.get("ok"):
            probe["node_id"] = located.get("focus_id")
            probe["node_name"] = (located.get("node") or {}).get("name")
            probe["match"] = located.get("match")
    if is_error and not answer.get("locations"):
        return [], f"SEE could not map the probes: {answer.get('message', text)}"

    node_of = {(p["file"], p["line"]): p.get("node_id") for p in probes}
    path = collapse(node_of[(h["file"], h["line"])] for h in hits)
    if not show_path:
        return path, "Path not shown (show_path is false)."
    if len(path) < 2:
        return path, "Path not shown: the hits lie in fewer than two SEE nodes."
    shown = path[:MAX_PATH_STEPS]
    is_error, text = see("show_path", {"targets": shown})
    try:
        message = json.loads(text).get("message", text)
    except json.JSONDecodeError:
        message = text
    cut = f" (first {MAX_PATH_STEPS} of {len(path)} steps)" if len(path) > MAX_PATH_STEPS else ""
    return path, ("show_path failed: " if is_error else f"show_path{cut}: ") + message


def run_probes(arguments, see):
    """Runs the probe_run tool; see(tool, args) sends a command to SEE and returns (is_error, text).

    Returns the answer as a dict; ok is false if the probes could not run or the command failed
    without reaching any probe.
    """
    command = arguments.get("command")
    if not isinstance(command, str) or not command.strip():
        raise ProbeError("command (the shell command that builds and runs the program) is required.")
    project_root = os.path.abspath(arguments.get("root") or os.getcwd())
    if not os.path.isdir(project_root):
        raise ProbeError(f"Project folder not found: {project_root}")
    probes = normalize_probes(arguments.get("probes"))
    timeout_s = int(arguments.get("timeout_s") or DEFAULT_TIMEOUT_S)
    max_hits = int(arguments.get("max_hits") or DEFAULT_MAX_HITS)

    copy = DisposableCopy(project_root)
    try:
        instrument_copy(copy, probes)
        exit_code, output, timed_out = run_command(command, copy.root, timeout_s)
    finally:
        copy.close()

    hits, total, counts = parse_hits(output, probes, max_hits)
    for probe, count in zip(probes, counts):
        probe["hits"] = count
        del probe["language"]
    path, see_message = show_in_see(see, project_root, probes, hits, arguments.get("show_path", True)) \
        if hits else ([], "SEE not used: no hits.")

    failed = total == 0 and (timed_out or exit_code != 0)
    if timed_out:
        message = f"Timed out after {timeout_s} s; {total} hit(s) before that."
    elif total == 0:
        message = (f"The command failed with exit code {exit_code} before any probe was reached "
                   "(e.g. a build error, maybe caused by an inserted line; see output_tail)."
                   if exit_code != 0 else "The command ran, but no probe was reached.")
    else:
        message = f"{total} hit(s)" + (f", showing the first {max_hits}" if total > max_hits else "") + "."
    answer = {
        "ok": not failed,
        "message": message,
        "mode": "source",
        "copy": copy.kind,
        "exit_code": exit_code,
        "timed_out": timed_out,
        "hits_total": total,
        "hits": hits,
        "probes": probes,
        "path": path,
        "see": see_message,
    }
    if failed or total == 0 or exit_code != 0:
        answer["output_tail"] = "\n".join(line for line in output.splitlines()[-OUTPUT_TAIL_LINES:]
                                          if MARKER not in line)
    return answer


def run_tool(arguments, see):
    """The MCP entry point: returns (is_error, text for the agent)."""
    try:
        answer = run_probes(arguments, see)
    except ProbeError as error:
        answer = {"ok": False, "message": str(error)}
    except OSError as error:
        answer = {"ok": False, "message": f"{type(error).__name__}: {error}"}
    return not answer["ok"], json.dumps(answer, indent=1, ensure_ascii=False)


TOOL = {
    "name": "probe_run",
    "description": (
        "Answer runtime questions (which code runs, in which order, with which values) by adding temporary "
        "log statements and running the program. Works in a disposable copy of the project (git worktree, "
        "or a folder copy outside git); the user's files are never changed. Each probe logs its expressions "
        "just BEFORE the given line, so pick a line that is a statement inside a method body (not a "
        "declaration, brace, else or case line) where the expressions are in scope. Supports C# (.cs) and "
        "Python (.py). Returns the hits in execution order with their values, the SEE node of each probe, and "
        "shows the execution path in SEE. If the command fails, output_tail shows why (e.g. a compile error "
        "from an inserted line: move the probe)."
    ),
    "inputSchema": {
        "type": "object",
        "properties": {
            "command": {"type": "string",
                        "description": "Shell command that builds and runs the program, executed in the copy "
                                       "of root (cmd.exe on Windows, so chain with &&)."},
            "root": {"type": "string", "description": "Project folder (default: the current folder)."},
            "probes": {
                "type": "array",
                "items": {
                    "type": "object",
                    "properties": {
                        "file": {"type": "string", "description": "Path relative to root, or absolute inside it."},
                        "line": {"type": "integer", "description": "The values are logged before this line (1-based)."},
                        "expressions": {"type": "array", "items": {"type": "string"},
                                        "description": "Expressions to log, e.g. [\"sum\", \"i\"]; may be empty."},
                    },
                    "required": ["file", "line"],
                },
            },
            "timeout_s": {"type": "integer", "description": f"Time limit in seconds (default {DEFAULT_TIMEOUT_S})."},
            "max_hits": {"type": "integer", "description": f"Maximum hits returned (default {DEFAULT_MAX_HITS})."},
            "show_path": {"type": "boolean", "description": "Show the execution path in SEE (default true)."},
        },
        "required": ["command", "probes"],
    },
}


if __name__ == "__main__":
    # Manual use: python probe.py '<arguments as JSON>' (without SEE).
    print(run_tool(json.loads(sys.argv[1]), lambda tool, args: (True, "SEE is not used from the command line."))[1])
