using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using VRVis.Elements;
using VRVis.Fallback;
using VRVis.Interaction.LaserHand;
using VRVis.Interaction.LaserPointer;
using VRVis.IO;
using VRVis.IO.Structure;
using VRVis.Spawner;
using VRVis.Spawner.Edges;
using VRVis.Spawner.File;

public class CodePopup : MonoBehaviour, IPointerClickHandler {

    public Transform classNameTransform;
    public Transform methodDeclarationTransform;

    public class CodePopupClickEvent : UnityEvent {}
    public static CodePopupClickEvent ClickEvent = new CodePopupClickEvent();

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
            tmproMethodDec.text = 
                LoadLineFromFile(link.TargetFile.GetNode(), link.EdgeLink.GetTo().lines.from);

            Link = link;
        }
        else {
            Debug.LogError("Passed link is null!");
        }
    }

    public void ClickOnPopup() {
        Debug.Log("Code Popup was clicked!");
        StartCoroutine(SpawnFileAndEdge());
    }

    public void OnPointerClick(PointerEventData eventData) {
        //ClickOnPopup();
        ClickEvent.Invoke();

        // called from fallback camera (mouse click)
        MouseNodePickup.MousePickupEventData e = eventData as MouseNodePickup.MousePickupEventData;
        if (e != null) {
            MouseNodePickup mnp = e.GetMNP();
            if (e.button.Equals(PointerEventData.InputButton.Left)) {
                mnp.AttachFileToSpawn(Link.TargetFile.GetNode(), Link.BaseFileInstance.Config.Name, e.pointerCurrentRaycast.worldPosition, ToDoAfterCodeWindowPlacement);
            }
        }

        // called from laser pointer controller
        LaserPointerEventData d = eventData as LaserPointerEventData;
        if (d != null) {
            var laserPointer = d.controller.GetComponent<ViveUILaserPointerPickup>();
            var laserHand = d.controller.GetComponent<LaserHand>();

            if (laserPointer) {
                laserPointer.StartCodeWindowPlacement(Link.TargetFile.GetNode(), Link.BaseFileInstance.Config.Name, transform, ToDoAfterCodeWindowPlacement);
            }

            if (laserPointer == null) {
                if (laserHand != null) {
                    laserHand.StartCodeWindowPlacement(Link.TargetFile.GetNode(), Link.BaseFileInstance.Config.Name, transform, ToDoAfterCodeWindowPlacement);
                }
            }
        }        
    }

    private void ToDoAfterCodeWindowPlacement(CodeFileReferences spawnedFileInstance) {
        var edgeConnection = SpawnEdgeConnection(Link.BaseFileInstance, spawnedFileInstance);

        if (edgeConnection != null) {
            edgeConnection.LineHighlight = HighlightCodeAreaInTargetfile(edgeConnection.GetEndCodeFileInstance());
        }
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
                Link.BaseFileInstance.Config.Name,
                Link.BaseFileInstance,
                true,
                FileSpawnCallback);
        }

        // wait until spawning is finished
        yield return new WaitUntil(() => windowSpawning == false);
    }

        /// <summary>
        /// Called after the window placement finished.
        /// </summary>
        private void FileSpawnCallback(bool success, CodeFileReferences fileReferences, string msg) {
            windowSpawned = success;
            windowSpawning = false;            

            if (!success) {
                string name = "";
                if (fileReferences != null && fileReferences.GetCodeFile().GetNode() != null) { name = "(" + fileReferences.GetCodeFile().GetNode().GetName() + ") "; }
                Debug.LogError("Failed to place window! " + name + msg);
            }

            ClickEvent.Invoke();
    }

        private CodeWindowEdgeConnection SpawnEdgeConnection(CodeFileReferences startFileInstance, CodeFileReferences endFileInstance) {
            var edgeConnection = fs.edgeSpawner.SpawnSingleEdgeConnection(startFileInstance, endFileInstance, Link.EdgeLink);

            return edgeConnection;
        }

        private LineHighlight HighlightCodeAreaInTargetfile(CodeFileReferences fileInstance) {
            int startLineToHighlight = Link.EdgeLink.GetTo().lines.from;
            int endLineToHighlight = Link.EdgeLink.GetTo().lines.to;
            var highlight = fileInstance.SpawnLineHighlight(startLineToHighlight, endLineToHighlight);
            if (highlight == null) {
                Debug.LogError("Could not highlight the lines " + startLineToHighlight + " to " +
                    endLineToHighlight + " inside code window for " + Link.TargetFile.GetNode().GetName());
            }

            return highlight;
        }

    /// <summary>
    /// Loads the highlighted source code from the file.<para/>
    /// Returns true on success and false otherwise.
    /// </summary>
    public string LoadLineFromFile(SNode fileNode, int lineIdx) {

        // https://docs.microsoft.com/en-us/dotnet/api/system.io.fileinfo.-ctor?view=netframework-4.7.2
        string filePath = fileNode.GetFullPath();
        FileInfo fi = new FileInfo(filePath);

        if (!fi.Exists) {
            Debug.LogError("File does not exist! (" + filePath + ")");
            return null;
        }

        Debug.Log("Reading file contents...");
        // https://docs.microsoft.com/en-us/dotnet/api/system.io.fileinfo.opentext?view=netframework-4.7.2
        using (StreamReader sr = fi.OpenText()) {
            // local and temp. information
            string curLine = "";
            string output = "";
            // string sourceCode = "";
            int linesRead = 0;
            
            while ((curLine = sr.ReadLine()) != null) {

                // update counts and source code
                // sourceCode += curLine + "\n";
                linesRead++;
                if (linesRead == lineIdx) {
                    // check for '{' at the end
                    if (IsDeclarationEndLine(curLine)) {
                        return curLine.Trim();
                    }
                    
                    // count leading whitespace
                    int leadingSpaceCount = curLine.TakeWhile(Char.IsWhiteSpace).Count();
                    output = curLine.TrimStart();

                    //01public
                    //0123wegwj

                    int declarationLines = 1;
                    while ((curLine = sr.ReadLine()) != null) {
                        declarationLines++;
                        if (!curLine.Equals(string.Empty)) {
                            output += '\n' + curLine?.Substring(leadingSpaceCount).TrimEnd();
                        }
                        else {
                            output += '\n';
                        }

                        if (IsDeclarationEndLine(curLine) || declarationLines > 10) {
                            return output;
                        }
                    }

                    return output;
                }

                
            }
        }

        return null;
    }

    private bool IsDeclarationEndLine(string line) {
        if (line.LastIndexOf('{') == -1) {
            return false;
        }
        return line.TrimEnd().Substring(line.LastIndexOf('{')).Equals("{</color>");
    }
}
