# Framework Changelog

This changelog was added after the pre-release version 1.0.0.  


# Version 1.1.0

For detailed information about the changes, pleaser refer to [changes_v110.txt](changes_v110.txt).

## Added
- SNode now holds a reference to the CodeFile (avoids additional calls to the StructureLoader class)
- **Code City visualization** implemented
- Code City hover UI
- Code City base plate (to lift and rotate it)
- Code City element texture generation to show NFP regions
- Showing/hiding graphs and city visualization in current UI terminal
- RegionSpawner: added events so that other components can register and receive notifications on region spawn
- **Code Window Content Overview** implemented (including content and regions)
- First part of "getting-started" to documentation
- Registered ApplicationLoader in "Script Execution Order"
- Added "isLeaf" to structure nodes (SNode class)
- StructureSpawnerV3 and V4 implemented
- Added nodes rotating towards their parent in directory cone trees
- **Generic code for improved cone tree layout** creation
- Added GenericNode to SNode and AFeature
- VariabilityModelSpawnerV4 using the new cone tree layout
- Added a new scene just for development
- Fallback camera: interaction with code city possible (click element to open file as usual)
- VR laser pointer: interaction with code city possible to spawn files as usual
- Scroll Wheel implementation with animation
- **FilenameSettings** added to color-code files according to their names (see ValueMappings in documentation)
- User can now switch between either NFP heightmap or content overview or disabled both
- Added check method for variability model validity and usage to VariabilityModelLoader class
- Added "readOnly" property to features/options (e.g. used by root node so that it can no longer be changed)
- Added a basic UI to change the value of numerical options/feature of the variability model using a slider
- Added VRVisHelper class with methods to get easier access to some information stored by the framework
- Controller: Laserpointer: Teleportation is now possible if the pointer is disabled

## Changed
- Refactoring of spawner system
  - Spawners can now be retrieved from the ApplicationLoader using their name
  - Only spawners that are registered in the ApplicationLoader component are usable
  - Spawners can have "sub-spawners" (e.g. FileSpawner has the EdgeSpawner and RegionsSpawner)
  - "Sub-spawners" can be retrieved through spawner classes using GetSpawner() and e.g. the FileSpawner.SpawnerList enum
  - Spawners can be set to be run on startup or not in the ApplicationLoader component
- SteamVR Update (from 2.2.0 to 2.3.2)
- Code Window: layout improvements for heightmap and feature region visualization
- Old StructureSpawner replaced with newest (V4)
- Moved I/O related methods from CodeCityV1 to Utility
- Applied new cone tree layout to variability model (using generic code base)
- Changed font of code windows to a monospace font "Source Code Pro Regular"
- Adjusted maximum "line number" text (left side of code window) to support up to 9999 lines
- CodeWindowMover automatically checks if Trigger & Teleport buttons needs to be locked on SelectNode call
- Terminal UI: notified by VariabilityModel after configuration validation to take care of adjusting feedback
- Terminal UI: notified by VariabilityModel after configuration changed to take care of adjusting feedback text
- Adjusted CodeCity and Structure visualizations to make use of FilenameSettings for color-coding
- Adjusted haptic feedback of structure node interaction
- Disabled lighting shadows in general
- Updated layermask behaviour of pointer and input module
- Variability model node interaction: pointer click now handled by VariabilityModelInteraction.cs
- Whitespaces of variability model feature/option names are now trimmed (also the outputString)
- Adjusted variability model node spacing
- Variabiliy Model nodes no longer blocking teleport trace

## Cleanup
- Removed old file and folder prefabs
- Cleanup of example scenes (brought them on a "stable" state)
- Removed old (and unused) base for laser pointer input module

## Fixed
- No terminal rotation velocity applied in "ExampleScene_Catena"
- NullPointerException when calling "WindowSpawnedCallback" function with no CodeFile
- Last line number not shown in Code Window
- Cone tree layout (folders did overlap in case they had only one child)
- Variability model node information shown in hover UI not always updating properly
  - each feature instance now allows to add an event listener called on value change
- Changing controller while pointing at folders/files does not change back color of marked folder/file
- Bug in "GetAllValues()" method of "Feature_Range.cs" where values greater than the maximum were added
- Numerical values now correctly provided by slider (min and max available)
- Hiding variabiltiy model hover UI when clicked on binary option/feature
- Terminal: Variability Model: Validation color not changed if terminal disabled and option state changes
- Hover windows still showing if laser pointer was disabled
- Horizontal scrollbar of code window is sometimes not adjusting the content
- Rare out of bounds exception when generating overview texture (when only a single line in file)
