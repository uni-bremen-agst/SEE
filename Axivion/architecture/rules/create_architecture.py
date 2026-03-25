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


def create_component(node: Node, subcomponents: list[Component], map_to_component: dict) -> Component:
    """
        Creates and returns a Component with the given subcomponents
        and adds it to the component_map (where Linkage.Name is the key).
    """
    full_name = fullname(code_facts, node)
    component = Component(full_name, *subcomponents)
    print("created component", full_name)
    map_to_component[full_name] = component
    return component


def create_components(node: Node, map_to_component: dict) -> Component:
    """
        Creates Components for all descendants of node. Returns a Component for node.
        The resulting Component is added to the component_map.
    """
    subcomponents = []
    for sub_ns in sub_namespaces(node):
        subcomponents.append(create_components(sub_ns, map_to_component))
    return create_component(node, subcomponents, map_to_component)


# Create the architecture model: turn each Namespace into a Component.
# Traverses the node hierarchy bottom up because children must be known
# when their parent is to be created.
for namespace in INPUT_RFG.nodes(code_facts, only_namespaces):
    if get_parent(code_facts, namespace) is None:
        ARCH.register(create_components(namespace, component_map))

lexer = Component("SEE.Scanner.Antlr.Lexer")
component_map["SEE.Scanner.Antlr"].register(lexer)


# We allow all SEE code to depend on System.
#arch.SEE.depends_on(arch.System)

# Determined via dominance tree.
#arch.SEE.UI.HelpSystem.depends_on(arch.UnityEngine.Video)
#arch.SEE.Game.Drawable.depends_on(arch.UnityEngine.TextCore)
#arch.SEE.Game.Evolution.depends_on(arch.SEE.Net.Actions.Animation)
#arch.SEE.UI.RuntimeConfigMenu.depends_on(arch.SEE.Net.Actions.RuntimeConfig)
