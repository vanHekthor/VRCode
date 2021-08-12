using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using VRVis.IO;
using VRVis.Spawner;
using VRVis.Spawner.File;
using VRVis.UI.Helper;

public class CodeWindowLinkButton : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler {

    public string TargetFilePath { get; set; }
    public CodeFile TargetFile { get; set; }
    public GameObject AnchorObject { get; set; }

    private bool windowSpawning = false;
    private bool windowSpawned = false;

    private Vector3 spawnPosition; 

    

    // Start is called before the first frame update
    void Start() {

    }

    // Update is called once per frame
    void Update() {

    }

    public void OnPointerClick(PointerEventData eventData) {
        Debug.Log("Link to " + TargetFilePath + " was clicked!");

        GridElement gridElement = AnchorObject.GetComponent<GridElement>();


        StartCoroutine(SpawnFileCoroutine());
    }
    
    public void OnPointerDown(PointerEventData eventData) {
        
        
    }

    public void OnPointerUp(PointerEventData eventData) {
        
    }



    private IEnumerator SpawnFileCoroutine() {

        // get full file path from relative one
        Debug.Log("Spawning window: " + TargetFilePath);
        if (TargetFile == null) {
            Debug.LogError("Failed to spawn code window '" + TargetFile + " - file not found!");
        }

        // spawn window
        windowSpawning = true;

        FileSpawner fs = (FileSpawner)ApplicationLoader.GetInstance().GetSpawner("FileSpawner");
        if (fs) {
            // fs.SpawnFile(TargetFile.GetNode(), gameObject.transform.position, gameObject.transform.rotation, WindowSpawnedCallback);
            fs.SpawnFileNextTo(TargetFile, AnchorObject.GetComponent<CodeFileReferences>(), WindowSpawnedCallback);

        }
        else {
            WindowSpawnedCallback(false, null, "Missing FileSpawner!");
        }

        // wait until spawning is finished
        yield return new WaitUntil(() => windowSpawning == false);
    }

    /// <summary>
    /// Called after the window placement finished.
    /// </summary>
    private void WindowSpawnedCallback(bool success, CodeFile file, string msg) {

        windowSpawned = success;
        windowSpawning = false;

        if (!success) {
            string name = "";
            if (file != null && file.GetNode() != null) { name = "(" + file.GetNode().GetName() + ") "; }
            Debug.LogError("Failed to place window! " + name + msg);
            return;
        }
    }    
}
