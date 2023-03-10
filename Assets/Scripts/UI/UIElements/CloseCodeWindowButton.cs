using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using VRVis.IO;
using VRVis.Spawner;
using VRVis.Spawner.File;

public class CloseCodeWindowButton : MonoBehaviour, IPointerClickHandler {

    public static CloseWindowEvent closeEvent = new CloseWindowEvent();
    public class CloseWindowEvent : UnityEvent<CodeFileReferences> { }

    private GameObject codeWindow;
    private CodeFileReferences fileInstance; 

    void Start() {
        fileInstance = gameObject.GetComponentInParent<CodeFileReferences>();
        codeWindow = fileInstance.gameObject;
    }
    
    public void OnPointerClick(PointerEventData eventData) {

        Debug.Log("Close Button clicked!");

        Debug.LogWarning("Deleting a code window...");

        FileSpawner fs = (FileSpawner)ApplicationLoader.GetInstance().GetSpawner("FileSpawner");
        if (!fs || !fs.DeleteFileWindow(fileInstance)) {
            Debug.LogWarning("Failed to delete code window!", codeWindow);
        }

        if (codeWindow != null) {
            Destroy(codeWindow);
            closeEvent.Invoke(fileInstance);
        }
    }
}
