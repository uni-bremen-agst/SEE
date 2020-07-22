###############################################################################
# Reads a GXL file and creates a CSV file for it containing additional metrics.
###############################################################################

library(sets)
library(data.table) # install.packages("data.table")
library(hash) # install.packages("hash")
library(xml2)
library(purrr)
library(dplyr)
# For data.tree, see https://cran.r-project.org/web/packages/data.tree/vignettes/data.tree.html
library(data.tree) # install.packages("data.tree")
library(DiagrammeR) # install.packages("DiagrammeR") # to plot a tree

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
filename = "../SEE/CodeFacts"

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
# attributes the element.
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

edge.map = to.map(edges)
node.map = to.map(nodes)

edge.df = data.table::rbindlist(values(edge.map), fill=TRUE)

# add.parent = function(node.map, edge.df, hierarchical.edges = c("Enclosing")) {
#   apply(X = edge.df[edge.df$type %in% hierarchical.edges, ], MARGIN = 1, 
#         FUN = function(e) { from = e[["from"]]
#                             to = e[["to"]]
#                             node = node.map[[from]]
#                             node[["parent"]] = to
#                             node.map[[from]] = node
#                           })
#   node.map
# }

# Yields a mapping, parent, of GXL node ids onto GXL node ids 
# where parent(id) denotes the parent GXL node id of the given id.
# A mapping exists only if there is a hierarchical edge in edge.df that
# has the id as a source. The result would then be the target id of that edge.
get.parent = function(node.map, edge.df, hierarchical.edges = c("Enclosing")) {
  parent = hash()
  apply(X = edge.df[edge.df$type %in% hierarchical.edges, ], MARGIN = 1, 
        FUN = function(e) { from = e[["from"]]
                            to = e[["to"]]
                            parent[[from]] = to
        })
  parent
}

# node.map = add.parent(node.map, edge.df)

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
      print(paste(node.id, "is a root"))
      roots[[node.id]] = node
    }
  }
  roots
}

node.tree = to.node.tree(node.map, edge.df)
node.tree

# all attribute names of a node: node.tree[[1]]$fields
# attribute access: node.tree[[1]]$Linkage.Name
# all attributes in a subtree: node.tree[[1]]$Get("Metric.LOC")
# all attributes in a subtree for nodes fulfilling a constraint:
#   node.tree[[1]]$Get("Metric.LOC", filterFun = function(n) !is.null(n$Metric.LOC))
#   node.tree[[1]]$Get("Metric.LOC", filterFun = function(n) n$id == "N148630")
# tree traversal: node.tree[[1]]$Do(fun = function(n) print(n$id), traversal = "post-order")

tree.apply = function(node.list, fun) {
   for(node in node.list) {
     node$Do(fun = fun, traversal = "post-order")
   }
}

tree.sum = function(node, metric) {
  if (is.null(node$metric)) {
    # node does not have the given metric
    value = sum(sapply(node$children, tree.sum, metric=metric))
    print(paste(node$id), value)
    return (value)
  } else {
    return (0)
  }
}

get.ids = function(node.list) {
  sapply(node.list, FUN = function(n) n$id)
}

LOC = function(node, metric, level) {
  result <- node$metric
  
  if(length(result) == 0 && length(node$children) > 0) {
    result = sum(sapply(node$children, FUN=LOC, metric = metric, level=level+1))
  }
  if (is.null(result)) {
    result = 0
  }
  # , "children=", as.character(get.ids(node$children))
  if (level == 1) {
     print(paste("level=", level, "id=", node$id, metric, "=", result, "#children=", length(node$children)))
  }
  result
}

LOC(node.tree[[1]], "Metric.LOC", 0)

# tree.apply(node.tree, fun = function(n) print(n$id))
tree.apply(node.tree, fun = function(n) tree.sum(n, "Metric.LOC"))

node.df = data.table::rbindlist(values(node.map), fill=TRUE)

