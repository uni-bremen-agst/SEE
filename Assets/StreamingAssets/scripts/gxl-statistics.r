###############################################################################
# Reads a GXL file and creates a CSV file for it containing additional metrics.
###############################################################################

library(sets)  # install.packages("sets")
library(data.table) # install.packages("data.table")
library(hash) # install.packages("hash")
library(xml2)  # install.packages("xml2")
library(purrr)  # install.packages("purrr")
library(dplyr)  # install.packages("dplyr")
# For data.tree, see https://cran.r-project.org/web/packages/data.tree/vignettes/data.tree.html
library(data.tree) # install.packages("data.tree")
library(DiagrammeR) # install.packages("DiagrammeR") # to plot a tree
library(stringr)

# The GXL file basename excluding the file extension ".gxl". 
# Its path should be relative to the directory in which
# this R script is located.
#filename = "arch"
#filename = "net"
#filename = "fs"
#filename = "drivers"
#has.clone.data = TRUE

# the following files have no clone data
has.clone.data = FALSE
# the basename of the GXL file excluding its extension .gxl
#filename = "../SEE/CodeFacts"
filename = "../SEE/Architecture"

# The GXL file to be read; must exist.
gxlfile = paste(filename, ".gxl", sep="")
# The CSV file to be written for the metrics of the GXL file: will be created.
csvfile = paste(filename, ".csv", sep="")

# All GXL node/edge attribute value types.
gxl.attribute.value.types = c("string", "int", "float", "enum")

# doc is the XML DOM of the read GXL file. The XML node hierarchy corresponds
# to the nesting of the XML clauses in the GXL file. That is, we have:
#   gxl
#     - graph
#        - node *
#           - type
#              - xml attributes (here: the node type)
#           - attr *
#              - one of: string, enum, float, int
#              - xml attributes (here: the name of the attribute)
#           - xml attributes (here: the value for the node id)
#        - edge *
#           - type
#              - xml attributes (here: the node type)
#           - attr *
#              - one of: string, enum, float, int
#              - xml attributes (here: the name of the attribute)
#           - xml attributes (here: the value for the node id)
doc <- read_xml(gxlfile)

# Yields all node XML clauses in the given XML document.
get.nodes = function(doc) {
  xml_find_all(doc, "/gxl/graph/node")
}

# Yields all edge XML clauses in the given XML document.
get.edges = function(doc) {
  xml_find_all(doc, "/gxl/graph/edge")
}

# Yields a hash map of GXL element ids onto those elements (nodes or edges)
# where each element is represented as a named list consisting of all 
# attributes of the element.
to.map = function(elements) {
  map = hash() # the resulting map
  for (gxl.element in elements) {
    # the element (node/edge) to be added to map
    element = list()
    # adding the XML clause attributes to the named list; a node has
    # has an attribute "id"; an edge has the attributes "id", "from", and "to"
    attrs = xml_attrs(gxl.element)
    for (attr.name in names(attrs)) {
      element[[attr.name]] = attrs[[attr.name]]
    }
    # the remaining information about the node/edge is represented as 
    # nested XLM nodes; one of them is tagged "type" (which is the node or edge
    # type) and all others tagged "attr" (which represent the other attributes)
    for (child in xml_children(gxl.element)) {
      tag = xml_name(child)
      if (tag == "type")
      {
        element.type = xml_attr(child, "href")
        element[["type"]] = element.type
      }
      else if (tag == "attr") {
        for (grandchild in xml_children(child)) {
          # an "attr" has a name, a type, and a value.
          attribute.name = xml_attr(child, "name")
          attribute.type = xml_name(grandchild)
          attribute.value = xml_text(grandchild, trim=TRUE)
          # the type of an attr can one of enum, string, int, or float
          if (attribute.type == "enum") {
            attribute.value = TRUE
          } else if (attribute.type == "string") {
            # nothing to be done; it is already a string
          } else if (attribute.type == "int") {
            attribute.value = strtoi(attribute.value)
          } else if (attribute.type == "float") {
            attribute.value = as.numeric(attribute.value)
          }
          else {
            print(paste("ERROR: attribute", attribute.name, 
                        "of UNKNOWN type", attribute.type, 
                        "with value", attribute.value))
          }
          element[[attribute.name]] = attribute.value
        }
      }
    }
    map[[element$id]] = element
  }
  map
}

