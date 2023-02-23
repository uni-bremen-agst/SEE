#!/bin/sh

# This script generates an RFG for our SEE implementation.
#
# It is expected to be run within an Axivion Command Prompt in the root
# folder of SEE.
#
# Axivion Suite version 7.1.4 or higher must be installed.

set -eu

# path to resulting RFG (relative to SEE root directory).
RFG="Data/GXL/SEE/SEE.rfg"

# Compile C# code into IR
msbuild SEE.csproj /p:langversion=latest /p:AdditionalOptions="/B$(pwd)"

# Extract RFG from IR
ir2rfg Temp/Bin/Debug/SEE.dll.ir "$RFG"

# Reduce the graph to all components in SEE and only its immediate neighbors.
rfgscript Axivion/reduce.py --graph "$RFG" --view "Code Facts" --namespace SEE "$RFG"

# RFG can be visualized as follows:
# gravis Data/GXL/SEE/SEE.rfg
echo "RFG available under $RFG."
