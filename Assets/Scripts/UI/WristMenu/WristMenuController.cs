using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

public class WristMenuController : MonoBehaviour {

    public static GameObject terminalInstance;

    [Tooltip("Tag to find it if it is not assigned")]
    public string terminalTag;

    [Tooltip("Name used together with tag to find it")]
    public string terminalName;

    [Tooltip("Shows/Hides the first child of the terminal transform")]
    public bool showHideFirstChild = true;

    void Awake()
    {
        // try to find by tag
        if (!terminalInstance)
        {
            GameObject[] terminals = GameObject.FindGameObjectsWithTag(terminalTag);
            foreach (GameObject terminal in terminals)
            {
                if (terminal.name == terminalName)
                {
                    terminalInstance = terminal;
                    Debug.Log("Terminal found.", terminal);
                    break;
                }
            }
        }
    }

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void OnWristButtonPress(Hand hand)
    {
        Debug.Log("SteamVR Button pressed!");

        if (!terminalInstance) { return; }

        // show/hide accordingly
        if (!showHideFirstChild)
        {
            bool state = terminalInstance.activeSelf;
            terminalInstance.SetActive(!state);
        }
        else if (terminalInstance.transform.childCount > 0)
        {
            GameObject firstChild = terminalInstance.gameObject.transform.GetChild(0).gameObject;
            if (!firstChild) { return; }
            bool state = firstChild.activeSelf;
            firstChild.SetActive(!state);
        }
    }
   
}