# All node XML clauses in doc.
nodes = get.nodes(doc)
# All edge XML clauses in doc.
edges = get.edges(doc)

# Mapping of edge ID -> list-of-attributes of edge
edge.map = to.map(edges)
#  Mapping of node ID -> list-of-attributes of node
node.map = to.map(nodes)

# Returns all types of the GXL elements in the given mapping of 
# IDs onto GXL elements
get.types = function(element.map) {
  unique(sapply(values(element.map, simplify = FALSE), FUN=function(l) l$type))  
}

# All node types in the graph
node.types = get.types(node.map)

# Yields all attributes of given graph element whose attribute name starts
# with "Metric.", in other words, it yields the list of attribute names
# representing a metric.
get.metric.names.of.element = function(element) {
  n = names(element)
  n[str_detect(n, "^Metric.")]
}

# Yields the list of metric names for the values of the given element map.
get.metric.names = function(element.map) {
  unique(unlist(sapply(values(element.map), FUN=get.metric.names.of.element)))
}

# All node metrics in the graph. Could be NULL, if there are no metrics.
node.metrics = get.metric.names(node.map)

# Data frame for edges. The columns are: id, from (node id), to (node id), and
# all other edge attributes.
edge.df = data.table::rbindlist(values(edge.map, simplify = FALSE), fill=TRUE)

# Yields a mapping, parent, of GXL node ids onto GXL node ids 
# where parent(id) denotes the parent GXL node id of the given id.
# A mapping exists only if there is a hierarchical edge in edge.df that
# has the id as a source. The result would then be the target id of that edge.
get.parent = function(node.map, edge.df, hierarchical.edges = c("Enclosing", "Belongs_To", "Part_Of")) {
  parent = hash()
  apply(X = edge.df[edge.df$type %in% hierarchical.edges, ], MARGIN = 1, 
        FUN = function(e) { from = e[["from"]]
                            to = e[["to"]]
                            parent[[from]] = to
        })
  parent
}

# Yields a forest as a list of tree root nodes.
to.node.tree = function (node.map, edge.df) {
  # mapping of GXL node ids onto their parent GXL node id
  parents = get.parent(node.map, edge.df)
  # create all tree nodes
  tree.nodes = hash()
  for (node.id in keys(node.map)) {
    # the information about the current node
    node = node.map[[node.id]]
    # the node in the resulting tree
    tree.node = Node$new(node.id)
    # assign the attributes
    for (attr.name in names(node)) {
      tree.node[[attr.name]] = node[[attr.name]]
      #cat(node.id, attr.name, tree.node[[attr.name]], "\n")
    }
    # add new node to the tree nodes
    tree.nodes[[node.id]] = tree.node
  }
  roots = list()
  # now that we have created all tree nodes, we set their parentship
  for (node.id in keys(tree.nodes)) {
    node = tree.nodes[[node.id]]
    parent.id = parents[[node.id]]
    if (! is.null(parent.id)) {
      parent = tree.nodes[[parent.id]]
      parent$AddChildNode(node)
    } else {
      #print(paste(node.id, "is a root"))
      roots[[node.id]] = node
    }
  }
  roots
}

# List of trees according to the node hierarchy.
node.tree = to.node.tree(node.map, edge.df)

# Returns the first node with the given name in node.tree or NULL if
# none exists.
find.node = function(name) {
  for (root in node.tree) {
    node = FindNode(root, name)
    if (!is.null(node)) {
      return (node)
    }
  }
  return (NULL)
}

