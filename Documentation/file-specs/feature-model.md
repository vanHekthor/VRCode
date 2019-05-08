# Feature Model Definitions

The support of the feature model is based on [SPL Conqueror](https://github.com/se-passau/SPLConqueror).  


## XML Structure

The currently supported XML structure looks like the following:  

```xml
<vm name="model name">
    <binaryOptions>
        <configurationOption>
            <name>Option_Name</name>
            <outputString>My Option Name</outputString>
            <parent/>
            <impliedOptions>
                <option>Implied_Option_Name</option>
                <option>Op1 | Op2 | Op3</option>
            </impliedOptions>
            <excludedOptions>
                <option>Excluded_Option_Name</option>
                <option>Op4 | Op5 | Op6</option>
            </excludedOptions>
            <optional>True</optional>
        </configurationOption>
    </binaryOptions>
    <numericOptions>
        <configurationOption>
            <name>Numeric_Option_Name</name>
            <outputString>My Numeric Option Name</outputString>
            <parent>Option_Name</parent>
            <impliedOptions/>
            <excludedOptions/>
            <minValue>0.1</minValue>
            <maxValue>0.4</maxValue>
            <stepFunction>n * 2</stepFunction>
        </configurationOption>
    </numericOptions>
</vm>
```

#### XML-Element Information

| XML-Element | Type | Description
| ---- | ---- | ----
| name | string | The name of this option.<br/>Can consist of letters, numbers and only **_** as a special character.
| outputString | string | How this option should be shown/named in the application.
| parent | string | The name of the parent option or empty to use "root".
| impliedOptions | XML-Element with string | Tells which options have to be selected, when this option is selected.<br/>For instance, A => B means "B needs to be true, when A is true".<br/>Can be used to create an **OR-GROUP**.<br/>For instance, option "A" has sub-options "B", "C" and "D".<br/>If at least one should be selected, add the implied option "<option>B | C | D</option>" to A.
| excludedOptions | XML-Element with string | Tells which options can not be selected, when this option is selected.<br/>Can be used to create an **ALTERNATIVE-GROUP**.<br/>Entries like "A | B | C" will be split up and added as single entries.
| optional | boolean | **(BIN. OPTIONS ONLY)** Tells of this option is **optional** to be selected **mandatory**.
| minValue | float | **(NUM. OPTIONS ONLY)** Minimum value a numeric option can take.
| maxValue | float | **(NUM. OPTIONS ONLY)** Maximum value a numeric option can take.
| stepFunction | string | **(NUM. OPTIONS ONLY)** Tells how to continue starting from "minValue" until "maxValue" is reached ("n" can be placed and represents this numeric option value)

## Option Groups

There are **two types** of option groups supported.  

#### OR-GROUP

`OR` represents a group of options where **at least one** of them has to be selected.  
They will be represented by `<impliedOptions>` in the XML document.  
Such a group exists, if:  
- all involved options share the same parent option, and  
- the according entry in `impliedOptions` exists.  

#### ALTERNATIVE-GROUP (ALT-GROUP)

`ALT` represents a group of alternative options where **exactly one** of them can be selected at a time (`xor`).  
They will be represented by `<excludedOptions>` in the XML document.  
Such a group exists, if:  
- all involved options share the same parent option, and  
- all involved options exclude each other through according entries in `excludedOptions`.  
