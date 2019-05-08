# Code To Rich Text

This tool converts code to the [Rich Text](https://docs.unity3d.com/Manual/StyledText.html) format used by Unity.  
Written in `Python`.  


<br/>

## Tools/Frameworks Used
- [Pygments](http://pygments.org/)
- [html.parser](https://docs.python.org/3/library/html.parser.html)


<br/>

## Example Command

```
python main.py
-p "../../prepared/example/src/main/java/Main.java"
-o "exported/"
-c "schema/color1.json"
```

To retrieve detailed information of how to run the script, use:  
```
python3 main.py -h
```


The exported file will have the same name but a different extension.  
For the previous command, the output file would be `Main.java.rt`.  
With `rt` as a shortcut for `Rich Text`.  

**Result:**

```
<color=#8000FF>class</color> <color=#045FB4>Main</color> {

    <color=#8000FF>public</color> <color=#8000FF>static</color> <color=#8000FF>void</color> main(String[] args) {

        <color=#8000FF>boolean</color> OPTION_A = false;
        <color=#8000FF>boolean</color> OPTION_B = true;
        <color=#8000FF>boolean</color> OPTION_C = false;
        <color=#8000FF>boolean</color> OPTION_D = true;

        Calc c = <color=#8000FF>new</color> Calc(15,5);

        <color=#8000FF>int</color> sum = 0;
        <color=#8000FF>if</color> (OPTION_A) { sum = c.<color=#045FB4>add</color>(); }
        
        <color=#8000FF>int</color> diff = 0;
        <color=#8000FF>if</color> (OPTION_B) { diff = c.<color=#045FB4>sub</color>(); }

        <color=#8000FF>int</color> prod = 0;
        <color=#8000FF>if</color> (OPTION_C) { prod = c.<color=#045FB4>sub</color>(); }

        <color=#8000FF>float</color> quot = 0;
        <color=#8000FF>if</color> (OPTION_D) { quot = c.<color=#045FB4>div</color>(); }

        System.<color=#045FB4>out</color>.<color=#045FB4>println</color>(sum);
        System.<color=#045FB4>out</color>.<color=#045FB4>println</color>(diff);
        System.<color=#045FB4>out</color>.<color=#045FB4>println</color>(prod);
        System.<color=#045FB4>out</color>.<color=#045FB4>println</color>(quot);
    }

}
```


<br/>

## Unity Text Limits

#### Some Calculation

File Commit Version:  
https://github.com/S1r0hub/ConfigCrusher_data/commit/4db7662ca2b98f03622fc45fa6a2bb34885433a2  

Limit (found on the Internet): 65535 vertices  
-> Character Limit: 65535 / 4 = 16383 (around 16000) characters  

file: **main.py**:  
characters: **6993**  
vertices (char * 4): 27972  
-> should fit **2 times** in a Unity text component  
-> Test Result: "It does so (as expected)."  

file: **main.py.rt**  
characters: 9062  
vertices (char * 4): 36248  
-> should only fit **once** in a Unity text component  
-> Test Result: "It does so (as expected)."  

**The limit I found seems to be correct.***


<br/>

## ToDo

- [X] Export result to file
- [X] Export whole program code (currently only one file)
