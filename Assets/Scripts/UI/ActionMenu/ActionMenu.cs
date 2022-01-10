using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRVis.Elements;
using VRVis.Spawner.Edges;

public class ActionMenu : MonoBehaviour {
    public GameObject codePopupPrefab;

    private Transform codePopupHolder;

    // Start is called before the first frame update
    void Start() {
        codePopupHolder = transform.Find("CodePopups");
        if (codePopupHolder == null) {
            Debug.LogError("ActionMenu is missing a code popup holder called 'CodePopups'!");
        }

        CodeWindowLinkButton.LinkClicked.AddListener(UpdatePopups);
    }

    // Update is called once per frame
    void Update() {
    }

    public void UpdatePopups(List<CodeWindowLink> links) {
        DeleteCodePopups();

        foreach (var link in links) {
            AddCodePopup(link);
        }
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
