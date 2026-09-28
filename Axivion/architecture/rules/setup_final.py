# At the end: create the actual views.

from typing import TYPE_CHECKING
# This is True in PyCharm, but False when you run the script
if TYPE_CHECKING:
    from setup_initial import *

ARCH.create_view(INPUT_RFG, "Architecture")
MAPPING.create_mapping_view("Mapping")
