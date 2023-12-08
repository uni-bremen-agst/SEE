#!/usr/bin/env rfgscript

#  Axivion Suite
#  https://www.axivion.com/
#  Copyright (C) Axivion GmbH, 2019

'''
Converts cpf's clone information (clone pairs and clone rates per file) in 
CSV format to an RFG. 

cpf creates two files relevant here (both in CSV format):
(1) clone pairs: pairs of cloned fragments identified by their source location

(2) statistics: statistics on the size of each file (lines of code,
    number of tokens) and its clone rate. The clone rate of a file is the
    number of cloned tokens divided by the total number of tokens of a
    file. A token is considered cloned if it is contained in a clone
    as reported in the clone-pair file of above (1).

These two files are expected to be created for the same source-code tree and 
are processed by this script as input.

The result RFG will have a single view 'Clones' containing a hierarchy
of directories and files as nodes. The nesting is represented by
Enclosing edges. All nodes will have a source name and a unique
linkname attribute. The files will have attributes for their size in
terms of lines of code (Metric.LOC) and number of tokens
(Metric.Number_of_Tokens), and their clone rate (Metric.Clone_Rate)
as imported from the statistics file.

The source name of a file is its basename and its unique linkname is
its complete path.

The source name of a directory is its basename and its unique linkname is
its complete path.

Edges will be created between two files, A and B, if they share 
a clone according to the clone-pair input file (A and B may be the
same file). There will be at most one edge among each pair of files.
If two files share more than one clone, which clone pair is represented
by an edge is random. That is, the clone edge attributes Clone.Number_Of_Tokens,
Clone.Type, and Clone.Length relate to only one of the clone pairs 
in between A and B. However, the edge attribute Clone.Multiplicity
counts the number of clone pairs in between A and B.
'''

try:
    import bauhaus

    del bauhaus
except ImportError:
    import subprocess
    import sys

    sys.exit(subprocess.call(['rfgscript'] + sys.argv))

import csv
import logging
import sys

from bauhaus import rfg

# --verbose switches the log level to INFO
# --debug switches it to DEBUG
FORMATTER = logging.Formatter('%(asctime)s - %(name)s - %(levelname)s - %(message)s')
CHANDLER = logging.StreamHandler()
logging.getLogger().setLevel(logging.WARNING)
logging.getLogger().addHandler(CHANDLER)
CHANDLER.setFormatter(FORMATTER)

# The metrics imported from the input CSV data attached to the nodes
METRIC_CLONE_RATE = 'Metric.Clone_Rate'
METRIC_TOKENS = 'Metric.Number_of_Tokens'
METRIC_LOC = 'Metric.LOC'

# Attributes for clone edges
START_LINE1 = 'Clone.Source.Start.Line'
START_COLUMN1 = 'Clone.Source.Start.Column'
END_LINE1 = 'Clone.Source.End.Line'
END_COLUMN1 = 'Clone.Source.End.Column'
START_LINE2 = 'Clone.Target.Start.Line'
START_COLUMN2 = 'Clone.Target.Start.Column'
END_LINE2 = 'Clone.Target.End.Line'
END_COLUMN2 = 'Clone.Target.End.Column'
CLONE_TYPE = 'Clone.Type'
SOURCE_SIZE = 'Clone.Source.Size'
TARGET_SIZE = 'Clone.Target.Size'
CLONE_LENGTH = 'Clone.Length'
CLONED_TOKENS = 'Clone.Number_Of_Tokens'
MULTIPLICITY = 'Clone.Multiplicity'

# edge type for clone relations
CLONE = 'Clone'


def only_clone_relations(edge):
    """Returns an edge matcher for clone relations.

    :param edge: the edge to be queried
    :returns: true iff edge is a clone relation
    """
    return edge.is_of_subtype(CLONE)


def insert_edge(graph, view, edge_type, source_node, target_node):
    """Creates an edge.

    Creates and inserts a new edge of given edge_type from source_node
    to target_node in given view of given graph.

    :param graph: RFG in which the edge is to be created
    :param view: RFG view the edge should be added to
    :param edge:_type type of the edge to be created
    :param source:_node source node of the edge
    :param target:_node target node of the edge
    :returns: the newly created edge
    """
    edge = graph.edge(edge_type)
    view.insert(edge, source=source_node, target=target_node)
    return edge


