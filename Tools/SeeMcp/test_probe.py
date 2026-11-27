"""Unit tests for probe.py; they need neither SEE nor .NET.

Run from the repository root: python -m unittest discover -s Tools/SeeMcp
"""

import json
import os
import subprocess
import sys
import tempfile
import textwrap
import unittest

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import probe  # noqa: E402  (needs the path above)

PROGRAM = textwrap.dedent("""\
    def average(values):
        total = 0
        for i in range(1, len(values)):
            total += values[i]
        return total / len(values)


    print("Average:", average([72, 95, 88]))
""")
"""A small Python program with the off-by-one bug of the debug demo; line 4 is the loop body."""


def no_see(tool, args):
    """Stands in for SEE when it is not running."""
    return True, "SEE is not reachable."


class FakeSee:
    """Records the commands sent to SEE and maps every location to the node 'n<line>'.

    directories maps a file name to the source directory of the city that knows it;
    files not in it are unknown. Without directories, every file is known (directory None).
    """

    def __init__(self, directories=None):
        self.calls = []
        self.directories = directories

    def __call__(self, tool, args):
        self.calls.append((tool, args))
        if tool == "node_at":
            located = [self.locate(loc) for loc in args["locations"]]
            return False, json.dumps({"ok": any(l["ok"] for l in located), "locations": located})
        return False, json.dumps({"ok": True, "message": "Path shown."})

    def locate(self, location):
        """The node_at answer for one location."""
        name = os.path.basename(location["file"])
        if self.directories is not None and name not in self.directories:
            return {"ok": False, "message": f"No node of any city is declared in '{location['file']}'."}
        return {"ok": True, "match": "range", "focus_id": f"n{location['line']}", "city": "CodeCity",
                "source_directory": (self.directories or {}).get(name),
                "node": {"name": f"N{location['line']}", "file": name}}


class InstrumentTest(unittest.TestCase):
    """Inserting the log statements."""

    def test_python_statement_goes_before_line_with_its_indentation(self):
        result = probe.instrument(PROGRAM, [(4, 0, ["i", "total"])], "python").splitlines()
        self.assertTrue(result[3].startswith("        print(\"@@SEE-PROBE@@\\t0\""))
        self.assertEqual(result[4], "        total += values[i]")

    def test_lines_refer_to_the_original_file(self):
        result = probe.instrument(PROGRAM, [(2, 0, []), (4, 1, [])], "python").splitlines()
        self.assertIn("\\t0", result[1])
        self.assertEqual(result[2], "    total = 0")
        self.assertIn("\\t1", result[4])
        self.assertEqual(result[5], "        total += values[i]")

    def test_blank_line_takes_indentation_from_above(self):
        result = probe.instrument("class A\n{\n    void M()\n    {\n\n    }\n}\n", [(5, 0, [])], "csharp").splitlines()
        self.assertTrue(result[4].startswith("    System.Console.Error.WriteLine("))

    def test_crlf_is_kept(self):
        result = probe.instrument("a = 1\r\nb = 2\r\n", [(2, 0, ["a"])], "python")
        self.assertNotIn("\n", result.replace("\r\n", ""))

    def test_csharp_statement_uses_invariant_culture(self):
        text = probe.statement("csharp", 3, ["sum", "values[i]"])
        self.assertTrue(text.startswith('System.Console.Error.WriteLine("@@SEE-PROBE@@\\t3"'))
        self.assertIn("System.Convert.ToString(values[i], System.Globalization.CultureInfo.InvariantCulture)", text)
        self.assertTrue(text.endswith(");"))

    def test_line_outside_file_is_rejected(self):
        with self.assertRaises(probe.ProbeError):
            probe.instrument("x = 1\n", [(5, 0, [])], "python")


class ParseTest(unittest.TestCase):
    """Reading the marker lines."""

    PROBES = [{"file": "a.py", "line": 4, "expressions": ["i", "total"]}, {"file": "a.py", "line": 5, "expressions": []}]

    def test_hits_in_order_with_values(self):
        output = "noise\n@@SEE-PROBE@@\t0\t1\t0\nmore noise\n@@SEE-PROBE@@\t0\t2\t95\n@@SEE-PROBE@@\t1\n"
        hits, total, counts = probe.parse_hits(output, self.PROBES, 10)
        self.assertEqual(total, 3)
        self.assertEqual(counts, [2, 1])
        self.assertEqual(hits[1], {"seq": 2, "file": "a.py", "line": 4, "values": {"i": "2", "total": "95"}})
        self.assertEqual(hits[2]["values"], {})

    def test_max_hits_and_long_values(self):
        output = "".join(f"@@SEE-PROBE@@\t0\t{'x' * 500}\t{n}\n" for n in range(5))
        hits, total, counts = probe.parse_hits(output, self.PROBES, 2)
        self.assertEqual((len(hits), total, counts[0]), (2, 5, 5))
        self.assertEqual(len(hits[0]["values"]["i"]), probe.MAX_VALUE_LENGTH + 1)

    def test_unknown_probe_ids_are_ignored(self):
        hits, total, _ = probe.parse_hits("@@SEE-PROBE@@\t7\n@@SEE-PROBE@@\tx\n", self.PROBES, 10)
        self.assertEqual((hits, total), ([], 0))

    def test_collapse(self):
        self.assertEqual(probe.collapse(["a", "a", None, "b", "b", "a"]), ["a", "b", "a"])


