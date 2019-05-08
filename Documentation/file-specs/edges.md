# Edges


## Pattern

`edges_*.json`  


## General Structure

The general structure is an array with JSON objects inside.  
Each object represents one edge that uses the keys described later.  

```json
{
    "edges": [
        {

        }
    ]
}
```


## Keys

Sub-keys are denoted by ".".  
Relative paths are always relative to the root folder of the software project.  
This root folder is defined in the application config.  
An example could look like this: "src/main/java/Main.java".  

Key | Type | Info
---- | ---- | ----
type | string | edge type
label | string | edge label
from | JSON Object | start point of an edge
from.file | string | relative path of the file
from.lines | JSON Object | connection at file
from.lines.from | int | line number
from.lines.to | int | line number
to | JSON Object | end point of an edge
to.file | string | relative path of file
to.lines | JSON Object | connection at file
to.lines.from | int | line number
to.lines.to | int | line number
value | float | weight of an edge


## Example

```json
{
    "edges": [
        {
            "type": "import",
            "label": "Import GraphInterface",
            "from": {
                "file": "src/main/java/Catena.java",
                "lines": { "from": 7 }
            },
            "to": {
                "file": "src/main/java/components/graph/GraphInterface.java",
                "lines": { "from": 11 }
            },
            "value": 1
        }
    ]
}
```