read.gxl = function(doc)
{
  if (has.clone.data)
  {
    # mapping:
    #  Linkage.Name            -> Linkname
    #  Metric.Number_of_Tokens -> Number_Of_Tokens
    #  Metric.LOC              -> LOC
    # Metric.Clone_Rate        -> CloneRate
    gxl = xml_find_all(doc, "/gxl/graph/node") %>% 
      map_df(function(x) {
        list(
          Node=xml_attr(x, "id"), #,
          Linkname=xml_find_first(x, ".//attr[@name='Linkage.Name']/string") %>%  xml_text(),
          Number_Of_Tokens=xml_find_first(x, ".//attr[@name='Metric.Number_of_Tokens']/int") %>% xml_text() %>% strtoi(),
          LOC=xml_find_first(x, ".//attr[@name='Metric.LOC']/int") %>% xml_text() %>% strtoi(),
          CloneRate=xml_find_first(x, ".//attr[@name='Metric.Clone_Rate']/float") %>% xml_text() %>% as.numeric()
        )
      })
  }
  else
  {
    #  Linkage.Name         -> Linkname
    #  Metric.Lines.LOC     -> LOC
    #  Metric.Lines.LOC     -> Number_Of_Tokens
    #  Metric.Lines.Comment -> CloneRate
    gxl = xml_find_all(doc, "/gxl/graph/node") %>% 
      map_df(function(x) {
        list(
          Node=xml_attr(x, "id"), #,
          Linkname=xml_find_first(x, ".//attr[@name='Linkage.Name']/string") %>%  xml_text(),
          Number_Of_Tokens=xml_find_first(x, ".//attr[@name='Metric.Lines.LOC']/int") %>% xml_text() %>% strtoi(),
          LOC=xml_find_first(x, ".//attr[@name='Metric.Lines.LOC']/int") %>% xml_text() %>% strtoi(),
          CloneRate=xml_find_first(x, ".//attr[@name='Metric.Lines.Comment']/int") %>% xml_text() %>% as.numeric()
        )
      })
  }
  gxl
}

gxl = read.gxl(gxlfile)

clone.statistics = function(gxl)
{
  cat("Number of tokens: mean=", mean(gxl$Number_Of_Tokens, na.rm = TRUE), "sd=", sd(gxl$Number_Of_Tokens, na.rm = TRUE))
  cat("Clone rate: mean=", mean(gxl$CloneRate, na.rm = TRUE), "sd=", sd(gxl$CloneRate, na.rm = TRUE))
  cat("LOC: mean=", mean(gxl$LOC, na.rm = TRUE), "sd=", sd(gxl$LOC, na.rm = TRUE))
}

clone.statistics(gxl)

# Yields a vector of randomized values having any desired correlation rho with Y.
# The optional X represents the regression function of the correlation. If X is 
# 1:length(y), a linear correlation is to be used. If X is omitted, the normal
# distribution is used.
# X any Y must have the same length.
#
# Taken from:
# https://stats.stackexchange.com/questions/15011/generate-a-random-variable-with-a-defined-correlation-to-an-existing-variables/313138#313138
complement <- function(y, rho, x) {
  stopifnot(length(x) == length(y))
  if (missing(x)) x <- rnorm(length(y)) # Optional: supply a default if `x` is not given
  y.perp <- residuals(lm(x ~ y))
  rho * sd(y.perp) * y + y.perp * sd(y) * sqrt(1 - rho^2)
}

# An example use of complement to experiment with.
try.complement = function() {
  y <- rnorm(50, sd=10)
  x <- 1:50 # Optional
  # draws six plots with six different values for rho ranging from -0.8 to 1.0
  rho <- seq(0, 1, length.out=6) * rep(c(-1,1), 3)
  X <- data.frame(z=as.vector(sapply(rho, function(rho) complement(y, rho, x))),
                  rho=ordered(rep(signif(rho, 2), each=length(y))),
                  y=rep(y, length(rho)))
  
  library(ggplot2)
  ggplot(X, aes(y,z, group=rho)) + 
    geom_smooth(method="lm", color="Black") + 
    geom_rug(sides="b") + 
    geom_point(aes(fill=rho), alpha=1/2, shape=21) +
    facet_wrap(~ rho, scales="free")
}

