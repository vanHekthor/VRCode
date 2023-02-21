# ToDo List

## Major

(**[X]**) = in progress

- [ ] Remove CodeCity Visualization
  - [ ] Other position for the influencing option
  - [ ] Other position for the hot-spot function
- [ ] Code-Window positioning
  - [ ] Dynamic directing code windows to user
  - [ ] Set direction of code windows when spawning
- [ ] Aggressive linking of active Windows

## Future Directions
- [ ] Multi-user VR experience
  - [ ] Opening & joining sessions from within the application (maybe in entry room)
  - [ ] Show an avatar for each user that only the other person can see
- [ ] Text input (VR keyboard)
- [ ] Support of different HMDs (e.g. Oculus Rift)
- [ ] Entry room on app startup (similar to SteamVR Home)
  - [ ] Advanced loading screen on startup
  - [ ] Loading workspace/settings from within application
  - [ ] Support of different controller types
  - [ ] Configuration of button layout through user
  - [ ] Use Unity's [Script Execution Order](https://docs.unity3d.com/Manual/class-MonoManager.html) feature
- [ ] Storing current VR Setup
  - [ ] Store opened file positions
  - [ ] Store configuration of visualizations

## Minor
- [ ] Performance Updates
  - [ ] Improve when to update specific parts of the code window (e.g. do not update everything if only feature regions changed)
  - [ ] Advanced accessibility and registration of loaders (like done with spawners)
- [ ] Configuring of Visualizations while in VR
- [ ] Upgrade Unity version and project + ensure everything still works fine
- [ ] Use more Unity Events to react on changes to settings
- [ ] For every change in visualizations (like show/hide) all NFP regions are always re-created (this can be improved!)
- [ ] Horizontal scrolling with new scroll-wheel implementation required?
- [ ] ValueMappings: provide some default color methods for usage in mappings (e.g. Fixed-Red, ...)
- [ ] ValueMappingsLoader: refactoring and generalization of setting types
- [ ] Put overview texture generation in a coroutine to minimize performance impact
- [ ] Improve code overview for the case that there are too many lines resulting in less than one pixel on the texture
- [ ] VariabilityModelSpawner: improve option index retrieval in last version (see comments in code)
- [ ] Add possibility to show/hide code or regions or both in overview window?
- [ ] Tilt hover window of directory graph to user
- [ ] Hints on interaction when user selects a tool for the first time
- [ ] Hints on interaction while using the tools
- [ ] Feature graph: enable parent node if child node activated
  - [ ] Automated check/validation of alternate group selection
- [ ] Possibility to zoom on text for better readability
- [ ] Feature regions counter to show amount of "affected" lines
- [ ] Instant snap to start/end of file instead of "endless" scrolling
- [ ] Improve edge rendering to avoid thin edges when they curve
- [ ] Edges with lighting for better depth clues
- [ ] Improve pointing at files far away (e.g. searching inside a radius)
- [ ] Notation hints on feature graph (e.g. on hover)
- [ ] Information about edges (e.g. on hover)
  - [ ] Type and value/weight of edge
  - [ ] Classes/files to connect between
  - [ ] Line numbers (in code) to connect to
- [ ] Improve counting of edges (add numbers or similar to show)
- [ ] Advanced positioning and rotating of code windows (snap to..., rotate with respect to...)
- [ ] Improve how CodeFile instances are retrieved (assigning an ID for every file and using this internally instead of strings)
- [ ] Code cleanup

## Bugs
- [ ] Overview window scroll for code and regions is hacky and sometimes wobbles a bit
- [ ] Laser pointer sometimes not working right after startup (very rare event that occurred during user study)
