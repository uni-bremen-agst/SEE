# import the basic library for creating architectures and mappings
from bauhaus.architecture.scripted_architecture import *
from bauhaus.rfg import *
from bauhaus.rfg.hierarchies import *

from typing import TYPE_CHECKING
# This is True in PyCharm, but False when you run the script
if TYPE_CHECKING:
    from architecture_common import *

# the global variable INPUT_RFG is provided via scripted_architecture
# Create architecture and mapping abstractions
ARCH = Architecture("Architecture")

# Mapping of architecture components onto implementation components.
# First parameter is the dependency graph for the implementation.
# Second parameter is the graph view representing the node hierarchy of
# the dependency graph.
MAPPING = Mapping(INPUT_RFG, 'Code Facts')

# Code Facts view
code_facts = INPUT_RFG.view("Code Facts")

# Node type of namespaces
namespace_type = INPUT_RFG.node_type("Namespace")

# Mapping of the fully qualified name of a Namespace (Linkage.Name) onto
# its corresponding Component.
component_map = {}

# There is a root "global" and a root ".entry". We do not want them.
for root in roots(code_facts):
    if name(root) not in [".entry", "global"]:
        raise AssertionError(f"Unexpected root {linkname(root)} found")
    print("removing root", name(root))
    code_facts.remove(root)


def only_namespaces(node: Node) -> bool:
    """True if node has type Namespace."""
    return node.is_of_subtype(namespace_type)


def sub_namespaces(node: Node) -> NodeSet:
    """Yields all direct children of type Namespace."""
    return children(node, code_facts).filter(only_namespaces)
