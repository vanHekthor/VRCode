using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;
using VRVis.Elements;
using VRVis.Spawner.Edges;

public class InteractionScreen : MonoBehaviour {
    public GameObject codePopupPrefab;
    public float distanceToPlayer = 1.8f;
    public float horizontalOffset = -0.1f;

    public GameObject callGraph;
    public float callGraphDistanceToPlayer = 3.6f;
    public bool showCallGraph;

    private Transform codePopupHolder;
    private Transform callGraphContainer;

    void Awake() {
        codePopupHolder = transform.Find("CodePopups");
        if (codePopupHolder == null) {
            Debug.LogError("InteractionScreen is missing a code popup holder called 'CodePopups'!");
        }

        CodeWindowLinkButton.LinkClicked.AddListener(LinkWasClicked);
        CodeWindowMethodRefButton.RefClicked.AddListener(RefButtonWasClicked);
        CodePopup.ClickEvent.AddListener(CodePopupWasClicked);

        gameObject.SetActive(false);

        if (callGraph == null) return;

        var callGraphContainerObject = new GameObject("CallGraphContainer");
        callGraphContainer = callGraphContainerObject.transform;
        callGraph.transform.SetParent(callGraphContainer, false);
        callGraph.SetActive(showCallGraph);
    }

    public void LinkWasClicked(List<CodeWindowLink> links) {
        Debug.Log("LinkWasClicked() was invoked by CodeWindowLinkButton.LinkClicked!");
        gameObject.SetActive(true);
        UpdatePopups(links);

        UpdateCodePopupScreenPosition();
    }

    public void RefButtonWasClicked(List<CodeWindowMethodRef> refs) {
        Debug.Log("RefButtonWasClicked() was invoked by CodeWindowMethodRefButton.RefClicked!");
        gameObject.SetActive(true);
        UpdatePopups(refs);

        UpdateCodePopupScreenPosition();
    }

    public void UpdatePopups(List<CodeWindowLink> links) {
        DeleteCodePopups();

        foreach (var link in links) {
            AddCodePopup(link);
        }
    }

    public void UpdatePopups(List<CodeWindowMethodRef> refs) {
        DeleteCodePopups();

        foreach (var refInstance in refs) {
            AddCodePopup(refInstance);
        }
    }

    public void CodePopupWasClicked() {
        Debug.Log("CodePopupWasClicked() was invoked by CodePopup.ClickEvent!");
        CloseInteractionScreen();
    }

    public void CloseInteractionScreen() {
        Debug.Log("Interaction screen was closed!");
        gameObject.SetActive(false);
    }

    private void UpdateCodePopupScreenPosition() {
        if (Player.instance != null && Player.instance.hmdTransform != null) {
            Vector3 planarLookDirection = Vector3.ProjectOnPlane(Player.instance.hmdTransform.forward, Vector3.up).normalized;
            Vector3 screenPos = Player.instance.hmdTransform.position + distanceToPlayer * planarLookDirection;
            transform.position = screenPos;
            Vector3 direction = screenPos - Player.instance.hmdTransform.position;
            transform.rotation = Quaternion.LookRotation(direction);
            transform.position += horizontalOffset * transform.right;
        }
    }

    private void AddCodePopup(CodeWindowLink link) {
        var codePopupObject = Instantiate(codePopupPrefab);
        var codePopup = codePopupObject.GetComponent<CodePopup>();
        codePopupObject.transform.SetParent(codePopupHolder, false);
        codePopup.UpdateContent(link);
    }

    private void AddCodePopup(CodeWindowMethodRef refInstance) {
        var codePopupObject = Instantiate(codePopupPrefab);
        var codePopup = codePopupObject.GetComponent<CodePopup>();
        codePopupObject.transform.SetParent(codePopupHolder, false);
        codePopup.UpdateContent(refInstance);
    }

    private void DeleteCodePopups() {
        for (int i = codePopupHolder.childCount - 1; i >= 0; i--) {
            Destroy(codePopupHolder.GetChild(i).gameObject);
        }
    }

    public void ToggleCallGraphVisibility() {
        showCallGraph = !showCallGraph;
        if (showCallGraph) {
            UpdateCallGraphPosition();
        }
        callGraph.SetActive(showCallGraph);
    }

    private void UpdateCallGraphPosition() {
        if (Player.instance != null && Player.instance.hmdTransform != null) {
            Vector3 planarLookDirection = Vector3.ProjectOnPlane(Player.instance.hmdTransform.forward, Vector3.up).normalized;
            Vector3 screenPos = Player.instance.hmdTransform.position + callGraphDistanceToPlayer * planarLookDirection;
            callGraphContainer.transform.position = screenPos;
            Vector3 direction = screenPos - Player.instance.hmdTransform.position;
            callGraphContainer.transform.rotation = Quaternion.LookRotation(-direction);
        }
    }
}
