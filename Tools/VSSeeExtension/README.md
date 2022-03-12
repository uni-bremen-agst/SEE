# VsSeeExtension
This Visual Studio Extension for SEE (Software Engineering Experience) integrates SEE and Visual Studio.
It allows you to select declarations in Visual Studio and highlight the corresponding visual representation
in SEE. Vice versa, SEE will be enabled to open a code window in Visual Studio when a SEE user selects
a node.

## Supported Versions
* Visual Studio 2019: supported
* Visual Studio 2022: experimental

## Install instructions

### Prerequisite
You must have installed the Visual Studio extension for VS extension development (Visual Studio SDK).

### Build and install
1. Open the solution file (./VSSeeExtension.sln)
2. Build the solution (release configuration).  
3. Close all open instances of Visual Studio.
4. Execute the generated .vsix file and follow the instructions. Files are located at:
    * Visual Studio 2019: ./VSSeeExtension.VS2019/bin/Release/VSSeeExtension.VS2019.vsix
    * Visual Studio 2022: ./VSSeeExtension.VS2022/bin/Release/VSSeeExtension.VS2022.vsix
5. Installation process is completed.

## Configuration instructions

### Configuration in Visual Studio

The configuration in Visual Studio can be done via the options menu (**Tools > Options...**) 
under the category **SEE Integration**. Here you can specify the TCP port where Visual Studio 
and SEE communicate over. Both Visual Studio and SEE must run on the same machine and they
must agree on the same TCP port.

### Configuration in SEE

#### Configuration of _IDEIntegration_
The scene SEEWorld must have a game object with a component of type _IDEIntegration_.
Only one such game object may exist in a scene.
Normally, that component can be found attached to the game object named _Scene Settings_.
Here you specify the following configuration options:

- The Visual Studio version to connect to.
- The maximal number of IDE clients allowed to connect to SEE.
- The TCP port (see above: this port number must be the same as specified in Visual Studio).
- If _Connect To Any_ is selected, an IDE can conntect that currently has a solution file not known to SEE.

In addition, you need to specify how Visual Studio and SEE match their entities (nodes in SEE and
declarations in source code in Visual Studio). If you ask Visual Studio to highlight an entity 
in SEE, it sends the following information about an entity to SEE:

- absolute path of the source file declaring the entity
- name of the entity
- source line of the declaration of the entity
- column in the source line of the declaration of the entity
- length of the source-code range declaring the entity in lines of code: more specifically, if E is the last line of the declaration of the entity, e.g., the line containing the closing bracket of a method declaration, and F is its first line, then length is E-F+1

The source-file path and the name of the entity is always used to identify the corresponding node in SEE.
Which of the other conveyed information is additionally used by SEE is determined by the following two options (they are not mutually exclusive):

- If _Use Element Position_ is checked, the source line and column will be used, too.
- If _Use Element Range_ is checked, the source line and length of the code range will be used, too.

#### Configuration of each code city

For every code city, the path to the Visual Studio solution file of the project and the path
where the source files of the project are located need to be configured in the 
Unity Inspector. The solution file is needed to open Visual studio.
The project path is required to show the source file within SEE and
also needed for SEE to locate nodes requested by the IDE. 

In the matching of entities in Visual Studio and nodes in SEE, an absolute path of the source file
is used. If the source paths of nodes are relative, the project path specified in the code city
will be prepended to these paths.

## Usage

### SEE

Start SEE in play mode. When a connection to the IDE is established, a notification will be shown.
To show a piece of code in Visual Studio, turn on the "Show Code" mode in the menu. Then select
a node. The code viewer of SEE will show up. Press the share symbol in the right upper corner
of the code viewer, to show that code in Visual Studio.

Note: The integration with Visual Studio does not work with the Unity editor. SEE must be in play mode.

### Visual Studio

SEE must be running in play mode and must have established a connection to the IDE.

Select the name of a method or class declaration in Visual Studio and right-click the mouse. 
Select the entry _SEE_ in the mouse menu. Select any of the following menu entries:

- Highlight Class: the node representing the selected class will be highlighted in SEE; if the node is a method, its declaring class will be highlighted.
- Highlight Method: the node representing the selected method will be highlighted in SEE (this entry shows up only if the selected entity is a method).
- Highlight Method References: the incoming edges of the node representing the selected method will be highlighted in SEE (again, this entry shows up only if the selected entity is a method).

