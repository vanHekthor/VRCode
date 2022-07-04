using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRVis.Spawner;
using VRVis.Spawner.CodeCity;
using VRVis.Spawner.File;

namespace VRVis.UI.CodeCity {
    public class SpawnedFileMarker : MonoBehaviour {

        public Transform codeCity;

        // Start is called before the first frame update
        void Start() {
            FileSpawner.GetInstance().onFileSpawned.AddListener(FileWasSpawned);
            FileSpawner.GetInstance().onFileClosed.AddListener(FileWasClosed);
        }

        private void FileWasSpawned(CodeFileReferences fileInstance) {
            var codeCityElement = FindCodeCityElementWithPath(fileInstance.GetCodeFile().GetNode().GetRelativePath());
            codeCityElement.DisplaySpawnMark();
        }

        private void FileWasClosed(CodeFileReferences fileInstance) {
            var codeCityElement = FindCodeCityElementWithPath(fileInstance.GetCodeFile().GetNode().GetRelativePath());
            codeCityElement.HideSpawnMark();
        }

        private CodeCityElement FindCodeCityElementWithPath(string relativePath) {
            var codeCityElement = codeCity.transform.Find(relativePath).GetComponent<CodeCityElement>();
            if (codeCityElement == null) {
                Debug.LogError($"Failed to find code city element with relative path '{relativePath}'!");
            }

            return codeCityElement;
        }
    }
}