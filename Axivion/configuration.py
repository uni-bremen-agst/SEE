import axivion.config
from bauhaus.rfg import Graph, View, Node, EdgeType, NodeSet, EdgeSet, misc #, rfgtool

the_analysis = axivion.config.get_analysis()

# All node types that define the nodes to be kept
NODE_TYPES = ["Namespace"]
# All node names (Source.Name) that define the nodes to be kept
NODE_NAMES = ["SEE"]
# All view names to be reduced
VIEW_NAMES = ["Code Facts"]
# Name of the super type of all hierarchical edges
BELONGS_TO = "Belongs_To"

def reduce(graph: Graph):
    """ Reduces all views in VIEW_NAMES for given graph as follows:
    Let R (for roots) be the set of nodes in a view that have a Source.Name in NODE_NAMES
    and a node type in NODE_TYPES. Let descendants(root) be a function yielding
    all ancestor nodes of node root in the node hierarchy spanned by any edge type 
    of either BELONGS_TO or derived from BELONGS_TO; in other words, descendants(root)
    consists of the transitive closure of all nodes reachable from root along
    BELONGS_TO edges in backward direction. Similarly, function ancestors(n) yields
    the transitive closure of all nodes reachable from node n along BELONGS_TO edges in 
    forward direction.
    
    Then H (for hierarchy) is defined as the union of descendants(r) over all r in R. All 
    nodes in H will be kept in the view. In addition, we want to keep the nodes at the 
    "fringes" of H along with their hierarchy. Nodes in the fringes, F, of H are
    all nodes not in H having an incoming or outgoing non-hierarchical edge to any 
    node in H. All nodes in F including their ancestors (union of ancestors(f) for
    all f in F) are kept, too.
    
    In addition to the nodes kept as described above, we keep all edges between 
    nodes that are to be kept (non-hierarchical and hierarchical). All other nodes
    and edges will be removed from the view.
    """
    if graph.is_edge_type_name(BELONGS_TO):
        belongs_to = graph.edge_type(BELONGS_TO)
        for view_name in VIEW_NAMES:
            if graph.is_view_name(view_name):
                v = graph.view(view_name)
                reduce_view(v, belongs_to)
            else:
                print("View %s not found in RFG" % view_name)
    else:
        print("Edge type %s does not exist in RFG" % BELONGS_TO)

def reduce_view(v: View, belongs_to: EdgeType):
    """See also the documentation on function reduce. This function reduces
    the given view v as described there. The given edge type belongs_to
    defines the edge types forming the node hierarchy.
    """
    nodes_in_hierarchy = NodeSet()
    neighbors = NodeSet()
    # gather nodes to keep
    for n in v.xnodes():
        if n.node_type().name() in NODE_TYPES and n["Source.Name"] in NODE_NAMES:
            print("Found relevant node %s." % n["Source.Name"])
            gather_nodes(v, n, belongs_to, nodes_in_hierarchy, neighbors)
    fringes = neighbors - nodes_in_hierarchy
    ancestors = gather_ancestors(v, belongs_to, fringes)
    fringes = fringes + ancestors
    # We keep the nodes in the hierarchy and the fringes. Note that removing
    # a node will also remove its incoming and outgoing edges.
    nodes_to_keep = nodes_in_hierarchy + fringes    
    for n in v.xnodes():
        if n not in nodes_to_keep:
            v.remove(n)

def gather_ancestors(v: View, belongs_to: EdgeType, nodes: NodeSet):
    """ Yields the he transitive closure of all nodes reachable from any node
    contained in the given set of nodes along belongs_to edges in backward direction.
    In other words, the descendants are returned.
    """
    edge_predicate = misc.create_edge_predicate(v.graph, belongs_to)
    return misc.transitive_neighbors(nodes, v, edge_predicate, "forward")

def gather_nodes(v: View, n: Node, belongs_to: EdgeType, nodes_in_hierarchy: NodeSet, neighbors: NodeSet):
    """ Adds node n and all its descendants to nodes_in_hierarchy (the hierarchy
    is defined by the given edge type belongs_to). In addition, every neighbor of 
    n reachable by a non-hierarchical edge will be added to the given edge set neighbors.
    """
    nodes_in_hierarchy.add(n)
    for incoming in n.incomings(v):
        if incoming.is_of_subtype(belongs_to):
            # childhood
            gather_nodes(v, incoming.source(), belongs_to, nodes_in_hierarchy, neighbors)
        else:
            # incoming non-hierarchical edge => keep only the source of the edge
            nodes_in_hierarchy.add(incoming.source())
            neighbors.add(incoming.source())
    for outgoing in n.outgoings(v):
        neighbors.add(outgoing.target())
            
the_analysis.activate('Architecture-CustomRFGFunction')
the_analysis['Architecture-CustomRFGFunction'].function = reduce
