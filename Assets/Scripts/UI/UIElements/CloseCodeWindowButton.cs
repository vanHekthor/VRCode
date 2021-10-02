using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using VRVis.IO;
using VRVis.Spawner;
using VRVis.Spawner.File;

public class CloseCodeWindowButton : MonoBehaviour, IPointerClickHandler {

    private GameObject codeWindow;
    private CodeFileReferences fileRefs; 

    void Start() {
        fileRefs = gameObject.GetComponentInParent<CodeFileReferences>();
        codeWindow = fileRefs.gameObject;
    }
    
    public void OnPointerClick(PointerEventData eventData) {      
        Debug.Log("Close Button clicked!");

        Debug.LogWarning("Deleting a code window...");

        FileSpawner fs = (FileSpawner)ApplicationLoader.GetInstance().GetSpawner("FileSpawner");
        if (!fs || !fs.DeleteFileWindow(fileRefs.GetCodeFile())) {
            Debug.LogWarning("Failed to delete code window!", codeWindow);
        }

        if (codeWindow != null) { Destroy(codeWindow); }
    }
}
