"""
    Runs various graph analysis using networkx to learn more about the architecture
    and the mapping.
"""
import csv
import os
import sys
import re
import networkx as nx
from bauhaus import rfg
#from bauhaus.rfg import *

# This is to be able to write special characters to the Windows console.
sys.stdout.reconfigure(encoding='utf-8')

def read_graph_from_csv(filename: str) -> nx.MultiDiGraph:
    """
        Reads the CSV data from given filename and returns the corresponding
        networkx.graph. The following assumptions about the CSV apply:
        1) there is no header
        2) the first column is the source of an edge
        3) the second column is the target of an edge
        4) the third column is the type of edge
    """
    graph = nx.MultiDiGraph()

    # Safely open and read the file.
    with open(filename, mode='r', encoding='utf-8') as file:
        reader = csv.reader(file, delimiter=';')

        print(f"\n--- Reading data from {filename} ---")

        row_count = 0
        for row in reader:
            # print(row)
            source = row[0]
            target = row[1]
            edge_type = row[2]
            graph.add_node(source)
            graph.add_node(target)
            graph.add_edge(source, target, type=edge_type)
            row_count += 1

        print(f"\nSuccessfully read {row_count} rows of data.")

    return graph


# The view to be processed. Must exist in the input RFG if there is any.
VIEW: str = "Lifted Code Facts"


def read_graph_from_rfg(filename: str) -> nx.MultiDiGraph:
    """
        Reads an RFG from filename and yields the corresponding NetworkX graph.
    """
    result = nx.MultiDiGraph()
    graph = rfg.Graph(filename)
    if not graph.is_view_name(VIEW):
        print(f"Available views: {graph.view_names()}")
        raise ValueError(f"No view named {VIEW}")
    view = graph.view(VIEW)
    no_nodes: int = 0
    for node in view.xnodes():
        #print(simple_name(node))
        result.add_node(simple_name(node))
        no_nodes = no_nodes + 1
    print(f"Read {no_nodes} nodes")
    no_edges: int = 0
    for edge in view.xedges():
        result.add_edge(simple_name(edge.source()), simple_name(edge.target()), type=edge.edge_type())
        no_edges = no_edges + 1
    print(f"Read {no_edges} edges")
    return result


def simple_name(node: rfg.Node) -> str:
    return extract_middle_fast(node["Linkage.Name"])


def extract_middle_fast(text: str) -> str:
    """
    Extracts the string between a prefix ending in ':' and a suffix starting with '@'.
    Example: 'A:my_target_data@domain.com' -> 'my_target_data'
    """
    if not text or ':' not in text or '@' not in text:
        return ""

    # split(':', 1)[1] grabs everything after the first colon
    # split('@', 1)[0] grabs everything before the first '@' in the remaining string
    return text.split(':', 1)[1].split('@', 1)[0]


def print_tree(tree, current_node=None, regex: str = "", level=0):
    """
    Recursively prints a NetworkX DiGraph representing a tree through nesting.
    """
    # Find the root if we're at the very beginning of the recursive loop
    at_root: bool = False
    if current_node is None:
        # A tree's root is the only node with an in-degree of 0
        roots = [n for n, in_degree in tree.in_degree() if in_degree == 0]
        if not roots:
            print("Error: No root found. Are you sure this is a valid tree?")
            return
        current_node = roots[0]
        at_root = True

    if not at_root and regex != "":
        if not re.search(regex, current_node):
            return

    # Format and print the current node
    indent = "    " * level
    # Add a little visual marker for children to make it look nicer
    marker = "└── " if level > 0 else ""
    print(f"{indent}{marker}{current_node}")

    # Find all successors (children) and recurse
    for child in tree.successors(current_node):
        print_tree(tree, child, regex, level + 1)


def get_roots(graph: nx.MultiDiGraph) -> list:
    """
    Finds all nodes in a directed graph that do not have any predecessor.

    Args:
        graph: The directed graph to analyze.

    Returns:
        list: A list of nodes with an in-degree of 0.
    """
    # graph.in_degree() returns an InDegreeView, which yields (node, in_degree) tuples
    return [node for node, degree in graph.in_degree if degree == 0]


def dominance_tree(graph: nx.MultiDiGraph) -> nx.DiGraph:
    """
        Returns a dominance tree for given graph.
        The edges of the resulting DiGraph have type 'dominates'
        and form a tree. The root is the artificial node "ROOT".
    """
    print("running dominance analysis")
    # Dominance analysis requires a unique root node.
    tree = nx.DiGraph()
    roots = get_roots(graph)
    if len(roots) == 1:
        root = roots[0]
    elif len(roots) > 1:
        root = "ROOT"
        graph.add_node(root)
        for node in roots:
            graph.add_edge(root, node, type="artificial_dep")
    else:
        print("Error: Graph is cyclic. No root found.")
        return tree
    dom_tree = nx.immediate_dominators(graph, root)
    for dominatee, dominator in dom_tree.items():
        tree.add_edge(dominator, dominatee, type="dominates")
    return tree


def find_cycles(graph: nx.MultiDiGraph):
    """
        Prints all cycles in the given graph.
    """
    print("finding cycles")
    # each cycle is represented by a list of nodes along the cycle.
    cycles = list(nx.simple_cycles(graph))
    if len(cycles) > 1:
        for cycle in cycles:
            print(cycle)
    else:
        print("No cycles")


def remove_self_loops(graph: nx.MultiDiGraph):
    """
        Removes all self loops of the given graph. A self loop is an
        edge from a node to itself.
    """
    print("removing self loops")
    self_loops = list(nx.selfloop_edges(graph))
    graph.remove_edges_from(self_loops)


if __name__ == "__main__":
    # 1. Ensure the user provided exactly one argument (the filename)
    # sys.argv[0] is the script name itself, so we need exactly 2 items.
    if len(sys.argv) != 2:
        print("Usage: python graph_analysis.py <path_to_file.(csv|rfg)>")
        sys.exit(1)  # Exit the program with an error status

    # Grab the filename from the arguments
    filename: str = sys.argv[1]

    # Verify the file actually exists to prevent crashes.
    if not os.path.isfile(filename):
        print(f"Error: The file '{filename}' could not be found.")
        sys.exit(1)

    try:
        if filename.endswith(".csv"):
            g = read_graph_from_csv(filename)
        elif filename.endswith(".rfg"):
            g = read_graph_from_rfg(filename)
        else:
            raise ValueError(f"ERROR: unsupported file extension of {filename}")

        if g.number_of_nodes() == 0:
            print("Warning: The graph has no nodes")
            sys.exit(1)

        remove_self_loops(g)
        # find_cycles(g)
        t = dominance_tree(g)
        print_tree(t, regex=r'^SEE.')
        # nx.drawing.nx_pydot.write_dot(t, Path(csv_file).stem + ".dot")

    except Exception as e:
        print(f"ERROR: {e}")
        sys.exit(1)
