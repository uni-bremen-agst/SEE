# VsSeeExtension
Visual Studio Extension for SEE (Software Engineering Experience).

## Supported Versions
* Visual Studio 2019: supported
* Visual Studio 2022: experimental

## Install instructions
### Prerequisite
1. You must have installed the Visual Studio extension for VS extension development (Visual Studio SDK).

### Build and install
1. Open the solution file (./VSSeeExtension.sln)
2. Build the solution (release configuration).  
3. Close all open instances of Visual Studio.
4. Execute the generated .vsix file and follow the instructions. Files are located at:
    * Visual Studio 2019: ./VSSeeExtension.VS2019/bin/Release/VSSeeExtension.VS2019.vsix
    * Visual Studio 2022: ./VSSeeExtension.VS2022/bin/Release/VSSeeExtension.VS2022.vsix
5. Installation process is completed.

## Configuration
Configuration can be done within the options menu (**Tools > Options...**) under the category **SEE Integration**.