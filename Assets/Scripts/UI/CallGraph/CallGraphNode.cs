using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using VRVis.Elements;
using VRVis.IO;
using VRVis.Spawner;
using VRVis.Spawner.File;
using VRVis.Utilities;

public class CallGraphNode : MonoBehaviour, IPointerClickHandler {
    public string filePath;
    public string methodName;
    public int methodStartLine;
    public int methodEndLine;

    public TextMeshProUGUI label;

    public string Id { get; private set; }

    public void SetId(string id) {
        Id = id;
    }

    public void ChangeNodeLabel(string text) {
        label.SetText(text);
    }

    public void OnPointerClick(PointerEventData eventData) {
        Debug.Log("Call graph node was clicked!");
        var codeCity = GameObject.FindGameObjectsWithTag("CodeCity")[0];
        var codeCityElement = CodeCityUtil.FindCodeCityElementWithPath(codeCity.transform, filePath);
        FileUtil.OpenClassFile(eventData, transform, codeCityElement.GetSNode(), HandleCodeWindowSpawn);
    }

    private void HandleCodeWindowSpawn(CodeFileReferences fileInstance) {
        LineHighlight highlight = fileInstance.SpawnLineHighlight(methodStartLine, methodEndLine);
        fileInstance.ScrollTo(highlight.GetComponent<RectTransform>());
    }
}
