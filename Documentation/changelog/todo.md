# To Do List

## Major

(**#1**) = important

- [X] Advanced spawner system (register new in editor, use others, ...) *(12.06.2019)*
- [X] Basic **code city** visualization *(28.06.2019)*
- [ ] **#1** Improve code city visualization
  - [ ] Textures to show regions and their performance
  - [ ] Interaction
    - [ ] Selecting elements
    - [X] Info on hover *(04.07.2019)*
    - [ ] "Be the data" approach (maybe?)
    - [ ] Open code window by clicking at the elements
      - [ ] Jump to according position in code when clicked on NFP texture
- [ ] External visualization settings (can be used for later loading of workspace)
- [ ] Code Window Content Overview
  - [X] **#1** Overview of code window content *(12.07.2019)*
  - [X] **#1** Overview of code regions *(18.07.2019)*
  - [ ] Where and how to show the content overview window?
- [ ] Overview of feature regions (already in code city texture considered?)
- [ ] Add task window from user study to let users load/make notes
- [ ] 2-Controller interaction
- [ ] **#3** New UI (more generic and not all windows opened at once)
  - [ ] Radial Menu (at users hand)
- [ ] **#1** Advanced accessibility and registration of loaders (like done with spawners)
- [ ] Text input (VR keyboard)
  - [ ] Search for files and highlighting in e.g. graph or code city
- [ ] Configuration of "mappings" while in application
- [ ] **#1** Storing configured frameworks from within application
  - [ ] Store opened file positions
  - [ ] Store configuration of visualizations...
- [ ] Entry room on app startup (similar to SteamVR Home)
  - [ ] Advanced loading screen on startup
  - [ ] Loading frameworks/settings from within application
  - [ ] Support of different controller types
  - [ ] Configuration of button layout through user
  - [ ] Use Unity's [Script Execution Order](https://docs.unity3d.com/Manual/class-MonoManager.html) feature
- [ ] Support of different HMDs
- [ ] Multi-user VR experience
- [X] **#2** Minimal cone tree layout *(05.08.2019)*

## Minor
- [X] Enable/Disable all visualizations in UI (includes graphs) *(10.07.2019)*
- [X] Improve overview texture generation by NOT pre-calculating line patterns *(17.07.2019)*
- [ ] Apply minimal cone tree layout algorithm to variability model as well
- [ ] Put overview texture generation in a curoutine to avoid performance issues
- [ ] Improve code overview for the case that there are too many lines resulting in less than one pixel on the texture
- [ ] Add raycast to check that code city hover window is not inside building
- [ ] Add possibility to enable/disable code/region overview window
- [ ] Add possibility to show/hide code or regions or both in overview window?
- [ ] Better font for code in code window (e.g. Consolas)
- [ ] Tilt hover window of directory graph to user
- [ ] Hints on interaction while using the tools
- [ ] Feature graph: enable parent node if child node activated
  - [ ] Automated check/validation of alternate group selection
  - [ ] Selecting and configuring numerical options (requires UI concerns)
- [ ] Possibility to zoom on text for better readability
- [ ] Feature regions counter to show amount
- [ ] Instant snap to start/end of file instead of "endless" scrolling
- [ ] Improve edge rendering to avoid thin edges
- [ ] Edges with lighting for better depth clues
- [ ] Improved scrolling using a gesture
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
- [ ] State of nodes of the variability model not always updating properly
- [ ] Laser pointer sometimes not working right after startup (rare)
- [ ] Overview window scroll for code and regions is hacky and sometimes wobbles a bit
- [ ] Overview region sometimes shown black
- [ ] Change controller while pointing at folders/files does not hide hover info window