def create_node(graph, view, node_map, node_type, name, linkname):
    """Creates a node.

    Creates a new node of given node_type with given source name
    and unique linkname and inserts it into given view of given graph
    and into the given node_map.

    :param graph: RFG in which the node is to be created
    :param view: RFG view the node should be added to
    :param the: map of node to which the new node is to be added; linkname is
    used for the key
    :param name: source name of the new node
    :param linkname: the unique linkname of the new node
    :param node:_type type of the node to be created
    :returns: the new node created
    """
    node = graph.node(node_type)
    view.insert(node)
    node_map[linkname] = node
    node['Source.Name'] = name
    node['Linkage.Name'] = linkname
    return node


def create_directory_node(graph, view, node_map, name, linkname):
    """Creates directory node.

    Creates and returns a new directory node. This function is equivalent to
    create_node(graph, view, node_map, "Directory", name, linkname).

    :param graph: RFG in which the node is to be created
    :param view: RFG view the node should be added to
    :param the: map of node to which the new node is to be added; linkname is
    used for the key
    :param name: source name of the new node
    :param linkname: the unique linkname of the new node
    :returns: the new directory node created
    """
    return create_node(graph, view, node_map, "Directory", name, linkname)


def create_file_node(
    graph, view, node_map, filename, directory_path, linkname, tokens, clone_rate, sloc
):
    """Creates file node.

    Creates and returns a new file node. This function is equivalent to
    create_node(graph, view, node_map, "File", name, linkname).

    :param graph: RFG in which the node is to be created
    :param view: RFG view the node should be added to
    :param the: map of node to which the new node is to be added; linkname is
    used for the key
    :param filename: source name of the new node
    :param directory:_path array of strings representing the directory the file is
     contained in
    :param linkname: the unique linkname of the new node
    :returns: the new file node created
    """
    node = create_node(graph, view, node_map, 'File', filename, linkname)
    node['Source.File'] = filename
    if len(directory_path) > 0:
        # The separator within Bauhaus paths is / and Bauhaus paths must end with /
        node['Source.Path'] = "/".join(directory_path) + "/"
    else:
        node['Source.Path'] = ""
    node[METRIC_CLONE_RATE] = clone_rate
    node[METRIC_TOKENS] = tokens
    node[METRIC_LOC] = sloc
    return node


def get_parent(graph, view, node_map, parents, path):
    """Returns parent of node.

    Returns the parent of a file-system entity (directory or file) identified
    by the given path. parents[path] is the RFG node representing path's parent
    if path is already contained in parents. If path is not yet contained in
    parents, a corresponding new directory node will be created and added
    to the given view and parents. The new directory node will become a child
    of its parent.

    :param graph: RFG in which the new directory node for the parent is to be
      created if there is no parent yet
    :param view: RFG view the new directory node for the parent is to be
      added if there is no parent yet
    :param parents: a map from paths (strings) onto RFG nodes where to look up
      the parent
    :param path: a list of strings; each string is a directory
    :returns: the parent of the given path; an RFG node
    Precondition: len(path) > 0.
    """
    linkname = "/".join(path)

    if linkname in parents:
        parent = parents[linkname]
    else:
        # parent does not exist yet; we need to create it
        name = path[0]
        parent = create_directory_node(graph, view, node_map, name, linkname)
        parents[linkname] = parent
        tail = path[1:]
        if (len(tail)) > 0:
            grand_parent = get_parent(graph, view, node_map, parents, tail)
            insert_edge(graph, view, "Enclosing", parent, grand_parent)

    return parent


def create_nodes(graph, view, clonestats_file):
    """Creates all nodes.

    Creates all nodes (files and directories) for all file-system entities
    in the given clone-statistics file.

    :param graph: RFG in which the nodes are to be created
    :param view: RFG view the nodes should be added to
    :param clonestats:_file name of the clone-statistics file in CSV format
    :returns: a map of nodes : linkname (string) -> RFG node for all added
     nodes
    """

    # expected columns of clonestats_file:
    # filename CLONED_TOKENS tokens clone_rate sloc

    node_map = {}
    parents = {}

    with open(clonestats_file, 'r', newline='', encoding='utf-8') as file:
        try:
            has_header = csv.Sniffer().has_header(file.readline())
        except Exception as e:
            print("cannot read " + str(clonestats_file))
            print("Exception (create_nodes): " + str(e))
            sys.exit(1)

        file.seek(0)  # Rewind.
        reader = csv.reader(file, delimiter=';', quotechar='"')
        if has_header:
            next(reader)  # Skip header row.
        for row in reader:
            # filename CLONED_TOKENS tokens clone_rate sloc
            path = row[0]
            tokens = int(row[2])
            clone_rate = float(row[3])
            sloc = int(row[4])
            (head, *tail) = reversed(
                path.split("/")
            )  # split by / and revert resulting list
            file = create_file_node(
                # we must revert tail again
                graph,
                view,
                node_map,
                head,
                tail[::-1],
                path,
                tokens,
                clone_rate,
                sloc,
            )
            if len(tail) > 0:
                parent = get_parent(graph, view, node_map, parents, tail)
                insert_edge(graph, view, "Enclosing", file, parent)
    return node_map


