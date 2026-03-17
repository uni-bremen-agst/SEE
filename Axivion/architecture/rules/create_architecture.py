"""
    Creates the nodes of the architecture. There is one architecture
    node for each namespace. The resulting architecture-node hierarchy
    is isomorphic to the namespace nesting.
"""
from typing import TYPE_CHECKING

# This is True in PyCharm, but False when you run the script
if TYPE_CHECKING:
    from setup_initial import *
    from architecture_common import *


def create_component(node: Node, subcomponents: list[Component]) -> Component:
    """
        Creates and returns a Component with the given subcomponents
        and adds it to the component_map (where Linkage.Name is the key).
    """
    component = Component(node["Source.Name"], *subcomponents)
    component_map[node["Linkage.Name"]] = component
    return component


def create_components(node: Node) -> Component:
    """
        Creates Components for all descendants of node. Returns a Component for node.
        The resulting Component is added to the component_map.
    """
    subcomponents = []
    for sub_ns in sub_namespaces(node):
        subcomponents.append(create_components(sub_ns))
    return create_component(node, subcomponents)


# Create the architecture model: turn each Namespace into a Component.
# Traverses the node hierarchy bottom up because children must be known
# when their parent is to be created.
for namespace in INPUT_RFG.nodes(code_facts, only_namespaces):
    if get_parent(code_facts, namespace) is None:
        ARCH.register(create_components(namespace))


ARCH.SEE.depends_on(ARCH.System)


# Expected architecture dependencies
# ARCH.App.depends_on(ARCH.Engine_Ctrl)
# ARCH.App.depends_on(ARCH.IO)
# ARCH.App.depends_on(ARCH.Sensor)
# ARCH.IO.depends_on(ARCH.HW)
# ARCH.Sensor.depends_on(ARCH.HW)
# ARCH.Engine_Ctrl.depends_on(ARCH.HW)


# ARCH.register(Component("Engine_Ctrl"))
# display = Component("Display")
# file_system = Component("File_System")
# io_children = [display, file_system]
# ARCH.register(Component("IO", *io_children))
# ARCH.register(Component("HW"))
# ARCH.register(Component("Sensor"))
