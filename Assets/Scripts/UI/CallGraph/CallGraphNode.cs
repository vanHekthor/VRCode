using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VRVis.Elements;
using VRVis.IO;
using VRVis.IO.Features;
using VRVis.Spawner;
using VRVis.Spawner.Edges;
using VRVis.Spawner.File;
using VRVis.Utilities;

public class CallGraphNode : MonoBehaviour, IPointerClickHandler {

    private Color DEFAULT_COLOR = new Color(0.8962f, 0.6979f, 0);
    private Color ACTIVE_COLOR = new Color(0.2634f, 0.7057f, 0.818f);

    public string filePath;
    public string methodName;
    public int methodStartLine;
    public int methodEndLine;
    public Image backgroundImage;

    public TextMeshProUGUI label;

    public bool RefMethodIsOpen { get; private set; }

    public CodeFileReferences RefFile { get; private set; }
    private bool refFileIsOpen = false;

    public string Id { get; private set; }
    public HashSet<CallGraphNode> PreviousNodes {  get; private set; }
    public HashSet<CallGraphNode> NextNodes { get; private set; }

    public void SetId(string id) {
        Id = id;
    }

    public void AddPreviousNode(CallGraphNode node) {
        if (PreviousNodes == null) {
            PreviousNodes = new HashSet<CallGraphNode>();
        }
        PreviousNodes.Add(node);
    }

    public void AddNextNode(CallGraphNode node) {
        if (NextNodes == null) {
            NextNodes = new HashSet<CallGraphNode>();
        }
        NextNodes.Add(node);
    }

    private void Start() {
        backgroundImage = transform.Find("Button").GetComponent<Image>();
        FileSpawner.GetInstance().onFileSpawned.AddListener(HandleCodeWindowOpenEvent);
        FileSpawner.GetInstance().onFileClosed.AddListener(HandleCodeWindowCloseEvent);
    }

    public void ChangeNodeLabel(string text) {
        label.SetText(text);
    }

    public void ChangeState(Color color, bool refMethodIsOpen) {
        backgroundImage.color = color;
        RefMethodIsOpen = refMethodIsOpen;
    }

    public void OnPointerClick(PointerEventData eventData) {
        var codeCity = GameObject.FindGameObjectsWithTag("CodeCity")[0];
        var codeCityElement = CodeCityUtil.FindCodeCityElementWithPath(codeCity.transform, filePath);
        FileUtil.OpenClassFile(eventData, transform, codeCityElement.GetSNode(), OpenClassFileCallback);
    }

    private void OpenClassFileCallback(CodeFileReferences openedFileInstance) {
        if (openedFileInstance == null) return;

        LineHighlight highlight = openedFileInstance.SpawnMethodHighlight(methodStartLine, methodEndLine);

        openedFileInstance.ScrollTo(highlight.GetComponent<RectTransform>());

        //if (PreviousNodes == null && NextNodes == null) return;

        //if (PreviousNodes != null) {
        //    foreach (var previousNode in PreviousNodes) {
        //        if (previousNode.RefMethodIsOpen) {
        //            var edgeLoader = ApplicationLoader.GetInstance().GetEdgeLoader();
        //            var edges = ApplicationLoader.GetInstance().GetEdgeLoader().GetEdges(
        //                previousNode.filePath,
        //                previousNode.methodStartLine,
        //                filePath,
        //                methodStartLine,
        //                ConfigManager.GetInstance().selectedConfig);

        //            foreach (var edge in edges) {
        //                var edgeConnection = SpawnEdgeConnection(previousNode.RefFile, RefFile, edge);
        //                edgeConnection.LineHighlight = highlight;
        //            }
        //        }
        //    }
        //}

        //if (NextNodes != null) {
        //    foreach (var nextNode in NextNodes) {
        //        if (nextNode.RefMethodIsOpen) {
        //            var edgeLoader = ApplicationLoader.GetInstance().GetEdgeLoader();
        //            var edges = ApplicationLoader.GetInstance().GetEdgeLoader().GetEdges(
        //                filePath,
        //                methodStartLine,
        //                nextNode.filePath,
        //                nextNode.methodStartLine,
        //                ConfigManager.GetInstance().selectedConfig);

        //            foreach (var edge in edges) {
        //                var edgeConnection = SpawnEdgeConnection(RefFile, nextNode.RefFile, edge);
        //                edgeConnection.LineHighlight = highlight;
        //            }
        //        }
        //    }
        //}
    }

    private CodeWindowEdgeConnection SpawnEdgeConnection(CodeFileReferences startFileInstance, CodeFileReferences endFileInstance, Edge edge) {
        var fs = (FileSpawner)ApplicationLoader.GetInstance().GetSpawner("FileSpawner");
        var edgeConnection = fs.edgeSpawner.SpawnSingleEdgeConnection(startFileInstance, endFileInstance, edge);        

        return edgeConnection;
    }

    private void HandleCodeWindowOpenEvent(CodeFileReferences openedFileInstance) {
        if (openedFileInstance == null) return;

        if (refFileIsOpen) return;

        bool refFileWasSpawned = filePath == openedFileInstance.GetCodeFile().GetNode().GetRelativePath();

        if (refFileWasSpawned) {
            refFileIsOpen = true;
            RefFile = openedFileInstance;
            RefFile.onMethodHighlightSpawned.AddListener(HandleMethodHighlightSpawnEvent);
            RefFile.onMethodHighlightRemoved.AddListener(HandleMethodHighlightRemoveEvent);
        }
    }

    private void HandleCodeWindowCloseEvent(CodeFileReferences closedFileInstance) {
        if (!this.refFileIsOpen) return;

        bool refFileWasClosed = filePath == closedFileInstance.GetCodeFile().GetNode().GetRelativePath();
        if (refFileWasClosed) {
            refFileIsOpen = false;
            RefFile.onMethodHighlightSpawned.RemoveListener(HandleMethodHighlightSpawnEvent);
            RefFile.onMethodHighlightRemoved.RemoveListener(HandleMethodHighlightRemoveEvent);
            ChangeState(DEFAULT_COLOR, false);
            RefFile = null;
        }
    }

    private void HandleMethodHighlightSpawnEvent(string relativeFilePath, int startLine) {
        if (relativeFilePath == filePath && startLine == methodStartLine)
            ChangeState(ACTIVE_COLOR, true);
    }

    private void HandleMethodHighlightRemoveEvent(string relativeFilePath, int startLine) {
        if (relativeFilePath == filePath && startLine == methodStartLine)
            ChangeState(DEFAULT_COLOR, false);
    }
}
