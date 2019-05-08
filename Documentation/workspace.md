# Framework Workspace

A workspace is considered to be the folder that holds all the information loaded in the framework.  
This includes formatted source code, configuration files and prepared measurement data.  


## Layout

The workspace must look like the following.

Name/Pattern | Type | Purpose
---- | ---- | ----
src (adjustable) | Folder | Contains the software system's source code (e.g. ".rt" files)
app_config.json | File | General framework configuration
variability_model.xml | File | Provides the feature model
edges_*.json | Files | Define edges for control flow
mappings_*.json | Files | Define settings for visualization
regions_*.json | Files | Define regions of code and the respective performance-influence models

How the format of the files must look like is described by other files in the "file-specs" folder.  
E.g. mappings use the format describes in "file-specs/value-mapping.md".  


To use a specific workspace, the according path must be set for the framework in Unity.  
More details on this will follow.
