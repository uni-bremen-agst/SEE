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


#ARCH.SEE.depends_on(ARCH.)

# Sources for Engine_Ctrl.
# MAPPING.add_mapping("src/engine_ctrl", ARCH.Engine_Ctrl)

# Sources for Sensors.
# MAPPING.add_mapping("src/sensors", ARCH.Sensor)

# Sources for IO.
# MAPPING.add_mapping("src/io/file_system", ARCH.IO.File_System)
# MAPPING.add_mapping("src/io/display", ARCH.IO.Display)

# Sources for HW.
# Everything in the component is private.
# MAPPING.add_mapping("src/hw", ARCH.HW, is_private=True)
# Except public headers, like hw_public.h
# MAPPING.add_mapping("src/hw/error_code.h", ARCH.HW, is_private=False)
