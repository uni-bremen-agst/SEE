#!/usr/bin/env rfgscript

import axivion.config

# NOTE: If this import does not work, the first 'reduce` may need to be changed to '.reduce'.
from reduce import reduce

the_analysis = axivion.config.get_analysis()

# All node types that define the nodes to be kept
NODE_TYPES = ["Namespace"]

# All node names (Source.Name) that define the nodes to be kept
NODE_NAMES = ["SEE"]

# All view names to be reduced
VIEW_NAMES = ["Code Facts"]

# Name of the super type of all hierarchical edges
BELONGS_TO = "Belongs_To"

the_analysis.activate("Architecture-CustomRFGFunction")
the_analysis["Architecture-CustomRFGFunction"].function = reduce
