#!/usr/bin/env rfgscript

from bauhaus.shared import bauhaustool
from bauhaus.rfg import rfgtool

# file:///C:/Program%20Files%20(x86)/Bauhaus/doc/html/project_configuration/tool_reference/80_rfg_and_ir_operations_with_bauhaustool/index.html

INPUTS = {
    'graph' :          { 'doc'     : 'the input rfg',
                         'type'    : 'rfg',
                         'switches'  : ['--graph', '--rfg', '-r', '--input', '-i'],
                       },
    'view' :           {
                         'doc'     : 'name of a view in the graph',
                         'type'    : 'existing_view',
                         'graph'   : 'graph',
                         'switches': ['--view'],
                       },
    'namespace'      : { 'doc'     : 'namespace to be kept',
                         'type'    : 'string',
                       },
   }
OUTPUT = 'rfg'

@rfgtool.with_rfg_types
@bauhaustool.BauhausTool(INPUTS, OUTPUT)
def perform(**kwargs):
    graph     = kwargs['graph']
    namespace = kwargs['namespace']
    view =      kwargs['view']
    
    perform.warning\
        ('View "%s" has %d nodes.' % (view.name(), len(view.items())),
         perform,
         'NoSourceNameAttributeSet',
         False)
         
    return graph

if __name__ == '__main__':
    perform.execute_as_command_line_tool\
        (usage='%prog [options] <resultrfg>',
         description = 'Reduces the graph to SEE and its immediate neighbors.')