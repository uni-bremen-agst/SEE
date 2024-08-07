#!/usr/bin/env python3

# A script which takes in a unified diff expected to be generated by
# git diff -U0 | grep '^[+@]'
# and outputs any bad pattern matches. See documentation of
# BadPattern.to_comment() for details of the output format.
# Note that this script is only run on CI, not as part of the Git hooks,
# due to it being written in Python rather than as a shell script.

import json
import re
import sys
from enum import Enum
from typing import Dict, List, Optional, Union


class Level(str, Enum):
    """
    Severity level of a bad pattern, associated to a GitHub alert.
    """

    NOTE = "NOTE"
    WARNING = "WARNING"
    IMPORTANT = "IMPORTANT"


# Filenames that a pattern will be applied to by default.
DEFAULT_FILENAMES = (r".*\.cs$",)

# List of matches to be printed as a JSON array at the end of the script.
collected_matches: List[Dict[str, Union[str, int]]] = []


class BadPattern:
    """
    A pattern within a file that must be avoided.
    """

    def __init__(
        self,
        regex,
        message,
        filenames=DEFAULT_FILENAMES,
        suggestion=None,
        level=Level.NOTE,
        see_only=True,
    ):
        """
        Takes a compiled regular expression `regex` that is checked against
        every line within changed files having a filename matched by a regex contained in
        `filenames`, a `message` that shall be displayed to the user in case
        a match has been found, a regex substitution `suggestion` for a found
        bad pattern, and a severity `level`.
        If `see_only` is set to `True`, the pattern will only be applied to
        files under `Assets/SEE`.

        """
        if regex is None:
            # Accept anything.
            regex = re.compile(r".*")
        self.regex = regex
        self.message = message
        self.filenames = [re.compile(x) for x in filenames]
        self.suggestion = suggestion
        self.level = level
        self.see_only = see_only

    def applies_to(self, filename: str, line: str = "") -> bool:
        """
        Returns whether the given pattern applies to the given filename.
        A pattern applies to a filename if:
        * The filename matches one of the regexes in `filenames`.
        * The filename starts with `Assets/SEE/` if `see_only` is set to `True`.
        * The line matches the regular expression `regex`.
        """
        return (
            (not self.see_only or filename.startswith("Assets/SEE/"))
            and any(x.match(filename) is not None for x in self.filenames)
            and self.regex.match(line) is not None
        )

    def to_json(
        self, filename: str, line_number: int, suggestion: Optional[str]
    ) -> Dict[str, Union[str, int]]:
        """
        Turns this bad pattern match into a dictionary representing a GitHub comment.
        """
        body_text = f"> [!{self.level.value}]\n> {self.message}\n"
        if suggestion is not None:
            body_text += f"\n```suggestion\n{suggestion}\n```"
        return {
            "path": filename,
            "line": line_number,
            "body": body_text,
        }


# Special case for missing newline at end of file, as this can't be detected on a per-line basis.
NO_NEWLINE_BAD_PATTERN = BadPattern(
    None,
    "Missing newline at end of file! Files should always end with a single newline character.",
    level=Level.WARNING,
    suggestion=r"\n",
)

# *** MODIFY BELOW TO ADD NEW BAD PATTERNS ***

BAD_PATTERNS = [
    BadPattern(
        re.compile(r"^(.*(?<!= )new \w*NetAction\w*\([^()]*\))([^.].*)$"),
        "Don't forget to call `.Execute()` on newly created net actions!",
        suggestion=r"\1.Execute()\2",
        level=Level.IMPORTANT,
    ),
    BadPattern(
        re.compile(
            r"(^\s*ActionManifestFileRelativeFilePath: StreamingAssets)/SteamVR/actions\.json(\s*)$"
        ),
        """Slashes were unnecessarily changed to forward slashes.
This happens on Linux systems automatically, but Windows systems will change this back.
We should just leave it as a backslash.""",
        suggestion=r"\1\SteamVR\actions.json\2",
        filenames=[r".*\.asset$"],
        level=Level.NOTE,
        see_only=False,
    ),
    BadPattern(
        re.compile(r"^\s*(\s|Object\.)Destroy\(.*$"),
        "Make sure to use `Destroyer.Destroy` (`Destroyer` class is in `SEE.Utils`) instead of `Object.Destroy`!",
        filenames=[r".*(?<!/Destroyer)\.cs$"],
        level=Level.WARNING,
    ),
    BadPattern(
        # For trailing whitespace
        re.compile(r"^(.*\S)?\s+$"),
        "Trailing whitespace detected! Please remove it.",
        level=Level.WARNING,
        suggestion=r"\1",
    ),
    BadPattern(
        re.compile(r"^\s*m_Loaders:(?! \[\])"),
        "You must not enable OpenXR on any PR that is planned to be merged, otherwise Linux builds will break!",
        filenames=[r".*\.asset$"],
        level=Level.IMPORTANT,
        see_only=False,
    ),
    BadPattern(
        re.compile(r"^.*/(?:/|\*) (?:TODO|FIXME)(?!\s*\((?:#?\d{2,}|external.*)\))"),
        "Always associate a TODO/FIXME comment with an issue on GitHub, so that we can keep track of open tasks.\n"
        "Reference either [a new issue](https://github.com/uni-bremen-agst/SEE/issues/new) or an existing (open) issue "
        "by putting its number in parentheses after the TODO, e.g., `// TODO (#614): Fix linux builds`",
        level=Level.WARNING,
    ),
]


