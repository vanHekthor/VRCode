# Value Mapping System V2.0

Mapping files follow the naming convention `mapping_*.json`.  
This means any file starting with `mapping_` and ending width `.json` will be parsed.  
It is suggested that the following files are used:  
- mapping_nfps.json
- mapping_features.json
- mapping_edges.json

Each file should then include the respective mapping of the according type.  


<br/>

# Base Mapping

The base mapping is what you can define in such mapping files.  
As suggested in the previous section, put each entry of the following example in its respective file.  
This makes it much easier to understand where what is defined.  

```json
{
    "nfp": {
        "methods": [{}],
        "mapping": [{}]
    },


    "feature": {
        "methods": [{}],
        "mapping": [{}]
    },


    "edge": {
        "methods": [{}],
        "mapping": [{}]
    }
}
```

More about what you can define in `nfp`, `feature` and `edge` can be found in following sections.  


<br/>

# Methods Concept

In the `methods` array, the user can define new methods based on so called `base methods`.  
Each derived method needs a **unique name** or will be overwritten by the last methods of the same name.  

There are general attributes that each method entry has to provide a value for.  

**Required Attributes:**  
- `name`: name of the user defined method (used in mappings)
- `base`: base method to derive from

Derived methods need to set values of specific attributes depending on the base method.  
Required values of base methods that are not set will always have the color `black` or value `0`.  
We name such attributes required because it is required to set them to see differences.  

**General Method Definition:**  
```json
{
    "name": "Name of method",
    "base": "Base method name",
    <additional method settings>
}
```

<br/>

## Base Methods

<details><summary>LIST OF BASE METHODS</summary>

<br/>

<details><summary>COLOR METHODS</summary>

#### Color_Scale

This method maps the current value of the NFP on a color scale.  
There is always a minimum and maximum value for each NFP  
which can either be relative to the file (local) or to the whole software system (global).  
The values of the minimum and maximum are calculated but can also be set to be fixed.  
For instance, if the current value is 10, the min. is 0 and the max. 20,  
then 10 is 50% on the color scale which would result in half of the first and half of the second color.  

**Example Definition:**  
```json
{
    "name": "Green_Blue",
    "base": "Color_Scale",
    "from": "0.4, 1.0, 0.4, 0.1",
    "to": "0.4, 0.4, 1.0, 1.0"
}
```

**Additional Required Attributes:**
- `from`: first color on scale (r, g, b, a) - `float` values in range 0-1
- `to`: second color on scale (r, g, b, a) - `float` values in range 0-1

**Optional Attributes:**
- None

**Planned:**  
- `steps`: affect smoothness of mapping curve - `int` in range 0-10

Note that the following **optional attributes were moved to general mapping**:  
- `min`: set minimum NFP value (overwrites calculated one!)
- `max`: set maximum NFP value (overwrites calculated one!)


#### Color_Fixed

This method provides the possibility to set a fixed color.  
It is especially useful for the NFP heightmap, features and edges.  

**Example Definition:**  
```json
{
    "name": "Green",
    "base": "Color_Fixed",
    "color": "0.4, 1.0, 0.4, 0.1"
}
```

**Additional Required Attributes:**
- `color`: fixed color value (r, g, b, a) - `float` values in range 0-1

**Optional Attributes:**
- *None*

</details>


<br/>

<details><summary>WIDTH METHODS</summary>

#### Width_Scale

This method maps a current value of an edge on its width in a range.  
It is used together with the minimum and maximum of this edge types values.  

**Example Definition:**  
```json
{
    "name": "Width_1-10",
    "base": "Width_Scale",
    "from": 1,
    "to": 10
}
```

**Additional Required Attributes:**  
- `from`: min. range value - `float` in range 0-100
- `to`: max. range value - `float` in range 0-100

**Optional Attributes:**  
- None

**Planned:**  
- `steps`: affect smoothness of mapping curve - `int` in range 0-10

Note that the following **optional attributes were moved to general mapping**:  
- `min`: set minimum edge type value (overwrites calculated one!)
- `max`: set maximum edge type value (overwrites calculated one!)


#### Width_Fixed

This method provides the possibility to set a fixed width of an edge.  

**Example Definition:**  
```json
{
    "name": "Width_1",
    "base": "Width_Fixed",
    "value": 1
}
```

**Additional Required Attributes:**
- `color`: fixed width value - `float` in range 0-1

**Optional Attributes:**
- *None*


</details>
</details>


<br/>

# Mapping Concept

Mappings define of set of properties for a specific type of NFP, FEATURE or EDGE.  
Every attribute set will overwrite a default value existing for each property.  
This means that if you want to set just a different property `width` for one type,  
you can do this without the need of specifying all the other attribute values again.  

