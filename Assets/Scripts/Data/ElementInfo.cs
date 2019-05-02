using Siro.IO.Structure;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


/**
 * Holds information about this element (folder or file).
 */
public class ElementInfo : MonoBehaviour {

	public string elementName;
    public string elementPath;
    public string elementFullPath;
    public DNode.DNodeTYPE nodeType = DNode.DNodeTYPE.UNKNOWN;
    public Text[] output_name;

    private string elementNameFixed = ""; // element name without the ".rt" extension


    public string GetElementName(bool fixedExtension) {

        if (fixedExtension) {

            // remove the ".rt" extension because we do not
            // load a code file directly, we load the syntax highlighted file
            if (elementNameFixed.Length == 0) {
                elementNameFixed = elementName;
                if (elementNameFixed.EndsWith(".rt")) {
                    elementNameFixed = elementName.Substring(0, elementName.Length-3);
                }
            }

            return elementNameFixed;
        }

        return elementName;
    }

    /** Path where root is the main folder. */
    public string GetElementPath() {
        return elementPath;
    }

    /** The whole path to the file. */
    public string GetElementFullPath() {
        return elementFullPath;
    }

    public DNode.DNodeTYPE GetNodeType() {
        return nodeType;
    }

    /**
     * Updates the output information if attached.
     * Called after the information was changed by the SpawnStructure.cs script.
     */
    public void UpdateView() {
        
        // update output texts for name attribute
        if (output_name.Length > 0) {
            foreach (Text textOut in output_name) {
                textOut.text = GetElementName(true);
            }
        }
    }

}