# *** MODIFY ABOVE TO ADD NEW BAD PATTERNS ***


def handle_added_line(line, filename, linenumber) -> int:
    """
    Handles a single added line within a diff hunk, checking it against
    any bad patterns, collecting comments for any matches it finds.
    """
    occurrences = 0
    for pattern in BAD_PATTERNS:
        if pattern.applies_to(filename, line):
            # We found a bad pattern.
            occurrences += 1
            # Try getting suggestion, if one exists.
            if pattern.suggestion:
                suggestion = pattern.regex.sub(pattern.suggestion, line)
            else:
                suggestion = None
            collected_matches.append(pattern.to_json(filename, linenumber, suggestion))
    return occurrences


def warn(message):
    """
    Prints a warning message to stderr.
    """
    print(f"::warning::{message}", file=sys.stderr)


def handle_missing_newline(
    filename: Optional[str], linenumber: int, last_line: Optional[str]
):
    """
    Handles a missing newline at the end of a file.
    :param filename: The name of the file.
    :param linenumber: The line number of the last line in the file.
    :param last_line: The last line in the file.
    """
    assert filename is not None
    if NO_NEWLINE_BAD_PATTERN.applies_to(filename):
        collected_matches.append(
            NO_NEWLINE_BAD_PATTERN.to_json(
                filename,
                linenumber,
                f"{last_line[1:] if last_line is not None else ''}\n",
            )
        )


def main():
    occurrences = 0
    filename = None
    diff_line = 0  # Current line number within a diff hunk.
    last_line = None  # Last line read.
    skip_file = False
    hunk_indicator = re.compile(r"^@@ -\d+(?:,\d+)? \+(\d+)(?:,(\d+))? @@.*$")
    missing_newline_at_eof = False
    sys.stdin.reconfigure(errors="ignore")
    for line in sys.stdin:
        line = line.rstrip("\r\n")
        if line.startswith("+++"):
            # New file here.
            if missing_newline_at_eof:
                handle_missing_newline(filename, diff_line - 1, last_line)
                missing_newline_at_eof = False
            split = line.split(" ", 1)
            if len(split) != 2:
                warn(f"Invalid unified diff file indicator: {line}")
                # Nonetheless, this is not a fatal error, so we can continue.
                continue
            filename = split[1]
            if split[1].startswith("b/"):
                # We need to remove the 'b/' prefix from the filename.
                filename = filename[2:]
            skip_file = filename == "dev/null"
        elif skip_file:
            continue
        elif line.startswith("@@"):
            # New diff hunk here.

            if missing_newline_at_eof:
                handle_missing_newline(filename, diff_line, last_line)
                missing_newline_at_eof = False

            m = hunk_indicator.match(line)
            if not m:
                warn(f"Invalid unified diff hunk in {filename}: {line}")
                # Nonetheless, this is not a fatal error, so we can continue.
                continue
            # Next line is starting line indicated by this line range
            diff_line = int(m.group(1))
        elif line.startswith("+"):
            # This is an actual added line within the hunk denoted by start_line.
            assert filename is not None
            # We skip the leading "+" character.
            occurrences += handle_added_line(line[1:], filename, diff_line)
            diff_line += 1
            last_line = line
            # We need to reset this flag. There were still added lines after the missing newline warning,
            # so it applied to the version of the file before it was changed and can be ignored.
            missing_newline_at_eof = False
        elif line.startswith(" "):
            # Lines starting with ' ' are just for context.
            diff_line += 1
            last_line = line
            missing_newline_at_eof = False
        # \ no newline at end of file
        elif line.startswith("\\ No newline at end of file"):
            # We can't report this immediately, as this string may occur twice.
            # Instead, we will report this once the next file / hunk starts.
            missing_newline_at_eof = True
        elif line != "" and line[0] not in ("-", "d", "i", "n", "o", "r", "B", "s"):
            # We ignore empty lines, removed lines, and diff metadata lines (starting with "diff" or "index" etc).
            warn(
                f'Unrecognized unified diff line indicator for line "{line}", skipping.'
            )

    if missing_newline_at_eof:
        handle_missing_newline(filename, diff_line, last_line)

    print(json.dumps(collected_matches))
    sys.exit(min(occurrences, 255))


if __name__ == "__main__":
    main()