A mapping `name` **should always be unique** or will result in overwriting if used again.  

You also have the possibility to **overwrite the default values**.  
In case you want to do this, it is required that the **mapping name is `default`**!  
Furthermore, it is not allowed to use this mapping name again or the according entry will be ignored.  

**General Mapping Definition:**  
```json
{
    "active": true,
    "name": "Name of NFP, FEATURE or EDGE",
    <additional method settings>
}
```

The key `active` must not be present if its value should be `true` (default value).  


<br/>

## Mappings

<details><summary>LIST OF MAPPINGS</summary>

<br/>

<details><summary>NFP MAPPING</summary>

## NFP Mapping

NFP is short for `non functional property`.  
For instance, such can describe the `performance`, `memory` or similar measured properties.  
NFPs are shown by either `region marking` where a color highlights a code section,  
or by a `heightmap` shown next to a code window.  
You can change the color mapping of the marked regions.  

#### General NFP Mapping

The general mapping looks like shown below.  
As already mentioned, you can change color attributes.  

```json
{
    "nfp": {
        "methods": [],
        "mapping": [
            {
                "active": true,
                "name": "Name of non-functional property",
                "color": {
                    "default": "Color method name for region-marking visualization",
                    "heightmap": "Color method name for heightmap visualization"
                },
                "unit": "ms"
            }
        ]
    }
}
```

There are a few methods available that you can use to change the color mapping.  
It is required that the `name` of the NFP matches a property of type "nfp" in the regions files to work as desired.  
If the respective nfp region property is not defined, this mapping will be ignored.  

**Optional Attributes:**  
- `minValue`: set minimum NFP value (overwrites the calculated one!) (values below will be set to this one)
- `maxValue`: set maximum NFP value (overwrites the calculated one!) (values above will be set to this one)
- `unit`: set the unit of values

#### NFP Base Methods

To change the color of the `default` visualization (code marking) or the heightmap,  
there are the following `base` methods available to use.  

- [Color_Scale](#color_scale)
- [Color_Fixed](#color_fixed)

</details>


<br/>

<details><summary>FEATURE MAPPING</summary>

## FEATURE Mapping

Features are shown on the left side of a code window and above.  
They tell which feature affects which code regions.  
It is required to set their color to make them distinguishable.  

#### General FEATURE Mapping

The general mapping looks like shown below.  

```json
{
    "feature": {
        "methods": [],
        "mapping": [
            {
                "active": true,
                "name": "Name of the feature",
                "color": "Fixed color method name"
            }
        ]
    }
}
```
  
It is required that the `name` of the mapping matches a feature name to work as desired.  
If the respective feature is not defined, this mapping will have no effect.  

#### FEATURE Base Methods

To change the color of the feature, there are the following `base` methods available to use.  

- [Color_Fixed](#color_fixed)

</details>


<br/>

<details><summary>EDGE MAPPING</summary>

## EDGE Mapping

An edge is a connection between nodes and regions inside or across code windows.  
They can have many different attributes like color, width or even curvature.  

#### General EDGE Mapping

The general mapping looks like shown below.  

```json
{
    "edge": {
        "methods": [],
        "mapping": [
            {
                "active": true,
                "name": "Name of edge type",
                "color": {
                    "relative_to": <none / direction / value>,
                    "method": "Name of color method or nothing for <region>"
                },
                "width": "Name of width method",
                "steps": 2-100,
                "curve_strength": 0-1,
                "curve_noise": 0-0.5
            }
        ]
    }
}
```
It is required that the `name` of the mapping matches an edge name to work as desired.  
If the respective edge is not defined, this mapping will have no effect.  

**Attributes:**
- `color -> relative_to`: what the color should be relative to - `string` of either "none", direction", "value" or "region"
- `color -> method`: the color method to use (should be scale method if relative to value)
- `steps`: points the edge consists of - `int` in range 2-100
- `curve_strength`: strength of the bezier curve - `float` in range 0-1
- `curve_noise`: noise added to curve strength (make lines with same strength distinguishable) - `float` in range 0-0.5

**Optional Attributes:**  
- `minValue`: set minimum value of an edge type (overwrites the calculated one!)
- `maxValue`: set maximum value of an edge type (overwrites the calculated one!)

**Planned:**
- `color -> relative_to`: additional type "region" to use the color of the region connected to

#### EDGE Base Methods

To change the color of the edge, there are the following `base` methods available to use.  

- [Color_Scale](#color_scale)
- [Color_Fixed](#color_fixed)

To change the width of the edge, there are the following `base` methods available to use.  

- [Width_Scale](#width_scale)
- [Width_Fixed](#width_fixed)

</details>
</details>
