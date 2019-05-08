# App_Config


## Pattern

`app_config.json`  


## General Structure

The general structure is a simple JSON object that uses the keys described later.  

```json
{

}
```


## Keys

Sub-keys are denoted by ".".  

Key | Type | Info
---- | ---- | ----
software_system | JSON Object | configuration of the loaded system
software_system.path | string | path where the system is located (if in same folder as this config, then use ".")
software_system.root_folde | string | name of root folder (e.g. "src") in the path
software_system.max_folder_depth | int | maximum folder depth for recursive search
software_system.ignore_files | string array | array of regex patterns to exclude files (e.g. ".*\\\\.html$")
software_system.remove_extensions | string array | array of file extensions that should be removed (e.g. ".rt")
features | string array | list of features that will be considered in this order

**Why "remove_extensions"?**  
As syntax highlighted files look like "Main.java.rt",
users would always see the ending ".rt" instead of ".java" in the name and path of the files.  
To avoid this, the ".rt" ending will be removed in their visual representation.  
This is also necessary to find the files according to their relative path (when defined in region files).  

E.g. a region is in "src/java/Main.java". Then this is what the region file includs as the path.  
The system on the other side, needs the highlighted files (ending at ".rt").  
To be able to still load the ".java" files, even if they do not exist,  
we have to ensure that we treat "Main.java.rt" as if it was "Main.java".  



## Example

```json

{
    "software_system": {
        "path": ".",
        "root_folder": "src",
        "max_folder_depth": 20,
        "ignore_files": [
            ".*\\.html$",
            ".*\\.java$"
        ],
        "remove_extensions": [
            ".rt"
        ]
    },
    "features": ["blake2b", "blake2b_1", "gamma", "dbg", "brg", "grg", "sbrg", "phi", "garlic", "lambda", "v_id", "d"],
}
```
