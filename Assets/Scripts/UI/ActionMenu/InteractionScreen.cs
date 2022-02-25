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

    private Transform codePopupHolder;

    void Awake() {
        codePopupHolder = transform.Find("CodePopups");
        if (codePopupHolder == null) {
            Debug.LogError("InteractionScreen is missing a code popup holder called 'CodePopups'!");
        }

        CodeWindowLinkButton.LinkClicked.AddListener(LinkWasClicked);
        CodePopup.ClickEvent.AddListener(CodePopupWasClicked);

        gameObject.SetActive(false);
    }

    public void LinkWasClicked(List<CodeWindowLink> links) {
        Debug.Log("LinkWasClicked() was invoked by CodeWindowLinkButton.LinkClicked!");
        gameObject.SetActive(true);
        UpdatePopups(links);


        if (Player.instance != null && Player.instance.hmdTransform != null) {
            Vector3 planarLookDirection = Vector3.ProjectOnPlane(Player.instance.hmdTransform.forward, Vector3.up).normalized;
            Vector3 screenPos = Player.instance.hmdTransform.position + distanceToPlayer * planarLookDirection;
            transform.position = screenPos;
            Vector3 direction = screenPos - Player.instance.hmdTransform.position;
            transform.rotation = Quaternion.LookRotation(direction);
            transform.position += horizontalOffset * transform.right;
        }
    }

    public void UpdatePopups(List<CodeWindowLink> links) {
        DeleteCodePopups();

        foreach (var link in links) {
            AddCodePopup(link);
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

    private void AddCodePopup(CodeWindowLink link) {
        var codePopupObject = Instantiate(codePopupPrefab);
        var codePopup = codePopupObject.GetComponent<CodePopup>();
        codePopupObject.transform.SetParent(codePopupHolder, false);
        codePopup.UpdateContent(link);
    }

    private void DeleteCodePopups() {
        for (int i = codePopupHolder.childCount - 1; i >= 0; i--) {
            Destroy(codePopupHolder.GetChild(i).gameObject);
        }
    }
}
