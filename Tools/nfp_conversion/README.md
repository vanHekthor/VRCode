# Conversion Tool V1

Converts the performance measurement values  
to the region format used by the VRVis application.


# Input Data

The input data looks like the following example:  
```
main.java.components.hash.algorithms.Blake2b:update [0.0, 76.63375992297294, -76.67466663294445, 26.71510094818527, 268.0030654942728, -67.69445652120066, -99.77556367423603, -95.77070912566913, -201.26274951665116, 33.64372671005811, 2.0764834701689736, -0.03508186731318291, 0.08228594465668962]
main.java.components.hash.algorithms.Blake2b_1:bytes2long [0.0, 0.0, 0.0, -71.54556193523155, 445.90563847524345, -445.8519413917992, 0.0, 0.0, 445.8757465242013, 174.96791237448573, 9.34458996207597, 1.0852279007641021, 0.015317040731516116]
main.java.components.hash.algorithms.Blake2b:long2bytes [0.0, 76.98014981080543, -77.01889631741739, 33.46243001392362, 276.25676036289815, -68.7305877586224, -102.87392250115488, -99.8886064398439, -215.74733695886954, 35.466806956635466, 1.6453970904254023, -0.0635239219584491, -0.13727565295722258]
main.java.components.hash.algorithms.Blake2b_1:getOutputSize [0.0, 0.0, 0.0, -0.0, 0.0, -0.0, 0.0, 0.0, 0.0, 0.0, 0.0007077771777687184, 3.4499977186574947e-05, -0.0001259295097588332]
main.java.components.gamma.algorithms.SaltMix:gamma [0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.35124781841622565, -0.014704982335108203, -0.0006920765868737203, -0.0034299032034351305]
main.java.components.gamma.algorithms.SaltMix:xorshift1024star [0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.057672275721764525, -0.001959932437271758, 0.00044507080821066153, -0.000837012101034863]
main.java.components.graph.algorithms.index.IndexBRG:<init> [0.0, 0.3436478381268603, -0.343933731767942, -0.0, 0.0, -2.6828026498647097, 0.07611830402441687, 0.2734480735133341, 0.0, 0.5672617848089115, 0.035624768628941324, 0.000580268366806693, -0.002553812833204631]
...
```

For line 1, this is information encodes the file path:  
```
main.java.components.hash.algorithms
```

And this information is the file name:  
```
Blake2b
```

After that, the method name follows:
```
update
```

And then the measured values:
```json
[0.0, 76.63375992297294, -76.67466663294445, 26.71510094818527, 268.0030654942728, -67.69445652120066, -99.77556367423603, -95.77070912566913, -201.26274951665116, 33.64372671005811, 2.0764834701689736, -0.03508186731318291, 0.08228594465668962]
```

The tool now searches for this information (the file and the method).  
It extracts the position of the method in the file (start line and end line).  

To find the method, a python library [`javalang`](https://github.com/c2nes/javalang) is used.  
It can be installed using `pip`.  


# Output Data

The data output will be in the format of the regions files used by VRVis.  
The `id` of a region entry is generated using pythons `uuid3` as follows:  
```python
uuid3(uuid.NAMESPACE_X500, location + ":" + method + ":" + propertyName)
```

Encoded is the file location relative to the source directory,  
the name of the method (no signature included currently) and the property name (e.g. "performance").  


# Example Run Command

To simply convert all the data:  
```
python .\conversion.py -mp .\measurement_values\values_all_fixed.txt -pp ..\original\src_orig\ -op .\output\ -on 'converted.json' -sce ".java"
```

To convert to an ugly but smaller file (without indentation) add the flag `-ni`.  
To overwrite existing files, add the flag `-ow`.  

For more information run:  
```
python .\conversion.py -h
```

Ensure that the location source directory is the same as in the software structure.  
E.g. locations could look like "src_orig/..."  
but the folder of the loaded software structure (`root_folder` setting in app_config) is "src".  
In such a case, rename all "src_orig/..." locations to "src/...".  
Otherwise no regions will be shown.
