"""
    Creates the individual mappings.
"""

from typing import TYPE_CHECKING

# This is True in PyCharm, but False when you run the script
if TYPE_CHECKING:
    from setup_initial import *
    from bauhaus.rfg.hierarchies import *


def map_onto(full_name: str, target: Component):
    """
        Maps the node with the given full_name onto the target component.
    """
    names = full_name.split(".")
    node = get_node_by_path(code_facts, names)
    if node is None:
        print(f"No such node name {full_name}")
        return
    MAPPING.add_concrete_mapping(node, target)


def create_mapping(node: Node):
    """
       Creates a mapping of given implementation node onto its corresponding Component.
    """
    full_name = fullname(code_facts, node)
    map_onto(full_name, component_map[full_name])

# Map each namespace in code_facts onto the corresponding architecture component
# with the same name.
for namespace in INPUT_RFG.nodes(code_facts, only_namespaces):
    create_mapping(namespace)

#map_onto("SEE.UI.Extensions", ARCH.SEE.UI.Extensions)

