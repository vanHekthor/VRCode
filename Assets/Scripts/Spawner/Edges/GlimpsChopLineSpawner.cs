using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRVis.IO;
using VRVis.Spawner;
using VRVis.Spawner.File;
using VRVis.Utilities.Glimps;

public class GlimpsChopLineSpawner : MonoBehaviour {

    private Dictionary<string, List<GlimpsChop>> chops;

    void Start() {
        chops = ApplicationLoader.GetInstance().GetChopsLoader().Chops;
        FileSpawner.GetInstance().onFileSpawned.AddListener(SpawnChopLineHighlightInFileInstance);
    }

    private void SpawnChopLineHighlightInFileInstance(CodeFileReferences fileInstance) {
        string relativePath = fileInstance.GetCodeFile().GetNode().GetRelativePath();

        if (!chops.ContainsKey(relativePath)) { return; }

        foreach (var entry in chops[relativePath]) {
            fileInstance.SpawnLineHighlight(entry.StartLineNumber, entry.EndLineNumber, new Color(1, 0.9f, 0, 0.035f));
        }        
    }
}
