"""
    Creates the individual mappings.
"""

from typing import TYPE_CHECKING
# This is True in PyCharm, but False when you run the script
if TYPE_CHECKING:
    from setup_initial import *


def create_mapping(node: Node):
    """
       Creates a mapping of given implementation node onto its corresponding Component.
    """
    MAPPING.add_concrete_mapping(node, component_map[node["Linkage.Name"]])


# Create the architecture model: turn each Namespace into a Component.
# Traverses the node hierarchy bottom up because children must be known
# when their parent is to be created.
for namespace in INPUT_RFG.nodes(code_facts, only_namespaces):
    create_mapping(namespace)

MAPPING.add_mapping(arch.SEE.UI.Extensions, arch.SEE.UI, is_private=True)
