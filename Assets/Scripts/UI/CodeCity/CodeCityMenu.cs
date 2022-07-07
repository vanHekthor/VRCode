using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using VRVis.Elements;
using VRVis.IO;
using VRVis.JSON.Serialization.Configuration;
using VRVis.Spawner;
using VRVis.Spawner.CodeCity;
using VRVis.Spawner.File;
using VRVis.Utilities;

public class CodeCityMenu : MonoBehaviour {
    public Transform codeCity;

    private JSONGlobalConfig globalConfig;

    private CodeCityElement mainClassBuilding;

    private string relativePathToMainClass = "";

    private int mainMethodLine;

    void Start() {
        globalConfig = ApplicationLoader.GetInstance().GetConfigurationLoader().GetGlobalConfig();

        relativePathToMainClass = globalConfig.software_system.main_method.Split(':')[0];
        mainMethodLine = 0;
        int.TryParse(globalConfig.software_system.main_method.Split(':')[1], out mainMethodLine);

        mainClassBuilding = CodeCityUtil.FindCodeCityElementWithPath(codeCity, relativePathToMainClass);

        FileSpawner.GetInstance().onFileSpawned.AddListener(HandleCodeWindowSpawn);
    }

    public void HandleMainButtonClick(PointerEventData eventData) {
        var codeCityElement = CodeCityUtil.FindCodeCityElementWithPath(codeCity.transform, relativePathToMainClass);
        FileUtil.OpenClassFile(eventData, transform, codeCityElement.GetSNode(), OpenClassFileCallback);
    }

    private void OpenClassFileCallback(CodeFileReferences openedFileInstance) {
        if (openedFileInstance == null) return;

        LineHighlight highlight = openedFileInstance.SpawnMethodHighlight(mainMethodLine, mainMethodLine);
        openedFileInstance.ScrollTo(highlight.GetComponent<RectTransform>());
    }

    private void HandleCodeWindowSpawn(CodeFileReferences fileInstance) {
        //if (fileInstance.GetCodeFile().GetNode().GetRelativePath() != relativePathToMainClass) return;

        //LineHighlight highlight = fileInstance.SpawnLineHighlight(mainMethodLine, mainMethodLine);
        //fileInstance.ScrollTo(highlight.GetComponent<RectTransform>());
    }

}