# Sets and return the given metric for the given node. If the node has this 
# metric set already, its value is returned and nothing else happens.
# If the node does not have the metric and is a leaf, its value will be set to 
# the given default. If the node does not have the metric and is an inner node,
# its value be set to the result of the given aggregation function over 
# set.metrics for all its children.
set.metric = function(node, metric, aggregation=sum, default=0) {
  # cat(node$id, metric, "\n")
  if (!is.null(node[[metric]])) {
    # nothing to be done for the node itself: the metric exists;
    # but its descendants might not have it; hence, we need to traverse
    # to the children if the node is an inner node
    if (!node$isLeaf) {
      sapply(node$children, FUN=set.metric, metric=metric)
    }
  } else if (node$isLeaf) {
    # leaf node without metric => receives a default
    node[[metric]] = default
  } else {
    ## inner node without metric
    node[[metric]] = aggregation(sapply(node$children, FUN=set.metric, metric=metric))
  }
  return (node[[metric]])
}

# Metric name for number of transitive descendants.
number.of.descendants.metric = "Metric.Number_Of_Descendants"

# Sets the number of (transitive) descendants of the given node and all
# its descendants. The value is stored in number.of.descendants.metric
set.descendants = function(node) {
  if (node$isLeaf) {
    # leaf node
    node[[number.of.descendants.metric]] = 0
  } else {
    # inner node
    node[[number.of.descendants.metric]] = sum(sapply(node$children, FUN=set.descendants))
  }
  return (1 + node[[number.of.descendants.metric]])
}

# Returns the metric name for the number of descendants of the given node type.
number.of.type.metric.name = function(type) {
  paste("Metric.Number_Of_", type, "s", sep="")
}

# Sets the number of (transitive) descendants of a particular type for the given 
# node and all its descendants. The value is stored in the attribute 
# Metric.Number_Of_<type>s. The type of the node itself counts as well.
set.descendants.of.type = function(node, type) {
  metric = number.of.type.metric.name(type)
  if (node$isLeaf) {
    # leaf node
    node[[metric]] = 0
  } else {
    # inner node
    node[[metric]] = sum(sapply(node$children, FUN=set.descendants.of.type, type=type))
  }
  if (node$type == type) {
    node[[metric]] = node[[metric]] + 1  
  } 
  return (node[[metric]])
}

# Aggregate all node metrics in the trees and set the number of descendants
# of all nodes.
number.of.tree.nodes = 0
for (root in node.tree)
{
  for (metric in node.metrics) {
    set.metric(root, metric)
  }
  for (type in node.types) {
    set.descendants.of.type(root, type)
  }
  number.of.tree.nodes = number.of.tree.nodes + set.descendants(root)
}

# All node metrics in the graph: those that existed before and those that
# we added.
all.metrics = c(node.metrics, sapply(node.types, FUN = number.of.type.metric.name), list(number.of.descendants.metric))

# Turn the metrics of the tree nodes into a data frame.
# Note: The column identifying a node in the CSV file is named 'ID'.
df = setNames(data.frame(matrix(ncol = 1 + length(all.metrics), nrow = number.of.tree.nodes)), c("ID", all.metrics))
row.index = 0 # the row index in df where the next values will be added by add.metrics()
add.metrics = function(node) {
  row.index <<- row.index + 1 # row.index is a global variable
  col.index = 1
  df[row.index, col.index] <<- node[["Linkage.Name"]] # df is a global variable
  for (metric in all.metrics) {
    col.index = col.index + 1
    value = node[[metric]]
    if (is.null(value) || is.na(value)) {
      cat("ERROR: undefined metric value for", metric, "of node", node$id, "\n")
      # continue with a default
      value = 0
    }
    df[row.index, col.index] <<- value # df is a global variable
  }
}

# Add all metrics of the nodes of the tree to the data frame df.
for (root in node.tree) {
  root$Do(fun=add.metrics, traversal="pre-order")
}

# Write the data
write.table(x=df, file=csvfile, quote=FALSE, sep=";", row.names=FALSE, fileEncoding = "UTF8")
