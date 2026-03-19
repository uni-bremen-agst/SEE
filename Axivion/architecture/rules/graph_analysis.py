"""
    Runs various graph analysis using networkx to learn more about the architecture
    and the mapping.
"""
import csv
import os
import sys
import networkx as nx
from pathlib import Path


def read_graph_from_csv(filename: str) -> nx.MultiDiGraph:
    """
        Reads the CSV data from given filename and returns the corresponding
        networkx.graph. The following assumptions about the CSV apply:
        1) there is no header
        2) the first column is the source of an edge
        3) the second column is the target of an edge
        4) the third column is the type of an edge
    """
    # Verify the file actually exists to prevent crashes.
    if not os.path.isfile(filename):
        print(f"Error: The file '{filename}' could not be found.")
        sys.exit(1)

    graph = nx.MultiDiGraph()

    # Safely open and read the file.
    try:
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

    except Exception as e:
        print(f"An unexpected error occurred: {e}")
        sys.exit(1)

    return graph


def print_tree(tree, current_node=None, level=0):
    """
    Recursively prints a NetworkX DiGraph representing a tree through nesting.
    """
    # Find the root if we're at the very beginning of the recursive loop
    if current_node is None:
        # A tree's root is the only node with an in-degree of 0
        roots = [n for n, in_degree in tree.in_degree() if in_degree == 0]
        if not roots:
            print("Error: No root found. Are you sure this is a valid tree?")
            return
        current_node = roots[0]

    # Format and print the current node
    indent = "    " * level
    # Add a little visual marker for children to make it look nicer
    marker = "└── " if level > 0 else ""
    print(f"{indent}{marker}{current_node}")

    # Find all successors (children) and recurse
    for child in tree.successors(current_node):
        print_tree(tree, child, level + 1)


def get_roots(graph: nx.MultiDiGraph) -> list:
    """
    Finds all nodes in a directed graph that do not have any predecessor.

    Args:
        graph: The directed graph to analyze.

    Returns:
        list: A list of nodes with an in-degree of 0.
    """
    # graph.in_degree() returns an InDegreeView, which yields (node, in_degree) tuples
    return [node for node, degree in graph.in_degree() if degree == 0]


def dominance_tree(graph: nx.MultiDiGraph) -> nx.DiGraph:
    """
        Returns a dominance tree for given graph.
        The edges of the resulting DiGraph have type 'dominates'
        and form a tree. The root is the artificial node "ROOT".
    """
    # Dominance analysis requires a unique root node.
    tree = nx.DiGraph()
    roots = get_roots(graph)
    if (len(roots)) > 1:
        graph.add_node("ROOT")
        for root in roots:
            graph.add_edge("ROOT", root, type="artifical_dep")
        dom_tree = nx.immediate_dominators(graph, "ROOT")
        for dominatee, dominator in dom_tree.items():
            tree.add_edge(dominator, dominatee, type="dominates")
    return tree


if __name__ == "__main__":
    # 1. Ensure the user provided exactly one argument (the filename)
    # sys.argv[0] is the script name itself, so we need exactly 2 items.
    if len(sys.argv) != 2:
        print("Usage: python graph_analysis.py <path_to_file.csv>")
        sys.exit(1)  # Exit the program with an error status

    # Grab the filename from the arguments
    csv_file = sys.argv[1]
    g = read_graph_from_csv(csv_file)
    t = dominance_tree(g)
    print_tree(t)
    nx.drawing.nx_pydot.write_dot(t, Path(csv_file).stem + ".dot")