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

        public enum SpawnerList {}


        // GETTER AND SETTER

        public string GetSpawnerName() { return spawner_name; }
        public void SetSpawnerName(string spawner_name) { this.spawner_name = spawner_name; }


        // FUNCTIONALITY

        /// <summary>
        /// Spawns the visualization.<para/>
        /// Implement this method if e.g. a spawner could run on startup.
        /// </summary>
        public virtual bool SpawnVisualization() { return false; }

        /// <summary>
        /// Returns a spawner or null if not implemented/available.<para/>
        /// Use the enum "SpawnerList" to retrieve names of available spawners!
        /// </summary>
        public virtual ASpawner GetSpawner(uint spawner) { return null; }

    }
}
