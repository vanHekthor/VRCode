using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRVis.Interaction.Controller {

    /// <summary>
    /// Script attached to the show/hide terminal button preview.<para/>
    /// It receives messages sent at the "Use" method and hides/shows the terminal accordingly.<para/>
    /// It searches for the terminal only once if not assigned and if found then.
    /// </summary>
    public class ShowHideTerminal : MonoBehaviour {

        public static GameObject terminalInstance;

        [Tooltip("Tag to find it if it is not assigned")]
        public string terminalTag;

        [Tooltip("Name used together with tag to find it")]
        public string terminalName;

        [Tooltip("Shows/Hides the first child of the terminal transform")]
        public bool showHideFirstChild = true;


        void Awake() {

            // try to find by tag
            if (!terminalInstance) {
                GameObject[] terminals = GameObject.FindGameObjectsWithTag(terminalTag);
                foreach (GameObject terminal in terminals) {
                    if (terminal.name == terminalName) {
                        terminalInstance = terminal;
                        Debug.Log("Terminal found.", terminal);
                        break;
                    }
                }
            }
        }


        /// <summary>
        /// "Use" function called by SendMessage("Use") call from other scripts
        /// (here the controller selection script).
        /// </summary>
        public void Use() {

            if (!terminalInstance) { return; }

            // show/hide accordingly
            if (!showHideFirstChild) {
                bool state = terminalInstance.activeSelf;
                terminalInstance.SetActive(!state);
            }
            else if (terminalInstance.transform.childCount > 0) {
                GameObject firstChild = terminalInstance.gameObject.transform.GetChild(0).gameObject;
                if (!firstChild) { return; }
                bool state = firstChild.activeSelf;
                firstChild.SetActive(!state);
            }
        }

    }
}
