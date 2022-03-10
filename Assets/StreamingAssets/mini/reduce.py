#!/usr/bin/env rfgscript

from bauhaus.shared import bauhaustool
from bauhaus.rfg import Graph, View, Node, EdgeType, NodeSet, EdgeSet, misc, rfgtool

INPUTS = {
    'graph' :    { 'doc'     : 'the input rfg',
                   'type'    : 'rfg',
                   'switches'  : ['--graph', '--rfg'],
                 },
    'view' :     { 'doc'     : 'the view to be reduced',
                   'type'    : 'string',
                   'switches'  : ['--view'],
                 },
    'linkname' : { 'doc'    : 'the linkage name of the node to be kept',
                   'type'    : 'string',
                   'switches' : ['--linkname'],
                 },                
   }
OUTPUT = 'rfg'

# Name of the super type of all hierarchical edges
BELONGS_TO = "Belongs_To"

def reduce(graph: Graph, view_name: str, linkname: str):
    """ graph: the rfg to be reduced;
    view_name: the name of the view within graph that should be reduced;
    linkname: the Linkage.Name of the root node of the node tree to be kept.
    Returns the resulting modified graph.
    """
    if graph.is_edge_type_name(BELONGS_TO):
        belongs_to = graph.edge_type(BELONGS_TO)
        if graph.is_view_name(view_name):
            v = graph.view(view_name)
            reduce_view(v, linkname, belongs_to)
        else:
            print("View %s not found in RFG" % view_name)
    else:
        print("Edge type %s does not exist in RFG" % BELONGS_TO)
    return graph

def reduce_view(v: View, linkname: str, belongs_to: EdgeType):
    """
    """
    # gather nodes to keep
    for n in v.xnodes():
        if n["Linkage.Name"] == linkname:
            print("Found relevant node %s." % n["Source.Name"])
            descendants = gather_descendants(v, n, belongs_to)
            keep_only(v, descendants)
            break

def keep_only(v: View, nodes: NodeSet):
    deleted = 0
    for n in v.xnodes():
        if "Element.Is_Artificial" in n or n not in nodes:
            v.remove(n)
            deleted = deleted + 1
    print("Deleted %s nodes" % deleted)

def gather_descendants(v: View, root: Node, belongs_to: EdgeType):
    """ Yields the he transitive closure of all nodes reachable from any node
    contained in the given set of nodes along belongs_to edges in backward direction.
    In other words, the descendants are returned including the root itself.
    """
    edge_predicate = misc.create_edge_predicate(v.graph, belongs_to)
    return misc.transitive_neighbors(root, v, edge_predicate, "backward")
    
@rfgtool.with_rfg_types
@bauhaustool.BauhausTool(INPUTS, OUTPUT)
def perform(**kwargs):
    graph = kwargs['graph']
    view = kwargs['view']
    linkname = kwargs['linkname']
    reduce(graph, view, linkname)
    return graph

if __name__ == '__main__':
    perform.execute_as_command_line_tool\
        (usage='%prog [options] <resultrfg>',
         description = 'Reduces a view to a node with given linkage name and all its descendants (including their edges; excluding artificial nodes).')