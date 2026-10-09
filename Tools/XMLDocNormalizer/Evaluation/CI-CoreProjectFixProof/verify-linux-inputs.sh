#!/bin/sh
# Read-only proof against a Git-only archive extracted on a case-sensitive filesystem.
# Usage: sh verify-linux-inputs.sh <extracted-repository-root> <evaluated-input-list>
set -eu
if [ "$#" -ne 2 ]; then
    echo 'Expected extracted Git root and evaluated source/project path list.' >&2
    exit 2
fi
root=$1
inputs=$2
core="$root/Tools/XMLDocNormalizer/src/XMLDocNormalizer.ExceptionFlow.Core"
test -f "$core/XMLDocNormalizer.ExceptionFlow.Core.csproj"
if [ -f "$core/xmldocnormalizer.exceptionflow.core.csproj" ]; then
    echo 'Proof requires a case-sensitive filesystem.' >&2
    exit 1
fi
outputs=$(find "$root/Tools/XMLDocNormalizer" -type d \( -name bin -o -name obj \))
if [ -n "$outputs" ]; then
    echo 'Git-only export unexpectedly contains bin/obj.' >&2
    exit 1
fi
count=0
while IFS= read -r path || [ -n "$path" ]; do
    if [ ! -f "$root/$path" ]; then
        printf 'Missing or case-mismatched input: %s\n' "$path" >&2
        exit 1
    fi
    count=$((count + 1))
done < "$inputs"
printf 'PASS: %s tracked source/project inputs exist on case-sensitive Linux; no bin/obj.\n' "$count"
