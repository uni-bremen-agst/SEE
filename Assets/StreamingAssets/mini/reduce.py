#!/usr/bin/env rfgscript

# This script allows to reduce a view to all nodes in a subtree rooted by a given 
# node and all the edges between nodes of this subtree. Artifical nodes are 
# removed, too.

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
    """ 
      graph: the rfg to be reduced;
      view_name: the name of the view within graph that should be reduced;
      linkname: the Linkage.Name of the root node of the node tree to be kept.
      Returns the resulting modified graph.
    """
    if graph.is_edge_type_name(BELONGS_TO):
        belongs_to = graph.edge_type(BELONGS_TO)
        if graph.is_view_name(view_name):
            view = graph.view(view_name)
            reduce_view(view, linkname, belongs_to)
        else:
            print("View %s not found in RFG." % view_name)
    else:
        print("Edge type %s does not exist in RFG." % BELONGS_TO)
    return graph

def reduce_view(view: View, linkname: str, belongs_to: EdgeType):
    """
       view: the view to be reduced
       linkname: the unique Linkage.Name of the root node of the subtree to be kept
       belongs_to: the hierarchical edge type determining the nesting of the subtree
    """
    for node in view.xnodes():
        if node["Linkage.Name"] == linkname:
            descendants = gather_descendants(view, node, belongs_to)
            keep_only(view, descendants)
            break

def keep_only(view: View, nodes: NodeSet):
    """
        Deletes all nodes in view but the ones in 'nodes'. Artifical nodes
        are deleted no matter whether they are in 'nodes' or not.
        view: view where nodes not included in 'nodes' are to be deleted
        nodes: all nodes to be kept
    """
    deleted = 0
    for node in view.xnodes():
        if "Element.Is_Artificial" in node or node not in nodes:
            view.remove(node)
            deleted = deleted + 1
    print("Deleted %s nodes." % deleted)

def gather_descendants(view: View, root: Node, belongs_to: EdgeType):
    """ Yields the transitive closure of all nodes reachable from given 'root' node
    along belongs_to edges in backward direction. In other words, the 
    descendants of 'root' are returned including the root itself.
    """
    edge_predicate = misc.create_edge_predicate(view.graph, belongs_to)
    return misc.transitive_neighbors(root, view, edge_predicate, "backward")
    
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