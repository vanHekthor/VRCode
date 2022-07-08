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

    private CodeFileReferences refFile;
    private bool refFileIsOpen = false;

    public string Id { get; private set; }

    public void SetId(string id) {
        Id = id;
    }

    private void Start() {
        backgroundImage = transform.Find("Button").GetComponent<Image>();
        FileSpawner.GetInstance().onFileSpawned.AddListener(HandleCodeWindowOpenEvent);
        FileSpawner.GetInstance().onFileClosed.AddListener(HandleCodeWindowCloseEvent);
    }

    public void ChangeNodeLabel(string text) {
        label.SetText(text);
    }

    public void ChangeBackgroundColor(Color color) {
        backgroundImage.color = color;
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
    }

    private void HandleCodeWindowOpenEvent(CodeFileReferences openedFileInstance) {
        if (openedFileInstance == null) return;

        if (refFileIsOpen) return;

        bool refFileWasSpawned = filePath == openedFileInstance.GetCodeFile().GetNode().GetRelativePath();

        if (refFileWasSpawned) {
            refFileIsOpen = true;
            refFile = openedFileInstance;
            refFile.onMethodHighlightSpawned.AddListener(HandleMethodHighlightSpawnEvent);
            refFile.onMethodHighlightRemoved.AddListener(HandleMethodHighlightRemoveEvent);
        }
    }

    private void HandleCodeWindowCloseEvent(CodeFileReferences closedFileInstance) {
        if (!this.refFileIsOpen) return;

        bool refFileWasClosed = filePath == closedFileInstance.GetCodeFile().GetNode().GetRelativePath();
        if (refFileWasClosed) {
            refFileIsOpen = false;
            refFile.onMethodHighlightSpawned.RemoveListener(HandleMethodHighlightSpawnEvent);
            refFile.onMethodHighlightRemoved.RemoveListener(HandleMethodHighlightRemoveEvent);
            ChangeBackgroundColor(DEFAULT_COLOR);
            refFile = null;
        }
    }

    private void HandleMethodHighlightSpawnEvent(string relativeFilePath, int startLine) {
        if (relativeFilePath == filePath && startLine == methodStartLine)
            ChangeBackgroundColor(ACTIVE_COLOR);
    }

    private void HandleMethodHighlightRemoveEvent(string relativeFilePath, int startLine) {
        if (relativeFilePath == filePath && startLine == methodStartLine)
            ChangeBackgroundColor(DEFAULT_COLOR);
    }
}
