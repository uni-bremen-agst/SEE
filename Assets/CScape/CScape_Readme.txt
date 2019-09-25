For a Manuals file (that will be constantly updated from now on) please go to this page: 
https://olivrproduction.wordpress.com/cscape-help/
IMPORTANT NOTICE:
IF YOU ARE UPDATING YOUR PROJECT WITH A NEW VERSION OF CSCAPE, YOUR EXISTING CITIES MATERIALS MIGHT BROKE. BE SURE TO BACKUP YOUR PROJECT BEFORE UPDATING PROJECT.
New CScape has many changes and it isn't tested for a compatibility with all possible cases of previous versions. IF you are updating already made city, after loading a scene, be sure to use Update City button. 
It should be able to reassign all materials. 

What is CScape? 
CScape is a ultra-optimized building/city generator system able to generate whole city by using only few materials - this means less draw calls and great performance.
Usage: 
Right Click into your hierarchy view, and choose CScape/Create MegaCity. This action will create a holder for your city. It will add a city prefab to your scene,
and this prefab holds main scripts for city generation. 
When you create your CScape city object, click on it and then on Generate City button. This button will create buildings.
You can try various Random seeds for this buildings, by manually inserting random seed number, or by clicking on randomize + or - button. 
(random seed assures replicability of street generation layouts - and it will be used in a future for runtime generation of a city).
-click on "generate streets"  - this will generate models for your streets. 
-click on "generate street details" -at this moment, this will generate lighting poles. In a future it could be used for generating other details as street signs and various lighting scenarios)
-click on "generate folliage" - this will generate folliage for streets that are wide enough (can be set inside randomizer settings layout). 
*To use this feature, please import Environment package from Unity Standard Assets (as this uses tree from those assets). 
Once you have created your city, you can easily modify separate buildings by clicking on them, and changing Building modifier values. There are lots of settings that can be modified (and there are more to come). 
If you hold CTRL button while editing a building, it will bring you a onscreen modificators 8at this moment there are few modificators, but a list of those will grow).
One good modificator is splitting a building in two. this will make two buildings out of one. 
also, all buildings have their scene modificators that can be used to controll the size of a building. 


Important suggestion on usage: 
This is a PBR asset, and it gives best results with Linear and HDR rendering. So, if you can, please switch unity to Linear rendering, and activate HDR rendering on your camera (and switch to Deferred rendering).
My suggestion is to use also image effects as Bloom, as they will give you a best possible result.
IMPORATANT NOTICE:
CScape is in it's alpha release - this means that enything done with it, in this Alpha version, may not be compatible with future versions. There are many features that have to come, and some logics will change. 



Thank you for supporting this alpha version. And feel free to give any suggestions!
Olivers Pavicevic
support mail: olix@iol.it
support forum: https://forum.unity3d.com/threads/cscape-advanced-building-generator-wip.460380/