class RunTest(unittest.TestCase):
    """The whole tool on small temporary projects."""

    def setUp(self):
        self.folder = tempfile.mkdtemp(prefix="see-probe-test-")
        with open(os.path.join(self.folder, "app.py"), "w", encoding="utf-8", newline="\n") as stream:
            stream.write(PROGRAM)
        self.command = f'"{sys.executable}" app.py'

    def tearDown(self):
        probe.remove_tree(self.folder)
        self.assertFalse(os.path.exists(self.folder), "the test folder could not be deleted")

    def arguments(self, **changes):
        arguments = {"command": self.command, "root": self.folder,
                     "probes": [{"file": "app.py", "line": 4, "expressions": ["i", "total"]},
                                {"file": "app.py", "line": 5, "expressions": ["total"]}]}
        arguments.update(changes)
        return arguments

    def test_directory_copy_finds_the_bug_and_leaves_the_project_unchanged(self):
        see = FakeSee()
        answer = probe.run_probes(self.arguments(), see)
        self.assertTrue(answer["ok"], answer)
        self.assertEqual(answer["copy"], "directory copy")
        self.assertEqual([h["values"].get("i") for h in answer["hits"]], ["1", "2", None])
        self.assertEqual(answer["hits"][-1]["values"], {"total": "183"})
        self.assertEqual(answer["path"], ["n4", "n5"])
        self.assertEqual(see.calls[-1], ("show_path", {"targets": ["n4", "n5"]}))
        with open(os.path.join(self.folder, "app.py"), encoding="utf-8") as stream:
            self.assertEqual(stream.read(), PROGRAM)

    def test_git_worktree_includes_uncommitted_changes(self):
        def run_git(*args):
            subprocess.run(["git", "-C", self.folder, *args], check=True, capture_output=True)
        run_git("init", "-q")
        run_git("add", "app.py")
        run_git("-c", "user.name=t", "-c", "user.email=t@example.com", "commit", "-q", "-m", "initial")
        with open(os.path.join(self.folder, "app.py"), "a", encoding="utf-8") as stream:
            stream.write('print("changed")\n')  # uncommitted change
        with open(os.path.join(self.folder, "extra.py"), "w", encoding="utf-8") as stream:
            stream.write("X = 1\n")  # untracked file

        answer = probe.run_probes(self.arguments(command=self.command + " && " + f'"{sys.executable}" extra.py'), no_see)
        self.assertTrue(answer["ok"], answer)
        self.assertEqual(answer["copy"], "git worktree")
        self.assertEqual(answer["hits_total"], 3)
        self.assertTrue(answer["see"].startswith("SEE not used"), answer["see"])
        worktrees = subprocess.run(["git", "-C", self.folder, "worktree", "list"], capture_output=True, text=True).stdout
        self.assertEqual(len(worktrees.strip().splitlines()), 1)  # the temporary worktree is gone

    def test_failing_command_returns_output_tail(self):
        answer = probe.run_probes(self.arguments(probes=[{"file": "app.py", "line": 3, "expressions": ["undefined_name"]}]),
                                  no_see)
        self.assertFalse(answer["ok"])
        self.assertIn("NameError", answer["output_tail"])

    def test_timeout(self):
        with open(os.path.join(self.folder, "slow.py"), "w", encoding="utf-8") as stream:
            stream.write("import time\nprint('start')\ntime.sleep(30)\n")
        answer = probe.run_probes(self.arguments(command=f'"{sys.executable}" slow.py', timeout_s=2,
                                                 probes=[{"file": "slow.py", "line": 3, "expressions": []}]), no_see)
        self.assertTrue(answer["timed_out"])
        self.assertEqual(answer["hits_total"], 1)

    def test_root_from_see_takes_the_city_source_directory(self):
        arguments = self.arguments(probes=[{"file": os.path.join(self.folder, "app.py"), "line": 4, "expressions": ["i"]}])
        del arguments["root"]
        answer = probe.run_probes(arguments, FakeSee({"app.py": self.folder}))
        self.assertTrue(answer["ok"], answer)
        self.assertEqual(answer["root"], os.path.abspath(self.folder))
        self.assertEqual(answer["probes"][0]["file"], "app.py")  # rewritten to the node's path
        self.assertEqual(answer["hits_total"], 2)

    def test_root_from_see_unknown_file(self):
        arguments = self.arguments()
        del arguments["root"]
        is_error, text = probe.run_tool(arguments, FakeSee({}))
        self.assertTrue(is_error)
        self.assertIn("no city in SEE knows app.py", json.loads(text)["message"])

    def test_root_from_see_without_see(self):
        arguments = self.arguments()
        del arguments["root"]
        is_error, text = probe.run_tool(arguments, no_see)
        self.assertTrue(is_error)
        self.assertIn("SEE cannot tell the project folder", json.loads(text)["message"])

    def test_root_from_see_two_projects(self):
        arguments = self.arguments(probes=[{"file": "app.py", "line": 4}, {"file": "other.py", "line": 1}])
        del arguments["root"]
        is_error, text = probe.run_tool(arguments, FakeSee({"app.py": self.folder, "other.py": tempfile.gettempdir()}))
        self.assertTrue(is_error)
        self.assertIn("several projects", json.loads(text)["message"])

    def test_root_from_see_city_without_source_directory(self):
        arguments = self.arguments()
        del arguments["root"]
        is_error, text = probe.run_tool(arguments, FakeSee())
        self.assertTrue(is_error)
        self.assertIn("has no source code directory", json.loads(text)["message"])

    def test_unsupported_file_type(self):
        is_error, text = probe.run_tool(self.arguments(probes=[{"file": "main.cpp", "line": 1}]), no_see)
        self.assertTrue(is_error)
        self.assertIn("only .cs, .py files are supported", json.loads(text)["message"])

    def test_file_outside_project(self):
        is_error, text = probe.run_tool(self.arguments(probes=[{"file": "../other.py", "line": 1}]), no_see)
        self.assertTrue(is_error)
        self.assertIn("not inside the project folder", json.loads(text)["message"])


if __name__ == "__main__":
    unittest.main()