def in_between(view, source, target, edge_matcher):
    """Returns the edge in between two nodes.

    Returns the edge in between source and target (any direction) matching
    the given edge matcher in given view if such an edge exists; otherwise
    None is returned.

    :param view: view in which the edge must be contained in
    :param source: one node of the edge
    :param target: the other node of the edge
    :returns: the edge in between or None if none is found
    """
    nodes = rfg.NodeSet()
    nodes.add(source)
    nodes.add(target)
    for e in view.connectings(nodes, edge_matcher):
        return e
    return None


def create_clone_edge(
    graph,
    view,
    node_map,
    filename1,
    startLine1,
    startColumn1,
    endLine1,
    endColumn1,
    filename2,
    startLine2,
    startColumn2,
    endLine2,
    endColumn2,
    cloneType,
    cloneLength,
    cloneSize,
):
    """Creates a clone edge.

    Creates a new Clone edge from a source file to a target file
    in given graph and view if there is no such edge yet.
    If such an edge exists already, nothing will happen.

    The source file is identified by node_map[filename1] and the
    target file by node_map[filename2].

    :param graph: graph where to add the edge
    :param view: view where to add the edge
    :param node:_map a map of nodes : linkname (string) -> RFG node for all nodes
    :param filename1: filename of the source node of the edge
    :param startLine1: start line of the source node of the edge
    :param startColumn1: start column of the source node of the edge
    :param endLine1: end line of the source node of the edge
    :param endColumn1: end column of the source node of the edge
    :param filename2: filename of the target node of the edge
    :param startLine2: start line of the target node of the edge
    :param startColumn2: start column of the target node of the edge
    :param endLine2: end line of the target node of the edge
    :param endColumn2: end column of the target node of the edge
    :param cloneType: type of clone the edge represents
    :param cloneLength: the length of the clone in LOC
    :param cloneSize: the length of the clone in number of tokens
    """
    source = node_map[filename1]
    target = node_map[filename2]

    # an edge will be introduced only once between any pair of nodes
    edge = in_between(view, source, target, only_clone_relations)
    if edge is None:
        edge = insert_edge(graph, view, CLONE, source, target)
        edge[START_LINE1] = startLine1
        edge[START_COLUMN1] = startColumn1
        edge[END_LINE1] = endLine1
        edge[END_COLUMN1] = endColumn1
        edge[START_LINE2] = startLine2
        edge[START_COLUMN2] = startColumn2
        edge[END_LINE2] = endLine2
        edge[END_COLUMN2] = endColumn2
        edge[CLONE_TYPE] = cloneType
        edge[CLONE_LENGTH] = cloneLength
        edge[CLONED_TOKENS] = cloneSize
        edge[MULTIPLICITY] = 1
    else:
        edge[MULTIPLICITY] = edge[MULTIPLICITY] + 1


