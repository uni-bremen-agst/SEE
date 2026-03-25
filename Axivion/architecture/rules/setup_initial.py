# import the basic library for creating architectures and mappings
from bauhaus.architecture.scripted_architecture import *
from bauhaus.rfg import *

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
for root in get_roots(code_facts):
    if name(root) not in [".entry", "global"]:
        raise AssertionError(f"Unexpected root {linkname(root)} found")
    print("removing root", name(root))
    code_facts.remove(root)

# Most general edge type for node hierarchy.
# Enclosing and Part_Of are subtypes of it. Enclosing is used in the implementation
# node hierarchy, while Part_Of is used in the architecture node hierarchy.
# belongs_to_type = INPUT_RFG.edge_type("Belongs_To")


def get_children(node: Node) -> NodeSet:
    """Yields all direct children of node."""
    return node.predecessors(code_facts, is_enclosing)


def only_namespaces(node: Node) -> bool:
    """True if node has type Namespace."""
    return node.is_of_subtype(namespace_type)


def sub_namespaces(node: Node) -> NodeSet:
    """Yields all direct children of type Namespace."""
    return get_children(node).filter(only_namespaces)
