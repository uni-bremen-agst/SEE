"""
    Helper functions for modules run as part of the scripted architecture and modules
    ran later.

    IMPORTANT NOTE: This module is shared by the Python modules executed in the pipeline
    of the configuration item Analysis/Architecture/Architecture-ScriptedArchitecture 1/architecture_files
    (where it must be placed at the first position) and Python modules running at later
     stages of the Axivion CI.
"""
from typing import List

from bauhaus.rfg import *
from bauhaus.rfg.hierarchies import *


def name(node: Node) -> str:
    """Returns the name of the node."""
    return node["Source.Name"]


def linkname(node: Node) -> str:
    """Returns the linkage name of the node."""
    return node["Linkage.Name"]


def fullname(view: View, node: Node) -> str:
    """
        Returns a fully qualified name for the node.
        This name consists of the complete list of ancestors
        of the node in the node hierarchy given in view
        where a period is used as a separator.
    """
    parent_node = parent(view, node)
    if parent_node is None:
        return name(node)
    else:
        return fullname(view, parent_node) + "." + name(node)
