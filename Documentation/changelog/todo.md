# ToDo List

## Major

(**#1**) = important

- [X] Advanced spawner system (register new in editor, use others, ...) *(12.06.2019)*
- [X] Basic **code city** visualization *(28.06.2019)*
- [ ] **#1** Improve code city visualization
  - [ ] **#1** Textures to show regions and their performance
  - [ ] Interaction
    - [X] **#1** Selecting elements to open files *(09.08.2019)*
    - [X] Info on hover *(04.07.2019)*
    - [ ] "Be the data" approach (maybe?)
    - [ ] **#2** Jump to according position in code when clicked on NFP texture
- [ ] External visualization settings (can be used for later loading of workspace)
- [ ] Code Window Content Overview
  - [X] Overview of code window content *(12.07.2019)*
  - [X] Overview of code regions *(18.07.2019)*
  - [ ] Where and how to show the content overview window?
- [ ] Overview of feature regions (already in code city texture considered?)
- [ ] Add task window from user study to let users load/make notes
- [ ] 2-Controller interaction
- [ ] **#3** New UI (more generic and not all windows opened at once)
  - [ ] Radial Menu (at users hand)
- [ ] **#3** Advanced accessibility and registration of loaders (like done with spawners)
- [ ] Variability Model improvements (non-boolean & mixed constraints)
- [ ] Text input (VR keyboard)
  - [ ] Search for files and highlighting in e.g. graph or code city
- [ ] Configuration of "mappings" while in application
- [ ] **#3** Storing configured workspace from within application
  - [ ] Store opened file positions
  - [ ] Store configuration of visualizations...
- [ ] Entry room on app startup (similar to SteamVR Home)
  - [ ] Advanced loading screen on startup
  - [ ] Loading workspace/settings from within application
  - [ ] Support of different controller types
  - [ ] Configuration of button layout through user
  - [ ] Use Unity's [Script Execution Order](https://docs.unity3d.com/Manual/class-MonoManager.html) feature
- [ ] Support of different HMDs
- [ ] Multi-user VR experience
- [X] **#2** Minimal cone tree layout *(05.08.2019)*

## Minor
- [X] Enable/Disable all visualizations in UI (includes graphs) *(10.07.2019)*
- [X] Improve overview texture generation by NOT pre-calculating line patterns *(17.07.2019)*
- [X] Nodes rotating towards parent node in cone tree layout *(05.08.2019)*
- [X] Generic code for cone tree layout used by software hierarchy visualization and variability model *(05.08.2019)*
  - [ ] Cleanup scenes accordingly after this modification!
- [X] Apply minimal cone tree layout algorithm to variability model as well *(06.08.2019)*
- [X] Better font for code in code window (e.g. Consolas - type "monospace" font) *(08.08.2019)*
- [X] **#1** Same color coding for cone tree edges and code city *(21.08.2019)*
- [ ] ValueMappings: provide some default color methods for usage in mappings
- [ ] ValueMappingsLoader: refactoring and generalization of setting types
- [ ] \(Sorting files in cone tree layout by their type to get kinda like a pie chart when color is applied\)?
- [ ] Put overview texture generation in a coroutine to minimize performance impact
- [ ] Improve code overview for the case that there are too many lines resulting in less than one pixel on the texture
- [ ] VariabilityModelSpawner: improve option index retrieval in last version (see comments in code)
- [ ] **#1** Add raycast to check that code city hover window is not inside building
- [ ] Add possibility to enable/disable code/region overview window
- [ ] Add possibility to show/hide code or regions or both in overview window?
- [ ] Tilt hover window of directory graph to user
- [ ] Hints on interaction when user selects a tool for the first time
- [ ] Hints on interaction while using the tools
- [ ] Feature graph: enable parent node if child node activated
  - [ ] Automated check/validation of alternate group selection
  - [ ] **#1** Selecting and configuring numerical options (requires UI concerns)
- [ ] Possibility to zoom on text for better readability
- [ ] Feature regions counter to show amount of "affected" lines
- [ ] Instant snap to start/end of file instead of "endless" scrolling
- [ ] Improve edge rendering to avoid thin edges when they curve
- [ ] Edges with lighting for better depth clues
- [X] **#3** Improved scrolling using a gesture *(16.08.2019)*
- [ ] **#3** Scroll wheel texture and rotation
- [ ] Improve pointing at files far away (e.g. searching inside a radius)
- [ ] Allow teleport when laser pointer is disabled
- [ ] Notation hints on feature graph (e.g. on hover)
- [ ] Information about edges (e.g. on hover)
  - [ ] Type and value/weight of edge
  - [ ] Classes to connect between
  - [ ] Line numbers to connect to
- [ ] Improve counting of edges
- [ ] Advanced positioning and rotating of code windows
- [ ] Code cleanup

## Bugs
- [X] NullPointerException when calling "WindowSpawnedCallback" function with no CodeFile *(12.07.2019)*
- [X] Last line number not shown in code file *(12.07.2019)*
- [ ] **#1** State of nodes of the variability model not always updating properly
- [ ] **#1** Overview region sometimes shown black
- [ ] Overview window scroll for code and regions is hacky and sometimes wobbles a bit
- [ ] Change controller while pointing at folders/files does not hide hover info window
- [ ] Laser pointer sometimes not working right after startup (rare)
- [ ] Horizontal scrollbar of code window is often not adjusting the content
