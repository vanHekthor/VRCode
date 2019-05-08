# Framework Setup

## Required Software

The following software must be installed.  

Software | Tested Version
---- | ----
[SteamVR](https://store.steampowered.com/app/250820/SteamVR) | 1.3.22, 1.4.1
[Unity](https://unity3d.com/de/get-unity/download/archive) | 2018.2.10f1 with .NET 4.x enabled
[SteamVR Plugin](https://assetstore.unity.com/packages/tools/integration/steamvr-plugin-32647) | 2.2.0
[TextMeshPro Plugin](https://assetstore.unity.com/packages/essentials/beta-projects/textmesh-pro-84126) | 1.2.2

Newer versions were not tested.  
Therefore, we cannot guarantee that they work.  

## Preparation

After installing SteamVR and Unity, start Unity and install the two aforementioned plugins using the [Asset Store](https://docs.unity3d.com/Manual/AssetStore.html).  
Most important is, that you are able to use the components of the SteamVR Plugin (e.g. Window -> SteamVR Input).  
If this works, you can open the project of this repository with Unity.  
Make sure the scene "Assets/Scenes/ExampleScene_Catena.unity" or any of the scenes located in the "Assets/Scenes/tasks" folder is loaded that supports VR.  
Also ensure that SteamVR is up and running.  
Sometimes, you have to be logged into your Steam account for full functionality!  
You can try to run the application and see if basic things like the controllers work.  
In case they don't, exit the application, open "Window -> SteamVR Input" and open the "bindings menu".  
The default bindings must be loaded.  
Using it as a base, edit it and in case you see a warning for an unassigned button (Trackpad), add it accordingly under the default tab.  
Apply the binding, close the bindings menu and inside the "SteamVR Input" menu that you have opened previously, click at "Save and generate".  
Try running the application again.  

Further information with possible troubleshooting will be added here in a while.  


## No VR
(Limited functionality, for testing purposes only!)

Previous steps are still required (installing and preparing Unity).  
In case you do not have a VR setup available, open the scene under "Assets/Scenes/ExampleScene_Catena_NoVR.unity".  
Press the "Play" button and you should be able to move around using your keyboard and mouse.  
Keyboard "W, S, A and D" control the direction.  
Holding "SHIFT" pressed will let you fly faster.  
Move the mouse while holding the button pressed to look around.  
You can interact with basic things, spawn files and close them using the "Mouse Wheel Button".  
GUI elements can be used as well but the VR tools (controllers) are not available in this mode.  
