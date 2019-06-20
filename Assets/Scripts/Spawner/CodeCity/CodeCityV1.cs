using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRVis.IO;
using VRVis.IO.Structure;

namespace VRVis.Spawner {

    public class CodeCityV1 : ASpawner {

        /// <summary>Prepares and spawns the visualization.</summary>
        public override bool SpawnVisualization() {

            StructureLoader sl = ApplicationLoader.GetInstance().GetStructureLoader();

            if (!sl.LoadedSuccessful()) {
                Debug.LogWarning("Failed to spawn code city! Loading was not successful.");
                return false;
            }

            // set root node and spawn the structure
            bool success = SpawnCodeCity(sl.GetRootNode());

            if (!success) { Debug.LogWarning("Failed to spawn code city v1."); }
            else { Debug.Log("Code City V1 successfully spawned."); }

            return success;
        }

        /// <summary>Spawns the code city visualization.</summary>
        public bool SpawnCodeCity(SNode rootNode) {

            Debug.LogError("ToDo!");
            return false;
        }

    }
}
