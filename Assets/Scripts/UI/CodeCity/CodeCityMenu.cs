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
        mainClassBuilding.OnPointerClick(eventData);
    }

    private void HandleCodeWindowSpawn(CodeFileReferences fileInstance) {
        if (fileInstance.GetCodeFile().GetNode().GetRelativePath() != relativePathToMainClass) return;

        LineHighlight highlight = fileInstance.SpawnLineHighlight(mainMethodLine, mainMethodLine);
        fileInstance.ScrollTo(highlight.GetComponent<RectTransform>());
    }

}
