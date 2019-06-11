using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRVis.Spawner {

    /// <summary>
    /// Class that each spawner inherits from.
    /// A spawner is a class that takes care of creating a visualization.
    /// </summary>
    [System.Serializable]
    public abstract class ASpawner : MonoBehaviour {

        private string spawner_name;


        // GETTER AND SETTER

        public string GetSpawnerName() { return spawner_name; }
        public void SetSpawnerName(string spawner_name) { this.spawner_name = spawner_name; }


        /// <summary>
        /// Spawns the visualization.
        /// </summary>
        public virtual bool SpawnVisualization() { return false; }

    }
}
