# VsSeeExtension
Visual Studio Extension for SEE (Software Engineering Experience).

## Supported Versions
* Visual Studio 2019: supported
* Visual Studio 2022: experimental

## Install instructions
1. You must have installed the Visual Studio extension for VS extension development (Visual Studio SDK).
1. Build the solution (release configuration).
   Open Tools/VSSeeExtension/VSSeeExtension.Shared/VSSeeExtension.Shared.sln
   
2. Close all open instances of Visual Studio.
3. Execute the generated .vsix file and follow the instructions. Files are located at:
    * Visual Studio 2019: ./VSSeeExtension.VS2019/bin/Release/VSSeeExtension.VS2019.vsix
    * Visual Studio 2022: ./VSSeeExtension.VS2022/bin/Release/VSSeeExtension.VS2022.vsix
4. Installation process is completed.

## Configuration
Configuration can be done within the options menu (**Tools > Options...**) under the category **SEE Integration**.