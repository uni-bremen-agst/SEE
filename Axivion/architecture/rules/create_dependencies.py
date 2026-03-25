"""
    A custom function to be used in the Axivion CI to export the result
    of the architecture check.
"""
import axivion.config
from architecture_common import *

the_analysis = axivion.config.get_analysis()

# The view containing the result of the reflexion analysis.
reflexion_result = "Architecture Check"


def emit_dependencies(graph: Graph) -> Graph:
    """
        Outputs all reflexion edges to two files, one of which
        has the Python depends_on rules to allow them, the other one has the source,
        target, and edge type in CSV format.
    """
    with open("dependencies.py", "w") as py_out, open("dependencies.csv", "w") as csv_out:
        convergence_type = graph.edge_type("Convergence")
        divergence_type = graph.edge_type("Divergence")
        reflexion_type = graph.edge_type("Architecture_Check_Result")
        v = graph.view(reflexion_result)
        for dep in v.xedges(lambda edge: edge.is_of_subtype(reflexion_type)):
            if dep.is_of_subtype(convergence_type) or dep.is_of_subtype(divergence_type):
                print(f"ARCH.{fullname(v, dep.source())}.depends_on({fullname(v, dep.target())})", file=py_out)
            print(f"{fullname(v, dep.source())};{fullname(v, dep.target())};{dep.edge_type().name()}", file=csv_out)
    return graph


the_analysis.activate('Architecture-CustomRFGFunction')
the_analysis['Architecture-CustomRFGFunction'].function = emit_dependencies