# metrics are randomly chosen from normal distribution using different scales
add.random.metrics = function(gxl)
{
  # all linkage names for the rows in gxl where all columns have values different from na
  Linkage.Name = gxl[complete.cases(gxl), ]$Linkname
  df = data.frame(Linkage.Name)
  df$Metric.Architecture_Violations = abs(rnorm(nrow(df), mean=4,     sd=4))
  df$Metric.Clone                   = abs(rnorm(nrow(df), mean=20,    sd=5))
  df$Metric.Dead_Code               = abs(rnorm(nrow(df), mean=8,     sd=2))
  df$Metric.Cycle                   = abs(rnorm(nrow(df), mean=0,     sd=4))
  df$Metric.Metric                  = abs(rnorm(nrow(df), mean=100,   sd=40))
  df$Metric.Style                   = abs(rnorm(nrow(df), mean=11100, sd=10000))
  df$Metric.Universal               = abs(rnorm(nrow(df), mean=0,     sd=0.1))
  df$Metric.Complexity              = abs(rnorm(nrow(df), mean=500,   sd=400))
  df
}

# normalized randomized values of y correlated by function x with
# correlation factor rho. The normalized values are limited to the 
# range [0, 1.0].
random.correlated = function(y, rho, x) {
  result = complement(y, rho, x)
  minimum = min(result)
  if (minimum < 0) {
    result = result + abs(minimum)
  }
  result / max(result)
}

# metrics are randomly chosen but linearly correlated to number of tokens
add.random.correlated.metrics = function(gxl)
{
  gxl.without.na = gxl[complete.cases(gxl), ]
  # all linkage names for the rows in gxl where all columns have values different from na
  Linkage.Name = gxl.without.na$Linkname
  df = data.frame(Linkage.Name)
  df$Number_Of_Tokens = gxl.without.na$Number_Of_Tokens
  
  x = 1:nrow(df) # we want a linear correlation
  df$Metric.Architecture_Violations = random.correlated(df$Number_Of_Tokens, 0.8, x)
  df$Metric.Clone                   = random.correlated(df$Number_Of_Tokens, 0.7, x)
  df$Metric.Dead_Code               = random.correlated(df$Number_Of_Tokens, 0.5, x)
  df$Metric.Cycle                   = random.correlated(df$Number_Of_Tokens, 0.9, x)
  df$Metric.Metric                  = random.correlated(df$Number_Of_Tokens, 0.6, x)
  df$Metric.Style                   = random.correlated(df$Number_Of_Tokens, 0.4, x)
  df$Metric.Universal               = random.correlated(df$Number_Of_Tokens, 0.85, x)
  df$Metric.Complexity              = random.correlated(df$Number_Of_Tokens, 0.3, x)
  df
}

#metrics = add.random.metrics(gxl)
metrics = add.random.correlated.metrics(gxl)

if (! has.clone.data)
{
  gxl.without.na = gxl[complete.cases(gxl), ]
  metrics$CloneRate = gxl.without.na$CloneRate
  metrics$LOC = gxl.without.na$LOC
}

#plot(metrics$Number_Of_Tokens, metrics$Metric.Architecture_Violations)

write.table(metrics, csvfile, quote=FALSE, sep=";", row.names=FALSE, col.names=TRUE, dec=".", fileEncoding = "UTF-8")

# Metric.Quality in range [0,1]
# Metric.McCabe_Complexity.sum
# Metric.Number_Of_Statements.sum
# Metric.Lines.Comment.sum
# Metric.Lines.LOC.sum