"""
    Helper functions for modules run as part of the scripted architecture and modules
    ran later.

    IMPORTANT NOTE: This module is shared by the Python modules executed in the pipeline
    of the configuration item Analysis/Architecture/Architecture-ScriptedArchitecture 1/architecture_files
    (where it must be placed at the first position) and Python modules running at later
     stages of the Axivion CI.
"""

from bauhaus.rfg import *


def is_enclosing(edge: Edge) -> bool:
    """True if edge has type Enclosing or any of its subtypes."""
    return edge.is_of_subtype("Belongs_To")


def get_parent(view: View, node: Node) -> Node:
    """Yields the parent of node or None if node is a root."""
    parents = node.successors(view, is_enclosing)
    if len(parents) == 0:
        return None
    elif len(parents) == 1:
        return next(iter(parents))
    else:
        raise ValueError("node has multiple parents")


def fullname(view: View, node: Node) -> str:
    """
        Returns a fully qualified name for the node.
        This name consists of the complete list of ancestors
        of the node in the node hierarchy given in view
        where a period is used as a separator.
    """
    parent = get_parent(view, node)
    if parent is None:
        return node["Source.Name"]
    else:
        return fullname(view, parent) + "." + node["Source.Name"]
