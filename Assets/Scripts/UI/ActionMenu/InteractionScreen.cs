using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRVis.Elements;
using VRVis.Spawner.Edges;

public class InteractionScreen : MonoBehaviour {
    public GameObject codePopupPrefab;

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
    }

    public void UpdatePopups(List<CodeWindowLink> links) {
        DeleteCodePopups();

        foreach (var link in links) {
            AddCodePopup(link);
        }
    }

    public void CodePopupWasClicked() {
        Debug.Log("CodePopupWasClicked() was invoked by CodePopup.ClickEvent!");
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
