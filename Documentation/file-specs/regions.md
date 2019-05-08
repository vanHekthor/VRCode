# Regions


## Pattern

`regions_*.json`  


## General Structure

The general structure is an array with JSON objects inside.  
Each object represents one region that uses the keys described later.  

```json
{
    "regions": [
        {

        }
    ]
}
```


## Keys

Two types of regions exist.  
The first is for non-functional properties (NFP)  
and the second is for feature regions.  
Both are covered separately in the following sections.  
You can put both types in a single file and even mix the types of properties of one region.

Keys that both have in common are the following:  

Key | Type | Info
---- | ---- | ----
id | string | unique ID of the region
location | string | relative file path
nodes | int array or string | array of line numbers or range of lines as string (e.g. "10-50")
properties | JSON Object | non-functional properties or features

Sub-keys are denoted by ".".  
Relative paths are always relative to the root folder of the software project.  
This root folder is defined in the application config.  
An example could look like this: "src/main/java/Main.java".  


#### Properties NFP

The following are according sub-keys of the "properties" JSON object.  

Key | Type | Info
---- | ---- | ----
type | string | value is always "nfp"
name | string | name of the type of nfp (e.g. performance)
value | float array | array of floats, size must be equal to "features" array in app config!


#### Properties Features

The following are according sub-keys of the "properties" JSON object.  

Key | Type | Info
---- | ---- | ----
type | string | value is always "feature"
name | string | name of the feature (must be given)



## Example: NFP Region

```json
{
    "regions": [
        {
            "id": "18d59a07-c93a-3cdc-ba46-327c1882f35e",
            "location": "src/main/java/Helper.java",
            "nodes": "81-95",
            "properties": [
                {
                    "type": "nfp",
                    "name": "performance",
                    "value": [
                        0.0,
                        6.9513,
                        -6.9459,
                        -2.548,
                        120.6036,
                        -42.7765,
                        -34.8014,
                        -38.3449,
                        12.2684,
                        24.5985,
                        1.2879,
                        0.0652,
                        -0.0653
                    ]
                }
            ]
        }
    ]
}
```


## Example: Feature Region

```json

{
    "regions": [
        {
            "id": "catena_h",
            "location": "src/main/java/Catena.java",
            "nodes": [22, 51, 52, 58, 59, 72, 73, 96, 98, 99, 105, 106, 110, 111, 115, 121, 126, 129, 275, 280, 284, 288, 293, 301],
            "properties": [
                {
                    "type": "feature",
                    "name": "blake2b"
                }
            ]
        }
    ]
}
```


## Example: Both

As previously mentioned, both can be mixed in one file.  
This is shown by the following example.  

```json

{
    "regions": [
        {
            "id": "catena_h",
            "location": "src/main/java/Catena.java",
            "nodes": [22, 51, 52, 58, 59, 72, 73, 96, 98, 99, 105, 106, 110, 111, 115, 121, 126, 129, 275, 280, 284, 288, 293, 301],
            "properties": [
                {
                    "type": "feature",
                    "name": "blake2b"
                }
            ]
        },
        {
            "id": "18d59a07-c93a-3cdc-ba46-327c1882f35e",
            "location": "src/main/java/Helper.java",
            "nodes": "81-95",
            "properties": [
                {
                    "type": "nfp",
                    "name": "performance",
                    "value": [
                        0.0,
                        6.9513,
                        -6.9459,
                        -2.548,
                        120.6036,
                        -42.7765,
                        -34.8014,
                        -38.3449,
                        12.2684,
                        24.5985,
                        1.2879,
                        0.0652,
                        -0.0653
                    ]
                }
            ]
        }
    ]
}
```