def create_edges(graph, view, node_map, clonepairs_file):
    """Creates all clone edges.

    Creates the clone edges for all clone pairs given in the clone-pair file
    whose filename is passed in clonepairs_file.

    :param graph: graph where to create the edges
    :param view: view in which to create the edges
    :param node:_map mapping of node linknames onto RFG nodes
    :param clonepairs:_file clone-pair files containing the clone pairs in CSV format
    """

    # expected columns of clonepairs_file:
    #     0         1          2       3         4          5         6        7            8
    # Filename1 StartLine1 EndLine1 Filename2 StartLine2 EndLine2 CloneType CloneLength CloneSize
    #     9            10          11          12
    # startcolumn1 endcolumn1 startcolumn2 endcolumn2 classid parameters parameter_overlap parameter_consistency
    # degree_of_valid_references fraction_of_repeated_parameters token_overlap diversity fragment_length volume
    # fraction_of_non_repetitive_tokens longest_repeat_length longest_repeat_subsequence_length longest_repeat_repetition
    with open(clonepairs_file, 'r', newline='', encoding='utf-8') as file:
        try:
            has_header = csv.Sniffer().has_header(file.readline())
        except Exception as e:
            print("cannot read " + str(clonepairs_file))
            print("Exception (create_edges): " + str(e))
            sys.exit(1)

        file.seek(0)  # Rewind.
        reader = csv.reader(file, delimiter=';', quotechar='"')
        if has_header:
            next(reader)  # Skip header row.
        for row in reader:
            filename1 = row[0]
            startLine1 = int(row[1])
            endLine1 = int(row[2])
            filename2 = row[3]
            startLine2 = int(row[4])
            endLine2 = int(row[5])
            cloneType = int(row[6])
            cloneLength = int(row[7])  # in terms of lines of code
            cloneSize = int(row[8])  # in terms of tokens
            startColumn1 = int(row[9])
            endColumn1 = int(row[10])
            startColumn2 = int(row[11])
            endColumn2 = int(row[12])
            create_clone_edge(
                graph,
                view,
                node_map,
                filename1,
                startLine1,
                startColumn1,
                endLine1,
                endColumn1,
                filename2,
                startLine2,
                startColumn2,
                endLine2,
                endColumn2,
                cloneType,
                cloneLength,
                cloneSize,
            )


def process(graph, view, clonepairs_file, clonestats_file):
    """Creates all nodes and edges.

    Creates the nodes and edges for given clone-pair file
    and clone-statistics file.

    :param graph: graph where to create the nodes and edges
    :param view: view in which to create the nodes and edges
    :param clonepairs:_file name of the file containing the clone pairs
    :param clonestats:_file name of the file containing the clone statistics
    """
    node_map = create_nodes(graph, view, clonestats_file)
    create_edges(graph, view, node_map, clonepairs_file)


def main(clonepairs_file, clonestats_file, targetrfg_file):
    """Main program.

    Main program setting things up, and creating the nodes and edges for
    given clone-pair file and clone-statistics file.

    :param clonepairs:_file name of the file containing the clone pairs
    :param clonestats:_file name of the file containing the clone statistics
    :param targetrfg:_file name of the output RFG file
    """

    try:
        graph = rfg.Graph()
    except Exception:
        print('Error while creating rfg: %s' % sys.exc_info()[0])
        sys.exit(1)

    # File node metrics
    graph.create_node_attribute(METRIC_CLONE_RATE, 'float')
    graph.create_node_attribute(METRIC_TOKENS, 'int')
    graph.create_node_attribute(METRIC_LOC, 'int')

    # Attributes of a clone edge
    graph.create_edge_attribute(START_LINE1, 'int')
    graph.create_edge_attribute(START_COLUMN1, 'int')
    graph.create_edge_attribute(END_LINE1, 'int')
    graph.create_edge_attribute(END_COLUMN1, 'int')
    graph.create_edge_attribute(START_LINE2, 'int')
    graph.create_edge_attribute(START_COLUMN2, 'int')
    graph.create_edge_attribute(END_LINE2, 'int')
    graph.create_edge_attribute(END_COLUMN2, 'int')
    graph.create_edge_attribute(CLONE_TYPE, 'int')
    graph.create_edge_attribute(SOURCE_SIZE, 'int')
    graph.create_edge_attribute(TARGET_SIZE, 'int')
    graph.create_edge_attribute(CLONE_LENGTH, 'int')
    graph.create_edge_attribute(CLONED_TOKENS, 'int')
    graph.create_edge_attribute(MULTIPLICITY, 'int')

    # Setup result view
    view = graph.view('Clones')

    # This call really does the import work.
    process(graph, view, clonepairs_file, clonestats_file)

    try:
        graph.save(targetrfg_file)
    except Exception:
        print('Error while saving target rfg: %s' % sys.exc_info()[0])
        sys.exit(1)


if __name__ == '__main__':
    if len(sys.argv) != 4:
        print('usage: %s <clonepairsCSV> <clonestatsCSV> <resultrfg>' % sys.argv[0])
        sys.exit(1)

    clonepairs = sys.argv[1]
    clonestats = sys.argv[2]
    targetrfg = sys.argv[3]

    main(clonepairs, clonestats, targetrfg)
