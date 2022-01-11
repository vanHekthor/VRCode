using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using VRVis.Elements;
using VRVis.IO;
using VRVis.Spawner;
using VRVis.Spawner.Edges;

public class CodePopup : MonoBehaviour, IPointerDownHandler {

    public Transform classNameTransform;
    public Transform methodDeclarationTransform;

    public CodeWindowLink Link { get; set; }

    private FileSpawner fs;
    private bool windowSpawning = false;
    private bool windowSpawned = false;

    void Awake() {
        classNameTransform = transform.Find("ClassName");
        if (classNameTransform == null) {
            Debug.LogError("Code popup is missing a class name object called 'ClassName' to display the class names!");
        }

        methodDeclarationTransform = transform.Find("MethodDeclaration");
        if (methodDeclarationTransform == null) {
            Debug.LogError("Code popup is missing a method declaration object called 'MethodDeclaration' to display the method declarations'!");
        }

        fs = (FileSpawner)ApplicationLoader.GetInstance().GetSpawner("FileSpawner");
        //if (fs.WindowScreen != null) {
        //    spawnWindowOntoSphere = true;
        //}
    }

    // Start is called before the first frame update
    void Start() {
        
    }

    // Update is called once per frame
    void Update() {

    }

    public void UpdateContent(CodeWindowLink link) {
        if (link != null) {
            var tmproClassName = classNameTransform.GetComponent<TextMeshProUGUI>();
            var tmproMethodDec = methodDeclarationTransform.GetComponent<TextMeshProUGUI>();

            tmproClassName.text = link.EdgeLink.GetTo().file;
            tmproMethodDec.text = "Displayling method declaration not supported yet!";

            Link = link;
        }
        else {
            Debug.LogError("Passed link is null!");
        }
    }

    public void ClickOnPopup() {
        Debug.Log("Code Popup was clicked!");
    }

    public void OnPointerDown(PointerEventData eventData) {
        ClickOnPopup();
        StartCoroutine(SpawnFileAndEdge());
    }

    private IEnumerator SpawnFileAndEdge() {
        // get full file path from relative one
        Debug.Log("Spawning window: " + Link.TargetFile.GetNode().GetPath());
        if (Link.TargetFile == null) {
            Debug.LogError("Failed to spawn code window '" + Link.TargetFile + " - file not found!");
        }

        // spawn window
        windowSpawning = true;

        if (fs) {
            fs.SpawnFileNextTo(
                Link.TargetFile,
                Link.BaseFile.GetReferences(),
                true,
                FileSpawnCallback);
        }

        // wait until spawning is finished
        yield return new WaitUntil(() => windowSpawning == false);
    }

        /// <summary>
        /// Called after the window placement finished.
        /// </summary>
        private void FileSpawnCallback(bool success, CodeFile file, string msg) {
            windowSpawned = success;
            windowSpawning = false;

            var edgeConnection = SpawnEdgeConnection();
            edgeConnection.LineHighlight = HighlightCodeAreaInTargetfile();

            if (!success) {
                string name = "";
                if (file != null && file.GetNode() != null) { name = "(" + file.GetNode().GetName() + ") "; }
                Debug.LogError("Failed to place window! " + name + msg);
            }            
        }

        private CodeWindowEdgeConnection SpawnEdgeConnection() {
            var edgeConnection = fs.edgeSpawner.SpawnSingleEdgeConnection(Link.BaseFile, Link.EdgeLink);

            return edgeConnection;
        }

        private LineHighlight HighlightCodeAreaInTargetfile() {
            int startLineToHighlight = Link.EdgeLink.GetTo().lines.from;
            int endLineToHighlight = Link.EdgeLink.GetTo().lines.to;
            var highlight = Link.TargetFile.HighlightLines(startLineToHighlight, endLineToHighlight);
            if (highlight == null) {
                Debug.LogError("Could not highlight the lines " + startLineToHighlight + " to " +
                    endLineToHighlight + " inside code window for " + Link.TargetFile.GetNode().GetName());
            }

            return highlight;
        }
}
