#!/usr/bin/env rfgscript

from bauhaus.shared import bauhaustool
from bauhaus.rfg import Graph, View, Node, EdgeType, NodeSet, EdgeSet, misc, rfgtool
from bauhaus.rfg.hierarchies import *

INPUTS = {
    'graph': {'doc': 'the input rfg',
              'type': 'rfg',
              'switches': ['--graph', '--rfg', '-r', '--input', '-i'],
              },
}
OUTPUT = 'rfg'

# All node types that define the nodes to be kept.
CONTAINER_NODE_TYPES = ["Type"]

# All view names to be reduced
VIEW_NAMES = ["Code Facts"]


def lift(graph: Graph) -> Graph:
    """
    Returns the resulting modified graph.
    """
    for view_name in VIEW_NAMES:
        if graph.is_view_name(view_name):
            v = graph.view(view_name)
            lift_view(v)
        else:
            print("View %s not found in RFG" % view_name)
    return graph


def lift_view(view: View):
    result = view.lift_edges_totally(view, "Lifted " + view.name())
    for node in result.xnodes(lambda n: not is_container(n)):
        result.remove(node)


def is_container(node: Node) -> bool:
    """
        True if the type of node is any of CONTAINER_NODE_TYPES or their subtypes.
    """
    for node_type in CONTAINER_NODE_TYPES:
        if node.is_of_subtype(node_type):
            return True
    return False


@rfgtool.with_rfg_types
@bauhaustool.BauhausTool(INPUTS, OUTPUT)
def perform(**kwargs):
    graph = kwargs['graph']
    lift(graph)
    return graph


if __name__ == '__main__':
    perform.execute_as_command_line_tool \
        (usage='%prog [options] <resultrfg>',
         description='Removes lower-level nodes and lifts their edges.')
