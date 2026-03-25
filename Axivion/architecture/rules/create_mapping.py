"""
    Creates the individual mappings.
"""

from typing import TYPE_CHECKING
# This is True in PyCharm, but False when you run the script
if TYPE_CHECKING:
    from setup_initial import *


MAPPING.add_mapping("SEE/UI/Extensions", ARCH.SEE)

def create_mapping(node: Node):
    """
       Creates a mapping of given implementation node onto its corresponding Component.
    """
    full_name = fullname(code_facts, node)
    print(f"mapping {full_name}")
    MAPPING.add_concrete_mapping(full_name, component_map[full_name])


# Map each namespace in code_facts onto the corresponding architecture component
# with the same name.
#for namespace in INPUT_RFG.nodes(code_facts, only_namespaces):
#    create_mapping(namespace)

# MAPPING.add_mapping("SEE.UI.Extensions", ARCH.SEE.UI.Extensions)
