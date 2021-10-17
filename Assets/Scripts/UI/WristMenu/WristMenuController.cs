using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Valve.VR.InteractionSystem;

public class WristMenuController : MonoBehaviour {

    public static GameObject terminalInstance;
    public GameObject holoPad;

    public Transform tutorialScreen;

    [Tooltip("Tag to find it if it is not assigned")]
    public string terminalTag;

    [Tooltip("Name used together with tag to find it")]
    public string terminalName;

    [Tooltip("Shows/Hides the first child of the terminal transform")]
    public bool showHideFirstChild = true;

    public static UnityEvent onHoloPadOpened = new UnityEvent();

    void Awake() {
        holoPad.SetActive(false);
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

    // Use this for initialization
    void Start() {

    }

    // Update is called once per frame
    void Update() {

    }

    public void OnWristButtonPress(Hand hand) {
        Debug.Log("Wrist Button pressed!");

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

        bool holoPadState = holoPad.activeSelf;
        holoPad.SetActive(!holoPadState);

        if (holoPad.activeSelf) {
            onHoloPadOpened.Invoke();
        }

        if (tutorialScreen != null) {
            if (holoPad.activeSelf) {
                if (!configCardVisibility) {
                    tutorialScreen.localPosition = new Vector3(0.252f, 0.0f, -0.03f);
                    tutorialScreen.localRotation = Quaternion.Euler(0, 53.5f, 0);
                }
                else {
                    tutorialScreen.localPosition = new Vector3(0.252f, 0.22f, -0.03f);
                    tutorialScreen.localRotation = Quaternion.Euler(0, 53.5f, 0);
                }
            }
            else {
                if (!configCardVisibility) {
                    tutorialScreen.localPosition = new Vector3(-0.2f, 0, 0);
                    tutorialScreen.localRotation = Quaternion.Euler(0, 0, 0);
                }
                else {
                    tutorialScreen.localPosition = new Vector3(-0.25f, 0, 0);
                    tutorialScreen.localRotation = Quaternion.Euler(0, 0, 0);
                }
            }
        }
    }

    bool configCardVisibility = false;
    public void ToggleConfigCardVisibility() {
        configCardVisibility = !configCardVisibility;

        if (configCardVisibility) {
            tutorialScreen.localPosition = new Vector3(0.252f, 0.22f, -0.03f);
            tutorialScreen.localRotation = Quaternion.Euler(0, 53.5f, 0);
        }
        else {
            tutorialScreen.localPosition = new Vector3(0.252f, 0.0f, -0.03f);
            tutorialScreen.localRotation = Quaternion.Euler(0, 53.5f, 0);
        }
    }

}
